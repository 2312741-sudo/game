#!/usr/bin/env python3
"""MAIN-105 static pre-flight for Unity YAML assets (stdlib only, read-only).

This is a STATIC check. It does not run Unity and its result is never a Unity result.

For every .unity / .prefab / .asset under Assets/TramChanh it checks:
  1. every `guid:` reference resolves to a .meta file in the project (Assets/ and any
     embedded Packages/<name>/ folder), or to a Unity built-in resource GUID;
  2. every local reference `{fileID: N}` (no guid) resolves to a `--- !u!<cls> &N` object
     in the same file;
  3. every cross-file reference `{fileID: N, guid: G, type: 2}` to another YAML asset
     in the project resolves to an object with that fileID in the target file;
and over the whole project:
  4. no two .meta files share a GUID.
It also lists .meta files without an asset and assets without a .meta (informational).

GUIDs that do not resolve inside the repository are reported per GUID with the referencing
files and the YAML key. Packages from Library/PackageCache (URP, Input System, uGUI, ...)
are not in the repository, so a reference into a registry package cannot be resolved here.
Such GUIDs fail the run unless they are listed in --allow-external (one GUID per line,
'#' comments allowed), so a deleted or renamed project script is never silently accepted.

Exit code: 0 = no FAIL findings, 1 = at least one FAIL, 2 = usage error.

Usage:
    python3 Automation/static_preflight.py [--root PATH] [--allow-external FILE] [--json OUT]
"""

import argparse
import json
import os
import re
import sys
from collections import defaultdict

BUILTIN_GUIDS = {
    "0000000000000000e000000000000000": "Library/unity default resources",
    "0000000000000000f000000000000000": "Resources/unity_builtin_extra",
    "0000000000000000d000000000000000": "built-in (legacy)",
}
SCANNED_EXT = (".unity", ".prefab", ".asset")
YAML_LIKE_EXT = (".unity", ".prefab", ".asset", ".mat", ".controller", ".anim", ".overrideController",
                 ".mask", ".physicMaterial", ".lighting", ".playable", ".signal", ".renderTexture",
                 ".cubemap", ".flare", ".guiskin", ".fontsettings", ".mixer", ".spriteatlas",
                 ".terrainlayer", ".brush", ".preset", ".shadervariants", ".inputactions")

OBJ_HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)")
REF = re.compile(r"\{fileID:\s*(-?\d+)(?:,\s*guid:\s*([0-9a-fA-F]{32}))?(?:,\s*type:\s*(\d+))?\s*\}")
META_GUID = re.compile(r"^guid:\s*([0-9a-fA-F]{32})\s*$", re.M)
KEY = re.compile(r"^\s*-?\s*([A-Za-z_][A-Za-z0-9_]*)\s*:")


def rel(root, p):
    return os.path.relpath(p, root).replace(os.sep, "/")


def collect_metas(root):
    """Map guid -> [asset paths] for Assets/ and embedded packages under Packages/."""
    by_guid = defaultdict(list)
    bad_meta = []
    search = [os.path.join(root, "Assets")]
    pk = os.path.join(root, "Packages")
    if os.path.isdir(pk):
        for name in sorted(os.listdir(pk)):
            d = os.path.join(pk, name)
            if os.path.isdir(d):
                search.append(d)
    for base in search:
        for dp, dns, fns in os.walk(base):
            dns.sort()
            for fn in sorted(fns):
                if not fn.endswith(".meta"):
                    continue
                mp = os.path.join(dp, fn)
                try:
                    txt = open(mp, encoding="utf-8", errors="replace").read()
                except OSError as e:
                    bad_meta.append((rel(root, mp), str(e)))
                    continue
                m = META_GUID.search(txt)
                if not m:
                    bad_meta.append((rel(root, mp), "no guid line"))
                    continue
                by_guid[m.group(1).lower()].append(rel(root, mp[:-5]))
    return by_guid, bad_meta


