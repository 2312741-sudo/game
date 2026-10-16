#!/usr/bin/env python3
"""
Generates authentic cake models and textures based on real-world reference photos:
- media_1792093714169_0e72cf66.png, media_1792093714170_6021f744.png, media_1792093714172_4f22241b.png (Elmich grill)
- media_1792093766023_5631e7bc.png (Cake station counter cleanup)
- media_1792093966313_aacc4b26.jpg, media_1792093966315_c2de2a78.jpg, media_1792093966342_e8ad8427.jpg (Cake before/after rolling)
"""
import os
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

class ObjWriter:
    def __init__(self, name):
        self.name = name
        self.verts = []
        self.uvs = []
        self.normals = []
        self.faces = []

    def add_vert(self, x, y, z):
        self.verts.append((x, y, z))
        return len(self.verts)

    def add_uv(self, u, v):
        self.uvs.append((u, v))
        return len(self.uvs)

    def add_normal(self, nx, ny, nz):
        l = math.sqrt(nx*nx + ny*ny + nz*nz) or 1.0
        self.normals.append((nx/l, ny/l, nz/l))
        return len(self.normals)

    def add_tri(self, v1, v2, v3, uv1, uv2, uv3, n):
        self.faces.append([(v1, uv1, n), (v2, uv2, n), (v3, uv3, n)])

    def add_quad(self, v1, v2, v3, v4, uv1, uv2, uv3, uv4, n):
        self.faces.append([(v1, uv1, n), (v2, uv2, n), (v3, uv3, n)])
        self.faces.append([(v1, uv1, n), (v3, uv3, n), (v4, uv4, n)])

    def add_box(self, xmin, xmax, ymin, ymax, zmin, zmax, u_min=0.0, u_max=1.0, v_min=0.0, v_max=1.0):
        # 8 vertices
        v000 = self.add_vert(xmin, ymin, zmin)
        v100 = self.add_vert(xmax, ymin, zmin)
        v110 = self.add_vert(xmax, ymax, zmin)
        v010 = self.add_vert(xmin, ymax, zmin)
        v001 = self.add_vert(xmin, ymin, zmax)
        v101 = self.add_vert(xmax, ymin, zmax)
        v111 = self.add_vert(xmax, ymax, zmax)
        v011 = self.add_vert(xmin, ymax, zmax)

        uv_bl = self.add_uv(u_min, v_min)
        uv_br = self.add_uv(u_max, v_min)
        uv_tr = self.add_uv(u_max, v_max)
        uv_tl = self.add_uv(u_min, v_max)

        # -Z face
        nz = self.add_normal(0, 0, -1)
        self.add_quad(v000, v010, v110, v100, uv_bl, uv_tl, uv_tr, uv_br, nz)
        # +Z face
        pz = self.add_normal(0, 0, 1)
        self.add_quad(v101, v111, v011, v001, uv_bl, uv_tl, uv_tr, uv_br, pz)
        # -X face
        nx = self.add_normal(-1, 0, 0)
        self.add_quad(v001, v011, v010, v000, uv_bl, uv_tl, uv_tr, uv_br, nx)
        # +X face
        px = self.add_normal(1, 0, 0)
        self.add_quad(v100, v110, v111, v101, uv_bl, uv_tl, uv_tr, uv_br, px)
        # -Y face
        ny = self.add_normal(0, -1, 0)
        self.add_quad(v001, v000, v100, v101, uv_bl, uv_tl, uv_tr, uv_br, ny)
        # +Y face
        py = self.add_normal(0, 1, 0)
        self.add_quad(v010, v011, v111, v110, uv_bl, uv_tl, uv_tr, uv_br, py)

    def add_cylinder(self, cx, cz, r, ymin, ymax, segments=12, u_min=0.0, u_max=1.0, v_min=0.0, v_max=1.0):
        bot_ring = []
        top_ring = []
        uvs_bot = []
        uvs_top = []
        for i in range(segments):
            ang = 2.0 * math.pi * i / float(segments)
            x = cx + r * math.cos(ang)
            z = cz + r * math.sin(ang)
            bot_ring.append(self.add_vert(x, ymin, z))
            top_ring.append(self.add_vert(x, ymax, z))
            u = u_min + (u_max - u_min) * (i / float(segments))
            uvs_bot.append(self.add_uv(u, v_min))
            uvs_top.append(self.add_uv(u, v_max))

        c_bot = self.add_vert(cx, ymin, cz)
        c_top = self.add_vert(cx, ymax, cz)
        uv_c = self.add_uv((u_min + u_max) * 0.5, (v_min + v_max) * 0.5)
        ny = self.add_normal(0, -1, 0)
        py = self.add_normal(0, 1, 0)

        for i in range(segments):
            ni = (i + 1) % segments
            ang_mid = 2.0 * math.pi * (i + 0.5) / float(segments)
            n_side = self.add_normal(math.cos(ang_mid), 0, math.sin(ang_mid))
            self.add_quad(bot_ring[i], top_ring[i], top_ring[ni], bot_ring[ni],
                          uvs_bot[i], uvs_top[i], uvs_top[ni], uvs_bot[ni], n_side)
            self.add_tri(c_bot, bot_ring[ni], bot_ring[i], uv_c, uvs_bot[ni], uvs_bot[i], ny)
            self.add_tri(c_top, top_ring[i], top_ring[ni], uv_c, uvs_top[i], uvs_top[ni], py)

    def normalize_to_unit_cube(self):
        if not self.verts: return
        xmin = min(v[0] for v in self.verts)
        xmax = max(v[0] for v in self.verts)
        ymin = min(v[1] for v in self.verts)
        ymax = max(v[1] for v in self.verts)
        zmin = min(v[2] for v in self.verts)
        zmax = max(v[2] for v in self.verts)
        dx = xmax - xmin or 1.0
        dy = ymax - ymin or 1.0
        dz = zmax - zmin or 1.0
        new_verts = []
        for x, y, z in self.verts:
            nx = -0.5 + (x - xmin) / dx
            ny = -0.5 + (y - ymin) / dy
            nz = -0.5 + (z - zmin) / dz
            new_verts.append((nx, ny, nz))
        self.verts = new_verts

    def write(self, filepath):
        os.makedirs(os.path.dirname(filepath), exist_ok=True)
        with open(filepath, 'w') as f:
            f.write(f"# Wavefront OBJ: {self.name}\n")
            f.write(f"o {self.name}\n")
            for v in self.verts:
                f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
            for vt in self.uvs:
                f.write(f"vt {vt[0]:.6f} {vt[1]:.6f}\n")
            for vn in self.normals:
                f.write(f"vn {vn[0]:.6f} {vn[1]:.6f} {vn[2]:.6f}\n")
            for face in self.faces:
                f.write("f " + " ".join(f"{v}/{vt}/{vn}" for v, vt, vn in face) + "\n")
        bx = (min(v[0] for v in self.verts), max(v[0] for v in self.verts))
        by = (min(v[1] for v in self.verts), max(v[1] for v in self.verts))
        bz = (min(v[2] for v in self.verts), max(v[2] for v in self.verts))
        print(f"Generated {filepath}: {len(self.verts)} verts, X:{bx}, Y:{by}, Z:{bz}")

