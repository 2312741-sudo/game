# PR #5 — Claude Code review (DRINK-001)

| | |
|---|---|
| PR | [#5 DRINK-001: pick up pre-portioned tea bags from the red rack](https://github.com/2312741-sudo/game/pull/5) |
| Head reviewed | `f83f366337751c17bc960741140d84c5c2744f27` (3 commits) |
| Base | `develop` @ `36db3ce9aa3afa40d8e1caa8d8c9c8c5983d4e94` (contains PR #3) |
| Reviewer | Claude Code (Technical Lead / Architect) |
| Date | 2026-10-07 |
| Verdict | **APPROVED** — safe to squash-merge into `develop` |
| Merge status | Not merged by the reviewer. Merge is the Product Owner's / Codex's action. |

Reviewed against `Docs/ARCHITECTURE.md`, `Docs/INTERACTION_SYSTEM.md`, `Docs/DRINK_WORKFLOW.md`,
`Docs/CODING_CONVENTIONS.md`, `Docs/PROJECT_TASK_PLAN.md` and ground truths GT-001…GT-008.

**Summary:** the pickup slice is correct. `Stored → Held` is right and stock drops exactly once per
pickup. The one-slot, empty-rack and hands-full guards work, the code follows the Query/Execute
contracts, and the ground truths are intact. No blockers.

## Checklist (14 points requested)

| # | Check | Result |
|---|---|---|
| 1 | Tea remains pre-portioned | ✅ The rack holds stored bag objects; no quantity exists anywhere. |
| 2 | No tea-measuring step | ✅ `TeaBagState` has only `Stored` and `Held`. A vocabulary scan of all added code found "measure", "portion" and "ml" only in comments saying they are absent. |
| 3 | Stock drops exactly once per pickup | ✅ `Stock` counts bags still in `Stored`; `TryPickUp` is the only transition. Tests cover a second pickup of the same bag and a second press while holding. |
| 4 | `Stored → Held` correct | ✅ Matches D1 in `DRINK_WORKFLOW.md`, with the same blocked-reason keys (`hands.full`, `drink.rack.empty`, `stall.no_ticket.drink`). |
| 5 | One bag at a time | ✅ `HeldItemSlot` is a single slot; the rack blocks with `hands.full`. |
| 6 | Empty rack blocked | ✅ Blocks with `drink.rack.empty`, creates no held bag; a test confirms stock is unchanged. |
| 7 | Held-item display generic | ✅ `HeldItemView` / `HeldItemChanged` live in `TramChanh.Interaction`, work on any held item and do not mention tea. `HeldItemChanged` is the event listed in `ARCHITECTURE.md` §6. |
| 8 | Follows Query/Execute | ✅ `Query` is pure and allocation-free (a test runs it 1,000 times); `Execute` re-checks availability and publishes `ActionBlocked`; paused and wrong-role cases are blocked. |
| 9 | Art replaceable | ✅ Controller and bag prefab use only the canonical anchors from `ASSET_INTEGRATION.md`; `PF_RedTeaRack.prefab` is untouched. See architecture concern 5. |
| 10 | No topping/ice/shake/wipe/Ready logic | ✅ The bag prefab has empty, inactive `Bag_Open` and `Contents/{CoconutJelly,LemonJelly,Ice}` objects matching the documented hierarchy; no logic. |
| 11 | Ticket exception is test-only | ✅ Opt-in flag, additionally gated by `Debug.isDebugBuild`; `true` only in `SCN_TeaRackPickupTest` (searched every tracked scene/prefab). Creates no order and touches no order/ticket code. Per the PR, the Product Owner approved it ("Allow pickup-only test scene; defer ticket binding"). See architecture concern 2. |
| 12 | No guessed tea quantity | ✅ Rack capacity and initial stock are `[Tbd("DEC-015")]` data. The test asset uses 2 = the number of slots on the placeholder rack, not a shop figure. |
| 13 | Test coverage | ✅ Mostly; gaps listed below. |
| 14 | Scope reasonable | ✅ 48 files: drink domain and runtime, held-item support, a test scene, data assets, tests, QA reports. Shared-code changes are minimal (2 `BalanceConfig` fields, held-item events in `HeldItemSlot`, one bootstrap field). No `Docs/`, `Packages/`, `ProjectSettings/`, stall, blockout or workstation files changed. |

## Evidence

- **Checked by the reviewer:** read all new code, tests and scene wiring; the test counts in source
  are 77 EditMode / 20 PlayMode, exactly as the PR reports; QA-000 passes on a clean checkout of the
  PR head; GUIDs are unique and every new asset has a `.meta`; protected files are unchanged.
- **Taken from the PR's report** (macOS; logs kept locally): compile, the test results, and the
  manual smoke test. The review environment has no Unity editor. The PR also reports a mutation
  experiment (reversing the `Stored` guard failed two tests).

## Blockers

None.

## Non-blocking technical debt

1. **Untested guards:** the wrong-role block (`interaction.stall_role_required`) and a bag prefab missing an anchor.
2. **Brittle guard test:** `GT_003_NoTeaMeasuringStepOrQuantityIsIntroduced` pins the enum to exactly `{Stored, Held}` and `TeaBagPickup` to exactly one property, so CX-030 must rewrite it. A name-based check ("no `Measure*` member") would age better.
3. **Generic view tested only with a tea bag:** `HeldItemViewTests` uses `TeaBagItem`; a neutral dummy holdable would prove genericity.
4. **Duplicated data:** `SO_Balance_TeaPickupTest` copies the player-tuning values of `SO_Balance_PlayerPreview`; `SO_PromptText_TeaPickupTest` copies the prompt table.
5. **Missing `.meta` files** for `Scripts/Drinks/` (folder, `TramChanh.Drinks.asmdef`, `AssemblyInfo.cs`) — the known CX-001 gap; the cleanup is still pending.
6. **Plan not updated:** `DRINK-001` is not a task in `PROJECT_TASK_PLAN.md` (see the mapping on the status board).
7. **Misleading test name:** `TC_DRINK_008_..._CannotCreateAnOrder` asserts nothing about orders (no order service exists yet).

## Architecture concerns

1. **The state change hides inside a callback.** `Stored → Held` happens in `TeaBagItem.OnPickedUp`, called by `HeldItemSlot.TryPickUp` after `Current` is set, and it throws if the bag is not stored. That would leave the slot holding an item whose state did not change. It is unreachable today (the controller only offers stored bags) but fragile: two objects own one transition. Fix with `DrinkPreparation` (CX-030): make the transition first (controller/domain) and roll back if the slot refuses, or give `IHoldable` a `CanPickUp`.
2. **The ticket bypass sits inside the production controller.** Use a ticket-gate interface that CX-021's queue plugs into, with the dev bypass in `TramChanh.DevTools`. Until then, guard it (see `CODING_CONVENTIONS.md` §8): a Development Build of the vertical-slice scene would honour a stray `_allowUnboundPickupForTest: 1`.
3. **Name mismatch with the docs:** the PR uses `Held`; `DRINK_WORKFLOW.md` says `PickedUp`. Accepted for this pickup-only PR; the drink-preparation work must rename to `PickedUp` (see `DRINK-WAVE-claude-plan-review.md` C6).
4. **Scene copies are compounding:** `SCN_TeaRackPickupTest` (2,329 lines) copies `SCN_InteractionTest`, itself a copy of the blockout. Stop copying; compose additively.
5. **The controller lives in the scene, not the prefab:** a final `PF_RedTeaRack` needs `_interactionPoint` and `_bagSlots` rewired. INT-004 should move the controller into the prefab.
6. **`?.` on Unity objects:** `_heldItemView?.Initialize(...)` bypasses Unity's null check; use `if (_heldItemView != null)`. No failure observed (the PR reports its tests pass).
7. **Held items stay parented after release:** `HeldItemView` disables colliders and sets the layer but never restores them or reparents. Deliberate for now ("Held is terminal"), but the first placement step must restore both.

## Merge readiness

**Safe to squash-merge into `develop`.** GitHub reports it mergeable with no conflicts, it is based on
the current `develop`, and it touches no environment, doc or ground-truth files.

---
_Generated by [Claude Code](https://claude.ai/code)_
