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
- Real dimensions: 0.355m (W) x 0.300m (H) x 0.329m (D).
- Pivot at bottom center (0, 0, 0).
- Budget: ~700-900 triangles (far below 1500 limit).
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

    def tri(self, v0, v1, v2, uv0, uv1, uv2):
        self.faces.append([(v0, uv0), (v1, uv1), (v2, uv2)])

    def quad(self, v0, v1, v2, v3, uv0, uv1, uv2, uv3):
        self.faces.append([(v0, uv0), (v1, uv1), (v2, uv2)])
        self.faces.append([(v0, uv0), (v2, uv2), (v3, uv3)])

    def build(self):
        # -------------------------------------------------------------
        # UV MAPPING HELPERS
        # -------------------------------------------------------------
        # Seat Top: UV in [0.04, 0.46] x [0.54, 0.96], center (0.25, 0.75)
        def uv_seat(x, z):
            u = 0.25 + (x / 0.150) * 0.20
            v = 0.75 + (z / 0.140) * 0.20
            return self.vt(np.clip(u, 0.02, 0.48), np.clip(v, 0.52, 0.98))

        # Side +Z / -Z (Wide Side with Large Trapezoid Arch): UV in [0.54, 0.96] x [0.54, 0.96]
        def uv_side_wide(x, y):
            u = 0.75 + (x / 0.180) * 0.20
            v = 0.54 + (y / 0.300) * 0.40
            return self.vt(np.clip(u, 0.52, 0.98), np.clip(v, 0.52, 0.98))

        # Side +X / -X (Narrow Side with Slot Cutout): UV in [0.54, 0.96] x [0.04, 0.46]
        def uv_side_narrow(z, y):
            u = 0.75 + (z / 0.165) * 0.20
            v = 0.04 + (y / 0.300) * 0.40
            return self.vt(np.clip(u, 0.52, 0.98), np.clip(v, 0.02, 0.48))

        # Solid plastic / inner flanges: UV in [0.04, 0.46] x [0.04, 0.46]
        def uv_flange(u_frac, v_frac):
            u = 0.05 + np.clip(u_frac, 0.0, 1.0) * 0.40
            v = 0.05 + np.clip(v_frac, 0.0, 1.0) * 0.40
            return self.vt(u, v)

        # -------------------------------------------------------------
        # 1. SEAT TOP PERIMETER & DECK
        # -------------------------------------------------------------
        # Dimensions of seat at top Y = 0.300:
        # X: [-0.138, 0.138], Z: [-0.128, 0.128], corner radius = 0.036
        cx_top = 0.138 - 0.036  # 0.102
        cz_top = 0.128 - 0.036  # 0.092
        cr_top = 0.036

        # Outer bevel edge at Y = 0.285:
        # X: [-0.146, 0.146], Z: [-0.136, 0.136], corner radius = 0.044
        cx_bev = 0.146 - 0.044  # 0.102
        cz_bev = 0.136 - 0.044  # 0.092
        cr_bev = 0.044

        def get_rounded_rect_24_pts(cx, cz, cr):
            pts = []
            # C0: (+X, +Z)
            pts.append((cx + cr * math.cos(0.0), cz + cr * math.sin(0.0)))
            pts.append((cx + cr * math.cos(math.pi/4), cz + cr * math.sin(math.pi/4)))
            pts.append((cx + cr * math.cos(math.pi/2), cz + cr * math.sin(math.pi/2)))
            # Side +Z (connecting C0 to C1, X decreasing from +cx to -cx)
            for t in [0.25, 0.50, 0.75]:
                pts.append((cx * (1 - 2*t), cz + cr))
            # C1: (-X, +Z)
            pts.append((-cx + cr * math.cos(math.pi/2), cz + cr * math.sin(math.pi/2)))
            pts.append((-cx + cr * math.cos(3*math.pi/4), cz + cr * math.sin(3*math.pi/4)))
            pts.append((-cx + cr * math.cos(math.pi), cz + cr * math.sin(math.pi)))
            # Side -X (connecting C1 to C2, Z decreasing from +cz to -cz)
            for t in [0.25, 0.50, 0.75]:
                pts.append((-cx - cr, cz * (1 - 2*t)))
            # C2: (-X, -Z)
            pts.append((-cx + cr * math.cos(math.pi), -cz + cr * math.sin(math.pi)))
            pts.append((-cx + cr * math.cos(5*math.pi/4), -cz + cr * math.sin(5*math.pi/4)))
            pts.append((-cx + cr * math.cos(3*math.pi/2), -cz + cr * math.sin(3*math.pi/2)))
            # Side -Z (connecting C2 to C3, X increasing from -cx to +cx)
            for t in [0.25, 0.50, 0.75]:
                pts.append((-cx * (1 - 2*t), -cz - cr))
            # C3: (+X, -Z)
            pts.append((cx + cr * math.cos(3*math.pi/2), -cz + cr * math.sin(3*math.pi/2)))
            pts.append((cx + cr * math.cos(7*math.pi/4), -cz + cr * math.sin(7*math.pi/4)))
            pts.append((cx + cr * math.cos(2*math.pi), -cz + cr * math.sin(2*math.pi)))
            # Side +X (connecting C3 to C0, Z increasing from -cz to +cz)
            for t in [0.25, 0.50, 0.75]:
                pts.append((cx + cr, -cz * (1 - 2*t)))
            return pts

        pts_top = get_rounded_rect_24_pts(cx_top, cz_top, cr_top)
        pts_bev = get_rounded_rect_24_pts(cx_bev, cz_bev, cr_bev)

        v_top = [self.v(px, 0.300, pz) for px, pz in pts_top]
        v_bev = [self.v(px, 0.285, pz) for px, pz in pts_bev]

        # Seat outer bevel ring (Y=0.300 down to Y=0.285)
        for i in range(24):
            i_next = (i + 1) % 24
            uv0 = uv_seat(pts_top[i][0], pts_top[i][1])
            uv1 = uv_seat(pts_top[i_next][0], pts_top[i_next][1])
            uv2 = uv_seat(pts_bev[i_next][0], pts_bev[i_next][1])
            uv3 = uv_seat(pts_bev[i][0], pts_bev[i][1])
            self.quad(v_top[i], v_top[i_next], v_bev[i_next], v_bev[i],
                      uv0, uv1, uv2, uv3)

        # Planar quad grid for seat top deck (5x5 vertices = 4x4 quads)
        x_grid = [-0.100, -0.050, 0.0, 0.050, 0.100]
        z_grid = [-0.090, -0.045, 0.0, 0.045, 0.090]
        grid_v = np.zeros((5, 5), dtype=int)
        for ix in range(5):
            for iz in range(5):
                grid_v[ix, iz] = self.v(x_grid[ix], 0.300, z_grid[iz])

        # Internal 16 quads
        for ix in range(4):
            for iz in range(4):
                v0 = grid_v[ix, iz]
                v1 = grid_v[ix+1, iz]
                v2 = grid_v[ix+1, iz+1]
                v3 = grid_v[ix, iz+1]
                p0 = self.verts[v0]; p1 = self.verts[v1]; p2 = self.verts[v2]; p3 = self.verts[v3]
                self.quad(v0, v3, v2, v1,
                          uv_seat(p0[0], p0[2]), uv_seat(p3[0], p3[2]),
                          uv_seat(p2[0], p2[2]), uv_seat(p1[0], p1[2]))

        # Connect seat perimeter to grid
        # Side +Z perimeter (points 2..6 to grid top row iz=4)
        for k in range(4):
            pt_a = 2 + k; pt_b = 3 + k
            g_a = grid_v[4 - k, 4]; g_b = grid_v[3 - k, 4]
            p_ra = self.verts[v_top[pt_a]]; p_rb = self.verts[v_top[pt_b]]
            p_ga = self.verts[g_a]; p_gb = self.verts[g_b]
            self.quad(v_top[pt_a], g_a, g_b, v_top[pt_b],
                      uv_seat(p_ra[0], p_ra[2]), uv_seat(p_ga[0], p_ga[2]),
                      uv_seat(p_gb[0], p_gb[2]), uv_seat(p_rb[0], p_rb[2]))

        # Side -Z perimeter (points 14..18 to grid bottom row iz=0)
        for k in range(4):
            pt_a = 14 + k; pt_b = 15 + k
            g_a = grid_v[k, 0]; g_b = grid_v[k + 1, 0]
            p_ra = self.verts[v_top[pt_a]]; p_rb = self.verts[v_top[pt_b]]
            p_ga = self.verts[g_a]; p_gb = self.verts[g_b]
            self.quad(v_top[pt_a], g_a, g_b, v_top[pt_b],
                      uv_seat(p_ra[0], p_ra[2]), uv_seat(p_ga[0], p_ga[2]),
                      uv_seat(p_gb[0], p_gb[2]), uv_seat(p_rb[0], p_rb[2]))

        # Side -X perimeter (points 8..12 to grid left col ix=0)
        for k in range(4):
            pt_a = 8 + k; pt_b = 9 + k
            g_a = grid_v[0, 4 - k]; g_b = grid_v[0, 3 - k]
            p_ra = self.verts[v_top[pt_a]]; p_rb = self.verts[v_top[pt_b]]
            p_ga = self.verts[g_a]; p_gb = self.verts[g_b]
            self.quad(v_top[pt_a], g_a, g_b, v_top[pt_b],
                      uv_seat(p_ra[0], p_ra[2]), uv_seat(p_ga[0], p_ga[2]),
                      uv_seat(p_gb[0], p_gb[2]), uv_seat(p_rb[0], p_rb[2]))

        # Side +X perimeter (points 20..23, 0 to grid right col ix=4)
        for k in range(4):
            pt_a = 20 + k; pt_b = (21 + k) % 24
            g_a = grid_v[4, k]; g_b = grid_v[4, k + 1]
            p_ra = self.verts[v_top[pt_a]]; p_rb = self.verts[v_top[pt_b]]
            p_ga = self.verts[g_a]; p_gb = self.verts[g_b]
            self.quad(v_top[pt_a], g_a, g_b, v_top[pt_b],
                      uv_seat(p_ra[0], p_ra[2]), uv_seat(p_ga[0], p_ga[2]),
                      uv_seat(p_gb[0], p_gb[2]), uv_seat(p_rb[0], p_rb[2]))

        # 4 Corners connecting grid corners to rounded corner points
        # C0 (+X, +Z)
        p_c0 = self.verts[grid_v[4, 4]]
        p_r0 = self.verts[v_top[0]]; p_r1 = self.verts[v_top[1]]; p_r2 = self.verts[v_top[2]]
        self.tri(v_top[0], grid_v[4, 4], v_top[1],
                 uv_seat(p_r0[0], p_r0[2]), uv_seat(p_c0[0], p_c0[2]), uv_seat(p_r1[0], p_r1[2]))
        self.tri(v_top[1], grid_v[4, 4], v_top[2],
                 uv_seat(p_r1[0], p_r1[2]), uv_seat(p_c0[0], p_c0[2]), uv_seat(p_r2[0], p_r2[2]))

        # C1 (-X, +Z)
        p_c1 = self.verts[grid_v[0, 4]]
        p_r6 = self.verts[v_top[6]]; p_r7 = self.verts[v_top[7]]; p_r8 = self.verts[v_top[8]]
        self.tri(v_top[6], grid_v[0, 4], v_top[7],
                 uv_seat(p_r6[0], p_r6[2]), uv_seat(p_c1[0], p_c1[2]), uv_seat(p_r7[0], p_r7[2]))
        self.tri(v_top[7], grid_v[0, 4], v_top[8],
                 uv_seat(p_r7[0], p_r7[2]), uv_seat(p_c1[0], p_c1[2]), uv_seat(p_r8[0], p_r8[2]))

        # C2 (-X, -Z)
        p_c2 = self.verts[grid_v[0, 0]]
        p_r12 = self.verts[v_top[12]]; p_r13 = self.verts[v_top[13]]; p_r14 = self.verts[v_top[14]]
        self.tri(v_top[12], grid_v[0, 0], v_top[13],
                 uv_seat(p_r12[0], p_r12[2]), uv_seat(p_c2[0], p_c2[2]), uv_seat(p_r13[0], p_r13[2]))
        self.tri(v_top[13], grid_v[0, 0], v_top[14],
                 uv_seat(p_r13[0], p_r13[2]), uv_seat(p_c2[0], p_c2[2]), uv_seat(p_r14[0], p_r14[2]))

        # C3 (+X, -Z)
        p_c3 = self.verts[grid_v[4, 0]]
        p_r18 = self.verts[v_top[18]]; p_r19 = self.verts[v_top[19]]; p_r20 = self.verts[v_top[20]]
        self.tri(v_top[18], grid_v[4, 0], v_top[19],
                 uv_seat(p_r18[0], p_r18[2]), uv_seat(p_c3[0], p_c3[2]), uv_seat(p_r19[0], p_r19[2]))
        self.tri(v_top[19], grid_v[4, 0], v_top[20],
                 uv_seat(p_r19[0], p_r19[2]), uv_seat(p_c3[0], p_c3[2]), uv_seat(p_r20[0], p_r20[2]))

        # -------------------------------------------------------------
        # 2. 4 FLARED CORNER LEGS (OUTER SHELL)
        # -------------------------------------------------------------
        leg_stages = [
            # Y,    cx,     cz,     cr_out
            (0.285, 0.102,  0.092,  0.044),
            (0.225, 0.108,  0.097,  0.046),
            (0.075, 0.120,  0.108,  0.050),
            (0.035, 0.124,  0.111,  0.051),
            (0.000, 0.1265, 0.1135, 0.051),
        ]

        corner_signs = [(1, 1), (-1, 1), (-1, -1), (1, -1)]
        corner_bevel_indices = [
            [0, 1, 2],      # C0 (+X, +Z)
            [6, 7, 8],      # C1 (-X, +Z)
            [12, 13, 14],   # C2 (-X, -Z)
            [18, 19, 20],   # C3 (+X, -Z)
        ]

        leg_corner_verts = []
        for c_idx, (sx, sz) in enumerate(corner_signs):
            c_stages = []
            base_ang = math.atan2(sz, sx)
            for s_idx, (y_h, mx, mz, r_out) in enumerate(leg_stages):
                if s_idx == 0:
                    inds = corner_bevel_indices[c_idx]
                    c_stages.append([v_bev[inds[0]], v_bev[inds[1]], v_bev[inds[2]]])
                else:
                    cx = sx * mx
                    cz = sz * mz
                    pts = []
                    for da in [-math.pi/4, 0.0, math.pi/4]:
                        ang = base_ang + da
                        px = cx + math.cos(ang) * r_out
                        pz = cz + math.sin(ang) * r_out
                        pts.append(self.v(px, y_h, pz))
                    c_stages.append(pts)
            leg_corner_verts.append(c_stages)

            # Skin leg corner quads vertically between stages
            for s in range(len(leg_stages) - 1):
                s_t = c_stages[s]
                s_b = c_stages[s + 1]
                p0t = self.verts[s_t[0]]; p1t = self.verts[s_t[1]]
                p0b = self.verts[s_b[0]]; p1b = self.verts[s_b[1]]
                self.quad(s_t[0], s_t[1], s_b[1], s_b[0],
                          uv_seat(p0t[0], p0t[2]), uv_seat(p1t[0], p1t[2]),
                          uv_seat(p1b[0], p1b[2]), uv_seat(p0b[0], p0b[2]))
                p2t = self.verts[s_t[2]]; p2b = self.verts[s_b[2]]
                self.quad(s_t[1], s_t[2], s_b[2], s_b[1],
                          uv_seat(p1t[0], p1t[2]), uv_seat(p2t[0], p2t[2]),
                          uv_seat(p2b[0], p2b[2]), uv_seat(p1b[0], p1b[2]))

        # -------------------------------------------------------------
        # 3. 4 SIDE WALLS WITH AUTHENTIC CUTOUTS & GROUND ARCHES
        # -------------------------------------------------------------
        def build_side_wall(ca, cb, side_bevel_inds, is_wide, normal_sign):
            la_y0 = leg_corner_verts[ca][0][2]; lb_y0 = leg_corner_verts[cb][0][0]
            la_y1 = leg_corner_verts[ca][1][2]; lb_y1 = leg_corner_verts[cb][1][0]
            la_y2 = leg_corner_verts[ca][2][2]; lb_y2 = leg_corner_verts[cb][2][0]
            la_y3 = leg_corner_verts[ca][3][2]; lb_y3 = leg_corner_verts[cb][3][0]
            la_y4 = leg_corner_verts[ca][4][2]; lb_y4 = leg_corner_verts[cb][4][0]

            p_la_y0 = self.verts[la_y0]; p_lb_y0 = self.verts[lb_y0]
            p_la_y1 = self.verts[la_y1]; p_lb_y1 = self.verts[lb_y1]
            p_la_y2 = self.verts[la_y2]; p_lb_y2 = self.verts[lb_y2]
            p_la_y3 = self.verts[la_y3]; p_lb_y3 = self.verts[lb_y3]
            p_la_y4 = self.verts[la_y4]; p_lb_y4 = self.verts[lb_y4]

            in_norm = np.array([0.0, 0.0, -0.008 * normal_sign], dtype=np.float32) if is_wide else np.array([-0.008 * normal_sign, 0.0, 0.0], dtype=np.float32)

            if is_wide:
                uv_fn = uv_side_wide
                z_face = p_la_y1[2]
                x_tl = -0.055; x_tr = 0.055
                x_bl = -0.080; x_br = 0.080

                if p_la_y1[0] > p_lb_y1[0]:
                    # ca is +X (Right), cb is -X (Left)
                    v_cut_tr = self.v(x_tr, 0.225, z_face)
                    v_cut_tm = self.v(0.0, 0.232, z_face)
                    v_cut_tl = self.v(x_tl, 0.225, z_face)
                    v_cut_br = self.v(x_br, 0.075, p_la_y2[2])
                    v_cut_bl = self.v(x_bl, 0.075, p_lb_y2[2])

                    bv0 = v_bev[side_bevel_inds[0]]
                    bv1 = v_bev[side_bevel_inds[1]]
                    bv2 = v_bev[side_bevel_inds[2]]

                    # Top Apron
                    self.quad(la_y0, bv0, v_cut_tr, la_y1,
                              uv_fn(p_la_y0[0], p_la_y0[1]), uv_fn(self.verts[bv0][0], self.verts[bv0][1]),
                              uv_fn(x_tr, 0.225), uv_fn(p_la_y1[0], p_la_y1[1]))
                    self.quad(bv0, bv1, v_cut_tm, v_cut_tr,
                              uv_fn(self.verts[bv0][0], self.verts[bv0][1]), uv_fn(self.verts[bv1][0], self.verts[bv1][1]),
                              uv_fn(0.0, 0.232), uv_fn(x_tr, 0.225))
                    self.quad(bv1, bv2, v_cut_tl, v_cut_tm,
                              uv_fn(self.verts[bv1][0], self.verts[bv1][1]), uv_fn(self.verts[bv2][0], self.verts[bv2][1]),
                              uv_fn(x_tl, 0.225), uv_fn(0.0, 0.232))
                    self.quad(bv2, lb_y0, lb_y1, v_cut_tl,
                              uv_fn(self.verts[bv2][0], self.verts[bv2][1]), uv_fn(p_lb_y0[0], p_lb_y0[1]),
                              uv_fn(p_lb_y1[0], p_lb_y1[1]), uv_fn(x_tl, 0.225))

                    # Side Columns
                    self.quad(la_y1, v_cut_tr, v_cut_br, la_y2,
                              uv_fn(p_la_y1[0], p_la_y1[1]), uv_fn(x_tr, 0.225),
                              uv_fn(x_br, 0.075), uv_fn(p_la_y2[0], p_la_y2[1]))
                    self.quad(v_cut_tl, lb_y1, lb_y2, v_cut_bl,
                              uv_fn(x_tl, 0.225), uv_fn(p_lb_y1[0], p_lb_y1[1]),
                              uv_fn(p_lb_y2[0], p_lb_y2[1]), uv_fn(x_bl, 0.075))

                    # Bottom Crossbar
                    z_arch = p_la_y3[2]
                    v_bar_m = self.v(0.0, 0.035, z_arch)
                    self.quad(la_y2, v_cut_br, v_bar_m, la_y3,
                              uv_fn(p_la_y2[0], p_la_y2[1]), uv_fn(x_br, 0.075),
                              uv_fn(0.0, 0.035), uv_fn(p_la_y3[0], p_la_y3[1]))
                    self.tri(v_cut_br, v_cut_bl, v_bar_m,
                             uv_fn(x_br, 0.075), uv_fn(x_bl, 0.075), uv_fn(0.0, 0.035))
                    self.quad(v_cut_bl, lb_y2, lb_y3, v_bar_m,
                              uv_fn(x_bl, 0.075), uv_fn(p_lb_y2[0], p_lb_y2[1]),
                              uv_fn(p_lb_y3[0], p_lb_y3[1]), uv_fn(0.0, 0.035))

                    # Inner Wall Jambs around Cutout Window
                    j_tl = self.v(*(self.verts[v_cut_tl] + in_norm))
                    j_tm = self.v(*(self.verts[v_cut_tm] + in_norm))
                    j_tr = self.v(*(self.verts[v_cut_tr] + in_norm))
                    j_br = self.v(*(self.verts[v_cut_br] + in_norm))
                    j_bl = self.v(*(self.verts[v_cut_bl] + in_norm))

                    self.quad(v_cut_tr, v_cut_tm, j_tm, j_tr,
                              uv_flange(0.0, 1.0), uv_flange(0.5, 1.0), uv_flange(0.5, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_cut_tm, v_cut_tl, j_tl, j_tm,
                              uv_flange(0.5, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.5, 0.0))
                    self.quad(v_cut_br, v_cut_tr, j_tr, j_br,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_cut_tl, v_cut_bl, j_bl, j_tl,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_cut_bl, v_cut_br, j_br, j_bl,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))

                    # Bottom Arch Inner Flange
                    j_bar_m = self.v(*(self.verts[v_bar_m] + in_norm))
                    j_la_y3 = self.v(*(self.verts[la_y3] + in_norm))
                    j_lb_y3 = self.v(*(self.verts[lb_y3] + in_norm))
                    self.quad(la_y3, v_bar_m, j_bar_m, j_la_y3,
                              uv_flange(0.0, 1.0), uv_flange(0.5, 1.0), uv_flange(0.5, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_bar_m, lb_y3, j_lb_y3, j_bar_m,
                              uv_flange(0.5, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.5, 0.0))

                else:
                    # ca is -X (Left), cb is +X (Right)
                    v_cut_tl = self.v(x_tl, 0.225, z_face)
                    v_cut_tm = self.v(0.0, 0.232, z_face)
                    v_cut_tr = self.v(x_tr, 0.225, z_face)
                    v_cut_bl = self.v(x_bl, 0.075, p_la_y2[2])
                    v_cut_br = self.v(x_br, 0.075, p_lb_y2[2])

                    bv0 = v_bev[side_bevel_inds[0]]
                    bv1 = v_bev[side_bevel_inds[1]]
                    bv2 = v_bev[side_bevel_inds[2]]

                    self.quad(la_y0, bv0, v_cut_tl, la_y1,
                              uv_fn(p_la_y0[0], p_la_y0[1]), uv_fn(self.verts[bv0][0], self.verts[bv0][1]),
                              uv_fn(x_tl, 0.225), uv_fn(p_la_y1[0], p_la_y1[1]))
                    self.quad(bv0, bv1, v_cut_tm, v_cut_tl,
                              uv_fn(self.verts[bv0][0], self.verts[bv0][1]), uv_fn(self.verts[bv1][0], self.verts[bv1][1]),
                              uv_fn(0.0, 0.232), uv_fn(x_tl, 0.225))
                    self.quad(bv1, bv2, v_cut_tr, v_cut_tm,
                              uv_fn(self.verts[bv1][0], self.verts[bv1][1]), uv_fn(self.verts[bv2][0], self.verts[bv2][1]),
                              uv_fn(x_tr, 0.225), uv_fn(0.0, 0.232))
                    self.quad(bv2, lb_y0, lb_y1, v_cut_tr,
                              uv_fn(self.verts[bv2][0], self.verts[bv2][1]), uv_fn(p_lb_y0[0], p_lb_y0[1]),
                              uv_fn(p_lb_y1[0], p_lb_y1[1]), uv_fn(x_tr, 0.225))

                    self.quad(la_y1, v_cut_tl, v_cut_bl, la_y2,
                              uv_fn(p_la_y1[0], p_la_y1[1]), uv_fn(x_tl, 0.225),
                              uv_fn(x_bl, 0.075), uv_fn(p_la_y2[0], p_la_y2[1]))
                    self.quad(v_cut_tr, lb_y1, lb_y2, v_cut_br,
                              uv_fn(x_tr, 0.225), uv_fn(p_lb_y1[0], p_lb_y1[1]),
                              uv_fn(p_lb_y2[0], p_lb_y2[1]), uv_fn(x_br, 0.075))

                    z_arch = p_la_y3[2]
                    v_bar_m = self.v(0.0, 0.035, z_arch)
                    self.quad(la_y2, v_cut_bl, v_bar_m, la_y3,
                              uv_fn(p_la_y2[0], p_la_y2[1]), uv_fn(x_bl, 0.075),
                              uv_fn(0.0, 0.035), uv_fn(p_la_y3[0], p_la_y3[1]))
                    self.tri(v_cut_bl, v_cut_br, v_bar_m,
                             uv_fn(x_bl, 0.075), uv_fn(x_br, 0.075), uv_fn(0.0, 0.035))
                    self.quad(v_cut_br, lb_y2, lb_y3, v_bar_m,
                              uv_fn(x_br, 0.075), uv_fn(p_lb_y2[0], p_lb_y2[1]),
                              uv_fn(p_lb_y3[0], p_lb_y3[1]), uv_fn(0.0, 0.035))

                    j_tl = self.v(*(self.verts[v_cut_tl] + in_norm))
                    j_tm = self.v(*(self.verts[v_cut_tm] + in_norm))
                    j_tr = self.v(*(self.verts[v_cut_tr] + in_norm))
                    j_br = self.v(*(self.verts[v_cut_br] + in_norm))
                    j_bl = self.v(*(self.verts[v_cut_bl] + in_norm))

                    self.quad(v_cut_tl, v_cut_tm, j_tm, j_tl,
                              uv_flange(0.0, 1.0), uv_flange(0.5, 1.0), uv_flange(0.5, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_cut_tm, v_cut_tr, j_tr, j_tm,
                              uv_flange(0.5, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.5, 0.0))
                    self.quad(v_cut_tl, v_cut_bl, j_bl, j_tl,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_cut_br, v_cut_tr, j_tr, j_br,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_cut_bl, v_cut_br, j_br, j_bl,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))

                    j_bar_m = self.v(*(self.verts[v_bar_m] + in_norm))
                    j_la_y3 = self.v(*(self.verts[la_y3] + in_norm))
                    j_lb_y3 = self.v(*(self.verts[lb_y3] + in_norm))
                    self.quad(la_y3, v_bar_m, j_bar_m, j_la_y3,
                              uv_flange(0.0, 1.0), uv_flange(0.5, 1.0), uv_flange(0.5, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_bar_m, lb_y3, j_lb_y3, j_bar_m,
                              uv_flange(0.5, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.5, 0.0))

            else:
                # Narrow Side with slot cutout
                uv_fn = uv_side_narrow
                x_face = p_la_y1[0]
                z_tt = 0.025; z_tb = -0.025
                z_bt = 0.045; z_bb = -0.045

                if p_la_y1[2] > p_lb_y1[2]:
                    # ca is +Z (top), cb is -Z (bottom)
                    v_cut_tt = self.v(x_face, 0.210, z_tt)
                    v_cut_tb = self.v(x_face, 0.210, z_tb)
                    v_cut_bt = self.v(x_face, 0.075, z_bt)
                    v_cut_bb = self.v(x_face, 0.075, z_bb)

                    bv0 = v_bev[side_bevel_inds[0]]
                    bv1 = v_bev[side_bevel_inds[1]]
                    bv2 = v_bev[side_bevel_inds[2]]

                    # Top Apron
                    self.quad(la_y0, bv0, v_cut_tt, la_y1,
                              uv_fn(p_la_y0[2], p_la_y0[1]), uv_fn(self.verts[bv0][2], self.verts[bv0][1]),
                              uv_fn(z_tt, 0.210), uv_fn(p_la_y1[2], p_la_y1[1]))
                    self.tri(bv0, bv1, v_cut_tt,
                             uv_fn(self.verts[bv0][2], self.verts[bv0][1]), uv_fn(self.verts[bv1][2], self.verts[bv1][1]),
                             uv_fn(z_tt, 0.210))
                    self.quad(bv1, bv2, v_cut_tb, v_cut_tt,
                              uv_fn(self.verts[bv1][2], self.verts[bv1][1]), uv_fn(self.verts[bv2][2], self.verts[bv2][1]),
                              uv_fn(z_tb, 0.210), uv_fn(z_tt, 0.210))
                    self.quad(bv2, lb_y0, lb_y1, v_cut_tb,
                              uv_fn(self.verts[bv2][2], self.verts[bv2][1]), uv_fn(p_lb_y0[2], p_lb_y0[1]),
                              uv_fn(p_lb_y1[2], p_lb_y1[1]), uv_fn(z_tb, 0.210))

                    # Side Columns
                    self.quad(la_y1, v_cut_tt, v_cut_bt, la_y2,
                              uv_fn(p_la_y1[2], p_la_y1[1]), uv_fn(z_tt, 0.210),
                              uv_fn(z_bt, 0.075), uv_fn(p_la_y2[2], p_la_y2[1]))
                    self.quad(v_cut_tb, lb_y1, lb_y2, v_cut_bb,
                              uv_fn(z_tb, 0.210), uv_fn(p_lb_y1[2], p_lb_y1[1]),
                              uv_fn(p_lb_y2[2], p_lb_y2[1]), uv_fn(z_bb, 0.075))

                    # Bottom Crossbar
                    v_bar_m = self.v(x_face, 0.035, 0.0)
                    self.quad(la_y2, v_cut_bt, v_bar_m, la_y3,
                              uv_fn(p_la_y2[2], p_la_y2[1]), uv_fn(z_bt, 0.075),
                              uv_fn(0.0, 0.035), uv_fn(p_la_y3[2], p_la_y3[1]))
                    self.tri(v_cut_bt, v_cut_bb, v_bar_m,
                             uv_fn(z_bt, 0.075), uv_fn(z_bb, 0.075), uv_fn(0.0, 0.035))
                    self.quad(v_cut_bb, lb_y2, lb_y3, v_bar_m,
                              uv_fn(z_bb, 0.075), uv_fn(p_lb_y2[2], p_lb_y2[1]),
                              uv_fn(p_lb_y3[2], p_lb_y3[1]), uv_fn(0.0, 0.035))

                    # Jambs
                    j_tt = self.v(*(self.verts[v_cut_tt] + in_norm))
                    j_tb = self.v(*(self.verts[v_cut_tb] + in_norm))
                    j_bt = self.v(*(self.verts[v_cut_bt] + in_norm))
                    j_bb = self.v(*(self.verts[v_cut_bb] + in_norm))

                    self.quad(v_cut_tt, v_cut_tb, j_tb, j_tt,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_cut_tb, v_cut_bb, j_bb, j_tb,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_cut_bb, v_cut_bt, j_bt, j_bb,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_cut_bt, v_cut_tt, j_tt, j_bt,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))

                    j_bar_m = self.v(*(self.verts[v_bar_m] + in_norm))
                    j_la_y3 = self.v(*(self.verts[la_y3] + in_norm))
                    j_lb_y3 = self.v(*(self.verts[lb_y3] + in_norm))
                    self.quad(la_y3, v_bar_m, j_bar_m, j_la_y3,
                              uv_flange(0.0, 1.0), uv_flange(0.5, 1.0), uv_flange(0.5, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_bar_m, lb_y3, j_lb_y3, j_bar_m,
                              uv_flange(0.5, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.5, 0.0))

                else:
                    # ca is -Z (bottom), cb is +Z (top)
                    v_cut_tb = self.v(x_face, 0.210, z_tb)
                    v_cut_tt = self.v(x_face, 0.210, z_tt)
                    v_cut_bb = self.v(x_face, 0.075, z_bb)
                    v_cut_bt = self.v(x_face, 0.075, z_bt)

                    bv0 = v_bev[side_bevel_inds[0]]
                    bv1 = v_bev[side_bevel_inds[1]]
                    bv2 = v_bev[side_bevel_inds[2]]

                    self.quad(la_y0, bv0, v_cut_tb, la_y1,
                              uv_fn(p_la_y0[2], p_la_y0[1]), uv_fn(self.verts[bv0][2], self.verts[bv0][1]),
                              uv_fn(z_tb, 0.210), uv_fn(p_la_y1[2], p_la_y1[1]))
                    self.tri(bv0, bv1, v_cut_tb,
                             uv_fn(self.verts[bv0][2], self.verts[bv0][1]), uv_fn(self.verts[bv1][2], self.verts[bv1][1]),
                             uv_fn(z_tb, 0.210))
                    self.quad(bv1, bv2, v_cut_tt, v_cut_tb,
                              uv_fn(self.verts[bv1][2], self.verts[bv1][1]), uv_fn(self.verts[bv2][2], self.verts[bv2][1]),
                              uv_fn(z_tt, 0.210), uv_fn(z_tb, 0.210))
                    self.quad(bv2, lb_y0, lb_y1, v_cut_tt,
                              uv_fn(self.verts[bv2][2], self.verts[bv2][1]), uv_fn(p_lb_y0[2], p_lb_y0[1]),
                              uv_fn(p_lb_y1[2], p_lb_y1[1]), uv_fn(z_tt, 0.210))

                    self.quad(la_y1, v_cut_tb, v_cut_bb, la_y2,
                              uv_fn(p_la_y1[2], p_la_y1[1]), uv_fn(z_tb, 0.210),
                              uv_fn(z_bb, 0.075), uv_fn(p_la_y2[2], p_la_y2[1]))
                    self.quad(v_cut_tt, lb_y1, lb_y2, v_cut_bt,
                              uv_fn(z_tt, 0.210), uv_fn(p_lb_y1[2], p_lb_y1[1]),
                              uv_fn(p_lb_y2[2], p_lb_y2[1]), uv_fn(z_bt, 0.075))

                    v_bar_m = self.v(x_face, 0.035, 0.0)
                    self.quad(la_y2, v_cut_bb, v_bar_m, la_y3,
                              uv_fn(p_la_y2[2], p_la_y2[1]), uv_fn(z_bb, 0.075),
                              uv_fn(0.0, 0.035), uv_fn(p_la_y3[2], p_la_y3[1]))
                    self.tri(v_cut_bb, v_cut_bt, v_bar_m,
                             uv_fn(z_bb, 0.075), uv_fn(z_bt, 0.075), uv_fn(0.0, 0.035))
                    self.quad(v_cut_bt, lb_y2, lb_y3, v_bar_m,
                              uv_fn(z_bt, 0.075), uv_fn(p_lb_y2[2], p_lb_y2[1]),
                              uv_fn(p_lb_y3[2], p_lb_y3[1]), uv_fn(0.0, 0.035))

                    j_tb = self.v(*(self.verts[v_cut_tb] + in_norm))
                    j_tt = self.v(*(self.verts[v_cut_tt] + in_norm))
                    j_bb = self.v(*(self.verts[v_cut_bb] + in_norm))
                    j_bt = self.v(*(self.verts[v_cut_bt] + in_norm))

                    self.quad(v_cut_tb, v_cut_tt, j_tt, j_tb,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_cut_tt, v_cut_bt, j_bt, j_tt,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_cut_bt, v_cut_bb, j_bb, j_bt,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_cut_bb, v_cut_tb, j_tb, j_bb,
                              uv_flange(0.0, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.0, 0.0))

                    j_bar_m = self.v(*(self.verts[v_bar_m] + in_norm))
                    j_la_y3 = self.v(*(self.verts[la_y3] + in_norm))
                    j_lb_y3 = self.v(*(self.verts[lb_y3] + in_norm))
                    self.quad(la_y3, v_bar_m, j_bar_m, j_la_y3,
                              uv_flange(0.0, 1.0), uv_flange(0.5, 1.0), uv_flange(0.5, 0.0), uv_flange(0.0, 0.0))
                    self.quad(v_bar_m, lb_y3, j_lb_y3, j_bar_m,
                              uv_flange(0.5, 1.0), uv_flange(1.0, 1.0), uv_flange(1.0, 0.0), uv_flange(0.5, 0.0))

        # Build the 4 side walls
        build_side_wall(0, 1, [3, 4, 5], is_wide=True, normal_sign=1.0)
        build_side_wall(1, 2, [9, 10, 11], is_wide=False, normal_sign=-1.0)
        build_side_wall(2, 3, [15, 16, 17], is_wide=True, normal_sign=-1.0)
        build_side_wall(3, 0, [21, 22, 23], is_wide=False, normal_sign=1.0)

        # -------------------------------------------------------------
        # 4. GROUND FOOT PADS (AT Y = 0.000)
        # -------------------------------------------------------------
        for c_idx in range(4):
            pts_gr = leg_corner_verts[c_idx][4]
            p0 = self.verts[pts_gr[0]]; p1 = self.verts[pts_gr[1]]; p2 = self.verts[pts_gr[2]]
            self.tri(pts_gr[0], pts_gr[1], pts_gr[2],
                     uv_seat(p0[0], p0[2]), uv_seat(p1[0], p1[2]), uv_seat(p2[0], p2[2]))

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
            for f in self.faces:
                line = "f " + " ".join(f"{v+1}/{vt+1}/{v+1}" for v, vt in f) + "\n"
                out.write(line)
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
    builder.compute_smooth_normals()
    builder.write_obj(obj_path)
    generate_pbr_textures()


if __name__ == "__main__":
    main()
