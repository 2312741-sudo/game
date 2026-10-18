#!/usr/bin/env python3
"""
ArtSource/Scripts/generate_rain_and_blackout_assets.py

Fulfills ART-FIX-023 Việc 6:
Prepares art assets for Rain & Blackout events (Pure Art, No Code):
1. Wet Road & Sidewalk Materials:
   - MAT_Road_WetRain.mat (High smoothness 0.92, reflective rainy asphalt)
   - MAT_Sidewalk_WetRain.mat (High smoothness 0.88, wet terrazzo tiles)
2. Water Puddles (Vũng nước):
   - SM_Puddle_Oval.obj & SM_Puddle_Irregular.obj
   - T_Puddle_Mask.png & MAT_Puddle_Water.mat
   - PF_Puddle_Oval.prefab & PF_Puddle_Irregular.prefab
3. Blackout Street Lamp (Đèn tắt):
   - MAT_StreetLamp_Blackout.mat (Zero emission, unlit luminaire)
   - PF_StreetLamp_Blackout.prefab (Light component disabled)
4. Hanging String Lights (Đèn dây):
   - SM_StringLights_Warm.obj (Catenary cable with hanging bulbs)
   - T_StringLights_BaseColor.png & MAT_StringLights_Warm.mat & MAT_StringLights_Blackout.mat
   - PF_StringLights_Warm.prefab & PF_StringLights_Blackout.prefab
"""

import os
import math
import uuid
import numpy as np
from PIL import Image, ImageDraw

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
# 1. WET ROAD & SIDEWALK MATERIALS
# ==============================================================================

