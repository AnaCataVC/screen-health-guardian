"""Work Health Timer — Entry point.

Launches the Work Health Timer application with logging configured.
Run with pythonw.exe to hide the console window:
    pythonw.exe main.py
"""

import logging
import sys
import os
import faulthandler

# In --windowed builds there is no console; an early crash (e.g. a C-level
# fault in a native import) would otherwise vanish silently. Dump any fatal
# fault to a file next to the config so it can be diagnosed post-mortem.
_crash_log = os.path.join(
    os.environ.get("APPDATA", os.path.expanduser("~")),
    "WorkHealthTimer", "crash.log",
)
os.makedirs(os.path.dirname(_crash_log), exist_ok=True)
_crash_fp = open(_crash_log, "w", encoding="utf-8")
faulthandler.enable(_crash_fp)

# Add src directory to path for imports.
# When bundled with PyInstaller (--onefile), files are extracted to a temp
# directory referenced by sys._MEIPASS. In normal mode, use __file__'s dir.
if getattr(sys, 'frozen', False):
    _base_path = os.path.join(sys._MEIPASS, 'src')
else:
    _base_path = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, _base_path)

from constants import APP_NAME, get_config_dir
from app import WorkHealthTimer


def setup_logging() -> None:
    """Configure logging to file and (optionally) console.

    Log file is stored alongside the config file in the APPDATA directory.
    Console output is only enabled when running with python.exe (not pythonw.exe).
    """
    log_file = os.path.join(get_config_dir(), "app.log")

    handlers = [
        logging.FileHandler(log_file, encoding="utf-8"),
    ]

    # Add console handler only when a console is available (python.exe, not pythonw.exe)
    if sys.stdout is not None:
        handlers.append(logging.StreamHandler(sys.stdout))

    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
        handlers=handlers,
    )


import socket
def check_single_instance():
    """Ensure only one instance of the application is running by binding to a local port."""
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        # Use a specific high port for the lock
        s.bind(('127.0.0.1', 49133))
    except socket.error:
        logger = logging.getLogger(__name__)
        logger.warning("Another instance is already running (port in use). Exiting.")
        sys.exit(0)
    # Return the socket to keep it open as long as the app runs
    return s


def main() -> None:
    """Initialize and run the Work Health Timer application."""
    setup_logging()
    logger = logging.getLogger(__name__)

    # Ensure only one instance runs at a time
    _mutex = check_single_instance()

    try:
        logger.info("Starting %s...", APP_NAME)
        app = WorkHealthTimer()
        app.run()
    except Exception as e:
        logger.critical("Fatal error: %s", e, exc_info=True)
        sys.exit(1)


if __name__ == "__main__":
    try:
        main()
    except SystemExit:
        raise
    except BaseException:
        import traceback
        traceback.print_exc(file=_crash_fp)
        _crash_fp.flush()
        raise
