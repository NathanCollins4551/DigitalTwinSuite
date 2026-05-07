@echo off
setlocal

echo ========================================
echo   Digital Twin Demo Launcher Setup
echo ========================================

:: Check if Python is installed
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Python is not installed or not in PATH. Please install Python.
    pause
    exit /b 1
)

:: Create virtual environment if it doesn't exist
if not exist ".launcher_env" (
    echo [INFO] Creating virtual environment...
    python -m venv .launcher_env
)

:: Activate virtual environment and install requirements
echo [INFO] Installing requirements...
call .launcher_env\Scripts\activate
pip install -r requirements_launcher.txt --quiet

:: Start the launcher
echo [INFO] Starting Launcher UI...
python launcher.py

echo [INFO] Launcher closed.
pause