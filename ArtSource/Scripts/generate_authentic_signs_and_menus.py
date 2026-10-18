#!/usr/bin/env python3
"""
generate_authentic_signs_and_menus.py
Generates authentic Vietnamese signage, table badges, and menu boards:
- T_TableSign_Atlas.png (2560x1024): 10 table signs with proper Vietnamese accents (BÀN 01-10, Trạm Chanh, Trà Chanh & Bánh Lăn Nướng, Quét mã gọi món)
- T_TableBillboard_Atlas.png (2560x1024): 10 floating table badges (BÀN 01-10) with sharp high-contrast rounded pills
- T_Menu_Board_BaseColor.png & Emission (2048x2048): Complete Vietnamese menu with seasonal specials and clear price listings
- T_Sign_TramChanh_New_BaseColor.png & Emission (2048x512): Main stall illuminated sign with crisp typography
"""

import os
from PIL import Image, ImageDraw, ImageFont

TEX_DIR = "Assets/TramChanh/Art/Textures"
FONT_BOLD = "/System/Library/Fonts/Supplemental/Arial Bold.ttf"
FONT_REGULAR = "/System/Library/Fonts/Supplemental/Arial.ttf"
FONT_BLACK = "/System/Library/Fonts/Supplemental/Arial Black.ttf"

def draw_styled_table_sign(draw, x0, y0, w, h, table_num):
    # Outer margin and card background
    pad = 12
    card_x0, card_y0 = x0 + pad, y0 + pad
    card_x1, card_y1 = x0 + w - pad, y0 + h - pad
    
    # White card base with subtle rounded corners
    draw.rounded_rectangle([card_x0, card_y0, card_x1, card_y1], radius=16, fill=(252, 252, 250), outline=(225, 175, 20), width=6)
    
    # Header Banner (Golden Yellow)
    banner_h = 105
    draw.rounded_rectangle([card_x0, card_y0, card_x1, card_y0 + banner_h], radius=16, fill=(232, 172, 16))
    # Fill bottom corners of header so it looks flat at bottom
    draw.rectangle([card_x0, card_y0 + banner_h - 16, card_x1, card_y0 + banner_h], fill=(232, 172, 16))
    
    # Green accent stripe below banner
    draw.rectangle([card_x0, card_y0 + banner_h, card_x1, card_y0 + banner_h + 10], fill=(22, 115, 60))
    
    # Brand title in banner
    f_title = ImageFont.truetype(FONT_BOLD, 42)
    title_text = "TRẠM CHANH"
    bbox = draw.textbbox((0, 0), title_text, font=f_title)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    draw.text((card_x0 + (card_x1 - card_x0 - tw) / 2, card_y0 + 26), title_text, font=f_title, fill=(18, 70, 38))
    
    # "BÀN" label
    f_ban = ImageFont.truetype(FONT_BOLD, 52)
    ban_text = "BÀN"
    bbox_b = draw.textbbox((0, 0), ban_text, font=f_ban)
    bw, bh = bbox_b[2] - bbox_b[0], bbox_b[3] - bbox_b[1]
    draw.text((card_x0 + (card_x1 - card_x0 - bw) / 2, card_y0 + 145), ban_text, font=f_ban, fill=(60, 64, 70))
    
    # Number "01" ... "10"
    f_num = ImageFont.truetype(FONT_BOLD, 120)
    num_str = f"{table_num:02d}"
    bbox_n = draw.textbbox((0, 0), num_str, font=f_num)
    nw, nh = bbox_n[2] - bbox_n[0], bbox_n[3] - bbox_n[1]
    draw.text((card_x0 + (card_x1 - card_x0 - nw) / 2, card_y0 + 205), num_str, font=f_num, fill=(24, 26, 30))
    
    # Gold divider bar
    draw.line([card_x0 + 60, card_y0 + 355, card_x1 - 60, card_y0 + 355], fill=(225, 175, 20), width=5)
    
    # Subtitle
    f_sub = ImageFont.truetype(FONT_BOLD, 28)
    sub_text = "Trà Chanh • Bánh Lăn Nướng"
    bbox_s = draw.textbbox((0, 0), sub_text, font=f_sub)
    sw, sh = bbox_s[2] - bbox_s[0], bbox_s[3] - bbox_s[1]
    draw.text((card_x0 + (card_x1 - card_x0 - sw) / 2, card_y0 + 375), sub_text, font=f_sub, fill=(50, 54, 60))
    
    # QR Code placeholder & order prompt
    qr_size = 54
    qr_x = card_x0 + 40
    qr_y = card_y0 + 415
    draw.rectangle([qr_x, qr_y, qr_x + qr_size, qr_y + qr_size], fill=(255, 255, 255), outline=(40, 44, 50), width=2)
    # Draw QR code square pattern
    draw.rectangle([qr_x + 6, qr_y + 6, qr_x + 18, qr_y + 18], fill=(30, 32, 36))
    draw.rectangle([qr_x + qr_size - 18, qr_y + 6, qr_x + qr_size - 6, qr_y + 18], fill=(30, 32, 36))
    draw.rectangle([qr_x + 6, qr_y + qr_size - 18, qr_x + 18, qr_y + qr_size - 6], fill=(30, 32, 36))
    draw.rectangle([qr_x + 24, qr_y + 24, qr_x + 32, qr_y + 32], fill=(30, 32, 36))
    
    # Order text
    f_order = ImageFont.truetype(FONT_BOLD, 20)
    draw.text((qr_x + qr_size + 16, qr_y + 6), "Quét mã gọi món tại bàn", font=f_order, fill=(30, 32, 36))
    f_wifi = ImageFont.truetype(FONT_REGULAR, 18)
    draw.text((qr_x + qr_size + 16, qr_y + 30), "Wifi: TramChanh | Pass: 88888888", font=f_wifi, fill=(100, 105, 115))

