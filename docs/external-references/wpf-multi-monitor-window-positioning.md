> **Created:** 2026-08-26
> **Last Updated:** 2026-08-26

# Technical Research: WPF Window Startup Positioning & Multi-Monitor Behavior

## 1. Overview & Official Specification
According to the official Microsoft .NET / WPF documentation ([Microsoft Learn: WindowStartupLocation Enumeration](https://learn.microsoft.com/en-us/dotnet/api/system.windows.windowstartuplocation)):

| Value | Official Microsoft Specification | Multi-Monitor Behavior |
| :--- | :--- | :--- |
| **`CenterScreen`** | *"The window is positioned in the center of the screen that contains the mouse cursor."* | Tracks the current physical location of the cursor (`GetCursorPos` / `MonitorFromPoint`) at the millisecond the window is shown. |
| **`CenterOwner`** | *"The window is positioned in the center of its owner window."* | Centers relative to `Window.Owner`. If `Owner` is null, falls back to `Manual`. |
| **`Manual`** | *"The window is positioned according to its `Left` and `Top` property values."* | Full programmatic control in WPF DIPs (Device Independent Pixels). |

---

## 2. Why Screen Health Guardian Jumped Between Monitors

In the previous codebase (`v2.0.0`):
```xml
<Window ...
        WindowStartupLocation="CenterScreen">
```

Because `AlertOverlayWindow` had `Owner = null` and used `WindowStartupLocation="CenterScreen"`:
1. When the health timer fired, WPF invoked Win32 `GetCursorPos()` to find which monitor currently contained the user's cursor.
2. If the user was typing or moving the mouse on **Monitor 2**, the alert appeared on **Monitor 2**.
3. If the user had moved the mouse to **Monitor 1** a few minutes later, the subsequent alert appeared on **Monitor 1**.
4. **Limitation**: The alert only ever existed on a single monitor at any given time. If the user was reading on Monitor 2 while their mouse cursor was idle on Monitor 1, the alert appeared on Monitor 1 out of their line of sight.

---

## 3. The Multi-Monitor Architecture Solution

To display alerts on **all connected monitors simultaneously**, the application refactors window management:

1. **Switch to `WindowStartupLocation="Manual"`**:
   Disable cursor-dependent single-screen positioning.
2. **Win32 Monitor Enumeration (`DisplayManager`)**:
   Query all active monitors via `EnumDisplayMonitors`, `GetMonitorInfo`, and `GetDpiForMonitor` (`Shcore.dll`).
3. **DIP-Aware Centering per Display**:
   Calculate exact horizontal and vertical center offsets per monitor accounting for fractional DPI scale factors:
   $$\text{Left}_{\text{DIP}} = \text{WorkArea.Left}_{\text{DIP}} + \frac{\text{WorkArea.Width}_{\text{DIP}} - \text{Window.Width}_{\text{DIP}}}{2}$$
4. **Group Synchronization**:
   Spawn an overlay instance per screen and coordinate atomic dismissal so user interaction on any screen closes all overlays.

---

## 4. References & Documentation Links
- [Microsoft Learn: Window.WindowStartupLocation Property](https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.windowstartuplocation)
- [Microsoft Learn: WindowStartupLocation Enumeration](https://learn.microsoft.com/en-us/dotnet/api/system.windows.windowstartuplocation)
- [Microsoft Learn: GetDpiForMonitor function (shellscalingapi.h)](https://learn.microsoft.com/en-us/windows/win32/api/shellscalingapi/nf-shellscalingapi-getdpiformonitor)
- [Microsoft Learn: EnumDisplayMonitors function (winuser.h)](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaymonitors)
