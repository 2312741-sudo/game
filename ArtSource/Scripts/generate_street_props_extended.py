#!/usr/bin/env python3
"""
ArtSource/Scripts/generate_street_props_extended.py

Generates 5 categories of street props with at least 2 variants each for ART-FIX-022 Task 5:
1. Thùng rác (Trash bins):
   - Variant 1: Green Municipal Wheelie Bin (SM_TrashBin_Municipal.obj, PF_TrashBin_Municipal.prefab)
   - Variant 2: Dual Organic/Inorganic Recycling Bin (SM_TrashBin_Dual.obj, PF_TrashBin_Dual.prefab)
2. Cột điện (Utility poles):
   - Variant 1: Concrete Pole with tangled telecom cables (SM_UtilityPole_Cables.obj, PF_UtilityPole_Cables.prefab)
   - Variant 2: Pole with transformer box & loudspeaker (SM_UtilityPole_Transformer.obj, PF_UtilityPole_Transformer.prefab)
3. Chậu cây (Potted plants):
   - Variant 1: Round ceramic glazed pot with bonsai / bougainvillea (SM_PottedPlant_Ceramic.obj, PF_PottedPlant_Ceramic.prefab)
   - Variant 2: Modern square terrazzo planter with foliage (SM_PottedPlant_Square.obj, PF_PottedPlant_Square.prefab)
4. Ghế đá (Stone benches):
   - Variant 1: Classic Vietnamese curved-back stone park bench (SM_StoneBench_Classic.obj, PF_StoneBench_Classic.prefab)
   - Variant 2: Flat polished granite slab bench (SM_StoneBench_Flat.obj, PF_StoneBench_Flat.prefab)
5. Bảng chỉ dẫn (Signposts):
   - Variant 1: Official Hanoi street name sign "PHỐ TRÀM CHANH" (SM_Signpost_StreetName.obj, PF_Signpost_StreetName.prefab)
   - Variant 2: Pedestrian tourist wayfinding post with 2 directional arrows (SM_Signpost_Wayfinding.obj, PF_Signpost_Wayfinding.prefab)

All models are < 400 triangles (< 3,000 budget), share MAT_Street_RoadsideProps.mat,
and have authentic Vietnamese markings.
"""

import os
import uuid
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont

def generate_guid():
    return uuid.uuid4().hex

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

def draw_text(draw, x, y, text, size=32, fill=(255, 255, 255)):
    font = get_font(size)
    draw.text((x, y), text, fill=fill, font=font)

# ==============================================================================
# 1. TEXTURE ATLAS GENERATION (2048x2048)
# ==============================================================================

