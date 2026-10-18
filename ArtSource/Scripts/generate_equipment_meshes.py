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
        self.faces.append([(v1, uv1, n), (v2, uv2, n), (v3, uv3, n)])
        self.faces.append([(v1, uv1, n), (v3, uv3, n), (v4, uv4, n)])

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

    def add_cylinder(self, cx, cz, radius, y0, y1, segments=12, u_min=0.1, u_max=0.6, v_min=0.1, v_max=0.6):
        bot_verts = []
        top_verts = []
        normals = []
        for i in range(segments):
            angle = 2.0 * math.pi * i / segments
            dx = math.cos(angle) * radius
            dz = math.sin(angle) * radius
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

            self.add_quad(bot_verts[i], bot_verts[nxt], top_verts[nxt], top_verts[i],
                          uv_b0, uv_b1, uv_t1, uv_t0, normals[i])
            self.add_tri(c_top, top_verts[nxt], top_verts[i], uv_c, uv_t1, uv_t0, n_top)
            self.add_tri(c_bot, bot_verts[i], bot_verts[nxt], uv_c, uv_b0, uv_b1, n_bot)

    def add_frustum(self, cx, cz, r_bot, r_top, y0, y1, segments=12, u_min=0.1, u_max=0.6, v_min=0.1, v_max=0.6):
        bot_verts = []
        top_verts = []
        normals = []
        dy = y1 - y0
        dr = r_bot - r_top
        slant = math.sqrt(dy*dy + dr*dr) or 1.0
        ny_side = dr / slant
        nr_side = dy / slant

        for i in range(segments):
            angle = 2.0 * math.pi * i / segments
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
        os.makedirs(os.path.dirname(filepath), exist_ok=True)
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
        bx, by, bz = self.get_bounds()
        print(f"Generated {filepath}: {len(self.verts)} verts, bounds: X:{bx}, Y:{by}, Z:{bz}")

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
    obj.add_box(-0.218, -0.208, -0.320, 0.000, -0.193, 0.193, 0.1, 0.3, 0.1, 0.6)
    obj.add_box(0.208, 0.218, -0.320, 0.000, -0.193, 0.193, 0.1, 0.3, 0.1, 0.6)
    obj.add_box(-0.208, 0.208, -0.320, 0.000, 0.183, 0.193, 0.1, 0.6, 0.1, 0.3)
    obj.add_box(-0.208, 0.208, -0.320, 0.000, -0.193, -0.183, 0.1, 0.6, 0.1, 0.3)
    obj.add_box(-0.208, 0.208, -0.320, -0.305, -0.183, 0.183, 0.1, 0.6, 0.1, 0.6)

    # Studded rubber bar mat in front of ice bin (8mm clearance separation from front flange at -0.225)
    obj.add_box(-0.235, 0.235, 0.000, 0.006, -0.278, -0.233, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(-0.235, 0.235, 0.006, 0.008, -0.278, -0.274, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(-0.235, 0.235, 0.006, 0.008, -0.237, -0.233, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(-0.235, -0.231, 0.006, 0.008, -0.274, -0.237, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(0.231, 0.235, 0.006, 0.008, -0.274, -0.237, 0.73, 0.99, 0.02, 0.38)
    for ix in range(15):
        cx = -0.215 + ix * (0.430 / 14.0)
        for iz in range(3):
            cz = -0.266 + iz * (0.022 / 2.0)
            obj.add_box(cx - 0.004, cx + 0.004, 0.006, 0.0085, cz - 0.004, cz + 0.004, 0.75, 0.85, 0.05, 0.25)

    # Narrow rubber runner strip on right side for ice scoop (10mm clearance separation from right flange at 0.250)
    obj.add_box(0.260, 0.335, 0.000, 0.006, -0.180, 0.180, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(0.260, 0.264, 0.006, 0.008, -0.180, 0.180, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(0.331, 0.335, 0.006, 0.008, -0.180, 0.180, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(0.264, 0.331, 0.006, 0.008, -0.180, -0.176, 0.73, 0.99, 0.02, 0.38)
    obj.add_box(0.264, 0.331, 0.006, 0.008, 0.176, 0.180, 0.73, 0.99, 0.02, 0.38)
    for ir in range(8):
        cz = -0.150 + ir * (0.300 / 7.0)
        obj.add_box(0.264, 0.331, 0.006, 0.0085, cz - 0.003, cz + 0.003, 0.75, 0.85, 0.05, 0.25)

    obj.write("Assets/TramChanh/Art/Models/DrinkStation/SM_IceBin_Well.obj")

def build_icebin_lids():
    obj = ObjWriter("SM_IceBin_Lids")
    # Front sliding lid (Z: -0.192 to 0.005, Y: 0.006 to 0.015)
    obj.add_box(-0.218, 0.218, 0.006, 0.015, -0.192, 0.005, 0.05, 0.45, 0.05, 0.45)
    obj.add_box(-0.060, 0.060, 0.015, 0.025, -0.120, -0.090, 0.6, 0.9, 0.1, 0.4)
    obj.add_box(-0.045, 0.045, 0.018, 0.025, -0.110, -0.100, 0.1, 0.3, 0.1, 0.3)

    # Rear sliding lid (Z: -0.005 to 0.192, Y: 0.015 to 0.024)
    obj.add_box(-0.218, 0.218, 0.015, 0.024, -0.005, 0.192, 0.05, 0.45, 0.05, 0.45)
    obj.add_box(-0.060, 0.060, 0.024, 0.034, 0.090, 0.120, 0.6, 0.9, 0.1, 0.4)
    obj.add_box(-0.045, 0.045, 0.027, 0.034, 0.100, 0.110, 0.1, 0.3, 0.1, 0.3)

    obj.write("Assets/TramChanh/Art/Models/DrinkStation/SM_IceBin_Lids.obj")

def build_ice_scoop():
    obj = ObjWriter("SM_IceScoop")
    # Tapered scoop bowl
    obj.add_frustum(0, 0.040, 0.028, 0.045, 0.000, 0.042, segments=14, u_min=0.55, u_max=0.95, v_min=0.1, v_max=0.45)
    # Scoop bowl bottom
    obj.add_box(-0.028, 0.028, 0.000, 0.004, 0.012, 0.068, 0.6, 0.9, 0.1, 0.4)
    # Cylindrical handle
    obj.add_cylinder(0, -0.075, 0.011, 0.020, 0.032, segments=12, u_min=0.55, u_max=0.95, v_min=0.1, v_max=0.45)
    # Hanging loop
    obj.add_cylinder(0, -0.145, 0.008, 0.022, 0.030, segments=8, u_min=0.55, u_max=0.95, v_min=0.1, v_max=0.45)
    # Neck connector
    obj.add_box(-0.012, 0.012, 0.015, 0.035, -0.015, 0.015, 0.6, 0.9, 0.1, 0.4)
    obj.write("Assets/TramChanh/Art/Models/DrinkStation/SM_IceScoop.obj")

def build_topping_station_frame():
    obj = ObjWriter("SM_ToppingStation_Frame")
    # Base and outer frame
    obj.add_box(-0.275, 0.275, 0.000, 0.035, -0.195, 0.195, 0.05, 0.4, 0.05, 0.4)
    # Inner pans cavity
    obj.add_box(-0.275, -0.260, 0.035, 0.145, -0.195, 0.195, 0.05, 0.2, 0.1, 0.6)
    obj.add_box(-0.275, 0.275, 0.035, 0.145, 0.180, 0.195, 0.05, 0.6, 0.1, 0.3)
    obj.add_box(-0.275, 0.275, 0.035, 0.145, -0.195, -0.180, 0.05, 0.6, 0.1, 0.3)
    obj.add_box(-0.015, 0.015, 0.035, 0.145, -0.180, 0.180, 0.1, 0.2, 0.1, 0.6)
    obj.add_box(-0.260, 0.185, 0.035, 0.145, -0.012, 0.012, 0.1, 0.6, 0.1, 0.2)
    obj.add_box(0.180, 0.195, 0.035, 0.145, -0.180, 0.180, 0.05, 0.2, 0.1, 0.6)
    obj.add_box(0.260, 0.275, 0.035, 0.145, -0.195, 0.195, 0.05, 0.2, 0.1, 0.6)

    # 4 GN pans with toppings
    obj.add_box(-0.255, -0.018, 0.080, 0.138, -0.175, -0.015, 0.74, 0.86, 0.44, 0.68)
    obj.add_box(0.018, 0.178, 0.080, 0.138, -0.175, -0.015, 0.87, 0.99, 0.44, 0.68)
    obj.add_box(-0.255, -0.018, 0.080, 0.138, 0.015, 0.175, 0.74, 0.86, 0.44, 0.68)
    obj.add_box(0.018, 0.178, 0.080, 0.138, 0.015, 0.175, 0.87, 0.99, 0.44, 0.68)

    # Syrup pump dispensers
    def add_syrup_bottle(cx, cz, u_min, u_max, v_min, v_max, spout_dz):
        obj.add_cylinder(cx, cz, 0.021, 0.035, 0.115, segments=12, u_min=u_min, u_max=u_max, v_min=v_min, v_max=v_max)
        obj.add_frustum(cx, cz, 0.021, 0.012, 0.115, 0.125, segments=12, u_min=u_min, u_max=u_max, v_min=v_min+0.1, v_max=v_max)
        obj.add_cylinder(cx, cz, 0.0135, 0.125, 0.130, segments=10, u_min=0.1, u_max=0.3, v_min=0.1, v_max=0.3)
        obj.add_cylinder(cx, cz, 0.005, 0.130, 0.142, segments=8, u_min=0.6, u_max=0.8, v_min=0.1, v_max=0.3)
        obj.add_box(cx - 0.010, cx + 0.010, 0.142, 0.148, cz - 0.010, cz + 0.010, 0.1, 0.3, 0.1, 0.3)
        sz0 = cz - 0.010
        sz1 = cz - 0.024 if spout_dz < 0 else cz + 0.024
        sz_min, sz_max = min(sz0, sz1), max(sz0, sz1)
        obj.add_box(cx - 0.004, cx + 0.004, 0.140, 0.146, sz_min, sz_max, 0.1, 0.3, 0.1, 0.3)

    add_syrup_bottle(0.230, -0.100, 0.74, 0.82, 0.74, 0.98, spout_dz=-1)
    add_syrup_bottle(0.230, -0.030, 0.83, 0.91, 0.74, 0.98, spout_dz=-1)

    # Straw basket
    obj.add_box(0.202, 0.262, 0.035, 0.120, 0.040, 0.160, 0.92, 0.99, 0.73, 0.98)
    straw_colors = [(0.75, 0.85, 0.05, 0.25), (0.1, 0.3, 0.6, 0.9), (0.7, 0.9, 0.7, 0.9)]
    for s_idx, (sx, sz) in enumerate([(0.218, 0.075), (0.242, 0.075), (0.220, 0.125), (0.244, 0.125), (0.230, 0.100)]):
        uv_c = straw_colors[s_idx % len(straw_colors)]
        obj.add_cylinder(sx, sz, 0.004, 0.120, 0.148, segments=6, u_min=uv_c[0], u_max=uv_c[1], v_min=uv_c[2], v_max=uv_c[3])

    # Hinge bracket pivot mounts on frame at Y = 0.090, Z = +0.140
    obj.add_box(-0.270, -0.245, 0.080, 0.110, 0.125, 0.155, 0.1, 0.4, 0.1, 0.4)
    obj.add_box(0.245, 0.270, 0.080, 0.110, 0.125, 0.155, 0.1, 0.4, 0.1, 0.4)
    obj.add_cylinder(-0.257, 0.140, 0.007, 0.085, 0.105, segments=8, u_min=0.1, u_max=0.3, v_min=0.1, v_max=0.3)
    obj.add_cylinder(0.257, 0.140, 0.007, 0.085, 0.105, segments=8, u_min=0.1, u_max=0.3, v_min=0.1, v_max=0.3)

    obj.write("Assets/TramChanh/Art/Models/DrinkStation/SM_ToppingStation_Frame.obj")

def build_topping_station_cover():
    obj = ObjWriter("SM_ToppingStation_Cover")
    # In TransparentCover local space, the hinge axis is at (Y = 0.000, Z = +0.140)
    # The front rim of the frame is at Z = -0.180.
    # Arc profile: from Z = +0.140 (rear hinge) up over pans (apex Y = 0.160) to Z = -0.180 (front rim).
    num_segs = 20
    profile = []
    # Center = -0.020, Radius = 0.160
    for i in range(num_segs + 1):
        t = i / float(num_segs)
        theta = math.pi * t
        z = -0.020 + 0.160 * math.cos(theta)
        y = 0.160 * math.sin(theta)
        profile.append((z, y))

    x_steps = [-0.250, -0.150, -0.050, 0.050, 0.150, 0.250]
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

    # Curved acrylic end-caps at X = -0.250 and X = +0.250
    for px, is_right in [(-0.250, False), (0.250, True)]:
        c_v = obj.add_vert(px, 0.000, -0.020)
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

    # Hinge knuckles along the hinge axis (Y = 0.000, Z = +0.140)
    obj.add_box(-0.255, -0.240, -0.010, 0.015, 0.125, 0.145, 0.1, 0.3, 0.1, 0.3)
    obj.add_box(0.240, 0.255, -0.010, 0.015, 0.125, 0.145, 0.1, 0.3, 0.1, 0.3)

    # Front handle bar on cover lip (Z = -0.185, Y = 0.015)
    obj.add_box(-0.100, 0.100, 0.005, 0.025, -0.190, -0.175, 0.1, 0.3, 0.1, 0.3)

    obj.write("Assets/TramChanh/Art/Models/DrinkStation/SM_ToppingStation_Cover.obj")

def build_grill_elmich():
    # Exact required bounds:
    # Base: X in [-0.170, 0.170], Y in [0.000, 0.125], Z in [-0.150, 0.152]
    # Lid: X in [-0.170, 0.170], Y in [-0.015, 0.058], Z in [-0.020, 0.372]

    # --- SM_Grill_Elmich_Base.obj ---
    base = ObjWriter("SM_Grill_Elmich_Base")

    # 1. Four Cylindrical Rubber Feet (Y: 0.000 .. 0.012)
    foot_r = 0.015
    for fx, fz in [(-0.140, -0.120), (0.140, -0.120), (-0.140, 0.120), (0.140, 0.120)]:
        base.add_cylinder(fx, fz, foot_r, 0.000, 0.012, segments=10, u_min=0.1, u_max=0.3, v_min=0.1, v_max=0.3)

    # 2. Lower Chassis Phenolic Body (Y: 0.012 .. 0.080)
    base.add_box(-0.170, 0.170, 0.012, 0.080, -0.135, 0.145, 0.05, 0.45, 0.05, 0.45)
    base.add_box(-0.170, -0.150, 0.012, 0.080, -0.148, -0.135, 0.05, 0.45, 0.05, 0.45)
    base.add_box(0.150, 0.170, 0.012, 0.080, -0.148, -0.135, 0.05, 0.45, 0.05, 0.45)

    # 3. Slanted Front Control Bezel & Digital Display (Facing Seller: -Z direction, reaching Z = -0.150)
    base.add_box(-0.145, 0.145, 0.015, 0.075, -0.148, -0.135, 0.02, 0.48, 0.02, 0.48)
    disp_v_bl = base.add_vert(-0.080, 0.025, -0.150)
    disp_v_br = base.add_vert(0.080, 0.025, -0.150)
    disp_v_tr = base.add_vert(0.080, 0.065, -0.145)
    disp_v_tl = base.add_vert(-0.080, 0.065, -0.145)
    uv_disp_bl = base.add_uv(0.0, 0.0)
    uv_disp_br = base.add_uv(1.0, 0.0)
    uv_disp_tr = base.add_uv(1.0, 1.0)
    uv_disp_tl = base.add_uv(0.0, 1.0)
    n_disp = base.add_normal(0.0, 0.12, -0.99)
    base.add_quad(disp_v_bl, disp_v_br, disp_v_tr, disp_v_tl,
                  uv_disp_bl, uv_disp_br, uv_disp_tr, uv_disp_tl, n_disp)

    # 4. Lower Ribbed Cooking Plate (Y: 0.080 .. 0.095)
    base.add_box(-0.160, 0.160, 0.080, 0.088, -0.130, 0.140, 0.1, 0.3, 0.1, 0.3)
    base.add_box(-0.140, 0.140, 0.085, 0.092, -0.115, 0.125, 0.6, 0.9, 0.1, 0.4)
    num_ribs = 14
    for i in range(num_ribs):
        rx_c = -0.140 + (i + 0.5) * (0.280 / num_ribs)
        base.add_box(rx_c - 0.003, rx_c + 0.003, 0.092, 0.096, -0.110, 0.120, 0.6, 0.9, 0.1, 0.4)

    # 5. Rear Hinge Towers (Rising up to Y = 0.125 at Z = +0.120 .. +0.150)
    base.add_box(-0.140, -0.115, 0.080, 0.125, 0.115, 0.150, 0.05, 0.3, 0.05, 0.3)
    base.add_box(0.115, 0.140, 0.080, 0.125, 0.115, 0.150, 0.05, 0.3, 0.05, 0.3)

    # 6. Rear Drip Tray Lip reaching Z = +0.152
    base.add_box(-0.090, 0.090, 0.015, 0.045, 0.144, 0.152, 0.05, 0.25, 0.05, 0.25)

    base.write("Assets/TramChanh/Art/Models/CakeStation/SM_Grill_Elmich_Base.obj")

    # --- SM_Grill_Elmich_Lid.obj ---
    lid = ObjWriter("SM_Grill_Elmich_Lid")

    # 1. Rear Hinge Knuckles (Z down to -0.020, Y down to -0.014)
    lid.add_box(-0.140, -0.115, -0.014, 0.030, -0.020, 0.040, 0.05, 0.3, 0.05, 0.3)
    lid.add_box(0.115, 0.140, -0.014, 0.030, -0.020, 0.040, 0.05, 0.3, 0.05, 0.3)
    lid.add_cylinder(-0.127, -0.005, 0.010, -0.012, 0.025, segments=8, u_min=0.6, u_max=0.9, v_min=0.1, v_max=0.3)
    lid.add_cylinder(0.127, -0.005, 0.010, -0.012, 0.025, segments=8, u_min=0.6, u_max=0.9, v_min=0.1, v_max=0.3)

    # 2. Underside Ribbed Upper Grill Plate (Y: -0.014 .. 0.008, Z in [0.040, 0.280])
    lid.add_box(-0.140, 0.140, 0.000, 0.008, 0.040, 0.280, 0.6, 0.9, 0.1, 0.4)
    for i in range(num_ribs):
        rx_c = -0.140 + (i + 0.5) * (0.280 / num_ribs)
        lid.add_box(rx_c - 0.003, rx_c + 0.003, -0.014, 0.000, 0.045, 0.275, 0.6, 0.9, 0.1, 0.4)

    # 3. Main Upper Hood Body (X in [-0.170, 0.170], Y in [0.010, 0.055], Z in [0.030, 0.320])
    lid.add_box(-0.155, 0.155, 0.010, 0.052, 0.030, 0.315, 0.05, 0.45, 0.05, 0.45)
    lid.add_box(-0.170, -0.150, 0.015, 0.048, 0.080, 0.240, 0.05, 0.35, 0.05, 0.35)
    lid.add_box(0.150, 0.170, 0.015, 0.048, 0.080, 0.240, 0.05, 0.35, 0.05, 0.35)

    # 4. Top Brushed Stainless Decorative Cover Plate (Y reaching 0.058)
    lid.add_box(-0.135, 0.135, 0.052, 0.058, 0.050, 0.295, 0.55, 0.95, 0.15, 0.45)

    # 5. Cantilever Handle Arms & Tubular Stainless Press Handle (Z up to 0.372)
    lid.add_box(-0.130, -0.110, 0.020, 0.032, 0.300, 0.360, 0.6, 0.9, 0.1, 0.3)
    lid.add_box(0.110, 0.130, 0.020, 0.032, 0.300, 0.360, 0.6, 0.9, 0.1, 0.3)
    h_r = 0.012
    h_z = 0.360
    h_y = 0.025
    h_segs = 12
    h_left = []
    h_right = []
    for i in range(h_segs):
        ang = 2.0 * math.pi * i / float(h_segs)
        vy = h_y + h_r * math.cos(ang)
        vz = h_z + h_r * math.sin(ang)
        h_left.append(lid.add_vert(-0.150, vy, vz))
        h_right.append(lid.add_vert(0.150, vy, vz))

    uv_h_b = lid.add_uv(0.6, 0.1)
    uv_h_t = lid.add_uv(0.9, 0.4)
    for i in range(h_segs):
        ni = (i + 1) % h_segs
        ang_mid = 2.0 * math.pi * (i + 0.5) / float(h_segs)
        n_side = lid.add_normal(0, math.cos(ang_mid), math.sin(ang_mid))
        lid.add_quad(h_left[i], h_right[i], h_right[ni], h_left[ni],
                     uv_h_b, uv_h_t, uv_h_t, uv_h_b, n_side)

    c_l = lid.add_vert(-0.150, h_y, h_z)
    c_r = lid.add_vert(0.150, h_y, h_z)
    n_hl = lid.add_normal(-1, 0, 0)
    n_hr = lid.add_normal(1, 0, 0)
    for i in range(h_segs):
        ni = (i + 1) % h_segs
        lid.add_tri(c_l, h_left[ni], h_left[i], uv_h_b, uv_h_t, uv_h_b, n_hl)
        lid.add_tri(c_r, h_right[i], h_right[ni], uv_h_b, uv_h_t, uv_h_b, n_hr)

    lid.write("Assets/TramChanh/Art/Models/CakeStation/SM_Grill_Elmich_Lid.obj")

def build_wipe_cloth():
    cloth = ObjWriter("SM_WipeCloth")
    cloth.add_box(-0.078, 0.078, 0.000, 0.009, -0.068, 0.068, 0.05, 0.95, 0.05, 0.95)
    cloth.add_cylinder(-0.076, 0.000, 0.0045, 0.000, 0.009, segments=8, u_min=0.1, u_max=0.3, v_min=0.1, v_max=0.3)
    cloth.add_box(-0.075, 0.075, 0.009, 0.017, -0.065, 0.065, 0.05, 0.95, 0.05, 0.95)
    cloth.add_box(-0.075, 0.075, 0.016, 0.018, 0.060, 0.065, 0.05, 0.95, 0.85, 0.95)
    cloth.add_box(-0.075, 0.075, 0.016, 0.018, -0.065, -0.060, 0.05, 0.95, 0.05, 0.15)
    cloth.add_box(-0.075, -0.070, 0.016, 0.018, -0.060, 0.060, 0.05, 0.15, 0.05, 0.95)
    cloth.add_box(0.070, 0.075, 0.016, 0.018, -0.060, 0.060, 0.85, 0.95, 0.05, 0.95)
    for i in range(3):
        cz = -0.035 + i * 0.035
        cloth.add_box(-0.065, 0.065, 0.016, 0.0185, cz - 0.003, cz + 0.003, 0.1, 0.9, 0.4, 0.6)

    os.makedirs("Assets/TramChanh/Art/Models/Tools", exist_ok=True)
    cloth.write("Assets/TramChanh/Art/Models/Tools/SM_WipeCloth.obj")

def build_cake_food_meshes():
    os.makedirs("Assets/TramChanh/Art/Models/Food", exist_ok=True)

    # 1. SM_Cake_Flat (waffle sheet with grilled ribs, sauce, and chà bông toppings)
    flat = ObjWriter("SM_Cake_Flat")
    flat.add_box(-0.48, 0.48, -0.40, 0.10, -0.48, 0.48, 0.05, 0.95, 0.05, 0.95)
    for i in range(7):
        cz = -0.42 + i * (0.84 / 6.0)
        flat.add_box(-0.47, 0.47, 0.08, 0.25, cz - 0.035, cz + 0.035, 0.05, 0.95, 0.1, 0.9)
    for i in [1, 3, 5]:
        cz = -0.42 + i * (0.84 / 6.0)
        flat.add_box(-0.45, 0.45, 0.24, 0.38, cz - 0.020, cz + 0.020, 0.1, 0.9, 0.4, 0.6)
    for i in range(12):
        mx = -0.38 + (i % 6) * 0.14
        mz = -0.30 + (i // 6) * 0.45
        flat.add_box(mx - 0.045, mx + 0.045, 0.24, 0.48, mz - 0.045, mz + 0.045, 0.7, 0.95, 0.7, 0.95)
    flat.normalize_to_unit_cube()
    flat.write("Assets/TramChanh/Art/Models/Food/SM_Cake_Flat.obj")

    # 2. SM_Cake_RolledVertical (two cylindrical rolls with longitudinal fluted ridges)
    rolled = ObjWriter("SM_Cake_RolledVertical")
    r_outer = 0.22
    rolled.add_cylinder(-0.24, 0, r_outer, -0.48, 0.48, segments=16, u_min=0.05, u_max=0.95, v_min=0.05, v_max=0.95)
    for i in range(6):
        ang = 2.0 * math.pi * i / 6.0
        bx = -0.24 + math.cos(ang) * (r_outer - 0.01)
        bz = math.sin(ang) * (r_outer - 0.01)
        rolled.add_box(bx - 0.025, bx + 0.025, -0.46, 0.46, bz - 0.025, bz + 0.025, 0.6, 0.9, 0.1, 0.4)
    rolled.add_cylinder(0.24, 0, r_outer, -0.48, 0.48, segments=16, u_min=0.05, u_max=0.95, v_min=0.05, v_max=0.95)
    for i in range(6):
        ang = 2.0 * math.pi * i / 6.0
        bx = 0.24 + math.cos(ang) * (r_outer - 0.01)
        bz = math.sin(ang) * (r_outer - 0.01)
        rolled.add_box(bx - 0.025, bx + 0.025, -0.46, 0.46, bz - 0.025, bz + 0.025, 0.6, 0.9, 0.1, 0.4)
    rolled.normalize_to_unit_cube()
    rolled.write("Assets/TramChanh/Art/Models/Food/SM_Cake_RolledVertical.obj")

    # 3. SM_Cake_WrappedInPaper (normalized unit box [-0.5, 0.5])
    wrap = ObjWriter("SM_Cake_WrappedInPaper")
    wrap.add_cylinder(0, 0, 0.38, -0.48, 0.48, segments=14, u_min=0.1, u_max=0.8, v_min=0.1, v_max=0.8)
    wrap.add_cylinder(0, 0, 0.46, -0.35, 0.35, segments=16, u_min=0.02, u_max=0.98, v_min=0.02, v_max=0.98)
    wrap.add_box(-0.15, 0.15, -0.35, 0.35, 0.44, 0.49, 0.1, 0.9, 0.1, 0.9)
    wrap.add_box(-0.42, 0.42, -0.50, -0.35, -0.42, 0.42, 0.1, 0.9, 0.1, 0.9)
    wrap.normalize_to_unit_cube()
    wrap.write("Assets/TramChanh/Art/Models/Food/SM_Cake_WrappedInPaper.obj")

def build_cake_tool_meshes():
    # 1. SM_Spatula_WoodHandle (normalized to [-0.5, 0.5])
    spat = ObjWriter("SM_Spatula_WoodHandle")
    spat.add_cylinder(0, -0.30, 0.06, -0.05, 0.05, segments=10, u_min=0.1, u_max=0.4, v_min=0.1, v_max=0.6)
    spat.add_box(-0.04, 0.04, -0.02, 0.02, -0.15, 0.05, 0.6, 0.9, 0.1, 0.4)
    spat.add_box(-0.12, 0.12, -0.01, 0.01, 0.05, 0.50, 0.6, 0.9, 0.1, 0.4)
    spat.normalize_to_unit_cube()
    spat.write("Assets/TramChanh/Art/Models/CakeStation/SM_Spatula_WoodHandle.obj")

    # 2. SM_Scissors_RedGray (normalized to [-0.5, 0.5])
    scis = ObjWriter("SM_Scissors_RedGray")
    scis.add_cylinder(0, 0, 0.15, -0.3, 0.3, segments=10, u_min=0.6, u_max=0.8, v_min=0.1, v_max=0.3)
    scis.add_box(-0.15, 0.0, -0.05, 0.05, 0.0, 0.5, 0.6, 0.9, 0.3, 0.6)
    scis.add_box(0.0, 0.15, -0.05, 0.05, 0.0, 0.5, 0.6, 0.9, 0.3, 0.6)
    scis.add_cylinder(-0.35, -0.3, 0.22, -0.2, 0.2, segments=12, u_min=0.1, u_max=0.4, v_min=0.6, v_max=0.9)
    scis.add_cylinder(0.35, -0.25, 0.28, -0.2, 0.2, segments=12, u_min=0.1, u_max=0.4, v_min=0.6, v_max=0.9)
    scis.add_box(-0.3, -0.05, -0.15, 0.15, -0.2, 0.0, 0.1, 0.4, 0.6, 0.9)
    scis.add_box(0.05, 0.3, -0.15, 0.15, -0.2, 0.0, 0.1, 0.4, 0.6, 0.9)
    scis.normalize_to_unit_cube()
    scis.write("Assets/TramChanh/Art/Models/CakeStation/SM_Scissors_RedGray.obj")

    # 3. SM_SauceBag (normalized to [-0.5, 0.5])
    sauce = ObjWriter("SM_SauceBag")
    sauce.add_frustum(0, 0, 0.08, 0.45, -0.5, 0.35, segments=16, u_min=0.1, u_max=0.5, v_min=0.1, v_max=0.8)
    sauce.add_box(-0.35, 0.35, 0.35, 0.5, -0.08, 0.08, 0.1, 0.5, 0.8, 0.95)
    sauce.normalize_to_unit_cube()
    sauce.write("Assets/TramChanh/Art/Models/CakeStation/SM_SauceBag.obj")

    # 4. SM_CakeWrappingPaper (Neat stack of Kraft wrapping paper sheets with Trạm Chanh brand stamp)
    paper = ObjWriter("SM_CakeWrappingPaper")
    paper.add_box(-0.081, 0.081, 0.000, 0.002, -0.066, 0.066, 0.05, 0.95, 0.05, 0.95)
    v1 = paper.add_vert(-0.080, 0.0028, -0.065)
    v2 = paper.add_vert(0.080, 0.0028, -0.065)
    v3 = paper.add_vert(0.080, 0.0028, 0.065)
    v4 = paper.add_vert(-0.080, 0.0028, 0.065)
    uv1 = paper.add_uv(0.0, 0.0)
    uv2 = paper.add_uv(1.0, 0.0)
    uv3 = paper.add_uv(1.0, 1.0)
    uv4 = paper.add_uv(0.0, 1.0)
    py = paper.add_normal(0, 1, 0)
    paper.add_quad(v1, v4, v3, v2, uv1, uv4, uv3, uv2, py)
    paper.add_box(-0.080, 0.080, 0.002, 0.0028, -0.065, 0.065, 0.0, 1.0, 0.0, 1.0)
    paper.write("Assets/TramChanh/Art/Models/CakeStation/SM_CakeWrappingPaper.obj")

    # 5. SM_RollBoard (Black rubber bar mat next to grill for rolling & tools)
    mat = ObjWriter("SM_RollBoard")
    mat.add_box(-0.100, 0.100, 0.000, 0.003, -0.070, 0.070, 0.04, 0.96, 0.04, 0.96)
    mat.add_box(-0.100, 0.100, 0.003, 0.006, -0.070, -0.062, 0.0, 1.0, 0.0, 0.08)
    mat.add_box(-0.100, 0.100, 0.003, 0.006, 0.062, 0.070, 0.0, 1.0, 0.92, 1.0)
    mat.add_box(-0.100, -0.092, 0.003, 0.006, -0.062, 0.062, 0.0, 0.08, 0.08, 0.92)
    mat.add_box(0.092, 0.100, 0.003, 0.006, -0.062, 0.062, 0.92, 1.0, 0.08, 0.92)
    mat.write("Assets/TramChanh/Art/Models/CakeStation/SM_RollBoard.obj")

    # 6. SM_BatterContainer (Stainless steel batter dispenser container)
    vat = ObjWriter("SM_BatterContainer")
    vat.add_cylinder(0, 0, 0.060, 0.000, 0.076, segments=16, u_min=0.1, u_max=0.9, v_min=0.1, v_max=0.9)
    vat.add_cylinder(0, 0, 0.064, 0.074, 0.080, segments=16, u_min=0.1, u_max=0.9, v_min=0.1, v_max=0.9)
    vat.add_box(0.062, 0.076, 0.040, 0.065, -0.015, 0.015, 0.1, 0.5, 0.1, 0.5)
    vat.write("Assets/TramChanh/Art/Models/CakeStation/SM_BatterContainer.obj")

    # 7. SM_Grill_Display (Slanted touch panel screen mesh)
    disp = ObjWriter("SM_Grill_Display")
    v_bl = disp.add_vert(0.080, 0.025, -0.1505)
    v_br = disp.add_vert(-0.080, 0.025, -0.1505)
    v_tr = disp.add_vert(-0.080, 0.065, -0.1455)
    v_tl = disp.add_vert(0.080, 0.065, -0.1455)
    uv_bl = disp.add_uv(0.0, 0.0)
    uv_br = disp.add_uv(1.0, 0.0)
    uv_tr = disp.add_uv(1.0, 1.0)
    uv_tl = disp.add_uv(0.0, 1.0)
    n_disp = disp.add_normal(0.0, 0.12, -0.99)
    disp.add_quad(v_bl, v_br, v_tr, v_tl, uv_bl, uv_br, uv_tr, uv_tl, n_disp)
    disp.write("Assets/TramChanh/Art/Models/CakeStation/SM_Grill_Display.obj")

def build_ready_counter_plate():
    plate = ObjWriter("SM_ReadyCounter_Plate")
    plate.add_box(-0.145, 0.145, 0.000, 0.003, -0.085, 0.085, 0.1, 0.6, 0.1, 0.6)
    plate.add_box(-0.150, 0.150, 0.000, 0.008, 0.080, 0.090, 0.6, 0.9, 0.1, 0.3)
    plate.add_box(-0.150, 0.150, 0.000, 0.008, -0.090, -0.080, 0.6, 0.9, 0.1, 0.3)
    plate.add_box(-0.150, -0.140, 0.000, 0.008, -0.080, 0.080, 0.6, 0.9, 0.1, 0.3)
    plate.add_box(0.140, 0.150, 0.000, 0.008, -0.080, 0.080, 0.6, 0.9, 0.1, 0.3)
    os.makedirs("Assets/TramChanh/Art/Models/Props", exist_ok=True)
    plate.write("Assets/TramChanh/Art/Models/Props/SM_ReadyCounter_Plate.obj")

def build_plastic_stool():
    try:
        from generate_stool_mesh import generate_plastic_stool
    except ImportError:
        import sys
        sys.path.append(os.path.dirname(__file__))
        from generate_stool_mesh import generate_plastic_stool
    generate_plastic_stool("Assets/TramChanh/Art/Models/Furniture/SM_PlasticStool.obj")

if __name__ == "__main__":
    build_icebin_well()
    build_icebin_lids()
    build_ice_scoop()
    build_topping_station_frame()
    build_topping_station_cover()
    build_grill_elmich()
    build_wipe_cloth()
    build_cake_food_meshes()
    build_cake_tool_meshes()
    build_ready_counter_plate()
    build_plastic_stool()
    print("ALL equipment and prop meshes generated successfully!")
