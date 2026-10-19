#!/usr/bin/env python3
"""
Procedural Asset Generator for Trạm Chanh New Menu Items (ART-FIX-028)
Generates:
1. Textures (1024x1024 RGBA PBR Atlases):
   - T_NewDrinks_BaseColor.png (Clear cup, amber tea, tropical tea, ice, lemon slices, seeds, straw)
   - T_NewCakes_BaseColor.png (Golden waffle crust, flambée marks, custard cream, banana slices)
2. 3D Wavefront OBJ Meshes:
   - SM_Drink_HoneyLemonTea.obj (< 2,500 tris)
   - SM_Drink_PassionfruitTea.obj (< 2,500 tris)
   - SM_Cake_CustardCream.obj (< 2,500 tris)
   - SM_Cake_BananaFlambee.obj (< 2,500 tris)
3. Materials (Unity URP Lit Shader):
   - MAT_Drink_HoneyLemonTea.mat
   - MAT_Drink_PassionfruitTea.mat
   - MAT_Cake_CustardCream.mat
   - MAT_Cake_BananaFlambee.mat
4. Prefabs (Pivot at bottom center, scale 1 unit = 1m, proper BoxColliders):
   - PF_Item_Drink_HoneyLemonTea.prefab
   - PF_Item_Drink_PassionfruitTea.prefab
   - PF_Item_Cake_CustardCream.prefab
   - PF_Item_Cake_BananaFlambee.prefab
5. Unity Meta Files (.meta) for all generated assets and directories.
"""

import os
import math
import hashlib
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# URP Lit Shader GUID
URP_LIT_SHADER_GUID = "933532a4fcc9baf4fa0491de14d08ed7"

# Deterministic GUID generator
def get_guid(rel_path):
    return hashlib.md5(f"ART-FIX-028/menu/{rel_path}".encode("utf-8")).hexdigest()

