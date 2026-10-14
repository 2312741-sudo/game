#!/usr/bin/env python3
"""
Prefab footprint report without Unity.

Reads a .prefab (and the prefabs nested inside it), works out the world-space box of every mesh renderer
(OBJ meshes and Unity primitives) and prints, in the prefab's own frame:
  * every renderer box: x, y, z ranges in metres
  * --floating : renderers whose bottom is above a surface they should rest on (default surface y=0 or --surface-y)
  * --overlaps : pairs of renderers whose boxes overlap by more than the tolerance
This is a quick check to run before saying "done"; it does not replace looking at the result in the game.

Usage:
  python3 Automation/prefab_bounds.py Assets/TramChanh/Prefabs/Stall/PF_Stall_TramChanh.prefab
  python3 Automation/prefab_bounds.py <prefab> --overlaps --tolerance 0.02 --name-filter Cake
  python3 Automation/prefab_bounds.py <prefab> --floating --surface-y 1.0 --name-filter Tea

Notes that matter (they caused real bugs here):
  * Unity mirrors OBJ files on X when it imports them; this script applies the same mirror.
  * Meshes inside LFS must be pulled first (`git lfs pull`); an LFS pointer has no vertices and is skipped.
  * Scale on a nested prefab instance (m_Modifications m_LocalScale) is applied; rotation uses the quaternion.
"""
import argparse
import glob
import os
import re
import sys

PRIMITIVE_BOUNDS = {
    '10202': [(-.5, .5)] * 3,                       # cube
    '10207': [(-.5, .5)] * 3,                       # sphere
    '10206': [(-.5, .5), (-1, 1), (-.5, .5)],       # cylinder
    '10208': [(-.5, .5), (-1, 1), (-.5, .5)],       # capsule
    '10209': [(-5, 5), (0, 0), (-5, 5)],            # plane
    '10210': [(-.5, .5), (-.5, .5), (0, 0)],        # quad
}


def find_project_root(start):
    cur = os.path.abspath(start)
    while True:
        if os.path.isdir(os.path.join(cur, 'Assets')):
            return cur
        parent = os.path.dirname(cur)
        if parent == cur:
            sys.exit('Run this from inside the Unity project (a folder with Assets/).')
        cur = parent


class Project:
    def __init__(self, root):
        self.root = root
        self.guid_to_path = {}
        for meta in glob.glob(os.path.join(root, 'Assets', '**', '*.meta'), recursive=True):
            try:
                with open(meta, encoding='utf-8', errors='ignore') as f:
                    m = re.search(r'guid: (\w+)', f.read())
            except OSError:
                continue
            if m:
                self.guid_to_path[m.group(1)] = meta[:-5]
        self._obj_cache = {}

    def obj_bounds(self, path):
        if path in self._obj_cache:
            return self._obj_cache[path]
        verts = []
        with open(path, encoding='utf-8', errors='ignore') as f:
            for line in f:
                if line.startswith('v '):
                    verts.append(tuple(map(float, line.split()[1:4])))
        if not verts:
            self._obj_cache[path] = None          # LFS pointer or empty file
            return None
        b = [(min(c), max(c)) for c in zip(*verts)]
        b[0] = (-b[0][1], -b[0][0])               # Unity mirrors OBJ on X
        self._obj_cache[path] = b
        return b


def qrot(q, v):
    x, y, z, w = q
    px, py, pz = v
    ix = w * px + y * pz - z * py
    iy = w * py + z * px - x * pz
    iz = w * pz + x * py - y * px
    iw = -x * px - y * py - z * pz
    return (ix * w + iw * -x + iy * -z - iz * -y,
            iy * w + iw * -y + iz * -x - ix * -z,
            iz * w + iw * -z + ix * -y - iy * -x)


def apply(t, pt):
    pt = (pt[0] * t['s'][0], pt[1] * t['s'][1], pt[2] * t['s'][2])
    pt = qrot(t['r'], pt)
    return (pt[0] + t['p'][0], pt[1] + t['p'][1], pt[2] + t['p'][2])


