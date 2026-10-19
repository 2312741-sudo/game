#!/usr/bin/env python3
"""
ArtSource/Scripts/generate_vfx_assets.py

Fulfills ART-FIX-028:
Generates weather and event visual effect particle assets for Project Trạm Chanh:
1. Procedural Textures (PIL / NumPy):
   - T_VFX_RainStreak.png: Translucent vertical water droplet streak with soft falloff
   - T_VFX_RainSplash.png: Concentric circular puddle ripples and radial droplet splash
   - T_VFX_HeatHaze.png: Soft organic warm air distortion quads
   - T_VFX_ElectricSparks.png: Brilliant electrical arc / starburst core with glowing rays
2. URP Particle Materials:
   - MAT_VFX_RainStreak.mat (Alpha blend)
   - MAT_VFX_RainSplash.mat (Alpha blend)
   - MAT_VFX_HeatHaze.mat (Alpha blend)
   - MAT_VFX_ElectricSparks.mat (Additive)
3. Unity Particle System Prefabs (URP / Unity 6 compatible):
   - PF_VFX_Rain.prefab (<= 500 particles, 15m x 15m box, subtle angle)
   - PF_VFX_RainSplash.prefab (<= 300 particles, pavement ripples)
   - PF_VFX_HeatHaze.prefab (<= 200 particles, warm air shimmer quads)
   - PF_VFX_Sparks_Blackout.prefab (<= 150 particles, blackout burst)

Strict Constraints:
- NO real-time lights attached to particle systems (mobile/WebGL budget)
- All particle systems have maxParticles <= 600
- Valid Unity YAML & meta files with resolving GUIDs and fileIDs
"""

import os
import sys
import math
import uuid
import hashlib
import numpy as np
from PIL import Image

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

TEX_DIR = os.path.join(REPO_ROOT, "Assets", "TramChanh", "Art", "Textures", "VFX")
MAT_DIR = os.path.join(REPO_ROOT, "Assets", "TramChanh", "Art", "Materials", "VFX")
PREFAB_DIR = os.path.join(REPO_ROOT, "Assets", "TramChanh", "Prefabs", "VFX")

# URP Particles Unlit Shader GUID
URP_PARTICLES_UNLIT_GUID = "0406db5a14f94604a8c57ccfbc9f3b46"


def deterministic_guid(seed_str: str) -> str:
    """Generate a consistent 32-character hex GUID from a seed string."""
    return hashlib.md5(seed_str.encode("utf-8")).hexdigest()


def deterministic_file_id(seed_str: str) -> int:
    """Generate a consistent 64-bit signed integer fileID from a seed string."""
    h = hashlib.sha256(seed_str.encode("utf-8")).hexdigest()
    # convert 15 hex chars to signed 64-bit int
    val = int(h[:15], 16)
    if val > 0x7FFFFFFFFFFFFFFF:
        val -= 0x10000000000000000
    return val


def get_or_create_guid(meta_path: str, fallback_seed: str) -> str:
    if os.path.exists(meta_path):
        with open(meta_path, "r", encoding="utf-8") as f:
            for line in f:
                if line.startswith("guid:"):
                    return line.strip().split()[1]
    return deterministic_guid(fallback_seed)


def ensure_folder_meta(folder_path: str, seed: str):
    os.makedirs(folder_path, exist_ok=True)
    meta_path = folder_path.rstrip("/\\") + ".meta"
    if not os.path.exists(meta_path):
        f_guid = deterministic_guid(seed)
        content = f"""fileFormatVersion: 2
guid: {f_guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(content)
        print(f"Created folder meta: {os.path.relpath(meta_path, REPO_ROOT)}")


def write_texture_meta(meta_path: str, tex_guid: str):
    content = f"""fileFormatVersion: 2
