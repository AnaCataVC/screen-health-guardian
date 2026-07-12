"""Work Health Timer — Main application orchestrator.

Manages two independent activity timers (eye rest and posture correction),
detects user idle time via Windows API, and shows overlay alerts when
active usage thresholds are reached.
"""

import tkinter as tk
from tkinter import font as tkfont
import logging
import sys
import winreg
import queue

from constants import (
    APP_NAME,
    APP_VERSION,
    CHECK_INTERVAL_SEC,
    DEFAULT_LOOK_AWAY_INTERVAL_MIN,
    DEFAULT_POSTURE_INTERVAL_MIN,
    DEFAULT_IDLE_THRESHOLD_SEC,
    DEFAULT_ALERT_AUTO_DISMISS_SEC,
    DEFAULT_SOUND_ENABLED,
    DEFAULT_AUTO_START,
    COLOR_BG_DARK,
    COLOR_BG_CARD,
    COLOR_ACCENT_BLUE,
    COLOR_ACCENT_PURPLE,
    COLOR_TEXT_PRIMARY,
    COLOR_TEXT_SECONDARY,
    COLOR_BUTTON_DISMISS,
    FONT_FAMILY,
)
from config_manager import ConfigManager
from idle_detector import IdleDetector
from alert_overlay import show_look_away_alert, show_posture_alert
from tray_icon import TrayIcon

logger = logging.getLogger(__name__)

# Windows Registry key for auto-start
REGISTRY_KEY = r"Software\Microsoft\Windows\CurrentVersion\Run"
REGISTRY_VALUE_NAME = "WorkHealthTimer"


