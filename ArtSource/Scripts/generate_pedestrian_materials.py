#!/usr/bin/env python3
"""
generate_pedestrian_materials.py
Authors distinct PBR material properties and color diversity for the 6 pedestrian bodies:
- 6 distinct outfits/shirts (Coral Red, Honey Mustard, Ocean Teal, Crisp White, Hi-Vis Lime-Yellow, Ruby Wine Dress)
- 6 distinct skin tones (Peach Ivory, Golden Honey, Sun-kissed Tan, Porcelain Fair, Weathered Bronze, Warm Olive)
- 6 distinct hair colors (Deep Espresso, Jet Black, Auburn Red-Brown, Milk-Tea Caramel, Cropped Black, Midnight Indigo)
"""

import os
import re

MAT_DIR = "Assets/TramChanh/Art/Materials/People"
PREFAB_DIR = "Assets/TramChanh/Prefabs/People"

URP_LIT_TEMPLATE = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
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
  disabledShaderPasses:
  - MOTIONVECTORS
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 0}}
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
        m_Texture: {{fileID: 0}}
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
    - _Smoothness: {smoothness:.2f}
    - _SmoothnessTextureChannel: 0
    - _SpecularHighlights: 1
    - _SrcBlend: 1
    - _SrcBlendAlpha: 1
    - _Surface: 0
    - _WorkflowMode: 1
    - _XRMotionVectorsPass: 1
    - _ZWrite: 1
    m_Colors:
    - _BaseColor: {{r: {r:.4f}, g: {g:.4f}, b: {b:.4f}, a: 1}}
    - _Color: {{r: {r:.4f}, g: {g:.4f}, b: {b:.4f}, a: 1}}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
    - _SpecColor: {{r: 0.2, g: 0.2, b: 0.2, a: 1}}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
--- !u!114 &7120337354809294964
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

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 2100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

NEW_MATERIALS = {
    "MAT_Person_Skin_Bronze": {
        "guid": "c18e38d726b1428f804593e8201a05b1",
        "r": 0.588, "g": 0.365, "b": 0.224, "smoothness": 0.20
    },
    "MAT_Person_Skin_Olive": {
        "guid": "f47a19c503d242188ab4d19e078c5206",
        "r": 0.816, "g": 0.627, "b": 0.455, "smoothness": 0.20
    },
    "MAT_Person_Hair_Caramel": {
        "guid": "a827419ef02b4a11b6194ca8105c3d2e",
        "r": 0.510, "g": 0.329, "b": 0.220, "smoothness": 0.25
    },
    "MAT_Person_Hair_DarkIndigo": {
        "guid": "d91834ef12a0487cb1a482937cd9104a",
        "r": 0.086, "g": 0.086, "b": 0.133, "smoothness": 0.25
    }
}

