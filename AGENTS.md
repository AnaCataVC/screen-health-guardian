# AGENTS.md — AI Agent Guidelines & Architecture Manual

This document serves as the authoritative operational manual, workflow reference, and architectural guide for AI coding agents working on the **Screen Health Guardian** repository.

---

## 1. Project Overview & Structure

**Screen Health Guardian** is a lightweight, high-performance Windows desktop application designed to promote ergonomic habits and screen health through automated, activity-aware reminders (20-20-20 eye rest and posture breaks).

The repository is organized as a monorepo containing both the desktop application and its official landing website:

```text
screen-health-guardian/
├── src/                            # C# / .NET 8 WPF Desktop Application
│   ├── ScreenHealthGuardian.csproj # Project file & NuGet dependencies (H.NotifyIcon.Wpf)
│   ├── App.xaml / App.xaml.cs      # App lifecycle, Win32 Mutex single instance, Tray Icon
│   ├── Models/
│   │   └── AppConfig.cs            # Persistent JSON config model (%APPDATA%/ScreenHealthGuardian/config.json)
│   ├── Services/
│   │   ├── IdleDetector.cs         # P/Invoke user32.dll GetLastInputInfo for activity sensing
│   │   ├── ConfigService.cs        # Thread-safe JSON configuration manager
│   │   ├── HealthTimerService.cs   # Independent activity timer loop (1s polling, idle reset)
│   │   └── LocalizationService.cs  # Dynamic dual-language support (EN / ES)
│   └── Views/
│       ├── AlertOverlayWindow.xaml # Borderless topmost translucent overlay with countdown bar
│       └── SettingsWindow.xaml     # Modern Windows 11-style preferences panel
├── website/                        # Astro-based Landing Page & Promotional Web
│   ├── src/                        # Astro components, layouts, and pages
│   ├── public/                     # Static assets (favicons, screenshots, installer links)
│   ├── package.json                # Website dependencies & build scripts
│   └── astro.config.mjs            # Astro configuration
├── build.bat                       # Automated release build script (dotnet publish + Inno Setup)
├── installer.iss                   # Inno Setup compilation script for Windows installer
├── docs/                           # Architecture decisions, benchmarks & learning notes
├── releases/                       # Generated installer & standalone binaries
└── README.md                       # Bilingual project documentation (EN/ES)
```

---

## 2. Core Architecture & Tech Stack

### Desktop Application (`src/`)
- **Runtime & Framework**: C# / .NET 8 (WPF) with Windows 10/11 native look and feel.
- **Activity & Idle Detection**: Win32 P/Invoke (`user32.dll!GetLastInputInfo`) monitors true user inactivity to pause timers when the user leaves the workstation.
- **Single Instance**: Controlled via named Win32 `Mutex` (`Global\ScreenHealthGuardian_SingleInstance_Mutex`).
- **Tray & Background Mode**: System tray integration powered by `H.NotifyIcon.Wpf`. Closing settings or overlays minimizes to tray.
- **Persistence**: Configuration saved in `%APPDATA%/ScreenHealthGuardian/config.json`.

### Website & Landing Page (`website/`)
- **Framework**: Astro (Static Site Generation / High Performance).
- **Styling**: Modern, responsive CSS / Tailwind.

---

## 3. Mandatory Agent Rules & Guidelines

### 🌐 Language & Communication
- **Source Code**: All code (variables, methods, classes), inline comments, docstrings, and documentation files MUST be in **English**.
- **User Chat**: Communicate with the user in **Spanish** (unless explicitly requested otherwise).
- **Git Commits**: MUST follow Conventional Commits format in **English** (e.g., `feat: ...`, `fix: ...`, `docs: ...`, `refactor: ...`).
- **README Files**: Maintain bilingual documentation (English and Spanish).

### 🔒 Security & Privacy
- **Absolute Paths**: NEVER leak absolute computer paths (e.g., `C:\Users\...`) into documentation, commit messages, or source code. Always use relative paths (`src/App.xaml.cs`) or placeholders.
- **Secrets**: Do not hardcode credentials, sensitive tokens, or private endpoints.

### 💻 PowerShell Environment
- **Command Chaining**: NEVER use `&&` or `||` in PowerShell terminal commands. Use `;` or separate sequential command executions.
- **GitHub CLI Account**: Ensure personal repository context uses `AnaCataVC` (`gh auth switch -u AnaCataVC --hostname github.com 2>$null`).

---

## 4. Development & Build Workflows

### Desktop App (`src/`)

```powershell
# Run in development mode
dotnet run --project src/ScreenHealthGuardian.csproj

# Build single-file release and Inno Setup installer
./build.bat
```

> [!CRITICAL]
> ### 🛑 Mandatory Pre-Release Step (Before Compiling Release)
> **NEVER compile a release or run `./build.bat` without first updating and verifying the version in the Settings UI and project files.**
> 
> Before generating release artifacts or installers:
> 1. **Settings UI (Preferences Screen)**: Edit [`src/Views/SettingsWindow.xaml`](src/Views/SettingsWindow.xaml) and update the footer version label (`Screen Health Guardian vX.Y.Z`) so the user sees the correct version inside the application.
> 2. **Project File**: Update `<Version>X.Y.Z</Version>` in [`src/ScreenHealthGuardian.csproj`](src/ScreenHealthGuardian.csproj).
> 3. **Inno Setup Installer Script**: Update `AppVersion=X.Y.Z` in [`installer.iss`](installer.iss).
> 4. **Landing Page**: Update [`website/package.json`](website/package.json) and download links/badges if applicable.

> **Note on Build Artifacts**: All compiled installers (`ScreenHealthGuardian-Setup.exe`) and production binaries must be output to `./releases/`.

### Website (`website/`)

```powershell
# Start dev server from website directory
cd website; npm run dev

# Alternative: Start dev server in background mode
cd website; npx astro dev --background

# Manage the background server
npx astro dev stop
npx astro dev status
npx astro dev logs

# Build production static site
cd website; npm run build
```

#### Official Astro Documentation & Guides
Consult these references before working on website features:
- [Astro Documentation Overview](https://docs.astro.build)
- [Routing, Dynamic Routes & Middleware](https://docs.astro.build/en/guides/routing/)
- [Astro Components Guide](https://docs.astro.build/en/basics/astro-components/)
- [UI Framework Components (React, Vue, Svelte)](https://docs.astro.build/en/guides/framework-components/)
- [Content Collections & Data Management](https://docs.astro.build/en/guides/content-collections/)
- [Styling & Tailwind CSS](https://docs.astro.build/en/guides/styling/)
- [Internationalization (i18n)](https://docs.astro.build/en/guides/internationalization/)

---

## 5. Testing & Quality Standards

- **Clean Code & Modularity**: Adhere to SOLID principles, DRY, and KISS.
- **UI & Overlay Behavior**: Overlays must be topmost, non-intrusive, and support multi-monitor positioning gracefully.
- **Timer Precision & Thread Safety**: Background activity checks must run asynchronously without blocking the WPF UI thread (`DispatcherTimer` / `Task.Run`).
