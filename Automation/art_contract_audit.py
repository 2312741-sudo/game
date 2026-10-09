#!/usr/bin/env python3
"""Read-only art integration contract audit for Tram Chanh prefabs (MAIN-104).

Parses Unity YAML prefabs (no Unity, Python 3 stdlib only), expands nested prefabs and
prefab variants, and checks the gameplay contract that art swaps must preserve:

  * required anchors / load-bearing child names (paths relative to the prefab root)
  * gameplay components present (resolved from script GUIDs via *.cs.meta)
  * collider layers (8 Environment, 9 Interactable, 10 Player, 11 NPC, 12 HeldItem)
  * every project MonoBehaviour script GUID resolves to an existing .cs file
  * serialized gameplay references still point at the expected child
  * optional TABLE_01..TABLE_10 environment anchors (TABLE_ANCHOR_CONTRACT.md)

Exit code 0 = no FAIL (WARN lines are known gaps), 1 = at least one FAIL, 2 = usage error.

Usage:
  python3 Automation/art_contract_audit.py [REPO_ROOT]
  python3 Automation/art_contract_audit.py [REPO_ROOT] --dump Assets/.../PF_X.prefab
  python3 Automation/art_contract_audit.py [REPO_ROOT] --only PF_CakeStation --verbose

Contract source: Docs/Coordination/ART_INTEGRATION_HANDOFF.md. The tool never writes files.
"""
import argparse
import copy
import os
import re
import sys

MASK = 0x7FFFFFFFFFFFFFFF
LAYERS = {8: "Environment", 9: "Interactable", 10: "Player", 11: "NPC", 12: "HeldItem"}
COLLIDERS = {"BoxCollider", "SphereCollider", "CapsuleCollider", "MeshCollider", "CharacterController"}
RENDERERS = {"MeshRenderer", "SkinnedMeshRenderer"}

# ---------------------------------------------------------------------------
# Minimal Unity YAML reader (the subset Unity writes for prefabs)
# ---------------------------------------------------------------------------

HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)( stripped)?")
KEY = re.compile(r"^([^\s:][^:]*?):(?: (.*))?$")


def _flow_map(text):
    text = text.strip()[1:-1]
    out = {}
    for part in text.split(","):
        if ":" in part:
            k, v = part.split(":", 1)
            out[k.strip()] = v.strip()
    return out


def _scalar(text):
    text = text.strip()
    if text.startswith("{") and text.endswith("}"):
        return _flow_map(text)
    if text == "[]":
        return []
    if len(text) >= 2 and text[0] == text[-1] and text[0] in "'\"":
        return text[1:-1]
    return text


def _parse_block(lines, i, indent):
    """Parse a block mapping or sequence starting at lines[i] with the given indent."""
    if i >= len(lines):
        return None, i
    ind, content = lines[i]
    if ind == indent and (content.startswith("- ") or content == "-"):
        seq = []
        while i < len(lines) and lines[i][0] == indent and (lines[i][1].startswith("- ") or lines[i][1] == "-"):
            item = lines[i][1][2:]
            m = KEY.match(item)
            if m and not item.startswith("{"):
                # Mapping item: first key inline, the rest at indent + 2.
                sub = [(indent + 2, item)]
                j = i + 1
                while j < len(lines) and lines[j][0] > indent:
                    sub.append(lines[j])
                    j += 1
                value, _ = _parse_block(sub, 0, indent + 2)
                seq.append(value)
                i = j
            else:
                j = i + 1
                text = item
                while j < len(lines) and lines[j][0] > indent:
                    text += " " + lines[j][1]
                    j += 1
                seq.append(_scalar(text))
                i = j
        return seq, i
    mapping = {}
    while i < len(lines) and lines[i][0] == indent and not lines[i][1].startswith("- "):
        content = lines[i][1]
        m = KEY.match(content)
        if not m:
            i += 1
            continue
        key, rest = m.group(1), m.group(2)
        i += 1
        if rest is None or rest == "":
            if i < len(lines) and (lines[i][0] > indent or (lines[i][0] == indent and lines[i][1].startswith("- "))):
                value, i = _parse_block(lines, i, lines[i][0])
            else:
                value = None if rest is None else ""
        else:
            text = rest
            # A scalar cannot own children: deeper lines are wrapped continuation text.
            while i < len(lines) and lines[i][0] > indent:
                text += " " + lines[i][1]
                i += 1
            value = _scalar(text)
        mapping[key] = value
    return mapping, i


def parse_unity_yaml(path):
    """Return {fileID: Doc} for one Unity YAML asset."""
    docs = {}
    with open(path, encoding="utf-8", errors="replace") as handle:
        raw = handle.read().split("\n")
    current = None
    body = []

    def flush():
        if current is None:
            return
        cls_id, file_id, stripped = current
        lines = [(len(l) - len(l.lstrip(" ")), l.strip()) for l in body if l.strip()]
        if not lines:
            return
        type_name = lines[0][1].rstrip(":")
        data, _ = _parse_block(lines, 1, 2) if len(lines) > 1 else ({}, 1)
        docs[file_id] = Doc(int(cls_id), file_id, bool(stripped), type_name, data or {})

    for line in raw:
        m = HEADER.match(line)
        if m:
            flush()
            current = (m.group(1), int(m.group(2)), m.group(3))
            body = []
        elif current is not None:
            body.append(line)
    flush()
    return docs


class Doc:
    def __init__(self, cls_id, file_id, stripped, type_name, data):
        self.cls_id, self.file_id, self.stripped, self.type, self.data = cls_id, file_id, stripped, type_name, data


def ref_id(value):
    if isinstance(value, dict) and "fileID" in value:
        try:
            return int(value["fileID"])
        except ValueError:
            return 0
    return 0


# ---------------------------------------------------------------------------
# Project index (GUID -> asset path)
# ---------------------------------------------------------------------------


class Project:
    def __init__(self, root):
        self.root = root
        self.guid_to_path = {}
        self.script_guid = {}
        for base, _dirs, files in os.walk(os.path.join(root, "Assets")):
            for name in files:
                if not name.endswith(".meta"):
                    continue
                meta = os.path.join(base, name)
                guid = None
                with open(meta, encoding="utf-8", errors="replace") as handle:
                    for line in handle:
                        if line.startswith("guid:"):
                            guid = line.split(":", 1)[1].strip()
                            break
                if not guid:
                    continue
                asset = meta[:-5]
                self.guid_to_path[guid] = os.path.relpath(asset, root)
                if asset.endswith(".cs"):
                    self.script_guid[guid] = os.path.splitext(os.path.basename(asset))[0]
        self._parsed = {}

    def parsed(self, rel):
        if rel not in self._parsed:
            self._parsed[rel] = parse_unity_yaml(os.path.join(self.root, rel))
        return self._parsed[rel]

    def asset_name(self, value):
        """Human name for an object reference {fileID, guid}."""
        if not isinstance(value, dict):
            return str(value)
        guid = value.get("guid")
        fid = value.get("fileID", "0")
        if fid == "0":
            return "None"
        if not guid:
            return "local:" + fid
        if guid == "0000000000000000e000000000000000" or guid == "0000000000000000f000000000000000":
            return {"10202": "builtin Cube", "10206": "builtin Cylinder", "10207": "builtin Sphere", "10208": "builtin Capsule",
                    "10209": "builtin Plane", "10210": "builtin Quad", "10303": "builtin Default-Material",
                    "10302": "builtin Default-Diffuse"}.get(fid, "builtin " + fid)
        path = self.guid_to_path.get(guid)
        return os.path.basename(path) if path else "MISSING-GUID " + guid


# ---------------------------------------------------------------------------
# Expanded prefab tree
# ---------------------------------------------------------------------------


class Node:
    def __init__(self, name, layer, active):
        self.name, self.layer, self.active = name, layer, active
        self.parent = None
        self.children = []
        self.components = []
        self.transform = None
        self.source = ""  # prefab the node was authored in

    def path(self, root):
        if self is root:
            return "."
        parts = []
        node = self
        while node is not None and node is not root:
            parts.append(node.name)
            node = node.parent
        return "/".join(reversed(parts))

    def walk(self):
        yield self
        for child in self.children:
            yield from child.walk()


class Comp:
    def __init__(self, doc_type, data, project):
        self.type = doc_type
        self.data = data
        self.node = None
        self.script = None
        self.script_guid = None
        self.scope_map = None
        if doc_type == "MonoBehaviour":
            script = data.get("m_Script") or {}
            self.script_guid = script.get("guid") if isinstance(script, dict) else None
            self.script = project.script_guid.get(self.script_guid) if self.script_guid else None

    @property
    def label(self):
        if self.type == "MonoBehaviour":
            return self.script or ("UnresolvedScript(" + str(self.script_guid) + ")")
        return self.type


def _set_path(data, path, value):
    """Apply one PrefabInstance modification (supports 'a.b.c' and 'list.Array.data[i]' / '.Array.size')."""
    parts = path.split(".")
    target = data
    i = 0
    while i < len(parts):
        part = parts[i]
        last = i == len(parts) - 1
        if part == "Array" and isinstance(target, list) and i + 1 < len(parts):
            nxt = parts[i + 1]
            if nxt == "size":
                try:
                    size = int(value)
                except (TypeError, ValueError):
                    return
                del target[size:]
                while len(target) < size:
                    target.append({"fileID": "0"})
                return
            m = re.match(r"data\[(\d+)\]", nxt)
            if not m:
                return
            index = int(m.group(1))
            while len(target) <= index:
                target.append({"fileID": "0"})
            if i + 1 == len(parts) - 1:
                target[index] = value
                return
            if not isinstance(target[index], dict):
                target[index] = {}
            target = target[index]
            i += 2
            continue
        if not isinstance(target, dict):
            return
        if last:
            target[part] = value
            return
        nxt = target.get(part)
        if parts[i + 1] == "Array":
            if not isinstance(nxt, list):
                nxt = []
                target[part] = nxt
        elif not isinstance(nxt, dict):
            nxt = {}
            target[part] = nxt
        target = nxt
        i += 1


