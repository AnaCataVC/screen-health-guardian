# Tkinter Canvas Aliasing in Windows

## The Problem
When trying to create modern UI elements (like rounded buttons) by drawing shapes (`create_polygon`, `create_oval`) inside a Tkinter `Canvas` on Windows, the resulting edges suffer from severe aliasing (jagged, pixelated borders). The native Windows rendering engine does not apply anti-aliasing to Tkinter canvas shapes by default, making custom-drawn UIs look unprofessional.

## The Solution
To achieve a clean, modern look without aliasing artifacts:
1. Avoid drawing custom shapes in a `Canvas` for interactive elements.
2. Use native `tk.Button` widgets.
3. Apply `relief="flat"` and `bd=0` to the button to remove the legacy 3D borders.
4. Rely on the operating system's native text and widget rendering, which handles sub-pixel anti-aliasing automatically.

This delegates the rendering to the OS, ensuring crisp text and clean rectangular boundaries.
