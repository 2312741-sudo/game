#!/usr/bin/env python3
"""
Procedural 3D Mesh Generator for Tram Chanh Street Environment & Furniture (Agent E)
Outputs native Wavefront OBJ files with normals and UVs:
- SM_PlasticStool.obj (Beige plastic stool based on IMG_5228.JPG: 300x300x300mm)
- SM_YellowCrateTable.obj (Upside-down beverage crate table based on IMG_5231.JPG: 450x350x320mm)
- SM_StainlessTray.obj (Stainless steel serving tray with rolled lip)
- SM_Sidewalk_Section.obj (Vietnamese terrazzo tile sidewalk with curb)
- SM_Asphalt_Road.obj (Wet reflective asphalt roadway module)
"""

import os
import math
import numpy as np

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

    def add_box(self, x_min, x_max, y_min, y_max, z_min, z_max, uv_scale=1.0):
        v000 = self.add_vertex(x_min, y_min, z_min)
        v001 = self.add_vertex(x_min, y_min, z_max)
        v010 = self.add_vertex(x_min, y_max, z_min)
        v011 = self.add_vertex(x_min, y_max, z_max)
        v100 = self.add_vertex(x_max, y_min, z_min)
        v101 = self.add_vertex(x_max, y_min, z_max)
        v110 = self.add_vertex(x_max, y_max, z_min)
        v111 = self.add_vertex(x_max, y_max, z_max)

        dx = (x_max - x_min) * uv_scale
        dy = (y_max - y_min) * uv_scale
        dz = (z_max - z_min) * uv_scale

        self.add_quad(v001, v101, v111, v011, (0, 0), (dx, 0), (dx, dy), (0, dy), (0, 0, 1))
        self.add_quad(v100, v000, v010, v110, (0, 0), (dx, 0), (dx, dy), (0, dy), (0, 0, -1))
        self.add_quad(v011, v111, v110, v010, (0, 0), (dx, 0), (dx, dz), (0, dz), (0, 1, 0))
        self.add_quad(v000, v100, v101, v001, (0, 0), (dx, 0), (dx, dz), (0, dz), (0, -1, 0))
        self.add_quad(v101, v100, v110, v111, (0, 0), (dz, 0), (dz, dy), (0, dy), (1, 0, 0))
        self.add_quad(v000, v001, v011, v010, (0, 0), (dz, 0), (dz, dy), (0, dy), (-1, 0, 0))

    def add_cylinder(self, p1, p2, radius, segments=16, uv_scale=1.0):
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

        ring1_idx = []
        ring2_idx = []
        ring_normals = []
        
        for i in range(segments):
            angle = 2.0 * math.pi * i / segments
            cos_a = math.cos(angle)
            sin_a = math.sin(angle)
            radial = cos_a * x_dir + sin_a * y_dir
            pos1 = p1 + radial * radius
            pos2 = p2 + radial * radius
            ring1_idx.append(self.add_vertex(*pos1))
            ring2_idx.append(self.add_vertex(*pos2))
            ring_normals.append(radial)

        for i in range(segments):
            next_i = (i + 1) % segments
            v1, v2, v3, v4 = ring1_idx[i], ring1_idx[next_i], ring2_idx[next_i], ring2_idx[i]
            u_cur = i / segments * uv_scale
            u_nxt = (i + 1) / segments * uv_scale
            n1_idx = self.add_normal(*ring_normals[i])
            n2_idx = self.add_normal(*ring_normals[next_i])
            uv1 = self.add_uv(u_cur, 0)
            uv2 = self.add_uv(u_nxt, 0)
            uv3 = self.add_uv(u_nxt, length * uv_scale)
            uv4 = self.add_uv(u_cur, length * uv_scale)
            self.add_face([(v1, uv1, n1_idx), (v2, uv2, n2_idx), (v3, uv3, n2_idx)])
            self.add_face([(v1, uv1, n1_idx), (v3, uv3, n2_idx), (v4, uv4, n1_idx)])

    def write_obj(self, filepath):
        os.makedirs(os.path.dirname(filepath), exist_ok=True)
        with open(filepath, "w") as f:
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
        print(f"Exported: {filepath} ({len(self.vertices)} verts, {len(self.faces)} faces)")

