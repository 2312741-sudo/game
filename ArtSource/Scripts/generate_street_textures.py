#!/usr/bin/env python3
"""
Generates upgraded high-fidelity Vietnamese street textures:
- T_Shopfront_A_BaseColor.png (Tiệm Tạp Hóa Bình An)
- T_Shopfront_B_BaseColor.png (Nhà Thuốc Tâm Đức / GPP)
- T_Shopfront_C_BaseColor.png (Tiệm Sửa Xe Máy Vĩnh Phát)
- T_UtilityPole_BaseColor.png (Cột điện với dây cáp, sứ cách điện và biển quảng cáo dán tường)
"""

import os
import math
from PIL import Image, ImageDraw, ImageFont
import numpy as np

def draw_vietnamese_text(draw, x, y, text, font_size=36, fill=(255, 255, 255), bold=True):
    # Try system fonts or draw high-contrast legible bitmap text
    try:
        # Mac system fonts
        font = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", font_size)
    except Exception:
        try:
            font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", font_size)
        except Exception:
            font = ImageFont.load_default()
    draw.text((x, y), text, font=font, fill=fill)

def generate_street_textures():
    output_dir = "Assets/TramChanh/Art/Textures"
    os.makedirs(output_dir, exist_ok=True)
    np.random.seed(101)

    # =========================================================================
    # 1. Shopfront A: Tiệm Tạp Hóa & Nước Giải Khát Bình An (2048x2048)
    # =========================================================================
    sfA = Image.new("RGB", (2048, 2048), (228, 196, 126)) # Ochre yellow colonial plaster
    arrA = np.array(sfA).astype(int)
    noiseA = np.random.randint(-12, 13, (2048, 2048, 3))
    # Vertical rain streaks
    streaks = np.random.randint(-8, 9, (1, 2048, 1))
    arrA = np.clip(arrA + noiseA + streaks, 0, 255).astype(np.uint8)
    sfA = Image.fromarray(arrA)
    drawA = ImageDraw.Draw(sfA)

    # Top cornice trim (Y: 0..60)
    drawA.rectangle([(0, 0), (2048, 60)], fill=(195, 160, 105))
    drawA.line([(0, 60), (2048, 60)], fill=(155, 125, 75), width=6)
    for cx in range(60, 2048, 80):
        drawA.rectangle([(cx, 15), (cx+40, 55)], fill=(175, 140, 90))

    # 3rd Floor (Y: 60..540): Arched windows with green shutters
    # Damp mold streaks along roofline
    for mx in range(80, 2048, 120):
        length = np.random.randint(40, 110)
        drawA.polygon([(mx, 60), (mx+30, 60), (mx+15, 60+length)], fill=(145, 120, 80))

    # Windows on 3rd floor
    for wx_left, wx_right in [(320, 840), (1200, 1720)]:
        # Decorative arch header
        drawA.arc([(wx_left, 100), (wx_right, 220)], 180, 360, fill=(185, 150, 95), width=10)
        # Window frame
        drawA.rectangle([(wx_left, 160), (wx_right, 490)], fill=(24, 75, 78), outline=(15, 52, 55), width=8)
        # Glass panes with sky reflection
        drawA.rectangle([(wx_left+35, 190), (wx_left+235, 465)], fill=(135, 180, 200))
        drawA.rectangle([(wx_right-235, 190), (wx_right-35, 465)], fill=(135, 180, 200))
        # Window muntins
        drawA.line([(wx_left+135, 190), (wx_left+135, 465)], fill=(24, 75, 78), width=5)
        drawA.line([(wx_right-135, 190), (wx_right-135, 465)], fill=(24, 75, 78), width=5)
        drawA.line([(wx_left+35, 325), (wx_left+235, 325)], fill=(24, 75, 78), width=5)
        drawA.line([(wx_right-235, 325), (wx_right-35, 325)], fill=(24, 75, 78), width=5)

    # 2nd Floor (Y: 560..1060): Balcony, wrought iron railing & AC units
    # French double doors with shutters
    drawA.rectangle([(320, 620), (840, 930)], fill=(24, 75, 78), outline=(15, 52, 55), width=8)
    drawA.rectangle([(355, 645), (555, 910)], fill=(140, 185, 205))
    drawA.rectangle([(605, 645), (805, 910)], fill=(140, 185, 205))
    # Louver slats on shutters
    for ly in range(650, 900, 16):
        drawA.line([(365, ly), (545, ly)], fill=(20, 65, 68), width=3)
        drawA.line([(615, ly), (795, ly)], fill=(20, 65, 68), width=3)

    # AC Condenser Unit on right wall
    drawA.rectangle([(1320, 680), (1760, 930)], fill=(238, 240, 242), outline=(155, 160, 165), width=6)
    drawA.ellipse([(1470, 715), (1670, 900)], fill=(215, 218, 222), outline=(120, 125, 130), width=6)
    for fa in range(0, 360, 45):
        rad = math.radians(fa)
        drawA.line([(1570, 807), (1570 + 85*math.cos(rad), 807 + 85*math.sin(rad))], fill=(80, 85, 90), width=6)
    drawA.text((1350, 700), "D A I K I N", fill=(40, 120, 180))
    # AC Rust drips
    drawA.polygon([(1320, 930), (1350, 1020), (1310, 1020)], fill=(145, 110, 70))
    drawA.polygon([(1730, 930), (1760, 1020), (1710, 1020)], fill=(145, 110, 70))

    # Cantilevered concrete balcony slab (Y: 980..1040)
    drawA.rectangle([(80, 980), (1968, 1040)], fill=(170, 174, 178), outline=(120, 124, 128), width=4)
    # Wrought iron filigree railing (Y: 890..980)
    drawA.line([(80, 890), (1968, 890)], fill=(32, 35, 40), width=8)
    drawA.line([(80, 940), (1968, 940)], fill=(32, 35, 40), width=4)
    for bx in range(100, 1950, 32):
        drawA.line([(bx, 890), (bx, 980)], fill=(32, 35, 40), width=5)
        # Decorative diamond ring on railing
        if (bx // 32) % 2 == 0:
            drawA.ellipse([(bx-10, 925), (bx+10, 945)], outline=(45, 50, 55), width=3)

    # Black telephone / electrical cables running across facade
    drawA.line([(0, 1050), (2048, 1058)], fill=(20, 22, 25), width=5)
    drawA.line([(0, 1060), (2048, 1065)], fill=(25, 27, 30), width=4)
    drawA.line([(0, 1072), (2048, 1068)], fill=(18, 20, 22), width=3)

    # Main Commercial Signboard: "TẠP HÓA BÌNH AN" (Y: 1075..1245)
    drawA.rectangle([(80, 1075), (1968, 1245)], fill=(245, 210, 45), outline=(195, 35, 25), width=10)
    drawA.rectangle([(100, 1090), (1948, 1230)], fill=(215, 38, 28))
    # Graphic border inside sign
    drawA.rectangle([(115, 1102), (1933, 1218)], outline=(255, 240, 80), width=4)
    # Text headers
    draw_vietnamese_text(drawA, 140, 1110, "TIỆM TẠP HÓA - NƯỚC GIẢI KHÁT", font_size=32, fill=(255, 245, 120))
    draw_vietnamese_text(drawA, 140, 1145, "B Ì N H   A N", font_size=68, fill=(255, 255, 255))
    draw_vietnamese_text(drawA, 140, 1200, "BÁNH KẸO • KEM CÂY • NƯỚC NGỌT • THẺ CÀO ĐT • ĐÁ LẠNH • ĐT: 0905.123.456", font_size=26, fill=(255, 250, 200))
    # Circular branding logo stamp on right
    drawA.ellipse([(1810, 1115), (1915, 1215)], fill=(255, 245, 80), outline=(255, 255, 255), width=4)
    draw_vietnamese_text(drawA, 1828, 1145, "Trạm", font_size=32, fill=(215, 35, 25))

    # Red & White canvas awning (Y: 1250..1380)
    for ax in range(60, 1988, 64):
        col = (210, 38, 32) if ((ax - 60) // 64) % 2 == 0 else (248, 246, 242)
        drawA.rectangle([(ax, 1250), (min(ax+64, 1988), 1365)], fill=col)
        # Scalloped bottom edge
        drawA.pieslice([(ax, 1345), (min(ax+64, 1988), 1385)], 0, 180, fill=col)
    drawA.line([(60, 1345), (1988, 1345)], fill=(160, 25, 20), width=4)

    # Ground Floor: Navy blue corrugated rolling shutter (Y: 1380..2048)
    drawA.rectangle([(160, 1400), (1888, 2048)], fill=(42, 78, 124))
    for ry in range(1400, 2048, 20):
        drawA.line([(160, ry), (1888, ry)], fill=(28, 55, 90), width=4)
        drawA.line([(160, ry+5), (1888, ry+5)], fill=(65, 110, 165), width=2)
    # Bottom lock bar & handle
    drawA.rectangle([(160, 2005), (1888, 2045)], fill=(60, 65, 72))
    drawA.rectangle([(980, 1970), (1068, 2005)], fill=(180, 185, 190), outline=(40, 45, 50), width=3)
    # Concrete entry pillars
    drawA.rectangle([(60, 1350), (160, 2048)], fill=(175, 178, 182), outline=(130, 134, 138), width=3)
    drawA.rectangle([(1888, 1350), (1988, 2048)], fill=(175, 178, 182), outline=(130, 134, 138), width=3)

    sfA_path = os.path.join(output_dir, "T_Shopfront_A_BaseColor.png")
    sfA.save(sfA_path, "PNG")
    print(f"Generated upgraded: {sfA_path}")

    # =========================================================================
    # 2. Shopfront B: Nhà Thuốc Tâm Đức / Chuẩn GPP (2048x2048)
    # =========================================================================
    sfB = Image.new("RGB", (2048, 2048), (185, 205, 195)) # Modern sage green porcelain tile facade
    arrB = np.array(sfB).astype(int)
    noiseB = np.random.randint(-10, 11, (2048, 2048, 3))
    arrB = np.clip(arrB + noiseB, 0, 255).astype(np.uint8)
    sfB = Image.fromarray(arrB)
    drawB = ImageDraw.Draw(sfB)

    # Ceramic tile grid lines across the building
    for ty in range(0, 1050, 40):
        drawB.line([(0, ty), (2048, ty)], fill=(170, 190, 180), width=2)
    for tx in range(0, 2048, 80):
        drawB.line([(tx, 0), (tx, 1050)], fill=(170, 190, 180), width=2)

    # Upper floors (4 storeys): Modern aluminum glass windows & flower balconies
    for fy in [680, 220]:
        # Glass and stainless balcony railing
        drawB.rectangle([(120, fy+320), (1928, fy+350)], fill=(180, 184, 188), outline=(130, 135, 140), width=4)
        # Windows
        drawB.rectangle([(260, fy), (900, fy+300)], fill=(38, 42, 46), outline=(20, 24, 28), width=6)
        drawB.rectangle([(300, fy+20), (860, fy+280)], fill=(130, 175, 205))
        drawB.rectangle([(1140, fy), (1780, fy+300)], fill=(38, 42, 46), outline=(20, 24, 28), width=6)
        drawB.rectangle([(1180, fy+20), (1740, fy+280)], fill=(130, 175, 205))
        # AC units
        drawB.rectangle([(940, fy+80), (1100, fy+240)], fill=(235, 238, 240), outline=(160, 165, 170), width=4)
        drawB.ellipse([(980, fy+110), (1060, fy+210)], outline=(120, 125, 130), width=4)
        # Red / Magenta Bougainvillea flower pots on balcony
        for fx in range(320, 840, 60):
            drawB.rectangle([(fx, fy+285), (fx+45, fy+320)], fill=(185, 85, 55)) # clay pot
            drawB.ellipse([(fx-5, fy+260), (fx+50, fy+290)], fill=(210, 35, 95)) # flowers
        for fx in range(1200, 1720, 60):
            drawB.rectangle([(fx, fy+285), (fx+45, fy+320)], fill=(185, 85, 55))
            drawB.ellipse([(fx-5, fy+260), (fx+50, fy+290)], fill=(225, 45, 110))

    # Pharmacy Signboard: "NHÀ THUỐC TÂM ĐỨC - ĐẠT CHUẨN GPP" (Y: 1050..1265)
    drawB.rectangle([(80, 1050), (1968, 1265)], fill=(255, 255, 255), outline=(25, 135, 65), width=10)
    drawB.rectangle([(100, 1065), (1948, 1250)], fill=(22, 140, 68))
    # White border
    drawB.rectangle([(115, 1078), (1933, 1237)], outline=(255, 255, 255), width=4)

    # Green Medical Cross Badge on Left
    drawB.ellipse([(140, 1095), (280, 1220)], fill=(255, 255, 255))
    drawB.rectangle([(190, 1115), (230, 1200)], fill=(22, 140, 68))
    drawB.rectangle([(160, 1138), (260, 1178)], fill=(22, 140, 68))

    # Pharmacy Typography
    draw_vietnamese_text(drawB, 320, 1088, "BỘ Y TẾ - ĐẠT CHUẨN GPP", font_size=28, fill=(255, 240, 120))
    draw_vietnamese_text(drawB, 320, 1120, "NHÀ THUỐC TÂM ĐỨC", font_size=72, fill=(255, 255, 255))
    draw_vietnamese_text(drawB, 320, 1200, "KÊ ĐƠN • DƯỢC PHẨM CHÍNH HÃNG • THIẾT BỊ Y TẾ • ĐT: 0236.3888.999", font_size=26, fill=(255, 250, 200))

    # Red Emergency Cross Badge on Right
    drawB.ellipse([(1780, 1095), (1910, 1220)], fill=(255, 255, 255))
    drawB.rectangle([(1825, 1115), (1865, 1200)], fill=(215, 35, 25))
    drawB.rectangle([(1800, 1138), (1890, 1178)], fill=(215, 35, 25))

    # Ground Floor: Clean emerald metal roller shutter door (Y: 1390..2048)
    drawB.rectangle([(140, 1390), (1908, 2048)], fill=(38, 98, 68))
    for ry in range(1390, 2048, 22):
        drawB.line([(140, ry), (1908, ry)], fill=(25, 72, 48), width=4)
        drawB.line([(140, ry+5), (1908, ry+5)], fill=(58, 135, 95), width=2)
    # Glass entry frame visible when slightly open
    drawB.rectangle([(140, 1990), (1908, 2048)], fill=(65, 70, 75))

    sfB_path = os.path.join(output_dir, "T_Shopfront_B_BaseColor.png")
    sfB.save(sfB_path, "PNG")
    print(f"Generated upgraded: {sfB_path}")

    # =========================================================================
    # 3. Shopfront C: Tiệm Sửa Xe Máy & Rửa Xe Vĩnh Phát (2048x2048)
    # =========================================================================
    sfC = Image.new("RGB", (2048, 2048), (218, 212, 196)) # Weathered light plaster & exposed clay bricks
    arrC = np.array(sfC).astype(int)
    noiseC = np.random.randint(-14, 15, (2048, 2048, 3))
    arrC = np.clip(arrC + noiseC, 0, 255).astype(np.uint8)
    sfC = Image.fromarray(arrC)
    drawC = ImageDraw.Draw(sfC)

    # Exposed red clay brick patches (bong tróc vôi vữa lộ gạch chỉ)
    for bx, by in [(240, 1500), (1620, 820), (450, 420), (1350, 260)]:
        drawC.rectangle([(bx, by), (bx+260, by+150)], fill=(182, 75, 48), outline=(130, 50, 32), width=3)
        for gy in range(by, by+150, 24):
            drawC.line([(bx, gy), (bx+260, gy)], fill=(195, 185, 175), width=3)
        for gx in range(bx, bx+260, 48):
            drawC.line([(gx, by), (gx, by+150)], fill=(195, 185, 175), width=2)

    # Upper floors: Vintage French turquoise wooden shutters & clay tile canopies
    for uy in [620, 140]:
        # Clay tile awning over window
        for tx in range(280, 840, 28):
            drawC.rectangle([(tx, uy-25), (tx+24, uy)], fill=(195, 82, 42), outline=(145, 55, 25), width=2)
        for tx in range(1200, 1760, 28):
            drawC.rectangle([(tx, uy-25), (tx+24, uy)], fill=(195, 82, 42), outline=(145, 55, 25), width=2)

        # Louvered windows
        drawC.rectangle([(300, uy), (820, uy+350)], fill=(38, 120, 122), outline=(22, 75, 78), width=6)
        for ly in range(uy+20, uy+335, 14):
            drawC.line([(320, ly), (800, ly)], fill=(25, 82, 85), width=3)
        drawC.rectangle([(1220, uy), (1740, uy+350)], fill=(38, 120, 122), outline=(22, 75, 78), width=6)
        for ly in range(uy+20, uy+335, 14):
            drawC.line([(1240, ly), (1720, ly)], fill=(25, 82, 85), width=3)

    # Main Mechanic Signboard: "SỬA XE MÁY VĨNH PHÁT" (Y: 1080..1285)
    drawC.rectangle([(100, 1080), (1948, 1285)], fill=(225, 45, 30), outline=(245, 215, 45), width=10)
    drawC.rectangle([(120, 1098), (1928, 1267)], fill=(25, 72, 160))
    # Border
    drawC.rectangle([(135, 1110), (1913, 1255)], outline=(245, 220, 45), width=4)

    # Mechanic Typography
    draw_vietnamese_text(drawC, 160, 1115, "TIỆM SỬA CHỮA XE MÁY & RỬA XE", font_size=28, fill=(255, 245, 120))
    draw_vietnamese_text(drawC, 160, 1145, "V Ĩ N H   P H Á T", font_size=72, fill=(255, 235, 40))
    draw_vietnamese_text(drawC, 160, 1225, "BƠM VÁ • THAY NHỚT CHÍNH HÃNG • BẢO DƯỠNG CÔN GA • CỨU HỘ 24/7: 0935.888.777", font_size=24, fill=(255, 255, 255))

    # Sponsor oil logos on right
    drawC.rectangle([(1780, 1120), (1895, 1245)], fill=(255, 255, 255), outline=(220, 35, 25), width=4)
    draw_vietnamese_text(drawC, 1795, 1160, "CASTROL", font_size=22, fill=(210, 30, 25))

    # Ground floor: Traditional scissor iron folding gate (Y: 1400..2048)
    drawC.rectangle([(160, 1400), (1888, 2048)], fill=(125, 130, 135))
    for sx in range(160, 1888, 36):
        drawC.line([(sx, 1400), (sx, 2048)], fill=(55, 58, 62), width=5)
        # Diamond scissor pantograph lattice
        drawC.line([(sx, 1400), (min(sx+72, 1888), 1620)], fill=(85, 90, 95), width=3)
        drawC.line([(sx, 1620), (min(sx+72, 1888), 1400)], fill=(85, 90, 95), width=3)
        drawC.line([(sx, 1620), (min(sx+72, 1888), 1840)], fill=(85, 90, 95), width=3)
        drawC.line([(sx, 1840), (min(sx+72, 1888), 1620)], fill=(85, 90, 95), width=3)
    # Grease / oil grime at ground level
    drawC.rectangle([(160, 1980), (1888, 2048)], fill=(38, 36, 32))

    sfC_path = os.path.join(output_dir, "T_Shopfront_C_BaseColor.png")
    sfC.save(sfC_path, "PNG")
    print(f"Generated upgraded: {sfC_path}")

    # =========================================================================
    # 4. Utility Pole Texture (1024x1024)
    # =========================================================================
    pole_img = Image.new("RGB", (1024, 1024), (165, 168, 172)) # Weathered concrete
    pole_draw = ImageDraw.Draw(pole_img)
    # Concrete seams & vertical grooves
    for px in range(40, 480, 60):
        pole_draw.line([(px, 0), (px, 1024)], fill=(135, 138, 142), width=3)

    # Street advertising flyers (KHOAN CẮT BÊ TÔNG, HÚT HẦM CẦU, CHO THUÊ NHÀ)
    # Flyer 1
    pole_draw.rectangle([(80, 520), (320, 650)], fill=(245, 245, 248), outline=(120, 125, 130), width=2)
    draw_vietnamese_text(pole_draw, 90, 530, "KHOAN CẮT", font_size=24, fill=(215, 35, 25))
    draw_vietnamese_text(pole_draw, 90, 560, "BÊ TÔNG", font_size=32, fill=(25, 45, 160))
    draw_vietnamese_text(pole_draw, 90, 610, "0905.123.456", font_size=26, fill=(20, 20, 25))

    # Flyer 2
    pole_draw.rectangle([(140, 680), (380, 800)], fill=(255, 248, 180), outline=(120, 125, 130), width=2)
    draw_vietnamese_text(pole_draw, 150, 690, "HÚT HẦM CẦU", font_size=26, fill=(215, 35, 25))
    draw_vietnamese_text(pole_draw, 150, 725, "GIÁ RẺ 24/7", font_size=28, fill=(35, 120, 50))
    draw_vietnamese_text(pole_draw, 150, 760, "0914.888.999", font_size=24, fill=(20, 20, 25))

    # High Voltage Danger Warning Triangle
    pole_draw.polygon([(260, 160), (190, 280), (330, 280)], fill=(245, 210, 30), outline=(25, 28, 30), width=4)
    draw_vietnamese_text(pole_draw, 245, 210, "⚡", font_size=42, fill=(215, 35, 25))

    # Cable bundle section (black/charcoal wires) in X: [500, 1000]
    pole_draw.rectangle([(500, 40), (1000, 980)], fill=(25, 26, 28))
    for cy in range(60, 960, 14):
        pole_draw.line([(500, cy), (1000, cy)], fill=(45, 48, 52), width=4)
    # Multi-colored telecom wire tracer stripes
    pole_draw.line([(500, 320), (1000, 320)], fill=(40, 140, 220), width=4)
    pole_draw.line([(500, 480), (1000, 480)], fill=(220, 140, 40), width=4)
    pole_draw.line([(500, 640), (1000, 640)], fill=(40, 180, 60), width=4)

    pole_path = os.path.join(output_dir, "T_UtilityPole_BaseColor.png")
    pole_img.save(pole_path, "PNG")
    print(f"Generated upgraded: {pole_path}")

if __name__ == "__main__":
    generate_street_textures()
