# MAIN-001 handoff — playable SCN_TramChanh_Main

Branch: `wave/MAIN-001-integration` (draft PR #22 → `develop`). Coordination: issue #20.

## What is in the branch

| Part | Source |
|---|---|
| ACCEL-01 contracts, order delivery, roadside environment, cake station | #17, #16, #18 (merged in the wave) |
| Drink prompt fix and Ready-slot gate | MAIN-002 `c7fbf68` |
| Cake review fixes F1–F4 (orphan discard, step events, uninitialized cup, Ready-slot gate) | MAIN-003 `4c76d50` |
| Mixed drink+cake order regressions | MAIN-004 `f950ff3` |
| Sync guide and Tram Chanh ▸ Scenes menu | #19 |
| `TramChanhMainBootstrap`, `SCN_TramChanh_Main`, `SO_PromptText_TramChanhMain`, `TramChanhMainSceneTests` | MAIN-001 |

## Polish wave (MAIN-101…105), merged

- **MAIN-101 (lead):**
  - The vehicle point trigger can now be aimed at (1 × 1.2 × 1 m instead of a 16 cm box on the ground).
  - The cake station's five points and the batter cup now have `InteractableRef`. Before this, the player could not focus them in Play Mode.
  - The HUD is wired up.
  - New PlayMode raycast reach tests.
- **MAIN-102:**
  - A drink whose order is cancelled can be discarded (`drink.discard_orphan`).
  - The tea rack restocks after its bags are used.
  - An empty batter cup can be put back (`cake.cup.return`).
  - A model check shows the Ready slots cannot deadlock.
  - Customers never leave in this slice. This is an explicit restriction.
- **MAIN-103:** HUD panels for active orders, the held item with its next step, toasts and hold progress, plus full Vietnamese/English prompt coverage (no raw keys).
- **MAIN-104:** `Docs/Coordination/ART_INTEGRATION_HANDOFF.md` and `Automation/art_contract_audit.py`.
- **MAIN-105:**
  - the Tram Chanh ▸ QA menu
  - `Automation/unity_validate.sh` for batch runs
  - `Automation/static_preflight.py`
  - `Docs/QA/UNITY_VALIDATION.md`
  - the test inventory

## Validation status

Run in the cloud container (no Unity):
- All runtime scripts compile against UnityEngine 2021.3.33 module reference DLLs, with an InputSystem stand-in.
- Static scene check: every GUID and prefab fileID resolves, with no unresolved local references.
- Harnesses: drinks 69/69, cakes 28/28, orders/lobby 180/181 (the one failure is the harness-only assembly-name test).

**Not run (required before merge):** Unity 6000.6.0f1 compile, EditMode and PlayMode suites, QA-000, Console check, and a manual Play Mode run.

## Steps on the Mac (canonical project in `Lưu trữ/game`)

1. Close Unity, or save and close any open scene.
2. `cd` into the project folder. Back up first, read-only:
   `bash ~/unity_local_sync.sh backup`
   (see `Docs/UNITY_LOCAL_SYNC.md` for how to get the script).
3. `git fetch origin`, then `bash ~/unity_local_sync.sh update wave/MAIN-001-integration`.
   The script refuses if tracked files are modified. Untracked files are kept.
4. Open the folder in Unity 6000.6.0f1 and wait for the import to finish.
5. Run **Tram Chanh ▸ Scenes ▸ Open Main Game (SCN_TramChanh_Main)**, then press Play.
6. Run **Tram Chanh ▸ QA ▸ Run All**. Then open Window ▸ General ▸ Test Runner and run EditMode, then PlayMode. The Console must have no errors.
   Or close Unity and run `bash Automation/unity_validate.sh`; the results go to `~/TramChanh-validation/<time>/SUMMARY.txt`.
   The full procedure and the acceptance gate are in `Docs/QA/UNITY_VALIDATION.md`.

## What you should see after pressing Play

- The night roadside street, with the green Tram Chanh stall in the middle.
- You start on the customer side. Three customers are waiting: the drink table, the cake table, and the takeaway vehicle.
- **E or left mouse button** at a customer opens the order-entry window. Press **Enter order**, then **Send to stall**.
- **Drink:** red tea rack → F to open → coconut jelly → lemon jelly → ice → hold F to shake → hold at the wipe area → Ready counter.
- **Cake:** pick up the 500 ml cup → hold at the batter area to measure → pour onto the grill → close the lid → open and flip → hold to cut → hold to add sauce → hold to roll vertically → wrap → Ready counter.
- When every item in the order is Ready, pick up the order at the counter and deliver it to the same table or vehicle. The order completes, and a new customer arrives a few seconds later.

## Known open items

- The Ready shelf holds 1 drink and 1 cake. The tea rack and the cake fill are blocked (`ready.slot_full`) until the Ready order is picked up. This is intentional and avoids the single-player soft-lock.
- Drink orphan discard: drinks have no discard path if an order fails. Nothing in the game fails orders today. This is MAIN-002 lead item 3.
- Docs drift to fix: DRINK_WORKFLOW §2.1 and the drink Delivered state, CAKE_WORKFLOW §2.3 and §6, ORDER_SYSTEM §7 and §9.
- Prompt key families: Cakes uses `game.paused` and `role.not_stall`, while Interaction uses `interaction.*`. Both are present in the main table. Pick one family later.
- The DEV seeds `_preheatSeconds` and `_openLidDegrees` on `PF_CakeStation` should be recorded under DEC-007 and DEC-013.
- PR #16, #17 and #18 are superseded by #22 once it merges. Close them then.
