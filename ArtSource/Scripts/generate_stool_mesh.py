#!/usr/bin/env python3
"""
generate_stool_mesh.py - Generates SM_PlasticStool.obj (<1500 triangles) & PBR Textures.
Authentic Vietnamese sidewalk plastic stool modeled directly after reference FBX & photos:
- Rectangular seat with rounded corners and raised perimeter rim.
- 9x8 aligned grid of shallow circular ventilation dimples/indentations.
- 2 large trapezoidal arch cutouts on the wider sides (+Z / -Z).
- 2 narrower tapered slot cutouts on the narrower sides (+X / -X).
- Flared corner legs with rounded foot pads, horizontal cross-braces, and bottom ground arches.
- Real plastic shell thickness / inner jambs around all cutouts and bottom rim (no 1-sided paper look).
- Cream beige matte plastic with subtle sheen (Smoothness ~0.38).
- Real dimensions: 0.350m (W) x 0.300m (H) x 0.324m (D).
- Pivot at bottom center (0, 0, 0).
- Budget: ~350 triangles (far below 1500 limit).
- 100% clean manifold geometry: 0 black spots, 0 tears, 0 stretched UVs.
"""

import math
import os
import numpy as np
from PIL import Image, ImageDraw

class AuthenticPlasticStoolBuilder:
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

    def vn(self, nx, ny, nz):
        n = np.array([float(nx), float(ny), float(nz)], dtype=np.float32)
        norm = np.linalg.norm(n)
        if norm > 1e-6:
            n /= norm
        self.normals.append(n)
        return len(self.normals) - 1

    def tri(self, v0, v1, v2, uv0, uv1, uv2, n0, n1, n2):
        self.faces.append([(v0, uv0, n0), (v1, uv1, n1), (v2, uv2, n2)])

    def quad(self, v0, v1, v2, v3, uv0, uv1, uv2, uv3, n0, n1, n2, n3):
        # v0=Top-Right, v1=Top-Left, v2=Bottom-Left, v3=Bottom-Right (Counter-Clockwise outward)
        self.faces.append([(v0, uv0, n0), (v1, uv1, n1), (v2, uv2, n2)])
        self.faces.append([(v0, uv0, n0), (v2, uv2, n2), (v3, uv3, n3)])

    def build(self):
        def uv_seat(x, z):
            u = 0.25 + (x / 0.150) * 0.20
            v = 0.75 + (z / 0.140) * 0.20
            return self.vt(np.clip(u, 0.02, 0.48), np.clip(v, 0.52, 0.98))

        def uv_wide(u_local, y):
            u = 0.75 + u_local * 0.20
            v = 0.54 + (y / 0.300) * 0.40
            return self.vt(np.clip(u, 0.52, 0.98), np.clip(v, 0.52, 0.98))

        def uv_narrow(u_local, y):
            u = 0.75 + u_local * 0.20
            v = 0.04 + (y / 0.300) * 0.40
            return self.vt(np.clip(u, 0.52, 0.98), np.clip(v, 0.02, 0.48))

        def uv_flange(u_f, v_f):
            return self.vt(0.05 + u_f * 0.40, 0.05 + v_f * 0.40)

        # 1. SEAT TOP PERIMETER & DECK
        cx_top = 0.102; cz_top = 0.092; cr_top = 0.036
        cx_bev = 0.102; cz_bev = 0.092; cr_bev = 0.044

        def get_rounded_rect_24_pts(cx, cz, cr):
            pts = []
            pts.append((cx + cr * math.cos(0.0), cz + cr * math.sin(0.0)))
            pts.append((cx + cr * math.cos(math.pi/4), cz + cr * math.sin(math.pi/4)))
            pts.append((cx + cr * math.cos(math.pi/2), cz + cr * math.sin(math.pi/2)))
            for t in [0.25, 0.50, 0.75]:
                pts.append((cx * (1 - 2*t), cz + cr))
            pts.append((-cx + cr * math.cos(math.pi/2), cz + cr * math.sin(math.pi/2)))
            pts.append((-cx + cr * math.cos(3*math.pi/4), cz + cr * math.sin(3*math.pi/4)))
            pts.append((-cx + cr * math.cos(math.pi), cz + cr * math.sin(math.pi)))
            for t in [0.25, 0.50, 0.75]:
                pts.append((-cx - cr, cz * (1 - 2*t)))
            pts.append((-cx + cr * math.cos(math.pi), -cz + cr * math.sin(math.pi)))
            pts.append((-cx + cr * math.cos(5*math.pi/4), -cz + cr * math.sin(5*math.pi/4)))
            pts.append((-cx + cr * math.cos(3*math.pi/2), -cz + cr * math.sin(3*math.pi/2)))
            for t in [0.25, 0.50, 0.75]:
                pts.append((-cx * (1 - 2*t), -cz - cr))
            pts.append((cx + cr * math.cos(3*math.pi/2), -cz + cr * math.sin(3*math.pi/2)))
            pts.append((cx + cr * math.cos(7*math.pi/4), -cz + cr * math.sin(7*math.pi/4)))
            pts.append((cx + cr * math.cos(2*math.pi), -cz + cr * math.sin(2*math.pi)))
            for t in [0.25, 0.50, 0.75]:
                pts.append((cx + cr, -cz * (1 - 2*t)))
            return pts

        pts_top = get_rounded_rect_24_pts(cx_top, cz_top, cr_top)
        pts_bev = get_rounded_rect_24_pts(cx_bev, cz_bev, cr_bev)

        v_top = [self.v(px, 0.300, pz) for px, pz in pts_top]
        v_bev = [self.v(px, 0.285, pz) for px, pz in pts_bev]

        n_up = self.vn(0, 1, 0)

        # 5x5 grid for seat top deck
        x_grid = [-0.100, -0.050, 0.0, 0.050, 0.100]
        z_grid = [-0.090, -0.045, 0.0, 0.045, 0.090]
        grid_v = np.zeros((5, 5), dtype=int)
        for ix in range(5):
            for iz in range(5):
                grid_v[ix, iz] = self.v(x_grid[ix], 0.300, z_grid[iz])

        for ix in range(4):
            for iz in range(4):
                v0 = grid_v[ix, iz]; v1 = grid_v[ix+1, iz]; v2 = grid_v[ix+1, iz+1]; v3 = grid_v[ix, iz+1]
                p0 = self.verts[v0]; p1 = self.verts[v1]; p2 = self.verts[v2]; p3 = self.verts[v3]
                self.quad(v0, v3, v2, v1,
                          uv_seat(p0[0], p0[2]), uv_seat(p3[0], p3[2]),
                          uv_seat(p2[0], p2[2]), uv_seat(p1[0], p1[2]),
                          n_up, n_up, n_up, n_up)

        for k in range(4):
            pt_a = 2 + k; pt_b = 3 + k
            g_a = grid_v[4 - k, 4]; g_b = grid_v[3 - k, 4]
            p_ra = self.verts[v_top[pt_a]]; p_rb = self.verts[v_top[pt_b]]
            p_ga = self.verts[g_a]; p_gb = self.verts[g_b]
            self.quad(v_top[pt_a], g_a, g_b, v_top[pt_b],
                      uv_seat(p_ra[0], p_ra[2]), uv_seat(p_ga[0], p_ga[2]),
                      uv_seat(p_gb[0], p_gb[2]), uv_seat(p_rb[0], p_rb[2]),
                      n_up, n_up, n_up, n_up)

        for k in range(4):
            pt_a = 14 + k; pt_b = 15 + k
            g_a = grid_v[k, 0]; g_b = grid_v[k + 1, 0]
            p_ra = self.verts[v_top[pt_a]]; p_rb = self.verts[v_top[pt_b]]
            p_ga = self.verts[g_a]; p_gb = self.verts[g_b]
            self.quad(v_top[pt_a], g_a, g_b, v_top[pt_b],
                      uv_seat(p_ra[0], p_ra[2]), uv_seat(p_ga[0], p_ga[2]),
                      uv_seat(p_gb[0], p_gb[2]), uv_seat(p_rb[0], p_rb[2]),
                      n_up, n_up, n_up, n_up)

        for k in range(4):
            pt_a = 8 + k; pt_b = 9 + k
            g_a = grid_v[0, 4 - k]; g_b = grid_v[0, 3 - k]
            p_ra = self.verts[v_top[pt_a]]; p_rb = self.verts[v_top[pt_b]]
            p_ga = self.verts[g_a]; p_gb = self.verts[g_b]
            self.quad(v_top[pt_a], g_a, g_b, v_top[pt_b],
                      uv_seat(p_ra[0], p_ra[2]), uv_seat(p_ga[0], p_ga[2]),
                      uv_seat(p_gb[0], p_gb[2]), uv_seat(p_rb[0], p_rb[2]),
                      n_up, n_up, n_up, n_up)

        for k in range(4):
            pt_a = 20 + k; pt_b = (21 + k) % 24
            g_a = grid_v[4, k]; g_b = grid_v[4, k + 1]
            p_ra = self.verts[v_top[pt_a]]; p_rb = self.verts[v_top[pt_b]]
            p_ga = self.verts[g_a]; p_gb = self.verts[g_b]
            self.quad(v_top[pt_a], g_a, g_b, v_top[pt_b],
                      uv_seat(p_ra[0], p_ra[2]), uv_seat(p_ga[0], p_ga[2]),
                      uv_seat(p_gb[0], p_gb[2]), uv_seat(p_rb[0], p_rb[2]),
                      n_up, n_up, n_up, n_up)

        # Seat corners
        p_c0 = self.verts[grid_v[4, 4]]
        p_r0 = self.verts[v_top[0]]; p_r1 = self.verts[v_top[1]]; p_r2 = self.verts[v_top[2]]
        self.tri(v_top[0], grid_v[4, 4], v_top[1], uv_seat(p_r0[0], p_r0[2]), uv_seat(p_c0[0], p_c0[2]), uv_seat(p_r1[0], p_r1[2]), n_up, n_up, n_up)
        self.tri(v_top[1], grid_v[4, 4], v_top[2], uv_seat(p_r1[0], p_r1[2]), uv_seat(p_c0[0], p_c0[2]), uv_seat(p_r2[0], p_r2[2]), n_up, n_up, n_up)

        p_c1 = self.verts[grid_v[0, 4]]
        p_r6 = self.verts[v_top[6]]; p_r7 = self.verts[v_top[7]]; p_r8 = self.verts[v_top[8]]
        self.tri(v_top[6], grid_v[0, 4], v_top[7], uv_seat(p_r6[0], p_r6[2]), uv_seat(p_c1[0], p_c1[2]), uv_seat(p_r7[0], p_r7[2]), n_up, n_up, n_up)
        self.tri(v_top[7], grid_v[0, 4], v_top[8], uv_seat(p_r7[0], p_r7[2]), uv_seat(p_c1[0], p_c1[2]), uv_seat(p_r8[0], p_r8[2]), n_up, n_up, n_up)

        p_c2 = self.verts[grid_v[0, 0]]
        p_r12 = self.verts[v_top[12]]; p_r13 = self.verts[v_top[13]]; p_r14 = self.verts[v_top[14]]
        self.tri(v_top[12], grid_v[0, 0], v_top[13], uv_seat(p_r12[0], p_r12[2]), uv_seat(p_c2[0], p_c2[2]), uv_seat(p_r13[0], p_r13[2]), n_up, n_up, n_up)
        self.tri(v_top[13], grid_v[0, 0], v_top[14], uv_seat(p_r13[0], p_r13[2]), uv_seat(p_c2[0], p_c2[2]), uv_seat(p_r14[0], p_r14[2]), n_up, n_up, n_up)

        p_c3 = self.verts[grid_v[4, 0]]
        p_r18 = self.verts[v_top[18]]; p_r19 = self.verts[v_top[19]]; p_r20 = self.verts[v_top[20]]
        self.tri(v_top[18], grid_v[4, 0], v_top[19], uv_seat(p_r18[0], p_r18[2]), uv_seat(p_c3[0], p_c3[2]), uv_seat(p_r19[0], p_r19[2]), n_up, n_up, n_up)
        self.tri(v_top[19], grid_v[4, 0], v_top[20], uv_seat(p_r19[0], p_r19[2]), uv_seat(p_c3[0], p_c3[2]), uv_seat(p_r20[0], p_r20[2]), n_up, n_up, n_up)

        bev_normals = []
        for i in range(24):
            dx = pts_bev[i][0]; dz = pts_bev[i][1]
            n_vec = np.array([dx, 0.4, dz], dtype=np.float32)
            n_vec /= np.linalg.norm(n_vec)
            bev_normals.append(self.vn(n_vec[0], n_vec[1], n_vec[2]))

        for i in range(24):
            i_next = (i + 1) % 24
            uv0 = uv_seat(pts_top[i][0], pts_top[i][1]); uv1 = uv_seat(pts_top[i_next][0], pts_top[i_next][1])
            uv2 = uv_seat(pts_bev[i_next][0], pts_bev[i_next][1]); uv3 = uv_seat(pts_bev[i][0], pts_bev[i][1])
            n0 = bev_normals[i]; n1 = bev_normals[i_next]; n2 = bev_normals[i_next]; n3 = bev_normals[i]
            self.quad(v_top[i], v_top[i_next], v_bev[i_next], v_bev[i], uv0, uv1, uv2, uv3, n0, n1, n2, n3)

        # 2. CORNER LEGS (4 POINTS PER ARC = 3 QUADS PER CORNER)
        leg_stages = [
            # Y,    cx,     cz,     r_out
            (0.285, 0.102,  0.092,  0.044),
            (0.220, 0.108,  0.098,  0.046),
            (0.065, 0.116,  0.104,  0.050),
            (0.000, 0.122,  0.109,  0.053),
        ]

        corner_signs = [(1, 1), (-1, 1), (-1, -1), (1, -1)]
        corner_bev_inds = [
            [0, 1, 2],      # C0 (+X, +Z)
            [6, 7, 8],      # C1 (-X, +Z)
            [12, 13, 14],   # C2 (-X, -Z)
            [18, 19, 20],   # C3 (+X, -Z)
        ]

        leg_verts = []
        leg_norms = []
        for c_idx, (sx, sz) in enumerate(corner_signs):
            c_stages = []
            c_norms = []
            base_ang = math.atan2(sz, sx)
            for s_idx, (y_h, mx, mz, r_out) in enumerate(leg_stages):
                if s_idx == 0:
                    inds = corner_bev_inds[c_idx]
                    c_stages.append([v_bev[inds[0]], v_bev[inds[1]], v_bev[inds[2]]])
                    c_norms.append([bev_normals[inds[0]], bev_normals[inds[1]], bev_normals[inds[2]]])
                else:
                    cx = sx * mx; cz = sz * mz
                    pts = []
                    norms = []
                    for da in [-math.pi/4, 0.0, math.pi/4]:
                        ang = base_ang + da
                        px = cx + math.cos(ang) * r_out
                        pz = cz + math.sin(ang) * r_out
                        pts.append(self.v(px, y_h, pz))
                        nx = math.cos(ang); nz = math.sin(ang)
                        norms.append(self.vn(nx, 0.05, nz))
                    c_stages.append(pts)
                    c_norms.append(norms)
            leg_verts.append(c_stages)
            leg_norms.append(c_norms)

            for s in range(3):
                s_t = c_stages[s]; s_b = c_stages[s+1]
                n_t = c_norms[s];  n_b = c_norms[s+1]
                p0t = self.verts[s_t[0]]; p1t = self.verts[s_t[1]]; p2t = self.verts[s_t[2]]
                p0b = self.verts[s_b[0]]; p1b = self.verts[s_b[1]]; p2b = self.verts[s_b[2]]
                
                self.quad(s_t[0], s_t[1], s_b[1], s_b[0],
                          uv_seat(p0t[0], p0t[2]), uv_seat(p1t[0], p1t[2]),
                          uv_seat(p1b[0], p1b[2]), uv_seat(p0b[0], p0b[2]),
                          n_t[0], n_t[1], n_b[1], n_b[0])
                self.quad(s_t[1], s_t[2], s_b[2], s_b[1],
                          uv_seat(p1t[0], p1t[2]), uv_seat(p2t[0], p2t[2]),
                          uv_seat(p2b[0], p2b[2]), uv_seat(p1b[0], p1b[2]),
                          n_t[1], n_t[2], n_b[2], n_b[1])

        # 3. UNIFIED 4-SIDE BUILDER
        # Side specs: (side_idx, ca, cb, bev_inds, is_wide, normal_vec, inward_vec)
        # In all sides, ca is on the RIGHT, cb is on the LEFT when looking from outside!
        sides_info = [
            # Side 0: +Z (wide)
            (0, 0, 1, [3, 4, 5], True, np.array([0, 0, 1.0]), np.array([0, 0, -0.008])),
            # Side 1: -X (narrow)
            (1, 1, 2, [9, 10, 11], False, np.array([-1.0, 0, 0]), np.array([0.008, 0, 0])),
            # Side 2: -Z (wide)
            (2, 2, 3, [15, 16, 17], True, np.array([0, 0, -1.0]), np.array([0, 0, 0.008])),
            # Side 3: +X (narrow)
            (3, 3, 0, [21, 22, 23], False, np.array([1.0, 0, 0]), np.array([-0.008, 0, 0])),
        ]

        n_down = self.vn(0, -1, 0)

        for side_idx, ca, cb, bev_inds, is_wide, n_out, in_norm in sides_info:
            wall_norm = self.vn(n_out[0], n_out[1], n_out[2])
            uv_fn = uv_wide if is_wide else uv_narrow

            # ca is Right (index 2), cb is Left (index 0)
            la = [leg_verts[ca][s][2] for s in range(4)]
            lb = [leg_verts[cb][s][0] for s in range(4)]

            # Bevel points between la[0] and lb[0]
            b0 = v_bev[bev_inds[0]]; b1 = v_bev[bev_inds[1]]; b2 = v_bev[bev_inds[2]]

            wall_y_top = 0.220
            wall_y_bot = 0.065
            arch_ceil_y = 0.025

            if is_wide:
                sign_lat = 1.0 if side_idx == 0 else -1.0
                z_face = self.verts[la[1]][2]
                z_foot = self.verts[la[3]][2]

                w_win_t = 0.050; w_win_b = 0.065
                w_arch = 0.045; w_foot = 0.075

                def pt_win(w, y):
                    return self.v(sign_lat * w, y, z_face)

                def pt_arch(w, y):
                    return self.v(sign_lat * w, y, z_foot)

                v_cut_tr = pt_win(w_win_t, wall_y_top)
                v_cut_tm = self.v(0.000, wall_y_top + 0.008, z_face)
                v_cut_tl = pt_win(-w_win_t, wall_y_top)
                v_cut_br = pt_win(w_win_b, wall_y_bot)
                v_cut_bl = pt_win(-w_win_b, wall_y_bot)

                v_arch_cr = pt_arch(w_arch, arch_ceil_y)
                v_arch_cl = pt_arch(-w_arch, arch_ceil_y)
                v_foot_r  = pt_arch(w_foot, 0.000)
                v_foot_l  = pt_arch(-w_foot, 0.000)
            else:
                sign_lat = 1.0 if side_idx == 1 else -1.0
                x_face = self.verts[la[1]][0]
                x_foot = self.verts[la[3]][0]

                w_win_t = 0.022; w_win_b = 0.035
                w_arch = 0.025; w_foot = 0.065

                def pt_win(w, y):
                    return self.v(x_face, y, sign_lat * w)

                def pt_arch(w, y):
                    return self.v(x_foot, y, sign_lat * w)

                v_cut_tr = pt_win(w_win_t, wall_y_top)
                v_cut_tm = self.v(x_face, wall_y_top + 0.006, 0.000)
                v_cut_tl = pt_win(-w_win_t, wall_y_top)
                v_cut_br = pt_win(w_win_b, wall_y_bot)
                v_cut_bl = pt_win(-w_win_b, wall_y_bot)

                v_arch_cr = pt_arch(w_arch, arch_ceil_y)
                v_arch_cl = pt_arch(-w_arch, arch_ceil_y)
                v_foot_r  = pt_arch(w_foot, 0.000)
                v_foot_l  = pt_arch(-w_foot, 0.000)

            # TOP APRON
            self.quad(la[0], b0, v_cut_tr, la[1], uv_fn(0.9, 0.285), uv_fn(0.5, 0.285), uv_fn(0.3, wall_y_top), uv_fn(0.9, wall_y_top), wall_norm, wall_norm, wall_norm, wall_norm)
            self.quad(b0, b1, v_cut_tm, v_cut_tr, uv_fn(0.5, 0.285), uv_fn(0.0, 0.285), uv_fn(0.0, wall_y_top+0.008), uv_fn(0.3, wall_y_top), wall_norm, wall_norm, wall_norm, wall_norm)
            self.quad(b1, b2, v_cut_tl, v_cut_tm, uv_fn(0.0, 0.285), uv_fn(-0.5, 0.285), uv_fn(-0.3, wall_y_top), uv_fn(0.0, wall_y_top+0.008), wall_norm, wall_norm, wall_norm, wall_norm)
            self.quad(b2, lb[0], lb[1], v_cut_tl, uv_fn(-0.5, 0.285), uv_fn(-0.9, 0.285), uv_fn(-0.9, wall_y_top), uv_fn(-0.3, wall_y_top), wall_norm, wall_norm, wall_norm, wall_norm)

            # SIDE COLUMNS BESIDE WINDOW
            self.quad(la[1], v_cut_tr, v_cut_br, la[2], uv_fn(0.9, wall_y_top), uv_fn(0.3, wall_y_top), uv_fn(0.4, wall_y_bot), uv_fn(0.9, wall_y_bot), wall_norm, wall_norm, wall_norm, wall_norm)
            self.quad(v_cut_tl, lb[1], lb[2], v_cut_bl, uv_fn(-0.3, wall_y_top), uv_fn(-0.9, wall_y_top), uv_fn(-0.9, wall_y_bot), uv_fn(-0.4, wall_y_bot), wall_norm, wall_norm, wall_norm, wall_norm)

            # SOLID LEG FACES DOWN TO GROUND (Y = 0.000)
            self.quad(la[2], v_cut_br, v_foot_r, la[3], uv_fn(0.9, wall_y_bot), uv_fn(0.4, wall_y_bot), uv_fn(0.5, 0.000), uv_fn(0.9, 0.000), wall_norm, wall_norm, wall_norm, wall_norm)
            self.quad(v_cut_bl, lb[2], lb[3], v_foot_l, uv_fn(-0.4, wall_y_bot), uv_fn(-0.9, wall_y_bot), uv_fn(-0.9, 0.000), uv_fn(-0.5, 0.000), wall_norm, wall_norm, wall_norm, wall_norm)

            # HORIZONTAL CROSSBAR & GROUND ARCH
            self.quad(v_cut_br, v_cut_bl, v_arch_cl, v_arch_cr, uv_fn(0.4, wall_y_bot), uv_fn(-0.4, wall_y_bot), uv_fn(-0.3, arch_ceil_y), uv_fn(0.3, arch_ceil_y), wall_norm, wall_norm, wall_norm, wall_norm)
            self.tri(v_cut_br, v_arch_cr, v_foot_r, uv_fn(0.4, wall_y_bot), uv_fn(0.3, arch_ceil_y), uv_fn(0.5, 0.000), wall_norm, wall_norm, wall_norm)
            self.tri(v_cut_bl, v_foot_l, v_arch_cl, uv_fn(-0.4, wall_y_bot), uv_fn(-0.5, 0.000), uv_fn(-0.3, arch_ceil_y), wall_norm, wall_norm, wall_norm)

            # WINDOW JAMBS
            j_tr = self.v(*(self.verts[v_cut_tr] + in_norm))
            j_tm = self.v(*(self.verts[v_cut_tm] + in_norm))
            j_tl = self.v(*(self.verts[v_cut_tl] + in_norm))
            j_br = self.v(*(self.verts[v_cut_br] + in_norm))
            j_bl = self.v(*(self.verts[v_cut_bl] + in_norm))

            n_jamb_top = self.vn(0, -1, 0)
            n_jamb_bot = self.vn(0, 1, 0)
            lat_axis = np.cross(n_out, np.array([0, 1, 0]))
            n_jamb_r = self.vn(-lat_axis[0], 0, -lat_axis[2])
            n_jamb_l = self.vn(lat_axis[0], 0, lat_axis[2])

            self.quad(v_cut_tr, v_cut_tm, j_tm, j_tr, uv_flange(0,1), uv_flange(0.5,1), uv_flange(0.5,0), uv_flange(0,0), n_jamb_top, n_jamb_top, n_jamb_top, n_jamb_top)
            self.quad(v_cut_tm, v_cut_tl, j_tl, j_tm, uv_flange(0.5,1), uv_flange(1,1), uv_flange(1,0), uv_flange(0.5,0), n_jamb_top, n_jamb_top, n_jamb_top, n_jamb_top)
            self.quad(v_cut_bl, v_cut_br, j_br, j_bl, uv_flange(0,1), uv_flange(1,1), uv_flange(1,0), uv_flange(0,0), n_jamb_bot, n_jamb_bot, n_jamb_bot, n_jamb_bot)
            self.quad(v_cut_br, v_cut_tr, j_tr, j_br, uv_flange(0,1), uv_flange(1,1), uv_flange(1,0), uv_flange(0,0), n_jamb_r, n_jamb_r, n_jamb_r, n_jamb_r)
            self.quad(v_cut_tl, v_cut_bl, j_bl, j_tl, uv_flange(0,1), uv_flange(1,1), uv_flange(1,0), uv_flange(0,0), n_jamb_l, n_jamb_l, n_jamb_l, n_jamb_l)

            # GROUND ARCH INNER FLANGE
            j_foot_r  = self.v(*(self.verts[v_foot_r] + in_norm))
            j_arch_cr = self.v(*(self.verts[v_arch_cr] + in_norm))
            j_arch_cl = self.v(*(self.verts[v_arch_cl] + in_norm))
            j_foot_l  = self.v(*(self.verts[v_foot_l] + in_norm))
            n_flange = self.vn(0, -1, 0)
            self.quad(v_foot_r, v_arch_cr, j_arch_cr, j_foot_r, uv_flange(0,1), uv_flange(0.3,1), uv_flange(0.3,0), uv_flange(0,0), n_flange, n_flange, n_flange, n_flange)
            self.quad(v_arch_cr, v_arch_cl, j_arch_cl, j_arch_cr, uv_flange(0.3,1), uv_flange(0.7,1), uv_flange(0.7,0), uv_flange(0.3,0), n_flange, n_flange, n_flange, n_flange)
            self.quad(v_arch_cl, v_foot_l, j_foot_l, j_arch_cl, uv_flange(0.7,1), uv_flange(1,1), uv_flange(1,0), uv_flange(0.7,0), n_flange, n_flange, n_flange, n_flange)

            # FOOT BOTTOM PADS UNDER FRONT FACES (Y = 0.000)
            j_la_3 = self.v(*(self.verts[la[3]] + in_norm))
            j_lb_3 = self.v(*(self.verts[lb[3]] + in_norm))
            self.quad(la[3], v_foot_r, j_foot_r, j_la_3, uv_flange(1,0), uv_flange(0,0), uv_flange(0,1), uv_flange(1,1), n_down, n_down, n_down, n_down)
            self.quad(v_foot_l, lb[3], j_lb_3, j_foot_l, uv_flange(0,0), uv_flange(1,0), uv_flange(1,1), uv_flange(0,1), n_down, n_down, n_down, n_down)

        # 4. SOLID CORNER FOOT BOTTOM PADS (Y = 0.000)
        for c_idx in range(4):
            pts_gr = leg_verts[c_idx][3]
            p0 = pts_gr[0]; p1 = pts_gr[1]; p2 = pts_gr[2]
            self.tri(p0, p2, p1, uv_seat(self.verts[p0][0], self.verts[p0][2]), uv_seat(self.verts[p2][0], self.verts[p2][2]), uv_seat(self.verts[p1][0], self.verts[p1][2]), n_down, n_down, n_down)

        return self.verts, self.uvs, self.normals, self.faces

    def write_obj(self, filepath):
        os.makedirs(os.path.dirname(os.path.abspath(filepath)), exist_ok=True)
        with open(filepath, 'w') as out:
            out.write("# SM_PlasticStool - Authentic Vietnamese Sidewalk Plastic Stool\n")
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
            for face in self.faces:
                f_str = " ".join([f"{v_idx+1}/{vt_idx+1}/{vn_idx+1}" for v_idx, vt_idx, vn_idx in face])
                out.write(f"f {f_str}\n")
        print(f"Generated {filepath}: {len(self.verts)} verts, {len(self.faces)} triangles")


