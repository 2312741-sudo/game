#!/usr/bin/env python3
"""
Generates high-resolution PBR textures for the Tram Chanh stall:
- T_Wood_DarkCounter_BaseColor.png, Normal, MaskMap (2048x2048)
- T_CorrugatedMetal_BaseColor.png, Normal, MaskMap (2048x2048)
- T_DarkFrame_BaseColor.png (1024x1024)
"""

import os
import numpy as np
from PIL import Image, ImageFilter

def compute_normal_map(height_map, strength=2.0):
    """Computes a tangent-space normal map from a grayscale height map using Sobel filters."""
    h = height_map.astype(float) / 255.0
    
    # Sobel kernels
    # dx: right - left
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5
    # dy: down - up (in image space, y goes down)
    dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * 0.5
    
    nx = -dx * strength
    ny = -dy * strength
    nz = np.ones_like(h)
    
    length = np.sqrt(nx**2 + ny**2 + nz**2)
    nx /= length
    ny /= length
    nz /= length
    
    # Map [-1, 1] to [0, 255]
    r = ((nx * 0.5 + 0.5) * 255).astype(np.uint8)
    g = (((-ny) * 0.5 + 0.5) * 255).astype(np.uint8) # Unity green channel (Y up)
    b = ((nz * 0.5 + 0.5) * 255).astype(np.uint8)
    
    return np.stack([r, g, b], axis=-1)

def generate_wood_textures(output_dir, size=2048):
    print("Generating Organic Wood Countertop PBR textures...")
    np.random.seed(42)
    
    # Coordinates
    x = np.linspace(0, 1, size)
    y = np.linspace(0, 1, size)
    xx, yy = np.meshgrid(x, y)
    
    # 5 distinct horizontal planks
    num_planks = 5
    plank_idx = (yy * num_planks).astype(int)
    plank_idx = np.clip(plank_idx, 0, num_planks - 1)
    
    # Per-plank tone offset
    plank_tones = np.array([0.92, 1.05, 0.96, 1.02, 0.94])
    tone_map = plank_tones[plank_idx]
    
    # Organic grain: dominant horizontal flow with high-frequency fibers and subtle wavy flow
    distortion = np.sin(xx * 6.0 + plank_idx * 1.5) * 0.02 + np.cos(xx * 14.0) * 0.008
    y_distorted = yy + distortion
    
    # Fine wood fibers running horizontally (high frequency across Y, slow across X)
    fiber_freq = 420.0
    fine_fibers = np.sin(y_distorted * fiber_freq + np.sin(xx * 18.0) * 2.0)
    fine_fibers += 0.5 * np.sin(y_distorted * fiber_freq * 2.3 + np.cos(xx * 30.0) * 3.0)
    fine_fibers = (fine_fibers * 0.5 + 0.5)
    
    # Broad growth rings (lower frequency)
    growth_rings = np.sin(y_distorted * 60.0 + np.sin(xx * 8.0) * 3.5) * 0.5 + 0.5
    growth_rings = growth_rings ** 1.8
    
    # Micro noise / porosity
    porosity = np.random.normal(0, 0.06, (size, size))
    
    # Composite wood grain
    grain = (0.5 * fine_fibers + 0.35 * growth_rings + 0.15 * porosity) * tone_map
    grain = np.clip(grain, 0.0, 1.0)
    
    # Plank seams: dark grooves between planks
    seam_mask = np.zeros((size, size))
    plank_h = size / num_planks
    for p in range(1, num_planks):
        sy = int(p * plank_h)
        for d in range(-4, 5):
            if 0 <= sy + d < size:
                seam_mask[sy + d, :] = 1.0 - abs(d) / 5.0
                
    # Rich dark espresso stained timber (#281C14 to #452E20)
    base_r = (38.0 + grain * 28.0) * (1.0 - seam_mask * 0.65)
    base_g = (25.0 + grain * 18.0) * (1.0 - seam_mask * 0.70)
    base_b = (18.0 + grain * 12.0) * (1.0 - seam_mask * 0.75)
    
    base_rgb = np.stack([
        np.clip(base_r, 10, 80).astype(np.uint8),
        np.clip(base_g, 7, 56).astype(np.uint8),
        np.clip(base_b, 5, 40).astype(np.uint8)
    ], axis=-1)
    
    Image.fromarray(base_rgb).save(os.path.join(output_dir, "T_Wood_DarkCounter_BaseColor.png"))
    
    # Height map for normal calculation
    height_map = ((grain * 0.6 + (1.0 - seam_mask) * 0.4) * 255).astype(np.uint8)
    normal_map = compute_normal_map(height_map, strength=1.2)
    Image.fromarray(normal_map).save(os.path.join(output_dir, "T_Wood_DarkCounter_Normal.png"))
    
    # URP Mask Map: R=Metallic (0), G=Occlusion, B=Detail, A=Smoothness (0.35-0.5)
    r_chan = np.zeros((size, size), dtype=np.uint8)
    g_chan = np.clip((1.0 - seam_mask * 0.6) * 255, 100, 255).astype(np.uint8)
    b_chan = np.zeros((size, size), dtype=np.uint8)
    a_chan = np.clip((0.38 + grain * 0.12 - seam_mask * 0.2) * 255, 60, 160).astype(np.uint8)
    
    mask_map = np.stack([r_chan, g_chan, b_chan, a_chan], axis=-1)
    Image.fromarray(mask_map, mode="RGBA").save(os.path.join(output_dir, "T_Wood_DarkCounter_MaskMap.png"))
    print("Organic Wood Countertop PBR textures generated.")


