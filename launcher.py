import os
import subprocess
import threading
import time
import tkinter as tk
from tkinter import ttk, messagebox
import cv2
from flask import Flask, Response
from pygrabber.dshow_graph import FilterGraph

# --- Configuration ---
SERVICES = [
    {"id": "infrastructure", "name": "Core Backend & Databases", "docker_name": "db redis influxdb rabbitmq backend"},
    {"id": "frontend", "name": "Frontend (Node.js)", "docker_name": "frontend"},
    {"id": "cv-inventory", "name": "CV Inventory Tracking", "docker_name": "cv-inventory"},
    {"id": "cv-personnel", "name": "CV Personnel Tracking", "docker_name": "cv-personnel"},
]

# --- Camera Streaming Server ---
class CameraStreamer:
    def __init__(self):
        self.apps = {}
        self.caps = {}
        self.threads = {}

    def start_stream(self, cam_index, port):
        if port in self.apps:
            self.stop_stream(port)

        app = Flask(f"cam_{port}")
        cap = cv2.VideoCapture(cam_index, cv2.CAP_DSHOW) # Use DSHOW for better Windows compatibility
        
        # Set resolution to 720p
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)
        cap.set(cv2.CAP_PROP_FPS, 30)
        
        self.caps[port] = cap

        @app.route('/video_feed')
        def video_feed():
            def generate():
                while True:
                    if port not in self.caps:
                        break
                    success, frame = self.caps[port].read()
                    if not success:
                        time.sleep(0.1)
                        continue
                    ret, jpeg = cv2.imencode('.jpg', frame)
                    if not ret:
                        continue
                    yield (b'--frame\r\n'
                           b'Content-Type: image/jpeg\r\n\r\n' + jpeg.tobytes() + b'\r\n')
                    time.sleep(0.03)
            return Response(generate(), mimetype='multipart/x-mixed-replace; boundary=frame')

        def run_app():
            app.run(host='0.0.0.0', port=port, threaded=True, use_reloader=False)

        thread = threading.Thread(target=run_app, daemon=True)
        thread.start()
        self.apps[port] = app
        self.threads[port] = thread
        print(f"Streaming camera {cam_index} on port {port}")

    def stop_stream(self, port):
        if port in self.caps:
            c = self.caps[port]
            del self.caps[port]
            c.release()

streamer = CameraStreamer()

# --- Docker Management ---
def run_docker_command(cmd_parts):
    try:
        # Build the full command string for shell execution
        full_cmd = f"docker compose {' '.join(cmd_parts)}"
        print(f"\n[LAUNCHER] --- EXECUTING DOCKER COMMAND ---")
        print(f"[LAUNCHER] Command: {full_cmd}")
        
        def run():
            # Use bufsize=1 for line-buffered output
            process = subprocess.Popen(
                full_cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT, # Merge stderr into stdout
                text=True,
                shell=True,
                bufsize=1,
                encoding='utf-8',
                errors='replace' # Handle any remaining invalid characters gracefully
            )
            
            print(f"[LAUNCHER] Subprocess started (PID: {process.pid})")
            
            # Read output line by line in real-time
            for line in process.stdout:
                print(f"[DOCKER]: {line.strip()}")
                
            process.wait()
            print(f"[LAUNCHER] Command finished with exit code: {process.returncode}")
            if process.returncode != 0:
                print(f"[LAUNCHER] ERROR: Command failed. Check Docker Desktop is running.")

        threading.Thread(target=run, daemon=True).start()
    except Exception as e:
        print(f"[LAUNCHER] CRITICAL EXCEPTION in run_docker_command: {e}")
        messagebox.showerror("Docker Error", f"Failed to run docker command: {str(e)}")

