#!/usr/bin/env python3
"""
ArtSource/Scripts/generate_menuboard_assets.py

Procedural art asset generator for Project Trạm Chanh (ART-FIX-028):
1. Textures in Assets/TramChanh/Art/Textures/Props/:
   - T_MenuBoard_Prices.png (2048x1024)
   - T_MenuBoard_Prices_Normal.png (2048x1024)
   - T_MenuBoard_Prices_MaskMap.png (2048x1024)
2. 3D Mesh in Assets/TramChanh/Art/Models/Props/:
   - SM_MenuBoard_Prices.obj (132 triangles < 200 budget, pivot at bottom center)
3. Materials in Assets/TramChanh/Art/Materials/Props/:
   - MAT_MenuBoard_Prices.mat (dry version)
   - MAT_MenuBoard_Prices_Wet.mat (wet version for rainy nights)
4. Prefabs in Assets/TramChanh/Prefabs/Props/:
   - PF_MenuBoard_Prices.prefab
   - PF_MenuBoard_Prices_Wet.prefab
"""

import os
import uuid
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

def deterministic_guid(path):
    """Generate deterministic GUID based on repository-relative path."""
    rel = os.path.relpath(path, REPO_ROOT).replace("\\", "/")
    return uuid.uuid5(uuid.NAMESPACE_DNS, f"tramchanh.asset.{rel}").hex

def get_or_create_guid(meta_path):
    if os.path.exists(meta_path):
        try:
            with open(meta_path, "r", encoding="utf-8") as f:
                for line in f:
                    if line.startswith("guid:"):
                        return line.split(":")[1].strip()
        except Exception:
            pass
    return deterministic_guid(meta_path[:-5] if meta_path.endswith(".meta") else meta_path)

def ensure_folder_meta(folder_path):
    os.makedirs(folder_path, exist_ok=True)
    meta_path = folder_path.rstrip("/\\") + ".meta"
    if not os.path.exists(meta_path):
        guid = deterministic_guid(folder_path)
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")
        print(f"Created folder meta: {meta_path}")

def get_font(size, bold=True):
    paths = [
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf" if bold else "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/Library/Fonts/Arial Unicode.ttf",
        "/System/Library/Fonts/Helvetica.ttc",
        "/System/Library/Fonts/SFNS.ttf"
    ]
    for p in paths:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                pass
    return ImageFont.load_default()

# ==============================================================================
# 1. TEXTURES GENERATION (2048 x 1024)
# ==============================================================================

def compute_normal_map(height_map, strength=2.2):
    """Computes a tangent-space normal map (Sobel filter, Unity Y-up)."""
    h = height_map.astype(float) / 255.0
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5
    dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * 0.5
    
    nx = -dx * strength
    ny = -dy * strength
    nz = np.ones_like(h)
    
    length = np.sqrt(nx**2 + ny**2 + nz**2)
    nx /= length
    ny /= length
    nz /= length
    
    r = ((nx * 0.5 + 0.5) * 255).astype(np.uint8)
    g = (((-ny) * 0.5 + 0.5) * 255).astype(np.uint8)  # Unity Y-up (green channel flipped)
    b = ((nz * 0.5 + 0.5) * 255).astype(np.uint8)
    return np.stack([r, g, b], axis=-1)

def draw_lemon_slice(draw, cx, cy, radius, chalk_color=(255, 235, 60), outline_color=(255, 255, 240)):
    """Draws a decorative chalk lemon slice."""
    # Outer rind
    draw.ellipse([cx - radius, cy - radius, cx + radius, cy + radius], outline=outline_color, width=4)
    # Inner pith
    draw.ellipse([cx - radius + 5, cy - radius + 5, cx + radius - 5, cy + radius - 5], outline=(255, 255, 220), width=2)
    # Pulp segments (8 triangular slices)
    num_segments = 8
    seg_r = radius - 10
    for i in range(num_segments):
        a1 = i * (2 * math.pi / num_segments) + 0.12
        a2 = (i + 1) * (2 * math.pi / num_segments) - 0.12
        mid = (a1 + a2) * 0.5
        p0 = (cx + math.cos(mid) * 4, cy + math.sin(mid) * 4)
        p1 = (cx + math.cos(a1) * seg_r, cy + math.sin(a1) * seg_r)
        p2 = (cx + math.cos(a2) * seg_r, cy + math.sin(a2) * seg_r)
        draw.polygon([p0, p1, p2], fill=chalk_color, outline=(255, 255, 220))

