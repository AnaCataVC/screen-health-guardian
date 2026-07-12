"""Configuration manager for the Work Health Timer application.

Handles loading, saving, and accessing user settings from a JSON file.
Thread-safe and resilient to corrupted config files.
"""

import json
import logging
import threading
from typing import Any

from constants import (
    DEFAULT_ALERT_AUTO_DISMISS_SEC,
    DEFAULT_AUTO_START,
    DEFAULT_IDLE_THRESHOLD_SEC,
    DEFAULT_LOOK_AWAY_INTERVAL_MIN,
    DEFAULT_POSTURE_INTERVAL_MIN,
    DEFAULT_SOUND_ENABLED,
    get_config_path,
)

logger = logging.getLogger(__name__)


class ConfigManager:
    """Manages application configuration with JSON file persistence.

    All public methods are thread-safe thanks to an internal lock.
    If the config file is missing or corrupted, defaults are applied
    automatically.
    """

    _DEFAULTS: dict[str, Any] = {
        "look_away_interval_min": DEFAULT_LOOK_AWAY_INTERVAL_MIN,
        "posture_interval_min": DEFAULT_POSTURE_INTERVAL_MIN,
        "idle_threshold_sec": DEFAULT_IDLE_THRESHOLD_SEC,
        "alert_auto_dismiss_sec": DEFAULT_ALERT_AUTO_DISMISS_SEC,
        "sound_enabled": DEFAULT_SOUND_ENABLED,
        "auto_start": DEFAULT_AUTO_START,
    }

    def __init__(self, config_path: str | None = None) -> None:
        """Initialize the configuration manager.

        Args:
            config_path: Optional path to the JSON config file.
                         Defaults to the standard application config path.
        """
        self._path = config_path or get_config_path()
        self._lock = threading.Lock()
        self._data: dict[str, Any] = {}
        self.load()

    # ------------------------------------------------------------------
    # Public API
    # ------------------------------------------------------------------

    def load(self) -> None:
        """Load configuration from disk.

        If the file does not exist or contains invalid JSON, the
        configuration is silently reset to defaults and persisted.
        """
        with self._lock:
            try:
                with open(self._path, "r", encoding="utf-8") as fh:
                    self._data = json.load(fh)
                    if not isinstance(self._data, dict):
                        raise ValueError("Config root must be a JSON object")
                logger.info("Configuration loaded from %s", self._path)
            except FileNotFoundError:
                logger.info(
                    "Config file not found. Creating defaults at %s",
                    self._path,
                )
                self._data = dict(self._DEFAULTS)
                self._write()
            except (json.JSONDecodeError, ValueError) as exc:
                logger.warning(
                    "Corrupted config file (%s). Resetting to defaults.",
                    exc,
                )
                self._data = dict(self._DEFAULTS)
                self._write()

    def save(self) -> None:
        """Persist the current configuration to disk."""
        with self._lock:
            self._write()

    def get(self, key: str, default: Any = None) -> Any:
        """Return the value for *key*, falling back to *default*.

        Args:
            key: Configuration key to look up.
            default: Value returned when *key* is not present.

        Returns:
            The stored value or *default*.
        """
        with self._lock:
            return self._data.get(key, default)

    def set(self, key: str, value: Any) -> None:
        """Set a configuration value and persist to disk.

        Args:
            key: Configuration key.
            value: New value to store.
        """
        with self._lock:
            self._data[key] = value
            self._write()

    def reset_to_defaults(self) -> None:
        """Replace all settings with their default values and persist."""
        with self._lock:
            self._data = dict(self._DEFAULTS)
            self._write()
        logger.info("Configuration reset to defaults.")

    # ------------------------------------------------------------------
    # Internal helpers
    # ------------------------------------------------------------------

    def _write(self) -> None:
        """Write ``self._data`` to the config file (caller holds lock)."""
        try:
            with open(self._path, "w", encoding="utf-8") as fh:
                json.dump(self._data, fh, indent=2)
        except OSError as exc:
            logger.error("Failed to write config file: %s", exc)
