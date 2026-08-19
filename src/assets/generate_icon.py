import os
import shutil
from PIL import Image, ImageDraw

def generate_icons():
    # Resolve repository root
    base_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    icon_png_path = os.path.join(base_dir, "icon.png")
    root_ico_path = os.path.join(base_dir, "icon.ico")
    website_public = os.path.join(base_dir, "website", "public")

    if os.path.exists(icon_png_path):
        img = Image.open(icon_png_path).convert("RGBA")
    else:
        # Fallback to programmatic shape
        size = 256
        color = '#4fc3f7'
        img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        quarter = size // 4
        half = size // 2
        r = quarter - 4
        cx1 = quarter + 4
        cx2 = 3 * quarter - 4
        cy = quarter + 8
        draw.ellipse([cx1 - r, cy - r, cx1 + r, cy + r], fill=color)
        draw.ellipse([cx2 - r, cy - r, cx2 + r, cy + r], fill=color)
        draw.polygon([(cx1 - r, cy + 4), (cx2 + r, cy + 4), (half, size - 12)], fill=color)
        img.save(icon_png_path, format="PNG")
        print(f"Generated fallback icon at {icon_png_path}")

    # Generate multi-resolution ICO for Windows and PyInstaller
    ico_sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    img.save(root_ico_path, format="ICO", sizes=ico_sizes)
    print(f"Saved: {root_ico_path}")

    # Sync to website/public if the directory exists
    if os.path.exists(website_public):
        # 1. icon.ico
        shutil.copyfile(root_ico_path, os.path.join(website_public, "icon.ico"))
        # 2. favicon.ico
        shutil.copyfile(root_ico_path, os.path.join(website_public, "favicon.ico"))
        # 3. icon.png
        shutil.copyfile(icon_png_path, os.path.join(website_public, "icon.png"))
        # 4. favicon.png (optimized 128x128 or full-res)
        favicon_png = img.resize((128, 128), Image.Resampling.LANCZOS)
        favicon_png.save(os.path.join(website_public, "favicon.png"), format="PNG")
        print(f"Synced icons to {website_public}")

if __name__ == "__main__":
    generate_icons()