# -----------------------------------------------------------------------------
# 3D Mesh Generator Utility Class
# -----------------------------------------------------------------------------
class FastMesh:
    def __init__(self, name):
        self.name = name
        self.vertices = []   # list of (x, y, z)
        self.normals = []    # list of (nx, ny, nz)
        self.uvs = []        # list of (u, v)
        self.faces = []      # list of list of (v, vt, vn) 1-based

    def add_vertex(self, x, y, z):
        self.vertices.append((float(x), float(y), float(z)))
        return len(self.vertices)

    def add_normal(self, nx, ny, nz):
        l = math.sqrt(nx*nx + ny*ny + nz*nz) or 1.0
        self.normals.append((float(nx/l), float(ny/l), float(nz/l)))
        return len(self.normals)

    def add_uv(self, u, v):
        self.uvs.append((float(u), float(v)))
        return len(self.uvs)

    def add_triangle(self, v1, v2, v3, uv1, uv2, uv3, n):
        n_idx = self.add_normal(*n)
        u1 = self.add_uv(*uv1)
        u2 = self.add_uv(*uv2)
        u3 = self.add_uv(*uv3)
        self.faces.append([(v1, u1, n_idx), (v2, u2, n_idx), (v3, u3, n_idx)])

    def add_quad(self, v1, v2, v3, v4, uv1, uv2, uv3, uv4, n):
        n_idx = self.add_normal(*n)
        u1 = self.add_uv(*uv1)
        u2 = self.add_uv(*uv2)
        u3 = self.add_uv(*uv3)
        u4 = self.add_uv(*uv4)
        self.faces.append([(v1, u1, n_idx), (v2, u2, n_idx), (v3, u3, n_idx)])
        self.faces.append([(v1, u1, n_idx), (v3, u3, n_idx), (v4, u4, n_idx)])

    def add_frustum(self, y0, r0, y1, r1, segments=18, u_range=(0.0, 1.0), v_range=(0.0, 1.0)):
        """Creates an open conical frustum side wall between y0 and y1."""
        bot_idx = []
        top_idx = []
        slant_angle = math.atan2(r0 - r1, y1 - y0)
        cos_s = math.cos(slant_angle)
        sin_s = math.sin(slant_angle)

        for i in range(segments):
            ang = 2.0 * math.pi * i / segments
            cos_a = math.cos(ang)
            sin_a = math.sin(ang)
            v_bot = self.add_vertex(r0 * cos_a, y0, r0 * sin_a)
            v_top = self.add_vertex(r1 * cos_a, y1, r1 * sin_a)
            bot_idx.append(v_bot)
            top_idx.append(v_top)

        for i in range(segments):
            nxt = (i + 1) % segments
            ang_mid = 2.0 * math.pi * (i + 0.5) / segments
            nr = (math.cos(ang_mid) * cos_s, sin_s, math.sin(ang_mid) * cos_s)
            u0 = u_range[0] + (u_range[1] - u_range[0]) * (i / segments)
            u1 = u_range[0] + (u_range[1] - u_range[0]) * ((i + 1) / segments)
            v0 = v_range[0]
            v1 = v_range[1]
            self.add_quad(
                bot_idx[i], bot_idx[nxt], top_idx[nxt], top_idx[i],
                (u0, v0), (u1, v0), (u1, v1), (u0, v1),
                nr
            )

    def add_disc(self, y, r, segments=18, is_top=True, u_center=0.5, v_center=0.5, uv_rad=0.2):
        """Creates a circular planar disc."""
        vc = self.add_vertex(0.0, y, 0.0)
        ring_idx = []
        for i in range(segments):
            ang = 2.0 * math.pi * i / segments
            ring_idx.append(self.add_vertex(r * math.cos(ang), y, r * math.sin(ang)))

        n = (0.0, 1.0, 0.0) if is_top else (0.0, -1.0, 0.0)
        for i in range(segments):
            nxt = (i + 1) % segments
            ang1 = 2.0 * math.pi * i / segments
            ang2 = 2.0 * math.pi * nxt / segments
            uv_c = (u_center, v_center)
            uv1 = (u_center + uv_rad * math.cos(ang1), v_center + uv_rad * math.sin(ang1))
            uv2 = (u_center + uv_rad * math.cos(ang2), v_center + uv_rad * math.sin(ang2))
            if is_top:
                self.add_triangle(vc, ring_idx[i], ring_idx[nxt], uv_c, uv1, uv2, n)
            else:
                self.add_triangle(vc, ring_idx[nxt], ring_idx[i], uv_c, uv2, uv1, n)

    def add_dome(self, y_base, r_base, y_top, r_aperture, lat_rings=4, segments=18, u_range=(0.0, 0.5), v_range=(0.5, 1.0)):
        """Creates a hemispherical domed lid with straw aperture hole at top."""
        rings = []
        for j in range(lat_rings + 1):
            t = j / float(lat_rings)
            # Hemispherical curvature
            phi = t * (math.pi * 0.5 - math.asin(r_aperture / r_base))
            cur_y = y_base + (y_top - y_base) * math.sin(phi)
            cur_r = r_base * math.cos(phi)
            if j == lat_rings:
                cur_r = r_aperture
                cur_y = y_top

            ring_v = []
            for i in range(segments):
                ang = 2.0 * math.pi * i / segments
                ring_v.append(self.add_vertex(cur_r * math.cos(ang), cur_y, cur_r * math.sin(ang)))
            rings.append(ring_v)

        for j in range(lat_rings):
            r_cur = rings[j]
            r_nxt = rings[j + 1]
            for i in range(segments):
                nxt = (i + 1) % segments
                ang = 2.0 * math.pi * (i + 0.5) / segments
                norm = (math.cos(ang) * 0.8, 0.6, math.sin(ang) * 0.8)
                u0 = u_range[0] + (u_range[1] - u_range[0]) * (i / segments)
                u1 = u_range[0] + (u_range[1] - u_range[0]) * ((i + 1) / segments)
                v0 = v_range[0] + (v_range[1] - v_range[0]) * (j / lat_rings)
                v1 = v_range[0] + (v_range[1] - v_range[0]) * ((j + 1) / lat_rings)
                self.add_quad(
                    r_cur[i], r_cur[nxt], r_nxt[nxt], r_nxt[i],
                    (u0, v0), (u1, v0), (u1, v1), (u0, v1),
                    norm
                )

    def add_cylinder(self, p1, p2, radius, segments=12, u_range=(0.0, 1.0), v_range=(0.0, 1.0)):
        """Creates a cylinder between 3D points p1 and p2."""
        p1 = np.array(p1, dtype=float)
        p2 = np.array(p2, dtype=float)
        axis = p2 - p1
        length = np.linalg.norm(axis)
        if length < 1e-6:
            return
        z_dir = axis / length
        up = np.array([0, 1, 0], dtype=float)
        if abs(np.dot(up, z_dir)) > 0.99:
            up = np.array([1, 0, 0], dtype=float)
        x_dir = np.cross(up, z_dir)
        x_dir /= np.linalg.norm(x_dir)
        y_dir = np.cross(z_dir, x_dir)

        r1, r2 = [], []
        radial_normals = []
        for i in range(segments):
            ang = 2.0 * math.pi * i / segments
            rad = math.cos(ang) * x_dir + math.sin(ang) * y_dir
            r1.append(self.add_vertex(*(p1 + rad * radius)))
            r2.append(self.add_vertex(*(p2 + rad * radius)))
            radial_normals.append(rad)

        for i in range(segments):
            nxt = (i + 1) % segments
            u0 = u_range[0] + (u_range[1] - u_range[0]) * (i / segments)
            u1 = u_range[0] + (u_range[1] - u_range[0]) * ((i + 1) / segments)
            v0 = v_range[0]
            v1 = v_range[1]
            n_mid = radial_normals[i] + radial_normals[nxt]
            self.add_quad(
                r1[i], r1[nxt], r2[nxt], r2[i],
                (u0, v0), (u1, v0), (u1, v1), (u0, v1),
                n_mid
            )
        # End caps
        vc1 = self.add_vertex(*p1)
        vc2 = self.add_vertex(*p2)
        uv_c = ((u_range[0] + u_range[1]) * 0.5, (v_range[0] + v_range[1]) * 0.5)
        for i in range(segments):
            nxt = (i + 1) % segments
            self.add_triangle(vc1, r1[nxt], r1[i], uv_c, (u_range[0], v_range[0]), (u_range[1], v_range[0]), -z_dir)
            self.add_triangle(vc2, r2[i], r2[nxt], uv_c, (u_range[0], v_range[1]), (u_range[1], v_range[1]), z_dir)

    def add_box(self, cx, cy, cz, sx, sy, sz, yaw=0.0, pitch=0.0, roll=0.0, u_range=(0.0, 1.0), v_range=(0.0, 1.0)):
        """Adds an oriented box centered at (cx, cy, cz)."""
        # Euler rotation matrices
        cy_a, sy_a = math.cos(yaw), math.sin(yaw)
        cp_a, sp_a = math.cos(pitch), math.sin(pitch)
        cr_a, sr_a = math.cos(roll), math.sin(roll)

        R = np.array([
            [cy_a * cp_a, cy_a * sp_a * sr_a - sy_a * cr_a, cy_a * sp_a * cr_a + sy_a * sr_a],
            [sy_a * cp_a, sy_a * sp_a * sr_a + cy_a * cr_a, sy_a * sp_a * cr_a - cy_a * sr_a],
            [-sp_a,       cp_a * sr_a,                      cp_a * cr_a]
        ])

        hx, hy, hz = sx * 0.5, sy * 0.5, sz * 0.5
        local_corners = [
            (-hx, -hy, -hz), ( hx, -hy, -hz), ( hx,  hy, -hz), (-hx,  hy, -hz),
            (-hx, -hy,  hz), ( hx, -hy,  hz), ( hx,  hy,  hz), (-hx,  hy,  hz)
        ]
        world_corners = []
        for c in local_corners:
            w = np.dot(R, np.array(c)) + np.array([cx, cy, cz])
            world_corners.append(self.add_vertex(*w))

        faces_def = [
            (4, 5, 6, 7, (0, 0, 1)),   # +Z
            (1, 0, 3, 2, (0, 0, -1)),  # -Z
            (3, 2, 6, 7, (0, 1, 0)),   # +Y
            (0, 1, 5, 4, (0, -1, 0)),  # -Y
            (5, 1, 2, 6, (1, 0, 0)),   # +X
            (0, 4, 7, 3, (-1, 0, 0))   # -X
        ]
        u0, u1 = u_range
        v0, v1 = v_range
        for i1, i2, i3, i4, n_loc in faces_def:
            n_w = np.dot(R, np.array(n_loc))
            self.add_quad(
                world_corners[i1], world_corners[i2], world_corners[i3], world_corners[i4],
                (u0, v0), (u1, v0), (u1, v1), (u0, v1),
                n_w
            )

    def write_obj(self, filepath):
        os.makedirs(os.path.dirname(filepath), exist_ok=True)
        with open(filepath, "w", encoding="utf-8") as f:
            f.write(f"# Wavefront OBJ: {self.name}\n")
            f.write(f"o {self.name}\n")
            for v in self.vertices:
                f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
            for vt in self.uvs:
                f.write(f"vt {vt[0]:.6f} {vt[1]:.6f}\n")
            for vn in self.normals:
                f.write(f"vn {vn[0]:.6f} {vn[1]:.6f} {vn[2]:.6f}\n")
            f.write("s 1\n")
            for face in self.faces:
                f.write("f " + " ".join([f"{v}/{vt}/{vn}" for v, vt, vn in face]) + "\n")
        tri_count = len(self.faces)
        print(f"Generated OBJ: {filepath} ({len(self.vertices)} verts, {tri_count} tris)")
        return tri_count