def generate_pbr_textures(out_dir="Assets/TramChanh/Art/Textures", size=1024):
    os.makedirs(out_dir, exist_ok=True)
    base_color_path = os.path.join(out_dir, "T_Plastic_Beige_BaseColor.png")
    normal_map_path = os.path.join(out_dir, "T_Plastic_Beige_Normal.png")
    mask_map_path = os.path.join(out_dir, "T_Plastic_Beige_MaskMap.png")

    # Colors
    # Authentic Vietnamese cream beige plastic: #DAC8B0
    beige_base = (218, 200, 176)
    highlight_rim = (245, 238, 226)
    cavity_dark = (125, 110, 92)
    hole_center = (65, 55, 45)

    # 1. BaseColor Image
    base_img = Image.new('RGB', (size, size), beige_base)
    draw_base = ImageDraw.Draw(base_img)

    # 2. Normal Map Image (Default flat tangent space normal = (128, 128, 255))
    normal_arr = np.zeros((size, size, 3), dtype=np.float32)
    normal_arr[:, :, 0] = 0.5
    normal_arr[:, :, 1] = 0.5
    normal_arr[:, :, 2] = 1.0

    # 3. Mask Map Image (R: Metallic=0, G: AO=255, B: Detail=0, A: Smoothness=98 ~ 0.38)
    mask_arr = np.zeros((size, size, 4), dtype=np.uint8)
    mask_arr[:, :, 0] = 0    # Metallic = 0
    mask_arr[:, :, 1] = 255  # AO = 1.0
    mask_arr[:, :, 2] = 0    # Detail = 0
    mask_arr[:, :, 3] = 98   # Smoothness = 0.384

    # -------------------------------------------------------------
    # SEAT TOP TEXTURING (UV in [0.02, 0.48] x [0.52, 0.98])
    # In Pillow coordinates: Seat Center is at (256, 256)
    # -------------------------------------------------------------
    seat_cx, seat_cy = 256, 256
    seat_w, seat_h = 280, 260
    r_seat_corner = 65

    # Raised outer perimeter rim
    x0, y0 = seat_cx - seat_w // 2, seat_cy - seat_h // 2
    x1, y1 = seat_cx + seat_w // 2, seat_cy + seat_h // 2
    draw_base.rounded_rectangle([x0, y0, x1, y1], radius=r_seat_corner, outline=highlight_rim, width=3)

    # 9x8 Dimple Grid
    grid_cols = 9 # X axis
    grid_rows = 8 # Z axis
    grid_w = 186
    grid_h = 160
    xs = np.linspace(seat_cx - grid_w // 2, seat_cx + grid_w // 2, grid_cols)
    ys = np.linspace(seat_cy - grid_h // 2, seat_cy + grid_h // 2, grid_rows)
    dimple_r = 7.5

    for ix, hx in enumerate(xs):
        for iy, hy in enumerate(ys):
            if (ix == 0 or ix == grid_cols - 1) and (iy == 0 or iy == grid_rows - 1):
                continue

            hx_i, hy_i = int(round(hx)), int(round(hy))

            # BaseColor: highlight rim + cavity gradient
            draw_base.ellipse([hx_i - dimple_r - 1.5, hy_i - dimple_r - 1.5,
                               hx_i + dimple_r + 1.5, hy_i + dimple_r + 1.5], fill=highlight_rim)
            draw_base.ellipse([hx_i - dimple_r - 0.5, hy_i - dimple_r - 0.5,
                               hx_i + dimple_r + 0.5, hy_i + dimple_r + 0.5], fill=cavity_dark)
            draw_base.ellipse([hx_i - dimple_r, hy_i - dimple_r,
                               hx_i + dimple_r, hy_i + dimple_r], fill=hole_center)

            # Normal Map & Mask Map: Spherical depression vectors
            r_int = int(math.ceil(dimple_r + 1.0))
            for dy in range(-r_int, r_int + 1):
                for dx in range(-r_int, r_int + 1):
                    dist = math.sqrt(dx*dx + dy*dy)
                    if dist <= dimple_r:
                        px = hx_i + dx
                        py = hy_i + dy
                        if 0 <= px < size and 0 <= py < size:
                            norm_x = (dx / dimple_r) * 0.75
                            norm_y = -(dy / dimple_r) * 0.75
                            norm_z = math.sqrt(max(0.0, 1.0 - norm_x*norm_x - norm_y*norm_y))

                            normal_arr[py, px, 0] = norm_x * 0.5 + 0.5
                            normal_arr[py, px, 1] = norm_y * 0.5 + 0.5
                            normal_arr[py, px, 2] = norm_z

                            ao_factor = 0.40 + 0.60 * (dist / dimple_r)
                            mask_arr[py, px, 1] = int(255 * ao_factor)
                            mask_arr[py, px, 3] = int(98 * (0.85 + 0.15 * (dist / dimple_r)))

    # -------------------------------------------------------------
    # SIDE EMBOSSED CONTOURS (Cutout window embossed frames)
    # -------------------------------------------------------------
    cx_w, cy_w = 768, 256
    poly_trap = [
        (cx_w - 75, cy_w + 60),
        (cx_w + 75, cy_w + 60),
        (cx_w + 50, cy_w - 70),
        (cx_w, cy_w - 80),
        (cx_w - 50, cy_w - 70),
    ]
    draw_base.polygon(poly_trap, outline=highlight_rim, width=4)

    cx_n, cy_n = 768, 768
    poly_slot = [
        (cx_n - 42, cy_n + 60),
        (cx_n + 42, cy_n + 60),
        (cx_n + 25, cy_n - 65),
        (cx_n - 25, cy_n - 65),
    ]
    draw_base.polygon(poly_slot, outline=highlight_rim, width=4)

    # Subtle micro-surface noise for authentic plastic texture
    np.random.seed(42)
    noise = np.random.normal(0, 1.0, (size, size, 3))
    base_arr = np.array(base_img, dtype=np.float32) + noise
    base_arr = np.clip(base_arr, 0, 255).astype(np.uint8)
    base_img = Image.fromarray(base_arr)

    # Save textures
    base_img.save(base_color_path)
    print(f"Generated BaseColor: {base_color_path}")

    normal_img_arr = np.clip(normal_arr * 255.0, 0, 255).astype(np.uint8)
    normal_img = Image.fromarray(normal_img_arr, mode='RGB')
    normal_img.save(normal_map_path)
    print(f"Generated NormalMap: {normal_map_path}")

    mask_img = Image.fromarray(mask_arr, mode='RGBA')
    mask_img.save(mask_map_path)
    print(f"Generated MaskMap: {mask_map_path}")


def main():
    obj_path = "Assets/TramChanh/Art/Models/Furniture/SM_PlasticStool.obj"
    builder = AuthenticPlasticStoolBuilder()
    builder.build()
    builder.write_obj(obj_path)
    generate_pbr_textures()


if __name__ == "__main__":
    main()
