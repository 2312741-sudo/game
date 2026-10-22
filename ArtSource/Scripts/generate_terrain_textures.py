#!/usr/bin/env python3
"""
Generates the Terrain Layer textures and the two Detail (grass) textures for the
street-end terrains of Tram Chanh (see Docs/Design/COURSE_BAI3_TERRAIN.md).

Output folder: Assets/TramChanh/Art/Textures/Terrain/
  T_Terrain_Dirt_Albedo.png      T_Terrain_Dirt_Normal.png      (512x512, seamless)
  T_Terrain_Grass_Albedo.png     T_Terrain_Grass_Normal.png     (512x512, seamless)
  T_Terrain_Gravel_Albedo.png    T_Terrain_Gravel_Normal.png    (512x512, seamless)
  T_Terrain_GrassBlades.png      (256x256 RGBA, Detail billboard)
  T_Terrain_WildFlowers.png      (256x256 RGBA, Detail billboard)

Seamless: every noise is periodic (lattice indices wrap) and the normal maps use
np.roll, so the left edge continues into the right edge and top into bottom.
Deterministic (fixed seeds). No third-party material: Project Original, CC0.

Usage (from the repo root):  python3 ArtSource/Scripts/generate_terrain_textures.py [size]
"""

import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT_DIR = "Assets/TramChanh/Art/Textures/Terrain"


# ----------------------------------------------------------------------------
# Periodic noise helpers
# ----------------------------------------------------------------------------

def periodic_value_noise(size, cells, rng):
    """Smooth value noise on a size x size grid that tiles every `cells` lattice cells."""
    lattice = rng.random((cells, cells))
    t = np.arange(size) * (cells / size)
    i0 = np.floor(t).astype(int)
    f = t - i0
    f = f * f * (3.0 - 2.0 * f)  # smoothstep
    i1 = (i0 + 1) % cells
    i0 = i0 % cells
    # separable bilinear on the smoothed fractions
    rows0 = lattice[i0][:, i0] * (1 - f)[None, :] + lattice[i0][:, i1] * f[None, :]
    rows1 = lattice[i1][:, i0] * (1 - f)[None, :] + lattice[i1][:, i1] * f[None, :]
    return rows0 * (1 - f)[:, None] + rows1 * f[:, None]


def fbm(size, base_cells, octaves, rng, persistence=0.5):
    total = np.zeros((size, size))
    amp, norm, cells = 1.0, 0.0, base_cells
    for _ in range(octaves):
        total += amp * periodic_value_noise(size, cells, rng)
        norm += amp
        amp *= persistence
        cells *= 2
    return total / norm


def periodic_worley(size, cells, rng):
    """Returns (F1 distance in cell units, id of the nearest feature point in [0,1))."""
    pts = rng.random((cells, cells, 2))
    ids = rng.random((cells, cells))
    coords = (np.arange(size) + 0.5) * (cells / size)
    gy, gx = np.meshgrid(coords, coords, indexing="ij")
    cy, cx = np.floor(gy).astype(int), np.floor(gx).astype(int)
    best = np.full((size, size), 1e9)
    best_id = np.zeros((size, size))
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            ny, nx = cy + dy, cx + dx
            py = ny + pts[ny % cells, nx % cells, 0]
            px = nx + pts[ny % cells, nx % cells, 1]
            d = np.sqrt((gy - py) ** 2 + (gx - px) ** 2)
            closer = d < best
            best = np.where(closer, d, best)
            best_id = np.where(closer, ids[ny % cells, nx % cells], best_id)
    return best, best_id


def normalise(a):
    lo, hi = a.min(), a.max()
    return (a - lo) / (hi - lo + 1e-9)


def compute_normal_map(height01, strength):
    """Tangent-space normal map (Unity convention, green up) from a 0..1 height field. Wraps."""
    dx = (np.roll(height01, -1, axis=1) - np.roll(height01, 1, axis=1)) * 0.5
    dy = (np.roll(height01, -1, axis=0) - np.roll(height01, 1, axis=0)) * 0.5
    nx, ny, nz = -dx * strength, dy * strength, np.ones_like(height01)
    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    nx, ny, nz = nx / length, ny / length, nz / length
    rgb = np.stack([nx * 0.5 + 0.5, ny * 0.5 + 0.5, nz * 0.5 + 0.5], axis=-1)
    return (rgb * 255).astype(np.uint8)


def to_rgb(r, g, b):
    return np.clip(np.stack([r, g, b], axis=-1) * 255.0, 0, 255).astype(np.uint8)


def save(arr, name):
    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, name)
    Image.fromarray(arr).save(path)
    print("  wrote", path)


# ----------------------------------------------------------------------------
# Terrain layers
# ----------------------------------------------------------------------------

def make_dirt(size):
    rng = np.random.default_rng(1101)
    large = fbm(size, 4, 5, rng)
    mid = fbm(size, 16, 4, rng)
    grain = rng.random((size, size))
    height = normalise(0.55 * large + 0.35 * mid + 0.10 * grain)
    tone = 0.75 + 0.5 * normalise(large)
    r = (0.30 + 0.10 * height) * tone
    g = (0.21 + 0.08 * height) * tone
    b = (0.14 + 0.05 * height) * tone
    # darker damp patches
    damp = np.clip((normalise(mid) - 0.65) * 2.0, 0, 1) * 0.25
    r, g, b = r * (1 - damp), g * (1 - damp), b * (1 - damp)
    return to_rgb(r, g, b), compute_normal_map(height, 3.0)