# -----------------------------------------------------------------------------
# 1. Texture Atlas Generation
# -----------------------------------------------------------------------------
def generate_drink_atlas(out_path):
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    W, H = 1024, 1024
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Quadrant 1: [0.0 .. 0.5, 0.5 .. 1.0] -> Translucent Takeaway Cup & Domed Lid
    # In pixel space (x: 0..512, y: 0..512)
    cup_bg = Image.new("RGBA", (512, 512), (245, 250, 255, 45))
    cup_draw = ImageDraw.Draw(cup_bg)
    # Highlights & rim reflections
    for x in range(30, 480, 40):
        cup_draw.line([(x, 0), (x + 20, 512)], fill=(255, 255, 255, 60), width=4)
    # Rolled plastic rim
    cup_draw.rectangle([(0, 470), (512, 512)], fill=(255, 255, 255, 110))
    img.paste(cup_bg, (0, 0))

    # Quadrant 2: [0.5 .. 1.0, 0.5 .. 1.0] -> Golden Amber Honey Lemon Tea Liquid
    # In pixel space (x: 512..1024, y: 0..512)
    tea_amber = Image.new("RGBA", (512, 512), (218, 142, 32, 240))
    tea_draw = ImageDraw.Draw(tea_amber)
    for y in range(512):
        factor = y / 512.0
        r = int(240 - 45 * factor)
        g = int(175 - 55 * factor)
        b = int(45 - 20 * factor)
        tea_draw.line([(0, y), (512, y)], fill=(r, g, b, 245))
    # Honey swirls
    for r in range(40, 240, 30):
        tea_draw.ellipse([(256 - r, 256 - r), (256 + r, 256 + r)], outline=(255, 205, 75, 180), width=3)
    img.paste(tea_amber, (512, 0))

    # Quadrant 3: [0.0 .. 0.5, 0.0 .. 0.5] -> Vibrant Tropical Yellow-Orange Passionfruit Tea
    # In pixel space (x: 0..512, y: 512..1024)
    tea_passion = Image.new("RGBA", (512, 512), (255, 130, 20, 240))
    p_draw = ImageDraw.Draw(tea_passion)
    for y in range(512):
        factor = y / 512.0
        r = int(255 - 20 * factor)
        g = int(185 - 75 * factor)
        b = int(25 + 15 * factor)
        p_draw.line([(0, y), (512, y)], fill=(r, g, b, 245))
    # Tropical citrus swirls
    for r in range(50, 240, 35):
        p_draw.ellipse([(256 - r, 256 - r), (256 + r, 256 + r)], outline=(255, 210, 40, 160), width=4)
    img.paste(tea_passion, (0, 512))

    # Quadrant 4: [0.5 .. 1.0, 0.0 .. 0.5] -> Toppings (Ice, Lemon Slice, Seeds, Straw)
    # In pixel space (x: 512..1024, y: 512..1024)
    toppings = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    t_draw = ImageDraw.Draw(toppings)

    # 4A. Ice Cubes area (x: 0..256, y: 0..256 within toppings)
    t_draw.rectangle([(10, 10), (246, 246)], fill=(220, 245, 255, 190), outline=(255, 255, 255, 240), width=6)
    for i in range(20, 240, 35):
        t_draw.line([(i, 10), (i + 15, 246)], fill=(255, 255, 255, 160), width=3)

    # 4B. Fresh Lemon Slice (x: 0..256, y: 256..512 within toppings)
    cx, cy, r_lemon = 128, 384, 110
    # Outer yellow rind
    t_draw.ellipse([(cx - r_lemon, cy - r_lemon), (cx + r_lemon, cy + r_lemon)], fill=(255, 215, 0, 255), outline=(230, 180, 0, 255), width=4)
    # White pith ring
    r_pith = r_lemon - 8
    t_draw.ellipse([(cx - r_pith, cy - r_pith), (cx + r_pith, cy + r_pith)], fill=(255, 255, 235, 255))
    # Lemon pulp core
    r_pulp = r_pith - 10
    t_draw.ellipse([(cx - r_pulp, cy - r_pulp), (cx + r_pulp, cy + r_pulp)], fill=(245, 225, 60, 245))
    # 8 Lemon pulp wedges
    for i in range(8):
        ang = i * (math.pi / 4.0)
        x_spoke = cx + r_pulp * math.cos(ang)
        y_spoke = cy + r_pulp * math.sin(ang)
        t_draw.line([(cx, cy), (x_spoke, y_spoke)], fill=(255, 255, 240, 255), width=3)
    t_draw.ellipse([(cx - 12, cy - 12), (cx + 12, cy + 12)], fill=(255, 255, 240, 255))

    # 4C. Passionfruit Seeds & Jelly (x: 256..384, y: 0..512 within toppings)
    t_draw.rectangle([(260, 10), (380, 502)], fill=(240, 150, 20, 210))
    for sy in range(30, 490, 45):
        # Translucent golden aril halo
        t_draw.ellipse([(285, sy - 15), (355, sy + 15)], fill=(255, 180, 30, 230))
        # Crisp black passionfruit seed
        t_draw.ellipse([(305, sy - 10), (335, sy + 10)], fill=(20, 15, 18, 255))
        t_draw.ellipse([(312, sy - 6), (320, sy - 2)], fill=(220, 220, 220, 200)) # Specular glint

    # 4D. Straw (x: 384..512, y: 0..512 within toppings)
    t_draw.rectangle([(390, 10), (502, 502)], fill=(250, 250, 250, 255))
    # Spiral vibrant green tea-shop stripes
    for sy in range(0, 512, 32):
        t_draw.polygon([(390, sy), (502, sy + 30), (502, sy + 44), (390, sy + 14)], fill=(40, 195, 75, 255))

    img.paste(toppings, (512, 512))
    img.save(out_path, "PNG")
    print(f"Generated Drink Texture Atlas: {out_path}")


