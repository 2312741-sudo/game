# MAIN-003 — Cake workflow review and fixes (CLAUDE-03)

**Branch:** `feature/MAIN-003-cake-workflow`, based on `origin/wave/MAIN-001-integration` (`ecc55aa`).
**Reviewed:** draft PR #16 (`feature/ACCEL-01-cake`, head `c68cc3a`). The cake files on the integration base are byte-identical to `c68cc3a`, so the line numbers below refer to `c68cc3a`.
**Verification:** a .NET 8 + NUnitLite harness with managed UnityEngine stand-ins, kept outside the repo. Unity Editor was **not** run and no Unity result is claimed.

## 1. Canonical step table

| # | Step | Class / method | Test | Gap |
|---|---|---|---|---|
| 1 | Receive ticket | `CakeStation.Query(Fill)` → `stall.no_ticket.cake`; `CakeStation.Measure` → `IStallTicketQueue.ClaimNext(Cake)` on first measure; `CakeRecipeCatalog.TryResolve` (released again if no recipe) | PM `TC_CAKE_ProductionTicket…` (no-ticket block, order → InPreparation) | Fixed: with capacity 1, a new claim could be made while the Ready cake slot was full, and that cake could never be placed. Optional `readyGate` added (§3 F4). |
| 2 | Measure batter (interactive `PF_BatterMeasureCup_500ml`) | `BatterMeasureCup` (pick up / empty back), `CakeStationPoint` (Continuous fill), `CakeStation.Measure` (`FillRateMlPerSecond`, capped at 500), `CakePreparation.Measure` → `BatterMeasurement` | EM `PerRecipeToleranceIncludesBothEdges`, `InvalidMeasureIsAtomic`, `EmptyAndTopup…`, `BlockPolicy…`; PM `ContinuousFillCapsCapacity…` | Fixed: an uninitialized cup threw a NullReferenceException on use (§3 F3). |
| 3 | Pour | `GrillModel.Pour` → `CakePreparation.Pour` (poured = measured); cup `ReturnHome` | EM `AllowPenaltyPreservesActualPouredAmount`, `GrillPreheatLidAndOccupancyGuard`; PM flow | — |
| 4 | Cook | `GrillModel.CloseLid` → `StartCooking`; `GrillModel.Advance` → `CakePreparation.Heat` (`CookedThreshold`/`BurnThreshold`/`OpenLidHeatFactor` from recipe) | EM `PausedClockCannotCookOrBurn`, `OpenLidUsesConfiguredHeat…`; PM flow (pause) | — |
| 5 | Flip | `GrillModel.Flip(IFlipAction)` → `CakePreparation.Flip`; `PlaceholderFlipAction.CanFlip/Present` moves the cake to `RollArea`; the cake leaves the plate, so cooking stops | EM `FlipLeavesGrillAndStopsCooking…` (fake `IFlipAction`); PM flow (parent == RollArea) | The `IFlipAction` signature differs from Docs/CAKE_WORKFLOW §2.3 (doc-only, see L4). |
| 6 | Cut | `CakeStation.Execute(RollArea)` with state Flipped → `CakePreparation.Cut` (Hold `CutHoldSeconds`) | EM strict sequence; PM flow | — |
| 7 | Sauce | `CakeStation.Execute(Sauce)` → `CakePreparation.Sauce(sauce)` (`Recipe.Sauce`, Hold `SauceHoldSeconds`) | EM `wrong_sauce`; PM flow | — |
| 8 | Roll vertically | `CakeStation.Execute(RollArea)` with state Sauced → `RollVertically` (Hold `RollHoldSeconds`); `CakeItem.Apply` shows `SM_Cake_RolledVertical` | PM flow (rolled visual active); EM saved-asset check (scale.y > scale.x, Unity only) | — |
| 9 | Wrap → in hands | `CakeStation.Execute(Wrap)` → `Wrap` + `Hands.TryPickUp(CakeItem)` (`UndoWrap` if pickup fails); the station releases `Current` | PM flow (`Hands.Current == item`, `IsFinished`) | Fixed: a wrapped cake held when its order failed could neither be placed nor discarded, so the hands soft-locked (§3 F1). |
| 10 | Ready (ready counter) | `ReadyCounterPoint.Execute` → `ReadyShelf.PlaceReady` → `CakeItem.MarkReady` (changes state only) → `OrderService.CommitReady` → slot → `PublishReady` | PM flow (observer sees shelf, cake and order all committed) | — |
| 11 | Handoff | `ReadyShelf.PickUp` (Lobby); `CakeItem` follows `OrderStatusChanged(Delivered)` → `CakePreparation.Delivered` | PM gate test uses `PickUp` | No cake-specific pickup test (Lobby owns it). |
| — | Burnt / abort | `GrillModel.RemoveBurnt` → `Release` (requeue) → burnt cake in hands → discard; empty back → `Release`; order failure → `Advance` clears the stale preparation, grill and cup | PM `BurnRemovalRequeues…`, `InvalidSequenceAndStaleBinding…`, `ProductionTicket…` (empty back requeues) | — |

