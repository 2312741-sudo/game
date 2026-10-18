#!/usr/bin/env python3
"""
generate_authentic_cake_and_props.py
Procedurally generates authentic high-fidelity 3D meshes for Trạm Chanh Cake Station:
1. SM_Grill_Elmich_Base: Matte black molded chassis, rubber feet, slanted front console, ribbed cooking plate, rear hinge towers (max Z = 0.140).
2. SM_Grill_Elmich_Lid: Cantilever arms, underside ribbed cooking plate, brushed stainless decorative cover with logo, cylindrical handle bar.
3. SM_Grill_Display: Angled digital LED touch display screen.
4. SM_ReadyCounter_Plate: Elegant cafe serving platter with curved lip, recessed bed, coaster deboss, rounded corners.
5. SM_Spatula_WoodHandle: Stainless steel offset blade, brass ferrule, ergonomic lathe-turned wooden handle.
6. SM_Scissors_RedGray: Dual-color molded ergonomic handles (red/grey), crossed precision cutting blades.
7. SM_SauceBag: Translucent culinary squeeze bottle, conical nozzle, ribbed cap, visible sauce level.
8. SM_CakeWrappingPaper: Stack of Kraft baking paper sheets with crisp folds.
9. SM_RollBoard: Heavy-duty black silicone/rubber bar mat with drainage border and textured studs.
"""

import os
import math

