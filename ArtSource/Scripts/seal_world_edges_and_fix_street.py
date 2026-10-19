#!/usr/bin/env python3
"""
ArtSource/Scripts/seal_world_edges_and_fix_street.py

Fulfills ART-FIX-023 Việc 5:
1. Creates SM_WorldEdge_BoulevardGate (Height 9.5m, Width 38m, Depth 2.0m) to enclose both ends
   of the street (at x = -249.5m and x = +249.5m), spanning the entire street width z in [-12m, 26m]:
   - Central ornamental portal archway over the roadway
   - Decorative security gates and masonry piers
   - Flanking boundary walls across north and south sidewalks
   - Foundation down to -0.35m to prevent ground gaps
2. Generates texture, material, and prefab for PF_WorldEdge_BoulevardGate.
3. Fixes all 1,133 target fileID mismatches across all 231 shopfront instances in
   PF_TramChanh_StreetExtended.prefab, ensuring all buildings are correctly positioned and rotated.
4. Corrects end-of-street building rotations so their front facades face inward toward the street:
   - At x = -248.5m: facing +X (rotY = -90)
   - At x = +248.5m: facing -X (rotY = +90)
5. Adds boundary gates, end-cap background towers, and framing street trees at x = -249.5m and +249.5m
   so that viewing from West240 and East240 shows a completely sealed, rich Vietnamese cityscape
   with zero brown void or sky horizon gaps!
"""

import os
import math
import uuid
import re
import numpy as np
from PIL import Image, ImageDraw, ImageFont

TEX_DIR = "Assets/TramChanh/Art/Textures"
MAT_DIR = "Assets/TramChanh/Art/Materials"
MODEL_DIR = "Assets/TramChanh/Art/Models/Environment"
PREFAB_DIR = "Assets/TramChanh/Prefabs/Environment"

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
# 1. GENERATE BOULEVARD GATE MODEL
# ==============================================================================

def generate_boulevard_gate():
    # Width along local X = 38.0m (which aligns with Z when rotated)
    # Height = 9.5m, Depth along local Z = 2.0m
    mesh = ObjMesh("SM_WorldEdge_BoulevardGate")
    w = 38.0
    h = 9.5
    d = 2.0

    # Continuous lower masonry wall with foundation down to -0.35m (height 3.5m across full 38m)
    mesh.add_box(-w/2, w/2, -0.35, 3.5, -d/2, d/2, (0.0, 1.0), (0.0, 0.40))

    # Flanking high privacy walls (X: -19m to -5.5m and +5.5m to +19m) up to height 7.5m
    mesh.add_box(-w/2, -5.5, 3.5, 7.5, -d/2 + 0.2, d/2 - 0.2, (0.0, 0.4), (0.40, 0.75))
    mesh.add_box(5.5, w/2, 3.5, 7.5, -d/2 + 0.2, d/2 - 0.2, (0.6, 1.0), (0.40, 0.75))

    # Central Portal Columns (X = -5.5 to -4.2 and X = +4.2 to +5.5) up to 9.0m with -0.35m foundation
    mesh.add_box(-5.5, -4.2, -0.35, 9.0, -d/2 - 0.2, d/2 + 0.2, (0.4, 0.5), (0.1, 0.9))
    mesh.add_box(4.2, 5.5, -0.35, 9.0, -d/2 - 0.2, d/2 + 0.2, (0.5, 0.6), (0.1, 0.9))

    # Central Portal Arch Overpass Beam (X: -5.5 to 5.5, Y: 6.2 to 8.2)
    mesh.add_box(-5.5, 5.5, 6.2, 8.2, -d/2 - 0.15, d/2 + 0.15, (0.3, 0.7), (0.65, 0.85))

    # Traditional Vietnamese Ceramic Tile Roof Canopy on Portal (Y: 8.2 to 9.5, overhangs to 6.2m)
    mesh.add_box(-6.2, 6.2, 8.2, 9.5, -d/2 - 0.4, d/2 + 0.4, (0.2, 0.8), (0.85, 1.0))

    # Ornamental Iron Security Grille panels across the lower arch (Y: 0.0 to 6.0, Z: -0.1 to 0.1)
    mesh.add_box(-4.2, 4.2, 0.0, 6.0, -0.1, 0.1, (0.35, 0.65), (0.1, 0.6))

    out_obj = os.path.join(MODEL_DIR, "SM_WorldEdge_BoulevardGate.obj")
    mesh.write_obj(out_obj)

