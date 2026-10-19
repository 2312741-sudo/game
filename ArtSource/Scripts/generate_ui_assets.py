#!/usr/bin/env python3
"""
ArtSource/Scripts/generate_ui_assets.py

Generates brand UI assets for Project Trạm Chanh (ART-FIX-028):
1. UI Icons (24 icons): 256x256 PNG with transparent backgrounds.
   Brand Identity:
   - Deep Tea Green background: #132B1C
   - Brand Orange border: #E67E22
   - Lime Gold accent: #F1C40F
   Categories:
   - Ingredients (4): IC_Ice, IC_Jelly, IC_Batter, IC_Wrap
   - Upgrades (5): IC_Up_BiggerIceBin, IC_Up_Pantry, IC_Up_StringLights, IC_Up_WholesaleDeal, IC_Up_GoldenMenuBoard
   - Status / Events (11): IC_Star_Full, IC_Star_Empty, IC_Money, IC_Patience, IC_Goal,
                           IC_Rain, IC_Hot, IC_PowerCut, IC_Festival, IC_Inspector, IC_Clear
   - Customers (4): IC_Cust_Student, IC_Cust_Family, IC_Cust_Office, IC_Cust_Regular
   Directory: Assets/TramChanh/Art/UI/Icons/

2. Title Background:
   - BG_Title.png (1920x1080): Night street scene of Trạm Chanh stall, warm glowing string lights,
     dark atmospheric sidewalk background, wet street reflections.
   Directory: Assets/TramChanh/Art/UI/Backgrounds/

3. Brand Logo:
   - LOGO_TramChanh.png: High-resolution logo with stylized Vietnamese typography 'TRẠM CHANH'
     (with correct diacritics: Ạ and H) and lime slice accent.
   Directory: Assets/TramChanh/Art/UI/Logo/

4. Unity .meta Files:
   Generates standard Unity Sprite 2D and UI (.meta) files for each asset:
   - textureType: 8 (Sprite 2D and UI)
   - spriteMode: 1 (Single)
   - generateMipMaps: 0 (Off)
   - isReadable: 0
   - compressionQuality: 100 (high quality, crisp alpha)
   And folder .meta files for UI directories.
"""

import os
import sys
import math
import uuid
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

# -----------------------------------------------------------------------------
# DIRECTORY PATHS
# -----------------------------------------------------------------------------
REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
UI_ROOT = os.path.join(REPO_ROOT, "Assets/TramChanh/Art/UI")
ICONS_DIR = os.path.join(UI_ROOT, "Icons")
BACKGROUNDS_DIR = os.path.join(UI_ROOT, "Backgrounds")
LOGO_DIR = os.path.join(UI_ROOT, "Logo")

# -----------------------------------------------------------------------------
# BRAND PALETTE
# -----------------------------------------------------------------------------
COLOR_DEEP_TEA_GREEN = (19, 43, 28, 255)       # #132B1C
COLOR_BRAND_ORANGE   = (230, 126, 34, 255)      # #E67E22
COLOR_LIME_GOLD      = (241, 196, 15, 255)      # #F1C40F
COLOR_LIME_GREEN     = (46, 204, 113, 255)      # #2ECC71
COLOR_DARK_GREEN     = (13, 28, 18, 255)        # #0D1C12
COLOR_WHITE          = (255, 255, 255, 255)
COLOR_CREAM          = (255, 250, 235, 255)
COLOR_GOLD_DARK      = (212, 172, 13, 255)
COLOR_AMBER          = (211, 84, 0, 255)
COLOR_ICE_BLUE       = (165, 232, 255, 255)
COLOR_ICE_CYAN       = (220, 248, 255, 255)
COLOR_RED            = (231, 76, 60, 255)

# -----------------------------------------------------------------------------
# DETERMINISTIC GUID GENERATION
# -----------------------------------------------------------------------------
def get_or_create_guid(asset_rel_path, meta_path):
    if os.path.exists(meta_path):
        try:
            with open(meta_path, "r", encoding="utf-8") as f:
                for line in f:
                    if line.startswith("guid:"):
                        return line.strip().split()[1]
        except Exception:
            pass
    # Deterministic GUID derived from relative path
    return uuid.uuid5(uuid.NAMESPACE_URL, f"tramchanh://{asset_rel_path}").hex

# -----------------------------------------------------------------------------
# UNITY .META TEMPLATES
# -----------------------------------------------------------------------------
def write_folder_meta(folder_path):
    meta_path = folder_path + ".meta"
    rel_path = os.path.relpath(folder_path, REPO_ROOT).replace("\\", "/")
    guid = get_or_create_guid(rel_path, meta_path)
    content = f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(content)
    return meta_path

