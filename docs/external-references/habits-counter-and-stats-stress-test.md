> **Created:** 2026-11-04
> **Last Updated:** 2026-11-04
> **Status:** Active
> **Scope:** Habit Tracking & Compliance Stats Engine (`ScreenHealthGuardian`)

# Adversarial Stress-Test: Habit Tracking, Weekly Retention & 20s Active Alert Engine

## 1. Executive Summary & Threat Vectors

This adversarial stress-test dissects the proposed habit tracking architecture for Screen Health Guardian, specifically:
1. **The 20-Second Guided Active Rest Overlay (Option A)**.
2. **Weekly vs. Lifetime metrics aggregation with a 6-month rolling daily retention window**.
3. **Decoupled persistence in `%APPDATA%/ScreenHealthGuardian/stats.json`**.

The evaluation tests the proposal across 5 core attack dimensions: operational failure modes, concurrency/race conditions, resource explosion, security/abuse vectors, and developer friction.

---

## 2. Adversarial Breakdown (The 5 Attack Vectors)

### Vector 1: Premortem & Operational Failure Modes
- **Threat 1.1: The "Looking Away" Sensory Disconnect (Audio Muted)**
  - *Vulnerability:* The 20-20-20 rule requires the user to physically look at an object 20 feet (6m) away. If `SoundEnabled = false` or the system volume is muted, the user cannot know when the 20 seconds have elapsed without repeatedly checking the screen, defeating the ergonomic purpose.
  - *Severity:* **[Major / Hardening Required]**
  - *Mitigation:* Provide a clear, distinct completion sound that honors system preferences, but also introduce a generous post-countdown visual completion card (persisting for 3–5 seconds with an uplifting message *"Rest completed! Take a deep breath"*) so returning late to the screen confirms success.
- **Threat 1.2: Conflicting `AlertAutoDismissSec` in Preferences**
  - *Vulnerability:* In `SettingsWindow`, the user can configure `AlertAutoDismissSec` down to 5 seconds. If applied to the eye rest alert, the overlay would auto-close before the 20s countdown can finish.
  - *Severity:* **[Critical / Blocker]**
  - *Mitigation:* Decouple the 20s eye rest timer from `AlertAutoDismissSec`. The eye rest countdown is strictly fixed at 20 seconds (the medical rule). `AlertAutoDismissSec` applies only to posture alerts and the post-completion grace buffer.
- **Threat 1.3: Workstation Lock / Sleep Mid-Countdown**
  - *Vulnerability:* If the user locks their PC (`Win + L`) or walks away and the OS sleeps while the 20s overlay is active, the app could wake up hours later and falsely credit a "Respected" break.
  - *Severity:* **[Minor / Observability]**
  - *Mitigation:* `IdleDetector` integration: if inactivity exceeds `IdleThresholdSec` during the countdown, or if the overlay loses focus due to session lock (`SessionSwitchEvent`), abort the countdown and do not falsely register a completed break.

---

### Vector 2: Concurrency, Race Conditions & State Drift
- **Threat 2.1: Multi-Monitor Session Desynchronization & Quadruple Counting**
  - *Vulnerability:* With 3 or 4 monitors, `App.xaml.cs` spawns 3 or 4 `AlertOverlayWindow` instances. If the user clicks "Skip" on monitor 1 while monitor 2 completes its timer, uncoordinated callbacks could record conflicting outcomes.
  - *Severity:* **[Critical / Blocker]**
  - *Mitigation:* Enforce an atomic session state machine using `Interlocked.CompareExchange` inside `App.xaml.cs`. Only the first terminal transition (`Respected`, `Discarded`, or `Snoozed`) commits the result to `StatsService`, immediately broadcasting dismissal to all sibling overlays.
- **Threat 2.2: Concurrent File Writes During Sudden Power Cut or OS Shutdown**
  - *Vulnerability:* Writing directly to `stats.json` can truncate or corrupt the file if the machine reboots or sleeps mid-write (`0-byte file`).
  - *Severity:* **[Major / Hardening Required]**
  - *Mitigation:* Atomic write pattern: serialize to `stats.json.tmp` -> flush stream -> `File.Move(..., overwrite: true)`. Wrap deserialization in a `try/catch` that automatically backs up corrupt files (`stats.corrupt.json`) and instantiates a pristine fallback state.

---

### Vector 3: Cost & Resource Explosion (Disk, CPU & Memory)
- **Threat 3.1: 6-Month Rolling Retention Scale & CPU Impact**
  - *Vulnerability:* Storing 180 individual daily buckets in a single JSON file. Will serializing and deserializing 180 days on every 20-minute alert cause micro-stutters in the WPF UI?
  - *Footprint Analysis:*
    - 1 day payload: ~120 bytes.
    - 180 days (6 months): 180 * 120 B ≈ 21.6 KB.
    - `System.Text.Json` deserialization of 22 KB takes `< 0.8 ms` on modern hardware.
  - *Severity:* **[Minor / Low Risk]**
  - *Mitigation:* Prune records older than 180 days during `StatsService.RecordEvent()`. Execute disk flushes on a background task (`Task.Run`) to keep the WPF UI Dispatcher thread completely unblocked.

---

### Vector 4: Security, Authorization & Abuse Vectors
- **Threat 4.1: Plaintext Metric Tampering or Malformed Keys**
  - *Vulnerability:* A user manually edits `%APPDATA%/ScreenHealthGuardian/stats.json` and inserts invalid dates, negative integers, or extreme numbers.
  - *Severity:* **[Minor / Observability]**
  - *Mitigation:* Defensive parsing: clamp numeric values (`Math.Max(0, value)`), validate ISO date formatting (`yyyy-MM-dd`), and ignore unparseable keys without throwing.

---

### Vector 5: Developer Friction & UX Cognitive Load
- **Threat 5.1: Settings Window Clutter**
  - *Vulnerability:* Adding too many charts or dense tables into `SettingsWindow` will overwhelm the existing 500x650px preferences window.
  - *Severity:* **[Major / Hardening Required]**
  - *Mitigation:* Use a compact, scannable Fluent Card with a clean tab/toggle: `[ Esta Semana / This Week ]` vs `[ Histórico Total / Lifetime ]`. Display 3 clean metric badges and a stacked proportional progress bar rather than heavy third-party graphing libraries.

---

## 3. Required Implementation Preconditions (Hardening Checklist)

1. [x] **Decouple 20s Eye Rest**: Do not bind eye rest countdown to `AlertAutoDismissSec`.
2. [x] **Atomic Multi-Monitor Gate**: Ensure `Interlocked` state locks single registration across displays.
3. [x] **Rolling 180-Day Pruning**: Automatic date threshold filter (`cutoff = Today.AddDays(-180)`).
4. [x] **Atomic Temp-File Persistence**: Write to `stats.json.tmp` before moving to `stats.json`.
5. [x] **Zero-Lag UI**: Asynchronous disk serialization offloaded from the UI Dispatcher thread.
