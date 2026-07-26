#!/usr/bin/env python3
"""Baut je Fraktion eine Stilplatte aus Innenausschnitten gelungener Assets.

Zweck: Dem Bildmodell Material, Farbwelt, Leuchtlinien-Anmutung und Malweise
vorgeben, OHNE ihm eine Silhouette zum Abmalen zu liefern. Deshalb werden
ausschliesslich Bildmitten ausgeschnitten - nie Kanten, nie Umrisse.
"""

import pathlib
import random
from PIL import Image, ImageFilter

ROOT = pathlib.Path(__file__).parent
IMG = ROOT / "img"
OUT = ROOT / "style"
OUT.mkdir(exist_ok=True)

# Quellen bewusst nur aus Bildern, die stilistisch sitzen.
SOURCES = {
    "alliance": ["alliance_unit_LightTank.png", "alliance_building_HQ.png",
                 "alliance_building_VehicleFactory.png", "alliance_building_Storage.png"],
    "legion": ["legion_unit_LightTank.png", "legion_building_Power.png",
               "legion_building_Refinery.png", "legion_building_ResearchLab.png"],
}

PLATE = 1024
GRID = 4          # 4x4 Kacheln
TILE = PLATE // GRID
# Nur aus dem mittleren Bereich schneiden: dort ist Objektoberflaeche,
# kein Umriss und kein Hintergrund.
SAFE = (0.34, 0.48, 0.66, 0.78)


def patches(path: pathlib.Path, count: int, rng: random.Random) -> list[Image.Image]:
    im = Image.open(path).convert("RGB")
    w, h = im.size
    x0, y0, x1, y1 = int(w*SAFE[0]), int(h*SAFE[1]), int(w*SAFE[2]), int(h*SAFE[3])
    out = []
    for _ in range(count):
        size = rng.randint(int(TILE*0.7), int(TILE*1.05))
        size = min(size, x1-x0, y1-y0)
        px = rng.randint(x0, x1-size)
        py = rng.randint(y0, y1-size)
        out.append(im.crop((px, py, px+size, py+size)).resize((TILE, TILE), Image.LANCZOS))
    return out


def build(faction: str) -> pathlib.Path:
    rng = random.Random(hash(faction) & 0xFFFF)
    tiles: list[Image.Image] = []
    per = (GRID*GRID) // len(SOURCES[faction]) + 1
    for name in SOURCES[faction]:
        p = IMG / name
        if p.exists():
            tiles += patches(p, per, rng)
    rng.shuffle(tiles)
    tiles = tiles[:GRID*GRID]

    plate = Image.new("RGB", (PLATE, PLATE), (11, 16, 23))
    for i, t in enumerate(tiles):
        plate.paste(t, ((i % GRID)*TILE, (i // GRID)*TILE))
    # Leichte Weichzeichnung an den Kachelgrenzen, damit das Raster nicht
    # selbst als Struktur gelesen und mitkopiert wird.
    plate = plate.filter(ImageFilter.GaussianBlur(0.6))
    dst = OUT / f"styleplate_{faction}.png"
    plate.save(dst)
    return dst


if __name__ == "__main__":
    for f in SOURCES:
        d = build(f)
        print(f"{d.name}: {d.stat().st_size//1024} KB")
