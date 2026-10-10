#!/usr/bin/env python3
"""
Procedural 3D Mesh Generator for Tram Chanh High-Fidelity Stall
Outputs native Wavefront OBJ files with normals and UVs:
- SM_Stall_Base.obj
- SM_Stall_Counter.obj
- SM_Stall_Frame.obj
- SM_Stall_Roof.obj
- SM_Stall_CasterWheel.obj
- SM_Sign_TramChanh_New.obj
- SM_EdisonBulb.obj
- SM_LEDStrip.obj
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
        # Two triangles
        n_idx = self.add_normal(*normal)
        u1 = self.add_uv(*uv1)
        u2 = self.add_uv(*uv2)
        u3 = self.add_uv(*uv3)
        u4 = self.add_uv(*uv4)
        
        self.add_face([(v1, u1, n_idx), (v2, u2, n_idx), (v3, u3, n_idx)])
        self.add_face([(v1, u1, n_idx), (v3, u3, n_idx), (v4, u4, n_idx)])

    def add_box(self, x_min, x_max, y_min, y_max, z_min, z_max, uv_scale=1.0):
        # 8 vertices
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

        # Front (+Z)
        self.add_quad(v001, v101, v111, v011, (0, 0), (dx, 0), (dx, dy), (0, dy), (0, 0, 1))
        # Back (-Z)
        self.add_quad(v100, v000, v010, v110, (0, 0), (dx, 0), (dx, dy), (0, dy), (0, 0, -1))
        # Top (+Y)
        self.add_quad(v011, v111, v110, v010, (0, 0), (dx, 0), (dx, dz), (0, dz), (0, 1, 0))
        # Bottom (-Y)
        self.add_quad(v000, v100, v101, v001, (0, 0), (dx, 0), (dx, dz), (0, dz), (0, -1, 0))
        # Right (+X)
        self.add_quad(v101, v100, v110, v111, (0, 0), (dz, 0), (dz, dy), (0, dy), (1, 0, 0))
        # Left (-X)
        self.add_quad(v000, v001, v011, v010, (0, 0), (dz, 0), (dz, dy), (0, dy), (-1, 0, 0))

    def add_cylinder(self, p1, p2, radius, segments=16, uv_scale=1.0):
        # Cylinder between p1 and p2
        p1 = np.array(p1, dtype=float)
        p2 = np.array(p2, dtype=float)
        axis = p2 - p1
        length = np.linalg.norm(axis)
        if length < 1e-6:
            return
        z_dir = axis / length

        # Find orthogonal basis
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

        # Quads around sides
        for i in range(segments):
            next_i = (i + 1) % segments
            v1 = ring1_idx[i]
            v2 = ring1_idx[next_i]
            v3 = ring2_idx[next_i]
            v4 = ring2_idx[i]
            
            u_cur = i / segments * uv_scale
            u_nxt = (i + 1) / segments * uv_scale
            
            n1 = ring_normals[i]
            n2 = ring_normals[next_i]
            
            n1_idx = self.add_normal(*n1)
            n2_idx = self.add_normal(*n2)
            
            uv1 = self.add_uv(u_cur, 0)
            uv2 = self.add_uv(u_nxt, 0)
            uv3 = self.add_uv(u_nxt, length * uv_scale)
            uv4 = self.add_uv(u_cur, length * uv_scale)
            
            self.add_face([(v1, uv1, n1_idx), (v2, uv2, n2_idx), (v3, uv3, n2_idx)])
            self.add_face([(v1, uv1, n1_idx), (v3, uv3, n2_idx), (v4, uv4, n1_idx)])

        # End caps
        n_cap1 = self.add_normal(*(-z_dir))
        n_cap2 = self.add_normal(*(z_dir))
        c1 = self.add_vertex(*p1)
        c2 = self.add_vertex(*p2)
        uv_center = self.add_uv(0.5, 0.5)

        for i in range(segments):
            next_i = (i + 1) % segments
            angle1 = 2.0 * math.pi * i / segments
            angle2 = 2.0 * math.pi * next_i / segments
            uv_c1 = self.add_uv(0.5 + 0.5 * math.cos(angle1), 0.5 + 0.5 * math.sin(angle1))
            uv_c2 = self.add_uv(0.5 + 0.5 * math.cos(angle2), 0.5 + 0.5 * math.sin(angle2))
            # p1 cap (facing -z_dir)
            self.add_face([(c1, uv_center, n_cap1), (ring1_idx[next_i], uv_c2, n_cap1), (ring1_idx[i], uv_c1, n_cap1)])
            # p2 cap (facing +z_dir)
            self.add_face([(c2, uv_center, n_cap2), (ring2_idx[i], uv_c1, n_cap2), (ring2_idx[next_i], uv_c2, n_cap2)])

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
            for face in self.faces:
                f.write("f " + " ".join([f"{v}/{vt}/{vn}" for v, vt, vn in face]) + "\n")
        print(f"Exported: {filepath} ({len(self.vertices)} verts, {len(self.faces)} faces)")

# ==============================================================================
# 1. SM_Stall_Base.obj: Lower cabinet with 3D corrugated metal panels & storage
# ==============================================================================
def generate_stall_base(out_path):
    mesh = ObjMesh("SM_Stall_Base")
    w = 1.80
    d = 0.80
    y_bot = 0.10
    y_top = 0.96
    
    # 1. Structural steel perimeter chassis (50mm square tubing)
    tube = 0.04
    # Bottom frame
    mesh.add_box(-w/2, w/2, y_bot, y_bot + tube, d/2 - tube, d/2)            # Front
    mesh.add_box(-w/2, w/2, y_bot, y_bot + tube, -d/2, -d/2 + tube)          # Back
    mesh.add_box(-w/2, -w/2 + tube, y_bot, y_bot + tube, -d/2 + tube, d/2 - tube) # Left
    mesh.add_box(w/2 - tube, w/2, y_bot, y_bot + tube, -d/2 + tube, d/2 - tube)  # Right
    
    # 4 Corner upright steel posts
    for sx in (-1, 1):
        for sz in (-1, 1):
            x = sx * (w/2 - tube/2)
            z = sz * (d/2 - tube/2)
            mesh.add_box(x - tube/2, x + tube/2, y_bot + tube, y_top - tube, z - tube/2, z + tube/2)
            
    # Top frame under counter
    mesh.add_box(-w/2, w/2, y_top - tube, y_top, d/2 - tube, d/2)
    mesh.add_box(-w/2, w/2, y_top - tube, y_top, -d/2, -d/2 + tube)
    mesh.add_box(-w/2, -w/2 + tube, y_top - tube, y_top, -d/2 + tube, d/2 - tube)
    mesh.add_box(w/2 - tube, w/2, y_top - tube, y_top, -d/2 + tube, d/2 - tube)

    # 2. Front 3D Sinusoidal Corrugated Metal Panel (+Z side)
    # 28 waves across 1.72m
    num_samples = 84 # 3 samples per wave
    x_coords = np.linspace(-w/2 + tube, w/2 - tube, num_samples)
    z_base = d/2 - 0.015
    amplitude = 0.012
    freq = 28.0 * 2.0 * math.pi / (w - 2*tube)
    
    # Build corrugated grid: 2 vertical slices (bottom and top)
    y_panel_bot = y_bot + tube
    y_panel_top = y_top - tube
    
    bot_v_indices = []
    top_v_indices = []
    normals = []
    
    for i, x in enumerate(x_coords):
        z_offset = amplitude * math.sin((x - (-w/2 + tube)) * freq)
        z = z_base + z_offset
        v_b = mesh.add_vertex(x, y_panel_bot, z)
        v_t = mesh.add_vertex(x, y_panel_top, z)
        bot_v_indices.append(v_b)
        top_v_indices.append(v_t)
        
        # Normal
        dz_dx = amplitude * freq * math.cos((x - (-w/2 + tube)) * freq)
        # Tangent: (1, 0, dz_dx), Y-vector: (0, 1, 0) -> Normal: (-dz_dx, 0, 1)
        nx = -dz_dx
        nz = 1.0
        n_len = math.sqrt(nx*nx + nz*nz)
        normals.append((nx/n_len, 0.0, nz/n_len))
        
    for i in range(num_samples - 1):
        v1 = bot_v_indices[i]
        v2 = bot_v_indices[i+1]
        v3 = top_v_indices[i+1]
        v4 = top_v_indices[i]
        
        u1 = i / (num_samples - 1)
        u2 = (i + 1) / (num_samples - 1)
        
        uv1 = mesh.add_uv(u1, 0.0)
        uv2 = mesh.add_uv(u2, 0.0)
        uv3 = mesh.add_uv(u2, 1.0)
        uv4 = mesh.add_uv(u1, 1.0)
        
        n1 = mesh.add_normal(*normals[i])
        n2 = mesh.add_normal(*normals[i+1])
        
        mesh.add_face([(v1, uv1, n1), (v2, uv2, n2), (v3, uv3, n2)])
        mesh.add_face([(v1, uv1, n1), (v3, uv3, n2), (v4, uv4, n1)])

    # 3. Left and Right Corrugated Side Panels (-X and +X)
    num_side_samples = 40
    z_side_coords = np.linspace(-d/2 + tube, d/2 - tube, num_side_samples)
    freq_side = 13.0 * 2.0 * math.pi / (d - 2*tube)
    
    # Left (-X)
    x_base_left = -w/2 + 0.015
    for sx, x_base, n_mult in [(-1, -w/2 + 0.015, -1.0), (1, w/2 - 0.015, 1.0)]:
        b_idx, t_idx, s_normals = [], [], []
        for i, z in enumerate(z_side_coords):
            x_offset = n_mult * amplitude * math.sin((z - (-d/2 + tube)) * freq_side)
            x = x_base + x_offset
            b_idx.append(mesh.add_vertex(x, y_panel_bot, z))
            t_idx.append(mesh.add_vertex(x, y_panel_top, z))
            dx_dz = n_mult * amplitude * freq_side * math.cos((z - (-d/2 + tube)) * freq_side)
            nx = n_mult * 1.0
            nz = -dx_dz
            l = math.sqrt(nx*nx + nz*nz)
            s_normals.append((nx/l, 0.0, nz/l))
            
        for i in range(num_side_samples - 1):
            v1, v2, v3, v4 = b_idx[i], b_idx[i+1], t_idx[i+1], t_idx[i]
            u1, u2 = i / (num_side_samples - 1), (i+1) / (num_side_samples - 1)
            uv1, uv2, uv3, uv4 = mesh.add_uv(u1, 0), mesh.add_uv(u2, 0), mesh.add_uv(u2, 1), mesh.add_uv(u1, 1)
            n1 = mesh.add_normal(*s_normals[i])
            n2 = mesh.add_normal(*s_normals[i+1])
            if n_mult > 0:
                mesh.add_face([(v1, uv1, n1), (v2, uv2, n2), (v3, uv3, n2)])
                mesh.add_face([(v1, uv1, n1), (v3, uv3, n2), (v4, uv4, n1)])
            else:
                mesh.add_face([(v1, uv1, n1), (v3, uv3, n2), (v2, uv2, n2)])
                mesh.add_face([(v1, uv1, n1), (v4, uv4, n1), (v3, uv3, n2)])

    # 4. Operator Side (-Z): Open storage shelves
    # Bottom floor plank
    mesh.add_box(-w/2 + tube, w/2 - tube, y_bot + 0.01, y_bot + 0.03, -d/2 + tube, d/2 - tube)
    # Middle shelf plank at y = 0.52
    mesh.add_box(-w/2 + tube, w/2 - tube, 0.50, 0.53, -d/2 + tube, d/2 - tube)
    # Center vertical partition support post
    mesh.add_box(-tube/2, tube/2, y_bot + 0.03, y_top - tube, -d/2 + tube, d/2 - tube)

    mesh.write_obj(out_path)

# ==============================================================================
# 2. SM_Stall_Counter.obj: Timber slab, drop-in cutouts & customer shelf
# ==============================================================================
def generate_stall_counter(out_path):
    mesh = ObjMesh("SM_Stall_Counter")
    w = 1.80
    d = 0.80
    y_top = 1.00 # GT-001 strict requirement
    thick = 0.04
    y_bot = y_top - thick
    
    # Countertop slab: 5 realistic wooden planks running along X from -w/2 to +w/2
    # Recessed drop-in areas:
    # 1. Ice Bin: X from -0.42 to -0.12, Z from -0.32 to -0.02
    # 2. Topping Station: X from +0.08 to +0.58, Z from -0.34 to +0.04
    # To model cutouts cleanly in geometry, we divide the counter into modular plank sections:
    
    # Rear operator plank section (behind cutouts, Z: -0.40 to -0.35)
    mesh.add_box(-w/2, w/2, y_bot, y_top, -d/2, -0.35, uv_scale=1.5)
    # Front customer plank section (in front of cutouts, Z: +0.05 to +d/2)
    mesh.add_box(-w/2, w/2, y_bot, y_top, 0.05, d/2, uv_scale=1.5)
    
    # Left wing (X: -w/2 to -0.42, Z: -0.35 to 0.05) - Prep/tea area
    mesh.add_box(-w/2, -0.42, y_bot, y_top, -0.35, 0.05, uv_scale=1.5)
    # Center divider between ice bin and topping station (X: -0.12 to 0.08, Z: -0.35 to 0.05)
    mesh.add_box(-0.12, 0.08, y_bot, y_top, -0.35, 0.05, uv_scale=1.5)
    # Right wing (X: 0.58 to w/2, Z: -0.35 to 0.05) - Grill/cake area
    mesh.add_box(0.58, w/2, y_bot, y_top, -0.35, 0.05, uv_scale=1.5)
    
    # Drop-in cutout recessed support flanges (stainless steel lip inside cutouts)
    flange_t = 0.015
    # Ice Bin flange
    mesh.add_box(-0.42, -0.12, y_bot - 0.02, y_bot, -0.35, -0.35 + flange_t)
    mesh.add_box(-0.42, -0.12, y_bot - 0.02, y_bot, 0.05 - flange_t, 0.05)
    mesh.add_box(-0.42, -0.42 + flange_t, y_bot - 0.02, y_bot, -0.35, 0.05)
    mesh.add_box(-0.12 - flange_t, -0.12, y_bot - 0.02, y_bot, -0.35, 0.05)
    
    # Topping Station flange
    mesh.add_box(0.08, 0.58, y_bot - 0.02, y_bot, -0.35, -0.35 + flange_t)
    mesh.add_box(0.08, 0.58, y_bot - 0.02, y_bot, 0.05 - flange_t, 0.05)
    mesh.add_box(0.08, 0.08 + flange_t, y_bot - 0.02, y_bot, -0.35, 0.05)
    mesh.add_box(0.58 - flange_t, 0.58, y_bot - 0.02, y_bot, -0.35, 0.05)

    # 3. Customer Front Shelf Bar (+Z side) as photographed in IMG_5236.JPG!
    # Contained within overall 0.80m depth (shelf ends at +d/2 = 0.40m, main slab ends at 0.32m)
    shelf_w = 1.70
    shelf_t = 0.03
    shelf_y_top = 0.95
    shelf_y_bot = shelf_y_top - shelf_t
    shelf_z_start = 0.32
    shelf_z_end = d/2 # exactly +0.40m
    
    # Main shelf plank
    mesh.add_box(-shelf_w/2, shelf_w/2, shelf_y_bot, shelf_y_top, shelf_z_start, shelf_z_end, uv_scale=1.5)
    
    # 4 Angled wooden support brackets underneath the shelf
    for bx in (-0.65, -0.22, 0.22, 0.65):
        # Angled bracket block
        mesh.add_box(bx - 0.02, bx + 0.02, shelf_y_bot - 0.12, shelf_y_bot, shelf_z_start, shelf_z_end)

    mesh.write_obj(out_path)

# ==============================================================================
# 3. SM_Stall_Frame.obj: A-frame lateral trusses, posts & roof rafters
# ==============================================================================
def generate_stall_frame(out_path):
    mesh = ObjMesh("SM_Stall_Frame")
    w = 1.80
    d = 0.80
    post_w = 0.05
    y_start = 1.00 # Countertop
    y_eave = 1.98  # Under-eave height
    y_ridge = 2.14 # Ridge beam under-roof height
    
    # 1. 4 Main Corner Vertical Posts
    for sx in (-1, 1):
        for sz in (-1, 1):
            px = sx * (w/2 - post_w/2)
            pz = sz * (d/2 - post_w/2)
            mesh.add_box(px - post_w/2, px + post_w/2, y_start, y_eave, pz - post_w/2, pz + post_w/2)

    # 2. Authentic A-frame / Inverted-V Lateral Truss Struts on Left & Right sides!
    # Meeting at the roof apex as photographed in IMG_5236.JPG & IMG_5237.JPG
    for sx in (-1, 1):
        x = sx * (w/2 - post_w/2)
        # Front diagonal strut: from (x, y_start, d/2) to (x, y_ridge, 0)
        p_front_bot = (x, y_start, d/2 - post_w/2)
        p_apex = (x, y_ridge, 0.0)
        p_rear_bot = (x, y_start, -d/2 + post_w/2)
        
        # Angled struts via cylinders or beveled beams
        mesh.add_cylinder(p_front_bot, p_apex, radius=0.024, segments=8)
        mesh.add_cylinder(p_rear_bot, p_apex, radius=0.024, segments=8)
        
        # Gusset plate at apex
        mesh.add_box(x - 0.025, x + 0.025, y_ridge - 0.08, y_ridge + 0.01, -0.05, 0.05)

    # 3. Longitudinal Tie Beams connecting the sides (strictly within -w/2 to +w/2)
    # Front eave beam
    mesh.add_box(-w/2 + post_w, w/2 - post_w, y_eave - post_w, y_eave, d/2 - post_w, d/2)
    # Rear eave beam
    mesh.add_box(-w/2 + post_w, w/2 - post_w, y_eave - post_w, y_eave, -d/2, -d/2 + post_w)
    # Central ridge purlin beam running along the peak
    mesh.add_box(-w/2 + post_w, w/2 - post_w, y_ridge - 0.04, y_ridge, -0.03, 0.03)
    
    # 4. Transverse rafters (3 cross rafters supporting the roof purlins)
    for rx in (-0.50, 0.0, 0.50):
        mesh.add_cylinder((rx, y_eave, d/2 - 0.03), (rx, y_ridge, 0.0), radius=0.020, segments=8)
        mesh.add_cylinder((rx, y_eave, -d/2 + 0.03), (rx, y_ridge, 0.0), radius=0.020, segments=8)
        
    # 5. Sign Mounting Brackets (extending forward from roof frame to mount PF_Sign_TramChanh_New)
    sign_z = d/2 - 0.01
    for bx in (-0.60, 0.60):
        mesh.add_box(bx - 0.02, bx + 0.02, y_eave - 0.02, y_eave + 0.05, sign_z - 0.05, sign_z + 0.01)

    mesh.write_obj(out_path)

# ==============================================================================
# 4. SM_Stall_Roof.obj: Pitched gabled canopy with 3D sinusoidal corrugation
# ==============================================================================
def generate_stall_roof(out_path):
    mesh = ObjMesh("SM_Stall_Roof")
    w = 1.80       # Exactly 1.80m wide matching stall width contract (GT-001)
    d = 0.80       # Exactly 0.80m deep matching stall depth contract (GT-001)
    y_ridge = 2.192 # Base ridge height so peak + ridge cap reaches exactly 2.2000m (GT-001)
    y_eave = 2.060  # Eaves slope down to 2.06m
    sheet_thick = 0.012
    amplitude = 0.008
    
    # Pitched roof has two slopes:
    # Front slope: Z from 0.0 to +d/2
    # Rear slope: Z from 0.0 to -d/2
    
    num_x = 76 # Corrugation samples across 1.90m (approx 25 waves)
    num_z = 16 # Slope samples from ridge to eave
    freq_x = 25.0 * 2.0 * math.pi / w
    
    x_coords = np.linspace(-w/2, w/2, num_x)
    
    # Function to generate corrugated surface for one slope
    def build_slope(z_start, z_end, is_front):
        z_coords = np.linspace(z_start, z_end, num_z)
        grid_top = []
        grid_bot = []
        normals_top = []
        
        for j, z in enumerate(z_coords):
            # Linear height along pitch
            pitch_t = abs(z) / (d/2)
            y_base = y_ridge * (1.0 - pitch_t) + y_eave * pitch_t
            
            row_top = []
            row_bot = []
            row_norm = []
            
            for i, x in enumerate(x_coords):
                corr = amplitude * math.sin((x - (-w/2)) * freq_x)
                y_corr = y_base + corr
                
                # Normal calculation
                # dy/dx = amplitude * freq_x * cos
                # dy/dz = (y_eave - y_ridge) / (d/2) * sign(z)
                dy_dx = amplitude * freq_x * math.cos((x - (-w/2)) * freq_x)
                dy_dz = (y_eave - y_ridge) / (d/2) * (1.0 if is_front else -1.0)
                
                # Normal = cross((1, dy_dx, 0), (0, dy_dz, 1))
                nx = -dy_dx
                ny = 1.0
                nz = -dy_dz
                l = math.sqrt(nx*nx + ny*ny + nz*nz)
                
                vt = mesh.add_vertex(x, y_corr, z)
                vb = mesh.add_vertex(x, y_corr - sheet_thick, z)
                row_top.append(vt)
                row_bot.append(vb)
                row_norm.append((nx/l, ny/l, nz/l))
                
            grid_top.append(row_top)
            grid_bot.append(row_bot)
            normals_top.append(row_norm)
            
        # Add quads for top surface
        for j in range(num_z - 1):
            for i in range(num_x - 1):
                v1 = grid_top[j][i]
                v2 = grid_top[j][i+1]
                v3 = grid_top[j+1][i+1]
                v4 = grid_top[j+1][i]
                
                u1 = i / (num_x - 1)
                u2 = (i + 1) / (num_x - 1)
                v_coord1 = j / (num_z - 1)
                v_coord2 = (j + 1) / (num_z - 1)
                
                uv1 = mesh.add_uv(u1, v_coord1)
                uv2 = mesh.add_uv(u2, v_coord1)
                uv3 = mesh.add_uv(u2, v_coord2)
                uv4 = mesh.add_uv(u1, v_coord2)
                
                n1 = mesh.add_normal(*normals_top[j][i])
                n2 = mesh.add_normal(*normals_top[j][i+1])
                n3 = mesh.add_normal(*normals_top[j+1][i+1])
                n4 = mesh.add_normal(*normals_top[j+1][i])
                
                if is_front:
                    mesh.add_face([(v1, uv1, n1), (v2, uv2, n2), (v3, uv3, n3)])
                    mesh.add_face([(v1, uv1, n1), (v3, uv3, n3), (v4, uv4, n4)])
                else:
                    mesh.add_face([(v1, uv1, n1), (v3, uv3, n3), (v2, uv2, n2)])
                    mesh.add_face([(v1, uv1, n1), (v4, uv4, n4), (v3, uv3, n3)])

    build_slope(0.0, d/2, is_front=True)
    build_slope(0.0, -d/2, is_front=False)
    
    # Ridge Cap Flashing covering the peak seam strictly within w = 1.80m and max y = 2.2000m
    cap_w = w
    mesh.add_box(-cap_w/2, cap_w/2, y_ridge - 0.01, 2.2000, -0.04, 0.04)
    
    # Gable end trim boards on Left and Right sides (recessed by radius so outer edge is w/2 and top is 2.2000m)
    rake_radius = 0.015
    for sx in (-1, 1):
        x = sx * (w/2 - rake_radius)
        # Front rake board
        mesh.add_cylinder((x, 2.200 - rake_radius, 0.0), (x, y_eave + 0.005, d/2 - rake_radius), radius=rake_radius, segments=8)
        # Rear rake board
        mesh.add_cylinder((x, 2.200 - rake_radius, 0.0), (x, y_eave + 0.005, -d/2 + rake_radius), radius=rake_radius, segments=8)

    mesh.write_obj(out_path)

# ==============================================================================
# 5. SM_Sign_TramChanh_New.obj: Lightbox with curved caps, rear-center pivot
# ==============================================================================
def generate_sign_mesh(out_path):
    mesh = ObjMesh("SM_Sign_TramChanh_New")
    # Exact specs from ASSET_INTEGRATION.md §5.2:
    # Size: approx 1.66m wide, 0.30m high, 0.12m deep.
    # Pivot: rear centre (0, 0, 0).
    # Box extends forward along +Z from Z=0 to Z=0.12.
    # Height extends from Y = -0.15 to Y = +0.15.
    # Width extends from X = -0.83 to X = +0.83.
    w = 1.66
    h = 0.30
    depth = 0.12
    y_min = -h/2
    y_max = h/2
    z_min = 0.0
    z_max = depth
    
    # End-cap rounded curvature
    r_cap = 0.04
    cap_segs = 8
    
    # 1. Front Diffuser Face (translucent acrylic panel at Z = z_max)
    # UV maps to the front banner region [0.15, 0.0] to [0.85, 1.0] of T_Sign_TramChanh_New_BaseColor.png
    v_flb = mesh.add_vertex(-w/2 + r_cap, y_min, z_max)
    v_frb = mesh.add_vertex(w/2 - r_cap, y_min, z_max)
    v_frt = mesh.add_vertex(w/2 - r_cap, y_max, z_max)
    v_flt = mesh.add_vertex(-w/2 + r_cap, y_max, z_max)
    
    uv_flb = mesh.add_uv(0.15, 0.0)
    uv_frb = mesh.add_uv(0.85, 0.0)
    uv_frt = mesh.add_uv(0.85, 1.0)
    uv_flt = mesh.add_uv(0.15, 1.0)
    
    n_f = mesh.add_normal(0, 0, 1)
    mesh.add_face([(v_flb, uv_flb, n_f), (v_frb, uv_frb, n_f), (v_frt, uv_frt, n_f)])
    mesh.add_face([(v_flb, uv_flb, n_f), (v_frt, uv_frt, n_f), (v_flt, uv_flt, n_f)])

    # 2. Left Curved End-Cap (X: -w/2 to -w/2 + r_cap)
    # Radiates from Z = z_max - r_cap to z_min
    # UV maps to [0.0, 0.0] to [0.15, 1.0]
    left_top_v = []
    left_bot_v = []
    left_normals = []
    center_x = -w/2 + r_cap
    center_z = z_max - r_cap
    
    for i in range(cap_segs + 1):
        angle = math.pi * 0.5 * (i / cap_segs) # 0 to pi/2 (pointing -X to +Z)
        cos_a = math.cos(angle)
        sin_a = math.sin(angle)
        px = center_x - r_cap * cos_a
        pz = center_z + r_cap * sin_a
        
        vb = mesh.add_vertex(px, y_min, pz)
        vt = mesh.add_vertex(px, y_max, pz)
        left_bot_v.append(vb)
        left_top_v.append(vt)
        
        nx = -cos_a
        nz = sin_a
        left_normals.append((nx, 0.0, nz))
        
    for i in range(cap_segs):
        v1, v2, v3, v4 = left_bot_v[i], left_bot_v[i+1], left_top_v[i+1], left_top_v[i]
        u1 = 0.15 * (i / cap_segs)
        u2 = 0.15 * ((i + 1) / cap_segs)
        uv1, uv2, uv3, uv4 = mesh.add_uv(u1, 0), mesh.add_uv(u2, 0), mesh.add_uv(u2, 1), mesh.add_uv(u1, 1)
        n1 = mesh.add_normal(*left_normals[i])
        n2 = mesh.add_normal(*left_normals[i+1])
        mesh.add_face([(v1, uv1, n1), (v2, uv2, n2), (v3, uv3, n2)])
        mesh.add_face([(v1, uv1, n1), (v3, uv3, n2), (v4, uv4, n1)])

    # 3. Right Curved End-Cap (X: w/2 - r_cap to w/2)
    # UV maps to [0.85, 0.0] to [1.0, 1.0]
    right_top_v = []
    right_bot_v = []
    right_normals = []
    center_x_r = w/2 - r_cap
    
    for i in range(cap_segs + 1):
        angle = math.pi * 0.5 * (i / cap_segs)
        cos_a = math.cos(angle)
        sin_a = math.sin(angle)
        px = center_x_r + r_cap * sin_a
        pz = center_z + r_cap * cos_a
        
        vb = mesh.add_vertex(px, y_min, pz)
        vt = mesh.add_vertex(px, y_max, pz)
        right_bot_v.append(vb)
        right_top_v.append(vt)
        right_normals.append((sin_a, 0.0, cos_a))
        
    for i in range(cap_segs):
        v1, v2, v3, v4 = right_bot_v[i], right_bot_v[i+1], right_top_v[i+1], right_top_v[i]
        u1 = 0.85 + 0.15 * (i / cap_segs)
        u2 = 0.85 + 0.15 * ((i + 1) / cap_segs)
        uv1, uv2, uv3, uv4 = mesh.add_uv(u1, 0), mesh.add_uv(u2, 0), mesh.add_uv(u2, 1), mesh.add_uv(u1, 1)
        n1 = mesh.add_normal(*right_normals[i])
        n2 = mesh.add_normal(*right_normals[i+1])
        mesh.add_face([(v1, uv1, n1), (v2, uv2, n2), (v3, uv3, n2)])
        mesh.add_face([(v1, uv1, n1), (v3, uv3, n2), (v4, uv4, n1)])

    # 4. Top and Bottom and Back Enclosure
    # Top face
    mesh.add_quad(
        mesh.add_vertex(-w/2, y_max, z_max), mesh.add_vertex(w/2, y_max, z_max),
        mesh.add_vertex(w/2, y_max, z_min), mesh.add_vertex(-w/2, y_max, z_min),
        (0, 0.95), (1, 0.95), (1, 1.0), (0, 1.0), (0, 1, 0))
    # Bottom face
    mesh.add_quad(
        mesh.add_vertex(-w/2, y_min, z_min), mesh.add_vertex(w/2, y_min, z_min),
        mesh.add_vertex(w/2, y_min, z_max), mesh.add_vertex(-w/2, y_min, z_max),
        (0, 0), (1, 0), (1, 0.05), (0, 0.05), (0, -1, 0))
    # Back face at Z = z_min
    mesh.add_quad(
        mesh.add_vertex(w/2, y_min, z_min), mesh.add_vertex(-w/2, y_min, z_min),
        mesh.add_vertex(-w/2, y_max, z_min), mesh.add_vertex(w/2, y_max, z_min),
        (0, 0), (1, 0), (1, 1), (0, 1), (0, 0, -1))

    mesh.write_obj(out_path)

# ==============================================================================
# 6. SM_Stall_CasterWheel.obj: Heavy-duty swivel caster wheel with step brake
# ==============================================================================
def generate_caster_wheel(out_path):
    mesh = ObjMesh("SM_Stall_CasterWheel")
    # Pivot at top mounting plate center (0, 0, 0)
    # Extends downward to y = -0.10m
    plate_size = 0.08
    plate_t = 0.006
    
    # Top 4-bolt mounting flange plate
    mesh.add_box(-plate_size/2, plate_size/2, -plate_t, 0.0, -plate_size/2, plate_size/2)
    # Turntable swivel bearing ring
    mesh.add_cylinder((0, -plate_t, 0), (0, -0.02, 0), radius=0.028, segments=12)
    
    # Swivel fork legs (left and right stamped steel brackets)
    fork_leg_t = 0.005
    fork_w = 0.045
    mesh.add_box(-fork_w/2 - fork_leg_t, -fork_w/2, -0.065, -0.018, -0.022, 0.022)
    mesh.add_box(fork_w/2, fork_w/2 + fork_leg_t, -0.065, -0.018, -0.022, 0.022)
    
    # Axle bolt pin along X
    wheel_y_center = -0.050
    wheel_radius = 0.050 # 100mm diameter
    wheel_width = 0.038
    mesh.add_cylinder((-fork_w/2 - 0.008, wheel_y_center, 0), (fork_w/2 + 0.008, wheel_y_center, 0), radius=0.007, segments=8)
    
    # Heavy rubber wheel along X axis
    mesh.add_cylinder((-wheel_width/2, wheel_y_center, 0), (wheel_width/2, wheel_y_center, 0), radius=wheel_radius, segments=20)
    
    # Step brake pedal lever extending diagonally
    mesh.add_box(-0.015, 0.015, -0.035, -0.025, 0.025, 0.060)

    mesh.write_obj(out_path)

# ==============================================================================
# 7. SM_EdisonBulb.obj: Hanging vintage drop cord, brass socket & filament bulb
# ==============================================================================
def generate_edison_bulb(out_path):
    mesh = ObjMesh("SM_EdisonBulb")
    # Pivot at top attachment point (0, 0, 0)
    # Drops down along -Y
    
    # Ceiling wire grip clamp
    mesh.add_cylinder((0, 0, 0), (0, -0.02, 0), radius=0.012, segments=8)
    # Twisted drop cord (length 0.18m)
    mesh.add_cylinder((0, -0.02, 0), (0, -0.18, 0), radius=0.004, segments=8)
    
    # Vintage brass E27 socket
    y_sock_top = -0.18
    y_sock_bot = -0.24
    mesh.add_cylinder((0, y_sock_top, 0), (0, y_sock_bot, 0), radius=0.018, segments=16)
    # Knurled threaded ring
    mesh.add_cylinder((0, y_sock_bot + 0.015, 0), (0, y_sock_bot + 0.035, 0), radius=0.021, segments=16)
    
    # Glass teardrop bulb envelope (lathed profile)
    # Lathed points from socket base y = -0.24 down to bulb tip y = -0.36
    bulb_profile = [
        (0.016, -0.240),
        (0.022, -0.255),
        (0.032, -0.280),
        (0.035, -0.300),
        (0.032, -0.325),
        (0.022, -0.345),
        (0.008, -0.358),
        (0.000, -0.362)
    ]
    
    segs = 16
    for p_idx in range(len(bulb_profile) - 1):
        r1, y1 = bulb_profile[p_idx]
        r2, y2 = bulb_profile[p_idx + 1]
        
        v_ring1 = []
        v_ring2 = []
        normals1 = []
        normals2 = []
        
        for i in range(segs):
            angle = 2.0 * math.pi * i / segs
            cos_a = math.cos(angle)
            sin_a = math.sin(angle)
            
            v_ring1.append(mesh.add_vertex(r1 * cos_a, y1, r1 * sin_a))
            v_ring2.append(mesh.add_vertex(r2 * cos_a, y2, r2 * sin_a))
            normals1.append((cos_a, 0.2, sin_a))
            normals2.append((cos_a, 0.2, sin_a))
            
        for i in range(segs):
            next_i = (i + 1) % segs
            v1, v2, v3, v4 = v_ring1[i], v_ring1[next_i], v_ring2[next_i], v_ring2[i]
            uv1, uv2 = mesh.add_uv(i/segs, p_idx/len(bulb_profile)), mesh.add_uv((i+1)/segs, p_idx/len(bulb_profile))
            uv3, uv4 = mesh.add_uv((i+1)/segs, (p_idx+1)/len(bulb_profile)), mesh.add_uv(i/segs, (p_idx+1)/len(bulb_profile))
            n1 = mesh.add_normal(*normals1[i])
            n2 = mesh.add_normal(*normals1[next_i])
            n3 = mesh.add_normal(*normals2[next_i])
            n4 = mesh.add_normal(*normals2[i])
            mesh.add_face([(v1, uv1, n1), (v2, uv2, n2), (v3, uv3, n3)])
            mesh.add_face([(v1, uv1, n1), (v3, uv3, n3), (v4, uv4, n4)])

    # Internal glowing filament loop
    mesh.add_cylinder((0, -0.25, 0), (0, -0.28, 0), radius=0.002, segments=6)
    mesh.add_box(-0.012, 0.012, -0.32, -0.28, -0.002, 0.002)

    mesh.write_obj(out_path)

# ==============================================================================
# 8. SM_LEDStrip.obj: Under-counter aluminum channel and diffuser lens
# ==============================================================================
def generate_led_strip(out_path):
    mesh = ObjMesh("SM_LEDStrip")
    # Length 1.68m along X, mounted under customer shelf facing downward (-Y)
    length = 1.68
    chan_w = 0.020
    chan_h = 0.012
    
    # Aluminum U-channel housing
    mesh.add_box(-length/2, length/2, -chan_h, 0.0, -chan_w/2, chan_w/2)
    # Frosted diffuser lens strip
    mesh.add_box(-length/2 + 0.005, length/2 - 0.005, -chan_h - 0.003, -chan_h, -chan_w/2 + 0.002, chan_w/2 - 0.002)

    mesh.write_obj(out_path)

# ==============================================================================
# Main Generator Runner
# ==============================================================================
def generate_all():
    stall_dir = "Assets/TramChanh/Art/Models/Stall"
    brand_dir = "Assets/TramChanh/Art/Models/Branding"
    light_dir = "Assets/TramChanh/Art/Models/Lighting"
    
    os.makedirs(stall_dir, exist_ok=True)
    os.makedirs(brand_dir, exist_ok=True)
    os.makedirs(light_dir, exist_ok=True)
    
    generate_stall_base(os.path.join(stall_dir, "SM_Stall_Base.obj"))
    generate_stall_counter(os.path.join(stall_dir, "SM_Stall_Counter.obj"))
    generate_stall_frame(os.path.join(stall_dir, "SM_Stall_Frame.obj"))
    generate_stall_roof(os.path.join(stall_dir, "SM_Stall_Roof.obj"))
    generate_caster_wheel(os.path.join(stall_dir, "SM_Stall_CasterWheel.obj"))
    generate_sign_mesh(os.path.join(brand_dir, "SM_Sign_TramChanh_New.obj"))
    generate_edison_bulb(os.path.join(light_dir, "SM_EdisonBulb.obj"))
    generate_led_strip(os.path.join(light_dir, "SM_LEDStrip.obj"))
    print("All 8 high-fidelity OBJ meshes generated successfully!")

if __name__ == "__main__":
    generate_all()