def generate_props_atlas():
    tex_path = "Assets/TramChanh/Art/Textures/T_Street_RoadsideProps_Atlas.png"
    im = Image.new("RGB", (2048, 2048), (140, 142, 145))
    draw = ImageDraw.Draw(im)

    # 1. Green Municipal Wheelie Bin: X: [0, 512], Y: [0, 512]
    # Dark green body
    draw.rectangle([0, 0, 512, 512], fill=(32, 98, 48))
    # Bright green lid area
    draw.rectangle([0, 0, 512, 120], fill=(42, 135, 65))
    # Slogan banner in yellow/white
    draw.rectangle([30, 220, 482, 380], fill=(24, 78, 38), outline=(220, 220, 220), width=3)
    draw_text(draw, 50, 240, "HÃY BỎ RÁC VÀO THÙNG", size=32, fill=(255, 245, 100))
    draw_text(draw, 75, 290, "VÌ MÔI TRƯỜNG XANH - SẠCH - ĐẸP", size=22, fill=(255, 255, 255))
    # Recycle mobius icon
    draw.ellipse([215, 395, 295, 475], outline=(255, 255, 255), width=4)
    draw_text(draw, 242, 420, "♻", size=42, fill=(255, 255, 255))

    # 2. Dual Recycling Bin: X: [512, 1024], Y: [0, 512]
    # Left half: Orange (Rác vô cơ)
    draw.rectangle([512, 0, 768, 512], fill=(215, 88, 28))
    draw.rectangle([530, 180, 750, 360], fill=(175, 65, 18), outline=(255, 255, 255), width=3)
    draw_text(draw, 550, 210, "RÁC VÔ CƠ", size=30, fill=(255, 255, 255))
    draw_text(draw, 545, 260, "TÁI CHẾ ĐƯỢC", size=24, fill=(255, 240, 160))
    # Right half: Green (Rác hữu cơ)
    draw.rectangle([768, 0, 1024, 512], fill=(35, 120, 55))
    draw.rectangle([786, 180, 1006, 360], fill=(25, 95, 42), outline=(255, 255, 255), width=3)
    draw_text(draw, 805, 210, "RÁC HỮU CƠ", size=30, fill=(255, 255, 255))
    draw_text(draw, 795, 260, "DỄ PHÂN HỦY", size=24, fill=(255, 240, 160))

    # 3. Utility Pole & Equipment: X: [1024, 1536], Y: [0, 512]
    # Concrete pole grey with yellow/black hazard stripes
    draw.rectangle([1024, 0, 1536, 512], fill=(165, 168, 172))
    # Hazard stripes on bottom
    for i in range(8):
        y0 = i * 40
        draw.polygon([(1024, y0), (1280, y0 + 35), (1280, y0 + 55), (1024, y0 + 20)], fill=(235, 185, 30))
        draw.polygon([(1024, y0 + 20), (1280, y0 + 55), (1280, y0 + 75), (1024, y0 + 40)], fill=(25, 25, 28))
    # Transformer box grey
    draw.rectangle([1300, 100, 1520, 420], fill=(110, 115, 120), outline=(50, 55, 60), width=4)
    draw_text(draw, 1320, 140, "TRẠM BIẾN ÁP", size=24, fill=(235, 195, 45))
    draw_text(draw, 1340, 180, "CẢNH BÁO", size=26, fill=(225, 40, 30))
    draw_text(draw, 1330, 220, "NGUY HIỂM ĐIỆN", size=22, fill=(255, 255, 255))
    # Electric skull / lightning bolt
    draw.polygon([(1400, 280), (1425, 280), (1395, 330), (1415, 330), (1380, 390), (1400, 335), (1385, 335)], fill=(235, 195, 45))

    # 4. Potted Plants Foliage & Ceramic Glaze: X: [1536, 2048], Y: [0, 512]
    # Terracotta ceramic pot red
    draw.rectangle([1536, 0, 1792, 256], fill=(185, 78, 45))
    draw.ellipse([1560, 40, 1768, 216], outline=(235, 210, 180), width=5)
    # Porcelain blue & white
    draw.rectangle([1792, 0, 2048, 256], fill=(245, 245, 250))
    draw.ellipse([1820, 30, 2020, 226], outline=(30, 75, 160), width=6)
    # Tropical foliage leaves green
    draw.rectangle([1536, 256, 2048, 512], fill=(42, 115, 48))
    for i in range(12):
        lx = 1550 + i * 40
        draw.ellipse([(lx, 270), (lx + 60, 490)], fill=(55, 145, 62), outline=(25, 80, 35), width=2)

    # 5. Classic Stone Bench (White/Grey Terrazzo Granito): X: [0, 1024], Y: [512, 1024]
    draw.rectangle([0, 512, 1024, 1024], fill=(225, 228, 230))
    # Granito speckles
    arr_bench = np.array(im.crop([0, 512, 1024, 1024])).astype(int)
    speckles = np.random.randint(-25, 26, (512, 1024, 3))
    im.paste(Image.fromarray(np.clip(arr_bench + speckles, 0, 255).astype(np.uint8)), (0, 512))
    draw = ImageDraw.Draw(im)
    # Sponsorship text on bench backrest: "KÍNH TẶNG PHỐ ĐI BỘ TRÀM CHANH"
    draw.rectangle([120, 680, 904, 820], fill=(210, 215, 218), outline=(140, 145, 150), width=3)
    draw_text(draw, 160, 710, "KÍNH TẶNG BÀ CON KHU PHỐ TRÀM CHANH", size=32, fill=(160, 35, 25))
    draw_text(draw, 270, 760, "XÃ HỘI HÓA ĐÔ THỊ VĂN MINH 2026", size=24, fill=(40, 65, 120))

    # 6. Flat Granite Bench (Dark speckled granite): X: [1024, 2048], Y: [512, 1024]
    draw.rectangle([1024, 512, 2048, 1024], fill=(75, 78, 82))
    arr_granite = np.array(im.crop([1024, 512, 2048, 1024])).astype(int)
    speckles_g = np.random.randint(-30, 31, (512, 1024, 3))
    im.paste(Image.fromarray(np.clip(arr_granite + speckles_g, 0, 255).astype(np.uint8)), (1024, 512))
    draw = ImageDraw.Draw(im)

    # 7. Street Name Signpost "PHỐ TRÀM CHANH": X: [0, 1024], Y: [1024, 1536]
    # Blue background, white border (Vietnamese official street signage format)
    draw.rectangle([0, 1024, 1024, 1536], fill=(25, 75, 145))
    draw.rectangle([40, 1064, 984, 1496], fill=(22, 68, 135), outline=(250, 250, 250), width=10)
    draw.rectangle([60, 1084, 964, 1476], outline=(250, 250, 250), width=3)
    draw_text(draw, 100, 1120, "QUẬN HOÀN KIẾM", size=38, fill=(240, 240, 240))
    draw_text(draw, 100, 1190, "PHỐ TRÀM CHANH", size=76, fill=(255, 255, 255))
    draw_text(draw, 100, 1340, "TUYẾN PHỐ VĂN MINH THƯƠNG MẠI", size=32, fill=(255, 240, 140))

    # 8. Tourist Wayfinding Signpost (2 arrows): X: [1024, 2048], Y: [1024, 1536]
    # Arrow 1: Right (KHU ẨM THỰC DÂN GIAN ->)
    draw.rectangle([1060, 1060, 1980, 1240], fill=(160, 42, 35), outline=(250, 250, 250), width=6)
    draw_text(draw, 1100, 1100, "KHU ẨM THỰC DÂN GIAN  50m", size=36, fill=(255, 255, 255))
    draw_text(draw, 1850, 1090, "➔", size=55, fill=(255, 240, 80))
    # Arrow 2: Left (<- QUẦY BÁNH VÀ TRÀ CHÈ 10m)
    draw.rectangle([1060, 1300, 1980, 1480], fill=(28, 95, 135), outline=(250, 250, 250), width=6)
    draw_text(draw, 1080, 1330, "⬅", size=55, fill=(255, 240, 80))
    draw_text(draw, 1180, 1340, "QUẦY BÁNH TRÀM CHANH 10m", size=36, fill=(255, 255, 255))

    # 9. Metal posts & concrete bases: X: [0, 2048], Y: [1536, 2048]
    draw.rectangle([0, 1536, 1024, 2048], fill=(65, 85, 75)) # dark green metal post
    draw.rectangle([1024, 1536, 2048, 2048], fill=(150, 152, 155)) # concrete foundation footing

    im.save(tex_path)
    print(f"Generated props texture atlas: {tex_path}")

    # Ensure texture meta
    meta_p = tex_path + ".meta"
    if not os.path.exists(meta_p):
        with open(meta_p, "w") as f:
            f.write(f"""fileFormatVersion: 2
guid: {generate_guid()}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 1
    sRGBTexture: 1
  textureType: 0
  textureShape: 1
  maxTextureSize: 2048
""")

    # Wire atlas into MAT_Street_RoadsideProps.mat
    mat_path = "Assets/TramChanh/Art/Materials/MAT_Street_RoadsideProps.mat"
    meta_tex_guid = ""
    with open(meta_p) as f:
        for line in f:
            if "guid:" in line:
                meta_tex_guid = line.strip().split()[1]
                break

    with open(mat_path, "r") as f:
        mat_txt = f.read()
    
    # Replace texture reference
    mat_txt_new = mat_txt.replace(
        "    - _BaseMap:\n        m_Texture: {fileID: 0}",
        f"    - _BaseMap:\n        m_Texture: {{fileID: 2800000, guid: {meta_tex_guid}, type: 3}}"
    )
    with open(mat_path, "w") as f:
        f.write(mat_txt_new)
    print(f"Assigned props atlas to {mat_path}")