class FastObj:
    def __init__(self, name):
        self.name = name
        self.verts = []
        self.uvs = []
        self.normals = []
        self.faces = []

    def v(self, x, y, z):
        self.verts.append((x, y, z))
        return len(self.verts)

    def vt(self, u, v):
        self.uvs.append((u, v))
        return len(self.uvs)

    def vn(self, nx, ny, nz):
        l = math.sqrt(nx*nx + ny*ny + nz*nz) or 1.0
        self.normals.append((nx/l, ny/l, nz/l))
        return len(self.normals)

    def quad(self, v1, v2, v3, v4, uv1, uv2, uv3, uv4, n):
        self.faces.append([(v1, uv1, n), (v2, uv2, n), (v3, uv3, n)])
        self.faces.append([(v1, uv1, n), (v3, uv3, n), (v4, uv4, n)])

    def tri(self, v1, v2, v3, uv1, uv2, uv3, n):
        self.faces.append([(v1, uv1, n), (v2, uv2, n), (v3, uv3, n)])

    def add_box(self, x0, x1, y0, y1, z0, z1, u0=0.1, u1=0.4, v0=0.1, v1=0.4):
        v000 = self.v(x0, y0, z0)
        v100 = self.v(x1, y0, z0)
        v110 = self.v(x1, y1, z0)
        v010 = self.v(x0, y1, z0)
        v001 = self.v(x0, y0, z1)
        v101 = self.v(x1, y0, z1)
        v111 = self.v(x1, y1, z1)
        v011 = self.v(x0, y1, z1)

        uv00 = self.vt(u0, v0)
        uv10 = self.vt(u1, v0)
        uv11 = self.vt(u1, v1)
        uv01 = self.vt(u0, v1)

        n_front = self.vn(0, 0, 1)
        n_back = self.vn(0, 0, -1)
        n_top = self.vn(0, 1, 0)
        n_bot = self.vn(0, -1, 0)
        n_right = self.vn(1, 0, 0)
        n_left = self.vn(-1, 0, 0)

        self.quad(v001, v101, v111, v011, uv00, uv10, uv11, uv01, n_front)
        self.quad(v100, v000, v010, v110, uv00, uv10, uv11, uv01, n_back)
        self.quad(v011, v111, v110, v010, uv00, uv10, uv11, uv01, n_top)
        self.quad(v000, v100, v101, v001, uv00, uv10, uv11, uv01, n_bot)
        self.quad(v101, v100, v110, v111, uv00, uv10, uv11, uv01, n_right)
        self.quad(v000, v001, v011, v010, uv00, uv10, uv11, uv01, n_left)

    def add_cylinder(self, cx, cz, radius, y0, y1, segs=12, u0=0.1, u1=0.4, v0=0.1, v1=0.4):
        bot, top, norms = [], [], []
        for i in range(segs):
            ang = 2.0 * math.pi * i / segs
            dx = math.cos(ang) * radius
            dz = math.sin(ang) * radius
            bot.append(self.v(cx + dx, y0, cz + dz))
            top.append(self.v(cx + dx, y1, cz + dz))
            norms.append(self.vn(math.cos(ang), 0, math.sin(ang)))

        n_top = self.vn(0, 1, 0)
        n_bot = self.vn(0, -1, 0)
        c_top = self.v(cx, y1, cz)
        c_bot = self.v(cx, y0, cz)
        uv_c = self.vt((u0+u1)/2, (v0+v1)/2)

        for i in range(segs):
            nxt = (i + 1) % segs
            ui0 = u0 + (u1 - u0) * (i / segs)
            ui1 = u0 + (u1 - u0) * ((i + 1) / segs)
            uv_b0 = self.vt(ui0, v0)
            uv_b1 = self.vt(ui1, v0)
            uv_t1 = self.vt(ui1, v1)
            uv_t0 = self.vt(ui0, v1)
            self.quad(bot[i], bot[nxt], top[nxt], top[i], uv_b0, uv_b1, uv_t1, uv_t0, norms[i])
            self.tri(c_top, top[nxt], top[i], uv_c, uv_t1, uv_t0, n_top)
            self.tri(c_bot, bot[i], bot[nxt], uv_c, uv_b0, uv_b1, n_bot)

    def add_cylinder_x(self, cy, cz, radius, x0, x1, segs=12, u0=0.6, u1=0.9, v0=0.1, v1=0.4):
        left, right, norms = [], [], []
        for i in range(segs):
            ang = 2.0 * math.pi * i / segs
            dy = math.cos(ang) * radius
            dz = math.sin(ang) * radius
            left.append(self.v(x0, cy + dy, cz + dz))
            right.append(self.v(x1, cy + dy, cz + dz))
            norms.append(self.vn(0, math.cos(ang), math.sin(ang)))

        n_l = self.vn(-1, 0, 0)
        n_r = self.vn(1, 0, 0)
        c_l = self.v(x0, cy, cz)
        c_r = self.v(x1, cy, cz)
        uv_c = self.vt((u0+u1)/2, (v0+v1)/2)

        for i in range(segs):
            nxt = (i + 1) % segs
            ui0 = u0 + (u1 - u0) * (i / segs)
            ui1 = u0 + (u1 - u0) * ((i + 1) / segs)
            uv_b0 = self.vt(ui0, v0)
            uv_b1 = self.vt(ui1, v0)
            uv_t1 = self.vt(ui1, v1)
            uv_t0 = self.vt(ui0, v1)
            self.quad(left[i], right[i], right[nxt], left[nxt], uv_b0, uv_t0, uv_t1, uv_b1, norms[i])
            self.tri(c_l, left[nxt], left[i], uv_c, uv_b1, uv_b0, n_l)
            self.tri(c_r, right[i], right[nxt], uv_c, uv_t0, uv_t1, n_r)

    def normalize_to_unit_cube(self):
        xs = [v[0] for v in self.verts]
        ys = [v[1] for v in self.verts]
        zs = [v[2] for v in self.verts]
        xmin, xmax = min(xs), max(xs)
        ymin, ymax = min(ys), max(ys)
        zmin, zmax = min(zs), max(zs)
        dx = xmax - xmin or 1.0
        dy = ymax - ymin or 1.0
        dz = zmax - zmin or 1.0
        self.verts = [(-0.5 + (x - xmin)/dx, -0.5 + (y - ymin)/dy, -0.5 + (z - zmin)/dz) for x, y, z in self.verts]

    def write(self, path):
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, 'w') as f:
            f.write(f'# Wavefront OBJ: {self.name}\n')
            f.write(f'o {self.name}\n')
            for vx, vy, vz in self.verts:
                f.write(f'v {vx:.6f} {vy:.6f} {vz:.6f}\n')
            for u, v in self.uvs:
                f.write(f'vt {u:.6f} {v:.6f}\n')
            for nx, ny, nz in self.normals:
                f.write(f'vn {nx:.6f} {ny:.6f} {nz:.6f}\n')
            for face in self.faces:
                f.write('f ' + ' '.join(f'{v}/{vt}/{vn}' for v, vt, vn in face) + '\n')
        print(f'Wrote {path} ({len(self.verts)} verts, {len(self.faces)} tris)')


