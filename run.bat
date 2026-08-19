@echo off
REM Screen Health Guardian - Run without console window
REM This script launches the timer using pythonw.exe (no terminal visible)

REM Check if virtual environment exists and activate it
if exist "%~dp0venv\Scripts\pythonw.exe" (
    start "" "%~dp0venv\Scripts\pythonw.exe" "%~dp0src\main.py"
) else (
    start "" pythonw.exe "%~dp0src\main.py"
)