def generate_cake_atlas(out_path):
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    W, H = 1024, 1024
    img = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    draw = ImageDraw.Draw(img)

    # Quadrant 1: [0.0 .. 0.5, 0.5 .. 1.0] -> Golden Browned Waffle Exterior Crust
    # (x: 0..512, y: 0..512)
    waffle = Image.new("RGBA", (512, 512), (232, 172, 85, 255))
    w_draw = ImageDraw.Draw(waffle)
    # Diagonal waffle grid indentations
    for d in range(-512, 1024, 48):
        w_draw.line([(d, 0), (d + 512, 512)], fill=(168, 98, 38, 255), width=6)
        w_draw.line([(d, 512), (d + 512, 0)], fill=(168, 98, 38, 255), width=6)
    # Longitudinal grilled toast ribs
    for y in range(40, 512, 64):
        w_draw.line([(0, y), (512, y)], fill=(115, 55, 18, 255), width=8)
    img.paste(waffle, (0, 0))

    # Quadrant 2: [0.5 .. 1.0, 0.5 .. 1.0] -> Fragrant Flambée Charred Waffle Crust
    # (x: 512..1024, y: 0..512)
    flambee = Image.new("RGBA", (512, 512), (225, 155, 70, 255))
    f_draw = ImageDraw.Draw(flambee)
    for d in range(-512, 1024, 48):
        f_draw.line([(d, 0), (d + 512, 512)], fill=(150, 80, 25, 255), width=6)
    # Dark flambée char marks & caramelized butter blisters
    np.random.seed(42)
    for _ in range(35):
        bx = int(np.random.randint(20, 492))
        by = int(np.random.randint(20, 492))
        rx = int(np.random.randint(12, 35))
        ry = int(np.random.randint(6, 18))
        f_draw.ellipse([(bx - rx, by - ry), (bx + rx, by + ry)], fill=(65, 25, 8, 255))
        f_draw.ellipse([(bx - rx//2, by - ry//2), (bx + rx//2, by + ry//2)], fill=(40, 15, 5, 255))
    img.paste(flambee, (512, 0))

    # Quadrant 3: [0.0 .. 0.5, 0.0 .. 0.5] -> Rich Golden Custard Cream Filling
    # (x: 0..512, y: 512..1024)
    custard = Image.new("RGBA", (512, 512), (255, 215, 85, 255))
    c_draw = ImageDraw.Draw(custard)
    # Creamy swirl ripples and glossy cream highlights
    for r in range(30, 250, 30):
        c_draw.ellipse([(256 - r, 256 - r), (256 + r, 256 + r)], outline=(255, 240, 150, 255), width=5)
        c_draw.ellipse([(256 - r - 6, 256 - r - 6), (256 + r - 6, 256 + r - 6)], outline=(235, 185, 55, 255), width=4)
    # Tiny vanilla bean specks
    for _ in range(80):
        vx = int(np.random.randint(10, 502))
        vy = int(np.random.randint(10, 502))
        c_draw.point((vx, vy), fill=(80, 50, 20, 255))
        c_draw.point((vx+1, vy), fill=(80, 50, 20, 255))
    img.paste(custard, (0, 512))

    # Quadrant 4: [0.5 .. 1.0, 0.0 .. 0.5] -> Caramelized Banana Flambée Filling
    # (x: 512..1024, y: 512..1024)
    banana_area = Image.new("RGBA", (512, 512), (185, 95, 25, 255)) # Caramel glaze bed
    b_draw = ImageDraw.Draw(banana_area)
    # 4 Caramelized Banana Slices
    banana_coords = [(140, 140), (370, 140), (140, 370), (370, 370)]
    for bx, by in banana_coords:
        rb = 95
        # Caramelized rim
        b_draw.ellipse([(bx - rb, by - rb), (bx + rb, by + rb)], fill=(125, 52, 14, 255))
        # Golden fried banana crust
        b_draw.ellipse([(bx - rb + 6, by - rb + 6), (bx + rb - 6, by + rb - 6)], fill=(220, 160, 60, 255))
        # Creamy banana flesh
        b_draw.ellipse([(bx - rb + 14, by - rb + 14), (bx + rb - 14, by + rb - 14)], fill=(250, 238, 185, 255))
        # Center triple seed star
        for a_deg in (0, 120, 240):
            rad = math.radians(a_deg)
            sx = bx + 8 * math.cos(rad)
            sy = by + 8 * math.sin(rad)
            b_draw.ellipse([(sx - 3, sy - 3), (sx + 3, sy + 3)], fill=(120, 80, 40, 255))
    img.paste(banana_area, (512, 512))

    img.save(out_path, "PNG")
    print(f"Generated Cake Texture Atlas: {out_path}")

# -----------------------------------------------------------------------------
# 2. 3D Model Generation
# -----------------------------------------------------------------------------
def build_honey_lemon_tea(out_path):
    m = FastMesh("SM_Drink_HoneyLemonTea")

    # 1. Clear Cup (Frustum: Rb=0.029, Rt=0.044, H=0.135)
    # UV range: Cup quadrant [0.05, 0.45], [0.55, 0.95]
    m.add_frustum(0.000, 0.029, 0.135, 0.044, segments=18, u_range=(0.05, 0.45), v_range=(0.55, 0.95))
    m.add_disc(0.000, 0.029, segments=18, is_top=False, u_center=0.25, v_center=0.75, uv_rad=0.15)
    # Cup Rolled Rim
    m.add_frustum(0.135, 0.044, 0.138, 0.046, segments=18, u_range=(0.05, 0.45), v_range=(0.95, 0.99))

    # 2. Domed Lid (Base at Y=0.138, R=0.046, top straw aperture at Y=0.175, R=0.007)
    m.add_dome(0.138, 0.046, 0.175, 0.007, lat_rings=4, segments=18, u_range=(0.05, 0.45), v_range=(0.55, 0.95))

    # 3. Golden Amber Honey Tea Liquid (inside cup: Y=0.005 to 0.120, R=0.027 to 0.042)
    # UV range: Tea liquid quadrant [0.55, 0.95], [0.55, 0.95]
    m.add_frustum(0.005, 0.027, 0.120, 0.042, segments=18, u_range=(0.55, 0.95), v_range=(0.55, 0.95))
    m.add_disc(0.120, 0.042, segments=18, is_top=True, u_center=0.75, v_center=0.75, uv_rad=0.18)
    m.add_disc(0.005, 0.027, segments=18, is_top=False, u_center=0.75, v_center=0.75, uv_rad=0.15)

    # 4. Ice Cubes (5 cubes floating at Y ~ 0.115 to 0.125)
    # UV range: Ice quadrant [0.52, 0.72], [0.28, 0.48]
    cubes_info = [
        ( 0.015, 0.115,  0.012, 0.4, 0.2, 0.0),
        (-0.016, 0.118,  0.015, -0.6, 0.3, 0.4),
        (-0.010, 0.116, -0.018, 0.8, -0.2, -0.3),
        ( 0.016, 0.117, -0.014, -0.3, 0.5, -0.4),
        ( 0.002, 0.122,  0.002, 1.2, 0.4, 0.5)
    ]
    for cx, cy, cz, yaw, pitch, roll in cubes_info:
        m.add_box(cx, cy, cz, 0.016, 0.016, 0.016, yaw, pitch, roll, u_range=(0.52, 0.72), v_range=(0.28, 0.48))

    # 5. Fresh Lemon Slices (3 slices floating in the tea)
    # UV: Top/Bottom disc mapped to Lemon circle [0.52, 0.74], [0.02, 0.24]
    slices_info = [
        ( 0.012, 0.121,  0.008, 0.021, 0.003,  0.25,  0.15),
        (-0.014, 0.119, -0.010, 0.019, 0.003, -0.35,  0.25),
        (-0.008, 0.122,  0.016, 0.020, 0.003,  0.40, -0.20)
    ]
    for lx, ly, lz, lr, lh, rx, rz in slices_info:
        # Generate oriented disc for lemon slice
        n_top = (math.sin(rz), math.cos(rx)*math.cos(rz), -math.sin(rx))
        p_top = (lx, ly + lh*0.5, lz)
        p_bot = (lx, ly - lh*0.5, lz)
        m.add_cylinder(p_bot, p_top, lr, segments=16, u_range=(0.52, 0.74), v_range=(0.02, 0.24))

    # 6. Straw (from bottom interior to poking through dome aperture)
    # UV range: Straw [0.88, 0.98], [0.05, 0.45]
    p_straw_bot = (-0.012, 0.010, 0.012)
    p_straw_top = ( 0.022, 0.210, -0.022)
    m.add_cylinder(p_straw_bot, p_straw_top, radius=0.005, segments=12, u_range=(0.88, 0.98), v_range=(0.05, 0.45))

    return m.write_obj(out_path)


def build_passionfruit_tea(out_path):
    m = FastMesh("SM_Drink_PassionfruitTea")

    # 1. Clear Cup
    m.add_frustum(0.000, 0.029, 0.135, 0.044, segments=18, u_range=(0.05, 0.45), v_range=(0.55, 0.95))
    m.add_disc(0.000, 0.029, segments=18, is_top=False, u_center=0.25, v_center=0.75, uv_rad=0.15)
    m.add_frustum(0.135, 0.044, 0.138, 0.046, segments=18, u_range=(0.05, 0.45), v_range=(0.95, 0.99))

    # 2. Domed Lid
    m.add_dome(0.138, 0.046, 0.175, 0.007, lat_rings=4, segments=18, u_range=(0.05, 0.45), v_range=(0.55, 0.95))

    # 3. Vibrant Tropical Yellow-Orange Tea Liquid
    # UV range: Passionfruit tea quadrant [0.05, 0.45], [0.05, 0.45]
    m.add_frustum(0.005, 0.027, 0.120, 0.042, segments=18, u_range=(0.05, 0.45), v_range=(0.05, 0.45))
    m.add_disc(0.120, 0.042, segments=18, is_top=True, u_center=0.25, v_center=0.25, uv_rad=0.18)
    m.add_disc(0.005, 0.027, segments=18, is_top=False, u_center=0.25, v_center=0.25, uv_rad=0.15)

    # 4. Ice Cubes (5 cubes)
    cubes_info = [
        ( 0.015, 0.115,  0.012, 0.4, 0.2, 0.0),
        (-0.016, 0.118,  0.015, -0.6, 0.3, 0.4),
        (-0.010, 0.116, -0.018, 0.8, -0.2, -0.3),
        ( 0.016, 0.117, -0.014, -0.3, 0.5, -0.4),
        ( 0.002, 0.122,  0.002, 1.2, 0.4, 0.5)
    ]
    for cx, cy, cz, yaw, pitch, roll in cubes_info:
        m.add_box(cx, cy, cz, 0.016, 0.016, 0.016, yaw, pitch, roll, u_range=(0.52, 0.72), v_range=(0.28, 0.48))

    # 5. Dark Passionfruit Seeds & Jelly (16 seeds/jelly clusters distributed in tea)
    # UV range: Seeds [0.76, 0.88], [0.05, 0.48]
    np.random.seed(123)
    for i in range(16):
        ang = i * (2.0 * math.pi / 16.0) + np.random.uniform(-0.2, 0.2)
        r_seed = np.random.uniform(0.008, 0.032)
        sx = r_seed * math.cos(ang)
        sz = r_seed * math.sin(ang)
        sy = np.random.uniform(0.025, 0.122) # Throughout liquid column and floating at top
        m.add_box(sx, sy, sz, 0.005, 0.003, 0.003, yaw=ang, pitch=0.3, roll=0.5, u_range=(0.76, 0.88), v_range=(0.05, 0.48))

    # 6. Straw
    p_straw_bot = (-0.012, 0.010, 0.012)
    p_straw_top = ( 0.022, 0.210, -0.022)
    m.add_cylinder(p_straw_bot, p_straw_top, radius=0.005, segments=12, u_range=(0.88, 0.98), v_range=(0.05, 0.45))

    return m.write_obj(out_path)


def build_cake_custard_cream(out_path):
    m = FastMesh("SM_Cake_CustardCream")

    # Dimensions: Length 0.170m along Z (from -0.085 to +0.085), outer radius 0.027, inner cavity 0.022
    # Bottom resting on counter at Y=0.000 -> Center at (0, 0.027, 0)
    L = 0.170
    z0, z1 = -L * 0.5, L * 0.5
    r_out = 0.027
    r_in = 0.022
    cy = 0.027
    segs = 24

    # Outer cylindrical waffle shell (with 4 longitudinal segments for UV tiling)
    z_slices = np.linspace(z0, z1, 5)
    for s in range(4):
        zs0, zs1 = z_slices[s], z_slices[s+1]
        for i in range(segs):
            nxt = (i + 1) % segs
            a0 = 2.0 * math.pi * i / segs
            a1 = 2.0 * math.pi * nxt / segs
            # Slight fluted waffle rib height
            rib0 = 0.001 * math.sin(a0 * 6.0)
            rib1 = 0.001 * math.sin(a1 * 6.0)
            v00 = m.add_vertex((r_out + rib0) * math.cos(a0), cy + (r_out + rib0) * math.sin(a0), zs0)
            v10 = m.add_vertex((r_out + rib1) * math.cos(a1), cy + (r_out + rib1) * math.sin(a1), zs0)
            v11 = m.add_vertex((r_out + rib1) * math.cos(a1), cy + (r_out + rib1) * math.sin(a1), zs1)
            v01 = m.add_vertex((r_out + rib0) * math.cos(a0), cy + (r_out + rib0) * math.sin(a0), zs1)

            u0 = 0.02 + 0.46 * (i / segs)
            u1 = 0.02 + 0.46 * ((i + 1) / segs)
            v0 = 0.52 + 0.46 * (s / 4.0)
            v1 = 0.52 + 0.46 * ((s + 1) / 4.0)
            norm = (math.cos((a0+a1)*0.5), math.sin((a0+a1)*0.5), 0.0)
            m.add_quad(v00, v10, v11, v01, (u0, v0), (u1, v0), (u1, v1), (u0, v1), norm)

    # Inner cavity wall
    for i in range(segs):
        nxt = (i + 1) % segs
        a0 = 2.0 * math.pi * i / segs
        a1 = 2.0 * math.pi * nxt / segs
        v00 = m.add_vertex(r_in * math.cos(a0), cy + r_in * math.sin(a0), z0)
        v10 = m.add_vertex(r_in * math.cos(a1), cy + r_in * math.sin(a1), z0)
        v11 = m.add_vertex(r_in * math.cos(a1), cy + r_in * math.sin(a1), z1)
        v01 = m.add_vertex(r_in * math.cos(a0), cy + r_in * math.sin(a0), z1)
        norm = (-math.cos((a0+a1)*0.5), -math.sin((a0+a1)*0.5), 0.0)
        m.add_quad(v00, v01, v11, v10, (0.05, 0.55), (0.45, 0.55), (0.45, 0.95), (0.05, 0.95), norm)

    # Crepe wall rim rings at ends
    for end_z, end_sign in [(z0, -1.0), (z1, 1.0)]:
        for i in range(segs):
            nxt = (i + 1) % segs
            a0 = 2.0 * math.pi * i / segs
            a1 = 2.0 * math.pi * nxt / segs
            vo0 = m.add_vertex(r_out * math.cos(a0), cy + r_out * math.sin(a0), end_z)
            vo1 = m.add_vertex(r_out * math.cos(a1), cy + r_out * math.sin(a1), end_z)
            vi1 = m.add_vertex(r_in * math.cos(a1), cy + r_in * math.sin(a1), end_z)
            vi0 = m.add_vertex(r_in * math.cos(a0), cy + r_in * math.sin(a0), end_z)
            if end_sign < 0:
                m.add_quad(vo0, vi0, vi1, vo1, (0.05, 0.55), (0.1, 0.55), (0.1, 0.6), (0.05, 0.6), (0, 0, -1))
            else:
                m.add_quad(vo0, vo1, vi1, vi0, (0.05, 0.55), (0.1, 0.55), (0.1, 0.6), (0.05, 0.6), (0, 0, 1))

    # Custard Cream Filling Swirl at both open ends
    # UV range: Custard Cream [0.05, 0.45], [0.05, 0.45]
    for end_z, end_sign in [(z0, -1.0), (z1, 1.0)]:
        vc = m.add_vertex(0.0, cy, end_z + end_sign * 0.007)
        c_ring = []
        for i in range(segs):
            a = 2.0 * math.pi * i / segs
            # Fluted cream lobes
            lobe = 0.002 * math.cos(a * 4.0)
            c_ring.append(m.add_vertex(
                (r_in * 0.95) * math.cos(a),
                cy + (r_in * 0.95) * math.sin(a),
                end_z + end_sign * (0.003 + lobe)
            ))
        for i in range(segs):
            nxt = (i + 1) % segs
            a1 = 2.0 * math.pi * i / segs
            a2 = 2.0 * math.pi * nxt / segs
            uv_c = (0.25, 0.25)
            uv1 = (0.25 + 0.18 * math.cos(a1), 0.25 + 0.18 * math.sin(a1))
            uv2 = (0.25 + 0.18 * math.cos(a2), 0.25 + 0.18 * math.sin(a2))
            norm = (0.3 * math.cos(a1), 0.3 * math.sin(a1), end_sign * 0.9)
            if end_sign > 0:
                m.add_triangle(vc, c_ring[i], c_ring[nxt], uv_c, uv1, uv2, norm)
            else:
                m.add_triangle(vc, c_ring[nxt], c_ring[i], uv_c, uv2, uv1, norm)

    return m.write_obj(out_path)


def build_cake_banana_flambee(out_path):
    m = FastMesh("SM_Cake_BananaFlambee")

    L = 0.170
    z0, z1 = -L * 0.5, L * 0.5
    r_out = 0.027
    r_in = 0.022
    cy = 0.027
    segs = 24

    # Flambée Charred Waffle Crust
    # UV range: Flambee crust [0.52, 0.98], [0.52, 0.98]
    z_slices = np.linspace(z0, z1, 5)
    for s in range(4):
        zs0, zs1 = z_slices[s], z_slices[s+1]
        for i in range(segs):
            nxt = (i + 1) % segs
            a0 = 2.0 * math.pi * i / segs
            a1 = 2.0 * math.pi * nxt / segs
            rib0 = 0.001 * math.sin(a0 * 6.0)
            rib1 = 0.001 * math.sin(a1 * 6.0)
            v00 = m.add_vertex((r_out + rib0) * math.cos(a0), cy + (r_out + rib0) * math.sin(a0), zs0)
            v10 = m.add_vertex((r_out + rib1) * math.cos(a1), cy + (r_out + rib1) * math.sin(a1), zs0)
            v11 = m.add_vertex((r_out + rib1) * math.cos(a1), cy + (r_out + rib1) * math.sin(a1), zs1)
            v01 = m.add_vertex((r_out + rib0) * math.cos(a0), cy + (r_out + rib0) * math.sin(a0), zs1)

            u0 = 0.52 + 0.46 * (i / segs)
            u1 = 0.52 + 0.46 * ((i + 1) / segs)
            v0 = 0.52 + 0.46 * (s / 4.0)
            v1 = 0.52 + 0.46 * ((s + 1) / 4.0)
            norm = (math.cos((a0+a1)*0.5), math.sin((a0+a1)*0.5), 0.0)
            m.add_quad(v00, v10, v11, v01, (u0, v0), (u1, v0), (u1, v1), (u0, v1), norm)

    # Inner cavity wall
    for i in range(segs):
        nxt = (i + 1) % segs
        a0 = 2.0 * math.pi * i / segs
        a1 = 2.0 * math.pi * nxt / segs
        v00 = m.add_vertex(r_in * math.cos(a0), cy + r_in * math.sin(a0), z0)
        v10 = m.add_vertex(r_in * math.cos(a1), cy + r_in * math.sin(a1), z0)
        v11 = m.add_vertex(r_in * math.cos(a1), cy + r_in * math.sin(a1), z1)
        v01 = m.add_vertex(r_in * math.cos(a0), cy + r_in * math.sin(a0), z1)
        norm = (-math.cos((a0+a1)*0.5), -math.sin((a0+a1)*0.5), 0.0)
        m.add_quad(v00, v01, v11, v10, (0.55, 0.55), (0.95, 0.55), (0.95, 0.95), (0.55, 0.95), norm)

    # Crepe wall rim rings at ends
    for end_z, end_sign in [(z0, -1.0), (z1, 1.0)]:
        for i in range(segs):
            nxt = (i + 1) % segs
            a0 = 2.0 * math.pi * i / segs
            a1 = 2.0 * math.pi * nxt / segs
            vo0 = m.add_vertex(r_out * math.cos(a0), cy + r_out * math.sin(a0), end_z)
            vo1 = m.add_vertex(r_out * math.cos(a1), cy + r_out * math.sin(a1), end_z)
            vi1 = m.add_vertex(r_in * math.cos(a1), cy + r_in * math.sin(a1), end_z)
            vi0 = m.add_vertex(r_in * math.cos(a0), cy + r_in * math.sin(a0), end_z)
            if end_sign < 0:
                m.add_quad(vo0, vi0, vi1, vo1, (0.55, 0.55), (0.6, 0.55), (0.6, 0.6), (0.55, 0.6), (0, 0, -1))
            else:
                m.add_quad(vo0, vo1, vi1, vi0, (0.55, 0.55), (0.6, 0.55), (0.6, 0.6), (0.55, 0.6), (0, 0, 1))

    # Caramelized Banana Slices and Glaze at both open ends
    # UV range: Banana slice quadrant [0.55, 0.95], [0.05, 0.45]
    for end_z, end_sign in [(z0, -1.0), (z1, 1.0)]:
        # Glaze backing disc
        m.add_disc(end_z + end_sign * 0.002, r_in * 0.95, segments=16, is_top=(end_sign > 0), u_center=0.75, v_center=0.25, uv_rad=0.18)

        # 2 Caramelized Banana Slices embedded in the opening
        banana_positions = [
            (-0.007, cy + 0.005, end_z + end_sign * 0.005, 0.009, 0.004),
            ( 0.008, cy - 0.005, end_z + end_sign * 0.007, 0.008, 0.004)
        ]
        for bx, by, bz, br, bh in banana_positions:
            p_bot = (bx, by, bz - end_sign * (bh * 0.5))
            p_top = (bx, by, bz + end_sign * (bh * 0.5))
            m.add_cylinder(p_bot, p_top, br, segments=12, u_range=(0.58, 0.92), v_range=(0.08, 0.42))

    return m.write_obj(out_path)

# -----------------------------------------------------------------------------
# 3. Unity Material Generation
# -----------------------------------------------------------------------------
URP_TRANSPARENT_MAT_TEMPLATE = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Shader: {{fileID: 4800000, guid: {shader_guid}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords:
  - _ALPHAPREMULTIPLY_ON
  - _SURFACE_TYPE_TRANSPARENT
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: 3000
  stringTagMap:
    RenderType: Transparent
  disabledShaderPasses:
  - MOTIONVECTORS
  - DepthOnly
  - SHADOWCASTER
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {texture_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _BumpMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailAlbedoMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailMask:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailNormalMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _EmissionMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {texture_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MetallicGlossMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _OcclusionMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _ParallaxMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _SpecGlossMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - unity_Lightmaps:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - unity_LightmapsInd:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - unity_ShadowMasks:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _AddPrecomputedVelocity: 0
    - _AlphaClip: 0
    - _AlphaToMask: 0
    - _Blend: 0
    - _BlendModePreserveSpecular: 1
    - _BumpScale: 1
    - _ClearCoatMask: 0
    - _ClearCoatSmoothness: 0
    - _Cull: 2
    - _Cutoff: 0.5
    - _DetailAlbedoMapScale: 1
    - _DetailNormalMapScale: 1
    - _DstBlend: 10
    - _DstBlendAlpha: 10
    - _EnvironmentReflections: 1
    - _GlossMapScale: 0
    - _Glossiness: 0
    - _GlossyReflections: 0
    - _Metallic: 0
    - _OcclusionStrength: 1
    - _Parallax: 0.005
    - _QueueOffset: 0
    - _ReceiveShadows: 1
    - _ScreenSpaceReflections: 1
    - _ScreenSpaceReflectionsContributeTransparent: 1
    - _Smoothness: 0.92
    - _SmoothnessTextureChannel: 0
    - _SpecularHighlights: 1
    - _SrcBlend: 1
    - _SrcBlendAlpha: 1
    - _Surface: 1
    - _WorkflowMode: 1
    - _XRMotionVectorsPass: 1
    - _ZWrite: 0
    m_Colors:
    - _BaseColor: {{r: 1, g: 1, b: 1, a: 1}}
    - _Color: {{r: 1, g: 1, b: 1, a: 1}}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
    - _SpecColor: {{r: 0.2, g: 0.2, b: 0.2, a: 1}}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
--- !u!114 &4852581453223956379
MonoBehaviour:
  m_ObjectHideFlags: 11
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: d0353a89b1f911e48b9e16bdc9f2e058, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Editor::UnityEditor.Rendering.Universal.AssetVersion
  version: 10
"""

URP_OPAQUE_MAT_TEMPLATE = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Shader: {{fileID: 4800000, guid: {shader_guid}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap:
    RenderType: Opaque
  disabledShaderPasses:
  - MOTIONVECTORS
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {texture_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _BumpMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailAlbedoMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailMask:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailNormalMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _EmissionMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {texture_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MetallicGlossMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _OcclusionMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _ParallaxMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _SpecGlossMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - unity_Lightmaps:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - unity_LightmapsInd:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - unity_ShadowMasks:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _AddPrecomputedVelocity: 0
    - _AlphaClip: 0
    - _AlphaToMask: 0
    - _Blend: 0
    - _BlendModePreserveSpecular: 1
    - _BumpScale: 1
    - _ClearCoatMask: 0
    - _ClearCoatSmoothness: 0
    - _Cull: 2
    - _Cutoff: 0.5
    - _DetailAlbedoMapScale: 1
    - _DetailNormalMapScale: 1
    - _DstBlend: 0
    - _DstBlendAlpha: 0
    - _EnvironmentReflections: 1
    - _GlossMapScale: 0
    - _Glossiness: 0
    - _GlossyReflections: 0
    - _Metallic: 0
    - _OcclusionStrength: 1
    - _Parallax: 0.005
    - _QueueOffset: 0
    - _ReceiveShadows: 1
    - _ScreenSpaceReflections: 1
    - _ScreenSpaceReflectionsContributeTransparent: 1
    - _Smoothness: 0.30
    - _SmoothnessTextureChannel: 0
    - _SpecularHighlights: 1
    - _SrcBlend: 1
    - _SrcBlendAlpha: 1
    - _Surface: 0
    - _WorkflowMode: 1
    - _XRMotionVectorsPass: 1
    - _ZWrite: 1
    m_Colors:
    - _BaseColor: {{r: 1, g: 1, b: 1, a: 1}}
    - _Color: {{r: 1, g: 1, b: 1, a: 1}}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
    - _SpecColor: {{r: 0.2, g: 0.2, b: 0.2, a: 1}}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
--- !u!114 &1705139221649200731
MonoBehaviour:
  m_ObjectHideFlags: 11
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: d0353a89b1f911e48b9e16bdc9f2e058, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Editor::UnityEditor.Rendering.Universal.AssetVersion
  version: 10
"""

def generate_materials():
    drink_tex_guid = get_guid("Assets/TramChanh/Art/Textures/Food/T_NewDrinks_BaseColor.png")
    cake_tex_guid = get_guid("Assets/TramChanh/Art/Textures/Food/T_NewCakes_BaseColor.png")

    mats_to_create = [
        ("Assets/TramChanh/Art/Materials/Food/MAT_Drink_HoneyLemonTea.mat", "MAT_Drink_HoneyLemonTea", drink_tex_guid, True),
        ("Assets/TramChanh/Art/Materials/Food/MAT_Drink_PassionfruitTea.mat", "MAT_Drink_PassionfruitTea", drink_tex_guid, True),
        ("Assets/TramChanh/Art/Materials/Food/MAT_Cake_CustardCream.mat", "MAT_Cake_CustardCream", cake_tex_guid, False),
        ("Assets/TramChanh/Art/Materials/Food/MAT_Cake_BananaFlambee.mat", "MAT_Cake_BananaFlambee", cake_tex_guid, False),
    ]

    for rel_path, name, tex_guid, is_trans in mats_to_create:
        full_path = os.path.join(ROOT, rel_path)
        os.makedirs(os.path.dirname(full_path), exist_ok=True)
        tmpl = URP_TRANSPARENT_MAT_TEMPLATE if is_trans else URP_OPAQUE_MAT_TEMPLATE
        content = tmpl.format(name=name, shader_guid=URP_LIT_SHADER_GUID, texture_guid=tex_guid)
        with open(full_path, "w", encoding="utf-8") as f:
            f.write(content)
        print(f"Generated Material: {rel_path}")

# -----------------------------------------------------------------------------
# 4. Unity Prefab Generation
# -----------------------------------------------------------------------------
PREFAB_TEMPLATE = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 100001}}
  - component: {{fileID: 100002}}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: 200001}}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!65 &100002
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {{x: {col_sx}, y: {col_sy}, z: {col_sz}}}
  m_Center: {{x: {col_cx}, y: {col_cy}, z: {col_cz}}}
--- !u!1 &200000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 200001}}
  - component: {{fileID: 200002}}
  - component: {{fileID: 200003}}
  m_Layer: 0
  m_Name: Visual
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &200001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 200000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 100001}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!33 &200002
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 200000}}
  m_Mesh: {{fileID: -2432090755550338912, guid: {mesh_guid}, type: 3}}
