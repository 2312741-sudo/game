#!/usr/bin/env python3
"""
PBR surface pass 2 for Tram Chanh (numpy + PIL only, deterministic, seamless).

Writes NEW files only (never overwrites an existing texture of another name):
  T_<Surface>_Normal_v2.png  tangent-space normal, OpenGL/Unity convention (green = +Y up)
  T_<Surface>_Mask_v2.png    URP packed map: R metallic, G occlusion, B detail mask, A smoothness

Surfaces: Asphalt (2048), Sidewalk (2048), Wood (1024), Corrugated (1024),
          Plastic (1024), Cloth (1024), Plaster (1024; normal only is applied), Shutter (1024).

Run from the repository root:  python3 ArtSource/Scripts/generate_pbr_pass2.py
Re-running rewrites the *_v2 files with identical bytes.
"""
import os
import numpy as np
from PIL import Image

OUT_DIR = os.path.join("Assets", "TramChanh", "Art", "Textures")
SEED = 20261022


# ----------------------------------------------------------------- helpers
def rng_for(name):
    return np.random.default_rng(SEED + sum(ord(c) * (i + 1) for i, c in enumerate(name)))


def noise(rng, h, w, beta=2.0, fx=1.0, fy=1.0):
    """Periodic noise from a power-law spectrum (FFT => tiles perfectly). Returns [0,1].
    beta: spectral falloff (higher = smoother). fx/fy stretch the spectrum (anisotropy)."""
    ky = np.fft.fftfreq(h)[:, None] * h
    kx = np.fft.fftfreq(w)[None, :] * w
    k = np.sqrt((kx / fx) ** 2 + (ky / fy) ** 2)
    k[0, 0] = 1.0
    amp = 1.0 / k ** (beta / 2.0)
    amp[0, 0] = 0.0
    spec = amp * np.exp(2j * np.pi * rng.random((h, w)))
    n = np.real(np.fft.ifft2(spec))
    n -= n.min()
    n /= max(n.max(), 1e-9)
    return n


def band(rng, size, lo, hi, beta=2.0):
    """Band-limited periodic noise [0,1] with only wavenumbers in [lo, hi] cycles/tile."""
    ky = np.fft.fftfreq(size)[:, None] * size
    kx = np.fft.fftfreq(size)[None, :] * size
    k = np.sqrt(kx ** 2 + ky ** 2)
    m = ((k >= lo) & (k <= hi)).astype(float)
    k[0, 0] = 1.0
    spec = m / k ** (beta / 2.0) * np.exp(2j * np.pi * rng.random((size, size)))
    n = np.real(np.fft.ifft2(spec))
    n -= n.min()
    n /= max(n.max(), 1e-9)
    return n


def worley(rng, size, cells, warp=0.0, warp_noise=None):
    """Periodic Worley. Returns (F1, F2, cell_id_value) in units of cell size."""
    pts = rng.random((cells, cells, 2))
    ids = rng.random((cells, cells))
    ys, xs = np.mgrid[0:size, 0:size].astype(np.float64)
    u = xs / size * cells
    v = ys / size * cells
    if warp and warp_noise is not None:
        u = u + (warp_noise[0] - 0.5) * warp
        v = v + (warp_noise[1] - 0.5) * warp
    cu = np.floor(u).astype(int)
    cv = np.floor(v).astype(int)
    f1 = np.full(u.shape, 9.0)
    f2 = np.full(u.shape, 9.0)
    cid = np.zeros(u.shape)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            gx = cu + dx
            gy = cv + dy
            px = gx + pts[gy % cells, gx % cells, 0]
            py = gy + pts[gy % cells, gx % cells, 1]
            d = np.sqrt((u - px) ** 2 + (v - py) ** 2)
            closer = d < f1
            f2 = np.where(closer, f1, np.minimum(f2, d))
            cid = np.where(closer, ids[gy % cells, gx % cells], cid)
            f1 = np.where(closer, d, f1)
    return f1, f2, cid


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def norm01(a):
    a = a - a.min()
    return a / max(a.max(), 1e-9)


def normal_from_height(hgt, rms_slope):
    """hgt in [0,1] (periodic). rms_slope = target RMS tangent of the surface slope
    (0.3 = visible but calm, 0.6 = strong). OpenGL/Unity: +X right, +Y up
    (image rows go down, so 'up' = previous row)."""
    dx = (np.roll(hgt, -1, axis=1) - np.roll(hgt, 1, axis=1)) * 0.5
    dy_up = (np.roll(hgt, 1, axis=0) - np.roll(hgt, -1, axis=0)) * 0.5
    raw = np.sqrt(np.mean(dx * dx + dy_up * dy_up))
    k = rms_slope / max(raw, 1e-9)
    nx = -dx * k
    ny = -dy_up * k
    nz = np.ones_like(hgt)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.stack([nx / ln, ny / ln, nz / ln], axis=-1)


