"""Windows idle detection using the GetLastInputInfo API.

Provides a lightweight way to determine how long the user has been
inactive (no keyboard or mouse input) by querying the Win32 API
through ctypes.
"""

import ctypes
from ctypes import Structure, byref, c_uint, sizeof, windll


class LASTINPUTINFO(Structure):
    """Windows LASTINPUTINFO structure for GetLastInputInfo API."""

    _fields_ = [
        ('cbSize', c_uint),
        ('dwTime', c_uint),
    ]


class IdleDetector:
    """Detects user idle time using Windows GetLastInputInfo API.

    Tracks the last keyboard/mouse input event timestamp and compares
    it against the current system tick count to determine idle duration.
    """

    @staticmethod
    def get_idle_seconds() -> float:
        """Return the number of seconds since the last user input.

        Uses GetLastInputInfo (user32.dll) and GetTickCount (kernel32.dll).
        Handles 32-bit tick count overflow (~49.7 days) with unsigned
        arithmetic.

        Returns:
            Seconds of idle time. Returns 0.0 if the API call fails.
        """
        last_input = LASTINPUTINFO()
        last_input.cbSize = sizeof(LASTINPUTINFO)

        if not windll.user32.GetLastInputInfo(byref(last_input)):
            return 0.0

        current_tick = windll.kernel32.GetTickCount()
        idle_ms = (current_tick - last_input.dwTime) & 0xFFFFFFFF
        return idle_ms / 1000.0

    @staticmethod
    def is_user_active(threshold_sec: float = 120.0) -> bool:
        """Check if the user has been active within the given threshold.

        Args:
            threshold_sec: Maximum seconds of inactivity to still count
                           as active.

        Returns:
            True if idle time is less than threshold_sec.
        """
        return IdleDetector.get_idle_seconds() < threshold_sec
