# MAIN-002 drink workflow audit

Branch `feature/MAIN-002-drink-workflow`, based on `origin/wave/MAIN-001-integration` (`ecc55aa`). Author: CLAUDE-02 (drink gameplay). No Unity Editor was available, so nothing here is a Unity result. See "Verification" for what actually ran.

## Canonical sequence vs implementation

Required: Receive ticket -> pick pre-portioned tea bag from red rack -> open bag -> add coconut jelly -> add lemon jelly -> add ice -> shake -> wipe -> Ready (place on ready counter) -> handoff (customer pickup/delivery). There is no tea-measuring step. `TeaBagState` has no measure/pour/seal state (GT_007/GT_003 tests).

| # | Step | Implementation | Tests | Gap |
|---|---|---|---|---|
| 1 | Receive ticket | `TeaRackController.Query` / `TeaRackInventory.TryTakeBag` require `IStallTicketQueue.HasPending(Drink)` and claim it with `ClaimNext` (`stall.no_ticket.drink`) | `TC_DRINK_008_*` (EditMode), `TC_DRINK_008_ProductionRackWithoutPendingTicketCannotConsumeStock` (PlayMode), `MAIN_002_TicketToHandoff...` (new) | none |
| 2 | Take pre-portioned bag from red rack | `TeaRackInventory.TryTakeBag` -> `DrinkPreparation.BeginBoundPickup` (Stored -> PickedUp), hand transfer with rollback; `TeaBagItem.OnPickedUp` | `TC_DRINK_002_*`, `TC_DRINK_005_*`, `DRINK_001_*` (Edit + PlayMode) | none |
| 3 | Open bag | `TeaBagItem.QueryUse/ExecuteUse` (held action, PickedUp -> Opened) -> `DrinkPreparation.Open` | `TC_DRINK_002_PickedUpBagOffersOpen...`, GT_007 tests | none |
| 4 | Add coconut jelly | `ToppingBin` (CoconutJelly) -> `DrinkPreparation.AddCoconutJelly` | GT_007, `TC_DRINK_007_*` | prompt bug at later states, fixed (see below) |
| 5 | Add lemon jelly | `ToppingBin` (LemonJelly) -> `AddLemonJelly` | GT_007, `TC_DRINK_007_*` | same, fixed |
| 6 | Add ice | `IceBin` -> `AddIce` | GT_007, `TC_DRINK_007_*` | same, fixed |
| 7 | Shake | `TeaBagItem` held Hold action (`DrinkRecipe.ShakeHoldSeconds`) at IceAdded -> `Shake`; `TeaBagStateView.SetShaking` | `GT_007_RealAdapters...BothHoldsRequireCompletion`, `MAIN_002_ShakeAndWipe...` (new), PlayMode `TC_DRINK_009_*` | none |
| 8 | Wipe | `WipeInteraction` Hold (`WipeHoldSeconds`) at Shaken -> `Wipe`; replacement-bag guard | `GT_007_*`, `TC_DRINK_007_WipeCancellation...`, `MAIN_002_ShakeAndWipe...` (new) | prompt bug at Wiped, fixed |
| 9 | Ready (place on counter) | `Stall.ReadyCounterPoint.Execute` -> `ReadyShelf.PlaceReady` -> `TeaBagItem.MarkReady` (Wiped -> Ready), then `ReleaseFromHand` and re-parent to `DrinkPlacementPoint` | `TC_DRINK_003_004_*`, `TC_DRINK_015_*`, `Ready/*` tests, PlayMode `TC_DRINK_009_*`, `MAIN_002_TicketToHandoff...` (new) | none in drink code |
| 10 | Handoff | `Lobby.ReadyOrderPickupPoint.Execute` -> `ReadyShelf.PickUp` -> `ServedOrder.Populate`; `OrderService.Deliver` retires the bundle | `Ready/*`, `Lobby/*`, PlayMode `TC_DRINK_009_*`, `MAIN_002_TicketToHandoff...` (new, includes Deliver) | `TeaBagState` has no `Delivered` (doc only, see lead requests) |

## Ready and held-item checks

- **Held item leaves the hand on Ready.** Confirmed correct. `ReadyCounterPoint` releases the slot once after the shelf commits (`HeldItemChanged(bag -> null)` is published exactly once), and the bag ends up under `DrinkPlacementPoint` in state Ready. The new test asserts this.
- **An unfinished drink cannot be placed.** Confirmed correct. `ReadyShelf.CanPlace` checks `IsFinished`, which is true only at Wiped or Ready. The new test tries the counter at PickedUp, Opened, CoconutJellyAdded, LemonJellyAdded, IceAdded and Shaken. Each attempt is refused with `ready.not_finished`, the bag stays in hand, the bag state does not change and the shelf stays empty.
- **Shake and wipe cannot run out of order.** Confirmed correct. Domain `Advance` guards and adapter queries refuse them. The new test drives the real `InteractionActionDriver` and confirms that no shake or wipe hold can start before its predecessor step, and that neither can be repeated.
- **The cup cannot be re-picked after placement.** Confirmed correct. A placed bag:
  - is in state Ready;
  - is on the Environment layer, so it is not raycast as an interactable;
  - is not offered by the rack, because the rack only offers bags in state Stored;
  - returns Hidden from `QueryUse` and `Query` when not held;
  - returns `ready.already_ready` from `MarkReady`.

  With empty hands, the rack refuses with `stall.no_ticket.drink` once the ticket is consumed, and the counter refuses with `ready.need_prepared_item`. The new test asserts all of these.