guid: {tex_guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
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
  maxTextureSize: 1024
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
    maxTextureSize: 1024
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


def generate_rain_streak_texture(output_path: str):
    """Generates T_VFX_RainStreak.png: 128x256 translucent water streak."""
    w, h = 128, 256
    arr = np.zeros((h, w, 4), dtype=np.uint8)
    cx = 64
    for y in range(h):
        if y < 15 or y > 245:
            continue
        t = (y - 15) / 230.0
        # Tapering width: narrow at top (tail), wider near droplet head
        sigma_x = 0.75 + 2.4 * t
        # Vertical envelope
        if t < 0.2:
            v_env = t / 0.2
        elif t > 0.88:
            v_env = (1.0 - t) / 0.12
        else:
            v_env = 1.0
        for x in range(w):
            dx = x - cx
            val = np.exp(-(dx * dx) / (2.0 * sigma_x * sigma_x)) * v_env
            if val > 0.005:
                alpha = int(min(255, val * 220))
                arr[y, x] = [215, 235, 255, alpha]
    img = Image.fromarray(arr)
    img.save(output_path, "PNG")
    print(f"Generated texture: {os.path.relpath(output_path, REPO_ROOT)}")


def generate_rain_splash_texture(output_path: str):
    """Generates T_VFX_RainSplash.png: 256x256 concentric ripple waves and spray."""
    w, h = 256, 256
    arr = np.zeros((h, w, 4), dtype=np.uint8)
    cx, cy = 128, 128
    y_idx, x_idx = np.ogrid[:h, :w]
    r = np.sqrt((x_idx - cx)**2 + (y_idx - cy)**2)

    # Outer ripple ring (radius ~82 px)
    ring1 = np.exp(-((r - 82)**2) / 22.0) * 0.72
    # Inner ripple ring (radius ~44 px)
    ring2 = np.exp(-((r - 44)**2) / 16.0) * 0.88
    # Center droplet impact point (radius ~10 px)
    core = np.exp(-(r**2) / 36.0) * 1.0

    splash = np.maximum(core, np.maximum(ring1, ring2))

    # Fine spray droplets scattered radially
    rng = np.random.RandomState(42)
    for _ in range(18):
        angle = rng.uniform(0, 2 * np.pi)
        dist = rng.uniform(22, 78)
        dx = cx + dist * np.cos(angle)
        dy = cy + dist * np.sin(angle)
        drop = np.exp(-((x_idx - dx)**2 + (y_idx - dy)**2) / 5.5) * 0.75
        splash = np.maximum(splash, drop)

    splash = np.clip(splash, 0.0, 1.0)
    arr[:, :, 0] = 220
    arr[:, :, 1] = 238
    arr[:, :, 2] = 255
    arr[:, :, 3] = (splash * 235).astype(np.uint8)

    img = Image.fromarray(arr)
    img.save(output_path, "PNG")
    print(f"Generated texture: {os.path.relpath(output_path, REPO_ROOT)}")


def generate_heat_haze_texture(output_path: str):
    """Generates T_VFX_HeatHaze.png: 256x256 soft undulating warm air distortion quad."""
    w, h = 256, 256
    arr = np.zeros((h, w, 4), dtype=np.uint8)
    cx, cy = 128, 128
    y_idx, x_idx = np.ogrid[:h, :w]
    r = np.sqrt((x_idx - cx)**2 + (y_idx - cy)**2)
    theta = np.arctan2(y_idx - cy, x_idx - cx)

    # Organic thermal perturbation
    wobble = 1.0 + 0.12 * np.sin(4 * theta + 0.6) + 0.08 * np.cos(6 * theta - 1.1)
    r_eff = r / wobble
    haze = np.clip(1.0 - (r_eff / 112.0), 0.0, 1.0)
    # Smooth Hermite curve
    haze = haze * haze * (3.0 - 2.0 * haze)

    arr[:, :, 0] = 255
    arr[:, :, 1] = 246
    arr[:, :, 2] = 230
    arr[:, :, 3] = (haze * 75).astype(np.uint8)

    img = Image.fromarray(arr)
    img.save(output_path, "PNG")
    print(f"Generated texture: {os.path.relpath(output_path, REPO_ROOT)}")


def generate_electric_sparks_texture(output_path: str):
    """Generates T_VFX_ElectricSparks.png: 256x256 electrical spark burst and arc rays."""
    w, h = 256, 256
    arr = np.zeros((h, w, 4), dtype=np.uint8)
    cx, cy = 128, 128
    y_idx, x_idx = np.ogrid[:h, :w]
    r = np.sqrt((x_idx - cx)**2 + (y_idx - cy)**2)

    # Central brilliant core
    core_glow = np.exp(-(r**2) / 100.0)
    # Primary cross rays along major axes
    ray_x = np.exp(-((y_idx - cy)**2) / 2.2) * np.clip(1.0 - np.abs(x_idx - cx) / 116.0, 0.0, 1.0)
    ray_y = np.exp(-((x_idx - cx)**2) / 2.2) * np.clip(1.0 - np.abs(y_idx - cy) / 116.0, 0.0, 1.0)
    # Diagonal rays
    diag1 = np.abs((x_idx - cx) - (y_idx - cy)) / np.sqrt(2)
    diag2 = np.abs((x_idx - cx) + (y_idx - cy)) / np.sqrt(2)
    ray_d1 = np.exp(-(diag1**2) / 2.2) * np.clip(1.0 - r / 88.0, 0.0, 1.0)
    ray_d2 = np.exp(-(diag2**2) / 2.2) * np.clip(1.0 - r / 88.0, 0.0, 1.0)

    spark_total = np.clip(core_glow * 1.25 + (ray_x + ray_y) * 0.95 + (ray_d1 + ray_d2) * 0.65, 0.0, 1.0)

    # Electric cyan core transitioning to radiant white
    arr[:, :, 0] = np.clip(170 + spark_total * 85, 0, 255).astype(np.uint8)
    arr[:, :, 1] = np.clip(220 + spark_total * 35, 0, 255).astype(np.uint8)
    arr[:, :, 2] = 255
    arr[:, :, 3] = (spark_total * 255).astype(np.uint8)

    img = Image.fromarray(arr)
    img.save(output_path, "PNG")
    print(f"Generated texture: {os.path.relpath(output_path, REPO_ROOT)}")


def generate_material(mat_path: str, mat_name: str, tex_guid: str, is_additive: bool,
                      color_rgba: tuple, emission_rgb: tuple = (0, 0, 0)):
    """Writes URP ParticlesUnlit material with valid YAML formatting."""
    meta_path = mat_path + ".meta"
    mat_guid = get_or_create_guid(meta_path, f"mat_{mat_name}")
    if not os.path.exists(meta_path):
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(f"fileFormatVersion: 2\nguid: {mat_guid}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

    blend_mode = 2 if is_additive else 0
    src_blend = 5 if not is_additive else 5
    dst_blend = 1 if is_additive else 10
    dst_blend_alpha = 1 if is_additive else 10
    r, g, b, a = color_rgba
    er, eg, eb = emission_rgb

    content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {mat_name}
  m_Shader: {{fileID: 4800000, guid: {URP_PARTICLES_UNLIT_GUID}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords:
  - _SURFACE_TYPE_TRANSPARENT
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: 3000
  stringTagMap:
    RenderType: Transparent
  disabledShaderPasses:
  - DepthOnly
  - SHADOWCASTER
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _BumpMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _EmissionMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _AlphaClip: 0
    - _AlphaToMask: 0
    - _Blend: {blend_mode}
    - _BlendOp: 0
    - _BumpScale: 1
    - _CameraFadingEnabled: 0
    - _CameraFarFadeDistance: 2
    - _CameraNearFadeDistance: 1
    - _ColorMode: 0
    - _Cull: 0
    - _Cutoff: 0.5
    - _DistortionBlend: 0.5
    - _DistortionEnabled: 0
    - _DistortionStrength: 1
    - _DistortionStrengthScaled: 0.1
    - _DstBlend: {dst_blend}
    - _DstBlendAlpha: {dst_blend_alpha}
    - _FlipbookBlending: 0
    - _FlipbookMode: 0
    - _QueueOffset: 0
    - _ReceiveShadows: 0
    - _SoftParticlesEnabled: 0
    - _SoftParticlesFarFadeDistance: 1
    - _SoftParticlesNearFadeDistance: 0
    - _SrcBlend: {src_blend}
    - _SrcBlendAlpha: 1
    - _Surface: 1
    - _ZWrite: 0
    m_Colors:
    - _BaseColor: {{r: {r}, g: {g}, b: {b}, a: {a}}}
    - _BaseColorAddSubDiff: {{r: 0, g: 0, b: 0, a: 0}}
    - _CameraFadeParams: {{r: 0, g: Infinity, b: 0, a: 0}}
    - _Color: {{r: {r}, g: {g}, b: {b}, a: {a}}}
    - _EmissionColor: {{r: {er}, g: {eg}, b: {eb}, a: 1}}
    - _SoftParticleFadeParams: {{r: 0, g: 0, b: 0, a: 0}}
  m_BuildTextureStacks: []
"""
    with open(mat_path, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Generated material: {os.path.relpath(mat_path, REPO_ROOT)}")
    return mat_guid


def generate_prefab(prefab_path: str, prefab_name: str, mat_guid: str,
                    pos: tuple, rot_quat: tuple, rot_euler: tuple,
                    duration: float, looping: int, prewarm: int,
                    max_particles: int,
                    lifetime_min: float, lifetime_max: float,
                    speed_min: float, speed_max: float,
                    size_min: float, size_max: float,
                    color_min_rgba: tuple, color_max_rgba: tuple,
                    gravity: float,
                    shape_type: int, shape_scale: tuple, shape_radius: float,
                    rate_over_time: float,
                    burst_count: int, burst_time: float, burst_num: int,
                    render_mode: int, velocity_scale: float, length_scale: float,
                    enable_size_over_lifetime: bool = False,
                    enable_color_over_lifetime: bool = False):
    """
    Generates a production Unity 6 ParticleSystem prefab.
    NO Light component is attached (guaranteed mobile/WebGL budget).
    LightsModule is explicitly disabled.
    """
    meta_path = prefab_path + ".meta"
    pf_guid = get_or_create_guid(meta_path, f"pf_{prefab_name}")
    if not os.path.exists(meta_path):
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(f"fileFormatVersion: 2\nguid: {pf_guid}\nPrefabImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

    go_id = deterministic_file_id(f"{prefab_name}_GameObject")
    tr_id = deterministic_file_id(f"{prefab_name}_Transform")
    ps_id = deterministic_file_id(f"{prefab_name}_ParticleSystem")
    psr_id = deterministic_file_id(f"{prefab_name}_ParticleSystemRenderer")

    px, py, pz = pos
    rx, ry, rz, rw = rot_quat
    ex, ey, ez = rot_euler
    sx, sy, sz = shape_scale

    cmin_r, cmin_g, cmin_b, cmin_a = color_min_rgba
    cmax_r, cmax_g, cmax_b, cmax_a = color_max_rgba

    burst_block = ""
    if burst_count > 0:
        burst_block = f"""    m_BurstCount: {burst_count}
    m_Bursts:
    - serializedVersion: 2
      time: {burst_time}
      countCurve:
        serializedVersion: 3
        minMaxState: 0
        scalar: {burst_num}
        minScalar: {burst_num}
        maxCurve:
          serializedVersion: 2
          m_Curve: []
          m_PreInfinity: 2
          m_PostInfinity: 2
          m_RotationOrder: 4
        minCurve:
          serializedVersion: 2
          m_Curve: []
          m_PreInfinity: 2
          m_PostInfinity: 2
          m_RotationOrder: 4
      cycleCount: 1
      repeatInterval: 0.01
      probability: 1"""
    else:
        burst_block = """    m_BurstCount: 0
    m_Bursts: []"""

    size_module_block = ""
    if enable_size_over_lifetime:
        size_module_block = """  SizeModule:
    enabled: 1
    curve:
      serializedVersion: 3
      minMaxState: 1
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0.25
          inSlope: 1.5
          outSlope: 1.5
          tangentMode: 0
          weightedMode: 0
          inWeight: 0.33333334
          outWeight: 0.33333334
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0.4
          outSlope: 0.4
          tangentMode: 0
          weightedMode: 0
          inWeight: 0.33333334
          outWeight: 0.33333334
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    y:
      serializedVersion: 3
      minMaxState: 1
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    z:
      serializedVersion: 3
      minMaxState: 1
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    separateAxes: 0"""
    else:
        size_module_block = """  SizeModule:
    enabled: 0
    curve:
      serializedVersion: 3
      minMaxState: 1
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    y:
      serializedVersion: 3
      minMaxState: 1
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    z:
      serializedVersion: 3
      minMaxState: 1
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    separateAxes: 0"""

    color_module_block = ""
    if enable_color_over_lifetime:
        color_module_block = """  ColorModule:
    enabled: 1
    gradient:
      serializedVersion: 2
      minMaxState: 1
      minColor: {r: 1, g: 1, b: 1, a: 1}
      maxColor: {r: 1, g: 1, b: 1, a: 1}
      maxGradient:
        serializedVersion: 2
        key0: {r: 1, g: 1, b: 1, a: 0}
        key1: {r: 1, g: 1, b: 1, a: 1}
        key2: {r: 1, g: 1, b: 1, a: 0}
        key3: {r: 0, g: 0, b: 0, a: 0}
        key4: {r: 0, g: 0, b: 0, a: 0}
        key5: {r: 0, g: 0, b: 0, a: 0}
        key6: {r: 0, g: 0, b: 0, a: 0}
        key7: {r: 0, g: 0, b: 0, a: 0}
        ctime0: 0
        ctime1: 65535
        ctime2: 0
        ctime3: 0
        ctime4: 0
        ctime5: 0
        ctime6: 0
        ctime7: 0
        atime0: 0
        atime1: 16384
        atime2: 65535
        atime3: 0
        atime4: 0
        atime5: 0
        atime6: 0
        atime7: 0
        m_Mode: 0
        m_ColorSpace: -1
        m_NumColorKeys: 2
        m_NumAlphaKeys: 3
      minGradient:
        serializedVersion: 2
        key0: {r: 1, g: 1, b: 1, a: 1}
        key1: {r: 1, g: 1, b: 1, a: 1}
        key2: {r: 0, g: 0, b: 0, a: 0}
        key3: {r: 0, g: 0, b: 0, a: 0}
        key4: {r: 0, g: 0, b: 0, a: 0}
        key5: {r: 0, g: 0, b: 0, a: 0}
        key6: {r: 0, g: 0, b: 0, a: 0}
        key7: {r: 0, g: 0, b: 0, a: 0}
        ctime0: 0
        ctime1: 65535
        ctime2: 0
        ctime3: 0
        ctime4: 0
        ctime5: 0
        ctime6: 0
        ctime7: 0
        atime0: 0
        atime1: 65535
        atime2: 0
        atime3: 0
        atime4: 0
        atime5: 0
        atime6: 0
        atime7: 0
        m_Mode: 0
        m_ColorSpace: -1
        m_NumColorKeys: 2
        m_NumAlphaKeys: 2"""
    else:
        color_module_block = """  ColorModule:
    enabled: 0
    gradient:
      serializedVersion: 2
      minMaxState: 1
      minColor: {r: 1, g: 1, b: 1, a: 1}
      maxColor: {r: 1, g: 1, b: 1, a: 1}
      maxGradient:
        serializedVersion: 2
        key0: {r: 1, g: 1, b: 1, a: 1}
        key1: {r: 1, g: 1, b: 1, a: 1}
        key2: {r: 0, g: 0, b: 0, a: 0}
        key3: {r: 0, g: 0, b: 0, a: 0}
        key4: {r: 0, g: 0, b: 0, a: 0}
        key5: {r: 0, g: 0, b: 0, a: 0}
        key6: {r: 0, g: 0, b: 0, a: 0}
        key7: {r: 0, g: 0, b: 0, a: 0}
        ctime0: 0
        ctime1: 65535
        ctime2: 0
        ctime3: 0
        ctime4: 0
        ctime5: 0
        ctime6: 0
        ctime7: 0
        atime0: 0
        atime1: 65535
        atime2: 0
        atime3: 0
        atime4: 0
        atime5: 0
        atime6: 0
        atime7: 0
        m_Mode: 0
        m_ColorSpace: -1
        m_NumColorKeys: 2
        m_NumAlphaKeys: 2
      minGradient:
        serializedVersion: 2
        key0: {r: 1, g: 1, b: 1, a: 1}
        key1: {r: 1, g: 1, b: 1, a: 1}
        key2: {r: 0, g: 0, b: 0, a: 0}
        key3: {r: 0, g: 0, b: 0, a: 0}
        key4: {r: 0, g: 0, b: 0, a: 0}
        key5: {r: 0, g: 0, b: 0, a: 0}
        key6: {r: 0, g: 0, b: 0, a: 0}
        key7: {r: 0, g: 0, b: 0, a: 0}
        ctime0: 0
        ctime1: 65535
        ctime2: 0
        ctime3: 0
        ctime4: 0
        ctime5: 0
        ctime6: 0
        ctime7: 0
        atime0: 0
        atime1: 65535
        atime2: 0
        atime3: 0
        atime4: 0
        atime5: 0
        atime6: 0
        atime7: 0
        m_Mode: 0
        m_ColorSpace: -1
        m_NumColorKeys: 2
        m_NumAlphaKeys: 2"""

    prefab_content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tr_id}}}
  - component: {{fileID: {ps_id}}}
  - component: {{fileID: {psr_id}}}
  m_Layer: 0
  m_Name: {prefab_name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tr_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: {rx}, y: {ry}, z: {rz}, w: {rw}}}
  m_LocalPosition: {{x: {px}, y: {py}, z: {pz}}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: {ex}, y: {ey}, z: {ez}}}
--- !u!198 &{ps_id}
ParticleSystem:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  serializedVersion: 8
  lengthInSec: {duration}
  simulationSpeed: 1
  stopAction: 0
  cullingMode: 0
  ringBufferMode: 0
  ringBufferLoopRange: {{x: 0, y: 1}}
  emitterVelocityMode: 0
  looping: {looping}
  prewarm: {prewarm}
  playOnAwake: 1
  useUnscaledTime: 0
  autoRandomSeed: 1
  startDelay:
    serializedVersion: 3
    minMaxState: 0
    scalar: 0
    minScalar: 0
    maxCurve:
      serializedVersion: 2
      m_Curve: []
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
    minCurve:
      serializedVersion: 2
      m_Curve: []
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  moveWithTransform: 0
  moveWithCustomTransform: {{fileID: 0}}
  scalingMode: 1
  randomSeed: 0
  InitialModule:
    serializedVersion: 3
    enabled: 1
    startLifetime:
      serializedVersion: 3
      minMaxState: 3
      scalar: {lifetime_max}
      minScalar: {lifetime_min}
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    startSpeed:
      serializedVersion: 3
      minMaxState: 3
      scalar: {speed_max}
      minScalar: {speed_min}
      maxCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0.33333334
          outWeight: 0.33333334
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0.33333334
          outWeight: 0.33333334
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    startColor:
      serializedVersion: 2
      minMaxState: 0
      minColor: {{r: {cmin_r}, g: {cmin_g}, b: {cmin_b}, a: {cmin_a}}}
      maxColor: {{r: {cmax_r}, g: {cmax_g}, b: {cmax_b}, a: {cmax_a}}}
    startSize:
      serializedVersion: 3
      minMaxState: 3
      scalar: {size_max}
      minScalar: {size_min}
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    startSizeY:
      serializedVersion: 3
      minMaxState: 0
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    startSizeZ:
      serializedVersion: 3
      minMaxState: 0
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    startRotationX:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    startRotationY:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    startRotation:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    randomizeRotationDirection: 0
    gravitySource: 0
    maxNumParticles: {max_particles}
    customEmitterVelocity: {{x: 0, y: 0, z: 0}}
    size3D: 0
    rotation3D: 0
    gravityModifier:
      serializedVersion: 3
      minMaxState: 0
      scalar: {gravity}
      minScalar: {gravity}
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
  ShapeModule:
    serializedVersion: 6
    enabled: 1
    type: {shape_type}
    angle: 25
    length: 5
    boxThickness: {{x: 0, y: 0, z: 0}}
    radiusThickness: 1
    donutRadius: 0.2
    m_Position: {{x: 0, y: 0, z: 0}}
    m_Rotation: {{x: 0, y: 0, z: 0}}
    m_Scale: {{x: {sx}, y: {sy}, z: {sz}}}
    placementMode: 0
    m_MeshMaterialIndex: 0
    m_MeshNormalOffset: 0
    radius:
      value: {shape_radius}
      mode: 0
      spread: 0
    arc:
      value: 360
      mode: 0
      spread: 0
  EmissionModule:
    enabled: 1
    serializedVersion: 4
    rateOverTime:
      serializedVersion: 2
      minMaxState: 0
      scalar: {rate_over_time}
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    rateOverDistance:
      serializedVersion: 2
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
{burst_block}
{size_module_block}
  RotationModule:
    enabled: 0
    x:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    y:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    curve:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0.7853982
      minScalar: 0.7853982
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    separateAxes: 0
{color_module_block}
  UVModule:
    enabled: 0
    mode: 0
    timeMode: 0
    fps: 30
    frameOverTime:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    startFrame:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    speedRange: {{x: 0, y: 1}}
    tilesX: 1
    tilesY: 1
    animationType: 0
    rowIndex: 0
    cycles: 1
    uvChannelMask: -1
    flipU: 0
    flipV: 0
    randomRow: 1
  VelocityModule:
    enabled: 0
    x:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    y:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    z:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    space: 1
  InheritVelocityModule:
    enabled: 0
    m_Mode: 0
    m_Curve:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
  LifetimeByEmitterSpeedModule:
    enabled: 0
    curve:
      serializedVersion: 3
      minMaxState: 0
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    range: {{x: 0, y: 1}}
  ForceModule:
    enabled: 0
    x:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    y:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    z:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    space: 0
    randomize: 0
  ExternalForcesModule:
    enabled: 0
    multiplierCurve:
      serializedVersion: 3
      minMaxState: 0
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    influenceFilter: 0
    influenceMask:
      serializedVersion: 2
      m_Bits: 4294967295
  ClampVelocityModule:
    enabled: 0
    x:
      serializedVersion: 3
      minMaxState: 0
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    y:
      serializedVersion: 3
      minMaxState: 0
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    z:
      serializedVersion: 3
      minMaxState: 0
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    magnitude:
      serializedVersion: 3
      minMaxState: 0
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    separateAxis: 0
    inWorldSpace: 0
    dampen: 1
  NoiseModule:
    enabled: 0
    strength:
      serializedVersion: 3
      minMaxState: 0
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    frequency: 0.5
    damping: 1
    octaves: 1
    octaveMultiplier: 0.5
    octaveScale: 2
    quality: 1
    scrollSpeed:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    remap:
      serializedVersion: 3
      minMaxState: 1
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    remapEnabled: 0
  SizeBySpeedModule:
    enabled: 0
    curve:
      serializedVersion: 3
      minMaxState: 1
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    range: {{x: 0, y: 1}}
  RotationBySpeedModule:
    enabled: 0
    curve:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0.7853982
      minScalar: 0.7853982
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    range: {{x: 0, y: 1}}
  ColorBySpeedModule:
    enabled: 0
    gradient:
      serializedVersion: 2
      minMaxState: 1
      minColor: {{r: 1, g: 1, b: 1, a: 1}}
      maxColor: {{r: 1, g: 1, b: 1, a: 1}}
      maxGradient:
        serializedVersion: 2
        key0: {{r: 1, g: 1, b: 1, a: 1}}
        key1: {{r: 1, g: 1, b: 1, a: 1}}
        key2: {{r: 0, g: 0, b: 0, a: 0}}
        key3: {{r: 0, g: 0, b: 0, a: 0}}
        key4: {{r: 0, g: 0, b: 0, a: 0}}
        key5: {{r: 0, g: 0, b: 0, a: 0}}
        key6: {{r: 0, g: 0, b: 0, a: 0}}
        key7: {{r: 0, g: 0, b: 0, a: 0}}
        ctime0: 0
        ctime1: 65535
        ctime2: 0
        ctime3: 0
        ctime4: 0
        ctime5: 0
        ctime6: 0
        ctime7: 0
        atime0: 0
        atime1: 65535
        atime2: 0
        atime3: 0
        atime4: 0
        atime5: 0
        atime6: 0
        atime7: 0
        m_Mode: 0
        m_ColorSpace: -1
        m_NumColorKeys: 2
        m_NumAlphaKeys: 2
      minGradient:
        serializedVersion: 2
        key0: {{r: 1, g: 1, b: 1, a: 1}}
        key1: {{r: 1, g: 1, b: 1, a: 1}}
        key2: {{r: 0, g: 0, b: 0, a: 0}}
        key3: {{r: 0, g: 0, b: 0, a: 0}}
        key4: {{r: 0, g: 0, b: 0, a: 0}}
        key5: {{r: 0, g: 0, b: 0, a: 0}}
        key6: {{r: 0, g: 0, b: 0, a: 0}}
        key7: {{r: 0, g: 0, b: 0, a: 0}}
        ctime0: 0
        ctime1: 65535
        ctime2: 0
        ctime3: 0
        ctime4: 0
        ctime5: 0
        ctime6: 0
        ctime7: 0
        atime0: 0
        atime1: 65535
        atime2: 0
        atime3: 0
        atime4: 0
        atime5: 0
        atime6: 0
        atime7: 0
        m_Mode: 0
        m_ColorSpace: -1
        m_NumColorKeys: 2
        m_NumAlphaKeys: 2
    range: {{x: 0, y: 1}}
  CollisionModule:
    enabled: 0
    serializedVersion: 3
    type: 0
    collisionMode: 0
    colliderForce: 0
    multiplyColliderForceByParticleSize: 0
    multiplyColliderForceByParticleSpeed: 0
    multiplyColliderForceByCollisionAngle: 0
    plane0: {{fileID: 0}}
    plane1: {{fileID: 0}}
    plane2: {{fileID: 0}}
    plane3: {{fileID: 0}}
    plane4: {{fileID: 0}}
    plane5: {{fileID: 0}}
    m_Dampen:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    m_Bounce:
      serializedVersion: 3
      minMaxState: 0
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    m_EnergyLossOnCollision:
      serializedVersion: 3
      minMaxState: 0
      scalar: 0
      minScalar: 0
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    minKillSpeed: 0
    maxKillSpeed: 10000
    radiusScale: 1
    collidesWith:
      serializedVersion: 2
      m_Bits: 4294967295
    maxCollisionShapes: 256
    quality: 0
    voxelSize: 0.5
    collisionMessages: 0
    collidesWithDynamic: 1
    interiorCollisions: 0
  TriggerModule:
    enabled: 0
    collisionShape0: {{fileID: 0}}
    collisionShape1: {{fileID: 0}}
    collisionShape2: {{fileID: 0}}
    collisionShape3: {{fileID: 0}}
    collisionShape4: {{fileID: 0}}
    collisionShape5: {{fileID: 0}}
    inside: 1
    outside: 0
    enter: 0
    exit: 0
    radiusScale: 1
  SubModule:
    enabled: 0
    subEmitterCount: 0
  LightsModule:
    enabled: 0
    ratio: 0
    light: {{fileID: 0}}
    randomDistribution: 0
    color: 0
    range: 0
    intensity: 0
    rangeCurve:
      serializedVersion: 3
      minMaxState: 0
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
  TrailModule:
    enabled: 0
    mode: 0
    ratio: 1
    lifetime:
      serializedVersion: 3
      minMaxState: 0
      scalar: 1
      minScalar: 1
      maxCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      minCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    minVertexDistance: 0.2
    textureMode: 0
    worldSpace: 0
    dieWithParticles: 1
    sizeAffectsWidth: 1
    sizeAffectsLifetime: 0
    inheritParticleColor: 1
    colorOverLifetime:
      serializedVersion: 2
      minMaxState: 0
      minColor: {{r: 1, g: 1, b: 1, a: 1}}
      maxColor: {{r: 1, g: 1, b: 1, a: 1}}
      maxGradient:
        serializedVersion: 2
        key0: {{r: 1, g: 1, b: 1, a: 1}}
        key1: {{r: 1, g: 1, b: 1, a: 1}}
        key2: {{r: 0, g: 0, b: 0, a: 0}}
        key3: {{r: 0, g: 0, b: 0, a: 0}}
        key4: {{r: 0, g: 0, b: 0, a: 0}}
        key5: {{r: 0, g: 0, b: 0, a: 0}}
        key6: {{r: 0, g: 0, b: 0, a: 0}}
        key7: {{r: 0, g: 0, b: 0, a: 0}}
        ctime0: 0
        ctime1: 65535
        ctime2: 0
        ctime3: 0
        ctime4: 0
        ctime5: 0
        ctime6: 0
        ctime7: 0
        atime0: 0
        atime1: 65535
        atime2: 0
        atime3: 0
        atime4: 0
        atime5: 0
        atime6: 0
        atime7: 0
        m_Mode: 0
        m_ColorSpace: -1
        m_NumColorKeys: 2
        m_NumAlphaKeys: 2
      minGradient:
        serializedVersion: 2
        key0: {{r: 1, g: 1, b: 1, a: 1}}
        key1: {{r: 1, g: 1, b: 1, a: 1}}
        key2: {{r: 0, g: 0, b: 0, a: 0}}
        key3: {{r: 0, g: 0, b: 0, a: 0}}
        key4: {{r: 0, g: 0, b: 0, a: 0}}
        key5: {{r: 0, g: 0, b: 0, a: 0}}
        key6: {{r: 0, g: 0, b: 0, a: 0}}
        key7: {{r: 0, g: 0, b: 0, a: 0}}
        ctime0: 0
        ctime1: 65535
        ctime2: 0
        ctime3: 0
        ctime4: 0
        ctime5: 0
        ctime6: 0
        ctime7: 0
        atime0: 0
        atime1: 65535
        atime2: 0
        atime3: 0
        atime4: 0
        atime5: 0
        atime6: 0
        atime7: 0
        m_Mode: 0
        m_ColorSpace: -1
        m_NumColorKeys: 2
        m_NumAlphaKeys: 2
    widthMultiplier: 1
  CustomDataModule:
    enabled: 0
    mode0: 0
    vectorComponentCount0: 4
    color0:
      serializedVersion: 2
      minMaxState: 0
      minColor: {{r: 1, g: 1, b: 1, a: 1}}
      maxColor: {{r: 1, g: 1, b: 1, a: 1}}
--- !u!199 &{psr_id}
ParticleSystemRenderer:
  serializedVersion: 7
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 0
  m_ReflectionProbeUsage: 0
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
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
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_RenderMode: {render_mode}
  m_MeshDistribution: 0
  m_SortMode: 0
  m_MinParticleSize: 0
  m_MaxParticleSize: 0.5
  m_CameraVelocityScale: 0
  m_VelocityScale: {velocity_scale}
  m_LengthScale: {length_scale}
  m_SortingFudge: 0
  m_NormalDirection: 1
  m_ShadowBias: 0
  m_RenderAlignment: 0
  m_Pivot: {{x: 0, y: 0, z: 0}}
  m_Flip: {{x: 0, y: 0, z: 0}}
  m_EnableGPUInstancing: 1
  m_ApplyActiveColorSpace: 1
  m_AllowRoll: 1
  m_FreeformStretching: 0
  m_RotateWithStretchDirection: 1
  m_UseCustomVertexStreams: 0
  m_VertexStreams: 00010304
  m_UseCustomTrailVertexStreams: 0
  m_TrailVertexStreams: 00010304
  m_Mesh: {{fileID: 0}}
  m_Mesh1: {{fileID: 0}}
  m_Mesh2: {{fileID: 0}}
  m_Mesh3: {{fileID: 0}}
  m_MeshWeighting: 1
  m_MeshWeighting1: 1
  m_MeshWeighting2: 1
  m_MeshWeighting3: 1
"""
    with open(prefab_path, "w", encoding="utf-8") as f:
        f.write(prefab_content)
    print(f"Generated prefab: {os.path.relpath(prefab_path, REPO_ROOT)}")


def main():
    print("=== Generating Weather and Event VFX Assets (ART-FIX-028) ===")

    # 1. Ensure folder structures and folder .meta files exist
    ensure_folder_meta(TEX_DIR, "folder_art_textures_vfx")
    ensure_folder_meta(MAT_DIR, "folder_art_materials_vfx")
    ensure_folder_meta(PREFAB_DIR, "folder_prefabs_vfx")

    # 2. Generate procedural textures
    tex_rain_streak = os.path.join(TEX_DIR, "T_VFX_RainStreak.png")
    tex_rain_splash = os.path.join(TEX_DIR, "T_VFX_RainSplash.png")
    tex_heat_haze = os.path.join(TEX_DIR, "T_VFX_HeatHaze.png")
    tex_sparks = os.path.join(TEX_DIR, "T_VFX_ElectricSparks.png")

    generate_rain_streak_texture(tex_rain_streak)
    generate_rain_splash_texture(tex_rain_splash)
    generate_heat_haze_texture(tex_heat_haze)
    generate_electric_sparks_texture(tex_sparks)

    # 3. Write Texture Meta files
    rain_streak_tex_guid = get_or_create_guid(tex_rain_streak + ".meta", "tex_T_VFX_RainStreak")
    rain_splash_tex_guid = get_or_create_guid(tex_rain_splash + ".meta", "tex_T_VFX_RainSplash")
    heat_haze_tex_guid = get_or_create_guid(tex_heat_haze + ".meta", "tex_T_VFX_HeatHaze")
    sparks_tex_guid = get_or_create_guid(tex_sparks + ".meta", "tex_T_VFX_ElectricSparks")

    write_texture_meta(tex_rain_streak + ".meta", rain_streak_tex_guid)
    write_texture_meta(tex_rain_splash + ".meta", rain_splash_tex_guid)
    write_texture_meta(tex_heat_haze + ".meta", heat_haze_tex_guid)
    write_texture_meta(tex_sparks + ".meta", sparks_tex_guid)

    # 4. Generate Materials
    mat_rain_streak_path = os.path.join(MAT_DIR, "MAT_VFX_RainStreak.mat")
    mat_rain_splash_path = os.path.join(MAT_DIR, "MAT_VFX_RainSplash.mat")
    mat_heat_haze_path = os.path.join(MAT_DIR, "MAT_VFX_HeatHaze.mat")
    mat_sparks_path = os.path.join(MAT_DIR, "MAT_VFX_ElectricSparks.mat")

    mat_rain_streak_guid = generate_material(
        mat_rain_streak_path, "MAT_VFX_RainStreak", rain_streak_tex_guid,
        is_additive=False, color_rgba=(0.85, 0.92, 1.0, 0.75)
    )
    mat_rain_splash_guid = generate_material(
        mat_rain_splash_path, "MAT_VFX_RainSplash", rain_splash_tex_guid,
        is_additive=False, color_rgba=(0.88, 0.94, 1.0, 0.8)
    )
    mat_heat_haze_guid = generate_material(
        mat_heat_haze_path, "MAT_VFX_HeatHaze", heat_haze_tex_guid,
        is_additive=False, color_rgba=(1.0, 0.96, 0.88, 0.22)
    )
    mat_sparks_guid = generate_material(
        mat_sparks_path, "MAT_VFX_ElectricSparks", sparks_tex_guid,
        is_additive=True, color_rgba=(0.85, 0.95, 1.0, 1.0),
        emission_rgb=(2.0, 2.5, 3.0)
    )

    # 5. Generate Particle System Prefabs
    # (a) PF_VFX_Rain.prefab (Budget <= 500 max particles)
    # Box emission 15m x 15m falling downward with subtle angle
    generate_prefab(
        prefab_path=os.path.join(PREFAB_DIR, "PF_VFX_Rain.prefab"),
        prefab_name="PF_VFX_Rain",
        mat_guid=mat_rain_streak_guid,
        pos=(0, 7.5, 0),
        rot_quat=(0.652876, 0.117184, -0.132896, 0.736413),
        rot_euler=(82, 15, 0),
        duration=5.0, looping=1, prewarm=1,
        max_particles=450,
        lifetime_min=0.55, lifetime_max=0.75,
        speed_min=10.0, speed_max=13.0,
        size_min=0.02, size_max=0.04,
        color_min_rgba=(0.75, 0.85, 0.95, 0.45), color_max_rgba=(0.85, 0.92, 1.0, 0.7),
        gravity=1.0,
        shape_type=5, shape_scale=(15, 15, 1), shape_radius=1.0,
        rate_over_time=320,
        burst_count=0, burst_time=0, burst_num=0,
        render_mode=1, velocity_scale=-0.045, length_scale=2.2,
        enable_size_over_lifetime=False,
        enable_color_over_lifetime=False
    )

    # (b) PF_VFX_RainSplash.prefab (Budget <= 300 max particles)
    # Concentric ripples / droplet bounces on pavement
    generate_prefab(
        prefab_path=os.path.join(PREFAB_DIR, "PF_VFX_RainSplash.prefab"),
        prefab_name="PF_VFX_RainSplash",
        mat_guid=mat_rain_splash_guid,
        pos=(0, 0.02, 0),
        rot_quat=(-0.7071068, 0, 0, 0.7071068),
        rot_euler=(-90, 0, 0),
        duration=5.0, looping=1, prewarm=1,
        max_particles=250,
        lifetime_min=0.22, lifetime_max=0.4,
        speed_min=0.0, speed_max=0.1,
        size_min=0.25, size_max=0.45,
        color_min_rgba=(0.78, 0.88, 0.98, 0.45), color_max_rgba=(0.88, 0.94, 1.0, 0.75),
        gravity=0.0,
        shape_type=5, shape_scale=(14, 14, 0.05), shape_radius=1.0,
        rate_over_time=130,
        burst_count=0, burst_time=0, burst_num=0,
        render_mode=2, velocity_scale=0, length_scale=2.0,
        enable_size_over_lifetime=True,
        enable_color_over_lifetime=False
    )

    # (c) PF_VFX_HeatHaze.prefab (Budget <= 200 max particles)
    # Gentle rising warm air distortion quads above hot street asphalt
    generate_prefab(
        prefab_path=os.path.join(PREFAB_DIR, "PF_VFX_HeatHaze.prefab"),
        prefab_name="PF_VFX_HeatHaze",
        mat_guid=mat_heat_haze_guid,
        pos=(0, 0.15, 0),
        rot_quat=(-0.7071068, 0, 0, 0.7071068),
        rot_euler=(-90, 0, 0),
        duration=5.0, looping=1, prewarm=1,
        max_particles=150,
        lifetime_min=1.8, lifetime_max=2.8,
        speed_min=0.25, speed_max=0.45,
        size_min=0.8, size_max=1.3,
        color_min_rgba=(0.95, 0.92, 0.85, 0.12), color_max_rgba=(1.0, 0.96, 0.88, 0.22),
        gravity=-0.04,
        shape_type=5, shape_scale=(12, 12, 0.2), shape_radius=1.0,
        rate_over_time=26,
        burst_count=0, burst_time=0, burst_num=0,
        render_mode=0, velocity_scale=0, length_scale=2.0,
        enable_size_over_lifetime=True,
        enable_color_over_lifetime=True
    )

    # (d) PF_VFX_Sparks_Blackout.prefab (Budget <= 150 max particles)
    # Brief burst of electrical sparks and smoke when power grid cuts out
    generate_prefab(
        prefab_path=os.path.join(PREFAB_DIR, "PF_VFX_Sparks_Blackout.prefab"),
        prefab_name="PF_VFX_Sparks_Blackout",
        mat_guid=mat_sparks_guid,
        pos=(0, 3.5, 0),
        rot_quat=(0, 0, 0, 1),
        rot_euler=(0, 0, 0),
        duration=1.5, looping=0, prewarm=0,
        max_particles=120,
        lifetime_min=0.18, lifetime_max=0.55,
        speed_min=2.5, speed_max=6.5,
        size_min=0.04, size_max=0.12,
        color_min_rgba=(1.0, 0.65, 0.25, 1.0), color_max_rgba=(0.8, 0.95, 1.0, 1.0),
        gravity=0.85,
        shape_type=0, shape_scale=(1, 1, 1), shape_radius=0.25,
        rate_over_time=0,
        burst_count=1, burst_time=0, burst_num=75,
        render_mode=1, velocity_scale=0.035, length_scale=1.8,
        enable_size_over_lifetime=False,
        enable_color_over_lifetime=False
    )

    print("=== VFX Asset Generation Complete ===")


if __name__ == "__main__":
    main()
