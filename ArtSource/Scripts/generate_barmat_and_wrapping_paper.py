import os
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont

def generate_textures():
    os.makedirs("Assets/TramChanh/Art/Textures", exist_ok=True)
    os.makedirs("Assets/TramChanh/Art/Materials", exist_ok=True)
    
    # -------------------------------------------------------------------------
    # 1. T_BarMat_BaseColor.png: Black rubber bar mat with rubber nubs and rim
    # -------------------------------------------------------------------------
    size = 512
    img_mat = Image.new("RGBA", (size, size), (22, 22, 24, 255))
    draw_mat = ImageDraw.Draw(img_mat)
    
    # Rim outer bevel
    rim_w = 24
    draw_mat.rectangle([0, 0, size, size], fill=(24, 24, 26, 255))
    # Outer highlight & shadow lines
    draw_mat.line([(0, 0), (size-1, 0)], fill=(45, 45, 50, 255), width=2)
    draw_mat.line([(0, 0), (0, size-1)], fill=(45, 45, 50, 255), width=2)
    draw_mat.line([(0, size-1), (size-1, size-1)], fill=(12, 12, 14, 255), width=2)
    draw_mat.line([(size-1, 0), (size-1, size-1)], fill=(12, 12, 14, 255), width=2)
    
    # Recessed inner well
    draw_mat.rectangle([rim_w, rim_w, size - rim_w, size - rim_w], fill=(16, 16, 18, 255))
    # Inner rim shadow/highlight
    draw_mat.line([(rim_w, rim_w), (size-rim_w, rim_w)], fill=(10, 10, 12, 255), width=2)
    draw_mat.line([(rim_w, rim_w), (rim_w, size-rim_w)], fill=(10, 10, 12, 255), width=2)
    draw_mat.line([(rim_w, size-rim_w), (size-rim_w, size-rim_w)], fill=(32, 32, 36, 255), width=2)
    draw_mat.line([(size-rim_w, rim_w), (size-rim_w, size-rim_w)], fill=(32, 32, 36, 255), width=2)
    
    # Grid of rubber nubs/prongs
    step = 16
    rad = 4
    for y in range(rim_w + 12, size - rim_w - 8, step):
        for x in range(rim_w + 12, size - rim_w - 8, step):
            # 3D shaded rubber nub
            draw_mat.ellipse([x - rad, y - rad, x + rad, y + rad], fill=(12, 12, 14, 255))
            draw_mat.ellipse([x - rad + 1, y - rad + 1, x + rad - 1, y + rad - 1], fill=(32, 32, 36, 255))
            draw_mat.arc([x - rad + 1, y - rad + 1, x + rad - 1, y + rad - 1], 180, 360, fill=(55, 55, 62, 255), width=1)
    
    # Debossed subtle branding in bottom rim
    try:
        font_sm = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 13)
    except Exception:
        font_sm = ImageFont.load_default()
    
    draw_mat.text((size // 2, size - rim_w // 2), "T R Ạ M  C H A N H", fill=(35, 35, 40, 255), anchor="mm", font=font_sm)
    
    img_mat.save("Assets/TramChanh/Art/Textures/T_BarMat_BaseColor.png")
    print("Generated T_BarMat_BaseColor.png")

    # -------------------------------------------------------------------------
    # 2. T_CakeWrappingPaper_BaseColor.png: Kraft paper with Trạm Chanh brand stamp
    # -------------------------------------------------------------------------
    # Kraft paper base with procedural subtle fiber grain
    np.random.seed(42)
    base_kraft = np.full((size, size, 3), [222, 206, 172], dtype=np.uint8)
    # Add subtle organic noise
    noise = (np.random.randn(size, size, 3) * 6.0).clip(-18, 18).astype(np.int16)
    kraft_arr = np.clip(base_kraft.astype(np.int16) + noise, 0, 255).astype(np.uint8)
    
    img_paper = Image.fromarray(kraft_arr, mode="RGB").convert("RGBA")
    draw_paper = ImageDraw.Draw(img_paper)
    
    # Outer vintage borders in warm dark brown
    stamp_col = (112, 78, 48, 220)
    stamp_sub = (130, 95, 65, 180)
    
    # Border frames
    draw_paper.rectangle([18, 18, size - 18, size - 18], outline=stamp_col, width=2)
    draw_paper.rectangle([25, 25, size - 25, size - 25], outline=stamp_sub, width=1)
    
    # Corner flourishes
    c_len = 16
    for cx, cy in [(18, 18), (size-18, 18), (18, size-18), (size-18, size-18)]:
        draw_paper.rectangle([cx-3, cy-3, cx+3, cy+3], fill=stamp_col)
    
    # Circular emblem stamp in center
    cx, cy = size // 2, size // 2
    r_out = 120
    draw_paper.ellipse([cx - r_out, cy - r_out, cx + r_out, cy + r_out], outline=stamp_col, width=3)
    draw_paper.ellipse([cx - r_out + 8, cy - r_out + 8, cx + r_out - 8, cy + r_out - 8], outline=stamp_sub, width=1)
    
    # Inner decorative dashed ring
    for deg in range(0, 360, 6):
        rad_d = math.radians(deg)
        x1 = cx + (r_out - 14) * math.cos(rad_d)
        y1 = cy + (r_out - 14) * math.sin(rad_d)
        x2 = cx + (r_out - 17) * math.cos(rad_d)
        y2 = cy + (r_out - 17) * math.sin(rad_d)
        draw_paper.line([(x1, y1), (x2, y2)], fill=stamp_sub, width=1)
        
    try:
        font_lg = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 26)
        font_md = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 15)
        font_xs = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 11)
    except Exception:
        font_lg = font_md = font_xs = ImageFont.load_default()
        
    draw_paper.text((cx, cy - 50), "TRẠM CHANH", fill=stamp_col, anchor="mm", font=font_lg)
    draw_paper.text((cx, cy - 22), "★ ★ ★", fill=stamp_sub, anchor="mm", font=font_md)
    draw_paper.line([(cx - 75, cy - 8), (cx + 75, cy - 8)], fill=stamp_col, width=2)
    draw_paper.text((cx, cy + 8), "BÁNH LĂN NƯỚNG", fill=stamp_col, anchor="mm", font=font_md)
    draw_paper.line([(cx - 75, cy + 24), (cx + 75, cy + 24)], fill=stamp_col, width=2)
    draw_paper.text((cx, cy + 42), "100% TƯƠI NGON", fill=stamp_sub, anchor="mm", font=font_xs)
    draw_paper.text((cx, cy + 58), "NƯỚNG GIÒN NÓNG HỔI", fill=stamp_sub, anchor="mm", font=font_xs)
    
    # Add a slight vintage distressed mask overlay
    distress = (np.random.rand(size, size) > 0.985)
    paper_arr = np.array(img_paper)
    paper_arr[distress, 3] = 160
    img_paper = Image.fromarray(paper_arr)
    
    img_paper.save("Assets/TramChanh/Art/Textures/T_CakeWrappingPaper_BaseColor.png")
    print("Generated T_CakeWrappingPaper_BaseColor.png")

if __name__ == "__main__":
    generate_textures()