--- !u!23 &200003
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 200000}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: {mat_guid}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
"""

def generate_prefabs():
    prefabs_info = [
        (
            "Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Drink_HoneyLemonTea.prefab",
            "PF_Item_Drink_HoneyLemonTea",
            get_guid("Assets/TramChanh/Art/Models/Food/SM_Drink_HoneyLemonTea.obj"),
            get_guid("Assets/TramChanh/Art/Materials/Food/MAT_Drink_HoneyLemonTea.mat"),
            0.10, 0.18, 0.10,  # size
            0.0, 0.09, 0.0     # center (pivot at bottom center, Y=0..0.18)
        ),
        (
            "Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Drink_PassionfruitTea.prefab",
            "PF_Item_Drink_PassionfruitTea",
            get_guid("Assets/TramChanh/Art/Models/Food/SM_Drink_PassionfruitTea.obj"),
            get_guid("Assets/TramChanh/Art/Materials/Food/MAT_Drink_PassionfruitTea.mat"),
            0.10, 0.18, 0.10,  # size
            0.0, 0.09, 0.0     # center
        ),
        (
            "Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Cake_CustardCream.prefab",
            "PF_Item_Cake_CustardCream",
            get_guid("Assets/TramChanh/Art/Models/Food/SM_Cake_CustardCream.obj"),
            get_guid("Assets/TramChanh/Art/Materials/Food/MAT_Cake_CustardCream.mat"),
            0.06, 0.056, 0.18, # size
            0.0, 0.028, 0.0    # center (pivot at bottom center, Y=0..0.056)
        ),
        (
            "Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Cake_BananaFlambee.prefab",
            "PF_Item_Cake_BananaFlambee",
            get_guid("Assets/TramChanh/Art/Models/Food/SM_Cake_BananaFlambee.obj"),
            get_guid("Assets/TramChanh/Art/Materials/Food/MAT_Cake_BananaFlambee.mat"),
            0.06, 0.056, 0.18, # size
            0.0, 0.028, 0.0    # center
        )
    ]

    for rel_path, name, mesh_guid, mat_guid, sx, sy, sz, cx, cy, cz in prefabs_info:
        full_path = os.path.join(ROOT, rel_path)
        os.makedirs(os.path.dirname(full_path), exist_ok=True)
        content = PREFAB_TEMPLATE.format(
            name=name,
            mesh_guid=mesh_guid,
            mat_guid=mat_guid,
            col_sx=sx, col_sy=sy, col_sz=sz,
            col_cx=cx, col_cy=cy, col_cz=cz
        )
        with open(full_path, "w", encoding="utf-8") as f:
            f.write(content)
        print(f"Generated Prefab: {rel_path}")

# -----------------------------------------------------------------------------
# 5. Meta File Generators
# -----------------------------------------------------------------------------
def write_folder_meta(folder_rel):
    meta_path = os.path.join(ROOT, f"{folder_rel}.meta")
    guid = get_guid(folder_rel)
    content = f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(content)

def write_texture_meta(tex_rel):
    meta_path = os.path.join(ROOT, f"{tex_rel}.meta")
    guid = get_guid(tex_rel)
    content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(content)

def write_model_meta(model_rel):
    meta_path = os.path.join(ROOT, f"{model_rel}.meta")
    guid = get_guid(model_rel)
    content = f"""fileFormatVersion: 2