def generate_grill_base(out_path):
    base = FastObj("SM_Grill_Elmich_Base")
    # UV Zones:
    # 0.05..0.45, 0.05..0.45: Matte Black Chassis
    # 0.55..0.95, 0.05..0.45: Stainless Steel
    # 0.05..0.45, 0.55..0.95: Ribbed Cooking Plate
    # 0.55..0.95, 0.55..0.95: Rubber Feet

    # 1. Four Cylindrical Rubber Feet (Y: 0.000 .. 0.012)
    foot_r = 0.015
    for fx, fz in [(-0.140, -0.115), (0.140, -0.115), (-0.140, 0.115), (0.140, 0.115)]:
        base.add_cylinder(fx, fz, foot_r, 0.000, 0.012, segs=12, u0=0.6, u1=0.9, v0=0.6, v1=0.9)

    # 2. Lower Chassis Main Phenolic Body (Y: 0.012 .. 0.078, Z: -0.135 .. 0.135)
    base.add_box(-0.168, 0.168, 0.012, 0.078, -0.135, 0.135, u0=0.05, u1=0.45, v0=0.05, v1=0.45)
    # Side shoulder bevels
    base.add_box(-0.170, 0.170, 0.030, 0.070, -0.135, 0.135, u0=0.05, u1=0.45, v0=0.05, v1=0.45)

    # 3. Slanted Front Control Bezel (Facing -Z, reaching Z = -0.150)
    # Sloping front face: connects (-0.150, 0.015, -0.150) up to (-0.150, 0.075, -0.135)
    v_bfl = base.v(-0.155, 0.015, -0.150)
    v_bfr = base.v(0.155, 0.015, -0.150)
    v_tfr = base.v(0.155, 0.075, -0.135)
    v_tfl = base.v(-0.155, 0.075, -0.135)
    uv_b0 = base.vt(0.05, 0.05)
    uv_b1 = base.vt(0.45, 0.05)
    uv_t1 = base.vt(0.45, 0.45)
    uv_t0 = base.vt(0.05, 0.45)
    n_slope = base.vn(0.0, 0.20, -0.98)
    base.quad(v_bfl, v_bfr, v_tfr, v_tfl, uv_b0, uv_b1, uv_t1, uv_t0, n_slope)
    # Front bottom lip and sides
    base.add_box(-0.168, 0.168, 0.012, 0.020, -0.150, -0.135, u0=0.05, u1=0.45, v0=0.05, v1=0.45)

    # 4. Lower Cooking Plate Well & Ribs (Y: 0.078 .. 0.092, X: -0.145 .. 0.145, Z: -0.125 .. 0.125)
    # Recessed tray bed
    base.add_box(-0.155, 0.155, 0.078, 0.084, -0.130, 0.130, u0=0.05, u1=0.45, v0=0.05, v1=0.45)
    base.add_box(-0.148, 0.148, 0.082, 0.088, -0.125, 0.125, u0=0.08, u1=0.42, v0=0.58, v1=0.92)
    # 14 Grill ribs
    num_ribs = 14
    for i in range(num_ribs):
        rx = -0.140 + (i + 0.5) * (0.280 / num_ribs)
        base.add_box(rx - 0.0035, rx + 0.0035, 0.088, 0.093, -0.120, 0.120, u0=0.10, u1=0.40, v0=0.60, v1=0.90)

    # 5. Rear Hinge Towers (Rising up to Y = 0.125, max Z = 0.140 STRICTLY)
    base.add_box(-0.140, -0.115, 0.078, 0.125, 0.110, 0.140, u0=0.05, u1=0.45, v0=0.05, v1=0.45)
    base.add_box(0.115, 0.140, 0.078, 0.125, 0.110, 0.140, u0=0.05, u1=0.45, v0=0.05, v1=0.45)
    # Stainless pivot axle pins at Y = 0.120, Z = 0.135
    base.add_cylinder_x(0.120, 0.134, 0.005, -0.145, -0.110, segs=8, u0=0.6, u1=0.9, v0=0.1, v1=0.4)
    base.add_cylinder_x(0.120, 0.134, 0.005, 0.110, 0.145, segs=8, u0=0.6, u1=0.9, v0=0.1, v1=0.4)

    # 6. Side lock/slider levers
    base.add_box(-0.170, -0.162, 0.035, 0.055, -0.040, 0.040, u0=0.6, u1=0.9, v0=0.1, v1=0.4)
    base.add_box(0.162, 0.170, 0.035, 0.055, -0.040, 0.040, u0=0.6, u1=0.9, v0=0.1, v1=0.4)

    base.write(out_path)


