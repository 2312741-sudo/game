#!/usr/bin/env python3
"""
Procedural 3D Mesh Generator for Tram Chanh Drink Station Equipment (Agent C)
Outputs native Wavefront OBJ files with normals and UVs:
- SM_TeaBag_Pouch_Closed.obj (Stand-up ziplock pouch based on IMG_5214.JPG)
- SM_TeaBag_Pouch_Open.obj (Open pouch ready for ice & topping)
- SM_TeaBag_Liquid.obj (Amber tea liquid volume)
- SM_Topping_CoconutJellyCubes.obj (Translucent floating jelly cubes)
- SM_Ice_Cubes.obj (Chamfered crystal ice cubes)
- SM_RedTeaRack.obj (Commercial red plastic display rack)
- SM_ToppingStation_Frame.obj (Stainless steel 6-pan chilled bar)
- SM_ToppingStation_Cover.obj (Curved roll-top clear acrylic dome)
- SM_IceBin_Well.obj (Insulated stainless drop-in well)
- SM_IceBin_Lids.obj (Sliding dual flush lids)
- SM_IceScoop.obj (Stainless steel contoured scoop)
"""

import os
import math
import numpy as np

class ObjMesh:
    def __init__(self, name):
        self.name = name
        self.vertices = []   # list of (x, y, z)
        self.normals = []    # list of (nx, ny, nz)
        self.uvs = []        # list of (u, v)
        self.faces = []      # list of list of (v_idx, vt_idx, vn_idx) 1-based

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
# 1. Stand-Up Pouch Tea Bag (IMG_5214.JPG)
# ==============================================================================
def generate_teabag_pouch(out_path, is_open=False):
    mesh = ObjMesh("SM_TeaBag_Pouch_Open" if is_open else "SM_TeaBag_Pouch_Closed")
    # Real dimensions from photo: width ~140mm, height ~220mm, bottom depth ~60mm
    w = 0.14
    h = 0.22
    d_base = 0.06
    
    # 6 vertical height stations:
    # 0: bottom base rim (y = 0.000m)
    # 1: lower fluid bulge (y = 0.050m)
    # 2: mid waist (y = 0.120m)
    # 3: zip lock track (y = 0.170m)
    # 4: handle opening bottom (y = 0.185m)
    # 5: header top seal (y = 0.220m)
    
    num_x = 12
    x_vals = np.linspace(-w/2, w/2, num_x)
    
    # Front and rear surfaces
    for surface_sign in [-1.0, 1.0]:
        is_front_surf = (surface_sign < 0)
        z_mult = surface_sign
        normal_z = surface_sign
        
        y_levels = [0.000, 0.050, 0.120, 0.170, 0.185, 0.220]
        # Depth profile at each level:
        # Base expands to d_base/2 at center, 0 at side weld
        depths = [
            d_base * 0.45,
            d_base * 0.50,
            d_base * 0.35,
            0.025 if is_open else 0.006, # Open pouch spreads at top!
            0.035 if is_open else 0.004,
            0.040 if is_open else 0.002
        ]
        
        grid_v = []
        for l_idx, (y_val, max_d) in enumerate(zip(y_levels, depths)):
            row_v = []
            for i, x in enumerate(x_vals):
                t = 1.0 - (2.0 * abs(x) / w)**2
                t = max(0.0, t)
                z = z_mult * max_d * math.sqrt(t)
                row_v.append(mesh.add_vertex(x, y_val, z))
            grid_v.append(row_v)
            
        # Connect quads
        for j in range(len(y_levels) - 1):
            for i in range(num_x - 1):
                v1 = grid_v[j][i]
                v2 = grid_v[j][i+1]
                v3 = grid_v[j+1][i+1]
                v4 = grid_v[j+1][i]
                
                v_coord1 = y_levels[j] / h
                v_coord2 = y_levels[j+1] / h
                
                n = (0.0, 0.1 if j > 2 else -0.1, normal_z)
                norm_len = math.sqrt(n[0]**2 + n[1]**2 + n[2]**2)
                normal = (n[0]/norm_len, n[1]/norm_len, n[2]/norm_len)
                
                if is_front_surf:
                    # Front face: facing -Z (towards viewer/camera), U in [0.0, 0.5] with authentic brand sticker
                    # Note: Unity OBJ importer inverts X; flipping U ensures un-mirrored front display in Unity
                    u1 = 0.5 - ((x_vals[i] + w/2) / w) * 0.5
                    u2 = 0.5 - ((x_vals[i+1] + w/2) / w) * 0.5
                    mesh.add_quad(v1, v4, v3, v2, (u1, v_coord1), (u1, v_coord2), (u2, v_coord2), (u2, v_coord1), normal)
                else:
                    # Rear face: facing +Z, U in [0.5, 1.0] with clean frosted film
                    u1 = 1.0 - ((x_vals[i] + w/2) / w) * 0.5
                    u2 = 1.0 - ((x_vals[i+1] + w/2) / w) * 0.5
                    mesh.add_quad(v1, v2, v3, v4, (u1, v_coord1), (u2, v_coord1), (u2, v_coord2), (u1, v_coord2), normal)

    # Bottom gusset sealing panel (oval base)
    gusset_segs = 16
    gusset_center = mesh.add_vertex(0.0, 0.008, 0.0)
    uv_c = mesh.add_uv(0.75, 0.15)
    n_down = mesh.add_normal(0.0, -1.0, 0.0)
    
    rim_v = []
    for i in range(gusset_segs):
        ang = 2.0 * math.pi * i / gusset_segs
        gx = (w * 0.45) * math.cos(ang)
        gz = (d_base * 0.45) * math.sin(ang)
        rim_v.append(mesh.add_vertex(gx, 0.000, gz))
        
    for i in range(gusset_segs):
        next_i = (i + 1) % gusset_segs
        v1 = rim_v[i]
        v2 = rim_v[next_i]
        uv1 = mesh.add_uv(0.75 + 0.20 * math.cos(2*math.pi*i/gusset_segs), 0.15 + 0.10 * math.sin(2*math.pi*i/gusset_segs))
        uv2 = mesh.add_uv(0.75 + 0.20 * math.cos(2*math.pi*next_i/gusset_segs), 0.15 + 0.10 * math.sin(2*math.pi*next_i/gusset_segs))
        mesh.add_face([(gusset_center, uv_c, n_down), (v1, uv1, n_down), (v2, uv2, n_down)])

    mesh.write_obj(out_path)

