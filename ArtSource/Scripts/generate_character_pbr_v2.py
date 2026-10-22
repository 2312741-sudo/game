#!/usr/bin/env python3
"""
generate_character_pbr_v2.py  (Character And Animal PBR Pass 2)

Writes seamless detail-normal maps and one atlas-aligned mask map for people and animals.
Deterministic (fixed seeds, numpy + PIL only). Never overwrites an existing file.

Outputs (default Assets/TramChanh/Art/Textures/Characters/):
  T_Cloth_Weave_Normal_v2.png    512  plain-weave cloth, 32 threads across the tile
  T_Skin_Micro_Normal_v2.png     512  very faint pores and skin relief
  T_Fur_Micro_Normal_v2.png      512  fine, slightly vertical fur strands
  T_Hair_Strand_Normal_v2.png    512  parallel hair strands, slightly wavy
  T_Leather_Grain_Normal_v2.png  512  pebbled leather grain (shoes, jackets)
  T_Rubber_Sole_Normal_v2.png    512  fine rubber stipple (soles)
  T_Animal_Atlas_Mask_v2.png     512  URP metallic-gloss map aligned 1:1 with T_Animal_Atlas.png
                                      (R metallic, G occlusion, B unused, A smoothness)

Normal maps: RGB = tangent-space normal (OpenGL / Unity convention, +Y up), A = 255.
All height fields are periodic (FFT noise, wrapped Voronoi, periodic sines) and the normals are
derived with wrap-around differences, so the tile edges are seamless.

Usage (from the repo root):  python3 ArtSource/Scripts/generate_character_pbr_v2.py [--out DIR]
"""
import argparse
import os
import sys

import numpy as np
from PIL import Image

SIZE = 512
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DEFAULT_OUT = os.path.join(REPO, "Assets", "TramChanh", "Art", "Textures", "Characters")
ATLAS_PATH = os.path.join(REPO, "Assets", "TramChanh", "Art", "Textures", "T_Animal_Atlas.png")


# ---------------------------------------------------------------- helpers
def _grid():
    y, x = np.mgrid[0:SIZE, 0:SIZE].astype(np.float64)
    return x / SIZE, y / SIZE


def periodic_noise(rng, lo, hi, aniso=(1.0, 1.0)):
    """Band-limited periodic noise in [-1, 1]. lo/hi are frequencies (cycles per tile).
    aniso=(ax, ay) stretches the spectrum: ay < 1 elongates features vertically."""
    white = rng.standard_normal((SIZE, SIZE))
    f = np.fft.fft2(white)
    fy = np.fft.fftfreq(SIZE) * SIZE
    fx = np.fft.fftfreq(SIZE) * SIZE
    FX, FY = np.meshgrid(fx, fy)
    r = np.sqrt((FX * aniso[0]) ** 2 + (FY * aniso[1]) ** 2)
    band = np.exp(-((np.log1p(r) - np.log1p((lo * hi) ** 0.5)) ** 2) / (2 * (0.5 * np.log1p(hi / max(lo, 1e-3))) ** 2))
    band[0, 0] = 0
    n = np.real(np.fft.ifft2(f * band))
    return n / (np.abs(n).max() + 1e-12)


def periodic_voronoi(rng, cells):
    """Wrapped Voronoi: returns (F1 distance, F2-F1) in cell units, tileable."""
    cs = SIZE / cells
    jitter = rng.random((cells, cells, 2))
    y, x = np.mgrid[0:SIZE, 0:SIZE].astype(np.float64)
    cx, cy = (x / cs).astype(int), (y / cs).astype(int)
    d1 = np.full((SIZE, SIZE), 1e9)
    d2 = np.full((SIZE, SIZE), 1e9)
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            gx, gy = (cx + ox) % cells, (cy + oy) % cells
            px = (cx + ox + jitter[gy, gx, 0]) * cs
            py = (cy + oy + jitter[gy, gx, 1]) * cs
            d = np.sqrt((x - px) ** 2 + (y - py) ** 2) / cs
            m = d < d1
            d2 = np.where(m, d1, np.minimum(d2, d))
            d1 = np.where(m, d, d1)
    return d1, d2 - d1


def height_to_normal(h, strength):
    """Tileable tangent-space normal (OpenGL, +Y up) from a periodic height field."""
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5
    dy = (np.roll(h, 1, axis=0) - np.roll(h, -1, axis=0)) * 0.5  # image y runs down, +Y is up
    nx, ny, nz = -dx * strength, -dy * strength, np.ones_like(h)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.stack([nx / ln, ny / ln, nz / ln], axis=-1)
    rgb = np.clip((n * 0.5 + 0.5) * 255.0 + 0.5, 0, 255).astype(np.uint8)
    a = np.full((SIZE, SIZE, 1), 255, np.uint8)
    return np.concatenate([rgb, a], axis=-1)


# ---------------------------------------------------------------- height fields
def cloth_weave():
    rng = np.random.default_rng(20261022)
    x, y = _grid()
    threads = 32  # threads per tile -> 16 px per thread
    tx, ty = x * threads, y * threads
    warp = 0.5 + 0.5 * np.cos(2 * np.pi * tx)          # vertical thread crowns
    weft = 0.5 + 0.5 * np.cos(2 * np.pi * ty)          # horizontal thread crowns
    over = (np.floor(tx).astype(int) + np.floor(ty).astype(int)) % 2 == 0
    h = np.where(over, 0.55 + 0.45 * warp * (0.6 + 0.4 * weft), 0.55 + 0.45 * weft * (0.6 + 0.4 * warp))
    h += 0.10 * periodic_noise(rng, 40, 120)           # fibre fuzz
    return h


