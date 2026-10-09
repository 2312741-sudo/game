# MAIN-004 — Lobby and order flow audit (dine-in, takeaway, mixed drink + cake)

Branch: `feature/MAIN-004-lobby-orders` (base `origin/wave/MAIN-001-integration` @ `ecc55aa`)
Owner: CLAUDE-04
Verification: .NET 8 + NUnitLite harness with managed UnityEngine stand-ins (outside the repo). **No Unity Editor run; nothing here is a Unity PASS.**

## 1. Scope audited

`Scripts/Orders/**`, `Scripts/Lobby/**`, `Scripts/UI/Orders/**`, `Scripts/Customers/**` (contains only the asmdef + AssemblyInfo), `Tests/EditMode/{Orders,Lobby,UI}/**`, `Docs/ORDER_SYSTEM.md`; read-only cross-checks of `Core/Bootstrap/DrinkWaveBootstrap.cs`, `Stall/Runtime/ReadyCounterPoint.cs`, `Drinks/Domain/TeaRackInventory.cs`, `Drinks/Runtime/TeaRackController.cs`, `Cakes/CakeStation.cs`, `Debug/Drinks/*`.

## 2. Flow table (step → class/method → test → gap)

| Step | Class / method | Tests | Gap |
|---|---|---|---|
| T1 customer requests at table / vehicle | `TableOrderPoint` / `VehicleOrderPoint` → `OrderPoint.RequestCustomerService` → `OrderService.RequestService` | `OrderPointTests.*RequestService*`, `MixedOrderLobbyFlowTests` (new) | No customer controller exists (`Scripts/Customers` is empty); requests are scripted in `DrinkWaveBootstrap.Start` (drink only). |
| T2 Lobby takes order | `OrderPoint.Execute` → `LobbyOrderController.OpenEntry` → `OrderService.BeginTaking`; publishes `OrderEntryRequested` | `OrderPointTests.Point_Execute*`, `LobbyOrderControllerTests.OpenEntry*`, new Lobby test | none |
| Entry UI shows request | `OrderEntryModel.OnRequested` (`OrderEntryUI` view) | `OrderEntryModelTests`, new Lobby test (model only) | `OrderEntryUI` (UIElements) not compiled in harness — unverified. |
| T3 Enter | `OrderEntryModel.Enter` → `OrderEntryConfirmed` → `LobbyOrderController.OnConfirmed` → `OrderService.Enter` (DEC-012 match + shelf capacity) | `LobbyOrderControllerTests.Enter_*`, `OrderGameplayTests.TC_ORDER_006_*`, new Lobby test | none |
| T4 Send → stall ticket | `OrderEntryModel.Send` → `OrderSendRequested` → `LobbyOrderController.OnSendRequested` → `OrderService.SendToStall` (FIFO insert) | `LobbyOrderControllerTests.Send_*`, `OrderGameplayTests.TC_ORDER_003_*`, new tests | none |
| Lobby not bypassed | Only `LobbyOrderController` calls `BeginTaking/Enter/SendToStall`; only `OrderPoint` calls `RequestService`; UI events without a Lobby session are ignored | `LobbyOrderControllerTests.EventsWithoutAnOpenedSessionAreIgnored`, `OrderGameplayTests.GT_004_*`, new Lobby test (stray events + closed model) | Dev tool `Debug/Drinks/DevelopmentPickupTicketQueue` fabricates `OrderItemRef`s without a domain order (used only by `TeaRackPickupTestSetup`, gated by `Debug.isDebugBuild`) — flagged, acceptable as dev-only. No production bypass found. |
| T5 preparation claims (per kind) | `StallTicketQueue.HasPending/ClaimNext(ItemKind)` → `OrderService.Claim`; callers `TeaRackInventory.TryTakeBag` (Drink), `CakeStation.Measure` (Cake) | `OrderGameplayTests.GT_004_*`, `TC_ORDER_003_*`, new `MixedOrderFlowTests` (drink and cake queues independent, cake skips drink-only ticket) | none in domain; see soft-lock (§4). |
| Release / reclaim | `StallTicketQueue.Release` → `OrderService.Release` | `TC_ORDER_009_*`, new `MAIN004_ReleasedCakeOfAPartiallyReadyMixedOrderIsReclaimedBeforeReady` | none |
| T6 Ready (all items) | `ReadyShelf.PlaceReady` → `OrderService.CommitReady` (order Ready only when every item Ready; ticket removed then) | `TC_ORDER_005_*`, new mixed tests in both orders (drink-first, cake-first) | none |
| T7 Lobby pickup | `ReadyOrderPickupPoint.Execute` → `ReadyShelf.PickUp` (whole order only) → `ServedOrder` bundle | `DeliveryAdapterTests`, new Lobby test (partial order blocked `ready.no_ready_order`) | Pickup needs empty hands → soft-lock §4. |
| T8 delivery to own point | `OrderPoint.Execute(ServedOrder)` → `OrderService.Deliver` (origin + customer match) | `OrderDeliveryTests`, `DeliveryAdapterTests`, new tests (wrong point rejected, then correct) | none |
| T9 Completed | `OrderPoint.Deliver` → `OrderService.Complete` (immediate in slice) | `OrderDeliveryTests.TC_ORDER_001_*`, new tests | Completion is by the point, not a customer controller (slice behaviour, as documented). |
| Failure | `OrderService.Fail` (debug / CustomerLeft) | `TC_ORDER_008_*` | No production caller (slice: debug only). |

