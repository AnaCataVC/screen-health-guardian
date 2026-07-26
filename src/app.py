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
    REGISTRY_KEY,
    REGISTRY_VALUE_NAME,
    COLOR_BG_DARK,
    COLOR_BG_CARD,
    COLOR_CARD_BG,
    COLOR_BORDER,
    COLOR_INPUT_BG,
    COLOR_ACCENT_BLUE,
    COLOR_ACCENT_HOVER,
    COLOR_ACCENT_PURPLE,
    COLOR_TEXT_PRIMARY,
    COLOR_TEXT_SECONDARY,
    COLOR_TEXT_MUTED,
    COLOR_BUTTON_DISMISS,
    COLOR_BUTTON_SECONDARY,
    COLOR_BUTTON_SECONDARY_HOVER,
    FONT_FAMILY,
)
from config_manager import ConfigManager
from idle_detector import IdleDetector
from alert_overlay import show_look_away_alert, show_posture_alert
from tray_icon import TrayIcon
import i18n

logger = logging.getLogger(__name__)

# Windows Registry key for auto-start
# ── Custom Modern UI Widgets for Settings Panel ─────────────────────

from ui_utils import create_rounded_polygon


class FlatButton(tk.Button):
    """A clean flat-style button using native tk.Button — no canvas, no clipping."""

    def __init__(
        self,
        parent,
        text: str,
        command,
        bg_color: str,
        fg_color: str,
        hover_bg: str,
        hover_fg: str | None = None,
        font=None,
        padx: int = 18,
        pady: int = 6,
        **kwargs,
    ):
        self.bg_color = bg_color
        self.fg_color = fg_color
        self.hover_bg = hover_bg
        self.hover_fg = hover_fg or fg_color
        super().__init__(
            parent,
            text=text,
            command=command,
            bg=bg_color,
            fg=fg_color,
            font=font or (FONT_FAMILY, 10, "bold"),
            relief="flat",
            bd=0,
            padx=padx,
            pady=pady,
            cursor="hand2",
            activebackground=hover_bg,
            activeforeground=hover_fg or fg_color,
            highlightthickness=0,
            **kwargs,
        )
        self.bind("<Enter>", self._on_enter)
        self.bind("<Leave>", self._on_leave)

    def _on_enter(self, event=None):
        self.config(bg=self.hover_bg, fg=self.hover_fg)

    def _on_leave(self, event=None):
        self.config(bg=self.bg_color, fg=self.fg_color)


# Keep alias so any remaining references don't break
RoundedCanvasButton = FlatButton


