> **Created:** 2026-08-26
> **Last Updated:** 2026-08-26

# Adversarial Stress-Test: Configurable Multi-Monitor Alert Overlay System

## 1. Executive Summary & Scope
This stress-test evaluates the introduction of the user-configurable display mode (`"all"` for all monitors vs `"active"` for the screen containing the cursor) across the 5 standard attack vectors.

---

## 2. Five-Dimension Attack Vector Dissection

### Vector 1: Premortem & Operational Failure Modes
- **Out-of-Bounds Cursor Coordinates [Major / Hardening Required]**:
  - *Failure Mode*: If the cursor is positioned at a virtual display seam, off-screen edge, or during an RDP session disconnect, `GetCursorPos` might yield coordinates $(X, Y)$ that do not lie within any monitor's `rcMonitor` bounding box.
  - *Mitigation*: In `DisplayManager.GetCursorDisplay()`, iterate through all monitors with point-in-rect checks (`rcWork.Left <= pt.X && pt.X < rcWork.Right ...`). If no bounding match is found, fallback immediately to the primary display (`d.IsPrimary`) or `displays[0]`.
- **Corrupted or Unknown `DisplayMode` Values in Config [Major / Hardening Required]**:
  - *Failure Mode*: If `config.json` is corrupted or edited manually with an invalid string (e.g. `"display_mode": "triple"` or `null`), the app could crash with an unhandled branch exception or undefined behavior.
  - *Mitigation*: Use defensive switch pattern matching in `App.xaml.cs`:
    ```csharp
    IReadOnlyList<DisplayInfo> targetDisplays = config.DisplayMode?.ToLowerInvariant() switch
    {
        "active" => new[] { DisplayManager.GetCursorDisplay() },
        _ => DisplayManager.GetDisplays() // Safely defaults to "all"
    };
    ```

---

### Vector 2: Concurrency, Race Conditions & State Drift
- **Mid-Flight Cursor Flick During Alert Initialization [Minor / Hardening Required]**:
  - *Failure Mode*: A user rapidly flicks their mouse across monitor borders at the exact millisecond the health timer triggers. If cursor position is polled at multiple different steps (e.g., once for monitor detection, once for centering), window calculations could be split across different screens.
  - *Mitigation*: Capture `GetCursorPos` once atomically during `GetCursorDisplay()`, resolve the target `DisplayInfo` record, and compute window coordinates strictly from that snapshot.
- **Dynamic Configuration Updates while Alert is Visible [Minor / Verified]**:
  - *Failure Mode*: Changing the display mode in Preferences while an alert overlay is open could cause desynchronized states.
  - *Mitigation*: Preferences changes apply to future timer events; existing open overlays maintain their session lifecycle until dismissed.

---

### Vector 3: Cost & Resource Explosion (Performance & Memory)
- **Win32 Polling Overhead [Negligible / Verified]**:
  - *Analysis*: `GetCursorPos` and `EnumDisplayMonitors` execute in $< 0.5\text{ms}$ on modern hardware and are called strictly on-demand (only once per 20-45 minutes when an alert triggers), resulting in zero CPU overhead during idle or normal operation.

---

### Vector 4: Security, Authorization & Abuse Vectors
- **Accessibility & Focus Lockout [Major / Hardening Required]**:
  - *Analysis*: Regardless of whether the alert appears on 1 or $N$ screens, pressing `ESC` or clicking anywhere on the window dismisses the alert immediately.
  - *Mitigation*: Ensure `Window_KeyDown` and `Window_MouseDown` handlers are active in both `"all"` and `"active"` modes.

---

### Vector 5: Developer Friction & Backward Compatibility
- **Backward Compatibility Guarantee [Critical / Verified]**:
  - *Analysis*: Existing users with `config.json` lacking the `display_mode` key will automatically deserialize with `DisplayMode = "all"` without schema migration errors or data loss.
- **Dynamic i18n Sync in Settings UI [Minor / Hardening Required]**:
  - *Failure Mode*: If the user switches language between English and Spanish while the Settings window is open, the Display Mode dropdown could remain untranslated if not refreshed.
  - *Mitigation*: Update `ApplyLanguage()` in `SettingsWindow.xaml.cs` to re-bind the localized labels for both `ComboBoxItem` options.

---

## 3. Threat Assessment & Hardening Checklist

| Vulnerability | Severity | Hardening Status |
| :--- | :--- | :--- |
| Out-of-bounds cursor coordinates | **Major** | Mitigated via Point-in-Rect matching + Primary Display fallback |
| Invalid / corrupted JSON value | **Major** | Mitigated via fallback switch defaulting unknown values to `"all"` |
| Dynamic UI translation desync | **Minor** | Mitigated via `ApplyLanguage()` updating `CmbDisplayMode` items |
| Dual-monitor dismissal race | **Critical** | Mitigated via atomic `Interlocked.Exchange` in `OnSessionDismissed` |