def orphans(root):
    """(.meta without asset, asset without .meta) under Assets/."""
    no_asset, no_meta = [], []
    base = os.path.join(root, "Assets")
    for dp, dns, fns in os.walk(base):
        names = set(fns) | set(dns)
        for fn in fns:
            full = os.path.join(dp, fn)
            if fn.endswith(".meta"):
                if fn[:-5] not in names:
                    no_asset.append(rel(root, full))
            elif not fn.startswith(".") and fn + ".meta" not in names:
                no_meta.append(rel(root, full))
        for d in dns:
            if not d.startswith(".") and not d.endswith("~") and d + ".meta" not in names:
                no_meta.append(rel(root, os.path.join(dp, d)) + "/")
        dns[:] = [d for d in dns if not d.startswith(".") and not d.endswith("~")]
    return sorted(no_asset), sorted(no_meta)


def parse_yaml_asset(path):
    """Return (set of object fileIDs, list of (line_no, key, fileID, guid, type)) or None if not text YAML."""
    try:
        with open(path, "rb") as fh:
            head = fh.read(5)
        if head != b"%YAML":
            return None
        lines = open(path, encoding="utf-8", errors="replace").read().splitlines()
    except OSError:
        return None
    ids, refs = set(), []
    last_key = "?"
    for i, line in enumerate(lines, 1):
        h = OBJ_HEADER.match(line)
        if h:
            ids.add(int(h.group(2)))
            last_key = "?"
            continue
        k = KEY.match(line)
        if k:
            last_key = k.group(1)
        for m in REF.finditer(line):
            fid = int(m.group(1))
            guid = m.group(2).lower() if m.group(2) else None
            typ = int(m.group(3)) if m.group(3) else None
            refs.append((i, last_key, fid, guid, typ))
    return ids, refs


def load_allow(path):
    allow = {}
    if not path:
        return allow
    for raw in open(path, encoding="utf-8"):
        line = raw.split("#", 1)
        g = line[0].strip().lower()
        if re.fullmatch(r"[0-9a-f]{32}", g):
            allow[g] = line[1].strip() if len(line) > 1 else ""
    return allow


