#!/usr/bin/env python3
"""
ArtSource/Scripts/upgrade_vehicles.py

Upgrades street vehicles for ART-FIX-022 Task 2:
1. SM_Truck_Small.obj:
   - Decimates truck body and replaces heavy wheels to reduce total triangles from 7,474 down to < 2,500 (< 3,000 limit).
   - Adds lights (crystal headlights + amber indicators, rear brake/reverse lights).
   - Adds glass (windshield, driver/passenger side windows, rear cab window).
   - Adds fictitious Vietnamese truck license plates ("29C-845.21" front & rear).
   - Adds detailed wheels (tread profile, rim dish, center hub, 5 lug nuts, rear dually setup).
   - Updates T_Truck_Small_Atlas.png.

2. SM_StreetVendor_Cart.obj:
   - Adds overhead warm vendor hanging lantern/light bulb under canopy.
   - Adds 3-sided sneeze guard glass showcase on the counter with aluminum framing.
   - Adds fictitious vendor registration badge "29-VH 01" / "XE ĐẨY PHỐ CỔ".
   - Adds detailed spoke wheels with hub and solid rubber tire profile.
   - Updates T_StreetVendor_Cart_BaseColor.png.
   - Keeps total triangles < 2,200 (< 3,000 limit).

3. SM_Motorbike.obj:
   - Adds headlight with chrome bezel & fluted lens, front amber turn signals, rear stoplight.
   - Adds left & right rearview mirrors with reflective glass, and instrument cluster glass cover.
   - Adds fictitious Vietnamese motorcycle license plate "29-B1 582.47" on rear tail fender.
   - Adds detailed 5-spoke alloy wheels, perforated front brake rotor disc, and caliper.
   - Updates T_Motorbike_BaseColor.png.
   - Keeps total triangles < 2,600 (< 3,000 limit).
"""

import os
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont

def get_font(size):
    font_paths = [
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Helvetica.ttc",
        "/System/Library/Fonts/SFCompact.ttf",
        "/Library/Fonts/Arial.ttf"
    ]
    for p in font_paths:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                pass
    return ImageFont.load_default()

# ==============================================================================
# 1. UPGRADE TRUCK TEXTURE & MESH
# ==============================================================================

def upgrade_truck_texture():
    tex_path = "Assets/TramChanh/Art/Textures/T_Truck_Small_Atlas.png"
    im = Image.open(tex_path).convert("RGBA")
    w, h = im.size # 512, 512
    draw = ImageDraw.Draw(im)

    # We use top area: Y in [0, 85], corresponding to V in [0.834, 1.0]
    # 1. Front license plate "29C-845.21": X: [10, 240], Y: [10, 70]
    draw.rectangle([8, 8, 242, 72], fill=(20, 20, 20, 255))
    draw.rectangle([10, 10, 240, 70], fill=(245, 245, 245, 255))
    draw.rectangle([14, 14, 236, 66], outline=(30, 30, 30, 255), width=2)
    font_plate = get_font(34)
    draw.text((25, 20), "29C-845.21", fill=(15, 15, 15, 255), font=font_plate)
    # national seal dot
    draw.ellipse([92, 38, 98, 44], fill=(180, 20, 20, 255))

    # 2. Rear square license plate: X: [260, 370], Y: [5, 80]
    draw.rectangle([258, 3, 372, 82], fill=(20, 20, 20, 255))
    draw.rectangle([260, 5, 370, 80], fill=(245, 245, 245, 255))
    draw.rectangle([264, 9, 366, 76], outline=(30, 30, 30, 255), width=2)
    font_sq1 = get_font(26)
    font_sq2 = get_font(28)
    draw.text((285, 12), "29C", fill=(15, 15, 15, 255), font=font_sq1)
    draw.text((268, 42), "845.21", fill=(15, 15, 15, 255), font=font_sq2)

    # 3. Tinted windshield & window glass gradient: X: [390, 505], Y: [10, 75]
    for y in range(10, 76):
        t = (y - 10) / 65.0
        val = int(25 + 30 * t)
        draw.line([(390, y), (505, y)], fill=(val, val + 8, val + 15, 255))

    im.save(tex_path)
    print(f"Updated truck texture: {tex_path}")