# ==============================================================================
# 2. 3D MODELS GENERATION (10 MODELS)
# ==============================================================================

class ObjMesh:
    def __init__(self, name):
        self.name = name
        self.vertices = []
        self.uvs = []
        self.faces = []

    def add_vert(self, x, y, z):
        self.vertices.append((x, y, z))
        return len(self.vertices)

    def add_uv(self, u, v):
        self.uvs.append((u, v))
        return len(self.uvs)

    def add_box(self, x1, x2, y1, y2, z1, z2, u_box=(0.0, 1.0), v_box=(0.0, 1.0)):
        v = [
            self.add_vert(x1, y1, z1), self.add_vert(x2, y1, z1),
            self.add_vert(x2, y2, z1), self.add_vert(x1, y2, z1),
            self.add_vert(x1, y1, z2), self.add_vert(x2, y1, z2),
            self.add_vert(x2, y2, z2), self.add_vert(x1, y2, z2)
        ]
        u1, u2 = u_box
        v1, v2 = v_box
        t0 = self.add_uv(u1, v1)
        t1 = self.add_uv(u2, v1)
        t2 = self.add_uv(u2, v2)
        t3 = self.add_uv(u1, v2)

        self.faces.append(f"f {v[0]}/{t0} {v[1]}/{t1} {v[2]}/{t2} {v[3]}/{t3}")
        self.faces.append(f"f {v[5]}/{t0} {v[4]}/{t1} {v[7]}/{t2} {v[6]}/{t3}")
        self.faces.append(f"f {v[4]}/{t0} {v[0]}/{t1} {v[3]}/{t2} {v[7]}/{t3}")
        self.faces.append(f"f {v[1]}/{t0} {v[5]}/{t1} {v[6]}/{t2} {v[2]}/{t3}")
        self.faces.append(f"f {v[3]}/{t0} {v[2]}/{t1} {v[6]}/{t2} {v[7]}/{t3}")
        self.faces.append(f"f {v[4]}/{t0} {v[5]}/{t1} {v[1]}/{t2} {v[0]}/{t3}")

    def add_cylinder(self, cx, cz, y_bot, y_top, r, segments=8, u_box=(0.0, 1.0), v_box=(0.0, 1.0)):
        u1, u2 = u_box
        v1, v2 = v_box
        pts_bot = []
        pts_top = []
        uv_bot = []
        uv_top = []
        for i in range(segments):
            ang = 2.0 * math.pi * i / segments
            x = cx + math.cos(ang) * r
            z = cz + math.sin(ang) * r
            pts_bot.append(self.add_vert(x, y_bot, z))
            pts_top.append(self.add_vert(x, y_top, z))
            u_frac = u1 + (u2 - u1) * (i / segments)
            uv_bot.append(self.add_uv(u_frac, v1))
            uv_top.append(self.add_uv(u_frac, v2))

        c_top = self.add_vert(cx, y_top, cz)
        t_top = self.add_uv((u1 + u2)*0.5, v2)

        for i in range(segments):
            nxt = (i + 1) % segments
            self.faces.append(f"f {pts_bot[i]}/{uv_bot[i]} {pts_bot[nxt]}/{uv_bot[nxt]} {pts_top[nxt]}/{uv_top[nxt]} {pts_top[i]}/{uv_top[i]}")
            self.faces.append(f"f {c_top}/{t_top} {pts_top[nxt]}/{uv_top[nxt]} {pts_top[i]}/{uv_top[i]}")

    def write(self, path):
        with open(path, "w") as f:
            f.write(f"# {self.name}.obj - Procedural CC0\nusemtl MAT_Street_RoadsideProps\no {self.name}\n")
            for v in self.vertices: f.write(f"v {v[0]:.4f} {v[1]:.4f} {v[2]:.4f}\n")
            for t in self.uvs: f.write(f"vt {t[0]:.4f} {t[1]:.4f}\n")
            for fc in self.faces: f.write(fc + "\n")
        
        meta = path + ".meta"
        if not os.path.exists(meta):
            with open(meta, "w") as f:
                f.write(f"""fileFormatVersion: 2
guid: {generate_guid()}
ModelImporter:
  serializedVersion: 21300
  internalIDToNameTable: []
  externalObjects: {{}}
  materials:
    materialImportMode: 1
    materialName: 0
    materialSearch: 1
  globalScale: 1
  addCollider: 0
""")
        print(f"Generated {path} ({len(self.faces)} faces)")

