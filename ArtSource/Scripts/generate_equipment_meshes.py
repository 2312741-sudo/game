#!/usr/bin/env python3
import math
import os

class ObjWriter:
    def __init__(self, name):
        self.name = name
        self.verts = []
        self.uvs = []
        self.normals = []
        self.faces = []

    def add_vert(self, x, y, z):
        self.verts.append((x, y, z))
        return len(self.verts)

    def add_uv(self, u, v):
        self.uvs.append((u, v))
        return len(self.uvs)

    def add_normal(self, nx, ny, nz):
        l = math.sqrt(nx*nx + ny*ny + nz*nz) or 1.0
        self.normals.append((nx/l, ny/l, nz/l))
        return len(self.normals)

    def add_quad(self, v1, v2, v3, v4, uv1, uv2, uv3, uv4, n):
        self.faces.append([(v1, uv1, n), (v2, uv2, n), (v3, uv3, n), (v4, uv4, n)])

    def add_tri(self, v1, v2, v3, uv1, uv2, uv3, n):
        self.faces.append([(v1, uv1, n), (v2, uv2, n), (v3, uv3, n)])

    def add_box(self, x0, x1, y0, y1, z0, z1, u_min=0.1, u_max=0.6, v_min=0.1, v_max=0.6):
        v000 = self.add_vert(x0, y0, z0)
        v100 = self.add_vert(x1, y0, z0)
        v110 = self.add_vert(x1, y1, z0)
        v010 = self.add_vert(x0, y1, z0)
        v001 = self.add_vert(x0, y0, z1)
        v101 = self.add_vert(x1, y0, z1)
        v111 = self.add_vert(x1, y1, z1)
        v011 = self.add_vert(x0, y1, z1)

        uv00 = self.add_uv(u_min, v_min)
        uv10 = self.add_uv(u_max, v_min)
        uv11 = self.add_uv(u_max, v_max)
        uv01 = self.add_uv(u_min, v_max)

        n_front = self.add_normal(0, 0, 1)
        n_back = self.add_normal(0, 0, -1)
        n_top = self.add_normal(0, 1, 0)
        n_bot = self.add_normal(0, -1, 0)
        n_right = self.add_normal(1, 0, 0)
        n_left = self.add_normal(-1, 0, 0)

        # Front (+Z)
        self.add_quad(v001, v101, v111, v011, uv00, uv10, uv11, uv01, n_front)
        # Back (-Z)
        self.add_quad(v100, v000, v010, v110, uv00, uv10, uv11, uv01, n_back)
        # Top (+Y)
        self.add_quad(v011, v111, v110, v010, uv00, uv10, uv11, uv01, n_top)
        # Bottom (-Y)
        self.add_quad(v000, v100, v101, v001, uv00, uv10, uv11, uv01, n_bot)
        # Right (+X)
        self.add_quad(v101, v100, v110, v111, uv00, uv10, uv11, uv01, n_right)
        # Left (-X)
        self.add_quad(v000, v001, v011, v010, uv00, uv10, uv11, uv01, n_left)

    def add_cylinder(self, cx, cz, r, y0, y1, segments=8, u_min=0.1, u_max=0.6, v_min=0.1, v_max=0.6):
        top_verts = []
        bot_verts = []
        normals = []
        for i in range(segments):
            angle = 2 * math.pi * i / segments
            dx = math.cos(angle) * r
            dz = math.sin(angle) * r
            bot_verts.append(self.add_vert(cx + dx, y0, cz + dz))
            top_verts.append(self.add_vert(cx + dx, y1, cz + dz))
            normals.append(self.add_normal(math.cos(angle), 0, math.sin(angle)))

        n_top = self.add_normal(0, 1, 0)
        n_bot = self.add_normal(0, -1, 0)
        c_top = self.add_vert(cx, y1, cz)
        c_bot = self.add_vert(cx, y0, cz)
        uv_c = self.add_uv((u_min+u_max)/2, (v_min+v_max)/2)

        for i in range(segments):
            nxt = (i + 1) % segments
            u0 = u_min + (u_max - u_min) * (i / segments)
            u1 = u_min + (u_max - u_min) * ((i + 1) / segments)
            uv_b0 = self.add_uv(u0, v_min)
            uv_b1 = self.add_uv(u1, v_min)
            uv_t1 = self.add_uv(u1, v_max)
            uv_t0 = self.add_uv(u0, v_max)

            # Side quad
            self.add_quad(bot_verts[i], bot_verts[nxt], top_verts[nxt], top_verts[i],
                          uv_b0, uv_b1, uv_t1, uv_t0, normals[i])
            # Top fan
            self.add_tri(c_top, top_verts[nxt], top_verts[i], uv_c, uv_t1, uv_t0, n_top)
            # Bottom fan
            self.add_tri(c_bot, bot_verts[i], bot_verts[nxt], uv_c, uv_b0, uv_b1, n_bot)

    def write(self, filepath):
        with open(filepath, 'w') as f:
            f.write(f"# Wavefront OBJ: {self.name}\n")
            f.write(f"o {self.name}\n")
            for v in self.verts:
                f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
            for vt in self.uvs:
                f.write(f"vt {vt[0]:.6f} {vt[1]:.6f}\n")
            for vn in self.normals:
                f.write(f"vn {vn[0]:.6f} {vn[1]:.6f} {vn[2]:.6f}\n")
            for face in self.faces:
                f.write("f " + " ".join(f"{v}/{vt}/{vn}" for v, vt, vn in face) + "\n")
        print(f"Generated {filepath}: {len(self.verts)} verts, {len(self.faces)} faces")

