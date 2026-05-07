# CV Personnel & Inventory Tracking System

A comprehensive Computer Vision system designed for real-time personnel tracking and inventory management. This project integrates YOLOv8 for object detection, QR codes for human-in-the-loop confirmation, and a FastAPI backend for state management.

## 🚀 Getting Started

### Prerequisites
* **Python 3.8+** (Using `py` launcher on Windows is recommended)
* **Git**

### Installation

1. **Clone the Repository**
   ```bash
   git clone https://github.com/NathanCollins4551/CV-Personnel-Tracking.git
   cd CV-Personnel-Tracking
   ```

2. **Install Dependencies**
   It is recommended to use a virtual environment, but you can install globally if preferred:
   ```bash
   # Install backend dependencies
   pip install -r requirements.txt

   # Install Computer Vision dependencies
   pip install -r requirements.cv.txt
   ```

## 🛠 Usage (Service Manager)

This project includes a `service.py` script to manage the Backend and CV services simultaneously.

### Commands
* **Start all services:**
  ```bash
  py service.py start
  ```
* **Check service status:**
  ```bash
  py service.py status
  ```
* **Stop all services:**
  ```bash
  py service.py stop
  ```

### Logs
When running via `service.py`, logs are generated in the root directory:
* `backend.log`: FastAPI server output.
* `cv.log`: Computer Vision pipeline output.

## 🌐 Local Access

The backend is accessible at **http://localhost:8000**. 

* **Consistency:** The backend is exposed locally on port 8000, allowing other services (like Unity or Mobile apps) to connect directly if they are on the same machine or network.

## 📁 Project Structure

* `/backend`: FastAPI application and business logic.
* `/cv`: Computer Vision pipeline, including YOLO detectors and trackers.
* `/config`: Configuration files for zones and CV parameters.
* `/training`: Scripts for dataset collection and YOLO model training.
* `service.py`: Unified service orchestrator.

## 👥 Collaboration Notes

* **Configuration:** If you need to adjust detection zones, edit `config/zones.json`.
* **Models:** The default model is `yolov8n.pt`. If you train a custom model, update the path in `config/cv.yaml`.
