# AGENTS.md — AI Agent Guidelines & Architecture Manual

This document serves as the authoritative operational manual and architectural guide for AI coding agents working on the **Screen Health Guardian** codebase.

---

## 1. Project Overview

**Screen Health Guardian** is a lightweight Windows desktop application built in Python. Its primary purpose is to send periodic health break reminders (such as eye rest and posture checks) based on **actual user activity**, pausing timers automatically when the user is idle.

### Core Features & Technologies
- **Python 3.11**: Core application logic.
- **Windows API (`ctypes`)**: Low-level idle time tracking using `GetLastInputInfo`.
- **Tkinter**: Native, frameless overlay windows for break reminders and settings UI.
- **pystray & Pillow**: System tray icon integration and contextual menu.
- **PyInstaller & Inno Setup**: Packaging into a standalone `.exe` and setup installer.

---

## 2. Technical Architecture & Module Structure

```text
screen-health-guardian/
├── src/
│   ├── main.py            # Application entry point, logging, single-instance socket lock
│   ├── app.py             # Main orchestrator (ScreenHealthGuardian) managing timers & state
│   ├── idle_detector.py   # Windows API idle detection via ctypes
│   ├── alert_overlay.py   # Frameless Tkinter overlay break windows
│   ├── tray_icon.py       # System tray icon & contextual menu (pystray)
│   ├── config_manager.py  # JSON config loader/saver (%APPDATA%/ScreenHealthGuardian/config.json)
│   ├── constants.py       # Application constants, default values, UI color palettes
│   ├── i18n.py            # Localized UI text strings (English / Spanish)
│   └── ui_utils.py        # Reusable UI components & dialog helpers
├── build.bat              # Build script for PyInstaller & Inno Setup
├── run.bat                # Dev runner (console-less windowed execution)
├── requirements.txt       # Python dependencies
└── README.md              # Bilingual user documentation
```

### Key Components

1. `src/main.py`: Single-instance socket lock on `127.0.0.1:49133` to prevent duplicate app processes. Initializes logging and starts the main application loop.
2. `src/app.py`: Central controller `ScreenHealthGuardian`. Runs the core timer loop, checks idle state, updates tray tooltips, and launches break alerts.
3. `src/idle_detector.py`: Queries Windows `user32.dll` via `ctypes` to fetch `LASTINPUTINFO`. Returns elapsed idle time in seconds.
4. `src/alert_overlay.py`: Creates top-most, non-intrusive Tkinter windows for "Eye Rest" and "Posture" alerts with progress bars and auto-dismiss counters.
5. `src/config_manager.py`: Handles persistent settings stored at `%APPDATA%/ScreenHealthGuardian/config.json`.
6. `src/tray_icon.py`: Manages the system tray icon, tooltips showing time until next break, and right-click menu commands.

---

## 3. Mandatory Rules for AI Agents

### 🌐 Language & Communication
- **Source Code**: All code (variables, functions, classes), inline comments, docstrings, and documentation files MUST be in **English**.
- **User Chat**: Chat responses to the user MUST be in **Spanish**, unless explicitly requested otherwise.
- **Commit Messages**: MUST be in **English** using Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`, etc.).

### 🔒 Privacy & Security
- **Path Privacy**: NEVER leak absolute local file paths (e.g., `C:\Users\...`) into documentation, code, or commits. Always use relative paths (`./src/main.py`) or generic placeholders (`/path/to/project`).
- **Secrets**: Do not hardcode credentials, tokens, or environment-specific private paths.

### 💻 Shell & Terminal Rules (Windows PowerShell)
- **No Command Chaining**: NEVER use `&&` or `||` in terminal commands as standard PowerShell does not support them.
- **Command Separation**: Separate commands with semicolons `;` or run them in distinct tool calls.

---

## 4. Development & Build Workflows

### Running Locally
```powershell
# Install dependencies
pip install -r requirements.txt

# Run application with console
python src/main.py

# Run application without console window
pythonw src/main.py
```

### Packaging & Builds
- **Build Command**: Execute `./build.bat` in PowerShell to run PyInstaller compilation and Inno Setup installer creation.
- **Build Artifacts**: Executables, installers, or bundled artifacts generated for release MUST be stored inside a `./releases/` directory at the project root.
- **Gitignore Safety**: Ensure `./releases/` and `./dist/` are listed in `.gitignore` to prevent committing binary build outputs.

---

## 5. Critical Engineering & Threading Constraints

1. **Tkinter Main Thread Constraint**:
   Tkinter UI elements and window updates must ONLY be executed from the main Tkinter thread. Background threads (e.g., `pystray` menu callbacks or timer loops) MUST schedule UI actions using `root.after(...)` or thread-safe queues.

2. **Single Instance Locking**:
   Do NOT replace the TCP socket lock (`127.0.0.1:48123` in `src/main.py`) with Windows Named Mutexes. PyInstaller packaged environments can cause false positives with Windows Mutexes.

3. **Resource Efficiency**:
   The application runs continuously in the background. Avoid high-frequency polling loops or busy-waiting. Keep idle check intervals reasonable (1–2 seconds).

---

## 6. Verification & Change Workflow

1. **Implementation Plan & Approval**:
   Before modifying codebase files or creating new components, present a clear implementation plan in Spanish and obtain user confirmation.
2. **Pre-Commit Verification**:
   Never execute git commits without verifying that Python code compiles without syntax errors.
3. **Exhaustive Commit Summaries**:
   When committing multiple file modifications, ensure commit messages exhaustively summarize all changes made across components.
