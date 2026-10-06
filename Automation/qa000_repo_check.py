#!/usr/bin/env python3
"""QA-000 — repository, docs and project-skeleton check (PROJECT_TASK_PLAN.md §7).

Read-only. Produces a PASS/FAIL report per criterion and exits non-zero on any FAIL.
Runs without Unity, so it is safe as the first gate in any worktree.

Usage:
    python3 Automation/qa000_repo_check.py [--report QA/Reports/QA-000-<date>.md]
"""

import argparse
import datetime
import json
import os
import re
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TC = os.path.join(ROOT, "Assets", "TramChanh")

REQUIRED_DOCS = [
    "Docs/ARCHITECTURE.md",
    "Docs/ORDER_SYSTEM.md",
    "Docs/INTERACTION_SYSTEM.md",
    "Docs/DRINK_WORKFLOW.md",
    "Docs/CAKE_WORKFLOW.md",
    "Docs/ASSET_INTEGRATION.md",
    "Docs/PROJECT_TASK_PLAN.md",
    "Docs/CODING_CONVENTIONS.md",
    "Docs/Reference/TramChanh_AI_GameDev_Workflow.md",
    "Docs/Reference/TramChanh_3D_Asset_Pipeline.md",
]

REQUIRED_FOLDERS = [
    "Art/Models", "Art/Materials", "Art/Textures", "Art/Animations", "Art/VFX",
    "Audio/SFX", "Audio/Ambient", "Audio/Music",
    "Prefabs/Stall", "Prefabs/Workstations", "Prefabs/Items", "Prefabs/NPC", "Prefabs/UI", "Prefabs/CustomerArea",
    "Scenes/Bootstrap", "Scenes/Gameplay", "Scenes/Test",
    "Scripts/Core", "Scripts/Orders", "Scripts/Interaction", "Scripts/Drinks", "Scripts/Cakes", "Scripts/Lobby",
    "Scripts/Customers", "Scripts/Inventory", "Scripts/UI", "Scripts/Save", "Scripts/Debug", "Scripts/Stall",
    "Scripts/Editor",
    "ScriptableObjects/Items", "ScriptableObjects/Recipes", "ScriptableObjects/Customers", "ScriptableObjects/Balance",
    "Tests/EditMode", "Tests/PlayMode",
]

REQUIRED_PACKAGES = [
    "com.unity.render-pipelines.universal",
    "com.unity.inputsystem",
    "com.unity.test-framework",
    "com.unity.ai.navigation",
]

# ARCHITECTURE.md §4 — project-internal references only.
RUNTIME = ["Core", "Content", "Interaction", "Orders", "Stall", "Drinks", "Cakes", "Lobby", "Customers", "UI"]
EXPECTED_REFS = {
    "TramChanh.Core": [],
    "TramChanh.Content": ["Core"],
    "TramChanh.Interaction": ["Core"],
    "TramChanh.Orders": ["Core", "Content"],
    "TramChanh.Stall": ["Core", "Interaction", "Orders"],
    "TramChanh.Drinks": ["Core", "Content", "Interaction", "Orders"],
    "TramChanh.Cakes": ["Core", "Content", "Interaction", "Orders"],
    "TramChanh.Lobby": ["Core", "Interaction", "Orders"],
    "TramChanh.Customers": ["Core", "Orders", "Lobby"],
    "TramChanh.UI": ["Core", "Interaction", "Orders", "Content"],
    "TramChanh.App": RUNTIME,
    "TramChanh.DevTools": RUNTIME + ["App"],
    "TramChanh.Editor": RUNTIME + ["App", "DevTools"],
    "TramChanh.Tests.EditMode": RUNTIME + ["App", "DevTools", "Editor"],
    "TramChanh.Tests.PlayMode": RUNTIME + ["App", "DevTools"],
}
FORBIDDEN = [  # (assembly, must not reference) — ARCHITECTURE.md §4 rules
    ("TramChanh.Orders", "TramChanh.Drinks"), ("TramChanh.Orders", "TramChanh.Cakes"),
    ("TramChanh.Orders", "TramChanh.Lobby"), ("TramChanh.Orders", "TramChanh.UI"),
    ("TramChanh.Drinks", "TramChanh.Cakes"), ("TramChanh.Cakes", "TramChanh.Drinks"),
    ("TramChanh.Interaction", "TramChanh.Orders"), ("TramChanh.Interaction", "TramChanh.Drinks"),
    ("TramChanh.Interaction", "TramChanh.Cakes"),
]