## 3. Changes in this branch

- **New** `Tests/EditMode/Orders/MixedOrderFlowTests.cs` (6 cases): drink + cake order through `OrderService` + `StallTicketQueue` + `ReadyShelf` + `IOrderDelivery` for table and vehicle, drink-first and cake-first; asserts per-kind queues, partial readiness keeps ticket and blocks pickup, exact status sequence T1…T9, item status events, wrong-target rejection; cross-order kind separation (drink-only table + mixed vehicle); released cake reclaimed before Ready.
- **New** `Tests/EditMode/Lobby/MixedOrderLobbyFlowTests.cs` (1 case): mixed orders at a table and a vehicle through `OrderPoint` → `LobbyOrderController` → real `OrderEntryModel.Enter/Send`, stall claims, `ReadyOrderPickupPoint`, wrong-point rejection, delivery, completion; stray UI events cannot bypass the Lobby.
- No production code changed: no domain bug found in the mixed path (mutation check: forcing `CommitReady` to mark Ready after the first item makes all 7 new cases fail).

## 4. Confirmed gameplay soft-lock (not fixed here — outside ownership / changes claim semantics)

With shelf capacity 1/1 (DEC-014) and one player holding both roles, a held finished item that cannot be placed has no exit:

1. Table: drink + cake; vehicle: drink. Both sent. Drink for table placed (drink slot full, table order still waiting for cake).
2. Player takes the next tea bag → `ClaimNext(Drink)` binds the vehicle drink; player finishes it.
3. `ReadyCounterPoint` → `ready.slot_full`; `ReadyOrderPickupPoint` → `hands.full`; `CakeStation` Fill needs the cup in hand → blocked; there is no put-down/discard for a tea bag. Only the debug `Fail` exits.

The same lock happens with two drink-only orders (first Ready, second drink in hand → pickup `hands.full`) — i.e. it is reachable in the current drink-only bootstrap (table + vehicle).
Harness probe (scratch, not committed) confirmed: `CanPlace = ready.slot_full`, table order `InPreparation`, pickup query `hands.full`, cake still pending.