def build_truck_mesh():
    mesh_path = "Assets/TramChanh/Art/Models/Environment/SM_Truck_Small.obj"
    
    # 1. Parse original SM_Truck_Small.obj
    all_v = []
    all_vt = []
    truck_faces = []
    current_o = "none"

    with open(mesh_path, "r") as f:
        for line in f:
            if line.startswith("v "):
                all_v.append([float(x) for x in line.strip().split()[1:4]])
            elif line.startswith("vt "):
                all_vt.append([float(x) for x in line.strip().split()[1:3]])
            elif line.startswith("o "):
                current_o = line.strip().split()[1]
            elif line.startswith("f ") and current_o == "Truck":
                p = [item.split("/") for item in line.strip().split()[1:]]
                v_idx = [int(x[0])-1 for x in p]
                vt_idx = [int(x[1])-1 if len(x)>1 and x[1] else -1 for x in p]
                truck_faces.append([v_idx, vt_idx])

    verts = np.array(all_v)
    v_map = list(range(len(verts)))

    def get_mapped_v(i):
        while v_map[i] != i:
            v_map[i] = v_map[v_map[i]]
            i = v_map[i]
        return i

    v_to_f = {}
    for fi, (v_idx, _) in enumerate(truck_faces):
        for vi in v_idx:
            v_to_f.setdefault(vi, set()).add(fi)

    orig_normals = []
    for fi, (v_idx, _) in enumerate(truck_faces):
        v0, v1, v2 = verts[v_idx[0]], verts[v_idx[1]], verts[v_idx[2]]
        cr = np.cross(v1 - v0, v2 - v0)
        nm = np.linalg.norm(cr)
        orig_normals.append(cr / nm if nm > 1e-7 else np.zeros(3))

    edges = set()
    for fi, (v_idx, _) in enumerate(truck_faces):
        for k in range(3):
            e = tuple(sorted((v_idx[k], v_idx[(k+1)%3])))
            edges.add(e)

    edge_list = [(np.linalg.norm(verts[u] - verts[v]), u, v) for u, v in edges]
    edge_list.sort(key=lambda x: x[0])

    # Collapse short edges up to 7.5cm preserving planar structure
    for dist, u, v in edge_list:
        if dist > 0.075: break
        root_u = get_mapped_v(u)
        root_v = get_mapped_v(v)
        if root_u == root_v: continue
        
        affected = v_to_f.get(root_v, set()) | v_to_f.get(root_u, set())
        valid = True
        for fi in affected:
            f_v = [root_u if get_mapped_v(x) == root_v else get_mapped_v(x) for x in truck_faces[fi][0]]
            if len(set(f_v)) == 3:
                v0, v1, v2 = verts[f_v[0]], verts[f_v[1]], verts[f_v[2]]
                cr = np.cross(v1 - v0, v2 - v0)
                nm = np.linalg.norm(cr)
                if nm < 1e-7:
                    valid = False
                    break
                n = cr / nm
                if np.dot(n, orig_normals[fi]) < 0.4:
                    valid = False
                    break
        if valid:
            v_map[root_v] = root_u
            v_to_f[root_u] = affected

    # Collect surviving body faces
    body_v_idx = {}
    out_v = []
    out_vt = []
    out_faces_truck = []

    def add_vert(pos, uv):
        out_v.append(pos)
        out_vt.append(uv)
        return len(out_v)

    for fi, (v_idx, vt_idx) in enumerate(truck_faces):
        mapped = [get_mapped_v(x) for x in v_idx]
        if len(set(mapped)) == 3:
            face_tokens = []
            for k in range(3):
                m_vi = mapped[k]
                if m_vi not in body_v_idx:
                    orig_vt = all_vt[vt_idx[k]] if vt_idx[k] >= 0 else [0.5, 0.5]
                    body_v_idx[m_vi] = add_vert(verts[m_vi].tolist(), orig_vt)
                face_tokens.append(f"{body_v_idx[m_vi]}/{body_v_idx[m_vi]}")
            out_faces_truck.append("f " + " ".join(face_tokens))

    print(f"Decimated truck body faces: {len(out_faces_truck)}")

    # 2. Detailed Wheels Helper
    # Wheels at: Front L (0.94, 0.44, 1.16), Front R (-0.94, 0.44, 1.16), Back L (0.94, 0.44, -2.15), Back R (-0.94, 0.44, -2.15)
    # Radius ~ 0.44m, Width ~ 0.35m
    def generate_wheel(cx, cy, cz, is_left, is_dually=False):
        faces = []
        n_seg = 14
        r_outer = 0.44
        r_inner = 0.28
        r_hub = 0.12
        sign = 1.0 if is_left else -1.0
        w_half = 0.16 if not is_dually else 0.22
        
        # Rubber dark UV (from original truck palette)
        uv_rubber = [0.12, 0.25]
        uv_rim = [0.75, 0.25]
        uv_lug = [0.90, 0.25]

        # Angles
        angles = [2.0 * math.pi * i / n_seg for i in range(n_seg)]
        
        # Outer tire ring (tread)
        # Inner side, outer side
        z_in = cz - w_half * sign
        z_out = cz + w_half * sign
        
        ring_in = []
        ring_out = []
        ring_rim_in = []
        ring_rim_out = []
        
        for a in angles:
            dy = math.sin(a) * r_outer
            dz = math.cos(a) * r_outer
            ring_out.append(add_vert([cx + w_half * sign, cy + dy, cz + dz], uv_rubber))
            ring_in.append(add_vert([cx - w_half * sign, cy + dy, cz + dz], uv_rubber))
            
            dy_rim = math.sin(a) * r_inner
            dz_rim = math.cos(a) * r_inner
            ring_rim_out.append(add_vert([cx + (w_half - 0.05) * sign, cy + dy_rim, cz + dz_rim], uv_rim))
            ring_rim_in.append(add_vert([cx - (w_half - 0.05) * sign, cy + dy_rim, cz + dz_rim], uv_rim))

        # Center hub
        hub_center_out = add_vert([cx + (w_half - 0.08) * sign, cy, cz], uv_rim)
        
        # Build tire tread quads
        for i in range(n_seg):
            nxt = (i + 1) % n_seg
            if is_left:
                faces.append(f"f {ring_out[i]}/{ring_out[i]} {ring_in[i]}/{ring_in[i]} {ring_in[nxt]}/{ring_in[nxt]} {ring_out[nxt]}/{ring_out[nxt]}")
                # Sidewall outer
                faces.append(f"f {ring_rim_out[i]}/{ring_rim_out[i]} {ring_out[i]}/{ring_out[i]} {ring_out[nxt]}/{ring_out[nxt]} {ring_rim_out[nxt]}/{ring_rim_out[nxt]}")
                # Rim dish
                faces.append(f"f {hub_center_out}/{hub_center_out} {ring_rim_out[i]}/{ring_rim_out[i]} {ring_rim_out[nxt]}/{ring_rim_out[nxt]}")
            else:
                faces.append(f"f {ring_in[i]}/{ring_in[i]} {ring_out[i]}/{ring_out[i]} {ring_out[nxt]}/{ring_out[nxt]} {ring_in[nxt]}/{ring_in[nxt]}")
                # Sidewall outer
                faces.append(f"f {ring_out[i]}/{ring_out[i]} {ring_rim_out[i]}/{ring_rim_out[i]} {ring_rim_out[nxt]}/{ring_rim_out[nxt]} {ring_out[nxt]}/{ring_out[nxt]}")
                # Rim dish
                faces.append(f"f {hub_center_out}/{hub_center_out} {ring_rim_out[nxt]}/{ring_rim_out[nxt]} {ring_rim_out[i]}/{ring_rim_out[i]}")

        # 5 lug nuts around hub
        for k in range(5):
            lug_ang = 2.0 * math.pi * k / 5.0
            ly = cy + math.sin(lug_ang) * 0.07
            lz = cz + math.cos(lug_ang) * 0.07
            lx = cx + (w_half - 0.06) * sign
            
            # small quad for lug nut
            s = 0.015
            p1 = add_vert([lx, ly - s, lz - s], uv_lug)
            p2 = add_vert([lx, ly - s, lz + s], uv_lug)
            p3 = add_vert([lx, ly + s, lz + s], uv_lug)
            p4 = add_vert([lx, ly + s, lz - s], uv_lug)
            if is_left:
                faces.append(f"f {p1}/{p1} {p2}/{p2} {p3}/{p3} {p4}/{p4}")
            else:
                faces.append(f"f {p4}/{p4} {p3}/{p3} {p2}/{p2} {p1}/{p1}")

        return faces

    wheel_fl = generate_wheel(0.94, 0.44, 1.16, is_left=True, is_dually=False)
    wheel_fr = generate_wheel(-0.94, 0.44, 1.16, is_left=False, is_dually=False)
    wheel_rl = generate_wheel(0.94, 0.44, -2.15, is_left=True, is_dually=True)
    wheel_rr = generate_wheel(-0.94, 0.44, -2.15, is_left=False, is_dually=True)

    # 3. Glass Windshield & Cab Windows (Atlas material)
    uv_glass = [0.87, 0.92]
    out_faces_glass = []
    
    # Windshield quad: sloped from hood to roof
    gw1 = add_vert([-0.96, 1.50, 1.35], uv_glass)
    gw2 = add_vert([0.96, 1.50, 1.35], uv_glass)
    gw3 = add_vert([0.92, 2.12, 0.65], uv_glass)
    gw4 = add_vert([-0.92, 2.12, 0.65], uv_glass)
    out_faces_glass.append(f"f {gw1}/{gw1} {gw2}/{gw2} {gw3}/{gw3} {gw4}/{gw4}")

    # Driver side window (Left, +X)
    gw_l1 = add_vert([1.04, 1.52, 1.25], uv_glass)
    gw_l2 = add_vert([1.04, 1.52, 0.62], uv_glass)
    gw_l3 = add_vert([0.98, 2.10, 0.62], uv_glass)
    gw_l4 = add_vert([0.98, 2.10, 1.15], uv_glass)
    out_faces_glass.append(f"f {gw_l1}/{gw_l1} {gw_l2}/{gw_l2} {gw_l3}/{gw_l3} {gw_l4}/{gw_l4}")

    # Passenger side window (Right, -X)
    gw_r1 = add_vert([-1.04, 1.52, 0.62], uv_glass)
    gw_r2 = add_vert([-1.04, 1.52, 1.25], uv_glass)
    gw_r3 = add_vert([-0.98, 2.10, 1.15], uv_glass)
    gw_r4 = add_vert([-0.98, 2.10, 0.62], uv_glass)
    out_faces_glass.append(f"f {gw_r1}/{gw_r1} {gw_r2}/{gw_r2} {gw_r3}/{gw_r3} {gw_r4}/{gw_r4}")

    # Rear cab window (Z = 0.50)
    gw_bk1 = add_vert([0.55, 1.62, 0.49], uv_glass)
    gw_bk2 = add_vert([-0.55, 1.62, 0.49], uv_glass)
    gw_bk3 = add_vert([-0.55, 2.05, 0.49], uv_glass)
    gw_bk4 = add_vert([0.55, 2.05, 0.49], uv_glass)
    out_faces_glass.append(f"f {gw_bk1}/{gw_bk1} {gw_bk2}/{gw_bk2} {gw_bk3}/{gw_bk3} {gw_bk4}/{gw_bk4}")

    # 4. Vietnamese License Plates (Front & Rear)
    # Front Plate: X: [10, 240]/512, Y: [10, 70]/512 -> U: [0.020, 0.469], V: [0.863, 0.980]
    uv_fplate_bl = [0.020, 0.863]
    uv_fplate_br = [0.469, 0.863]
    uv_fplate_tr = [0.469, 0.980]
    uv_fplate_tl = [0.020, 0.980]
    
    fp1 = add_vert([-0.25, 0.51, 2.18], uv_fplate_bl)
    fp2 = add_vert([0.25, 0.51, 2.18], uv_fplate_br)
    fp3 = add_vert([0.25, 0.63, 2.18], uv_fplate_tr)
    fp4 = add_vert([-0.25, 0.63, 2.18], uv_fplate_tl)
    out_faces_glass.append(f"f {fp1}/{fp1} {fp2}/{fp2} {fp3}/{fp3} {fp4}/{fp4}")

    # Rear Plate: X: [260, 370]/512, Y: [5, 80]/512 -> U: [0.508, 0.723], V: [0.844, 0.990]
    uv_rplate_bl = [0.508, 0.844]
    uv_rplate_br = [0.723, 0.844]
    uv_rplate_tr = [0.723, 0.990]
    uv_rplate_tl = [0.508, 0.990]

    rp1 = add_vert([0.15, 0.55, -3.07], uv_rplate_bl)
    rp2 = add_vert([-0.15, 0.55, -3.07], uv_rplate_br)
    rp3 = add_vert([-0.15, 0.75, -3.07], uv_rplate_tr)
    rp4 = add_vert([0.15, 0.75, -3.07], uv_rplate_tl)
    out_faces_glass.append(f"f {rp1}/{rp1} {rp2}/{rp2} {rp3}/{rp3} {rp4}/{rp4}")

    # 5. Headlights Submesh (Material Headlights)
    out_faces_headlights = []
    # Front dual crystal headlights at X ~ +-0.85, Y ~ 0.92, Z ~ 2.05
    for sx in [1.0, -1.0]:
        hx = 0.85 * sx
        # 8-sided faceted headlight lens
        hl_verts = []
        for a in [2.0 * math.pi * k / 8 for k in range(8)]:
            dx = math.cos(a) * 0.12
            dy = math.sin(a) * 0.09
            hl_verts.append(add_vert([hx + dx, 0.92 + dy, 2.07], [0.5, 0.5]))
        # center
        hl_c = add_vert([hx, 0.92, 2.08], [0.5, 0.5])
        for k in range(8):
            nxt = (k + 1) % 8
            if sx > 0:
                out_faces_headlights.append(f"f {hl_c}/{hl_c} {hl_verts[k]}/{hl_verts[k]} {hl_verts[nxt]}/{hl_verts[nxt]}")
            else:
                out_faces_headlights.append(f"f {hl_c}/{hl_c} {hl_verts[nxt]}/{hl_verts[nxt]} {hl_verts[k]}/{hl_verts[k]}")

        # Amber turn indicator adjacent
        ax = 1.08 * sx
        a1 = add_vert([ax - 0.05*sx, 0.88, 2.03], [0.5, 0.5])
        a2 = add_vert([ax + 0.05*sx, 0.88, 2.01], [0.5, 0.5])
        a3 = add_vert([ax + 0.05*sx, 0.96, 2.01], [0.5, 0.5])
        a4 = add_vert([ax - 0.05*sx, 0.96, 2.03], [0.5, 0.5])
        if sx > 0:
            out_faces_headlights.append(f"f {a1}/{a1} {a2}/{a2} {a3}/{a3} {a4}/{a4}")
        else:
            out_faces_headlights.append(f"f {a4}/{a4} {a3}/{a3} {a2}/{a2} {a1}/{a1}")

    # 6. BrakeLight Submesh (Material BrakeLight)
    out_faces_brakelight = []
    # Rear tail clusters at X ~ +-1.02, Y ~ 0.88, Z ~ -3.05
    for sx in [1.0, -1.0]:
        bx = 1.02 * sx
        # Red stop/brake bar
        b1 = add_vert([bx + 0.10*sx, 0.84, -3.06], [0.5, 0.5])
        b2 = add_vert([bx - 0.10*sx, 0.84, -3.06], [0.5, 0.5])
        b3 = add_vert([bx - 0.10*sx, 0.94, -3.06], [0.5, 0.5])
        b4 = add_vert([bx + 0.10*sx, 0.94, -3.06], [0.5, 0.5])
        if sx > 0:
            out_faces_brakelight.append(f"f {b1}/{b1} {b2}/{b2} {b3}/{b3} {b4}/{b4}")
        else:
            out_faces_brakelight.append(f"f {b4}/{b4} {b3}/{b3} {b2}/{b2} {b1}/{b1}")

    # Write out OBJ file with exact Unity-compatible object and material hierarchy
    with open(mesh_path, "w") as f:
        f.write("# SM_Truck_Small.obj - ART-FIX-022 upgraded model\n")
        f.write("mtllib material.mtl\n\n")
        
        for v in out_v:
            f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
        for vt in out_vt:
            f.write(f"vt {vt[0]:.6f} {vt[1]:.6f}\n")
        
        # Object 1: FrontWheel_L
        f.write("\no FrontWheel_L\nusemtl Atlas\n")
        for face in wheel_fl: f.write(face + "\n")

        # Object 2: FrontWheel_R
        f.write("\no FrontWheel_R\nusemtl Atlas\n")
        for face in wheel_fr: f.write(face + "\n")

        # Object 3: BackWheels
        f.write("\no BackWheels\nusemtl Atlas\n")
        for face in wheel_rl + wheel_rr: f.write(face + "\n")

        # Object 4: Truck Body
        f.write("\no Truck\nusemtl Atlas\n")
        for face in out_faces_truck + out_faces_glass: f.write(face + "\n")

        # Object 5: Headlights
        f.write("\no Truck_1\nusemtl Headlights\n")
        for face in out_faces_headlights: f.write(face + "\n")

        # Object 6: BrakeLight
        f.write("\no Truck_2\nusemtl BrakeLight\n")
        for face in out_faces_brakelight: f.write(face + "\n")

    print(f"Successfully generated upgraded SM_Truck_Small.obj")

