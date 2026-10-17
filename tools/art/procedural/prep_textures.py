"""Concept image -> front/side/roof projection textures for bake_projected.py.

Usage: python3 prep_textures.py <concept.png> <out_prefix> [x0 y0 x1 y1]
Writes <prefix>_front.png (object crop), _side.png (wall band), _top.png (calm roof plates).
Brightness is lifted linearly (contrast kept) to mean luminance 0.11; a gamma lift washes the
asset_pass grade out.
"""
import sys
import numpy as np
from PIL import Image, ImageFilter

src, pre = sys.argv[1], sys.argv[2]
im = Image.open(src).convert("RGB")
if len(sys.argv) > 6:
    box = tuple(int(v) for v in sys.argv[3:7])
else:
    a = np.asarray(im).astype(float)
    edge = np.concatenate([a[:20].reshape(-1, 3), a[-20:].reshape(-1, 3), a[:, :20].reshape(-1, 3), a[:, -20:].reshape(-1, 3)])
    d = np.abs(a - np.median(edge, axis=0)).sum(-1) > 40
    rows, cols = np.where(d.mean(1) > 0.02)[0], np.where(d.mean(0) > 0.02)[0]
    box = (cols[0], rows[0], cols[-1] + 1, rows[-1] + 1)
c = im.crop(box)


def lift(img, target=0.11):
    x = np.asarray(img).astype(np.float32) / 255
    lum = (x @ np.array([0.2126, 0.7152, 0.0722])).mean()
    return Image.fromarray((np.clip(x * min(target / max(lum, 1e-4), 4.0), 0, 1) * 255).astype(np.uint8))


w, h = c.size
front = lift(c).resize((2048, 2048), Image.LANCZOS)
side = lift(c.crop((int(w * .04), int(h * .32), int(w * .40), int(h * .95)))).resize((2048, 2048), Image.LANCZOS)
# roof: large-scale colour from the wall band, fine grain at low contrast, no recognisable features
base = side.resize((24, 24), Image.BILINEAR).resize((2048, 2048), Image.BICUBIC)
grain = side.convert("L").filter(ImageFilter.GaussianBlur(3)).convert("RGB")
roof = Image.blend(base, Image.blend(base, grain, 0.5), 0.35)
front.save(pre + "_front.png"); side.save(pre + "_side.png"); roof.save(pre + "_top.png")
print("box", box)
