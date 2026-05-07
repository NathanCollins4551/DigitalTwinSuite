# Digital Twin Suite

Welcome to the **Digital Twin Suite**, a comprehensive system for real-time monitoring and management of makerspace environments. This suite integrates computer vision, a robust backend, and an interactive frontend dashboard.

## 🚀 Overview

The suite consists of several core components:

### 1. [Backend](./backend)
A .NET-based API that serves as the central hub for data management, printer status tracking, and message handling.
- **Technology:** .NET Core, C#
- **Key Features:** RESTful API, SSE (Server-Sent Events) for real-time updates.

### 2. [Frontend Dashboard](./frontend)
A modern web application to visualize makerspace data, printer statuses, and computer vision alerts.
- **Technology:** Node.js, Express, JavaScript, Unity Integration.
- **Key Features:** Real-time dashboards, Unity 3D visualization.

### 3. [CV Inventory Tracking](./CV-Inventory-Tracking)
Computer vision system focused on tracking materials (e.g., filament spools) and equipment.
- **Technology:** Python, YOLOv8, OpenCV.
- **Key Features:** Object detection, QR code scanning, inventory zone management.

### 4. [CV Personnel Tracking](./CV-Personnel-Tracking)
Computer vision system for monitoring personnel activity and safety within the makerspace.
- **Technology:** Python, YOLOv8, OpenCV.
- **Key Features:** People detection, activity monitoring.

---

## 🛠️ Getting Started

### Prerequisites
- [Docker & Docker Compose](https://www.docker.com/products/docker-desktop/)
- [Python 3.12+](https://www.python.org/downloads/)
- [.NET SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/)

### Quick Start
1. **Clone the repository:**
   ```bash
   git clone https://github.com/NathanCollins4551/DigitalTwinSuite.git
   cd DigitalTwinSuite
   ```

2. **Run using the Launcher:**
   The suite includes a convenient Python-based launcher to start all services.
   ```bash
   # Create a virtual environment for the launcher (optional but recommended)
   python -m venv .launcher_env
   source .launcher_env/bin/activate  # or .launcher_env\Scripts\activate on Windows
   pip install -r requirements_launcher.txt

   # Start the suite
   python launcher.py
   ```

3. **Run using Docker Compose:**
   Alternatively, you can run the entire suite using Docker:
   ```bash
   docker-compose up --build
   ```

---

## 📦 Git LFS
This project uses **Git LFS** (Large File Storage) to manage large model files (`.pt`) and media assets. Ensure you have Git LFS installed before cloning:
```bash
git lfs install
git clone https://github.com/NathanCollins4551/DigitalTwinSuite.git
```

---

## 📄 License
[Insert License Information Here]
