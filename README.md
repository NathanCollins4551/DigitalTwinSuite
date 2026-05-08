# Digital Twin Suite

The Digital Twin Suite is an integrated system designed for real-time monitoring and management of makerspace environments. It combines computer vision tracking, a robust .NET backend, and an interactive web dashboard to provide a comprehensive view of equipment status, material inventory, and personnel activity.

## System Architecture

The suite consists of four primary components:

### 1. Backend Service
A .NET Core API that acts as the central data hub. It manages equipment status, processes tracking events, and provides real-time updates via Server-Sent Events (SSE).

### 2. Frontend Dashboard
A Node.js and Express-based web application. It visualizes real-time data from the backend and integrates Unity 3D components for spatial visualization of the makerspace.

### 3. CV Inventory Tracking
A Python-based computer vision service utilizing YOLOv8 and OpenCV. It is responsible for tracking material inventory (such as filament spools) and equipment within defined zones.

### 4. CV Personnel Tracking
A Python-based computer vision service focused on monitoring personnel movement and safety within the environment.

## Prerequisites

To run the full suite, the following software must be installed on the host machine:

*   Docker and Docker Compose
*   Python 3.12 or higher
*   .NET 8.0 SDK (for local development)
*   Node.js (for local development)
*   Git with Git LFS support

## Getting Started

### 1. Clone the Repository
This repository uses Git LFS for large model files. Ensure Git LFS is installed before cloning.

```bash
git lfs install
git clone https://github.com/NathanCollins4551/DigitalTwinSuite.git
cd DigitalTwinSuite
```

### 2. Run with the Launcher
The easiest way to run the suite is using the provided graphical launcher. The launcher manages camera streaming, Docker containers, and service status monitoring.

#### Setup the Launcher Environment:
```bash
python -m venv .launcher_env
.launcher_env\Scripts\activate
pip install -r requirements_launcher.txt
```

#### Run the Launcher:
```bash
python launcher.py
```

Within the launcher UI, you can select your camera sources, start/stop individual services, and view live computer vision feeds.

### 3. Run with Docker Compose
Alternatively, you can start the entire infrastructure and all services using Docker Compose directly:

```bash
docker compose up -d
```

## Service Ports

*   Backend API: 5017
*   Frontend Dashboard: 3000
*   Inventory CV Feed: 9001
*   Personnel CV Feed: 9002
*   RabbitMQ Management: 15672
*   InfluxDB: 8086

## License
Refer to the LICENSE file for details on usage and distribution rights.