def build_icebin_well():
    obj = ObjWriter("SM_IceBin_Well")
    # Stainless outer flange resting on counter
    # Outer bounds: X: [-0.250, 0.250], Y: [0.000, 0.006], Z: [-0.225, 0.225]
    # Well opening: X: [-0.218, 0.218], Z: [-0.193, 0.193]
    # Left flange
    obj.add_box(-0.250, -0.218, 0.000, 0.006, -0.225, 0.225, 0.05, 0.25, 0.05, 0.65)
    # Right flange
    obj.add_box(0.218, 0.250, 0.000, 0.006, -0.225, 0.225, 0.05, 0.25, 0.05, 0.65)
    # Back flange (+Z)
    obj.add_box(-0.218, 0.218, 0.000, 0.006, 0.193, 0.225, 0.05, 0.65, 0.05, 0.25)
    # Front flange (-Z, seller side)
    obj.add_box(-0.218, 0.218, 0.000, 0.006, -0.225, -0.193, 0.05, 0.65, 0.05, 0.25)

    # Well walls going down to y = -0.320
    # Left wall
    obj.add_box(-0.218, -0.208, -0.320, 0.000, -0.193, 0.193, 0.1, 0.3, 0.1, 0.6)
    # Right wall
    obj.add_box(0.208, 0.218, -0.320, 0.000, -0.193, 0.193, 0.1, 0.3, 0.1, 0.6)
    # Back wall
    obj.add_box(-0.208, 0.208, -0.320, 0.000, 0.183, 0.193, 0.1, 0.6, 0.1, 0.3)
    # Front wall
    obj.add_box(-0.208, 0.208, -0.320, 0.000, -0.193, -0.183, 0.1, 0.6, 0.1, 0.3)
    # Well bottom
    obj.add_box(-0.208, 0.208, -0.320, -0.305, -0.183, 0.183, 0.1, 0.6, 0.1, 0.6)

    # Black studded rubber bar mat in front of ice bin (facing seller)
    # UV maps to rubber mat atlas region U: [0.73, 1.0], V: [0.0, 0.40]
    obj.add_box(-0.240, 0.240, 0.006, 0.009, -0.225, -0.160, 0.73, 0.99, 0.02, 0.38)

    # Narrow rubber runner strip on right side for ice scoop
    obj.add_box(0.180, 0.245, 0.006, 0.009, -0.160, 0.200, 0.73, 0.99, 0.02, 0.38)

    obj.write("Assets/TramChanh/Art/Models/DrinkStation/SM_IceBin_Well.obj")