# ==============================================================================
# 2. Tea Liquid Volume
# ==============================================================================
def generate_teabag_liquid(out_path):
    mesh = ObjMesh("SM_TeaBag_Liquid")
    w = 0.134
    h_liquid = 0.125
    d_base = 0.054
    
    # Modeled slightly recessed within pouch inner envelope
    num_x = 10
    x_vals = np.linspace(-w/2, w/2, num_x)
    y_levels = [0.005, 0.045, 0.085, h_liquid]
    depths = [d_base * 0.42, d_base * 0.46, d_base * 0.38, d_base * 0.28]
    
    top_rim_front = []
    top_rim_back = []
    
    for surface_sign in [1.0, -1.0]:
        z_mult = surface_sign
        normal_z = surface_sign
        grid_v = []
        for l_idx, (y_val, max_d) in enumerate(zip(y_levels, depths)):
            row_v = []
            for i, x in enumerate(x_vals):
                t = max(0.0, 1.0 - (2.0 * abs(x) / w)**2)
                z = z_mult * max_d * math.sqrt(t)
                v = mesh.add_vertex(x, y_val, z)
                row_v.append(v)
                if l_idx == len(y_levels) - 1:
                    if surface_sign > 0:
                        top_rim_front.append(v)
                    else:
                        top_rim_back.append(v)
            grid_v.append(row_v)
            
        for j in range(len(y_levels) - 1):
            for i in range(num_x - 1):
                v1, v2, v3, v4 = grid_v[j][i], grid_v[j][i+1], grid_v[j+1][i+1], grid_v[j+1][i]
                u1, u2 = (x_vals[i] + w/2)/w, (x_vals[i+1] + w/2)/w
                v_c1, v_c2 = y_levels[j]/h_liquid, y_levels[j+1]/h_liquid
                if surface_sign > 0:
                    mesh.add_quad(v1, v2, v3, v4, (u1, v_c1), (u2, v_c1), (u2, v_c2), (u1, v_c2), (0, 0, normal_z))
                else:
                    mesh.add_quad(v1, v4, v3, v2, (u1, v_c1), (u1, v_c2), (u2, v_c2), (u2, v_c1), (0, 0, normal_z))

    # Fluid Meniscus Top Surface
    n_up = (0, 1, 0)
    for i in range(num_x - 1):
        vf1 = top_rim_front[i]
        vf2 = top_rim_front[i+1]
        vb2 = top_rim_back[i+1]
        vb1 = top_rim_back[i]
        u1, u2 = (x_vals[i] + w/2)/w, (x_vals[i+1] + w/2)/w
        mesh.add_quad(vf1, vf2, vb2, vb1, (u1, 0), (u2, 0), (u2, 1), (u1, 1), n_up)

    mesh.write_obj(out_path)

