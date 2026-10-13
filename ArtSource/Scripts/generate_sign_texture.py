#!/usr/bin/env python3
"""
Generates high-fidelity 2048x512 flat straight-on sign texture and emissive map for SM_Sign_TramChanh_New.
Fully compliant with ART-FIX-007 Item 3:
- Flat straight-on view (no 3D oblique skew or perspective warp)
- Centered title "Trạm Chanh" ("Trạm" in black, "Chanh" in vibrant citrus yellow-orange, subtle drop shadow)
- Subtitle: "TRÀ TRÁI CÂY • BÁNH LĂN"
- Circular logos on both curved end-caps (centered at UV [0.0, 0.15] and [0.85, 1.0])
- Footer with fictitious address and hotline
- Physical emission map with warm illuminated acrylic & glowing translucent lettering
"""

import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont
import numpy as np

def generate_sign_textures(output_dir="Assets/TramChanh/Art/Textures"):
    os.makedirs(output_dir, exist_ok=True)
    
    width = 2048
    height = 512
    
    # 1. Base acrylic diffuser panel (clean, slightly warm white)
    base_img = Image.new("RGBA", (width, height), (252, 252, 250, 255))
    draw = ImageDraw.Draw(base_img)
    
    # Subtle radial falloff simulating internal fluorescent/LED tube illumination
    for y in range(height):
        t = abs((y - height / 2.0) / (height / 2.0))
        v = int(255 - t * 5)
        draw.line([(0, y), (width, y)], fill=(v, v, int(v * 0.98), 255))
        
    # 2. Top architectural cap (mustard orange)
    mustard = (229, 138, 31, 255) # #E58A1F
    draw.rectangle([(0, 0), (width, 36)], fill=mustard)
    # Beveled highlight & shadow on top cap
    draw.line([(0, 0), (width, 0)], fill=(255, 185, 90, 255), width=2)
    draw.line([(0, 35), (width, 35)], fill=(130, 70, 12, 255), width=2)
    
    # 3. Bottom accent trim: cyan bar + mustard base trim
    cyan_trim = (38, 148, 171, 255)   # #2694AB
    mustard_trim = (229, 138, 31, 255)
    draw.rectangle([(0, height - 36), (width, height - 20)], fill=cyan_trim)
    draw.rectangle([(0, height - 20), (width, height)], fill=mustard_trim)
    draw.line([(0, height - 36), (width, height - 36)], fill=(25, 90, 105, 255), width=2)
    draw.line([(0, height - 1), (width, height - 1)], fill=(130, 70, 12, 255), width=2)

    # 4. Circular Logo Badges flanking the main brand name on the front face (UV [0.15, 0.85])
    # Front banner region runs from X = 308 to X = 1740.
    # Badges are positioned on the front face at X = 466 and X = 1582 so they are 100% visible and NEVER clipped.
    logo_path = "ArtSource/References/IMG_5205.JPG"
    if not os.path.exists(logo_path):
        logo_path = "/Users/nthtam/.gemini/antigravity/brain/7ea37817-86db-4ce9-8ff5-37bad71fb2d1/scratch/Luu_tru_3/IMG_5205.JPG"
        
    badge_size = 190
    if os.path.exists(logo_path):
        raw_logo = Image.open(logo_path).convert("RGBA")
        lw, lh = raw_logo.size
        min_dim = min(lw, lh)
        logo_sq = raw_logo.crop(((lw - min_dim)//2, (lh - min_dim)//2, (lw + min_dim)//2, (lh + min_dim)//2))
        
        # Circular alpha mask
        mask = Image.new("L", (min_dim, min_dim), 0)
        mask_draw = ImageDraw.Draw(mask)
        mask_draw.ellipse([(12, 12), (min_dim - 12, min_dim - 12)], fill=255)
        logo_sq.putalpha(mask)
        
        logo_badge = logo_sq.resize((badge_size, badge_size), Image.Resampling.LANCZOS)
        
        # Draw circular frame borders for badges
        frame = Image.new("RGBA", (badge_size + 16, badge_size + 16), (0, 0, 0, 0))
        fd = ImageDraw.Draw(frame)
        fd.ellipse([(0, 0), (badge_size + 15, badge_size + 15)], fill=(229, 138, 31, 255))
        fd.ellipse([(4, 4), (badge_size + 11, badge_size + 11)], fill=(255, 255, 255, 255))
        frame.paste(logo_badge, (8, 8), logo_badge)
        
        # Paste left & right on front visible panel (never clipped)
        bw, bh = frame.size
        base_img.paste(frame, (466 - bw//2, 256 - bh//2), frame)
        base_img.paste(frame, (1582 - bw//2, 256 - bh//2), frame)

    # 5. Flat Straight-On Typography Layout (Between X = 308 and X = 1740, Center = 1024)
    # Available width = 1432 px
    font_bold_path = "/System/Library/Fonts/Supplemental/Arial Bold.ttf"
    font_regular_path = "/System/Library/Fonts/Supplemental/Arial.ttf"
    if not os.path.exists(font_bold_path):
        font_bold_path = "/System/Library/Fonts/Helvetica.ttc"
        font_regular_path = "/System/Library/Fonts/Helvetica.ttc"
        
    font_title = ImageFont.truetype(font_bold_path, 146)
    font_sub = ImageFont.truetype(font_bold_path, 44)
    font_foot = ImageFont.truetype(font_regular_path, 25)
    
    # Measure title elements
    meas_img = Image.new("RGBA", (10, 10))
    md = ImageDraw.Draw(meas_img)
    
    bb_tram = md.textbbox((0, 0), "Trạm", font=font_title)
    bb_space = md.textbbox((0, 0), " ", font=font_title)
    bb_chanh = md.textbbox((0, 0), "Chanh", font=font_title)
    
    w_tram = bb_tram[2] - bb_tram[0]
    w_space = bb_space[2] - bb_space[0]
    w_chanh = bb_chanh[2] - bb_chanh[0]
    total_title_w = w_tram + w_space + w_chanh
    
    x_tram = 1024 - total_title_w // 2
    x_space = x_tram + w_tram
    x_chanh = x_space + w_space
    y_title = 88
    
    # Separate text overlay layer for rendering drop shadow
    text_layer = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    t_draw = ImageDraw.Draw(text_layer)
    
    # Soft drop shadow for title
    shadow_offset = (3, 4)
    shadow_color = (0, 0, 0, 85)
    t_draw.text((x_tram + shadow_offset[0], y_title + shadow_offset[1]), "Trạm", fill=shadow_color, font=font_title)
    t_draw.text((x_chanh + shadow_offset[0], y_title + shadow_offset[1]), "Chanh", fill=shadow_color, font=font_title)
    
    # Title text: "Trạm" in deep black, "Chanh" in vibrant yellow-orange
    color_tram = (24, 24, 27, 255)      # Deep Black #18181B
    color_chanh = (245, 148, 14, 255)   # Vibrant Citrus Yellow-Orange #F5940E
    
    # Subtle crisp edge definition / stroke
    t_draw.text((x_tram, y_title), "Trạm", fill=color_tram, font=font_title, stroke_width=1, stroke_fill=(10, 10, 10, 120))
    t_draw.text((x_chanh, y_title), "Chanh", fill=color_chanh, font=font_title, stroke_width=1, stroke_fill=(200, 105, 5, 120))
    
    # Subtitle: "TRÀ TRÁI CÂY • BÁNH LĂN"
    sub_text1 = "TRÀ TRÁI CÂY"
    sub_dot = "  •  "
    sub_text2 = "BÁNH LĂN"
    
    bb_s1 = md.textbbox((0, 0), sub_text1, font=font_sub)
    bb_sd = md.textbbox((0, 0), sub_dot, font=font_sub)
    bb_s2 = md.textbbox((0, 0), sub_text2, font=font_sub)
    
    w_s1 = bb_s1[2] - bb_s1[0]
    w_sd = bb_sd[2] - bb_sd[0]
    w_s2 = bb_s2[2] - bb_s2[0]
    total_sub_w = w_s1 + w_sd + w_s2
    
    x_sub1 = 1024 - total_sub_w // 2
    x_subd = x_sub1 + w_s1
    x_sub2 = x_subd + w_sd
    y_sub = 282
    
    # Subtitle shadow & text
    t_draw.text((x_sub1 + 2, y_sub + 2), sub_text1, fill=(0, 0, 0, 50), font=font_sub)
    t_draw.text((x_sub2 + 2, y_sub + 2), sub_text2, fill=(0, 0, 0, 50), font=font_sub)
    t_draw.text((x_sub1, y_sub), sub_text1, fill=(35, 35, 38, 255), font=font_sub)
    t_draw.text((x_subd, y_sub), sub_dot, fill=(229, 138, 31, 255), font=font_sub)
    t_draw.text((x_sub2, y_sub), sub_text2, fill=(35, 35, 38, 255), font=font_sub)
    
    # Footer line: Updated address "9 Hải Thượng" and hotline "0941 668 405" per ART-FIX-010c
    foot_text = "ĐC: 9 HẢI THƯỢNG   •   HOTLINE: 0941 668 405"
    bb_foot = md.textbbox((0, 0), foot_text, font=font_foot)
    w_foot = bb_foot[2] - bb_foot[0]
    x_foot = 1024 - w_foot // 2
    y_foot = 384
    t_draw.text((x_foot, y_foot), foot_text, fill=(75, 85, 99, 255), font=font_foot)
    
    # Composite text over base lightbox
    base_img.alpha_composite(text_layer)
    
    # 6. Subtle curved end-cap accent lines
    draw = ImageDraw.Draw(base_img)
    draw.line([(307, 36), (307, height - 36)], fill=(225, 228, 232, 255), width=1)
    draw.line([(1741, 36), (1741, height - 36)], fill=(225, 228, 232, 255), width=1)
    
    # Save BaseColor texture
    base_color_path = os.path.join(output_dir, "T_Sign_TramChanh_New_BaseColor.png")
    base_img.convert("RGB").save(base_color_path, "PNG")
    print(f"Generated BaseColor: {base_color_path}")
    
    # 7. Physical Emissive Map Generation (anti-blowout calibrated for URP bloom)
    em_data = np.zeros((height, width, 3), dtype=np.uint8)
    base_arr = np.array(base_img.convert("RGB"))
    r = base_arr[:, :, 0].astype(float)
    g = base_arr[:, :, 1].astype(float)
    b = base_arr[:, :, 2].astype(float)
    luminance = 0.299 * r + 0.587 * g + 0.114 * b
    
    # Balanced white acrylic diffuser emission (reduced to prevent burn out)
    diffuser_mask = (luminance > 220)
    em_data[diffuser_mask] = [175, 180, 190]
    
    # Mustard top & bottom trims emission
    mustard_mask = (r > 190) & (g > 110) & (b < 60)
    em_data[mustard_mask] = [140, 85, 20]
    
    # Cyan bottom trim emission
    cyan_mask = (r < 60) & (g > 120) & (b > 140)
    em_data[cyan_mask] = [18, 85, 105]
    
    # Citrus Orange "Chanh" lettering backlight emission
    orange_text_mask = (r > 180) & (g > 110) & (g < 175) & (b < 50)
    em_data[orange_text_mask] = [190, 115, 15]
    
    # Front circular logo badges illumination
    for cx in [466, 1582]:
        cy = 256
        y_grid, x_grid = np.ogrid[:height, :width]
        dist_sq = (x_grid - cx)**2 + (y_grid - cy)**2
        badge_mask = (dist_sq <= ((badge_size + 16) // 2)**2)
        badge_glow = badge_mask & (luminance > 140)
        em_data[badge_glow] = [170, 170, 180]
    
    # Dark text "Trạm", subtitle, footer, and shadows block backlight
    dark_mask = (luminance < 110)
    em_data[dark_mask] = [8, 8, 10]
    
    emissive_img = Image.fromarray(em_data)
    emissive_img = emissive_img.filter(ImageFilter.GaussianBlur(radius=1.0))
    
    emissive_path = os.path.join(output_dir, "T_Sign_TramChanh_New_Emission.png")
    emissive_img.save(emissive_path, "PNG")
    print(f"Generated Emissive: {emissive_path}")

if __name__ == "__main__":
    generate_sign_textures()
