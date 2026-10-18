"""Legt das normierte Rezept-Raster (0..1, Schritt 0,1) ueber die Ansichten von
asset_layout.py und setzt sie zu einem Blatt zusammen. Laeuft mit System-Python + Pillow:

    python3 tools/art/layout_grid.py <layout_dir> <stem> <out.png>
"""
import sys
from PIL import Image, ImageDraw

d, stem, out = sys.argv[1], sys.argv[2], sys.argv[3]
AXES = {"top": ("x", "y"), "side": ("y", "z"), "front": ("x", "z")}
tiles = []
for view, (hx, vy) in AXES.items():
    im = Image.open(f"{d}/{stem}_{view}.png").convert("RGB")
    w, h = im.size
    pad = 34
    canvas = Image.new("RGB", (w + pad * 2, h + pad * 2), (24, 24, 28))
    canvas.paste(im, (pad, pad))
    g = ImageDraw.Draw(canvas)
    for i in range(11):
        t = i / 10
        x = pad + t * w
        y = pad + (1 - t) * h          # vertikal waechst nach oben
        col = (255, 220, 0) if i in (0, 5, 10) else (255, 255, 255)
        g.line([(x, pad), (x, pad + h)], fill=col, width=1)
        g.line([(pad, y), (pad + w, y)], fill=col, width=1)
        g.text((x - 8, pad + h + 6), f"{t:.1f}", fill=(255, 255, 0))
        g.text((4, y - 6), f"{t:.1f}", fill=(255, 255, 0))
    g.text((pad, 8), f"{view}: horizontal {hx}, vertikal {vy}", fill=(120, 220, 255))
    tiles.append(canvas)
W = sum(t.size[0] for t in tiles)
H = max(t.size[1] for t in tiles)
sheet = Image.new("RGB", (W, H), (24, 24, 28))
x = 0
for t in tiles:
    sheet.paste(t, (x, 0))
    x += t.size[0]
sheet.save(out)
print(out)