def generate_grill_lid(out_path):
    lid = FastObj("SM_Grill_Elmich_Lid")
    # Coordinates in Lid space:
    # +Z points towards the front handle (Z in [0.000, 0.360])
    # +Y points upward (Y in [-0.015, 0.058])
    # X in [-0.170, 0.170]

    # 1. Rear Hinge Knuckles at Z = 0.000..0.030, Y = -0.010..0.025
    lid.add_box(-0.140, -0.115, -0.010, 0.025, 0.000, 0.035, u0=0.05, u1=0.45, v0=0.05, v1=0.45)
    lid.add_box(0.115, 0.140, -0.010, 0.025, 0.000, 0.035, u0=0.05, u1=0.45, v0=0.05, v1=0.45)
    # Hinge sleeve cylinders
    lid.add_cylinder_x(0.005, 0.010, 0.012, -0.145, -0.110, segs=10, u0=0.05, u1=0.45, v0=0.05, v1=0.45)
    lid.add_cylinder_x(0.005, 0.010, 0.012, 0.110, 0.145, segs=10, u0=0.05, u1=0.45, v0=0.05, v1=0.45)

    # 2. Cantilever Structural Side Arms (X = +-0.135, Z in [0.020, 0.355])
    lid.add_box(-0.145, -0.125, 0.015, 0.035, 0.020, 0.355, u0=0.05, u1=0.45, v0=0.05, v1=0.45)
    lid.add_box(0.125, 0.145, 0.015, 0.035, 0.020, 0.355, u0=0.05, u1=0.45, v0=0.05, v1=0.45)

    # 3. Underside Upper Ribbed Cooking Plate (Y: -0.014 .. 0.006, Z in [0.040, 0.280])
    lid.add_box(-0.145, 0.145, 0.000, 0.006, 0.040, 0.280, u0=0.08, u1=0.42, v0=0.58, v1=0.92)
    num_ribs = 14
    for i in range(num_ribs):
        rx = -0.140 + (i + 0.5) * (0.280 / num_ribs)
        lid.add_box(rx - 0.0035, rx + 0.0035, -0.012, 0.000, 0.045, 0.275, u0=0.10, u1=0.40, v0=0.60, v1=0.90)

    # 4. Main Upper Hood Matte Black Casing (Y: 0.006 .. 0.052, X: -0.155 .. 0.155, Z: 0.030 .. 0.315)
    lid.add_box(-0.155, 0.155, 0.006, 0.052, 0.030, 0.315, u0=0.05, u1=0.45, v0=0.05, v1=0.45)
    # Beveled perimeter rim
    lid.add_box(-0.160, 0.160, 0.010, 0.045, 0.035, 0.310, u0=0.05, u1=0.45, v0=0.05, v1=0.45)

    # 5. Top Inset Brushed Stainless Steel Decorative Plate (Y: 0.052 .. 0.058, X: -0.135 .. 0.135, Z: 0.050 .. 0.295)
    # Mapped to Zone 2 (Brushed Stainless Steel with Elmich cursive logo!)
    lid.add_box(-0.135, 0.135, 0.052, 0.058, 0.050, 0.295, u0=0.55, u1=0.95, v0=0.05, v1=0.45)

    # 6. Front Handle: Cantilever brackets & Cylindrical Stainless Press Handle
    # Brackets reaching to Z = 0.360
    lid.add_box(-0.142, -0.128, 0.018, 0.032, 0.315, 0.360, u0=0.6, u1=0.9, v0=0.1, v1=0.4)
    lid.add_box(0.128, 0.142, 0.018, 0.032, 0.315, 0.360, u0=0.6, u1=0.9, v0=0.1, v1=0.4)
    # Cylindrical stainless handle bar across front (X: -0.150 .. 0.150, Y = 0.025, Z = 0.360, R = 0.012)
    lid.add_cylinder_x(0.025, 0.360, 0.012, -0.150, 0.150, segs=16, u0=0.55, u1=0.95, v0=0.05, v1=0.45)

    lid.write(out_path)


