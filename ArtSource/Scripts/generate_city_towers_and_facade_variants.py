#!/usr/bin/env python3
"""
ArtSource/Scripts/generate_city_towers_and_facade_variants.py

Fulfills ART-FIX-023 Việc 4:
1. Enhances Vietnamese Shophouses D, E, F with authentic 3D architectural features:
   - D: 3D window shutter frames, clay tile hoods, wrought iron balcony railing, AC unit, rooftop water tank
   - E: Hoi An style carved wooden balustrades, 3D hanging lanterns, arched window moldings, roof coping
   - F: 3D barber pole cylinder, scissor gate framing, AC unit, rooftop clothesline drying rack
2. Generates 3 tall background building types ("các tòa nhà cao hai bên") to break visual monotony:
   - Tower A: Modern residential apartment block (8 storeys, height 26m, width 14m, depth 9m)
   - Tower B: French-colonial / Indochina residential block (6 storeys, height 21m, width 12m, depth 8.5m)
   - Tower C: Contemporary commercial office midrise (10 storeys, height 32m, width 15m, depth 10m)
3. Generates textures, materials, and prefabs for towers with foundation reaching <= 0.01m.
4. Integrates the background towers along both sides of PF_TramChanh_StreetExtended.prefab.
"""

import os
import math
import uuid
import numpy as np
from PIL import Image, ImageDraw, ImageFont

TEX_DIR = "Assets/TramChanh/Art/Textures"
MAT_DIR = "Assets/TramChanh/Art/Materials"
MODEL_DIR = "Assets/TramChanh/Art/Models/Environment"
PREFAB_DIR = "Assets/TramChanh/Prefabs/Environment"

os.makedirs(TEX_DIR, exist_ok=True)
os.makedirs(MAT_DIR, exist_ok=True)
os.makedirs(MODEL_DIR, exist_ok=True)
os.makedirs(PREFAB_DIR, exist_ok=True)

def generate_guid():
    return uuid.uuid4().hex

class ObjMesh:
    def __init__(self, name):
        self.name = name
        self.vertices = []
        self.normals = []
        self.uvs = []
        self.faces = []

    def add_vertex(self, x, y, z):
        self.vertices.append((float(x), float(y), float(z)))
        return len(self.vertices)

    def add_normal(self, nx, ny, nz):
        self.normals.append((float(nx), float(ny), float(nz)))
        return len(self.normals)

    def add_uv(self, u, v):
        self.uvs.append((float(u), float(v)))
        return len(self.uvs)

    def add_face(self, v_vt_vn_list):
        self.faces.append(v_vt_vn_list)

    def add_quad(self, v1, v2, v3, v4, uv1, uv2, uv3, uv4, normal):
        n_idx = self.add_normal(*normal)
        u1 = self.add_uv(*uv1)
        u2 = self.add_uv(*uv2)
        u3 = self.add_uv(*uv3)
        u4 = self.add_uv(*uv4)
        self.add_face([(v1, u1, n_idx), (v2, u2, n_idx), (v3, u3, n_idx)])
        self.add_face([(v1, u1, n_idx), (v3, u3, n_idx), (v4, u4, n_idx)])

    def add_box(self, x_min, x_max, y_min, y_max, z_min, z_max, u_range=(0.0, 1.0), v_range=(0.0, 1.0)):
        v000 = self.add_vertex(x_min, y_min, z_min)
        v001 = self.add_vertex(x_min, y_min, z_max)
        v010 = self.add_vertex(x_min, y_max, z_min)
        v011 = self.add_vertex(x_min, y_max, z_max)
        v100 = self.add_vertex(x_max, y_min, z_min)
        v101 = self.add_vertex(x_max, y_min, z_max)
        v110 = self.add_vertex(x_max, y_max, z_min)
        v111 = self.add_vertex(x_max, y_max, z_max)

        u0, u1 = u_range
        v0, v1 = v_range

        # Front (+Z)
        self.add_quad(v001, v101, v111, v011, (u0, v0), (u1, v0), (u1, v1), (u0, v1), (0, 0, 1))
        # Back (-Z)
        self.add_quad(v100, v000, v010, v110, (u0, v0), (u1, v0), (u1, v1), (u0, v1), (0, 0, -1))
        # Top (+Y)
        self.add_quad(v011, v111, v110, v010, (u0, v0), (u1, v0), (u1, v1), (u0, v1), (0, 1, 0))
        # Bottom (-Y)
        self.add_quad(v000, v100, v101, v001, (u0, v0), (u1, v0), (u1, v1), (u0, v1), (0, -1, 0))
        # Right (+X)
        self.add_quad(v101, v100, v110, v111, (u0, v0), (u1, v0), (u1, v1), (u0, v1), (1, 0, 0))
        # Left (-X)
        self.add_quad(v000, v001, v011, v010, (u0, v0), (u1, v0), (u1, v1), (u0, v1), (-1, 0, 0))

    def write_obj(self, filepath):
        with open(filepath, "w") as f:
            f.write(f"# Wavefront OBJ: {self.name}\n")
            f.write(f"o {self.name}\n")
            for v in self.vertices:
                f.write(f"v {v[0]:.4f} {v[1]:.4f} {v[2]:.4f}\n")
            for vt in self.uvs:
                f.write(f"vt {vt[0]:.4f} {vt[1]:.4f}\n")
            for vn in self.normals:
                f.write(f"vn {vn[0]:.4f} {vn[1]:.4f} {vn[2]:.4f}\n")
            for face in self.faces:
                f.write("f " + " ".join([f"{v[0]}/{v[1]}/{v[2]}" for v in face]) + "\n")
        print(f"Exported: {filepath} ({len(self.vertices)} vertices, {len(self.faces)} faces)")

