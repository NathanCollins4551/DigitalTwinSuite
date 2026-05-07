from ultralytics import YOLO

def main():
    model = YOLO("yolov8s.pt")  # or yolov8n.pt for faster CPU
    model.train(
        data="datasets/inventory_v1/data.yaml",
        epochs=80,
        imgsz=640,
        batch=8,
        device="cpu",
        project="runs/inventory",
        name="spool_detector_v1",
    )

if __name__ == "__main__":
    main()