def generate_cake_textures():
    output_dir = "Assets/TramChanh/Art/Textures"
    os.makedirs(output_dir, exist_ok=True)
    
    w, h = 1024, 1024
    img = Image.new("RGBA", (w, h), (228, 165, 82, 255)) # Warm golden-brown waffle base
    draw = ImageDraw.Draw(img)
    
    # 1. Background waffle batter texture: speckled baked grain
    np.random.seed(42)
    noise = np.random.randint(-15, 16, (h, w), dtype=np.int16)
    arr = np.array(img, dtype=np.int16)
    for c in range(3):
        arr[:, :, c] = np.clip(arr[:, :, c] + noise, 0, 255)
    img = Image.fromarray(arr.astype(np.uint8))
    draw = ImageDraw.Draw(img)

    # 2. Parallel deep grill lines (from the Elmich ribbed grill plate)
    # Rib grooves run horizontally across texture
    num_ribs = 14
    rib_spacing = h / float(num_ribs)
    for i in range(num_ribs):
        yc = int((i + 0.5) * rib_spacing)
        # Dark caramelized groove (#582008)
        draw.rectangle([(0, yc - 14), (w, yc + 14)], fill=(88, 32, 8, 255))
        draw.rectangle([(0, yc - 9), (w, yc + 9)], fill=(65, 22, 5, 255))
        draw.rectangle([(0, yc - 4), (w, yc + 4)], fill=(45, 14, 2, 255))
        # Golden toasted highlight ridges along groove borders (#EFA838)
        draw.rectangle([(0, yc - 18), (w, yc - 14)], fill=(235, 165, 50, 255))
        draw.rectangle([(0, yc + 14), (w, yc + 18)], fill=(235, 165, 50, 255))

    # Blur the grill lines slightly for organic toasted look
    img = img.filter(ImageFilter.GaussianBlur(radius=2.0))
    draw = ImageDraw.Draw(img)

    # 3. Drizzled wavy white mayonnaise / cream sauce lines
    # Squeezed along grooves in authentic undulating patterns
    white_sauce = (255, 252, 235, 245)
    sauce_shadow = (180, 120, 60, 180)
    for i in [2, 4, 6, 8, 10, 12]:
        yc = int((i + 0.5) * rib_spacing)
        points = []
        for x in range(0, w, 16):
            # wavy oscillation
            wave = math.sin(x * 0.08 + i * 2.5) * 8.0 + math.cos(x * 0.03) * 4.0
            points.append((x, yc + wave))
        
        # Shadow underneath sauce
        for (x, y) in points:
            draw.ellipse([(x - 8, y - 6 + 3), (x + 8, y + 6 + 3)], fill=sauce_shadow)
        # White creamy sauce body
        for (x, y) in points:
            draw.ellipse([(x - 7, y - 6), (x + 7, y + 6)], fill=white_sauce)
            # Gloss highlight
            draw.ellipse([(x - 3, y - 4), (x + 3, y)], fill=(255, 255, 255, 255))

    # 4. Sprinkled golden pork/egg floss (chà bông) toppings
    # Realistic golden-yellow fibrous flakes scattered over sauce and grill grooves
    floss_colors = [
        (255, 195, 45, 250), # Golden yellow
        (240, 170, 30, 245), # Toasted amber
        (220, 145, 20, 240), # Deep golden
        (255, 215, 80, 255), # Bright highlight
    ]
    np.random.seed(999)
    # Dense scatter along the center and sauce lines
    for _ in range(3500):
        fx = np.random.randint(40, w - 40)
        fy = np.random.randint(60, h - 60)
        col = floss_colors[np.random.randint(0, len(floss_colors))]
        flen = np.random.randint(6, 18)
        fang = np.random.uniform(0, math.pi)
        dx = math.cos(fang) * flen
        dy = math.sin(fang) * flen
        fthick = np.random.randint(2, 4)
        draw.line([(fx, fy), (fx + dx, fy + dy)], fill=col, width=fthick)

    cake_tex_path = os.path.join(output_dir, "T_RollCake_BaseColor.png")
    img.save(cake_tex_path, "PNG")
    print(f"Generated authentic cake texture: {cake_tex_path}")