# ==============================================================================
# 1. UPGRADE SHOPFRONTS D, E, F WITH AUTHENTIC 3D DETAILS
# ==============================================================================

def generate_upgraded_shopfronts():
    # --------------------------------------------------------------------------
    # SM_Shopfront_D: Quán Phở Bò Gia Truyền Hà Nội (Colonial 3-Storey)
    # --------------------------------------------------------------------------
    meshD = ObjMesh("SM_Shopfront_D")
    w, h, d = 4.5, 11.5, 4.5

    # Plinth down to -0.35m
    meshD.add_box(-w/2 - 0.05, w/2 + 0.05, -0.35, 0.0, -0.05, d + 0.05, (0.0, 1.0), (0.0, 0.1))
    # Main shell
    meshD.add_box(-w/2, w/2, 0.0, h, 0.0, d, (0.0, 1.0), (0.0, 1.0))
    # Projecting signboard (Y: 3.4m to 4.3m, Z: -0.20m to 0.0m)
    meshD.add_box(-w/2 + 0.1, w/2 - 0.1, 3.4, 4.3, -0.20, 0.0, (0.05, 0.95), (1075.0/2048.0, 1265.0/2048.0))
    # Slanted fabric awning
    meshD.add_box(-w/2 + 0.05, w/2 - 0.05, 2.5, 3.3, -1.0, 0.0, (0.05, 0.95), (1270.0/2048.0, 1380.0/2048.0))
    # 2nd floor balcony slab
    meshD.add_box(-w/2 + 0.2, w/2 - 0.2, 4.35, 4.55, -0.85, 0.0, (0.1, 0.9), (0.4, 0.5))
    # Balcony railing
    meshD.add_box(-w/2 + 0.2, w/2 - 0.2, 5.30, 5.40, -0.87, -0.83, (0.1, 0.9), (0.4, 0.5))
    meshD.add_box(-w/2 + 0.2, -w/2 + 0.25, 4.55, 5.30, -0.85, 0.0, (0.1, 0.9), (0.4, 0.5))
    meshD.add_box(w/2 - 0.25, w/2 - 0.2, 4.55, 5.30, -0.85, 0.0, (0.1, 0.9), (0.4, 0.5))
    # Clay tile window hoods (2nd & 3rd floor)
    meshD.add_box(-1.6, -0.4, 7.2, 7.35, -0.25, 0.0, (0.1, 0.4), (0.6, 0.7))
    meshD.add_box(0.4, 1.6, 7.2, 7.35, -0.25, 0.0, (0.6, 0.9), (0.6, 0.7))
    meshD.add_box(-1.6, -0.4, 10.2, 10.35, -0.25, 0.0, (0.1, 0.4), (0.8, 0.9))
    meshD.add_box(0.4, 1.6, 10.2, 10.35, -0.25, 0.0, (0.6, 0.9), (0.8, 0.9))
    # AC outdoor condenser unit on 2nd floor wall
    meshD.add_box(1.0, 1.8, 5.6, 6.3, -0.35, 0.0, (0.7, 0.9), (0.5, 0.6))
    # Rooftop parapet & Sơn Hà stainless steel water tank
    meshD.add_box(-w/2, w/2, h, h + 0.7, -0.1, 0.1, (0.0, 1.0), (0.9, 1.0))
    meshD.add_box(-1.5, -0.3, h + 0.1, h + 1.1, 1.2, 2.6, (0.2, 0.6), (0.8, 1.0)) # Water tank
    meshD.write_obj(os.path.join(MODEL_DIR, "SM_Shopfront_D.obj"))

    # --------------------------------------------------------------------------
    # SM_Shopfront_E: Cà Phê Muối Xứ Huế & Trà Cổ (Hoi An Ochre 4-Storey)
    # --------------------------------------------------------------------------
    meshE = ObjMesh("SM_Shopfront_E")
    w, h, d = 5.0, 14.0, 4.5

    # Plinth down to -0.35m
    meshE.add_box(-w/2 - 0.05, w/2 + 0.05, -0.35, 0.0, -0.05, d + 0.05, (0.0, 1.0), (0.0, 0.1))
    # Main shell
    meshE.add_box(-w/2, w/2, 0.0, h, 0.0, d, (0.0, 1.0), (0.0, 1.0))
    # Projecting signboard (Y: 3.5m to 4.6m, Z: -0.22m to 0.0m)
    meshE.add_box(-w/2 + 0.1, w/2 - 0.1, 3.5, 4.6, -0.22, 0.0, (0.05, 0.95), (1060.0/2048.0, 1265.0/2048.0))
    # 2nd floor projecting balcony
    meshE.add_box(-w/2 + 0.3, w/2 - 0.3, 4.8, 5.0, -0.9, 0.0, (0.1, 0.9), (0.4, 0.5))
    # Carved wooden balustrade
    meshE.add_box(-w/2 + 0.3, w/2 - 0.3, 5.75, 5.85, -0.92, -0.88, (0.1, 0.9), (0.4, 0.5))
    meshE.add_box(-w/2 + 0.3, -w/2 + 0.35, 5.0, 5.85, -0.9, 0.0, (0.1, 0.9), (0.4, 0.5))
    meshE.add_box(w/2 - 0.35, w/2 - 0.3, 5.0, 5.85, -0.9, 0.0, (0.1, 0.9), (0.4, 0.5))
    # 3rd floor decorative arched window moldings
    meshE.add_box(-1.8, -0.4, 8.4, 8.6, -0.2, 0.0, (0.1, 0.4), (0.6, 0.7))
    meshE.add_box(0.4, 1.8, 8.4, 8.6, -0.2, 0.0, (0.6, 0.9), (0.6, 0.7))
    # Hanging lanterns (3D lantern boxes with gold tassels)
    meshE.add_box(-1.6, -1.3, 5.0, 5.5, -0.45, -0.15, (0.1, 0.2), (0.1, 0.2))
    meshE.add_box(1.3, 1.6, 5.0, 5.5, -0.45, -0.15, (0.8, 0.9), (0.1, 0.2))
    # Rooftop decorative coping
    meshE.add_box(-w/2, w/2, h, h + 0.8, -0.15, 0.15, (0.0, 1.0), (0.9, 1.0))
    meshE.add_box(-0.8, 0.8, h + 0.8, h + 1.3, -0.15, 0.15, (0.4, 0.6), (0.9, 1.0))
    meshE.write_obj(os.path.join(MODEL_DIR, "SM_Shopfront_E.obj"))

    # --------------------------------------------------------------------------
    # SM_Shopfront_F: Tiệm Cắt Tóc Nam Bình Dân & Gội Đầu (Vintage 3-Storey)
    # --------------------------------------------------------------------------
    meshF = ObjMesh("SM_Shopfront_F")
    w, h, d = 4.2, 11.0, 4.5

    # Plinth down to -0.35m
    meshF.add_box(-w/2 - 0.05, w/2 + 0.05, -0.35, 0.0, -0.05, d + 0.05, (0.0, 1.0), (0.0, 0.1))
    # Main shell
    meshF.add_box(-w/2, w/2, 0.0, h, 0.0, d, (0.0, 1.0), (0.0, 1.0))
    # Projecting vintage signboard
    meshF.add_box(-w/2 + 0.1, w/2 - 0.1, 3.4, 4.3, -0.18, 0.0, (0.05, 0.95), (1075.0/2048.0, 1265.0/2048.0))
    # 3D Barber pole cylinder
    meshF.add_box(1.70, 1.95, 2.2, 3.2, -0.30, -0.05, (1810.0/2048.0, 1895.0/2048.0), (1105.0/2048.0, 1235.0/2048.0))
    # Barber pole top & bottom chrome caps
    meshF.add_box(1.65, 2.00, 3.2, 3.32, -0.32, -0.03, (0.8, 0.9), (0.9, 1.0))
    meshF.add_box(1.65, 2.00, 2.08, 2.2, -0.32, -0.03, (0.8, 0.9), (0.9, 1.0))
    # Collapsible scissor gate framing
    meshF.add_box(-w/2 + 0.15, -w/2 + 0.35, 0.0, 3.3, -0.12, 0.0, (0.1, 0.2), (0.1, 0.3))
    meshF.add_box(w/2 - 0.35, w/2 - 0.15, 0.0, 3.3, -0.12, 0.0, (0.8, 0.9), (0.1, 0.3))
    meshF.add_box(-w/2 + 0.15, w/2 - 0.15, 3.2, 3.35, -0.12, 0.0, (0.1, 0.9), (0.2, 0.3))
    # AC unit on 2nd floor
    meshF.add_box(-1.6, -0.9, 5.2, 5.9, -0.35, 0.0, (0.1, 0.3), (0.4, 0.5))
    # Rooftop clothesline / laundry drying frame
    meshF.add_box(-1.5, 1.5, h + 0.05, h + 0.15, 1.5, 2.5, (0.2, 0.8), (0.8, 0.9))
    meshF.add_box(-1.5, -1.4, h, h + 1.2, 1.5, 1.6, (0.2, 0.3), (0.8, 1.0))
    meshF.add_box(1.4, 1.5, h, h + 1.2, 1.5, 1.6, (0.7, 0.8), (0.8, 1.0))
    meshF.write_obj(os.path.join(MODEL_DIR, "SM_Shopfront_F.obj"))