class WorkHealthTimer:
    """Main application class that orchestrates all components.

    Architecture:
        - Tkinter mainloop runs on the main thread (required by Tk).
        - pystray tray icon runs in a daemon thread.
        - Activity is polled every CHECK_INTERVAL_SEC using root.after().
        - Two independent counters track active time for each alert type.
        - Counters reset when the user goes idle beyond the threshold.
    """

    def __init__(self) -> None:
        self.config = ConfigManager()
        self.idle_detector = IdleDetector()

        # Active time counters (in seconds)
        self._active_seconds_look_away: float = 0.0
        self._active_seconds_posture: float = 0.0

        # Pause state
        self._is_paused: bool = False

        # Current active alert (prevent stacking)
        self._current_alert = None

        # Setup tkinter root (hidden)
        self.root = tk.Tk()
        self.root.withdraw()
        self.root.protocol("WM_DELETE_WINDOW", self._quit)

        # Setup system tray icon
        self.tray = TrayIcon(
            on_pause_resume=self._toggle_pause,
            on_settings=self._show_settings,
            on_quit=self._quit,
            get_status=self._get_status_text,
        )

        # Apply auto-start setting from config
        self._sync_auto_start()

        logger.info(
            "%s v%s started. Look away: %d min, Posture: %d min",
            APP_NAME,
            APP_VERSION,
            self.config.get("look_away_interval_min", DEFAULT_LOOK_AWAY_INTERVAL_MIN),
            self.config.get("posture_interval_min", DEFAULT_POSTURE_INTERVAL_MIN),
        )

    def run(self) -> None:
        """Start the application main loop."""
        self._ui_queue = queue.Queue()
        self._process_queue()
        
        self.tray.start()
        self._schedule_check()
        self.root.mainloop()

    def _process_queue(self) -> None:
        """Process thread-safe UI requests from the tray icon."""
        try:
            while True:
                task = self._ui_queue.get_nowait()
                task()
        except queue.Empty:
            pass
        finally:
            self.root.after(100, self._process_queue)

    # ── Timer Loop ──────────────────────────────────────────────────

    def _schedule_check(self) -> None:
        """Schedule the next activity check."""
        self.root.after(CHECK_INTERVAL_SEC * 1000, self._check_activity)

    def _check_activity(self) -> None:
        """Main timer loop — runs every CHECK_INTERVAL_SEC.

        1. If paused, skip and reschedule.
        2. Check if user is active (idle < threshold).
        3. If active, increment both counters.
        4. If idle beyond threshold, reset counters.
        5. If a counter exceeds its interval, show the corresponding alert.
        """
        if not self._is_paused:
            idle_threshold = self.config.get(
                "idle_threshold_sec", DEFAULT_IDLE_THRESHOLD_SEC
            )

            if self.idle_detector.is_user_active(idle_threshold):
                self._active_seconds_look_away += CHECK_INTERVAL_SEC
                self._active_seconds_posture += CHECK_INTERVAL_SEC

                # Check look-away timer
                look_away_limit = (
                    self.config.get(
                        "look_away_interval_min", DEFAULT_LOOK_AWAY_INTERVAL_MIN
                    )
                    * 60
                )
                if self._active_seconds_look_away >= look_away_limit:
                    self._trigger_look_away_alert()

                # Check posture timer
                posture_limit = (
                    self.config.get(
                        "posture_interval_min", DEFAULT_POSTURE_INTERVAL_MIN
                    )
                    * 60
                )
                if self._active_seconds_posture >= posture_limit:
                    self._trigger_posture_alert()
            else:
                # User is idle — reset counters
                self._active_seconds_look_away = 0.0
                self._active_seconds_posture = 0.0

        self._schedule_check()

    # ── Alerts ──────────────────────────────────────────────────────

    def _trigger_look_away_alert(self) -> None:
        """Show the eye rest overlay and reset the look-away counter."""
        if self._current_alert is not None and self._current_alert.is_active:
            return  # Don't stack alerts

        auto_dismiss = self.config.get(
            "alert_auto_dismiss_sec", DEFAULT_ALERT_AUTO_DISMISS_SEC
        )
        sound = self.config.get("sound_enabled", DEFAULT_SOUND_ENABLED)

        self._current_alert = show_look_away_alert(
            root=self.root,
            auto_dismiss_sec=auto_dismiss,
            sound_enabled=sound,
            on_dismiss=self._on_look_away_dismissed,
        )
        logger.info("Look-away alert triggered after %.0f seconds of active use.",
                     self._active_seconds_look_away)

    def _trigger_posture_alert(self) -> None:
        """Show the posture overlay and reset the posture counter."""
        if self._current_alert is not None and self._current_alert.is_active:
            # Queue posture alert — will show after current alert is dismissed
            self._pending_posture = True
            self._active_seconds_posture = 0.0
            return

        auto_dismiss = self.config.get(
            "alert_auto_dismiss_sec", DEFAULT_ALERT_AUTO_DISMISS_SEC
        )
        sound = self.config.get("sound_enabled", DEFAULT_SOUND_ENABLED)

        self._current_alert = show_posture_alert(
            root=self.root,
            auto_dismiss_sec=auto_dismiss,
            sound_enabled=sound,
            on_dismiss=self._on_posture_dismissed,
        )
        logger.info("Posture alert triggered after %.0f seconds of active use.",
                     self._active_seconds_posture)
        self._active_seconds_posture = 0.0

    def _on_look_away_dismissed(self) -> None:
        """Callback when the look-away alert is dismissed."""
        self._active_seconds_look_away = 0.0
        self._current_alert = None

        # Show queued posture alert if pending
        if getattr(self, '_pending_posture', False):
            self._pending_posture = False
            self._trigger_posture_alert()

    def _on_posture_dismissed(self) -> None:
        """Callback when the posture alert is dismissed."""
        self._current_alert = None

    # ── Tray Callbacks ──────────────────────────────────────────────

    def _toggle_pause(self) -> None:
        """Toggle the paused state of all timers."""
        self._is_paused = not self._is_paused
        if self._is_paused:
            self._active_seconds_look_away = 0.0
            self._active_seconds_posture = 0.0
            logger.info("Timers paused.")
        else:
            logger.info("Timers resumed.")

    def _get_status_text(self) -> str:
        """Generate a status string for the tray menu."""
        if self._is_paused:
            return "⏸  Paused"

        look_remaining = (
            self.config.get(
                "look_away_interval_min", DEFAULT_LOOK_AWAY_INTERVAL_MIN
            )
            * 60
            - self._active_seconds_look_away
        )
        posture_remaining = (
            self.config.get(
                "posture_interval_min", DEFAULT_POSTURE_INTERVAL_MIN
            )
            * 60
            - self._active_seconds_posture
        )

        look_min = max(0, int(look_remaining // 60))
        posture_min = max(0, int(posture_remaining // 60))

        return f"👀 {look_min}min  |  🧘 {posture_min}min"

    def _show_settings(self) -> None:
        """Open the settings window."""
        self._ui_queue.put(self._create_settings_window)

    def _create_settings_window(self) -> None:
        """Build and display the settings window using Tkinter."""
        settings_win = tk.Toplevel(self.root)
        settings_win.title(f"{APP_NAME} — Settings")
        settings_win.configure(bg=COLOR_BG_DARK)
        settings_win.resizable(False, False)
        settings_win.wm_attributes("-topmost", True)
        # NOTE: no transient(self.root) — the root window is withdrawn, and a
        # transient of a hidden master stays withdrawn (invisible) on Windows.

        # Center on screen
        win_w, win_h = 420, 480
        screen_w = settings_win.winfo_screenwidth()
        screen_h = settings_win.winfo_screenheight()
        x = (screen_w - win_w) // 2
        y = (screen_h - win_h) // 2
        settings_win.geometry(f"{win_w}x{win_h}+{x}+{y}")

        # Title
        tk.Label(
            settings_win,
            text="⚙  Settings",
            font=(FONT_FAMILY, 16, "bold"),
            bg=COLOR_BG_DARK,
            fg=COLOR_TEXT_PRIMARY,
        ).pack(pady=(20, 15))

        # Settings frame
        frame = tk.Frame(settings_win, bg=COLOR_BG_DARK, padx=30)
        frame.pack(fill="x")

        # Helper to create labeled spinbox entries
        entries = {}

        def add_setting(parent, label_text: str, key: str, default, from_: int, to: int, row: int):
            tk.Label(
                parent,
                text=label_text,
                font=(FONT_FAMILY, 11),
                bg=COLOR_BG_DARK,
                fg=COLOR_TEXT_SECONDARY,
                anchor="w",
            ).grid(row=row, column=0, sticky="w", pady=(10, 2))

            var = tk.IntVar(value=self.config.get(key, default))
            spinbox = tk.Spinbox(
                parent,
                from_=from_,
                to=to,
                textvariable=var,
                width=8,
                font=(FONT_FAMILY, 11),
                bg=COLOR_BG_CARD,
                fg=COLOR_TEXT_PRIMARY,
                buttonbackground=COLOR_BG_CARD,
                relief="flat",
                highlightthickness=1,
                highlightcolor=COLOR_ACCENT_BLUE,
                insertbackground=COLOR_TEXT_PRIMARY,
            )
            spinbox.grid(row=row, column=1, sticky="e", pady=(10, 2), padx=(10, 0))
            entries[key] = var

        frame.columnconfigure(0, weight=1)
        frame.columnconfigure(1, weight=0)

        add_setting(frame, "👀  Eye rest interval (min)", "look_away_interval_min",
                    DEFAULT_LOOK_AWAY_INTERVAL_MIN, 1, 120, 0)
        add_setting(frame, "🧘  Posture check interval (min)", "posture_interval_min",
                    DEFAULT_POSTURE_INTERVAL_MIN, 1, 240, 1)
        add_setting(frame, "⏱  Idle threshold (sec)", "idle_threshold_sec",
                    DEFAULT_IDLE_THRESHOLD_SEC, 30, 600, 2)
        add_setting(frame, "⏳  Alert auto-dismiss (sec)", "alert_auto_dismiss_sec",
                    DEFAULT_ALERT_AUTO_DISMISS_SEC, 5, 120, 3)

        # Checkboxes frame
        checks_frame = tk.Frame(settings_win, bg=COLOR_BG_DARK, padx=30)
        checks_frame.pack(fill="x", pady=(15, 0))

        sound_var = tk.BooleanVar(
            value=self.config.get("sound_enabled", DEFAULT_SOUND_ENABLED)
        )
        tk.Checkbutton(
            checks_frame,
            text="🔔  Enable notification sound",
            variable=sound_var,
            font=(FONT_FAMILY, 11),
            bg=COLOR_BG_DARK,
            fg=COLOR_TEXT_SECONDARY,
            selectcolor=COLOR_BG_CARD,
            activebackground=COLOR_BG_DARK,
            activeforeground=COLOR_TEXT_PRIMARY,
            anchor="w",
        ).pack(fill="x", pady=(5, 0))

        auto_start_var = tk.BooleanVar(
            value=self.config.get("auto_start", DEFAULT_AUTO_START)
        )
        tk.Checkbutton(
            checks_frame,
            text="🚀  Start with Windows",
            variable=auto_start_var,
            font=(FONT_FAMILY, 11),
            bg=COLOR_BG_DARK,
            fg=COLOR_TEXT_SECONDARY,
            selectcolor=COLOR_BG_CARD,
            activebackground=COLOR_BG_DARK,
            activeforeground=COLOR_TEXT_PRIMARY,
            anchor="w",
        ).pack(fill="x", pady=(5, 0))

        # Save button
        def save_settings():
            for key, var in entries.items():
                self.config.set(key, var.get())
            self.config.set("sound_enabled", sound_var.get())
            self.config.set("auto_start", auto_start_var.get())
            self.config.save()

            # Reset counters so new intervals take effect cleanly
            self._active_seconds_look_away = 0.0
            self._active_seconds_posture = 0.0

            # Apply auto-start
            self._sync_auto_start()

            logger.info("Settings saved.")
            settings_win.destroy()

        save_btn = tk.Button(
            settings_win,
            text="💾  Save Settings",
            font=(FONT_FAMILY, 12, "bold"),
            bg=COLOR_ACCENT_BLUE,
            fg="#000000",
            activebackground=COLOR_ACCENT_PURPLE,
            activeforeground="#ffffff",
            relief="flat",
            cursor="hand2",
            padx=30,
            pady=10,
            command=save_settings,
        )
        save_btn.pack(pady=(25, 10))
        save_btn.bind("<Enter>", lambda e: save_btn.configure(bg=COLOR_ACCENT_PURPLE, fg="#ffffff"))
        save_btn.bind("<Leave>", lambda e: save_btn.configure(bg=COLOR_ACCENT_BLUE, fg="#000000"))

        # Version info
        tk.Label(
            settings_win,
            text=f"{APP_NAME} v{APP_VERSION}",
            font=(FONT_FAMILY, 9),
            bg=COLOR_BG_DARK,
            fg="#555577",
        ).pack(side="bottom", pady=(0, 10))

    # ── Auto-Start ──────────────────────────────────────────────────

    def _sync_auto_start(self) -> None:
        """Sync the Windows auto-start registry entry with the config."""
        auto_start = self.config.get("auto_start", DEFAULT_AUTO_START)
        try:
            key = winreg.OpenKey(
                winreg.HKEY_CURRENT_USER, REGISTRY_KEY, 0, winreg.KEY_SET_VALUE
            )
            if auto_start:
                if getattr(sys, "frozen", False):
                    # Bundled exe launches itself directly.
                    command = f'"{sys.executable}"'
                else:
                    # Dev mode: run the script with pythonw.exe (no console).
                    exe = sys.executable.replace("python.exe", "pythonw.exe")
                    command = f'"{exe}" "{sys.argv[0]}"'
                winreg.SetValueEx(
                    key, REGISTRY_VALUE_NAME, 0, winreg.REG_SZ, command
                )
                logger.info("Auto-start enabled in Windows registry.")
            else:
                try:
                    winreg.DeleteValue(key, REGISTRY_VALUE_NAME)
                    logger.info("Auto-start disabled in Windows registry.")
                except FileNotFoundError:
                    pass  # Key doesn't exist, nothing to delete
            winreg.CloseKey(key)
        except OSError as e:
            logger.warning("Failed to update auto-start registry: %s", e)

    # ── Quit ────────────────────────────────────────────────────────

    def _quit(self) -> None:
        """Gracefully shut down the application."""
        logger.info("Shutting down %s...", APP_NAME)
        self.tray.stop()

        # Dismiss any active alert
        if self._current_alert is not None and self._current_alert.is_active:
            self._current_alert.dismiss()

        self.root.quit()
        self.root.destroy()
