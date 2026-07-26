import tkinter as tk

def create_rounded_polygon(canvas: tk.Canvas, x1: float, y1: float, x2: float, y2: float, radius: int = 0, **kwargs):
    """
    Draw a flat rectangle. 
    Tkinter on Windows lacks anti-aliasing, causing rounded corners to look jagged or 'chopped'.
    A crisp, flat rectangle looks much cleaner and more professional in this framework.
    """
    tag = f"rect_{id(canvas)}_{id(kwargs)}"
    kwargs["tags"] = (tag,)
    
    fill_color = kwargs.get("fill", "")
    if fill_color:
        kwargs["outline"] = fill_color
        
    if "smooth" in kwargs:
        del kwargs["smooth"]
        
    canvas.create_rectangle(x1, y1, x2, y2, **kwargs)
    return tag