def expand(project, rel, depth=0):
    """Expand a prefab into (roots, idmap) where idmap maps this file's fileIDs to Node/Comp objects."""
    if depth > 12:
        raise RuntimeError("prefab nesting too deep at " + rel)
    docs = project.parsed(rel)
    idmap = {}
    instance_roots = []  # (root node, transform parent id)

    for doc in docs.values():
        if doc.type != "PrefabInstance":
            continue
        mod = doc.data.get("m_Modification") or {}
        source = doc.data.get("m_SourcePrefab") or {}
        src_rel = project.guid_to_path.get(source.get("guid", ""))
        if not src_rel:
            raise RuntimeError(rel + ": nested prefab GUID " + str(source.get("guid")) + " does not resolve")
        sub_roots, sub_map = expand(project, src_rel, depth + 1)
        for key, obj in sub_map.items():
            idmap[(doc.file_id ^ key) & MASK] = obj
        for change in mod.get("m_Modifications") or []:
            target = sub_map.get(ref_id(change.get("target")))
            prop = change.get("propertyPath", "")
            obj_ref = change.get("objectReference")
            value = obj_ref if ref_id(obj_ref) else change.get("value")
            if isinstance(target, Node):
                if prop == "m_Name":
                    target.name = str(value)
                elif prop == "m_Layer":
                    target.layer = int(value)
                elif prop == "m_IsActive":
                    target.active = str(value) == "1"
            elif isinstance(target, Comp):
                if isinstance(value, dict) and ref_id(value) and "guid" not in value:
                    value = {"fileID": str(ref_id(value)), "_map": idmap}
                _set_path(target.data, prop, value)
        for removed in mod.get("m_RemovedComponents") or []:
            comp = sub_map.get(ref_id(removed))
            if isinstance(comp, Comp) and comp.node and comp in comp.node.components:
                comp.node.components.remove(comp)
        for removed in mod.get("m_RemovedGameObjects") or []:
            node = sub_map.get(ref_id(removed))
            if isinstance(node, Node) and node.parent:
                node.parent.children.remove(node)
                node.parent = None
        parent_id = ref_id(mod.get("m_TransformParent"))
        for root in sub_roots:
            instance_roots.append((root, parent_id))

    for doc in docs.values():
        if doc.stripped or doc.type == "PrefabInstance":
            continue
        if doc.type == "GameObject":
            node = Node(doc.data.get("m_Name", ""), int(doc.data.get("m_Layer", 0) or 0), str(doc.data.get("m_IsActive", "1")) == "1")
            node.source = rel
            idmap[doc.file_id] = node
    for doc in docs.values():
        if doc.stripped or doc.type in ("PrefabInstance", "GameObject"):
            continue
        if "m_GameObject" not in doc.data:
            continue
        comp = Comp(doc.type, copy.deepcopy(doc.data), project)
        comp.scope_map = idmap
        idmap[doc.file_id] = comp
        owner = idmap.get(ref_id(doc.data.get("m_GameObject")))
        if isinstance(owner, Node):
            comp.node = owner
            owner.components.append(comp)
            if doc.type in ("Transform", "RectTransform"):
                owner.transform = comp
    for doc in docs.values():
        if doc.stripped or doc.type not in ("Transform", "RectTransform"):
            continue
        comp = idmap.get(doc.file_id)
        father = idmap.get(ref_id(doc.data.get("m_Father")))
        if comp is None or comp.node is None:
            continue
        if isinstance(father, Comp) and father.node is not None:
            comp.node.parent = father.node
            father.node.children.append(comp.node)
    for root, parent_id in instance_roots:
        father = idmap.get(parent_id)
        if isinstance(father, Comp) and father.node is not None:
            root.parent = father.node
            father.node.children.append(root)

    roots = []
    seen = set()
    for obj in idmap.values():
        if isinstance(obj, Node) and obj.parent is None and id(obj) not in seen:
            # Detached nodes removed via m_RemovedGameObjects are not roots of this file.
            seen.add(id(obj))
            roots.append(obj)
    roots = [r for r in roots if r.transform is not None]
    top = [r for r in roots if any(r is ir for ir, _ in instance_roots) or r.source == rel]
    return (top or roots), idmap


class Prefab:
    def __init__(self, project, rel):
        self.project = project
        self.rel = rel
        roots, self.idmap = expand(project, rel)
        if len(roots) != 1:
            raise RuntimeError(rel + ": expected exactly one root, found " + str(len(roots)))
        self.root = roots[0]

    def find(self, path):
        """transform.Find semantics: '/'-separated direct-child names from the root."""
        node = self.root
        for part in path.split("/"):
            node = next((c for c in node.children if c.name == part), None)
            if node is None:
                return None
        return node

    def find_all(self, name):
        return [n for n in self.root.walk() if n.name == name]

    def components(self, label):
        return [c for n in self.root.walk() for c in n.components if c.label == label]

    def resolve(self, comp, field):
        """Node a serialized Transform/GameObject/Component field points at (None if unset/unresolved)."""
        return self.resolve_value(comp, comp.data.get(field))

    def _lookup(self, comp, fid, value=None):
        scope = value.get("_map") if isinstance(value, dict) and "_map" in value else comp.scope_map
        return scope.get(fid) if scope is not None else None

    def resolve_value(self, comp, value):
        fid = ref_id(value)
        if not fid:
            return None
        if isinstance(value, dict) and value.get("guid"):
            return "asset:" + self.project.asset_name(value)
        obj = self._lookup(comp, fid, value)
        if isinstance(obj, Node):
            return obj
        if isinstance(obj, Comp):
            return obj.node
        return None


# ---------------------------------------------------------------------------
# Description helpers (dump)
# ---------------------------------------------------------------------------


def _vec(value):
    if isinstance(value, dict):
        return "(" + ", ".join(str(value.get(k, "?")) for k in ("x", "y", "z") if k in value) + ")"
    return str(value)


def describe_component(project, comp):
    d = comp.data
    t = comp.type
    if t == "BoxCollider":
        return "BoxCollider size%s center%s trigger=%s" % (_vec(d.get("m_Size")), _vec(d.get("m_Center")), d.get("m_IsTrigger"))
    if t == "SphereCollider":
        return "SphereCollider r=%s trigger=%s" % (d.get("m_Radius"), d.get("m_IsTrigger"))
    if t == "CapsuleCollider":
        return "CapsuleCollider r=%s h=%s trigger=%s" % (d.get("m_Radius"), d.get("m_Height"), d.get("m_IsTrigger"))
    if t == "MeshCollider":
        return "MeshCollider mesh=%s convex=%s trigger=%s" % (project.asset_name(d.get("m_Mesh")), d.get("m_Convex"), d.get("m_IsTrigger"))
    if t == "CharacterController":
        return "CharacterController r=%s h=%s" % (d.get("m_Radius"), d.get("m_Height"))
    if t == "MeshFilter":
        return "MeshFilter " + project.asset_name(d.get("m_Mesh"))
    if t in RENDERERS:
        mats = d.get("m_Materials") or []
        return "%s mats=[%s] enabled=%s" % (t, ", ".join(project.asset_name(m) for m in mats), d.get("m_Enabled"))
    if t == "Animator":
        return "Animator controller=" + project.asset_name(d.get("m_Controller"))
    if t == "MonoBehaviour":
        return comp.label
    return t


def dump(prefab):
    project = prefab.project
    lines = [prefab.rel]
    for node in prefab.root.walk():
        depth = 0
        p = node
        while p is not prefab.root:
            depth += 1
            p = p.parent
        tr = node.transform.data if node.transform else {}
        head = "%s%s  [layer %s%s]" % ("  " * depth, node.name, LAYERS.get(node.layer, node.layer), "" if node.active else ", inactive")
        if tr:
            head += "  pos%s scale%s" % (_vec(tr.get("m_LocalPosition")), _vec(tr.get("m_LocalScale")))
        lines.append(head)
        for comp in node.components:
            if comp.type in ("Transform", "RectTransform"):
                continue
            text = describe_component(project, comp)
            if comp.type == "MonoBehaviour":
                refs = []
                for key, value in comp.data.items():
                    if key.startswith("m_"):
                        continue
                    if isinstance(value, dict) and ref_id(value):
                        target = prefab.resolve(comp, key)
                        refs.append("%s->%s" % (key, target.path(prefab.root) if isinstance(target, Node) else target))
                    elif isinstance(value, list) and value and all(isinstance(v, dict) for v in value):
                        names = []
                        for v in value:
                            target = prefab.resolve_value(comp, v)
                            names.append(target.path(prefab.root) if isinstance(target, Node) else str(target))
                        refs.append("%s->[%s]" % (key, ", ".join(names)))
                if refs:
                    text += " {" + "; ".join(refs) + "}"
            lines.append("%s  - %s" % ("  " * depth, text))
    return "\n".join(lines)