Proposed fixes for the lead (pick one):
- (a) **Stall/Drinks/Cakes**: block starting a preparation (`TeaRackController.Query/Execute`, `CakeStation` Fill) when `IReadyShelfPlacement.Occupied(kind)` is true **or** a same-kind preparation is already in flight (`Occupied` exists on the contract but has no caller).
- (b) **Orders (needs contract sign-off)**: `StallTicketQueue.ClaimNext(kind)` fails with `ready.slot_full` when in-flight + shelved items of that kind ≥ shelf capacity. With FIFO claims this is provably deadlock-free for any mix, but it changes `ClaimNext` behaviour that existing tests (`TC_ORDER_003_*`) and `ORDER_SYSTEM.md §5` rely on, so it was not done here.
- (c) Raise drink/cake shelf capacity in bootstrap (reduces, does not remove, the lock).

## 5. Lead requests (bootstrap / scene — not owned by MAIN-004)

For mixed drink + cake orders from tables and vehicles, `Scripts/Core/Bootstrap/DrinkWaveBootstrap.cs` (or the main-scene bootstrap) must:

1. Add a cake `ItemDefinition` field and pass **both** definitions to `new ContentDatabase(new[] { _drinkDefinition, _cakeDefinition })`; otherwise `RequestService` rejects any cake request with `order.items.invalid`.
2. Keep `OrderService(..., drinkCapacity, cakeCapacity)` and `new ReadyShelf(service, Tickets, drinkCapacity, cakeCapacity)` equal (constructor throws otherwise); slice: 1/1.
3. Instantiate/enable the cake station and call `CakeStation.Initialize(Tickets, _ids, _events, _clock, new CakeRecipeCatalog((IOrderItemCatalog)Tickets, recipes))` with the **same** `StallTicketQueue` instance used by `TeaRackController.Initialize`; initialize every `CakeStationPoint`. Do not disable the Grill/BatterArea/RollArea/Sauce/Wrap anchors that the station uses.
4. Keep a single `ReadyCounterPoint.Initialize(Shelf, _events)` with both `DrinkPlacementPoint` and `CakePlacementPoint` assigned, and `ReadyOrderPickupPoint.Initialize(Shelf, Orders, _events)`.
5. Scripted customers: request `{ drink x1, cake x1 }` via `Table.RequestCustomerService` / `Vehicle.RequestCustomerService` (never `IOrderService` directly) so T2–T4 still go through `LobbyOrderController` + `OrderEntryUI`.
6. Decide the §4 soft-lock fix before a mixed playtest.
7. Docs (`Docs/ORDER_SYSTEM.md`, not owned): §7 says T7 happens at `ReadyCounterPoint` — it is `ReadyOrderPickupPoint`; §9 API sketch is stale (`BeginTaking` takes the point, `PickUp` lives on `IReadyShelfPickup`, `Deliver/Complete` on `IOrderDelivery` with an actor). `LobbyOrderController` XML summary still says "Delivery and completion are out of scope" (cosmetic).

## 6. Harness counts

Harness compiles Core, Orders, Lobby, Interaction subset, `UI/Orders/OrderEntryModel.cs` and all tests in `Tests/EditMode/Orders/**`, `Tests/EditMode/Lobby/**`, `Tests/EditMode/UI/OrderEntryModelTests.cs` into one assembly.

| Run | Total | Passed | Failed |
|---|---|---|---|
| Baseline (`ecc55aa`) | 174 | 173 | 1 |
| This branch | 181 | 180 | 1 |
| New tests only | 7 | 7 | 0 |

The single failure in both runs is `OrderContractTests.ARCH001_PublicContractsReferenceOnlyCoreOrdersAndSystemTypes`, a harness artifact: it checks assembly names (`TramChanh.Core`/`TramChanh.Orders`) and the harness compiles everything into one assembly.

## 7. Not verified

- Anything in the Unity Editor/Test Runner, PlayMode tests, scenes, prefabs, serialized wiring.
- `OrderEntryUI.cs` and `OrderEntryUITests.cs` (UIElements / UnityEditor not stubbed).
- `Is.Not.AllocatingGCMemory()` is approximated in the harness with `GC.GetAllocatedBytesForCurrentThread` after a warm-up call.
- Real `CakeStation`/`TeaRackController` driving the shelf for a mixed order (tests use prepared-item fakes).