UPDATED_COLORS = {
    "MAT_Person_Shirt_Coral": (0.880, 0.298, 0.176, 0.15),      # Vibrant Coral Red / Tangerine
    "MAT_Person_Top_Yellow": (0.920, 0.635, 0.082, 0.15),       # Warm Honey Mustard Yellow
    "MAT_Person_Hoodie_Teal": (0.067, 0.573, 0.651, 0.15),      # Ocean Teal / Turquoise
    "MAT_Person_Shirt_White": (0.980, 0.980, 0.990, 0.18),      # Crisp Pure Silk White
    "MAT_Person_Vest_Yellow": (0.784, 0.933, 0.031, 0.25),      # Hi-Vis Fluorescent Safety Lime
    "MAT_Person_Clothing_B": (0.580, 0.094, 0.224, 0.25),       # Elegant Ruby Wine / Crimson Dress
    "MAT_Person_Pants_Charcoal": (0.188, 0.208, 0.227, 0.15),   # Slate Charcoal Joggers
    "MAT_Person_Pants_Navy": (0.102, 0.157, 0.251, 0.15),       # Deep Indigo Denim
    "MAT_Person_Pants_Olive": (0.243, 0.302, 0.173, 0.15),      # Forest Muted Olive Chinos
    "MAT_Person_Skin_Light": (0.925, 0.820, 0.749, 0.20),       # Warm Peach Ivory
    "MAT_Person_Skin_Golden": (0.863, 0.671, 0.486, 0.20),      # Warm Golden Honey
    "MAT_Person_Skin_Tan": (0.714, 0.494, 0.337, 0.20),         # Sun-Kissed Tan
    "MAT_Person_Skin_Fair": (0.969, 0.878, 0.839, 0.20),        # Porcelain Rosy Fair
    "MAT_Person_Hair_DarkBrown": (0.141, 0.094, 0.071, 0.25),   # Deep Espresso
    "MAT_Person_Hair_Black": (0.071, 0.071, 0.075, 0.25),       # Sleek Jet Black
    "MAT_Person_Hair_Chestnut": (0.431, 0.204, 0.094, 0.25),    # Auburn Red-Brown
    "MAT_Person_Hat_Orange": (1.000, 0.271, 0.000, 0.20),       # Safety Blaze Orange
    "MAT_Person_Shoes_White": (0.961, 0.961, 0.969, 0.20),      # Clean White Sneakers
    "MAT_Person_Shoes_Black": (0.078, 0.078, 0.086, 0.20),      # Sturdy Black Shoes/Boots
    "MAT_Person_Shoes_Brown": (0.376, 0.208, 0.122, 0.25),      # Cognac Brown Leather Loafers
    "MAT_Person_Shoes_Tan": (0.620, 0.420, 0.220, 0.20)         # Warm Tan Casual Loafers
}

def update_material(path, r, g, b, smoothness):
    with open(path, "r") as f:
        text = f.read()

    # Update _BaseColor and _Color
    base_color_pat = r"- _BaseColor: \{r: [^,]+, g: [^,]+, b: [^,]+, a: [^}]+\}"
    repl_base = f"- _BaseColor: {{r: {r:.4f}, g: {g:.4f}, b: {b:.4f}, a: 1}}"
    text = re.sub(base_color_pat, repl_base, text)

    color_pat = r"- _Color: \{r: [^,]+, g: [^,]+, b: [^,]+, a: [^}]+\}"
    repl_col = f"- _Color: {{r: {r:.4f}, g: {g:.4f}, b: {b:.4f}, a: 1}}"
    text = re.sub(color_pat, repl_col, text)

    # Update _Smoothness
    smooth_pat = r"- _Smoothness: [0-9\.]+"
    repl_smooth = f"- _Smoothness: {smoothness:.2f}"
    text = re.sub(smooth_pat, repl_smooth, text)

    with open(path, "w") as f:
        f.write(text)
    print(f"Updated material: {path}")

def create_new_materials():
    for name, data in NEW_MATERIALS.items():
        mat_path = os.path.join(MAT_DIR, name + ".mat")
        meta_path = os.path.join(MAT_DIR, name + ".mat.meta")
        content = URP_LIT_TEMPLATE.format(
            name=name, r=data["r"], g=data["g"], b=data["b"], smoothness=data["smoothness"]
        )
        with open(mat_path, "w") as f:
            f.write(content)
        with open(meta_path, "w") as f:
            f.write(META_TEMPLATE.format(guid=data["guid"]))
        print(f"Created material: {mat_path} (guid={data['guid']})")

def update_existing_materials():
    for name, (r, g, b, s) in UPDATED_COLORS.items():
        mat_path = os.path.join(MAT_DIR, name + ".mat")
        if os.path.exists(mat_path):
            update_material(mat_path, r, g, b, s)

