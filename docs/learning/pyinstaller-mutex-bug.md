# PyInstaller Mutex Bug (Single Instance Lock)

## The Problem
When building a desktop application, it's common to prevent multiple instances from running simultaneously by using a Windows Mutex (`win32event.CreateMutex`). However, when the application is bundled using **PyInstaller**, calling `GetLastError()` chronically returns `183` (`ERROR_ALREADY_EXISTS`) during initialization. This creates a false positive, tricking the application into believing another instance is already running, which causes it to silently crash or exit immediately upon launch.

## The Solution
Abandon the traditional Windows Mutex in favor of a network-based "Socket Lock". 
1. The application attempts to bind a local TCP socket to a specific, obscure port (e.g., `127.0.0.1:49133`).
2. If the port is already in use (`socket.error`), it guarantees with 100% certainty that another instance of the application is currently running.
3. If the bind is successful, the socket is kept open for the lifecycle of the application.

This approach is completely immune to PyInstaller's runtime environment quirks and works flawlessly in `--windowed` mode.
