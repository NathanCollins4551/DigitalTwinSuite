import subprocess
import time
import os
import sys
import signal
import json

# Paths
ROOT_DIR = os.path.dirname(os.path.abspath(__file__))
PID_FILE = os.path.join(ROOT_DIR, "services.pid")

def get_pids():
    if os.path.exists(PID_FILE):
        with open(PID_FILE, "r") as f:
            try:
                return json.load(f)
            except:
                return {}
    return {}

def save_pids(pids):
    with open(PID_FILE, "w") as f:
        json.dump(pids, f)

def start():
    pids = get_pids()
    if pids:
        print("Services are already running or PID file exists. Run 'stop' first.")
        return

    print("Starting Backend Service...")
    backend_proc = subprocess.Popen(
        ["py", "-m", "uvicorn", "backend.main:app", "--host", "0.0.0.0", "--port", "8000"],
        cwd=ROOT_DIR,
        stdout=open(os.path.join(ROOT_DIR, "backend.log"), "a"),
        stderr=subprocess.STDOUT
    )

    print("Starting CV Service...")
    cv_proc = subprocess.Popen(
        ["py", "-m", "cv.main"],
        cwd=ROOT_DIR,
        stdout=open(os.path.join(ROOT_DIR, "cv.log"), "a"),
        stderr=subprocess.STDOUT
    )

    save_pids({
        "backend": backend_proc.pid,
        "cv": cv_proc.pid
    })
    print(f"Services started. \nBackend PID: {backend_proc.pid}\nCV PID: {cv_proc.pid}")
    print("Logs: backend.log, cv.log")

def stop():
    pids = get_pids()
    if not pids:
        print("No services found running.")
        return

    for name, pid in pids.items():
        print(f"Stopping {name} (PID {pid})...")
        try:
            # On Windows, taskkill is more reliable for process trees
            subprocess.run(["taskkill", "/F", "/T", "/PID", str(pid)], capture_output=True)
        except Exception as e:
            print(f"Error stopping {name}: {e}")

    if os.path.exists(PID_FILE):
        os.remove(PID_FILE)
    print("All services stopped.")

def status():
    pids = get_pids()
    if not pids:
        print("Services are STOPPED.")
        return

    print("Service Status:")
    for name, pid in pids.items():
        # Check if process is still running
        result = subprocess.run(["tasklist", "/FI", f"PID eq {pid}"], capture_output=True, text=True)
        if str(pid) in result.stdout:
            print(f"  [RUNNING] {name} (PID: {pid})")
        else:
            print(f"  [FAILED]  {name} (PID: {pid}) - Process not found.")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: py service.py [start|stop|status]")
        sys.exit(1)

    cmd = sys.argv[1].lower()
    if cmd == "start":
        start()
    elif cmd == "stop":
        stop()
    elif cmd == "status":
        status()
    else:
        print(f"Unknown command: {cmd}")
