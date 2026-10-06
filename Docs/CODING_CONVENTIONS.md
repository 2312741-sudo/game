# TRAM CHANH — CODING CONVENTIONS

**Task:** CX-001 · **Applies to:** everything under `Assets/TramChanh/Scripts` and `Assets/TramChanh/Tests`
**Enforced by:** `.editorconfig`, assembly definitions, review gates, `Automation/qa000_repo_check.py`

## 1. Project layout

- Unity project root = repository root. Unity 6 (`ProjectSettings/ProjectVersion.txt`), URP, Input System, Unity Test Framework.
- One assembly per module (`ARCHITECTURE.md` §4). Adding a reference between `TramChanh.*` assemblies needs Claude Code review; QA-000 fails on any reference not in the architecture table.
- Namespaces = `TramChanh.<Module>[.<Subfolder>]`. Exceptions to avoid clashing with Unity types:
  - `Scripts/Debug` → assembly and namespace `TramChanh.DevTools` (a `TramChanh.Debug` namespace would hide `UnityEngine.Debug`).
  - `Scripts/Editor` → assembly `TramChanh.Editor`, namespace `TramChanh.EditorTools` (a `TramChanh.Editor` namespace would hide `UnityEditor.Editor`).

## 2. C# style

- C# 9 (Unity 6). 4-space indent, Allman braces, braces always.
- One public type per file; file name = type name.
- `PascalCase` types/methods/properties/constants; `_camelCase` private fields; `camelCase` locals/parameters.
- Serialized fields: `[SerializeField] private` + read-only property. No public mutable fields.
- No `FindObjectOfType` / `GameObject.Find` for services; resolve from `ServiceRegistry` (CX-002).
- No static mutable singletons.
- No allocations in per-frame paths (`Update`, `IInteractable.Query`): no LINQ, closures, boxing or string concatenation.

## 3. Ground truth vs provisional data

- **Ground truth** (confirmed real-world facts, `ARCHITECTURE.md` §1) may be code constants and live only in `TramChanh.Core.GroundTruth` (e.g. `StallDimensions`).
- **Provisional values** — anything not yet confirmed from the real shop (station positions, sign size, batter amount, cooking time, burn time, topping quantities, hold durations, sauce mapping, vehicle size) — **must never be code constants**. They live in:
  - ScriptableObjects (`SO_*`), with the field marked `[Tbd("DEC-xxx")]`;
  - prefab/scene transforms (station anchors: `StallAnchor` with *Position Confirmed* off).
- Placeholder prefabs carry `PlaceholderAsset`. Gameplay code may depend only on a placeholder's hierarchy names, anchors and colliders, so swapping in final art changes no script.
- Editor blockout builders may contain provisional geometry numbers, clearly labelled as such, because they only seed placeholder assets.

## 4. Domain vs adapters

- Domain state machines (orders, drink, cake, grill) are plain C#: no `MonoBehaviour`, no `UnityEngine.Object` references, no `Time.*`; time comes from `IGameClock`.
- MonoBehaviours are thin adapters (input → domain call; domain event → visuals).
- Workflow step order (GT-007, GT-008) is a code constant covered by tests, never data.
- Implementation details whose real-world form is not confirmed (e.g. the physical form of the cake *Flip* step) sit behind an interface so they can change without touching the state machine.

## 5. Tests

- EditMode for all domain logic and validators; PlayMode for smoke and end-to-end.
- Test names carry their spec id: `GT_001_…`, `TC_ORDER_004_…`, `TC_DRINK_003_…`.
- Every bug fix adds a failing test first.

## 6. Assets

- Prefixes: `PF_`, `SM_`, `MAT_`, `T_`, `SO_`, `AN_`, `SFX_`, `AMB_`, `MUS_`, `SCN_`; placeholders without a final asset spec use `PF_Placeholder_`.
- Text serialization, visible meta files (Unity 6 defaults). Commit `.meta` files with their assets.
- Binary art/audio via Git LFS (`.gitattributes`).

## 7. Git

- `main` / `develop` / `feature/*` / `review/*` / `qa/*`; one worktree per task ([WF] §17–18).
- Commit messages: imperative summary line with task id, e.g. `CX-020: add order state machine`.
- Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, IDE files.
