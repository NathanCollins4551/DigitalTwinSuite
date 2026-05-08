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

# --- Camera Streaming Server (Optimized & Shared) ---
class CameraCaptureThread(threading.Thread):
    """Handles physical camera access and shares frames with multiple streamers."""
    def __init__(self, cam_index):
        super().__init__(daemon=True)
        self.cam_index = cam_index
        self.cap = cv2.VideoCapture(cam_index, cv2.CAP_DSHOW)
        self.cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
        self.cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)
        self.cap.set(cv2.CAP_PROP_FPS, 30)
        self.cap.set(cv2.CAP_PROP_BUFFERSIZE, 1) # Low latency
        
        self.latest_frame = None
        self.lock = threading.Lock()
        self.running = True
        self.ref_count = 0

    def run(self):
        while self.running:
            success, frame = self.cap.read()
            if success:
                with self.lock:
                    self.latest_frame = frame
            else:
                time.sleep(0.1)
        self.cap.release()

    def get_frame(self):
        with self.lock:
            return self.latest_frame.copy() if self.latest_frame is not None else None

class CameraStreamManager:
    def __init__(self):
        self.captures = {} # cam_index -> CameraCaptureThread
        self.ports = {}    # port -> cam_index
        self.apps = {}     # port -> Flask App

    def start_stream(self, cam_index, port):
        # Stop existing on this port
        self.stop_stream(port)

        # Ensure we have a capture thread for this camera
        if cam_index not in self.captures:
            cap = CameraCaptureThread(cam_index)
            cap.start()
            self.captures[cam_index] = cap
        
        self.captures[cam_index].ref_count += 1
        self.ports[port] = cam_index

        app = Flask(f"cam_{port}")
        
        # Suppress Flask/Werkzeug banner and request logs
        import logging
        log = logging.getLogger('werkzeug')
        log.setLevel(logging.ERROR)
        
        @app.route('/video_feed')
        def video_feed():
            def generate():
                while port in self.ports:
                    idx = self.ports[port]
                    frame = self.captures[idx].get_frame()
                    if frame is None:
                        time.sleep(0.01)
                        continue
                    
                    # Optimized encoding
                    ret, jpeg = cv2.imencode('.jpg', frame, [int(cv2.IMWRITE_JPEG_QUALITY), 70])
                    if not ret:
                        continue
                        
                    yield (b'--frame\r\n'
                           b'Content-Type: image/jpeg\r\n\r\n' + jpeg.tobytes() + b'\r\n')
                    time.sleep(0.02) # ~50 FPS potential limit
            return Response(generate(), mimetype='multipart/x-mixed-replace; boundary=frame')

        def run_app():
            app.run(host='0.0.0.0', port=port, threaded=True, use_reloader=False)

        thread = threading.Thread(target=run_app, daemon=True)
        thread.start()
        self.apps[port] = {"thread": thread, "app": app}
        print(f"Streaming camera index {cam_index} on port {port}")

    def stop_stream(self, port):
        if port in self.ports:
            idx = self.ports[port]
            del self.ports[port]
            
            if idx in self.captures:
                self.captures[idx].ref_count -= 1
                if self.captures[idx].ref_count <= 0:
                    self.captures[idx].running = False
                    del self.captures[idx]
            
            if port in self.apps:
                del self.apps[port]

stream_manager = CameraStreamManager()

# --- Docker Management ---
def run_docker_command(cmd_parts, callback=None):
    try:
        full_cmd = f"docker compose {' '.join(cmd_parts)}"
        print(f"\n[LAUNCHER] --- EXECUTING DOCKER COMMAND ---")
        print(f"[LAUNCHER] Command: {full_cmd}")
        
        def run():
            process = subprocess.Popen(
                full_cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                text=True,
                shell=True,
                bufsize=1,
                encoding='utf-8',
                errors='replace'
            )
            
            print(f"[LAUNCHER] Subprocess started (PID: {process.pid})")
            for line in process.stdout:
                print(f"[DOCKER]: {line.strip()}")
                
            process.wait()
            print(f"[LAUNCHER] Command finished with exit code: {process.returncode}")
            if callback:
                callback(process.returncode)

        threading.Thread(target=run, daemon=True).start()
    except Exception as e:
        print(f"[LAUNCHER] CRITICAL EXCEPTION in run_docker_command: {e}")
        messagebox.showerror("Docker Error", f"Failed to run docker command: {str(e)}")