def write_sprite_meta(asset_path, max_texture_size=2048):
    meta_path = asset_path + ".meta"
    rel_path = os.path.relpath(asset_path, REPO_ROOT).replace("\\", "/")
    guid = get_or_create_guid(rel_path, meta_path)
    sprite_guid = uuid.uuid5(uuid.NAMESPACE_URL, f"tramchanh-sprite://{rel_path}").hex
    
    content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
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
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {max_texture_size}
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
  compressionQuality: 100
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: 8
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
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {max_texture_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: {max_texture_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: WebGL
    maxTextureSize: {max_texture_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 100
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
    spriteID: {sprite_guid}
    internalID: 21300000
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(content)
    return meta_path

# -----------------------------------------------------------------------------
# FONT LOADER
# -----------------------------------------------------------------------------
def get_vietnamese_font(size, bold=True):
    candidates = [
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf" if bold else "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Supplemental/Trebuchet MS Bold.ttf" if bold else "/System/Library/Fonts/Supplemental/Trebuchet MS.ttf",
        "/Library/Fonts/Arial Unicode.ttf",
        "/System/Library/Fonts/Helvetica.ttc",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/truetype/freefont/FreeSansBold.ttf" if bold else "/usr/share/fonts/truetype/freefont/FreeSans.ttf",
    ]
    for c in candidates:
        if os.path.exists(c):
            try:
                return ImageFont.truetype(c, size)
            except Exception:
                continue
    return ImageFont.load_default()

# -----------------------------------------------------------------------------
# DRAWING HELPERS (HIGH-RES 1024x1024 SUPERSAMPLING)
# -----------------------------------------------------------------------------
def create_icon_canvas():
    """Create a 1024x1024 RGBA canvas with the standard Trạm Chanh brand frame."""
    canvas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)
    
    # Drop shadow
    shadow_box = [68, 80, 956, 968]
    draw.rounded_rectangle(shadow_box, radius=180, fill=(0, 0, 0, 90))
    
    # Outer base badge: Deep Tea Green
    badge_box = [64, 64, 960, 960]
    draw.rounded_rectangle(badge_box, radius=180, fill=COLOR_DEEP_TEA_GREEN)
    
    # Subtle inner gradient highlight
    highlight_box = [88, 88, 936, 520]
    draw.rounded_rectangle(highlight_box, radius=160, fill=(28, 64, 42, 100))
    
    # Brand Orange Outer Border
    draw.rounded_rectangle(badge_box, radius=180, outline=COLOR_BRAND_ORANGE, width=28)
    
    # Lime Gold Inner Pin-stripe
    inner_box = [112, 112, 912, 912]
    draw.rounded_rectangle(inner_box, radius=150, outline=COLOR_LIME_GOLD, width=8)
    
    # Corner Gold Rivet/Bead Accents
    rivet_radius = 12
    rivets = [(170, 170), (854, 170), (170, 854), (854, 854)]
    for rx, ry in rivets:
        draw.ellipse([rx - rivet_radius, ry - rivet_radius, rx + rivet_radius, ry + rivet_radius],
                     fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=3)
        draw.ellipse([rx - 4, ry - 4, rx + 4, ry + 4], fill=COLOR_WHITE)
        
    return canvas, draw

def finalize_icon(canvas):
    """Downsample from 1024x1024 to 256x256 with high-quality Lanczos resampling."""
    return canvas.resize((256, 256), Image.Resampling.LANCZOS)

def draw_star(draw, cx, cy, r_out, r_in, fill, outline=None, width=1, rotation=-math.pi/2):
    """Draw a 5-pointed star."""
    pts = []
    for i in range(10):
        angle = rotation + i * (math.pi / 5)
        r = r_out if i % 2 == 0 else r_in
        pts.append((cx + r * math.cos(angle), cy + r * math.sin(angle)))
    draw.polygon(pts, fill=fill, outline=outline, width=width)
    return pts

def draw_sparkle(draw, cx, cy, radius, fill=COLOR_WHITE):
    """Draw a 4-point diamond sparkle glint."""
    pts = [
        (cx, cy - radius),
        (cx + radius * 0.25, cy - radius * 0.25),
        (cx + radius, cy),
        (cx + radius * 0.25, cy + radius * 0.25),
        (cx, cy + radius),
        (cx - radius * 0.25, cy + radius * 0.25),
        (cx - radius, cy),
        (cx - radius * 0.25, cy - radius * 0.25),
    ]
    draw.polygon(pts, fill=fill)

# -----------------------------------------------------------------------------
# ICON GENERATORS (24 ICONS)
# -----------------------------------------------------------------------------

# (1) IC_Ice.png
def gen_ic_ice():
    im, d = create_icon_canvas()
    
    def draw_iso_cube(ox, oy, size, col_top, col_left, col_right):
        w = size
        h = size * 0.58
        d_val = size * 0.85
        # Top
        top = [(ox, oy - h), (ox + w, oy), (ox, oy + h), (ox - w, oy)]
        d.polygon(top, fill=col_top, outline=(255, 255, 255, 180), width=4)
        # Left
        left = [(ox - w, oy), (ox, oy + h), (ox, oy + h + d_val), (ox - w, oy + d_val)]
        d.polygon(left, fill=col_left, outline=(180, 230, 255, 140), width=3)
        # Right
        right = [(ox, oy + h), (ox + w, oy), (ox + w, oy + d_val), (ox, oy + h + d_val)]
        d.polygon(right, fill=col_right, outline=(150, 210, 250, 140), width=3)
        # Top highlight line
        d.line([(ox, oy - h), (ox + w, oy)], fill=COLOR_WHITE, width=6)
        d.line([(ox, oy - h), (ox - w, oy)], fill=COLOR_WHITE, width=6)

    # Main ice block
    draw_iso_cube(512, 540, 170, (230, 250, 255, 240), (145, 215, 245, 240), (85, 175, 225, 240))
    # Secondary ice cube upper-right
    draw_iso_cube(630, 420, 120, (240, 252, 255, 235), (165, 225, 250, 235), (95, 185, 230, 235))
    # Third small ice cube lower-left
    draw_iso_cube(370, 620, 100, (245, 253, 255, 230), (150, 220, 248, 230), (80, 170, 220, 230))
    
    # Sparkle glints
    draw_sparkle(d, 512, 430, 36, COLOR_WHITE)
    draw_sparkle(d, 680, 340, 26, COLOR_LIME_GOLD)
    draw_sparkle(d, 330, 560, 24, COLOR_WHITE)
    draw_sparkle(d, 430, 710, 18, COLOR_ICE_CYAN)
    
    return finalize_icon(im)

# (2) IC_Jelly.png
def gen_ic_jelly():
    im, d = create_icon_canvas()
    
    # Cut grass jelly (thạch đen) block
    # Main cube (isometric)
    ox, oy = 500, 520
    w = 190
    h = 100
    depth = 220
    
    top = [(ox, oy - h), (ox + w, oy), (ox, oy + h), (ox - w, oy)]
    left = [(ox - w, oy), (ox, oy + h), (ox, oy + h + depth), (ox - w, oy + depth)]
    right = [(ox, oy + h), (ox + w, oy), (ox + w, oy + depth), (ox, oy + h + depth)]
    
    d.polygon(left, fill=(20, 30, 24, 255), outline=(40, 55, 45, 255), width=4)
    d.polygon(right, fill=(12, 20, 15, 255), outline=(30, 45, 35, 255), width=4)
    d.polygon(top, fill=(35, 52, 40, 255), outline=(60, 85, 65, 255), width=4)
    
    # Glossy specular reflection stripes across top
    d.polygon([(ox - 90, oy - 20), (ox - 40, oy - 45), (ox + 50, oy + 5), (ox, oy + 30)], fill=(75, 125, 90, 160))
    d.line([(ox - 30, oy - 40), (ox + 70, oy + 12)], fill=(200, 255, 220, 200), width=6)
    
    # Fresh lime slice garnish leaning on the jelly
    lx, ly, lr = 350, 650, 100
    d.ellipse([lx - lr, ly - lr, lx + lr, ly + lr], fill=(39, 174, 96, 255), outline=COLOR_LIME_GOLD, width=6)
    d.ellipse([lx - lr + 14, ly - lr + 14, lx + lr - 14, ly + lr - 14], fill=(235, 250, 235, 255))
    d.ellipse([lx - lr + 24, ly - lr + 24, lx + lr - 24, ly + lr - 24], fill=(241, 196, 15, 255))
    # Segment spokes
    for a in range(0, 360, 45):
        rad = math.radians(a)
        d.line([(lx, ly), (lx + (lr - 26) * math.cos(rad), ly + (lr - 26) * math.sin(rad))], fill=(255, 255, 255, 220), width=4)
    d.ellipse([lx - 12, ly - 12, lx + 12, ly + 12], fill=COLOR_WHITE)
    
    # Mint leaves on top corner
    d.ellipse([ox + 40, oy - h - 30, ox + 150, oy - h + 30], fill=COLOR_LIME_GREEN, outline=(19, 141, 62, 255), width=4)
    d.line([(ox + 40, oy - h), (ox + 150, oy - h)], fill=COLOR_LIME_GOLD, width=3)
    
    # Sparkling dew drops
    draw_sparkle(d, ox + 130, oy - 30, 24, COLOR_WHITE)
    draw_sparkle(d, 310, 580, 20, COLOR_WHITE)
    
    return finalize_icon(im)

# (3) IC_Batter.png
def gen_ic_batter():
    im, d = create_icon_canvas()
    
    # Ceramic mixing bowl
    bx, by = 512, 600
    bw, bh = 280, 200
    
    # Bowl body
    d.pieslice([bx - bw, by - bh, bx + bw, by + bh], 0, 180, fill=COLOR_CREAM, outline=COLOR_BRAND_ORANGE, width=14)
    
    # Bowl top rim
    rim_h = 70
    d.ellipse([bx - bw, by - rim_h, bx + bw, by + rim_h], fill=COLOR_CREAM, outline=COLOR_BRAND_ORANGE, width=14)
    # Smooth golden batter pool inside
    d.ellipse([bx - bw + 20, by - rim_h + 16, bx + bw - 20, by + rim_h - 12], fill=(249, 219, 109, 255))
    d.ellipse([bx - bw + 60, by - rim_h + 30, bx + bw - 60, by + rim_h - 26], fill=COLOR_LIME_GOLD)
    
    # Batter swirl ripples
    d.arc([bx - 120, by - 40, bx + 120, by + 20], 30, 150, fill=COLOR_BRAND_ORANGE, width=6)
    
    # Stainless wire whisk dipped into batter at an angle
    wx, wy = 560, 480
    # Handle
    d.line([(wx + 160, wy - 220), (wx + 60, wy - 80)], fill=(160, 82, 45, 255), width=24)
    d.line([(wx + 60, wy - 80), (wx + 45, wy - 60)], fill=(200, 200, 200, 255), width=28)
    # Whisk wire loops
    for angle_offset in [-30, -15, 0, 15, 30]:
        rad = math.radians(135 + angle_offset)
        end_x = wx + 120 * math.cos(rad)
        end_y = wy + 120 * math.sin(rad)
        d.arc([wx - 100, wy - 60, wx + 60, wy + 100], 60 + angle_offset, 240 + angle_offset,
              fill=(220, 225, 230, 255), width=6)
        
    # Batter ribbon stream pouring from above
    d.line([(420, 220), (460, 360), (480, 520)], fill=(249, 219, 109, 255), width=26)
    d.line([(420, 220), (460, 360), (480, 520)], fill=COLOR_WHITE, width=8)
    # Batter splash drops
    d.ellipse([450, 480, 470, 500], fill=COLOR_LIME_GOLD)
    d.ellipse([510, 470, 530, 490], fill=COLOR_LIME_GOLD)
    
    # Highlight sparkles
    draw_sparkle(d, 410, 260, 22, COLOR_WHITE)
    draw_sparkle(d, 680, 620, 24, COLOR_WHITE)
    
    return finalize_icon(im)

# (4) IC_Wrap.png
def gen_ic_wrap():
    im, d = create_icon_canvas()
    
    # Rolled cylinder of kraft wrapping paper with sheet unrolling forward
    cx, cy = 512, 530
    
    # Unrolled sheet curling forward
    sheet_pts = [
        (320, 510), (680, 450), (740, 680), (680, 720),
        (380, 750), (300, 700), (320, 510)
    ]
    d.polygon(sheet_pts, fill=(245, 225, 195, 255), outline=COLOR_BRAND_ORANGE, width=8)
    
    # Checked / stamped paper watermark pattern
    for i in range(4):
        y_off = 560 + i * 40
        d.line([(340 + i * 15, y_off), (700, y_off - 40)], fill=(230, 200, 165, 255), width=4)
        
    # Lime brand stamp on wrapping paper
    sx, sy, sr = 520, 620, 50
    d.ellipse([sx - sr, sy - sr, sx + sr, sy + sr], outline=COLOR_DEEP_TEA_GREEN, width=5)
    d.ellipse([sx - sr + 10, sy - sr + 10, sx + sr - 10, sy + sr - 10], outline=COLOR_BRAND_ORANGE, width=3)
    d.line([(sx - 35, sy), (sx + 35, sy)], fill=COLOR_DEEP_TEA_GREEN, width=3)
    d.line([(sx, sy - 35), (sx, sy + 35)], fill=COLOR_DEEP_TEA_GREEN, width=3)
    
    # Rolled cylinder body
    roll_box = [280, 400, 720, 510]
    d.rounded_rectangle(roll_box, radius=50, fill=(215, 165, 115, 255), outline=COLOR_BRAND_ORANGE, width=8)
    # Roll coil spiral on left end
    d.ellipse([270, 400, 360, 510], fill=(235, 195, 150, 255), outline=COLOR_BRAND_ORANGE, width=8)
    d.ellipse([290, 425, 340, 485], fill=(185, 135, 85, 255), outline=COLOR_BRAND_ORANGE, width=6)
    d.ellipse([305, 445, 325, 465], fill=(120, 75, 35, 255))
    
    # Rustic twine string tied around roll with bow
    d.line([(480, 395), (480, 515)], fill=COLOR_LIME_GOLD, width=12)
    # Bow loops
    d.ellipse([430, 360, 485, 410], outline=COLOR_LIME_GOLD, width=8)
    d.ellipse([475, 360, 530, 410], outline=COLOR_LIME_GOLD, width=8)
    d.ellipse([470, 385, 490, 405], fill=COLOR_LIME_GOLD)
    
    draw_sparkle(d, 680, 420, 26, COLOR_WHITE)
    draw_sparkle(d, 360, 730, 20, COLOR_WHITE)
    
    return finalize_icon(im)

# (5) IC_Up_BiggerIceBin.png
def gen_ic_up_bigger_ice_bin():
    im, d = create_icon_canvas()
    
    # Stainless insulated commercial ice chest / cooler
    cx, cy = 512, 540
    
    # Cooler body
    box = [260, 460, 764, 760]
    d.rounded_rectangle(box, radius=30, fill=(215, 225, 235, 255), outline=COLOR_BRAND_ORANGE, width=12)
    
    # Horizontal stainless ribs
    for ry in [530, 610, 690]:
        d.line([(280, ry), (744, ry)], fill=(175, 190, 205, 255), width=8)
        d.line([(280, ry - 3), (744, ry - 3)], fill=COLOR_WHITE, width=4)
        
    # Open lid propped up at an angle
    lid_pts = [(230, 440), (794, 440), (750, 310), (274, 310)]
    d.polygon(lid_pts, fill=(190, 205, 220, 255), outline=COLOR_BRAND_ORANGE, width=12)
    # Lid inner insulation gasket
    d.polygon([(280, 425), (744, 425), (710, 335), (314, 335)], fill=(240, 245, 250, 255))
    
    # Mountain of sparkling ice cubes inside
    for ix, iy, ir in [(400, 410, 45), (512, 380, 55), (620, 410, 45), (460, 430, 50), (560, 430, 50)]:
        d.rounded_rectangle([ix - ir, iy - ir, ix + ir, iy + ir], radius=12,
                            fill=COLOR_ICE_CYAN, outline=COLOR_ICE_BLUE, width=4)
        d.line([(ix - ir + 8, iy - ir + 8), (ix + ir - 8, iy - ir + 8)], fill=COLOR_WHITE, width=4)
        
    # Golden snowflake / ice emblem on front
    draw_star(d, 512, 610, 45, 22, COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=4)
    # Heavy duty steel side handles
    d.rounded_rectangle([220, 550, 260, 630], radius=15, fill=(160, 175, 190, 255), outline=COLOR_BRAND_ORANGE, width=8)
    d.rounded_rectangle([764, 550, 804, 630], radius=15, fill=(160, 175, 190, 255), outline=COLOR_BRAND_ORANGE, width=8)
    
    # Frost sparkles
    draw_sparkle(d, 512, 310, 32, COLOR_WHITE)
    draw_sparkle(d, 350, 370, 22, COLOR_WHITE)
    draw_sparkle(d, 680, 360, 24, COLOR_WHITE)
    
    return finalize_icon(im)

# (6) IC_Up_Pantry.png
def gen_ic_up_pantry():
    im, d = create_icon_canvas()
    
    # 3-tier wooden and metal pantry shelving
    left_x = 280
    right_x = 744
    
    # Upright support beams
    d.rounded_rectangle([left_x, 260, left_x + 36, 780], radius=8, fill=(130, 75, 35, 255), outline=COLOR_BRAND_ORANGE, width=6)
    d.rounded_rectangle([right_x - 36, 260, right_x, 780], radius=8, fill=(130, 75, 35, 255), outline=COLOR_BRAND_ORANGE, width=6)
    
    # Shelves (Top, Middle, Bottom)
    shelves_y = [390, 560, 730]
    for sy in shelves_y:
        d.rounded_rectangle([left_x - 20, sy, right_x + 20, sy + 28], radius=6, fill=(180, 115, 60, 255), outline=COLOR_BRAND_ORANGE, width=6)
        d.line([(left_x - 16, sy + 4), (right_x + 16, sy + 4)], fill=(225, 165, 110, 255), width=4)
        
    # Top shelf items: Glass ingredient jars with golden tea leaves and red syrups
    jars_x = [370, 512, 650]
    jar_colors = [(46, 204, 113, 220), (241, 196, 15, 220), (231, 76, 60, 220)]
    for jx, col in zip(jars_x, jar_colors):
        # Jar body
        d.rounded_rectangle([jx - 36, 295, jx + 36, 390], radius=12, fill=col, outline=COLOR_CREAM, width=5)
        # Metallic lid
        d.rounded_rectangle([jx - 40, 280, jx + 40, 300], radius=6, fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=4)
        # Specular shine
        d.line([(jx - 24, 310), (jx - 24, 375)], fill=COLOR_WHITE, width=4)
        
    # Middle shelf items: 2 Vietnamese tea crates branded with green leaf
    for cx in [410, 610]:
        d.rounded_rectangle([cx - 75, 440, cx + 75, 560], radius=8, fill=(210, 150, 95, 255), outline=COLOR_BRAND_ORANGE, width=6)
        # Diagonal slat
        d.line([(cx - 70, 445), (cx + 70, 555)], fill=(160, 100, 55, 255), width=6)
        # Leaf stencil
        d.ellipse([cx - 20, 485, cx + 20, 515], fill=COLOR_LIME_GREEN)
        
    # Bottom shelf items: Woven basket of green limes
    bx = 512
    d.pieslice([bx - 120, 630, bx + 120, 750], 0, 180, fill=(165, 110, 60, 255), outline=COLOR_BRAND_ORANGE, width=8)
    for lx in [440, 485, 535, 580]:
        d.ellipse([lx - 26, 640, lx + 26, 692], fill=COLOR_LIME_GREEN, outline=(19, 141, 62, 255), width=3)
        
    draw_sparkle(d, 370, 275, 20, COLOR_WHITE)
    draw_sparkle(d, 680, 435, 22, COLOR_LIME_GOLD)
    
    return finalize_icon(im)

# (7) IC_Up_StringLights.png
def gen_ic_up_string_lights():
    im, d = create_icon_canvas()
    
    cx, cy = 512, 540
    
    # Twisted hanging wire
    d.line([(512, 160), (512, 340)], fill=(40, 40, 40, 255), width=16)
    d.line([(506, 160), (518, 340)], fill=(70, 70, 70, 255), width=6)
    
    # Brass socket fixture
    d.rounded_rectangle([470, 340, 554, 400], radius=10, fill=COLOR_BRAND_ORANGE, outline=COLOR_LIME_GOLD, width=6)
    d.rounded_rectangle([485, 400, 539, 425], radius=6, fill=COLOR_LIME_GOLD)
    
    # Warm glowing radiant halos (concentric bloom)
    for r, alpha in [(320, 25), (250, 45), (180, 80), (120, 130)]:
        d.ellipse([cx - r, cy - r + 30, cx + r, cy + r + 30], fill=(241, 196, 15, alpha))
        
    # Clear Edison glass teardrop bulb outline
    d.pieslice([cx - 150, cy - 130, cx + 150, cy + 170], 0, 180, fill=(255, 255, 255, 60), outline=COLOR_CREAM, width=8)
    d.polygon([(cx - 150, cy + 20), (cx + 150, cy + 20), (539, 425), (485, 425)], fill=(255, 255, 255, 40))
    d.arc([cx - 150, cy - 130, cx + 150, cy + 170], 0, 360, fill=COLOR_WHITE, width=8)
    
    # Zigzag glowing tungsten filament
    pts = [
        (512, 425), (490, 490), (534, 520), (490, 560), (534, 600), (512, 640)
    ]
    for i in range(len(pts) - 1):
        d.line([pts[i], pts[i+1]], fill=(255, 255, 220, 255), width=12)
        d.line([pts[i], pts[i+1]], fill=COLOR_WHITE, width=6)
        
    # Radiating golden star rays
    for angle in range(0, 360, 45):
        rad = math.radians(angle)
        r1, r2 = 180, 270
        d.line([(cx + r1 * math.cos(rad), cy + 30 + r1 * math.sin(rad)),
                (cx + r2 * math.cos(rad), cy + 30 + r2 * math.sin(rad))],
               fill=COLOR_LIME_GOLD, width=6)
        
    draw_sparkle(d, cx - 110, cy - 60, 30, COLOR_WHITE)
    draw_sparkle(d, cx + 130, cy + 90, 26, COLOR_WHITE)
    
    return finalize_icon(im)

# (8) IC_Up_WholesaleDeal.png
def gen_ic_up_wholesale_deal():
    im, d = create_icon_canvas()
    
    cx, cy = 512, 540
    
    # Wooden wholesale cargo crate in background
    box = [260, 350, 764, 760]
    d.rounded_rectangle(box, radius=24, fill=(195, 135, 75, 255), outline=COLOR_BRAND_ORANGE, width=12)
    # Diagonal slats
    d.line([(270, 360), (754, 750)], fill=(155, 95, 45, 255), width=16)
    d.line([(754, 360), (270, 750)], fill=(155, 95, 45, 255), width=16)
    # Steel corner brackets
    for bx, by in [(260, 350), (724, 350), (260, 720), (724, 720)]:
        d.rectangle([bx, by, bx + 40, by + 40], fill=(120, 130, 140, 255), outline=COLOR_BRAND_ORANGE, width=4)
        
    # Round golden discount badge: "%"
    badge_x, badge_y, badge_r = 650, 340, 85
    d.ellipse([badge_x - badge_r, badge_y - badge_r, badge_x + badge_r, badge_y + badge_r],
              fill=COLOR_BRAND_ORANGE, outline=COLOR_LIME_GOLD, width=10)
    font_pct = get_vietnamese_font(90, bold=True)
    d.text((badge_x - 36, badge_y - 56), "%", font=font_pct, fill=COLOR_WHITE)
    
    # Firm golden handshake in foreground
    # Left hand / sleeve
    d.polygon([(240, 520), (380, 520), (460, 580), (380, 640), (240, 640)],
              fill=COLOR_BRAND_ORANGE, outline=COLOR_LIME_GOLD, width=6)
    # Right hand / sleeve
    d.polygon([(784, 520), (644, 520), (564, 580), (644, 640), (784, 640)],
              fill=(41, 128, 185, 255), outline=COLOR_CREAM, width=6)
    # Clasped hands (gold)
    d.ellipse([430, 520, 594, 640], fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=8)
    # Fingers interlaced
    for fy in [545, 575, 605]:
        d.line([(470, fy), (550, fy)], fill=COLOR_BRAND_ORANGE, width=6)
        
    # Spilling gold coins
    for coin_x in [330, 390, 450]:
        d.ellipse([coin_x, 720, coin_x + 70, 760], fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=5)
        
    draw_sparkle(d, 512, 480, 28, COLOR_WHITE)
    draw_sparkle(d, 650, 250, 24, COLOR_WHITE)
    
    return finalize_icon(im)

# (9) IC_Up_GoldenMenuBoard.png
def gen_ic_up_golden_menu_board():
    im, d = create_icon_canvas()
    
    # Polished golden menu plaque
    cx, cy = 512, 540
    
    # Plaque body with ornate corners
    w, h = 480, 520
    box = [cx - w//2, cy - h//2, cx + w//2, cy + h//2]
    d.rounded_rectangle(box, radius=40, fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=16)
    
    # Inner border
    inner_box = [box[0] + 24, box[1] + 24, box[2] - 24, box[3] - 24]
    d.rounded_rectangle(inner_box, radius=24, fill=(255, 248, 220, 255), outline=COLOR_BRAND_ORANGE, width=6)
    
    # Baroque arched pediment on top
    d.pieslice([cx - 120, box[1] - 60, cx + 120, box[1] + 60], 180, 360,
               fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=12)
    draw_star(d, cx, box[1] - 15, 25, 12, COLOR_BRAND_ORANGE)
    
    # Menu header text
    font_menu = get_vietnamese_font(60, bold=True)
    d.text((cx - 85, box[1] + 45), "MENU", font=font_menu, fill=COLOR_DEEP_TEA_GREEN)
    d.line([(cx - 140, box[1] + 120), (cx + 140, box[1] + 120)], fill=COLOR_BRAND_ORANGE, width=6)
    
    # Simulated menu rows (drink line + price dots + price)
    for row_y in [box[1] + 160, box[1] + 225, box[1] + 290, box[1] + 355]:
        d.line([(box[0] + 60, row_y), (box[0] + 240, row_y)], fill=COLOR_DEEP_TEA_GREEN, width=8)
        # Price dots
        for dx in range(box[0] + 260, box[2] - 120, 20):
            d.ellipse([dx - 2, row_y - 2, dx + 2, row_y + 2], fill=COLOR_BRAND_ORANGE)
        # Price badge
        d.rounded_rectangle([box[2] - 110, row_y - 12, box[2] - 60, row_y + 12], radius=4, fill=COLOR_BRAND_ORANGE)
        
    # Rating stars at the bottom of menu
    for sx in range(cx - 100, cx + 120, 50):
        draw_star(d, sx, box[3] - 50, 18, 9, COLOR_BRAND_ORANGE)
        
    # Diagonal specular shine streak across plaque
    d.polygon([(box[0] + 60, box[1] + 24), (box[0] + 140, box[1] + 24),
               (box[2] - 24, box[3] - 140), (box[2] - 24, box[3] - 220)],
              fill=(255, 255, 255, 90))
    
    draw_sparkle(d, box[0] + 30, box[1] + 30, 28, COLOR_WHITE)
    draw_sparkle(d, box[2] - 30, box[1] + 30, 24, COLOR_WHITE)
    
    return finalize_icon(im)

# (10) IC_Star_Full.png
def gen_ic_star_full():
    im, d = create_icon_canvas()
    
    cx, cy = 512, 512
    r_out, r_in = 310, 135
    
    # Outer gold halo
    d.ellipse([cx - r_out - 20, cy - r_out - 20, cx + r_out + 20, cy + r_out + 20], fill=(241, 196, 15, 40))
    
    # 3D Faceted 5-point Star
    # Outer points
    pts = []
    for i in range(10):
        a = -math.pi/2 + i * (math.pi / 5)
        r = r_out if i % 2 == 0 else r_in
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
        
    # Draw facets: left bright lemon gold, right rich amber
    for i in range(5):
        tip = pts[i * 2]
        right_in = pts[(i * 2 + 1) % 10]
        left_in = pts[(i * 2 - 1) % 10]
        
        # Left facet (bright)
        d.polygon([(cx, cy), left_in, tip], fill=(255, 243, 115, 255), outline=COLOR_BRAND_ORANGE, width=3)
        # Right facet (amber shadow)
        d.polygon([(cx, cy), tip, right_in], fill=(217, 130, 20, 255), outline=COLOR_BRAND_ORANGE, width=3)
        
    # Outer border
    d.polygon(pts, outline=COLOR_BRAND_ORANGE, width=12)
    
    # Diamond glints
    draw_sparkle(d, cx + 180, cy - 180, 42, COLOR_WHITE)
    draw_sparkle(d, cx - 180, cy + 120, 28, COLOR_WHITE)
    draw_sparkle(d, cx, cy - r_out, 24, COLOR_WHITE)
    
    return finalize_icon(im)

# (11) IC_Star_Empty.png
def gen_ic_star_empty():
    im, d = create_icon_canvas()
    
    cx, cy = 512, 512
    r_out, r_in = 310, 135
    
    pts = []
    for i in range(10):
        a = -math.pi/2 + i * (math.pi / 5)
        r = r_out if i % 2 == 0 else r_in
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
        
    # Translucent inner star fill
    d.polygon(pts, fill=(19, 43, 28, 120))
    
    # Inner shadow line
    pts_in = []
    for i in range(10):
        a = -math.pi/2 + i * (math.pi / 5)
        r = (r_out - 30) if i % 2 == 0 else (r_in - 16)
        pts_in.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    d.polygon(pts_in, outline=(10, 25, 16, 180), width=8)
    
    # Thick golden star border
    d.polygon(pts, outline=COLOR_LIME_GOLD, width=28)
    d.polygon(pts, outline=COLOR_BRAND_ORANGE, width=8)
    
    # Accents at tips
    for i in range(5):
        tip = pts[i * 2]
        d.ellipse([tip[0] - 10, tip[1] - 10, tip[0] + 10, tip[1] + 10], fill=COLOR_WHITE)
        
    return finalize_icon(im)

# (12) IC_Money.png
def gen_ic_money():
    im, d = create_icon_canvas()
    
    # VND Banknotes and golden coin stack
    # Background banknotes (pastel jade green and soft orange)
    # Note 1 (angled left)
    note1 = [(260, 420), (520, 290), (600, 440), (340, 570)]
    d.polygon(note1, fill=(160, 220, 190, 255), outline=COLOR_BRAND_ORANGE, width=8)
    d.ellipse([410, 410, 470, 470], outline=(46, 139, 87, 255), width=6)
    
    # Note 2 (angled right)
    note2 = [(440, 320), (740, 360), (700, 530), (400, 490)]
    d.polygon(note2, fill=(245, 190, 150, 255), outline=COLOR_BRAND_ORANGE, width=8)
    d.ellipse([550, 400, 610, 460], outline=COLOR_BRAND_ORANGE, width=6)
    
    # Foreground stack of 5 gold coins
    coin_w, coin_h = 240, 75
    cx = 512
    base_y = 730
    for i in range(5):
        cy = base_y - i * 35
        # Milled edge
        d.ellipse([cx - coin_w//2, cy - coin_h//2 + 16, cx + coin_w//2, cy + coin_h//2 + 16],
                  fill=(180, 115, 15, 255), outline=COLOR_BRAND_ORANGE, width=6)
        # Coin face
        d.ellipse([cx - coin_w//2, cy - coin_h//2, cx + coin_w//2, cy + coin_h//2],
                  fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=6)
        
    # Top coin face detailing
    top_y = base_y - 4 * 35
    # Inner rim
    d.ellipse([cx - coin_w//2 + 20, top_y - coin_h//2 + 10, cx + coin_w//2 - 20, top_y + coin_h//2 - 10],
              outline=(255, 235, 130, 255), width=5)
    # Embossed "₫" symbol
    font_dong = get_vietnamese_font(65, bold=True)
    d.text((cx - 24, top_y - 42), "₫", font=font_dong, fill=COLOR_DEEP_TEA_GREEN)
    
    draw_sparkle(d, cx - 110, top_y - 20, 32, COLOR_WHITE)
    draw_sparkle(d, cx + 110, top_y - 20, 26, COLOR_WHITE)
    draw_sparkle(d, 680, 350, 24, COLOR_WHITE)
    
    return finalize_icon(im)

# (13) IC_Patience.png
def gen_ic_patience():
    im, d = create_icon_canvas()
    
    # Wooden-framed glass hourglass
    cx, cy = 512, 530
    w, h = 300, 480
    
    # Top and bottom turned bases
    base_h = 36
    # Top base
    d.rounded_rectangle([cx - w//2, cy - h//2, cx + w//2, cy - h//2 + base_h],
                        radius=12, fill=(130, 75, 35, 255), outline=COLOR_BRAND_ORANGE, width=8)
    d.rounded_rectangle([cx - w//2 + 20, cy - h//2 + base_h, cx + w//2 - 20, cy - h//2 + base_h + 16],
                        radius=8, fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=6)
    # Bottom base
    d.rounded_rectangle([cx - w//2, cy + h//2 - base_h, cx + w//2, cy + h//2],
                        radius=12, fill=(130, 75, 35, 255), outline=COLOR_BRAND_ORANGE, width=8)
    d.rounded_rectangle([cx - w//2 + 20, cy + h//2 - base_h - 16, cx + w//2 - 20, cy + h//2 - base_h],
                        radius=8, fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=6)
    
    # Brass side pillars
    for px in [cx - w//2 + 24, cx + w//2 - 24]:
        d.line([(px, cy - h//2 + base_h + 16), (px, cy + h//2 - base_h - 16)], fill=COLOR_LIME_GOLD, width=14)
        d.line([(px, cy - h//2 + base_h + 16), (px, cy + h//2 - base_h - 16)], fill=COLOR_BRAND_ORANGE, width=4)
        
    # Glass hourglass bulbs
    glass_top = [(cx - 100, cy - h//2 + 55), (cx + 100, cy - h//2 + 55), (cx + 25, cy), (cx - 25, cy)]
    d.polygon(glass_top, fill=(210, 240, 255, 100), outline=COLOR_CREAM, width=8)
    glass_bot = [(cx - 25, cy), (cx + 25, cy), (cx + 100, cy + h//2 - 55), (cx - 100, cy + h//2 - 55)]
    d.polygon(glass_bot, fill=(210, 240, 255, 100), outline=COLOR_CREAM, width=8)
    
    # Golden sand inside
    # Upper bulb sand (half empty)
    d.polygon([(cx - 75, cy - 60), (cx + 75, cy - 60), (cx + 15, cy), (cx - 15, cy)], fill=COLOR_LIME_GOLD)
    # Center falling sand stream
    d.line([(cx, cy), (cx, cy + 90)], fill=COLOR_LIME_GOLD, width=8)
    # Lower bulb sand mound
    d.polygon([(cx - 85, cy + h//2 - 58), (cx + 85, cy + h//2 - 58), (cx, cy + 80)], fill=COLOR_LIME_GOLD)
    
    # Specular shine streaks on glass
    d.line([(cx - 70, cy - h//2 + 75), (cx - 20, cy - 20)], fill=COLOR_WHITE, width=6)
    d.line([(cx + 70, cy + h//2 - 75), (cx + 20, cy + 20)], fill=COLOR_WHITE, width=6)
    
    draw_sparkle(d, cx - 80, cy - 130, 24, COLOR_WHITE)
    draw_sparkle(d, cx + 80, cy + 110, 24, COLOR_WHITE)
    
    return finalize_icon(im)

# (14) IC_Goal.png
def gen_ic_goal():
    im, d = create_icon_canvas()
    
    # Golden trophy cup
    cx, cy = 512, 510
    
    # Pedestal base
    d.rounded_rectangle([cx - 120, cy + 180, cx + 120, cy + 240], radius=12,
                        fill=(120, 65, 30, 255), outline=COLOR_BRAND_ORANGE, width=8)
    d.rounded_rectangle([cx - 80, cy + 140, cx + 80, cy + 180], radius=8,
                        fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=6)
    # Brass plaque on base
    d.rounded_rectangle([cx - 60, cy + 195, cx + 60, cy + 225], radius=4, fill=COLOR_LIME_GOLD)
    
    # Chalice stem
    d.polygon([(cx - 40, cy + 70), (cx + 40, cy + 70), (cx + 25, cy + 140), (cx - 25, cy + 140)],
              fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=6)
              
    # Curved trophy handles
    d.arc([cx - 260, cy - 140, cx - 60, cy + 40], 90, 270, fill=COLOR_LIME_GOLD, width=32)
    d.arc([cx - 260, cy - 140, cx - 60, cy + 40], 90, 270, fill=COLOR_BRAND_ORANGE, width=8)
    d.arc([cx + 60, cy - 140, cx + 260, cy + 40], 270, 90, fill=COLOR_LIME_GOLD, width=32)
    d.arc([cx + 60, cy - 140, cx + 260, cy + 40], 270, 90, fill=COLOR_BRAND_ORANGE, width=8)
    
    # Chalice body
    chalice = [
        (cx - 150, cy - 130), (cx + 150, cy - 130),
        (cx + 120, cy - 10), (cx + 40, cy + 70),
        (cx - 40, cy + 70), (cx - 120, cy - 10)
    ]
    d.polygon(chalice, fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=12)
    
    # Chalice rim
    d.ellipse([cx - 160, cy - 160, cx + 160, cy - 100], fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=10)
    d.ellipse([cx - 135, cy - 145, cx + 135, cy - 115], fill=(255, 240, 140, 255))
    
    # Star emblem embossed on cup
    draw_star(d, cx, cy - 30, 45, 22, COLOR_BRAND_ORANGE, outline=COLOR_WHITE, width=3)
    
    # Specular shine streaks
    d.polygon([(cx - 90, cy - 120), (cx - 40, cy - 120), (cx - 10, cy + 50), (cx - 30, cy + 50)],
              fill=(255, 255, 255, 120))
              
    draw_sparkle(d, cx - 140, cy - 130, 36, COLOR_WHITE)
    draw_sparkle(d, cx + 140, cy - 130, 30, COLOR_WHITE)
    
    return finalize_icon(im)

# (15) IC_Rain.png
def gen_ic_rain():
    im, d = create_icon_canvas()
    
    cx, cy = 512, 450
    
    # Billowing storm cloud in slate blue
    cloud_puffs = [
        (cx - 140, cy + 20, 110),
        (cx + 140, cy + 20, 110),
        (cx - 60, cy - 60, 130),
        (cx + 70, cy - 50, 120),
        (cx, cy + 20, 120)
    ]
    for px, py, pr in cloud_puffs:
        d.ellipse([px - pr, py - pr, px + pr, py + pr], fill=(52, 73, 94, 255))
    # Bottom flat fill
    d.rounded_rectangle([cx - 180, cy - 10, cx + 180, cy + 90], radius=40, fill=(52, 73, 94, 255))
    
    # Top puff silver highlights
    for px, py, pr in cloud_puffs[2:4]:
        d.arc([px - pr + 8, py - pr + 8, px + pr - 8, py + pr - 8], 200, 340, fill=(120, 144, 156, 255), width=8)
        
    # Falling raindrops (angled cyan drops)
    drops = [
        (360, 590), (440, 650), (512, 580), (590, 660), (660, 600),
        (400, 710), (480, 730), (560, 740), (630, 710)
    ]
    for dx, dy in drops:
        # Teardrop shape
        drop_pts = [(dx - 8, dy + 24), (dx + 8, dy + 24), (dx + 2, dy - 16)]
        d.polygon(drop_pts, fill=COLOR_ICE_CYAN, outline=(41, 128, 185, 255), width=2)
        d.ellipse([dx - 8, dy + 10, dx + 8, dy + 26], fill=COLOR_ICE_CYAN)
        
    # Small electric spark from cloud
    d.line([(520, 500), (505, 530), (525, 535), (510, 565)], fill=COLOR_LIME_GOLD, width=6)
    
    draw_sparkle(d, 360, 360, 24, COLOR_WHITE)
    draw_sparkle(d, 680, 390, 20, COLOR_WHITE)
    
    return finalize_icon(im)

# (16) IC_Hot.png
def gen_ic_hot():
    im, d = create_icon_canvas()
    
    cx, cy = 460, 512
    
    # Blazing sun with swirling flame rays
    sun_r = 170
    # Flame rays
    for i in range(12):
        angle = i * (2 * math.pi / 12)
        ray_pts = [
            (cx + sun_r * math.cos(angle - 0.2), cy + sun_r * math.sin(angle - 0.2)),
            (cx + (sun_r + 90) * math.cos(angle + 0.15), cy + (sun_r + 90) * math.sin(angle + 0.15)),
            (cx + sun_r * math.cos(angle + 0.2), cy + sun_r * math.sin(angle + 0.2)),
        ]
        d.polygon(ray_pts, fill=(230, 126, 34, 255), outline=COLOR_RED, width=3)
        
    # Sun core
    d.ellipse([cx - sun_r, cy - sun_r, cx + sun_r, cy + sun_r],
              fill=(243, 156, 18, 255), outline=COLOR_RED, width=8)
    d.ellipse([cx - sun_r + 24, cy - sun_r + 24, cx + sun_r - 24, cy + sun_r - 24],
              fill=COLOR_LIME_GOLD)
              
    # Heat thermometer in foreground (right side)
    tx, ty = 660, 512
    tw, th = 70, 420
    
    # Thermometer glass stem
    d.rounded_rectangle([tx - tw//2, ty - th//2, tx + tw//2, ty + th//2 - 40],
                        radius=24, fill=(255, 255, 255, 230), outline=COLOR_BRAND_ORANGE, width=8)
    # Bottom bulb
    bulb_r = 65
    bulb_y = ty + th//2 - 30
    d.ellipse([tx - bulb_r, bulb_y - bulb_r, tx + bulb_r, bulb_y + bulb_r],
              fill=COLOR_RED, outline=COLOR_BRAND_ORANGE, width=8)
              
    # Red mercury column maxed out to top
    d.rounded_rectangle([tx - 16, ty - th//2 + 20, tx + 16, bulb_y],
                        radius=10, fill=COLOR_RED)
                        
    # Degree tick marks
    for my in range(int(ty - th//2 + 40), int(bulb_y - 40), 35):
        d.line([(tx + 18, my), (tx + 30, my)], fill=COLOR_BRAND_ORANGE, width=4)
        
    draw_sparkle(d, cx - 80, cy - 80, 32, COLOR_WHITE)
    draw_sparkle(d, tx, ty - th//2 - 10, 24, COLOR_WHITE)
    
    return finalize_icon(im)

# (17) IC_PowerCut.png
def gen_ic_power_cut():
    im, d = create_icon_canvas()
    
    cx, cy = 512, 512
    
    # Dark unlit light bulb silhouette
    # Base socket
    d.rounded_rectangle([470, 310, 554, 370], radius=8, fill=(80, 95, 110, 255), outline=COLOR_BRAND_ORANGE, width=6)
    
    # Bulb glass
    d.arc([cx - 160, cy - 130, cx + 160, cy + 190], 0, 360, fill=(45, 60, 75, 255), width=16)
    d.ellipse([cx - 150, cy - 120, cx + 150, cy + 180], fill=(30, 42, 54, 255), outline=(70, 85, 100, 255), width=8)
    
    # Broken severed filament inside
    d.line([(480, 430), (500, 480)], fill=(90, 105, 120, 255), width=8)
    d.line([(544, 430), (524, 480)], fill=(90, 105, 120, 255), width=8)
    # Gap between ends
    
    # Bold electric zigzag lightning bolt slashing through
    bolt_pts = [
        (560, 260), (460, 460), (530, 470), (420, 680),
        (470, 690), (440, 760), (570, 520), (500, 510), (600, 320)
    ]
    d.polygon(bolt_pts, fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=6)
    
    # Bold red barrier / slash across
    d.line([(310, 310), (714, 714)], fill=COLOR_RED, width=28)
    d.line([(310, 310), (714, 714)], fill=COLOR_WHITE, width=8)
    
    # Sparks snapping at break point
    draw_sparkle(d, 470, 470, 28, COLOR_LIME_GOLD)
    draw_sparkle(d, 550, 520, 24, COLOR_WHITE)
    
    return finalize_icon(im)

# (18) IC_Festival.png
def gen_ic_festival():
    im, d = create_icon_canvas()
    
    cx, cy = 512, 480
    
    # Traditional Vietnamese Hội An silk lantern
    # Top wooden cap & suspension ring
    d.ellipse([cx - 30, cy - 240, cx + 30, cy - 180], outline=COLOR_LIME_GOLD, width=8)
    d.rounded_rectangle([cx - 80, cy - 180, cx + 80, cy - 140], radius=8, fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=6)
    
    # Diamond / ovoid silk lantern body
    lantern_pts = [
        (cx, cy - 140), (cx + 170, cy), (cx, cy + 140), (cx - 170, cy)
    ]
    d.polygon(lantern_pts, fill=(192, 57, 43, 255), outline=COLOR_BRAND_ORANGE, width=12)
    
    # Curved vertical golden rib lines
    for rx in [-110, -55, 55, 110]:
        d.arc([cx + rx - 40, cy - 140, cx + rx + 40, cy + 140], 90, 270 if rx < 0 else 90,
              fill=COLOR_LIME_GOLD, width=6)
    d.line([(cx, cy - 140), (cx, cy + 140)], fill=COLOR_LIME_GOLD, width=8)
    
    # Horizontal central festive band
    d.polygon([(cx - 170, cy), (cx, cy - 30), (cx + 170, cy), (cx, cy + 30)],
              fill=COLOR_BRAND_ORANGE, outline=COLOR_LIME_GOLD, width=4)
              
    # Bottom collar
    d.rounded_rectangle([cx - 70, cy + 140, cx + 70, cy + 170], radius=8, fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=6)
    
    # Cascading golden silk tassels
    tassel_y1 = cy + 170
    tassel_y2 = cy + 350
    for tx_off in [-40, -20, 0, 20, 40]:
        d.line([(cx + tx_off, tassel_y1), (cx + tx_off * 1.5, tassel_y2)], fill=COLOR_LIME_GOLD, width=8)
    d.ellipse([cx - 20, tassel_y1, cx + 20, tassel_y1 + 40], fill=COLOR_BRAND_ORANGE)
    
    # Celebratory confetti stars
    draw_sparkle(d, 310, 340, 26, COLOR_LIME_GOLD)
    draw_sparkle(d, 714, 360, 30, COLOR_WHITE)
    draw_sparkle(d, 340, 620, 20, COLOR_LIME_GREEN)
    draw_sparkle(d, 680, 600, 22, COLOR_BRAND_ORANGE)
    
    return finalize_icon(im)

# (19) IC_Inspector.png
def gen_ic_inspector():
    im, d = create_icon_canvas()
    
    cx, cy = 470, 520
    w, h = 380, 500
    
    # Wooden clipboard body
    d.rounded_rectangle([cx - w//2, cy - h//2, cx + w//2, cy + h//2],
                        radius=20, fill=(155, 95, 45, 255), outline=COLOR_BRAND_ORANGE, width=12)
                        
    # White inspection sheet
    sheet = [cx - w//2 + 24, cy - h//2 + 50, cx + w//2 - 24, cy + h//2 - 30]
    d.rounded_rectangle(sheet, radius=10, fill=COLOR_WHITE, outline=(210, 215, 220, 255), width=4)
    
    # Stainless top clip
    clip_w, clip_h = 160, 60
    d.rounded_rectangle([cx - clip_w//2, cy - h//2 - 10, cx + clip_w//2, cy - h//2 + clip_h],
                        radius=12, fill=(200, 210, 220, 255), outline=COLOR_BRAND_ORANGE, width=6)
    d.ellipse([cx - 20, cy - h//2 + 10, cx + 20, cy - h//2 + 50], fill=(80, 90, 100, 255))
    
    # 3 Checklist rows with green checkmarks
    for i, row_y in enumerate([cy - 120, cy - 30, cy + 60]):
        # Checkbox
        box_x = cx - 110
        d.rounded_rectangle([box_x - 20, row_y - 20, box_x + 20, row_y + 20],
                            radius=6, fill=(240, 250, 240, 255), outline=COLOR_LIME_GREEN, width=4)
        # Bold green checkmark
        d.line([(box_x - 12, row_y), (box_x - 3, row_y + 12), (box_x + 14, row_y - 12)],
               fill=COLOR_LIME_GREEN, width=6)
        # Text line
        d.line([(box_x + 40, row_y), (cx + 110, row_y)], fill=(120, 135, 150, 255), width=8)
        
    # Official golden star inspection shield badge pinned on bottom right
    badge_x, badge_y = 660, 660
    badge_w, badge_h = 170, 200
    shield = [
        (badge_x - badge_w//2, badge_y - badge_h//2),
        (badge_x + badge_w//2, badge_y - badge_h//2),
        (badge_x + badge_w//2, badge_y + 20),
        (badge_x, badge_y + badge_h//2),
        (badge_x - badge_w//2, badge_y + 20)
    ]
    d.polygon(shield, fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=10)
    draw_star(d, badge_x, badge_y - 5, 42, 20, COLOR_BRAND_ORANGE, outline=COLOR_WHITE, width=2)
    
    draw_sparkle(d, badge_x - 50, badge_y - badge_h//2, 28, COLOR_WHITE)
    draw_sparkle(d, cx - 110, cy - h//2 + 30, 22, COLOR_WHITE)
    
    return finalize_icon(im)

# (20) IC_Clear.png
def gen_ic_clear():
    im, d = create_icon_canvas()
    
    cx, cy = 512, 512
    
    # Soft twilight rays in background
    for a in range(0, 360, 30):
        rad = math.radians(a)
        d.line([(cx, cy), (cx + 280 * math.cos(rad), cy + 280 * math.sin(rad))],
               fill=(241, 196, 15, 30), width=8)
               
    # Golden crescent moon
    # Outer circle
    moon_r = 190
    moon_cx, moon_cy = 480, 512
    
    # Create moon via mask
    moon_img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    md = ImageDraw.Draw(moon_img)
    md.ellipse([moon_cx - moon_r, moon_cy - moon_r, moon_cx + moon_r, moon_cy + moon_r], fill=COLOR_LIME_GOLD)
    # Cut out inner circle
    cut_r = 160
    cut_x, cut_y = moon_cx + 70, moon_cy - 40
    md.ellipse([cut_x - cut_r, cut_y - cut_r, cut_x + cut_r, cut_y + cut_r], fill=(0, 0, 0, 0))
    im.alpha_composite(moon_img)
    
    # Crescent moon border
    d.arc([moon_cx - moon_r, moon_cy - moon_r, moon_cx + moon_r, moon_cy + moon_r], 45, 285,
          fill=COLOR_BRAND_ORANGE, width=10)
          
    # 5-pointed star nestled in the crescent's arms
    draw_star(d, 580, 440, 80, 36, COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=6)
    
    # Sparkling constellation stars
    for sx, sy, sr in [(340, 320, 22), (680, 290, 26), (360, 680, 20), (660, 650, 24)]:
        draw_sparkle(d, sx, sy, sr, COLOR_WHITE)
        
    return finalize_icon(im)

# (21) IC_Cust_Student.png
def gen_ic_cust_student():
    im, d = create_icon_canvas()
    
    cx, cy = 512, 530
    
    # Youthful student bust silhouette
    # Backpack strap over shoulder
    d.rounded_rectangle([320, 560, 410, 780], radius=35, fill=COLOR_BRAND_ORANGE, outline=COLOR_LIME_GOLD, width=8)
    
    # Torso (Polo collar)
    torso = [(350, 640), (674, 640), (710, 800), (314, 800)]
    d.polygon(torso, fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=10)
    # Polo V-collar
    d.polygon([(462, 640), (512, 720), (562, 640)], fill=COLOR_WHITE, outline=COLOR_BRAND_ORANGE, width=6)
    
    # Neck
    d.rectangle([462, 540, 562, 640], fill=(255, 230, 195, 255))
    
    # Head
    d.ellipse([412, 360, 612, 560], fill=(255, 230, 195, 255), outline=COLOR_BRAND_ORANGE, width=8)
    
    # Backwards baseball cap with visor
    cap_box = [400, 320, 624, 460]
    d.pieslice(cap_box, 180, 360, fill=COLOR_BRAND_ORANGE, outline=COLOR_LIME_GOLD, width=8)
    # Curved cap visor pointing backwards (left)
    d.polygon([(400, 410), (330, 430), (360, 460), (430, 440)], fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=6)
    
    # Glasses
    d.rectangle([450, 440, 500, 470], outline=COLOR_DEEP_TEA_GREEN, width=5)
    d.rectangle([524, 440, 574, 470], outline=COLOR_DEEP_TEA_GREEN, width=5)
    d.line([(500, 455), (524, 455)], fill=COLOR_DEEP_TEA_GREEN, width=4)
    # Cheerful smile
    d.arc([470, 480, 554, 530], 20, 160, fill=COLOR_DEEP_TEA_GREEN, width=6)
    
    draw_sparkle(d, 330, 380, 24, COLOR_WHITE)
    draw_sparkle(d, 680, 420, 22, COLOR_LIME_GOLD)
    
    return finalize_icon(im)

# (22) IC_Cust_Family.png
def gen_ic_cust_family():
    im, d = create_icon_canvas()
    
    # Parent (left) and child (right) silhouette
    # Parent bust
    px, py = 410, 520
    # Parent head
    d.ellipse([px - 70, py - 180, px + 70, py - 40], fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=8)
    # Parent hair
    d.pieslice([px - 75, py - 190, px + 75, py - 80], 180, 360, fill=COLOR_DEEP_TEA_GREEN)
    # Parent torso
    d.rounded_rectangle([px - 100, py - 20, px + 100, py + 260], radius=40,
                        fill=COLOR_BRAND_ORANGE, outline=COLOR_LIME_GOLD, width=8)
                        
    # Child bust (smaller, right)
    cx, cy = 612, 590
    # Child head
    d.ellipse([cx - 50, cy - 130, cx + 50, cy - 30], fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=6)
    # Child cap
    d.pieslice([cx - 55, cy - 145, cx + 55, cy - 65], 180, 360, fill=(46, 204, 113, 255), outline=COLOR_BRAND_ORANGE, width=4)
    # Child torso
    d.rounded_rectangle([cx - 70, cy - 10, cx + 70, cy + 190], radius=30,
                        fill=(52, 152, 219, 255), outline=COLOR_BRAND_ORANGE, width=6)
                        
    # Golden heart floating between them
    hx, hy = 512, 450
    hr = 40
    # Heart lobes
    d.ellipse([hx - hr, hy - hr, hx, hy], fill=COLOR_RED, outline=COLOR_LIME_GOLD, width=4)
    d.ellipse([hx, hy - hr, hx + hr, hy], fill=COLOR_RED, outline=COLOR_LIME_GOLD, width=4)
    d.polygon([(hx - hr, hy - hr//3), (hx + hr, hy - hr//3), (hx, hy + hr)], fill=COLOR_RED)
    
    draw_sparkle(d, hx, hy - hr - 15, 24, COLOR_WHITE)
    draw_sparkle(d, 310, 380, 20, COLOR_WHITE)
    draw_sparkle(d, 710, 480, 22, COLOR_LIME_GOLD)
    
    return finalize_icon(im)

# (23) IC_Cust_Office.png
def gen_ic_cust_office():
    im, d = create_icon_canvas()
    
    cx, cy = 470, 530
    
    # Sharp business office worker bust
    # Collared shirt torso
    torso = [(cx - 150, cy + 110), (cx + 150, cy + 110), (cx + 180, cy + 260), (cx - 180, cy + 260)]
    d.polygon(torso, fill=COLOR_CREAM, outline=COLOR_BRAND_ORANGE, width=8)
    
    # Necktie (Brand Orange with gold stripes)
    tie = [(cx - 24, cy + 110), (cx + 24, cy + 110), (cx + 36, cy + 250), (cx, cy + 280), (cx - 36, cy + 250)]
    d.polygon(tie, fill=COLOR_BRAND_ORANGE, outline=COLOR_LIME_GOLD, width=5)
    d.polygon([(cx - 20, cy + 100), (cx + 20, cy + 100), (cx + 15, cy + 130), (cx - 15, cy + 130)],
              fill=COLOR_LIME_GOLD)
              
    # Neck
    d.rectangle([cx - 45, cy + 20, cx + 45, cy + 110], fill=(255, 230, 195, 255))
    
    # Head
    d.ellipse([cx - 85, cy - 150, cx + 85, cy + 40], fill=(255, 230, 195, 255), outline=COLOR_BRAND_ORANGE, width=8)
    # Sleek side-parted dark hair
    d.pieslice([cx - 90, cy - 165, cx + 90, cy - 40], 190, 360, fill=COLOR_DEEP_TEA_GREEN)
    
    # Modern glasses
    d.rectangle([cx - 65, cy - 60, cx - 15, cy - 30], outline=COLOR_DEEP_TEA_GREEN, width=5)
    d.rectangle([cx + 15, cy - 60, cx + 65, cy - 30], outline=COLOR_DEEP_TEA_GREEN, width=5)
    d.line([(cx - 15, cy - 45), (cx + 15, cy - 45)], fill=COLOR_DEEP_TEA_GREEN, width=4)
    
    # Leather briefcase on right side
    bx, by = 680, 640
    bw, bh = 140, 180
    d.rounded_rectangle([bx - bw//2, by - bh//2, bx + bw//2, by + bh//2],
                        radius=16, fill=(130, 75, 35, 255), outline=COLOR_BRAND_ORANGE, width=8)
    # Handle & lock
    d.arc([bx - 30, by - bh//2 - 25, bx + 30, by - bh//2 + 15], 180, 360, fill=COLOR_LIME_GOLD, width=8)
    d.rectangle([bx - 15, by - bh//2 + 15, bx + 15, by - bh//2 + 35], fill=COLOR_LIME_GOLD)
    
    draw_sparkle(d, 330, 360, 24, COLOR_WHITE)
    draw_sparkle(d, 680, 520, 22, COLOR_WHITE)
    
    return finalize_icon(im)

# (24) IC_Cust_Regular.png
def gen_ic_cust_regular():
    im, d = create_icon_canvas()
    
    cx, cy = 470, 530
    
    # Friendly regular customer bust
    # Torso
    torso = [(cx - 140, cy + 110), (cx + 140, cy + 110), (cx + 170, cy + 260), (cx - 170, cy + 260)]
    d.polygon(torso, fill=COLOR_DEEP_TEA_GREEN, outline=COLOR_BRAND_ORANGE, width=8)
    
    # Neck & Head
    d.rectangle([cx - 45, cy + 30, cx + 45, cy + 110], fill=(255, 230, 195, 255))
    d.ellipse([cx - 85, cy - 140, cx + 85, cy + 50], fill=(255, 230, 195, 255), outline=COLOR_BRAND_ORANGE, width=8)
    # Hair with neat curl
    d.pieslice([cx - 90, cy - 160, cx + 90, cy - 30], 180, 360, fill=(100, 60, 30, 255))
    
    # Big friendly smile and winking eye
    d.arc([cx - 60, cy - 35, cx - 20, cy - 15], 20, 160, fill=COLOR_DEEP_TEA_GREEN, width=5)  # wink
    d.ellipse([cx + 25, cy - 35, cx + 45, cy - 15], fill=COLOR_DEEP_TEA_GREEN)                # open eye
    d.arc([cx - 50, cy - 10, cx + 50, cy + 35], 0, 180, fill=COLOR_RED, width=6)              # warm smile
    
    # Raising a signature Trạm Chanh takeaway cup on the right!
    cup_x, cup_y = 670, 560
    cw1, cw2, ch = 90, 70, 140
    # Clear plastic cup with iced tea
    cup_pts = [
        (cup_x - cw1//2, cup_y - ch//2),
        (cup_x + cw1//2, cup_y - ch//2),
        (cup_x + cw2//2, cup_y + ch//2),
        (cup_x - cw2//2, cup_y + ch//2)
    ]
    d.polygon(cup_pts, fill=(245, 190, 45, 220), outline=COLOR_CREAM, width=6)
    # Cup lid
    d.ellipse([cup_x - cw1//2 - 10, cup_y - ch//2 - 16, cup_x + cw1//2 + 10, cup_y - ch//2 + 16],
              fill=COLOR_CREAM, outline=COLOR_BRAND_ORANGE, width=4)
    # Green straw
    d.line([(cup_x + 10, cup_y - ch//2 - 40), (cup_x - 10, cup_y + 20)], fill=COLOR_LIME_GREEN, width=10)
    # Lime slice garnish on rim
    d.ellipse([cup_x - cw1//2 - 20, cup_y - ch//2 - 25, cup_x - cw1//2 + 25, cup_y - ch//2 + 20],
              fill=COLOR_LIME_GREEN, outline=COLOR_LIME_GOLD, width=4)
              
    # Thumbs-up gesture on the left
    tx, ty = 280, 600
    d.ellipse([tx - 25, ty - 45, tx + 25, ty + 25], fill=(255, 230, 195, 255), outline=COLOR_BRAND_ORANGE, width=6)
    d.polygon([(tx - 15, ty - 45), (tx + 10, ty - 75), (tx + 30, ty - 65), (tx + 20, ty - 35)],
              fill=(255, 230, 195, 255), outline=COLOR_BRAND_ORANGE, width=6)
              
    draw_sparkle(d, cup_x, cup_y - ch//2 - 50, 26, COLOR_WHITE)
    draw_sparkle(d, 320, 360, 22, COLOR_WHITE)
    
    return finalize_icon(im)

# -----------------------------------------------------------------------------
# TITLE BACKGROUND GENERATOR (1920x1080)
# -----------------------------------------------------------------------------
def generate_title_background():
    """
    Generate BG_Title.png (1920x1080):
    Night street scene of Trạm Chanh stall, warm glowing string lights,
    dark atmospheric sidewalk background, shophouse silhouettes, wet street reflections.
    """
    width, height = 1920, 1080
    bg = Image.new("RGBA", (width, height), (0, 0, 0, 255))
    draw = ImageDraw.Draw(bg)
    
    # 1. Sky & Atmospheric gradient
    # Deep midnight indigo (#081018) -> Teal twilight (#0E241E) -> Street horizon (#142E24)
    sky_arr = np.zeros((height, width, 4), dtype=np.uint8)
    for y in range(height):
        t = y / height
        if t < 0.45:
            # Upper sky
            factor = t / 0.45
            r = int(8 + (14 - 8) * factor)
            g = int(16 + (36 - 16) * factor)
            b = int(24 + (30 - 24) * factor)
        else:
            # Sidewalk & lower ground
            factor = (t - 0.45) / 0.55
            r = int(14 + (18 - 14) * factor)
            g = int(36 + (28 - 36) * factor)
            b = int(30 + (22 - 30) * factor)
        sky_arr[y, :, 0] = r
        sky_arr[y, :, 1] = g
        sky_arr[y, :, 2] = b
        sky_arr[y, :, 3] = 255
    bg = Image.fromarray(sky_arr, "RGBA")
    draw = ImageDraw.Draw(bg)
    
    # 2. Distant Shophouses & City Skyline Silhouettes (Y: 220 to 650)
    # Multiple colonial shophouses with varied heights, cornices, balconies
    buildings = [
        (0, 180, 280),
        (260, 240, 260),
        (500, 160, 320),
        (800, 210, 290),
        (1070, 170, 310),
        (1360, 230, 280),
        (1620, 190, 300)
    ]
    for bx, by, bw in buildings:
        # Building silhouette
        draw.rectangle([bx, by, bx + bw, 700], fill=(12, 22, 18, 255))
        # Cornice / roof trim
        draw.rectangle([bx - 10, by - 12, bx + bw + 10, by], fill=(18, 30, 25, 255))
        
        # Illuminated windows with warm amber glow
        for wy in range(by + 40, 600, 75):
            for wx in range(bx + 35, bx + bw - 35, 60):
                # 60% chance of illuminated window
                if ((wx * 13 + wy * 17) % 10) < 6:
                    glow_col = (243, 156, 18, 210) if ((wx + wy) % 2 == 0) else (241, 196, 15, 240)
                    draw.rounded_rectangle([wx, wy, wx + 36, wy + 50], radius=4, fill=glow_col)
                    # Window muntin grid
                    draw.line([(wx + 18, wy), (wx + 18, wy + 50)], fill=(40, 30, 15, 200), width=2)
                    draw.line([(wx, wy + 25), (wx + 36, wy + 25)], fill=(40, 30, 15, 200), width=2)
                    
    # Overhead utility pole and silhouette wires (iconic Hanoi signature)
    draw.line([(180, 150), (180, 700)], fill=(16, 26, 20, 255), width=18)
    draw.line([(140, 200), (220, 200)], fill=(16, 26, 20, 255), width=8)
    # Drooping overhead cables across the sky
    for sag_y in [190, 230, 280]:
        catenary_pts = []
        for x in range(0, width, 40):
            norm_x = (x - width/2) / (width/2)
            y = sag_y + int(60 * (1 - norm_x * norm_x))
            catenary_pts.append((x, y))
        draw.line(catenary_pts, fill=(15, 25, 20, 200), width=3)
        
    # 3. Sidewalk & Curb (Y: 650 to 1080)
    # Sidewalk surface
    draw.rectangle([0, 650, width, 920], fill=(28, 42, 34, 255))
    # Diamond terrazzo tiles
    for ty in range(660, 910, 45):
        for tx in range(0, width, 45):
            draw.line([(tx, ty), (tx + 45, ty + 45)], fill=(38, 54, 44, 255), width=2)
            draw.line([(tx + 45, ty), (tx, ty + 45)], fill=(38, 54, 44, 255), width=2)
    # Sidewalk granite curb
    draw.rectangle([0, 910, width, 935], fill=(45, 60, 50, 255))
    draw.line([(0, 910), (width, 910)], fill=(75, 95, 80, 255), width=3)
    
    # Wet asphalt street road surface (Y: 935 to 1080)
    draw.rectangle([0, 935, width, height], fill=(16, 20, 18, 255))
    
    # 4. Trạm Chanh Street Stall (Midground: X: 800 to 1600, Y: 380 to 880)
    stall_x1, stall_x2 = 820, 1620
    
    # Stall awning roof (corrugated green canopy)
    roof_pts = [(stall_x1 - 40, 450), (stall_x2 + 40, 450), (stall_x2 + 10, 360), (stall_x1 - 10, 360)]
    draw.polygon(roof_pts, fill=(28, 62, 42, 255), outline=COLOR_BRAND_ORANGE, width=6)
    # Roof corrugation ribs
    for rx in range(stall_x1 - 20, stall_x2 + 20, 25):
        draw.line([(rx, 365), (rx - 10, 445)], fill=(45, 85, 60, 255), width=4)
        
    # Luminous Storefront Backlit Sign: "TRẠM CHANH"
    sign_box = [stall_x1 + 100, 380, stall_x2 - 100, 440]
    draw.rounded_rectangle(sign_box, radius=12, fill=COLOR_DEEP_TEA_GREEN, outline=COLOR_BRAND_ORANGE, width=6)
    draw.rounded_rectangle([sign_box[0] + 6, sign_box[1] + 6, sign_box[2] - 6, sign_box[3] - 6],
                           radius=8, outline=COLOR_LIME_GOLD, width=3)
    font_sign = get_vietnamese_font(44, bold=True)
    draw.text((stall_x1 + 240, sign_box[1] + 8), "TRẠM CHANH", font=font_sign, fill=COLOR_LIME_GOLD)
    
    # Wooden Stall Counter Body
    counter_box = [stall_x1, 560, stall_x2, 850]
    draw.rounded_rectangle(counter_box, radius=8, fill=(70, 42, 25, 255), outline=COLOR_BRAND_ORANGE, width=8)
    # Vertical timber slats
    for sx in range(stall_x1 + 30, stall_x2, 40):
        draw.line([(sx, 565), (sx, 845)], fill=(95, 60, 35, 255), width=6)
        
    # Polished stainless steel counter top
    draw.rounded_rectangle([stall_x1 - 20, 540, stall_x2 + 20, 570], radius=6,
                           fill=(200, 215, 225, 255), outline=COLOR_BRAND_ORANGE, width=4)
    draw.line([(stall_x1 - 15, 544), (stall_x2 + 15, 544)], fill=COLOR_WHITE, width=4)
    
    # Stall Equipment on Counter:
    # 2 Stainless tea urns with level gauges
    for ux in [stall_x1 + 100, stall_x1 + 210]:
        draw.rounded_rectangle([ux - 40, 450, ux + 40, 545], radius=10, fill=(185, 200, 215, 255), outline=(130, 145, 160, 255), width=4)
        draw.line([(ux - 30, 460), (ux - 30, 535)], fill=COLOR_WHITE, width=4)
        # Red/Gold tea level tube
        draw.line([(ux + 25, 470), (ux + 25, 530)], fill=COLOR_BRAND_ORANGE, width=6)
        
    # Stainless Ice Bin well
    draw.rounded_rectangle([stall_x1 + 320, 490, stall_x1 + 520, 545], radius=6,
                           fill=(160, 180, 195, 255), outline=COLOR_CREAM, width=4)
                           
    # Topping container display (color syrup bottles)
    for bx, bcol in zip(range(stall_x1 + 560, stall_x1 + 760, 45),
                        [COLOR_LIME_GREEN, COLOR_LIME_GOLD, COLOR_RED, (142, 68, 173, 255)]):
        draw.rounded_rectangle([bx - 16, 475, bx + 16, 545], radius=6, fill=bcol, outline=COLOR_CREAM, width=3)
        draw.rounded_rectangle([bx - 8, 460, bx + 8, 475], radius=2, fill=COLOR_LIME_GOLD)
        
    # 5. Sidewalk Seating: Plastic Stools & Crate Table (Left Foreground: X: 280 to 680)
    # Inverted yellow drink crate as low sidewalk table
    crate_box = [420, 720, 560, 840]
    draw.rounded_rectangle(crate_box, radius=8, fill=(243, 156, 18, 255), outline=COLOR_BRAND_ORANGE, width=6)
    # Crate grid cutouts
    for gx in [445, 485, 525]:
        draw.rounded_rectangle([gx, 740, gx + 25, 820], radius=4, fill=(180, 110, 10, 255))
        
    # 2 Iced Lemon Tea cups sitting on crate table
    for cup_x in [460, 515]:
        draw.polygon([(cup_x - 14, 675), (cup_x + 14, 675), (cup_x + 10, 720), (cup_x - 10, 720)],
                     fill=(245, 195, 45, 230), outline=COLOR_CREAM, width=2)
        # Green straw
        draw.line([(cup_x + 4, 655), (cup_x - 2, 715)], fill=COLOR_LIME_GREEN, width=4)
        # Lime slice on rim
        draw.ellipse([cup_x - 18, 668, cup_x - 4, 682], fill=COLOR_LIME_GREEN)
        
    # Authentic Vietnamese Blue and Red plastic stools
    # Blue stool (left)
    draw.polygon([(290, 750), (370, 750), (380, 840), (280, 840)], fill=(41, 128, 185, 255), outline=(25, 90, 140, 255), width=4)
    draw.ellipse([285, 740, 375, 760], fill=(52, 152, 219, 255), outline=(41, 128, 185, 255), width=4)
    # Red stool (right)
    draw.polygon([(610, 750), (690, 750), (700, 840), (600, 840)], fill=(192, 57, 43, 255), outline=(140, 35, 25, 255), width=4)
    draw.ellipse([605, 740, 695, 760], fill=(231, 76, 60, 255), outline=(192, 57, 43, 255), width=4)
    
    # 6. Hanging String Lights (Festoon Bulbs across upper scene)
    # Catenary curved wires
    wires = [
        ([(0, 140), (480, 240), (960, 270), (1440, 220), (1920, 130)], 14),
        ([(0, 220), (520, 320), (1020, 350), (1500, 300), (1920, 210)], 12)
    ]
    for w_pts, num_bulbs in wires:
        draw.line(w_pts, fill=(35, 45, 40, 255), width=4)
        
        # Calculate bulb positions along curve
        for bi in range(1, num_bulbs):
            bx = int(bi * (width / num_bulbs))
            # Quadratic bezier interpolation for sagging wire
            norm_x = (bx - width/2) / (width/2)
            by = w_pts[0][1] + int(120 * (1 - norm_x * norm_x * 0.5))
            
            # Short drop cable
            draw.line([(bx, by), (bx, by + 25)], fill=(30, 40, 35, 255), width=4)
            
            # Luminous radiant warm bulb glow (soft bloom)
            for gr, g_alpha in [(80, 20), (55, 45), (35, 90), (18, 180)]:
                draw.ellipse([bx - gr, by + 35 - gr, bx + gr, by + 35 + gr], fill=(241, 196, 15, g_alpha))
                
            # Bulb core
            draw.ellipse([bx - 10, by + 25, bx + 10, by + 45], fill=(255, 255, 220, 255))
            draw.ellipse([bx - 4, by + 31, bx + 4, by + 39], fill=COLOR_WHITE)
            
    # 7. Wet Asphalt Street Reflections on the Roadway
    # Mirror reflection streaks beneath the warm bulbs and neon stall sign
    for rx in range(100, width, 140):
        # Golden reflection sheen
        draw.line([(rx, 945), (rx + 25, 1070)], fill=(241, 196, 15, 40), width=18)
        draw.line([(rx + 6, 960), (rx + 20, 1055)], fill=(230, 126, 34, 50), width=10)
    # Bright green and gold reflection pool beneath stall sign
    draw.ellipse([1000, 960, 1450, 1040], fill=(46, 204, 113, 35))
    draw.ellipse([1050, 975, 1400, 1025], fill=(241, 196, 15, 45))
    
    # 8. Cinematic Vignette (Soft dark border shading)
    vignette = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    vd = ImageDraw.Draw(vignette)
    for i in range(120):
        alpha = int(140 * (1 - i / 120))
        vd.rectangle([i, i, width - i, height - i], outline=(0, 0, 0, alpha), width=2)
    bg.alpha_composite(vignette)
    
    return bg

# -----------------------------------------------------------------------------
# BRAND LOGO GENERATOR (1024x1024)
# -----------------------------------------------------------------------------
def generate_brand_logo():
    """
    Generate LOGO_TramChanh.png (1024x1024):
    High-resolution brand logo with stylized Vietnamese typography 'TRẠM CHANH'
    (correct diacritics: Ạ and H) with fresh lime slice accent, brand ribbon,
    and decorative tea leaves.
    """
    size = 1024
    logo = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(logo)
    
    cx, cy = 512, 512
    
    # 1. Outer Brand Heraldic Seal / Badge: Deep Tea Green with Orange & Gold
    seal_r = 440
    # Soft drop shadow
    d.ellipse([cx - seal_r + 10, cy - seal_r + 20, cx + seal_r + 10, cy + seal_r + 20], fill=(0, 0, 0, 80))
    # Outer Deep Tea Green badge
    d.ellipse([cx - seal_r, cy - seal_r, cx + seal_r, cy + seal_r], fill=COLOR_DEEP_TEA_GREEN)
    # Brand Orange rim
    d.ellipse([cx - seal_r, cy - seal_r, cx + seal_r, cy + seal_r], outline=COLOR_BRAND_ORANGE, width=24)
    # Inner Lime Gold pin-stripe
    d.ellipse([cx - seal_r + 32, cy - seal_r + 32, cx + seal_r - 32, cy + seal_r - 32], outline=COLOR_LIME_GOLD, width=8)
    
    # 16 Golden Rivet Accents around perimeter
    for i in range(16):
        angle = i * (2 * math.pi / 16)
        rx = cx + (seal_r - 16) * math.cos(angle)
        ry = cy + (seal_r - 16) * math.sin(angle)
        d.ellipse([rx - 8, ry - 8, rx + 8, ry + 8], fill=COLOR_LIME_GOLD, outline=COLOR_BRAND_ORANGE, width=2)
        
    # 2. Prominent Fresh Lime Slice Accent (Upper Center: cy=380, r=200)
    lime_cx, lime_cy, lime_r = 512, 370, 190
    
    # Outer dark lime rind
    d.ellipse([lime_cx - lime_r, lime_cy - lime_r, lime_cx + lime_r, lime_cy + lime_r],
              fill=(30, 132, 73, 255), outline=COLOR_LIME_GOLD, width=8)
    # Vibrant lime zest
    d.ellipse([lime_cx - lime_r + 16, lime_cy - lime_r + 16, lime_cx + lime_r - 16, lime_cy + lime_r - 16],
              fill=(46, 204, 113, 255))
    # Crisp ivory pith ring
    d.ellipse([lime_cx - lime_r + 28, lime_cy - lime_r + 28, lime_cx + lime_r - 28, lime_cy + lime_r - 28],
              fill=(240, 255, 240, 255))
    # Translucent lime pulp base
    d.ellipse([lime_cx - lime_r + 40, lime_cy - lime_r + 40, lime_cx + lime_r - 40, lime_cy + lime_r - 40],
              fill=(169, 223, 191, 255))
              
    # 8 Radial Citrus Pulp Segments with Golden Lime gradient
    pulp_r = lime_r - 48
    for seg in range(8):
        start_deg = seg * 45 + 5
        end_deg = (seg + 1) * 45 - 5
        # Segment wedge
        d.pieslice([lime_cx - pulp_r, lime_cy - pulp_r, lime_cx + pulp_r, lime_cy + pulp_r],
                   start_deg, end_deg, fill=COLOR_LIME_GOLD, outline=COLOR_WHITE, width=4)
        
    # Center pith star
    d.ellipse([lime_cx - 24, lime_cy - 24, lime_cx + 24, lime_cy + 24], fill=(240, 255, 240, 255))
    d.ellipse([lime_cx - 12, lime_cy - 12, lime_cx + 12, lime_cy + 12], fill=COLOR_WHITE)
    
    # Sparkling citrus juice drops
    draw_sparkle(d, lime_cx - 100, lime_cy - 100, 32, COLOR_WHITE)
    draw_sparkle(d, lime_cx + 110, lime_cy - 90, 26, COLOR_WHITE)
    
    # Emerald Tea Leaves sprouting left and right
    # Left leaf
    d.ellipse([lime_cx - lime_r - 70, lime_cy - 40, lime_cx - lime_r + 30, lime_cy + 50],
              fill=COLOR_LIME_GREEN, outline=COLOR_BRAND_ORANGE, width=6)
    d.line([(lime_cx - lime_r - 60, lime_cy + 5), (lime_cx - lime_r + 20, lime_cy + 5)], fill=COLOR_LIME_GOLD, width=4)
    # Right leaf
    d.ellipse([lime_cx + lime_r - 30, lime_cy - 40, lime_cx + lime_r + 70, lime_cy + 50],
              fill=COLOR_LIME_GREEN, outline=COLOR_BRAND_ORANGE, width=6)
    d.line([(lime_cx + lime_r - 20, lime_cy + 5), (lime_cx + lime_r + 60, lime_cy + 5)], fill=COLOR_LIME_GOLD, width=4)
    
    # 3. Stylized Vietnamese Typography: 'TRẠM CHANH'
    # High-impact 3D beveled typography with clean diacritics
    font_main = get_vietnamese_font(128, bold=True)
    text = "TRẠM CHANH"
    
    # Calculate bounding box to center text accurately
    bbox = font_main.getbbox(text)
    tw = bbox[2] - bbox[0]
    tx = cx - tw // 2
    ty = 560
    
    # Text Banner Plaque / Backing Ribbon
    banner_w = tw + 100
    banner_h = 170
    banner_box = [cx - banner_w // 2, ty - 25, cx + banner_w // 2, ty + banner_h - 25]
    d.rounded_rectangle(banner_box, radius=30, fill=COLOR_DEEP_TEA_GREEN, outline=COLOR_BRAND_ORANGE, width=12)
    d.rounded_rectangle([banner_box[0] + 12, banner_box[1] + 12, banner_box[2] - 12, banner_box[3] - 12],
                        radius=20, outline=COLOR_LIME_GOLD, width=4)
                        
    # 3D Extrusion Layers (Dark drop shadow -> Brand Orange extrusion -> Golden face)
    # Shadow
    d.text((tx + 8, ty + 12), text, font=font_main, fill=(10, 25, 16, 255))
    # Extrusion depth
    for offset_y in range(8, 0, -2):
        d.text((tx, ty + offset_y), text, font=font_main, fill=(186, 74, 0, 255))
    # Brand Orange outline
    for ox, oy in [(-3, 0), (3, 0), (0, -3), (0, 3), (-2, -2), (2, 2)]:
        d.text((tx + ox, ty + oy), text, font=font_main, fill=COLOR_BRAND_ORANGE)
    # Golden Cream Face
    d.text((tx, ty), text, font=font_main, fill=(255, 252, 235, 255))
    
    # Diacritic check: ensure underdot on 'Ạ' is robust, bold, and crisp!
    # In 'TRẠM CHANH', 'Ạ' is at character index 2
    t_prefix_bbox = font_main.getbbox("TR")
    tra_bbox = font_main.getbbox("TRẠ")
    dot_cx = tx + (t_prefix_bbox[2] + tra_bbox[2]) // 2
    dot_cy = ty + bbox[3] - 4
    # Reinforce underdot if needed for maximum clarity
    d.ellipse([dot_cx - 8, dot_cy - 8, dot_cx + 8, dot_cy + 8], fill=COLOR_BRAND_ORANGE)
    d.ellipse([dot_cx - 5, dot_cy - 5, dot_cx + 5, dot_cy + 5], fill=(255, 252, 235, 255))
    
    # 4. Subtitle Ribbon Banner: "TRÀ CHANH PHỐ CỔ"
    ribbon_y = ty + 155
    ribbon_w = 480
    ribbon_h = 60
    ribbon_box = [cx - ribbon_w // 2, ribbon_y, cx + ribbon_w // 2, ribbon_y + ribbon_h]
    
    # Ribbon swallowtails (left and right)
    d.polygon([(ribbon_box[0] - 40, ribbon_y + 10), (ribbon_box[0], ribbon_y),
               (ribbon_box[0], ribbon_y + ribbon_h), (ribbon_box[0] - 40, ribbon_y + ribbon_h - 10),
               (ribbon_box[0] - 20, ribbon_y + ribbon_h // 2)],
              fill=(186, 74, 0, 255), outline=COLOR_LIME_GOLD, width=4)
    d.polygon([(ribbon_box[2] + 40, ribbon_y + 10), (ribbon_box[2], ribbon_y),
               (ribbon_box[2], ribbon_y + ribbon_h), (ribbon_box[2] + 40, ribbon_y + ribbon_h - 10),
               (ribbon_box[2] + 20, ribbon_y + ribbon_h // 2)],
              fill=(186, 74, 0, 255), outline=COLOR_LIME_GOLD, width=4)
              
    # Main ribbon body
    d.rounded_rectangle(ribbon_box, radius=12, fill=COLOR_BRAND_ORANGE, outline=COLOR_LIME_GOLD, width=6)
    
    font_sub = get_vietnamese_font(34, bold=True)
    sub_text = "TRÀ CHANH PHỐ CỔ"
    sub_bbox = font_sub.getbbox(sub_text)
    sub_w = sub_bbox[2] - sub_bbox[0]
    d.text((cx - sub_w // 2, ribbon_y + 10), sub_text, font=font_sub, fill=COLOR_WHITE)
    
    # 5. Decorative Stars & Glints
    draw_star(d, cx - ribbon_w // 2 - 20, ribbon_y + ribbon_h // 2, 16, 8, COLOR_LIME_GOLD)
    draw_star(d, cx + ribbon_w // 2 + 20, ribbon_y + ribbon_h // 2, 16, 8, COLOR_LIME_GOLD)
    
    draw_sparkle(d, tx + 60, ty - 15, 34, COLOR_WHITE)
    draw_sparkle(d, tx + tw - 40, ty - 15, 30, COLOR_WHITE)
    
    return logo

# -----------------------------------------------------------------------------
# MAIN ASSET GENERATION PIPELINE
# -----------------------------------------------------------------------------
def main():
    print("=" * 70)
    print("Trạm Chanh UI Asset Generator (ART-FIX-028)")
    print("=" * 70)
    
    # 1. Ensure target directories exist
    os.makedirs(ICONS_DIR, exist_ok=True)
    os.makedirs(BACKGROUNDS_DIR, exist_ok=True)
    os.makedirs(LOGO_DIR, exist_ok=True)
    
    # 2. Write folder .meta files
    print("\n[1/4] Generating folder .meta files...")
    folder_metas = [
        write_folder_meta(UI_ROOT),
        write_folder_meta(ICONS_DIR),
        write_folder_meta(BACKGROUNDS_DIR),
        write_folder_meta(LOGO_DIR),
    ]
    for fm in folder_metas:
        print(f"  -> Created folder meta: {os.path.relpath(fm, REPO_ROOT)}")
        
    # 3. Generate UI Icons (24 icons)
    print("\n[2/4] Generating 24 UI Icons (256x256, transparent PNG)...")
    icon_generators = [
        # Ingredients (4)
        ("IC_Ice.png", gen_ic_ice),
        ("IC_Jelly.png", gen_ic_jelly),
        ("IC_Batter.png", gen_ic_batter),
        ("IC_Wrap.png", gen_ic_wrap),
        # Upgrades (5)
        ("IC_Up_BiggerIceBin.png", gen_ic_up_bigger_ice_bin),
        ("IC_Up_Pantry.png", gen_ic_up_pantry),
        ("IC_Up_StringLights.png", gen_ic_up_string_lights),
        ("IC_Up_WholesaleDeal.png", gen_ic_up_wholesale_deal),
        ("IC_Up_GoldenMenuBoard.png", gen_ic_up_golden_menu_board),
        # Status / Events (11)
        ("IC_Star_Full.png", gen_ic_star_full),
        ("IC_Star_Empty.png", gen_ic_star_empty),
        ("IC_Money.png", gen_ic_money),
        ("IC_Patience.png", gen_ic_patience),
        ("IC_Goal.png", gen_ic_goal),
        ("IC_Rain.png", gen_ic_rain),
        ("IC_Hot.png", gen_ic_hot),
        ("IC_PowerCut.png", gen_ic_power_cut),
        ("IC_Festival.png", gen_ic_festival),
        ("IC_Inspector.png", gen_ic_inspector),
        ("IC_Clear.png", gen_ic_clear),
        # Customers (4)
        ("IC_Cust_Student.png", gen_ic_cust_student),
        ("IC_Cust_Family.png", gen_ic_cust_family),
        ("IC_Cust_Office.png", gen_ic_cust_office),
        ("IC_Cust_Regular.png", gen_ic_cust_regular),
    ]
    
    generated_icons = []
    for filename, gen_fn in icon_generators:
        out_path = os.path.join(ICONS_DIR, filename)
        icon_img = gen_fn()
        icon_img.save(out_path, "PNG", optimize=True)
        meta_p = write_sprite_meta(out_path, max_texture_size=2048)
        generated_icons.append((out_path, meta_p))
        print(f"  [OK] {filename} (256x256) -> {os.path.relpath(out_path, REPO_ROOT)}")
        
    # 4. Generate Title Background (1920x1080)
    print("\n[3/4] Generating Title Background (1920x1080 PNG)...")
    bg_path = os.path.join(BACKGROUNDS_DIR, "BG_Title.png")
    bg_img = generate_title_background()
    bg_img.save(bg_path, "PNG", optimize=True)
    bg_meta = write_sprite_meta(bg_path, max_texture_size=2048)
    print(f"  [OK] BG_Title.png (1920x1080) -> {os.path.relpath(bg_path, REPO_ROOT)}")
    
    # 5. Generate Brand Logo (1024x1024)
    print("\n[4/4] Generating Brand Logo (1024x1024 PNG)...")
    logo_path = os.path.join(LOGO_DIR, "LOGO_TramChanh.png")
    logo_img = generate_brand_logo()
    logo_img.save(logo_path, "PNG", optimize=True)
    logo_meta = write_sprite_meta(logo_path, max_texture_size=2048)
    print(f"  [OK] LOGO_TramChanh.png (1024x1024) -> {os.path.relpath(logo_path, REPO_ROOT)}")
    
    print("\n" + "=" * 70)
    print("SUCCESS: All UI assets and .meta files generated cleanly!")
    print(f"Total icons generated: {len(generated_icons)}")
    print(f"Title background: 1 ({os.path.relpath(bg_path, REPO_ROOT)})")
    print(f"Brand logo: 1 ({os.path.relpath(logo_path, REPO_ROOT)})")
    print("=" * 70)

if __name__ == "__main__":
    main()
