# CX-010–CX-012 — first playable player interaction slice

Open `Assets/TramChanh/Scenes/Test/SCN_InteractionTest.unity` and press Play.
WASD moves; mouse looks; E / left click inspects; Esc releases the cursor and pauses the preview clock. Click or E recaptures input without executing an inspection on that frame.

Tea rack, grill and test cube toggle a green inspection highlight and localized prompt. No ingredients are created, nothing is cooked and no real workflow is changed.

## Implementation

- Documented IInteractable contracts, typed Core messages and injected IGameClock.
- Scene-owned PlayerInteractionTestBootstrap, using the canonical self-contained test-scene exception. Production bootstrap/GameState/SceneLoader remain deferred.
- PF_Player: CharacterController, 1.6 m camera, HandSocket, input reader, movement and PlayerInteractor.
- Collider-parent InteractableRef lookup, Interactable-only raycast, SO-configured reach.
- Pure Query, prompt events only on change, localized text cached separately from progress.
- Press/Hold/Continuous driver and one logical held-item slot; only Press inspection is exposed here.
- Editable [Tbd] BalanceConfig and English/Vietnamese prompt assets.
- Existing project setup enables Input System on legacy-only checkouts. No local package/version upgrades are committed.
- Original blockout scene and pre-existing stall/sign/workstation prefabs are byte-for-byte unchanged. Tests compare every anchor/child pose and confirmed dimensions.

## Validation

| Check | Result |
|---|---|
| C# compilation | PASS; zero compiler errors or warnings |
| Full EditMode | 66 passed, 0 failed, 0 skipped |
| Full PlayMode | 15 passed, 0 failed, 0 skipped |
| Native GC checks | PASS: repeated Query and unchanged prompt rendering |
| Original manual Unity preview | Tea rack prompt, E highlight/prompt response, Esc cursor release; Console 0 errors / 0 warnings |
| QA-000 clean committed checkout | PASS; repository, docs, ground truths and assembly graph |
| Source/assembly whitespace | PASS |
| Original blockout/prefabs | SHA-256 comparison PASS |
| Architecture and real-world workflows | Unchanged; task status board updated for REV-001 / CX-010–CX-012 |

Validation used a fresh Git checkout with no Library cache and no copied local settings, using the committed Unity 6000.6.0f1 manifest and lockfile. Package resolution preserved the lockfile; both Unity.InputSystem.TestFramework and TramChanh.Tests.PlayMode compiled. Saved scene/prefab tests passed without serialization errors, and all tracked Assets remained byte-for-byte unchanged. Compile/test XML and logs and QA-000 reports are retained at `/private/tmp/CX-010-env-alignment`. Original manual screenshots remain at `/private/tmp/CX-010-delivery`.

Regression tests cover saved asset references surviving scene opening, Input System backend setup, tiny per-frame movement and prompt allocations. PF_Player retains small movements with zero minimum move distance, per [Unity guidance](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/charactercontroller/minmovedistance).

Reproduce from a fresh checkout with the repository-pinned editor. Run QA-000 before opening Unity so generated local settings do not affect its clean-tree criterion:

```sh
python3 "$PROJECT_PATH/Automation/qa000_repo_check.py"
CX010_UNITY="/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity"
"$CX010_UNITY" -batchmode -nographics -quit -projectPath "$PROJECT_PATH" -logFile /tmp/CX010-Compile.log
"$CX010_UNITY" -batchmode -nographics -runTests -projectPath "$PROJECT_PATH" -testPlatform EditMode -testFilter TramChanh.Tests -testResults /tmp/CX010-EditMode.xml -logFile /tmp/CX010-EditMode.log
"$CX010_UNITY" -batchmode -nographics -runTests -projectPath "$PROJECT_PATH" -testPlatform PlayMode -testFilter TramChanh.Tests -testResults /tmp/CX010-PlayMode.xml -logFile /tmp/CX010-PlayMode.log
```

## Saved hierarchy

```text
SCN_InteractionTest
├── Environment / GroundPlane
├── PF_Stall_TramChanh
│   ├── Existing frame, countertop, roof, NEW sign placeholder
│   └── Anchors
│       ├── TeaRackAnchor / PF_RedTeaRack + inspection
│       ├── ToppingStationAnchor / PF_ToppingStation
│       ├── IceBinAnchor / PF_IceBin
│       ├── GrillAnchor / PF_Grill_Elmich + inspection
│       ├── SauceAnchor
│       ├── WrappingAnchor
│       └── ReadyCounterAnchor / PF_ReadyCounterPoint
├── CustomerArea
├── ScaleReferences / PlayerHeightReference_1.7m
├── Lighting
├── BlockoutOverviewCamera (camera/listener disabled)
├── PF_Placeholder_InteractionCube
├── PF_Player / PlayerCamera / HandSocket
├── InteractionHUD
└── TestBootstrap
```

## REV-001 blocking changes

- Input System testability is supplied by merged ENV-001 / PR #4; the fixture-based PlayMode suite compiles and passes on a fresh checkout.
- The repository pins and validation environment now match: Unity 6000.6.0f1, URP 17.6.0, Input System 1.20.0, Test Framework 1.8.0. No validation-only package or settings overrides are used.
- The PR scope is CX-010–CX-012. The task-plan status board records delivery and the completed Claude review, with re-review / final approval pending.

All three blocking changes from Claude's review are addressed. The review's non-blocking findings and production architecture notes remain deferred; this update changes no runtime code.

## Dependencies and limitations

- Rebased onto origin/develop at `0055cad2d1d8446293e5cd1a57908a7090429fda`, which contains the merged CX-002, ART-STALL-001 and ENV-001 infrastructure PRs. Only the interaction/player/prompt slice remains above develop; dependency commits and environment files are excluded from this 101-file diff. Rebase had no conflicts and preserved all runtime source and saved assets.
- Claude Code completed REV-001 against the previous head, accepted the architecture and requested the blocking changes above. The task board records review completion, not final approval; re-review remains pending.
- Validated on macOS with the exact committed Unity 6000.6.0f1 and resolved packages. Other platforms have not been validated in this update.
- No standalone player build; PlayMode tests use Editor scene-loading APIs.
- No products, physical pickups/hand attachment, UseHeld/Discard gameplay, production, orders or final art. Corresponding contracts/bindings reserve future integration points.
- Reach, movement tuning, spawn/test-cube placement and pre-existing provisional station positions remain adjustable preview data.
- Existing local upgrades, generated unrelated settings and saved local scenes are excluded. Relevant Unity asset/folder metadata retains its GUIDs.

All files in the develop-targeted diff are listed in [CX-010-changed-files.txt](CX-010-changed-files.txt).