def generate_grill_display(out_path):
    disp = FastObj("SM_Grill_Display")
    v_bl = disp.v(0.080, 0.025, -0.1505)
    v_br = disp.v(-0.080, 0.025, -0.1505)
    v_tr = disp.v(-0.080, 0.065, -0.1455)
    v_tl = disp.v(0.080, 0.065, -0.1455)
    uv_bl = disp.vt(0.0, 0.0)
    uv_br = disp.vt(1.0, 0.0)
    uv_tr = disp.vt(1.0, 1.0)
    uv_tl = disp.vt(0.0, 1.0)
    n_disp = disp.vn(0.0, 0.12, -0.99)
    disp.quad(v_bl, v_br, v_tr, v_tl, uv_bl, uv_br, uv_tr, uv_tl, n_disp)
    disp.write(out_path)


def generate_serving_plate(out_path):
    plate = FastObj("SM_ReadyCounter_Plate")
    # Authentic modern cafe serving platter / ceramic-melamine tray:
    # Outer bounds: X in [-0.140, 0.140] (28cm), Z in [-0.090, 0.090] (18cm), Y in [0.000, 0.016] (16mm)
    # 1. Base pedestal ring (Y: 0.000 .. 0.0025, inset by 1.5cm)
    plate.add_box(-0.120, 0.120, 0.000, 0.0025, -0.075, 0.075, u0=0.1, u1=0.9, v0=0.1, v1=0.9)

    # 2. Main tray body & recessed floor (Y: 0.0025 .. 0.005)
    plate.add_box(-0.138, 0.138, 0.0025, 0.005, -0.088, 0.088, u0=0.1, u1=0.9, v0=0.1, v1=0.9)

    # 3. Flared curved perimeter walls (Y: 0.005 .. 0.015)
    # North wall (+Z)
    plate.add_box(-0.140, 0.140, 0.005, 0.015, 0.080, 0.090, u0=0.1, u1=0.9, v0=0.8, v1=0.95)
    # South wall (-Z)
    plate.add_box(-0.140, 0.140, 0.005, 0.015, -0.090, -0.080, u0=0.1, u1=0.9, v0=0.05, v1=0.2)
    # West wall (-X)
    plate.add_box(-0.140, -0.130, 0.005, 0.015, -0.085, 0.085, u0=0.05, u1=0.15, v0=0.1, v1=0.9)
    # East wall (+X)
    plate.add_box(0.130, 0.140, 0.005, 0.015, -0.085, 0.085, u0=0.85, u1=0.95, v0=0.1, v1=0.9)

    # 4. Smooth rolled top lip rim (Y: 0.014 .. 0.016)
    plate.add_box(-0.142, 0.142, 0.014, 0.016, 0.085, 0.092, u0=0.1, u1=0.9, v0=0.85, v1=0.95)
    plate.add_box(-0.142, 0.142, 0.014, 0.016, -0.092, -0.085, u0=0.1, u1=0.9, v0=0.05, v1=0.15)
    plate.add_box(-0.142, -0.135, 0.014, 0.016, -0.090, 0.090, u0=0.05, u1=0.12, v0=0.1, v1=0.9)
    plate.add_box(0.135, 0.142, 0.014, 0.016, -0.090, 0.090, u0=0.88, u1=0.95, v0=0.1, v1=0.9)

    # 5. Left cup coaster landing ring (X = -0.080, matching DrinkPlacement)
    plate.add_cylinder(-0.080, 0.000, 0.038, 0.004, 0.006, segs=16, u0=0.3, u1=0.5, v0=0.3, v1=0.5)

    # 6. Right cake serving area embossed liner (X = +0.080, matching CakePlacement)
    plate.add_box(0.025, 0.130, 0.004, 0.006, -0.065, 0.065, u0=0.2, u1=0.8, v0=0.2, v1=0.8)

    plate.write(out_path)


def generate_spatula(out_path):
    spat = FastObj("SM_Spatula_WoodHandle")
    # Unit cube [-0.5, 0.5]:
    # Hardwood handle: Z in [-0.50, -0.15]
    spat.add_cylinder(0, 0, 0.10, -0.48, -0.18, segs=12, u0=0.1, u1=0.4, v0=0.1, v1=0.5)
    # Handle end pommel
    spat.add_cylinder(0, 0, 0.12, -0.50, -0.46, segs=12, u0=0.1, u1=0.4, v0=0.1, v1=0.5)
    # Brass ferrule ring
    spat.add_cylinder(0, 0, 0.09, -0.18, -0.14, segs=12, u0=0.6, u1=0.9, v0=0.6, v1=0.9)
    # Offset steel neck
    spat.add_box(-0.04, 0.04, -0.03, 0.03, -0.14, 0.00, u0=0.6, u1=0.9, v0=0.1, v1=0.4)
    # Thin stainless scraper blade (Z in [0.00, 0.50], X in [-0.35, 0.35])
    spat.add_box(-0.35, 0.35, -0.02, 0.02, 0.00, 0.48, u0=0.55, u1=0.95, v0=0.05, v1=0.45)
    # Beveled chisel scraper edge at front
    spat.add_box(-0.35, 0.35, -0.01, 0.01, 0.48, 0.50, u0=0.55, u1=0.95, v0=0.05, v1=0.45)

    spat.normalize_to_unit_cube()
    spat.write(out_path)


