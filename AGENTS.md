# AGENTS.md — AI Agent Guidelines & Architecture Manual

This document serves as the authoritative operational manual and architectural guide for AI coding agents working on the **Screen Health Guardian** codebase.

---

## 1. Project Overview

**Screen Health Guardian** is an ultra-lightweight, high-performance Windows desktop application built in **C# / .NET 9 (WPF)**. Its primary purpose is to send periodic health break reminders (eye rest 20-20-20 and posture correction) based on **actual user activity**, pausing timers automatically when the user is idle.

### Core Features & Technologies
- **C# / .NET 9 (WPF)**: Core application logic, MVVM structure, hardware-accelerated UI.
- **Windows API (P/Invoke)**: Low-level idle time tracking using `user32.dll!GetLastInputInfo` and single-instance control via Win32 `Mutex`.
- **H.NotifyIcon.Wpf**: Modern Windows notification area / system tray integration.
- **Inno Setup**: Packaging into a standalone installer (`ScreenHealthGuardian-Setup.exe`).

---

## 2. Technical Architecture & Module Structure

```text
screen-health-guardian/
├── src/
│   ├── ScreenHealthGuardian.csproj # .NET 9 WPF project configuration & NuGet packages
│   ├── App.xaml / App.xaml.cs      # Application lifecycle, Win32 Mutex single instance, Tray Icon
│   ├── Models/
│   │   └── AppConfig.cs            # Persistent JSON config model (%APPDATA%/ScreenHealthGuardian/config.json)
│   ├── Services/
│   │   ├── IdleDetector.cs         # P/Invoke user32.dll GetLastInputInfo
│   │   ├── ConfigService.cs        # Thread-safe JSON configuration manager
│   │   ├── HealthTimerService.cs   # Independent activity timer loop (1s polling, idle reset)
│   │   └── LocalizationService.cs  # Dual-language support (EN / ES) with dynamic lookup
│   └── Views/
│       ├── AlertOverlayWindow.xaml # Borderless topmost translucent overlay with countdown bar
│       └── SettingsWindow.xaml     # Modern Windows 11 preferences panel
├── build.bat                       # Build script for dotnet publish & Inno Setup installer
├── installer.iss                   # Inno Setup compilation script
└── README.md                       # Bilingual user documentation
```

---

## 3. Mandatory Rules for AI Agents

### 🌐 Language & Communication
- **Source Code**: All code (variables, functions, classes), inline comments, docstrings, and documentation files MUST be in **English**.
- **User Chat**: Chat responses to the user MUST be in **Spanish**, unless explicitly requested otherwise.
- **Commits**: MUST be in **English** using Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`, etc.).

### 🔒 Privacy & Security
- **Path Privacy**: NEVER leak absolute local file paths into documentation, code, or commits. Always use relative paths (`./src/App.xaml.cs`) or generic placeholders (`/path/to/project`).
- **Secrets**: Do not hardcode credentials or private tokens.

---

## 4. Development & Build Workflows

### Running Locally
```powershell
# Run application in dev mode
dotnet run --project src/ScreenHealthGuardian.csproj
```

### Packaging & Builds
```powershell
# Compiles single-file release and Inno Setup installer
./build.bat
```
- **Build Artifacts**: Executables, installers, or bundled artifacts generated for release MUST be stored inside `./releases/` at the project root.
