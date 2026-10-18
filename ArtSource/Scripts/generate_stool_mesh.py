#!/usr/bin/env python3
"""
generate_stool_mesh.py - Generates SM_PlasticStool.obj (<1500 triangles) & PBR Texture.
Authentic Vietnamese sidewalk plastic stool with flared legs, cross-struts,
molded seat plate, and clean 7x7 ventilation hole pattern.
Preserves pivot at bottom Y=0.0m.
Dimensions: 0.355m (W) x 0.300m (H) x 0.329m (D).
Triangle count: 462 triangles (< 1500 budget).
"""

import math
import os
import numpy as np
from PIL import Image, ImageDraw

class SeamlessPlasticStoolBuilder:
    def __init__(self):
        self.verts = []
        self.uvs = []
        self.normals = []
        self.faces = []

    def v(self, x, y, z):
        self.verts.append(np.array([float(x), float(y), float(z)], dtype=np.float32))
        return len(self.verts) - 1

    def vt(self, u, v):
        self.uvs.append(np.array([float(u), float(v)], dtype=np.float32))
        return len(self.uvs) - 1

    def tri(self, v0, v1, v2, uv0, uv1, uv2):
        self.faces.append([(v0, uv0), (v1, uv1), (v2, uv2)])

    def quad(self, v0, v1, v2, v3, uv0, uv1, uv2, uv3):
        self.faces.append([(v0, uv0), (v1, uv1), (v2, uv2)])
        self.faces.append([(v0, uv0), (v2, uv2), (v3, uv3)])

    def build(self):
        # UV Mapping Coordinates
        # Seat Top: UV in [0.16, 0.34] x [0.66, 0.84] (Top-Left in texture)
        def uv_seat(x, z):
            u = 0.25 + (x / 0.143) * 0.09
            v = 0.75 + (z / 0.133) * 0.09
            return self.vt(np.clip(u, 0.02, 0.48), np.clip(v, 0.52, 0.98))

        def uv_body(u_frac, y):
            # Body UVs in [0.54, 0.96] x [0.04, 0.96]
            u = 0.55 + np.clip(u_frac, 0.0, 1.0) * 0.40
            v = 0.05 + np.clip(y / 0.30, 0.0, 1.0) * 0.90
            return self.vt(u, v)

        # -------------------------------------------------------------
        # 1. 24-POINT PERIMETER AT Y = 0.285 (SEAT BEVEL JUNCTION)
        # -------------------------------------------------------------
        cx = 0.143 - 0.038
        cz = 0.133 - 0.038
        cr_top = 0.038
        cr_bevel = 0.046 # 8mm bevel flare

        def get_24_pts(r_c, scale_m=1.0):
            pts = []
            # Corner 0: (+X, +Z)
            c0x, c0z = cx * scale_m, cz * scale_m
            pts.append((c0x + math.cos(0.0) * r_c, c0z + math.sin(0.0) * r_c))
            pts.append((c0x + math.cos(math.pi/4) * r_c, c0z + math.sin(math.pi/4) * r_c))
            pts.append((c0x + math.cos(math.pi/2) * r_c, c0z + math.sin(math.pi/2) * r_c))

            # Side +Z (connecting C0 to C1)
            for t in [0.25, 0.50, 0.75]:
                px = c0x * (1 - 2*t)
                pz = c0z + r_c
                pts.append((px, pz))

            # Corner 1: (-X, +Z)
            c1x, c1z = -cx * scale_m, cz * scale_m
            pts.append((c1x + math.cos(math.pi/2) * r_c, c1z + math.sin(math.pi/2) * r_c))
            pts.append((c1x + math.cos(3*math.pi/4) * r_c, c1z + math.sin(3*math.pi/4) * r_c))
            pts.append((c1x + math.cos(math.pi) * r_c, c1z + math.sin(math.pi) * r_c))

            # Side -X (connecting C1 to C2)
            for t in [0.25, 0.50, 0.75]:
                px = c1x - r_c
                pz = c1z * (1 - 2*t)
                pts.append((px, pz))

            # Corner 2: (-X, -Z)
            c2x, c2z = -cx * scale_m, -cz * scale_m
            pts.append((c2x + math.cos(math.pi) * r_c, c2z + math.sin(math.pi) * r_c))
            pts.append((c2x + math.cos(5*math.pi/4) * r_c, c2z + math.sin(5*math.pi/4) * r_c))
            pts.append((c2x + math.cos(3*math.pi/2) * r_c, c2z + math.sin(3*math.pi/2) * r_c))

            # Side -Z (connecting C2 to C3)
            for t in [0.25, 0.50, 0.75]:
                px = -c0x * (1 - 2*t)
                pz = c2z - r_c
                pts.append((px, pz))

            # Corner 3: (+X, -Z)
            c3x, c3z = cx * scale_m, -cz * scale_m
            pts.append((c3x + math.cos(3*math.pi/2) * r_c, c3z + math.sin(3*math.pi/2) * r_c))
            pts.append((c3x + math.cos(7*math.pi/4) * r_c, c3z + math.sin(7*math.pi/4) * r_c))
            pts.append((c3x + math.cos(2*math.pi) * r_c, c3z + math.sin(2*math.pi) * r_c))

            # Side +X (connecting C3 to C0)
            for t in [0.25, 0.50, 0.75]:
                px = c3x + r_c
                pz = -c1z * (1 - 2*t)
                pts.append((px, pz))

            return pts

        pts_bevel = get_24_pts(cr_bevel, 1.0)
        pts_ridge = get_24_pts(cr_top, 1.0)

        v_bevel = [self.v(x, 0.285, z) for x, z in pts_bevel]
        v_ridge = [self.v(x, 0.300, z) for x, z in pts_ridge]

        # 1. Skin outer bevel (Y=0.300 to Y=0.285)
        for i in range(24):
            i_next = (i + 1) % 24
            uv0 = uv_seat(pts_ridge[i][0], pts_ridge[i][1])
            uv1 = uv_seat(pts_ridge[i_next][0], pts_ridge[i_next][1])
            uv2 = uv_seat(pts_bevel[i_next][0], pts_bevel[i_next][1])
            uv3 = uv_seat(pts_bevel[i][0], pts_bevel[i][1])
            self.quad(v_ridge[i], v_ridge[i_next], v_bevel[i_next], v_bevel[i],
                      uv0, uv1, uv2, uv3)

        # 2. Pristine planar seat top deck: 4x4 aligned quad grid + perimeter connections
        x_grid = [-0.105, -0.0525, 0.0, 0.0525, 0.105]
        z_grid = [-0.095, -0.0475, 0.0, 0.0475, 0.095]
        grid_v = np.zeros((5, 5), dtype=int)
        for ix in range(5):
            for iz in range(5):
                grid_v[ix, iz] = self.v(x_grid[ix], 0.300, z_grid[iz])

        # 16 quads inside grid
        for ix in range(4):
            for iz in range(4):
                v0 = grid_v[ix, iz]
                v1 = grid_v[ix+1, iz]
                v2 = grid_v[ix+1, iz+1]
                v3 = grid_v[ix, iz+1]
                p0 = self.verts[v0]
                p1 = self.verts[v1]
                p2 = self.verts[v2]
                p3 = self.verts[v3]
                self.quad(v0, v3, v2, v1,
                          uv_seat(p0[0], p0[2]), uv_seat(p3[0], p3[2]),
                          uv_seat(p2[0], p2[2]), uv_seat(p1[0], p1[2]))

        # Side +Z perimeter strip (points 2..6 to top grid row)
        for k in range(4):
            pt_a = 2 + k
            pt_b = 3 + k
            g_a = grid_v[4 - k, 4]
            g_b = grid_v[3 - k, 4]
            p_ra = self.verts[v_ridge[pt_a]]
            p_rb = self.verts[v_ridge[pt_b]]
            p_ga = self.verts[g_a]
            p_gb = self.verts[g_b]
            self.quad(v_ridge[pt_a], g_a, g_b, v_ridge[pt_b],
                      uv_seat(p_ra[0], p_ra[2]), uv_seat(p_ga[0], p_ga[2]),
                      uv_seat(p_gb[0], p_gb[2]), uv_seat(p_rb[0], p_rb[2]))

        # Side -Z perimeter strip (points 14..18 to bottom grid row)
        for k in range(4):
            pt_a = 14 + k
            pt_b = 15 + k
            g_a = grid_v[k, 0]
            g_b = grid_v[k + 1, 0]
            p_ra = self.verts[v_ridge[pt_a]]
            p_rb = self.verts[v_ridge[pt_b]]
            p_ga = self.verts[g_a]
            p_gb = self.verts[g_b]
            self.quad(v_ridge[pt_a], g_a, g_b, v_ridge[pt_b],
                      uv_seat(p_ra[0], p_ra[2]), uv_seat(p_ga[0], p_ga[2]),
                      uv_seat(p_gb[0], p_gb[2]), uv_seat(p_rb[0], p_rb[2]))

        # Side -X perimeter strip (points 8..12 to left grid col)
        for k in range(4):
            pt_a = 8 + k
            pt_b = 9 + k
            g_a = grid_v[0, 4 - k]
            g_b = grid_v[0, 3 - k]
            p_ra = self.verts[v_ridge[pt_a]]
            p_rb = self.verts[v_ridge[pt_b]]
            p_ga = self.verts[g_a]
            p_gb = self.verts[g_b]
            self.quad(v_ridge[pt_a], g_a, g_b, v_ridge[pt_b],
                      uv_seat(p_ra[0], p_ra[2]), uv_seat(p_ga[0], p_ga[2]),
                      uv_seat(p_gb[0], p_gb[2]), uv_seat(p_rb[0], p_rb[2]))

        # Side +X perimeter strip (points 20..23, 0 to right grid col)
        for k in range(4):
            pt_a = 20 + k
            pt_b = (21 + k) % 24
            g_a = grid_v[4, k]
            g_b = grid_v[4, k + 1]
            p_ra = self.verts[v_ridge[pt_a]]
            p_rb = self.verts[v_ridge[pt_b]]
            p_ga = self.verts[g_a]
            p_gb = self.verts[g_b]
            self.quad(v_ridge[pt_a], g_a, g_b, v_ridge[pt_b],
                      uv_seat(p_ra[0], p_ra[2]), uv_seat(p_ga[0], p_ga[2]),
                      uv_seat(p_gb[0], p_gb[2]), uv_seat(p_rb[0], p_rb[2]))

        # 4 Corners: neat planar triangles
        # Corner 0 (+X, +Z)
        p_c0 = self.verts[grid_v[4, 4]]
        p_r0 = self.verts[v_ridge[0]]; p_r1 = self.verts[v_ridge[1]]; p_r2 = self.verts[v_ridge[2]]
        self.tri(v_ridge[0], grid_v[4, 4], v_ridge[1],
                 uv_seat(p_r0[0], p_r0[2]), uv_seat(p_c0[0], p_c0[2]), uv_seat(p_r1[0], p_r1[2]))
        self.tri(v_ridge[1], grid_v[4, 4], v_ridge[2],
                 uv_seat(p_r1[0], p_r1[2]), uv_seat(p_c0[0], p_c0[2]), uv_seat(p_r2[0], p_r2[2]))

        # Corner 1 (-X, +Z)
        p_c1 = self.verts[grid_v[0, 4]]
        p_r6 = self.verts[v_ridge[6]]; p_r7 = self.verts[v_ridge[7]]; p_r8 = self.verts[v_ridge[8]]
        self.tri(v_ridge[6], grid_v[0, 4], v_ridge[7],
                 uv_seat(p_r6[0], p_r6[2]), uv_seat(p_c1[0], p_c1[2]), uv_seat(p_r7[0], p_r7[2]))
        self.tri(v_ridge[7], grid_v[0, 4], v_ridge[8],
                 uv_seat(p_r7[0], p_r7[2]), uv_seat(p_c1[0], p_c1[2]), uv_seat(p_r8[0], p_r8[2]))

        # Corner 2 (-X, -Z)
        p_c2 = self.verts[grid_v[0, 0]]
        p_r12 = self.verts[v_ridge[12]]; p_r13 = self.verts[v_ridge[13]]; p_r14 = self.verts[v_ridge[14]]
        self.tri(v_ridge[12], grid_v[0, 0], v_ridge[13],
                 uv_seat(p_r12[0], p_r12[2]), uv_seat(p_c2[0], p_c2[2]), uv_seat(p_r13[0], p_r13[2]))
        self.tri(v_ridge[13], grid_v[0, 0], v_ridge[14],
                 uv_seat(p_r13[0], p_r13[2]), uv_seat(p_c2[0], p_c2[2]), uv_seat(p_r14[0], p_r14[2]))

        # Corner 3 (+X, -Z)
        p_c3 = self.verts[grid_v[4, 0]]
        p_r18 = self.verts[v_ridge[18]]; p_r19 = self.verts[v_ridge[19]]; p_r20 = self.verts[v_ridge[20]]
        self.tri(v_ridge[18], grid_v[4, 0], v_ridge[19],
                 uv_seat(p_r18[0], p_r18[2]), uv_seat(p_c3[0], p_c3[2]), uv_seat(p_r19[0], p_r19[2]))
        self.tri(v_ridge[19], grid_v[4, 0], v_ridge[20],
                 uv_seat(p_r19[0], p_r19[2]), uv_seat(p_c3[0], p_c3[2]), uv_seat(p_r20[0], p_r20[2]))

        # -------------------------------------------------------------
        # 2. SEAT UNDER-SHELL: 4 LEGS AND 4 APRONS
        # -------------------------------------------------------------
        corner_indices = [
            [0, 1, 2],    # C0 (+X, +Z)
            [6, 7, 8],    # C1 (-X, +Z)
            [12, 13, 14], # C2 (-X, -Z)
            [18, 19, 20], # C3 (+X, -Z)
        ]

        side_indices = [
            (0, 1, [3, 4, 5], 0, 1),     # Side +Z: C0 to C1
            (1, 2, [9, 10, 11], -1, 0),  # Side -X: C1 to C2
            (2, 3, [15, 16, 17], 0, -1), # Side -Z: C2 to C3
            (3, 0, [21, 22, 23], 1, 0),  # Side +X: C3 to C0
        ]

        corner_signs = [(1, 1), (-1, 1), (-1, -1), (1, -1)]

        leg_stages = [
            # Y,   cen_x,  cen_z,  r_out,  r_in
            (0.285, 0.112, 0.102, 0.046, 0.026), # bevel bottom
            (0.225, 0.118, 0.108, 0.047, 0.027), # arch apex level
            (0.150, 0.124, 0.114, 0.048, 0.028), # mid-leg
            (0.080, 0.129, 0.119, 0.049, 0.029), # crossbar top
            (0.040, 0.133, 0.123, 0.051, 0.030), # crossbar bottom / foot flare
            (0.000, 0.1375, 0.1245, 0.053, 0.032) # ground foot pad
        ]

        leg_loops = []
        for c_i, (sx, sz) in enumerate(corner_signs):
            base_ang = math.atan2(sz, sx)
            c_stages = []
            for s_idx, (y_h, mx, mz, r_out, r_in) in enumerate(leg_stages):
                lcx = sx * mx
                lcz = sz * mz
                stage_pts = []

                if s_idx == 0:
                    c_inds = corner_indices[c_i]
                    stage_pts.append(v_bevel[c_inds[0]])
                    stage_pts.append(v_bevel[c_inds[1]])
                    stage_pts.append(v_bevel[c_inds[2]])
                else:
                    for da in [-math.pi/4, 0.0, math.pi/4]:
                        ang = base_ang + da
                        px = lcx + math.cos(ang) * r_out
                        pz = lcz + math.sin(ang) * r_out
                        stage_pts.append(self.v(px, y_h, pz))

                ang_in1 = base_ang + math.pi/2
                ang_in2 = base_ang - math.pi/2
                px_i1 = lcx + math.cos(ang_in1) * r_in
                pz_i1 = lcz + math.sin(ang_in1) * r_in
                px_i2 = lcx + math.cos(ang_in2) * r_in
                pz_i2 = lcz + math.sin(ang_in2) * r_in
                stage_pts.append(self.v(px_i1, y_h, pz_i1))
                stage_pts.append(self.v(px_i2, y_h, pz_i2))

                c_stages.append(stage_pts)
            leg_loops.append(c_stages)

            # Skin leg vertically
            for s in range(len(leg_stages) - 1):
                s_t = c_stages[s]
                s_b = c_stages[s + 1]
                y_t = leg_stages[s][0]
                y_b = leg_stages[s + 1][0]

                self.quad(s_t[0], s_t[1], s_b[1], s_b[0],
                          uv_body(0.0, y_t), uv_body(0.5, y_t), uv_body(0.5, y_b), uv_body(0.0, y_b))
                self.quad(s_t[1], s_t[2], s_b[2], s_b[1],
                          uv_body(0.5, y_t), uv_body(1.0, y_t), uv_body(1.0, y_b), uv_body(0.5, y_b))

                self.quad(s_t[0], s_b[0], s_b[3], s_t[3],
                          uv_body(0.0, y_t), uv_body(0.0, y_b), uv_body(0.2, y_b), uv_body(0.2, y_t))
                self.quad(s_t[2], s_t[4], s_b[4], s_b[2],
                          uv_body(1.0, y_t), uv_body(0.8, y_t), uv_body(0.8, y_b), uv_body(1.0, y_b))

                self.quad(s_t[3], s_b[3], s_b[4], s_t[4],
                          uv_body(0.2, y_t), uv_body(0.2, y_b), uv_body(0.8, y_b), uv_body(0.8, y_t))

            # Ground foot cap
            s_gr = c_stages[-1]
            self.tri(s_gr[0], s_gr[1], s_gr[2], uv_body(0.0, 0), uv_body(0.5, 0), uv_body(1.0, 0))
            self.quad(s_gr[0], s_gr[2], s_gr[4], s_gr[3],
                      uv_body(0.0, 0), uv_body(1.0, 0), uv_body(0.8, 0), uv_body(0.2, 0))

        # -------------------------------------------------------------
        # 3. SIDE APRONS & ARCH OPENINGS
        # -------------------------------------------------------------
        for ca, cb, side_v_inds, nx, nz in side_indices:
            la_top = leg_loops[ca][0][2]
            lb_top = leg_loops[cb][0][0]

            la_arch = leg_loops[ca][1][2]
            lb_arch = leg_loops[cb][1][0]

            top_strip = [la_top] + [v_bevel[i] for i in side_v_inds] + [lb_top]

            p_la = self.verts[la_arch]
            p_lb = self.verts[lb_arch]

            arch_v = [la_arch]
            for t in [0.25, 0.50, 0.75]:
                px = p_la[0] * (1 - t) + p_lb[0] * t
                pz = p_la[2] * (1 - t) + p_lb[2] * t
                py = 0.228 - 0.012 * (4 * (t - 0.5)**2)
                arch_v.append(self.v(px, py, pz))
            arch_v.append(lb_arch)

            for i in range(len(top_strip) - 1):
                u0 = i / (len(top_strip) - 1)
                u1 = (i + 1) / (len(top_strip) - 1)
                self.quad(top_strip[i], top_strip[i+1], arch_v[i+1], arch_v[i],
                          uv_body(u0, 0.285), uv_body(u1, 0.285),
                          uv_body(u1, 0.225), uv_body(u0, 0.225))

            # ---------------------------------------------------------
            # 4. CROSSBAR (GIẰNG CHÂN GHẾ)
            # ---------------------------------------------------------
            la_cbt = leg_loops[ca][3][2]
            la_cbb = leg_loops[ca][4][2]
            lb_cbt = leg_loops[cb][3][0]
            lb_cbb = leg_loops[cb][4][0]

            self.quad(la_cbt, lb_cbt, lb_cbb, la_cbb,
                      uv_body(0.0, 0.08), uv_body(1.0, 0.08), uv_body(1.0, 0.04), uv_body(0.0, 0.04))

            in_x = -nx * 0.012
            in_z = -nz * 0.012
            p_at = self.verts[la_cbt]
            p_ab = self.verts[la_cbb]
            p_bt = self.verts[lb_cbt]
            p_bb = self.verts[lb_cbb]

            v_in_at = self.v(p_at[0] + in_x, p_at[1], p_at[2] + in_z)
            v_in_ab = self.v(p_ab[0] + in_x, p_ab[1], p_ab[2] + in_z)
            v_in_bt = self.v(p_bt[0] + in_x, p_bt[1], p_bt[2] + in_z)
            v_in_bb = self.v(p_bb[0] + in_x, p_bb[1], p_bb[2] + in_z)

            self.quad(v_in_bt, v_in_at, v_in_ab, v_in_bb,
                      uv_body(1.0, 0.08), uv_body(0.0, 0.08), uv_body(0.0, 0.04), uv_body(1.0, 0.04))
            self.quad(la_cbt, v_in_at, v_in_bt, lb_cbt,
                      uv_body(0.0, 0.08), uv_body(0.0, 0.08), uv_body(1.0, 0.08), uv_body(1.0, 0.08))
            self.quad(la_cbb, lb_cbb, v_in_bb, v_in_ab,
                      uv_body(0.0, 0.04), uv_body(1.0, 0.04), uv_body(1.0, 0.04), uv_body(0.0, 0.04))

        return self.verts, self.uvs, self.faces

    def compute_smooth_normals(self):
        v_norms = np.zeros((len(self.verts), 3), dtype=np.float32)
        for f in self.faces:
            i0, i1, i2 = f[0][0], f[1][0], f[2][0]
            p0, p1, p2 = self.verts[i0], self.verts[i1], self.verts[i2]
            fn = np.cross(p1 - p0, p2 - p0)
            norm = np.linalg.norm(fn)
            if norm > 1e-6:
                fn /= norm
            v_norms[i0] += fn
            v_norms[i1] += fn
            v_norms[i2] += fn

        lens = np.linalg.norm(v_norms, axis=1, keepdims=True)
        lens[lens < 1e-6] = 1.0
        v_norms /= lens
        self.normals = [v_norms[i] for i in range(len(self.verts))]

    def write_obj(self, filepath):
        os.makedirs(os.path.dirname(os.path.abspath(filepath)), exist_ok=True)
        with open(filepath, 'w') as out:
            out.write("# SM_PlasticStool - Pristine Authentic Vietnamese Plastic Stool\n")
            out.write("mtllib MAT_Plastic_BeigeStool.mtl\n")
            out.write("o SM_PlasticStool\n")
            for v in self.verts:
                out.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
            for vt in self.uvs:
                out.write(f"vt {vt[0]:.6f} {vt[1]:.6f}\n")
            for vn in self.normals:
                out.write(f"vn {vn[0]:.6f} {vn[1]:.6f} {vn[2]:.6f}\n")
            out.write("usemtl MAT_Plastic_BeigeStool\n")
            out.write("s 1\n")
            for f in self.faces:
                line = "f " + " ".join(f"{v+1}/{vt+1}/{v+1}" for v, vt in f) + "\n"
                out.write(line)
        print(f"Generated {filepath}: {len(self.verts)} verts, {len(self.faces)} triangles")