**Checks:**
- **Step order:** enforced by `CakePreparation.Step(required, next)` and the station queries.
- **Flip:** a real step through `IFlipAction`.
- **Pause:** `IGameClock` discards paused time and `GrillModel.Advance` returns early while paused.
- **Recipe values:** all come from `CakeRecipe` and `MeasureCupDefinition` data. Real quantities are absent; the seeds are labelled `DEV TEST SEED ONLY … TBD`.
- **ORDER_SYSTEM §6:** the cake only mutates its own state in `MarkReady`, and every order or item event is published by `OrderService`/`ReadyShelf`. Cakes publishes only its own `BatterMeasured` and `CakeStepCompleted` events.

## 2. Saved YAML audit (script `yamlcheck.py`, run outside the repo)

- **Prefabs and assets checked:** all six `Prefabs/ACCEL01/Cakes/*.prefab`, the three `Data/ACCEL01/Cakes/*.asset` and `Workstations/PF_Grill_Elmich.prefab`.
- **Script GUIDs:** every `m_Script` GUID resolves to a `.cs.meta`.
- **Field names:** every serialized key matches a `[SerializeField]` in its script, and every scalar field is also applied by the harness, which throws on an unknown name.
- **References:** every stripped or nested reference resolves to an object of the correct type in its source prefab, and every override target exists.
- **Null references:** the only one is `PF_BatterMeasureCup_500ml._home`. It is overridden in `PF_CakeStation` to `BatterArea`, so it is not required on the standalone tool prefab.
- **One reported "problem" is a false positive:** the stripped `BatterMeasureCup` inside `PF_CakeStation` carries no fields, by design.
- **Wiring:** `CakeStation` references (`_cup`, `_cakePrefab` = the `CakeItem` on `PF_Cake_Prepared`, `_cakePlacement`, `_lidPivot`, `_scissors`, `_sauceBag`, `_wrappingArea`, `_flipAction`) are all set. There are 5 `CakeStationPoint`s with ids 1102 to 1106. The sauce point and the recipe both use `_sauce: DEV_TBD`.

## 3. Fixes on this branch (each has a regression test that fails on `c68cc3a` in the harness)