class ModernStepper(tk.Frame):
    """Modern numeric stepper widget: [-]  [ VALUE ]  [+]"""

    def __init__(
        self,
        parent,
        var: tk.IntVar,
        min_val: int,
        max_val: int,
        step: int = 1,
        bg: str = COLOR_CARD_BG,
    ):
        super().__init__(parent, bg=bg)
        self.var = var
        self.min_val = min_val
        self.max_val = max_val
        self.step = step

        # Decrement button
        self.btn_minus = tk.Button(
            self,
            text="<",
            command=self._decrement,
            bg=COLOR_INPUT_BG,
            fg=COLOR_TEXT_PRIMARY,
            font=(FONT_FAMILY, 11, "bold"),
            relief="flat",
            bd=0,
            padx=8,
            pady=2,
            cursor="hand2",
            activebackground=COLOR_BORDER,
            activeforeground=COLOR_ACCENT_BLUE,
            highlightthickness=0,
        )
        self.btn_minus.pack(side="left", padx=2)

        # Value display entry box
        vcmd = (self.register(self._validate_input), "%P")
        self.val_entry = tk.Entry(
            self,
            textvariable=self.var,
            font=(FONT_FAMILY, 10, "bold"),
            bg=COLOR_INPUT_BG,
            fg=COLOR_TEXT_PRIMARY,
            insertbackground=COLOR_TEXT_PRIMARY,
            width=4,
            relief="flat",
            justify="center",
            validate="key",
            validatecommand=vcmd,
            highlightthickness=0,
        )
        self.val_entry.pack(side="left", padx=4, ipady=3)
        self.val_entry.bind("<FocusOut>", self._on_focus_out)
        self.val_entry.bind("<Return>", lambda e: self.focus())  # unfocus on enter

        # Increment button
        self.btn_plus = tk.Button(
            self,
            text=">",
            command=self._increment,
            bg=COLOR_INPUT_BG,
            fg=COLOR_TEXT_PRIMARY,
            font=(FONT_FAMILY, 11, "bold"),
            relief="flat",
            bd=0,
            padx=8,
            pady=2,
            cursor="hand2",
            activebackground=COLOR_BORDER,
            activeforeground=COLOR_ACCENT_BLUE,
            highlightthickness=0,
        )
        self.btn_plus.pack(side="left", padx=2)

    def _validate_input(self, P):
        """Only allow digits or empty string while typing."""
        if P == "" or P.isdigit():
            return True
        return False

    def _on_focus_out(self, event=None):
        """Clamp the value to min/max when the user finishes editing."""
        try:
            val = self.var.get()
            if val < self.min_val:
                self.var.set(self.min_val)
            elif val > self.max_val:
                self.var.set(self.max_val)
        except tk.TclError:
            # If the field is empty or invalid (e.g. '-')
            self.var.set(self.min_val)

    def _decrement(self):
        try:
            val = self.var.get() - self.step
        except tk.TclError:
            val = self.min_val
            
        if val < self.min_val:
            val = self.min_val
        self.var.set(val)

    def _increment(self):
        try:
            val = self.var.get() + self.step
        except tk.TclError:
            val = self.min_val
            
        if val > self.max_val:
            val = self.max_val
        self.var.set(val)