class Prefab:
    def __init__(self, project, path):
        self.project = project
        self.path = path
        with open(path, encoding='utf-8', errors='ignore') as f:
            text = f.read()
        docs = re.split(r'(?m)^--- !u!(\d+) &(-?\d+)( stripped)?\s*$', text)
        self.go, self.tf, self.mesh, self.inst = {}, {}, {}, {}
        for i in range(1, len(docs), 4):
            kind, oid, stripped, body = docs[i], docs[i + 1], bool(docs[i + 2]), docs[i + 3]
            if kind == '1001':
                self.inst[oid] = body
            if stripped:
                continue
            if kind == '1':
                m = re.search(r'm_Name: (.*)', body)
                self.go[oid] = m.group(1) if m else ''
            elif kind == '4':
                g = re.search(r'm_GameObject: \{fileID: (-?\d+)', body)
                p = re.search(r'm_LocalPosition: \{x: ([-\de.]+), y: ([-\de.]+), z: ([-\de.]+)', body)
                r = re.search(r'm_LocalRotation: \{x: ([-\de.]+), y: ([-\de.]+), z: ([-\de.]+), w: ([-\de.]+)', body)
                s = re.search(r'm_LocalScale: \{x: ([-\de.]+), y: ([-\de.]+), z: ([-\de.]+)', body)
                fa = re.search(r'm_Father: \{fileID: (-?\d+)', body)
                if g and p and r and s and fa:
                    self.tf[oid] = dict(go=g.group(1), p=tuple(map(float, p.groups())), r=tuple(map(float, r.groups())),
                                        s=tuple(map(float, s.groups())), fa=fa.group(1))
            elif kind == '33':
                g = re.search(r'm_GameObject: \{fileID: (-?\d+)', body)
                m = re.search(r'm_Mesh: \{fileID: (-?\d+), guid: (\w+)', body)
                if g and m:
                    self.mesh[g.group(1)] = (m.group(1), m.group(2))

    def local_bounds(self, go_id):
        m = self.mesh.get(go_id)
        if not m:
            return None
        fid, guid = m
        if guid.startswith('0000000000000000e'):
            return PRIMITIVE_BOUNDS.get(fid)
        path = self.project.guid_to_path.get(guid)
        if path and path.lower().endswith('.obj') and os.path.exists(path):
            return self.project.obj_bounds(path)
        return None

    def chain(self, tf_id):
        out = []
        while tf_id in self.tf:
            out.append(self.tf[tf_id])
            tf_id = self.tf[tf_id]['fa']
        return out                                 # leaf ... root

    def boxes(self):
        """List of (name, (minx, maxx, miny, maxy, minz, maxz)) in this prefab's root frame."""
        result = []
        for tf_id, tf in self.tf.items():
            lb = self.local_bounds(tf['go'])
            if not lb:
                continue
            pts = []
            for cx in lb[0]:
                for cy in lb[1]:
                    for cz in lb[2]:
                        pt = (cx, cy, cz)
                        for t in self.chain(tf_id):
                            pt = apply(t, pt)
                        pts.append(pt)
            result.append((self.go.get(tf['go'], '?'), tuple(v for c in zip(*pts) for v in (min(c), max(c)))))
        for body in self.inst.values():
            src = re.search(r'm_SourcePrefab: \{fileID: \d+, guid: (\w+)', body)
            path = self.project.guid_to_path.get(src.group(1)) if src else None
            if not path or not path.endswith('.prefab') or not os.path.exists(path):
                continue
            mods = dict(re.findall(r'propertyPath: (\S+)\n\s+value: ([^\n]*)', body))
            num = lambda k, d: float(mods.get(k, d))
            inst_t = dict(p=(num('m_LocalPosition.x', 0), num('m_LocalPosition.y', 0), num('m_LocalPosition.z', 0)),
                          r=(num('m_LocalRotation.x', 0), num('m_LocalRotation.y', 0), num('m_LocalRotation.z', 0), num('m_LocalRotation.w', 1)),
                          s=(num('m_LocalScale.x', 1), num('m_LocalScale.y', 1), num('m_LocalScale.z', 1)))
            parent = re.search(r'm_TransformParent: \{fileID: (-?\d+)', body)
            parents = self.chain(parent.group(1)) if parent else []
            for name, b in Prefab(self.project, path).boxes():
                pts = []
                for x in b[0:2]:
                    for y in b[2:4]:
                        for z in b[4:6]:
                            pt = apply(inst_t, (x, y, z))
                            for t in parents:
                                pt = apply(t, pt)
                            pts.append(pt)
                result.append((os.path.basename(path)[:-7] + '/' + name,
                               tuple(v for c in zip(*pts) for v in (min(c), max(c)))))
        return result


def overlap(a, b):
    ox = min(a[1], b[1]) - max(a[0], b[0])
    oy = min(a[3], b[3]) - max(a[2], b[2])
    oz = min(a[5], b[5]) - max(a[4], b[4])
    return ox, oy, oz


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('prefab')
    ap.add_argument('--overlaps', action='store_true', help='list pairs of boxes that overlap in all three axes')
    ap.add_argument('--floating', action='store_true', help='list boxes whose bottom is above --surface-y')
    ap.add_argument('--surface-y', type=float, default=0.0)
    ap.add_argument('--tolerance', type=float, default=0.02, help='metres; overlaps and gaps smaller than this are ignored')
    ap.add_argument('--name-filter', default='', help='only report boxes whose name contains this text')
    args = ap.parse_args()

    root = find_project_root(os.path.dirname(os.path.abspath(args.prefab)))
    prefab = Prefab(Project(root), os.path.abspath(args.prefab))
    boxes = [(n, b) for n, b in prefab.boxes() if args.name_filter in n]
    if not boxes:
        print('No renderer boxes found (meshes may be LFS pointers: run `git lfs pull`).')
        return 1
    for name, b in sorted(boxes, key=lambda x: x[0]):
        print('%-60s x %6.2f..%5.2f  y %6.2f..%5.2f  z %6.2f..%5.2f' % ((name[:60],) + b))
    status = 0
    if args.floating:
        print('\nFloating (bottom above y=%.2f by more than %.2f m):' % (args.surface_y, args.tolerance))
        for name, b in boxes:
            gap = b[2] - args.surface_y
            if gap > args.tolerance:
                print('  %-58s gap %.2f m' % (name[:58], gap))
                status = 2
    if args.overlaps:
        print('\nOverlaps (more than %.2f m in every axis):' % args.tolerance)
        for i in range(len(boxes)):
            for j in range(i + 1, len(boxes)):
                ox, oy, oz = overlap(boxes[i][1], boxes[j][1])
                if min(ox, oy, oz) > args.tolerance:
                    print('  %s  <->  %s  (%.2f x %.2f x %.2f)' % (boxes[i][0], boxes[j][0], ox, oy, oz))
                    status = 2
    return status


if __name__ == '__main__':
    sys.exit(main())