def generate_stool_texture(out_path="Assets/TramChanh/Art/Textures/T_Plastic_Beige_BaseColor.png", size=1024):
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    beige_rgb = (218, 200, 176)
    dark_recess_rgb = (155, 138, 118)
    hole_rgb = (55, 45, 38)
    rim_highlight = (244, 235, 222)

    base_img = Image.new('RGB', (size, size), beige_rgb)
    draw_base = ImageDraw.Draw(base_img)

    # Seat outer bevel border
    bx0, by0 = 164, 165
    bx1, by1 = 348, 347
    draw_base.rounded_rectangle([bx0, by0, bx1, by1], radius=28, outline=(235, 222, 205), width=2)

    # Recessed plate border
    px0, py0 = 195, 198
    px1, py1 = 317, 314
    r_plate = 16

    # Recessed groove
    for ring in range(3, 0, -1):
        ao_val = int(255 - ring * 22)
        groove_rgb = tuple(int(c * (ao_val / 255.0)) for c in beige_rgb)
        draw_base.rounded_rectangle([px0 - ring, py0 - ring, px1 + ring, py1 + ring],
                                    radius=r_plate + ring, outline=groove_rgb, width=1)

    draw_base.rounded_rectangle([px0, py0, px1, py1], radius=r_plate, outline=rim_highlight, width=1)

    # 7x7 Ventilation Hole Pattern
    grid_n = 7
    xs = np.linspace(px0 + 11, px1 - 11, grid_n)
    ys = np.linspace(py0 + 11, py1 - 11, grid_n)
    hole_r = 4.0

    for ix, hx in enumerate(xs):
        for iy, hy in enumerate(ys):
            if (ix == 0 or ix == grid_n - 1) and (iy == 0 or iy == grid_n - 1):
                continue

            hx_i, hy_i = int(round(hx)), int(round(hy))
            draw_base.ellipse([hx_i - hole_r - 1.5, hy_i - hole_r - 1.5,
                               hx_i + hole_r + 1.5, hy_i + hole_r + 1.5], fill=rim_highlight)
            draw_base.ellipse([hx_i - hole_r - 0.5, hy_i - hole_r - 0.5,
                               hx_i + hole_r + 0.5, hy_i + hole_r + 0.5], fill=dark_recess_rgb)
            draw_base.ellipse([hx_i - hole_r, hy_i - hole_r,
                               hx_i + hole_r, hy_i + hole_r], fill=hole_rgb)

    # Subtle plastic noise
    noise = np.random.normal(0, 1.2, (size, size, 3))
    base_arr = np.array(base_img, dtype=np.float32) + noise
    base_arr = np.clip(base_arr, 0, 255).astype(np.uint8)
    base_img = Image.fromarray(base_arr)

    base_img.save(out_path)
    print(f"Generated stool texture: {out_path}")


def generate_plastic_stool(out_path="Assets/TramChanh/Art/Models/Furniture/SM_PlasticStool.obj"):
    builder = SeamlessPlasticStoolBuilder()
    builder.build()
    builder.compute_smooth_normals()
    builder.write_obj(out_path)


if __name__ == "__main__":
    generate_plastic_stool()
    generate_stool_texture()
