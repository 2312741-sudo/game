# PR #11 — Claude Code review (drink preparation, DRINK-WAVE)

| | |
|---|---|
| PR | #11 "drink preparation" (2312741-sudo/game) |
| Head reviewed | `877b935` (4 commits: `23d8b85` rename, `4335688` feature, `3e2d114` runtime/test fix, `877b935` QA note) |
| Runtime commit | `3e2d114` |
| Base | `develop` @ `01e50b4` (3-dot diff; PR branch is based on develop 01e50b4) |
| Reviewer | Claude Code (independent review) |
| Date | 2026-10-07 |
| Verdict | **APPROVED** |

Not merged, no branch touched. There is no Unity editor here, so everything that needs UnityEngine was read, not run.

## Scope inspected

Full 3-dot diff, 41 files, +1393/-108, against the amended docs: `ORDER_SYSTEM.md` §5-§6.4 and TC-READY, `DRINK_WORKFLOW.md`, `INTERACTION_SYSTEM.md` §3.1, `ARCHITECTURE.md` §4/§6, `CODING_CONVENTIONS.md` §1/§3/§4/§8, `DRINK-WAVE-claude-plan-review.md` C1-C12. Every runtime file was read in full:
- Domain: `DrinkPreparation`, `TeaRackInventory`, `TeaBagState`, `DrinkStepCompleted`, `SequenceMode`.
- Adapters: `TeaBagItem`, `TeaBagStateView`, `TeaRackController`, `ToppingBin`, `IceBin`, `WipeInteraction`, `DrinkActionGuards`.
- Other: `DrinkRecipe`, the DevTools pair, the scene builder and the scene diff.
- Tests: `DrinkPreparationTests`, `DrinkAdapterTests`, the edited pickup, asset and held-view tests, and the PlayMode pickup diff.

I also read the frozen contracts (`IPreparedItem`, `IStallTicketQueue`, `OrderItemRef`), `InteractionActionDriver`, `HeldItemSlot` and the shared fixture `PreparedItemContractAssertions`.

## Verified independently

- **Managed harness** (`SP/h11`, .NET 8, NUnitLite 3.14.0, outside the repo). It compiles Core minus the Unity files, plus Orders, `Drinks/Domain`, the shared fixture and `DrinkPreparationTests.cs`. Result: build 0 warnings and 0 errors, **14/14 pass**. That is the domain/EditMode slice only. `DrinkAdapterTests`, `TeaRack*`, DevTools and PlayMode need UnityEngine and were not run.
- **Mutation probes** on the domain source, each applied alone and caught. M2 is caught only by the same-instance lifecycle test.

| Probe | Result |
|---|---|
| M1 skip the IceAdded→Shaken guard | caught by `TC_DRINK_007` matrix |
| M2 rejection latch (a failed `MarkReady` poisons a later success) | caught only by `TC_DRINK_015` domain lifecycle |
| M4 clear the binding on Ready | caught by `TC_DRINK_015` and `TC_DRINK_003_004(7)` |
| M5 failed `MarkReady` mutates state | caught by 3 tests |
| M6 omit `Release` on rollback | caught by `TC_DRINK_008_Refused…` |
| M7 claim before the guards | caught by 8 tests |
| M8 allow Ready from Shaken | caught by 3 tests |