def update_prefabs():
    # Helper to replace material guid in prefab for SkinnedMeshRenderer (all LODs)
    # Person 03: replace Skin_Golden with Skin_Tan
    p03_path = os.path.join(PREFAB_DIR, "PF_Person_03.prefab")
    with open(p03_path, "r") as f:
        p03 = f.read()
    # 374304ba9ac5e403daf98591d52fbbae is MAT_Person_Skin_Golden -> ae2c1ae91683946e5bd92866144d59e3 (Skin_Tan)
    p03_updated = p03.replace("374304ba9ac5e403daf98591d52fbbae", "ae2c1ae91683946e5bd92866144d59e3")
    with open(p03_path, "w") as f:
        f.write(p03_updated)
    print("Updated PF_Person_03: assigned MAT_Person_Skin_Tan")

    # Person 04: replace Hair_Chestnut with Hair_Caramel
    p04_path = os.path.join(PREFAB_DIR, "PF_Person_04.prefab")
    with open(p04_path, "r") as f:
        p04 = f.read()
    # 4c39b81aeebda4866aff73db7ef1c2b0 is Hair_Chestnut -> a827419ef02b4a11b6194ca8105c3d2e (Hair_Caramel)
    p04_updated = p04.replace("4c39b81aeebda4866aff73db7ef1c2b0", "a827419ef02b4a11b6194ca8105c3d2e")
    with open(p04_path, "w") as f:
        f.write(p04_updated)
    print("Updated PF_Person_04: assigned MAT_Person_Hair_Caramel")

    # Person 05: replace Skin_Tan with Skin_Bronze
    p05_path = os.path.join(PREFAB_DIR, "PF_Person_05.prefab")
    with open(p05_path, "r") as f:
        p05 = f.read()
    # ae2c1ae91683946e5bd92866144d59e3 is Skin_Tan -> c18e38d726b1428f804593e8201a05b1 (Skin_Bronze)
    p05_updated = p05.replace("ae2c1ae91683946e5bd92866144d59e3", "c18e38d726b1428f804593e8201a05b1")
    with open(p05_path, "w") as f:
        f.write(p05_updated)
    print("Updated PF_Person_05: assigned MAT_Person_Skin_Bronze")

    # Person 06: replace Skin_Fair with Skin_Olive, and Hair_Black with Hair_DarkIndigo
    p06_path = os.path.join(PREFAB_DIR, "PF_Person_06.prefab")
    with open(p06_path, "r") as f:
        p06 = f.read()
    # 651543c06499d4c35934a6de06ee76c6 is Skin_Fair -> f47a19c503d242188ab4d19e078c5206 (Skin_Olive)
    p06_updated = p06.replace("651543c06499d4c35934a6de06ee76c6", "f47a19c503d242188ab4d19e078c5206")
    # 1887f4b54c06e440c8b12ab2382f78df is Hair_Black -> d91834ef12a0487cb1a482937cd9104a (Hair_DarkIndigo)
    p06_updated = p06_updated.replace("1887f4b54c06e440c8b12ab2382f78df", "d91834ef12a0487cb1a482937cd9104a")
    with open(p06_path, "w") as f:
        f.write(p06_updated)
    print("Updated PF_Person_06: assigned MAT_Person_Skin_Olive and MAT_Person_Hair_DarkIndigo")

def adjust_street_lamp_in_street_prefab():
    # Relocate the stray street lamp at x = -0.6 to x = -4.5 so it doesn't block the camera in View 11 lineup
    street_prefab = "Assets/TramChanh/Prefabs/Environment/PF_TramChanh_StreetExtended.prefab"
    if not os.path.exists(street_prefab):
        return
    with open(street_prefab, "r") as f:
        content = f.read()
    if re.search(r"propertyPath: m_LocalPosition\.x\s+value: -0\.6", content):
        content = re.sub(r"(propertyPath: m_LocalPosition\.x\s+value:) -0\.6", r"\1 -4.5", content, count=1)
        with open(street_prefab, "w") as f:
            f.write(content)
        print("Adjusted street lamp position in PF_TramChanh_StreetExtended from x=-0.6 to x=-4.5 (unblocks camera line of sight)")
    else:
        print("Street lamp already adjusted or not found at -0.6")

def main():
    print("=== Generating Authentic Pedestrian Materials & Outfits ===")
    create_new_materials()
    update_existing_materials()
    update_prefabs()
    adjust_street_lamp_in_street_prefab()
    print("=== Done Pedestrian Setup ===")

if __name__ == "__main__":
    main()
