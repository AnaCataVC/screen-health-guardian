import tkinter as tk
from tkinter import font as tkfont
import winsound
import logging

logger = logging.getLogger(__name__)





class AlertOverlay:
    """A modern, dark-themed overlay alert window using tkinter.
    
    Creates a borderless, always-on-top popup with fade-in animation,
    auto-dismiss timer, and optional sound notification.
    """

    def __init__(
        self,
        root: tk.Tk,
        emoji: str,
        title: str,
        message: str,
        button_text: str,
        accent_color: str,
        auto_dismiss_sec: int = 30,
        sound_enabled: bool = False,
        on_dismiss: callable = None,
    ):
        self.root = root
        self.on_dismiss = on_dismiss
        self.auto_dismiss_sec = auto_dismiss_sec
        self.sound_enabled = sound_enabled
        self.overlay = None
        self.dismiss_after_id = None
        self.fade_after_id = None
        self._dismissed = False

        self._create_overlay(
            emoji, title, message, button_text, accent_color
        )

    def _create_overlay(
        self,
        emoji: str,
        title: str,
        message: str,
        button_text: str,
        accent_color: str,
    ) -> None:
        """Build and display the overlay window."""
        from i18n import t
        self.overlay = tk.Toplevel(self.root)
        self.overlay.title(t("alert_window_title"))
        self.overlay.overrideredirect(True)
        self.overlay.wm_attributes("-topmost", True)
        self.overlay.wm_attributes("-alpha", 0.0)  # Start invisible for fade-in
        self.overlay.configure(bg='#0f0f23')

        # NOTE: no transient() here. The main root window is withdrawn, and a
        # transient whose master is hidden inherits the withdrawn state and
        # never becomes visible. overrideredirect already keeps it off the taskbar.

        # Window dimensions and centering (larger & spacious card)
        win_w, win_h = 600, 380
        screen_w = self.overlay.winfo_screenwidth()
        screen_h = self.overlay.winfo_screenheight()
        x = (screen_w - win_w) // 2
        y = (screen_h - win_h) // 2
        self.overlay.geometry(f"{win_w}x{win_h}+{x}+{y}")

        # Main container frame with subtle border
        container = tk.Frame(
            self.overlay,
            bg='#0f0f23',
            highlightbackground='#292b4a',
            highlightthickness=1,
        )
        container.pack(fill='both', expand=True, padx=0, pady=0)

        inner_frame = tk.Frame(container, bg='#0f0f23', padx=35, pady=25)
        inner_frame.pack(fill='both', expand=True)
        
        # Center alignment frame
        content_frame = tk.Frame(inner_frame, bg='#0f0f23')
        content_frame.pack(expand=True)

        # Emoji label
        emoji_label = tk.Label(
            content_frame,
            text=emoji,
            font=("Segoe UI Emoji", 48),
            bg='#0f0f23',
            fg='#ffffff',
        )
        emoji_label.pack(pady=(0, 6))

        # Title label
        title_label = tk.Label(
            content_frame,
            text=title,
            font=("Segoe UI", 18, "bold"),
            bg='#0f0f23',
            fg=accent_color,
        )
        title_label.pack(pady=(0, 6))

        # Message label
        msg_label = tk.Label(
            content_frame,
            text=message,
            font=("Segoe UI", 12),
            bg='#0f0f23',
            fg='#b0bec5',
            wraplength=460,
            justify="center",
        )
        msg_label.pack(pady=(0, 20))

        # Dismiss button
        btn_frame = tk.Frame(content_frame, bg='#0f0f23')
        btn_frame.pack()

        hover_fg_color = '#0b0e14' if accent_color == '#4fc3f7' else '#ffffff'
        self.btn = tk.Button(
            btn_frame,
            text=button_text,
            command=self.dismiss,
            bg='#27294d',
            fg='#ffffff',
            font=("Segoe UI", 11, "bold"),
            relief="flat",
            bd=0,
            padx=30,
            pady=10,
            cursor="hand2",
            activebackground=accent_color,
            activeforeground=hover_fg_color,
            highlightthickness=0,
        )
        self.btn.pack(ipadx=10, ipady=6)

        # Allow closing by clicking anywhere on the overlay
        for widget in [self.overlay, container, inner_frame, content_frame, emoji_label, title_label, msg_label]:
            widget.bind('<Button-1>', lambda e: self.dismiss())

        # Start fade-in animation
        self._fade_in(0.0)

        # Schedule auto-dismiss
        if self.auto_dismiss_sec > 0:
            self.dismiss_after_id = self.overlay.after(
                self.auto_dismiss_sec * 1000, self.dismiss
            )

        # Play sound if enabled
        if self.sound_enabled:
            try:
                winsound.MessageBeep(winsound.MB_ICONASTERISK)
            except Exception:
                pass  # Silently ignore sound errors

    def _fade_in(self, alpha: float) -> None:
        """Animate the overlay fading in from 0 to 0.95 alpha."""
        if self._dismissed or self.overlay is None:
            return
        try:
            if alpha < 0.95:
                self.overlay.wm_attributes("-alpha", alpha)
                self.fade_after_id = self.overlay.after(
                    20, self._fade_in, alpha + 0.05
                )
            else:
                self.overlay.wm_attributes("-alpha", 0.95)
        except tk.TclError:
            pass  # Window was destroyed during fade

    def dismiss(self) -> None:
        """Close the overlay and trigger the on_dismiss callback."""
        if self._dismissed:
            return
        self._dismissed = True

        try:
            if self.dismiss_after_id is not None:
                self.overlay.after_cancel(self.dismiss_after_id)
            if self.fade_after_id is not None:
                self.overlay.after_cancel(self.fade_after_id)
            self.overlay.destroy()
        except tk.TclError:
            pass  # Window already destroyed

        if self.on_dismiss:
            try:
                self.on_dismiss()
            except Exception as e:
                logger.error("Error in on_dismiss callback: %s", e)

    @property
    def is_active(self) -> bool:
        """Check if the overlay is still visible."""
        return not self._dismissed


def show_look_away_alert(
    root: tk.Tk,
    title: str,
    message: str,
    button_text: str,
    auto_dismiss_sec: int = 30,
    sound_enabled: bool = False,
    on_dismiss: callable = None,
) -> AlertOverlay:
    """Show the eye rest alert (20-20-20 rule).
    
    Reminds the user to look at something 20 feet away for 20 seconds
    to reduce eye strain.
    """
    return AlertOverlay(
        root=root,
        emoji="👀",
        title=title,
        message=message,
        button_text=button_text,
        accent_color="#4fc3f7",
        auto_dismiss_sec=auto_dismiss_sec,
        sound_enabled=sound_enabled,
        on_dismiss=on_dismiss,
    )


def show_posture_alert(
    root: tk.Tk,
    title: str,
    message: str,
    button_text: str,
    auto_dismiss_sec: int = 30,
    sound_enabled: bool = False,
    on_dismiss: callable = None,
) -> AlertOverlay:
    """Show the posture correction alert.
    
    Reminds the user to check and correct their sitting posture.
    """
    return AlertOverlay(
        root=root,
        emoji="✨",
        title=title,
        message=message,
        button_text=button_text,
        accent_color="#b388ff",
        auto_dismiss_sec=auto_dismiss_sec,
        sound_enabled=sound_enabled,
        on_dismiss=on_dismiss,
    )
