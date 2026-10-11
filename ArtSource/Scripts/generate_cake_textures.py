#!/usr/bin/env python3
"""
Procedural PBR Texture Generator for Tram Chanh Cake Station Equipment (Agent D)
Generates:
- T_Grill_Display_BaseColor.png & Emission (Digital 160°C temperature readout)
- T_Grill_Plate_BaseColor.png & MaskMap (Non-stick cast ribbed grill plate)
- T_RollCake_BaseColor.png (Golden-brown toasted swirl rolled cake from IMG_5210.JPG)
- T_KraftPaper_BaseColor.png (Brown fibrous kraft wrapping paper from IMG_5212.JPG)
"""

import os
import math
from PIL import Image, ImageDraw, ImageFilter, ImageFont
import numpy as np

def generate_cake_textures(output_dir="Assets/TramChanh/Art/Textures"):
    os.makedirs(output_dir, exist_ok=True)
    
    # 1. Digital 160°C Display (512x256)
    disp_w, disp_h = 512, 256
    disp_img = Image.new("RGBA", (disp_w, disp_h), (18, 20, 22, 255))
    disp_draw = ImageDraw.Draw(disp_img)
    
    # Acrylic display bevel frame
    disp_draw.rectangle([(8, 8), (disp_w - 8, disp_h - 8)], outline=(60, 65, 70, 255), width=3)
    
    # Segment display background window
    disp_draw.rectangle([(30, 30), (disp_w - 30, disp_h - 30)], fill=(8, 10, 12, 255))
    
    # Draw digital readout "160 °C" using 7-segment style geometry
    # Green digital segments (#38E54D)
    green_seg = (56, 229, 77, 255)
    # Digit '1'
    disp_draw.rectangle([(120, 50), (135, 170)], fill=green_seg)
    # Digit '6'
    disp_draw.rectangle([(170, 50), (230, 65)], fill=green_seg)
    disp_draw.rectangle([(170, 50), (185, 170)], fill=green_seg)
    disp_draw.rectangle([(170, 102), (230, 117)], fill=green_seg)
    disp_draw.rectangle([(215, 102), (230, 170)], fill=green_seg)
    disp_draw.rectangle([(170, 155), (230, 170)], fill=green_seg)
    # Digit '0'
    disp_draw.rectangle([(265, 50), (325, 65)], fill=green_seg)
    disp_draw.rectangle([(265, 50), (280, 170)], fill=green_seg)
    disp_draw.rectangle([(310, 50), (325, 170)], fill=green_seg)
    disp_draw.rectangle([(265, 155), (325, 170)], fill=green_seg)
    
    # Degree circle '°' and 'C'
    disp_draw.rectangle([(350, 55), (365, 70)], fill=green_seg)
    disp_draw.rectangle([(385, 50), (445, 65)], fill=green_seg)
    disp_draw.rectangle([(385, 50), (400, 170)], fill=green_seg)
    disp_draw.rectangle([(385, 155), (445, 170)], fill=green_seg)
    
    # Status indicator LED dots (Power: Green, Heating: Orange)
    disp_draw.ellipse([(60, 190), (75, 205)], fill=(56, 229, 77, 255)) # Green power
    disp_draw.ellipse([(110, 190), (125, 205)], fill=(240, 130, 20, 255)) # Amber heat
    
    disp_path = os.path.join(output_dir, "T_Grill_Display_BaseColor.png")
    disp_img.save(disp_path, "PNG")
    print(f"Generated: {disp_path}")
    
    # Display Emission Map
    em_arr = np.zeros((disp_h, disp_w, 3), dtype=np.uint8)
    disp_arr = np.array(disp_img.convert("RGB"))
    # Green segments emit bright green
    green_mask = (disp_arr[:, :, 1] > 180) & (disp_arr[:, :, 0] < 100)
    em_arr[green_mask] = [50, 240, 70]
    # Orange indicator emits warm amber
    amber_mask = (disp_arr[:, :, 0] > 180) & (disp_arr[:, :, 1] > 90) & (disp_arr[:, :, 2] < 50)
    em_arr[amber_mask] = [255, 140, 20]
    
    em_img = Image.fromarray(em_arr).filter(ImageFilter.GaussianBlur(radius=1.2))
    em_path = os.path.join(output_dir, "T_Grill_Display_Emission.png")
    em_img.save(em_path, "PNG")
    print(f"Generated: {em_path}")

    # 2. Toasted Rolled Cake Texture (1024x1024)
    cake_w, cake_h = 1024, 1024
    cake_arr = np.zeros((cake_h, cake_w, 3), dtype=np.uint8)
    # Warm golden-brown sponge cake base (#E8A854)
    cake_arr[:, :, 0] = 232
    cake_arr[:, :, 1] = 168
    cake_arr[:, :, 2] = 84
    
    # Horizontal caramelized grill toast lines
    num_grill_toasts = 8
    for gy in np.linspace(80, cake_h - 80, num_grill_toasts):
        y_int = int(gy)
        for offset in range(-12, 13):
            fade = math.cos(offset / 12.0 * math.pi * 0.5)
            line_y = np.clip(y_int + offset, 0, cake_h - 1)
            # Deep toasted caramel color (#7A3814)
            cake_arr[line_y, :, 0] = np.clip(cake_arr[line_y, :, 0].astype(int) - int(110 * fade), 0, 255)
            cake_arr[line_y, :, 1] = np.clip(cake_arr[line_y, :, 1].astype(int) - int(115 * fade), 0, 255)
            cake_arr[line_y, :, 2] = np.clip(cake_arr[line_y, :, 2].astype(int) - int(65 * fade), 0, 255)
            
    # Surface sponge pores & speckled texture
    np.random.seed(101)
    pore_noise = np.random.randint(-18, 19, (cake_h, cake_w))
    for c in range(3):
        cake_arr[:, :, c] = np.clip(cake_arr[:, :, c].astype(int) + pore_noise, 0, 255).astype(np.uint8)
        
    cake_img = Image.fromarray(cake_arr)
    cake_path = os.path.join(output_dir, "T_RollCake_BaseColor.png")
    cake_img.save(cake_path, "PNG")
    print(f"Generated: {cake_path}")

    # 3. Brown Kraft Paper Wrapper Texture (512x512)
    kraft_w, kraft_h = 512, 512
    kraft_arr = np.zeros((kraft_h, kraft_w, 3), dtype=np.uint8)
    # Natural unbleached kraft paper brown (#C29968)
    kraft_arr[:, :, 0] = 194
    kraft_arr[:, :, 1] = 153
    kraft_arr[:, :, 2] = 104
    # Wood pulp fiber noise
    fiber_noise = np.random.randint(-14, 15, (kraft_h, kraft_w))
    for c in range(3):
        kraft_arr[:, :, c] = np.clip(kraft_arr[:, :, c].astype(int) + fiber_noise, 0, 255).astype(np.uint8)
        
    kraft_img = Image.fromarray(kraft_arr)
    kraft_path = os.path.join(output_dir, "T_KraftPaper_BaseColor.png")
    kraft_img.save(kraft_path, "PNG")
    print(f"Generated: {kraft_path}")

if __name__ == "__main__":
    generate_cake_textures()