# --- UI Application ---
class DigitalTwinLauncher(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Digital Twin Demo Launcher")
        self.geometry("650x500")
        self.resizable(False, False)

        print("[LAUNCHER] Initializing UI...")
        self.service_vars = {}
        self.camera_map = self.get_available_cameras()
        print(f"[LAUNCHER] Discovered {len(self.camera_map)} camera(s)")
        
        self.setup_ui()
        # Wait a moment for UI to render before first refresh
        print("[LAUNCHER] Starting status monitoring loop...")
        self.after(1000, self.update_status_loop)

    def get_available_cameras(self):
        """Returns a dict of { 'Camera Name': index }"""
        available = {}
        try:
            print("[LAUNCHER] Scanning for cameras via pygrabber...")
            devices = FilterGraph().get_input_devices()
            for i, name in enumerate(devices):
                available[f"{name} (Index {i})"] = i
        except Exception as e:
            print(f"[LAUNCHER] pygrabber failed: {e}")
        
        # Fallback if pygrabber finds nothing but OpenCV might
        if not available:
            print("[LAUNCHER] Falling back to OpenCV camera scan...")
            for i in range(3):
                try:
                    cap = cv2.VideoCapture(i, cv2.CAP_DSHOW)
                    if cap.isOpened():
                        available[f"Camera {i}"] = i
                        cap.release()
                except:
                    pass
        return available

    def setup_ui(self):
        main_frame = ttk.Frame(self, padding="20")
        main_frame.pack(fill=tk.BOTH, expand=True)

        ttk.Label(main_frame, text="Digital Twin Suite Management", font=("Helvetica", 16, "bold")).pack(pady=(0, 20))

        # Camera Selection
        cam_frame = ttk.LabelFrame(main_frame, text="Camera Configuration", padding="10")
        cam_frame.pack(fill=tk.BOTH, pady=(0, 20))

        cam_names = list(self.camera_map.keys())
        default_inv = cam_names[0] if cam_names else "No Cameras Found"
        default_per = cam_names[1] if len(cam_names) > 1 else (cam_names[0] if cam_names else "No Cameras Found")

        ttk.Label(cam_frame, text="Inventory Cam:").grid(row=0, column=0, sticky=tk.W)
        self.inv_cam_var = tk.StringVar(value=default_inv)
        ttk.Combobox(cam_frame, textvariable=self.inv_cam_var, values=cam_names, width=50).grid(row=0, column=1, padx=10, sticky=tk.W)

        ttk.Label(cam_frame, text="Personnel Cam:").grid(row=1, column=0, sticky=tk.W, pady=(5,0))
        self.per_cam_var = tk.StringVar(value=default_per)
        ttk.Combobox(cam_frame, textvariable=self.per_cam_var, values=cam_names, width=50).grid(row=1, column=1, padx=10, sticky=tk.W, pady=(5,0))

        # Services List
        self.services_frame = ttk.LabelFrame(main_frame, text="Services", padding="10")
        self.services_frame.pack(fill=tk.BOTH, expand=True)

        for i, service in enumerate(SERVICES):
            var = tk.BooleanVar(value=True)
            self.service_vars[service['id']] = var
            
            ttk.Checkbutton(self.services_frame, variable=var).grid(row=i, column=0, sticky=tk.W)
            ttk.Label(self.services_frame, text=service['name']).grid(row=i, column=1, sticky=tk.W, padx=5)
            
            status_lbl = ttk.Label(self.services_frame, text="Checking...", foreground="gray")
            status_lbl.grid(row=i, column=2, sticky=tk.W, padx=20)
            service['status_lbl'] = status_lbl

            if "cv" in service['id']:
                btn = ttk.Button(self.services_frame, text="View Feed", command=lambda s=service: self.view_feed(s))
                btn.grid(row=i, column=3, sticky=tk.E, padx=5)

        # Control Buttons
        btn_frame = ttk.Frame(main_frame, padding="10")
        btn_frame.pack(fill=tk.X, pady=(20, 0))

        ttk.Button(btn_frame, text="START SELECTED", command=self.start_selected).pack(side=tk.LEFT, padx=5)
        ttk.Button(btn_frame, text="STOP ALL", command=self.stop_all).pack(side=tk.LEFT, padx=5)
        ttk.Button(btn_frame, text="REFRESH STATUS", command=self.refresh_status).pack(side=tk.RIGHT, padx=5)

    def view_feed(self, service):
        port = 9001 if "inventory" in service['id'] else 9002
        url = f"http://localhost:{port}/video_feed"
        win_name = f"Live Feed: {service['name']}"
        print(f"[LAUNCHER] Opening feed for {service['id']} via {url}")
        
        def show_stream():
            cap = cv2.VideoCapture(url)
            start_time = time.time()
            while True:
                ret, frame = cap.read()
                if not ret:
                    if time.time() - start_time > 15:
                        print(f"[LAUNCHER] Feed timeout for {service['id']}")
                        break
                    time.sleep(1.0)
                    cap.open(url)
                    continue
                
                cv2.imshow(win_name, frame)
                if cv2.waitKey(1) & 0xFF == ord('q'):
                    break
                
                try:
                    if cv2.getWindowProperty(win_name, cv2.WND_PROP_VISIBLE) < 1:
                        break
                except:
                    break
                    
            cap.release()
            try:
                cv2.destroyWindow(win_name)
            except:
                pass

        threading.Thread(target=show_stream, daemon=True).start()
        messagebox.showinfo("Feed View", f"Opening feed for {service['name']}.\n(Ensure service is RUNNING first)\nPress 'q' in video window to close.")

    def start_selected(self):
        print("\n[LAUNCHER] Start sequence initiated.")
        # 1. Start camera streamers
        try:
            inv_idx = self.camera_map.get(self.inv_cam_var.get(), 0)
            per_idx = self.camera_map.get(self.per_cam_var.get(), 0)
            print(f"[LAUNCHER] Mapping Cam 8081 -> Index {inv_idx}, Cam 8082 -> Index {per_idx}")
            streamer.start_stream(inv_idx, 8081)
            streamer.start_stream(per_idx, 8082)
        except Exception as e:
            print(f"[LAUNCHER] Camera streaming setup error: {e}")

        # 2. Start Docker services
        to_start = []
        for service in SERVICES:
            if self.service_vars[service['id']].get():
                to_start.extend(service['docker_name'].split())

        if not to_start:
            print("[LAUNCHER] No services selected to start.")
            return

        print(f"[LAUNCHER] Selected Docker services: {to_start}")
        run_docker_command(["up", "-d"] + to_start)

    def stop_all(self):
        print("\n[LAUNCHER] Stop all initiated.")
        run_docker_command(["stop"])

    def refresh_status(self):
        try:
            # Silent PS check
            result = subprocess.run(
                "docker compose ps --format json", 
                capture_output=True, 
                text=True, 
                shell=True
            )
            
            output = result.stdout.lower()
            if not output and result.stderr:
                # Only print error if it's not a 'no containers' warning
                if "no such service" not in result.stderr.lower():
                    print(f"[LAUNCHER] Status Check Error: {result.stderr.strip()}")

            for service in SERVICES:
                is_running = False
                for container_name in service['docker_name'].split():
                    # Check if the container name is present and has an active status
                    if container_name in output and ("running" in output or "up" in output or "healthy" in output):
                        is_running = True
                        break
                
                if is_running:
                    service['status_lbl'].config(text="RUNNING", foreground="green")
                else:
                    service['status_lbl'].config(text="STOPPED", foreground="red")
        except Exception as e:
            # Avoid spamming status exceptions unless serious
            pass

    def update_status_loop(self):
        self.refresh_status()
        self.after(3000, self.update_status_loop)

if __name__ == "__main__":
    app = DigitalTwinLauncher()
    app.mainloop()