guid: {guid}
ModelImporter:
  serializedVersion: 24600
  internalIDToNameTable: []
  externalObjects: {{}}
  materials:
    materialImportMode: 2
    materialName: 0
    materialSearch: 1
    materialLocation: 1
    searchTexturesGlobally: 0
  animations:
    legacyGenerateAnimations: 4
    bakeSimulation: 0
    resampleCurves: 1
    optimizeGameObjects: 0
    removeConstantScaleCurves: 0
    motionNodeName: 
    animationImportErrors: 
    animationImportWarnings: 
    animationRetargetingWarnings: 
    animationDoRetargetingWarnings: 0
    importAnimatedCustomProperties: 0
    importConstraints: 0
    animationCompression: 1
    animationRotationError: 0.5
    animationPositionError: 0.5
    animationScaleError: 0.5
    animationWrapMode: 0
    extraExposedTransformPaths: []
    extraUserProperties: []
    clipAnimations: []
    isReadable: 0
  meshes:
    lODScreenPercentages: []
    globalScale: 1
    meshCompression: 0
    addColliders: 0
    useSRGBMaterialColor: 1
    sortHierarchyByName: 1
    preBakeConvexCollisionMesh: 0
    preBakeTriangleCollisionMesh: 0
    importPhysicalCameras: 1
    importVisibility: 1
    importBlendShapes: 1
    importCameras: 1
    importLights: 1
    nodeNameCollisionStrategy: 1
    fileIdsGeneration: 2
    swapUVChannels: 0
    generateSecondaryUV: 0
    useFileUnits: 1
    keepQuads: 0
    weldVertices: 1
    bakeAxisConversion: 0
    preserveHierarchy: 0
    skinWeightsMode: 0
    maxBonesPerVertex: 4
    minBoneWeight: 0.001
    optimizeBones: 1
    generateMeshLods: 0
    meshLodGenerationFlags: 0
    maximumMeshLod: -1
    importUVs: -1
    importVertexColors: 1
    meshOptimizationFlags: -1
    indexFormat: 0
    secondaryUVAngleDistortion: 8
    secondaryUVAreaDistortion: 15.000001
    secondaryUVHardAngle: 88
    secondaryUVMarginMethod: 1
    secondaryUVMinLightmapResolution: 40
    secondaryUVMinObjectScale: 1
    secondaryUVPackMargin: 4
    useFileScale: 1
    strictVertexDataChecks: 0
  tangentSpace:
    normalSmoothAngle: 60
    normalImportMode: 0
    tangentImportMode: 3
    normalCalculationMode: 4
    legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes: 0
    blendShapeNormalImportMode: 1
    normalSmoothingSource: 0
    calculateBlendshapeNormalsDeltaFromImportedNormals: 0
  referencedClips: []
  importAnimation: 1
  humanDescription:
    serializedVersion: 3
    human: []
    skeleton: []
    armTwist: 0.5
    foreArmTwist: 0.5
    upperLegTwist: 0.5
    legTwist: 0.5
    armStretch: 0.05
    legStretch: 0.05
    feetSpacing: 0
    globalScale: 1
    rootMotionBoneName: 
    hasTranslationDoF: 0
    hasExtraRoot: 0
    skeletonHasParents: 1
  lastHumanDescriptionAvatarSource: {{instanceID: 0}}
  autoGenerateAvatarMappingIfUnspecified: 1
  animationType: 2
  humanoidOversampling: 1
  avatarSetup: 0
  addHumanoidExtraRootOnlyWhenUsingAvatar: 1
  importBlendShapeDeformPercent: 1
  remapMaterialsIfMaterialImportModeIsNone: 0
  additionalBone: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(content)