def generate_all_models():
    m_dir = "Assets/TramChanh/Art/Models/Environment"

    # UV regions
    uv_bin1 = (0.0, 0.25), (0.75, 1.0)
    uv_bin2 = (0.25, 0.50), (0.75, 1.0)
    uv_pole = (0.50, 0.75), (0.75, 1.0)
    uv_trans = (0.64, 0.75), (0.75, 1.0)
    uv_plant1 = (0.75, 0.88), (0.75, 1.0)
    uv_plant2 = (0.88, 1.00), (0.75, 1.0)
    uv_foliage = (0.75, 1.00), (0.75, 0.88)
    uv_bench1 = (0.0, 0.50), (0.50, 0.75)
    uv_bench2 = (0.50, 1.00), (0.50, 0.75)
    uv_sign1 = (0.0, 0.50), (0.25, 0.50)
    uv_sign2 = (0.50, 1.00), (0.25, 0.50)
    uv_metal = (0.0, 0.50), (0.0, 0.25)

    # 1. SM_TrashBin_Municipal.obj (w=0.50, h=0.95, d=0.55)
    m = ObjMesh("SM_TrashBin_Municipal")
    m.add_box(-0.25, 0.25, 0.10, 0.88, -0.25, 0.25, uv_bin1[0], uv_bin1[1]) # Bin body
    m.add_box(-0.27, 0.27, 0.88, 0.95, -0.27, 0.27, uv_bin1[0], uv_bin1[1]) # Slanted lid
    # 2 Wheels at bottom rear
    m.add_cylinder(-0.28, -0.15, 0.0, 0.08, 0.08, segments=8, u_box=uv_metal[0], v_box=uv_metal[1])
    m.add_cylinder(0.28, -0.15, 0.0, 0.08, 0.08, segments=8, u_box=uv_metal[0], v_box=uv_metal[1])
    m.write(os.path.join(m_dir, "SM_TrashBin_Municipal.obj"))

    # 2. SM_TrashBin_Dual.obj (w=0.85, h=0.95, d=0.40)
    m = ObjMesh("SM_TrashBin_Dual")
    m.add_box(-0.40, -0.02, 0.15, 0.92, -0.18, 0.18, (0.25, 0.37), (0.75, 1.0)) # Orange bin (Inorganic)
    m.add_box(0.02, 0.40, 0.15, 0.92, -0.18, 0.18, (0.37, 0.50), (0.75, 1.0))  # Green bin (Organic)
    m.add_box(-0.44, 0.44, 0.92, 0.96, -0.20, 0.20, uv_metal[0], uv_metal[1])   # Stainless hood
    # Stainless base stand
    m.add_box(-0.35, 0.35, 0.00, 0.15, -0.15, 0.15, uv_metal[0], uv_metal[1])
    m.write(os.path.join(m_dir, "SM_TrashBin_Dual.obj"))

    # 3. SM_UtilityPole_Cables.obj (Round concrete pole, h=8.5m, with crossarms & cable bundle)
    m = ObjMesh("SM_UtilityPole_Cables")
    m.add_cylinder(0.0, 0.0, 0.0, 8.5, 0.18, segments=10, u_box=uv_pole[0], v_box=uv_pole[1])
    # Crossarm T-bar at y = 7.8
    m.add_box(-1.0, 1.0, 7.7, 7.85, -0.08, 0.08, uv_metal[0], uv_metal[1])
    # Cable coil drum at y = 4.2
    m.add_cylinder(0.0, 0.0, 4.0, 4.5, 0.35, segments=8, u_box=uv_metal[0], v_box=uv_metal[1])
    m.write(os.path.join(m_dir, "SM_UtilityPole_Cables.obj"))

    # 4. SM_UtilityPole_Transformer.obj (Square concrete pole with metal transformer & speaker)
    m = ObjMesh("SM_UtilityPole_Transformer")
    m.add_box(-0.16, 0.16, 0.0, 8.5, -0.16, 0.16, uv_pole[0], uv_pole[1])
    # Transformer box at y: 4.8 to 6.2
    m.add_box(-0.45, 0.45, 4.8, 6.2, 0.16, 0.75, uv_trans[0], uv_trans[1])
    # Public loudspeaker horn at y = 7.5
    m.add_box(-0.30, 0.30, 7.4, 7.8, -0.60, -0.16, uv_metal[0], uv_metal[1])
    m.write(os.path.join(m_dir, "SM_UtilityPole_Transformer.obj"))

    # 5. SM_PottedPlant_Ceramic.obj (Glazed round ceramic pot with bonsai/bougainvillea)
    m = ObjMesh("SM_PottedPlant_Ceramic")
    m.add_cylinder(0.0, 0.0, 0.0, 0.55, 0.32, segments=10, u_box=uv_plant1[0], v_box=uv_plant1[1])
    # Plant foliage canopy
    m.add_box(-0.35, 0.35, 0.55, 1.25, -0.35, 0.35, uv_foliage[0], uv_foliage[1])
    m.write(os.path.join(m_dir, "SM_PottedPlant_Ceramic.obj"))

    # 6. SM_PottedPlant_Square.obj (Modern square terrazzo planter with foliage)
    m = ObjMesh("SM_PottedPlant_Square")
    m.add_box(-0.25, 0.25, 0.0, 0.60, -0.25, 0.25, uv_plant2[0], uv_plant2[1])
    m.add_box(-0.32, 0.32, 0.60, 1.40, -0.32, 0.32, uv_foliage[0], uv_foliage[1])
    m.write(os.path.join(m_dir, "SM_PottedPlant_Square.obj"))

    # 7. SM_StoneBench_Classic.obj (Classic Vietnamese stone park bench with backrest)
    m = ObjMesh("SM_StoneBench_Classic")
    # Seat slab (w=1.40, d=0.45, y=0.42)
    m.add_box(-0.70, 0.70, 0.38, 0.45, -0.22, 0.22, uv_bench1[0], uv_bench1[1])
    # Backrest slab (w=1.40, h=0.40, y: 0.45..0.85, z: -0.22)
    m.add_box(-0.70, 0.70, 0.45, 0.85, -0.24, -0.16, uv_bench1[0], uv_bench1[1])
    # 2 Concrete leg blocks
    m.add_box(-0.55, -0.42, 0.0, 0.38, -0.18, 0.18, uv_bench1[0], uv_bench1[1])
    m.add_box(0.42, 0.55, 0.0, 0.38, -0.18, 0.18, uv_bench1[0], uv_bench1[1])
    m.write(os.path.join(m_dir, "SM_StoneBench_Classic.obj"))

    # 8. SM_StoneBench_Flat.obj (Flat polished granite bench)
    m = ObjMesh("SM_StoneBench_Flat")
    m.add_box(-0.75, 0.75, 0.38, 0.46, -0.25, 0.25, uv_bench2[0], uv_bench2[1])
    m.add_box(-0.60, -0.45, 0.0, 0.38, -0.20, 0.20, uv_bench2[0], uv_bench2[1])
    m.add_box(0.45, 0.60, 0.0, 0.38, -0.20, 0.20, uv_bench2[0], uv_bench2[1])
    m.write(os.path.join(m_dir, "SM_StoneBench_Flat.obj"))

    # 9. SM_Signpost_StreetName.obj (Official Hanoi blue street name sign "PHỐ TRÀM CHANH")
    m = ObjMesh("SM_Signpost_StreetName")
    m.add_cylinder(0.0, 0.0, 0.0, 2.7, 0.045, segments=8, u_box=uv_metal[0], v_box=uv_metal[1]) # Steel pole
    # Street sign plaque (w=0.65, h=0.30, mounted at y = 2.45)
    m.add_box(-0.35, 0.35, 2.30, 2.65, -0.02, 0.02, uv_sign1[0], uv_sign1[1])
    m.write(os.path.join(m_dir, "SM_Signpost_StreetName.obj"))

    # 10. SM_Signpost_Wayfinding.obj (Tourist wayfinding signpost with 2 directional arrows)
    m = ObjMesh("SM_Signpost_Wayfinding")
    m.add_cylinder(0.0, 0.0, 0.0, 2.8, 0.045, segments=8, u_box=uv_metal[0], v_box=uv_metal[1])
    # Top arrow (Food zone ->)
    m.add_box(-0.45, 0.45, 2.40, 2.65, -0.02, 0.02, uv_sign2[0], (0.37, 0.50))
    # Bottom arrow (<- Tram Chanh Stall)
    m.add_box(-0.45, 0.45, 2.10, 2.35, -0.02, 0.02, uv_sign2[0], (0.25, 0.37))
    m.write(os.path.join(m_dir, "SM_Signpost_Wayfinding.obj"))