OLD_SIGN_PATTERN = re.compile(r"(old.?sign|sign.?old|illuminated|tramchanh_?letters|letters_?tramchanh)", re.I)


class Report:
    def __init__(self):
        self.rows = []

    def check(self, criterion, ok, detail=""):
        self.rows.append((criterion, "PASS" if ok else "FAIL", detail))
        return ok

    def note(self, criterion, detail):
        self.rows.append((criterion, "INFO", detail))

    @property
    def failed(self):
        return any(status == "FAIL" for _, status, _ in self.rows)

    def markdown(self, header):
        lines = [header, "", "| # | Criterion | Result | Detail |", "|---|---|---|---|"]
        for i, (criterion, status, detail) in enumerate(self.rows, 1):
            lines.append(f"| {i} | {criterion} | **{status}** | {detail.replace('|', '/')} |")
        lines += ["", f"**Overall: {'FAIL' if self.failed else 'PASS'}**", ""]
        return "\n".join(lines)


def git(*args):
    result = subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True)
    return result.returncode, result.stdout.strip()


def load_asmdefs():
    found = {}
    for dirpath, _, files in os.walk(TC):
        for f in files:
            if f.endswith(".asmdef"):
                with open(os.path.join(dirpath, f), encoding="utf-8") as fh:
                    data = json.load(fh)
                found[data["name"]] = data
    return found


def has_cycle(graph):
    state = {}

    def visit(node):
        if state.get(node) == 1:
            return True
        if state.get(node) == 2:
            return False
        state[node] = 1
        if any(visit(n) for n in graph.get(node, []) if n in graph):
            return True
        state[node] = 2
        return False

    return any(visit(n) for n in graph)