- **Event observer in the domain tests is inert.** `DrinkPreparation` has no bus, so a publish-in-`MarkReady` mutant cannot even be written in the domain. The `count` observers in `DrinkPreparationTests.cs:154, 174-176` subscribe to an `EventBus` the domain never sees. They cannot fail, so the domain tests prove nothing about events. The adapter lifecycle test is the real event check (see N1).
- **Strict chain.** `DrinkPreparation.cs:31-37`: Stored→PickedUp→Opened→CoconutJelly→LemonJelly→Ice→Shaken→Wiped→Ready through a single `Advance(required,next)`. No measure/pour state; the enum names are pinned exactly. A failed step returns `Result.Fail` and mutates nothing. D1 order is: CanTake (hands, rack) → `HasPending` → `ClaimNext` → pickup → rollback with `Release`. Role and pause are checked by `DrinkActionGuards.Actor`, called from `TeaRackController.Query`.
- **Events.** The domain publishes nothing (grep: no bus, no publish). `TeaBagItem.CompleteStep` (`:102-106`) publishes `DrinkStepCompleted` only on a successful D2-D7 step. `MarkReady` (`DrinkPreparation.cs:39-47`, `TeaBagItem.cs:108`) is validate-then-commit, has no latch and publishes nothing. No Ready or Delivered `DrinkStepCompleted` exists.
- **IPreparedItem.** Kind = Drink; `IsFinished` stays true after Ready (`:15`); Quality 100; `BoundItem` carries the `PreparationId` (`:14`).
- **Bypass.** `_allowUnboundPickupForTest` is gone from the whole tree (grep of scripts, scenes and tests). The replacement is `TramChanh.DevTools` (`defineConstraints: DEVELOPMENT_BUILD || UNITY_EDITOR`) plus `TeaRackPickupTestSetup`, which is serialized only in `SCN_TeaRackPickupTest` and also checks `Debug.isDebugBuild`.
- **Scene diff.** Only `SCN_TeaRackPickupTest.unity` changed. It gains the setup component, `_recipe: 0`, the removed bypass field, and 10 whitespace-only `m_Name:` serializer hunks. No blockout scene, prefab or station transform changed, so stall 1.8x0.8x~2.2 and the 1.0 m counter are untouched.
- **Pause and hold (§3.1).** `Tick` returns early while paused (`InteractionActionDriver.cs:76`), so a hold freezes. `Query` returns Blocked on pause, but the driver does not re-query until resume. The shake hold targets the held item (`IsHeldUse`) and is cancelled on release or when the held item changes. The wipe hold is focus-locked and guards a replacement bag (`WipeInteraction.cs:116`).
- **Asmdef graph.** No asmdef changed. Drinks references Core, Content, Interaction and Orders, as ARCHITECTURE §4 allows; no Lobby/Stall/Cakes reference. No UnityEditor in runtime asmdefs.
- **`.meta` files.** Present for every new file under `Assets/` and for new folders (`Debug/Drinks.meta`). `QA/Reports` is outside Assets and needs none. `TeaBagPickup.cs` is deleted and its meta renamed to `DrinkPreparation.cs.meta` (guid kept).
- **Recipe data.** `DrinkRecipe` (`Scripts/Core/Content/DrinkRecipe.cs`) marks portions and hold seconds `[Tbd("DEC-007")]` and has no numeric defaults. The step sequence is a code constant. The only numeric constant in the drink code is `Quality => 100` (doc-mandated strict value).

## Taken from author's report (not re-run)

Unity compile 0 diagnostics; EditMode 136/136; PlayMode 25/25; QA-000 PASS; the replacement-bag fixture fix (`3e2d114`, with the production guard in `TeaBagItem.OnPickedUp` kept); `DrinkAdapterTests`, `TeaRack*` tests, DevTools wiring and the PlayMode pickup tests. I read these tests and they look sound, but I could not execute them.

## Blocking findings

None.

## Non-blocking findings

- **N1 — adapter lifecycle test runs on an unbound bag, so the bound-identity check is vacuous there.** `DrinkAdapterTests.cs:276` builds the bag with `model.TryPickUp()` and never claims, so `BoundItem` is `default` throughout `TC_DRINK_015_Runtime…` (`:235-242`). The `PreparationId` stability check is real only in the domain lifecycle test. The adapter test does catch a publish inside `TeaBagItem.MarkReady`, which is the useful part. Fix: build the adapter bag through `TeaRackInventory.TryTakeBag` with a fake queue.
- **N2 — `DrinkPreparation.TryPickUp()` is public and skips the claim** (`DrinkPreparation.cs:31`). Production only uses `BeginBoundPickup`; `TryPickUp` has 4 test callers. Any future caller could create an unbound PickedUp bag, which D1 forbids. Make it `internal` (InternalsVisibleTo for the test assemblies already exists).
- **N3 — presentation is not wired anywhere.** No prefab or scene gets `TeaBagStateView`; the `_bagClosed`/`_bagOpen`/… serialized references are unassigned, and `PF_TeaBag_PrePortioned` already has `Bag_Open` and `Contents/*` children that nothing drives. `TeaBagItem._stateView` resolves to null, so the held bag never changes visually in any shipped scene. No test covers `TeaBagStateView`'s `OrderItemStatusChanged` path. This is consistent with C11 (prefabs and scenes belong to the integration branch), but the wiring is required before the drink slice is playable.
- **N4 — no `SO_Recipe_Drink_Slice.asset` and no `_recipe` in the rack.** Without it every shake and wipe returns `drink.recipe.not_configured` (the safe failure). The DEC-007 asset belongs to integration.
- **N5 — `DevelopmentPickupTicketQueue` mints `OrderItemRef(OrderId(1), ++n, prep)` and `HasPending` is always true in debug builds.** CODING_CONVENTIONS §8.4 says "no fake domain data". It is isolated, creates no order and publishes no event, and it is exactly the §8.3 pattern, so I accept it, but it must be deleted with the integration retirement task. QA-000 has no GUARD-001 check for `TeaRackPickupTestSetup` in other scenes; add one.
- **N6 — `TeaBagItem.OnPickedUp` throws unless State is PickedUp** (`:42`). `HeldItemSlot.TryPickUp` has already set `Current`, so a throw would leave the slot occupied. A partially prepared bag can never be re-picked. The guard is acceptable for the slice, but a future put-down or re-pickup feature needs a design.
- **N7 — exceptions are not rolled back.** `TryTakeBag` (`TeaRackInventory.cs:57-90`) does not roll back or release if `transferToHands` throws (only if it returns false). A throwing `DrinkStepCompleted` listener in `CompleteStep` (`TeaBagItem.cs:104`) escapes after the domain has advanced and skips `Apply`. Both are post-commit faults, consistent with §6.4.4, but the event publish should be guarded.
- **N8 — shake animation not frozen on pause.** The Animator `Shaking` bool stays true while the hold is frozen. Cosmetic.
- **N9 — test hygiene.**
  - Test ids don't match the spec: `TC_DRINK_005_D1…` is the D1 commit test (spec TC-DRINK-005 is oldest-ticket), and `TC_DRINK_008_Refused…` is spec TC-DRINK-010.
  - `DrinkPreparationTests.cs:80, 99, 154` are inert observers (see Verified).
  - TC-DRINK-005 (oldest ticket) and the PlayMode TC-DRINK-009 and TC-DRINK-014 (E/F exclusion with real input) are not present; they need the real order service or station prefab.
  - `TeaRackPickupSceneBuilder.MigratePickupFixture` is a one-shot, unreferenced public editor method (`:115`). Delete it.
  - `InteractableId(PreparationId.Value)` (`TeaBagItem.cs:28`) can collide with scene ids and throws for an uninitialized bag.