def generate_scissors(out_path):
    scis = FastObj("SM_Scissors_RedGray")
    # Unit cube [-0.5, 0.5]:
    # Crossed steel blades (Z in [0.00, 0.50])
    scis.add_box(-0.10, 0.00, -0.03, 0.03, 0.00, 0.50, u0=0.6, u1=0.9, v0=0.1, v1=0.4)
    scis.add_box(0.00, 0.10, -0.03, 0.03, 0.00, 0.50, u0=0.6, u1=0.9, v0=0.1, v1=0.4)
    # Pivot rivet at Z = 0
    scis.add_cylinder(0, 0, 0.08, -0.05, 0.05, segs=10, u0=0.6, u1=0.9, v0=0.1, v1=0.4)

    # Ergonomic molded handles (Z in [-0.50, 0.00])
    # Left thumb loop (red rubber grip outside, grey plastic core inside)
    scis.add_cylinder(-0.25, -0.28, 0.18, -0.04, 0.04, segs=12, u0=0.1, u1=0.4, v0=0.6, v1=0.9)
    # Right finger loop
    scis.add_cylinder(0.25, -0.25, 0.22, -0.04, 0.04, segs=12, u0=0.1, u1=0.4, v0=0.6, v1=0.9)
    # Handle connecting shafts
    scis.add_box(-0.25, -0.05, -0.04, 0.04, -0.20, 0.00, u0=0.1, u1=0.4, v0=0.6, v1=0.9)
    scis.add_box(0.05, 0.25, -0.04, 0.04, -0.20, 0.00, u0=0.1, u1=0.4, v0=0.6, v1=0.9)

    scis.normalize_to_unit_cube()
    scis.write(out_path)


def generate_sauce_bag(out_path):
    sauce = FastObj("SM_SauceBag")
    # Unit cube [-0.5, 0.5]:
    # Cylindrical bottle body (Y in [-0.48, 0.20])
    sauce.add_cylinder(0, 0, 0.40, -0.48, 0.20, segs=16, u0=0.1, u1=0.5, v0=0.1, v1=0.8)
    # Bottle base pedestal (Y in [-0.50, -0.48])
    sauce.add_cylinder(0, 0, 0.38, -0.50, -0.48, segs=16, u0=0.1, u1=0.5, v0=0.1, v1=0.8)
    # Tapered bottle shoulder (Y in [0.20, 0.32])
    # Frustum from radius 0.40 down to 0.20
    segs = 16
    bot, top, norms = [], [], []
    for i in range(segs):
        ang = 2.0 * math.pi * i / segs
        bot.append(sauce.v(math.cos(ang)*0.40, 0.20, math.sin(ang)*0.40))
        top.append(sauce.v(math.cos(ang)*0.20, 0.32, math.sin(ang)*0.20))
        norms.append(sauce.vn(math.cos(ang)*0.5, 0.86, math.sin(ang)*0.5))
    for i in range(segs):
        nxt = (i + 1) % segs
        uv_b0 = sauce.vt(0.1 + 0.4*(i/segs), 0.7)
        uv_b1 = sauce.vt(0.1 + 0.4*((i+1)/segs), 0.7)
        uv_t1 = sauce.vt(0.1 + 0.4*((i+1)/segs), 0.85)
        uv_t0 = sauce.vt(0.1 + 0.4*(i/segs), 0.85)
        sauce.quad(bot[i], bot[nxt], top[nxt], top[i], uv_b0, uv_b1, uv_t1, uv_t0, norms[i])

    # Ribbed screw cap (Y in [0.32, 0.38])
    sauce.add_cylinder(0, 0, 0.22, 0.32, 0.38, segs=16, u0=0.1, u1=0.5, v0=0.85, v1=0.95)
    # Conical precision nozzle (Y in [0.38, 0.50], radius 0.12 down to 0.03)
    bot_n, top_n, norms_n = [], [], []
    for i in range(segs):
        ang = 2.0 * math.pi * i / segs
        bot_n.append(sauce.v(math.cos(ang)*0.12, 0.38, math.sin(ang)*0.12))
        top_n.append(sauce.v(math.cos(ang)*0.03, 0.50, math.sin(ang)*0.03))
        norms_n.append(sauce.vn(math.cos(ang)*0.8, 0.6, math.sin(ang)*0.8))
    for i in range(segs):
        nxt = (i + 1) % segs
        uv_b0 = sauce.vt(0.1 + 0.4*(i/segs), 0.9)
        uv_b1 = sauce.vt(0.1 + 0.4*((i+1)/segs), 0.9)
        uv_t1 = sauce.vt(0.1 + 0.4*((i+1)/segs), 1.0)
        uv_t0 = sauce.vt(0.1 + 0.4*(i/segs), 1.0)
        sauce.quad(bot_n[i], bot_n[nxt], top_n[nxt], top_n[i], uv_b0, uv_b1, uv_t1, uv_t0, norms_n[i])

    sauce.normalize_to_unit_cube()
    sauce.write(out_path)


