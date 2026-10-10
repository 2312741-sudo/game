#!/usr/bin/env python3
"""
Procedural 3D Mesh Generator for Tram Chanh Street Environment & Furniture
High-Fidelity Environment Expansion Wave:
- SM_PlasticStool.obj (Beige plastic stool based on IMG_5228.JPG: 300x300x300mm)
- SM_YellowCrateTable.obj (Upside-down beverage crate table based on IMG_5231.JPG: 450x350x320mm)
- SM_StainlessTray.obj (Stainless steel serving tray with rolled rim)
- SM_Sidewalk_Extended.obj (Deep Vietnamese terrazzo tile sidewalk with curb)
- SM_Asphalt_Road_Extended.obj (Wet multi-lane asphalt roadway module)
- SM_Shopfront_A.obj (Shophouse A: Tạp Hóa Bình An, 3 storeys, awning, balcony)
- SM_Shopfront_B.obj (Shophouse B: Nhà Thuốc Đức Nguyên, 4 storeys, pharmacy bay window, water tank)
- SM_Shopfront_C.obj (Shophouse C: Sửa Xe Máy Vĩnh Phát, 3 storeys, scissor gate, shutters)
- SM_StreetTree.obj (Vietnamese tropical shade tree: bark trunk & cross-quad canopy)
- SM_StreetLamp.obj (6m urban cobra-head streetlight mast)
- SM_UtilityPole.obj (Vietnamese concrete electrical pole with transformer, meters & cable bundles)
- SM_Motorbike.obj (Parked Vietnamese Honda Wave commuter motorbike)
- SM_TrashBin.obj (Curbside green plastic trash bin)
- SM_PottedPlant.obj (Ceramic planter pot with decorative shrub)
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

    def add_box(self, x_min, x_max, y_min, y_max, z_min, z_max, uv_scale=1.0, u_offset=0.0, v_offset=0.0):
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
        uo = u_offset
        vo = v_offset

        # Front (+Z)
        self.add_quad(v001, v101, v111, v011, (uo, vo), (uo+dx, vo), (uo+dx, vo+dy), (uo, vo+dy), (0, 0, 1))
        # Back (-Z)
        self.add_quad(v100, v000, v010, v110, (uo, vo), (uo+dx, vo), (uo+dx, vo+dy), (uo, vo+dy), (0, 0, -1))
        # Top (+Y)
        self.add_quad(v011, v111, v110, v010, (uo, vo), (uo+dx, vo), (uo+dx, vo+dz), (uo, vo+dz), (0, 1, 0))
        # Bottom (-Y)
        self.add_quad(v000, v100, v101, v001, (uo, vo), (uo+dx, vo), (uo+dx, vo+dz), (uo, vo+dz), (0, -1, 0))
        # Right (+X)
        self.add_quad(v101, v100, v110, v111, (uo, vo), (uo+dz, vo), (uo+dz, vo+dy), (uo, vo+dy), (1, 0, 0))
        # Left (-X)
        self.add_quad(v000, v001, v011, v010, (uo, vo), (uo+dz, vo), (uo+dz, vo+dy), (uo, vo+dy), (-1, 0, 0))

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
    w = 0.30
    d = 0.30
    h = 0.30
    seat_t = 0.018
    
    # Seat plate (recessed center, ventilation cutout hole)
    mesh.add_box(-w/2, w/2, h - seat_t, h, d/2 - 0.03, d/2)
    mesh.add_box(-w/2, w/2, h - seat_t, h, -d/2, -d/2 + 0.03)
    mesh.add_box(-w/2, -w/2 + 0.03, h - seat_t, h, -d/2 + 0.03, d/2 - 0.03)
    mesh.add_box(w/2 - 0.03, w/2, h - seat_t, h, -d/2 + 0.03, d/2 - 0.03)
    
    # Seat inner webbing panel with center oval handle/vent hole
    mesh.add_box(-w/2 + 0.03, -0.04, h - seat_t, h - 0.006, -d/2 + 0.03, d/2 - 0.03)
    mesh.add_box(0.04, w/2 - 0.03, h - seat_t, h - 0.006, -d/2 + 0.03, d/2 - 0.03)
    mesh.add_box(-0.04, 0.04, h - seat_t, h - 0.006, 0.04, d/2 - 0.03)
    mesh.add_box(-0.04, 0.04, h - seat_t, h - 0.006, -d/2 + 0.03, -0.04)
    
    # 4 Flared Stacking Legs
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
    l = 0.45
    w = 0.35
    h = 0.32
    wall_t = 0.015
    
    # Top table surface (bottom of inverted crate)
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
    tw = 0.40
    td = 0.30
    th = 0.022
    t_sheet = 0.002
    
    mesh.add_box(-tw/2 + 0.015, tw/2 - 0.015, 0.0, t_sheet, -td/2 + 0.015, td/2 - 0.015)
    mesh.add_box(-tw/2, tw/2, t_sheet, th, td/2 - 0.015, td/2)
    mesh.add_box(-tw/2, tw/2, t_sheet, th, -td/2, -td/2 + 0.015)
    mesh.add_box(-tw/2, -tw/2 + 0.015, t_sheet, th, -td/2 + 0.015, td/2 - 0.015)
    mesh.add_box(tw/2 - 0.015, tw/2, t_sheet, th, -td/2 + 0.015, td/2 - 0.015)
    
    rim_r = 0.004
    mesh.add_cylinder((-tw/2, th, td/2), (tw/2, th, td/2), radius=rim_r, segments=8)
    mesh.add_cylinder((-tw/2, th, -td/2), (tw/2, th, -td/2), radius=rim_r, segments=8)
    mesh.add_cylinder((-tw/2, th, -td/2), (-tw/2, th, td/2), radius=rim_r, segments=8)
    mesh.add_cylinder((tw/2, th, -td/2), (tw/2, th, td/2), radius=rim_r, segments=8)

    mesh.write_obj(out_path)

# ==============================================================================
# 4. Extended Sidewalk & Multi-lane Road Modules
# ==============================================================================
def generate_extended_road_sidewalk(sidewalk_path, road_path):
    # Deep Sidewalk module: length 36.0m along X (x: -18 to 18), depth along Z (z: -5.2 to 3.6), top walking surface at y: 0.0m, height 0.15m curb
    sw = ObjMesh("SM_Sidewalk_Extended")
    # Main walking surface with terrazzo tile texture (top at y: 0.00m)
    sw.add_box(-18.0, 18.0, -0.15, 0.00, -5.2, 3.45, uv_scale=1.5)
    # Granite curb stone along road boundary (z: 3.45 to 3.60, top at y: 0.00m)
    sw.add_box(-18.0, 18.0, -0.15, 0.00, 3.45, 3.60, uv_scale=1.0)
    sw.write_obj(sidewalk_path)
    
    # Roadway module: length 36.0m along X (x: -18 to 18), width along Z (z: 3.6 to 14.0), road surface at y: -0.15m
    rd = ObjMesh("SM_Asphalt_Road_Extended")
    # Curb drainage gutter strip (z: 3.6 to 4.0, slightly recessed to y: -0.165m)
    rd.add_box(-18.0, 18.0, -0.20, -0.165, 3.6, 4.0, uv_scale=1.0)
    # Asphalt surface (z: 4.0 to 14.0, top surface at y: -0.15m)
    rd.add_box(-18.0, 18.0, -0.20, -0.150, 4.0, 14.0, uv_scale=0.8)
    rd.write_obj(road_path)

# ==============================================================================
# 5. Vietnamese Shophouse A: Tạp Hóa Bình An (3 Storeys)
# ==============================================================================
def generate_shopfront_a(out_path):
    mesh = ObjMesh("SM_Shopfront_A")
    w = 4.5
    h = 11.5
    d = 4.5
    
    # Main building volumetric shell
    # Building sits with back at +Z (z: 0 to d), front facade facing -Z (toward the street)
    # Side and rear walls
    mesh.add_box(-w/2, -w/2 + 0.25, 0.0, h, 0.0, d, uv_scale=0.5)
    mesh.add_box(w/2 - 0.25, w/2, 0.0, h, 0.0, d, uv_scale=0.5)
    mesh.add_box(-w/2, w/2, 0.0, h, d - 0.25, d, uv_scale=0.5)
    mesh.add_box(-w/2, w/2, h - 0.3, h, 0.0, d, uv_scale=0.5) # Flat roof slab

    # Front Architectural Facade (facing -Z)
    # Front facing quad with exact un-mirrored UV projection for Unity OBJ import:
    # v1=(-w/2, y1), v2=(w/2, y1), v3=(w/2, y2), v4=(-w/2, y2)
    # With Unity x = -x inversion: v1 (x < 0) becomes right, v2 (x > 0) becomes left.
    # Therefore, uv1=(u_max, v_min), uv2=(u_min, v_min), uv3=(u_min, v_max), uv4=(u_max, v_max).
    def add_front_facade_panel(x1, x2, y1, y2, z, u1_norm, u2_norm, v1_norm, v2_norm):
        va = mesh.add_vertex(x1, y1, z)
        vb = mesh.add_vertex(x2, y1, z)
        vc = mesh.add_vertex(x2, y2, z)
        vd = mesh.add_vertex(x1, y2, z)
        mesh.add_quad(va, vb, vc, vd, (u2_norm, v1_norm), (u1_norm, v1_norm), (u1_norm, v2_norm), (u2_norm, v2_norm), (0, 0, -1))

    # Upper facade wall (y: 3.5m to 11.5m) at z = 0.0m
    add_front_facade_panel(-w/2, w/2, 3.5, h, 0.0, 0.0, 1.0, 3.5/h, 1.0)

    # Recessed ground floor entrance (z: 0.6m, y: 0 to 3.5m)
    # Roller shutter
    add_front_facade_panel(-w/2 + 0.3, w/2 - 0.3, 0.0, 3.5, 0.6, 0.08, 0.92, 0.0, 3.5/h)
    # Left & Right entrance reveal walls
    mesh.add_box(-w/2, -w/2 + 0.3, 0.0, 3.5, 0.0, 0.6, uv_scale=0.5)
    mesh.add_box(w/2 - 0.3, w/2, 0.0, 3.5, 0.0, 0.6, uv_scale=0.5)

    # Projecting Commercial Signboard "TẠP HÓA BÌNH AN" (y: 3.4m to 4.3m, z: -0.15m)
    mesh.add_box(-w/2 + 0.1, w/2 - 0.1, 3.4, 4.3, -0.15, 0.0)

    # Slanted Canvas Awning (bạt xếp di động)
    # Slants forward from (y=3.3, z=0.0) down to (y=2.5, z=-1.1)
    a_w = w * 0.95
    v_awn_t1 = mesh.add_vertex(-a_w/2, 3.3, 0.0)
    v_awn_t2 = mesh.add_vertex(a_w/2, 3.3, 0.0)
    v_awn_b2 = mesh.add_vertex(a_w/2, 2.5, -1.1)
    v_awn_b1 = mesh.add_vertex(-a_w/2, 2.5, -1.1)
    mesh.add_quad(v_awn_b1, v_awn_b2, v_awn_t2, v_awn_t1, (1, 0.60), (0, 0.60), (0, 0.67), (1, 0.67), (0, 0.6, -0.8))
    # Awning support steel diagonal struts
    mesh.add_cylinder((-a_w/2 + 0.2, 2.0, 0.0), (-a_w/2 + 0.2, 2.5, -1.1), radius=0.02, segments=6)
    mesh.add_cylinder((a_w/2 - 0.2, 2.0, 0.0), (a_w/2 - 0.2, 2.5, -1.1), radius=0.02, segments=6)

    # 2nd Floor Cantilever Balcony Slab (y = 4.4m, extends forward to z = -0.9m)
    mesh.add_box(-w/2 + 0.2, w/2 - 0.2, 4.35, 4.55, -0.9, 0.0)
    # Wrought iron railing around balcony
    mesh.add_box(-w/2 + 0.2, w/2 - 0.2, 5.35, 5.40, -0.92, -0.88) # top rail
    mesh.add_box(-w/2 + 0.2, -w/2 + 0.24, 4.55, 5.35, -0.9, 0.0) # left rail
    mesh.add_box(w/2 - 0.24, w/2 - 0.2, 4.55, 5.35, -0.9, 0.0) # right rail
    for rx in np.linspace(-w/2 + 0.3, w/2 - 0.3, 9):
        mesh.add_cylinder((rx, 4.55, -0.9), (rx, 5.35, -0.9), radius=0.012, segments=6)

    # AC Outdoor Condenser unit on 2nd floor wall
    mesh.add_box(0.9, 1.7, 5.8, 6.5, -0.35, 0.0)

    mesh.write_obj(out_path)

# ==============================================================================
# 6. Vietnamese Shophouse B: Nhà Thuốc Đức Nguyên (4 Storeys)
# ==============================================================================
def generate_shopfront_b(out_path):
    mesh = ObjMesh("SM_Shopfront_B")
    w = 5.0
    h = 14.0
    d = 4.5
    
    # Side and rear walls
    mesh.add_box(-w/2, -w/2 + 0.25, 0.0, h, 0.0, d, uv_scale=0.5)
    mesh.add_box(w/2 - 0.25, w/2, 0.0, h, 0.0, d, uv_scale=0.5)
    mesh.add_box(-w/2, w/2, 0.0, h, d - 0.25, d, uv_scale=0.5)
    mesh.add_box(-w/2, w/2, h - 0.3, h, 0.0, d, uv_scale=0.5)

    def add_front_facade_panel(x1, x2, y1, y2, z, u1_norm, u2_norm, v1_norm, v2_norm):
        va = mesh.add_vertex(x1, y1, z)
        vb = mesh.add_vertex(x2, y1, z)
        vc = mesh.add_vertex(x2, y2, z)
        vd = mesh.add_vertex(x1, y2, z)
        mesh.add_quad(va, vb, vc, vd, (u2_norm, v1_norm), (u1_norm, v1_norm), (u1_norm, v2_norm), (u2_norm, v2_norm), (0, 0, -1))

    # Upper facade (y: 3.6m to 14.0m)
    add_front_facade_panel(-w/2, w/2, 3.6, h, 0.0, 0.0, 1.0, 3.6/h, 1.0)
    # Ground floor green rolling door recessed at z = 0.5m
    add_front_facade_panel(-w/2 + 0.3, w/2 - 0.3, 0.0, 3.6, 0.5, 0.06, 0.94, 0.0, 3.6/h)
    mesh.add_box(-w/2, -w/2 + 0.3, 0.0, 3.6, 0.0, 0.5, uv_scale=0.5)
    mesh.add_box(w/2 - 0.3, w/2, 0.0, 3.6, 0.0, 0.5, uv_scale=0.5)

    # 3D Projecting Pharmacy Signboard Box (y: 3.5m to 4.6m, projecting -0.2m)
    mesh.add_box(-w/2 + 0.1, w/2 - 0.1, 3.5, 4.6, -0.2, 0.0)

    # 2nd & 3rd Floor Cantilever Bay Window Projection (x: -1.8 to 1.8, y: 5.0 to 10.5, z: -0.45 to 0.0)
    mesh.add_box(-1.8, 1.8, 5.0, 10.5, -0.45, 0.0)

    # Rooftop Parapet and Stainless Steel Cylindrical Water Tank (Đại Thành / Sơn Hà)
    mesh.add_box(-w/2, w/2, h, h + 0.8, -0.1, 0.1) # front parapet
    # Horizontal stainless water tank cylinder on rooftop
    mesh.add_cylinder((0.5, h + 0.8, 1.2), (0.5, h + 0.8, 2.8), radius=0.55, segments=12)
    # Metal stand legs for water tank
    mesh.add_box(0.1, 0.9, h, h + 0.3, 1.3, 1.5)
    mesh.add_box(0.1, 0.9, h, h + 0.3, 2.5, 2.7)

    mesh.write_obj(out_path)

# ==============================================================================
# 7. Vietnamese Shophouse C: Sửa Xe Máy Vĩnh Phát (3 Storeys)
# ==============================================================================
def generate_shopfront_c(out_path):
    mesh = ObjMesh("SM_Shopfront_C")
    w = 4.2
    h = 11.0
    d = 4.5
    
    mesh.add_box(-w/2, -w/2 + 0.25, 0.0, h, 0.0, d, uv_scale=0.5)
    mesh.add_box(w/2 - 0.25, w/2, 0.0, h, 0.0, d, uv_scale=0.5)
    mesh.add_box(-w/2, w/2, 0.0, h, d - 0.25, d, uv_scale=0.5)
    mesh.add_box(-w/2, w/2, h - 0.3, h, 0.0, d, uv_scale=0.5)

    def add_front_facade_panel(x1, x2, y1, y2, z, u1_norm, u2_norm, v1_norm, v2_norm):
        va = mesh.add_vertex(x1, y1, z)
        vb = mesh.add_vertex(x2, y1, z)
        vc = mesh.add_vertex(x2, y2, z)
        vd = mesh.add_vertex(x1, y2, z)
        mesh.add_quad(va, vb, vc, vd, (u2_norm, v1_norm), (u1_norm, v1_norm), (u1_norm, v2_norm), (u2_norm, v2_norm), (0, 0, -1))

    # Upper facade (y: 3.3m to 11.0m)
    add_front_facade_panel(-w/2, w/2, 3.3, h, 0.0, 0.0, 1.0, 3.3/h, 1.0)
    # Ground floor recessed workshop with iron scissor gate (cửa kéo sắt) at z = 0.7m
    add_front_facade_panel(-w/2 + 0.25, w/2 - 0.25, 0.0, 3.3, 0.7, 0.06, 0.94, 0.0, 3.3/h)
    mesh.add_box(-w/2, -w/2 + 0.25, 0.0, 3.3, 0.0, 0.7, uv_scale=0.5)
    mesh.add_box(w/2 - 0.25, w/2, 0.0, 3.3, 0.0, 0.7, uv_scale=0.5)

    # Weathered sign board "SỬA XE MÁY VĨNH PHÁT" at y: 3.2m to 4.1m
    mesh.add_box(-w/2 + 0.1, w/2 - 0.1, 3.2, 4.1, -0.15, 0.0)

    # Concrete sunshade eaves (ô văng che mưa) above 2nd & 3rd floor windows
    mesh.add_box(-1.6, 1.6, 7.2, 7.35, -0.5, 0.0)
    mesh.add_box(-1.6, 1.6, 10.4, 10.55, -0.5, 0.0)

    mesh.write_obj(out_path)

# ==============================================================================
# 8. Vietnamese Roadside Shade Tree (Cây Bàng Đài Loan / Phượng)
# ==============================================================================
def generate_street_tree(trunk_path, canopy_path):
    trunk_mesh = ObjMesh("SM_StreetTree_Trunk")
    canopy_mesh = ObjMesh("SM_StreetTree_Canopy")
    
    # 1. Main trunk with natural taper from ground up to fork
    trunk_mesh.add_cylinder((0, 0, 0), (0, 2.6, 0), radius=0.18, segments=12, uv_scale=1.5)
    # Buttress base root flares
    for angle in [0.2, 1.8, 3.4, 4.9]:
        rx = math.cos(angle) * 0.25
        rz = math.sin(angle) * 0.25
        trunk_mesh.add_cylinder((rx, 0, rz), (0, 0.6, 0), radius=0.08, segments=6, uv_scale=1.0)
        
    # 2. 3 Main branch forks spreading upward
    forks = [
        ((-0.8, 4.0,  0.4), 0.10),
        (( 0.7, 4.2, -0.5), 0.10),
        (( 0.1, 4.6,  0.6), 0.11),
    ]
    for tip, rad in forks:
        trunk_mesh.add_cylinder((0, 2.5, 0), tip, radius=rad, segments=8, uv_scale=1.0)

    # Secondary branch splits
    sec_branches = [
        ((-0.8, 4.0, 0.4), (-1.4, 4.8, 0.7), 0.06),
        ((-0.8, 4.0, 0.4), (-0.4, 5.0, 1.1), 0.05),
        ((0.7, 4.2, -0.5), (1.3, 4.9, -0.8), 0.06),
        ((0.7, 4.2, -0.5), (0.9, 5.2, 0.2), 0.05),
        ((0.1, 4.6, 0.6), (-0.2, 5.5, 0.4), 0.06),
        ((0.1, 4.6, 0.6), (0.4, 5.6, 0.8), 0.06)
    ]
    for b_start, b_end, rad in sec_branches:
        trunk_mesh.add_cylinder(b_start, b_end, radius=rad, segments=6, uv_scale=1.0)

    # 3. Cross-quad foliage canopy planes (textured with T_Foliage_Tree_BaseColor.png)
    def add_foliage_cross_quad(cx, cy, cz, size=2.0):
        hs = size / 2.0
        v1 = canopy_mesh.add_vertex(cx - hs, cy - hs, cz)
        v2 = canopy_mesh.add_vertex(cx + hs, cy - hs, cz)
        v3 = canopy_mesh.add_vertex(cx + hs, cy + hs, cz)
        v4 = canopy_mesh.add_vertex(cx - hs, cy + hs, cz)
        canopy_mesh.add_quad(v1, v2, v3, v4, (0, 0), (1, 0), (1, 1), (0, 1), (0, 0, 1))
        canopy_mesh.add_quad(v2, v1, v4, v3, (1, 0), (0, 0), (0, 1), (1, 1), (0, 0, -1))
        
        v5 = canopy_mesh.add_vertex(cx, cy - hs, cz - hs)
        v6 = canopy_mesh.add_vertex(cx, cy - hs, cz + hs)
        v7 = canopy_mesh.add_vertex(cx, cy + hs, cz + hs)
        v8 = canopy_mesh.add_vertex(cx, cy + hs, cz - hs)
        canopy_mesh.add_quad(v5, v6, v7, v8, (0, 0), (1, 0), (1, 1), (0, 1), (1, 0, 0))
        canopy_mesh.add_quad(v6, v5, v8, v7, (1, 0), (0, 0), (0, 1), (1, 1), (-1, 0, 0))

    canopy_centers = [
        (-1.2, 4.7, 0.6),
        (-0.5, 5.1, 1.0),
        ( 1.1, 4.8, -0.7),
        ( 0.8, 5.3, 0.2),
        ( 0.0, 5.4, 0.5),
        (-0.6, 5.3, -0.2),
        ( 0.3, 5.8, 0.3)
    ]
    for cc in canopy_centers:
        add_foliage_cross_quad(*cc, size=2.2)

    trunk_mesh.write_obj(trunk_path)
    canopy_mesh.write_obj(canopy_path)

# ==============================================================================
# 9. Modern Urban Cobra-Head Streetlight (6m mast)
# ==============================================================================
def generate_street_lamp(out_path):
    mesh = ObjMesh("SM_StreetLamp")
    
    # Octagonal base mounting flange with bolts
    mesh.add_cylinder((0, 0, 0), (0, 0.15, 0), radius=0.22, segments=8)
    
    # Vertical tubular steel mast from y=0.15m to y=5.2m
    mesh.add_cylinder((0, 0.15, 0), (0, 5.2, 0), radius=0.08, segments=12)
    
    # Curved cantilever arm sweeping over the road
    # 4 segments forming a smooth curved arch
    p_curv = [
        (0.0, 5.2, 0.0),
        (0.0, 5.6, 0.3),
        (0.0, 5.9, 0.8),
        (0.0, 6.1, 1.4)
    ]
    for i in range(len(p_curv)-1):
        mesh.add_cylinder(p_curv[i], p_curv[i+1], radius=0.05, segments=8)

    # Cobra-head luminaire fixture at end of arm
    # Size: width 0.30m, length 0.75m, height 0.18m
    mesh.add_box(-0.15, 0.15, 5.95, 6.15, 1.35, 2.10)
    # Downward glass diffuser panel
    v_g1 = mesh.add_vertex(-0.13, 5.95, 1.40)
    v_g2 = mesh.add_vertex( 0.13, 5.95, 1.40)
    v_g3 = mesh.add_vertex( 0.13, 5.95, 2.05)
    v_g4 = mesh.add_vertex(-0.13, 5.95, 2.05)
    mesh.add_quad(v_g1, v_g2, v_g3, v_g4, (0, 0), (1, 0), (1, 1), (0, 1), (0, -1, 0))

    mesh.write_obj(out_path)

# ==============================================================================
# 10. Iconic Vietnamese Concrete Electrical Utility Pole
# ==============================================================================
def generate_utility_pole(out_path):
    mesh = ObjMesh("SM_UtilityPole")
    
    # Main octagonal concrete pole: base at y=0, height 7.5m, radius 0.16m
    mesh.add_cylinder((0, 0, 0), (0, 7.5, 0), radius=0.16, segments=8)
    
    # Lower steel cross-arm (xà điện) at y = 5.8m (length 1.8m in X)
    mesh.add_box(-0.9, 0.9, 5.75, 5.85, -0.06, 0.06)
    # Upper steel cross-arm at y = 6.8m (length 1.3m in X)
    mesh.add_box(-0.65, 0.65, 6.75, 6.85, -0.06, 0.06)
    
    # Ceramic bell insulators (sứ cách điện) on cross-arms
    for ix in [-0.8, -0.4, 0.4, 0.8]:
        mesh.add_cylinder((ix, 5.85, 0), (ix, 6.05, 0), radius=0.04, segments=6)
    for ix in [-0.55, 0.55]:
        mesh.add_cylinder((ix, 6.85, 0), (ix, 7.05, 0), radius=0.04, segments=6)

    # Steel transformer box mounted at y = 4.2m
    mesh.add_box(0.12, 0.65, 4.0, 4.9, -0.25, 0.25)
    
    # Electric meters cluster (cụm đồng hồ điện gia đình) at y = 2.4m
    mesh.add_box(-0.45, -0.12, 2.2, 3.0, -0.15, 0.15)
    for my in [2.35, 2.60, 2.85]:
        mesh.add_cylinder((-0.46, my, 0), (-0.54, my, 0), radius=0.08, segments=8)

    # Tangled power cable bundles (dây điện chằng chịt) running along street (X axis)
    # Spans from x = -16.0m to +16.0m with natural catenary sag
    for offset_z in [-0.15, 0.15]:
        for y_top in [5.8, 6.2]:
            p_start = (-16.0, y_top, offset_z)
            p_mid1  = (-8.0,  y_top - 0.45, offset_z + 0.05)
            p_pole  = ( 0.0,  y_top, offset_z)
            p_mid2  = ( 8.0,  y_top - 0.45, offset_z - 0.05)
            p_end   = ( 16.0, y_top, offset_z)
            mesh.add_cylinder(p_start, p_mid1, radius=0.02, segments=6)
            mesh.add_cylinder(p_mid1, p_pole, radius=0.02, segments=6)
            mesh.add_cylinder(p_pole, p_mid2, radius=0.02, segments=6)
            mesh.add_cylinder(p_mid2, p_end, radius=0.02, segments=6)

    mesh.write_obj(out_path)

# ==============================================================================
# 11. Authentic Vietnamese Motorbike (Honda Wave / Dream)
# ==============================================================================
def generate_motorbike(out_path):
    mesh = ObjMesh("SM_Motorbike")
    # Centered at (0,0,0), aligned along Z (facing -Z), width along X
    
    # Wheels (spoke rims + rubber tires)
    # Front wheel: z = -0.70m, y = 0.28m, radius 0.28m
    mesh.add_cylinder((-0.035, 0.28, -0.70), (0.035, 0.28, -0.70), radius=0.28, segments=12)
    # Rear wheel: z = 0.70m, y = 0.28m, radius 0.28m
    mesh.add_cylinder((-0.040, 0.28, 0.70), (0.040, 0.28, 0.70), radius=0.28, segments=12)
    
    # Front fork tubes and suspension
    mesh.add_cylinder((-0.07, 0.28, -0.70), (-0.05, 0.78, -0.55), radius=0.02, segments=6)
    mesh.add_cylinder(( 0.07, 0.28, -0.70), ( 0.05, 0.78, -0.55), radius=0.02, segments=6)
    
    # Handlebars & Headlight Cowl
    mesh.add_box(-0.32, 0.32, 0.88, 0.94, -0.58, -0.50) # handlebar crossbar
    mesh.add_box(-0.12, 0.12, 0.82, 0.96, -0.65, -0.50) # headlight pod
    # Left & Right grips and mirrors
    mesh.add_cylinder((-0.32, 0.91, -0.54), (-0.35, 0.91, -0.54), radius=0.025, segments=6)
    mesh.add_cylinder(( 0.32, 0.91, -0.54), ( 0.35, 0.91, -0.54), radius=0.025, segments=6)

    # Classic step-through plastic body fairing (yếm xe Wave)
    # Leg shields (yếm trước)
    mesh.add_box(-0.24, 0.24, 0.35, 0.78, -0.52, -0.22)
    # Mid-frame & engine casing
    mesh.add_box(-0.14, 0.14, 0.20, 0.55, -0.22, 0.25)
    # Rear side tail fairings
    mesh.add_box(-0.16, 0.16, 0.52, 0.72, 0.20, 0.75)
    
    # Vinyl two-passenger seat (yên xe)
    # Length: z = -0.20m to +0.65m, y: 0.72m to 0.78m
    mesh.add_box(-0.13, 0.13, 0.72, 0.79, -0.20, 0.65)
    
    # Chrome exhaust muffler on right side
    mesh.add_cylinder((0.15, 0.22, 0.05), (0.16, 0.28, 0.75), radius=0.045, segments=8)
    
    # Kickstand (chân chống nghiêng)
    mesh.add_cylinder((-0.10, 0.25, 0.0), (-0.22, 0.0, 0.05), radius=0.015, segments=6)

    mesh.write_obj(out_path)

# ==============================================================================
# 12. Roadside Props: Green Plastic Trash Bin & Ceramic Potted Plant
# ==============================================================================
def generate_roadside_props(bin_path, plant_path):
    # Trash Bin: height 0.85m, radius 0.22m
    tb = ObjMesh("SM_TrashBin")
    tb.add_cylinder((0, 0, 0), (0, 0.70, 0), radius=0.22, segments=12)
    # Hinged dome swing lid
    tb.add_cylinder((0, 0.70, 0), (0, 0.85, 0), radius=0.24, segments=12)
    tb.write_obj(bin_path)

    # Ceramic Planter Pot with Shrub
    pp = ObjMesh("SM_PottedPlant")
    # Ceramic pot tapering down
    pp.add_cylinder((0, 0, 0), (0, 0.45, 0), radius=0.25, segments=10)
    # Foliage shrub clump (dome of leaves)
    for sy in [0.45, 0.65, 0.80]:
        pp.add_cylinder((0, sy, 0), (0, sy + 0.18, 0), radius=0.35 * (1.0 - (sy-0.45)*0.8), segments=10)
    pp.write_obj(plant_path)

# ==============================================================================
# Master Generator Entry Point
# ==============================================================================
def generate_all_env_assets():
    furn_dir = "Assets/TramChanh/Art/Models/Furniture"
    env_dir = "Assets/TramChanh/Art/Models/Environment"
    os.makedirs(furn_dir, exist_ok=True)
    os.makedirs(env_dir, exist_ok=True)

    # Furniture
    generate_plastic_stool(os.path.join(furn_dir, "SM_PlasticStool.obj"))
    generate_yellow_crate_table(os.path.join(furn_dir, "SM_YellowCrateTable.obj"))
    generate_stainless_tray(os.path.join(furn_dir, "SM_StainlessTray.obj"))

    # Environment Ground
    generate_extended_road_sidewalk(
        os.path.join(env_dir, "SM_Sidewalk_Extended.obj"),
        os.path.join(env_dir, "SM_Asphalt_Road_Extended.obj")
    )

    # Architecture
    generate_shopfront_a(os.path.join(env_dir, "SM_Shopfront_A.obj"))
    generate_shopfront_b(os.path.join(env_dir, "SM_Shopfront_B.obj"))
    generate_shopfront_c(os.path.join(env_dir, "SM_Shopfront_C.obj"))

    # Props & Nature
    generate_street_tree(
        os.path.join(env_dir, "SM_StreetTree_Trunk.obj"),
        os.path.join(env_dir, "SM_StreetTree_Canopy.obj")
    )
    generate_street_lamp(os.path.join(env_dir, "SM_StreetLamp.obj"))
    generate_utility_pole(os.path.join(env_dir, "SM_UtilityPole.obj"))
    generate_motorbike(os.path.join(env_dir, "SM_Motorbike.obj"))
    generate_roadside_props(
        os.path.join(env_dir, "SM_TrashBin.obj"),
        os.path.join(env_dir, "SM_PottedPlant.obj")
    )

    print("\n[SUCCESS] All Street Environment, Shophouses, Props & Furniture 3D Meshes Generated!")

if __name__ == "__main__":
    generate_all_env_assets()
