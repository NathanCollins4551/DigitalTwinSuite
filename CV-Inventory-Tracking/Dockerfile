# Dockerfile for CV Inventory Tracking
FROM python:3.10-slim

# Install system dependencies for OpenCV headless
RUN apt-get update && apt-get install -y \
    libgl1 \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy requirements files
COPY requirements.txt .
COPY requirements.cv.txt .

# Install dependencies
RUN pip install --no-cache-dir -r requirements.txt
RUN pip install --no-cache-dir -r requirements.cv.txt

# Install flask for the annotated stream output
RUN pip install flask

# Copy the rest of the application
COPY . .

# Expose port for the annotated MJPEG stream
EXPOSE 9001

# Set environment variables
ENV DOCKER_ENV=true
ENV PYTHONUNBUFFERED=1

# Command to run the CV script
CMD ["python", "cv/main.py"]