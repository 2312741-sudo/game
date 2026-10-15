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

    def add_frustum(self, cx, cz, r_bot, r_top, y0, y1, segments=12, u_min=0.1, u_max=0.6, v_min=0.1, v_max=0.6):
        top_verts = []
        bot_verts = []
        normals = []
        dy = y1 - y0
        dr = r_bot - r_top
        slant = math.sqrt(dr*dr + dy*dy) or 1.0
        ny_side = dr / slant
        nr_side = dy / slant
        for i in range(segments):
            angle = 2 * math.pi * i / segments
            dx_b = math.cos(angle) * r_bot
            dz_b = math.sin(angle) * r_bot
            dx_t = math.cos(angle) * r_top
            dz_t = math.sin(angle) * r_top
            bot_verts.append(self.add_vert(cx + dx_b, y0, cz + dz_b))
            top_verts.append(self.add_vert(cx + dx_t, y1, cz + dz_t))
            normals.append(self.add_normal(math.cos(angle)*nr_side, ny_side, math.sin(angle)*nr_side))

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

            self.add_quad(bot_verts[i], bot_verts[nxt], top_verts[nxt], top_verts[i],
                          uv_b0, uv_b1, uv_t1, uv_t0, normals[i])
            if r_top > 0.0001:
                self.add_tri(c_top, top_verts[nxt], top_verts[i], uv_c, uv_t1, uv_t0, n_top)
            if r_bot > 0.0001:
                self.add_tri(c_bot, bot_verts[i], bot_verts[nxt], uv_c, uv_b0, uv_b1, n_bot)

    def get_bounds(self):
        xs = [v[0] for v in self.verts]
        ys = [v[1] for v in self.verts]
        zs = [v[2] for v in self.verts]
        return (min(xs), max(xs)), (min(ys), max(ys)), (min(zs), max(zs))

    def normalize_to_unit_cube(self):
        (xmin, xmax), (ymin, ymax), (zmin, zmax) = self.get_bounds()
        dx = xmax - xmin or 1.0
        dy = ymax - ymin or 1.0
        dz = zmax - zmin or 1.0
        new_verts = []
        for x, y, z in self.verts:
            nx = -0.5 + (x - xmin) / dx
            ny = -0.5 + (y - ymin) / dy
            nz = -0.5 + (z - zmin) / dz
            new_verts.append((nx, ny, nz))
        self.verts = new_verts

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
    # Base mat slab: y in [0.006, 0.0075]
    obj.add_box(-0.240, 0.240, 0.006, 0.0075, -0.225, -0.160, 0.73, 0.99, 0.02, 0.38)
    # Raised perimeter rims: y in [0.0075, 0.009]
    obj.add_box(-0.240, 0.240, 0.0075, 0.009, -0.225, -0.221, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(-0.240, 0.240, 0.0075, 0.009, -0.164, -0.160, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(-0.240, -0.236, 0.0075, 0.009, -0.221, -0.164, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(0.236, 0.240, 0.0075, 0.009, -0.221, -0.164, 0.73, 0.99, 0.02, 0.38)
    # Studded rubber pins grid: 15 along X, 3 along Z
    for ix in range(15):
        cx = -0.220 + ix * (0.440 / 14.0)
        for iz in range(3):
            cz = -0.210 + iz * (0.040 / 2.0)
            obj.add_box(cx - 0.004, cx + 0.004, 0.0075, 0.009, cz - 0.004, cz + 0.004, 0.75, 0.85, 0.05, 0.25)

    # Narrow rubber runner strip on right side for ice scoop
    obj.add_box(0.180, 0.245, 0.006, 0.0075, -0.160, 0.200, 0.73, 0.99, 0.02, 0.38)
    # Runner perimeter rims
    obj.add_box(0.180, 0.184, 0.0075, 0.009, -0.160, 0.200, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(0.241, 0.245, 0.0075, 0.009, -0.160, 0.200, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(0.184, 0.241, 0.0075, 0.009, -0.160, -0.156, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(0.184, 0.241, 0.0075, 0.009, 0.196, 0.200, 0.73, 0.99, 0.02, 0.38)
    # Transverse drainage ribs
    for ir in range(8):
        cz = -0.140 + ir * (0.320 / 7.0)
        obj.add_box(0.184, 0.241, 0.0075, 0.009, cz - 0.003, cz + 0.003, 0.75, 0.85, 0.05, 0.25)

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
    # Helper for detailed syrup pump dispenser
    def add_syrup_bottle(cx, cz, u_min, u_max, v_min, v_max, spout_dz):
        # Bottle cylinder body
        obj.add_cylinder(cx, cz, 0.021, 0.035, 0.115, segments=12, u_min=u_min, u_max=u_max, v_min=v_min, v_max=v_max)
        # Tapered bottle shoulder
        obj.add_frustum(cx, cz, 0.021, 0.012, 0.115, 0.125, segments=12, u_min=u_min, u_max=u_max, v_min=v_min+0.1, v_max=v_max)
        # Ribbed screw cap collar
        obj.add_cylinder(cx, cz, 0.0135, 0.125, 0.130, segments=10, u_min=0.1, u_max=0.3, v_min=0.1, v_max=0.3)
        # Plunger stem rod
        obj.add_cylinder(cx, cz, 0.005, 0.130, 0.142, segments=8, u_min=0.6, u_max=0.8, v_min=0.1, v_max=0.3)
        # Ergonomic pump head press cap
        obj.add_box(cx - 0.010, cx + 0.010, 0.142, 0.148, cz - 0.010, cz + 0.010, 0.1, 0.3, 0.1, 0.3)
        # Dispenser spout nozzle pointing towards seller
        sz0 = cz - 0.010
        sz1 = cz - 0.024 if spout_dz < 0 else cz + 0.024
        sz_min, sz_max = min(sz0, sz1), max(sz0, sz1)
        obj.add_box(cx - 0.004, cx + 0.004, 0.140, 0.146, sz_min, sz_max, 0.1, 0.3, 0.1, 0.3)

    # Bottle 1: Yellow passionfruit syrup pump bottle (cz = -0.100)
    add_syrup_bottle(0.230, -0.100, 0.74, 0.82, 0.74, 0.98, spout_dz=-1)

    # Bottle 2: Clear sugar syrup pump bottle (cz = -0.030)
    add_syrup_bottle(0.230, -0.030, 0.83, 0.91, 0.74, 0.98, spout_dz=-1)

    # Straw Basket on rear-right: Yellow perforated basket with colorful drinking straws
    obj.add_box(0.202, 0.262, 0.035, 0.120, 0.040, 0.160, 0.92, 0.99, 0.73, 0.98)
    straw_colors = [(0.75, 0.85, 0.05, 0.25), (0.1, 0.3, 0.6, 0.9), (0.7, 0.9, 0.7, 0.9)]
    for s_idx, (sx, sz) in enumerate([(0.218, 0.075), (0.242, 0.075), (0.220, 0.125), (0.244, 0.125), (0.230, 0.100)]):
        uv_c = straw_colors[s_idx % len(straw_colors)]
        obj.add_cylinder(sx, sz, 0.004, 0.120, 0.148, segments=6, u_min=uv_c[0], u_max=uv_c[1], v_min=uv_c[2], v_max=uv_c[3])

    obj.write("Assets/TramChanh/Art/Models/DrinkStation/SM_ToppingStation_Frame.obj")

def build_topping_station_cover():
    # In PF_DrinkStation, PF_ToppingStation is at (0.6, 0.85, -0.05) rotated 90 deg around Y.
    # To satisfy X <= 0.90 and counter flush Y >= 1.00:
    # Mesh bounds in OBJ space: X in [-0.25, 0.25], Y in [0.00, 0.18], Z in [-0.15, 0.07]
    obj = ObjWriter("SM_ToppingStation_Cover")
    num_segs = 20
    profile = []
    for i in range(num_segs + 1):
        t = i / float(num_segs)
        theta = math.pi * t
        z = -0.04 - 0.11 * math.cos(theta)
        y = 0.18 * math.sin(theta)
        profile.append((z, y))

    x_steps = [-0.25, -0.15, -0.05, 0.05, 0.15, 0.25]
    grid = []
    for r_idx, (pz, py) in enumerate(profile):
        row = []
        v_coord = r_idx / float(len(profile) - 1)
        for c_idx, px in enumerate(x_steps):
            u_coord = c_idx / float(len(x_steps) - 1)
            v = obj.add_vert(px, py, pz)
            uv = obj.add_uv(u_coord, v_coord)
            row.append((v, uv))
        grid.append(row)

    n_up = obj.add_normal(0, 1, 0)
    n_dn = obj.add_normal(0, -1, 0)
    for r in range(len(grid) - 1):
        for c in range(len(x_steps) - 1):
            (v1, uv1), (v2, uv2) = grid[r][c], grid[r][c+1]
            (v3, uv3), (v4, uv4) = grid[r+1][c+1], grid[r+1][c]
            obj.add_quad(v1, v2, v3, v4, uv1, uv2, uv3, uv4, n_up)
            obj.add_quad(v4, v3, v2, v1, uv4, uv3, uv2, uv1, n_dn)

    for px, is_right in [(-0.25, False), (0.25, True)]:
        c_v = obj.add_vert(px, 0.00, -0.04)
        uv_c = obj.add_uv(0.5, 0.0)
        n_side = obj.add_normal(1 if is_right else -1, 0, 0)
        for i in range(len(profile) - 1):
            z1, y1 = profile[i]
            z2, y2 = profile[i+1]
            va = obj.add_vert(px, y1, z1)
            vb = obj.add_vert(px, y2, z2)
            uva = obj.add_uv(i / float(len(profile)), 1.0)
            uvb = obj.add_uv((i + 1) / float(len(profile)), 1.0)
            if is_right:
                obj.add_tri(c_v, va, vb, uv_c, uva, uvb, n_side)
                obj.add_tri(vb, va, c_v, uvb, uva, uv_c, n_side)
            else:
                obj.add_tri(c_v, vb, va, uv_c, uvb, uva, n_side)
                obj.add_tri(va, vb, c_v, uva, uvb, uv_c, n_side)

    obj.write("Assets/TramChanh/Art/Models/DrinkStation/SM_ToppingStation_Cover.obj")

def build_grill_elmich():
    # Exact required bounds: Base X in [-0.170, 0.170], Y in [0.000, 0.125], Z in [-0.150, 0.152]
    # Lid X in [-0.170, 0.170], Y in [-0.015, 0.058], Z in [-0.020, 0.372]

    # --- SM_Grill_Elmich_Base.obj ---
    verts_b = []
    uvs_b = []
    faces_b = []

    def add_v_b(x, y, z):
        verts_b.append((x, y, z))
        return len(verts_b)

    def add_uv_b(u, v):
        uvs_b.append((u, v))
        return len(uvs_b)

    def add_f_b(v_list, vt_list):
        if len(v_list) == 3:
            faces_b.append((v_list[0], vt_list[0], v_list[1], vt_list[1], v_list[2], vt_list[2]))
        elif len(v_list) == 4:
            faces_b.append((v_list[0], vt_list[0], v_list[1], vt_list[1], v_list[2], vt_list[2]))
            faces_b.append((v_list[0], vt_list[0], v_list[2], vt_list[2], v_list[3], vt_list[3]))

    uv0_b = add_uv_b(0.0, 0.0)

    # 1. Four Cylindrical Rubber Feet (Y: 0.000 .. 0.012)
    foot_r = 0.014
    for fx, fz in [(-0.140, -0.120), (0.140, -0.120), (-0.140, 0.120), (0.140, 0.120)]:
        f_bot = []
        f_top = []
        for i in range(8):
            ang = 2.0 * math.pi * i / 8.0
            vx = fx + foot_r * math.cos(ang)
            vz = fz + foot_r * math.sin(ang)
            f_bot.append(add_v_b(vx, 0.000, vz))
            f_top.append(add_v_b(vx, 0.012, vz))
        bot_center = add_v_b(fx, 0.000, fz)
        for i in range(8):
            ni = (i + 1) % 8
            add_f_b([bot_center, f_bot[ni], f_bot[i]], [uv0_b]*3)
        for i in range(8):
            ni = (i + 1) % 8
            add_f_b([f_bot[i], f_bot[ni], f_top[ni], f_top[i]], [uv0_b]*4)

    # 2. Lower Chassis Box (Y: 0.012 .. 0.080)
    b_l_b = add_v_b(-0.165, 0.012, -0.145)
    b_r_b = add_v_b(0.165, 0.012, -0.145)
    b_l_f = add_v_b(-0.165, 0.012, 0.145)
    b_r_f = add_v_b(0.165, 0.012, 0.145)
    add_f_b([b_l_b, b_r_b, b_r_f, b_l_f], [uv0_b]*4)

    m_l_b = add_v_b(-0.170, 0.080, -0.150)
    m_r_b = add_v_b(0.170, 0.080, -0.150)
    m_l_f = add_v_b(-0.170, 0.080, 0.135)
    m_r_f = add_v_b(0.170, 0.080, 0.135)

    add_f_b([b_l_b, m_l_b, m_l_f, b_l_f], [uv0_b]*4)
    add_f_b([b_r_b, b_r_f, m_r_f, m_r_b], [uv0_b]*4)
    add_f_b([b_l_b, b_r_b, m_r_b, m_l_b], [uv0_b]*4)

    # 3. Sloped Front Control Bezel & Digital Display Panel (Z up to 0.152)
    disp_v_bl = add_v_b(-0.080, 0.025, 0.152)
    disp_v_br = add_v_b(0.080, 0.025, 0.152)
    disp_v_tr = add_v_b(0.080, 0.065, 0.145)
    disp_v_tl = add_v_b(-0.080, 0.065, 0.145)

    uv_disp_bl = add_uv_b(0.0, 0.0)
    uv_disp_br = add_uv_b(1.0, 0.0)
    uv_disp_tr = add_uv_b(1.0, 1.0)
    uv_disp_tl = add_uv_b(0.0, 1.0)

    add_f_b([disp_v_bl, disp_v_br, disp_v_tr, disp_v_tl],
            [uv_disp_bl, uv_disp_br, uv_disp_tr, uv_disp_tl])

    add_f_b([b_l_f, b_r_f, disp_v_br, disp_v_bl], [uv0_b]*4)
    add_f_b([disp_v_tl, disp_v_tr, m_r_f, m_l_f], [uv0_b]*4)
    add_f_b([b_l_f, disp_v_bl, disp_v_tl, m_l_f], [uv0_b]*4)
    add_f_b([disp_v_br, b_r_f, m_r_f, disp_v_tr], [uv0_b]*4)

    # 4. Upper Cooking Well & Rim (Y = 0.080 .. 0.092)
    w_l_b = add_v_b(-0.145, 0.080, -0.120)
    w_r_b = add_v_b(0.145, 0.080, -0.120)
    w_l_f = add_v_b(-0.145, 0.080, 0.120)
    w_r_f = add_v_b(0.145, 0.080, 0.120)

    add_f_b([m_l_b, m_r_b, w_r_b, w_l_b], [uv0_b]*4)
    add_f_b([m_l_f, w_l_f, w_r_f, m_r_f], [uv0_b]*4)
    add_f_b([m_l_b, w_l_b, w_l_f, m_l_f], [uv0_b]*4)
    add_f_b([m_r_b, m_r_f, w_r_f, w_r_b], [uv0_b]*4)

    # 5. Lower Ribbed Cast Aluminum Cooking Plate
    num_ribs = 10
    rib_w = 0.007
    plate_x_span = 0.280
    step = plate_x_span / num_ribs

    p_floor_lb = add_v_b(-0.140, 0.092, -0.090)
    p_floor_rb = add_v_b(0.140, 0.092, -0.090)
    p_floor_rf = add_v_b(0.140, 0.092, 0.125)
    p_floor_lf = add_v_b(-0.140, 0.092, 0.125)
    add_f_b([p_floor_lb, p_floor_rb, p_floor_rf, p_floor_lf], [uv0_b]*4)

    for i in range(num_ribs):
        rx_c = -0.140 + (i + 0.5) * step
        rx1 = rx_c - rib_w * 0.5
        rx2 = rx_c + rib_w * 0.5
        r_b1 = add_v_b(rx1, 0.096, -0.085)
        r_b2 = add_v_b(rx2, 0.096, -0.085)
        r_f2 = add_v_b(rx2, 0.096, 0.125)
        r_f1 = add_v_b(rx1, 0.096, 0.125)
        add_f_b([r_b1, r_b2, r_f2, r_f1], [uv0_b]*4)

    # 6. Rear Hinge Towers (Rising up to Y = 0.125, Z down to -0.150)
    hl_b1 = add_v_b(-0.140, 0.125, -0.150)
    hl_b2 = add_v_b(-0.115, 0.125, -0.150)
    hl_f2 = add_v_b(-0.115, 0.125, -0.115)
    hl_f1 = add_v_b(-0.140, 0.125, -0.115)
    add_f_b([hl_b1, hl_b2, hl_f2, hl_f1], [uv0_b]*4)
    add_f_b([m_l_b, hl_b1, hl_f1, w_l_b], [uv0_b]*4)

    hr_b1 = add_v_b(0.115, 0.125, -0.150)
    hr_b2 = add_v_b(0.140, 0.125, -0.150)
    hr_f2 = add_v_b(0.140, 0.125, -0.115)
    hr_f1 = add_v_b(0.115, 0.125, -0.115)
    add_f_b([hr_b1, hr_b2, hr_f2, hr_f1], [uv0_b]*4)
    add_f_b([m_r_b, w_r_b, hr_f2, hr_b2], [uv0_b]*4)

    out_base = "Assets/TramChanh/Art/Models/CakeStation/SM_Grill_Elmich_Base.obj"
    with open(out_base, "w") as f:
        f.write("# Wavefront OBJ: SM_Grill_Elmich_Base\n")
        f.write("o SM_Grill_Elmich_Base\n")
        for vx, vy, vz in verts_b:
            f.write(f"v {vx:.6f} {vy:.6f} {vz:.6f}\n")
        for u, v in uvs_b:
            f.write(f"vt {u:.6f} {v:.6f}\n")
        for f_tuple in faces_b:
            f.write(f"f {f_tuple[0]}/{f_tuple[1]} {f_tuple[2]}/{f_tuple[3]} {f_tuple[4]}/{f_tuple[5]}\n")
    print(f"Generated {out_base}: {len(verts_b)} verts, {len(faces_b)} faces")

    # --- SM_Grill_Elmich_Lid.obj ---
    verts_l = []
    uvs_l = []
    faces_l = []

    def add_v_l(x, y, z):
        verts_l.append((x, y, z))
        return len(verts_l)

    def add_uv_l(u, v):
        uvs_l.append((u, v))
        return len(uvs_l)

    def add_f_l(v_list, vt_list):
        if len(v_list) == 3:
            faces_l.append((v_list[0], vt_list[0], v_list[1], vt_list[1], v_list[2], vt_list[2]))
        elif len(v_list) == 4:
            faces_l.append((v_list[0], vt_list[0], v_list[1], vt_list[1], v_list[2], vt_list[2]))
            faces_l.append((v_list[0], vt_list[0], v_list[2], vt_list[2], v_list[3], vt_list[3]))

    uv0_l = add_uv_l(0.0, 0.0)

    # 1. Rear Hinge Knuckles (Z down to -0.020, Y down to -0.015)
    kl_b1 = add_v_l(-0.140, -0.015, -0.020)
    kl_b2 = add_v_l(-0.115, -0.015, -0.020)
    kl_f2 = add_v_l(-0.115, 0.025, 0.035)
    kl_f1 = add_v_l(-0.140, 0.025, 0.035)
    add_f_l([kl_b1, kl_b2, kl_f2, kl_f1], [uv0_l]*4)

    kr_b1 = add_v_l(0.115, -0.015, -0.020)
    kr_b2 = add_v_l(0.140, -0.015, -0.020)
    kr_f2 = add_v_l(0.140, 0.025, 0.035)
    kr_f1 = add_v_l(0.115, 0.025, 0.035)
    add_f_l([kr_b1, kr_b2, kr_f2, kr_f1], [uv0_l]*4)

    # 2. Underside Ribbed Upper Grill Plate
    pl_b = add_v_l(-0.140, 0.000, 0.040)
    pr_b = add_v_l(0.140, 0.000, 0.040)
    pr_f = add_v_l(0.140, 0.000, 0.280)
    pl_f = add_v_l(-0.140, 0.000, 0.280)
    add_f_l([pl_b, pl_f, pr_f, pr_b], [uv0_l]*4)

    for i in range(num_ribs):
        rx_c = -0.140 + (i + 0.5) * step
        rx1 = rx_c - rib_w * 0.5
        rx2 = rx_c + rib_w * 0.5
        r_b1 = add_v_l(rx1, -0.015, 0.045)
        r_b2 = add_v_l(rx2, -0.015, 0.045)
        r_f2 = add_v_l(rx2, -0.015, 0.275)
        r_f1 = add_v_l(rx1, -0.015, 0.275)
        add_f_l([r_b1, r_f1, r_f2, r_b2], [uv0_l]*4)

    # 3. Main Domed Top Casing (Y up to 0.058, X up to +-0.155, Z in [0.030, 0.300])
    t_l_b = add_v_l(-0.155, 0.040, 0.030)
    t_r_b = add_v_l(0.155, 0.040, 0.030)
    t_l_f = add_v_l(-0.155, 0.040, 0.300)
    t_r_f = add_v_l(0.155, 0.040, 0.300)

    apex_lb = add_v_l(-0.130, 0.058, 0.050)
    apex_rb = add_v_l(0.130, 0.058, 0.050)
    apex_rf = add_v_l(0.130, 0.058, 0.280)
    apex_lf = add_v_l(-0.130, 0.058, 0.280)

    add_f_l([apex_lb, apex_rb, apex_rf, apex_lf], [uv0_l]*4)
    add_f_l([t_l_b, apex_lb, apex_lf, t_l_f], [uv0_l]*4)
    add_f_l([apex_rb, t_r_b, t_r_f, apex_rf], [uv0_l]*4)
    add_f_l([t_l_b, t_r_b, apex_rb, apex_lb], [uv0_l]*4)
    add_f_l([apex_lf, apex_rf, t_r_f, t_l_f], [uv0_l]*4)

    # 4. Side Composite Grip Brackets (Reaching X = +-0.170, Y in [-0.005, 0.045])
    g_l_b = add_v_l(-0.170, 0.020, 0.120)
    g_l_f = add_v_l(-0.170, 0.020, 0.220)
    g_l_t = add_v_l(-0.170, 0.045, 0.170)
    g_l_d = add_v_l(-0.170, -0.005, 0.170)
    add_f_l([g_l_b, g_l_d, g_l_f, g_l_t], [uv0_l]*4)
    add_f_l([t_l_b, g_l_b, g_l_t, t_l_f], [uv0_l]*4)

    g_r_b = add_v_l(0.170, 0.020, 0.120)
    g_r_f = add_v_l(0.170, 0.020, 0.220)
    g_r_t = add_v_l(0.170, 0.045, 0.170)
    g_r_d = add_v_l(0.170, -0.005, 0.170)
    add_f_l([g_r_b, g_r_t, g_r_f, g_r_d], [uv0_l]*4)
    add_f_l([t_r_b, t_r_f, g_r_t, g_r_b], [uv0_l]*4)

    # 5. Cantilever Handle Arms
    al_b = add_v_l(-0.119, 0.025, 0.290)
    al_f = add_v_l(-0.119, 0.025, 0.355)
    ar_b = add_v_l(0.119, 0.025, 0.290)
    ar_f = add_v_l(0.119, 0.025, 0.355)

    # 6. Tubular Front Crossbar Handle (Z up to 0.372)
    h_r = 0.012
    h_z = 0.360
    h_y = 0.025
    h_segs = 8
    h_left = []
    h_right = []
    for i in range(h_segs):
        ang = 2.0 * math.pi * i / float(h_segs)
        vy = h_y + h_r * math.cos(ang)
        vz = h_z + h_r * math.sin(ang)
        h_left.append(add_v_l(-0.120, vy, vz))
        h_right.append(add_v_l(0.120, vy, vz))

    for i in range(h_segs):
        ni = (i + 1) % h_segs
        add_f_l([h_left[i], h_right[i], h_right[ni], h_left[ni]], [uv0_l]*4)

    c_l = add_v_l(-0.120, h_y, h_z)
    c_r = add_v_l(0.120, h_y, h_z)
    for i in range(h_segs):
        ni = (i + 1) % h_segs
        add_f_l([c_l, h_left[ni], h_left[i]], [uv0_l]*3)
        add_f_l([c_r, h_right[i], h_right[ni]], [uv0_l]*3)

    out_lid = "Assets/TramChanh/Art/Models/CakeStation/SM_Grill_Elmich_Lid.obj"
    with open(out_lid, "w") as f:
        f.write("# Wavefront OBJ: SM_Grill_Elmich_Lid\n")
        f.write("o SM_Grill_Elmich_Lid\n")
        for vx, vy, vz in verts_l:
            f.write(f"v {vx:.6f} {vy:.6f} {vz:.6f}\n")
        for u, v in uvs_l:
            f.write(f"vt {u:.6f} {v:.6f}\n")
        for f_tuple in faces_l:
            f.write(f"f {f_tuple[0]}/{f_tuple[1]} {f_tuple[2]}/{f_tuple[3]} {f_tuple[4]}/{f_tuple[5]}\n")
    print(f"Generated {out_lid}: {len(verts_l)} verts, {len(faces_l)} faces")

def build_cake_tool_meshes():
    # 1. SM_Spatula_WoodHandle (normalized to [-0.5, 0.5])
    spat = ObjWriter("SM_Spatula_WoodHandle")
    # Wood handle along Z: z in [-0.5, -0.1]
    spat.add_cylinder(0, -0.3, 0.25, -0.5, -0.1, segments=12, u_min=0.1, u_max=0.5, v_min=0.1, v_max=0.5)
    # Ferrule: z in [-0.1, -0.05]
    spat.add_cylinder(0, -0.3, 0.28, -0.1, -0.05, segments=12, u_min=0.6, u_max=0.8, v_min=0.1, v_max=0.3)
    # Crank neck: stepped from y=-0.3 to y=0.0
    spat.add_box(-0.15, 0.15, -0.35, 0.05, -0.05, 0.08, 0.6, 0.8, 0.1, 0.3)
    # Wide blade: z in [0.08, 0.5], x in [-0.5, 0.5], y in [0.0, 0.05]
    spat.add_box(-0.5, 0.5, 0.0, 0.05, 0.08, 0.5, 0.6, 0.9, 0.3, 0.6)
    spat.normalize_to_unit_cube()
    spat.write("Assets/TramChanh/Art/Models/CakeStation/SM_Spatula_WoodHandle.obj")

    # 2. SM_Scissors_RedGray (normalized to [-0.5, 0.5])
    scis = ObjWriter("SM_Scissors_RedGray")
    # Pivot screw
    scis.add_cylinder(0, 0, 0.15, -0.3, 0.3, segments=10, u_min=0.6, u_max=0.8, v_min=0.1, v_max=0.3)
    # Twin stainless blades extending +Z: 0.0 to 0.5
    scis.add_box(-0.15, 0.0, -0.05, 0.05, 0.0, 0.5, 0.6, 0.9, 0.3, 0.6)
    scis.add_box(0.0, 0.15, -0.05, 0.05, 0.0, 0.5, 0.6, 0.9, 0.3, 0.6)
    # Ergonomic handle loops extending -Z: -0.5 to 0.0
    # Thumb loop (-X side)
    scis.add_cylinder(-0.35, -0.3, 0.22, -0.2, 0.2, segments=12, u_min=0.1, u_max=0.4, v_min=0.6, v_max=0.9)
    # Multi-finger loop (+X side)
    scis.add_cylinder(0.35, -0.25, 0.28, -0.2, 0.2, segments=12, u_min=0.1, u_max=0.4, v_min=0.6, v_max=0.9)
    # Shanks connecting pivot to loops
    scis.add_box(-0.3, -0.05, -0.15, 0.15, -0.2, 0.0, 0.1, 0.4, 0.6, 0.9)
    scis.add_box(0.05, 0.3, -0.15, 0.15, -0.2, 0.0, 0.1, 0.4, 0.6, 0.9)
    scis.normalize_to_unit_cube()
    scis.write("Assets/TramChanh/Art/Models/CakeStation/SM_Scissors_RedGray.obj")

    # 3. SM_SauceBag (normalized to [-0.5, 0.5])
    sauce = ObjWriter("SM_SauceBag")
    # Conical squeeze piping pouch: apex dispensing tip at y = -0.5, wide body at y = 0.35
    sauce.add_frustum(0, 0, 0.08, 0.45, -0.5, 0.35, segments=16, u_min=0.1, u_max=0.5, v_min=0.1, v_max=0.8)
    # Crimped sealed top closure
    sauce.add_box(-0.35, 0.35, 0.35, 0.5, -0.08, 0.08, 0.1, 0.5, 0.8, 0.95)
    sauce.normalize_to_unit_cube()
    sauce.write("Assets/TramChanh/Art/Models/CakeStation/SM_SauceBag.obj")

    # 4. SM_CakeWrappingPaper (normalized to [-0.5, 0.5])
    paper = ObjWriter("SM_CakeWrappingPaper")
    paper.add_box(-0.5, 0.5, -0.5, 0.0, -0.5, 0.5, 0.1, 0.9, 0.1, 0.9)
    # Subtle crease borders
    paper.add_box(-0.5, 0.5, 0.0, 0.5, -0.5, -0.45, 0.1, 0.9, 0.1, 0.2)
    paper.add_box(-0.5, 0.5, 0.0, 0.5, 0.45, 0.5, 0.1, 0.9, 0.8, 0.9)
    paper.normalize_to_unit_cube()
    paper.write("Assets/TramChanh/Art/Models/CakeStation/SM_CakeWrappingPaper.obj")

def build_ready_counter_plate():
    plate = ObjWriter("SM_ReadyCounter_Plate")
    # Width X: [-0.150, 0.150], Depth Z: [-0.090, 0.090], Height Y: [0.000, 0.008]
    # Bottom plate bed
    plate.add_box(-0.145, 0.145, 0.000, 0.003, -0.085, 0.085, 0.1, 0.6, 0.1, 0.6)
    # Raised rolled rim
    plate.add_box(-0.150, 0.150, 0.000, 0.008, 0.080, 0.090, 0.6, 0.9, 0.1, 0.3)
    plate.add_box(-0.150, 0.150, 0.000, 0.008, -0.090, -0.080, 0.6, 0.9, 0.1, 0.3)
    plate.add_box(-0.150, -0.140, 0.000, 0.008, -0.080, 0.080, 0.6, 0.9, 0.1, 0.3)
    plate.add_box(0.140, 0.150, 0.000, 0.008, -0.080, 0.080, 0.6, 0.9, 0.1, 0.3)
    os.makedirs("Assets/TramChanh/Art/Models/Props", exist_ok=True)
    plate.write("Assets/TramChanh/Art/Models/Props/SM_ReadyCounter_Plate.obj")

if __name__ == "__main__":
    build_icebin_well()
    build_icebin_lids()
    build_ice_scoop()
    build_topping_station_frame()
    build_topping_station_cover()
    build_grill_elmich()
    build_cake_tool_meshes()
    build_ready_counter_plate()
    print("ALL equipment meshes generated successfully!")

