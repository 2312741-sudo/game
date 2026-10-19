#!/usr/bin/env python3
"""
ArtSource/Scripts/generate_shopfronts_extended.py

Generates 3 new Vietnamese shopfront styles (D, E, F) and 6 commercial signboards
with 100% correct Vietnamese diacritics for ART-FIX-022 Task 4:
- Shopfront D: Quán Phở Bò Gia Truyền Hà Nội (French colonial 3-storey)
- Shopfront E: Quán Cà Phê Muối Xứ Huế & Trà Cổ (Hoi An ochre 4-storey)
- Shopfront F: Tiệm Cắt Tóc Nam Bình Dân & Gội Đầu (Vintage shophouse 3-storey)
"""

import os
import uuid
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
# 1. TEXTURES GENERATION
# ==============================================================================

def generate_textures():
    tex_dir = "Assets/TramChanh/Art/Textures"
    os.makedirs(tex_dir, exist_ok=True)

    # --------------------------------------------------------------------------
    # Shopfront D: Phở Bò Gia Truyền Hà Nội (Warm vintage colonial yellow plaster)
    # --------------------------------------------------------------------------
    sfD = Image.new("RGB", (2048, 2048), (235, 205, 135))
    arrD = np.array(sfD).astype(int) + np.random.randint(-12, 13, (2048, 2048, 3))
    sfD = Image.fromarray(np.clip(arrD, 0, 255).astype(np.uint8))
    drawD = ImageDraw.Draw(sfD)

    # Upper floor windows with dark green wooden shutters (French colonial)
    for wy in [640, 180]:
        # Clay tile window header
        drawD.rectangle([(280, wy - 30), (840, wy)], fill=(185, 75, 40), outline=(130, 50, 25), width=2)
        drawD.rectangle([(1200, wy - 30), (1760, wy)], fill=(185, 75, 40), outline=(130, 50, 25), width=2)
        # Wooden shutters
        drawD.rectangle([(300, wy), (820, wy + 320)], fill=(30, 75, 50), outline=(18, 45, 30), width=6)
        for ly in range(wy + 15, wy + 310, 16):
            drawD.line([(320, ly), (800, ly)], fill=(22, 60, 40), width=3)
        drawD.rectangle([(1220, wy), (1740, wy + 320)], fill=(30, 75, 50), outline=(18, 45, 30), width=6)
        for ly in range(wy + 15, wy + 310, 16):
            drawD.line([(1240, ly), (1720, ly)], fill=(22, 60, 40), width=3)

    # Commercial Signboard 4: "PHỞ BÒ GIA TRUYỀN HÀ NỘI" (Y: 1075..1265)
    drawD.rectangle([(80, 1075), (1968, 1265)], fill=(180, 30, 20), outline=(245, 215, 50), width=10)
    drawD.rectangle([(100, 1090), (1948, 1250)], fill=(195, 32, 22))
    drawD.rectangle([(115, 1102), (1933, 1238)], outline=(255, 235, 80), width=4)

    draw_text(drawD, 140, 1110, "ẨM THỰC HÀ NỘI XƯA - GIA TRUYỀN TỪ 1986", size=28, fill=(255, 240, 120))
    draw_text(drawD, 140, 1145, "PHỞ BÒ GIA TRUYỀN", size=68, fill=(255, 255, 255))
    draw_text(drawD, 140, 1215, "TÁI • CHÍN • NẠM • GẦU • SỐT VANG • QUẨY GIÒN • MỞ CỬA: 06:00 - 23:00", size=24, fill=(255, 245, 180))
    
    # Stamp on right
    drawD.ellipse([(1800, 1115), (1915, 1225)], fill=(245, 215, 50), outline=(255, 255, 255), width=3)
    draw_text(drawD, 1820, 1150, "Phở", size=36, fill=(180, 30, 20))

    # Striped Red & Beige Canvas Awning (Y: 1270..1380)
    for ax in range(60, 1988, 64):
        col = (185, 35, 25) if ((ax - 60) // 64) % 2 == 0 else (240, 230, 205)
        drawD.rectangle([(ax, 1270), (min(ax+64, 1988), 1365)], fill=col)
        drawD.pieslice([(ax, 1345), (min(ax+64, 1988), 1385)], 0, 180, fill=col)

    # Ground floor: Traditional folding wooden panel doors (Y: 1390..2048)
    drawD.rectangle([(140, 1390), (1908, 2048)], fill=(75, 45, 25))
    for px in range(140, 1908, 88):
        drawD.rectangle([(px, 1400), (px + 82, 2040)], fill=(95, 58, 32), outline=(50, 30, 15), width=4)
        drawD.rectangle([(px + 12, 1420), (px + 70, 1700)], fill=(75, 45, 25), outline=(40, 25, 12), width=3)
        drawD.rectangle([(px + 12, 1720), (px + 70, 2020)], fill=(75, 45, 25), outline=(40, 25, 12), width=3)

    texD_path = os.path.join(tex_dir, "T_Shopfront_D_BaseColor.png")
    sfD.save(texD_path)
    print(f"Generated texture: {texD_path}")

    # --------------------------------------------------------------------------
    # Shopfront E: Cà Phê Muối Xứ Huế & Trà Cổ (Hoi An golden ochre plaster)
    # --------------------------------------------------------------------------
    sfE = Image.new("RGB", (2048, 2048), (228, 178, 55))
    arrE = np.array(sfE).astype(int) + np.random.randint(-10, 11, (2048, 2048, 3))
    sfE = Image.fromarray(np.clip(arrE, 0, 255).astype(np.uint8))
    drawE = ImageDraw.Draw(sfE)

    # Upper floor French arched balcony with wooden balustrades & bougainvillea
    for wy in [650, 200]:
        # Arched windows
        drawE.rectangle([(300, wy + 80), (820, wy + 320)], fill=(45, 30, 20), outline=(25, 15, 10), width=6)
        drawE.pieslice([(300, wy), (820, wy + 160)], 180, 360, fill=(45, 30, 20), outline=(25, 15, 10), width=6)
        drawE.rectangle([(1220, wy + 80), (1740, wy + 320)], fill=(45, 30, 20), outline=(25, 15, 10), width=6)
        drawE.pieslice([(1220, wy), (1740, wy + 160)], 180, 360, fill=(45, 30, 20), outline=(25, 15, 10), width=6)
        
        # Hanging Red Lanterns
        for lx in [220, 900, 1140, 1820]:
            drawE.line([(lx, wy - 40), (lx, wy + 40)], fill=(20, 20, 20), width=3)
            drawE.ellipse([(lx - 35, wy + 40), (lx + 35, wy + 120)], fill=(215, 30, 25), outline=(245, 215, 50), width=3)
            # Gold tassel
            drawE.line([(lx, wy + 120), (lx, wy + 160)], fill=(245, 215, 50), width=4)

    # Commercial Signboard 5: "CÀ PHÊ MUỐI XỨ HUẾ" (Y: 1060..1265)
    drawE.rectangle([(80, 1060), (1968, 1265)], fill=(55, 35, 25), outline=(235, 195, 65), width=10)
    drawE.rectangle([(100, 1075), (1948, 1250)], fill=(42, 26, 18))
    drawE.rectangle([(115, 1088), (1933, 1237)], outline=(225, 185, 55), width=4)

    draw_text(drawE, 140, 1098, "ĐẬM ĐÀ HƯƠNG VỊ CỐ ĐÔ - RANG XAY MỘC", size=28, fill=(245, 215, 110))
    draw_text(drawE, 140, 1135, "CÀ PHÊ MUỐI XỨ HUẾ", size=68, fill=(255, 240, 200))
    draw_text(drawE, 140, 1210, "CÀ PHÊ PHIN • TRÀ HOA CÚC • TRÀ CUNG ĐÌNH • BÁNH ĐẬU XANH • ĐT: 0988.333.666", size=24, fill=(235, 205, 150))

    # Ceramic coffee cup stamp on right
    drawE.ellipse([(1800, 1105), (1915, 1215)], fill=(235, 195, 65), outline=(255, 255, 255), width=3)
    draw_text(drawE, 1825, 1140, "Huế", size=36, fill=(42, 26, 18))

    # Ground floor: Rustic dark wood frame with glass entry (Y: 1390..2048)
    drawE.rectangle([(140, 1390), (1908, 2048)], fill=(60, 38, 22))
    drawE.rectangle([(220, 1430), (950, 1980)], fill=(35, 55, 70), outline=(85, 55, 30), width=8)
    drawE.rectangle([(1090, 1430), (1820, 1980)], fill=(35, 55, 70), outline=(85, 55, 30), width=8)

    texE_path = os.path.join(tex_dir, "T_Shopfront_E_BaseColor.png")
    sfE.save(texE_path)
    print(f"Generated texture: {texE_path}")

    # --------------------------------------------------------------------------
    # Shopfront F: Tiệm Cắt Tóc Nam Bình Dân & Gội Đầu (Vintage mint green / teal)
    # --------------------------------------------------------------------------
    sfF = Image.new("RGB", (2048, 2048), (175, 215, 205))
    arrF = np.array(sfF).astype(int) + np.random.randint(-10, 11, (2048, 2048, 3))
    sfF = Image.fromarray(np.clip(arrF, 0, 255).astype(np.uint8))
    drawF = ImageDraw.Draw(sfF)

    # Modern steel/aluminum framed windows
    for wy in [640, 180]:
        drawF.rectangle([(280, wy), (860, wy + 320)], fill=(40, 45, 50), outline=(20, 25, 30), width=6)
        drawF.rectangle([(310, wy + 20), (830, wy + 300)], fill=(125, 170, 195))
        drawF.rectangle([(1180, wy), (1760, wy + 320)], fill=(40, 45, 50), outline=(20, 25, 30), width=6)
        drawF.rectangle([(1210, wy + 20), (1730, wy + 300)], fill=(125, 170, 195))

    # Commercial Signboard 6: "TIỆM CẮT TÓC BÌNH DÂN" (Y: 1075..1265)
    drawF.rectangle([(80, 1075), (1968, 1265)], fill=(20, 75, 140), outline=(245, 215, 45), width=10)
    drawF.rectangle([(100, 1090), (1948, 1250)], fill=(16, 62, 118))
    drawF.rectangle([(115, 1102), (1933, 1238)], outline=(255, 255, 255), width=4)

    draw_text(drawF, 140, 1110, "CHUYÊN CẮT TÓC NAM • TẠO KIỂU • GỘI ĐẦU DƯỠNG SINH", size=26, fill=(245, 225, 100))
    draw_text(drawF, 140, 1145, "TIỆM CẮT TÓC BÌNH DÂN", size=66, fill=(255, 255, 255))
    draw_text(drawF, 140, 1215, "CẮT TÓC • CẠO MẶT • RÁY TAI • GIÁ CHỈ TỪ 40.000 Đ • PHỤC VỤ CHU ĐÁO", size=24, fill=(210, 235, 255))

    # Barber pole cylinder symbol on right
    drawF.rectangle([(1810, 1105), (1895, 1235)], fill=(255, 255, 255), outline=(30, 30, 30), width=3)
    for i in range(5):
        y0 = 1105 + i * 26
        drawF.polygon([(1810, y0), (1895, y0 + 20), (1895, y0 + 32), (1810, y0 + 12)], fill=(215, 30, 25))
        drawF.polygon([(1810, y0 + 12), (1895, y0 + 32), (1895, y0 + 44), (1810, y0 + 24)], fill=(25, 75, 160))

    # Ground floor: Rolling shutter (Y: 1390..2048)
    drawF.rectangle([(140, 1390), (1908, 2048)], fill=(155, 160, 165))
    for ry in range(1390, 2048, 22):
        drawF.line([(140, ry), (1908, ry)], fill=(110, 115, 120), width=4)
        drawF.line([(140, ry + 5), (1908, ry + 5)], fill=(185, 190, 195), width=2)

    texF_path = os.path.join(tex_dir, "T_Shopfront_F_BaseColor.png")
    sfF.save(texF_path)
    print(f"Generated texture: {texF_path}")

    # Ensure Texture .meta files exist
    for p in [texD_path, texE_path, texF_path]:
        meta_p = p + ".meta"
        if not os.path.exists(meta_p):
            with open(meta_p, "w") as f:
                f.write(f"""fileFormatVersion: 2
guid: {generate_guid()}
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
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
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
  alphaIsTransparency: 0
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbook: 0
  ignorePngGamma: 0
  cookieLightType: 0
""")

# ==============================================================================
# 2. MATERIALS GENERATION
# ==============================================================================

def generate_materials():
    mat_dir = "Assets/TramChanh/Art/Materials"
    os.makedirs(mat_dir, exist_ok=True)

    tex_guids = {}
    for letter in ["D", "E", "F"]:
        meta_p = f"Assets/TramChanh/Art/Textures/T_Shopfront_{letter}_BaseColor.png.meta"
        with open(meta_p) as f:
            for line in f:
                if "guid:" in line:
                    tex_guids[letter] = line.strip().split()[1]
                    break

    for letter in ["D", "E", "F"]:
        mat_path = os.path.join(mat_dir, f"MAT_Shopfront_{letter}.mat")
        meta_path = mat_path + ".meta"
        
        if not os.path.exists(meta_path):
            with open(meta_path, "w") as f:
                f.write(f"""fileFormatVersion: 2
guid: {generate_guid()}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 2100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")

        with open(mat_path, "w") as f:
            f.write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &-5677987210349623388
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
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: MAT_Shopfront_{letter}
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
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
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {tex_guids[letter]}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _Smoothness: 0.35
    - _Metallic: 0.05
    m_Colors:
    - _BaseColor: {{r: 1, g: 1, b: 1, a: 1}}
""")
        print(f"Generated material: {mat_path}")

# ==============================================================================
# 3. 3D MODELS (OBJ) GENERATION
# ==============================================================================

class ObjMesh:
    def __init__(self, name):
        self.name = name
        self.vertices = []
        self.uvs = []
        self.faces = []

    def add_vertex(self, x, y, z):
        self.vertices.append((float(x), float(y), float(z)))
        return len(self.vertices)

    def add_uv(self, u, v):
        self.uvs.append((float(u), float(v)))
        return len(self.uvs)

    def add_box(self, x1, x2, y1, y2, z1, z2, u_range=(0.0, 1.0), v_range=(0.0, 1.0)):
        if x1 > x2: x1, x2 = x2, x1
        if y1 > y2: y1, y2 = y2, y1
        if z1 > z2: z1, z2 = z2, z1
        
        base_v = len(self.vertices)
        v = [
            self.add_vertex(x1, y1, z1), # 0
            self.add_vertex(x2, y1, z1), # 1
            self.add_vertex(x2, y2, z1), # 2
            self.add_vertex(x1, y2, z1), # 3
            self.add_vertex(x1, y1, z2), # 4
            self.add_vertex(x2, y1, z2), # 5
            self.add_vertex(x2, y2, z2), # 6
            self.add_vertex(x1, y2, z2), # 7
        ]
        
        u1, u2 = u_range
        v1, v2 = v_range
        base_t = len(self.uvs)
        t_front = [
            self.add_uv(u1, v1),
            self.add_uv(u2, v1),
            self.add_uv(u2, v2),
            self.add_uv(u1, v2),
        ]
        t_side = [
            self.add_uv(0.01, 0.01),
            self.add_uv(0.09, 0.01),
            self.add_uv(0.09, 0.09),
            self.add_uv(0.01, 0.09),
        ]

        # Front (-Z, street facing): v0 -> v3 -> v2 -> v1
        self.faces.append(f"f {v[0]}/{t_front[0]} {v[3]}/{t_front[3]} {v[2]}/{t_front[2]} {v[1]}/{t_front[1]}")
        # Back (+Z): v5 -> v6 -> v7 -> v4
        self.faces.append(f"f {v[5]}/{t_side[0]} {v[6]}/{t_side[1]} {v[7]}/{t_side[2]} {v[4]}/{t_side[3]}")
        # Left (-X): v0 -> v4 -> v7 -> v3
        self.faces.append(f"f {v[0]}/{t_side[0]} {v[4]}/{t_side[1]} {v[7]}/{t_side[2]} {v[3]}/{t_side[3]}")
        # Right (+X): v1 -> v2 -> v6 -> v5
        self.faces.append(f"f {v[1]}/{t_side[0]} {v[2]}/{t_side[1]} {v[6]}/{t_side[2]} {v[5]}/{t_side[3]}")
        # Bottom (-Y): v0 -> v1 -> v5 -> v4
        self.faces.append(f"f {v[0]}/{t_side[0]} {v[1]}/{t_side[1]} {v[5]}/{t_side[2]} {v[4]}/{t_side[3]}")
        # Top (+Y): v3 -> v7 -> v6 -> v2
        self.faces.append(f"f {v[3]}/{t_side[0]} {v[7]}/{t_side[1]} {v[6]}/{t_side[2]} {v[2]}/{t_side[3]}")

    def write_obj(self, out_path):
        with open(out_path, "w") as f:
            f.write(f"# Wavefront OBJ: {self.name}\n")
            f.write(f"o {self.name}\n")
            for x, y, z in self.vertices:
                f.write(f"v {x:.4f} {y:.4f} {z:.4f}\n")
            for u, v in self.uvs:
                f.write(f"vt {u:.4f} {v:.4f}\n")
            for face in self.faces:
                f.write(face + "\n")
        print(f"Generated model: {out_path} ({len(self.faces)} faces)")

def generate_models():
    model_dir = "Assets/TramChanh/Art/Models/Environment"
    os.makedirs(model_dir, exist_ok=True)

    configs = {
        "SM_Shopfront_A": [
            (-2.000, 2.000, -0.350, 11.000, 0.000, 4.500),
            (-2.150, 2.150, 3.100, 3.400, -1.100, -0.010),
            (-1.750, 1.750, 6.800, 7.100, -0.600, -0.010),
            (-2.050, 2.050, 11.010, 11.450, -0.050, 4.550)
        ],
        "SM_Shopfront_B": [
            (-2.400, 2.400, -0.350, 14.800, 0.000, 4.500),
            (-2.450, 2.450, 3.500, 4.300, -0.250, -0.010),
            (-2.100, 2.100, 7.500, 7.800, -0.500, -0.010),
            (-2.450, 2.450, 14.810, 15.350, -0.050, 4.550)
        ],
        "SM_Shopfront_C": [
            (-2.000, 2.000, -0.350, 10.500, 0.000, 4.500),
            (-2.100, 2.100, 3.200, 3.500, -0.800, -0.010),
            (-1.900, 1.900, 3.600, 4.300, -0.200, -0.010),
            (-2.050, 2.050, 10.510, 11.000, -0.050, 4.550)
        ],
        "SM_Shopfront_D": [
            (-2.200, 2.200, -0.350, 12.000, 0.000, 4.500),
            (-2.250, 2.250, 3.000, 3.300, -1.000, -0.010),
            (-2.100, 2.100, 3.400, 4.200, -0.250, -0.010),
            (-1.800, 1.800, 5.200, 5.500, -0.700, -0.010),
            (-2.250, 2.250, 12.010, 12.600, -0.050, 4.550)
        ],
        "SM_Shopfront_E": [
            (-2.400, 2.400, -0.350, 14.700, 0.000, 4.500),
            (-2.450, 2.450, 3.200, 3.500, -0.900, -0.010),
            (-2.100, 2.100, 5.600, 5.900, -0.650, -0.010),
            (-2.050, 2.050, 9.200, 9.500, -0.650, -0.010),
            (-2.500, 2.500, 14.710, 15.300, -0.150, 4.650)
        ],
        "SM_Shopfront_F": [
            (-2.000, 2.000, -0.350, 11.600, 0.000, 4.500),
            (-1.950, 1.950, 3.300, 4.100, -0.200, -0.010),
            (-1.750, 1.750, 5.800, 6.100, -0.550, -0.010),
            (-2.050, 2.050, 11.610, 12.200, -0.050, 4.550)
        ]
    }

    for name, boxes in configs.items():
        m = BoxMesh(name)
        for b in boxes:
            m.add_box(*b)
        out_p = os.path.join(model_dir, f"{name}.obj")
        m.write_obj(out_p)

# ==============================================================================
# 4. PREFABS GENERATION
# ==============================================================================

def generate_prefabs():
    prefab_dir = "Assets/TramChanh/Prefabs/Environment"
    os.makedirs(prefab_dir, exist_ok=True)

    mesh_guids = {}
    mat_guids = {}

    for letter in ["D", "E", "F"]:
        mesh_meta = f"Assets/TramChanh/Art/Models/Environment/SM_Shopfront_{letter}.obj.meta"
        with open(mesh_meta) as f:
            for line in f:
                if "guid:" in line:
                    mesh_guids[letter] = line.strip().split()[1]
                    break

        mat_meta = f"Assets/TramChanh/Art/Materials/MAT_Shopfront_{letter}.mat.meta"
        with open(mat_meta) as f:
            for line in f:
                if "guid:" in line:
                    mat_guids[letter] = line.strip().split()[1]
                    break

    configs = {
        "D": {"w": 4.5, "h": 11.5, "d": 4.5},
        "E": {"w": 5.0, "h": 14.0, "d": 4.5},
        "F": {"w": 4.2, "h": 11.0, "d": 4.5},
    }

    for letter, cfg in configs.items():
        pf_path = os.path.join(prefab_dir, f"PF_VietnameseShopfront_{letter}.prefab")
        meta_path = pf_path + ".meta"

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

        w, h, d = cfg["w"], cfg["h"], cfg["d"]
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
  m_Name: PF_VietnameseShopfront_{letter}
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
  - {{fileID: 2100000, guid: {mat_guids[letter]}, type: 2}}
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
  m_Center: {{x: 0, y: {h/2.0:.2f}, z: {d/2.0:.2f}}}
""")
        print(f"Generated prefab: {pf_path}")

def main():
    print("=== GENERATING EXTENDED SHOPFRONTS & SIGNBOARDS ===")
    generate_textures()
    generate_materials()
    generate_models()
    generate_prefabs()
    print("=== COMPLETED EXTENDED SHOPFRONTS GENERATION ===")

if __name__ == "__main__":
    main()