def generate_wrapping_paper(out_path):
    paper = FastObj("SM_CakeWrappingPaper")
    # Stack of Kraft wrapping sheets (X: -0.080..0.080, Z: -0.065..0.065, Y: 0.000..0.0035)
    paper.add_box(-0.081, 0.081, 0.000, 0.002, -0.066, 0.066, u0=0.05, u1=0.95, v0=0.05, v1=0.95)
    paper.add_box(-0.080, 0.080, 0.002, 0.0035, -0.065, 0.065, u0=0.0, u1=1.0, v0=0.0, v1=1.0)
    paper.write(out_path)


def generate_roll_board(out_path):
    mat = FastObj("SM_RollBoard")
    # Black rubber bar mat (X: -0.100..0.100, Z: -0.070..0.070, Y: 0.000..0.006)
    mat.add_box(-0.100, 0.100, 0.000, 0.003, -0.070, 0.070, u0=0.04, u1=0.96, v0=0.04, v1=0.96)
    # Raised border rim
    mat.add_box(-0.100, 0.100, 0.003, 0.006, -0.070, -0.062, u0=0.0, u1=1.0, v0=0.0, v1=0.08)
    mat.add_box(-0.100, 0.100, 0.003, 0.006, 0.062, 0.070, u0=0.0, u1=1.0, v0=0.92, v1=1.0)
    mat.add_box(-0.100, -0.092, 0.003, 0.006, -0.062, 0.062, u0=0.0, u1=0.08, v0=0.08, v1=0.92)
    mat.add_box(0.092, 0.100, 0.003, 0.006, -0.062, 0.062, u0=0.92, u1=1.0, v0=0.08, v1=0.92)
    # Textured drainage studs
    for r in range(5):
        cz = -0.050 + r * 0.025
        for c in range(9):
            cx = -0.080 + c * 0.020
            mat.add_cylinder(cx, cz, 0.003, 0.003, 0.0055, segs=6, u0=0.1, u1=0.3, v0=0.1, v1=0.3)
    mat.write(out_path)


def generate_all():
    cake_dir = "Assets/TramChanh/Art/Models/CakeStation"
    props_dir = "Assets/TramChanh/Art/Models/Props"
    generate_grill_base(os.path.join(cake_dir, "SM_Grill_Elmich_Base.obj"))
    generate_grill_lid(os.path.join(cake_dir, "SM_Grill_Elmich_Lid.obj"))
    generate_grill_display(os.path.join(cake_dir, "SM_Grill_Display.obj"))
    generate_serving_plate(os.path.join(props_dir, "SM_ReadyCounter_Plate.obj"))
    generate_spatula(os.path.join(cake_dir, "SM_Spatula_WoodHandle.obj"))
    generate_scissors(os.path.join(cake_dir, "SM_Scissors_RedGray.obj"))
    generate_sauce_bag(os.path.join(cake_dir, "SM_SauceBag.obj"))
    generate_wrapping_paper(os.path.join(cake_dir, "SM_CakeWrappingPaper.obj"))
    generate_roll_board(os.path.join(cake_dir, "SM_RollBoard.obj"))

if __name__ == "__main__":
    generate_all()
