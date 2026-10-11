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

    # =========================================================================
    # 5. Vietnamese Shopfront A (Tạp Hóa Bình An - 2048x2048)
    # =========================================================================
    sfA = Image.new("RGB", (2048, 2048), (228, 198, 132)) # Warm Vietnamese ochre yellow
    drawA = ImageDraw.Draw(sfA)
    # Weathering and stucco texture
    arrA = np.array(sfA)
    noiseA = np.random.randint(-12, 13, (2048, 2048, 3))
    arrA = np.clip(arrA.astype(int) + noiseA, 0, 255).astype(np.uint8)
    sfA = Image.fromarray(arrA)
    drawA = ImageDraw.Draw(sfA)
    
    # Ground Floor (y: 1350..2048): Blue corrugated rolling shutter
    drawA.rectangle([(160, 1420), (1888, 2048)], fill=(48, 86, 135))
    for ry in range(1420, 2048, 24):
        drawA.line([(160, ry), (1888, ry)], fill=(32, 62, 102), width=4)
        drawA.line([(160, ry+6), (1888, ry+6)], fill=(75, 120, 175), width=2)
    # Shop entrance concrete pillars
    drawA.rectangle([(60, 1350), (160, 2048)], fill=(175, 178, 182))
    drawA.rectangle([(1888, 1350), (1988, 2048)], fill=(175, 178, 182))
    
    # Red-and-white canvas awning (y: 1250..1380)
    for ax in range(60, 1988, 64):
        col = (205, 42, 38) if ((ax - 60) // 64) % 2 == 0 else (245, 245, 248)
        drawA.rectangle([(ax, 1250), (min(ax+64, 1988), 1380)], fill=col)
    drawA.line([(60, 1380), (1988, 1380)], fill=(160, 30, 28), width=6)
    
    # Commercial Signboard "TẠP HÓA BÌNH AN" (y: 1080..1240)
    drawA.rectangle([(80, 1080), (1968, 1240)], fill=(232, 180, 28), outline=(185, 36, 25), width=8)
    drawA.rectangle([(100, 1095), (1948, 1225)], fill=(245, 215, 65))
    # Signboard text graphic banner
    drawA.rectangle([(220, 1115), (1828, 1195)], fill=(195, 32, 28))
    # White typography block representing bold Vietnamese letters
    for tx in range(260, 1780, 48):
        drawA.rectangle([(tx, 1130), (tx+32, 1180)], fill=(255, 255, 255))
        
    # 2nd Floor (y: 560..1060): Balcony & teal shuttered windows
    # Cantilevered concrete balcony slab
    drawA.rectangle([(100, 1020), (1948, 1060)], fill=(168, 172, 176))
    # Wrought iron balcony railing
    for bx in range(120, 1928, 36):
        drawA.line([(bx, 930), (bx, 1020)], fill=(32, 35, 40), width=5)
    drawA.line([(100, 930), (1948, 930)], fill=(45, 48, 54), width=8)
    # French double windows
    drawA.rectangle([(320, 620), (840, 930)], fill=(28, 82, 85), outline=(18, 55, 58), width=8)
    drawA.rectangle([(360, 650), (560, 910)], fill=(140, 185, 205))
    drawA.rectangle([(600, 650), (800, 910)], fill=(140, 185, 205))
    # AC Outdoor Condenser unit
    drawA.rectangle([(1320, 720), (1760, 960)], fill=(235, 238, 240), outline=(150, 155, 160), width=6)
    # Fan grill circle
    drawA.ellipse([(1480, 750), (1680, 930)], outline=(110, 115, 120), width=6)
    # Wall rust/rain weathering streaks
    drawA.polygon([(1320, 960), (1350, 1060), (1310, 1060)], fill=(145, 115, 75))
    drawA.polygon([(1730, 960), (1760, 1060), (1710, 1060)], fill=(145, 115, 75))
    
    # 3rd Floor (y: 60..540): Windows and decorative top pediment
    drawA.rectangle([(320, 160), (840, 480)], fill=(28, 82, 85), outline=(18, 55, 58), width=8)
    drawA.rectangle([(360, 190), (560, 460)], fill=(130, 175, 195))
    drawA.rectangle([(600, 190), (800, 460)], fill=(130, 175, 195))
    drawA.rectangle([(1200, 160), (1720, 480)], fill=(28, 82, 85), outline=(18, 55, 58), width=8)
    drawA.rectangle([(1240, 190), (1440, 460)], fill=(130, 175, 195))
    drawA.rectangle([(1480, 190), (1680, 460)], fill=(130, 175, 195))
    # Top cornice trim
    drawA.rectangle([(0, 0), (2048, 60)], fill=(195, 165, 110))
    
    sfA_path = os.path.join(output_dir, "T_Shopfront_A_BaseColor.png")
    sfA.save(sfA_path, "PNG")
    print(f"Generated: {sfA_path}")

    # =========================================================================
    # 6. Vietnamese Shopfront B (Nhà Thuốc Đức Nguyên - 2048x2048)
    # =========================================================================
    sfB = Image.new("RGB", (2048, 2048), (178, 195, 185)) # Sage green/grey cement stucco
    arrB = np.array(sfB)
    noiseB = np.random.randint(-10, 11, (2048, 2048, 3))
    arrB = np.clip(arrB.astype(int) + noiseB, 0, 255).astype(np.uint8)
    sfB = Image.fromarray(arrB)
    drawB = ImageDraw.Draw(sfB)
    
    # Ground Floor: Green metal roller shutter
    drawB.rectangle([(140, 1400), (1908, 2048)], fill=(45, 95, 72))
    for ry in range(1400, 2048, 22):
        drawB.line([(140, ry), (1908, ry)], fill=(30, 68, 52), width=4)
        drawB.line([(140, ry+5), (1908, ry+5)], fill=(65, 130, 102), width=2)
    # Green cross pharmacy sign
    drawB.rectangle([(80, 1060), (1968, 1260)], fill=(245, 248, 250), outline=(32, 125, 68), width=8)
    drawB.rectangle([(120, 1080), (1928, 1240)], fill=(28, 145, 76))
    # Pharmacy Cross
    drawB.rectangle([(220, 1110), (320, 1210)], fill=(255, 255, 255))
    drawB.rectangle([(250, 1090), (290, 1230)], fill=(255, 255, 255))
    # Typography bars
    for tx in range(380, 1820, 52):
        drawB.rectangle([(tx, 1125), (tx+36, 1195)], fill=(255, 255, 255))
        
    # Upper floors: Modern aluminum glass windows
    for fy in [680, 220]:
        drawB.rectangle([(120, fy+320), (1928, fy+350)], fill=(160, 165, 170)) # Balcony/ledge
        drawB.rectangle([(260, fy), (900, fy+300)], fill=(42, 45, 50), outline=(25, 28, 32), width=6)
        drawB.rectangle([(300, fy+20), (860, fy+280)], fill=(125, 168, 192))
        drawB.rectangle([(1140, fy), (1780, fy+300)], fill=(42, 45, 50), outline=(25, 28, 32), width=6)
        drawB.rectangle([(1180, fy+20), (1740, fy+280)], fill=(125, 168, 192))
        # AC units on each level
        drawB.rectangle([(940, fy+80), (1100, fy+240)], fill=(225, 228, 230), outline=(160, 165, 170), width=4)
        drawB.ellipse([(980, fy+110), (1060, fy+210)], outline=(120, 125, 130), width=4)
        
    sfB_path = os.path.join(output_dir, "T_Shopfront_B_BaseColor.png")
    sfB.save(sfB_path, "PNG")
    print(f"Generated: {sfB_path}")

    # =========================================================================
    # 7. Vietnamese Shopfront C (Sửa Xe Máy Vĩnh Phát - 2048x2048)
    # =========================================================================
    sfC = Image.new("RGB", (2048, 2048), (218, 212, 198)) # Weathered light plaster with exposed brick
    arrC = np.array(sfC)
    noiseC = np.random.randint(-14, 15, (2048, 2048, 3))
    arrC = np.clip(arrC.astype(int) + noiseC, 0, 255).astype(np.uint8)
    sfC = Image.fromarray(arrC)
    drawC = ImageDraw.Draw(sfC)
    
    # Exposed red clay brick patches
    for bx, by in [(240, 1500), (1600, 850), (450, 420)]:
        drawC.rectangle([(bx, by), (bx+240, by+140)], fill=(175, 78, 55))
        for gy in range(by, by+140, 22):
            drawC.line([(bx, gy), (bx+240, gy)], fill=(150, 145, 140), width=3)
            
    # Ground floor: Scissor iron gate (Cửa kéo sắt)
    drawC.rectangle([(160, 1420), (1888, 2048)], fill=(135, 138, 142))
    for sx in range(160, 1888, 38):
        drawC.line([(sx, 1420), (sx, 2048)], fill=(65, 68, 72), width=5)
        # Diamond lattice scissors
        drawC.line([(sx, 1420), (min(sx+76, 1888), 1620)], fill=(90, 95, 100), width=3)
        drawC.line([(sx, 1620), (min(sx+76, 1888), 1420)], fill=(90, 95, 100), width=3)
        drawC.line([(sx, 1620), (min(sx+76, 1888), 1820)], fill=(90, 95, 100), width=3)
        drawC.line([(sx, 1820), (min(sx+76, 1888), 1620)], fill=(90, 95, 100), width=3)
        
    # Blue motorbike repair sign
    drawC.rectangle([(100, 1100), (1948, 1280)], fill=(25, 75, 155), outline=(225, 45, 35), width=8)
    drawC.rectangle([(120, 1120), (1928, 1260)], fill=(32, 90, 180))
    for tx in range(220, 1820, 48):
        drawC.rectangle([(tx, 1145), (tx+34, 1225)], fill=(255, 235, 60))
        
    # Vintage French turquoise wooden shutters on upper floors
    for uy in [620, 140]:
        drawC.rectangle([(300, uy), (820, uy+360)], fill=(45, 125, 128), outline=(28, 75, 78), width=6)
        # Louver slats
        for ly in range(uy+20, uy+340, 14):
            drawC.line([(320, ly), (800, ly)], fill=(30, 85, 88), width=3)
        drawC.rectangle([(1220, uy), (1740, uy+360)], fill=(45, 125, 128), outline=(28, 75, 78), width=6)
        for ly in range(uy+20, uy+340, 14):
            drawC.line([(1240, ly), (1720, ly)], fill=(30, 85, 88), width=3)
            
    sfC_path = os.path.join(output_dir, "T_Shopfront_C_BaseColor.png")
    sfC.save(sfC_path, "PNG")
    print(f"Generated: {sfC_path}")

    # =========================================================================
    # 8. Tree Foliage Atlas with Alpha Cutout (1024x1024 RGBA)
    # =========================================================================
    fol_w, fol_h = 1024, 1024
    fol_img = Image.new("RGBA", (fol_w, fol_h), (0, 0, 0, 0))
    fol_draw = ImageDraw.Draw(fol_img)
    np.random.seed(99)
    # Generate realistic tropical leaf clusters
    num_leaves = 650
    for _ in range(num_leaves):
        lx = np.random.randint(60, fol_w - 60)
        ly = np.random.randint(60, fol_h - 60)
        lw = np.random.randint(28, 65)
        lh = np.random.randint(45, 110)
        ang = np.random.uniform(0, 360)
        
        # Leaf tone gradient (yellow-green to deep emerald)
        g_val = np.random.randint(110, 195)
        r_val = np.random.randint(35, int(g_val * 0.65))
        b_val = np.random.randint(20, 55)
        leaf_color = (r_val, g_val, b_val, 255)
        
        # Elliptical leaf shape
        fol_draw.ellipse([(lx - lw//2, ly - lh//2), (lx + lw//2, ly + lh//2)], fill=leaf_color)
        # Center vein
        fol_draw.line([(lx, ly - lh//2), (lx, ly + lh//2)], fill=(min(255, r_val + 30), min(255, g_val + 30), b_val, 255), width=2)
        
    fol_path = os.path.join(output_dir, "T_Foliage_Tree_BaseColor.png")
    fol_img.save(fol_path, "PNG")
    print(f"Generated: {fol_path}")

    # =========================================================================
    # 9. Tree Trunk Bark (512x512)
    # =========================================================================
    bark_w, bark_h = 512, 512
    bark_arr = np.zeros((bark_h, bark_w, 3), dtype=np.uint8)
    bark_arr[:, :, 0] = 78   # Dark grey-brown
    bark_arr[:, :, 1] = 68
    bark_arr[:, :, 2] = 58
    # Vertical fibrous ridges
    for y in range(bark_h):
        for x in range(bark_w):
            fissure = int(18 * math.sin(x * 0.12) + 12 * math.sin(x * 0.35 + y * 0.05))
            fissure += np.random.randint(-10, 11)
            for c in range(3):
                bark_arr[y, x, c] = np.clip(int(bark_arr[y, x, c]) + fissure, 0, 255)
    bark_img = Image.fromarray(bark_arr)
    bark_path = os.path.join(output_dir, "T_Bark_Tree_BaseColor.png")
    bark_img.save(bark_path, "PNG")
    print(f"Generated: {bark_path}")

    # =========================================================================
    # 10. Utility Pole & Street Props (1024x1024)
    # =========================================================================
    pole_img = Image.new("RGB", (1024, 1024), (160, 162, 165))
    pole_draw = ImageDraw.Draw(pole_img)
    # Concrete weathering noise
    arrP = np.array(pole_img)
    noiseP = np.random.randint(-15, 16, (1024, 1024, 3))
    arrP = np.clip(arrP.astype(int) + noiseP, 0, 255).astype(np.uint8)
    pole_img = Image.fromarray(arrP)
    pole_draw = ImageDraw.Draw(pole_img)
    
    # Steel transformer box (dark olive grey)
    pole_draw.rectangle([(60, 60), (460, 460)], fill=(75, 82, 78), outline=(45, 50, 48), width=8)
    # Warning sign (yellow triangle with black lightning bolt)
    pole_draw.polygon([(260, 180), (200, 290), (320, 290)], fill=(240, 205, 30))
    # Electric meter glass cylinders
    for mx in [140, 260, 380]:
        pole_draw.ellipse([(mx-35, 350), (mx+35, 420)], fill=(210, 225, 235), outline=(50, 55, 58), width=4)
    # Cable bundle section (black/charcoal)
    pole_draw.rectangle([(520, 60), (960, 960)], fill=(28, 29, 32))
    for cy in range(80, 940, 18):
        pole_draw.line([(520, cy), (960, cy)], fill=(42, 45, 50), width=4)
        
    pole_path = os.path.join(output_dir, "T_UtilityPole_BaseColor.png")
    pole_img.save(pole_path, "PNG")
    print(f"Generated: {pole_path}")

    # =========================================================================
    # 11. Vietnamese Street Motorbike (Honda Wave / Dream - 1024x1024)
    # =========================================================================
    moto_img = Image.new("RGB", (1024, 1024), (45, 50, 55))
    moto_draw = ImageDraw.Draw(moto_img)
    # Teal/Cyan metallic body panels
    moto_draw.rectangle([(60, 60), (480, 480)], fill=(22, 110, 125)) # Classic Wave cyan
    moto_draw.rectangle([(100, 100), (440, 440)], fill=(32, 142, 160))
    # Silver/Chrome engine block and exhaust pipe
    moto_draw.rectangle([(540, 60), (960, 480)], fill=(195, 200, 205))
    moto_draw.rectangle([(580, 120), (920, 220)], fill=(235, 240, 245)) # Chrome muffler heat shield
    # Black rubber tires & vinyl seat
    moto_draw.rectangle([(60, 540), (480, 960)], fill=(20, 20, 22))
    # Seat leather seam
    moto_draw.line([(60, 680), (480, 680)], fill=(40, 42, 45), width=6)
    # Red taillight & orange turn indicators
    moto_draw.rectangle([(540, 540), (740, 720)], fill=(210, 25, 22)) # Red reflector
    moto_draw.rectangle([(780, 540), (960, 720)], fill=(235, 145, 20)) # Amber blinker
    # Headlight lens (bright crystalline)
    moto_draw.rectangle([(540, 760), (960, 960)], fill=(230, 242, 255), outline=(140, 160, 180), width=6)
    
    moto_path = os.path.join(output_dir, "T_Motorbike_BaseColor.png")
    moto_img.save(moto_path, "PNG")
    print(f"Generated: {moto_path}")

if __name__ == "__main__":
    generate_env_textures()
