# Tkinter Transient Windows Visibility

## The Problem
In applications designed to run exclusively in the system tray (background apps), it is standard practice to hide the main root window using `root.withdraw()`. However, if secondary windows (like Settings panels or Alert Overlays) are created and explicitly set as transient to the root window (e.g., `settings_win.transient(root)`), they will fail to appear on screen in Windows.

## The Cause
In the Tkinter implementation for Windows, a window declared as `transient` strictly inherits the state of its parent (master) window. If the master window is `withdrawn` (hidden), all of its transient children are forcibly forced into the `withdrawn` state by the OS. The code will execute without errors, but the windows will remain invisible (`winfo_ismapped() == 0`).

## The Solution
For background applications where `root` is hidden:
1. **Do not use `transient(root)`** on secondary windows that need to be visible.
2. If you need the window to stay on top without a taskbar icon (like a custom alert popup), use `overrideredirect(True)` combined with `wm_attributes("-topmost", True)`. 
3. If it's a standard dialog (like Settings), simply instantiate it via `tk.Toplevel(root)` without calling `transient()`, allowing it to map and display independently of the hidden root.
