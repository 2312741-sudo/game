#!/usr/bin/env python3
"""
Generates high-fidelity authentic Elmich Grill digital display textures:
- T_Grill_Display_BaseColor.png
- T_Grill_Display_Emission.png
Matching the product photos media_1792093714169_0e72cf66.png & media_1792093714172_4f22241b.png.
Features:
- Cursive Elmich brand logo with circular swirl on the left.
- Red 7-segment readouts: "180", "02", "180".
- Labels: "V" (upper plate), "TIME", "^" (lower plate), "°C".
- Touch button controls: "-", "+", plate selectors, start/stop.
- Power button circle on the right.
"""
import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

def draw_7segment(draw, x, y, w, h, digit, color, off_color=(45, 12, 12, 255)):
    # Segments: a (top), b (top-right), c (bot-right), d (bottom), e (bot-left), f (top-left), g (middle)
    # digit map
    seg_map = {
        '0': (1,1,1,1,1,1,0),
        '1': (0,1,1,0,0,0,0),
        '2': (1,1,0,1,1,0,1),
        '3': (1,1,1,1,0,0,1),
        '4': (0,1,1,0,0,1,1),
        '5': (1,0,1,1,0,1,1),
        '6': (1,0,1,1,1,1,1),
        '7': (1,1,1,0,0,0,0),
        '8': (1,1,1,1,1,1,1),
        '9': (1,1,1,1,0,1,1),
    }
    t = max(3, w // 5) # segment thickness
    hh = h // 2
    
    segs = seg_map.get(str(digit), (0,0,0,0,0,0,0))
    
    # coords
    coords = [
        # a: top
        [(x + t, y), (x + w - t, y + t)],
        # b: top-right
        [(x + w - t, y + t), (x + w, y + hh)],
        # c: bot-right
        [(x + w - t, y + hh), (x + w, y + h - t)],
        # d: bottom
        [(x + t, y + h - t), (x + w - t, y + h)],
        # e: bot-left
        [(x, y + hh), (x + t, y + h - t)],
        # f: top-left
        [(x, y + t), (x + t, y + hh)],
        # g: middle
        [(x + t, y + hh - t//2), (x + w - t, y + hh + t//2)],
    ]
    
    for active, box in zip(segs, coords):
        draw.rectangle(box, fill=color if active else off_color)

def generate_display_textures(out_dir):
    w, h = 1024, 512
    # Base color: deep glossy piano black glass
    base_img = Image.new("RGBA", (w, h), (14, 15, 17, 255))
    draw = ImageDraw.Draw(base_img)
    
    # Subtle inner bevel frame
    draw.rectangle([(20, 20), (w - 20, h - 20)], outline=(35, 38, 42, 255), width=3)
    draw.rectangle([(40, 40), (w - 40, h - 40)], outline=(22, 24, 28, 255), width=2)
    
    # Try loading clean font
    font_path = "/System/Library/Fonts/Helvetica.ttc"
    try:
        font_logo = ImageFont.truetype(font_path, 42)
        font_label = ImageFont.truetype(font_path, 22)
        font_icon = ImageFont.truetype(font_path, 28)
    except Exception:
        font_logo = ImageFont.load_default()
        font_label = ImageFont.load_default()
        font_icon = ImageFont.load_default()

    # 1. Left side: Elmich logo (white cursive-style text + leaf/swirl circle)
    # Circle swirl
    logo_cx, logo_cy = 100, 256
    draw.ellipse([(logo_cx - 24, logo_cy - 24), (logo_cx + 24, logo_cy + 24)], outline=(255, 255, 255, 255), width=4)
    draw.ellipse([(logo_cx - 12, logo_cy - 12), (logo_cx + 12, logo_cy + 12)], fill=(255, 255, 255, 255))
    draw.text((logo_cx + 36, logo_cy - 24), "elmich", fill=(255, 255, 255, 255), font=font_logo)

    # 2. Central Digital LED Readout Cluster
    # Digit dimensions
    dw, dh = 36, 68
    led_red = (255, 35, 40, 255)
    led_off = (48, 12, 14, 255)
    white_txt = (220, 225, 230, 255)

    # First readout: Upper plate "180"
    x_start = 360
    y_start = 200
    
    # "V" label above 180
    draw.text((x_start + 40, y_start - 36), "V", fill=white_txt, font=font_label)
    draw_7segment(draw, x_start, y_start, dw, dh, 1, led_red, led_off)
    draw_7segment(draw, x_start + dw + 10, y_start, dw, dh, 8, led_red, led_off)
    draw_7segment(draw, x_start + (dw + 10) * 2, y_start, dw, dh, 0, led_red, led_off)

    # Middle readout: TIME "02"
    x_time = x_start + (dw + 10) * 3 + 40
    draw.text((x_time + 16, y_start - 36), "TIME", fill=white_txt, font=font_label)
    draw_7segment(draw, x_time, y_start, dw, dh, 0, led_red, led_off)
    draw_7segment(draw, x_time + dw + 10, y_start, dw, dh, 2, led_red, led_off)

    # Second readout: Lower plate "180"
    x_lower = x_time + (dw + 10) * 2 + 40
    draw.text((x_lower + 40, y_start - 36), "^", fill=white_txt, font=font_label)
    draw_7segment(draw, x_lower, y_start, dw, dh, 1, led_red, led_off)
    draw_7segment(draw, x_lower + dw + 10, y_start, dw, dh, 8, led_red, led_off)
    draw_7segment(draw, x_lower + (dw + 10) * 2, y_start, dw, dh, 0, led_red, led_off)

    # Minus and Plus buttons
    btn_y = y_start + dh + 30
    draw.ellipse([(x_start - 50, y_start + 18), (x_start - 16, y_start + 52)], outline=white_txt, width=2)
    draw.text((x_start - 40, y_start + 20), "-", fill=white_txt, font=font_icon)

    draw.ellipse([(x_lower + (dw + 10) * 3 + 16, y_start + 18), (x_lower + (dw + 10) * 3 + 50, y_start + 52)], outline=white_txt, width=2)
    draw.text((x_lower + (dw + 10) * 3 + 24, y_start + 20), "+", fill=white_txt, font=font_icon)

    # Row of touch circle buttons below display: [UpperPlate], [Menu], [Temp], [LowerPlate]
    btn_r = 18
    btn_centers = [
        x_start + 30,
        x_start + 100,
        x_time + 30,
        x_lower + 30,
        x_lower + 100
    ]
    for bcx in btn_centers:
        draw.ellipse([(bcx - btn_r, btn_y - btn_r), (bcx + btn_r, btn_y + btn_r)], outline=(120, 130, 140, 255), width=2)
    # Inner icons
    draw.rectangle([(btn_centers[0] - 8, btn_y - 2), (btn_centers[0] + 8, btn_y + 2)], fill=(0, 210, 230, 255))
    draw.polygon([(btn_centers[1] - 6, btn_y - 4), (btn_centers[1] + 6, btn_y - 4), (btn_centers[1], btn_y + 6)], fill=white_txt)
    draw.text((btn_centers[2] - 10, btn_y - 12), "°C", fill=white_txt, font=ImageFont.load_default())
    draw.polygon([(btn_centers[3] - 6, btn_y + 4), (btn_centers[3] + 6, btn_y + 4), (btn_centers[3], btn_y - 6)], fill=white_txt)
    draw.rectangle([(btn_centers[4] - 8, btn_y - 2), (btn_centers[4] + 8, btn_y + 2)], fill=(0, 210, 230, 255))

    # 3. Right side: Power Button (Red glowing power symbol)
    p_cx, p_cy = 920, 256
    p_r = 30
    draw.ellipse([(p_cx - p_r, p_cy - p_r), (p_cx + p_r, p_cy + p_r)], outline=(80, 85, 90, 255), width=3)
    # Power symbol arc and bar
    draw.arc([(p_cx - 18, p_cy - 18), (p_cx + 18, p_cy + 18)], start=120, end=420, fill=led_red, width=3)
    draw.line([(p_cx, p_cy - 20), (p_cx, p_cy - 4)], fill=led_red, width=3)

    # Save BaseColor
    os.makedirs(out_dir, exist_ok=True)
    base_path = os.path.join(out_dir, "T_Grill_Display_BaseColor.png")
    base_img.save(base_path)
    print(f"Generated display base color: {base_path}")

    # Generate Emission map: LEDs and glowing elements emit light
    em_img = Image.new("RGB", (w, h), (0, 0, 0))
    em_draw = ImageDraw.Draw(em_img)
    # Draw glowing digits
    draw_7segment(em_draw, x_start, y_start, dw, dh, 1, (255, 45, 50))
    draw_7segment(em_draw, x_start + dw + 10, y_start, dw, dh, 8, (255, 45, 50))
    draw_7segment(em_draw, x_start + (dw + 10) * 2, y_start, dw, dh, 0, (255, 45, 50))
    draw_7segment(em_draw, x_time, y_start, dw, dh, 0, (255, 45, 50))
    draw_7segment(em_draw, x_time + dw + 10, y_start, dw, dh, 2, (255, 45, 50))
    draw_7segment(em_draw, x_lower, y_start, dw, dh, 1, (255, 45, 50))
    draw_7segment(em_draw, x_lower + dw + 10, y_start, dw, dh, 8, (255, 45, 50))
    draw_7segment(em_draw, x_lower + (dw + 10) * 2, y_start, dw, dh, 0, (255, 45, 50))

    # Cyan buttons emission
    em_draw.rectangle([(btn_centers[0] - 8, btn_y - 2), (btn_centers[0] + 8, btn_y + 2)], fill=(0, 240, 255))
    em_draw.rectangle([(btn_centers[4] - 8, btn_y - 2), (btn_centers[4] + 8, btn_y + 2)], fill=(0, 240, 255))

    # Power button emission
    em_draw.arc([(p_cx - 18, p_cy - 18), (p_cx + 18, p_cy + 18)], start=120, end=420, fill=(255, 45, 50), width=3)
    em_draw.line([(p_cx, p_cy - 20), (p_cx, p_cy - 4)], fill=(255, 45, 50), width=3)

    em_img_blurred = em_img.filter(ImageFilter.GaussianBlur(radius=1.5))
    em_path = os.path.join(out_dir, "T_Grill_Display_Emission.png")
    em_img_blurred.save(em_path)
    print(f"Generated display emission map: {em_path}")

if __name__ == "__main__":
    tex_dir = "Assets/TramChanh/Art/Textures"
    generate_display_textures(tex_dir)
