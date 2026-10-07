# PR #3 — Claude Code review (REV-001)

| | |
|---|---|
| PR | [#3 CX-010: add the playable first-person interaction preview](https://github.com/2312741-sudo/game/pull/3) |
| Head reviewed | `944f9407c472bf6ab7d2fddc67cd8314d3d7e540` |
| Base | `develop` @ `97d85ebac18de388050e5e3d8ec57fa9eda81631` |
| Reviewer | Claude Code (Technical Lead / Architect) |
| Date | 2026-10-07 |
| Verdict | **CHANGES REQUESTED — do not merge yet** |

> Posted on GitHub as a review *comment*: GitHub does not allow "Request changes" on a PR
> opened by the same account. The verdict is still **CHANGES REQUESTED**.

Reviewed against `Docs/ARCHITECTURE.md`, `Docs/INTERACTION_SYSTEM.md`, `Docs/CODING_CONVENTIONS.md`,
`Docs/PROJECT_TASK_PLAN.md` and ground truths GT-001…GT-008.

**Summary:** the interaction code follows the approved architecture and needs no redesign.
Merging is blocked by the environment (pinned Unity/package versions vs. the versions used
for validation), not by the code. The scope also covers CX-010 to CX-012, not CX-010 alone.

## Checklist

| # | Check | Result |
|---|---|---|
| 1 | Scope is CX-010 only | ❌ CX-010 + CX-011 + CX-012, plus parts of CX-003 (`BalanceConfig`) and CX-004 (test bootstrap) |
| 2 | Movement/camera separated from interaction | ✅ `FirstPersonController` / `PlayerInputReader` are separate components in `Interaction/Player` |
| 3 | `PlayerInteractor` follows the interaction contract | ✅ Interactable-layer raycast, reach from data, prompt published on change only, hold/continuous on the injected `IGameClock`, Hidden/Blocked handled |
| 4 | `IInteractable` not coupled to tea/cake | ✅ `TramChanh.Interaction` references only Core + Input System |
| 5 | Tea rack / grill are placeholders | ✅ `InspectionInteractable` only toggles a highlight; no tea bag, grill state or order logic |
| 6 | No hard-coded recipe values | ✅ Player tuning is `[Tbd]` data in `SO_Balance_PlayerPreview`; builder seeds are labelled provisional |
| 7 | Ground truths unchanged | ✅ `Docs/`, `Core/GroundTruth`, `Scripts/Stall`, stall/tea-rack/grill prefabs, blockout scene, `ProjectSettings/`, `Packages/` untouched |
| 8 | No unrelated files | ⚠️ Nothing unrelated; scope creep (#1) plus 15 `.meta` files for files that already existed on `develop` |
| 9 | Test coverage | ✅ TC-INT-001…008, zero-allocation checks, reach/layer, pause, scene references (but see blocking issue 1) |
| 10 | Prefab/scene references | ✅ All 26 scene GUIDs and all prefab/data-asset GUIDs resolve; explicit serialized references, no scene searches |

**Why there are 100 files.** All of them are legitimate:

- 59 `.meta` files: 44 for new items, 15 for pre-existing items.
- 29 `.cs` files.
- 12 assets: scene, 2 prefabs, 2 data assets, input actions, UI theme + panel, 2 modified asmdefs, 2 QA reports.

There are no duplicated commits from #1/#2 and no stray files.

## 🔴 Blocking

1. **The PlayMode test assembly won't compile on a fresh checkout.** `PlayerInteractionTests` uses
   `InputTestFixture`, and the PlayMode asmdef now references `Unity.InputSystem.TestFramework`.
   That assembly is only built when `Packages/manifest.json` contains
   `"testables": ["com.unity.inputsystem"]`, and this PR does not change the manifest. The 15/15
   PlayMode pass came from a disposable project copy. One compile error blocks Play Mode for the
   whole project. **Fix:** add the `testables` entry.
2. **Validated against a different environment than the repo pins.**
   - Validation used Unity 6000.6.0f1 / URP 17.6 / Input System 1.20 / Test Framework 1.8.
   - The repo pins 6000.0.40f1 / 17.0.3 / 1.11.2 / 1.4.5.
   - The scene, prefabs and assets were serialized by 6000.6 (e.g. the new
     `m_EditorClassIdentifier` format). Opening them in the pinned editor may reserialize them or fail.
   - The 6000.0.40f1 pin itself was never verified.

   **Fix:** in a separate small PR, re-pin `ProjectVersion.txt` and `manifest.json` to the versions
   actually in use. Then rebase this PR and re-run EditMode/PlayMode in the real repository.
3. **Scope / gate labelling.** The tests are named `CX_011_*` and the cube is labelled
   "CX-012 test fixture". Splitting the PR is not required. **Fix:** retitle it as CX-010–CX-012
   and update the status board in `PROJECT_TASK_PLAN.md`. This review can be recorded as REV-001.

## 🟡 Non-blocking

- **Missing `.meta` files on `develop`.** About 30 files (most asmdefs, `StallDimensions.cs`,
  `StallPlaceholderBuilder.cs`, …) and about 20 folders still have no `.meta`, a pre-existing gap
  from CX-001. Unity assigns random GUIDs per machine, so parallel PRs will conflict. The 15 partial
  metas in this PR show the pattern. Suggest a one-off chore PR that commits all generated metas
  from the pinned editor.
- **Restart needed after `EnsureInputSystem`.** It switches `activeInputHandler` to Both, which only
  takes effect after an editor restart. Log an explicit "restart required" message.
- **Pause cancels holds instead of freezing them.** A paused clock clears focus, which cancels any
  hold. TC-INT-006 asks for progress to freeze. The driver-level test passes; the in-game behaviour
  differs.
- **`InspectionInteractable.Awake` can throw.** It reads `sharedMaterial.GetColor("_BaseColor")`,
  which throws if a renderer has no material, and it handles only the first material slot.
  Acceptable for a fixture.
- **QA reports point to local `/private/tmp/...` paths**, so the evidence is not reproducible.
  The `QA/` folder is owned by Antigravity.

## 🔵 Architecture notes (acceptable for this test slice; address before production)

- **The test scene is a full copy of `SCN_Gameplay_Blockout`**, including its 43 position
  overrides. `GT_001_InteractionScene_RetainsStallDimensionsAndStationLayout` correctly compares
  the copy against the original rather than hard-coded values. However, the builder returns early
  once the scene exists, so DEC-011 layout updates will require regeneration. Loading the blockout
  scene additively would remove the duplication.
- **`PlayerInputReader` pauses and resumes the global clock.** Pause belongs to `GameState`
  (CX-004). Production code should send a pause request to `GameState` instead.
- **Test-only code ships in builds.** `PlayerInteractionTestBootstrap` (in `TramChanh.App`) and
  `InspectionInteractable` (in `TramChanh.Interaction`) are in runtime assemblies. Consider moving
  them to `TramChanh.DevTools` or putting them behind a define.
- **`PlayerInteractor` depends on the concrete `PlayerInputReader`.** A small input interface would
  improve testability.

## Merge readiness

**Not safe to merge into `develop` yet.** Ready once:

1. the version pin is reconciled in its own PR;
2. Input System `testables` is added;
3. EditMode + PlayMode pass in the real repository;
4. the PR is retitled and the status board updated.

---
_Generated by [Claude Code](https://claude.ai/code)_