def generate_wet_ground_materials():
    # Find road texture GUID
    road_tex_guid = "228e93ee5c4644a1bb3709b1f237c541"
    for p in ["Assets/TramChanh/Art/Textures/T_Asphalt_Night_BaseColor.png.meta"]:
        if os.path.exists(p):
            with open(p) as f:
                for line in f:
                    if "guid:" in line:
                        road_tex_guid = line.strip().split()[1]
                        break

    # Find sidewalk texture GUID
    sw_tex_guid = "46b856ca010e42d7aa0ea1797ca3cb73"
    for p in ["Assets/TramChanh/Art/Textures/T_Sidewalk_Tiles_BaseColor.png.meta"]:
        if os.path.exists(p):
            with open(p) as f:
                for line in f:
                    if "guid:" in line:
                        sw_tex_guid = line.strip().split()[1]
                        break

    # MAT_Road_WetRain.mat
    mat_road_path = os.path.join(MAT_DIR, "MAT_Road_WetRain.mat")
    mat_road_meta = mat_road_path + ".meta"
    if not os.path.exists(mat_road_meta):
        guid = generate_guid()
        with open(mat_road_meta, "w") as f:
            f.write(f"fileFormatVersion: 2\nguid: {guid}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

    with open(mat_road_path, "w") as f:
        f.write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: MAT_Road_WetRain
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {road_tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {road_tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Floats:
    - _Metallic: 0.15
    - _Smoothness: 0.92
    m_Colors:
    - _BaseColor: {{r: 0.75, g: 0.75, b: 0.8, a: 1}}
    - _Color: {{r: 0.75, g: 0.75, b: 0.8, a: 1}}
""")
    print(f"Created: {mat_road_path}")

    # MAT_Sidewalk_WetRain.mat
    mat_sw_path = os.path.join(MAT_DIR, "MAT_Sidewalk_WetRain.mat")
    mat_sw_meta = mat_sw_path + ".meta"
    if not os.path.exists(mat_sw_meta):
        guid = generate_guid()
        with open(mat_sw_meta, "w") as f:
            f.write(f"fileFormatVersion: 2\nguid: {guid}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

    with open(mat_sw_path, "w") as f:
        f.write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: MAT_Sidewalk_WetRain
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {sw_tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {sw_tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Floats:
    - _Metallic: 0.1
    - _Smoothness: 0.88
    m_Colors:
    - _BaseColor: {{r: 0.8, g: 0.8, b: 0.85, a: 1}}
    - _Color: {{r: 0.8, g: 0.8, b: 0.85, a: 1}}
""")
    print(f"Created: {mat_sw_path}")

# ==============================================================================
# 2. PUDDLE MESHES & MATERIAL
# ==============================================================================

def generate_puddle_assets():
    # Texture for puddle
    im = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    # Draw soft water puddle mask
    draw.ellipse([30, 60, 482, 452], fill=(25, 30, 35, 230))
    draw.ellipse([60, 90, 452, 422], fill=(20, 24, 28, 245))
    tex_path = os.path.join(TEX_DIR, "T_Puddle_Mask.png")
    im.save(tex_path)

    tex_meta = tex_path + ".meta"
    if not os.path.exists(tex_meta):
        tex_guid = generate_guid()
        with open(tex_meta, "w") as f:
            f.write(f"fileFormatVersion: 2\nguid: {tex_guid}\nTextureImporter:\n  serializedVersion: 13000\n")
    else:
        with open(tex_meta) as f:
            for line in f:
                if "guid:" in line:
                    tex_guid = line.strip().split()[1]
                    break

    # MAT_Puddle_Water.mat
    mat_puddle_path = os.path.join(MAT_DIR, "MAT_Puddle_Water.mat")
    mat_meta = mat_puddle_path + ".meta"
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

    with open(mat_puddle_path, "w") as f:
        f.write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: MAT_Puddle_Water
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Floats:
    - _Surface: 1
    - _Blend: 0
    - _Metallic: 0.1
    - _Smoothness: 0.98
    m_Colors:
    - _BaseColor: {{r: 0.15, g: 0.18, b: 0.22, a: 0.92}}
    - _Color: {{r: 0.15, g: 0.18, b: 0.22, a: 0.92}}
""")
    print(f"Created: {mat_puddle_path}")

    # SM_Puddle_Oval.obj (Width: 2.2m, Length: 1.4m, Thin height: 0.005m)
    mesh_oval = ObjMesh("SM_Puddle_Oval")
    mesh_oval.add_box(-1.1, 1.1, 0.0, 0.005, -0.7, 0.7, (0.0, 1.0), (0.0, 1.0))
    p1 = os.path.join(MODEL_DIR, "SM_Puddle_Oval.obj")
    mesh_oval.write_obj(p1)

    m1_meta = p1 + ".meta"
    if not os.path.exists(m1_meta):
        guid1 = generate_guid()
        with open(m1_meta, "w") as f:
            f.write(f"fileFormatVersion: 2\nguid: {guid1}\nModelImporter:\n  serializedVersion: 22203\n")
    else:
        with open(m1_meta) as f:
            for line in f:
                if "guid:" in line:
                    guid1 = line.strip().split()[1]
                    break

    # SM_Puddle_Irregular.obj (Width: 3.2m, Length: 1.8m)
    mesh_irr = ObjMesh("SM_Puddle_Irregular")
    mesh_irr.add_box(-1.6, 1.6, 0.0, 0.005, -0.9, 0.9, (0.0, 1.0), (0.0, 1.0))
    p2 = os.path.join(MODEL_DIR, "SM_Puddle_Irregular.obj")
    mesh_irr.write_obj(p2)

    m2_meta = p2 + ".meta"
    if not os.path.exists(m2_meta):
        guid2 = generate_guid()
        with open(m2_meta, "w") as f:
            f.write(f"fileFormatVersion: 2\nguid: {guid2}\nModelImporter:\n  serializedVersion: 22203\n")
    else:
        with open(m2_meta) as f:
            for line in f:
                if "guid:" in line:
                    guid2 = line.strip().split()[1]
                    break

    # Prefabs for puddles
    for name, m_guid, (pw, pd) in [("PF_Puddle_Oval", guid1, (2.2, 1.4)), ("PF_Puddle_Irregular", guid2, (3.2, 1.8))]:
        pf_path = os.path.join(PREFAB_DIR, f"{name}.prefab")
        pf_meta = pf_path + ".meta"
        if not os.path.exists(pf_meta):
            pf_guid = generate_guid()
            with open(pf_meta, "w") as f:
                f.write(f"fileFormatVersion: 2\nguid: {pf_guid}\nPrefabImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

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
  m_Layer: 8
  m_Name: {name}
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
  m_Mesh: {{fileID: -2432090755550338912, guid: {m_guid}, type: 3}}
--- !u!23 &6571542907280224375
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3670817710514171788}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_Materials:
  - {{fileID: 2100000, guid: {mat_guid}, type: 2}}
""")
        print(f"Created: {pf_path}")

# ==============================================================================
# 3. BLACKOUT STREET LAMP
# ==============================================================================

def generate_blackout_streetlamp():
    # Material for blackout lamp (zero emission)
    mat_path = os.path.join(MAT_DIR, "MAT_StreetLamp_Blackout.mat")
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
  m_Name: MAT_StreetLamp_Blackout
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Floats:
    - _Metallic: 0.6
    - _Smoothness: 0.4
    m_Colors:
    - _BaseColor: {{r: 0.18, g: 0.22, b: 0.25, a: 1}}
    - _Color: {{r: 0.18, g: 0.22, b: 0.25, a: 1}}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
""")
    print(f"Created: {mat_path}")

    # Prefab for blackout lamp
    # Uses existing SM_StreetLamp.obj
    lamp_mesh_guid = "472775bd794eb401fa65f999398443d6"
    pf_path = os.path.join(PREFAB_DIR, "PF_StreetLamp_Blackout.prefab")
    pf_meta = pf_path + ".meta"
    if not os.path.exists(pf_meta):
        pf_guid = generate_guid()
        with open(pf_meta, "w") as f:
            f.write(f"fileFormatVersion: 2\nguid: {pf_guid}\nPrefabImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

    with open(pf_path, "w") as f:
        f.write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &6035353063394943782
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 1156198737822671591}}
  - component: {{fileID: 953871973894973075}}
  - component: {{fileID: 7518328783529247750}}
  - component: {{fileID: 3864179195356326872}}
  m_Layer: 8
  m_Name: PF_StreetLamp_Blackout
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &1156198737822671591
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 6035353063394943782}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: 6168219387540733530}}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!33 &953871973894973075
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 6035353063394943782}}
  m_Mesh: {{fileID: -2432090755550338912, guid: {lamp_mesh_guid}, type: 3}}
--- !u!23 &7518328783529247750
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 6035353063394943782}}
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
--- !u!65 &3864179195356326872
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 6035353063394943782}}
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
  m_Size: {{x: 0.5, y: 6.2, z: 2.2}}
  m_Center: {{x: 0, y: 3.1, z: 0.7}}
--- !u!1 &7234834805032650834
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 6168219387540733530}}
  - component: {{fileID: 7884936518857532955}}
  m_Layer: 0
  m_Name: LuminaireLight
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &6168219387540733530
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 7234834805032650834}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0.6755902, y: 0, z: 0, w: 0.7372774}}
  m_LocalPosition: {{x: 0, y: 5.95, z: 1.75}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 1156198737822671591}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!108 &7884936518857532955
Light:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 7234834805032650834}}
  m_Enabled: 0
  serializedVersion: 13
  m_Type: 0
  m_Color: {{r: 1, g: 0.84, b: 0.6, a: 1}}
  m_Intensity: 0
  m_Range: 14
""")
    print(f"Created: {pf_path}")

