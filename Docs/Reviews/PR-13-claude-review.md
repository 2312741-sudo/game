# PR #13 — Claude Code review of the Ready adapters (`ReadyCounterPoint`, `ReadyOrderPickupPoint`, `ServedOrder`)

| | |
|---|---|
| PR | [#13 DRINK-WAVE: Ready adapters](https://github.com/2312741-sudo/game/pull/13) |
| Head reviewed | `3421f3d` (docs-only on top of the runtime commit) |
| Runtime commit | `5c2f4b4` (`538d1d0` place/carry, `5c2f4b4` reentrant-failure reconcile) |
| Base | `develop` `01e50b4` (PR #8 squash). The branch is built on the **pre-squash** PR #8 commits (merge-base `eb10c26`), so the 3-dot diff shows all PR #8 files. Real delta = `git diff origin/feature/DRINK-WAVE-contracts origin/pr/13` = 14 files, +906. `git diff 01e50b4 origin/pr/13` gives the same 14 files, so the trees are identical and a rebase is mechanical |
| Reviewed against | Amended `ORDER_SYSTEM.md` §5–§6.4 and §10 (TC-READY-001…009), `DRINK_WORKFLOW.md`, `INTERACTION_SYSTEM.md`, `ARCHITECTURE.md`, `CODING_CONVENTIONS.md`, `ASSET_INTEGRATION.md`, `Reviews/PR-8-claude-review.md`, plan review C1–C12; the real shelf is PR #10 `3fd62e8` (read only, not reviewed) |
| Reviewer | Claude Code (independent review) |
| Date | 2026-10-07 |
| **Verdict (initial, head `3421f3d`)** | **CHANGES REQUESTED** — three small, local blockers (B1–B3). The architecture, capability narrowing, post-commit ordering and the `5c2f4b4` Failed-path fix are sound; the defects are in exception safety of the post-commit presentation, in the new `PickedUp` branch of the fix, and in missing tests for the headline bundle requirement |

## Scope inspected

All 14 changed files: `Scripts/Stall/Runtime/ReadyCounterPoint.cs` (193 lines), `Scripts/Lobby/ReadyOrderPickupPoint.cs` (106), `Scripts/Lobby/ServedOrder.cs` (79), `Tests/EditMode/Ready/ReadyAdapterTests.cs` (320), `Tests/PlayMode/Ready/ReadyPresentationTests.cs` (185), `QA/Reports/DRINK-WAVE-ready.md`, 7 `.meta` files. Context read: `HeldItemSlot`, `HeldItemView`, `InteractionActionDriver`, `PlayerInteractor`, `IHoldable`, `InteractionQuery`, both asmdefs, both test asmdefs, and PR #10's `ReadyShelf.cs` and `OrderService.cs` (outbox, `FlushOutbox`, `Fail`, `PickUp`).

## Verified independently

Method: managed .NET 8 harness in `SP/h13` (outside the repo). It compiles unchanged from source: Core, the Orders module **from PR #10 head** (real `OrderService`, `StallTicketQueue`, `ReadyShelf`), the Interaction types the adapters use (`HeldItemSlot`, `HeldItemView`, contexts, queries), and the three PR #13 adapters; `UnityEngine` is a hand-written managed stand-in (`GameObject`, `Transform` with position-only math, `Collider`, Unity-null semantics, `Destroy`). Also ran the author's `ReadyAdapterTests` (patched only to replace `SerializedObject` by reflection) plus 8 integration tests of mine against the **real shelf**.

1. **Capability narrowing (check 1) — pass.** `ReadyCounterPoint` holds only `IReadyShelfPlacement` (`ReadyCounterPoint.cs:16,30`); `ReadyOrderPickupPoint` holds only `IReadyShelfPickup` (`ReadyOrderPickupPoint.cs:12,16`). No `IReadyShelf`, no `IOrderService`; adapters never mutate an order; T7 goes only through `PickUp` (GT-004 structure). The tests' `Shelf : IReadyShelf` union is composition-root/test use, which the doc permits.
2. **Roles, pause guard, ActionBlocked — pass.** Stall role `ReadyCounterPoint.cs:45`, Lobby role `ReadyOrderPickupPoint.cs:28`, pause `:49` / `:32` (pickup re-checks after the hand reservation, `:70`). `Execute` re-queries and publishes `ActionBlocked` (INTERACTION_SYSTEM §84). `Query` allocates nothing (pattern tests only; author's zero-GC test also passes in my harness).
3. **Post-commit order (check 2) — pass for the normal and observer-fault cases.** The hand is released only after `PlaceReady` returns success (`:87-102`); a failed `PlaceReady` returns before the release (`:95-99`). With the real shelf and a listener that throws on `OrderStatusChanged` during placement (my I3): the shelf still returns success, `ObserverFaults == 1`, the hand is released and the visual lands on `DrinkPlacement`. The `PlacementPoint` anchor is validated *before* the commit (`:75-80`), so no adapter-side refusal can occur after commit.
4. **Reentrant-failure fix (`5c2f4b4`), Failed path — sound.** With the real shelf and a listener on `OrderItemStatusChanged(Ready)` that calls `OrderService.Fail` (my I2): hand empty, slot empty, `DrinkPlacement` has no child, item inactive/destroyed, order Failed; the item is never both on the shelf and in the hand and is not lost. The shelf's `Fail` frees slots before the Failed event (`OrderService.cs:122-133`), and the event is delivered inside the outermost `PlaceReady` flush, which is exactly the window `_placingOrder`/`_placementDisposition` captures (`ReadyCounterPoint.cs:81-94,157`). A later Failed for an already-tracked visual is handled by `ClearVisual`.
5. **Regression test strength.** `TC_READY_009_ReentrantFailureDoesNotLeaveStaleReadyVisual` (`ReadyAdapterTests.cs:213`) **does fail without the fix**: mutation "delete the `disposition.HasValue` block" fails it (and my real-shelf I2); before the fix `ClearVisual` finds `_drinkVisual == null` during `PlaceReady`, then `Execute` parents a stale Ready visual under `DrinkPlacement`.
6. **Mutation probes on the author's tests** (adapter copies, one change each): release hand before `PlaceReady` → `FailedPlacementKeepsHandsAndVisualsUnchanged` fails (caught); swallow a failed placement result → same test fails (caught); remove Lobby role check → `PickupRequiresLobbyRole…` fails (caught); remove pause check on placement → `PlacementRequiresStallRoleAndUnpausedClock` fails (caught); delete the reconcile block → TC_READY_009 test fails (caught). Single-assertion coverage for each, no vacuous test found in these paths.
7. **Static checks.** `Stall.asmdef` references Core, Interaction, Orders only; `Lobby.asmdef` the same; `ReadyCounterPoint` in `Scripts/Stall/Runtime` is allowed; namespaces `TramChanh.Stall.Runtime`/`TramChanh.Lobby` follow the convention. Public members expose only Core/Orders/Interaction types. No editor-only code in runtime assemblies (`UnityEditor` only in the EditMode asmdef and under `#if UNITY_EDITOR` in PlayMode). All 14 files have `.meta`, no duplicate GUIDs, no asmdef/ProjectSettings/Docs change. Anchors use the canonical `Anchors/PlacementPoint` / `Anchors/HandGrip` (ASSET_INTEGRATION §anchors); layers via `TramChanhLayers`. No numeric gameplay values, no assets (so no `PlaceholderAsset` marking needed), no T8/T9, no order creation path (GT-004/005/006/007 unaffected).
8. **Bundle with the real shelf (my I1, drink + cake).** Place drink, place cake, order Ready, `ReadyOrderPickupPoint.Execute`: one `ServedOrder` in hand, `Items` = [drink, cake] in ascending `OrderItemId`, both slots empty, order `PickedUpByLobby`. Code is correct; the PR has no test proving it (see B3).

## Taken from author's report (not re-run)

Unity 6000.6.0f1 compile 0 diagnostics, EditMode 122/122, PlayMode 28/28, QA-000 clean-checkout PASS; the claim that the regression test was RED before the fix (I reproduced RED by mutation, item 5). I did not run Unity, the PlayMode tests (they need `HeldItemView`, real `Transform` math, rotations) or ARCH-001.

## Blocking findings

**B1 — Post-commit presentation is skipped when a `HeldItemChanged` listener faults during the hand release.**
`ReadyCounterPoint.cs:100-103` then `:117-141`. `HeldItemSlot.TryRelease` clears `Current`, calls `OnReleased`, then publishes `HeldItemChanged`; `EventBus.Publish` propagates a listener exception (pinned by `CX_002`). The exception leaves `Execute` before the visual is moved to the counter.
Scenario (reproduced as my I4 against the real shelf): any `HeldItemChanged` subscriber throws (HUD, audio, future prompt) → shelf has the item Ready, the hand slot is empty, but the visual is still parented to the hand anchor on the `HeldItem` layer with collider disabled; the item is on the shelf logically and in the player's hand visually, `_drinkVisual` is never recorded, so a later `PickedUp`/`Failed` event cannot clean it. This is the §6.4 refinement-4 failure class the review of PR #8 asked adapters to avoid ("observer faults after the commit must not skip" the post-commit step), moved one step later.
Required fix: make the post-commit steps independent of listener faults, e.g. present first and release second, or wrap the release in `try { TryRelease(); } catch (Exception e) { report }` / `finally { present }`, and route the fault to the same sink the owner uses (or `Debug.LogException`). Add an EditMode test with a throwing `HeldItemChanged` listener asserting the visual is on `DrinkPlacement`, the slot is tracked, and the shelf state is unchanged.

**B2 — The `5c2f4b4` reconcile for `PickedUpByLobby` detaches items from the bundle the Lobby just built.**
`ReadyCounterPoint.cs:106-115` (`SetParent(null, true)` at `:108`) with `:157`. If a listener of the placement flush picks the order up (`ReadyOrderPickupPoint.Execute` → `PickUp` → `ServedOrder.Populate` reparents the item into the bundle, `ServedOrder.cs:56`), the `PickedUp` event arrives inside the same `PlaceReady`, `_placementDisposition = PickedUpByLobby`, and after `PlaceReady` returns `Execute` unparents the item from the bundle and leaves it in the world on the `HeldItem` layer. Reproduced as my I5 (real shelf, order Ready → listener runs the pickup adapter): item `parent == null`, bundle empty of visuals while `Items` still lists it.
For `PickedUp` nothing has to be done by the placement path (the item was never parented to the counter, and the owner of its pose is now the bundle). Required fix: handle only `Failed` in the reconcile; for `PickedUp` return without touching the transform (and without layer/collider changes). Add the I5-style test (placement flush whose listener performs the pickup) and assert `item.transform.parent == bundle.transform`.

**B3 — The headline requirement "all items of one Ready order as a single bundle, oldest-ready first" has no adapter-level test.**
`ReadyAdapterTests.cs:123-137`, `ReadyPresentationTests.cs:65-102`. Every pickup test uses a one-item order and a one-slot fake whose `NextReadyOrder` has a single candidate. Nothing proves that `ServedOrder.Items` contains drink **and** cake, in ascending `OrderItemId`, that both slots clear, or that the adapter takes `NextReadyOrder` (not an arbitrary order) when two are Ready. The code is correct against the real shelf (my I1), so the fix is test-only: a two-item test (fake or real service) plus a two-orders-Ready test asserting the first-ready order is the one carried. Required before merge because `ServedOrder` is the contract the delivery work (CX-025) will build on.

## Non-blocking findings

1. **Failed order while the bundle is carried (soft-lock).** `ServedOrder` never observes `OrderStatusChanged`; `OrderService.Fail` is allowed from `PickedUpByLobby`. A customer leaving while the Lobby carries the bundle leaves a Failed-order bundle in hand, with `hands.full` blocking everything and no release path (my I6: Failed during the pickup flush leaves the bundle with an active item). Fix belongs with T8/T9 (CX-025): the bundle must be discarded on Failed (or Failed must be delivered somewhere). Record it as a CX-025 acceptance item.
2. **Capacity-1 assumption is implicit.** One `_drinkVisual`/`_cakeVisual` and one order id per kind (`ReadyCounterPoint.cs:19-22,132-141`). `ReadyShelf` accepts any capacity; with 2 drink slots the second placement overwrites the first reference and its visual is never cleaned on `Failed`/`PickedUp` (and both visuals share one pose). Either assert capacity ≤ 1 in `Initialize` (a `Configure` that fails fast) or track a list.
3. **Re-entrant adapter use.** `_placingOrder` is a single field; a nested `Execute` for another order inside a listener resets it in `finally` for the outer call. Low probability; a depth guard or a stack avoids it.
4. **`Query` and `Execute` disagree on the anchor.** The missing `Anchors/PlacementPoint` is detected only in `Execute` (`:75-80`) after `Query` said Available; the prompt advertises an action that will block. Cheap fix: cache the anchor lookup at `Initialize` or make `Query` return the same reason (without allocating).
5. **Post-commit throw in the pickup adapter strands items.** If `ServedOrder.Populate` throws (`ServedOrder.cs:38-41,45-48`), `committed` stays false, so the `finally` destroys the reservation (`ReadyOrderPickupPoint.cs:84-103`) after the shelf already committed T7 and cleared its slots; the items are orphaned. Only reachable by a contract violation, but throw-after-commit should be logged, not rolled back. Same pattern for the silent skip at `ReadyCounterPoint.cs:100` when the hand no longer holds the item (§6.4(6) says that is an invariant violation).
6. **Reservation is published pre-commit.** `TryPickUp(bundle)` publishes `HeldItemChanged` with an empty bundle before T7 (`ReadyOrderPickupPoint.cs:65`). Well-guarded (all failure paths release it; tests cover refusing hands and a listener releasing it) but listeners see a transient empty bundle; document it in the class summary.
7. **Bundle presentation.** `ServedOrder.Populate` puts every item at `localPosition = zero` (`:57`), ignoring each item's `HandGrip`/`PlacementPoint` and overlapping drink and cake. Acceptable for the slice; add a placeholder layout (offset per item) or a TODO tied to the carry visuals task.
8. **Destroying module-owned objects.** The adapter destroys prepared visuals on `Failed` (`:109-113`, `:171-181`). The Drinks/Cakes adapters (next PR) own those objects; they must tolerate a destroyed visual (Unity-null) and must not hold the only reference. State this in the next PR's checklist.
9. **Test IDs.** Tests are named `TC_ORDER_006/007` and `TC_READY_009` for adapter behaviour, but in the docs TC-ORDER-006 is the illegal-transition matrix, TC-ORDER-007 "stall cannot see orders" and TC-READY-009 the re-entrant-listener FIFO case (the new test is about re-entrant *failure*). Rename (`TC-READY-0xx` additions or `TC-INT-…`) so the traceability table stays honest.
10. **Test doubles.** Both test files declare their own `Shelf` fake (publishing synchronously, no outbox, no ready.* gating) and `PreparedHoldable`/`PreparedItem`; use the shared `Tests/EditMode/Orders` fakes (F1) and, once PR #10 lands, the real `ReadyShelf`. PlayMode wraps the whole file in `#if UNITY_EDITOR` for `SerializedObject`; reflection or a public test seam would make the file compile in player test builds.
11. **Docs drift (corrected in ORDER_SYSTEM §7 on 2026-10-08).** ORDER_SYSTEM §7 said `LobbyOrderController` performs T7 and builds `ServedOrder`; the PR does it in `ReadyOrderPickupPoint` (a better fit for an interactable on the counter). Amend §7's table. `QA/Reports/DRINK-WAVE-ready.md` has missing spaces ("head5c2f4b4", "compilePASS", "Ordersservice").
12. **Untested adapter paths.** Cake placement in PlayMode, `PlacementPoint` rotation (stand-in and tests use identity), `Failed` for a cake, partial order (drink on shelf, cake Failed), and `PickedUp` ordering relative to `Populate` are not covered; my I1/I7 pass with the real shelf for the first, fourth and last.

## Conformance table vs amended docs

| Requirement | Result | Evidence |
|---|---|---|
| Stall holds only `IReadyShelfPlacement`; Lobby only `IReadyShelfPickup`; no `PickUp` on `IOrderService` | Pass | `ReadyCounterPoint.cs:16`, `ReadyOrderPickupPoint.cs:12` |
| Roles (Stall places, Lobby picks up), empty hands, clock pause, `ActionBlocked`, Query/Execute re-check | Pass | `Query` both files; tests `…RequiresStallRole…`, `…RequiresLobbyRole…` (mutation-verified) |
| Adapters never mutate order state | Pass | no `IOrderService`; T7 only via `PickUp` |
| §6.4(6) hand released only after successful `PlaceReady`; failure keeps item in hand | Pass | `:87-102`; `FailedPlacementKeepsHands…`, mutation `releasefirst`/`swallowfail` caught |
| §6.4(4) observer fault must not skip hand release/presentation | **Partial** | shelf-listener faults pass (I3); `HeldItemChanged` fault strands visual (B1) |
| Reentrant failure during placement consistent | Pass (Failed) / **Fail** (PickedUp) | I2 pass; B2 / I5 |
| §6.3 all items one bundle, snapshot ascending id, oldest-ready first | Code pass, **untested** | I1 pass (real shelf); B3 |
| `ServedOrder` is generic `IHoldable` in `TramChanh.Lobby`, `HandGrip` anchor | Pass | `ServedOrder.cs:11,27-33` |
| Canonical anchors, layer/collider restore, nothing destroyed while referenced | Pass (Failed path destroys only after hand release; see N8) | tests `ReadyPlacement…RestoresPresentation`, PlayMode transfer test |
| GT-004, GT-005/006, GT-007; no T8/T9; asmdef graph; public-API rule; `.meta`; no editor code in runtime | Pass | items 7 above |
| TC-READY-001 (failed `MarkReady`/placement leaves state) | Adapter side only | `TC_ORDER_006_FailedPlacementKeepsHands…`; shelf side belongs to PR #10 |
| TC-READY-002 | Not applicable here (Drinks/Cakes fixture) | — |
| TC-READY-003 (pickup, slots cleared, ascending, freed) | Partial | `TC_ORDER_007_LobbyPickupCarries…`, `…FreesCounterForNextStallOrder`; multi-item missing (B3) |
| TC-READY-004 (oldest first) | Not covered at adapter level | B3 |
| TC-READY-005 | Not applicable (Orders) | — |
| TC-READY-006 (Fail after placement frees slots) | Pass | `TC_ORDER_006_FailedOrderClearsItsShelfVisual`, `TC_READY_009_Reentrant…`; I7 real shelf |
| TC-READY-007 (throwing listener) | Adapter test missing | I3 pass in harness; add to PR (B1 test) |
| TC-READY-008 (silence in transaction) | Not applicable (Orders) | — |
| TC-READY-009 (re-entrant listener FIFO) | Partial | failure only; adapters make no synchronous-visibility assumption except via `_placementDisposition`; pickup re-entrancy broken (B2) |

## Merge readiness and conditions

**Not merge-ready: CHANGES REQUESTED.** Conditions, in order:

1. Fix B1, B2 and add the tests listed in B1–B3 (small, local to the three adapter files and the two test files).
2. **Rebase onto `develop` `01e50b4`** (drop the pre-squash PR #8 commits; the content is identical, so expect a clean 14-file diff) before merge.
3. After PR #10 (real `ReadyShelf`/`OrderService`) lands, add integration tests against the real services (the harness cases I1, I2, I3, I5, I7 are a ready starting point; they use only public APIs) and keep the doubles for unit tests only. The PR's own QA note already says this is pending.
4. Fresh-checkout QA gate (QA-000, compile, EditMode, PlayMode, ARCH-001) on the final rebased head; the author's results above were on `5c2f4b4` and do not cover the B1/B2 fixes.
5. Record N1 (failed order while carried) as an acceptance item of CX-025 (T8/T9), and N8 in the Drinks/Cakes adapter PR checklist.

I re-check only the follow-up commit once these are in; no redesign is needed.

## Lead verification (Claude Code, Technical Lead)

This review was produced by a delegated reviewer and checked by the lead before posting. Re-checked in source: **B1** — `HeldItemSlot.TryRelease` clears `Current`, then publishes `HeldItemChanged`; a throwing listener propagates out of `ReadyCounterPoint.Execute` at `:102`, before the presentation block (`:117-141`), so the item is committed on the shelf but still parented in the hand and untracked. **B2** — `ServedOrder` re-parents every picked-up visual under the bundle (`ServedOrder.cs:56`); the new `PickedUpByLobby` branch then calls `SetParent(null, true)` on the same visual (`ReadyCounterPoint.cs:108`). The harness reproduction and the 5 mutation probes are the reviewer's; Unity results are the author's and were not re-run. Severity note: B1 and B2 are fault/re-entrancy paths of the post-commit step, which is exactly what ORDER_SYSTEM §6.4 (4)–(6) requires to be safe, so they are blocking even though normal play does not hit them.

## Re-verification after fixes (2026-10-07)

| | |
|---|---|
| Fix commit | `4dd9daf` on `feature/DRINK-WAVE-ready`, rebased onto `develop` `01e50b4` (commits `6e4a501`, `8b89821`, `1083af5`, then the fix). Diff against `develop`: the same 14 files as before plus this change |
| Authored by | Claude Code at the root integrator's request (implementation, not the reviewer's own work product) |
| Verdict | **B1–B3 resolved; verdict upgraded to APPROVED**, merge still gated on the root's Unity run, the real-service integration run and a fresh checkout. An independent check of the fixes by the root or the original reviewer is recommended because the author of the fixes is also the lead reviewer |

- **B1 resolved.** `ReadyCounterPoint.ReleaseFromHand` isolates the release after a committed `PlaceReady`: a throwing `HeldItemChanged` listener is logged and no longer skips presentation or reconciliation; the slot's actual state is read back (an already released hand is not released twice; a slot that still holds the item gets a `LogError` and the committed placement is still presented).
- **B2 resolved.** The pre-commit parent (the hand anchor) is captured; on `PickedUpByLobby` the item is detached only if it is still under that anchor, so an item a `ServedOrder` adopted is never unparented. `Failed` still discards the visual.
- **B3 resolved.** Adapter tests `B1_*`, `B2_*`, `B3_PickupRequests…` (fake shelf) and, against the real `OrderService` / `StallTicketQueue` / `ReadyShelf`, `ReadyRealServiceTests` (drink + cake bundle in ascending item ids, partial order not pickable, capacity-2 oldest first, a newer Ready order cannot jump the queue, throwing `HeldItemChanged`, order observer fault plus release fault, real Lobby pickup inside the placement flush, reentrant failure).
- **Evidence.** Out-of-Unity harness with the real PR #10 services and a managed `UnityEngine` stand-in: 25/25 pass. On the unfixed adapter the new real-service B1 and B2 tests failed (RED, 3 failures), then passed with the fix. Mutations "always unparent on pickup" and "release not isolated" each fail 2 and 4 of the new tests. One mutation (repeat the release when a listener already released) survives; it is an equivalent mutant because `HeldItemSlot.TryRelease` is a no-op when empty.
- **Where the real-service tests live.** They need PR #10's classes, so they are in `Tests/EditMode/Ready/ReadyRealServiceTests.cs` on `feature/DRINK-WAVE-ready-realservice` (`7c70b04`, PR #13 + PR #10 head `3fd62e8` merged + that file). Apply them after PR #10 and PR #13 merge.
- **Remaining limitations (carried, non-blocking):** `ServedOrder` still does not observe `Failed` (CX-025 acceptance item); one visual per kind (capacity 1) is assumed by `ReadyCounterPoint`; `Query` says Available when `Anchors/PlacementPoint` is missing (only `Execute` blocks); bundle items overlap at local zero; cake placement and cake `Failed` remain untested at adapter level; the fixture-shelf tests still use their own fakes. The Unity results quoted in `DRINK-WAVE-ready.md` predate the fix.

### Re-verification 2 — hand-release callback window (head `a871d29`)

The root found a further window: the in-flight marker was cleared right after `PlaceReady`, so a `HeldItemChanged(Current == null)` observer that failed the order or ran the real Lobby pickup during the hand release was not recorded. The marker now stays set through `ReleaseFromHand`, the latest disposition is read afterwards, and the `finally` cleanup is kept on every exit. Four new regressions (two fake-shelf, two real-service in `ReadyRealServiceTests`) were RED on `4dd9daf` and are GREEN on `a871d29` in the out-of-Unity harness (29/29, real PR #10 head `8c9a623`); reading the disposition before the release turns them RED again. The `finally` removal is an equivalent mutant. Stacked branch `feature/DRINK-WAVE-ready-realservice` is `246966c`. Verdict unchanged: APPROVED after fixes, gated on the root's Unity and integration runs.

## Status (2026-10-08)

Merged as `d9143c1`. Final head `4e58059` (runtime `a871d29`, rebased onto `77ea69a`, with the real-service tests and explicit `ReferenceEquals` fixes) reports Unity compile with 0 diagnostics, EditMode 278/278, PlayMode 28/28, QA-000 clean PASS (the root's report). The root's independent run also passed the release-callback fix. Still open and non-blocking: `ServedOrder` does not observe `Failed`; one visual per kind is assumed; `Query` does not check the placement anchor.

_Generated by [Claude Code](https://claude.ai/code)_