# ==============================================================================
# 3. PREFABS GENERATION (10 PREFABS)
# ==============================================================================

def generate_all_prefabs():
    pf_dir = "Assets/TramChanh/Prefabs/Environment"
    mat_guid = ""
    with open("Assets/TramChanh/Art/Materials/MAT_Street_RoadsideProps.mat.meta") as f:
        for line in f:
            if "guid:" in line:
                mat_guid = line.strip().split()[1]
                break

    prop_configs = {
        "PF_TrashBin_Municipal": {
            "model": "SM_TrashBin_Municipal", "size": (0.55, 0.95, 0.55), "center": (0, 0.475, 0)
        },
        "PF_TrashBin_Dual": {
            "model": "SM_TrashBin_Dual", "size": (0.90, 0.96, 0.45), "center": (0, 0.48, 0)
        },
        "PF_UtilityPole_Cables": {
            "model": "SM_UtilityPole_Cables", "size": (2.0, 8.5, 0.4), "center": (0, 4.25, 0)
        },
        "PF_UtilityPole_Transformer": {
            "model": "SM_UtilityPole_Transformer", "size": (0.9, 8.5, 0.9), "center": (0, 4.25, 0.25)
        },
        "PF_PottedPlant_Ceramic": {
            "model": "SM_PottedPlant_Ceramic", "size": (0.7, 1.25, 0.7), "center": (0, 0.625, 0)
        },
        "PF_PottedPlant_Square": {
            "model": "SM_PottedPlant_Square", "size": (0.65, 1.40, 0.65), "center": (0, 0.70, 0)
        },
        "PF_StoneBench_Classic": {
            "model": "SM_StoneBench_Classic", "size": (1.45, 0.85, 0.55), "center": (0, 0.425, 0)
        },
        "PF_StoneBench_Flat": {
            "model": "SM_StoneBench_Flat", "size": (1.55, 0.46, 0.55), "center": (0, 0.23, 0)
        },
        "PF_Signpost_StreetName": {
            "model": "SM_Signpost_StreetName", "size": (0.75, 2.7, 0.15), "center": (0, 1.35, 0)
        },
        "PF_Signpost_Wayfinding": {
            "model": "SM_Signpost_Wayfinding", "size": (0.95, 2.8, 0.15), "center": (0, 1.40, 0)
        },
    }

    for pf_name, cfg in prop_configs.items():
        pf_path = os.path.join(pf_dir, f"{pf_name}.prefab")
        meta_path = pf_path + ".meta"

        # Model GUID
        mesh_meta = f"Assets/TramChanh/Art/Models/Environment/{cfg['model']}.obj.meta"
        mesh_guid = ""
        with open(mesh_meta) as f:
            for line in f:
                if "guid:" in line:
                    mesh_guid = line.strip().split()[1]
                    break

        if not os.path.exists(meta_path):
            with open(meta_path, "w") as f:
                f.write(f"""fileFormatVersion: 2
guid: {generate_guid()}
PrefabImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")

        w, h, d = cfg["size"]
        cx, cy, cz = cfg["center"]

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
  m_Name: {pf_name}
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
  m_Mesh: {{fileID: -2432090755550338912, guid: {mesh_guid}, type: 3}}
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
  m_Size: {{x: {w:.2f}, y: {h:.2f}, z: {d:.2f}}}
  m_Center: {{x: {cx:.2f}, y: {cy:.2f}, z: {cz:.2f}}}
""")
        print(f"Generated prefab: {pf_path}")

def main():
    print("=== GENERATING STREET PROPS (5 CATEGORIES, 2 VARIANTS EACH) ===")
    generate_props_atlas()
    generate_all_models()
    generate_all_prefabs()
    print("=== ALL STREET PROPS GENERATED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