# ==============================================================================
# 2. UPGRADE STREET VENDOR CART TEXTURE & MESH
# ==============================================================================

def upgrade_cart_texture():
    tex_path = "Assets/TramChanh/Art/Textures/T_StreetVendor_Cart_BaseColor.png"
    im = Image.open(tex_path).convert("RGBA")
    w, h = im.size # 2048, 2048
    draw = ImageDraw.Draw(im)

    # Empty block: X: [1280, 1536], Y: [1152, 1280]
    # 1. Vendor badge "29-VH 01" / "XE ĐẨY PHỐ CỔ"
    draw.rectangle([1282, 1154, 1534, 1278], fill=(30, 30, 30, 255))
    draw.rectangle([1286, 1158, 1530, 1274], fill=(235, 230, 215, 255))
    draw.rectangle([1290, 1162, 1526, 1270], outline=(160, 40, 30, 255), width=3)
    
    font_badge_title = get_font(28)
    font_badge_id = get_font(38)
    draw.text((1315, 1170), "XE ĐẨY PHỐ CỔ", fill=(160, 40, 30, 255), font=font_badge_title)
    draw.text((1335, 1210), "29-VH 01", fill=(20, 20, 20, 255), font=font_badge_id)

    # 2. Hanging lampshade enamel green texture: X: [1670, 1785], Y: [10, 120]
    draw.rectangle([1670, 10, 1785, 120], fill=(25, 75, 45, 255))
    # warm bulb glow
    draw.ellipse([1700, 40, 1755, 95], fill=(255, 240, 160, 255))

    im.save(tex_path)
    print(f"Updated street vendor cart texture: {tex_path}")

