#!/usr/bin/env python3
"""
Generates high-fidelity 2048x512 sign texture and emissive map for SM_Sign_TramChanh_New.
Based strictly on the approved NEW Tram Chanh sign artwork from:
- ArtSource/References/TeaBagAndSign/IMG_5207.JPG (front elevation of new sign)
- ArtSource/References/sign_rect_5207.png (isolated new sign artwork)
- ArtSource/References/TeaBagAndSign/IMG_5205.JPG (authentic circular brand logo)
"""

import os
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance
import numpy as np

def isolate_white_background(img, bg_threshold=235):
    """Converts near-white diffuser background into smooth alpha transparency."""
    img_rgba = img.convert("RGBA")
    data = np.array(img_rgba, dtype=float)
    
    r, g, b = data[:, :, 0], data[:, :, 1], data[:, :, 2]
    luminance = 0.299 * r + 0.587 * g + 0.114 * b
    
    # Distance from pure white
    dist = 255.0 - luminance
    
    # Alpha channel: opaque for dark letters/orange graphics, transparent for white background
    alpha = np.clip((dist - 10.0) / 40.0, 0.0, 1.0) * 255.0
    
    data[:, :, 3] = alpha
    return Image.fromarray(data.astype(np.uint8))

def generate_sign_textures(output_dir="Assets/TramChanh/Art/Textures"):
    os.makedirs(output_dir, exist_ok=True)
    
    width = 2048
    height = 512
    
    # Base image: crisp white lightbox panel
    base_img = Image.new("RGBA", (width, height), (255, 255, 255, 255))
    draw = ImageDraw.Draw(base_img)
    
    # 1. Subtle horizontal lighting falloff simulating realistic backlit LED lightbox
    for y in range(height):
        t = abs((y - height / 2.0) / (height / 2.0))
        v = int(255 - t * 4)
        draw.line([(0, y), (width, y)], fill=(v, v, v, 255))
        
    # 2. Top mustard-orange horizontal cap as in the new sign artwork
    mustard = (229, 138, 31, 255) # #E58A1F
    draw.rectangle([(0, 0), (width, 36)], fill=mustard)
    # Beveled highlight & shadow on top cap
    draw.line([(0, 0), (width, 0)], fill=(255, 185, 90, 255), width=2)
    draw.line([(0, 35), (width, 35)], fill=(130, 70, 12, 255), width=2)
    
    # 3. Bottom accent trim as seen in IMG_5207: cyan accent bar + mustard base trim
    cyan_trim = (38, 148, 171, 255)   # #2694AB
    mustard_trim = (229, 138, 31, 255)
    draw.rectangle([(0, height - 36), (width, height - 20)], fill=cyan_trim)
    draw.rectangle([(0, height - 20), (width, height)], fill=mustard_trim)
    draw.line([(0, height - 36), (width, height - 36)], fill=(25, 90, 105, 255), width=2)
    draw.line([(0, height - 1), (width, height - 1)], fill=(130, 70, 12, 255), width=2)

    # 4. Circular Logo Badges for Left & Right End-Caps
    logo_path = "ArtSource/References/TeaBagAndSign/IMG_5205.JPG"
    if os.path.exists(logo_path):
        raw_logo = Image.open(logo_path).convert("RGBA")
        # Crop square and make circular mask
        lw, lh = raw_logo.size
        min_dim = min(lw, lh)
        logo_sq = raw_logo.crop(((lw - min_dim)//2, (lh - min_dim)//2, (lw + min_dim)//2, (lh + min_dim)//2))
        
        # Create anti-aliased circular alpha mask
        mask = Image.new("L", (min_dim, min_dim), 0)
        mask_draw = ImageDraw.Draw(mask)
        mask_draw.ellipse([(8, 8), (min_dim - 8, min_dim - 8)], fill=255)
        logo_sq.putalpha(mask)
        
        # Resize for end-caps (diameter 240 px)
        badge_size = 230
        logo_badge = logo_sq.resize((badge_size, badge_size), Image.Resampling.LANCZOS)
        
        # Left end-cap centered at x=154, y=256
        base_img.paste(logo_badge, (154 - badge_size//2, 256 - badge_size//2), logo_badge)
        # Right end-cap centered at x=1894, y=256
        base_img.paste(logo_badge, (1894 - badge_size//2, 256 - badge_size//2), logo_badge)

    # 5. Front Face: Place Approved NEW Sign Artwork (IMG_5207)
    sign_raw_path = "ArtSource/References/sign_rect_5207.png"
    if os.path.exists(sign_raw_path):
        sign_raw = Image.open(sign_raw_path).convert("RGB")
        # Isolate letters and graphics by removing white background
        clean_sign = isolate_white_background(sign_raw, bg_threshold=235)
        
        # Scale to fit front face height (leaving margins for top and bottom trim)
        # Front face runs from x=308 to x=1740 (width=1432, height=512)
        target_h = 420
        target_w = int(target_h * (clean_sign.size[0] / clean_sign.size[1]))
        
        # If target_w > 1380, clamp width
        if target_w > 1380:
            target_w = 1380
            target_h = int(target_w * (clean_sign.size[1] / clean_sign.size[0]))
            
        sign_scaled = clean_sign.resize((target_w, target_h), Image.Resampling.LANCZOS)
        
        # Center in front face: center X = (308 + 1740) // 2 = 1024
        pos_x = 1024 - target_w // 2
        pos_y = 256 - target_h // 2
        
        base_img.paste(sign_scaled, (pos_x, pos_y), sign_scaled)

    # 6. Vertical seam groove lines separating end-caps for UV readability
    draw.line([(307, 36), (307, height - 36)], fill=(215, 220, 225, 255), width=2)
    draw.line([(1741, 36), (1741, height - 36)], fill=(215, 220, 225, 255), width=2)

    # Save BaseColor
    base_color_path = os.path.join(output_dir, "T_Sign_TramChanh_New_BaseColor.png")
    base_img.save(base_color_path, "PNG")
    print(f"Generated: {base_color_path}")
    
    # 7. Generate Physical Emissive Map
    em_data = np.zeros((height, width, 3), dtype=np.uint8)
    base_arr = np.array(base_img.convert("RGB"))
    r = base_arr[:, :, 0].astype(float)
    g = base_arr[:, :, 1].astype(float)
    b = base_arr[:, :, 2].astype(float)
    luminance = 0.299 * r + 0.587 * g + 0.114 * b
    
    # Backlit diffuser emits bright neutral light
    diffuser_mask = (luminance > 225)
    em_data[diffuser_mask] = [242, 246, 252]
    
    # Mustard top trim soft emission
    mustard_mask = (r > 190) & (g > 110) & (b < 60)
    em_data[mustard_mask] = [185, 115, 25]
    
    # Cyan bottom trim soft emission
    cyan_mask = (r < 60) & (g > 120) & (b > 140)
    em_data[cyan_mask] = [20, 110, 130]
    
    # Orange text "Chanh" backlit warmth
    orange_text_mask = (r > 170) & (g > 120) & (g < 170) & (b < 80)
    em_data[orange_text_mask] = [210, 135, 30]
    
    # Dark text "Trạm" and menu items block backlight
    dark_mask = (luminance < 160)
    em_data[dark_mask] = [6, 6, 8]
    
    emissive_img = Image.fromarray(em_data)
    emissive_img = emissive_img.filter(ImageFilter.GaussianBlur(radius=1.2))
    
    emissive_path = os.path.join(output_dir, "T_Sign_TramChanh_New_Emission.png")
    emissive_img.save(emissive_path, "PNG")
    print(f"Generated: {emissive_path}")

if __name__ == "__main__":
    generate_sign_textures()
