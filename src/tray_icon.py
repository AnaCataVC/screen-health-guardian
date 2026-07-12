import threading
import logging
from typing import Optional, Callable

import pystray
from PIL import Image, ImageDraw

from i18n import t

logger = logging.getLogger(__name__)


class TrayIcon:
    """System tray icon for Work Health Timer.
    
    Provides a tray icon with a context menu for controlling the timer.
    Runs pystray in a daemon thread to avoid blocking the main Tkinter loop.
    """

    ICON_SIZE = 64
    COLOR_ACTIVE = '#4fc3f7'
    COLOR_PAUSED = '#ffd54f'
    COLOR_BG = '#1a1a3e'

    def __init__(
        self,
        on_pause_resume: Callable[[], None],
        on_settings: Callable[[], None],
        on_quit: Callable[[], None],
        get_status: Callable[[], str],
    ):
        """Initialize the tray icon.
        
        Args:
            on_pause_resume: Callback when user clicks Pause/Resume.
            on_settings: Callback when user clicks Settings.
            on_quit: Callback when user clicks Quit.
            get_status: Callable that returns a status string for the menu.
        """
        self._on_pause_resume = on_pause_resume
        self._on_settings = on_settings
        self._on_quit = on_quit
        self._get_status = get_status
        self._is_paused = False
        self._icon: Optional[pystray.Icon] = None
        self._thread: Optional[threading.Thread] = None

    def _create_icon_image(self, color: str = None) -> Image.Image:
        """Generate a heart-shaped tray icon programmatically.
        
        Args:
            color: The fill color for the heart. Defaults to active color.
        
        Returns:
            A PIL Image suitable for use as a tray icon.
        """
        if color is None:
            color = self.COLOR_ACTIVE if not self._is_paused else self.COLOR_PAUSED

        size = self.ICON_SIZE
        img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)

        # Draw a simple heart shape using circles and a polygon
        quarter = size // 4
        half = size // 2

        # Two circles for the top of the heart
        r = quarter - 2
        cx1 = quarter + 2
        cx2 = 3 * quarter - 2
        cy = quarter + 4

        draw.ellipse(
            [cx1 - r, cy - r, cx1 + r, cy + r],
            fill=color,
        )
        draw.ellipse(
            [cx2 - r, cy - r, cx2 + r, cy + r],
            fill=color,
        )

        # Triangle for the bottom of the heart
        draw.polygon(
            [
                (cx1 - r, cy + 2),
                (cx2 + r, cy + 2),
                (half, size - 6),
            ],
            fill=color,
        )

        return img

    def _build_menu(self) -> pystray.Menu:
        """Build the context menu for the tray icon."""
        return pystray.Menu(
            pystray.MenuItem(
                lambda _: self._get_status(),
                None,
                enabled=False,
            ),
            pystray.Menu.SEPARATOR,
            pystray.MenuItem(
                lambda _: t('tray_resume') if self._is_paused else t('tray_pause'),
                self._handle_pause_resume,
            ),
            pystray.MenuItem(
                lambda _: t('tray_settings'),
                self._handle_settings,
                default=True,
            ),
            pystray.Menu.SEPARATOR,
            pystray.MenuItem(
                lambda _: t('tray_quit'),
                self._handle_quit,
            ),
        )

    def _handle_pause_resume(self, icon, item) -> None:
        """Handle pause/resume menu click."""
        self._is_paused = not self._is_paused
        self._on_pause_resume()
        self._refresh_icon()

    def _handle_settings(self, icon, item) -> None:
        """Handle settings menu click."""
        self._on_settings()

    def _handle_quit(self, icon, item) -> None:
        """Handle quit menu click."""
        self._on_quit()

    def _refresh_icon(self) -> None:
        """Update the icon image and tooltip."""
        if self._icon is not None:
            self._icon.icon = self._create_icon_image()
            status = t('tray_status_paused') if self._is_paused else t('tray_status_active')
            self._icon.title = f'Work Health Timer — {status}'
            self._icon.menu = self._build_menu()

    def start(self) -> None:
        """Start the tray icon in a daemon thread."""
        self._icon = pystray.Icon(
            name='work_health_timer',
            icon=self._create_icon_image(),
            title=f'Work Health Timer — {t("tray_status_active")}',
            menu=self._build_menu(),
        )

        self._thread = threading.Thread(
            target=self._icon.run,
            daemon=True,
            name='TrayIconThread',
        )
        self._thread.start()
        logger.info('Tray icon started.')

    def stop(self) -> None:
        """Stop the tray icon gracefully."""
        if self._icon is not None:
            try:
                self._icon.stop()
                logger.info('Tray icon stopped.')
            except Exception as e:
                logger.error('Error stopping tray icon: %s', e)

    def set_paused(self, paused: bool) -> None:
        """Update the paused state and refresh the icon."""
        self._is_paused = paused
        self._refresh_icon()

    @property
    def is_paused(self) -> bool:
        """Return the current paused state."""
        return self._is_paused