def pack_normal(n):
    rgb = np.clip(np.round((n * 0.5 + 0.5) * 255.0), 0, 255).astype(np.uint8)
    return rgb


def cavity_ao(hgt, radius_noise_scale=1):
    """Cheap AO: darker where height is below its blurred surroundings (periodic blur)."""
    s = hgt.shape[0]
    f = np.fft.fft2(hgt)
    ky = np.fft.fftfreq(s)[:, None]
    kx = np.fft.fftfreq(s)[None, :]
    sigma = 0.012 * radius_noise_scale
    g = np.exp(-2 * (np.pi ** 2) * sigma ** 2 * (kx ** 2 + ky ** 2) * s * s * 0.25)
    blur = np.real(np.fft.ifft2(f * g))
    return np.clip(0.5 + (hgt - blur) * 6.0, 0.0, 1.0)


def to_u8(a):
    return np.clip(np.round(a * 255.0), 0, 255).astype(np.uint8)


def write_png(arr, name):
    path = os.path.join(OUT_DIR, name)
    mode = "RGBA" if arr.shape[-1] == 4 else "RGB"
    Image.fromarray(arr, mode).save(path, optimize=True)
    print("wrote", path, arr.shape)


def write_set(name, size, hgt, rms_slope, metallic, occlusion, detail, smooth, normal_only=False):
    n = normal_from_height(hgt, rms_slope)
    write_png(pack_normal(n), "T_%s_Normal_v2.png" % name)
    if normal_only:
        return
    mask = np.stack([to_u8(metallic), to_u8(occlusion), to_u8(detail), to_u8(smooth)], axis=-1)
    write_png(mask, "T_%s_Mask_v2.png" % name)


# ---------------------------------------------------------------- surfaces
def asphalt(size=2048):
    r = rng_for("asphalt")
    # aggregate: Worley grains of two sizes + fine noise
    f1, _, cid = worley(r, size, 160)
    grain = (1 - smoothstep(0.0, 0.75, f1)) * (0.6 + 0.4 * cid)
    f1b, _, cidb = worley(r, size, 420)
    fine = (1 - smoothstep(0.0, 0.7, f1b)) * (0.5 + 0.5 * cidb)
    patina = noise(r, size, size, beta=2.6)
    # cracks: thin edges (F2-F1) of a warped coarse Worley, only where a mask noise allows them
    wn = (noise(r, size, size, beta=3.2), noise(r, size, size, beta=3.2))
    c1, c2, _ = worley(r, size, 7, warp=0.9, warp_noise=wn)
    edge = c2 - c1
    crack_zone = smoothstep(0.52, 0.68, noise(r, size, size, beta=3.0))
    crack = (1 - smoothstep(0.0, 0.035, edge)) * crack_zone
    # patched repair strips (smooth, slightly raised)
    patch = smoothstep(0.62, 0.66, noise(r, size, size, beta=3.6))
    hgt = 0.34 * grain + 0.18 * fine + 0.20 * patina + 0.08 * patch - 0.55 * crack
    hgt = norm01(hgt)
    # wet film: low areas hold water -> high smoothness; peaks of grain stay rough
    low = 1.0 - smoothstep(0.30, 0.55, hgt)
    wet_zone = smoothstep(0.58, 0.68, noise(r, size, size, beta=3.4))
    puddle = np.clip(low * 0.3 + wet_zone * 0.9, 0, 1)
    smooth = 0.42 + 0.18 * (1 - grain) + 0.0
    smooth = np.where(puddle > 0.55, 0.93 + 0.05 * smoothstep(0.55, 1.0, puddle), smooth)
    smooth = np.clip(smooth - 0.18 * crack, 0, 1)
    occl = np.clip(0.55 + 0.45 * cavity_ao(hgt) - 0.35 * crack, 0, 1)
    write_set("Asphalt", size, hgt, 0.35, np.zeros_like(hgt), occl, 0.4 + 0.6 * grain, smooth)