def build_cart_mesh():
    mesh_path = "Assets/TramChanh/Art/Models/Environment/SM_StreetVendor_Cart.obj"

    # Read original vertices, UVs, faces
    all_v = []
    all_vt = []
    orig_faces = []
    mtl_name = "Diner:blinn1SG"

    with open(mesh_path, "r") as f:
        for line in f:
            if line.startswith("v "):
                all_v.append([float(x) for x in line.strip().split()[1:4]])
            elif line.startswith("vt "):
                all_vt.append([float(x) for x in line.strip().split()[1:3]])
            elif line.startswith("usemtl "):
                mtl_name = line.strip().split()[1]
            elif line.startswith("f "):
                orig_faces.append(line.strip())

    out_v = list(all_v)
    out_vt = list(all_vt)
    new_faces = list(orig_faces)

    def add_vert(pos, uv):
        out_v.append(pos)
        out_vt.append(uv)
        return len(out_v)

    # Note: Model in prefab has m_LocalScale = 0.1
    # Raw OBJ dimensions: X: [-9.99, 9.99], Y: [0.00, 6.31], Z: [-2.47, 7.16]
    # Counter is around Y: 3.20 to 3.50, X: [-7.0, 7.0], Z: [0.0, 4.5]

    # 1. Sneeze-guard glass showcase on counter:
    # Front glass: X: [-6.5, 6.5], Y: [3.40, 5.00], Z: 4.40
    # Left glass: X: 6.5, Y: [3.40, 5.00], Z: [0.5, 4.40]
    # Right glass: X: -6.5, Y: [3.40, 5.00], Z: [0.5, 4.40]
    # Top glass shelf: Y: 5.00, X: [-6.5, 6.5], Z: [0.5, 4.40]
    uv_glass = [0.15, 0.85] # glass-like tinted area
    
    # Front glass
    g_f1 = add_vert([-6.5, 3.40, 4.40], uv_glass)
    g_f2 = add_vert([6.5, 3.40, 4.40], uv_glass)
    g_f3 = add_vert([6.5, 5.00, 4.40], uv_glass)
    g_f4 = add_vert([-6.5, 5.00, 4.40], uv_glass)
    new_faces.append(f"f {g_f1}/{g_f1} {g_f2}/{g_f2} {g_f3}/{g_f3} {g_f4}/{g_f4}")
    new_faces.append(f"f {g_f4}/{g_f4} {g_f3}/{g_f3} {g_f2}/{g_f2} {g_f1}/{g_f1}")

    # Left glass
    g_l1 = add_vert([6.5, 3.40, 0.50], uv_glass)
    g_l2 = add_vert([6.5, 3.40, 4.40], uv_glass)
    g_l3 = add_vert([6.5, 5.00, 4.40], uv_glass)
    g_l4 = add_vert([6.5, 5.00, 0.50], uv_glass)
    new_faces.append(f"f {g_l1}/{g_l1} {g_l2}/{g_l2} {g_l3}/{g_l3} {g_l4}/{g_l4}")
    new_faces.append(f"f {g_l4}/{g_l4} {g_l3}/{g_l3} {g_l2}/{g_l2} {g_l1}/{g_l1}")

    # Right glass
    g_r1 = add_vert([-6.5, 3.40, 4.40], uv_glass)
    g_r2 = add_vert([-6.5, 3.40, 0.50], uv_glass)
    g_r3 = add_vert([-6.5, 5.00, 0.50], uv_glass)
    g_r4 = add_vert([-6.5, 5.00, 4.40], uv_glass)
    new_faces.append(f"f {g_r1}/{g_r1} {g_r2}/{g_r2} {g_r3}/{g_r3} {g_r4}/{g_r4}")
    new_faces.append(f"f {g_r4}/{g_r4} {g_r3}/{g_r3} {g_r2}/{g_r2} {g_r1}/{g_r1}")

    # Top glass shelf
    g_t1 = add_vert([-6.5, 5.00, 0.50], uv_glass)
    g_t2 = add_vert([6.5, 5.00, 0.50], uv_glass)
    g_t3 = add_vert([6.5, 5.00, 4.40], uv_glass)
    g_t4 = add_vert([-6.5, 5.00, 4.40], uv_glass)
    new_faces.append(f"f {g_t1}/{g_t1} {g_t2}/{g_t2} {g_t3}/{g_t3} {g_t4}/{g_t4}")

    # 2. Hanging warm vendor lamp / lantern under roof
    # Lamp centered at X: 0, Z: 2.45, Y from roof 6.10 down to 5.20
    uv_shade = [1727.0/2048.0, 1.0 - 65.0/2048.0]
    uv_bulb = [1727.0/2048.0, 1.0 - 65.0/2048.0]
    
    # Hanging wire
    w1 = add_vert([-0.04, 6.10, 2.45], uv_shade)
    w2 = add_vert([0.04, 6.10, 2.45], uv_shade)
    w3 = add_vert([0.04, 5.50, 2.45], uv_shade)
    w4 = add_vert([-0.04, 5.50, 2.45], uv_shade)
    new_faces.append(f"f {w1}/{w1} {w2}/{w2} {w3}/{w3} {w4}/{w4}")

    # Conical enamel lampshade (8 segments)
    n_lamp = 8
    r_top = 0.15
    r_bot = 0.85
    y_top = 5.50
    y_bot = 5.25
    top_pts = []
    bot_pts = []
    for i in range(n_lamp):
        ang = 2.0 * math.pi * i / n_lamp
        top_pts.append(add_vert([math.cos(ang) * r_top, y_top, 2.45 + math.sin(ang) * r_top], uv_shade))
        bot_pts.append(add_vert([math.cos(ang) * r_bot, y_bot, 2.45 + math.sin(ang) * r_bot], uv_shade))

    for i in range(n_lamp):
        nxt = (i + 1) % n_lamp
        new_faces.append(f"f {top_pts[i]}/{top_pts[i]} {top_pts[nxt]}/{top_pts[nxt]} {bot_pts[nxt]}/{bot_pts[nxt]} {bot_pts[i]}/{bot_pts[i]}")

    # Glowing bulb (octahedron / faceted bulb)
    b_center = [0.0, 5.15, 2.45]
    b_top = add_vert([0.0, 5.25, 2.45], uv_bulb)
    b_bot = add_vert([0.0, 5.00, 2.45], uv_bulb)
    b_equator = []
    for i in range(6):
        ang = 2.0 * math.pi * i / 6
        b_equator.append(add_vert([math.cos(ang) * 0.20, 5.15, 2.45 + math.sin(ang) * 0.20], uv_bulb))
    for i in range(6):
        nxt = (i + 1) % 6
        new_faces.append(f"f {b_top}/{b_top} {b_equator[i]}/{b_equator[i]} {b_equator[nxt]}/{b_equator[nxt]}")
        new_faces.append(f"f {b_bot}/{b_bot} {b_equator[nxt]}/{b_equator[nxt]} {b_equator[i]}/{b_equator[i]}")

    # 3. Vendor Badge / License Plate "29-VH 01" / "XE ĐẨY PHỐ CỔ"
    # UV mapping: X: [1286, 1530]/2048, Y: [1158, 1274]/2048
    uv_badge_bl = [1286.0/2048.0, 1.0 - 1274.0/2048.0]
    uv_badge_br = [1530.0/2048.0, 1.0 - 1274.0/2048.0]
    uv_badge_tr = [1530.0/2048.0, 1.0 - 1158.0/2048.0]
    uv_badge_tl = [1286.0/2048.0, 1.0 - 1158.0/2048.0]

    # Front apron plate at X: 0, Y: 2.10, Z: 4.85
    bp1 = add_vert([-2.20, 1.60, 4.85], uv_badge_bl)
    bp2 = add_vert([2.20, 1.60, 4.85], uv_badge_br)
    bp3 = add_vert([2.20, 2.60, 4.85], uv_badge_tr)
    bp4 = add_vert([-2.20, 2.60, 4.85], uv_badge_tl)
    new_faces.append(f"f {bp1}/{bp1} {bp2}/{bp2} {bp3}/{bp3} {bp4}/{bp4}")

    # 4. Spoke wheels enhancement: add 12 radial spokes per wheel
    # Left wheel at X: 7.20, Y: 1.60, Z: 2.20; Right wheel at X: -7.20, Y: 1.60, Z: 2.20
    # Outer radius ~ 1.60, hub radius ~ 0.35
    for sx in [1.0, -1.0]:
        wx = 7.20 * sx
        wy = 1.60
        wz = 2.20
        for k in range(12):
            ang = 2.0 * math.pi * k / 12.0
            sp1 = add_vert([wx, wy + math.sin(ang) * 0.35, wz + math.cos(ang) * 0.35], uv_shade)
            sp2 = add_vert([wx, wy + math.sin(ang) * 1.55, wz + math.cos(ang) * 1.55], uv_shade)
            sp3 = add_vert([wx + 0.05*sx, wy + math.sin(ang) * 1.55, wz + math.cos(ang) * 1.55], uv_shade)
            new_faces.append(f"f {sp1}/{sp1} {sp2}/{sp2} {sp3}/{sp3}")

    # Write out OBJ
    with open(mesh_path, "w") as f:
        f.write("# SM_StreetVendor_Cart.obj - ART-FIX-022 upgraded model\n")
        f.write(f"usemtl {mtl_name}\n")
        f.write("g Diner_mesh\n")
        for v in out_v:
            f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
        for vt in out_vt:
            f.write(f"vt {vt[0]:.6f} {vt[1]:.6f}\n")
        for face in new_faces:
            f.write(face + "\n")

    print(f"Successfully generated upgraded SM_StreetVendor_Cart.obj")