def draw_tea_glass(draw, x0, y0, w, h):
    """Draws a chalk illustration of iced lemon tea."""
    top_y = y0 + 35
    bot_y = y0 + h - 10
    top_w = w * 0.72
    bot_w = w * 0.48
    cx = x0 + w * 0.5
    
    # Glass outline
    p_tl = (cx - top_w * 0.5, top_y)
    p_tr = (cx + top_w * 0.5, top_y)
    p_br = (cx + bot_w * 0.5, bot_y)
    p_bl = (cx - bot_w * 0.5, bot_y)
    draw.polygon([p_tl, p_tr, p_br, p_bl], outline=(230, 240, 245), width=4)
    
    # Tea liquid fill
    tea_y = top_y + 30
    tea_tl = (cx - (top_w * 0.46), tea_y)
    tea_tr = (cx + (top_w * 0.46), tea_y)
    draw.polygon([tea_tl, tea_tr, (p_br[0] - 4, p_br[1] - 4), (p_bl[0] + 4, p_bl[1] - 4)], fill=(85, 62, 28))
    
    # Ice cubes floating
    draw.rounded_rectangle([cx - 28, tea_y + 20, cx + 12, tea_y + 55], radius=5, outline=(225, 245, 255), width=3)
    draw.rounded_rectangle([cx - 10, tea_y + 65, cx + 32, tea_y + 105], radius=5, outline=(225, 245, 255), width=3)
    draw.rounded_rectangle([cx - 32, tea_y + 115, cx + 8, tea_y + 150], radius=5, outline=(225, 245, 255), width=3)
    
    # Straw
    draw.line([(cx + 15, bot_y - 20), (cx + 38, top_y - 30)], fill=(255, 80, 80), width=6)
    # Lemon wheel on rim
    draw_lemon_slice(draw, int(cx - top_w * 0.5 + 10), int(top_y - 5), radius=32)

def draw_roll_cake(draw, x0, y0, w, h):
    """Draws a chalk illustration of hot baked roll cake."""
    cx = x0 + w * 0.5
    cy = y0 + h * 0.58
    
    # Three sliced roll cake rounds
    cake_r = 46
    offsets = [(-60, 20), (0, 0), (60, 20)]
    for ox, oy in offsets:
        kx, ky = cx + ox, cy + oy
        # Golden cake crust
        draw.ellipse([kx - cake_r, ky - cake_r * 0.75, kx + cake_r, ky + cake_r * 0.75], fill=(210, 145, 45), outline=(255, 235, 180), width=4)
        # Inner swirl / spiral
        draw.arc([kx - cake_r + 14, ky - cake_r * 0.55, kx + cake_r - 14, ky + cake_r * 0.55], start=45, end=320, fill=(120, 75, 25), width=5)
        draw.arc([kx - cake_r + 25, ky - cake_r * 0.38, kx + cake_r - 25, ky + cake_r * 0.38], start=90, end=270, fill=(75, 45, 15), width=4)
        # Scallion / chili flakes garnish
        draw.line([(kx - 10, ky - 5), (kx - 2, ky - 2)], fill=(90, 180, 60), width=3)
        draw.line([(kx + 6, ky + 4), (kx + 14, ky + 1)], fill=(235, 50, 40), width=3)
    
    # Steam swirls rising above
    for sx in [cx - 45, cx, cx + 45]:
        sy = cy - 65
        draw.arc([sx - 15, sy - 35, sx + 15, sy], start=180, end=360, fill=(235, 240, 245), width=3)
        draw.arc([sx - 15, sy - 65, sx + 15, sy - 30], start=0, end=180, fill=(235, 240, 245), width=3)

