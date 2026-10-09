# MAIN-102 — Gameplay reliability (deadlock / edge audit)

Owner: CLAUDE-02 · Branch: `feature/MAIN-102-reliability` (base `wave/MAIN-001-integration` @ 248eac1)

> **No Unity PASS is claimed.** This container has no Unity Editor. Verification used .NET 8 + NUnitLite harnesses
> with managed UnityEngine stand-ins, plus a compile of all runtime scripts (and the new test files) against the real
> UnityEngine 2021.3.33 reference DLLs. The changes must be validated in Unity 6 (6000.6.0f1) EditMode/PlayMode.

## Summary of code changes

| Commit | Change | Files |
|---|---|---|
| drink orphan discard | `TeaBagItem` held-use `drink.discard_orphan`, offered only when the claimed order item is no longer live; retires the preparation, releases the hand, destroys the bag; publishes nothing order-related. Drink stations block steps on an orphaned bag with `ready.no_order`. | `Drinks/Domain/DrinkPreparation.cs` (`IsOrphaned`, `IsRetired`, internal `Retire()`), `Drinks/Runtime/TeaBagItem.cs`, `Drinks/Runtime/DrinkActionGuards.cs` |
| tea rack restock (lead priority) | When the rack has no stored bag, every *previously stocked* slot whose bag is Ready or a discarded orphan gets a fresh, unbound Stored bag with a new `PreparationId`. Held or in-preparation bags keep their slot. Never-stocked slots stay empty. No ticket claimed, no event published. Quantity policy remains **DEC-015 TBD** (documented in code; balance asset untouched). | `Drinks/Domain/TeaRackInventory.cs` (`RestockWhenEmpty`, `IsRestockable`), `Drinks/Runtime/TeaRackController.cs` (`RestockEmptySlots()`, called from `Query`/`Execute`) |
| tea bag feedback (MAIN-103 request) | `TeaBagItem` implements `IPreparationFeedback`: `PreparationStateKey` = `drink.state.<state>`; `NextActionKey` maps Stored/PickedUp → `drink.bag.open`, Opened → `drink.add_coconut`, CoconutJellyAdded → `drink.add_lemon`, LemonJellyAdded → `drink.add_ice`, IceAdded → `drink.bag.shake`, Shaken → `drink.wipe`, Wiped → `ready.place_item`, Ready → `ready.pick_up_order`, orphaned bag → `drink.discard_orphan`. | `Drinks/Runtime/TeaBagItem.cs` |
| batter cup return | An empty cup (no measurement or claim in progress) can be put back on its stand with its held action `cake.cup.return`. A measured cup still empties first (unchanged top-up flow). | `Cakes/BatterMeasureCup.cs` |

No public contract in `Docs/ACCEL-01_CONTRACTS.md` / `Docs/ORDER_SYSTEM.md` changed. The new public members are additive
(`DrinkPreparation.IsOrphaned/IsRetired`, `TeaBagItem.DiscardOrphanPromptKey`, `TeaBagItem : IPreparationFeedback` (this one closes a gap against the ACCEL-01 contract), `TeaRackInventory.RestockWhenEmpty/IsRestockable`,
`TeaRackController.RestockEmptySlots`, `BatterMeasureCup.ReturnPromptKey`). ORDER_SYSTEM §3.3 already says "the drink/cake
in progress becomes unassigned and may be discarded"; the drink discard implements that sentence.
§6.4 is respected: the discard and the restock mutate local Drinks state only and publish nothing; the order owner already
released the item (and published) inside `OrderService.Fail`.

## Scenario table