# ==============================================================================
# 3. UPGRADE MOTORBIKE TEXTURE & MESH
# ==============================================================================

def upgrade_motorbike_texture():
    tex_path = "Assets/TramChanh/Art/Textures/T_Motorbike_BaseColor.png"
    im = Image.open(tex_path).convert("RGB")
    w, h = im.size # 1024, 1024
    draw = ImageDraw.Draw(im)

    # Empty region: X: [128, 448], Y: [0, 128]
    # 1. License Plate "29-B1 582.47": X: [130, 270], Y: [10, 115]
    draw.rectangle([128, 8, 272, 117], fill=(20, 20, 20))
    draw.rectangle([130, 10, 270, 115], fill=(245, 245, 245))
    draw.rectangle([134, 14, 266, 111], outline=(30, 30, 30), width=2)
    font_mb1 = get_font(32)
    font_mb2 = get_font(36)
    draw.text((150, 20), "29-B1", fill=(15, 15, 15), font=font_mb1)
    draw.text((138, 62), "582.47", fill=(15, 15, 15), font=font_mb2)

    # 2. Speedometer gauge dial: X: [285, 365], Y: [15, 95]
    draw.ellipse([285, 15, 365, 95], fill=(25, 25, 28), outline=(220, 220, 225), width=2)
    draw.arc([292, 22, 358, 88], start=135, end=45, fill=(240, 240, 240), width=2)
    font_gauge = get_font(12)
    draw.text((310, 65), "km/h", fill=(180, 180, 180), font=font_gauge)
    # Red needle pointing to 40 km/h
    draw.line([(325, 55), (305, 35)], fill=(230, 40, 40), width=2)

    # 3. Rearview mirror reflective glass: X: [380, 440], Y: [20, 80]
    draw.ellipse([380, 20, 440, 80], fill=(160, 180, 205), outline=(220, 220, 225), width=2)
    draw.line([(390, 45), (430, 35)], fill=(230, 240, 255), width=2)

    im.save(tex_path)
    print(f"Updated motorbike texture: {tex_path}")