def build_icebin_lids():
    obj = ObjWriter("SM_IceBin_Lids")
    # Dual sliding stainless steel lids with recessed rectangular handles
    # Envelopes: X: [-0.218, 0.218], Y: [0.006, 0.026], Z: [-0.192, 0.192]

    # Front sliding lid (Z: -0.192 to 0.005, Y: 0.006 to 0.015)
    # Main slab
    obj.add_box(-0.218, 0.218, 0.006, 0.015, -0.192, 0.005, 0.05, 0.65, 0.05, 0.65)
    # Recessed handle rim (raised surround around recessed pocket)
    obj.add_box(-0.065, 0.065, 0.015, 0.018, -0.120, -0.070, 0.05, 0.25, 0.05, 0.25)
    # Handle pocket depression
    obj.add_box(-0.055, 0.055, 0.008, 0.015, -0.110, -0.080, 0.02, 0.15, 0.02, 0.15)

    # Rear sliding lid (Z: -0.005 to 0.192, Y: 0.016 to 0.025, stepped slightly up to slide smoothly)
    obj.add_box(-0.218, 0.218, 0.016, 0.025, -0.005, 0.192, 0.05, 0.65, 0.05, 0.65)
    # Recessed handle rim
    obj.add_box(-0.065, 0.065, 0.025, 0.028, 0.070, 0.120, 0.05, 0.25, 0.05, 0.25)
    # Handle pocket depression
    obj.add_box(-0.055, 0.055, 0.018, 0.025, 0.080, 0.110, 0.02, 0.15, 0.02, 0.15)

    obj.write("Assets/TramChanh/Art/Models/DrinkStation/SM_IceBin_Lids.obj")

def build_ice_scoop():
    obj = ObjWriter("SM_IceScoop")
    # Stainless steel ice scoop
    # Envelope: X: [-0.0325, 0.0325], Y: [0.000, 0.0492], Z: [-0.1558, 0.0600]

    # 1. Ergonomic Handle: cylindrical bar along Z: [-0.145, -0.060], radius 0.007, centered at y = 0.032
    obj.add_cylinder(0, -0.100, 0.0075, 0.024, 0.040, segments=8, u_min=0.1, u_max=0.3, v_min=0.1, v_max=0.5)

    # 2. Hanging loop end cap at Z: [-0.155, -0.145]
    obj.add_cylinder(0, -0.150, 0.006, 0.026, 0.038, segments=8, u_min=0.1, u_max=0.2, v_min=0.1, v_max=0.3)

    # 3. Transition neck at Z: [-0.060, -0.045]
    obj.add_box(-0.012, 0.012, 0.022, 0.042, -0.060, -0.045, 0.2, 0.4, 0.2, 0.4)

    # 4. Scoop Bowl: rounded curved scoop
    # Bowl bottom and walls: Z: [-0.045, 0.058], X: [-0.032, 0.032], Y: [0.002, 0.045]
    # Bowl bottom
    obj.add_box(-0.030, 0.030, 0.002, 0.008, -0.045, 0.058, 0.05, 0.55, 0.05, 0.55)
    # Bowl left wall
    obj.add_box(-0.032, -0.027, 0.008, 0.045, -0.045, 0.055, 0.1, 0.3, 0.1, 0.5)
    # Bowl right wall
    obj.add_box(0.027, 0.032, 0.008, 0.045, -0.045, 0.055, 0.1, 0.3, 0.1, 0.5)
    # Bowl rear back wall
    obj.add_box(-0.027, 0.027, 0.008, 0.045, -0.045, -0.040, 0.1, 0.5, 0.1, 0.3)
    # Front tapered scraping lip
    obj.add_box(-0.028, 0.028, 0.002, 0.005, 0.050, 0.058, 0.2, 0.4, 0.2, 0.3)

    obj.write("Assets/TramChanh/Art/Models/DrinkStation/SM_IceScoop.obj")