def generate_cake_models():
    # 1. SM_Cake_Flat.obj (waffle sheet with grilled ribs, sauce, and chà bông)
    flat = ObjWriter("SM_Cake_Flat")
    # Base waffle sheet
    flat.add_box(-0.48, 0.48, -0.40, 0.10, -0.48, 0.48, 0.05, 0.95, 0.05, 0.95)
    # 7 parallel raised grilled waffle ribs
    for i in range(7):
        cz = -0.42 + i * (0.84 / 6.0)
        flat.add_box(-0.47, 0.47, 0.08, 0.25, cz - 0.035, cz + 0.035, 0.05, 0.95, 0.1, 0.9)
    # Sauce lines on top of ribs
    for i in [1, 3, 5]:
        cz = -0.42 + i * (0.84 / 6.0)
        flat.add_box(-0.45, 0.45, 0.24, 0.38, cz - 0.020, cz + 0.020, 0.1, 0.9, 0.4, 0.6)
    # Shredded chà bông topping mounds
    for i in range(12):
        mx = -0.38 + (i % 6) * 0.14
        mz = -0.30 + (i // 6) * 0.45
        flat.add_box(mx - 0.045, mx + 0.045, 0.24, 0.48, mz - 0.045, mz + 0.045, 0.7, 0.95, 0.7, 0.95)
    flat.normalize_to_unit_cube()
    flat.write("Assets/TramChanh/Art/Models/Food/SM_Cake_Flat.obj")

    # 2. SM_Cake_RolledVertical.obj (two cylindrical rolls with longitudinal fluted ridges)
    rolled = ObjWriter("SM_Cake_RolledVertical")
    # Roll 1 (front roll)
    r_outer = 0.22
    rolled.add_cylinder(-0.24, 0, r_outer, -0.48, 0.48, segments=16, u_min=0.05, u_max=0.95, v_min=0.05, v_max=0.95)
    # Longitudinal fluted ribs on Roll 1
    for i in range(6):
        ang = 2.0 * math.pi * i / 6.0
        bx = -0.24 + math.cos(ang) * (r_outer - 0.01)
        bz = math.sin(ang) * (r_outer - 0.01)
        rolled.add_box(bx - 0.025, bx + 0.025, -0.46, 0.46, bz - 0.025, bz + 0.025, 0.6, 0.9, 0.1, 0.4)
    # Roll 2 (back roll)
    rolled.add_cylinder(0.24, 0, r_outer, -0.48, 0.48, segments=16, u_min=0.05, u_max=0.95, v_min=0.05, v_max=0.95)
    for i in range(6):
        ang = 2.0 * math.pi * i / 6.0
        bx = 0.24 + math.cos(ang) * (r_outer - 0.01)
        bz = math.sin(ang) * (r_outer - 0.01)
        rolled.add_box(bx - 0.025, bx + 0.025, -0.46, 0.46, bz - 0.025, bz + 0.025, 0.6, 0.9, 0.1, 0.4)
    rolled.normalize_to_unit_cube()
    rolled.write("Assets/TramChanh/Art/Models/Food/SM_Cake_RolledVertical.obj")

    # 3. SM_RollBoard.obj (Black rubber bar mat next to grill for rolling & tools)
    mat = ObjWriter("SM_RollBoard")
    # Base floor of the mat (20cm x 14cm x 3mm)
    mat.add_box(-0.100, 0.100, 0.000, 0.003, -0.070, 0.070, 0.04, 0.96, 0.04, 0.96)
    # Raised border rim (3mm higher, 8mm wide)
    mat.add_box(-0.100, 0.100, 0.003, 0.006, -0.070, -0.062, 0.0, 1.0, 0.0, 0.08)
    mat.add_box(-0.100, 0.100, 0.003, 0.006, 0.062, 0.070, 0.0, 1.0, 0.92, 1.0)
    mat.add_box(-0.100, -0.092, 0.003, 0.006, -0.062, 0.062, 0.0, 0.08, 0.08, 0.92)
    mat.add_box(0.092, 0.100, 0.003, 0.006, -0.062, 0.062, 0.92, 1.0, 0.08, 0.92)
    mat.write("Assets/TramChanh/Art/Models/CakeStation/SM_RollBoard.obj")

    # 4. SM_CakeWrappingPaper.obj (Neat stack of Kraft wrapping paper sheets with Trạm Chanh brand stamp)
    paper = ObjWriter("SM_CakeWrappingPaper")
    # Base paper stack (16cm x 13cm, 2.5mm thick)
    paper.add_box(-0.081, 0.081, 0.000, 0.002, -0.066, 0.066, 0.05, 0.95, 0.05, 0.95)
    # Top sheet with pristine UV mapping for brand stamp
    v1 = paper.add_vert(-0.080, 0.0028, -0.065)
    v2 = paper.add_vert(0.080, 0.0028, -0.065)
    v3 = paper.add_vert(0.080, 0.0028, 0.065)
    v4 = paper.add_vert(-0.080, 0.0028, 0.065)
    uv1 = paper.add_uv(0.0, 0.0)
    uv2 = paper.add_uv(1.0, 0.0)
    uv3 = paper.add_uv(1.0, 1.0)
    uv4 = paper.add_uv(0.0, 1.0)
    py = paper.add_normal(0, 1, 0)
    paper.add_quad(v1, v4, v3, v2, uv1, uv4, uv3, uv2, py)
    # Side edge skirts
    paper.add_box(-0.080, 0.080, 0.002, 0.0028, -0.065, 0.065, 0.0, 1.0, 0.0, 1.0)
    paper.write("Assets/TramChanh/Art/Models/CakeStation/SM_CakeWrappingPaper.obj")

    # 5. SM_BatterContainer.obj (Stainless steel batter dispenser container)
    vat = ObjWriter("SM_BatterContainer")
    # Cylindrical stainless pot (radius 0.06m, height 0.08m)
    vat.add_cylinder(0, 0, 0.060, 0.000, 0.076, segments=16, u_min=0.1, u_max=0.9, v_min=0.1, v_max=0.9)
    # Top rolled lip
    vat.add_cylinder(0, 0, 0.064, 0.074, 0.080, segments=16, u_min=0.1, u_max=0.9, v_min=0.1, v_max=0.9)
    # Side wire handle
    vat.add_box(0.062, 0.076, 0.040, 0.065, -0.015, 0.015, 0.1, 0.5, 0.1, 0.5)
    vat.write("Assets/TramChanh/Art/Models/CakeStation/SM_BatterContainer.obj")

    # 6. SM_Grill_Display.obj (Slanted touch panel screen mesh)
    disp = ObjWriter("SM_Grill_Display")
    # Front slanted panel vertices
    # Note: X is inverted in OBJ so that after Unity's X mirror, text reads left-to-right
    v_bl = disp.add_vert(0.080, 0.025, -0.1505)
    v_br = disp.add_vert(-0.080, 0.025, -0.1505)
    v_tr = disp.add_vert(-0.080, 0.065, -0.1455)
    v_tl = disp.add_vert(0.080, 0.065, -0.1455)
    uv_bl = disp.add_uv(0.0, 0.0)
    uv_br = disp.add_uv(1.0, 0.0)
    uv_tr = disp.add_uv(1.0, 1.0)
    uv_tl = disp.add_uv(0.0, 1.0)
    n_disp = disp.add_normal(0.0, 0.12, -0.99)
    disp.add_quad(v_bl, v_br, v_tr, v_tl, uv_bl, uv_br, uv_tr, uv_tl, n_disp)
    disp.write("Assets/TramChanh/Art/Models/CakeStation/SM_Grill_Display.obj")

if __name__ == "__main__":
    generate_cake_textures()
    generate_cake_models()