# ==============================================================================
# 3. Floating Coconut Jelly Cubes & Ice Cubes
# ==============================================================================
def generate_jelly_cubes(out_path):
    mesh = ObjMesh("SM_Topping_CoconutJellyCubes")
    # 8 distinct nata-de-coco cubes floating inside amber liquid
    cube_s = 0.013
    positions = [
        (-0.035, 0.040, 0.005),
        (-0.015, 0.065, -0.008),
        ( 0.020, 0.050, 0.006),
        ( 0.038, 0.075, -0.004),
        (-0.025, 0.095, 0.003),
        ( 0.010, 0.105, -0.005),
        (-0.005, 0.030, -0.006),
        ( 0.030, 0.090, 0.007)
    ]
    for cx, cy, cz in positions:
        mesh.add_box(cx - cube_s/2, cx + cube_s/2, cy - cube_s/2, cy + cube_s/2, cz - cube_s/2, cz + cube_s/2)
    mesh.write_obj(out_path)

def generate_ice_cubes(out_path):
    mesh = ObjMesh("SM_Ice_Cubes")
    # 5 chunky ice cubes floating near top liquid surface
    s = 0.024
    ice_pos = [
        (-0.028, 0.115, 0.004),
        ( 0.022, 0.120, -0.005),
        (-0.005, 0.110, -0.006),
        ( 0.035, 0.112, 0.005),
        (-0.038, 0.108, -0.003)
    ]
    for cx, cy, cz in ice_pos:
        mesh.add_box(cx - s/2, cx + s/2, cy - s/2, cy + s/2, cz - s/2, cz + s/2)
    mesh.write_obj(out_path)

# ==============================================================================
# 4. Red Tea Rack (Commercial Crate Rack with Grid Slots)
# ==============================================================================
def generate_red_tea_rack(out_path):
    mesh = ObjMesh("SM_RedTeaRack")
    # Outer dimensions: width 0.32m, height 0.28m, depth 0.22m
    w = 0.32
    h = 0.28
    d = 0.22
    t = 0.012 # Plastic wall thickness
    
    # Outer basket box (hollowed inside)
    # Bottom plate
    mesh.add_box(-w/2, w/2, 0.0, t, -d/2, d/2)
    # Front rim (lowered front lip facing -Z for barista pull)
    mesh.add_box(-w/2, w/2, t, h * 0.45, -d/2, -d/2 + t)
    # Back high support wall (+Z)
    mesh.add_box(-w/2, w/2, t, h, d/2 - t, d/2)
    # Left and Right stepped sidewalls
    mesh.add_box(-w/2, -w/2 + t, t, h * 0.85, -d/2 + t, d/2 - t)
    mesh.add_box(w/2 - t, w/2, t, h * 0.85, -d/2 + t, d/2 - t)
    
    # Center divider partition creating Slot01 and Slot02
    mesh.add_box(-t/2, t/2, t, h * 0.65, -d/2 + t, d/2 - t)
    
    # Top reinforcement carry handles
    handle_r = 0.008
    mesh.add_cylinder((-w/2 + t, h * 0.82, 0), (w/2 - t, h * 0.82, 0), radius=handle_r, segments=10)
    
    mesh.write_obj(out_path)

