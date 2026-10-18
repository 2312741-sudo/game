#!/usr/bin/env python3
"""
ArtSource/Scripts/upgrade_street_pavement_textures.py

Upgrades sidewalk pavers and asphalt roadway textures for ART-FIX-022 Task 6:
- T_Sidewalk_Tiles_BaseColor.png (2048x2048):
  - Vietnamese sidewalk terrazzo pavers with concentric border reliefs
  - Multi-tone pavers (warm yellow ochre, buff limestone, pale terracotta)
  - Fine quartz, basalt, and jasper aggregate flecks
  - Clean recessed mortar joints and perimeter drainage seams
- T_Asphalt_Night_BaseColor.png & T_Asphalt_Night_MaskMap.png (2048x2048):
  - Marked roadway with solid white curb edge lines
  - Dashed yellow traffic divider centerline (3m dash, 2m gap)
  - Cast-iron storm drain grates (cống thoát nước) along both curbs
  - Wet puddles (vết ướt) with darker soaked tarmac and ultra-smooth specular sheen
  - Zero heavy geometry added (100% texture-based)
"""

import os
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

def generate_sidewalk_tiles(out_path="Assets/TramChanh/Art/Textures/T_Sidewalk_Tiles_BaseColor.png"):
    w, h = 2048, 2048
    # 4x4 large tiles per repeat (each 512x512 pixels), representing ~33x33cm paver blocks
    tile_size = 512
    joint_w = 12

    img = Image.new("RGB", (w, h), (135, 132, 128)) # Dark mortar joint color
    draw = ImageDraw.Draw(img)

    np.random.seed(42)

    # Base paver palette: Authentic Vietnamese urban terrazzo colors
    palette = [
        np.array([205, 198, 185], dtype=float), # Buff limestone
        np.array([212, 192, 162], dtype=float), # Warm yellow ochre terrazzo
        np.array([195, 188, 178], dtype=float), # Cool grey terrazzo
        np.array([208, 182, 168], dtype=float), # Pale terracotta terrazzo
    ]

    tile_arr = np.array(img).astype(float)

    for ty in range(4):
        for tx in range(4):
            x0 = tx * tile_size + joint_w // 2
            y0 = ty * tile_size + joint_w // 2
            x1 = (tx + 1) * tile_size - joint_w // 2
            y1 = (ty + 1) * tile_size - joint_w // 2

            # Select palette based on checker/grid pattern
            p_idx = (tx + ty * 2) % len(palette)
            base_col = palette[p_idx].copy()
            # Slight per-tile tone variation
            base_col += np.random.uniform(-6, 6, 3)

            # Fill paver body
            tile_arr[y0:y1, x0:x1] = base_col

            # Inner concentric embossed border (classic VN paver style)
            inset1 = 28
            border1_col = base_col * 0.94 # slightly darker recessed groove
            tile_arr[y0+inset1:y0+inset1+8, x0+inset1:x1-inset1] = border1_col
            tile_arr[y1-inset1-8:y1-inset1, x0+inset1:x1-inset1] = border1_col
            tile_arr[y0+inset1:y1-inset1, x0+inset1:x0+inset1+8] = border1_col
            tile_arr[y0+inset1:y1-inset1, x1-inset1-8:x1-inset1] = border1_col

            # Center medallion square
            inset2 = 140
            medallion_col = base_col * 1.04 # slightly raised lighter center
            tile_arr[y0+inset2:y1-inset2, x0+inset2:x1-inset2] = medallion_col
            # Medallion inner groove
            med_border = medallion_col * 0.93
            tile_arr[y0+inset2:y0+inset2+6, x0+inset2:x1-inset2] = med_border
            tile_arr[y1-inset2-6:y1-inset2, x0+inset2:x1-inset2] = med_border
            tile_arr[y0+inset2:y1-inset2, x0+inset2:x0+inset2+6] = med_border
            tile_arr[y0+inset2:y1-inset2, x1-inset2-6:x1-inset2] = med_border

    # Add fine cement grain noise
    cement_noise = np.random.normal(0, 5, (h, w, 3))
    tile_arr = np.clip(tile_arr + cement_noise, 0, 255)

    # Add aggregate flecks (terrazzo chips: white quartz, red jasper, black basalt)
    num_chips = 16000
    for _ in range(num_chips):
        cx = np.random.randint(0, w)
        cy = np.random.randint(0, h)
        chip_type = np.random.choice([0, 1, 2], p=[0.55, 0.25, 0.20])
        if chip_type == 0:
            c = [240, 238, 232] # White quartz
        elif chip_type == 1:
            c = [165, 82, 65]   # Red jasper
        else:
            c = [50, 52, 58]    # Black basalt
        r = np.random.randint(1, 4)
        y_min = max(0, cy - r)
        y_max = min(h, cy + r + 1)
        x_min = max(0, cx - r)
        x_max = min(w, cx + r + 1)
        tile_arr[y_min:y_max, x_min:x_max] = c

    # Convert back to image
    res_img = Image.fromarray(tile_arr.astype(np.uint8))
    res_img.save(out_path, "PNG")
    print(f"Generated enhanced sidewalk texture: {out_path}")


