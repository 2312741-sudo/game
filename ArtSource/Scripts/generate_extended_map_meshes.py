#!/usr/bin/env python3
"""
Generate 3D OBJ meshes for the Extended Street Environment (Task ART-MAP-002).
Includes:
- SM_Sidewalk_Extended_80m.obj (Continuous south sidewalk x: -40 to 40, y: 0.0m, curb at z: 3.6m, alley opening at x: 16.5 to 20.5)
- SM_North_Sidewalk_80m.obj (North sidewalk x: -40 to 40, z: 12.6 to 16.6, y: 0.0m)
- SM_Asphalt_Road_80m.obj (2-lane roadway x: -40 to 40, z: 3.6 to 12.6, y: -0.15m)
- SM_Alley_Extended.obj (Side alley pavement x: 16.5 to 20.5, z: -15.0 to -3.8, y: 0.0m)
- SM_Alley_Walls.obj (Side alley brick/plaster flanking walls)
"""

import os
import math

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

    def add_box(self, x_min, x_max, y_min, y_max, z_min, z_max, uv_scale=1.0, u_offset=0.0, v_offset=0.0):
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
        uo = u_offset
        vo = v_offset

        # Front (+Z)
        self.add_quad(v001, v101, v111, v011, (uo, vo), (uo+dx, vo), (uo+dx, vo+dy), (uo, vo+dy), (0, 0, 1))
        # Back (-Z)
        self.add_quad(v100, v000, v010, v110, (uo, vo), (uo+dx, vo), (uo+dx, vo+dy), (uo, vo+dy), (0, 0, -1))
        # Top (+Y)
        self.add_quad(v011, v111, v110, v010, (uo, vo), (uo+dx, vo), (uo+dx, vo+dz), (uo, vo+dz), (0, 1, 0))
        # Bottom (-Y)
        self.add_quad(v000, v100, v101, v001, (uo, vo), (uo+dx, vo), (uo+dx, vo+dz), (uo, vo+dz), (0, -1, 0))
        # Right (+X)
        self.add_quad(v101, v100, v110, v111, (uo, vo), (uo+dz, vo), (uo+dz, vo+dy), (uo, vo+dy), (1, 0, 0))
        # Left (-X)
        self.add_quad(v000, v001, v011, v010, (uo, vo), (uo+dz, vo), (uo+dz, vo+dy), (uo, vo+dy), (-1, 0, 0))

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

def generate_all_extended_meshes(out_dir):
    # 1. SM_Sidewalk_Extended_80m
    # South sidewalk: x from -40.0 to +40.0, z from -3.8 to 3.6, y: -0.15 to 0.00
    # Alley at x in [16.5, 20.5] cuts south.
    sw = ObjMesh("SM_Sidewalk_Extended_80m")
    # West segment: x in [-40.0, 16.5]
    sw.add_box(-40.0, 16.5, -0.15, 0.00, -3.8, 3.45, uv_scale=1.5, u_offset=0.0)
    sw.add_box(-40.0, 16.5, -0.15, 0.00, 3.45, 3.60, uv_scale=1.0, u_offset=0.0) # curb
    # Alley entrance threshold: x in [16.5, 20.5], z in [-3.8, 3.45]
    sw.add_box(16.5, 20.5, -0.15, 0.00, -3.8, 3.45, uv_scale=1.5, u_offset=(16.5+40)*1.5)
    sw.add_box(16.5, 20.5, -0.15, 0.00, 3.45, 3.60, uv_scale=1.0, u_offset=(16.5+40)*1.0) # curb
    # East segment: x in [20.5, 40.0]
    sw.add_box(20.5, 40.0, -0.15, 0.00, -3.8, 3.45, uv_scale=1.5, u_offset=(20.5+40)*1.5)
    sw.add_box(20.5, 40.0, -0.15, 0.00, 3.45, 3.60, uv_scale=1.0, u_offset=(20.5+40)*1.0) # curb
    sw.write_obj(os.path.join(out_dir, "SM_Sidewalk_Extended_80m.obj"))

    # 2. SM_North_Sidewalk_80m
    # North sidewalk across the road: x in [-40.0, 40.0], z in [12.6, 16.6], top at y: 0.00
    nsw = ObjMesh("SM_North_Sidewalk_80m")
    nsw.add_box(-40.0, 40.0, -0.15, 0.00, 12.6, 12.75, uv_scale=1.0) # south-facing curb
    nsw.add_box(-40.0, 40.0, -0.15, 0.00, 12.75, 16.6, uv_scale=1.5) # terrazzo pavement
    nsw.write_obj(os.path.join(out_dir, "SM_North_Sidewalk_80m.obj"))

    # 3. SM_Asphalt_Road_80m
    # 2-lane roadway: x in [-40.0, 40.0], z in [3.6, 12.6], road surface at y: -0.15m
    rd = ObjMesh("SM_Asphalt_Road_80m")
    # South gutter strip (z: 3.6 to 4.0, y: -0.165m)
    rd.add_box(-40.0, 40.0, -0.22, -0.165, 3.6, 4.0, uv_scale=1.0)
    # Asphalt surface (z: 4.0 to 12.2, y: -0.150m)
    rd.add_box(-40.0, 40.0, -0.22, -0.150, 4.0, 12.2, uv_scale=0.8)
    # North gutter strip (z: 12.2 to 12.6, y: -0.165m)
    rd.add_box(-40.0, 40.0, -0.22, -0.165, 12.2, 12.6, uv_scale=1.0)
    rd.write_obj(os.path.join(out_dir, "SM_Asphalt_Road_80m.obj"))

    # 4. SM_Alley_Extended
    # Side alley pavement: x in [16.5, 20.5], z in [-15.0, -3.8], top at y: 0.00m
    al = ObjMesh("SM_Alley_Extended")
    al.add_box(16.5, 20.5, -0.15, 0.00, -15.0, -3.8, uv_scale=1.5)
    al.write_obj(os.path.join(out_dir, "SM_Alley_Extended.obj"))

    # 5. SM_Alley_Walls
    # Alley flanking walls:
    # West wall: x in [16.4, 16.5], z in [-15.0, -3.8], height y in [0.0, 7.5]
    # East wall: x in [20.5, 20.6], z in [-15.0, -3.8], height y in [0.0, 7.5]
    # Back wall: z in [-15.1, -15.0], x in [16.4, 20.6], height y in [0.0, 7.5]
    aw = ObjMesh("SM_Alley_Walls")
    aw.add_box(16.4, 16.5, 0.0, 7.5, -15.0, -3.8, uv_scale=0.5)
    aw.add_box(20.5, 20.6, 0.0, 7.5, -15.0, -3.8, uv_scale=0.5)
    aw.add_box(16.4, 20.6, 0.0, 7.5, -15.1, -15.0, uv_scale=0.5)
    aw.write_obj(os.path.join(out_dir, "SM_Alley_Walls.obj"))

if __name__ == "__main__":
    target = "Assets/TramChanh/Art/Models/Environment"
    generate_all_extended_meshes(target)
