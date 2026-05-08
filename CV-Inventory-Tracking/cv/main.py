import os
import sys
import threading
import time
from flask import Flask, Response

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import cv2
from cv.pipeline import CVPipeline
from cv.utils.fps import FPS
import yaml

# Global for the latest annotated frame for streaming
_latest_annotated_frame = None
_frame_lock = threading.Lock()

app = Flask(__name__)

@app.route('/video_feed')
def video_feed():
    def generate():
        while True:
            with _frame_lock:
                if _latest_annotated_frame is None:
                    time.sleep(0.01)
                    continue
                ret, jpeg = cv2.imencode('.jpg', _latest_annotated_frame)
                if not ret:
                    continue
                frame_bytes = jpeg.tobytes()
            
            yield (b'--frame\r\n'
                   b'Content-Type: image/jpeg\r\n\r\n' + frame_bytes + b'\r\n')
            time.sleep(0.05) # ~20 FPS limit for stream
    
    return Response(generate(), mimetype='multipart/x-mixed-replace; boundary=frame')

def run_flask():
    import logging
    log = logging.getLogger('werkzeug')
    log.setLevel(logging.ERROR)
    app.run(host='0.0.0.0', port=9001, threaded=True)

def load_yaml(path):
    with open(path, "r", encoding="utf-8") as f:
        return yaml.safe_load(f)

def main():
    cfg = load_yaml("config/cv.yaml")
    cam_cfg = cfg["camera"]

    # Use VIDEO_SOURCE env var if provided (from launcher)
    video_source = os.environ.get("VIDEO_SOURCE")
    if video_source:
        print(f"Using remote video source: {video_source}")
        cap = cv2.VideoCapture(video_source)
    else:
        print(f"Using local camera index: {cam_cfg.get('index', 0)}")
        cap = cv2.VideoCapture(int(cam_cfg.get("index", 0)))
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, int(cam_cfg["width"]))
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, int(cam_cfg["height"]))
        cap.set(cv2.CAP_PROP_FPS, int(cam_cfg["fps"]))
        cap.set(cv2.CAP_PROP_AUTOFOCUS, 0)
        cap.set(cv2.CAP_PROP_FOCUS, int(cam_cfg.get("focus", 30)))
        cap.set(cv2.CAP_PROP_AUTO_EXPOSURE, 1)

    is_docker = os.environ.get("DOCKER_ENV") == "true"
    if is_docker:
        print("Running in Docker (Headless). MJPEG stream active on port 9001.")
        threading.Thread(target=run_flask, daemon=True).start()
    else:
        print("CV Service running. Press 'q' to quit.")

    pipeline = CVPipeline("config/cv.yaml", "config/zones.json")
    fps = FPS()

    global _latest_annotated_frame
    while True:
        ok, frame = cap.read()
        if not ok:
            print("Failed to read from camera.")
            # If we are using a remote source, try to reconnect
            if video_source:
                time.sleep(1)
                cap.open(video_source)
                continue
            break

        annotated, debug = pipeline.step(frame)
        f = fps.tick()

        # Update global frame for stream
        with _frame_lock:
            _latest_annotated_frame = annotated.copy()

        if not is_docker:
            # overlay fps + counts
            cv2.putText(annotated, f"FPS: {f:.1f}", (20, 40),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.9, (255, 255, 255), 2, cv2.LINE_AA)

            y = 70
            if debug.get("counts"):
                for k, v in debug["counts"].items():
                    cv2.putText(annotated, f"{k}: {v}", (20, y),
                                cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2, cv2.LINE_AA)
                    y += 28

            anomaly_count = debug.get("anomaly_count", 0)
            color = (0, 0, 255) if anomaly_count > 0 else (255, 255, 255)
            cv2.putText(annotated, f"Anomalies: {anomaly_count}", (20, y),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.7, color, 2, cv2.LINE_AA)

            cv2.imshow("Inventory CV (Phase 2)", annotated)
            key = cv2.waitKey(1) & 0xFF
            if key == ord("q"):
                break

    cap.release()
    if not is_docker:
        cv2.destroyAllWindows()

if __name__ == "__main__":
    main()

if __name__ == "__main__":
    main()