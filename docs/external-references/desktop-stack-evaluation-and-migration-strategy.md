> **Created:** 2026-08-19
> **Last Updated:** 2026-08-19

# Windows Desktop Stacks: Technical Evaluation & Migration Strategy

## 1. Executive Summary & Verdict

| Project | Current Stack | Fit for Purpose | Recommendation | Primary Reason |
| :--- | :--- | :--- | :--- | :--- |
| **Work Activity Panel** | C# / .NET 9 / WinUI 3 | ⭐⭐⭐⭐⭐ **Optimal** | **Keep as Flagship** | Fits modern Win11 Fluent UI, rich MVVM, iCal parsing, and cloud sync. |
| **Simple PC Monitor** | C# / .NET 4.5 / WPF / P-Invoke | ⭐⭐⭐⭐☆ **High** | **Keep / Optional .NET 9 WPF** | Delivers sub-millisecond telemetry, 585 KB single-binary, and 0 dependencies. |
| **Screen Health Guardian** *(formerly Work Health Timer)* | Python 3.11 / Tkinter / PyInstaller | ⭐⭐☆☆☆ **Suboptimal** | **Migrate / Consolidate** | PyInstaller overhead (~30MB bundle), AV false positives, Tkinter UI limitations, GIL/tray concurrency. |

---

## 2. In-Depth Project Analysis

### A. Work Activity Panel (Flagship Productivity Suite)
- **Current Stack:** C# 13, .NET 9 (`net9.0-windows10.0.26100.0`), WinUI 3 (Windows App SDK 2.4), `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Hosting`, `H.NotifyIcon.WinUI`.
- **Verdict:** Perfectly aligned with requirements. It requires deep integration with Windows 11 Fluent Design, Mica backdrop, background scheduling without CPU spin, GitHub CLI interop, RFC 5545 calendar parsing, and Google Drive hash sync.
- **Migration Need:** **None**. It represents the modern standard for Windows 11 enterprise/developer desktop apps.

### B. Simple PC Monitor (Hardware & Kernel Telemetry HUD)
- **Current Stack:** C# / .NET Framework 4.5 / WPF / Win32 P-Invoke (`powrprof.dll`, `psapi.dll`, `kernel32.dll`, `DXGI`, `SetupAPI`).
- **Verdict:** Highly effective for its specific mission: instant portability (< 1 MB), zero installation requirement, and zero external runtime dependencies.
- **Optional Evolution:**
  - *Keep as-is* if universal "drop-in" portability on legacy and modern Windows without runtime installation is top priority.
  - *Migrate to .NET 9 WPF* if modern C# 13 features, better GC throughput, and modern thread scheduling are desired, keeping in mind self-contained deployment size increases unless trimmed.

### C. Screen Health Guardian (Health & Ergonomics Daemon) — **Prime Migration Candidate**
- **Current Stack:** Python 3.11, Tkinter, `ctypes` (`GetLastInputInfo`), `pystray`, `Pillow`, PyInstaller.
- **Identified Pain Points:**
  1. **Packaging Inefficiency:** A simple timer and popup utility requires a 25-35 MB PyInstaller bundle containing an entire Python interpreter runtime and shared C-libraries.
  2. **Security & Antivirus Friction:** Generic PyInstaller bootloaders frequently trigger heuristic false positives in Windows Defender and third-party AVs due to runtime unpacking behavior.
  3. **UI & Threading Friction:** Tkinter lacks modern Windows 11 styling (no Mica/Fluent, basic widgets) and requires awkward queue marshaling between the `pystray` system tray background thread and Tkinter `mainloop`.
  4. **Redundancy:** Both *Work Activity Panel* and *Screen Health Guardian* run as background daemons tracking the user's active workday.

---

## 3. Migration Roadmap for Screen Health Guardian

### Strategy 1 (Recommended): Native Integration into Work Activity Panel
- **Approach:** Absorb eye-rest and posture break timers directly as background services within `Work Activity Panel` (e.g., `HealthWellnessService`).
- **Benefits:**
  - Eliminates the need to maintain two separate background desktop daemons.
  - Reuses `Work Activity Panel`'s existing work schedule, vacation mode, system tray (`H.NotifyIcon`), and Windows 11 Mica settings UI.
  - Native C# implementation of `GetLastInputInfo` via P/Invoke (already proven in the ecosystem).
  - High testability with `xUnit` and `Moq`.

### Strategy 2: Standalone C# .NET 9 / WPF Lightweight Tray Utility
- **Approach:** Re-implement as a dedicated standalone C# tray app using WPF or Native Win32 with Native AOT compilation.
- **Benefits:**
  - Sub-5 MB memory footprint.
  - Native executable with zero AV false positives.
  - Clean separation if kept as an independent product.

### Strategy 3: Rust + Windows-rs / Tray-Icon
- **Approach:** Build with Rust, `tray-icon`, and `windows-rs` for native Win32 window overlays.
- **Benefits:**
  - Microscopic footprint (< 2 MB executable, < 5 MB RAM).
  - Guaranteed memory safety and sub-millisecond startup.

---

## 4. Key Takeaways & Architecture Decision

1. **Keep Work Activity Panel and Simple PC Monitor as-is:** Their tech stacks match their domain requirements accurately (Modern App SDK for complex workflow hub; Native C#/WPF for lightweight hardware HUD).
2. **Migrate Screen Health Guardian:** The Python + PyInstaller approach creates disproportionate packaging overhead and AV friction for what is conceptually a lightweight background daemon.
3. **Consolidation provides maximum UX value:** Merging ergonomic health reminders into `Work Activity Panel` unifies all workday automation into a single cohesive, low-overhead Windows 11 application.