def build_topping_station_frame():
    obj = ObjWriter("SM_ToppingStation_Frame")
    # Envelope: X: [-0.275, 0.275], Y: [0.000, 0.149], Z: [-0.195, 0.195]

    # Outer stainless steel housing
    # Bottom slab
    obj.add_box(-0.275, 0.275, 0.000, 0.035, -0.195, 0.195, 0.05, 0.65, 0.05, 0.65)
    # Left casing wall
    obj.add_box(-0.275, -0.260, 0.035, 0.145, -0.195, 0.195, 0.05, 0.25, 0.1, 0.6)
    # Back casing wall (+Z)
    obj.add_box(-0.260, 0.260, 0.035, 0.145, 0.180, 0.195, 0.05, 0.65, 0.1, 0.25)
    # Front casing wall (-Z)
    obj.add_box(-0.260, 0.260, 0.035, 0.145, -0.195, -0.180, 0.05, 0.65, 0.1, 0.25)
    # Center divider bar (X: around 0)
    obj.add_box(-0.015, 0.015, 0.035, 0.145, -0.180, 0.180, 0.1, 0.2, 0.1, 0.6)
    # Cross divider bar (Z: around 0)
    obj.add_box(-0.260, 0.185, 0.035, 0.145, -0.012, 0.012, 0.1, 0.6, 0.1, 0.2)
    # Right inner divider separating pans from bottles area
    obj.add_box(0.180, 0.195, 0.035, 0.145, -0.180, 0.180, 0.05, 0.2, 0.1, 0.6)
    # Right outer wall
    obj.add_box(0.260, 0.275, 0.035, 0.145, -0.195, 0.195, 0.05, 0.2, 0.1, 0.6)

    # 4 GN Pan Topping Contents:
    # 1. Front-Left Pan: Translucent white coconut jelly cubes (thạch dừa)
    # X: [-0.255, -0.018], Z: [-0.175, -0.015], Y: [0.080, 0.138]
    # UVs map to coconut jelly atlas region: U: [0.74, 0.86], V: [0.44, 0.68]
    obj.add_box(-0.255, -0.018, 0.080, 0.138, -0.175, -0.015, 0.74, 0.86, 0.44, 0.68)

    # 2. Front-Right Pan: Golden amber fruit jelly / aloe vera cubes
    # X: [0.018, 0.178], Z: [-0.175, -0.015], Y: [0.080, 0.138]
    # UVs map to golden amber jelly: U: [0.87, 0.99], V: [0.44, 0.68]
    obj.add_box(0.018, 0.178, 0.080, 0.138, -0.175, -0.015, 0.87, 0.99, 0.44, 0.68)

    # 3. Rear-Left Pan: Coconut jelly
    # X: [-0.255, -0.018], Z: [0.015, 0.175], Y: [0.080, 0.138]
    obj.add_box(-0.255, -0.018, 0.080, 0.138, 0.015, 0.175, 0.74, 0.86, 0.44, 0.68)

    # 4. Rear-Right Pan: Amber fruit topping
    # X: [0.018, 0.178], Z: [0.015, 0.175], Y: [0.080, 0.138]
    obj.add_box(0.018, 0.178, 0.080, 0.138, 0.015, 0.175, 0.87, 0.99, 0.44, 0.68)

    # Ledge beside pans: X in [0.198, 0.268]
    # Bottle 1: Yellow passionfruit syrup pump bottle
    # Body cylinder
    obj.add_cylinder(0.230, -0.100, 0.022, 0.035, 0.125, segments=8, u_min=0.74, u_max=0.82, v_min=0.74, v_max=0.98)
    # Pump neck & head
    obj.add_cylinder(0.230, -0.100, 0.007, 0.125, 0.142, segments=6, u_min=0.1, u_max=0.3, v_min=0.1, v_max=0.3)
    obj.add_box(0.220, 0.240, 0.140, 0.148, -0.125, -0.090, 0.1, 0.3, 0.1, 0.3)

    # Bottle 2: Clear sugar syrup pump bottle
    obj.add_cylinder(0.230, -0.030, 0.022, 0.035, 0.125, segments=8, u_min=0.83, u_max=0.91, v_min=0.74, v_max=0.98)
    # Pump neck & head
    obj.add_cylinder(0.230, -0.030, 0.007, 0.125, 0.142, segments=6, u_min=0.1, u_max=0.3, v_min=0.1, v_max=0.3)
    obj.add_box(0.220, 0.240, 0.140, 0.148, -0.055, -0.020, 0.1, 0.3, 0.1, 0.3)

    # Straw Basket on rear-right: Yellow perforated basket with colorful drinking straws
    obj.add_box(0.202, 0.262, 0.035, 0.148, 0.040, 0.160, 0.92, 0.99, 0.73, 0.98)

    obj.write("Assets/TramChanh/Art/Models/DrinkStation/SM_ToppingStation_Frame.obj")

