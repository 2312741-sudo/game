#!/usr/bin/env python3
"""
Procedural PBR Texture Generator for Tram Chanh Street Environment & Furniture (Agent E)
Generates:
- T_Plastic_Beige_BaseColor.png (Beige stool from IMG_5228.JPG)
- T_Crate_Yellow_BaseColor.png (Yellow beverage crate table from IMG_5231.JPG)
- T_Sidewalk_Tiles_BaseColor.png & Normal (Vietnamese terrazzo sidewalk tiles)
- T_Asphalt_Night_BaseColor.png & MaskMap (Wet nocturnal street asphalt)
"""

import os
import math
from PIL import Image, ImageDraw, ImageFilter
import numpy as np

def generate_env_textures(output_dir="Assets/TramChanh/Art/Textures"):
    os.makedirs(output_dir, exist_ok=True)
    
    # 1. Beige Plastic Stool BaseColor (512x512)
    stool_w, stool_h = 512, 512
    stool_arr = np.zeros((stool_h, stool_w, 3), dtype=np.uint8)
    # Authentic Vietnamese beige plastic (#D6C8AC)
    stool_arr[:, :, 0] = 214
    stool_arr[:, :, 1] = 200
    stool_arr[:, :, 2] = 172
    # Polypropylene matte grain
    np.random.seed(77)
    grain = np.random.randint(-6, 7, (stool_h, stool_w))
    for c in range(3):
        stool_arr[:, :, c] = np.clip(stool_arr[:, :, c].astype(int) + grain, 0, 255).astype(np.uint8)
    stool_img = Image.fromarray(stool_arr)
    stool_path = os.path.join(output_dir, "T_Plastic_Beige_BaseColor.png")
    stool_img.save(stool_path, "PNG")
    print(f"Generated: {stool_path}")

    # 2. Yellow Crate Plastic BaseColor (512x512)
    crate_w, crate_h = 512, 512
    crate_arr = np.zeros((crate_h, crate_w, 3), dtype=np.uint8)
    # Bright commercial crate yellow (#E5B017)
    crate_arr[:, :, 0] = 229
    crate_arr[:, :, 1] = 176
    crate_arr[:, :, 2] = 23
    np.random.seed(88)
    grain_crate = np.random.randint(-7, 8, (crate_h, crate_w))
    for c in range(3):
        crate_arr[:, :, c] = np.clip(crate_arr[:, :, c].astype(int) + grain_crate, 0, 255).astype(np.uint8)
    crate_img = Image.fromarray(crate_arr)
    crate_path = os.path.join(output_dir, "T_Crate_Yellow_BaseColor.png")
    crate_img.save(crate_path, "PNG")
    print(f"Generated: {crate_path}")

    # 3. Sidewalk Terrazzo Tile Pattern (1024x1024)
    tile_w, tile_h = 1024, 1024
    tile_img = Image.new("RGB", (tile_w, tile_h), (180, 182, 185))
    tile_draw = ImageDraw.Draw(tile_img)
    # Grid of square sidewalk pavers (128x128 blocks)
    grid_size = 128
    for x in range(0, tile_w, grid_size):
        tile_draw.line([(x, 0), (x, tile_h)], fill=(120, 122, 125), width=4)
    for y in range(0, tile_h, grid_size):
        tile_draw.line([(0, y), (tile_w, y)], fill=(120, 122, 125), width=4)
        
    # Subtle terrazzo aggregate specks
    tile_arr = np.array(tile_img)
    specks = np.random.randint(-15, 16, (tile_h, tile_w, 3))
    tile_arr = np.clip(tile_arr.astype(int) + specks, 0, 255).astype(np.uint8)
    tile_img = Image.fromarray(tile_arr)
    tile_path = os.path.join(output_dir, "T_Sidewalk_Tiles_BaseColor.png")
    tile_img.save(tile_path, "PNG")
    print(f"Generated: {tile_path}")

    # 4. Nocturnal Wet Asphalt (1024x1024)
    asp_w, asp_h = 1024, 1024
    asp_arr = np.zeros((asp_h, asp_w, 3), dtype=np.uint8)
    # Dark tarmac base (#1A1B1D)
    asp_arr[:, :, 0] = 26
    asp_arr[:, :, 1] = 27
    asp_arr[:, :, 2] = 29
    # Tar aggregate noise
    agg_noise = np.random.randint(-10, 11, (asp_h, asp_w))
    for c in range(3):
        asp_arr[:, :, c] = np.clip(asp_arr[:, :, c].astype(int) + agg_noise, 0, 255).astype(np.uint8)
    asp_img = Image.fromarray(asp_arr)
    asp_path = os.path.join(output_dir, "T_Asphalt_Night_BaseColor.png")
    asp_img.save(asp_path, "PNG")
    print(f"Generated: {asp_path}")
    
    # Asphalt MaskMap (Metallic 0, AO 255, Detail 0, High Smoothness for wet puddle reflections)
    asp_mask = np.zeros((asp_h, asp_w, 4), dtype=np.uint8)
    asp_mask[:, :, 0] = 25  # Low metallic
    asp_mask[:, :, 1] = 255 # Full AO
    asp_mask[:, :, 2] = 0
    asp_mask[:, :, 3] = 210 # High smoothness (wet reflective sheen)
    asp_mask_img = Image.fromarray(asp_mask, "RGBA")
    asp_mask_path = os.path.join(output_dir, "T_Asphalt_Night_MaskMap.png")
    asp_mask_img.save(asp_mask_path, "PNG")
    print(f"Generated: {asp_mask_path}")

if __name__ == "__main__":
    generate_env_textures()