# ---------------------------------------------------------------------------
# Geometry (offline bounds for builtin primitives and BoxColliders)
# ---------------------------------------------------------------------------

BUILTIN_EXTENTS = {"10202": (0.5, 0.5, 0.5), "10206": (0.5, 1.0, 0.5), "10207": (0.5, 0.5, 0.5),
                   "10208": (0.5, 1.0, 0.5), "10210": (0.5, 0.5, 0.0), "10209": (5.0, 0.0, 5.0)}


def _f(value, default=0.0):
    try:
        return float(value)
    except (TypeError, ValueError):
        return default


def _trs(tr):
    p = tr.get("m_LocalPosition") or {}
    r = tr.get("m_LocalRotation") or {}
    sc = tr.get("m_LocalScale") or {}
    x, y, z, w = _f(r.get("x")), _f(r.get("y")), _f(r.get("z")), _f(r.get("w"), 1.0)
    sx, sy, sz = _f(sc.get("x"), 1.0), _f(sc.get("y"), 1.0), _f(sc.get("z"), 1.0)
    rot = [[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
           [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
           [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]]
    m = [[rot[i][0] * sx, rot[i][1] * sy, rot[i][2] * sz, _f(p.get(k))] for i, k in enumerate("xyz")]
    m.append([0.0, 0.0, 0.0, 1.0])
    return m


def _mul(a, b):
    return [[sum(a[i][k] * b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def world_matrix(node, root):
    """Matrix from node-local space to root-local space (root's own transform excluded)."""
    m = [[1.0 if i == j else 0.0 for j in range(4)] for i in range(4)]
    chain = []
    while node is not None and node is not root:
        chain.append(node)
        node = node.parent
    for n in reversed(chain):
        m = _mul(m, _trs(n.transform.data if n.transform else {}))
    return m


def _box_corners(m, center, ext):
    out = []
    for dx in (-1, 1):
        for dy in (-1, 1):
            for dz in (-1, 1):
                v = (center[0] + dx * ext[0], center[1] + dy * ext[1], center[2] + dz * ext[2])
                out.append(tuple(m[i][0] * v[0] + m[i][1] * v[1] + m[i][2] * v[2] + m[i][3] for i in range(3)))
    return out


def _aabb(points):
    if not points:
        return None
    return [min(p[i] for p in points) for i in range(3)], [max(p[i] for p in points) for i in range(3)]


def renderer_bounds(prefab, nodes, root=None):
    """(aabb, unknown_count) of enabled MeshRenderers under nodes; custom meshes are unknown offline."""
    root = root or prefab.root
    points, unknown = [], 0
    for top in nodes:
        for node in top.walk():
            if not node.active:
                continue
            rend = [c for c in node.components if c.type == "MeshRenderer" and str(c.data.get("m_Enabled", "1")) == "1"]
            if not rend:
                continue
            mf = next((c for c in node.components if c.type == "MeshFilter"), None)
            mesh = (mf.data.get("m_Mesh") or {}) if mf else {}
            ext = BUILTIN_EXTENTS.get(mesh.get("fileID")) if mesh.get("guid", "").startswith("0000000000000000") else None
            if ext is None:
                unknown += 1
                continue
            points += _box_corners(world_matrix(node, root), (0, 0, 0), ext)
    return _aabb(points), unknown


def collider_bounds(prefab, nodes, root=None):
    root = root or prefab.root
    points = []
    for top in nodes:
        for node in top.walk():
            for c in node.components:
                if c.type != "BoxCollider":
                    continue
                size, center = c.data.get("m_Size") or {}, c.data.get("m_Center") or {}
                ext = tuple(_f(size.get(k), 1.0) / 2 for k in "xyz")
                points += _box_corners(world_matrix(node, root), tuple(_f(center.get(k)) for k in "xyz"), ext)
    return _aabb(points)


# ---------------------------------------------------------------------------
# Animator bindings
# ---------------------------------------------------------------------------


def controller_info(project, controller_ref):
    """(parameter names, set of animated transform paths) for an AnimatorController reference."""
    rel = project.guid_to_path.get((controller_ref or {}).get("guid", ""))
    if not rel:
        return None
    params, paths = [], set()
    with open(os.path.join(project.root, rel), encoding="utf-8", errors="replace") as handle:
        text = handle.read()
    block = re.search(r"m_AnimatorParameters:\n((?:  - .*\n|    .*\n)*)", text)
    if block:
        params = re.findall(r"  - m_Name: (\S+)", block.group(1))
    for guid in set(re.findall(r"m_Motion: \{fileID: \d+, guid: ([0-9a-f]{32})", text)):
        clip = project.guid_to_path.get(guid)
        if not clip:
            paths.add("<missing clip " + guid + ">")
            continue
        with open(os.path.join(project.root, clip), encoding="utf-8", errors="replace") as handle:
            for line in handle:
                m = re.match(r"\s+path: (.*)$", line)
                if m and not re.fullmatch(r"\d+", m.group(1).strip()):
                    paths.add(m.group(1).strip())
    return params, paths


# ---------------------------------------------------------------------------
# Contract
# ---------------------------------------------------------------------------

P = "Assets/TramChanh/Prefabs/"

# Requested name -> (status, [paths], note). Used for the mapping table and for --only.
MAPPING = [
    ("PF_Stall_TramChanh", [P + "Stall/PF_Stall_TramChanh.prefab"], "visual + StallAnchorSet; nested in the environment under StallRoot"),
    ("PF_Sign_TramChanh_New", [P + "Stall/PF_Sign_TramChanh_New.prefab"], "visual-only; nested in the stall under Sign"),
    ("PF_RedTeaRack", [P + "Workstations/PF_RedTeaRack.prefab", P + "Workstations/DrinkWave/PF_RedTeaRack.prefab"], "base visual + DrinkWave gameplay variant"),
    ("PF_TeaBag_PrePortioned", [P + "Items/PF_TeaBag_PrePortioned.prefab"], "gameplay held item"),
    ("PF_ToppingStation", [P + "Workstations/PF_ToppingStation.prefab", P + "Workstations/DrinkWave/PF_ToppingStation.prefab"], "base visual + DrinkWave gameplay variant"),
    ("PF_IceBin", [P + "Workstations/PF_IceBin.prefab", P + "Workstations/DrinkWave/PF_IceBin.prefab"], "base visual + DrinkWave gameplay variant"),
    ("PF_IceScoop", [P + "Tools/PF_IceScoop.prefab"], "animated station tool"),
    ("PF_Grill_Elmich", [P + "Workstations/PF_Grill_Elmich.prefab"], "base visual; nested (with gameplay additions) in PF_CakeStation"),
    ("PF_BatterMeasureCup_500ml", [P + "ACCEL01/Cakes/PF_BatterMeasureCup_500ml.prefab"], "gameplay held item; nested in PF_CakeStation"),
    ("PF_Spatula_WoodHandle", [P + "ACCEL01/Cakes/PF_Spatula_WoodHandle.prefab"], "visual tool; moved by PlaceholderFlipAction"),
    ("PF_Scissors_RedGray", [P + "ACCEL01/Cakes/PF_Scissors_RedGray.prefab"], "visual tool; rotated by CakeStation"),
    ("PF_SauceBag", [P + "ACCEL01/Cakes/PF_SauceBag.prefab"], "visual tool; gameplay added in PF_CakeStation"),
    ("PF_CakeWrappingPaper", [P + "ACCEL01/Cakes/PF_CakeWrappingPaper.prefab"], "visual; nested under PF_CakeStation/WrappingArea"),
    ("PF_PlasticStool", [P + "CustomerArea/PF_PlasticStool.prefab"], "visual-only furniture"),
    ("PF_YellowCrateTable", [P + "CustomerArea/PF_YellowCrateTable.prefab", P + "Workstations/DrinkWave/PF_YellowCrateTable.prefab"], "visual furniture + DrinkWave gameplay variant (TableOrderPoint)"),
    ("PF_DrinkStation", [P + "Workstations/DrinkWave/PF_DrinkStation.prefab"], "gameplay group (instantiated by bootstrap)"),
    ("PF_CakeStation", [P + "ACCEL01/Cakes/PF_CakeStation.prefab"], "gameplay group (instantiated by bootstrap)"),
    ("PF_ReadyCounterPoint", [P + "Stall/PF_ReadyCounterPoint.prefab", P + "Workstations/DrinkWave/PF_ReadyCounterPoint.prefab"], "legacy blockout (no component) + DrinkWave gameplay variant"),
    ("PF_Placeholder_VehiclePoint", [P + "Workstations/DrinkWave/PF_Placeholder_VehiclePoint.prefab"], "gameplay point (renderers hidden by bootstrap)"),
    ("PF_Cake_Prepared", [P + "ACCEL01/Cakes/PF_Cake_Prepared.prefab"], "gameplay prepared item (spawned by CakeStation)"),
    ("PF_AccelRoadsideEnvironment", [P + "ACCEL01/Environment/PF_AccelRoadsideEnvironment.prefab"], "environment; composition anchors read by name"),
    ("PF_Player", [P + "NPC/PF_Player.prefab"], "player; HeldItemView + HoldAnchor are added in SCN_TramChanh_Main"),
]

F, W = "FAIL", "WARN"


def C(**kw):
    kw.setdefault("nodes", [])
    kw.setdefault("components", [])
    kw.setdefault("refs", [])
    kw.setdefault("collider_layers", [])
    kw.setdefault("custom", [])
    return kw


class Audit:
    def __init__(self, project, verbose=False):
        self.project = project
        self.verbose = verbose
        self.results = []  # (level, label, message)

    def add(self, level, label, message):
        self.results.append((level, label, message))

    # -- generic checks -----------------------------------------------------
    def run(self, label, rel, contract):
        full = os.path.join(self.project.root, rel)
        if not os.path.isfile(full):
            self.add(F, label, "prefab missing: " + rel)
            return
        try:
            prefab = Prefab(self.project, rel)
        except Exception as error:  # parsing problems are contract failures
            self.add(F, label, "cannot parse/expand: %s" % error)
            return
        self.add("OK", label, "parsed (%d nodes)" % sum(1 for _ in prefab.root.walk()))
        self.check_scripts(label, prefab)
        root_tr = prefab.root.transform.data if prefab.root.transform else {}
        scale = root_tr.get("m_LocalScale") or {}
        if any(abs(_f(scale.get(k), 1.0) - 1.0) > 1e-6 for k in "xyz"):
            self.add(W, label, "root scale is not (1,1,1): scale belongs on Visual children, never on the root")
        for path, level, why in contract["nodes"]:
            node = prefab.find(path)
            self.add("OK" if node else level, label, ("child '%s' present" if node else "missing child '%s'") % path + ("" if node else " - " + why))
        for path, cls, level in contract["components"]:
            node = prefab.root if path == "." else prefab.find(path)
            ok = node is not None and any(c.label == cls for c in node.components)
            self.add("OK" if ok else level, label, "%s on '%s'" % (cls, path) + ("" if ok else " is missing"))
        for path, cls, field, expected in contract["refs"]:
            self.check_ref(label, prefab, path, cls, field, expected)
        for prefix, allowed, level in contract["collider_layers"]:
            base = prefab.root if prefix == "." else prefab.find(prefix)
            if base is None:
                continue
            for node in base.walk():
                if any(c.type in COLLIDERS for c in node.components) and node.layer not in allowed:
                    self.add(level, label, "collider on '%s' is on layer %s; expected %s" % (
                        node.path(prefab.root), LAYERS.get(node.layer, node.layer), "/".join(LAYERS.get(a, str(a)) for a in sorted(allowed))))
        if contract.get("visual_root") is not None:
            vr = prefab.find(contract["visual_root"])
            if vr is None:
                self.add(W, label, "target-structure gap: no '%s' root for replaceable art (proposed Claude-side restructure, issue #20)" % contract["visual_root"])
        if contract.get("focus"):
            self.check_focus(label, prefab, contract.get("focus"))
        self.check_animators(label, prefab)
        for fn in contract["custom"]:
            fn(self, label, prefab)

    def check_scripts(self, label, prefab):
        bad = 0
        for node in prefab.root.walk():
            for comp in node.components:
                if comp.type == "MonoBehaviour" and comp.script is None:
                    bad += 1
                    self.add(F, label, "MonoBehaviour on '%s' references script GUID %s with no .cs in Assets" % (node.path(prefab.root), comp.script_guid))
        if not bad:
            self.add("OK", label, "all MonoBehaviour script GUIDs resolve")

    def check_ref(self, label, prefab, path, cls, field, expected):
        node = prefab.root if path == "." else prefab.find(path)
        comp = next((c for c in node.components if c.label == cls), None) if node else None
        if comp is None:
            self.add(F, label, "%s.%s: component %s not found on '%s'" % (cls, field, cls, path))
            return
        value = comp.data.get(field)
        if isinstance(expected, list):
            got = []
            for v in value or []:
                t = prefab.resolve_value(comp, v)
                got.append(t.path(prefab.root) if isinstance(t, Node) else str(t))
            ok = got == expected
            shown = "[" + ", ".join(got) + "]"
            want = "[" + ", ".join(expected) + "]"
        else:
            t = prefab.resolve_value(comp, value)
            shown = t.path(prefab.root) if isinstance(t, Node) else str(t)
            want = expected
            ok = shown == expected
        self.add("OK" if ok else F, label, "%s.%s -> %s%s" % (cls, field, shown, "" if ok else " (expected %s)" % want))

    def check_focus(self, label, prefab, mode):
        """PlayerInteractor raycasts layer 9 only and takes GetComponentInParent<InteractableRef>()."""
        refs = [n for n in prefab.root.walk() if any(c.label == "InteractableRef" for c in n.components)]
        for ref_node in refs:
            comp = next(c for c in ref_node.components if c.label == "InteractableRef")
            target = prefab.resolve(comp, "_behaviour")
            if target is None:
                self.add(F, label, "InteractableRef on '%s' has no _behaviour" % ref_node.path(prefab.root))
            hits = [n for n in ref_node.walk() if n.layer == 9 and n.active and any(c.type in COLLIDERS for c in n.components)
                    and _nearest_ref(n) is ref_node]
            if hits:
                self.add("OK", label, "focusable: '%s' has %d Interactable-layer collider node(s)" % (ref_node.path(prefab.root), len(hits)))
            else:
                self.add(F, label, "InteractableRef on '%s' has no collider on layer Interactable below it: the player cannot focus it" % ref_node.path(prefab.root))
        if mode == "orphans":
            orphans = [n for n in prefab.root.walk() if n.layer == 9 and any(c.type in COLLIDERS for c in n.components) and _nearest_ref(n) is None]
            if orphans:
                self.add(W, label, "%d Interactable-layer collider node(s) have no InteractableRef ancestor (block the interaction ray, cannot be focused): %s" % (
                    len(orphans), ", ".join(sorted({o.path(prefab.root) for o in orphans})[:6]) + (" ..." if len(orphans) > 6 else "")))

    def check_animators(self, label, prefab):
        for node in prefab.root.walk():
            for comp in node.components:
                if comp.type != "Animator":
                    continue
                info = controller_info(self.project, comp.data.get("m_Controller"))
                where = node.path(prefab.root)
                if info is None:
                    self.add(W, label, "Animator on '%s' has no resolvable controller" % where)
                    continue
                params, paths = info
                for path in sorted(paths):
                    base = node
                    found = base
                    for part in path.split("/"):
                        found = next((c for c in found.children if c.name == part), None) if found else None
                    self.add("OK" if found else F, label, "animation binding '%s' (relative to Animator on '%s')%s" % (
                        path, where, "" if found else " has no matching child: clip will animate nothing"))
                if any(c.label == "StationToolView" for c in node.components):
                    need = {"Active", "Action"} & set(params)
                    if not need:
                        self.add(F, label, "StationToolView on '%s': controller has neither 'Active' (bool) nor 'Action' (trigger)" % where)
                if any(c.label == "TeaBagStateView" for c in node.components) and "Shaking" not in params:
                    self.add(F, label, "TeaBagStateView animator on '%s' lacks bool parameter 'Shaking'" % where)


def _nearest_ref(node):
    while node is not None:
        if any(c.label == "InteractableRef" for c in node.components):
            return node
        node = node.parent
    return None


# -- custom checks -------------------------------------------------------------

STALL_ANCHORS = {0: "TeaRackAnchor", 1: "ToppingAnchor", 2: "IceBinAnchor", 3: "WipeAreaAnchor", 4: "GrillAnchor",
                 5: "BatterAreaAnchor", 6: "RollAreaAnchor", 7: "SauceAnchor", 8: "WrapAnchor", 9: "ReadyCounterAnchor"}
ENV_ANCHORS = ["StallRoot", "PlayerSpawn", "LobbyPosition", "ReadyHandoff", "TablePoint", "CakeTablePoint", "VehiclePoint"]


def _close(a, b, tol):
    return abs(a - b) <= tol


def stall_checks(audit, label, prefab, stall=None, root=None):
    stall = stall or prefab.root
    anchors = [n for n in stall.walk() if any(c.label == "StallAnchor" for c in n.components)]
    ids = {}
    for n in anchors:
        comp = next(c for c in n.components if c.label == "StallAnchor")
        ids[int(_f(comp.data.get("_id"), 0))] = n
        if str(comp.data.get("_positionConfirmed", "0")) == "1":
            audit.add(W, label, "StallAnchor '%s' is PositionConfirmed (only allowed after DEC-011 measurement)" % n.name)
        if n.children:
            audit.add(W, label, "StallAnchor '%s' has children %s: TramChanhMainBootstrap deactivates every child of a stall anchor at runtime" % (
                n.name, [c.name for c in n.children]))
    missing = [STALL_ANCHORS[i] for i in STALL_ANCHORS if i not in ids]
    audit.add(F if missing else "OK", label, "StallAnchor ids 0..9 present" + (" - missing %s" % missing if missing else ""))
    for i, n in ids.items():
        if STALL_ANCHORS.get(i) and n.name != STALL_ANCHORS[i]:
            audit.add(W, label, "StallAnchor id %d is named '%s' (canonical '%s'); lookup is by id, name is informational" % (i, n.name, STALL_ANCHORS[i]))
    for node in stall.walk():
        if any(c.type in COLLIDERS for c in node.components) and node.layer != 8:
            audit.add(F, label, "stall collider on '%s' is on layer %s (must be Environment: an Interactable-layer stall collider blocks the interaction ray)" % (
                node.path(stall), LAYERS.get(node.layer, node.layer)))
    cols = stall.children and next((c for c in stall.children if c.name == "Colliders"), None)
    if cols is not None:
        lo, hi = collider_bounds(prefab, [cols], root=stall)
        size = [hi[i] - lo[i] for i in range(3)]
        ok = _close(size[0], 1.8, 0.02) and _close(size[2], 0.8, 0.02) and _close(hi[1], 2.2, 0.05) and _close(lo[1], 0.1, 0.11)
        audit.add("OK" if ok else F, label, "stall collider envelope W %.3f x D %.3f, top %.3f m (GT-001: 1.8 x 0.8, ~2.2)" % (size[0], size[2], hi[1]))
        body = next((c for c in cols.children if c.name == "Body"), None)
        if body is not None:
            blo, bhi = collider_bounds(prefab, [body], root=stall)
            audit.add("OK" if _close(bhi[1], 1.0, 0.01) else F, label, "Colliders/Body top at %.3f m (counter 1.00 m)" % bhi[1])
    groups = [g for g in (next((c for c in stall.children if c.name == n), None) for n in ("Structure", "Counter", "Frame", "Roof", "Wheels")) if g]
    box, unknown = renderer_bounds(prefab, groups, root=stall)
    if unknown:
        audit.add(W, label, "%d custom-mesh renderer(s) in Structure/Counter/Frame/Roof/Wheels: bounds cannot be checked offline - run the GT-001 EditMode tests in Unity" % unknown)
    if box:
        lo, hi = box
        size = [hi[i] - lo[i] for i in range(3)]
        ok = _close(size[0], 1.8, 0.02) and _close(size[2], 0.8, 0.02) and _close(hi[1], 2.2, 0.05) and _close(lo[1], 0.0, 0.01)
        audit.add("OK" if ok or unknown else F, label, "stall renderer bounds %.3f x %.3f x %.3f m (min y %.3f) - GT-001 1.80 x 0.80 x ~2.20" % (size[0], size[2], hi[1], lo[1]))
    counter = next((c for c in stall.children if c.name == "Counter"), None)
    if counter is not None:
        cbox, cunk = renderer_bounds(prefab, [counter], root=stall)
        if cbox and not cunk:
            audit.add("OK" if _close(cbox[1][1], 1.0, 0.01) else F, label, "Counter renderer top at %.3f m (GT-001: 1.00 +/- 0.01)" % cbox[1][1])


def sign_uniqueness(audit, label, prefab):
    offenders = []
    for base, _dirs, files in os.walk(os.path.join(audit.project.root, "Assets")):
        for name in files:
            if name.endswith(".meta"):
                continue
            stem = os.path.splitext(name)[0]
            if (stem.startswith("PF_Sign_") or stem.startswith("SM_Sign_")) and not stem.startswith(("PF_Sign_TramChanh_New", "SM_Sign_TramChanh_New")):
                offenders.append(os.path.relpath(os.path.join(base, name), audit.project.root))
    audit.add(F if offenders else "OK", label, "GT-002 single new sign asset" + (": extra sign assets %s" % offenders if offenders else ""))
    mats = [audit.project.asset_name(m) for n in prefab.root.walk() for c in n.components if c.type in RENDERERS for m in (c.data.get("m_Materials") or [])]
    audit.add("OK" if "MAT_Sign_TramChanh_New.mat" in mats else W, label, "MAT_Sign_TramChanh_New slot in use" + ("" if "MAT_Sign_TramChanh_New.mat" in mats else " - branding material slot missing"))


def environment_checks(audit, label, prefab):
    root = prefab.root
    for name in ENV_ANCHORS:
        node = next((c for c in root.children if c.name == name), None)
        if node is None:
            deep = prefab.find_all(name)
            audit.add(W if deep else F, label, "composition anchor '%s' %s" % (name, "is not a direct child of the root (bootstrap falls back to a deep search; tests require a direct child)" if deep else "missing (TramChanhMainBootstrap / ACCEL tests read it by name)"))
            continue
        sc = (node.transform.data.get("m_LocalScale") or {}) if node.transform else {}
        unit = all(_close(_f(sc.get(k), 1.0), 1.0, 1e-6) for k in "xyz")
        audit.add("OK" if unit else F, label, "composition anchor '%s' direct child%s" % (name, "" if unit else " but scale is not 1"))
    stall_root = prefab.find("StallRoot")
    stall = prefab.find("StallRoot/PF_Stall_TramChanh")
    if stall is None:
        audit.add(F, label, "StallRoot/PF_Stall_TramChanh missing (canonical nested stall)")
    else:
        canonical = P + "Stall/PF_Stall_TramChanh.prefab"
        audit.add("OK" if stall.source == canonical else F, label, "StallRoot/PF_Stall_TramChanh is a nested instance of the canonical stall prefab")
        if stall_root and not any(c.label == "StallAnchorSet" for n in stall_root.walk() for c in n.components):
            audit.add(F, label, "StallRoot has no StallAnchorSet")
        stall_checks(audit, label, prefab, stall=stall)
    signs = prefab.find_all("PF_Sign_TramChanh_New")
    audit.add("OK" if len(signs) == 1 else F, label, "exactly one PF_Sign_TramChanh_New in environment (found %d)" % len(signs))
    olds = [n.name for n in root.walk() if "OldSign" in n.name or "IlluminatedLetters" in n.name]
    if olds:
        audit.add(F, label, "old sign objects present: %s" % olds)
    for path, why in [("StallRoot/PF_Stall_TramChanh/Sign/PF_Sign_TramChanh_New/BrandingPlaceholder", "GT_002 test"),
                      ("TablePoint/PF_YellowCrateTable/DeliveryPoint", "ACCEL_SCENE_002"),
                      ("CakeTablePoint/PF_YellowCrateTable/DeliveryPoint", "ACCEL cake-table test"),
                      ("VehiclePoint/GenericVehicleVisual", "ACCEL_SCENE_002"), ("VehiclePoint/TakeawayCustomer", "ACCEL_SCENE_002"),
                      ("DineInCustomer", "ACCEL_SCENE_002"), ("CakeDineInCustomer/SeatedBody", "ACCEL cake-table test"),
                      ("Roadside/Sidewalk", "ACCEL_SCENE_003"), ("Roadside/Street", "ACCEL_SCENE_003")]:
        node = prefab.find(path)
        audit.add("OK" if node else F, label, ("'%s' present" if node else "'%s' missing") % path + ("" if node else " (" + why + ")"))
    street = prefab.find("Roadside/Street")
    if street is not None and not any(c.type == "BoxCollider" for c in street.components):
        audit.add(F, label, "Roadside/Street needs a BoxCollider (walkable ground)")
    texts = [str(c.data.get("m_Text", "")) for n in root.walk() for c in n.components if c.type == "TextMesh"]
    has_brand = any("Tr" in t and "Chanh" in t for t in texts)
    audit.add("OK" if has_brand else F, label, "a TextMesh with the brand text 'Trạm Chanh' exists (GT_002 test)" if has_brand else "no TextMesh brand text 'Trạm Chanh' (GT_002 test)")
    stools = len(prefab.find_all("PF_PlasticStool"))
    audit.add("OK" if stools >= 3 else F, label, "%d PF_PlasticStool instances (>= 3)" % stools)
    for name in ("TablePoint", "CakeTablePoint", "VehiclePoint"):
        node = prefab.find(name)
        if node and any(c.label in ("TableOrderPoint", "VehicleOrderPoint", "InteractableRef") for n in node.walk() for c in n.components):
            audit.add(F, label, "'%s' carries gameplay components; the bootstrap adds the gameplay point itself - environment art must stay visual" % name)
    cams = [n.path(root) for n in root.walk() for c in n.components if c.type in ("Camera", "AudioListener")]
    if cams:
        audit.add(W, label, "Camera/AudioListener in environment (bootstrap disables them at runtime): %s" % cams)


TABLE_COUNT = 10
TABLE_EXACT = re.compile(r"^TABLE_(0[1-9]|10)$")
TABLE_LIKE = re.compile(r"^table[ _-]?\d+$", re.IGNORECASE)


def table_anchor_checks(audit, label, prefab):
    """TABLES-03: optional TABLE_01..TABLE_10 dine-in anchors (Docs/Coordination/TABLE_ANCHOR_CONTRACT.md).

    Missing anchors are a WARN (the bootstrap uses TablePoint/CakeTablePoint and its fallback layout).
    Present anchors must be exact, unique and direct children of the root or of a single 'CustomerArea' child;
    a missing 'Seat' child is a WARN; gameplay components on table furniture are a FAIL.
    """
    root = prefab.root
    found = {}
    for node in root.walk():
        if node is root:
            continue
        if TABLE_EXACT.match(node.name):
            found.setdefault(node.name, []).append(node)
        elif TABLE_LIKE.match(node.name):
            audit.add(F, label, "'%s' looks like a table anchor but is misnamed (use TABLE_01..TABLE_%02d: two digits, upper case)" % (node.path(root), TABLE_COUNT))
    areas = [c for c in root.children if c.name == "CustomerArea"]
    if len(areas) > 1:
        audit.add(F, label, "%d direct 'CustomerArea' children; table anchors need a single CustomerArea" % len(areas))
    missing = []
    for number in range(1, TABLE_COUNT + 1):
        name = "TABLE_%02d" % number
        nodes = found.get(name, [])
        if not nodes:
            missing.append(name)
            continue
        if len(nodes) > 1:
            audit.add(F, label, "table anchor '%s' is duplicated: %s" % (name, ", ".join(n.path(root) for n in nodes)))
            continue
        node = nodes[0]
        placed = node.parent is root or (len(areas) == 1 and node.parent is areas[0])
        audit.add("OK" if placed else F, label, "table anchor '%s' %s" % (node.path(root), "placed under the root/CustomerArea" if placed else "must be a direct child of the environment root or of its single CustomerArea child"))
        if not any(c.name == "Seat" for c in node.children):
            audit.add(W, label, "table anchor '%s' has no 'Seat' child (bootstrap uses its default seat offset)" % node.path(root))
        gameplay = [n.path(root) for n in node.walk() for c in n.components if c.label in ("TableOrderPoint", "VehicleOrderPoint", "InteractableRef")]
        if gameplay:
            audit.add(F, label, "table anchor '%s' carries gameplay components %s; furniture must stay visual (the bootstrap adds the point)" % (name, gameplay))
        bad_layers = [n.path(root) for n in node.walk() if n.layer != 8 and any(c.type in COLLIDERS and str(c.data.get("m_IsTrigger", "0")) != "1" for c in n.components)]
        if bad_layers:
            audit.add(W, label, "table anchor '%s' has solid colliders off the Environment layer: %s" % (name, bad_layers))
    if len(missing) == TABLE_COUNT:
        audit.add(W, label, "no TABLE_01..TABLE_%02d anchors (optional): bootstrap uses TablePoint/CakeTablePoint + fallback layout" % TABLE_COUNT)
    elif missing:
        audit.add(W, label, "table anchors missing (fallback layout used for them): %s" % ", ".join(missing))


def cake_station_checks(audit, label, prefab):
    points = [(n, c) for n in prefab.root.walk() for c in n.components if c.label == "CakeStationPoint"]
    actions = sorted(int(_f(c.data.get("_action"), 0)) for _, c in points)
    audit.add("OK" if actions == [0, 1, 2, 3, 4] else F, label, "5 CakeStationPoints with actions Fill/Grill/RollArea/Sauce/Wrap (found %s)" % actions)
    for node, comp in points:
        has = any(c.type in COLLIDERS for c in node.components) and node.layer == 9
        audit.add("OK" if has else F, label, "CakeStationPoint '%s' has its own Interactable-layer collider" % node.path(prefab.root))
        if prefab.resolve(comp, "_station") is not prefab.root:
            audit.add(F, label, "CakeStationPoint '%s'._station does not point at the root CakeStation" % node.path(prefab.root))
    no_ref = [n.path(prefab.root) for n, _ in points if _nearest_ref(n) is None]
    cup = prefab.find("BatterArea/PF_BatterMeasureCup_500ml")
    if cup is not None and _nearest_ref(cup) is None:
        no_ref.append("BatterArea/PF_BatterMeasureCup_500ml")
    if no_ref:
        audit.add(W, label, "GAMEPLAY GAP (Claude-side): no InteractableRef on %s - PlayerInteractor (raycast + GetComponentInParent<InteractableRef>) cannot focus them in Play Mode" % no_ref)
    lid = prefab.find("PF_Grill_Elmich/LidPivot")
    if lid is not None:
        z = _f(((lid.transform.data.get("m_LocalPosition") or {}) if lid.transform else {}).get("z"))
        audit.add("OK" if z > 0 else F, label, "LidPivot local z = %.3f (rear hinge, must be > 0)" % z)
    st = next((c for c in prefab.root.components if c.label == "CakeStation"), None)
    if st is not None:
        for field in ("_preheatSeconds", "_openLidDegrees"):
            audit.add("INFO", label, "CakeStation.%s = %s (DEV seed, DEC-007/DEC-013)" % (field, st.data.get(field)))


def grill_checks(audit, label, prefab):
    lid = prefab.find("LidPivot")
    base = prefab.find("Base")
    if lid is not None and base is not None:
        box, unknown = renderer_bounds(prefab, [base])
        z = _f((lid.transform.data.get("m_LocalPosition") or {}).get("z"))
        if box and not unknown:
            audit.add("OK" if _close(z, box[1][2], 1e-3) else F, label, "LidPivot z %.3f equals Base rear edge %.3f (GameplayBlockoutTests)" % (z, box[1][2]))
        else:
            audit.add(W, label, "Base uses a custom mesh: verify LidPivot sits on the rear hinge axis in Unity")
    lid_mesh = prefab.find("LidPivot/Lid")
    if lid_mesh is not None and not any(c.type == "BoxCollider" for c in lid_mesh.components):
        audit.add(F, label, "LidPivot/Lid needs a BoxCollider (moves with the lid; GameplayBlockoutTests)")


def cake_item_checks(audit, label, prefab):
    rolled = prefab.find("SM_Cake_RolledVertical")
    if rolled is not None and rolled.transform:
        sc = rolled.transform.data.get("m_LocalScale") or {}
        ok = _f(sc.get("y"), 1) > _f(sc.get("x"), 1)
        audit.add("OK" if ok else F, label, "SM_Cake_RolledVertical localScale.y > x (CakeSavedAssetTests asserts the roll is vertical via scale)")


def teabag_checks(audit, label, prefab):
    for path in ("Visual/Bag_Closed", "Visual/TeaLiquid"):
        node = prefab.find(path)
        has = node is not None and any(c.type in RENDERERS for c in node.components)
        audit.add("OK" if has else F, label, "'%s' has a Renderer on the node itself (TeaRackPickupAssetTests)" % path)
    vis = prefab.find("Visual")
    if vis is not None and vis.transform:
        d = vis.transform.data
        p, r = d.get("m_LocalPosition") or {}, d.get("m_LocalRotation") or {}
        ident = all(abs(_f(p.get(k))) < 1e-6 for k in "xyz") and all(abs(_f(r.get(k))) < 1e-6 for k in "xyz")
        audit.add("OK" if ident else W, label, "'Visual' has identity local pose (AN_TeaBag_Shake drives its rotation)")


def cup_checks(audit, label, prefab):
    level = prefab.find("BatterLevel")
    if level is not None:
        audit.add("INFO", label, "BatterLevel localScale.y and localPosition.y are rewritten by BatterMeasureCup.ApplyLevel (y scale 0.001..0.1, y pos 0.005..0.055)")
    if _nearest_ref(prefab.root) is None:
        audit.add(W, label, "GAMEPLAY GAP (Claude-side): no InteractableRef on the cup - it cannot be focused/picked up by the player raycast")


def vehicle_checks(audit, label, prefab):
    tr = prefab.root.transform.data if prefab.root.transform else {}
    pos = tr.get("m_LocalPosition") or {}
    if any(abs(_f(pos.get(k))) > 1e-6 for k in "xyz"):
        audit.add(W, label, "root localPosition is (%s, %s, %s), not 0 (harmless: bootstrap overrides it; reset when restructuring)" % (pos.get("x"), pos.get("y"), pos.get("z")))
    box = next((c for c in prefab.root.components if c.type == "BoxCollider"), None)
    if box is not None:
        size, center = box.data.get("m_Size") or {}, box.data.get("m_Center") or {}
        if max(_f(size.get(k)) for k in "xyz") < 0.5:
            audit.add(W, label, "interaction trigger is only %sx%sx%s m centred at y=%s (ground); hard to aim at - size is [Tbd] DEC-010 (Claude-side)" % (
                size.get("x"), size.get("y"), size.get("z"), center.get("y")))


def drink_station_checks(audit, label, prefab):
    want = {"TeaRackController": 1, "ReadyCounterPoint": 1, "ReadyOrderPickupPoint": 1, "IceBin": 1, "ToppingBin": 2, "WipeInteraction": 1}
    for cls, count in want.items():
        got = len(prefab.components(cls))
        audit.add("OK" if got == count else F, label, "%d x %s (expected %d)" % (got, cls, count))
    types = sorted(int(_f(c.data.get("_toppingType"), -1)) for c in prefab.components("ToppingBin"))
    if len(set(types)) != len(types):
        audit.add(F, label, "ToppingBins share a _toppingType %s" % types)


def rack_focus_note(audit, label, prefab):
    rack = prefab.find("Rack")
    if rack is not None and not any(c.type in COLLIDERS for c in prefab.root.components):
        audit.add(W, label, "focus depends on the colliders of the visual meshes under 'Rack' (no root trigger): an art swap that drops them makes the rack unfocusable (proposed: root trigger BoxCollider, issue #20)")


def visual_only_checks(audit, label, prefab):
    gameplay = sorted({c.label for n in prefab.root.walk() for c in n.components if c.type == "MonoBehaviour" and c.label not in ("PlaceholderAsset",)})
    if gameplay:
        audit.add("INFO", label, "components present: %s" % gameplay)


IP = ["InteractionPoint"]
CONTRACTS = [
    ("PF_Stall_TramChanh", P + "Stall/PF_Stall_TramChanh.prefab", C(
        nodes=[(n, F, "GT-001 tests / bounds groups") for n in ("Structure", "Counter", "Frame", "Roof", "Wheels", "Sign", "Anchors", "Colliders")]
        + [("Sign/PF_Sign_TramChanh_New", F, "nested new sign"), ("Lights", W, "light group")],
        components=[(".", "StallAnchorSet", F)],
        custom=[stall_checks])),
    ("PF_Sign_TramChanh_New", P + "Stall/PF_Sign_TramChanh_New.prefab", C(
        nodes=[("SM_Sign_TramChanh_New", W, "branding mesh carrying MAT_Sign_TramChanh_New")],
        collider_layers=[(".", {8}, F)], custom=[sign_uniqueness])),
    ("PF_RedTeaRack (base)", P + "Workstations/PF_RedTeaRack.prefab", C(
        nodes=[("BagSlots/Slot01", F, "TeaRackController._bagSlots[0]"), ("BagSlots/Slot02", F, "_bagSlots[1]"), ("InteractionPoint", F, "_interactionPoint"),
               ("OutputPoint", W, "bag spawn anchor"), ("Rack", W, "visual group whose colliders are the focus colliders")],
        collider_layers=[(".", {9}, F)], visual_root="Visual", custom=[visual_only_checks])),
    ("PF_RedTeaRack (DrinkWave)", P + "Workstations/DrinkWave/PF_RedTeaRack.prefab", C(
        nodes=[("BagSlots/Slot01", F, ""), ("BagSlots/Slot02", F, ""), ("InteractionPoint", F, "")],
        components=[(".", "TeaRackController", F), (".", "InteractableRef", F)],
        refs=[(".", "TeaRackController", "_interactionPoint", "InteractionPoint"), (".", "TeaRackController", "_bagSlots", ["BagSlots/Slot01", "BagSlots/Slot02"]),
              (".", "TeaRackController", "_bagPrefab", "asset:PF_TeaBag_PrePortioned.prefab"), (".", "InteractableRef", "_behaviour", ".")],
        focus="orphans", visual_root="Visual", custom=[rack_focus_note])),
    ("PF_TeaBag_PrePortioned", P + "Items/PF_TeaBag_PrePortioned.prefab", C(
        nodes=[(p, F, "TeaBagItem / TeaBagStateView / tests") for p in ("Anchors/HandGrip", "Anchors/PlacementPoint", "Visual", "Visual/Bag_Closed", "Visual/Bag_Open",
                                                                         "Visual/TeaLiquid", "Visual/Contents/CoconutJelly", "Visual/Contents/LemonJelly", "Visual/Contents/Ice", "Visual/Condensation")],
        components=[(".", "TeaBagItem", F), (".", "TeaBagStateView", F), (".", "Animator", F)],
        refs=[(".", "TeaBagItem", "_handGrip", "Anchors/HandGrip"), (".", "TeaBagItem", "_placementPoint", "Anchors/PlacementPoint"), (".", "TeaBagItem", "_stateView", "."),
              (".", "TeaBagStateView", "_bagClosed", "Visual/Bag_Closed"), (".", "TeaBagStateView", "_bagOpen", "Visual/Bag_Open"),
              (".", "TeaBagStateView", "_coconutJelly", "Visual/Contents/CoconutJelly"), (".", "TeaBagStateView", "_lemonJelly", "Visual/Contents/LemonJelly"),
              (".", "TeaBagStateView", "_ice", "Visual/Contents/Ice"), (".", "TeaBagStateView", "_condensation", "Visual/Condensation"), (".", "TeaBagStateView", "_animator", ".")],
        custom=[teabag_checks])),
    ("PF_ToppingStation (base)", P + "Workstations/PF_ToppingStation.prefab", C(
        nodes=[(p, F, "variant overrides / animation") for p in ("CoverPivot", "CoverPivot/TransparentCover", "LemonJellyBin", "LemonJellyBin/InteractionPoint",
                                                                 "CoconutJellyBin", "CoconutJellyBin/InteractionPoint", "StationFrame")],
        collider_layers=[(".", {9}, F)], visual_root="Visual", custom=[visual_only_checks])),
    ("PF_ToppingStation (DrinkWave)", P + "Workstations/DrinkWave/PF_ToppingStation.prefab", C(
        nodes=[(p, F, "") for p in ("CoverPivot", "LemonJellyBin/InteractionPoint", "CoconutJellyBin/InteractionPoint")],
        components=[(".", "Animator", F), (".", "StationToolView", F), ("LemonJellyBin", "ToppingBin", F), ("LemonJellyBin", "InteractableRef", F),
                    ("CoconutJellyBin", "ToppingBin", F), ("CoconutJellyBin", "InteractableRef", F)],
        refs=[("LemonJellyBin", "ToppingBin", "_interactionPoint", "LemonJellyBin/InteractionPoint"),
              ("CoconutJellyBin", "ToppingBin", "_interactionPoint", "CoconutJellyBin/InteractionPoint")],
        focus="orphans", visual_root="Visual")),
    ("PF_IceBin (base)", P + "Workstations/PF_IceBin.prefab", C(
        nodes=[(p, F, "variant overrides") for p in ("InteractionPoint", "ScoopRestPoint", "IceVolume", "Bin")],
        collider_layers=[(".", {9}, F)], visual_root="Visual", custom=[visual_only_checks])),
    ("PF_IceBin (DrinkWave)", P + "Workstations/DrinkWave/PF_IceBin.prefab", C(
        nodes=[("InteractionPoint", F, "_interactionPoint"), ("ScoopRestPoint/PF_IceScoop", F, "scoop tool rest")],
        components=[(".", "IceBin", F), (".", "InteractableRef", F)],
        refs=[(".", "IceBin", "_interactionPoint", "InteractionPoint"), (".", "InteractableRef", "_behaviour", ".")],
        focus="orphans", visual_root="Visual")),
    ("PF_IceScoop", P + "Tools/PF_IceScoop.prefab", C(
        nodes=[("Visual", F, "AN_Ice_Scoop animates 'Visual' localPosition")],
        components=[(".", "Animator", F), (".", "StationToolView", F)])),
    ("PF_WipeCloth", P + "Tools/PF_WipeCloth.prefab", C(
        nodes=[("Visual", F, "AN_Cloth_Wipe animates 'Visual' localPosition")],
        components=[(".", "Animator", F), (".", "StationToolView", F)])),
    ("PF_Grill_Elmich", P + "Workstations/PF_Grill_Elmich.prefab", C(
        nodes=[(p, F, "CakeStation refs / grill tests") for p in ("Base", "LowerPlate", "LidPivot", "LidPivot/Lid", "LidPivot/UpperPlate", "LidPivot/Handle",
                                                                  "Display", "Anchors/CakePlacementPoint", "Anchors/InteractionPoint")]
        + [("Anchors/SpatulaPoint", W, "spec anchor"), ("Anchors/AudioPoint", W, "spec anchor")],
        collider_layers=[(".", {9}, F)], visual_root="Visual", custom=[grill_checks, visual_only_checks])),
    ("PF_BatterMeasureCup_500ml", P + "ACCEL01/Cakes/PF_BatterMeasureCup_500ml.prefab", C(
        nodes=[("Anchors/HandGrip", F, "_handGrip / tests"), ("BatterLevel", F, "_liquid (scaled by code)"), ("Anchors/PourPoint", W, "spec anchor"),
               ("Anchors/PlacementPoint", W, "spec anchor")],
        components=[(".", "BatterMeasureCup", F)],
        refs=[(".", "BatterMeasureCup", "_handGrip", "Anchors/HandGrip"), (".", "BatterMeasureCup", "_liquid", "BatterLevel")],
        visual_root="Visual", custom=[cup_checks])),
    ("PF_Spatula_WoodHandle", P + "ACCEL01/Cakes/PF_Spatula_WoodHandle.prefab", C(
        nodes=[("Anchors/HandGrip", F, "CakeSavedAssetTests"), ("SM_Spatula_WoodHandle", W, "visual mesh")], visual_root="Visual")),
    ("PF_Scissors_RedGray", P + "ACCEL01/Cakes/PF_Scissors_RedGray.prefab", C(
        nodes=[("Anchors/HandGrip", F, "CakeSavedAssetTests"), ("SM_Scissors_RedGray", W, "visual mesh")], visual_root="Visual")),
    ("PF_SauceBag", P + "ACCEL01/Cakes/PF_SauceBag.prefab", C(
        nodes=[("Anchors/HandGrip", F, "CakeSavedAssetTests"), ("SM_SauceBag", W, "visual mesh"), ("Anchors/PourPoint", W, "spec anchor (missing today)")], visual_root="Visual")),
    ("PF_CakeWrappingPaper", P + "ACCEL01/Cakes/PF_CakeWrappingPaper.prefab", C(
        nodes=[("Anchors/HandGrip", F, "CakeSavedAssetTests"), ("SM_CakeWrappingPaper", W, "visual mesh")], visual_root="Visual")),
    ("PF_Cake_Prepared", P + "ACCEL01/Cakes/PF_Cake_Prepared.prefab", C(
        nodes=[("Anchors/HandGrip", F, "_handGrip"), ("Anchors/PlacementPoint", F, "ReadyCounterPoint Find(\"Anchors/PlacementPoint\")"),
               ("SM_Cake_Flat", F, "_flatVisual"), ("SM_Cake_RolledVertical", F, "_rolledVisual + CakeStationFlowTests Find"), ("SM_Cake_WrappingPaper", F, "_paperVisual")],
        components=[(".", "CakeItem", F)],
        refs=[(".", "CakeItem", "_handGrip", "Anchors/HandGrip"), (".", "CakeItem", "_flatVisual", "SM_Cake_Flat"),
              (".", "CakeItem", "_rolledVisual", "SM_Cake_RolledVertical"), (".", "CakeItem", "_paperVisual", "SM_Cake_WrappingPaper")],
        custom=[cake_item_checks])),
    ("PF_CakeStation", P + "ACCEL01/Cakes/PF_CakeStation.prefab", C(
        nodes=[(p, F, "CakeSavedAssetTests / CakeStation refs") for p in ("BatterArea", "BatterSource", "PF_Grill_Elmich", "RollArea", "WrappingArea", "PF_Spatula_WoodHandle",
                                                                          "PF_Scissors_RedGray", "PF_SauceBag", "BatterArea/PF_BatterMeasureCup_500ml",
                                                                          "PF_Grill_Elmich/LidPivot", "PF_Grill_Elmich/Anchors/CakePlacementPoint")],
        components=[(".", "CakeStation", F), (".", "PlaceholderFlipAction", F), ("BatterArea/PF_BatterMeasureCup_500ml", "BatterMeasureCup", F)],
        refs=[(".", "CakeStation", "_cup", "BatterArea/PF_BatterMeasureCup_500ml"), (".", "CakeStation", "_cakePrefab", "asset:PF_Cake_Prepared.prefab"),
              (".", "CakeStation", "_cakePlacement", "PF_Grill_Elmich/Anchors/CakePlacementPoint"), (".", "CakeStation", "_lidPivot", "PF_Grill_Elmich/LidPivot"),
              (".", "CakeStation", "_scissors", "PF_Scissors_RedGray"), (".", "CakeStation", "_sauceBag", "PF_SauceBag"),
              (".", "CakeStation", "_wrappingArea", "WrappingArea"), (".", "CakeStation", "_flipAction", "."),
              (".", "PlaceholderFlipAction", "_rollArea", "RollArea"), (".", "PlaceholderFlipAction", "_spatula", "PF_Spatula_WoodHandle"),
              ("BatterArea/PF_BatterMeasureCup_500ml", "BatterMeasureCup", "_home", "BatterArea")],
        custom=[cake_station_checks])),
    ("PF_DrinkStation", P + "Workstations/DrinkWave/PF_DrinkStation.prefab", C(
        nodes=[(p, W, "group name (components are found by type)") for p in ("WipeArea", "PF_ReadyCounterPoint", "PF_ToppingStation", "PF_IceBin", "PF_RedTeaRack")]
        + [("WipeArea/PF_WipeCloth/Visual", F, "AN_Cloth_Wipe binding"), ("PF_IceBin/ScoopRestPoint/PF_IceScoop/Visual", F, "AN_Ice_Scoop binding")],
        components=[("WipeArea", "WipeInteraction", F), ("WipeArea", "InteractableRef", F)],
        refs=[("WipeArea", "WipeInteraction", "_interactionPoint", "WipeArea/InteractionPoint"),
              ("PF_ReadyCounterPoint", "ReadyCounterPoint", "_drinkPlacementPoint", "PF_ReadyCounterPoint/DrinkPlacement"),
              ("PF_ReadyCounterPoint", "ReadyCounterPoint", "_cakePlacementPoint", "PF_ReadyCounterPoint/CakePlacement")],
        focus="orphans", custom=[drink_station_checks])),
    ("PF_ReadyCounterPoint (Stall, legacy)", P + "Stall/PF_ReadyCounterPoint.prefab", C(
        nodes=[(p, W, "blockout slot") for p in ("InteractionTrigger", "DrinkPlacement", "CakePlacement", "OrderIndicator")],
        custom=[lambda a, l, p: a.add(W, l, "legacy blockout without ReadyCounterPoint component; the game uses Workstations/DrinkWave/PF_ReadyCounterPoint")])),
    ("PF_ReadyCounterPoint (DrinkWave)", P + "Workstations/DrinkWave/PF_ReadyCounterPoint.prefab", C(
        nodes=[(p, F, "ReadyCounterPoint / ReadyOrderPickupPoint refs") for p in ("InteractionTrigger", "DrinkPlacement", "CakePlacement", "LobbyPickup")]
        + [("OrderIndicator", W, "spec marker")],
        components=[(".", "ReadyCounterPoint", F), (".", "InteractableRef", F), ("LobbyPickup", "ReadyOrderPickupPoint", F), ("LobbyPickup", "InteractableRef", F)],
        refs=[(".", "ReadyCounterPoint", "_interactionPoint", "InteractionTrigger"), (".", "ReadyCounterPoint", "_drinkPlacementPoint", "DrinkPlacement"),
              (".", "ReadyCounterPoint", "_cakePlacementPoint", "CakePlacement"), ("LobbyPickup", "ReadyOrderPickupPoint", "_interactionPoint", "LobbyPickup")],
        focus="orphans", collider_layers=[(".", {9}, F)])),
    ("PF_PlasticStool", P + "CustomerArea/PF_PlasticStool.prefab", C(
        nodes=[("SeatPoint", W, "spec anchor")], collider_layers=[(".", {8}, F)], visual_root="Visual", custom=[visual_only_checks])),
    ("PF_YellowCrateTable (CustomerArea)", P + "CustomerArea/PF_YellowCrateTable.prefab", C(
        nodes=[("InteractionPoint", F, "variant + bootstrap Find(\"InteractionPoint\")"), ("DeliveryPoint", F, "ACCEL tests"), ("Seats/Seat01", W, ""), ("Seats/Seat02", W, "")],
        collider_layers=[(".", {8}, F)], visual_root="Visual", custom=[visual_only_checks])),
    ("PF_YellowCrateTable (DrinkWave)", P + "Workstations/DrinkWave/PF_YellowCrateTable.prefab", C(
        nodes=[("InteractionPoint", F, "TramChanhMainBootstrap Find(\"InteractionPoint\")"), ("DeliveryPoint", W, "spec anchor")],
        components=[(".", "TableOrderPoint", F), (".", "InteractableRef", F)],
        refs=[(".", "InteractableRef", "_behaviour", ".")], focus="any")),
    ("PF_Placeholder_VehiclePoint", P + "Workstations/DrinkWave/PF_Placeholder_VehiclePoint.prefab", C(
        nodes=[("Anchors/InteractionPoint", F, "TramChanhMainBootstrap Find(\"Anchors/InteractionPoint\")"), ("Anchors/CustomerWaitPoint", W, "spec anchor (missing today)")],
        components=[(".", "VehicleOrderPoint", F), (".", "InteractableRef", F)],
        refs=[(".", "InteractableRef", "_behaviour", ".")], focus="any", visual_root="Visual", custom=[vehicle_checks])),
    ("PF_AccelRoadsideEnvironment", P + "ACCEL01/Environment/PF_AccelRoadsideEnvironment.prefab", C(custom=[environment_checks, table_anchor_checks])),
    ("PF_Player", P + "NPC/PF_Player.prefab", C(
        nodes=[("PlayerCamera", F, "FirstPersonController._camera"), ("PlayerCamera/HandSocket", F, "parent of the scene-added HoldAnchor (tests)")],
        components=[(".", "CharacterController", F), (".", "PlayerInputReader", F), (".", "FirstPersonController", F), (".", "PlayerInteractor", F)],
        refs=[(".", "FirstPersonController", "_camera", "PlayerCamera")],
        collider_layers=[(".", {10}, F)])),
]


def mapping_table(root):
    lines = ["Requested name -> actual prefab(s)"]
    for name, paths, note in MAPPING:
        found = [p for p in paths if os.path.isfile(os.path.join(root, p))]
        status = "OK" if len(found) == len(paths) else ("MISSING" if not found else "PARTIAL")
        if found and any(os.path.splitext(os.path.basename(p))[0] != name for p in found):
            status = "NAME DIFFERS"
        lines.append("  %-28s %-8s %s  (%s)" % (name, status, ", ".join(found) or "-", note))
    return "\n".join(lines)


def main(argv=None):
    parser = argparse.ArgumentParser(description="Read-only Tram Chanh art integration contract audit.")
    parser.add_argument("root", nargs="?", default=os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")))
    parser.add_argument("--only", action="append", help="audit only contracts whose label contains this text (repeatable)")
    parser.add_argument("--dump", help="print the expanded hierarchy of one prefab (repo-relative path) and exit")
    parser.add_argument("--verbose", action="store_true", help="also print passing checks")
    parser.add_argument("--no-mapping", action="store_true", help="skip the requested-name mapping table")
    args = parser.parse_args(argv)
    root = os.path.abspath(args.root)
    if not os.path.isdir(os.path.join(root, "Assets")):
        print("error: %s is not a Unity project root (no Assets/)" % root, file=sys.stderr)
        return 2
    project = Project(root)
    if args.dump:
        print(dump(Prefab(project, args.dump)))
        return 0
    if not args.no_mapping:
        print(mapping_table(root))
        print()
    audit = Audit(project, args.verbose)
    for label, rel, contract in CONTRACTS:
        if args.only and not any(o in label for o in args.only):
            continue
        audit.run(label, rel, contract)
    by_label = {}
    for level, label, message in audit.results:
        by_label.setdefault(label, []).append((level, message))
    fails = warns = oks = 0
    for label, rel, _contract in CONTRACTS:
        if label not in by_label:
            continue
        items = by_label[label]
        f = sum(1 for l, _ in items if l == F)
        w = sum(1 for l, _ in items if l == W)
        o = sum(1 for l, _ in items if l == "OK")
        fails, warns, oks = fails + f, warns + w, oks + o
        print("[%s] %s  (%s)  ok=%d warn=%d fail=%d" % ("FAIL" if f else ("WARN" if w else "PASS"), label, rel, o, w, f))
        for level, message in items:
            if level in (F, W) or args.verbose:
                print("    %-4s %s" % (level, message))
    print()
    print("SUMMARY: %d checks ok, %d WARN (known gaps), %d FAIL -> %s" % (oks, warns, fails, "FAIL" if fails else "PASS"))
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main())