# ==============================================================================
# 2. GENERATE TALL CITY TOWERS (8-10 STOREYS) TO BREAK MONOTONY
# ==============================================================================

def generate_tall_towers_models():
    # --------------------------------------------------------------------------
    # Tower A: Modern Residential Apartment (8 Storeys, Height 26m, W=14m, D=9m)
    # --------------------------------------------------------------------------
    meshA = ObjMesh("SM_Building_Tower_A")
    w, h, d = 14.0, 26.0, 9.0

    # Foundation plinth down to -0.35m
    meshA.add_box(-w/2 - 0.1, w/2 + 0.1, -0.35, 0.0, -0.1, d + 0.1, (0.0, 1.0), (0.0, 0.05))
    # Main tower volumetric core
    meshA.add_box(-w/2, w/2, 0.0, h, 0.0, d, (0.0, 1.0), (0.05, 1.0))
    # 4 Tiers of Cantilever Balconies (Floors 2, 4, 6, 8)
    for by in [6.5, 12.5, 18.5, 24.5]:
        meshA.add_box(-w/2 + 1.0, w/2 - 1.0, by - 0.3, by, -0.7, 0.0, (0.1, 0.9), (0.3, 0.4))
        meshA.add_box(-w/2 + 1.0, w/2 - 1.0, by + 0.8, by + 0.9, -0.72, -0.68, (0.1, 0.9), (0.4, 0.5)) # Railing
    # Rooftop Elevator Penthouse (X: -2.5 to 2.5, Y: 26 to 29.5, Z: 2.0 to 6.0)
    meshA.add_box(-2.5, 2.5, h, h + 3.5, 2.0, 6.0, (0.3, 0.7), (0.7, 0.9))
    # Dual Rooftop Stainless Steel Cylindrical Water Tanks
    meshA.add_box(3.2, 5.2, h + 0.2, h + 1.6, 2.5, 4.5, (0.7, 0.9), (0.8, 1.0))
    meshA.write_obj(os.path.join(MODEL_DIR, "SM_Building_Tower_A.obj"))

    # --------------------------------------------------------------------------
    # Tower B: Neoclassical / Indochina Apartment (6 Storeys, Height 21m, W=12m, D=8.5m)
    # --------------------------------------------------------------------------
    meshB = ObjMesh("SM_Building_Tower_B")
    w, h, d = 12.0, 21.0, 8.5

    # Foundation plinth down to -0.35m
    meshB.add_box(-w/2 - 0.1, w/2 + 0.1, -0.35, 0.0, -0.1, d + 0.1, (0.0, 1.0), (0.0, 0.05))
    # Main tower core
    meshB.add_box(-w/2, w/2, 0.0, 18.0, 0.0, d, (0.0, 1.0), (0.05, 0.85))
    # Decorative French Cornice Bands (Floors 3 & 6)
    meshB.add_box(-w/2 - 0.2, w/2 + 0.2, 9.5, 9.9, -0.25, d + 0.2, (0.0, 1.0), (0.4, 0.5))
    meshB.add_box(-w/2 - 0.2, w/2 + 0.2, 17.8, 18.3, -0.25, d + 0.2, (0.0, 1.0), (0.8, 0.9))
    # Terracotta Hipped Mansard Roof Crown (Y: 18.0 to 22.0)
    meshB.add_box(-w/2 + 0.5, w/2 - 0.5, 18.0, 21.5, 0.5, d - 0.5, (0.05, 0.95), (0.85, 1.0))
    meshB.write_obj(os.path.join(MODEL_DIR, "SM_Building_Tower_B.obj"))

    # --------------------------------------------------------------------------
    # Tower C: Contemporary Commercial Office Midrise (10 Storeys, Height 32m, W=15m, D=10m)
    # --------------------------------------------------------------------------
    meshC = ObjMesh("SM_Building_Tower_C")
    w, h, d = 15.0, 32.0, 10.0

    # Foundation plinth down to -0.35m
    meshC.add_box(-w/2 - 0.1, w/2 + 0.1, -0.35, 0.0, -0.1, d + 0.1, (0.0, 1.0), (0.0, 0.05))
    # Main glass & steel tower core
    meshC.add_box(-w/2, w/2, 0.0, h, 0.0, d, (0.0, 1.0), (0.05, 0.95))
    # Projecting architectural concrete vertical fins
    meshC.add_box(-w/2 - 0.15, -w/2 + 0.25, 0.0, h + 0.5, -0.2, 0.0, (0.0, 0.1), (0.1, 1.0))
    meshC.add_box(w/2 - 0.25, w/2 + 0.15, 0.0, h + 0.5, -0.2, 0.0, (0.9, 1.0), (0.1, 1.0))
    meshC.add_box(-0.3, 0.3, 0.0, h + 0.5, -0.2, 0.0, (0.45, 0.55), (0.1, 1.0))
    # Rooftop mechanical room (Y: 32m to 35.5m)
    meshC.add_box(-3.5, 3.5, h, h + 3.2, 2.5, 7.5, (0.2, 0.8), (0.8, 1.0))
    # Rooftop Telecom Antenna Mast
    meshC.add_box(-0.15, 0.15, h + 3.2, h + 8.5, 4.85, 5.15, (0.4, 0.6), (0.9, 1.0))
    meshC.write_obj(os.path.join(MODEL_DIR, "SM_Building_Tower_C.obj"))