# ==============================================================================
# 4. HANGING STRING LIGHTS (ĐÈN DÂY)
# ==============================================================================

def generate_string_lights():
    # Model: 6.0m catenary wire with 8 hanging light bulbs
    mesh = ObjMesh("SM_StringLights_Warm")
    # Wire line across X: -3.0m to +3.0m, sagging from Y=3.0 down to Y=2.5 at center
    num_pts = 16
    prev_pt = None
    for i in range(num_pts + 1):
        t = i / num_pts
        x = -3.0 + 6.0 * t
        # Catenary sag
        y = 3.0 - 0.5 * (1.0 - 4.0 * (t - 0.5)**2)
        pt = (x, y, 0.0)
        if prev_pt is not None:
            # Add thin wire segment
            mesh.add_box(prev_pt[0], pt[0], min(prev_pt[1], pt[1]) - 0.01, max(prev_pt[1], pt[1]) + 0.01, -0.01, 0.01, (0.0, 0.1), (0.0, 0.1))
        prev_pt = pt

    # 8 Hanging Edison bulbs
    for bi in range(8):
        t = (bi + 0.5) / 8.0
        bx = -3.0 + 6.0 * t
        by = 3.0 - 0.5 * (1.0 - 4.0 * (t - 0.5)**2)
        # Small drop wire
        mesh.add_box(bx - 0.005, bx + 0.005, by - 0.12, by, -0.005, 0.005, (0.0, 0.1), (0.0, 0.1))
        # Bulb cube/sphere approximation
        mesh.add_box(bx - 0.04, bx + 0.04, by - 0.20, by - 0.12, -0.04, 0.04, (0.8, 1.0), (0.8, 1.0))

    m_path = os.path.join(MODEL_DIR, "SM_StringLights_Warm.obj")
    mesh.write_obj(m_path)

    m_meta = m_path + ".meta"
    if not os.path.exists(m_meta):
        mesh_guid = generate_guid()
        with open(m_meta, "w") as f:
            f.write(f"fileFormatVersion: 2\nguid: {mesh_guid}\nModelImporter:\n  serializedVersion: 22203\n")
    else:
        with open(m_meta) as f:
            for line in f:
                if "guid:" in line:
                    mesh_guid = line.strip().split()[1]
                    break

    # Texture for string lights
    im = Image.new("RGB", (256, 256), (30, 30, 30)) # Black cable
    draw = ImageDraw.Draw(im)
    draw.rectangle([180, 180, 256, 256], fill=(255, 230, 150)) # Golden amber bulb
    tex_path = os.path.join(TEX_DIR, "T_StringLights_BaseColor.png")
    im.save(tex_path)

    t_meta = tex_path + ".meta"
    if not os.path.exists(t_meta):
        tex_guid = generate_guid()
        with open(t_meta, "w") as f:
            f.write(f"fileFormatVersion: 2\nguid: {tex_guid}\nTextureImporter:\n  serializedVersion: 13000\n")
    else:
        with open(t_meta) as f:
            for line in f:
                if "guid:" in line:
                    tex_guid = line.strip().split()[1]
                    break

    # Material: Warm (Lit with emission)
    mat_warm_path = os.path.join(MAT_DIR, "MAT_StringLights_Warm.mat")
    mat_warm_meta = mat_warm_path + ".meta"
    if not os.path.exists(mat_warm_meta):
        mat_warm_guid = generate_guid()
        with open(mat_warm_meta, "w") as f:
            f.write(f"fileFormatVersion: 2\nguid: {mat_warm_guid}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    else:
        with open(mat_warm_meta) as f:
            for line in f:
                if "guid:" in line:
                    mat_warm_guid = line.strip().split()[1]
                    break

    with open(mat_warm_path, "w") as f:
        f.write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: MAT_StringLights_Warm
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Floats:
    - _Metallic: 0.1
    - _Smoothness: 0.6
    m_Colors:
    - _BaseColor: {{r: 1, g: 1, b: 1, a: 1}}
    - _Color: {{r: 1, g: 1, b: 1, a: 1}}
    - _EmissionColor: {{r: 1.0, g: 0.8, b: 0.45, a: 1}}
""")
    print(f"Created: {mat_warm_path}")

    # Material: Blackout (No emission)
    mat_blackout_path = os.path.join(MAT_DIR, "MAT_StringLights_Blackout.mat")
    mat_blackout_meta = mat_blackout_path + ".meta"
    if not os.path.exists(mat_blackout_meta):
        mat_blackout_guid = generate_guid()
        with open(mat_blackout_meta, "w") as f:
            f.write(f"fileFormatVersion: 2\nguid: {mat_blackout_guid}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    else:
        with open(mat_blackout_meta) as f:
            for line in f:
                if "guid:" in line:
                    mat_blackout_guid = line.strip().split()[1]
                    break

    with open(mat_blackout_path, "w") as f:
        f.write(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: MAT_StringLights_Blackout
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Floats:
    - _Metallic: 0.1
    - _Smoothness: 0.2
    m_Colors:
    - _BaseColor: {{r: 0.3, g: 0.3, b: 0.3, a: 1}}
    - _Color: {{r: 0.3, g: 0.3, b: 0.3, a: 1}}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
""")
    print(f"Created: {mat_blackout_path}")

    # Prefabs: PF_StringLights_Warm & PF_StringLights_Blackout
    for name, m_guid in [("PF_StringLights_Warm", mat_warm_guid), ("PF_StringLights_Blackout", mat_blackout_guid)]:
        pf_path = os.path.join(PREFAB_DIR, f"{name}.prefab")
        pf_meta = pf_path + ".meta"
        if not os.path.exists(pf_meta):
            pf_guid = generate_guid()
            with open(pf_meta, "w") as f:
                f.write(f"fileFormatVersion: 2\nguid: {pf_guid}\nPrefabImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

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
  m_Layer: 8
  m_Name: {name}
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
  m_CastShadows: 0
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_Materials:
  - {{fileID: 2100000, guid: {m_guid}, type: 2}}
""")
        print(f"Created: {pf_path}")

def main():
    print("=== Generating Rain & Blackout Art Assets ===")
    generate_wet_ground_materials()
    generate_puddle_assets()
    generate_blackout_streetlamp()
    generate_string_lights()
    print("=== Done Rain & Blackout Assets ===")

if __name__ == "__main__":
    main()