def write_material_meta(mat_rel):
    meta_path = os.path.join(ROOT, f"{mat_rel}.meta")
    guid = get_guid(mat_rel)
    content = f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 2100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(content)

def write_prefab_meta(prefab_rel):
    meta_path = os.path.join(ROOT, f"{prefab_rel}.meta")
    guid = get_guid(prefab_rel)
    content = f"""fileFormatVersion: 2
guid: {guid}
PrefabImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(content)

def generate_all_metas():
    # Folder metas
    folders = [
        "Assets/TramChanh/Art/Materials/Food",
        "Assets/TramChanh/Art/Textures/Food",
        "Assets/TramChanh/Prefabs/Items/NewMenu"
    ]
    for folder in folders:
        write_folder_meta(folder)

    # Texture metas
    textures = [
        "Assets/TramChanh/Art/Textures/Food/T_NewDrinks_BaseColor.png",
        "Assets/TramChanh/Art/Textures/Food/T_NewCakes_BaseColor.png"
    ]
    for tex in textures:
        write_texture_meta(tex)

    # Model metas
    models = [
        "Assets/TramChanh/Art/Models/Food/SM_Drink_HoneyLemonTea.obj",
        "Assets/TramChanh/Art/Models/Food/SM_Drink_PassionfruitTea.obj",
        "Assets/TramChanh/Art/Models/Food/SM_Cake_CustardCream.obj",
        "Assets/TramChanh/Art/Models/Food/SM_Cake_BananaFlambee.obj"
    ]
    for mod in models:
        write_model_meta(mod)

    # Material metas
    materials = [
        "Assets/TramChanh/Art/Materials/Food/MAT_Drink_HoneyLemonTea.mat",
        "Assets/TramChanh/Art/Materials/Food/MAT_Drink_PassionfruitTea.mat",
        "Assets/TramChanh/Art/Materials/Food/MAT_Cake_CustardCream.mat",
        "Assets/TramChanh/Art/Materials/Food/MAT_Cake_BananaFlambee.mat"
    ]
    for mat in materials:
        write_material_meta(mat)

    # Prefab metas
    prefabs = [
        "Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Drink_HoneyLemonTea.prefab",
        "Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Drink_PassionfruitTea.prefab",
        "Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Cake_CustardCream.prefab",
        "Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Cake_BananaFlambee.prefab"
    ]
    for pf in prefabs:
        write_prefab_meta(pf)

    print("All Unity Meta Files successfully generated!")

# -----------------------------------------------------------------------------
# Main Orchestrator
# -----------------------------------------------------------------------------
def main():
    print("=== Generating ART-FIX-028 New Menu Assets ===")
    
    # 1. Textures
    generate_drink_atlas(os.path.join(ROOT, "Assets/TramChanh/Art/Textures/Food/T_NewDrinks_BaseColor.png"))
    generate_cake_atlas(os.path.join(ROOT, "Assets/TramChanh/Art/Textures/Food/T_NewCakes_BaseColor.png"))

    # 2. Meshes
    t1 = build_honey_lemon_tea(os.path.join(ROOT, "Assets/TramChanh/Art/Models/Food/SM_Drink_HoneyLemonTea.obj"))
    t2 = build_passionfruit_tea(os.path.join(ROOT, "Assets/TramChanh/Art/Models/Food/SM_Drink_PassionfruitTea.obj"))
    t3 = build_cake_custard_cream(os.path.join(ROOT, "Assets/TramChanh/Art/Models/Food/SM_Cake_CustardCream.obj"))
    t4 = build_cake_banana_flambee(os.path.join(ROOT, "Assets/TramChanh/Art/Models/Food/SM_Cake_BananaFlambee.obj"))

    print(f"\n--- Polygon Budget Verification (< 2,500 triangles each) ---")
    print(f"SM_Drink_HoneyLemonTea:   {t1} tris (Budget: 2,500) -> {'PASS' if t1 < 2500 else 'FAIL'}")
    print(f"SM_Drink_PassionfruitTea: {t2} tris (Budget: 2,500) -> {'PASS' if t2 < 2500 else 'FAIL'}")
    print(f"SM_Cake_CustardCream:     {t3} tris (Budget: 2,500) -> {'PASS' if t3 < 2500 else 'FAIL'}")
    print(f"SM_Cake_BananaFlambee:    {t4} tris (Budget: 2,500) -> {'PASS' if t4 < 2500 else 'FAIL'}")

    # 3. Materials
    generate_materials()

    # 4. Prefabs
    generate_prefabs()

    # 5. Meta files
    generate_all_metas()

    print("\n=== Generation Complete ===")

if __name__ == "__main__":
    main()