class ModernToggle(tk.Canvas):
    """Pill-shaped modern toggle switch control."""

    def __init__(self, parent, var: tk.BooleanVar, bg=COLOR_CARD_BG, width=44, height=22):
        super().__init__(
            parent,
            width=width,
            height=height,
            bg=bg,
            highlightthickness=0,
            bd=0,
            cursor="hand2",
        )
        self.var = var
        self.w = width
        self.h = height

        self.bind("<Button-1>", self._toggle)
        self._draw()

    def _draw(self):
        self.delete("all")
        is_on = self.var.get()
        track_color = COLOR_ACCENT_BLUE if is_on else COLOR_INPUT_BG
        knob_color = "#0b0e14" if is_on else COLOR_TEXT_MUTED

        # Track
        create_rounded_polygon(
            self, 1, 1, self.w - 1, self.h - 1, radius=11, fill=track_color, outline=COLOR_BORDER if not is_on else ""
        )

        # Knob circle
        knob_x = self.w - 12 if is_on else 12
        self.create_oval(knob_x - 7, self.h / 2 - 7, knob_x + 7, self.h / 2 + 7, fill=knob_color, outline="")

    def _toggle(self, event=None):
        self.var.set(not self.var.get())
        self._draw()


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
        
        # Set initial language
        i18n.set_language(self.config.get("language", "en"))

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
            title=i18n.t("alert_look_away_title"),
            message=i18n.t("alert_look_away_msg"),
            button_text=i18n.t("alert_look_away_btn"),
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
            title=i18n.t("alert_posture_title"),
            message=i18n.t("alert_posture_msg"),
            button_text=i18n.t("alert_posture_btn"),
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
            return i18n.t("status_paused_menu")

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

        return f"👀 {look_min}min  |  ✨ {posture_min}min"

    def _show_settings(self) -> None:
        """Open the settings window."""
        self._ui_queue.put(self._create_settings_window)

    def _create_settings_window(self) -> None:
        """Build and display the modernized settings window using Tkinter."""
        settings_win = tk.Toplevel(self.root)
        settings_win.withdraw()  # Hide window while building to prevent flashing
        settings_win.title(f"{APP_NAME} — {i18n.t('settings_title')}")
        settings_win.configure(bg=COLOR_BG_DARK)
        settings_win.resizable(False, False)
        settings_win.wm_attributes("-topmost", True)
        
        import os
        import sys
        try:
            if getattr(sys, 'frozen', False):
                base_path = sys._MEIPASS
            else:
                base_path = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
            icon_path = os.path.join(base_path, "icon.ico")
            if os.path.exists(icon_path):
                settings_win.iconbitmap(icon_path)
        except Exception as e:
            logger.error("Failed to load icon for settings window: %s", e)

        # Center window on screen with spacious modern dimensions
        win_w, win_h = 480, 620
        screen_w = settings_win.winfo_screenwidth()
        screen_h = settings_win.winfo_screenheight()
        x = (screen_w - win_w) // 2
        y = (screen_h - win_h) // 2
        settings_win.geometry(f"{win_w}x{win_h}+{x}+{y}")

        # ── Header Banner ─────────────────────────────────────────────
        header_frame = tk.Frame(settings_win, bg=COLOR_BG_DARK)
        header_frame.pack(fill="x", padx=25, pady=15)

        tk.Label(
            header_frame,
            text=i18n.t("settings_title"),
            font=(FONT_FAMILY, 16, "bold"),
            bg=COLOR_BG_DARK,
            fg=COLOR_TEXT_PRIMARY,
            anchor="w",
        ).pack(fill="x")

        tk.Label(
            header_frame,
            text=i18n.t("settings_subtitle"),
            font=(FONT_FAMILY, 9),
            bg=COLOR_BG_DARK,
            fg=COLOR_TEXT_MUTED,
            anchor="w",
        ).pack(fill="x", pady=(2, 0))

        # Main scrollable/container area
        content_frame = tk.Frame(settings_win, bg=COLOR_BG_DARK)
        content_frame.pack(fill="both", expand=True, padx=25)

        # ── Card 1: Timer Intervals ──────────────────────────────────
        card_intervals = tk.Frame(
            content_frame,
            bg=COLOR_CARD_BG,
            highlightbackground=COLOR_BORDER,
            highlightthickness=1,
            padx=18,
            pady=14,
        )
        card_intervals.pack(fill="x", pady=10)

        tk.Label(
            card_intervals,
            text=i18n.t("settings_sec_intervals"),
            font=(FONT_FAMILY, 9, "bold"),
            bg=COLOR_CARD_BG,
            fg=COLOR_ACCENT_BLUE,
            anchor="w",
        ).pack(fill="x", pady=(0, 10))

        grid_intervals = tk.Frame(card_intervals, bg=COLOR_CARD_BG)
        grid_intervals.pack(fill="x")
        grid_intervals.columnconfigure(0, weight=1)
        grid_intervals.columnconfigure(1, weight=0)
        grid_intervals.columnconfigure(2, weight=0)

        # Variables for settings
        look_away_var = tk.IntVar(value=self.config.get("look_away_interval_min", DEFAULT_LOOK_AWAY_INTERVAL_MIN))
        posture_var = tk.IntVar(value=self.config.get("posture_interval_min", DEFAULT_POSTURE_INTERVAL_MIN))
        # Idle threshold converted to MINUTES for UI
        idle_sec_val = self.config.get("idle_threshold_sec", DEFAULT_IDLE_THRESHOLD_SEC)
        idle_min_var = tk.IntVar(value=max(1, idle_sec_val // 60))
        dismiss_var = tk.IntVar(value=self.config.get("alert_auto_dismiss_sec", DEFAULT_ALERT_AUTO_DISMISS_SEC))

        def add_stepper_row(parent, label_text: str, var: tk.IntVar, min_val: int, max_val: int, unit_key: str, row: int, step: int = 1):
            tk.Label(
                parent,
                text=label_text,
                font=(FONT_FAMILY, 10),
                bg=COLOR_CARD_BG,
                fg=COLOR_TEXT_PRIMARY,
                anchor="w",
            ).grid(row=row, column=0, sticky="w", pady=6)

            stepper = ModernStepper(parent, var=var, min_val=min_val, max_val=max_val, step=step, bg=COLOR_CARD_BG)
            stepper.grid(row=row, column=1, sticky="e", pady=6, padx=(10, 6))

            tk.Label(
                parent,
                text=i18n.t(unit_key),
                font=(FONT_FAMILY, 9),
                bg=COLOR_CARD_BG,
                fg=COLOR_TEXT_MUTED,
                anchor="w",
            ).grid(row=row, column=2, sticky="w", pady=6)

        add_stepper_row(grid_intervals, i18n.t("settings_eye_rest"), look_away_var, 1, 120, "unit_min", 0)
        add_stepper_row(grid_intervals, i18n.t("settings_posture"), posture_var, 1, 240, "unit_min", 1)
        add_stepper_row(grid_intervals, i18n.t("settings_idle"), idle_min_var, 1, 30, "unit_min", 2)
        add_stepper_row(grid_intervals, i18n.t("settings_dismiss"), dismiss_var, 5, 120, "unit_sec", 3, step=5)

        # ── Card 2: Preferences & System ─────────────────────────────
        card_prefs = tk.Frame(
            content_frame,
            bg=COLOR_CARD_BG,
            highlightbackground=COLOR_BORDER,
            highlightthickness=1,
            padx=18,
            pady=14,
        )
        card_prefs.pack(fill="x", pady=10)

        tk.Label(
            card_prefs,
            text=i18n.t("settings_sec_preferences"),
            font=(FONT_FAMILY, 9, "bold"),
            bg=COLOR_CARD_BG,
            fg=COLOR_ACCENT_PURPLE,
            anchor="w",
        ).pack(fill="x", pady=5)

        # Sound toggle row
        sound_var = tk.BooleanVar(value=self.config.get("sound_enabled", DEFAULT_SOUND_ENABLED))
        sound_row = tk.Frame(card_prefs, bg=COLOR_CARD_BG)
        sound_row.pack(fill="x", pady=4)

        tk.Label(
            sound_row,
            text=i18n.t("settings_sound"),
            font=(FONT_FAMILY, 10),
            bg=COLOR_CARD_BG,
            fg=COLOR_TEXT_PRIMARY,
            anchor="w",
        ).pack(side="left")

        ModernToggle(sound_row, var=sound_var, bg=COLOR_CARD_BG).pack(side="right")

        # Auto-start toggle row
        auto_start_var = tk.BooleanVar(value=self.config.get("auto_start", DEFAULT_AUTO_START))
        auto_row = tk.Frame(card_prefs, bg=COLOR_CARD_BG)
        auto_row.pack(fill="x", pady=4)

        tk.Label(
            auto_row,
            text=i18n.t("settings_autostart"),
            font=(FONT_FAMILY, 10),
            bg=COLOR_CARD_BG,
            fg=COLOR_TEXT_PRIMARY,
            anchor="w",
        ).pack(side="left")

        ModernToggle(auto_row, var=auto_start_var, bg=COLOR_CARD_BG).pack(side="right")

        # Language dropdown row inside Card 2
        lang_row = tk.Frame(card_prefs, bg=COLOR_CARD_BG)
        lang_row.pack(fill="x", pady=(6, 2))

        tk.Label(
            lang_row,
            text=i18n.t("settings_language"),
            font=(FONT_FAMILY, 10),
            bg=COLOR_CARD_BG,
            fg=COLOR_TEXT_PRIMARY,
            anchor="w",
        ).pack(side="left")

        lang_var = tk.StringVar(value=self.config.get("language", "en"))
        lang_options = {"English": "en", "Español": "es"}
        inv_lang_options = {v: k for k, v in lang_options.items()}
        display_lang_var = tk.StringVar(value=inv_lang_options.get(lang_var.get(), "English"))

        def on_lang_change(val):
            lang_var.set(lang_options[val])

        lang_menu = tk.OptionMenu(
            lang_row,
            display_lang_var,
            *lang_options.keys(),
            command=on_lang_change
        )
        lang_menu.config(
            bg=COLOR_INPUT_BG,
            fg=COLOR_TEXT_PRIMARY,
            font=(FONT_FAMILY, 9, "bold"),
            activebackground=COLOR_ACCENT_BLUE,
            activeforeground="#000000",
            highlightthickness=1,
            highlightbackground=COLOR_BORDER,
            relief="flat",
            indicatoron=0,
            padx=12,
            pady=3,
            cursor="hand2"
        )
        lang_menu["menu"].config(
            bg=COLOR_CARD_BG,
            fg=COLOR_TEXT_PRIMARY,
            activebackground=COLOR_ACCENT_BLUE,
            activeforeground="#000000",
            font=(FONT_FAMILY, 9)
        )
        lang_menu.pack(side="right")

        # ── Action Buttons Footer ─────────────────────────────────────
        footer_frame = tk.Frame(settings_win, bg=COLOR_BG_DARK)
        footer_frame.pack(fill="x", side="bottom", padx=25, pady=10)

        def save_settings():
            self.config.set("look_away_interval_min", look_away_var.get())
            self.config.set("posture_interval_min", posture_var.get())
            # Convert idle threshold from minutes back to seconds for config
            self.config.set("idle_threshold_sec", idle_min_var.get() * 60)
            self.config.set("alert_auto_dismiss_sec", dismiss_var.get())
            self.config.set("sound_enabled", sound_var.get())
            self.config.set("auto_start", auto_start_var.get())
            self.config.set("language", lang_var.get())
            self.config.save()
            
            i18n.set_language(lang_var.get())
            self._active_seconds_look_away = 0.0
            self._active_seconds_posture = 0.0
            self._sync_auto_start()
            self.tray._refresh_icon()

            logger.info("Settings saved.")
            settings_win.destroy()

        btn_box = tk.Frame(footer_frame, bg=COLOR_BG_DARK)
        btn_box.pack(fill="x")

        # Cancel button
        cancel_btn = tk.Button(
            btn_box,
            text=i18n.t("settings_cancel"),
            command=settings_win.destroy,
            bg=COLOR_BUTTON_SECONDARY,
            fg=COLOR_TEXT_SECONDARY,
            font=(FONT_FAMILY, 10, "bold"),
            relief="flat",
            bd=0,
            padx=20,
            pady=8,
            cursor="hand2",
            activebackground=COLOR_BUTTON_SECONDARY_HOVER,
            activeforeground=COLOR_TEXT_PRIMARY,
            highlightthickness=0,
        )
        cancel_btn.pack(side="left", ipadx=10, ipady=4)

        # Save button
        save_btn = tk.Button(
            btn_box,
            text=i18n.t("settings_save"),
            command=save_settings,
            bg=COLOR_ACCENT_BLUE,
            fg="#0b0e14",
            font=(FONT_FAMILY, 10, "bold"),
            relief="flat",
            bd=0,
            padx=20,
            pady=8,
            cursor="hand2",
            activebackground=COLOR_ACCENT_HOVER,
            activeforeground="#000000",
            highlightthickness=0,
        )
        save_btn.pack(side="right", ipadx=10, ipady=4)

        # Version tag
        tk.Label(
            footer_frame,
            text=f"{APP_NAME} v{APP_VERSION}",
            font=(FONT_FAMILY, 8),
            bg=COLOR_BG_DARK,
            fg=COLOR_TEXT_MUTED,
        ).pack(pady=(12, 0))

        # Show the window once everything is built and positioned
        settings_win.deiconify()

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