# --- UI Application ---
class DigitalTwinLauncher(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Digital Twin Suite Launcher")
        self.geometry("800x600")
        self.configure(bg="#f0f2f5")

        # Custom Styles
        style = ttk.Style(self)
        style.theme_use('clam')
        style.configure("TFrame", background="#f0f2f5")
        style.configure("TLabel", background="#f0f2f5", font=("Segoe UI", 10))
        style.configure("Header.TLabel", font=("Segoe UI", 18, "bold"), foreground="#1a73e8")
        style.configure("Status.TLabel", font=("Segoe UI", 9, "bold"))
        style.configure("Action.TButton", font=("Segoe UI", 10, "bold"), padding=5)
        style.configure("Toggle.TButton", font=("Segoe UI", 12), width=4)
        style.configure("Start.TButton", foreground="white", background="#34a853")
        style.map("Start.TButton", background=[('active', '#2d8e47')])
        style.configure("Stop.TButton", foreground="white", background="#ea4335")
        style.map("Stop.TButton", background=[('active', '#d93025')])

        print("[LAUNCHER] Initializing UI...")
        self.service_vars = {}
        self.camera_map = self.get_available_cameras()
        
        self.setup_ui()
        self.after(1000, self.update_status_loop)

    def get_available_cameras(self):
        available = {}
        try:
            devices = FilterGraph().get_input_devices()
            for i, name in enumerate(devices):
                available[f"{name} (Index {i})"] = i
        except Exception:
            pass
        
        if not available:
            for i in range(3):
                try:
                    cap = cv2.VideoCapture(i, cv2.CAP_DSHOW)
                    if cap.isOpened():
                        available[f"Camera {i}"] = i
                        cap.release()
                except: pass
        return available

    def setup_ui(self):
        main_container = ttk.Frame(self, padding="30")
        main_container.pack(fill=tk.BOTH, expand=True)

        # Header
        header_frame = ttk.Frame(main_container)
        header_frame.pack(fill=tk.X, pady=(0, 25))
        ttk.Label(header_frame, text="Digital Twin Suite", style="Header.TLabel").pack(side=tk.LEFT)
        
        self.global_status_lbl = ttk.Label(header_frame, text="System: Monitoring", style="Status.TLabel", foreground="#5f6368")
        self.global_status_lbl.pack(side=tk.RIGHT, pady=5)

        # Camera Configuration
        cam_frame = ttk.LabelFrame(main_container, text=" Camera Sources ", padding="15")
        cam_frame.pack(fill=tk.X, pady=(0, 20))

        cam_names = list(self.camera_map.keys())
        default_inv = cam_names[0] if cam_names else "No Cameras Found"
        default_per = cam_names[1] if len(cam_names) > 1 else (cam_names[0] if cam_names else "No Cameras Found")

        grid_cam = ttk.Frame(cam_frame)
        grid_cam.pack(fill=tk.X)
        
        ttk.Label(grid_cam, text="Inventory:").grid(row=0, column=0, sticky=tk.W, padx=(0,10))
        self.inv_cam_var = tk.StringVar(value=default_inv)
        self.inv_combo = ttk.Combobox(grid_cam, textvariable=self.inv_cam_var, values=cam_names, width=60, state="readonly")
        self.inv_combo.grid(row=0, column=1, sticky=tk.W)

        ttk.Label(grid_cam, text="Personnel:").grid(row=1, column=0, sticky=tk.W, padx=(0,10), pady=(10,0))
        self.per_cam_var = tk.StringVar(value=default_per)
        self.per_combo = ttk.Combobox(grid_cam, textvariable=self.per_cam_var, values=cam_names, width=60, state="readonly")
        self.per_combo.grid(row=1, column=1, sticky=tk.W, pady=(10,0))

        # Services List
        services_container = ttk.LabelFrame(main_container, text=" Service Control ", padding="15")
        services_container.pack(fill=tk.BOTH, expand=True, pady=(0, 20))

        # Headers for services
        header_grid = ttk.Frame(services_container)
        header_grid.pack(fill=tk.X, pady=(0, 5))
        ttk.Label(header_grid, text="SEL", width=4, font=("Segoe UI", 9, "bold")).grid(row=0, column=0)
        ttk.Label(header_grid, text="SERVICE NAME", width=35, font=("Segoe UI", 9, "bold")).grid(row=0, column=1, padx=5)
        ttk.Label(header_grid, text="STATUS", width=12, font=("Segoe UI", 9, "bold")).grid(row=0, column=2)
        ttk.Label(header_grid, text="ACTIONS", font=("Segoe UI", 9, "bold")).grid(row=0, column=3, columnspan=2, padx=20)

        self.rows_frame = ttk.Frame(services_container)
        self.rows_frame.pack(fill=tk.BOTH, expand=True)

        for i, service in enumerate(SERVICES):
            row = ttk.Frame(self.rows_frame)
            row.pack(fill=tk.X, pady=2)
            
            var = tk.BooleanVar(value=True)
            self.service_vars[service['id']] = var
            ttk.Checkbutton(row, variable=var).pack(side=tk.LEFT, padx=(5, 10))
            
            ttk.Label(row, text=service['name'], width=35).pack(side=tk.LEFT)
            
            status_lbl = ttk.Label(row, text="---", width=12, style="Status.TLabel")
            status_lbl.pack(side=tk.LEFT)
            service['status_lbl'] = status_lbl

            # Per-service toggle button (Triangle/Stop)
            toggle_btn = ttk.Button(row, text="▶", style="Toggle.TButton", command=lambda s=service: self.toggle_service(s))
            toggle_btn.pack(side=tk.LEFT, padx=10)
            service['toggle_btn'] = toggle_btn

            if "cv" in service['id']:
                view_btn = ttk.Button(row, text="Live Feed", command=lambda s=service: self.view_feed(s))
                view_btn.pack(side=tk.LEFT)

        # Bulk Actions (Selected)
        selected_frame = ttk.Frame(main_container)
        selected_frame.pack(fill=tk.X, pady=(0, 15))
        
        ttk.Label(selected_frame, text="Selected Services:", font=("Segoe UI", 10, "italic")).pack(side=tk.LEFT, padx=(0,10))
        ttk.Button(selected_frame, text="START SELECTED", style="Action.TButton", command=self.start_selected).pack(side=tk.LEFT, padx=5)
        ttk.Button(selected_frame, text="STOP SELECTED", style="Action.TButton", command=self.stop_selected).pack(side=tk.LEFT, padx=5)

        # Bottom Buttons (All)
        bottom_frame = ttk.Frame(main_container)
        bottom_frame.pack(fill=tk.X, side=tk.BOTTOM)
        
        ttk.Button(bottom_frame, text="STOP ALL", style="Action.TButton", command=self.stop_all).pack(side=tk.RIGHT, padx=5)
        ttk.Button(bottom_frame, text="START ALL", style="Action.TButton", command=self.start_all).pack(side=tk.RIGHT, padx=5)
        ttk.Button(bottom_frame, text="REFRESH", style="Action.TButton", command=self.refresh_status).pack(side=tk.LEFT)

    def view_feed(self, service):
        port = 9001 if "inventory" in service['id'] else 9002
        url = f"http://localhost:{port}/video_feed"
        win_name = f"Live Feed: {service['name']}"
        
        loading = tk.Toplevel(self)
        loading.title("Loading...")
        loading.geometry("300x120")
        loading.transient(self)
        loading.grab_set()
        
        ttk.Label(loading, text=f"Connecting to {service['name']}...", padding=10).pack()
        progress = ttk.Progressbar(loading, mode='indeterminate', length=200)
        progress.pack(pady=10)
        progress.start()
        
        def show_stream():
            cap = cv2.VideoCapture(url)
            start_time = time.time()
            window_created = False
            
            while True:
                ret, frame = cap.read()
                if ret:
                    loading.destroy()
                    break
                if time.time() - start_time > 15:
                    loading.destroy()
                    messagebox.showerror("Feed Timeout", f"Could not connect to {service['name']} feed.")
                    return
                time.sleep(0.5); cap.open(url)

            while True:
                ret, frame = cap.read()
                if not ret:
                    time.sleep(0.1)
                    ret, frame = cap.read()
                    if not ret: break
                
                cv2.imshow(win_name, frame)
                window_created = True
                
                if window_created and time.time() - start_time < 2:
                    try:
                        import ctypes
                        hwnd = ctypes.windll.user32.FindWindowW(None, win_name)
                        if hwnd: ctypes.windll.user32.SetForegroundWindow(hwnd)
                    except: pass

                if cv2.waitKey(1) & 0xFF == ord('q'): break
                try:
                    if cv2.getWindowProperty(win_name, cv2.WND_PROP_VISIBLE) < 1: break
                except: break
            
            cap.release()
            if window_created:
                try: cv2.destroyWindow(win_name)
                except: pass

        threading.Thread(target=show_stream, daemon=True).start()

    def toggle_service(self, service):
        current = service['status_lbl'].cget("text")
        if current == "RUNNING":
            print(f"[LAUNCHER] Stopping {service['id']}...")
            to_stop = service['docker_name'].split()
            run_docker_command(["stop"] + to_stop)
            
            # Release camera resources
            if "cv-inventory" in service['id']:
                stream_manager.stop_stream(8081)
            elif "cv-personnel" in service['id']:
                stream_manager.stop_stream(8082)
        else:
            print(f"[LAUNCHER] Starting {service['id']} (no-deps)...")
            if "cv-inventory" in service['id']:
                self.start_individual_stream(8081)
            elif "cv-personnel" in service['id']:
                self.start_individual_stream(8082)
            run_docker_command(["up", "-d", "--no-deps"] + service['docker_name'].split())

    def start_individual_stream(self, port):
        try:
            cam_var = self.inv_cam_var if port == 8081 else self.per_cam_var
            cam_idx = self.camera_map.get(cam_var.get(), 0)
            stream_manager.start_stream(cam_idx, port)
        except Exception as e:
            print(f"[LAUNCHER] Camera error: {e}")

    def ensure_streamers(self):
        """Used for 'Start All' or 'Start Selected' to ensure required streams are up."""
        for service in SERVICES:
            if "cv" in service['id'] and self.service_vars[service['id']].get():
                port = 8081 if "inventory" in service['id'] else 8082
                self.start_individual_stream(port)

    def start_selected(self):
        self.ensure_streamers()
        to_start = []
        for service in SERVICES:
            if self.service_vars[service['id']].get():
                to_start.extend(service['docker_name'].split())
        if to_start:
            run_docker_command(["up", "-d", "--no-deps"] + to_start)

    def stop_selected(self):
        to_stop = []
        for service in SERVICES:
            if self.service_vars[service['id']].get():
                for part in service['docker_name'].split():
                    to_stop.append(part)
                # Release camera resources
                if service['id'] == "cv-inventory":
                    stream_manager.stop_stream(8081)
                elif service['id'] == "cv-personnel":
                    stream_manager.stop_stream(8082)
        if to_stop:
            run_docker_command(["stop"] + to_stop)

    def start_all(self):
        # Ensure BOTH streams start if they are selected
        self.start_individual_stream(8081)
        self.start_individual_stream(8082)
        run_docker_command(["up", "-d"])

    def stop_all(self):
        run_docker_command(["stop"])
        stream_manager.stop_stream(8081)
        stream_manager.stop_stream(8082)

    def refresh_status(self):
        try:
            result = subprocess.run("docker compose ps --format json", capture_output=True, text=True, shell=True)
            output = result.stdout.lower()
            
            for service in SERVICES:
                is_running = False
                for part in service['docker_name'].split():
                    if f'"{part}"' in output and ("running" in output or "up" in output):
                        is_running = True
                        break
                
                if is_running:
                    service['status_lbl'].config(text="RUNNING", foreground="#34a853")
                    service['toggle_btn'].config(text="■")
                else:
                    service['status_lbl'].config(text="STOPPED", foreground="#ea4335")
                    service['toggle_btn'].config(text="▶")
        except Exception:
            pass

    def update_status_loop(self):
        self.refresh_status()
        self.after(2500, self.update_status_loop)

if __name__ == "__main__":
    app = DigitalTwinLauncher()
    app.mainloop()