- **N10 — `BoundItem` is derived from `IsBound` on the queue (`:14`)**, so it survives only while the real queue keeps reporting a Ready item as bound. The fake queues cannot show that. Add a real-queue integration test: after `PlaceReady`, `bag.BoundItem` must still equal the claimed ref (the view check at `TeaBagStateView.cs:252` depends on it).
- **N11 — scene churn.** The drink branch commits a scene edit, contrary to C11 (integration owns scenes). It is justified here because the removed serialized field forced it, and it is limited to the test scene.

## Conformance table vs amended docs

| Requirement | Result |
|---|---|
| GT-007 chain, no tea measuring (GT-003) | Met (`:31-37`; enum pinned by `GT_007…` test) |
| Out-of-order steps blocked, no mutation | Met (matrix test, M1/M5 caught) |
| D1: guards, claim after guards, Release on refusal | Met (`TeaRackInventory.cs:57-90`; M6/M7 caught) |
| `PickedUp` canonical, `Held` removed | Met (first commit `23d8b85`) |
| D2-D7 published by the adapter only, after success | Met (`TeaBagItem.cs:102-106`) |
| Domain pure; no Ready/Delivered step event | Met |
| `MarkReady`: mutate-only, no latch, no publish | Met (M2 caught only by the same-instance lifecycle test) |
| Same-instance lifecycle with real observers | Met in `TeaBagItem` (event observer real); domain observer inert; adapter bag unbound (N1) |
| Ticket binding via `IStallTicketQueue`; stale/foreign rejected | Met in the domain with fakes; real queue untested (N10) |
| §8 bypass: DevTools, Debug-only, test scene only | Met; fake ref noted (N5) |
| Scene and ground truths untouched | Met (N11) |
| Interaction: Query/Execute, Press/Hold, pause, role | Met |
| §3.1 pause freezes, focus loss cancels | Met (driver); animation (N8) |
| DrinkRecipe `[Tbd]` data, no magic numbers | Met; asset missing (N4) |
| Asmdef graph, public API only Core/own types | Met |
| `.meta` files | Met |
| Scope | Within drink preparation, plus the retired pickup bypass |
| Visual mapping (§5) | Code only, unwired (N3) |

## Merge readiness and conditions

Safe to merge into `develop` as the drink domain and adapter layer. It is based on develop `01e50b4`.

Conditions for later:
1. The real-service integration tests will gate the merges: real queue and shelf, with an `IsBound`/`BoundItem` check after `PlaceReady` (N10), and the oldest-ticket case (TC-DRINK-005).
2. A fresh-checkout EditMode + PlayMode run on the pinned editor must be green; I could not run Unity.
3. Integration owns the prefab and scene wiring and the `SO_Recipe_Drink_Slice` asset (N3, N4), and the retirement of `DevelopmentPickupTicketQueue`.
4. Recommended before or just after merge: N2 (make `TryPickUp` internal) and N1 (bound adapter bag).

## Lead verification (Claude Code, Technical Lead)

This review was produced by a delegated reviewer and checked by the lead before posting. Re-checked in source: `DrinkPreparation.TryPickUp()` is public and does not claim a ticket (N2); `TeaBagItem.OnPickedUp` throws for a bag not in `PickedUp` after the slot is set (N6); `DrinkRecipe` portions are `[Tbd("DEC-007")]`. The out-of-Unity run (14/14 plus 7 caught mutations) is the reviewer's; Unity results are the author's and were not re-run.

_Generated by [Claude Code](https://claude.ai/code)_