def generate_metal_textures(output_dir, size=2048):
    print("Generating Corrugated Metal PBR textures...")
    np.random.seed(101)
    
    # Fine metallic noise and subtle vertical brush / roller lines
    noise = np.random.normal(0, 0.04, (size, size))
    y_grad = np.linspace(0, 1, size)[:, None]
    
    # Charcoal / gunmetal painted sheet metal (#2A2E32 to #343A40)
    base_val = 48.0 + noise * 40.0
    base_r = base_val * 0.92
    base_g = base_val * 0.98
    base_b = base_val * 1.04
    
    base_rgb = np.stack([
        np.clip(base_r, 20, 75).astype(np.uint8),
        np.clip(base_g, 22, 80).astype(np.uint8),
        np.clip(base_b, 24, 88).astype(np.uint8)
    ], axis=-1)
    
    Image.fromarray(base_rgb).save(os.path.join(output_dir, "T_CorrugatedMetal_BaseColor.png"))
    
    # Normal map: subtle paint orange-peel / rolling micro-texture
    height_map = ((noise + 0.5) * 255).astype(np.uint8)
    normal_map = compute_normal_map(height_map, strength=0.8)
    Image.fromarray(normal_map).save(os.path.join(output_dir, "T_CorrugatedMetal_Normal.png"))
    
    # URP Mask Map: R=Metallic (0.45), G=Occlusion (0.95), B=Detail (0), A=Smoothness (0.45)
    r_chan = np.full((size, size), int(0.45 * 255), dtype=np.uint8) # Coated semi-metallic
    g_chan = np.full((size, size), int(0.95 * 255), dtype=np.uint8) # Occlusion
    b_chan = np.zeros((size, size), dtype=np.uint8)
    a_chan = np.clip((0.42 + noise * 0.1) * 255, 80, 160).astype(np.uint8) # Smoothness
    
    mask_map = np.stack([r_chan, g_chan, b_chan, a_chan], axis=-1)
    Image.fromarray(mask_map, mode="RGBA").save(os.path.join(output_dir, "T_CorrugatedMetal_MaskMap.png"))
    print("Corrugated Metal PBR textures generated.")

def generate_frame_textures(output_dir, size=1024):
    print("Generating Dark Frame PBR textures...")
    np.random.seed(333)
    # Dark structural timber / painted metal (#222426)
    noise = np.random.normal(0, 0.03, (size, size))
    val = np.clip((0.14 + noise) * 255, 20, 60).astype(np.uint8)
    frame_rgb = np.stack([val, (val * 1.02).astype(np.uint8), (val * 1.05).astype(np.uint8)], axis=-1)
    Image.fromarray(frame_rgb).save(os.path.join(output_dir, "T_DarkFrame_BaseColor.png"))
    print("Dark Frame textures generated.")

if __name__ == "__main__":
    out_dir = "Assets/TramChanh/Art/Textures"
    generate_wood_textures(out_dir)
    generate_metal_textures(out_dir)
    generate_frame_textures(out_dir)
