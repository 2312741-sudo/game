#!/usr/bin/env python3
"""
High-Fidelity PBR Texture Generator for Tram Chanh Drink Equipment (Agent C)
Based strictly on authentic shop photographs (IMG_5214.JPG, IMG_4932 2.JPG, IMG_5248.JPG, IMG_5254.JPG)

Generates:
- T_TeaBag_Pouch_BaseColor.png (2048x2048, dual-face front/back with authentic circular Trạm logo, frosted plastic, handle cutout, straw holes, condensation droplets)
- T_TeaBag_Pouch_Normal.png (2048x2048 tangent-space normal map with embossed zipper, cutout bevels, weld crimps, and condensation droplets)
- T_TeaBag_Pouch_MaskMap.png (2048x2048 Metallic/AO/Detail/Smoothness map)
- T_TeaRack_RedPlastic_BaseColor.png (Commercial red molded plastic crate finish)
- T_Steel_Brushed_BaseColor.png, MaskMap (Stainless topping bar and ice well)
- T_Menu_Board_BaseColor.png (Authentic menu board from IMG_4932 2.JPG)
"""

import os
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

def generate_drink_textures(output_dir="Assets/TramChanh/Art/Textures"):
    os.makedirs(output_dir, exist_ok=True)
    
    # ==============================================================================
    # 1. Stand-Up Pouch PBR Textures (2048x2048)
    # Dual-face UV layout:
    # Left half  [U: 0.0 .. 0.5] -> FRONT OF POUCH (x: 0 .. 1024) with authentic circular Trạm sticker
    # Right half [U: 0.5 .. 1.0] -> BACK OF POUCH  (x: 1024 .. 2048) clean transparent frosted plastic
    # ==============================================================================
    W, H = 2048, 2048
    pouch_base = Image.new("RGBA", (W, H), (255, 255, 255, 0))
    pouch_draw = ImageDraw.Draw(pouch_base)
    
    height_map = np.zeros((H, W), dtype=float)
    
    # Prepare authentic circular Trạm sticker badge
    ref_candidates = [
        "/tmp/ref3/IMG_5214.JPG",
        "ArtSource/References/TeaBagAndSign/IMG_5214.JPG",
        "/Users/nthtam/Downloads/Lưu trữ 3.zip"
    ]
    badge_img = None
    for ref_p in ref_candidates:
        if os.path.exists(ref_p) and ref_p.endswith(".JPG"):
            pouch_photo = Image.open(ref_p)
            pad_w, pad_h = pouch_photo.size[0] + 400, pouch_photo.size[1] + 400
            padded = Image.new("RGB", (pad_w, pad_h), (255, 255, 255))
            padded.paste(pouch_photo, (200, 200))
            cx, cy = 965 + 200, 1070 + 200
            # Rotate -40.5 deg to make "Trạm" baseline completely horizontal
            rot = padded.rotate(-40.5, center=(cx, cy), resample=Image.Resampling.BICUBIC)
            
            # Badge center in rotated image
            arr_rot = np.array(rot)
            bcx, bcy = cx, cy
            r_crop = 220
            crop = rot.crop((bcx - r_crop, bcy - r_crop, bcx + r_crop, bcy + r_crop))
            
            # Anti-aliased circular mask
            m_size = r_crop * 2
            mask = Image.new("L", (m_size * 4, m_size * 4), 0)
            m_draw = ImageDraw.Draw(mask)
            m_draw.ellipse([(0, 0), (m_size * 4 - 1, m_size * 4 - 1)], fill=255)
            mask = mask.resize((m_size, m_size), Image.Resampling.LANCZOS)
            
            badge_img = Image.new("RGBA", (m_size, m_size), (0, 0, 0, 0))
            badge_img.paste(crop, (0, 0))
            badge_img.putalpha(mask)
            break

    # If badge_img not found from raw photo, fallback to cached clean badge
    if badge_img is None and os.path.exists("/tmp/badge_level2.png"):
        badge_img = Image.open("/tmp/badge_level2.png").convert("RGBA")

    def draw_pouch_face(x_offset, is_front):
        half_w = W // 2 # 1024
        
        # 1. Base frosted plastic body (semi-transparent)
        body_alpha = 50
        pouch_draw.rectangle([(x_offset + 90, 465), (x_offset + half_w - 90, H)], fill=(242, 246, 252, body_alpha))
        
        # 2. Side weld borders (crimped heat seals)
        weld_w = 95
        for x in range(0, weld_w, 6):
            a = 150 if (x // 6) % 2 == 0 else 190
            pouch_draw.rectangle([(x_offset + x, 0), (x_offset + min(x + 5, weld_w), H)], fill=(235, 240, 248, a))
            height_map[:, x_offset + x : x_offset + min(x + 5, weld_w)] = 0.3 if a == 190 else 0.1
        for x in range(0, weld_w, 6):
            a = 150 if (x // 6) % 2 == 0 else 190
            rx = x_offset + half_w - weld_w + x
            pouch_draw.rectangle([(rx, 0), (min(rx + 5, x_offset + half_w), H)], fill=(235, 240, 248, a))
            height_map[:, rx : min(rx + 5, x_offset + half_w)] = 0.3 if a == 190 else 0.1

        # 3. Top header heat seal band
        pouch_draw.rectangle([(x_offset + 90, 0), (x_offset + half_w - 90, 465)], fill=(240, 245, 252, 175))
        height_map[0:465, x_offset + 90 : x_offset + half_w - 90] = np.maximum(height_map[0:465, x_offset + 90 : x_offset + half_w - 90], 0.2)

        # Zipper rib lines
        pouch_draw.rectangle([(x_offset + 90, 450), (x_offset + half_w - 90, 475)], fill=(210, 222, 235, 230))
        pouch_draw.line([(x_offset + 90, 462), (x_offset + half_w - 90, 462)], fill=(175, 190, 205, 255), width=4)
        height_map[450:475, x_offset + 90 : x_offset + half_w - 90] = 0.6
        height_map[460:465, x_offset + 90 : x_offset + half_w - 90] = 0.1 # groove

        # 4. Handle punch cutout (Alpha 0)
        handle_w, handle_h = 420, 120
        hx1, hy1 = x_offset + half_w//2 - handle_w//2, 220 - handle_h//2
        hx2, hy2 = x_offset + half_w//2 + handle_w//2, 220 + handle_h//2
        pouch_draw.rounded_rectangle([(hx1, hy1), (hx2, hy2)], radius=60, fill=(0, 0, 0, 0), outline=(210, 225, 240, 240), width=6)
        
        # Straw holes (Alpha 0)
        hole_r = 38
        for scx in [x_offset + 260, x_offset + half_w - 260]:
            pouch_draw.ellipse([(scx - hole_r, 220 - hole_r), (scx + hole_r, 220 + hole_r)], fill=(0, 0, 0, 0), outline=(210, 225, 240, 240), width=5)

        # 5. Condensation droplets
        np.random.seed(42 if is_front else 84)
        num_drops = 750
        for _ in range(num_drops):
            dx = np.random.randint(x_offset + 110, x_offset + half_w - 110)
            dy = np.random.randint(490, H - 40)
            dr = np.random.randint(3, 11)
            # Droplet highlight
            pouch_draw.ellipse([(dx - dr, dy - dr), (dx + dr, dy + dr)], outline=(255, 255, 255, 130), fill=(248, 252, 255, 70), width=2)
            pouch_draw.point((dx - dr//2, dy - dr//2), fill=(255, 255, 255, 220))
            # Height bump
            y_min, y_max = max(0, dy - dr), min(H, dy + dr)
            x_min, x_max = max(0, dx - dr), min(W, dx + dr)
            height_map[y_min:y_max, x_min:x_max] = np.maximum(height_map[y_min:y_max, x_min:x_max], 0.45)

        # 6. If front, paste the authentic Trạm circular badge
        if is_front and badge_img is not None:
            badge_d = 580
            bw, bh = badge_img.size
            b_r = min(bw, bh) // 2
            mask = Image.new("L", (b_r*2 * 4, b_r*2 * 4), 0)
            m_draw = ImageDraw.Draw(mask)
            m_draw.ellipse([(0, 0), (b_r*2 * 4 - 1, b_r*2 * 4 - 1)], fill=255)
            mask = mask.resize((b_r*2, b_r*2), Image.Resampling.LANCZOS)
            badge_sq = badge_img.crop(((bw - b_r*2)//2, (bh - b_r*2)//2, (bw + b_r*2)//2, (bh + b_r*2)//2))
            badge_sq.putalpha(mask)
            badge_final = badge_sq.resize((badge_d, badge_d), Image.Resampling.LANCZOS)
            
            bx = x_offset + half_w//2 - badge_d//2
            by = 1020 - badge_d//2
            pouch_base.paste(badge_final, (bx, by), badge_final)
            
            # Badge height bump
            y_b1, y_b2 = by, by + badge_d
            x_b1, x_b2 = bx, bx + badge_d
            height_map[y_b1:y_b2, x_b1:x_b2] = np.maximum(height_map[y_b1:y_b2, x_b1:x_b2], 0.25)

    draw_pouch_face(0, is_front=True)       # Left half: FRONT with badge
    draw_pouch_face(1024, is_front=False)   # Right half: BACK without badge

    pouch_path = os.path.join(output_dir, "T_TeaBag_Pouch_BaseColor.png")
    pouch_base.save(pouch_path, "PNG")
    print(f"Generated: {pouch_path}")

    # Generate Tangent-Space Normal Map
    gy, gx = np.gradient(height_map * 8.0)
    normal_x = -gx
    normal_y = -gy
    normal_z = np.ones_like(height_map)
    norm = np.sqrt(normal_x**2 + normal_y**2 + normal_z**2)
    normal_x /= norm
    normal_y /= norm
    normal_z /= norm

    norm_r = np.clip((normal_x * 0.5 + 0.5) * 255, 0, 255).astype(np.uint8)
    norm_g = np.clip((normal_y * 0.5 + 0.5) * 255, 0, 255).astype(np.uint8)
    norm_b = np.clip((normal_z * 0.5 + 0.5) * 255, 0, 255).astype(np.uint8)
    normal_img = Image.fromarray(np.dstack([norm_r, norm_g, norm_b]), "RGB")
    normal_path = os.path.join(output_dir, "T_TeaBag_Pouch_Normal.png")
    normal_img.save(normal_path, "PNG")
    print(f"Generated: {normal_path}")

    # Generate PBR MaskMap (Metallic=0, AO=255, Detail=0, Smoothness=A)
    mask_arr = np.zeros((H, W, 4), dtype=np.uint8)
    mask_arr[:, :, 0] = 0   # Metallic
    mask_arr[:, :, 1] = 255 # Occlusion
    mask_arr[:, :, 2] = 0   # Detail
    mask_arr[:, :, 3] = 230 # High plastic smoothness
    mask_arr[0:465, :] = 160 # Frosted header is slightly rougher
    mask_img = Image.fromarray(mask_arr, "RGBA")
    mask_path = os.path.join(output_dir, "T_TeaBag_Pouch_MaskMap.png")
    mask_img.save(mask_path, "PNG")
    print(f"Generated: {mask_path}")

    # ==============================================================================
    # 2. Commercial Red Plastic Rack (1024x1024)
    # ==============================================================================
    rack_w, rack_h = 1024, 1024
    rack_arr = np.zeros((rack_h, rack_w, 3), dtype=np.uint8)
    rack_arr[:, :, 0] = 212
    rack_arr[:, :, 1] = 36
    rack_arr[:, :, 2] = 27
    np.random.seed(42)
    noise = np.random.randint(-6, 7, (rack_h, rack_w))
    for c in range(3):
        rack_arr[:, :, c] = np.clip(rack_arr[:, :, c].astype(int) + noise, 0, 255).astype(np.uint8)
        
    rack_img = Image.fromarray(rack_arr)
    rack_path = os.path.join(output_dir, "T_TeaRack_RedPlastic_BaseColor.png")
    rack_img.save(rack_path, "PNG")
    print(f"Generated: {rack_path}")

    # ==============================================================================
    # 3. Brushed Stainless Steel Textures (1024x1024)
    # ==============================================================================
    steel_w, steel_h = 1024, 1024
    steel_arr = np.zeros((steel_h, steel_w, 3), dtype=np.uint8)
    steel_arr[:, :, :] = 210
    brush_lines = np.random.randint(-15, 16, (steel_h, 1))
    brush_field = np.repeat(brush_lines, steel_w, axis=1)
    for c in range(3):
        steel_arr[:, :, c] = np.clip(steel_arr[:, :, c].astype(int) + brush_field, 0, 255).astype(np.uint8)
        
    steel_img = Image.fromarray(steel_arr)
    steel_path = os.path.join(output_dir, "T_Steel_Brushed_BaseColor.png")
    steel_img.save(steel_path, "PNG")
    print(f"Generated: {steel_path}")
    
    steel_mask_arr = np.zeros((steel_h, steel_w, 4), dtype=np.uint8)
    steel_mask_arr[:, :, 0] = 235 # High Metallic
    steel_mask_arr[:, :, 1] = 255 # Full AO
    steel_mask_arr[:, :, 2] = 0   # Detail mask
    steel_mask_arr[:, :, 3] = 185 # Semi-smooth brushed sheen
    steel_mask_img = Image.fromarray(steel_mask_arr, "RGBA")
    steel_mask_path = os.path.join(output_dir, "T_Steel_Brushed_MaskMap.png")
    steel_mask_img.save(steel_mask_path, "PNG")
    print(f"Generated: {steel_mask_path}")

    # ==============================================================================
    # 4. Authentic Menu Board Texture (2048x2048) from IMG_4932 2.JPG
    # ==============================================================================
    menu_sources = [
        "/tmp/ref3/IMG_4932 2.JPG",
        "ArtSource/References/TeaBagAndSign/IMG_4932 2.JPG"
    ]
    for menu_src in menu_sources:
        if os.path.exists(menu_src):
            menu_raw = Image.open(menu_src)
            mw, mh = menu_raw.size
            menu_crop = menu_raw.crop((20, 20, mw - 20, mh - 20))
            menu_tex = menu_crop.resize((2048, 2048), Image.Resampling.LANCZOS)
            menu_path = os.path.join(output_dir, "T_Menu_Board_BaseColor.png")
            menu_tex.save(menu_path, "PNG")
            print(f"Generated: {menu_path}")
            break

if __name__ == "__main__":
    generate_drink_textures()