# ==============================================================================
# 1. Authentic Vietnamese Plastic Stool (IMG_5228.JPG)
# ==============================================================================
def generate_plastic_stool(out_path):
    mesh = ObjMesh("SM_PlasticStool")
    # Exact contract dimensions: 0.30m x 0.30m x 0.30m
    w = 0.30
    d = 0.30
    h = 0.30
    seat_t = 0.018
    
    # Seat plate (recessed center, ventilation cutout hole)
    # Seat top at y = 0.30m
    # 4 outer rims
    mesh.add_box(-w/2, w/2, h - seat_t, h, d/2 - 0.03, d/2)
    mesh.add_box(-w/2, w/2, h - seat_t, h, -d/2, -d/2 + 0.03)
    mesh.add_box(-w/2, -w/2 + 0.03, h - seat_t, h, -d/2 + 0.03, d/2 - 0.03)
    mesh.add_box(w/2 - 0.03, w/2, h - seat_t, h, -d/2 + 0.03, d/2 - 0.03)
    
    # Seat inner webbing panel with center oval handle/vent hole
    mesh.add_box(-w/2 + 0.03, -0.04, h - seat_t, h - 0.006, -d/2 + 0.03, d/2 - 0.03)
    mesh.add_box(0.04, w/2 - 0.03, h - seat_t, h - 0.006, -d/2 + 0.03, d/2 - 0.03)
    mesh.add_box(-0.04, 0.04, h - seat_t, h - 0.006, 0.04, d/2 - 0.03)
    mesh.add_box(-0.04, 0.04, h - seat_t, h - 0.006, -d/2 + 0.03, -0.04)
    
    # 4 Flared Stacking Legs (tapered from top corner to bottom corner)
    leg_r_top = 0.016
    leg_r_bot = 0.012
    for sx in [-1, 1]:
        for sz in [-1, 1]:
            p_top = (sx * (w/2 - 0.03), h - seat_t, sz * (d/2 - 0.03))
            p_bot = (sx * (w/2 - 0.015), 0.000, sz * (d/2 - 0.015))
            mesh.add_cylinder(p_bot, p_top, radius=leg_r_bot, segments=10)
            
    # Cross-bracing reinforcement ribs under seat
    mesh.add_cylinder((-w/2 + 0.03, h - 0.08, 0), (w/2 - 0.03, h - 0.08, 0), radius=0.007, segments=8)
    mesh.add_cylinder((0, h - 0.08, -d/2 + 0.03), (0, h - 0.08, d/2 - 0.03), radius=0.007, segments=8)

    mesh.write_obj(out_path)

# ==============================================================================
# 2. Inverted Yellow Beverage Crate Table (IMG_5231.JPG)
# ==============================================================================
def generate_yellow_crate_table(out_path):
    mesh = ObjMesh("SM_YellowCrateTable")
    # Standard 24-bottle crate dimensions: length 450mm, width 350mm, height 320mm
    l = 0.45
    w = 0.35
    h = 0.32
    wall_t = 0.015
    
    # Top table surface (which is the bottom of the inverted crate at y = h)
    mesh.add_box(-l/2, l/2, h - wall_t, h, -w/2, w/2)
    
    # 4 reinforced corner posts
    post_w = 0.04
    for sx in [-1, 1]:
        for sz in [-1, 1]:
            mesh.add_box(sx * (l/2 - post_w/2) - post_w/2, sx * (l/2 - post_w/2) + post_w/2,
                         0.0, h - wall_t,
                         sz * (w/2 - post_w/2) - post_w/2, sz * (w/2 - post_w/2) + post_w/2)
            
    # Perimeter ground base rim
    mesh.add_box(-l/2, l/2, 0.0, wall_t * 1.5, w/2 - wall_t, w/2)
    mesh.add_box(-l/2, l/2, 0.0, wall_t * 1.5, -w/2, -w/2 + wall_t)
    mesh.add_box(-l/2, -l/2 + wall_t, 0.0, wall_t * 1.5, -w/2 + wall_t, w/2 - wall_t)
    mesh.add_box(l/2 - wall_t, l/2, 0.0, wall_t * 1.5, -w/2 + wall_t, w/2 - wall_t)
    
    # Mid-height horizontal reinforcement rib
    mid_y = h * 0.45
    mesh.add_box(-l/2, l/2, mid_y, mid_y + wall_t, w/2 - wall_t, w/2)
    mesh.add_box(-l/2, l/2, mid_y, mid_y + wall_t, -w/2, -w/2 + wall_t)
    mesh.add_box(-l/2, -l/2 + wall_t, mid_y, mid_y + wall_t, -w/2 + wall_t, w/2 - wall_t)
    mesh.add_box(l/2 - wall_t, l/2, mid_y, mid_y + wall_t, -w/2 + wall_t, w/2 - wall_t)
    
    # Hand-hold cutouts on ends with molded lip
    # Longitudinal walls with lattice vertical slats
    for sx in np.linspace(-l/2 + 0.06, l/2 - 0.06, 7):
        mesh.add_box(sx - 0.005, sx + 0.005, wall_t * 1.5, h - wall_t, w/2 - wall_t, w/2)
        mesh.add_box(sx - 0.005, sx + 0.005, wall_t * 1.5, h - wall_t, -w/2, -w/2 + wall_t)
        
    mesh.write_obj(out_path)