# ==============================================================================
# 5. Stainless Steel Topping Station (IMG_5248.JPG)
# ==============================================================================
def generate_topping_station(frame_path, cover_path):
    # 5.1 Main Chilled Housing & 6 Recessed GN Pans
    frame = ObjMesh("SM_ToppingStation_Frame")
    w = 0.52
    h = 0.14  # Tabletop unit sitting directly on counter
    d = 0.36
    
    # Outer stainless steel housing body
    frame.add_box(-w/2, w/2, 0.0, h, -d/2, d/2)
    
    # Top flange rim
    flange = 0.015
    flange_t = 0.005
    frame.add_box(-w/2 - flange, w/2 + flange, h, h + flange_t, -d/2 - flange, d/2 + flange)
    
    # 6 Stainless GN Insert Pans (2 rows of 3 pans)
    pan_w = 0.14
    pan_d = 0.15
    pan_h = 0.10
    pan_xs = [-0.155, 0.0, 0.155]
    pan_zs = [-0.085, 0.085]
    
    for px in pan_xs:
        for pz in pan_zs:
            # Pan rim bevel resting on top plate
            frame.add_box(px - pan_w/2, px + pan_w/2, h + flange_t, h + flange_t + 0.004, pz - pan_d/2, pz + pan_d/2)
            # Pan inner metallic volume
            frame.add_box(px - pan_w/2 + 0.005, px + pan_w/2 - 0.005, h - pan_h, h + flange_t, pz - pan_d/2 + 0.005, pz + pan_d/2 - 0.005)

    frame.write_obj(frame_path)
    
    # 5.2 Roll-Top Transparent Acrylic Cover (Curves over the top of the GN pans)
    cover = ObjMesh("SM_ToppingStation_Cover")
    r_dome = 0.18
    segs = 16
    cw = w + 0.02
    
    arc_angles = np.linspace(0.0, math.pi * 0.52, segs)
    top_rim = []
    
    for a in arc_angles:
        cy = h + flange_t + r_dome * math.sin(a)
        cz = d/2 - 0.02 - r_dome * (1.0 - math.cos(a))
        top_rim.append((cy, cz))
        
    for i in range(segs - 1):
        y1, z1 = top_rim[i]
        y2, z2 = top_rim[i+1]
        
        v1 = cover.add_vertex(-cw/2, y1, z1)
        v2 = cover.add_vertex( cw/2, y1, z1)
        v3 = cover.add_vertex( cw/2, y2, z2)
        v4 = cover.add_vertex(-cw/2, y2, z2)
        
        n_rot = (0, math.cos(arc_angles[i]), -math.sin(arc_angles[i]))
        cover.add_quad(v1, v4, v3, v2, (0, i/segs), (0, (i+1)/segs), (1, (i+1)/segs), (1, i/segs), n_rot)
        
    # Stainless lift handle across front edge
    yf_last, zf_last = top_rim[-1]
    cover.add_cylinder((-cw*0.35, yf_last + 0.015, zf_last), (cw*0.35, yf_last + 0.015, zf_last), radius=0.008, segments=10)
    cover.write_obj(cover_path)

# ==============================================================================
# 6. Stainless Steel Drop-In Ice Bin & Scoop (IMG_5254.JPG)
# ==============================================================================
def generate_ice_bin(well_path, lids_path):
    well = ObjMesh("SM_IceBin_Well")
    w = 0.45
    h = 0.32
    d = 0.40
    
    # Top perimeter mounting flange
    flange = 0.025
    well.add_box(-w/2 - flange, w/2 + flange, 0.0, 0.006, -d/2 - flange, d/2 + flange)
    
    # Insulated well walls extending downward
    well.add_box(-w/2, w/2, -h, 0.0, d/2 - 0.015, d/2)
    well.add_box(-w/2, w/2, -h, 0.0, -d/2, -d/2 + 0.015)
    well.add_box(-w/2, -w/2 + 0.015, -h, 0.0, -d/2, d/2)
    well.add_box(w/2 - 0.015, w/2, -h, 0.0, -d/2, d/2)
    well.add_box(-w/2, w/2, -h, -h + 0.015, -d/2, d/2)
    
    # Internal perforated false bottom drain grate
    well.add_box(-w/2 + 0.02, w/2 - 0.02, -h + 0.035, -h + 0.045, -d/2 + 0.02, d/2 - 0.02)
    well.write_obj(well_path)
    
    # Dual sliding flush lids
    lids = ObjMesh("SM_IceBin_Lids")
    lw = w - 0.01
    ld = (d - 0.01) / 2
    # Rear lid (slides under front)
    lids.add_box(-lw/2, lw/2, 0.006, 0.014, -d/2 + 0.005, 0.00)
    # Front lid with recessed flush pull
    lids.add_box(-lw/2, lw/2, 0.014, 0.022, 0.00, d/2 - 0.005)
    # Recessed handle on front lid
    lids.add_box(-0.06, 0.06, 0.022, 0.030, d/2 * 0.4, d/2 * 0.6)
    lids.write_obj(lids_path)

def generate_ice_scoop(out_path):
    scoop = ObjMesh("SM_IceScoop")
    # Tapered deep scoop bowl with round heel and ergonomic handle
    bw = 0.065
    bl = 0.120
    bh = 0.045
    
    # U-shaped bowl profile
    scoop.add_box(-bw/2, bw/2, 0.0, 0.004, -bl/2, bl/2)
    scoop.add_box(-bw/2, -bw/2 + 0.004, 0.004, bh, -bl/2, bl/2)
    scoop.add_box(bw/2 - 0.004, bw/2, 0.004, bh, -bl/2, bl/2)
    scoop.add_box(-bw/2, bw/2, 0.004, bh, -bl/2, -bl/2 + 0.004) # back wall
    
    # Cylindrical contoured handle extending rearward
    handle_len = 0.095
    scoop.add_cylinder((0, bh * 0.7, -bl/2), (0, bh * 0.85, -bl/2 - handle_len), radius=0.011, segments=12)
    scoop.write_obj(out_path)

