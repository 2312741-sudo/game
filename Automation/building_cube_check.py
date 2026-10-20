#!/usr/bin/env python3
"""Checks that building body meshes are solid cubes (no recess, no opening, no hollow inside).

A mesh may hold several separate parts (for example a row of buildings). Every separate part must be a solid cube:
  1. its vertices sit on exactly 8 distinct corner positions (a single cuboid, no recess or opening),
  2. it has all 6 sides and every side points outward,
  3. it has no extra faces inside the volume.
Parts are found by shared vertices, so a part that touches another only by position is still a separate part.

Usage:
  python3 Automation/building_cube_check.py                 # default building meshes
  python3 Automation/building_cube_check.py path/to/SM_X.obj ...
Exit code 1 when any mesh fails. Run `git lfs pull` first (OBJ files are stored in Git LFS).
"""
import glob
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ENV = os.path.join(ROOT, "Assets", "TramChanh", "Art", "Models", "Environment")
DEFAULTS = ["SM_Building_Tower_*.obj", "SM_Shopfront_[A-Z].obj", "SM_Alley_Walls.obj", "SM_Alley_Extended.obj", "SM_WorldEdge_*.obj", "SM_Boundary_*.obj", "SM_Backdrop_*.obj"]


def read_obj(path):
    verts, faces = [], []
    with open(path, encoding="utf-8", errors="replace") as handle:
        for line in handle:
            if line.startswith("v "):
                verts.append(tuple(float(x) for x in line.split()[1:4]))
            elif line.startswith("f "):
                faces.append([int(t.split("/")[0]) - 1 for t in line.split()[1:]])
    return verts, faces


def components(verts, faces):
    """Groups faces that share vertex positions into separate parts."""
    key = [tuple(round(c, 3) for c in v) for v in verts]
    parent = {}

    def find(x):
        while parent.setdefault(x, x) != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x

    for face in faces:
        base = find(key[face[0]])
        for idx in face[1:]:
            parent[find(key[idx])] = base
    parts = {}
    for face in faces:
        parts.setdefault(find(key[face[0]]), []).append(face)
    return list(parts.values())


def check_part(verts, faces, label):
    problems = []
    used = sorted({i for f in faces for i in f})
    pv = [verts[i] for i in used]
    corners = {tuple(round(c, 3) for c in v) for v in pv}
    if len(corners) != 8:
        problems.append("%s: %d distinct corner positions, a solid cube has 8 (recess, opening or extra parts)" % (label, len(corners)))
    lo = [min(v[i] for v in pv) for i in range(3)]
    hi = [max(v[i] for v in pv) for i in range(3)]
    seen = set()
    for face in faces:
        a, b, c = verts[face[0]], verts[face[1]], verts[face[2]]
        u = [b[i] - a[i] for i in range(3)]
        w = [c[i] - a[i] for i in range(3)]
        n = [u[1] * w[2] - u[2] * w[1], u[2] * w[0] - u[0] * w[2], u[0] * w[1] - u[1] * w[0]]
        axis = max(range(3), key=lambda i: abs(n[i]))
        sign = 1 if n[axis] > 0 else -1
        plane = a[axis]
        on_lo = abs(plane - lo[axis]) < 0.002
        on_hi = abs(plane - hi[axis]) < 0.002
        if not (on_lo or on_hi):
            problems.append("%s: face inside the volume on axis %s" % (label, "xyz"[axis]))
            continue
        if not ((sign > 0 and on_hi) or (sign < 0 and on_lo)):
            problems.append("%s: face on axis %s points inward (looks hollow from outside)" % (label, "xyz"[axis]))
        seen.add((axis, on_hi))
    if len(seen) != 6:
        problems.append("%s: only %d of 6 sides are closed" % (label, len(seen)))
    return problems


def check(path):
    verts, faces = read_obj(path)
    if not verts:
        return ["no vertices (Git LFS pointer? run git lfs pull)"]
    problems = []
    for number, part in enumerate(components(verts, faces), 1):
        problems.extend(check_part(verts, part, "part %d" % number))
    return sorted(set(problems))[:12]


def main(argv):
    paths = argv[1:]
    if not paths:
        for pattern in DEFAULTS:
            paths.extend(sorted(glob.glob(os.path.join(ENV, pattern))))
    failed = 0
    for path in paths:
        problems = check(path)
        name = os.path.relpath(path, ROOT)
        if problems:
            failed += 1
            print("FAIL %s" % name)
            for problem in problems:
                print("     - %s" % problem)
        else:
            print("PASS %s" % name)
    print("%d mesh(es) checked, %d failed" % (len(paths), failed))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
