# DRINK-001 — pre-portioned tea rack pickup

Open `Assets/TramChanh/Scenes/Test/SCN_TeaRackPickupTest.unity` and press Play. Aim at the red rack and press E / left click. The same pre-stocked bag moves to the camera hand anchor, rack stock decreases by one, and another pickup is blocked while the player's single hand slot is occupied. WASD/mouse movement and Esc pause use the existing player slice.

## Scope and architecture

- Plain C# `TeaBagPickup` / `TeaRackInventory` model only `Stored → Held`; `Held` is the user-requested pickup-slice name corresponding to the full drink workflow's `PickedUp` state.
- `TeaRackController` uses documented `IInteractable.Query/Execute` contracts, pure availability queries, Execute rechecks and typed `ActionBlocked` messages.
- Generic `HeldItemChanged` / `HeldItemView` bind the existing one-item slot to `PlayerCamera/HandSocket/HoldAnchor`, align `Anchors/HandGrip`, set the HeldItem layer and disable held colliders. View subscription lifetime is tied to scene ownership.
- Capacity and starting stock are editable `[Tbd("DEC-015")]` BalanceConfig data. The test asset seeds two bags from the existing two rack slots; this is provisional fixture data, not a real-shop capacity or tea quantity.
- `PF_TeaBag_PrePortioned` uses primitive closed-bag and tea visuals, canonical anchors and inactive future-content hierarchy names. No measured quantity, preparation step, recipe or final art is introduced.
- No assembly reference, package pin, canonical architecture or workflow document changes. Existing blockout/interaction scenes and stall/player/workstation prefabs remain unchanged. An EditMode test compares every original transform to the pickup test scene.

## Approved test-scene exception

`Docs/DRINK_WORKFLOW.md` D1 requires a pending drink ticket. The user explicitly approved: “Allow pickup-only test scene; defer ticket binding”. This slice opts into unbound pickup in the dedicated test scene only. The controller defaults to `stall.no_ticket.drink`; its opt-in is also restricted to editor/development runs through `Debug.isDebugBuild`. No order is fabricated and the Lobby contract is unchanged. Production ticket claiming remains a later order/drink integration task.

## Validation

| Check | Result |
|---|---|
| Fresh C# compile | PASS; exit 0, zero C# errors/warnings |
| Full EditMode | 77 passed, 0 failed, 0 skipped (11 new cases) |
| Full PlayMode | 20 passed, 0 failed, 0 skipped (5 new cases) |
| Input System testability | PASS; both test assemblies and Unity.InputSystem.TestFramework compile from the fresh checkout |
| QA-000 before fresh import | PASS; 90 criteria plus one branch information row |
| Saved scenes/prefabs | PASS; scene references, bag anchors and every original transform checked; no serialization errors |
| Package resolution and tracked serialization | Manifest, lockfile and every tracked file unchanged after fresh import and both suites |
| Manual native Unity smoke | E pickup, visible held bag, decreased rack stock, second pickup blocked; Console 0 logs / 0 warnings / 0 errors |
| Query allocation/pause guards | PASS; 1,000 pure Query calls allocate no GC memory; paused Execute preserves stock |
| Mutation experiment | Reversing the Stored transition guard fails two domain tests; original code restored before final fresh validation |
| Whitespace/metadata | PASS; task metadata GUIDs unique, task assets retain matching metadata |

Fresh compile and both suites tested implementation commit `736941b306ef6bcabdfeb90f7b1878a6d86d4393`, based on develop `36db3ce9aa3afa40d8e1caa8d8c9c8c5983d4e94`. This later reporting commit changes only QA report files. The QA-000 details are in [QA-000-DRINK-001.md](QA-000-DRINK-001.md).

The committed implementation was validated from a fresh local Git checkout with no Library cache or copied user settings, using Unity 6000.6.0f1, URP 17.6.0, Input System 1.20.0 and Test Framework 1.8.0. Run QA-000 before Unity imports generate local settings. Validation artifacts are retained under `/private/tmp/DRINK-001`; the manual pickup screenshot is `manual-pickup.png`.

Reproduce with a clean checkout and the repository-pinned editor:

```sh
python3 Automation/qa000_repo_check.py
DRINK001_UNITY="/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity"
"$DRINK001_UNITY" -batchmode -nographics -quit -projectPath "$PROJECT_PATH" -logFile /tmp/DRINK001-Compile.log
"$DRINK001_UNITY" -batchmode -nographics -runTests -projectPath "$PROJECT_PATH" -testPlatform EditMode -testFilter TramChanh.Tests -testResults /tmp/DRINK001-EditMode.xml -logFile /tmp/DRINK001-EditMode.log
"$DRINK001_UNITY" -batchmode -nographics -runTests -projectPath "$PROJECT_PATH" -testPlatform PlayMode -testFilter TramChanh.Tests -testResults /tmp/DRINK001-PlayMode.xml -logFile /tmp/DRINK001-PlayMode.log
```

## Limitations

Ticket binding, return/drop/refill, opening, toppings, ice, shaking, wiping and ready/order completion are deferred. `TeaBagItem` implements IHoldable only; the later full drink slice will add held actions and prepared-item contracts. `Held` is terminal for this pickup slice. No standalone player build or other-platform validation is claimed; PlayMode tests use Editor scene-loading APIs. Final bag dimensions/art and rack capacity remain provisional.

The local project contained generated settings and unsaved scene edits. Those files were preserved and excluded from the PR; a local-only `SCN_Local_PreDrinkPickup` backup preserves the previous unsaved scene. QA-000's clean-tree gate was evaluated in the clean checkout.

Every changed file, including metadata, is listed in [DRINK-001-changed-files.txt](DRINK-001-changed-files.txt).