# ==============================================================================
# 7. Standing Menu Board Display (A4 Acrylic Easel Stand)
# ==============================================================================
def generate_menu_board(out_path):
    board = ObjMesh("SM_Menu_Board")
    mw = 0.210  # 210mm A4 width
    mh = 0.297  # 297mm A4 height
    thick = 0.004 # 4mm acrylic
    
    # 1. Base foot stand (beveled acrylic block)
    board.add_box(-mw*0.55, mw*0.55, 0.000, 0.016, -0.045, 0.045)
    
    # 2. Upright A4 display sheet tilted back by 10 degrees (0.174 rad)
    tilt = math.radians(10.0)
    cos_t = math.cos(tilt)
    sin_t = math.sin(tilt)
    
    # Bottom vertices
    y_b = 0.012
    z_b = 0.008
    # Top vertices (tilted back along +Z)
    y_t = y_b + mh * cos_t
    z_t = z_b + mh * sin_t
    
    # Normal tilted forward-up (towards camera at -Z)
    n_front = (0.0, sin_t, -cos_t)
    
    # Front facing display panel (full UV 0..1 for T_Menu_Board_BaseColor)
    # Looking from -Z: -mw/2 is left (U=0), +mw/2 is right (U=1)
    v_bl = board.add_vertex(-mw/2, y_b, z_b)
    v_br = board.add_vertex( mw/2, y_b, z_b)
    v_tr = board.add_vertex( mw/2, y_t, z_t)
    v_tl = board.add_vertex(-mw/2, y_t, z_t)
    
    # Winding order facing -Z: v_bl -> v_tl -> v_tr -> v_br
    # Inverted U in OBJ compensates for Unity importer X inversion
    board.add_quad(v_bl, v_tl, v_tr, v_br, (1.0, 0.0), (1.0, 1.0), (0.0, 1.0), (0.0, 0.0), n_front)
    
    # Back face
    v_bl_b = board.add_vertex(-mw/2, y_b, z_b + thick)
    v_br_b = board.add_vertex( mw/2, y_b, z_b + thick)
    v_tr_b = board.add_vertex( mw/2, y_t, z_t + thick)
    v_tl_b = board.add_vertex(-mw/2, y_t, z_t + thick)
    n_back = (0.0, -sin_t, cos_t)
    board.add_quad(v_bl_b, v_br_b, v_tr_b, v_tl_b, (1.0, 0.0), (0.0, 0.0), (0.0, 1.0), (1.0, 1.0), n_back)
    
    board.write_obj(out_path)

def generate_all_drink_assets():
    models_dir = "Assets/TramChanh/Art/Models/DrinkStation"
    os.makedirs(models_dir, exist_ok=True)
    
    generate_teabag_pouch(os.path.join(models_dir, "SM_TeaBag_Pouch_Closed.obj"), is_open=False)
    generate_teabag_pouch(os.path.join(models_dir, "SM_TeaBag_Pouch_Open.obj"), is_open=True)
    generate_teabag_liquid(os.path.join(models_dir, "SM_TeaBag_Liquid.obj"))
    generate_jelly_cubes(os.path.join(models_dir, "SM_Topping_CoconutJellyCubes.obj"))
    generate_ice_cubes(os.path.join(models_dir, "SM_Ice_Cubes.obj"))
    generate_red_tea_rack(os.path.join(models_dir, "SM_RedTeaRack.obj"))
    generate_topping_station(
        os.path.join(models_dir, "SM_ToppingStation_Frame.obj"),
        os.path.join(models_dir, "SM_ToppingStation_Cover.obj")
    )
    generate_ice_bin(
        os.path.join(models_dir, "SM_IceBin_Well.obj"),
        os.path.join(models_dir, "SM_IceBin_Lids.obj")
    )
    generate_ice_scoop(os.path.join(models_dir, "SM_IceScoop.obj"))
    generate_menu_board(os.path.join(models_dir, "SM_Menu_Board.obj"))
    print("All Drink Station 3D Meshes successfully generated!")

if __name__ == "__main__":
    generate_all_drink_assets()