def generate_asphalt_and_mask(
    color_path="Assets/TramChanh/Art/Textures/T_Asphalt_Night_BaseColor.png",
    mask_path="Assets/TramChanh/Art/Textures/T_Asphalt_Night_MaskMap.png"
):
    w, h = 2048, 2048
    np.random.seed(101)

    # 1. Base dark tarmac
    # Base color around RGB(34, 35, 38)
    base_tarmac = np.zeros((h, w, 3), dtype=float)
    base_tarmac[:, :, 0] = 33.0
    base_tarmac[:, :, 1] = 34.0
    base_tarmac[:, :, 2] = 37.0

    # Aggregate stone noise
    agg = np.random.normal(0, 6.0, (h, w, 3))
    base_tarmac = np.clip(base_tarmac + agg, 0, 255)

    # Mask map channels: R=Metallic, G=AO, B=Detail, A=Smoothness
    # Default dry asphalt: Metallic=0, AO=255, Detail=0, Smoothness=70 (matte rough ~0.27)
    mask = np.zeros((h, w, 4), dtype=np.uint8)
    mask[:, :, 0] = 5    # almost non-metallic
    mask[:, :, 1] = 255  # full AO
    mask[:, :, 2] = 0    # detail
    mask[:, :, 3] = 72   # dry asphalt smoothness

    # 2. Road Markings (Vạch kẻ đường)
    # A. Solid white curb lines at Y=160..195 and Y=1850..1885
    # White paint: RGB(225, 228, 230), smoothness=110
    white_col = np.array([225, 228, 230], dtype=float)
    # South curb white line
    base_tarmac[160:195, :] = white_col + np.random.normal(0, 8, (35, w, 3))
    mask[160:195, :, 3] = 115
    # North curb white line
    base_tarmac[1850:1885, :] = white_col + np.random.normal(0, 8, (35, w, 3))
    mask[1850:1885, :, 3] = 115

    # B. Yellow center divider dashed line (Vạch phân làn đứt nét chuẩn VN)
    # Center is at Y=1000..1048. Dash length: X in [0..1228] (3m out of 5m repeat), Gap: [1228..2048] (2m)
    yellow_col = np.array([235, 192, 35], dtype=float)
    dash_len = 1228
    base_tarmac[1000:1048, 0:dash_len] = yellow_col + np.random.normal(0, 7, (48, dash_len, 3))
    mask[1000:1048, 0:dash_len, 3] = 125

    # 3. Cast Iron Storm Drain Grates (Cống thoát nước)
    # Located along south curb gutter: Y in [30..130], X in [400..880] (grate length ~1.2m)
    # And north curb gutter: Y in [1918..2018], X in [1400..1880]
    def stamp_grate(y_start, y_end, x_start, x_end):
        grate_h = y_end - y_start
        grate_w = x_end - x_start
        # Cast iron frame (dark iron steel RGB(45, 47, 50), Metallic=140, Smoothness=140)
        iron_col = np.array([48, 50, 54], dtype=float)
        base_tarmac[y_start:y_end, x_start:x_end] = iron_col
        mask[y_start:y_end, x_start:x_end, 0] = 145 # metallic iron
        mask[y_start:y_end, x_start:x_end, 3] = 135 # cast iron smoothness
        mask[y_start:y_end, x_start:x_end, 1] = 220 # AO

        # Slotted drain openings (bars every 36 pixels)
        for gx in range(x_start + 25, x_end - 25, 36):
            base_tarmac[y_start+15:y_end-15, gx:gx+14] = [12, 13, 15] # deep dark void slot
            mask[y_start+15:y_end-15, gx:gx+14, 0] = 0
            mask[y_start+15:y_end-15, gx:gx+14, 1] = 30 # deep AO occlusion shadow
            mask[y_start+15:y_end-15, gx:gx+14, 3] = 20

    stamp_grate(30, 130, 400, 880)
    stamp_grate(1918, 2018, 1400, 1880)

    # 4. Wet Puddles (Vết ướt)
    # Generate smooth organic puddle masks using radial falloffs
    puddle_mask = np.zeros((h, w), dtype=float)

    puddle_centers = [
        # South lane puddles
        (450, 600, 280, 160),
        (550, 1450, 320, 190),
        # Gutter puddles near drain
        (100, 640, 150, 70),
        (1960, 1640, 160, 75),
        # North lane puddles
        (1400, 400, 340, 200),
        (1500, 1200, 300, 170),
        (1600, 1800, 260, 150),
    ]

    for cy, cx, rx, ry in puddle_centers:
        y_grid, x_grid = np.ogrid[-cy:h-cy, -cx:w-cx]
        # Distort coordinates with harmonic perturbations for natural organic water contours
        angle = np.arctan2(y_grid, x_grid)
        dist = np.sqrt((x_grid / rx)**2 + (y_grid / ry)**2)
        dist += 0.15 * np.sin(4 * angle) + 0.10 * np.cos(6 * angle)
        p = np.clip(1.0 - dist, 0.0, 1.0)
        p = p ** 1.5 # smooth falloff
        puddle_mask = np.maximum(puddle_mask, p)

    # Apply wet puddle darkening to base color (wet asphalt looks significantly darker and richer)
    puddle_3d = puddle_mask[:, :, np.newaxis]
    wet_darken_factor = 1.0 - (0.55 * puddle_3d) # up to 55% darker
    base_tarmac = base_tarmac * wet_darken_factor

    # In wet puddles, smoothness goes up to 245 (mirror water reflection: 0.96)
    dry_smooth = mask[:, :, 3].astype(float)
    wet_smooth = dry_smooth * (1.0 - puddle_mask) + 245.0 * puddle_mask
    mask[:, :, 3] = np.clip(wet_smooth, 0, 255).astype(np.uint8)

    # Save outputs
    res_color = Image.fromarray(np.clip(base_tarmac, 0, 255).astype(np.uint8))
    res_color.save(color_path, "PNG")
    print(f"Generated enhanced asphalt color: {color_path}")

    res_mask = Image.fromarray(mask, "RGBA")
    res_mask.save(mask_path, "PNG")
    print(f"Generated enhanced asphalt mask map: {mask_path}")

def main():
    print("=== UPGRADING STREET PAVEMENT TEXTURES (ART-FIX-022 TASK 6) ===")
    generate_sidewalk_tiles()
    generate_asphalt_and_mask()
    print("=== STREET PAVEMENT TEXTURES UPGRADED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