def build_topping_station_cover():
    obj = ObjWriter("SM_ToppingStation_Cover")
    # Envelope: X: [-0.270, 0.270], Y: [0.145, 0.3473], Z: [-0.0393, 0.160]
    # Curved transparent sneeze guard roll-top hood
    x0, x1 = -0.270, 0.270
    z_center, y_center = 0.060, 0.145
    radius_z = 0.100
    radius_y = 0.200

    segments = 16
    arc_points = []
    # Arc from angle 0 (z = 0.160, y = 0.145) to angle ~135 degrees (z = -0.039, y = 0.285)
    for i in range(segments + 1):
        t = i / segments
        angle = t * 2.356 # ~135 degrees in radians
        az = z_center + radius_z * math.cos(angle)
        ay = y_center + radius_y * math.sin(angle)
        arc_points.append((ay, az))

    # Extrude along X from x0 to x1
    for i in range(segments):
        y_a, z_a = arc_points[i]
        y_b, z_b = arc_points[i+1]

        v1 = obj.add_vert(x0, y_a, z_a)
        v2 = obj.add_vert(x1, y_a, z_a)
        v3 = obj.add_vert(x1, y_b, z_b)
        v4 = obj.add_vert(x0, y_b, z_b)

        uv1 = obj.add_uv(0.0, i / segments)
        uv2 = obj.add_uv(1.0, i / segments)
        uv3 = obj.add_uv(1.0, (i + 1) / segments)
        uv4 = obj.add_uv(0.0, (i + 1) / segments)

        # Normal
        ny = y_b - y_a
        nz = z_b - z_a
        n = obj.add_normal(0, -nz, ny)

        # Add double-sided quad for acrylic transparency
        obj.add_quad(v1, v2, v3, v4, uv1, uv2, uv3, uv4, n)
        n_back = obj.add_normal(0, nz, -ny)
        obj.add_quad(v4, v3, v2, v1, uv4, uv3, uv2, uv1, n_back)

    # Left and Right clear end caps
    for side_x, normal_x in [(x0, -1), (x1, 1)]:
        c_vert = obj.add_vert(side_x, y_center, z_center)
        uv_c = obj.add_uv(0.5, 0.5)
        n_side = obj.add_normal(normal_x, 0, 0)
        for i in range(segments):
            y_a, z_a = arc_points[i]
            y_b, z_b = arc_points[i+1]
            va = obj.add_vert(side_x, y_a, z_a)
            vb = obj.add_vert(side_x, y_b, z_b)
            uva = obj.add_uv(0.5 + 0.5 * math.cos(i/segments * 2.356), 0.5 + 0.5 * math.sin(i/segments * 2.356))
            uvb = obj.add_uv(0.5 + 0.5 * math.cos((i+1)/segments * 2.356), 0.5 + 0.5 * math.sin((i+1)/segments * 2.356))
            if normal_x > 0:
                obj.add_tri(c_vert, va, vb, uv_c, uva, uvb, n_side)
            else:
                obj.add_tri(c_vert, vb, va, uv_c, uvb, uva, n_side)

    # Front molded acrylic lift handle
    y_front, z_front = arc_points[-1]
    obj.add_box(-0.080, 0.080, y_front - 0.005, y_front + 0.015, z_front - 0.015, z_front, 0.1, 0.4, 0.1, 0.4)

    obj.write("Assets/TramChanh/Art/Models/DrinkStation/SM_ToppingStation_Cover.obj")