def sidewalk(size=2048):
    r = rng_for("sidewalk")
    n = 8                                   # 8x8 paving tiles per texture
    ys, xs = np.mgrid[0:size, 0:size]
    cell = size // n
    lx = (xs % cell) / cell
    ly = (ys % cell) / cell
    ti = (xs // cell) + (ys // cell) * n
    tile_off = r.random(n * n)[ti]
    d_edge = np.minimum(np.minimum(lx, 1 - lx), np.minimum(ly, 1 - ly)) * cell   # pixels
    grout_w = cell * 0.018
    grout = 1 - smoothstep(grout_w * 0.6, grout_w * 1.6, d_edge)                 # 1 in the joint
    bevel = smoothstep(grout_w, grout_w * 5, d_edge)
    surface = noise(r, size, size, beta=2.4)
    sand = band(r, size, 180, 700, beta=0.8)                                     # fine grit
    wear = smoothstep(0.55, 0.9, noise(r, size, size, beta=3.2))                 # worn patches
    chip = (1 - smoothstep(0.0, 0.25, worley(r, size, 90)[0])) * smoothstep(0.55, 0.8, noise(r, size, size, beta=2.0))
    hgt = (0.55 + 0.10 * tile_off + 0.05 * (surface - 0.5) + 0.03 * (sand - 0.5)
           - 0.20 * grout - 0.06 * (1 - bevel) - 0.05 * wear - 0.12 * chip)
    hgt = norm01(hgt)
    smooth = 0.38 - 0.10 * (sand - 0.5) + 0.12 * wear - 0.15 * grout - 0.10 * chip
    smooth = np.clip(smooth, 0.12, 0.6)
    occl = np.clip(1.0 - 0.55 * grout - 0.12 * (1 - bevel) - 0.2 * chip, 0, 1)
    write_set("Sidewalk", size, hgt, 0.30, np.zeros_like(hgt), occl, 0.5 + 0.5 * wear, smooth)


def wood(size=1024):
    r = rng_for("wood")
    planks = 5
    ys, xs = np.mgrid[0:size, 0:size]
    pid = ys * planks // size
    ly = (ys * planks / size) % 1.0
    d_edge = np.minimum(ly, 1 - ly) * (size / planks)
    seam = 1 - smoothstep(0.8, 3.0, d_edge)
    # grain: noise stretched strongly along X (fx small => long features along x)
    long_grain = noise(r, size, size, beta=1.6, fx=0.06, fy=1.0)
    fibre = noise(r, size, size, beta=0.6, fx=0.03, fy=1.0)
    ring = np.sin((noise(r, size, size, beta=3.0, fx=0.2, fy=1.0) * 14 + ly * 0.0) * 2 * np.pi)
    pore = (noise(r, size, size, beta=0.0, fx=0.05, fy=1.0) > 0.93).astype(float)
    plank_tone = r.random(planks)[pid]
    hgt = 0.55 * long_grain + 0.30 * fibre + 0.10 * ring + 0.10 * plank_tone - 0.25 * pore - 0.10 * seam
    hgt = norm01(hgt)
    # varnish worn near the front edge of boards & by use
    use = smoothstep(0.45, 0.8, noise(r, size, size, beta=3.2))
    smooth = np.clip(0.50 - 0.22 * use - 0.2 * pore - 0.2 * seam + 0.08 * (long_grain - 0.5), 0.15, 0.6)
    occl = np.clip(1 - 0.6 * seam - 0.35 * pore, 0, 1)
    write_set("Wood", size, hgt, 0.22, np.zeros_like(hgt), occl, 0.5 + 0.5 * fibre, smooth)


def corrugated(size=1024):
    r = rng_for("corrugated")
    ys, xs = np.mgrid[0:size, 0:size].astype(float)
    ridges = 16                                 # sheets ripple along Y like the existing normal
    wave = np.sin(ys / size * ridges * 2 * np.pi + 0.35 * noise(r, size, size, beta=3.0))
    base = 0.5 + 0.5 * wave
    # rust: blotchy, gathers in the valleys
    rust_n = noise(r, size, size, beta=2.8)
    valley = 1 - base
    rust = smoothstep(0.52, 0.72, rust_n * 0.75 + 0.35 * valley * rust_n)
    pit = (1 - smoothstep(0.0, 0.5, worley(r, size, 70)[0])) * (0.4 + 0.6 * rust)
    pit *= (noise(r, size, size, beta=2.0) > 0.4)
    scratch = band(r, size, 100, 400, beta=0.5)
    hgt = 0.80 * base + 0.08 * rust_n + 0.06 * (scratch - 0.5) - 0.12 * pit
    hgt = norm01(hgt)
    metallic = np.clip(0.92 - 0.85 * rust, 0, 1)
    smooth = np.clip(0.55 - 0.40 * rust - 0.12 * pit - 0.10 * (scratch - 0.5) + 0.10 * base, 0.08, 0.7)
    occl = np.clip(0.55 + 0.45 * base - 0.3 * pit, 0, 1)
    write_set("Corrugated", size, hgt, 0.55, metallic, occl, 0.3 + 0.7 * rust, smooth)


def plastic(size=1024):
    r = rng_for("plastic")
    # injection-mould texture: fine stipple + faint orange peel + a faint grid of cooling marks
    stipple = band(r, size, 140, 420, beta=0.5)
    peel = noise(r, size, size, beta=2.2)
    # scuffs: short bright scratches from long thin band noise in two directions
    s1 = noise(r, size, size, beta=1.0, fx=0.04, fy=1.0)
    s2 = noise(r, size, size, beta=1.0, fx=1.0, fy=0.04)
    scuff = np.maximum(smoothstep(0.80, 0.92, s1), smoothstep(0.82, 0.93, s2))
    scuff *= smoothstep(0.45, 0.7, noise(r, size, size, beta=3.0))
    hgt = norm01(0.55 * stipple + 0.30 * peel - 0.35 * scuff)
    smooth = np.clip(0.50 - 0.22 * scuff - 0.10 * (stipple - 0.5) - 0.08 * (peel - 0.5), 0.2, 0.62)
    occl = np.clip(0.9 + 0.1 * cavity_ao(hgt) - 0.1 * scuff, 0, 1)
    write_set("Plastic", size, hgt, 0.18, np.zeros_like(hgt), occl, 0.4 + 0.6 * scuff, smooth)


def cloth(size=1024):
    r = rng_for("cloth")
    ys, xs = np.mgrid[0:size, 0:size].astype(float)
    threads = 128                                # threads per tile in each direction
    px = xs / size * threads * 2 * np.pi
    py = ys / size * threads * 2 * np.pi
    # plain weave: over/under checker of two thread directions
    warp = 0.5 + 0.5 * np.cos(px)                # vertical threads, brightness across x
    weft = 0.5 + 0.5 * np.cos(py)
    checker = 0.5 + 0.5 * np.sign(np.cos(px * 0.5) * np.cos(py * 0.5) + 1e-9)
    thread_irreg = noise(r, size, size, beta=1.2, fx=1.0, fy=0.05)   # slubs along threads
    thread_irreg2 = noise(r, size, size, beta=1.2, fx=0.05, fy=1.0)
    hgt = checker * warp + (1 - checker) * weft
    hgt = 0.80 * hgt + 0.12 * thread_irreg + 0.12 * thread_irreg2
    fold = noise(r, size, size, beta=3.6)
    hgt = norm01(hgt + 0.25 * fold)
    smooth = np.clip(0.28 + 0.06 * (fold - 0.5) - 0.06 * (hgt - 0.5), 0.15, 0.38)
    occl = np.clip(0.7 + 0.3 * hgt, 0, 1)
    write_set("Cloth", size, hgt, 0.30, np.zeros_like(hgt), occl, 0.6 + 0.4 * thread_irreg, smooth)


def plaster(size=1024):
    r = rng_for("plaster")
    coarse = noise(r, size, size, beta=3.0)
    trowel = noise(r, size, size, beta=2.4, fx=0.35, fy=1.0)      # faint trowel sweeps
    grit = band(r, size, 150, 480, beta=0.5)
    pit = (1 - smoothstep(0.0, 0.35, worley(r, size, 110)[0])) * (noise(r, size, size, beta=1.5) > 0.55)
    hgt = norm01(0.45 * coarse + 0.20 * trowel + 0.25 * grit - 0.25 * pit)
    write_set("Plaster", size, hgt, 0.20, np.zeros_like(hgt), np.ones_like(hgt), grit, np.full_like(hgt, 0.25),
              normal_only=True)


def shutter(size=1024):
    r = rng_for("shutter")
    ys = np.mgrid[0:size, 0:size][0].astype(float)
    slats = 32
    ph = (ys / size * slats) % 1.0
    profile = 0.5 - 0.5 * np.cos(ph * 2 * np.pi)                # rounded slats
    lip = smoothstep(0.0, 0.08, ph) * (1 - smoothstep(0.92, 1.0, ph))
    dent = noise(r, size, size, beta=2.6)
    rust = smoothstep(0.55, 0.8, noise(r, size, size, beta=2.8))
    grain = band(r, size, 120, 400, beta=0.6)
    hgt = norm01(0.75 * profile * lip + 0.12 * dent + 0.05 * grain - 0.1 * rust)
    metallic = np.clip(0.85 - 0.8 * rust, 0, 1)
    smooth = np.clip(0.45 - 0.3 * rust + 0.1 * profile, 0.1, 0.6)
    occl = np.clip(0.4 + 0.6 * profile * lip + 0.0, 0, 1)
    write_set("Shutter", size, hgt, 0.50, metallic, occl, 0.3 + 0.7 * rust, smooth)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for fn in (asphalt, sidewalk, wood, corrugated, plastic, cloth, plaster, shutter):
        print("--", fn.__name__)
        fn()


if __name__ == "__main__":
    main()