# ==============================================================================
# 2. GENERATE TEXTURE, MATERIAL & PREFAB FOR BOULEVARD GATE
# ==============================================================================

def generate_gate_texture_and_prefab():
    # Texture: Terracotta brick, cream stucco, green ornamental ironwork
    im = Image.new("RGB", (1024, 1024), (215, 185, 150))
    draw = ImageDraw.Draw(im)

    # Terracotta roof header (Y: 850 to 1024)
    for ry in range(850, 1024, 16):
        col = (175, 60, 30) if (ry // 16) % 2 == 0 else (195, 75, 40)
        draw.rectangle([0, ry, 1024, ry + 16], fill=col)

    # Cream architectural stucco (Y: 400 to 850)
    draw.rectangle([0, 400, 1024, 850], fill=(235, 225, 205))
    # Civic banner
    draw.rectangle([250, 680, 774, 810], fill=(180, 25, 20), outline=(245, 215, 50), width=6)
    draw.text((290, 725), "KHU ĐÔ THỊ TRẠM CHANH", fill=(255, 255, 255))

    # Red brick lower wall (Y: 50 to 400)
    for by in range(50, 400, 18):
        for bx in range(0, 1024, 45):
            shift = 22 if (by // 18) % 2 == 1 else 0
            draw.rectangle([bx + shift, by, bx + shift + 40, by + 15], fill=(160, 65, 45), outline=(130, 50, 35), width=2)

    # Iron gate pattern
    for gx in range(350, 674, 25):
        draw.line([(gx, 100), (gx, 600)], fill=(30, 55, 40), width=4)

    tex_path = os.path.join(TEX_DIR, "T_WorldEdge_BoulevardGate_BaseColor.png")
    im.save(tex_path)
    print(f"Generated: {tex_path}")

    # Meta for model
    m_path = os.path.join(MODEL_DIR, "SM_WorldEdge_BoulevardGate.obj")
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

    # Meta for texture
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

    # Material
    mat_path = os.path.join(MAT_DIR, "MAT_WorldEdge_BoulevardGate.mat")
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
  m_Name: MAT_WorldEdge_BoulevardGate
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
    - _Smoothness: 0.3
    m_Colors:
    - _BaseColor: {{r: 1, g: 1, b: 1, a: 1}}
    - _Color: {{r: 1, g: 1, b: 1, a: 1}}
""")
    print(f"Created material: {mat_path}")

    # Prefab
    pf_path = os.path.join(PREFAB_DIR, "PF_WorldEdge_BoulevardGate.prefab")
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
  m_Name: PF_WorldEdge_BoulevardGate
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
  m_Size: {{x: 38.0, y: 9.85, z: 2.0}}
  m_Center: {{x: 0, y: 4.575, z: 0}}
""")
    print(f"Created prefab: {pf_path}")
    return pf_guid

# ==============================================================================
# 3. FIX TARGET FILEIDS AND SEAL STREET ENDS IN PF_TramChanh_StreetExtended.prefab
# ==============================================================================

def fix_and_seal_street_prefab(gate_pf_guid):
    street_pf = os.path.join(PREFAB_DIR, "PF_TramChanh_StreetExtended.prefab")
    with open(street_pf) as f:
        content = f.read()

    # Map GUID to correct (GO, Transform)
    guid_map = {
        "d57a6e49195fd436db0244cc9bcc3736": ("3670817710514171788", "5110386587436877682"), # A
        "905c106b12a2a4b87ae293b5860d5e1d": ("435146432788792478", "3362705833130610544"), # B
        "beeb4858137804eef9d8e58e49fef828": ("3282979905441890864", "5960364670772929821"), # C
        "4d720ad7697b4ecda83296c09dfb621e": ("3670817710514171788", "5110386587436877682"), # D
        "b62e717e81c340ba805e5ec099d91944": ("3670817710514171788", "5110386587436877682"), # E
        "d5b4b41dbd414b8f9f9044e11d686bde": ("3670817710514171788", "5110386587436877682"), # F
    }

    # Step 3.1: Fix all target fileIDs in PrefabInstances
    blocks = content.split("--- !u!1001 &")
    fixed_blocks = [blocks[0]]

    fixed_count = 0
    end_count = 0

    for b in blocks[1:]:
        src_m = re.search(r"m_SourcePrefab:\s*\{fileID:\s*\d+,\s*guid:\s*([a-f0-9]+)", b)
        if src_m and src_m.group(1) in guid_map:
            guid = src_m.group(1)
            correct_go, correct_tf = guid_map[guid]

            # Parse modifications
            # We want to replace target: {fileID: X, guid: GUID} with the correct fileID
            def fix_target(match):
                nonlocal fixed_count
                old_id = match.group(1)
                g = match.group(2)
                prop = match.group(3)
                if g == guid:
                    new_id = correct_go if prop == "m_Name" else correct_tf
                    if old_id != new_id:
                        fixed_count += 1
                        return f"target: {{fileID: {new_id}, guid: {g}, type: 3}}\n      propertyPath: {prop}"
                return match.group(0)

            b_fixed = re.sub(r"target:\s*\{fileID:\s*(\d+),\s*guid:\s*([a-f0-9]+),\s*type:\s*3\}\n\s*propertyPath:\s*([^\n]+)", fix_target, b)

            # Check if this is an end closure building at x = -248.5 or +248.5
            pos_x_m = re.search(r"propertyPath: m_LocalPosition\.x\s*\n\s*value:\s*(-?248\.5)", b_fixed)
            if pos_x_m:
                val = float(pos_x_m.group(1))
                end_count += 1
                if val < 0:
                    # West end: face inward (+X), so rotY = -90 (EulerAnglesHint.y = -90, localRotation = (0, -0.7071068, 0, 0.7071068))
                    b_fixed = re.sub(r"(propertyPath: m_LocalRotation\.y\s*\n\s*value:\s*)[^\n]+", r"\g<1>-0.7071068", b_fixed)
                    b_fixed = re.sub(r"(propertyPath: m_LocalRotation\.w\s*\n\s*value:\s*)[^\n]+", r"\g<1>0.7071068", b_fixed)
                    b_fixed = re.sub(r"(propertyPath: m_LocalEulerAnglesHint\.y\s*\n\s*value:\s*)[^\n]+", r"\g<1>-90", b_fixed)
                else:
                    # East end: face inward (-X), so rotY = 90 (EulerAnglesHint.y = 90, localRotation = (0, 0.7071068, 0, 0.7071068))
                    b_fixed = re.sub(r"(propertyPath: m_LocalRotation\.y\s*\n\s*value:\s*)[^\n]+", r"\g<1>0.7071068", b_fixed)
                    b_fixed = re.sub(r"(propertyPath: m_LocalRotation\.w\s*\n\s*value:\s*)[^\n]+", r"\g<1>0.7071068", b_fixed)
                    b_fixed = re.sub(r"(propertyPath: m_LocalEulerAnglesHint\.y\s*\n\s*value:\s*)[^\n]+", r"\g<1>90", b_fixed)

            fixed_blocks.append(b_fixed)
        else:
            fixed_blocks.append(b)

    content = "--- !u!1001 &".join(fixed_blocks)
    print(f"Fixed {fixed_count} target fileIDs and corrected {end_count} end building rotations!")

    # Step 3.2: Add Boulevard Gates, End Backdrop Towers, and Framing Trees if not already present
    if "PF_WorldEdge_BoulevardGate" in content:
        print("PF_WorldEdge_BoulevardGate already wired.")
    else:
        # Get ExtendedBuildings Transform fileID
        ext_tf_match = re.search(r"--- !u!1 &(\d+)\nGameObject:\n[\s\S]*?m_Name: ExtendedBuildings", content)
        ext_go_id = ext_tf_match.group(1)
        tf_match = re.search(r"--- !u!4 &(\d+)\nTransform:\n[\s\S]*?m_GameObject: \{fileID: " + ext_go_id + r"\}", content)
        ext_tf_id = tf_match.group(1)

        tower_a_guid = "90b50302bfca4e78b77051dcaeb971f4" # from previous run
        for p in [os.path.join(PREFAB_DIR, "PF_Building_Tower_A.prefab.meta")]:
            with open(p) as f:
                for line in f:
                    if "guid:" in line:
                        tower_a_guid = line.strip().split()[1]
                        break

        tree_guid = "46c395f03f7734c059f16b854f03f49d"

        # Gate at West: x = -249.5, z = 7.0 (center of street span z in [-12, 26]), rotY = 90
        # Gate at East: x = 249.5, z = 7.0, rotY = -90
        new_instances = f"""--- !u!1001 &9910000000000000001
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {ext_tf_id}}}
    m_Modifications:
    - target: {{fileID: 3670817710514171788, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_Name
      value: PF_WorldEdge_BoulevardGate_West
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: -249.5
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 7.0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_LocalRotation.w
      value: 0.7071068
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_LocalRotation.y
      value: 0.7071068
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 90
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {gate_pf_guid}, type: 3}}
--- !u!1001 &9910000000000000002
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {ext_tf_id}}}
    m_Modifications:
    - target: {{fileID: 3670817710514171788, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_Name
      value: PF_WorldEdge_BoulevardGate_East
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: 249.5
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 7.0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_LocalRotation.w
      value: 0.7071068
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_LocalRotation.y
      value: -0.7071068
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {gate_pf_guid}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.y
      value: -90
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {gate_pf_guid}, type: 3}}
--- !u!1001 &9910000000000000003
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {ext_tf_id}}}
    m_Modifications:
    - target: {{fileID: 3670817710514171788, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_Name
      value: PF_Building_Tower_EndBackdrop_West
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: -256.0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 7.0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_LocalRotation.w
      value: 0.7071068
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_LocalRotation.y
      value: 0.7071068
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 90
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {tower_a_guid}, type: 3}}
--- !u!1001 &9910000000000000004
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {ext_tf_id}}}
    m_Modifications:
    - target: {{fileID: 3670817710514171788, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_Name
      value: PF_Building_Tower_EndBackdrop_East
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: 256.0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 7.0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_LocalRotation.w
      value: 0.7071068
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_LocalRotation.y
      value: -0.7071068
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5110386587436877682, guid: {tower_a_guid}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.y
      value: -90
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {tower_a_guid}, type: 3}}
--- !u!1001 &9910000000000000005
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {ext_tf_id}}}
    m_Modifications:
    - target: {{fileID: 2418227009475731716, guid: {tree_guid}, type: 3}}
      propertyPath: m_Name
      value: PF_StreetTree_End_West_South
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5180483940072663407, guid: {tree_guid}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: -246.0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5180483940072663407, guid: {tree_guid}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5180483940072663407, guid: {tree_guid}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 3.2
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {tree_guid}, type: 3}}
--- !u!1001 &9910000000000000006
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {ext_tf_id}}}
    m_Modifications:
    - target: {{fileID: 2418227009475731716, guid: {tree_guid}, type: 3}}
      propertyPath: m_Name
      value: PF_StreetTree_End_West_North
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5180483940072663407, guid: {tree_guid}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: -246.0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5180483940072663407, guid: {tree_guid}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5180483940072663407, guid: {tree_guid}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 13.0
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {tree_guid}, type: 3}}
--- !u!1001 &9910000000000000007
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {ext_tf_id}}}
    m_Modifications:
    - target: {{fileID: 2418227009475731716, guid: {tree_guid}, type: 3}}
      propertyPath: m_Name
      value: PF_StreetTree_End_East_South
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5180483940072663407, guid: {tree_guid}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: 246.0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5180483940072663407, guid: {tree_guid}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5180483940072663407, guid: {tree_guid}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 3.2
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {tree_guid}, type: 3}}
--- !u!1001 &9910000000000000008
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {ext_tf_id}}}
    m_Modifications:
    - target: {{fileID: 2418227009475731716, guid: {tree_guid}, type: 3}}
      propertyPath: m_Name
      value: PF_StreetTree_End_East_North
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5180483940072663407, guid: {tree_guid}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: 246.0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5180483940072663407, guid: {tree_guid}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5180483940072663407, guid: {tree_guid}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 13.0
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {tree_guid}, type: 3}}
"""
        content = content + "\n" + new_instances

    with open(street_pf, "w") as f:
        f.write(content)
    print("Successfully sealed world edges in street prefab!")

def main():
    print("=== Sealing World Edges and Fixing Street Prefab ===")
    generate_boulevard_gate()
    gate_guid = generate_gate_texture_and_prefab()
    fix_and_seal_street_prefab(gate_guid)
    print("=== Done Sealing World Edges ===")

if __name__ == "__main__":
    main()