| # | Scenario | Behaviour now | Fix or restriction | Test(s) | Status |
|---|---|---|---|---|---|
| 1 | Player holds an item when its order is cancelled/failed | **Drink:** bag becomes orphaned → held-use `drink.discard_orphan` (Press, Stall role, not paused) → hand released, bag destroyed, no order events. Stations refuse further steps (`ready.no_order`); the ready counter already refused (`ready.no_order`). A shake hold in progress is cancelled because the held query changes. **Cake:** existing `cake.discard_orphan` / station self-clear. **Cup:** see #12. **Served bundle:** existing self-retire. | **Fix** (drink) | `DrinkOrphanDiscardTests` (7 cases); cake: `CakeStationFlowTests.TC_CAKE_WrappedCakeOfFailedOrderCanBeDiscardedFromHands`, `TC_CAKE_InvalidSequenceAndStaleBindingDoNotMutate`; bundle: `DeliveryAdapterTests.TC_ORDER_008_*` | Harness PASS; mutations caught |
| — | Tea rack runs empty in the endless loop (lead priority) | Capacity 2 rack now serves any number of drink orders; stock stays in `[0, capacity]`. | **Fix** | `TeaRackRestockTests` (7): 3 orders back to back at capacity 2, 10-order loop, discarded-orphan slot restocked, held/in-prep slot never refilled, restock unbound + silent, intentionally empty rack stays empty, domain only-when-empty | Harness PASS; mutations caught (trigger removed → 3-ticket test fails) |
| 2 | Ready counter occupied | Rack (`ready.slot_full`) and cake Fill (`ready.slot_full`) refuse new claims while their kind's slot is occupied (existing gates). New: exhaustive model check proves no cross-order deadlock between the two capacity-1 slots. | Existing gates + **new regression** | `TeaRackReadyGateTests`, `CakeStationFlowTests.TC_CAKE_OccupiedReadyCakeSlotBlocksNewClaimUntilCleared`, **`ReadySlotDeadlockModelTests`** (27 order mixes + gate-removed sanity case) | Harness PASS; LIFO-claim mutation caught |
| 3 | Wrong step | Each drink station / held action is Blocked with the missing-step reason; completed steps are Hidden; cake station blocks out-of-order steps. | Existing | `DrinkAdapterTests.TC_DRINK_007_WrongStatesAreBlockedAndUnrelatedHeldItemsAreHidden`, `DrinkPreparationTests`, `CakeStationFlowTests.TC_CAKE_InvalidSequenceAndStaleBindingDoNotMutate` | Harness PASS (existing) |
| 4 | Right step at the wrong station | Bag at an order point / pickup point → `hands.full`; unfinished bag at the ready counter → `ready.not_finished`; a non-bag held item at drink stations → Hidden; nothing mutates. | Existing + **new regression** | **`DrinkReliabilityScenarioTests.MAIN_102_S4_*`**, `DrinkAdapterTests.TC_DRINK_007_*`, `DeliveryAdapterTests.TC_ORDER_006_AnotherHeldItemCannotDeliverOrOpenTheOrderEntry` | Harness PASS; mutation caught |
| 5 | Customer leaves before receiving the order | **Restriction: customers never leave in this slice.** There is no patience system; `OrderService.Fail` is reachable only from tests (and a debug menu that does not exist yet). If `Fail` is triggered, every held item is recoverable: drink (#1), cake (orphan discard), cup (#12), served bundle (self-retire / discard at any point). | **Restriction** + recovery verified | `DrinkOrphanDiscardTests`, `CakeCupReturnTests.MAIN_102_CupHeldWhenTheOrderFailsCanBeReturned`, `CakeStationFlowTests.TC_CAKE_WrappedCakeOfFailedOrderCanBeDiscardedFromHands`, `DeliveryAdapterTests.TC_ORDER_008_FailedCarriedBundleReleasesOnlyItsOwnHandsAndCannotDeliver`, `OrderGameplayTests.TC_ORDER_008_*` | Harness PASS |
| 6 | Multiple active orders | One live order per point; tickets FIFO by send time (id tie-break); kinds queue separately; whole-order pickup oldest Ready first. Model check covers 3 concurrent orders. | Existing + **new regression** | `OrderGameplayTests.TC_ORDER_003_*`, `GT_005_GT_006_*`, `MixedOrderFlowTests.MAIN004_DrinkAndCakeQueuesServeKindsSeparately…`, **`ReadySlotDeadlockModelTests`** | Harness PASS |
| 7 | Drink and cake in the same order | Ready only when both are on the shelf; one pickup carries both; delivered to its origin. | Existing + model check | `MixedOrderFlowTests.MAIN004_*`, `MixedOrderLobbyFlowTests`, `ReadySlotDeadlockModelTests` | Harness PASS |
| 8 | Item bound to a different ticket | Ready placement checks the binding and kind (`ready.no_order`); the wrong delivery point rejects (`order.delivery.wrong_target`) and keeps the bundle; a stale preparation id cannot re-bind. | Existing | `OrderGameplayTests.TC_ORDER_005_UnfinishedUnboundWrongKindAndFullShelfDoNotMutate`, `TC_ORDER_009_*`, `DeliveryAdapterTests.TC_ORDER_004_*`, `OrderDeliveryTests.TC_ORDER_004_*` | Harness PASS (existing) |
| 9 | Picking up another item while holding one | The hand slot holds one item; rack / cup / pickup point / order point refuse with `hands.full` and claim nothing. | Existing + **new regression** | **`DrinkReliabilityScenarioTests.MAIN_102_S9_*`**, `TeaRackPickupTests`, `InteractionDriverTests.TC_INT_004_*` | Harness PASS; mutation caught |
| 10 | Order completion triggered twice | `Complete` accepts only `Delivered`; repeats return `order.transition.invalid`; the point retries only T9 and never re-delivers. | Existing | `OrderDeliveryTests.TC_ORDER_006_CompleteRequiresDeliveredAndRejectsTerminalRepetition`, `TC_ORDER_007_ReentrantCompletion…`, `DeliveryAdapterTests.TC_ORDER_007_CompletionRejection…` | Harness PASS (existing) |
| 11 | Customer interaction during preparation | Order point is Hidden for `SentToStall…Ready` (no prompt, no mutation); the delivery prompt appears only while holding the bundle. | Existing | `OrderPointTests.Point_QueryIsHiddenWithoutALiveOrderAndForOrdersPastEntry`, `Point_QueryPromptsFollowTheOrderStatusTable` | Harness PASS (existing) |
| 12 | An item that cannot be handed off | **Found and fixed:** the batter cup could leave the hands only by pouring. Picked up with no cake ticket, it blocked order entry and the tea rack (`hands.full`), so no cake ticket could ever arrive: a permanent soft-lock. Now `cake.cup.return` puts an empty cup back. Other items: burnt cake → `cake.discard_burnt`; orphan cake/drink → discard; obsolete bundle → discard at any order point. | **Fix** (cup) | **`CakeCupReturnTests`** (4) | Harness PASS; mutation caught |
| 13 | Duplicate finished item | Second `PlaceReady` / `MarkReady` → `ready.already_ready`; a bound preparation id cannot claim another ticket (`stall.ticket.preparation_bound`); the rack offers no second bag for a one-drink order. | Existing + **new regression** | **`DrinkReliabilityScenarioTests.MAIN_102_S13_*`**, `OrderGameplayTests.TC_ORDER_006_DuplicatePreparationIdCannotClaimTwoItems`, `TC_ORDER_005_ReadyRequiresEveryEnteredItemAndPlacementIsExactlyOnce` | Harness PASS; mutation caught |
| 14 | Restarting an interaction sequence (early release, re-begin) | Release cancels the hold (shake animation / wipe flag cleared, no state change); re-begin restarts progress from zero; cake hold cancel resets tool motion; continuous fill measures only the elapsed time. | Existing + **new regression** | **`DrinkReliabilityScenarioTests.MAIN_102_S14_*`**, `DrinkAdapterTests.GT_007_*`, `HeldUseDriverTests.TC_INT_009_HeldHoldCancellationResetsWithoutExecuting`, `CakeStationFlowTests.TC_CAKE_ContinuousFill…` | Harness PASS; mutation caught |
| 15 | Scene reload with active state | No mutable `static` fields, singletons, `DontDestroyOnLoad` or `RuntimeInitializeOnLoadMethod` anywhere in `Scripts/**` (only `static readonly` animator hashes and pure helpers). Services, EventBus and clock are per-bootstrap instances; the bootstrap disposes the shelf and the bus in `OnDestroy`; adapters dispose their subscriptions in `OnDisable`/`OnDestroy`. | Audit: no leak found | Code audit (grep); `ServiceRegistryTests`, `CoreServicesSmokeTests` (existing, not run here) | No issue; nothing to request |

## New prompt / reason keys (for the UX agent and the lead)

Not added to `SO_PromptText_TramChanhMain.asset` (out of my ownership). These are the only two new keys; the restock adds
none. I simulated the wave's `PromptTextCoverageTests` literal scan (same regex) over this branch's `Scripts/**` against the
current wave table (`origin/wave/MAIN-001-integration`, which includes MAIN-103): exactly these two keys are missing.
All `drink.state.*` keys and every `NextActionKey` target already exist in the wave table. Reused keys (`ready.no_order`, `role.not_stall`,
`game.paused`, `interaction.stall_role_required`, `hands.full`, `drink.rack.empty`) already exist in that table.

| Key | Kind | English | Vietnamese |
|---|---|---|---|
| `drink.discard_orphan` | held-use prompt | Discard tea bag (order cancelled) | Bỏ túi trà (đơn đã hủy) |
| `cake.cup.return` | held-use prompt | Put the measuring cup back | Đặt ly đong về chỗ cũ |

## Requests to the lead

1. **Localization:** add `drink.discard_orphan` and `cake.cup.return` (table above) to `SO_PromptText_TramChanhMain.asset`.
2. **Optional debug cancel:** `OrderService.Fail` has no in-game trigger (no patience, no debug menu), so the recovery paths
   in #1/#5 can only be reached by tests today. For manual QA of those paths, consider a DevTools or bootstrap debug
   action that calls `Orders.Fail(id, FailureReason.CancelledByDebug)` on the oldest active order. No bootstrap edit was made.
3. **Editor check of hand-view ordering:** both new held actions release the hand before moving the object (cup:
   `TryRelease()` then `ReturnHome()`, the same order as the existing pour path; bag: `TryRelease()` then destroy). Please
   confirm in Play Mode that `HeldItemView` (Interaction, not mine) does not re-parent the cup after it is home.
4. Bootstrap: no problem found (#15). Nothing requested.

## Harnesses (scratchpad `r02`, not in the repo)

| Harness | What | Result |
|---|---|---|
| `m002` (drinks) | Drinks + Orders + Lobby + ReadyCounterPoint with EditMode Drinks/Ready tests | **91 / 91** |
| `m3h` (cakes) | Cakes + Orders with CakeDomainTests + all PlayMode Cakes fixtures (runner generalised to every fixture in the namespace) | **32 / 32** |
| `m4` (orders/lobby) | Orders + Lobby + UI model with EditMode Orders/Lobby tests | **208 / 209**. The one failure, `ARCH001_PublicContractsReferenceOnlyCoreOrdersAndSystemTypes`, is a pre-existing harness artefact: it checks assembly names and the harness compiles everything into one assembly. It fails identically on the base commit. |
| `mainc/c.csproj` | All runtime `Scripts/**` against real UnityEngine reference DLLs | **0 errors** |
| `mainc/rt` | Runtime scripts + every new test file against the real reference DLLs (NUnit; editor stub for `AssetDatabase`) | **0 errors** |

Mutation checks (each test was run with its fix or guarded behaviour removed, then restored):
discard offer (6 failures), station orphan guard (1), restock trigger (3, including the 3-ticket test), restock of a held slot (2),
restock while stock remains (3), restock of never-stocked slots (2), cup return (4), LIFO ticket claims (model check + 6 existing),
hold-restart reset (2), orphan `NextActionKey` (1), second pickup while holding (4), order-point `hands.full` (1), duplicate preparation claim (1).

## Unverified

- The branch is based on 248eac1 and was not rebased onto the newer wave head (MAIN-101/103/104/105 merges). Those merges
  touch none of the files changed here; `PromptTextCoverageTests` (wave only) was simulated, not run.

- No Unity Editor run of any kind (EditMode, PlayMode, scene). In particular, these were not executed in any harness:
  `Tests/PlayMode/Drinks/TeaRackPickupPlayModeTests.cs` (checked by reading: its empty-rack case uses initial stock 0, and
  restock deliberately leaves never-stocked slots empty), `Tests/PlayMode/Integration/*`, `Tests/EditMode/Drinks/TeaRackPickupAssetTests.cs`.
- Visual placement of restocked bags at their slot anchors and the destroy/re-parent behaviour of `HeldItemView` (lead request 3).
- `DestroyImmediate` vs `Destroy` in the drink discard follows `ReadyCounterPoint`'s `Application.isPlaying` pattern; real Unity behaviour is unverified.
- The deadlock model abstracts station timing (cook/burn) and assumes no order failures during the search; failure recovery is covered separately (#1/#5).