def generate_table_signs_atlas():
    w, h = 2560, 1024
    im = Image.new("RGB", (w, h), (240, 240, 240))
    draw = ImageDraw.Draw(im)
    
    cell_w = w // 5
    cell_h = h // 2
    
    for row in range(2):
        for col in range(5):
            table_num = row * 5 + col + 1
            x0 = col * cell_w
            y0 = row * cell_h
            draw_styled_table_sign(draw, x0, y0, cell_w, cell_h, table_num)
            
    out_path = os.path.join(TEX_DIR, "T_TableSign_Atlas.png")
    im.save(out_path, "PNG")
    print(f"Generated: {out_path} (2560x1024, 10 tables)")

def generate_table_billboards_atlas():
    w, h = 2560, 1024
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    
    cell_w = w // 5
    
    # Row 0: Y in [160, 352] (height = 192)
    # Row 1: Y in [672, 864] (height = 192)
    row_y_ranges = [
        (160, 352),
        (672, 864)
    ]
    
    f_num = ImageFont.truetype(FONT_BLACK, 84)
    
    for row in range(2):
        y0, y1 = row_y_ranges[row]
        for col in range(5):
            table_num = row * 5 + col + 1
            x0 = col * cell_w
            x1 = x0 + cell_w
            
            # Draw rounded pill badge with margin
            pad_x = 24
            pad_y = 12
            bx0, by0 = x0 + pad_x, y0 + pad_y
            bx1, by1 = x1 - pad_x, y1 - pad_y
            
            # Dark charcoal body
            draw.rounded_rectangle([bx0, by0, bx1, by1], radius=38, fill=(34, 38, 44, 245), outline=(235, 185, 20, 255), width=8)
            
            # Green status indicator circle
            cy = (by0 + by1) / 2
            cx = bx0 + 60
            cr = 26
            draw.ellipse([cx - cr, cy - cr, cx + cr, cy + cr], fill=(225, 185, 20), outline=(255, 255, 255), width=3)
            draw.ellipse([cx - cr + 6, cy - cr + 6, cx + cr - 6, cy + cr - 6], fill=(26, 155, 68))
            
            # Table label text "BÀN 01"
            text = f"BÀN {table_num:02d}"
            bbox = draw.textbbox((0, 0), text, font=f_num)
            tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
            tx = cx + cr + 30 + (bx1 - (cx + cr + 30) - tw) / 2
            ty = cy - th / 2 - 8
            draw.text((tx, ty), text, font=f_num, fill=(255, 255, 255, 255))
            
    out_path = os.path.join(TEX_DIR, "T_TableBillboard_Atlas.png")
    im.save(out_path, "PNG")
    print(f"Generated: {out_path} (2560x1024, 10 table billboards)")

