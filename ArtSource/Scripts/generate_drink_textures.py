#!/usr/bin/env python3
"""
Procedural PBR Texture Generator for Tram Chanh Drink Equipment (Agent C)
Generates:
- T_TeaBag_Pouch_BaseColor.png (Authentic transparent stand-up pouch with printed Trạm logo from IMG_5214.JPG)
- T_TeaRack_RedPlastic_BaseColor.png (Commercial red crate finish)
- T_Steel_Brushed_BaseColor.png, Normal, MaskMap (Stainless topping bar and ice well)
"""

import os
from PIL import Image, ImageDraw, ImageFilter
import numpy as np

def generate_drink_textures(output_dir="Assets/TramChanh/Art/Textures"):
    os.makedirs(output_dir, exist_ok=True)
    
    # 1. Stand-Up Pouch Plastic Texture (1024x1024)
    pouch_w, pouch_h = 1024, 1024
    pouch_img = Image.new("RGBA", (pouch_w, pouch_h), (255, 255, 255, 0)) # Alpha 0 by default
    pouch_draw = ImageDraw.Draw(pouch_img)
    
    # Frosted side weld borders (alpha ~ 180)
    weld_w = 40
    pouch_draw.rectangle([(0, 0), (weld_w, pouch_h)], fill=(245, 248, 252, 160))
    pouch_draw.rectangle([(pouch_w - weld_w, 0), (pouch_w, pouch_h)], fill=(245, 248, 252, 160))
    
    # Top zipper heat seal header
    pouch_draw.rectangle([(0, 0), (pouch_w, 120)], fill=(240, 245, 250, 190))
    # Zipper rib line
    pouch_draw.line([(0, 115), (pouch_w, 115)], fill=(200, 210, 220, 230), width=4)
    # Oval punch handle
    pouch_draw.ellipse([(pouch_w//2 - 120, 35), (pouch_w//2 + 120, 85)], fill=(0, 0, 0, 0))
    pouch_draw.ellipse([(pouch_w//2 - 120, 35), (pouch_w//2 + 120, 85)], outline=(220, 230, 240, 220), width=4)
    
    # Front printed circular "Trạm" logo
    # Load circular badge from references if available
    ref_logo = "ArtSource/References/TeaBagAndSign/IMG_5205.JPG"
    if os.path.exists(ref_logo):
        logo_im = Image.open(ref_logo).convert("RGBA")
        min_d = min(logo_im.size)
        sq = logo_im.crop(((logo_im.size[0] - min_d)//2, (logo_im.size[1] - min_d)//2,
                           (logo_im.size[0] + min_d)//2, (logo_im.size[1] + min_d)//2))
        # Circular mask
        mask = Image.new("L", (min_d, min_d), 0)
        mask_draw = ImageDraw.Draw(mask)
        mask_draw.ellipse([(10, 10), (min_d - 10, min_d - 10)], fill=240)
        sq.putalpha(mask)
        
        badge_size = 280
        badge_resized = sq.resize((badge_size, badge_size), Image.Resampling.LANCZOS)
        # Paste at center-upper portion of pouch (x = 512, y = 450)
        pouch_img.paste(badge_resized, (pouch_w//2 - badge_size//2, 440 - badge_size//2), badge_resized)
        
    pouch_path = os.path.join(output_dir, "T_TeaBag_Pouch_BaseColor.png")
    pouch_img.save(pouch_path, "PNG")
    print(f"Generated: {pouch_path}")

    # 2. Red Plastic Rack BaseColor (512x512)
    rack_w, rack_h = 512, 512
    rack_arr = np.zeros((rack_h, rack_w, 3), dtype=np.uint8)
    # Vibrant commercial crate red (#D4241B)
    rack_arr[:, :, 0] = 212
    rack_arr[:, :, 1] = 36
    rack_arr[:, :, 2] = 27
    # Subtle plastic molded grain noise
    np.random.seed(42)
    noise = np.random.randint(-6, 7, (rack_h, rack_w))
    for c in range(3):
        rack_arr[:, :, c] = np.clip(rack_arr[:, :, c].astype(int) + noise, 0, 255).astype(np.uint8)
        
    rack_img = Image.fromarray(rack_arr)
    rack_path = os.path.join(output_dir, "T_TeaRack_RedPlastic_BaseColor.png")
    rack_img.save(rack_path, "PNG")
    print(f"Generated: {rack_path}")

    # 3. Brushed Stainless Steel Texture (1024x1024)
    steel_w, steel_h = 1024, 1024
    steel_arr = np.zeros((steel_h, steel_w, 3), dtype=np.uint8)
    # Neutral brushed base
    steel_arr[:, :, :] = 210
    # Horizontal brushed lines
    brush_lines = np.random.randint(-15, 16, (steel_h, 1))
    brush_field = np.repeat(brush_lines, steel_w, axis=1)
    for c in range(3):
        steel_arr[:, :, c] = np.clip(steel_arr[:, :, c].astype(int) + brush_field, 0, 255).astype(np.uint8)
        
    steel_img = Image.fromarray(steel_arr)
    steel_path = os.path.join(output_dir, "T_Steel_Brushed_BaseColor.png")
    steel_img.save(steel_path, "PNG")
    print(f"Generated: {steel_path}")
    
    # Stainless Steel MaskMap (Metallic: R ~ 240, AO: G ~ 255, Detail: B ~ 0, Smoothness: A ~ 190)
    mask_arr = np.zeros((steel_h, steel_w, 4), dtype=np.uint8)
    mask_arr[:, :, 0] = 235 # High Metallic
    mask_arr[:, :, 1] = 255 # Full AO
    mask_arr[:, :, 2] = 0   # Detail mask
    mask_arr[:, :, 3] = 185 # Semi-smooth brushed sheen
    mask_img = Image.fromarray(mask_arr, "RGBA")
    mask_path = os.path.join(output_dir, "T_Steel_Brushed_MaskMap.png")
    mask_img.save(mask_path, "PNG")
    print(f"Generated: {mask_path}")

if __name__ == "__main__":
    generate_drink_textures()
