# PR #15 — Claude Code independent review (DRINK-WAVE playable Lobby-to-Ready integration)

| | |
|---|---|
| PR | [#15 DRINK-WAVE: playable Lobby-to-Ready drink integration](https://github.com/2312741-sudo/game/pull/15) (draft) |
| Head reviewed | `395b8f1` (`feature/DRINK-WAVE-integration`): composition `0284c6c` + `88d4e6a` + the validation report. Rebased onto `develop` `d9143c1` (PR #13 merged, PR #14 `7158e3c`) |
| Tested head | `8a2ab06` (fresh remote clone, root): Unity 6000.6.0f1 compile 0 C# diagnostics, EditMode 284/284, PlayMode 33/33, QA-000 clean PASS. `git diff 8a2ab06 395b8f1 -- Assets Packages ProjectSettings` is empty, so the tested assets and the reviewed head are identical; only `QA/Reports` differ |
| Reviewer | Claude Code (Technical Lead / Architect), independent of the implementation |
| Date | 2026-10-08 (local) |
| Verdict | **APPROVED** — no blocking findings. Merge condition M1 (real pointer clicks reach the order-entry buttons) was **satisfied at head `3ccbafa` on 2026-10-08** (see "M1 re-verification"); only the usual gates remain. I did not merge and did not touch any gameplay branch |

## Scope inspected

Composition commits `a7f79bc` / `d53dc18` (now `0284c6c` / `88d4e6a`): `DrinkWaveBootstrap`, `DrinkWaveSceneLoader`, `StationToolView`, `DrinkWaveSceneBuilder` (editor), `SCN_DrinkWave.unity`, 7 prefab variants (`Prefabs/Workstations/DrinkWave`), 2 tool prefabs, the modified `PF_TeaBag_PrePortioned.prefab`, 4 animator controllers and 8 clips, 4 ScriptableObjects (balance, item, recipe, localization), `DrinkWaveAssetTests` (4 EditMode) and `DrinkWaveFlowTests` (3 PlayMode), `QA/Reports/DRINK-WAVE-integration.md`. The Ready, held-prompt and earlier commits were reviewed separately (PR #10/#11/#13/#14 reviews).

## Verified independently (static; no Unity editor here)

| Check | Result |
|---|---|
| Originals unchanged | `git diff --quiet origin/develop 395b8f1` is empty for `SCN_Gameplay_Blockout.unity`, `Prefabs/Stall`, `Prefabs/CustomerArea`, the three original workstation prefabs, `Packages`, `ProjectSettings`, `Core/GroundTruth`, `Stall/Anchors`. The only pre-existing asset touched is `PF_TeaBag_PrePortioned.prefab` (state view, animator, hooks), shared with `SCN_TeaRackPickupTest` |
| Saved GUID references | 29 YAML files (scene, prefabs, assets, controllers, clips): 0 missing GUIDs, 0 duplicate GUIDs across 347 indexed metas, every local `fileID` defined in its file |
| Script bindings | 45 `MonoBehaviour`s: each `m_Script` resolves to a class whose name matches the file, and every serialized key exists as a member (4 hits are `OrderPoint` base-class fields — false positives). The two `UIDocument`s share the project `PanelSettings` (sorting order 0 prompt, 10 order entry) |
| Unassigned references | none in the scene or prefabs, except `_interactionPoint` on the table and vehicle prefabs, which `Initialize(...)` sets at runtime. `DrinkWaveAssetTests` additionally pins all 13 `DrinkWaveBootstrap` references and both table assets |
| Interactable ids | station 1 rack, 2/3 toppings, 4 ice, 5 wipe, 6 ready counter, 7 pickup, 21 table, 22 vehicle: unique (see N2 for the bag) |
| Animation | the four controllers have the states, parameter types and transition conditions their views use (`Shaking` bool, `Active` bool, `Action` trigger; non-looping clips exit at 1.0); clip paths exist (`Visual`, `CoverPivot` is a direct child of the topping root and its 70° rest equals the prefab's rotation); the shake controller that was empty before `88d4e6a` now has 2 states with motions and 1 parameter, pinned by a test |
| Hooks | persistent `UnityEvent` listeners (`PlayOnce`, `Begin`, `Stop`) target `TramChanh.App.StationToolView`; the Drinks module has no compile dependency on the App assembly |
| Dev bypass | `DevelopmentPickupTicketQueue` / `TeaRackPickupTestSetup` are referenced only by `SCN_TeaRackPickupTest`; the drink-wave scene uses the real `StallTicketQueue` (GUARD-001 satisfied) |
| Metas | all added files and folders under `Assets` have `.meta` files |
| QA-000 | PASS on a clean checkout of the PR tree (my run, on the identical-asset head) |

## Conformance with the ground truths and amended docs

| Rule | Evidence | Verdict |
|---|---|---|
| GT-001/002 stall and sign unchanged | blockout scene and `Prefabs/Stall` byte-identical; no sign asset added | ✅ |
| GT-003 tea pre-portioned, no measuring | bag prefab and recipe `_teaBagType: Pre-portioned tea bag`; no measuring/pouring state or interactable anywhere in the composition | ✅ |
| GT-004/005/006 Lobby intake only | the rack query uses the real queue (`stall.no_ticket.drink` until Send); table 1 / vehicle 1 intake via `RequestCustomerService`; flow test pins blocked ticketless rack, Send-before-Enter rejected, no pending ticket after Enter | ✅ |
| GT-007 chain | Coconut (type 0, id 2) → Lemon (type 1, id 3) → Ice → hold Shake → hold Wipe → Ready; the flow test asserts lemon-first rejected, early Ready rejected twice, and the exact `DrinkStepCompleted` list `Opened…Wiped` (no Ready/Delivered step event) | ✅ |
| Station poses | station root placed at the anchor set's pose; each variant at the first child of its anchor, read from the saved blockout; original placeholders disabled at runtime only (the loaded scene is not saved) | ✅ |
| TBD values | shake/wipe 1 s and portions 0 are serialized editor seeds in `SO_Recipe_Drink_Slice`; no real-world quantity asserted; item name "Drink (menu name pending)", vehicle and art are `PlaceholderAsset`s | ✅ |
| DI | `Awake` (execution order −1000) builds clock, bus, ids, content, `OrderService`, `StallTicketQueue`, `ReadyShelf` and injects them; no statics or scene searches except the base-scene lookup of the anchor set; rack, ready counter and pickup are initialized while the station is still inactive, so subscriptions start in `OnEnable`; `OnDestroy` disconnects UI/views/interactor, disposes the shelf and the bus | ✅ |
| Modal pause guard | `Shown` disables `PlayerInputReader` (its `OnDisable` pauses) and frees the cursor, so the Interact binding cannot re-capture over a button; `Hidden` closes the entry session, re-enables the reader and re-captures; `OnDestroy` unsubscribes before `Disconnect`; the open session accepts Enter/Send while paused and a new point interaction stays blocked; Cancel keeps the order in `TakingOrder` with no ticket (test) | ✅ (see M1) |
| Animation vs clock | all station animators (including the bags instantiated by the inactive rack) are collected and `speed` follows `IsPaused`; shake/wipe completion stays on the injected clock | ✅ |
| Scope | no delivery, completion, customer spawning or stock refill; two scripted requests only | ✅ |

## Blocking findings

None.

## Merge condition (satisfied — see M1 re-verification below)

**M1 — prove that a real mouse click reaches the three entry buttons.** While the entry is open the composition disables the gameplay input map, including Esc, and relies on the UI Toolkit buttons (Enter, Send, Close) to leave the modal. The project uses the Input System package; no scene `EventSystem` exists, and no test clicks a button (the tests call `Enter()` / `Send()` / `Cancel()` directly). If pointer events do not reach the panel under this configuration, the player is soft-locked with the clock paused. Before merge, do one Editor Play check (click Enter, Send and Close with the mouse) or add a PlayMode pointer test; as hardening, bind Enter/Esc keys to Enter and Close while the modal is open.

## Non-blocking findings

- **N1 — localization drift.** `SO_PromptText_DrinkWave` is a generated copy of the prompt rows plus the order-entry asset. It lacks keys the code can show: `drink.need_pickup`, `drink.bag.not_stored`, `interaction.held_item_changed` (wipe), `ready.no_complete_order`, `ready.quality.invalid`, `stall.ticket.*`. The asset test only covers the order-entry keys. Extend it (or QA-000) to check every reason/prompt literal.
- **N2 — interactable id namespace.** `TeaBagItem.Id` is built from the `PreparationId`; the shared sequential generator gives the two stocked bags ids 1 and 2, which equal the rack (1) and the coconut bin (2). Today ids are used for diagnostics and prompt de-duplication only, so nothing breaks; give held items a separate id space before ids drive behaviour.
- **N3 — post-pickup dead end (known scope boundary).** After the whole-order pickup the `ServedOrder` stays in the hands, and take-order, rack pickup and pickup all require empty hands; the second scripted customer can only be served if it was entered and sent before the first pickup. Stock is 2 bags with no refill. Delivery/completion (CX-025) must add the way out; record it as an acceptance item.
- **N4 — reach and spawn are not covered.** The flow tests call `Execute` directly, so movement, raycast reach (1.8 m) and the provisional spawn at (−0.72, 0, −1.2) are unverified. Add a PlayMode smoke that aims at each station and reads the prompt, or a documented manual pass.
- **N5 — builder hygiene.** `CreateAndSave` returns early when the scene exists (no rebuild path); `RepairShakeVisual` and `RepairSavedSceneReferences` are one-off repair entry points left public; variant prefabs reuse the originals' names in a second folder (`PF_RedTeaRack.prefab` twice), which can trip name-based tooling. Move the repairs into tests or delete them once the assets are stable.
- **N6 — tests.** Animation coverage pins only the shake controller; the other three controllers and the hook wiring were checked statically by me, not by tests. Ice-before-lemon and shake-before-ice are covered by the PR #11 domain tests, not the saved-scene flow.
- **N7 — standalone build.** `DrinkWaveSceneLoader` loads by path (Editor path via `EditorSceneManager`); a player build needs both scenes in the build settings. Out of scope here and not claimed by the PR.

## Taken from the author's report (not re-run: no Unity editor here)

Fresh-clone Unity compile with 0 diagnostics, EditMode 284/284, PlayMode 33/33, QA-000 clean PASS at `8a2ab06`; the RED-then-GREEN regressions for the missing scene ScriptableObject references and the empty shake controller (`QA/Reports/DRINK-WAVE-integration.md`).

## Merge readiness

Safe to merge after M1 and the standard gates (the fresh-checkout run already covered `8a2ab06`; assets are identical at `395b8f1`). Mark the PR ready only after the final head's CI-equivalent run if the root rebases again. No gameplay branch was modified by this review.

## M1 re-verification — head `3ccbafa` (2026-10-08)

**M1 is satisfied.** The root added `TC_ORDER_UI_MousePipelineActivatesEnterSendAndCloseWithoutRecapturingGameplay` and `TC_ORDER_UI_DestroyedGameplayInputDoesNotBreakModalClosure` and one production guard. Relative to `395b8f1` the delta is exactly two files (`DrinkWaveBootstrap.cs`, `DrinkWaveFlowTests.cs`); I read both.

- **What the test proves.** A real `Mouse` device is added to the Input System and driven only with queued `MouseState` events (position, then left-button press and release). The three buttons are found in the laid-out runtime panel, and each click is confirmed as a real pointer event (a trickle-down `PointerDownEvent` and `PointerUpEvent` counter, each exactly 1) before asserting the effect: Enter moves the order to `Entered` with the modal still open, the clock paused, the cursor unlocked and the gameplay reader still disabled; Send moves it to `SentToStall`, closes the modal and resumes the clock; Close on a second order leaves it in `TakingOrder` and releases modal input and clock ownership. No synthetic UI events and no direct button invocation are used. The screen-to-panel coordinate mapping accounts for the scale-with-screen-size panel.
- **Test hooks.** In batch mode only, the internal `UIElementsRuntimeUtility.eventSystemUpdateMode` is switched to `Always` through reflection (asserted present, so a Unity upgrade fails loudly), and the Input System background and editor-input behaviours are set for the test; all three are restored in a `finally`, and the test mouse is removed. This is brittle against Unity changes but isolated and pinned to 6000.6.0f1. An interactive focused Editor uses the same pipeline.
- **Production change.** `OnEntryHidden` now null-checks the lobby controller and input reader (Unity fake-null) so scene teardown that destroys the player before the modal raises `Hidden` no longer throws; the second test destroys the reader, cancels the modal and requires no unexpected log. Correct and minimal.
- **Reported results (the root's, not re-run by me):** compile 0 C# diagnostics, EditMode 284/284, PlayMode 35/35, QA-000 clean 91/91 at `3ccbafa`. Assets, Packages and ProjectSettings are otherwise identical to the tested `8a2ab06`.
- **Still recommended (non-blocking):** the keyboard fallback (Enter/Esc while the modal is open) as hardening; findings N1–N7 above are unchanged. Note that the ACCEL-01 wave branch carries the same two files as its own commit `6cf0e95` (identical content), so it must be rebased or merged onto `develop` after PR #15 lands rather than merged twice.

Remaining gates are the root's: a clean current head and the merge itself.

---
_Generated by [Claude Code](https://claude.ai/code)_