def build_grill_elmich():
    # SM_Grill_Elmich_Base.obj
    # In PF_Grill_Elmich, Base has scale: (0.32, 0.1, 0.28) at (0, 0.05, 0)
    # Local unit space in Base: vx in [-0.5, 0.5], vy in [-0.5, 0.6], vz in [-0.54, 0.54]
    base = ObjWriter("SM_Grill_Elmich_Base")

    # Main black satin housing
    # Lower chassis
    base.add_box(-0.50, 0.50, -0.45, 0.40, -0.52, 0.52, 0.1, 0.8, 0.1, 0.8)
    # Sloped front control bezel (VZ: -0.54 to -0.40, VY: -0.10 to 0.40)
    base.add_box(-0.46, 0.46, -0.10, 0.35, -0.54, -0.42, 0.1, 0.7, 0.1, 0.7)
    # Grease drip tray lip at front bottom
    base.add_box(-0.35, 0.35, -0.45, -0.35, -0.54, -0.48, 0.1, 0.5, 0.1, 0.3)

    # 4 Non-slip rubber feet
    for fx, fz in [(-0.42, -0.42), (0.42, -0.42), (-0.42, 0.42), (0.42, 0.42)]:
        base.add_cylinder(fx, fz, 0.05, -0.50, -0.45, segments=8, u_min=0.75, u_max=0.95, v_min=0.05, v_max=0.25)

    # Rear hinge bracket uprights (VZ: 0.42 to 0.52, VY: 0.30 to 0.70)
    base.add_box(-0.48, -0.40, 0.30, 0.70, 0.40, 0.52, 0.1, 0.3, 0.1, 0.5)
    base.add_box(0.40, 0.48, 0.30, 0.70, 0.40, 0.52, 0.1, 0.3, 0.1, 0.5)

    base.write("Assets/TramChanh/Art/Models/CakeStation/SM_Grill_Elmich_Base.obj")

    # SM_Grill_Elmich_Lid.obj
    # In PF_Grill_Elmich, Lid has scale: (0.32, 0.06, 0.28) at (0, 0.04, -0.14) under LidPivot
    lid = ObjWriter("SM_Grill_Elmich_Lid")

    # Curved top stainless cover
    lid.add_box(-0.50, 0.50, -0.40, 0.40, -0.50, 0.50, 0.1, 0.9, 0.1, 0.9)
    # Side black heat-resistant flank grips
    lid.add_box(-0.53, -0.48, -0.45, 0.35, -0.45, 0.45, 0.1, 0.3, 0.1, 0.8)
    lid.add_box(0.48, 0.53, -0.45, 0.35, -0.45, 0.45, 0.1, 0.3, 0.1, 0.8)

    # Rear hinge knuckles at VZ = 0.50 connecting to LidPivot
    lid.add_cylinder(-0.44, 0.48, 0.08, -0.20, 0.20, segments=8, u_min=0.1, u_max=0.4, v_min=0.1, v_max=0.4)
    lid.add_cylinder(0.44, 0.48, 0.08, -0.20, 0.20, segments=8, u_min=0.1, u_max=0.4, v_min=0.1, v_max=0.4)

    # Front dual bracket arms holding chrome bar handle
    lid.add_box(-0.35, -0.28, -0.40, 0.10, -0.58, -0.48, 0.1, 0.3, 0.1, 0.4)
    lid.add_box(0.28, 0.35, -0.40, 0.10, -0.58, -0.48, 0.1, 0.3, 0.1, 0.4)

    lid.write("Assets/TramChanh/Art/Models/CakeStation/SM_Grill_Elmich_Lid.obj")

if __name__ == "__main__":
    build_icebin_well()
    build_icebin_lids()
    build_ice_scoop()
    build_topping_station_frame()
    build_topping_station_cover()
    build_grill_elmich()
    print("ALL equipment meshes generated successfully!")
