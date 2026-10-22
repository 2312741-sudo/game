#!/usr/bin/env python3
"""
ArtSource/Scripts/generate_particle_textures.py

Procedural particle textures for the Effects module (Docs/Design/COURSE_BAI7_PARTICLE_SYSTEM.md).
Project Original, generated with numpy + PIL, deterministic (fixed seeds), CC0. No third-party material.

Writes to Assets/TramChanh/Art/Textures/Effects/ (plus a .meta with a stable GUID per path):
  T_FX_Raindrop.png  64 x 128  thin soft vertical streak (Stretched Billboard rain)
  T_FX_Splash.png    64 x 64   small ring plus a few droplets (water hitting the ground)
  T_FX_Steam.png     128 x 128 soft cloudy puff, noise-modulated radial falloff (steam)
  T_FX_Glow.png      64 x 64   soft round light spot
  T_FX_Trail.png     16 x 64   soft band across the width, constant along the length (tail light TrailRenderer)

All textures are white RGB with the shape in the alpha channel, so the colour comes from the particle
(Start Color, Color over Lifetime) and the material tint.

Usage (from the repository root):
  python3 ArtSource/Scripts/generate_particle_textures.py [--raindrop-horizontal]
  --raindrop-horizontal  rotate the rain streak by 90 degrees (use it if Stretched Billboard turns out to
                         stretch along the texture X axis instead of Y when the lead checks in Unity).
"""

import argparse
import hashlib
import os

import numpy as np
from PIL import Image

OUT_DIR = os.path.join("Assets", "TramChanh", "Art", "Textures", "Effects")


def guid_for(path):
    """Stable 32-hex GUID derived from the project-relative path, so reruns never change a .meta."""
    return hashlib.md5(("TramChanh:" + path.replace(os.sep, "/")).encode("utf-8")).hexdigest()


def smoothstep(edge0, edge1, x):
    t = np.clip((x - edge0) / (edge1 - edge0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def rgba_white(alpha):
    a = np.clip(alpha, 0.0, 1.0)
    rgba = np.empty(a.shape + (4,), dtype=np.uint8)
    rgba[..., 0:3] = 255
    rgba[..., 3] = np.round(a * 255.0).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def grid(width, height):
    y, x = np.mgrid[0:height, 0:width].astype(np.float64)
    u = (x + 0.5) / width * 2.0 - 1.0      # -1..1
    v = (y + 0.5) / height * 2.0 - 1.0     # -1..1
    return u, v


def value_noise(width, height, cells, rng):
    """Smooth value noise in 0..1 (bilinear over a random lattice), no scipy needed."""
    lattice = rng.random((cells + 1, cells + 1))
    xs = np.linspace(0, cells, width, endpoint=False)
    ys = np.linspace(0, cells, height, endpoint=False)
    x0 = np.floor(xs).astype(int)
    y0 = np.floor(ys).astype(int)
    fx = smoothstep(0.0, 1.0, xs - x0)[None, :]
    fy = smoothstep(0.0, 1.0, ys - y0)[:, None]
    a = lattice[np.ix_(y0, x0)]
    b = lattice[np.ix_(y0, x0 + 1)]
    c = lattice[np.ix_(y0 + 1, x0)]
    d = lattice[np.ix_(y0 + 1, x0 + 1)]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def raindrop(horizontal):
    w, h = 64, 128
    u, v = grid(w, h)
    # thin streak: narrow gaussian across, soft ends, brighter toward one end
    across = np.exp(-(u / 0.10) ** 2)
    along = smoothstep(-1.0, -0.55, v) * (1.0 - smoothstep(0.55, 1.0, v))
    head = 0.55 + 0.45 * smoothstep(-0.2, 0.7, v)
    img = rgba_white(across * along * head)
    return img.rotate(90, expand=True) if horizontal else img


def splash():
    w = h = 64
    u, v = grid(w, h)
    r = np.sqrt(u * u + v * v)
    ring = np.exp(-((r - 0.55) / 0.10) ** 2) * (1.0 - smoothstep(0.75, 1.0, r))
    centre = np.exp(-(r / 0.16) ** 2) * 0.8
    rng = np.random.default_rng(7)
    drops = np.zeros_like(r)
    for _ in range(6):
        ang = rng.uniform(0, 2 * np.pi)
        rad = rng.uniform(0.25, 0.75)
        cx, cy = np.cos(ang) * rad, np.sin(ang) * rad
        drops = np.maximum(drops, np.exp(-(((u - cx) ** 2 + (v - cy) ** 2) / 0.012)) * 0.9)
    return rgba_white(np.maximum.reduce([ring * 0.8, centre, drops]))


def steam():
    w = h = 128
    u, v = grid(w, h)
    r = np.sqrt(u * u + v * v)
    rng = np.random.default_rng(11)
    noise = 0.55 * value_noise(w, h, 4, rng) + 0.30 * value_noise(w, h, 8, rng) + 0.15 * value_noise(w, h, 16, rng)
    base = 1.0 - smoothstep(0.15, 1.0, r)
    alpha = base * (0.35 + 0.9 * noise)
    alpha *= 1.0 - smoothstep(0.82, 1.0, r)  # zero at the border: no visible quad edge
    return rgba_white(np.clip(alpha, 0.0, 1.0) * 0.85)


def glow():
    w = h = 64
    u, v = grid(w, h)
    r = np.sqrt(u * u + v * v)
    alpha = np.exp(-(r / 0.38) ** 2) * (1.0 - smoothstep(0.85, 1.0, r))
    return rgba_white(alpha)


def trail():
    w, h = 16, 64
    _, v = grid(w, h)
    # Trail "Stretch" maps U (image x) along the length and V (image y) across the width: soft band across V.
    return rgba_white(np.exp(-(v / 0.55) ** 2) * (1.0 - smoothstep(0.85, 1.0, np.abs(v))))


def folder_meta(path):
    return ("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
            "  userData: \n  assetBundleName: \n  assetBundleVariant: \n") % guid_for(path)


TEXTURE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 1
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 128
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 128
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--raindrop-horizontal", action="store_true")
    args = parser.parse_args()

    os.makedirs(OUT_DIR, exist_ok=True)
    folder_meta_path = OUT_DIR + ".meta"
    if not os.path.exists(folder_meta_path):
        with open(folder_meta_path, "w", newline="\n") as f:
            f.write(folder_meta(OUT_DIR))
        print("wrote", folder_meta_path)

    outputs = {
        "T_FX_Raindrop.png": raindrop(args.raindrop_horizontal),
        "T_FX_Splash.png": splash(),
        "T_FX_Steam.png": steam(),
        "T_FX_Glow.png": glow(),
        "T_FX_Trail.png": trail(),
    }
    for name, image in outputs.items():
        path = os.path.join(OUT_DIR, name)
        image.save(path)
        meta_path = path + ".meta"
        if not os.path.exists(meta_path):
            with open(meta_path, "w", newline="\n") as f:
                f.write(TEXTURE_META.format(guid=guid_for(path)))
        print("wrote", path, image.size)


if __name__ == "__main__":
    main()
