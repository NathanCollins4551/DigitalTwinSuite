import os
import sys
import threading
import time
import requests
import yaml
from flask import Flask, Response

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import cv2
from cv.pipeline import CVPipeline
from cv.utils.fps import FPS

# Global for the latest annotated frame and counts
_latest_annotated_frame = None
_latest_counts = {
    "Zone_TopLeft": 0,
    "Zone_TopRight": 0,
    "Zone_BottomLeft": 0,
    "Zone_BottomRight": 0
}
_state_lock = threading.Lock()

app = Flask(__name__)

@app.route('/video_feed')
def video_feed():
    def generate():
        while True:
            with _state_lock:
                if _latest_annotated_frame is None:
                    time.sleep(0.01)
                    continue
                ret, jpeg = cv2.imencode('.jpg', _latest_annotated_frame)
                if not ret:
                    continue
                frame_bytes = jpeg.tobytes()
            
            yield (b'--frame\r\n'
                   b'Content-Type: image/jpeg\r\n\r\n' + frame_bytes + b'\r\n')
            time.sleep(0.05) # ~20 FPS limit
    
    return Response(generate(), mimetype='multipart/x-mixed-replace; boundary=frame')

@app.route('/api/tracking/live')
def get_live():
    with _state_lock:
        return {
            "zone_counts": _latest_counts,
            "updated_at": time.time(),
            "stale": False
        }

def run_flask():
    app.run(host='0.0.0.0', port=9002, threaded=True)

def load_yaml(path):
    with open(path, "r", encoding="utf-8") as f:
        return yaml.safe_load(f)

def push_frame_to_backend(frame, url):
    """Encodes frame to JPEG and POSTs to backend."""
    _, img_encoded = cv2.imencode('.jpg', frame)
    try:
        requests.post(url, data=img_encoded.tobytes(), headers={'Content-Type': 'image/jpeg'}, timeout=0.05)
    except:
        pass

def main():
    cfg = load_yaml("config/cv.yaml")
    cam_cfg = cfg["camera"]
    backend_cfg = cfg.get("backend", {})
    
    # In Docker, backend is accessible via service name
    is_docker = os.environ.get("DOCKER_ENV") == "true"
    if is_docker:
        base_url = os.environ.get("BACKEND_URL", "http://backend:5017")
    else:
        base_url = backend_cfg.get("base_url", "http://localhost:5017")
        
    video_update_url = f"{base_url}/api/tracking/video_update"

    # Use VIDEO_SOURCE env var if provided (from launcher)
    video_source = os.environ.get("VIDEO_SOURCE")
    if video_source:
        print(f"Using remote video source: {video_source}")
        cap = cv2.VideoCapture(video_source)
    else:
        print(f"Using local camera index: {cam_cfg.get('index', 0)}")
        cap = cv2.VideoCapture(int(cam_cfg["index"]), cv2.CAP_DSHOW)
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, int(cam_cfg["width"]))
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, int(cam_cfg["height"]))
        cap.set(cv2.CAP_PROP_FPS, int(cam_cfg["fps"]))

    pipeline = CVPipeline("config/cv.yaml", "config/zones.json")
    fps = FPS()

    if is_docker:
        print("Running in Docker (Headless). MJPEG stream active on port 9002.")
        threading.Thread(target=run_flask, daemon=True).start()
    else:
        print(f"CV Service running. Video stream at: {base_url}/api/tracking/video_feed")
        print("Press 'q' to quit.")

    display_counts = {}
    last_count_update = 0.0
    last_video_push = 0.0

    consecutive_fails = 0
    while True:
        ok, frame = cap.read()
        if not ok:
            consecutive_fails += 1
            if consecutive_fails >= 10:
                print("Camera unrecoverable after 10 retries.")
                if video_source:
                    time.sleep(2)
                    cap.open(video_source)
                    consecutive_fails = 0
                    continue
                break
            print(f"Failed to read frame (attempt {consecutive_fails}/10), retrying...")
            cv2.waitKey(100)
            continue
        consecutive_fails = 0

        annotated, debug = pipeline.step(frame)
        f = fps.tick()

        now = time.time()
        
        # Update global frame and counts for local stream
        with _state_lock:
            global _latest_annotated_frame
            _latest_annotated_frame = annotated.copy()
            if debug.get("counts"):
                _latest_counts.update(debug["counts"])

        # Push frame to backend (throttled to ~15 FPS)
        if now - last_video_push >= 0.066:
            threading.Thread(target=push_frame_to_backend, args=(annotated, video_update_url), daemon=True).start()
            last_video_push = now

        if not is_docker:
            # Throttle zone count display to once per second
            if debug.get("counts") and now - last_count_update >= 1.0:
                display_counts = dict(debug["counts"])
                last_count_update = now

            # overlay fps + counts
            cv2.putText(annotated, f"FPS: {f:.1f}", (20, 40),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.9, (255, 255, 255), 2, cv2.LINE_AA)

            if display_counts:
                y = 70
                for k, v in display_counts.items():
                    cv2.putText(annotated, f"{k}: {v}", (20, y),
                                cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2, cv2.LINE_AA)
                    y += 28

            cv2.imshow("Personnel CV (Phase 2)", annotated)
            key = cv2.waitKey(1) & 0xFF
            if key == ord("q"):
                break

    cap.release()
    if not is_docker:
        cv2.destroyAllWindows()

if __name__ == "__main__":
    main()