| ID | Problem (PR #16 location) | Fix | Regression test |
|---|---|---|---|
| F1 | **Blocker:** a wrapped cake whose order failed while it was held stayed stuck in the hands. Ready rejects it with `ready.no_order` and `QueryUse` only allowed discarding Ruined cakes (`CakeItem.cs:47-49`), so every empty-hands action was blocked from then on. | `CakeItem.QueryUse` also offers discard (`cake.discard_orphan`, actor/pause guarded) for a held Wrapped cake that is no longer bound. | PM `TC_CAKE_WrappedCakeOfFailedOrderCanBeDiscardedFromHands` |
| F2 | `CakeStepCompleted` was published when no step happened: on open/close lid (`CakeStation.cs:198`) and on batter top-up (`CakeStation.cs:136`). | Publish only when `Current.State` changed. `BatterMeasured` is still published on every measure or top-up. | PM `TC_CAKE_StepEventsPublishOncePerCanonicalStepAndNeverForLidOrTopUp` (exact sequence BatterMeasured…Wrapped) |
| F3 | `BatterMeasureCup` could be picked up before `CakeStation.Initialize`, and then `QueryUse`/`ExecuteUse` threw a NullReferenceException (`BatterMeasureCup.cs:29,45`). | Block pickup and use with `cake.station_uninitialized`. | EM `TC_CAKE_UninitializedCupIsBlockedInsteadOfThrowing` |
| F4 | **Blocker (lead task):** with Ready capacity 1, the cake slot holds order A's cake while A waits. Starting a cake then claims order B's ticket, and B's cake can never be placed (`ready.slot_full`). | This is an addition only: a new overload `CakeStation.Initialize(queue, ids, events, clock, recipes, IReadyShelfPlacement readyGate)` sits next to the 5-arg overload, which is unchanged. While the gate reports the Cake slot occupied **and** `Current == null`, Fill is Blocked `ready.slot_full`, and `Measure` goes through the same query, so it never claims. A cake already in progress is unaffected. | PM `TC_CAKE_OccupiedReadyCakeSlotBlocksNewClaimUntilCleared` (blocked; no claim: ticket still pending and order still SentToStall; available again and claims order B after `ReadyShelf.PickUp` clears the slot); PM `TC_CAKE_ReadyGateDoesNotStopCakeAlreadyInProgress` |

Test-only change: `SendOrder(int table = 1)` in `CakeStationFlowTests`, because a second concurrent order needs a free table.

## 4. Requests for the lead (outside my ownership)

- **L1 (blocking for playability):** no bootstrap or scene instantiates `PF_CakeStation` or calls `CakeStation.Initialize`. `DrinkWaveBootstrap` builds `ContentDatabase` from the drink definition only, so cake orders can't exist in-game. Please do all three of these:
  - Place or instantiate `PF_CakeStation` at the stall cake anchor.
  - Add `SO_Item_Cake_DEV_TBD` (or the real cake definitions) to the `ContentDatabase`.
  - Call the **6-arg** `Initialize(Tickets, ids, events, clock, new CakeRecipeCatalog(Tickets, recipes), Shelf)` (`Shelf` is the same `ReadyShelf` the `ReadyCounterPoint` uses).
- **L2 (localization):** `SO_PromptText_DrinkWave.asset` already has `ready.slot_full`, `ready.no_order`, `ready.not_finished`, `ready.already_ready` and `hands.full`. These cake keys are **missing**:
  - **Cake:** `cake.fill`, `cake.cup.pickup`, `cake.empty_back`, `cake.pour`, `cake.flip`, `cake.cut`, `cake.sauce`, `cake.roll_vertical`, `cake.wrap`, `cake.wait`, `cake.ready`, `cake.remove_burnt`, `cake.discard_burnt`, `cake.discard_orphan` (new), `cake.already_poured`, `cake.batter_not_measured`, `cake.batter_not_poured`, `cake.batter_out_of_tolerance`, `cake.cup_not_held`, `cake.flip_unavailable`, `cake.flip_unconfigured`, `cake.invalid_measurement`, `cake.need_cut_first`, `cake.need_flip_first`, `cake.need_roll_first`, `cake.need_sauce_first`, `cake.not_burnt`, `cake.not_cooked`, `cake.recipe_not_configured`, `cake.sauce_unconfigured`, `cake.wrong_sauce`, `cake.station_busy`, `cake.station_uninitialized` (new), and `cake.state.<state>` for every `CakeState`.
  - **Grill:** `grill.open`, `grill.close`, `grill.already_open`, `grill.lid_closed`, `grill.occupied`, `grill.preheating`.
  - **Other:** `ready.place`, `stall.no_ticket.cake`, `game.paused`, `role.not_stall`.
  - **Wording:** Cakes uses `game.paused` and `role.not_stall`, while Ready uses `interaction.paused` and `interaction.stall_role_required`. Please decide on one key family; if Cakes should change, I'll do it in a follow-up.
- **L3:** `PF_CakeStation` scalars `_preheatSeconds: 1` and `_openLidDegrees: 70` are DEV seeds (`[Tbd]` in code) but are not labelled DEV in the asset. Please note them in the decision register (DEC-007/DEC-013).
- **L4 (docs):** Docs/CAKE_WORKFLOW.md has drifted from the code:
  - **§2.3:** says `IFlipAction.Perform → FlipOutcome`; the code has `Destination` + `Present(CakeItem)`.
  - **§6:** quality includes a doneness score with weights in `SO_Balance_Slice`; the code scores batter deviation only, using `CakeRecipe.DeviationPenaltyPerMl` and the policy on `CakeRecipe`.

  Please either update the doc or open a DEC.
- **L5:** `TC-CAKE-017` (no drink interactable accepts the batter cup, GT-003) has no test. It belongs to Drinks.

## 5. Notes (not changed, non-blocking)

- **Order events fire while the station is mid-update.** Each case below happens synchronously inside `OrderService`; Cakes publishes none of these events:
  - **Claim:** `OrderItemStatusChanged(InPreparation)` is published inside `ClaimNext`, before `CakeStation.Current` is assigned. If no recipe resolves, a `Pending` event follows immediately.
  - **Burnt removal and empty back:** the `Pending` event is published while `Current` still references the released preparation.
- `PlaceholderFlipAction.Present` leaves the spatula rotated at −15°. This is cosmetic only.

## 6. Harness (what ran)

- **Location:** `scratchpad/m3/h` (`t.csproj`, `UnityStub.cs`, `Harness.cs`), outside the repo. It is .NET 8.0.131 with NUnitLite 3.14.0, and `UNITY_EDITOR`/`UNITY_INCLUDE_TESTS` are defined.
- **Compiled from the worktree:**
  - Core, Content (ItemDefinition/ContentDatabase/CakeRecipe/MeasureCupDefinition), Orders, the Interaction subset, all `Scripts/Cakes/*.cs` and `Stall/Runtime/ReadyCounterPoint.cs`.
  - Tests: `Tests/EditMode/Cakes/CakeDomainTests.cs` and `Tests/PlayMode/Cakes/CakeStationFlowTests.cs` (the real test files, unmodified by the harness).
- **How the PlayMode tests run:** `AssetDatabase.LoadAssetAtPath` returns managed stand-ins. Their scalar values are parsed from the saved YAML, and an unknown field name throws. Their structure mirrors the YAML verified in §2. `[UnityTest]` coroutines are drained by a runner, frames are no-ops, and `Update` is not ticked.
- **Results:**
  - **Baseline (`c68cc3a` code):** 23/23 passed.
  - **New tests against the old code:** 3 of the 5 new tests fail, one per fix (F1, F2, F3). The two gate tests (F4) were checked separately with the gate line removed, and both fail.
  - **Final:** **28/28 passed**: 20 EditMode cases (CakeDomainTests including parameterized cases) and 8 PlayMode cases.
- **Not run:** `CakeSavedAssetTests` (it needs a real `AssetDatabase`/`SerializedObject`). It is replaced by the YAML audit in §2.

## 7. Unverified

- Nothing ran in Unity 6000.6.0f1: no EditMode or PlayMode runner, no import, no compile with the real UnityEngine.
- Real `Object.Destroy` timing: the orphan test yields one frame before asserting `item == null`.
- `MaterialPropertyBlock`/renderer visuals and the lid rotation.
- `CakeSavedAssetTests`.
- Any scene: none contains the cake station.