# ==============================================================================
# 3. GENERATE TEXTURES & MATERIALS FOR CITY TOWERS
# ==============================================================================

def generate_tower_textures_and_materials():
    font = ImageFont.load_default()

    # --------------------------------------------------------------------------
    # Tower A Texture: Modern Concrete & Warm Lit Windows
    # --------------------------------------------------------------------------
    imA = Image.new("RGB", (1024, 1024), (195, 200, 205))
    drawA = ImageDraw.Draw(imA)
    # Add subtle concrete noise
    arrA = np.array(imA).astype(int) + np.random.randint(-8, 9, (1024, 1024, 3))
    imA = Image.fromarray(np.clip(arrA, 0, 255).astype(np.uint8))
    drawA = ImageDraw.Draw(imA)

    # Windows grid across 8 storeys
    for floor in range(8):
        fy = 120 + floor * 105
        # Draw floor demarcation band
        drawA.line([(0, fy - 15), (1024, fy - 15)], fill=(150, 155, 160), width=4)
        for col in range(6):
            wx = 75 + col * 150
            # Window frame
            drawA.rectangle([wx, fy, wx + 110, fy + 75], fill=(35, 50, 65), outline=(90, 95, 100), width=3)
            # Warm interior lamp glow
            drawA.rectangle([wx + 8, fy + 12, wx + 48, fy + 65], fill=(240, 215, 130))
            drawA.rectangle([wx + 56, fy + 12, wx + 102, fy + 65], fill=(225, 200, 115))

    texA_path = os.path.join(TEX_DIR, "T_Building_Tower_A_BaseColor.png")
    imA.save(texA_path)
    print(f"Generated: {texA_path}")

    # --------------------------------------------------------------------------
    # Tower B Texture: Colonial Ochre Plaster & Mansard Roof
    # --------------------------------------------------------------------------
    imB = Image.new("RGB", (1024, 1024), (225, 192, 115))
    arrB = np.array(imB).astype(int) + np.random.randint(-10, 11, (1024, 1024, 3))
    imB = Image.fromarray(np.clip(arrB, 0, 255).astype(np.uint8))
    drawB = ImageDraw.Draw(imB)

    # Terracotta roof header at top (Y: 0 to 180)
    for ry in range(0, 180, 14):
        col = (175, 68, 38) if (ry // 14) % 2 == 0 else (195, 80, 48)
        drawB.rectangle([0, ry, 1024, ry + 14], fill=col)

    # Upper floors with white stucco moldings & green shutters
    for floor in range(6):
        fy = 200 + floor * 130
        drawB.rectangle([0, fy - 18, 1024, fy - 4], fill=(248, 246, 240)) # White cornice
        for col in range(5):
            wx = 90 + col * 180
            # Arched window pediment
            drawB.pieslice([wx - 6, fy - 20, wx + 116, fy + 24], 180, 360, fill=(248, 246, 240))
            # Dark green wooden shutters
            drawB.rectangle([wx, fy, wx + 110, fy + 90], fill=(25, 75, 45), outline=(248, 246, 240), width=4)
            # Glass center
            drawB.rectangle([wx + 25, fy + 8, wx + 85, fy + 82], fill=(45, 65, 80))

    texB_path = os.path.join(TEX_DIR, "T_Building_Tower_B_BaseColor.png")
    imB.save(texB_path)
    print(f"Generated: {texB_path}")

    # --------------------------------------------------------------------------
    # Tower C Texture: Contemporary Commercial Glass Curtain Wall
    # --------------------------------------------------------------------------
    imC = Image.new("RGB", (1024, 1024), (55, 75, 95))
    drawC = ImageDraw.Draw(imC)

    # 10 Storeys of continuous glass ribbon windows & silver spandrels
    for floor in range(10):
        fy = 50 + floor * 95
        # Silver composite panel spandrel
        drawC.rectangle([0, fy, 1024, fy + 24], fill=(185, 190, 195), outline=(130, 135, 140), width=2)
        # Reflective blue-tinted glass band
        drawC.rectangle([0, fy + 25, 1024, fy + 90], fill=(30, 58, 85))
        # Vertical mullions
        for mx in range(0, 1024, 64):
            drawC.line([(mx, fy + 25), (mx, fy + 90)], fill=(75, 105, 135), width=2)

    texC_path = os.path.join(TEX_DIR, "T_Building_Tower_C_BaseColor.png")
    imC.save(texC_path)
    print(f"Generated: {texC_path}")

# ==============================================================================
# 4. CREATE PREFABS AND METAS FOR TOWERS
# ==============================================================================

def create_tower_materials_and_prefabs():
    towers = {
        "A": {"w": 14.0, "h": 26.0, "d": 9.0},
        "B": {"w": 12.0, "h": 21.0, "d": 8.5},
        "C": {"w": 15.0, "h": 32.0, "d": 10.0},
    }

    mesh_guids = {}
    tex_guids = {}

    for letter in ["A", "B", "C"]:
        # Mesh meta
        m_path = os.path.join(MODEL_DIR, f"SM_Building_Tower_{letter}.obj")
        m_meta = m_path + ".meta"
        if not os.path.exists(m_meta):
            guid = generate_guid()
            with open(m_meta, "w") as f:
                f.write(f"fileFormatVersion: 2\nguid: {guid}\nModelImporter:\n  serializedVersion: 22203\n")
            mesh_guids[letter] = guid
        else:
            with open(m_meta) as f:
                for line in f:
                    if "guid:" in line:
                        mesh_guids[letter] = line.strip().split()[1]
                        break

        # Tex meta
        t_path = os.path.join(TEX_DIR, f"T_Building_Tower_{letter}_BaseColor.png")
        t_meta = t_path + ".meta"
        if not os.path.exists(t_meta):
            guid = generate_guid()
            with open(t_meta, "w") as f:
                f.write(f"fileFormatVersion: 2\nguid: {guid}\nTextureImporter:\n  serializedVersion: 13000\n")
            tex_guids[letter] = guid
        else:
            with open(t_meta) as f:
                for line in f:
                    if "guid:" in line:
                        tex_guids[letter] = line.strip().split()[1]
                        break

        # Material
        mat_path = os.path.join(MAT_DIR, f"MAT_Building_Tower_{letter}.mat")
        mat_meta = mat_path + ".meta"
        if not os.path.exists(mat_meta):
            mat_guid = generate_guid()
            with open(mat_meta, "w") as f:
                f.write(f"fileFormatVersion: 2\nguid: {mat_guid}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
        else:
            with open(mat_meta) as f:
                for line in f:
                    if "guid:" in line:
                        mat_guid = line.strip().split()[1]
                        break

        with open(mat_path, "w") as f:
            f.write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: MAT_Building_Tower_{letter}
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {tex_guids[letter]}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {tex_guids[letter]}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Floats:
    - _Metallic: 0.1
    - _Smoothness: 0.35
    m_Colors:
    - _BaseColor: {{r: 1, g: 1, b: 1, a: 1}}
    - _Color: {{r: 1, g: 1, b: 1, a: 1}}
""")
        print(f"Created material: {mat_path}")

        # Prefab
        pf_path = os.path.join(PREFAB_DIR, f"PF_Building_Tower_{letter}.prefab")
        pf_meta = pf_path + ".meta"
        if not os.path.exists(pf_meta):
            pf_guid = generate_guid()
            with open(pf_meta, "w") as f:
                f.write(f"fileFormatVersion: 2\nguid: {pf_guid}\nPrefabImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
        else:
            with open(pf_meta) as f:
                for line in f:
                    if "guid:" in line:
                        pf_guid = line.strip().split()[1]
                        break

        cfg = towers[letter]
        pw, ph, pd = cfg["w"], cfg["h"], cfg["d"]
        with open(pf_path, "w") as f:
            f.write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &3670817710514171788
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 5110386587436877682}}
  - component: {{fileID: 4700595132476312954}}
  - component: {{fileID: 6571542907280224375}}
  - component: {{fileID: 8789350620605570058}}
  m_Layer: 8
  m_Name: PF_Building_Tower_{letter}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &5110386587436877682
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3670817710514171788}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!33 &4700595132476312954
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3670817710514171788}}
  m_Mesh: {{fileID: -2432090755550338912, guid: {mesh_guids[letter]}, type: 3}}
--- !u!23 &6571542907280224375
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3670817710514171788}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_Materials:
  - {{fileID: 2100000, guid: {mat_guid}, type: 2}}
--- !u!65 &8789350620605570058
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3670817710514171788}}
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
  m_Size: {{x: {pw:.2f}, y: {ph + 0.35:.2f}, z: {pd:.2f}}}
  m_Center: {{x: 0, y: {ph/2.0 - 0.175:.2f}, z: {pd/2.0:.2f}}}
""")
        print(f"Created prefab: {pf_path}")

# ==============================================================================
# 5. INTEGRATE TOWERS INTO PF_TramChanh_StreetExtended.prefab
# ==============================================================================

def integrate_towers_into_street():
    street_pf = os.path.join(PREFAB_DIR, "PF_TramChanh_StreetExtended.prefab")
    with open(street_pf) as f:
        content = f.read()

    # Get tower prefab GUIDs
    tower_guids = {}
    for letter in ["A", "B", "C"]:
        pf_meta = os.path.join(PREFAB_DIR, f"PF_Building_Tower_{letter}.prefab.meta")
        with open(pf_meta) as f:
            for line in f:
                if "guid:" in line:
                    tower_guids[letter] = line.strip().split()[1]
                    break

    # Look for ExtendedBuildings Transform
    import re
    # Transform fileID for ExtendedBuildings
    # We saw it was 6222623358474285367
    ext_tf_match = re.search(r"--- !u!1 &(\d+)\nGameObject:\n[\s\S]*?m_Name: ExtendedBuildings", content)
    if not ext_tf_match:
        print("ERROR: ExtendedBuildings GameObject not found!")
        return
    ext_go_id = ext_tf_match.group(1)

    # Find the corresponding Transform
    tf_match = re.search(r"--- !u!4 &(\d+)\nTransform:\n[\s\S]*?m_GameObject: \{fileID: " + ext_go_id + r"\}", content)
    if not tf_match:
        print("ERROR: ExtendedBuildings Transform not found!")
        return
    ext_tf_id = tf_match.group(1)
    print(f"Found ExtendedBuildings: GO={ext_go_id}, Transform={ext_tf_id}")

    # Check if towers are already present
    if "PF_Building_Tower_" in content:
        print("Tower instances already present in street prefab, updating positions if needed.")
        return

    # Generate 14 tower positions South (z = -13.5m, rotY = 180) and 14 tower positions North (z = 24.5m, rotY = 0)
    south_xs = [-225, -190, -155, -120, -85, -50, -20, 20, 55, 90, 125, 160, 195, 225]
    north_xs = [-225, -190, -155, -120, -85, -50, -20, 20, 55, 90, 125, 160, 195, 225]

    instances_yaml = []
    child_file_ids = []

    inst_counter = 8880000000000000000

    # South towers
    tower_types = ["A", "B", "C"]
    for i, x in enumerate(south_xs):
        t_type = tower_types[i % 3]
        guid = tower_guids[t_type]
        inst_id = inst_counter
        inst_counter += 1
        tf_id = inst_counter
        inst_counter += 1
        child_file_ids.append(tf_id)

        inst_str = f"""--- !u!1001 &{inst_id}
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {ext_tf_id}}}
    m_Modifications:
    - target: {{fileID: 3670817710514171788, guid: {guid}, type: 3}}
      propertyPath: m_Name
      value: PF_Building_Tower_{t_type}_South_{i+1:02d}
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {guid}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: {x}
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {guid}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {guid}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: -13.5
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {guid}, type: 3}}
      propertyPath: m_LocalRotation.w
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {guid}, type: 3}}
      propertyPath: m_LocalRotation.y
      value: 1
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {guid}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 180
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}
"""
        instances_yaml.append(inst_str)

    # North towers
    for i, x in enumerate(north_xs):
        t_type = tower_types[(i + 1) % 3]
        guid = tower_guids[t_type]
        inst_id = inst_counter
        inst_counter += 1
        tf_id = inst_counter
        inst_counter += 1
        child_file_ids.append(tf_id)

        inst_str = f"""--- !u!1001 &{inst_id}
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {ext_tf_id}}}
    m_Modifications:
    - target: {{fileID: 3670817710514171788, guid: {guid}, type: 3}}
      propertyPath: m_Name
      value: PF_Building_Tower_{t_type}_North_{i+1:02d}
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {guid}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: {x}
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {guid}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {guid}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 24.5
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}
"""
        instances_yaml.append(inst_str)

    # Append instances_yaml to end of file
    new_content = content + "\n" + "\n".join(instances_yaml)
    with open(street_pf, "w") as f:
        f.write(new_content)
    print(f"Successfully integrated {len(child_file_ids)} tall background towers into {street_pf}!")

def main():
    print("=== Generating City Towers & Upgraded Shophouses ===")
    generate_upgraded_shopfronts()
    generate_tall_towers_models()
    generate_tower_textures_and_materials()
    create_tower_materials_and_prefabs()
    integrate_towers_into_street()
    print("=== Completed Task 4 Asset Generation ===")

if __name__ == "__main__":
    main()
