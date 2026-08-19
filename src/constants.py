"""Default constants for the Screen Health Guardian application."""

import os


# Application metadata
APP_NAME = "Screen Health Guardian"
APP_VERSION = "1.1.0"

# Default timer intervals (in minutes)
DEFAULT_LOOK_AWAY_INTERVAL_MIN = 20
DEFAULT_POSTURE_INTERVAL_MIN = 60

# Idle detection
DEFAULT_IDLE_THRESHOLD_SEC = 120  # 2 minutes without input = user is away

# Timer check frequency
CHECK_INTERVAL_SEC = 10  # Poll activity every 10 seconds

# Alert settings
DEFAULT_ALERT_AUTO_DISMISS_SEC = 30
DEFAULT_SOUND_ENABLED = False

# Auto-start
DEFAULT_AUTO_START = False

# Windows Registry (auto-start)
REGISTRY_KEY = r"Software\Microsoft\Windows\CurrentVersion\Run"
REGISTRY_VALUE_NAME = "ScreenHealthGuardian"

# Default Language
DEFAULT_LANGUAGE = 'en'


# Paths
def get_config_dir() -> str:
    """Get the configuration directory path. Uses APPDATA on Windows."""
    app_data = os.environ.get('APPDATA', os.path.expanduser('~'))
    config_dir = os.path.join(app_data, 'ScreenHealthGuardian')
    os.makedirs(config_dir, exist_ok=True)
    return config_dir


def get_config_path() -> str:
    """Get the full path to the config file."""
    return os.path.join(get_config_dir(), 'config.json')


# UI Colors (dark theme)
COLOR_BG_DARK = '#0f0f23'
COLOR_BG_CARD = '#1a1a3e'
COLOR_CARD_BG = '#16172d'
COLOR_BORDER = '#292b4a'
COLOR_INPUT_BG = '#1e1f38'
COLOR_ACCENT_BLUE = '#4fc3f7'
COLOR_ACCENT_HOVER = '#3aaedc'
COLOR_ACCENT_PURPLE = '#b388ff'
COLOR_ACCENT_GREEN = '#69f0ae'
COLOR_ACCENT_CORAL = '#ff8a80'
COLOR_TEXT_PRIMARY = '#ffffff'
COLOR_TEXT_SECONDARY = '#b0bec5'
COLOR_TEXT_MUTED = '#78909c'
COLOR_BUTTON_DISMISS = '#2d2d5e'
COLOR_BUTTON_HOVER = '#3d3d7e'
COLOR_BUTTON_SECONDARY = '#252744'
COLOR_BUTTON_SECONDARY_HOVER = '#32345a'

# Fonts
FONT_FAMILY = 'Segoe UI'