def generate_textures(out_dir):
    ensure_folder_meta(out_dir)
    print("Generating MenuBoard PBR textures (2048x1024)...")
    np.random.seed(2026)
    w, h = 2048, 1024
    
    # --------------------------------------------------------------------------
    # 1. BaseColor Texture
    # --------------------------------------------------------------------------
    im_base = Image.new("RGB", (w, h), (26, 31, 33))
    draw = ImageDraw.Draw(im_base)
    
    # (a) Wood frame dimensions: outer 60px top/bottom, 80px left/right
    frame_x = 80
    frame_y = 60
    
    # Procedural dark varnished timber for outer frame
    wood_arr = np.zeros((h, w, 3), dtype=np.uint8)
    yy, xx = np.meshgrid(np.arange(h), np.arange(w), indexing="ij")
    grain = np.sin(yy * 0.35 + np.sin(xx * 0.05) * 4.0) * 0.5 + 0.5
    fine = np.random.normal(0, 0.08, (h, w))
    wood_val = np.clip((0.35 * grain + 0.5 + fine) * 255, 0, 255)
    
    # Rich warm teak/walnut (#382214 to #54341E)
    wood_arr[..., 0] = np.clip(wood_val * 0.36 + 25, 20, 85).astype(np.uint8)
    wood_arr[..., 1] = np.clip(wood_val * 0.22 + 15, 14, 55).astype(np.uint8)
    wood_arr[..., 2] = np.clip(wood_val * 0.14 + 10, 8, 35).astype(np.uint8)
    
    # Chalkboard slate texture in center ([frame_y, h - frame_y], [frame_x, w - frame_x])
    slate_noise = np.random.normal(0, 0.04, (h - 2 * frame_y, w - 2 * frame_x))
    slate_val = np.clip((0.11 + slate_noise) * 255, 18, 45).astype(np.uint8)
    slate_r = np.clip(slate_val * 0.95, 16, 40).astype(np.uint8)
    slate_g = np.clip(slate_val * 1.10, 20, 48).astype(np.uint8)
    slate_b = np.clip(slate_val * 1.15, 22, 52).astype(np.uint8)
    slate_rgb = np.stack([slate_r, slate_g, slate_b], axis=-1)
    
    # Composite wood and slate
    wood_arr[frame_y:h - frame_y, frame_x:w - frame_x] = slate_rgb
    im_base = Image.fromarray(wood_arr)
    draw = ImageDraw.Draw(im_base)
    
    # Inner frame bevel / shadow
    draw.rectangle([frame_x, frame_y, w - frame_x, h - frame_y], outline=(14, 18, 19), width=5)
    draw.rectangle([frame_x + 5, frame_y + 5, w - frame_x - 5, h - frame_y - 5], outline=(90, 65, 42), width=2)
    
    # Corner brass brackets
    b_size = 90
    b_arm = 28
    corners = [
        (frame_x - 15, frame_y - 15, 1, 1),
        (w - frame_x + 15, frame_y - 15, -1, 1),
        (frame_x - 15, h - frame_y + 15, 1, -1),
        (w - frame_x + 15, h - frame_y + 15, -1, -1)
    ]
    for cx, cy, sx, sy in corners:
        p1 = (cx, cy)
        p2 = (cx + sx * b_size, cy)
        p3 = (cx + sx * b_size, cy + sy * b_arm)
        p4 = (cx + sx * b_arm, cy + sy * b_arm)
        p5 = (cx + sx * b_arm, cy + sy * b_size)
        p6 = (cx, cy + sy * b_size)
        draw.polygon([p1, p2, p3, p4, p5, p6], fill=(225, 175, 48), outline=(150, 110, 25))
        # Brass screw heads
        for rx, ry in [(cx + sx * 16, cy + sy * 48), (cx + sx * 48, cy + sy * 16)]:
            draw.ellipse([rx - 6, ry - 6, rx + 6, ry + 6], fill=(255, 220, 95), outline=(100, 75, 20))
            draw.line([(rx - 4, ry), (rx + 4, ry)], fill=(80, 55, 15), width=2)
            
    # Chalk eraser marks / smudges across the board
    for smudge_y in [310, 640]:
        for i in range(12):
            ang = i * 0.3
            sx = int(frame_x + 100 + i * 140 + math.sin(ang) * 40)
            sy = int(smudge_y + math.cos(ang) * 15)
            draw.ellipse([sx, sy, sx + 120, sy + 30], fill=(36, 45, 48))
            
    # Double chalk border lines
    cb_x0, cb_y0 = frame_x + 25, frame_y + 20
    cb_x1, cb_y1 = w - frame_x - 25, h - frame_y - 20
    draw.rounded_rectangle([cb_x0, cb_y0, cb_x1, cb_y1], radius=24, outline=(245, 242, 235), width=4)
    draw.rounded_rectangle([cb_x0 + 8, cb_y0 + 8, cb_x1 - 8, cb_y1 - 8], radius=18, outline=(180, 195, 190), width=2)
    
    # Corner chalk flourishes
    for fx, fy, f_ang in [(cb_x0 + 35, cb_y0 + 35, 0), (cb_x1 - 35, cb_y0 + 35, 90),
                          (cb_x1 - 35, cb_y1 - 35, 180), (cb_x0 + 35, cb_y1 - 35, 270)]:
        draw.ellipse([fx - 14, fy - 14, fx + 14, fy + 14], outline=(255, 245, 140), width=3)
        draw.point((fx, fy), fill=(255, 255, 255))
        
    # --------------------------------------------------------------------------
    # HEADER BANNER
    # --------------------------------------------------------------------------
    banner_w = 1560
    banner_h = 110
    bx0 = (w - banner_w) // 2
    by0 = frame_y + 45
    draw.rounded_rectangle([bx0, by0, bx0 + banner_w, by0 + banner_h], radius=20, fill=(35, 45, 46), outline=(255, 225, 45), width=4)
    
    # Mandatory string 1: 'TRẠM CHANH - Trà chanh & Bánh lăn hương'
    f_title = get_font(52, bold=True)
    title_text = "TRẠM CHANH - Trà chanh & Bánh lăn hương"
    t_box = draw.textbbox((0, 0), title_text, font=f_title)
    tw = t_box[2] - t_box[0]
    tx = (w - tw) // 2
    ty = by0 + 26
    # Drop shadow
    draw.text((tx + 3, ty + 3), title_text, font=f_title, fill=(150, 115, 15))
    # Main text in vibrant gold and lime
    draw.text((tx, ty), title_text, font=f_title, fill=(255, 235, 55))
    
    # Decorative divider under header
    div_y = by0 + banner_h + 22
    draw.line([bx0 + 60, div_y, w // 2 - 40, div_y], fill=(225, 235, 230), width=3)
    draw.line([w // 2 + 40, div_y, bx0 + banner_w - 60, div_y], fill=(225, 235, 230), width=3)
    draw_lemon_slice(draw, w // 2, div_y, radius=22)
    
    # --------------------------------------------------------------------------
    # LEFT COLUMN: TRÀ CHANH (BEVERAGE)
    # --------------------------------------------------------------------------
    col_w = 780
    left_x0 = frame_x + 60
    col_y0 = div_y + 35
    col_h = 360
    
    # Frame for Left Card
    draw.rounded_rectangle([left_x0, col_y0, left_x0 + col_w, col_y0 + col_h], radius=16, fill=(30, 38, 40), outline=(210, 225, 215), width=3)
    # Tea glass illustration
    draw_tea_glass(draw, left_x0 + 30, col_y0 + 35, 180, 290)
    
    # Mandatory string 2: 'Trà chanh 25.000đ'
    f_item_head = get_font(50, bold=True)
    draw.text((left_x0 + 235, col_y0 + 38), "Trà chanh 25.000đ", font=f_item_head, fill=(255, 255, 255))
    
    # Item description & subtitle
    f_desc = get_font(28, bold=False)
    draw.text((left_x0 + 235, col_y0 + 104), "Trà lài giã tay truyền thống", font=f_desc, fill=(185, 230, 170))
    draw.text((left_x0 + 235, col_y0 + 142), "Chua ngọt thanh mát, đã khát", font=f_desc, fill=(205, 215, 210))
    
    # Dedicated Price Badge
    badge_x = left_x0 + 235
    badge_y = col_y0 + 200
    draw.rounded_rectangle([badge_x, badge_y, badge_x + 320, badge_y + 70], radius=18, fill=(245, 180, 20), outline=(255, 255, 255), width=3)
    f_badge = get_font(42, bold=True)
    draw.text((badge_x + 35, badge_y + 12), "GIÁ: 25.000đ", font=f_badge, fill=(24, 30, 25))
    
    # Best seller tag
    f_tag = get_font(24, bold=True)
    draw.text((left_x0 + 235, col_y0 + 295), "★ BÁN CHẠY NHẤT • TOP 1 PHỐ ★", font=f_tag, fill=(255, 230, 80))
    
    # --------------------------------------------------------------------------
    # RIGHT COLUMN: BÁNH LĂN HƯƠNG (FOOD)
    # --------------------------------------------------------------------------
    right_x0 = w - frame_x - 60 - col_w
    draw.rounded_rectangle([right_x0, col_y0, right_x0 + col_w, col_y0 + col_h], radius=16, fill=(30, 38, 40), outline=(210, 225, 215), width=3)
    # Roll cake illustration
    draw_roll_cake(draw, right_x0 + 30, col_y0 + 35, 180, 290)
    
    # Mandatory string 3: 'Bánh lăn hương 20.000đ'
    draw.text((right_x0 + 235, col_y0 + 38), "Bánh lăn hương 20.000đ", font=f_item_head, fill=(255, 255, 255))
    
    draw.text((right_x0 + 235, col_y0 + 104), "Bánh cuộn nướng than hoa", font=f_desc, fill=(255, 205, 160))
    draw.text((right_x0 + 235, col_y0 + 142), "Vỏ giòn rụm, nhân đậm đà", font=f_desc, fill=(205, 215, 210))
    
    # Dedicated Price Badge
    draw.rounded_rectangle([right_x0 + 235, badge_y, right_x0 + 235 + 320, badge_y + 70], radius=18, fill=(230, 90, 25), outline=(255, 255, 255), width=3)
    draw.text((right_x0 + 235 + 35, badge_y + 12), "GIÁ: 20.000đ", font=f_badge, fill=(255, 255, 255))
    
    draw.text((right_x0 + 235, col_y0 + 295), "★ MỚI RA LÒ • NÓNG HỔI ★", font=f_tag, fill=(255, 160, 65))
    
    # --------------------------------------------------------------------------
    # CENTER COMBO BANNER (PROMO HIGHLIGHT)
    # --------------------------------------------------------------------------
    combo_y0 = col_y0 + col_h + 30
    combo_w = 1680
    combo_h = 105
    cx0 = (w - combo_w) // 2
    draw.rounded_rectangle([cx0, combo_y0, cx0 + combo_w, combo_y0 + combo_h], radius=18, fill=(45, 36, 20), outline=(255, 210, 45), width=4)
    
    # Combo ribbon tag
    draw.rounded_rectangle([cx0 + 25, combo_y0 + 18, cx0 + 250, combo_y0 + combo_h - 18], radius=12, fill=(215, 45, 30))
    f_combo_tag = get_font(30, bold=True)
    draw.text((cx0 + 42, combo_y0 + 33), "★ COMBO HOT", font=f_combo_tag, fill=(255, 255, 255))
    
    # Mandatory string 4: 'Combo trà + bánh 40.500đ'
    f_combo = get_font(48, bold=True)
    combo_text = "Combo trà + bánh 40.500đ"
    draw.text((cx0 + 285, combo_y0 + 25), combo_text, font=f_combo, fill=(255, 235, 65))
    
    f_combo_sub = get_font(26, bold=False)
    draw.text((cx0 + 1040, combo_y0 + 37), "(Tiết kiệm 4.500đ • Trọn vị vỉa hè)", font=f_combo_sub, fill=(160, 240, 150))
    
    # --------------------------------------------------------------------------
    # FOOTER: OPENING HOURS & STREET STALL MOTTO
    # --------------------------------------------------------------------------
    hours_y = combo_y0 + combo_h + 30
    # Mandatory string 5: 'Mở cửa 18:00 - 24:00'
    f_hours = get_font(42, bold=True)
    hours_text = "Mở cửa 18:00 - 24:00"
    h_box = draw.textbbox((0, 0), hours_text, font=f_hours)
    hw = h_box[2] - h_box[0]
    hx = (w - hw) // 2
    
    # Small badge behind hours
    draw.rounded_rectangle([hx - 50, hours_y - 6, hx + hw + 50, hours_y + 50], radius=14, fill=(28, 36, 38), outline=(255, 235, 90), width=2)
    draw.text((hx - 36, hours_y + 3), "✦", font=f_hours, fill=(255, 220, 40))
    draw.text((hx, hours_y + 2), hours_text, font=f_hours, fill=(255, 248, 140))
    draw.text((hx + hw + 14, hours_y + 3), "✦", font=f_hours, fill=(255, 220, 40))
    
    # Save BaseColor
    tex_base_path = os.path.join(out_dir, "T_MenuBoard_Prices.png")
    im_base.save(tex_base_path, "PNG")
    print(f"Generated BaseColor: {tex_base_path}")
    
    meta_base = tex_base_path + ".meta"
    guid_base = get_or_create_guid(meta_base)
    with open(meta_base, "w", encoding="utf-8") as f:
        f.write(f"""fileFormatVersion: 2
guid: {guid_base}
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
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 1
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
  alphaIsTransparency: 0
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: 0
  textureShape: 1
""")

    # --------------------------------------------------------------------------
    # 2. Normal Map & Height Map with Rain Streaks & Water Droplets
    # --------------------------------------------------------------------------
    height_map = np.full((h, w), 80, dtype=np.uint8)
    
    # Outer wood frame elevated (+80 height)
    height_map[:frame_y, :] = 160
    height_map[h - frame_y:, :] = 160
    height_map[:, :frame_x] = 160
    height_map[:, w - frame_x:] = 160
    
    # Brass corner plates elevated (+40 height)
    for cx, cy, sx, sy in corners:
        xmin = min(cx, cx + sx * b_size)
        xmax = max(cx, cx + sx * b_size)
        ymin = min(cy, cy + sy * b_size)
        ymax = max(cy, cy + sy * b_size)
        height_map[ymin:ymax, xmin:xmax] = 200
        
    # Embossed chalk lettering and decorative borders (from luminance of text/borders)
    base_gray = np.array(im_base.convert("L"))
    chalk_mask = (base_gray > 110) & (xx > frame_x) & (xx < w - frame_x) & (yy > frame_y) & (yy < h - frame_y)
    height_map[chalk_mask] = np.clip(height_map[chalk_mask] + 45, 0, 255)
    
    # Vertical rain streaks and beaded droplets (subtle procedural rivulets)
    streak_mask = np.zeros((h, w), dtype=float)
    for s in range(40):
        sx = int(frame_x + 30 + (s * 47) % (w - 2 * frame_x - 60))
        sy_start = int(frame_y + 40 + (s * 83) % (h // 2))
        s_len = int(120 + (s * 31) % 250)
        s_width = 2 if s % 3 == 0 else 1
        for y_cur in range(sy_start, min(h - frame_y - 20, sy_start + s_len)):
            for xw in range(-s_width, s_width + 1):
                streak_mask[y_cur, sx + xw] = max(streak_mask[y_cur, sx + xw], 0.7)
        # Droplet at bottom of streak
        d_rad = 3 + s % 3
        d_cy = min(h - frame_y - 20, sy_start + s_len)
        y_slice = slice(max(0, d_cy - d_rad), min(h, d_cy + d_rad + 1))
        x_slice = slice(max(0, sx - d_rad), min(w, sx + d_rad + 1))
        streak_mask[y_slice, x_slice] = 1.0
        
    height_map = np.clip(height_map.astype(float) + streak_mask * 40.0, 0, 255).astype(np.uint8)
    normal_arr = compute_normal_map(height_map, strength=2.0)
    
    tex_normal_path = os.path.join(out_dir, "T_MenuBoard_Prices_Normal.png")
    Image.fromarray(normal_arr).save(tex_normal_path, "PNG")
    print(f"Generated Normal: {tex_normal_path}")
    
    meta_normal = tex_normal_path + ".meta"
    guid_normal = get_or_create_guid(meta_normal)
    with open(meta_normal, "w", encoding="utf-8") as f:
        f.write(f"""fileFormatVersion: 2
guid: {guid_normal}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 0
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
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 1
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
  alphaIsTransparency: 0
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: 1
  textureShape: 1
""")

    # --------------------------------------------------------------------------
    # 3. MaskMap: R=Metallic, G=AO, B=Detail/Rain, A=Smoothness
    # --------------------------------------------------------------------------
    r_chan = np.zeros((h, w), dtype=np.uint8)
    g_chan = np.full((h, w), 255, dtype=np.uint8)
    b_chan = np.clip(streak_mask * 255, 0, 255).astype(np.uint8)
    a_chan = np.full((h, w), 50, dtype=np.uint8)  # Dry chalkboard matte (smoothness ~0.2)
    
    # Varnished wood frame: smoothness ~0.5
    frame_mask = (xx < frame_x) | (xx > w - frame_x) | (yy < frame_y) | (yy > h - frame_y)
    a_chan[frame_mask] = 125
    
    # Inner bevel crevice AO
    g_chan[yy == frame_y] = 120
    g_chan[yy == h - frame_y] = 120
    g_chan[xx == frame_x] = 120
    g_chan[xx == w - frame_x] = 120
    
    # Brass plates: metallic = 0.82, smoothness = 0.85
    for cx, cy, sx, sy in corners:
        xmin = min(cx, cx + sx * b_size)
        xmax = max(cx, cx + sx * b_size)
        ymin = min(cy, cy + sy * b_size)
        ymax = max(cy, cy + sy * b_size)
        r_chan[ymin:ymax, xmin:xmax] = 210
        a_chan[ymin:ymax, xmin:xmax] = 220
        
    # Wet droplets & streaks: smoothness = 0.98
    wet_pixels = streak_mask > 0.2
    a_chan[wet_pixels] = 250
    
    mask_arr = np.stack([r_chan, g_chan, b_chan, a_chan], axis=-1)
    tex_mask_path = os.path.join(out_dir, "T_MenuBoard_Prices_MaskMap.png")
    Image.fromarray(mask_arr, mode="RGBA").save(tex_mask_path, "PNG")
    print(f"Generated MaskMap: {tex_mask_path}")
    
    meta_mask = tex_mask_path + ".meta"
    guid_mask = get_or_create_guid(meta_mask)
    with open(meta_mask, "w", encoding="utf-8") as f:
        f.write(f"""fileFormatVersion: 2
guid: {guid_mask}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 0
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
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 1
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
  alphaIsTransparency: 0
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: 0
  textureShape: 1
""")

    return guid_base, guid_normal, guid_mask

# ==============================================================================
# 2. 3D MODEL GENERATION (OBJ)
# ==============================================================================

class ObjMeshBuilder:
    def __init__(self, name):
        self.name = name
        self.vertices = []
        self.uvs = []
        self.triangles = []  # list of ((v0, vt0), (v1, vt1), (v2, vt2))

    def add_vert(self, x, y, z):
        self.vertices.append((x, y, z))
        return len(self.vertices)  # 1-based index

    def add_uv(self, u, v):
        self.uvs.append((u, v))
        return len(self.uvs)

    def add_box(self, x1, x2, y1, y2, z1, z2, uv_front, uv_side=None):
        """Adds a cuboid with outward-pointing normals and split triangles."""
        v = [
            self.add_vert(x1, y1, z1),  # 0
            self.add_vert(x2, y1, z1),  # 1
            self.add_vert(x2, y2, z1),  # 2
            self.add_vert(x1, y2, z1),  # 3
            self.add_vert(x1, y1, z2),  # 4
            self.add_vert(x2, y1, z2),  # 5
            self.add_vert(x2, y2, z2),  # 6
            self.add_vert(x1, y2, z2),  # 7
        ]
        
        uf = uv_front
        us = uv_side if uv_side else uv_front
        
        tf0 = self.add_uv(uf[0], uf[2])
        tf1 = self.add_uv(uf[1], uf[2])
        tf2 = self.add_uv(uf[1], uf[3])
        tf3 = self.add_uv(uf[0], uf[3])
        
        ts0 = self.add_uv(us[0], us[2])
        ts1 = self.add_uv(us[1], us[2])
        ts2 = self.add_uv(us[1], us[3])
        ts3 = self.add_uv(us[0], us[3])
        
        # 6 Quads -> 12 Triangles
        # Front (+Z): 4, 5, 6, 7
        self.triangles.append(((v[4], tf0), (v[5], tf1), (v[6], tf2)))
        self.triangles.append(((v[4], tf0), (v[6], tf2), (v[7], tf3)))
        # Back (-Z): 1, 0, 3, 2
        self.triangles.append(((v[1], ts1), (v[0], ts0), (v[3], ts3)))
        self.triangles.append(((v[1], ts1), (v[3], ts3), (v[2], ts2)))
        # Left (-X): 0, 4, 7, 3
        self.triangles.append(((v[0], ts0), (v[4], ts1), (v[7], ts2)))
        self.triangles.append(((v[0], ts0), (v[7], ts2), (v[3], ts3)))
        # Right (+X): 5, 1, 2, 6
        self.triangles.append(((v[5], ts0), (v[1], ts1), (v[2], ts2)))
        self.triangles.append(((v[5], ts0), (v[2], ts2), (v[6], ts3)))
        # Top (+Y): 7, 6, 2, 3
        self.triangles.append(((v[7], ts0), (v[6], ts1), (v[2], ts2)))
        self.triangles.append(((v[7], ts0), (v[2], ts2), (v[3], ts3)))
        # Bottom (-Y): 4, 0, 1, 5
        self.triangles.append(((v[4], ts0), (v[0], ts1), (v[1], ts2)))
        self.triangles.append(((v[4], ts0), (v[1], ts2), (v[5], ts3)))

    def write(self, path):
        with open(path, "w", encoding="utf-8") as f:
            f.write(f"# {self.name}.obj - Trạm Chanh Art Contract CC0 1.0 Universal\n")
            f.write(f"usemtl MAT_MenuBoard_Prices\n")
            f.write(f"o {self.name}\n")
            for v in self.vertices:
                f.write(f"v {v[0]:.4f} {v[1]:.4f} {v[2]:.4f}\n")
            for vt in self.uvs:
                f.write(f"vt {vt[0]:.4f} {vt[1]:.4f}\n")
            for t in self.triangles:
                f.write(f"f {t[0][0]}/{t[0][1]} {t[1][0]}/{t[1][1]} {t[2][0]}/{t[2][1]}\n")
        print(f"Generated OBJ mesh: {path} ({len(self.triangles)} triangles, {len(self.vertices)} vertices)")

def generate_mesh(out_dir):
    ensure_folder_meta(out_dir)
    mesh_path = os.path.join(out_dir, "SM_MenuBoard_Prices.obj")
    
    # UV Sub-rectangles: (u_min, u_max, v_min, v_max)
    uv_chalk_front = (0.04, 0.96, 0.06, 0.94)
    uv_wood_edge    = (0.01, 0.07, 0.01, 0.07)
    uv_wood_rail_h  = (0.01, 0.99, 0.94, 1.00)
    uv_wood_rail_b  = (0.01, 0.99, 0.00, 0.06)
    uv_wood_stile_l = (0.00, 0.04, 0.06, 0.94)
    uv_wood_stile_r = (0.96, 1.00, 0.06, 0.94)
    uv_brass        = (0.01, 0.06, 0.93, 0.99)
    
    m = ObjMeshBuilder("SM_MenuBoard_Prices")
    
    # 1. OUTER FRAME (Width = 0.90m, Height = 0.60m, Depth = 0.03m)
    # Top rail (y: 0.56 to 0.60, x: -0.45 to 0.45, z: -0.015 to 0.015)
    m.add_box(-0.45, 0.45, 0.56, 0.60, -0.015, 0.015, uv_wood_rail_h, uv_wood_edge)
    # Bottom rail (y: 0.00 to 0.04, x: -0.45 to 0.45)
    m.add_box(-0.45, 0.45, 0.00, 0.04, -0.015, 0.015, uv_wood_rail_b, uv_wood_edge)
    # Left stile (x: -0.45 to -0.41, y: 0.04 to 0.56)
    m.add_box(-0.45, -0.41, 0.04, 0.56, -0.015, 0.015, uv_wood_stile_l, uv_wood_edge)
    # Right stile (x: 0.41 to 0.45, y: 0.04 to 0.56)
    m.add_box(0.41, 0.45, 0.04, 0.56, -0.015, 0.015, uv_wood_stile_r, uv_wood_edge)
    
    # 2. INNER CHALKBOARD PANEL (inset 7mm from frame faces)
    # Panel (x: -0.41 to 0.41, y: 0.04 to 0.56, z: -0.008 to 0.008)
    m.add_box(-0.41, 0.41, 0.04, 0.56, -0.008, 0.008, uv_chalk_front, uv_wood_edge)
    
    # 3. BASE STABILIZER FEET (sitting on Y = 0.000)
    # Left foot: x: -0.38 to -0.32, y: 0.00 to 0.02, z: -0.05 to 0.05
    m.add_box(-0.38, -0.32, 0.00, 0.02, -0.05, 0.05, uv_wood_edge, uv_wood_edge)
    # Right foot: x: 0.32 to 0.38, y: 0.00 to 0.02, z: -0.05 to 0.05
    m.add_box(0.32, 0.38, 0.00, 0.02, -0.05, 0.05, uv_wood_edge, uv_wood_edge)
    
    # 4. TOP HANGING EYELETS / MOUNTING BRACKETS
    # Left eyelet base bracket: x: -0.315 to -0.285, y: 0.585 to 0.605, z: -0.016 to 0.016
    m.add_box(-0.315, -0.285, 0.585, 0.605, -0.016, 0.016, uv_brass, uv_brass)
    # Left eyelet ring loop: x: -0.315 to -0.285, y: 0.605 to 0.635, z: -0.004 to 0.004
    m.add_box(-0.315, -0.285, 0.605, 0.635, -0.004, 0.004, uv_brass, uv_brass)
    # Right eyelet base bracket: x: 0.285 to 0.315, y: 0.585 to 0.605, z: -0.016 to 0.016
    m.add_box(0.285, 0.315, 0.585, 0.605, -0.016, 0.016, uv_brass, uv_brass)
    # Right eyelet ring loop: x: 0.285 to 0.315, y: 0.605 to 0.635, z: -0.004 to 0.004
    m.add_box(0.285, 0.315, 0.605, 0.635, -0.004, 0.004, uv_brass, uv_brass)
    
    m.write(mesh_path)
    
    meta_mesh = mesh_path + ".meta"
    guid_mesh = get_or_create_guid(meta_mesh)
    with open(meta_mesh, "w", encoding="utf-8") as f:
        f.write(f"""fileFormatVersion: 2
guid: {guid_mesh}
ModelImporter:
  serializedVersion: 21300
  internalIDToNameTable: []
  externalObjects: {{}}
  materials:
    materialImportMode: 1
    materialName: 0
    materialSearch: 1
  globalScale: 1
  addCollider: 0
""")
    print(f"Generated Model meta: {meta_mesh}")
    return guid_mesh, len(m.triangles)

# ==============================================================================
# 3. MATERIALS GENERATION
# ==============================================================================

def generate_materials(mat_dir, guid_base, guid_normal, guid_mask):
    ensure_folder_meta(mat_dir)
    urp_lit_shader_guid = "933532a4fcc9baf4fa0491de14d08ed7"
    
    # 1. MAT_MenuBoard_Prices.mat (Dry Version)
    mat_dry_path = os.path.join(mat_dir, "MAT_MenuBoard_Prices.mat")
    meta_dry = mat_dry_path + ".meta"
    guid_mat_dry = get_or_create_guid(meta_dry)
    
    with open(mat_dry_path, "w", encoding="utf-8") as f:
        f.write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: MAT_MenuBoard_Prices
  m_Shader: {{fileID: 4800000, guid: {urp_lit_shader_guid}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords:
  - _METALLICSPECGLOSSMAP
  - _NORMALMAP
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap:
    RenderType: Opaque
  disabledShaderPasses:
  - MOTIONVECTORS
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {guid_base}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _BumpMap:
        m_Texture: {{fileID: 2800000, guid: {guid_normal}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MetallicGlossMap:
        m_Texture: {{fileID: 2800000, guid: {guid_mask}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {guid_base}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Floats:
    - _BumpScale: 1
    - _Metallic: 0.05
    - _Smoothness: 0.35
    - _Surface: 0
    m_Colors:
    - _BaseColor: {{r: 1, g: 1, b: 1, a: 1}}
    - _Color: {{r: 1, g: 1, b: 1, a: 1}}
""")
    with open(meta_dry, "w", encoding="utf-8") as f:
        f.write(f"""fileFormatVersion: 2
guid: {guid_mat_dry}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 2100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")
    print(f"Generated Dry Material: {mat_dry_path}")
    
    # 2. MAT_MenuBoard_Prices_Wet.mat (Wet Rainy Weather Version)
    mat_wet_path = os.path.join(mat_dir, "MAT_MenuBoard_Prices_Wet.mat")
    meta_wet = mat_wet_path + ".meta"
    guid_mat_wet = get_or_create_guid(meta_wet)
    
    with open(mat_wet_path, "w", encoding="utf-8") as f:
        f.write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: MAT_MenuBoard_Prices_Wet
  m_Shader: {{fileID: 4800000, guid: {urp_lit_shader_guid}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords:
  - _METALLICSPECGLOSSMAP
  - _NORMALMAP
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap:
    RenderType: Opaque
  disabledShaderPasses:
  - MOTIONVECTORS
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {guid_base}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _BumpMap:
        m_Texture: {{fileID: 2800000, guid: {guid_normal}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MetallicGlossMap:
        m_Texture: {{fileID: 2800000, guid: {guid_mask}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {guid_base}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Floats:
    - _BumpScale: 1.2
    - _Metallic: 0.15
    - _Smoothness: 0.92
    - _Surface: 0
    m_Colors:
    - _BaseColor: {{r: 0.82, g: 0.85, b: 0.90, a: 1}}
    - _Color: {{r: 0.82, g: 0.85, b: 0.90, a: 1}}
""")
    with open(meta_wet, "w", encoding="utf-8") as f:
        f.write(f"""fileFormatVersion: 2
guid: {guid_mat_wet}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 2100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")
    print(f"Generated Wet Material: {mat_wet_path}")
    
    return guid_mat_dry, guid_mat_wet

# ==============================================================================
# 4. PREFABS GENERATION
# ==============================================================================

def generate_prefabs(pf_dir, guid_mesh, guid_mat_dry, guid_mat_wet):
    ensure_folder_meta(pf_dir)
    
    prefabs = [
        ("PF_MenuBoard_Prices", guid_mat_dry),
        ("PF_MenuBoard_Prices_Wet", guid_mat_wet)
    ]
    
    for pf_name, mat_guid in prefabs:
        pf_path = os.path.join(pf_dir, f"{pf_name}.prefab")
        meta_path = pf_path + ".meta"
        pf_guid = get_or_create_guid(meta_path)
        
        with open(pf_path, "w", encoding="utf-8") as f:
            f.write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &3670817710514171788
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 5110386587436877682}}
  - component: {{fileID: 4700595132476312954}}
  - component: {{fileID: 6571542907280224375}}
  - component: {{fileID: 8789350620605570058}}
  m_Layer: 8
  m_Name: {pf_name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &5110386587436877682
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3670817710514171788}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!33 &4700595132476312954
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3670817710514171788}}
  m_Mesh: {{fileID: -2432090755550338912, guid: {guid_mesh}, type: 3}}
--- !u!23 &6571542907280224375
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3670817710514171788}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: {mat_guid}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
--- !u!65 &8789350620605570058
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3670817710514171788}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {{x: 0.90, y: 0.60, z: 0.08}}
  m_Center: {{x: 0.00, y: 0.30, z: 0.00}}
""")
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(f"""fileFormatVersion: 2
guid: {pf_guid}
PrefabImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")
        print(f"Generated Prefab: {pf_path}")

# ==============================================================================
# MAIN ENTRY POINT
# ==============================================================================

def main():
    print("=== ART-FIX-028: GENERATING MENU AND PRICE BOARD ASSETS ===")
    
    tex_dir = os.path.join(REPO_ROOT, "Assets", "TramChanh", "Art", "Textures", "Props")
    model_dir = os.path.join(REPO_ROOT, "Assets", "TramChanh", "Art", "Models", "Props")
    mat_dir = os.path.join(REPO_ROOT, "Assets", "TramChanh", "Art", "Materials", "Props")
    pf_dir = os.path.join(REPO_ROOT, "Assets", "TramChanh", "Prefabs", "Props")
    
    guid_base, guid_normal, guid_mask = generate_textures(tex_dir)
    guid_mesh, tri_count = generate_mesh(model_dir)
    guid_mat_dry, guid_mat_wet = generate_materials(mat_dir, guid_base, guid_normal, guid_mask)
    generate_prefabs(pf_dir, guid_mesh, guid_mat_dry, guid_mat_wet)
    
    print(f"\nAsset Generation Complete:")
    print(f"- 3D Mesh Triangles: {tri_count} (< 200 budget)")
    print(f"- BaseColor: {os.path.join(tex_dir, 'T_MenuBoard_Prices.png')}")
    print(f"- Materials: Dry & Wet generated in {mat_dir}")
    print(f"- Prefabs: Dry & Wet generated in {pf_dir}")
    print("=== DONE ===")

if __name__ == "__main__":
    main()