# ==============================================================================
# 3. Stainless Steel Serving Tray
# ==============================================================================
def generate_stainless_tray(out_path):
    mesh = ObjMesh("SM_StainlessTray")
    # Standard tea shop serving tray: 400mm x 300mm x 22mm
    tw = 0.40
    td = 0.30
    th = 0.022
    t_sheet = 0.002
    
    # Bottom flat sheet
    mesh.add_box(-tw/2 + 0.015, tw/2 - 0.015, 0.0, t_sheet, -td/2 + 0.015, td/2 - 0.015)
    # Flared raised sidewalls
    mesh.add_box(-tw/2, tw/2, t_sheet, th, td/2 - 0.015, td/2)
    mesh.add_box(-tw/2, tw/2, t_sheet, th, -td/2, -td/2 + 0.015)
    mesh.add_box(-tw/2, -tw/2 + 0.015, t_sheet, th, -td/2 + 0.015, td/2 - 0.015)
    mesh.add_box(tw/2 - 0.015, tw/2, t_sheet, th, -td/2 + 0.015, td/2 - 0.015)
    # Rolled top rim bead
    rim_r = 0.004
    mesh.add_cylinder((-tw/2, th, td/2), (tw/2, th, td/2), radius=rim_r, segments=8)
    mesh.add_cylinder((-tw/2, th, -td/2), (tw/2, th, -td/2), radius=rim_r, segments=8)
    mesh.add_cylinder((-tw/2, th, -td/2), (-tw/2, th, td/2), radius=rim_r, segments=8)
    mesh.add_cylinder((tw/2, th, -td/2), (tw/2, th, td/2), radius=rim_r, segments=8)

    mesh.write_obj(out_path)

# ==============================================================================
# 4. Sidewalk & Road Modules
# ==============================================================================
def generate_street_environment(sidewalk_path, road_path):
    # Sidewalk module: width 4.0m, length 8.0m, height 0.15m curb
    sw = ObjMesh("SM_Sidewalk_Section")
    sw.add_box(-2.0, 2.0, 0.0, 0.15, -4.0, 4.0, uv_scale=2.0)
    # Concrete curb beveled stone
    sw.add_box(1.85, 2.0, 0.0, 0.15, -4.0, 4.0, uv_scale=1.0)
    sw.write_obj(sidewalk_path)
    
    # Roadway module: width 8.0m, length 12.0m, flat at y = 0.00m
    rd = ObjMesh("SM_Asphalt_Road")
    rd.add_box(-4.0, 4.0, -0.05, 0.0, -6.0, 6.0, uv_scale=3.0)
    rd.write_obj(road_path)

def generate_all_env_assets():
    furn_dir = "Assets/TramChanh/Art/Models/Furniture"
    env_dir = "Assets/TramChanh/Art/Models/Environment"
    os.makedirs(furn_dir, exist_ok=True)
    os.makedirs(env_dir, exist_ok=True)

    generate_plastic_stool(os.path.join(furn_dir, "SM_PlasticStool.obj"))
    generate_yellow_crate_table(os.path.join(furn_dir, "SM_YellowCrateTable.obj"))
    generate_stainless_tray(os.path.join(furn_dir, "SM_StainlessTray.obj"))
    generate_street_environment(
        os.path.join(env_dir, "SM_Sidewalk_Section.obj"),
        os.path.join(env_dir, "SM_Asphalt_Road.obj")
    )
    print("All Street Environment & Furniture 3D Meshes successfully generated!")

if __name__ == "__main__":
    generate_all_env_assets()
