#!/usr/bin/env python3
"""
Procedural 3D Mesh Generator for Tram Chanh Cake Station Equipment (Agent D)
Outputs native Wavefront OBJ files with normals and UVs:
- SM_Grill_Elmich_Base.obj (Lower unit, ribbed plate, digital 160°C panel, feet)
- SM_Grill_Elmich_Lid.obj (Upper floating hood, ribbed plate, rear hinge, front handle)
- SM_BatterMeasureCup_500ml.obj (500ml graduated cup with spout and batter volume)
- SM_SauceBag.obj (Squeeze piping bag)
- SM_Spatula.obj (Crepe spatula with offset stainless blade and wood handle)
- SM_Scissors.obj (Heavy-duty red kitchen scissors from IMG_5245.JPG)
- SM_RollCake_Baked.obj (Toasted rolled cake from IMG_5210.JPG)
- SM_RollCake_Wrapper.obj (Kraft paper wrapper from IMG_5212.JPG)
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
# 1. Elmich Contact Grill Base & Lid (IMG_5244.JPG)
# ==============================================================================
def generate_grill_base(out_path):
    mesh = ObjMesh("SM_Grill_Elmich_Base")
    # Base dimensions: 340mm wide, 300mm deep, 80mm high
    w = 0.34
    d = 0.30
    h = 0.08
    
    # 4 Silicone rubber feet
    foot_r = 0.015
    for sx in [-1, 1]:
        for sz in [-1, 1]:
            mesh.add_cylinder((sx * (w/2 - 0.03), 0.0, sz * (d/2 - 0.03)),
                              (sx * (w/2 - 0.03), 0.012, sz * (d/2 - 0.03)), radius=foot_r, segments=10)
            
    # Main lower chassis (molded matte black phenolic body)
    mesh.add_box(-w/2, w/2, 0.012, h, -d/2, d/2)
    
    # Lower non-stick ribbed cast plate ($280\text{ mm} \times 220\text{ mm}$)
    pw = 0.28
    pd = 0.22
    py = h
    mesh.add_box(-pw/2, pw/2, py, py + 0.012, -pd/2 + 0.02, pd/2 + 0.02)
    
    # Ribbed grilling ridges on plate (14 longitudinal ridges)
    num_ribs = 14
    rib_xs = np.linspace(-pw/2 + 0.01, pw/2 - 0.01, num_ribs)
    for rx in rib_xs:
        mesh.add_box(rx - 0.003, rx + 0.003, py + 0.012, py + 0.016, -pd/2 + 0.025, pd/2 + 0.015)
        
    # Front angled digital control panel
    # Recessed display window for 160°C readout
    mesh.add_box(-0.06, 0.06, 0.025, 0.065, d/2 - 0.005, d/2 + 0.002)
    
    # Rear heavy hinge bracket towers (where LidPivot attaches at z = -d/2 + 0.02)
    mesh.add_box(-w*0.38, -w*0.32, h, h + 0.045, -d/2 + 0.01, -d/2 + 0.035)
    mesh.add_box( w*0.32,  w*0.38, h, h + 0.045, -d/2 + 0.01, -d/2 + 0.035)
    
    mesh.write_obj(out_path)

def generate_grill_lid(out_path):
    mesh = ObjMesh("SM_Grill_Elmich_Lid")
    # Pivot is at rear hinge axis: (0, 0, 0) relative to LidPivot!
    # Lid extends forward along +Z from Z = 0 to Z = 0.28m
    w = 0.34
    lid_len = 0.28
    lid_t = 0.045
    
    # Dual rear hinge connecting arms
    arm_w = 0.022
    mesh.add_box(-w*0.38, -w*0.38 + arm_w, -0.015, 0.015, -0.02, 0.04)
    mesh.add_box( w*0.38 - arm_w,  w*0.38, -0.015, 0.015, -0.02, 0.04)
    
    # Upper floating hood body
    mesh.add_box(-w/2, w/2, 0.01, 0.01 + lid_t, 0.04, 0.04 + lid_len)
    
    # Stainless top decorative accent cover plate
    mesh.add_box(-w/2 + 0.02, w/2 - 0.02, 0.01 + lid_t, 0.01 + lid_t + 0.003, 0.06, 0.04 + lid_len - 0.02)
    
    # Upper ribbed grill plate (facing downward)
    pw = 0.28
    pd = 0.22
    mesh.add_box(-pw/2, pw/2, -0.008, 0.01, 0.06, 0.06 + pd)
    
    # Ribbed ridges facing downward
    num_ribs = 14
    rib_xs = np.linspace(-pw/2 + 0.01, pw/2 - 0.01, num_ribs)
    for rx in rib_xs:
        mesh.add_box(rx - 0.003, rx + 0.003, -0.012, -0.008, 0.065, 0.06 + pd - 0.005)
        
    # Floating front tubular handle
    handle_z = 0.04 + lid_len + 0.04
    # Dual support standoffs
    mesh.add_cylinder((-w*0.30, 0.025, 0.04 + lid_len), (-w*0.30, 0.025, handle_z), radius=0.008, segments=8)
    mesh.add_cylinder(( w*0.30, 0.025, 0.04 + lid_len), ( w*0.30, 0.025, handle_z), radius=0.008, segments=8)
    # Transverse handle bar
    mesh.add_cylinder((-w*0.35, 0.025, handle_z), (w*0.35, 0.025, handle_z), radius=0.012, segments=12)
    
    mesh.write_obj(out_path)

# ==============================================================================
# 2. 500ml Batter Measuring Cup & Batter Liquid
# ==============================================================================
def generate_measuring_cup(out_path):
    mesh = ObjMesh("SM_BatterMeasureCup_500ml")
    # Tapered beaker: bottom r = 0.042m, top r = 0.052m, height = 0.135m
    h = 0.135
    segs = 18
    r_bot = 0.042
    r_top = 0.052
    
    angles = np.linspace(0, 2*math.pi, segs+1)
    
    # Outer beaker wall
    for i in range(segs):
        a1, a2 = angles[i], angles[i+1]
        v1 = mesh.add_vertex(r_bot * math.cos(a1), 0.0, r_bot * math.sin(a1))
        v2 = mesh.add_vertex(r_bot * math.cos(a2), 0.0, r_bot * math.sin(a2))
        v3 = mesh.add_vertex(r_top * math.cos(a2), h,   r_top * math.sin(a2))
        v4 = mesh.add_vertex(r_top * math.cos(a1), h,   r_top * math.sin(a1))
        
        n_out = (math.cos(a1), 0.1, math.sin(a1))
        mesh.add_quad(v1, v2, v3, v4, (i/segs, 0), ((i+1)/segs, 0), ((i+1)/segs, 1), (i/segs, 1), n_out)
        
    # Pouring spout on +Z rim
    mesh.add_box(-0.015, 0.015, h - 0.005, h + 0.012, r_top - 0.005, r_top + 0.018)
    
    # Open C-shaped handle on -Z side
    mesh.add_cylinder((0, h * 0.20, -r_bot - 0.005), (0, h * 0.20, -r_bot - 0.035), radius=0.006, segments=8)
    mesh.add_cylinder((0, h * 0.20, -r_bot - 0.035), (0, h * 0.85, -r_top - 0.035), radius=0.006, segments=8)
    mesh.add_cylinder((0, h * 0.85, -r_top - 0.035), (0, h * 0.85, -r_top - 0.005), radius=0.006, segments=8)
    
    # Internal liquid batter volume (approx 350ml filled, yellow batter)
    h_bat = h * 0.65
    r_bat = r_bot + (r_top - r_bot) * 0.65 - 0.003
    mesh.add_cylinder((0, 0.005, 0), (0, h_bat, 0), radius=r_bat, segments=16)
    
    mesh.write_obj(out_path)

# ==============================================================================
# 3. Sauce Piping Bag & Crepe Spatula
# ==============================================================================
def generate_sauce_bag(out_path):
    mesh = ObjMesh("SM_SauceBag")
    # Triangular squeeze pouch: apex nozzle at y = 0, expands to round top at y = 0.22m
    h = 0.22
    r_top = 0.045
    segs = 12
    angles = np.linspace(0, 2*math.pi, segs+1)
    
    v_tip = mesh.add_vertex(0, 0, 0)
    for i in range(segs):
        a1, a2 = angles[i], angles[i+1]
        v1 = mesh.add_vertex(r_top * math.cos(a1), h, r_top * math.sin(a1))
        v2 = mesh.add_vertex(r_top * math.cos(a2), h, r_top * math.sin(a2))
        n = (math.cos(a1), 0.2, math.sin(a1))
        n_idx = mesh.add_normal(*n)
        uv1, uv2 = mesh.add_uv(i/segs, 1), mesh.add_uv((i+1)/segs, 1)
        uv_tip = mesh.add_uv(0.5, 0)
        mesh.add_face([(v_tip, uv_tip, n_idx), (v1, uv1, n_idx), (v2, uv2, n_idx)])
        
    # Top twist seal
    mesh.add_cylinder((0, h, 0), (0, h + 0.025, 0), radius=0.012, segments=8)
    mesh.write_obj(out_path)

def generate_spatula(out_path):
    mesh = ObjMesh("SM_Spatula")
    # Total length 320mm: wood handle 110mm, offset crank 35mm, stainless blade 175mm
    # Handle along Z: -0.16m to -0.05m
    mesh.add_cylinder((0, 0.025, -0.16), (0, 0.025, -0.05), radius=0.013, segments=12)
    # Offset crank neck
    mesh.add_box(-0.010, 0.010, 0.005, 0.025, -0.05, -0.02)
    # Long flexible blade (width 32mm, thickness 1.5mm, length 175mm)
    mesh.add_box(-0.016, 0.016, 0.003, 0.005, -0.02, 0.16)
    # Rounded tip
    mesh.add_cylinder((0, 0.004, 0.155), (0, 0.004, 0.165), radius=0.016, segments=10)
    mesh.write_obj(out_path)

# ==============================================================================
# 4. Kitchen Scissors (IMG_5245.JPG)
# ==============================================================================
def generate_scissors(out_path):
    mesh = ObjMesh("SM_Scissors")
    # Overall length 210mm
    # Dual blades (stainless steel) along +Z: 0 to 0.105m
    mesh.add_box(-0.008, 0.000, -0.002, 0.002, 0.0, 0.105)
    mesh.add_box( 0.000, 0.008, -0.002, 0.002, 0.0, 0.105)
    # Center pivot brass screw
    mesh.add_cylinder((0, -0.005, 0.01), (0, 0.005, 0.01), radius=0.006, segments=10)
    
    # Red molded ergonomic finger bows extending -Z: 0.01m to -0.095m
    # Thumb bow (left)
    mesh.add_cylinder((-0.022, 0, -0.04), (-0.022, 0, -0.08), radius=0.014, segments=10)
    # Multi-finger loop (right)
    mesh.add_cylinder(( 0.025, 0, -0.03), ( 0.025, 0, -0.09), radius=0.018, segments=10)
    mesh.write_obj(out_path)

# ==============================================================================
# 5. Rolled Cake & Kraft Paper Wrapper (IMG_5210.JPG - IMG_5213.JPG)
# ==============================================================================
def generate_roll_cake(cake_path, wrapper_path):
    # 5.1 Baked Rolled Cake ("Bánh Lăn Nướng")
    # Dimensions: diameter 55mm, length 180mm
    cake = ObjMesh("SM_RollCake_Baked")
    r_cake = 0.028
    l_cake = 0.180
    cake.add_cylinder((0, r_cake, -l_cake/2), (0, r_cake, l_cake/2), radius=r_cake, segments=20)
    cake.write_obj(cake_path)
    
    # 5.2 Brown Kraft Paper Wrapper sleeve
    # Wraps around middle 120mm of cake with folded seam
    wrapper = ObjMesh("SM_RollCake_Wrapper")
    r_wrap = r_cake + 0.002
    l_wrap = 0.120
    wrapper.add_cylinder((0, r_cake, -l_wrap/2), (0, r_cake, l_wrap/2), radius=r_wrap, segments=20)
    # Folded flap seal
    wrapper.add_box(-0.015, 0.015, r_cake * 2.0, r_cake * 2.0 + 0.004, -l_wrap/2, l_wrap/2)
    wrapper.write_obj(wrapper_path)

def generate_all_cake_assets():
    models_dir = "Assets/TramChanh/Art/Models/CakeStation"
    food_dir = "Assets/TramChanh/Art/Models/Food"
    props_dir = "Assets/TramChanh/Art/Models/Props"
    os.makedirs(models_dir, exist_ok=True)
    os.makedirs(food_dir, exist_ok=True)
    os.makedirs(props_dir, exist_ok=True)
    
    generate_grill_base(os.path.join(models_dir, "SM_Grill_Elmich_Base.obj"))
    generate_grill_lid(os.path.join(models_dir, "SM_Grill_Elmich_Lid.obj"))
    generate_measuring_cup(os.path.join(props_dir, "SM_BatterMeasureCup_500ml.obj"))
    generate_sauce_bag(os.path.join(props_dir, "SM_SauceBag.obj"))
    generate_spatula(os.path.join(props_dir, "SM_Spatula.obj"))
    generate_scissors(os.path.join(props_dir, "SM_Scissors.obj"))
    generate_roll_cake(
        os.path.join(food_dir, "SM_RollCake_Baked.obj"),
        os.path.join(food_dir, "SM_RollCake_Wrapper.obj")
    )
    print("All Cake Station 3D Meshes successfully generated!")

if __name__ == "__main__":
    generate_all_cake_assets()