- **Held prompts.**
  - **Bug, confirmed and fixed.** `DrinkActionGuards.Step` returned Blocked with the "previous step" reason for every state other than the expected one, including states the bag had already passed. As a result, a wiped bag aimed at the coconut bin showed "Add coconut jelly — Open the bag first". The ice bin at Shaken said "Add lemon jelly first", and the wipe area at Wiped said "Shake first". All three teach a step the player already did.
  - **The fix.** A station whose step the held bag has already passed is now Hidden. This matches `TeaBagItem.QueryUse`, which already hides a finished shake, and the prompt falls back to the held-item prompt. Earlier states keep their existing Blocked reasons. One condition was changed in `Assets/TramChanh/Scripts/Drinks/Runtime/DrinkActionGuards.cs`.
  - **Regression test.** `MAIN_002_StationsHideCompletedStepsInsteadOfTeachingAPassedStep` checks the state × station matrix and that a stale Execute on a hidden step does not mutate or publish. It failed on the unfixed guard (mutation run: 64/65) and passes with the fix.

## Changes

- `Assets/TramChanh/Scripts/Drinks/Runtime/DrinkActionGuards.cs`: completed steps are Hidden (the fix above).
- `Assets/TramChanh/Tests/EditMode/Drinks/DrinkReadyHandoffTests.cs` (+ `.meta`, new guid) contains 3 tests. They use the real `OrderService`, `StallTicketQueue`, `ReadyShelf`, `TeaRackInventory`, drink adapters, `ReadyCounterPoint`, `ReadyOrderPickupPoint` and `InteractionActionDriver`:
  - `MAIN_002_TicketToHandoffRunsOnlyTheCanonicalSequenceAtRealAdapters`
  - `MAIN_002_StationsHideCompletedStepsInsteadOfTeachingAPassedStep`
  - `MAIN_002_ShakeAndWipeCannotRunOutOfOrderThroughTheHoldDriver`

No changes to Stall, Orders, Interaction, Lobby, bootstrap, scenes, prefabs or assets.

## Requests for the lead (outside drink ownership)

1. **Docs/DRINK_WORKFLOW.md §2.1**: add one line after "Each interactable is **Blocked** (not Hidden)…". It should say that a station whose step the held bag has already completed is **Hidden**, and that the prompt falls back to the held-item prompt.
2. **Docs/DRINK_WORKFLOW.md §2 / §3 vs code**: the doc lists `Ready → Delivered` (D9) and a `MarkDelivered()` method. `TeaBagState` ends at `Ready`, and the delivered drink is destroyed together with its `ServedOrder` bundle (`Lobby/ServedOrder.ReleaseAndRetire`). This has no gameplay effect. Decide whether to amend the doc or to add a drink `Delivered` state. Adding the state would also need `GT_007_OnlyCanonicalStatesExist...` updated. Cakes do track `Delivered`.
3. **Orphaned bag when an order fails (P1)**: `OrderService.Fail` unbinds the bag. The bag can still be finished, but Ready refuses it with `ready.no_order`, and drinks have no discard. A player holding it would keep full hands permanently. This is **not reachable at runtime today** because no production code calls `OrderService.Fail`; only tests do. Before patience or a debug fail ships, a discard path is needed. Options: a drink discard via the existing `Discard` input action (`PlayerInteractionSceneBuilder`), similar to `CakeItem`'s burnt-cake discard, or Interaction-level support for a second held action.
4. **Interaction hardening (optional)**: `HeldItemSlot.TryPickUp` assigns `Current` before calling `OnPickedUp`. `TeaBagItem.OnPickedUp` throws for any state except PickedUp, so a stray pickup of a non-PickedUp bag would leave the slot holding it. No current code path does this.

## Notes, not changed

- At Opened, CoconutJellyAdded and LemonJellyAdded, the held prompt reads "Hold to shake — Add ice first". This is deliberate and asserted by `TC_DRINK_002`. It tells the player what shake needs, not which topping comes next.
- `Execute` on a Hidden step, which can happen only when the step is called directly rather than through the driver, publishes `ActionBlocked("interaction.item_not_held")`, as it already did for "no bag held". No state changes.

## Verification (what actually ran)

- **No Unity run.** No Unity Editor in this container. Unity compile, EditMode and PlayMode results are **not** claimed.
- **Harness.** .NET 8 with NUnitLite 3.14, C# `LangVersion 9.0`, and managed UnityEngine stand-ins (scratchpad only, not in repo). It compiled the real sources: Core, Content (excluding MeasureCup/Cake recipe), Orders, the Interaction subset, all of Drinks, `Stall/Runtime/ReadyCounterPoint.cs` and Lobby.
  - **Tests run.** `Tests/EditMode/Drinks/*` (excluding `TeaRackPickupAssetTests`, which needs AssetDatabase), `Tests/EditMode/Ready/ReadyAdapterTests.cs`, `ReadyRealServiceTests.cs` and `Orders/PreparedItemContractAssertions.cs`.
  - **Result.** **65/65 passed.**
  - **Mutation run.** With the guard reverted: **64/65**, and `MAIN_002_StationsHideCompletedSteps...` fails.
  - `TC_INT_007` zero-allocation assertions were checked with the CLR `GC.GetAllocatedBytesForCurrentThread`, not Unity's profiler.
- **Drinks compile check.** Drinks was compiled against only its asmdef references (Core, Content, Interaction, Orders): succeeded.
- **Not run.** PlayMode tests (`TeaRackPickupPlayModeTests`, `DrinkWaveFlowTests`, `ReadyPresentationTests`) and asset/scene tests were not run. Root should run full Unity EditMode and PlayMode on this branch.