def update_menu_board():
    menu_path = os.path.join(TEX_DIR, "T_Menu_Board_BaseColor.png")
    if not os.path.exists(menu_path):
        return
    im = Image.open(menu_path).convert("RGB")
    draw = ImageDraw.Draw(im)
    
    # "Món theo mùa:" empty box coordinates:
    # Top left: ~ (85, 1220), Bottom right: ~ (940, 1460)
    box_x0, box_y0 = 85, 1220
    box_x1, box_y1 = 940, 1460
    
    # Clean background for seasonal specials box
    draw.rectangle([box_x0 + 4, box_y0 + 4, box_x1 - 4, box_y1 - 4], fill=(244, 236, 218))
    
    # Draw dashed or clean border
    draw.rectangle([box_x0, box_y0, box_x1, box_y1], outline=(180, 140, 90), width=3)
    
    # Text inside seasonal specials box
    f_spec_title = ImageFont.truetype(FONT_BOLD, 36)
    draw.text((box_x0 + 30, box_y0 + 20), "★ ĐẶC SẢN NỔI BẬT ★", font=f_spec_title, fill=(180, 40, 20))
    
    f_item = ImageFont.truetype(FONT_BOLD, 32)
    f_price = ImageFont.truetype(FONT_BOLD, 34)
    
    specials = [
        ("• Trà Chanh Giã Tay Quảng Đông", "25"),
        ("• Trà Đỏ Nhiệt Đới Size L", "35"),
        ("• Bánh Lăn Trứng Muối Phô Mai", "35"),
        ("• Combo Trà + Bánh Lăn Giòn", "45")
    ]
    
    y = box_y0 + 75
    for item, price in specials:
        draw.text((box_x0 + 30, y), item, font=f_item, fill=(45, 30, 20))
        # Draw price in orange circle badge
        draw.rounded_rectangle([box_x1 - 95, y - 4, box_x1 - 25, y + 36], radius=8, fill=(230, 140, 25))
        draw.text((box_x1 - 85, y - 2), price, font=f_price, fill=(255, 255, 255))
        y += 42
        
    im.save(menu_path, "PNG")
    print(f"Updated: {menu_path} (filled seasonal specials with authentic Vietnamese items)")
    
    # Also update emission map to light up the new specials box
    em_path = os.path.join(TEX_DIR, "T_Menu_Board_Emission.png")
    if os.path.exists(em_path):
        em = Image.open(em_path).convert("RGB")
        em_draw = ImageDraw.Draw(em)
        # Soft warm glow for seasonal box
        em_draw.rectangle([box_x0, box_y0, box_x1, box_y1], fill=(120, 100, 70))
        em.save(em_path, "PNG")
        print(f"Updated: {em_path}")

def update_main_stall_sign():
    sign_path = os.path.join(TEX_DIR, "T_Sign_TramChanh_New_BaseColor.png")
    if not os.path.exists(sign_path):
        return
    im = Image.open(sign_path).convert("RGB")
    draw = ImageDraw.Draw(im)
    
    # Ensure subtitle reads: TRÀ TRÁI CÂY • BÁNH LĂN NƯỚNG
    # Clear the subtitle bar region in the center
    # Center text area: X around 700..1350, Y around 260..340
    draw.rectangle([650, 260, 1400, 335], fill=(255, 255, 255))
    
    f_sub = ImageFont.truetype(FONT_BOLD, 46)
    sub_text = "TRÀ TRÁI CÂY  •  BÁNH LĂN NƯỚNG"
    bbox = draw.textbbox((0, 0), sub_text, font=f_sub)
    tw = bbox[2] - bbox[0]
    draw.text(((2048 - tw) / 2, 270), sub_text, font=f_sub, fill=(24, 28, 32))
    
    im.save(sign_path, "PNG")
    print(f"Updated: {sign_path}")
    
    em_path = os.path.join(TEX_DIR, "T_Sign_TramChanh_New_Emission.png")
    if os.path.exists(em_path):
        em = Image.open(em_path).convert("RGB")
        em_draw = ImageDraw.Draw(em)
        em_draw.rectangle([650, 260, 1400, 335], fill=(255, 255, 255))
        em_draw.text(((2048 - tw) / 2, 270), sub_text, font=f_sub, fill=(0, 0, 0))
        em.save(em_path, "PNG")
        print(f"Updated: {em_path}")

def main():
    print("=== Generating Authentic Signs, Billboards, and Menus ===")
    generate_table_signs_atlas()
    generate_table_billboards_atlas()
    update_menu_board()
    update_main_stall_sign()
    print("=== Done Signs & Menus Setup ===")

if __name__ == "__main__":
    main()