def skin_micro():
    rng = np.random.default_rng(20261023)
    h = 0.55 * periodic_noise(rng, 6, 20) + 0.25 * periodic_noise(rng, 30, 90)
    d1, _ = periodic_voronoi(rng, 48)                  # shallow pores
    pores = np.clip(1.0 - d1 * 3.2, 0, 1) ** 2
    jitter_mask = (periodic_noise(rng, 3, 10) > 0.15).astype(np.float64)
    h -= 0.5 * pores * jitter_mask
    return h


def fur_micro():
    rng = np.random.default_rng(20261024)
    strands = periodic_noise(rng, 60, 200, aniso=(1.0, 0.12))   # tall thin streaks
    clump = periodic_noise(rng, 6, 18, aniso=(1.0, 0.35))
    return 0.8 * strands + 0.35 * clump


def hair_strand():
    rng = np.random.default_rng(20261025)
    x, y = _grid()
    wave = 0.012 * np.sin(2 * np.pi * (2 * y)) + 0.006 * periodic_noise(rng, 2, 6)
    n_strands = 96
    u = (x + wave) * n_strands
    phase = rng.random(n_strands)
    idx = np.floor(u).astype(int) % n_strands
    f = u - np.floor(u)
    width = 0.35 + 0.15 * rng.random(n_strands)
    prof = np.clip(1.0 - np.abs(f - 0.5) / width[idx], 0, 1) ** 0.7
    mod = 0.85 + 0.15 * np.sin(2 * np.pi * (y * (1 + (idx % 3)) + phase[idx]))
    return prof * mod + 0.08 * periodic_noise(rng, 20, 80, aniso=(1.0, 0.1))


def leather_grain():
    rng = np.random.default_rng(20261026)
    d1, edge = periodic_voronoi(rng, 40)
    pebble = np.clip(1.0 - d1 * 1.1, 0, 1) ** 0.6
    crease = np.clip(edge * 4.0, 0, 1)
    return 0.6 * pebble * (0.5 + 0.5 * crease) + 0.15 * periodic_noise(rng, 30, 100)


def rubber_sole():
    rng = np.random.default_rng(20261027)
    return 0.6 * periodic_noise(rng, 50, 160) + 0.3 * periodic_noise(rng, 8, 24)


# ---------------------------------------------------------------- atlas mask
def atlas_mask():
    """Metallic-gloss map aligned 1:1 with T_Animal_Atlas.png (the animals' UVs index that
    palette, so per-colour smoothness is the only variation the UVs can carry).
    Rules are read from the atlas colour itself (no hand painted regions):
      dark neutral (eyes, nose leather, pads)  -> smoothness 0.75
      pink (inner ear, nose)                    -> 0.40
      strongly saturated yellow (beak, legs)    -> 0.38   (keratin, satin)
      saturated red (comb, wattle)              -> 0.40
      everything else (fur, feathers)           -> 0.20
    R metallic 0, G occlusion 1 (no baked AO), B 0."""
    if not os.path.isfile(ATLAS_PATH):
        raise SystemExit("atlas not found: " + ATLAS_PATH)
    atlas = np.asarray(Image.open(ATLAS_PATH).convert("RGB")).astype(np.float64) / 255.0
    r, g, b = atlas[..., 0], atlas[..., 1], atlas[..., 2]
    mx, mn = atlas.max(-1), atlas.min(-1)
    chroma = mx - mn
    sat = chroma / np.maximum(mx, 1e-6)
    smooth = np.full(mx.shape, 0.20)
    pink = (r > 0.7) & (g > 0.4) & (g < 0.62) & (b > 0.4) & (b < 0.62) & (np.abs(g - b) < 0.1)
    yellow = (sat > 0.75) & (r > 0.6) & (g > 0.35) & (b < 0.2)
    red = (sat > 0.6) & (r > 0.4) & (g < 0.25) & (b < 0.25)
    dark = (mx < 0.36) & (chroma < 0.08)
    smooth[pink] = 0.40
    smooth[yellow] = 0.38
    smooth[red] = 0.40
    smooth[dark] = 0.75
    out = np.zeros((SIZE, SIZE, 4), np.uint8)
    out[..., 0] = 0
    out[..., 1] = 255
    out[..., 3] = np.clip(smooth * 255 + 0.5, 0, 255).astype(np.uint8)
    out[..., 2] = 0
    return out


# ---------------------------------------------------------------- main
TEXTURES = [
    ("T_Cloth_Weave_Normal_v2.png", lambda: height_to_normal(cloth_weave(), 5.0)),
    ("T_Skin_Micro_Normal_v2.png", lambda: height_to_normal(skin_micro(), 2.2)),
    ("T_Fur_Micro_Normal_v2.png", lambda: height_to_normal(fur_micro(), 1.8)),
    ("T_Hair_Strand_Normal_v2.png", lambda: height_to_normal(hair_strand(), 2.5)),
    ("T_Leather_Grain_Normal_v2.png", lambda: height_to_normal(leather_grain(), 2.5)),
    ("T_Rubber_Sole_Normal_v2.png", lambda: height_to_normal(rubber_sole(), 1.5)),
    ("T_Animal_Atlas_Mask_v2.png", atlas_mask),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=DEFAULT_OUT)
    args = ap.parse_args()
    os.makedirs(args.out, exist_ok=True)
    for name, fn in TEXTURES:
        assert name.endswith("_v2.png")
        path = os.path.join(args.out, name)
        if os.path.exists(path):
            print("SKIP (exists, never overwritten): " + path)
            continue
        Image.fromarray(fn(), "RGBA").save(path, optimize=True)
        print("wrote " + path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
