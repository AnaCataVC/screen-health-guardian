from PIL import Image, ImageDraw

def create_icon_image():
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

    # Save as ICO with multiple sizes for Windows
    img.save("icon.ico", format="ICO", sizes=[(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    print("icon.ico generated successfully.")

if __name__ == "__main__":
    create_icon_image()