def make_grass(size):
    rng = np.random.default_rng(2202)
    patch = fbm(size, 4, 5, rng)
    blades = fbm(size, 64, 3, rng, persistence=0.65)
    fine = rng.random((size, size))
    height = normalise(0.4 * blades + 0.4 * fine + 0.2 * patch)
    tone = 0.7 + 0.6 * normalise(patch)
    r = (0.12 + 0.10 * height) * tone
    g = (0.27 + 0.16 * height) * tone
    b = (0.07 + 0.05 * height) * tone
    # a few dry yellowish patches
    dry = np.clip((normalise(fbm(size, 8, 3, rng)) - 0.7) * 3.0, 0, 1) * 0.35
    r, g = r + dry * 0.18, g - dry * 0.04
    return to_rgb(r, g, b), compute_normal_map(height, 2.5)


def make_gravel(size):
    rng = np.random.default_rng(3303)
    cells = max(16, size // 18)
    dist, pid = periodic_worley(size, cells, rng)
    pebble = np.clip(1.0 - dist / 0.62, 0, 1) ** 0.6   # rounded dome per pebble
    height = normalise(0.85 * pebble + 0.15 * fbm(size, 32, 3, rng))
    shade = 0.42 + 0.45 * pid                           # each pebble has its own grey
    warm = 0.92 + 0.16 * fbm(size, 8, 3, rng)
    crevice = 0.55 + 0.45 * pebble                      # gaps between pebbles darker
    r = shade * warm * crevice * 0.92
    g = shade * crevice * 0.88
    b = shade * crevice * 0.80
    return to_rgb(r, g, b), compute_normal_map(height, 4.0)


# ----------------------------------------------------------------------------
# Detail billboards (RGBA, transparent background)
# ----------------------------------------------------------------------------

SS = 4  # supersampling factor


def _blade(draw, base_x, base_y, tip_x, tip_y, width, color):
    # tapered blade: quadratic bend from base to tip
    n = 12
    left, right = [], []
    for i in range(n + 1):
        t = i / n
        x = (1 - t) * base_x + t * tip_x + 14 * SS * (t * t) * np.sign(tip_x - base_x + 1e-6) * 0.3
        y = (1 - t) * base_y + t * tip_y
        w = width * (1 - t) ** 0.8
        left.append((x - w, y))
        right.append((x + w, y))
    draw.polygon(left + right[::-1], fill=color)


def make_grass_blades(size=256):
    rng = np.random.default_rng(4404)
    big = size * SS
    img = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for _ in range(22):
        bx = big * (0.5 + rng.normal(0, 0.12))
        tx = bx + rng.normal(0, big * 0.14)
        ty = big * rng.uniform(0.08, 0.5)
        shade = rng.uniform(0.6, 1.1)
        col = (int(60 * shade), int(120 * shade), int(40 * shade), 255)
        _blade(d, bx, big - 2, tx, ty, rng.uniform(5, 9) * SS, col)
    return img.resize((size, size), Image.LANCZOS)


def make_wild_flowers(size=256):
    rng = np.random.default_rng(5505)
    big = size * SS
    img = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for _ in range(7):
        bx = big * rng.uniform(0.2, 0.8)
        top_y = big * rng.uniform(0.22, 0.55)
        tx = bx + rng.normal(0, big * 0.05)
        d.line([(bx, big - 2), (tx, top_y)], fill=(70, 130, 55, 255), width=int(3 * SS))
        for _ in range(3):  # leaves
            ly = rng.uniform(top_y + 40 * SS, big - 20 * SS)
            _blade(d, bx, ly, bx + rng.choice([-1, 1]) * 28 * SS, ly - 30 * SS, 4 * SS, (60, 125, 45, 255))
        r = rng.uniform(7, 10) * SS
        petal = (238, 236, 220, 255) if rng.random() < 0.6 else (236, 214, 90, 255)
        for k in range(5):
            a = k * 2 * np.pi / 5
            px, py = tx + np.cos(a) * r, top_y + np.sin(a) * r
            d.ellipse([px - r * 0.8, py - r * 0.8, px + r * 0.8, py + r * 0.8], fill=petal)
        d.ellipse([tx - r * 0.5, top_y - r * 0.5, tx + r * 0.5, top_y + r * 0.5], fill=(200, 150, 40, 255))
    out = img.resize((size, size), Image.LANCZOS)
    return out


def main():
    size = int(sys.argv[1]) if len(sys.argv) > 1 else 512
    print(f"Generating terrain textures at {size}x{size} into {OUT_DIR}")
    for name, fn in (("Dirt", make_dirt), ("Grass", make_grass), ("Gravel", make_gravel)):
        albedo, normal = fn(size)
        save(albedo, f"T_Terrain_{name}_Albedo.png")
        save(normal, f"T_Terrain_{name}_Normal.png")
    os.makedirs(OUT_DIR, exist_ok=True)
    make_grass_blades().save(os.path.join(OUT_DIR, "T_Terrain_GrassBlades.png"))
    make_wild_flowers().save(os.path.join(OUT_DIR, "T_Terrain_WildFlowers.png"))
    print("Done.")


if __name__ == "__main__":
    main()