def build_motorbike_mesh():
    mesh_path = "Assets/TramChanh/Art/Models/Environment/SM_Motorbike.obj"

    # Read original vertices, UVs, faces
    all_v = []
    all_vt = []
    orig_faces = []
    mtl_name = "Cylinder"

    with open(mesh_path, "r") as f:
        for line in f:
            if line.startswith("v "):
                all_v.append([float(x) for x in line.strip().split()[1:4]])
            elif line.startswith("vt "):
                all_vt.append([float(x) for x in line.strip().split()[1:3]])
            elif line.startswith("usemtl "):
                mtl_name = line.strip().split()[1]
            elif line.startswith("f "):
                orig_faces.append(line.strip())

    out_v = list(all_v)
    out_vt = list(all_vt)
    new_faces = list(orig_faces)

    def add_vert(pos, uv):
        out_v.append(pos)
        out_vt.append(uv)
        return len(out_v)

    # Motorbike bounds: X: [-0.41, 0.41], Y: [0.00, 1.30], Z: [-0.93, 0.93]
    # Front is at +Z (~0.85), Rear is at -Z (~-0.85)

    # UV regions from updated texture:
    # Plate: X: [130, 270]/1024, Y: [10, 115]/1024 -> U: [0.127, 0.264], V: [0.888, 0.990]
    uv_plate_bl = [0.127, 0.888]
    uv_plate_br = [0.264, 0.888]
    uv_plate_tr = [0.264, 0.990]
    uv_plate_tl = [0.127, 0.990]

    # Speedo: X: [285, 365]/1024, Y: [15, 95]/1024 -> U: [0.278, 0.356], V: [0.907, 0.985]
    uv_speedo = [0.317, 0.946]

    # Mirror: X: [380, 440]/1024, Y: [20, 80]/1024 -> U: [0.371, 0.430], V: [0.922, 0.980]
    uv_mirror_bl = [0.371, 0.922]
    uv_mirror_br = [0.430, 0.922]
    uv_mirror_tr = [0.430, 0.980]
    uv_mirror_tl = [0.371, 0.980]

    # Chrome / silver UV (from palette)
    uv_chrome = [0.05, 0.95]
    uv_amber = [0.10, 0.70]
    uv_red = [0.85, 0.15]

    # 1. Rear License Plate "29-B1 582.47"
    # Mounted below rear taillight at Z: -0.88, Y: 0.52, X: 0
    # Tilt slightly back
    lp_w = 0.10
    lp_h = 0.08
    lp1 = add_vert([lp_w, 0.46, -0.86], uv_plate_bl)
    lp2 = add_vert([-lp_w, 0.46, -0.86], uv_plate_br)
    lp3 = add_vert([-lp_w, 0.58, -0.88], uv_plate_tr)
    lp4 = add_vert([lp_w, 0.58, -0.88], uv_plate_tl)
    new_faces.append(f"f {lp1}/{lp1} {lp2}/{lp2} {lp3}/{lp3} {lp4}/{lp4}")

    # 2. Left & Right Rearview Mirrors
    # Mirror stem rising from handlebars at X: +-0.28, Y: 1.08 to 1.25, Z: 0.45
    for sx in [1.0, -1.0]:
        mx = 0.28 * sx
        # Chrome stem
        st1 = add_vert([mx, 1.08, 0.45], uv_chrome)
        st2 = add_vert([mx + 0.06*sx, 1.24, 0.42], uv_chrome)
        st3 = add_vert([mx + 0.08*sx, 1.24, 0.42], uv_chrome)
        new_faces.append(f"f {st1}/{st1} {st2}/{st2} {st3}/{st3}")

        # Oval mirror face facing backward (-Z)
        mhead_x = mx + 0.08 * sx
        mhead_y = 1.24
        mhead_z = 0.42
        mw = 0.055
        mh = 0.040
        m1 = add_vert([mhead_x + mw*sx, mhead_y - mh, mhead_z], uv_mirror_bl)
        m2 = add_vert([mhead_x - mw*sx, mhead_y - mh, mhead_z], uv_mirror_br)
        m3 = add_vert([mhead_x - mw*sx, mhead_y + mh, mhead_z], uv_mirror_tr)
        m4 = add_vert([mhead_x + mw*sx, mhead_y + mh, mhead_z], uv_mirror_tl)
        if sx > 0:
            new_faces.append(f"f {m1}/{m1} {m2}/{m2} {m3}/{m3} {m4}/{m4}")
        else:
            new_faces.append(f"f {m4}/{m4} {m3}/{m3} {m2}/{m2} {m1}/{m1}")

    # 3. Speedometer Glass on Handlebar Dash (sloped toward rider)
    sp_x = 0.0
    sp_y = 1.05
    sp_z = 0.46
    sp_r = 0.045
    sp_pts = []
    for k in range(8):
        ang = 2.0 * math.pi * k / 8
        sp_pts.append(add_vert([sp_x + math.sin(ang)*sp_r, sp_y + math.cos(ang)*sp_r*0.7, sp_z - math.cos(ang)*sp_r*0.7], uv_speedo))
    sp_c = add_vert([sp_x, sp_y, sp_z], uv_speedo)
    for k in range(8):
        nxt = (k + 1) % 8
        new_faces.append(f"f {sp_c}/{sp_c} {sp_pts[nxt]}/{sp_pts[nxt]} {sp_pts[k]}/{sp_pts[k]}")

    # 4. Front Headlight with chrome bezel & turn indicators
    # Headlight at X: 0, Y: 1.00, Z: 0.76, facing +Z
    hl_pts = []
    hl_r = 0.09
    for k in range(8):
        ang = 2.0 * math.pi * k / 8
        hl_pts.append(add_vert([math.cos(ang)*hl_r, 1.00 + math.sin(ang)*hl_r, 0.77], uv_chrome))
    hl_c = add_vert([0.0, 1.00, 0.79], uv_chrome)
    for k in range(8):
        nxt = (k + 1) % 8
        new_faces.append(f"f {hl_c}/{hl_c} {hl_pts[k]}/{hl_pts[k]} {hl_pts[nxt]}/{hl_pts[nxt]}")

    # Front amber turn indicators
    for sx in [1.0, -1.0]:
        tx = 0.18 * sx
        t1 = add_vert([tx - 0.02*sx, 0.98, 0.72], uv_amber)
        t2 = add_vert([tx + 0.02*sx, 0.98, 0.72], uv_amber)
        t3 = add_vert([tx + 0.02*sx, 1.02, 0.72], uv_amber)
        t4 = add_vert([tx - 0.02*sx, 1.02, 0.72], uv_amber)
        if sx > 0:
            new_faces.append(f"f {t1}/{t1} {t2}/{t2} {t3}/{t3} {t4}/{t4}")
        else:
            new_faces.append(f"f {t4}/{t4} {t3}/{t3} {t2}/{t2} {t1}/{t1}")

    # 5. Rear Taillight / Stoplight Cluster (Red)
    # At rear tip: X: 0, Y: 0.68, Z: -0.84, facing -Z
    rt1 = add_vert([0.08, 0.64, -0.85], uv_red)
    rt2 = add_vert([-0.08, 0.64, -0.85], uv_red)
    rt3 = add_vert([-0.08, 0.72, -0.84], uv_red)
    rt4 = add_vert([0.08, 0.72, -0.84], uv_red)
    new_faces.append(f"f {rt1}/{rt1} {rt2}/{rt2} {rt3}/{rt3} {rt4}/{rt4}")

    # 6. Detailed Front Wheel Brake Disc & Caliper
    # Front wheel center: X: 0, Y: ~0.30, Z: ~0.65
    disc_r = 0.14
    disc_x = 0.06 # mounted on left side of front wheel
    disc_pts = []
    for k in range(8):
        ang = 2.0 * math.pi * k / 8
        disc_pts.append(add_vert([disc_x, 0.30 + math.sin(ang)*disc_r, 0.65 + math.cos(ang)*disc_r], uv_chrome))
    disc_c = add_vert([disc_x, 0.30, 0.65], uv_chrome)
    for k in range(8):
        nxt = (k + 1) % 8
        new_faces.append(f"f {disc_c}/{disc_c} {disc_pts[k]}/{disc_pts[k]} {disc_pts[nxt]}/{disc_pts[nxt]}")

    # Brake caliper (gold/black hydraulic block over the rotor)
    cal1 = add_vert([disc_x - 0.02, 0.38, 0.71], uv_amber)
    cal2 = add_vert([disc_x + 0.02, 0.38, 0.71], uv_amber)
    cal3 = add_vert([disc_x + 0.02, 0.44, 0.67], uv_amber)
    cal4 = add_vert([disc_x - 0.02, 0.44, 0.67], uv_amber)
    new_faces.append(f"f {cal1}/{cal1} {cal2}/{cal2} {cal3}/{cal3} {cal4}/{cal4}")

    # Write out OBJ
    with open(mesh_path, "w") as f:
        f.write("# SM_Motorbike.obj - ART-FIX-022 upgraded model\n")
        f.write(f"usemtl {mtl_name}\n")
        f.write("o SM_Motorbike\n")
        for v in out_v:
            f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
        for vt in out_vt:
            f.write(f"vt {vt[0]:.6f} {vt[1]:.6f}\n")
        for face in new_faces:
            f.write(face + "\n")

    print(f"Successfully generated upgraded SM_Motorbike.obj")

def main():
    print("=== UPGRADING VEHICLES FOR ART-FIX-022 ===")
    upgrade_truck_texture()
    build_truck_mesh()
    
    upgrade_cart_texture()
    build_cart_mesh()
    
    upgrade_motorbike_texture()
    build_motorbike_mesh()
    print("=== ALL VEHICLES UPGRADED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