def git_tracked(root):
    """Set of repo-relative paths Git tracks, or None when Git is unavailable."""
    import subprocess
    try:
        out = subprocess.run(["git", "-C", root, "ls-files", "-z"], capture_output=True, check=True).stdout
    except (OSError, subprocess.CalledProcessError):
        return None
    return {p.decode("utf-8") for p in out.split(b"\0") if p}


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", default=os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))
    ap.add_argument("--scope", default="Assets/TramChanh", help="folder whose .unity/.prefab/.asset files are scanned")
    ap.add_argument("--allow-external", help="file of GUIDs known to live in registry packages")
    ap.add_argument("--json", help="also write machine-readable findings here")
    ap.add_argument("--include-untracked", action="store_true",
                    help="also scan files Git does not track (default: tracked only, so locally generated "
                         "settings such as the URP asset from Apply Project Settings do not fail the check)")
    a = ap.parse_args()
    root = os.path.abspath(a.root)
    scope = os.path.join(root, a.scope)
    if not os.path.isdir(scope):
        print("usage error: %s is not a folder" % scope, file=sys.stderr)
        return 2

    by_guid, bad_meta = collect_metas(root)
    allow = load_allow(a.allow_external)
    fails, warns, infos = [], [], []

    # 4. duplicate GUIDs
    dups = {g: p for g, p in by_guid.items() if len(p) > 1}
    for g, paths in sorted(dups.items()):
        fails.append("DUPLICATE_GUID %s used by %s" % (g, ", ".join(paths)))
    for p, why in bad_meta:
        fails.append("BAD_META %s (%s)" % (p, why))

    targets = []
    for dp, dns, fns in os.walk(scope):
        dns.sort()
        for fn in sorted(fns):
            if fn.endswith(SCANNED_EXT):
                targets.append(os.path.join(dp, fn))

    if not a.include_untracked:
        tracked = git_tracked(root)
        if tracked is not None:
            skipped = [t for t in targets if rel(root, t) not in tracked]
            targets = [t for t in targets if rel(root, t) in tracked]
            for t in skipped:
                infos.append("UNTRACKED_SKIPPED %s (use --include-untracked to scan it)" % rel(root, t))

    parsed_cache = {}

    def parsed(path):
        if path not in parsed_cache:
            parsed_cache[path] = parse_yaml_asset(path)
        return parsed_cache[path]

    external = defaultdict(lambda: {"files": set(), "keys": set(), "count": 0})
    stats = defaultdict(int)
    for t in targets:
        rp = rel(root, t)
        res = parsed(t)
        if res is None:
            warns.append("NOT_TEXT_YAML %s (binary serialization; not checked)" % rp)
            continue
        ids, refs = res
        stats["files"] += 1
        stats["objects"] += len(ids)
        for line_no, key, fid, guid, typ in refs:
            stats["refs"] += 1
            if guid is None:
                if fid == 0:
                    stats["null_refs"] += 1
                    continue
                stats["local_refs"] += 1
                if fid not in ids:
                    fails.append("LOCAL_FILEID_MISSING %s:%d %s -> fileID %d" % (rp, line_no, key, fid))
                continue
            stats["guid_refs"] += 1
            if guid in BUILTIN_GUIDS:
                stats["builtin_refs"] += 1
                continue
            paths = by_guid.get(guid)
            if not paths:
                e = external[guid]
                e["files"].add(rp)
                e["keys"].add(key)
                e["count"] += 1
                continue
            stats["resolved_guid_refs"] += 1
            # 3. cross-file fileID check into project YAML assets (type 2 = serialized asset)
            target = os.path.join(root, paths[0])
            if typ == 2 and target.endswith(YAML_LIKE_EXT) and os.path.isfile(target):
                tres = parsed(target)
                if tres is not None:
                    stats["cross_refs_checked"] += 1
                    if fid not in tres[0]:
                        fails.append("CROSS_FILEID_MISSING %s:%d %s -> %s fileID %d" % (rp, line_no, key, paths[0], fid))

    for g, e in sorted(external.items()):
        msg = "UNRESOLVED_GUID %s refs=%d keys=%s files=%s" % (
            g, e["count"], ",".join(sorted(e["keys"])), ", ".join(sorted(e["files"])))
        if g in allow:
            infos.append("EXTERNAL_ALLOWED %s (%s) refs=%d" % (g, allow[g] or "allow-list", e["count"]))
        else:
            fails.append(msg)

    no_asset, no_meta = orphans(root)
    for p in no_asset:
        warns.append("META_WITHOUT_ASSET %s" % p)
    for p in no_meta:
        warns.append("ASSET_WITHOUT_META %s" % p)

    print("MAIN-105 static pre-flight (STATIC CHECK - not a Unity result)")
    print("root: %s" % root)
    print("scope: %s" % a.scope)
    print("meta GUIDs indexed: %d (duplicates: %d)" % (len(by_guid), len(dups)))
    print("scanned files: %d, objects: %d, references: %d" % (stats["files"], stats["objects"], stats["refs"]))
    print("  null {fileID: 0}: %d" % stats["null_refs"])
    print("  local fileID refs: %d" % stats["local_refs"])
    print("  guid refs: %d (resolved in repo: %d, built-in: %d, unresolved: %d in %d GUIDs)" % (
        stats["guid_refs"], stats["resolved_guid_refs"], stats["builtin_refs"],
        sum(e["count"] for e in external.values()), len(external)))
    print("  cross-file fileIDs checked: %d" % stats["cross_refs_checked"])
    for title, items in (("FAIL", fails), ("WARN", warns), ("INFO", infos)):
        print("%s: %d" % (title, len(items)))
        for it in items:
            print("  " + it)
    verdict = "FAIL" if fails else "PASS"
    print("STATIC PREFLIGHT: %s" % verdict)
    if a.json:
        with open(a.json, "w", encoding="utf-8") as fh:
            json.dump({"verdict": verdict, "fail": fails, "warn": warns, "info": infos, "stats": dict(stats)}, fh, indent=2)
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main())