def run(report):
    # 1. Docs
    for doc in REQUIRED_DOCS:
        path = os.path.join(ROOT, doc)
        ok = os.path.isfile(path) and os.path.getsize(path) > 200
        report.check(f"Doc present and non-empty: `{doc}`", ok, "" if ok else "missing or empty")

    arch = open(os.path.join(ROOT, "Docs/ARCHITECTURE.md"), encoding="utf-8").read() if os.path.isfile(
        os.path.join(ROOT, "Docs/ARCHITECTURE.md")) else ""
    missing_gt = [f"GT-00{i}" for i in range(1, 9) if f"GT-00{i}" not in arch]
    report.check("ARCHITECTURE.md lists ground truths GT-001…GT-008", not missing_gt, ", ".join(missing_gt))

    # 2. Git
    _, branch = git("rev-parse", "--abbrev-ref", "HEAD")
    report.note("Current branch", branch)
    _, status = git("status", "--porcelain")
    report.check("Working tree clean", status == "", status.replace("\n", "; ")[:300])
    code, remote = git("ls-remote", "--heads", "origin")
    if code == 0:
        heads = {line.split("refs/heads/")[-1] for line in remote.splitlines()}
        for required in ("main", "develop"):
            report.check(f"Remote branch `{required}` exists ([WF] §17)", required in heads,
                         "" if required in heads else "not created yet — needs repository owner")
    else:
        report.note("Remote branches", "origin not reachable; skipped")

    # 3. Unity skeleton
    pv = os.path.join(ROOT, "ProjectSettings/ProjectVersion.txt")
    version = open(pv).read().split("\n")[0] if os.path.isfile(pv) else ""
    report.check("ProjectVersion.txt pins a Unity 6 editor", "m_EditorVersion: 6000." in version, version)
    manifest_path = os.path.join(ROOT, "Packages/manifest.json")
    deps = json.load(open(manifest_path))["dependencies"] if os.path.isfile(manifest_path) else {}
    for package in REQUIRED_PACKAGES:
        report.check(f"Package `{package}` in manifest", package in deps, deps.get(package, "missing"))
    for folder in REQUIRED_FOLDERS:
        ok = os.path.isdir(os.path.join(TC, folder))
        report.check(f"Folder `Assets/TramChanh/{folder}`", ok)
    for f in (".gitignore", ".gitattributes", ".editorconfig"):
        report.check(f"Repo file `{f}`", os.path.isfile(os.path.join(ROOT, f)))
    attributes = open(os.path.join(ROOT, ".gitattributes")).read() if os.path.isfile(os.path.join(ROOT, ".gitattributes")) else ""
    report.check("Git LFS configured for FBX/PNG/BLEND", all(f"*.{e} filter=lfs" in attributes for e in ("fbx", "png", "blend")))

    # 4. Assembly graph
    asmdefs = load_asmdefs()
    report.check("Assembly set matches ARCHITECTURE.md §4", set(asmdefs) == set(EXPECTED_REFS),
                 f"extra={sorted(set(asmdefs) - set(EXPECTED_REFS))} missing={sorted(set(EXPECTED_REFS) - set(asmdefs))}")
    graph = {}
    for name, expected in EXPECTED_REFS.items():
        if name not in asmdefs:
            continue
        actual = sorted(r for r in asmdefs[name]["references"] if r.startswith("TramChanh."))
        graph[name] = actual
        want = sorted("TramChanh." + e for e in expected)
        report.check(f"`{name}` references", actual == want, f"actual={actual}" if actual != want else "")
    for source, target in FORBIDDEN:
        report.check(f"`{source}` does not reference `{target}`", target not in graph.get(source, []))
    report.check("No assembly reference cycles", not has_cycle(graph))
    tests = [a for n, a in asmdefs.items() if n.startswith("TramChanh.Tests.")]
    report.check("Test assemblies are test-only (UNITY_INCLUDE_TESTS, not auto-referenced)",
                 all("UNITY_INCLUDE_TESTS" in a["defineConstraints"] and not a["autoReferenced"] for a in tests))

    # 5. Ground-truth guardrails
    offenders = []
    for top in ("Assets", "ArtSource"):
        for dirpath, _, files in os.walk(os.path.join(ROOT, top)):
            offenders += [os.path.join(dirpath, f) for f in files if OLD_SIGN_PATTERN.search(f)]
    report.check("GT-002: no old-sign asset files", not offenders, ", ".join(offenders))
    sign_assets = []
    for dirpath, _, files in os.walk(TC):
        sign_assets += [f for f in files if re.match(r"(PF|SM)_Sign_", f) and "TramChanh_New" not in f]
    report.check("GT-002: every sign asset is the NEW sign", not sign_assets, ", ".join(sign_assets))

    superseded = re.compile(r"\b(PF_MeasuringCup_500ml|SM_MeasuringCup_500ml|PF_BatterCup|PF_BatterMeasureCup|SM_BatterMeasureCup)\b")
    stale = []
    for top in ("Docs", "Assets", "ArtSource", "Automation"):
        for dirpath, _, files in os.walk(os.path.join(ROOT, top)):
            if os.path.join("Docs", "Reference") in dirpath:
                continue  # verbatim source documents (errata banner at the top)
            for f in files:
                if not f.endswith((".md", ".cs", ".py", ".asset", ".prefab", ".json")) or f == os.path.basename(__file__):
                    continue
                path = os.path.join(dirpath, f)
                for n, line in enumerate(open(path, encoding="utf-8", errors="ignore"), 1):
                    if superseded.search(line):
                        stale.append(f"{os.path.relpath(path, ROOT)}:{n}")
    report.check("Canonical batter cup name only (`PF_BatterMeasureCup_500ml`)", not stale, ", ".join(stale))

    gt = os.path.join(TC, "Scripts/Core/GroundTruth/StallDimensions.cs")
    src = open(gt).read() if os.path.isfile(gt) else ""
    report.check("GT-001 constants (1.8 / 0.8 / 1.0 / 1.2) defined once in StallDimensions.cs",
                 all(v in src for v in ("Width = 1.8f", "Depth = 0.8f", "CounterHeight = 1.0f", "CounterToRoof = 1.2f")))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--report", default=None)
    args = parser.parse_args()

    report = Report()
    run(report)
    _, commit = git("rev-parse", "--short", "HEAD")
    header = (f"# QA-000 — Repo, docs and skeleton check\n\n"
              f"- Date: {datetime.date.today().isoformat()}\n- Commit: `{commit}`\n"
              f"- Script: `Automation/qa000_repo_check.py` (read-only, no Unity required)")
    text = report.markdown(header)
    print(text)
    if args.report:
        os.makedirs(os.path.dirname(os.path.join(ROOT, args.report)), exist_ok=True)
        with open(os.path.join(ROOT, args.report), "w", encoding="utf-8") as fh:
            fh.write(text)
    return 1 if report.failed else 0


if __name__ == "__main__":
    sys.exit(main())
