# TRAM CHANH — ORDER SYSTEM

**Status:** Draft for review gate `REV-002`
**Module:** `TramChanh.Orders` (domain, plain C#) + `TramChanh.Lobby` / `TramChanh.Stall` / `TramChanh.UI` (adapters)
**Ground truth:** GT-004, GT-005, GT-006 (see `ARCHITECTURE.md` §1)

---

## 1. Real-world flow (from [WF] §5.1 — not to be changed)

### Dine-in

```text
Customer sits at table
→ Lobby goes to table → takes order
→ Lobby enters order → sends to stall
→ Stall prepares → marks Ready
→ Lobby picks up → delivers to the correct table
```

### Takeaway at vehicle

```text
Customer at vehicle
→ Lobby goes out → takes order
→ Lobby enters order → sends to stall
→ Stall prepares → marks Ready
→ Lobby picks up → delivers to the correct customer/vehicle
```

The stall **never** receives an order directly from a customer.

---

## 2. Data model

```text
Order
├── OrderId            int (sequential, from IIdGenerator)
├── OrderType          DineIn | TakeawayVehicle
├── Origin             OrderOrigin  (DineIn(TableId) | Vehicle(VehicleId))
├── CustomerId         CustomerId
├── TableId            TableId?     (set iff DineIn)
├── VehicleId          VehicleId?   (set iff TakeawayVehicle)
├── RequestedItems     IReadOnlyList<ItemRequest>   (what the customer asked; DEC-012)
├── Items              IReadOnlyList<OrderItem>     (what the Lobby entered)
├── CreatedAt          double (IGameClock time)
├── StatusTimestamps   per-status entry time (for UI + later patience/economy)
├── Status             OrderStatus
├── FailureReason      FailureReason? (set iff Failed)
├── DeliveryAttempts   int (wrong deliveries counted here)
└── QualityScore       int 0..100 (computed at Delivered)

OrderItem
├── OrderItemId        int
├── ItemDefinitionId   string (SO_ ItemDefinition id)
├── Kind               Drink | Cake
├── Variant            e.g. SauceType for cakes (from recipe)
├── Status             OrderItemStatus
├── PreparationId      int? (set while bound to a drink/cake in preparation)
└── Quality            int 0..100 (set when the item reaches Ready)

ItemRequest
├── ItemDefinitionId
└── Quantity
```

`OrderType` and `Origin` are always consistent (constructor-enforced). `Items` expand quantities: two drinks = two `OrderItem`s, because each is prepared separately.

Slice limits: one item definition per kind; an order contains 1 drink, 1 cake, or 1 drink + 1 cake (customer scenario config).

---

## 3. Order status state machine

Statuses come from [WF] §9 and are used unchanged:

```text
WaitingForLobby → TakingOrder → Entered → SentToStall → InPreparation
→ Ready → PickedUpByLobby → Delivered → Completed
                       (any pre-Delivered state) → Failed
```

### 3.1 Transition table

| # | From | To | Trigger (API) | Called by | Guard |
|---|---|---|---|---|---|
| T1 | — | `WaitingForLobby` | `RequestService(origin, customerId, requested)` | Customer arriving at a `TableOrderPoint` / `VehicleOrderPoint` | Point not already occupied by an active order; `requested` non-empty |
| T2 | `WaitingForLobby` | `TakingOrder` | `BeginTaking(orderId, actor)` | Lobby interacting with that point | `actor.Role == Lobby`; actor is at the order's point |
| T3 | `TakingOrder` | `Entered` | `Enter(orderId, items)` | Order-entry UI confirm | `items` non-empty; every item exists in content; slice: `items == RequestedItems` (DEC-012) |
| T4 | `Entered` | `SentToStall` | `SendToStall(orderId)` | Order-entry UI "Send" | — ; order is enqueued in `IStallTicketQueue` |
| T5 | `SentToStall` | `InPreparation` | automatic when the first item is claimed (`ClaimNext`) | `StallTicketQueue` | At least one item `InPreparation` |
| T6 | `InPreparation` | `Ready` | automatic when the last item is placed on the Ready shelf | `ReadyShelf` | All items `Ready` |
| T7 | `Ready` | `PickedUpByLobby` | `PickUp(orderId, actor)` | Lobby at `ReadyCounterPoint` | `actor.Role == Lobby`; actor's hands empty |
| T8 | `PickedUpByLobby` | `Delivered` | `Deliver(orderId, target)` | Lobby at a table / vehicle point | `target` matches `Origin` **and** `CustomerId` |
| T9 | `Delivered` | `Completed` | `Complete(orderId)` | Customer controller after receiving | — (slice: immediately; later: after eating / payment) |
| T10 | any of `WaitingForLobby…PickedUpByLobby` | `Failed` | `Fail(orderId, reason)` | Customer (left) / debug cancel | Not `Delivered`/`Completed`/`Failed` |

Every other transition is **rejected**: the call returns `Result.Fail(ReasonKey)`, state is unchanged, nothing is published except `ActionBlocked` by the caller. Terminal states: `Completed`, `Failed`.

`T3` going back: while `TakingOrder`, the Lobby may cancel the entry UI → stays `TakingOrder` (no transition). Editing after `Entered` is not supported in the slice.

### 3.2 Wrong delivery (TC-ORDER-004)

`Deliver` with a non-matching target:

- returns `Fail("order.delivery.wrong_target")`;
- order stays `PickedUpByLobby`, items stay in the Lobby's hands;
- `DeliveryAttempts++`;
- publishes `DeliveryRejected { OrderId, AttemptedTarget, Reason }`.

Matching rule: `DineIn` → target must be `TableOrderPoint` with the same `TableId` *and* the seated customer is `CustomerId`. `TakeawayVehicle` → target must be the `VehicleOrderPoint` with the same `VehicleId` *and* the waiting customer is `CustomerId`.

### 3.3 Failure reasons (slice)

`CustomerLeft`, `CancelledByDebug`. Patience-driven leaving is P1; in the slice `Failed` is only reachable through tests and the debug menu. A failed order is removed from the ticket queue; any bound preparation is unbound (the drink/cake in progress becomes unassigned and may be discarded).

---

## 4. Order item status

```text
Pending → InPreparation → Ready
              │
              └──(ruined/discarded)──► Pending
```

| From | To | Trigger | Called by |
|---|---|---|---|
| `Pending` | `InPreparation` | `ClaimNext(kind, preparationId)` | Drink/cake station at the first preparation action (tea bag picked / batter measured) |
| `InPreparation` | `Ready` | `PlaceReady(preparedItem)` | `ReadyCounterPoint` |
| `InPreparation` | `Pending` | `Release(orderItemId)` | Discard of a ruined product (DEC-006) or order failure |

Item status does not track pickup/delivery; those are order-level.

---

## 5. Stall ticket queue and preparation binding

The stall sees **only** orders in `SentToStall` or `InPreparation` (GT-004).

`IStallTicketQueue`:

```text
IReadOnlyList<OrderId> Tickets                     FIFO by SentToStall time
bool HasPending(ItemKind kind)
Result<OrderItemRef> ClaimNext(ItemKind kind, int preparationId)
void Release(OrderItemRef item)
```

Binding rule (slice): when a preparation starts, it claims the **oldest pending item of that kind** across tickets. If none exists, the action is blocked with `"stall.no_ticket.drink"` / `"stall.no_ticket.cake"` — this is how "receive ticket from Lobby" (step 1 of both workflows) is enforced without adding a step.

The binding is stored on both sides: `OrderItem.PreparationId` and the preparation's `OrderItemRef`. Manual ticket selection by the player is P1.

---

## 6. Ready shelf (Ready counter)

Domain `IReadyShelf` + adapter `ReadyCounterPoint` (`PF_ReadyCounterPoint`, [AP] §49):

```text
PF_ReadyCounterPoint
├── InteractionTrigger
├── DrinkPlacement      (slot, capacity 1 in slice — DEC-014)
├── CakePlacement       (slot, capacity 1 in slice)
└── OrderIndicator
```

Contract shared with Drinks/Cakes (defined in `Orders`):

```csharp
public interface IPreparedItem
{
    ItemKind Kind { get; }
    OrderItemRef BoundItem { get; }   // invalid if unbound
    bool IsFinished { get; }          // drink: Wiped; cake: Wrapped
    int Quality { get; }
}
```

Placement (`Stall` interaction, holding an `IPreparedItem`):

1. Blocked if `!IsFinished` → `"ready.not_finished"` (covers "cannot Ready before shake/wipe").
2. Blocked if unbound → `"ready.no_order"`.
3. Blocked if slot of that kind is full → `"ready.slot_full"`.
4. Otherwise item placed, `OrderItem → Ready`, the preparation's own state → `Ready`; if all items of the order are Ready → order `Ready` (T6).

Pickup (Lobby, empty hands): picks up **all items of one Ready order** as a single held `ServedOrder` bundle (T7). Partial pickup is not supported in the slice. When several orders are Ready (later), the oldest is picked first.

---

## 7. Lobby adapters

| Component | Prefab | Role |
|---|---|---|
| `TableOrderPoint` | on `PF_YellowCrateTable` | Holds `TableId`, seat(s) (`PF_PlasticStool`), the seated customer, the active order. Interactable for **take order** (T2) and **deliver** (T8). |
| `VehicleOrderPoint` | placeholder vehicle (DEC-010) | Holds `VehicleId`, the waiting customer, the active order. Interactable for take order and deliver. |
| `LobbyOrderController` | on player (DEC-001) | Opens the order-entry UI after T2; performs T3, T4; performs T7/T8 through the points. |
| `OrderEntryUI` | UI | Shows the customer's request pre-filled; buttons *Enter* (T3) and *Send to stall* (T4). |
| `StallTicketUI` | UI | Lists tickets with items and item status for the stall side. |
| `ReadyOrdersUI` | UI | Lists Ready orders and their destination (table/vehicle id). |

Prompts at an order point depend on the order's status:

| Order status at point | Lobby holding | Prompt | Action |
|---|---|---|---|
| `WaitingForLobby` | nothing | "Take order" | T2 + open entry UI |
| `TakingOrder` / `Entered` | nothing | "Continue order" | reopen entry UI |
| `PickedUpByLobby` (this order) | its `ServedOrder` | "Deliver" | T8 |
| `PickedUpByLobby` (another order) | a `ServedOrder` | "Deliver" | T8 → rejected (wrong target) |
| other | — | none | — |

---

## 8. Quality score

Computed at `Delivered`: `QualityScore = round(mean(OrderItem.Quality))`.
- Drink in strict mode: 100 (every step done in order; there is no partial credit in the slice).
- Cake: from batter deviation and doneness (see `CAKE_WORKFLOW.md` §6).
Wrong deliveries do not change quality in the slice; they are counted in `DeliveryAttempts` for later economy/feedback.

---

## 9. Public API sketch (not final code)

```csharp
public interface IOrderService
{
    Result<OrderId> RequestService(OrderOrigin origin, CustomerId customer, IReadOnlyList<ItemRequest> requested);
    Result BeginTaking(OrderId id, ActorRef actor);
    Result Enter(OrderId id, IReadOnlyList<ItemRequest> entered);
    Result SendToStall(OrderId id);
    Result PickUp(OrderId id, ActorRef actor);
    Result Deliver(OrderId id, DeliveryTarget target);
    Result Complete(OrderId id);
    Result Fail(OrderId id, FailureReason reason);
    IReadOnlyOrder Get(OrderId id);
    IReadOnlyList<IReadOnlyOrder> Active { get; }
}
```

There is intentionally no `CreateOrderFromStall` or `CreateOrderFromCustomer` that bypasses `BeginTaking`/`Enter`/`SendToStall` (GT-004).

---

## 10. Test cases

From [WF] §3 PHASE 3, plus transition coverage:

| ID | Case | Expected |
|---|---|---|
| TC-ORDER-001 | Dine-in full flow at table T1 | Statuses visited in exact order T1…T9; ends `Completed`; `TableId` set, `VehicleId` null |
| TC-ORDER-002 | Takeaway at vehicle V1 full flow | Same sequence; `VehicleId` set, `TableId` null; delivered to V1 |
| TC-ORDER-003 | Two orders (one dine-in, one vehicle) in flight | FIFO tickets; items bind to oldest order; each delivered to its own target |
| TC-ORDER-004 | Deliver order of T1 to V1 | Rejected; status stays `PickedUpByLobby`; `DeliveryAttempts == 1`; `DeliveryRejected` published |
| TC-ORDER-005 | Ready order | Order reaches `Ready` only when **all** items are on the shelf; partial → stays `InPreparation` |
| TC-ORDER-006 | Every illegal transition (matrix) | Rejected, state unchanged |
| TC-ORDER-007 | Stall cannot see orders before `SentToStall` | `HasPending` false while `Entered` |
| TC-ORDER-008 | Fail from each non-terminal state | `Failed`, removed from queue, bound preparation released |
| TC-ORDER-009 | Discard ruined cake | Item back to `Pending`; next measure re-claims it |
| GT-004 | No API creates an order outside Lobby path | Reflection test: `IOrderService` has no other creation method; stall queue empty until `SendToStall` |
| GT-005 | Dine-in origin is a table | `RequestService` with `DineIn` requires a `TableId` |
| GT-006 | Takeaway origin is a vehicle | `RequestService` with `Vehicle` requires a `VehicleId` |
