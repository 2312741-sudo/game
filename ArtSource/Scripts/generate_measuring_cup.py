#!/usr/bin/env python3
"""
Generates the authentic 500 mL graduated measuring cup:
1. Texture T_BatterMeasureCup_BaseColor.png with crisp red graduations (100, 200, 300, 400, 500 mL),
   cup fractions (1/2, 1, 2 cup), tick marks, and frosted volume bands matching user photo media_1792093825030_2731399e.png.
2. OBJ SM_BatterMeasureCup_500ml.obj with tapered beaker body, pouring spout, open-bottom ergonomic handle,
   and clean cylindrical UV mapping.
"""
import math
import os
from PIL import Image, ImageDraw, ImageFont

def generate_texture(out_path):
    width, height = 1024, 1024
    # Transparent background with subtle clear plastic sheen
    img = Image.new("RGBA", (width, height), (240, 248, 255, 30))
    draw = ImageDraw.Draw(img)

    # Try loading clean font
    font_path = "/System/Library/Fonts/Helvetica.ttc"
    try:
        font_large = ImageFont.truetype(font_path, 34)
        font_med = ImageFont.truetype(font_path, 26)
        font_small = ImageFont.truetype(font_path, 20)
    except Exception:
        font_large = ImageFont.load_default()
        font_med = ImageFont.load_default()
        font_small = ImageFont.load_default()

    # Red color from authentic product photo
    red_color = (215, 35, 45, 255)
    frosted_band_color = (255, 255, 255, 55)

    # The markings are mapped to the front-right quadrant of the cylinder (U: 0.15 .. 0.50)
    # V goes from 0.0 (bottom) to 1.0 (top)
    # In pixel space: Y goes from height (bottom) to 0 (top)
    
    # 500ml cup levels (bottom y_norm ~ 0.10, 500ml y_norm ~ 0.85)
    levels = [
        (100, 0.22, "100", "1/2"),
        (200, 0.38, "200", "1"),
        (300, 0.54, "300", ""),
        (400, 0.70, "400", "2"),
        (500, 0.86, "500", "cup"),
    ]

    ruler_x = int(width * 0.32)

    # Draw frosted horizontal bands behind numbers across U: 0.12 .. 0.48
    for ml, y_norm, ml_str, cup_str in levels:
        py = int(height * (1.0 - y_norm))
        band_h = 36
        draw.rectangle([int(width * 0.10), py - band_h // 2, int(width * 0.48), py + band_h // 2], fill=frosted_band_color)

    # Draw vertical red gauge line
    py_bottom = int(height * (1.0 - 0.15))
    py_top = int(height * (1.0 - 0.90))
    draw.line([(ruler_x, py_bottom), (ruler_x, py_top)], fill=red_color, width=3)

    # Draw tick marks every 10 ml
    for ml_val in range(100, 501, 10):
        norm = 0.22 + (ml_val - 100) / 400.0 * (0.86 - 0.22)
        py = int(height * (1.0 - norm))
        if ml_val % 100 == 0:
            tick_len = 24
            line_w = 4
        elif ml_val % 50 == 0:
            tick_len = 16
            line_w = 3
        else:
            tick_len = 8
            line_w = 2
        draw.line([(ruler_x - tick_len // 2, py), (ruler_x + tick_len // 2, py)], fill=red_color, width=line_w)

    # Draw text labels
    for ml, y_norm, ml_str, cup_str in levels:
        py = int(height * (1.0 - y_norm))
        # Long indicator lines
        draw.line([(ruler_x + 14, py), (ruler_x + 60, py)], fill=red_color, width=3)
        draw.line([(ruler_x - 60, py), (ruler_x - 14, py)], fill=red_color, width=3)

        # Numbers on the right: "100", "200", etc.
        draw.text((ruler_x + 70, py - 18), ml_str, fill=red_color, font=font_large)
        if ml == 500:
            draw.text((ruler_x + 155, py - 24), "mL", fill=red_color, font=font_small)

        # Cup fractions on the left
        if cup_str:
            if cup_str == "cup":
                draw.text((ruler_x - 120, py - 24), "cup", fill=red_color, font=font_small)
            else:
                draw.text((ruler_x - 90, py - 18), cup_str, fill=red_color, font=font_med)

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    img.save(out_path)
    print(f"Generated cup texture: {out_path}")

def generate_obj(out_path):
    verts = []
    uvs = []
    normals = []
    faces = []

    def add_v(x, y, z):
        verts.append((x, y, z))
        return len(verts)

    def add_uv(u, v):
        uvs.append((u, v))
        return len(uvs)

    def add_n(nx, ny, nz):
        l = math.hypot(nx, ny, nz) or 1.0
        normals.append((nx / l, ny / l, nz / l))
        return len(normals)

    def add_quad(v1, v2, v3, v4, uv1, uv2, uv3, uv4, n):
        faces.append([(v1, uv1, n), (v2, uv2, n), (v3, uv3, n)])
        faces.append([(v1, uv1, n), (v3, uv3, n), (v4, uv4, n)])

    def add_tri(v1, v2, v3, uv1, uv2, uv3, n):
        faces.append([(v1, uv1, n), (v2, uv2, n), (v3, uv3, n)])

    # Dimensions matching authentic 500ml cup:
    # Height: 0.125m (125mm)
    # Bottom radius: 0.040m (80mm diameter)
    # Top rim radius: 0.052m (104mm diameter)
    # Pour spout reaches out to 0.065m on -Z
    # Handle on +Z: extends from rim out to 0.082m, down to 0.025m
    h = 0.125
    r_bot = 0.040
    r_top = 0.052
    segs = 32

    # 1. Base bottom cap (at y = 0.000)
    c_bot = add_v(0, 0, 0)
    uv_c = add_uv(0.5, 0.5)
    n_down = add_n(0, -1, 0)
    bot_ring = []
    for i in range(segs):
        ang = 2.0 * math.pi * i / segs
        x = r_bot * math.sin(ang)
        z = r_bot * math.cos(ang)
        bot_ring.append(add_v(x, 0.0, z))
    for i in range(segs):
        ni = (i + 1) % segs
        uv_a = add_uv(0.5 + 0.3 * math.sin(2.0 * math.pi * i / segs), 0.5 + 0.3 * math.cos(2.0 * math.pi * i / segs))
        uv_b = add_uv(0.5 + 0.3 * math.sin(2.0 * math.pi * ni / segs), 0.5 + 0.3 * math.cos(2.0 * math.pi * ni / segs))
        add_tri(c_bot, bot_ring[ni], bot_ring[i], uv_c, uv_b, uv_a, n_down)

    # 2. Outer beaker wall with pouring spout at -Z (angle ~ pi)
    top_ring = []
    for i in range(segs):
        ang = 2.0 * math.pi * i / segs
        # Flaring spout on -Z (ang near pi)
        diff = abs(ang - math.pi)
        if diff < 0.35:
            spout_factor = math.cos(diff / 0.35 * math.pi * 0.5)
            r_curr = r_top + 0.013 * spout_factor
            y_curr = h + 0.007 * spout_factor
        else:
            r_curr = r_top
            y_curr = h
        x = r_curr * math.sin(ang)
        z = r_curr * math.cos(ang)
        top_ring.append(add_v(x, y_curr, z))

    for i in range(segs):
        ni = (i + 1) % segs
        u1 = i / float(segs)
        u2 = (i + 1) / float(segs)
        uv_b1 = add_uv(u1, 0.05)
        uv_b2 = add_uv(u2, 0.05)
        uv_t2 = add_uv(u2, 0.95)
        uv_t1 = add_uv(u1, 0.95)

        ang_mid = 2.0 * math.pi * (i + 0.5) / segs
        n_side = add_n(math.sin(ang_mid), 0.12, math.cos(ang_mid))
        add_quad(bot_ring[i], bot_ring[ni], top_ring[ni], top_ring[i],
                 uv_b1, uv_b2, uv_t2, uv_t1, n_side)

    # 3. Inner wall (hollow inside)
    r_bot_in = r_bot - 0.0025
    r_top_in = r_top - 0.0025
    c_in = add_v(0, 0.003, 0)
    n_up = add_n(0, 1, 0)
    in_bot_ring = []
    in_top_ring = []
    for i in range(segs):
        ang = 2.0 * math.pi * i / segs
        diff = abs(ang - math.pi)
        if diff < 0.35:
            spout_factor = math.cos(diff / 0.35 * math.pi * 0.5)
            r_curr = r_top_in + 0.012 * spout_factor
            y_curr = h + 0.007 * spout_factor
        else:
            r_curr = r_top_in
            y_curr = h
        in_bot_ring.append(add_v(r_bot_in * math.sin(ang), 0.003, r_bot_in * math.cos(ang)))
        in_top_ring.append(add_v(r_curr * math.sin(ang), y_curr, r_curr * math.cos(ang)))

    for i in range(segs):
        ni = (i + 1) % segs
        add_tri(c_in, in_bot_ring[i], in_bot_ring[ni], uv_c, uv_c, uv_c, n_up)
        ang_mid = 2.0 * math.pi * (i + 0.5) / segs
        n_in = add_n(-math.sin(ang_mid), 0.12, -math.cos(ang_mid))
        add_quad(in_bot_ring[ni], in_bot_ring[i], in_top_ring[i], in_top_ring[ni],
                 uv_c, uv_c, uv_c, uv_c, n_in)
        # Rim top edge connecting outer and inner
        n_rim = add_n(0, 1, 0)
        add_quad(top_ring[i], top_ring[ni], in_top_ring[ni], in_top_ring[i],
                 uv_c, uv_c, uv_c, uv_c, n_rim)

    # 4. Ergonomic Open Handle on +Z side (ang ~ 0)
    # The handle arches out from top rim at Z = r_top out to Z = 0.082m,
    # curves down smoothly to Y = 0.025m, width = 0.016m, thickness = 0.007m.
    h_segs = 12
    handle_profile = []
    for s in range(h_segs + 1):
        t = s / float(h_segs)
        if t < 0.25:
            # Arch outward from rim
            lt = t / 0.25
            hy = h - 0.010 * lt
            hz = r_top + 0.030 * math.sin(lt * math.pi * 0.5)
        else:
            # Straight downward taper with slight flare at bottom
            lt = (t - 0.25) / 0.75
            hy = (h - 0.010) - lt * (h - 0.035)
            hz = (r_top + 0.030) - lt * 0.008 + 0.004 * math.sin(lt * math.pi)
        handle_profile.append((hy, hz))

    hw = 0.008  # half width
    hth = 0.0035 # half thickness
    n_h_out = add_n(0, 0, 1)
    n_h_in = add_n(0, 0, -1)
    n_h_l = add_n(-1, 0, 0)
    n_h_r = add_n(1, 0, 0)

    h_sections = []
    for hy, hz in handle_profile:
        v_tl = add_v(-hw, hy + hth, hz)
        v_tr = add_v(hw, hy + hth, hz)
        v_br = add_v(hw, hy - hth, hz)
        v_bl = add_v(-hw, hy - hth, hz)
        h_sections.append((v_tl, v_tr, v_br, v_bl))

    uv_h = add_uv(0.05, 0.05)
    for s in range(h_segs):
        s1 = h_sections[s]
        s2 = h_sections[s + 1]
        # Front face (+Z)
        add_quad(s1[0], s1[1], s2[1], s2[0], uv_h, uv_h, uv_h, uv_h, n_h_out)
        # Back face (-Z)
        add_quad(s2[3], s2[2], s1[2], s1[3], uv_h, uv_h, uv_h, uv_h, n_h_in)
        # Left face (-X)
        add_quad(s1[3], s1[0], s2[0], s2[3], uv_h, uv_h, uv_h, uv_h, n_h_l)
        # Right face (+X)
        add_quad(s1[1], s1[2], s2[2], s2[1], uv_h, uv_h, uv_h, uv_h, n_h_r)

    # Bottom cap of handle
    s_end = h_sections[-1]
    n_h_bot = add_n(0, -1, 0)
    add_quad(s_end[3], s_end[2], s_end[1], s_end[0], uv_h, uv_h, uv_h, uv_h, n_h_bot)

    # Write OBJ file
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, "w", encoding="utf-8") as f:
        f.write("# Wavefront OBJ: SM_BatterMeasureCup_500ml\n")
        f.write("o SM_BatterMeasureCup_500ml\n")
        for x, y, z in verts:
            f.write(f"v {x:.6f} {y:.6f} {z:.6f}\n")
        for u, v in uvs:
            f.write(f"vt {u:.6f} {v:.6f}\n")
        for nx, ny, nz in normals:
            f.write(f"vn {nx:.6f} {ny:.6f} {nz:.6f}\n")
        for face in faces:
            f.write("f " + " ".join(f"{v}/{vt}/{vn}" for v, vt, vn in face) + "\n")
    print(f"Generated cup model: {out_path} ({len(verts)} verts, {len(faces)} tris)")

if __name__ == "__main__":
    tex_path = "Assets/TramChanh/Art/Textures/T_BatterMeasureCup_BaseColor.png"
    obj_path = "Assets/TramChanh/Art/Models/Props/SM_BatterMeasureCup_500ml.obj"
    generate_texture(tex_path)
    generate_obj(obj_path)
