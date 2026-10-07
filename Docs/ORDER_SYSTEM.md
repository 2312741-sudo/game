# TRAM CHANH — ORDER SYSTEM

**Status:** Approved for the slice (REV-000, 2026-10-06). **Amended 2026-10-07** by the drink-wave plan review ([`Reviews/DRINK-WAVE-claude-plan-review.md`](Reviews/DRINK-WAVE-claude-plan-review.md) C1–C12): `ItemKind` lives in Core, typed `PreparationId`, validate-then-commit shelf protocol, shelf pickup owns T7. Signatures are final once the contract-first commit is approved (REV-002).
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
├── RecipeId           SO_ recipe (sauce etc. come from recipe data, DEC-008)
├── Status             OrderItemStatus
├── PreparationId      int? (set while bound to a drink/cake in preparation)
└── Quality            int 0..100 (set when the item reaches Ready)

ItemRequest
├── ItemDefinitionId
└── Quantity
```

`OrderType` and `Origin` are always consistent (constructor-enforced). `Items` expand quantities: two drinks = two `OrderItem`s, because each is prepared separately.

Slice limits: one item definition per kind; an order contains 1 drink, 1 cake, or 1 drink + 1 cake (customer scenario config). The limit is **enforced**, not assumed: `Enter` rejects an order whose quantity of any kind exceeds the Ready shelf's capacity for that kind (`order.too_many_for_shelf`; slice capacity 1 per kind, DEC-014). Otherwise two drinks could never both be placed and the order could never reach `Ready`.

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
| T2 | `WaitingForLobby` | `TakingOrder` | `BeginTaking(orderId, actor, at)` | Lobby interacting with that point | Domain: `at` equals the order's `Origin`; status is `WaitingForLobby`. **Adapter** (`Lobby`): `ActorRole.Lobby` — `ActorRole` lives in `Interaction`, which `Orders` cannot reference |
| T3 | `TakingOrder` | `Entered` | `Enter(orderId, items)` | Order-entry UI confirm (via `OrderEntryConfirmed`, §7) | `items` non-empty; every item exists in content (`IContentDatabase`); per-kind quantity ≤ shelf capacity (`order.too_many_for_shelf`); slice: `items == RequestedItems` (DEC-012) |
| T4 | `Entered` | `SentToStall` | `SendToStall(orderId)` | Order-entry UI "Send" | — ; order is enqueued in `IStallTicketQueue` |
| T5 | `SentToStall` | `InPreparation` | automatic when the first item is claimed (`ClaimNext`) | `StallTicketQueue` | At least one item `InPreparation` |
| T6 | `InPreparation` | `Ready` | automatic when the last item is placed on the Ready shelf | `ReadyShelf` | All items `Ready` |
| T7 | `Ready` | `PickedUpByLobby` | `IReadyShelfPickup.PickUp(orderId, actor)` — the **only** public path (§6) | Lobby at `ReadyCounterPoint` | Domain: order is `Ready` and all its items are on the shelf. **Adapter** (`Lobby`): `ActorRole.Lobby` and the actor's hands are empty |
| T8 | `PickedUpByLobby` | `Delivered` | `Deliver(orderId, target)` | Lobby at a table / vehicle point | `target` matches `Origin` **and** `CustomerId` |
| T9 | `Delivered` | `Completed` | `Complete(orderId)` | Customer controller after receiving | — (slice: immediately; later: after eating / payment) |
| T10 | any of `WaitingForLobby…PickedUpByLobby` | `Failed` | `Fail(orderId, reason)` | Customer (left) / debug cancel | Not `Delivered`/`Completed`/`Failed`. Releases every ticket binding **and clears the order's Ready-shelf slots** |

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

`CustomerLeft`, `CancelledByDebug`. Patience-driven leaving is P1; in the slice `Failed` is only reachable through tests and the debug menu. A failed order is removed from the ticket queue; any bound preparation is unbound (the drink/cake in progress becomes unassigned and may be discarded); any of its items already on the Ready shelf are removed so their slots are free again.

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

**Types.** `ItemKind` (`Drink = 0`, `Cake = 1`; explicit values, append-only because `ItemDefinition.Kind` serializes it) lives in
**`TramChanh.Core`**. `Stall`, `Lobby` and `Customers` may not reference `Content`, yet they consume `ItemKind` through the public contracts
below (`ARCHITECTURE.md` §4, public-API rule); `Content.ItemDefinition.Kind` reuses the Core enum.
`OrderItemRef` is a value type with equality over all three of `OrderId`, `OrderItemId` and `PreparationId`; `IsValid` needs all three;
`default` means *unbound*. `PreparationId` is the typed Core id, assigned (via `IIdGenerator`) when the preparation object is created,
so it is stable and unbound until D1.

`IStallTicketQueue`:

```csharp
IReadOnlyList<OrderId> Tickets { get; }          // FIFO by SentToStall time; ties broken by ascending OrderId
bool HasPending(ItemKind kind);                  // pure
Result<OrderItemRef> ClaimNext(ItemKind kind, PreparationId preparation);
Result Release(OrderItemRef item);               // stale or unknown ref => "stall.ticket.not_bound"
bool IsBound(OrderItemRef item);                 // all three fields match the live claim
```

Binding rules (slice):

- A preparation claims the **oldest pending item of that kind** across tickets (items of one order in ascending `OrderItemId`). Orders that are `Failed` are skipped. If none exists, the action is blocked with `"stall.no_ticket.drink"` / `"stall.no_ticket.cake"` — this is how "receive ticket from Lobby" (step 1 of both workflows) is enforced without adding a step.
- The first claim moves the order `SentToStall → InPreparation` (T5). `Release` returns the item to `Pending` and the order **stays** `InPreparation` (there is no backward order transition).
- The binding is stored on both sides: `OrderItem.PreparationId` and the preparation's `OrderItemRef`. `IsBound` compares all three fields, so a ref that outlived a release and a re-claim by another preparation is **stale** and is rejected.
- A blocked pickup never claims: the preparation runs every non-ticket guard first, then claims, then performs the physical pickup, and calls `Release` if the pickup is refused (`DRINK_WORKFLOW.md` §2.2).

Manual ticket selection by the player is P1.

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

### 6.1 Contracts (all defined in `Orders`; expose only Core and Orders types)

```csharp
public interface IPreparedItem                 // implemented by the Drinks / Cakes adapters
{
    ItemKind Kind { get; }
    OrderItemRef BoundItem { get; }            // default (invalid) when unbound
    bool IsFinished { get; }                   // final step done (drink: Wiped; cake: Wrapped); stays true after Ready
    int Quality { get; }
    Result MarkReady();                        // mutates ONLY on success; "ready.not_finished" / "ready.already_ready"
}

public interface IReadyShelfPlacement          // injected into Stall
{
    Availability CanPlace(IPreparedItem item); // pure, allocation-free (called every frame by ReadyCounterPoint.Query)
    Result PlaceReady(IPreparedItem item);
    bool Occupied(ItemKind kind);
}

public interface IReadyShelfPickup             // injected into Lobby
{
    OrderId NextReadyOrder { get; }            // oldest Ready order (time it became Ready; ties by OrderId); IsValid == false when none; pure
    Result<IReadOnlyList<IPreparedItem>> PickUp(OrderId id, ActorRef actor);   // snapshot, ascending OrderItemId
}

public interface IReadyShelf : IReadyShelfPlacement, IReadyShelfPickup { }     // union: composition root and tests only
```

`Result MarkReady()` is the shelf's way to move a Drinks/Cakes object to `Ready` without referencing either module; the drink and cake
workflows already call it (`DRINK_WORKFLOW.md` D8, `CAKE_WORKFLOW.md` C10). Contract, verified for **every** implementation by a shared
test fixture: `IsFinished` and not yet ready ⇒ `MarkReady()` succeeds.

### 6.2 Placement (`Stall`, holding an `IPreparedItem`) — validate, then commit, publish last

1. `CanPlace` (pure). Reasons, in priority order: `"ready.not_finished"` (covers "cannot Ready before shake/wipe"), `"ready.no_order"` (unbound or `!IsBound`), `"ready.already_ready"`, `"ready.slot_full"`.
2. `item.MarkReady()` **first**: it changes only the item and only on success, so a failure aborts with nothing changed.
3. Order-side commit: `OrderItem InPreparation → Ready`; when the last item is Ready, order `Ready` (T6). Already validated in step 1, so a failure here is an invariant violation: **throw**, never return partial success.
4. Occupy the slot, then publish `OrderItemStatusChanged` / `OrderStatusChanged` only after all state is consistent.

### 6.3 Pickup (`Lobby`) — the shelf is the single public owner of T7

`IReadyShelfPickup.PickUp(orderId, actor)`:

1. Validate (pure): the order exists, is `Ready`, and every one of its items is on the shelf. The actor's role and empty hands are **adapter** checks in `Lobby`.
2. Commit T7 `Ready → PickedUpByLobby` (pre-validated; a failure is an invariant violation: throw).
3. Clear **all** slots held by that order and return a snapshot of the items.
4. Publish last: `OrderStatusChanged (Ready → PickedUpByLobby)`. `Stall` adapters release the placed visuals on it; `Lobby` wraps the returned items in a generic `ServedOrder` holdable (`IHoldable`, class in `TramChanh.Lobby`; the composition root only injects its dependencies).

Partial pickup is not supported in the slice. The items' own state stays `Ready` until delivery (D9 / C11, driven by `OrderStatusChanged → Delivered`). `IOrderService` therefore has **no** `PickUp`.

---

## 7. Lobby adapters

| Component | Prefab | Role |
|---|---|---|
| `TableOrderPoint` | on `PF_YellowCrateTable` | Holds `TableId`, seat(s) (`PF_PlasticStool`), the seated customer, the active order. Interactable for **take order** (T2) and **deliver** (T8). |
| `VehicleOrderPoint` | `PF_Placeholder_VehiclePoint` — generic vehicle interaction point (DEC-010) | Holds `VehicleId`, the waiting customer, the active order. Interactable for take order and deliver. Knows nothing about the vehicle's model or type. |
| `LobbyOrderController` | on player (DEC-001) | Checks `ActorRole.Lobby`; opens the order-entry UI after T2; performs T3/T4 when the UI's `OrderEntryConfirmed` / `OrderSendRequested` events arrive; performs T7 through `IReadyShelfPickup` and T8 through the points; builds the `ServedOrder`. |
| `OrderEntryUI` | UI | Shows the customer's request pre-filled; buttons *Enter* (T3) and *Send to stall* (T4). |
| `StallTicketUI` | UI | Lists tickets with items and item status for the stall side. |
| `ReadyOrdersUI` | UI | Lists Ready orders and their destination (table/vehicle id). |

`UI` cannot reference `Lobby` (`ARCHITECTURE.md` §4), so the entry UI never calls the controller: it publishes the typed events `OrderEntryConfirmed { OrderId, Items }` (*Enter*) and `OrderSendRequested { OrderId }` (*Send to stall*), defined in `Orders`, and `LobbyOrderController` handles them. The two confirmations stay separate (DEC-003).

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

## 9. Public API sketch (final once the contract-first commit is approved, REV-002)

```csharp
public interface IOrderService   // Lobby, Customers' requester, order-entry intents
{
    Result<OrderId> RequestService(OrderOrigin origin, CustomerId customer, IReadOnlyList<ItemRequest> requested);
    Result BeginTaking(OrderId id, ActorRef actor, OrderOrigin at);
    Result Enter(OrderId id, IReadOnlyList<ItemRequest> entered);
    Result SendToStall(OrderId id);
    Result Deliver(OrderId id, DeliveryTarget target);
    Result Complete(OrderId id);
    Result Fail(OrderId id, FailureReason reason);
    IReadOnlyOrder Get(OrderId id);
    IReadOnlyList<IReadOnlyOrder> Active { get; }
}
```

There is intentionally no `CreateOrderFromStall` or `CreateOrderFromCustomer` that bypasses `BeginTaking`/`Enter`/`SendToStall` (GT-004), and no `PickUp` (owned by `IReadyShelfPickup`, §6.3).

**Public-API rule.** Interfaces consumed across modules (`IOrderService`, `IStallTicketQueue`, `IPreparedItem`, `IReadyShelfPlacement`, `IReadyShelfPickup`) expose only Core types and `Orders` types, never `Content` types (`ItemRequest.ItemDefinitionId` is a `string`). `OrderService`'s own implementation may use `IContentDatabase`.

**Interface segregation (GT-004 structure).** Each module receives only the narrow interface it needs:

| Module | Receives |
|---|---|
| `Lobby` | `IOrderService`, `IReadyShelfPickup` |
| `Customers` (scripted requester) | `IOrderService` (only `RequestService`, `Complete`, `Fail` are used) |
| `Drinks`, `Cakes` | `IStallTicketQueue` |
| `Stall` | `IReadyShelfPlacement` |
| `UI` | read-only views (`IReadOnlyOrder`, `Tickets`, `NextReadyOrder`) |

No stall-side type may hold `IOrderService` or `IReadyShelfPickup`.

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
| GT-004 | No API creates an order outside Lobby path | Reflection test: `IOrderService` has no other creation method; stall queue empty until `SendToStall`; injection test: no `Drinks`/`Cakes`/`Stall` type holds `IOrderService` or `IReadyShelfPickup` |
| TC-ORDER-010 | FIFO tie-break | Two orders sent at the same `ManualClock` time are claimed in ascending `OrderId` |
| TC-ORDER-011 | Stale ref | Release, then re-claim by another preparation: the first preparation's ref fails `IsBound` and `CanPlace` (`ready.no_order`) |
| TC-ORDER-012 | `ClaimNext` skips `Failed` orders; blocked pickup never claims | Queue unchanged |
| TC-READY-001 | `PlaceReady` validate → `MarkReady` → commit → publish | Fake item whose `MarkReady` fails ⇒ shelf, order and events unchanged; events arrive only after state is consistent |
| TC-READY-002 | `PreparedItemContractTests` | Every `IPreparedItem` implementation: `IsFinished` and not ready ⇒ `MarkReady()` succeeds |
| TC-READY-003 | `PickUp` | Only a `Ready` order; T7 committed; all its slots cleared; snapshot ascending `OrderItemId`; event last; second order can then be placed (slot freed) |
| TC-READY-004 | `NextReadyOrder` | Oldest Ready first; `IsValid == false` when none; allocation-free |
| TC-READY-005 | Capacity guard | `Enter` with two drinks on a capacity-1 shelf ⇒ `order.too_many_for_shelf` |
| TC-READY-006 | `Fail` after placement | The order's slots are freed |
| ARCH-001 | Public API has no `Content` types | Reflection over the public signatures of `Orders` contracts: none from `TramChanh.Content` |
| GT-005 | Dine-in origin is a table | `RequestService` with `DineIn` requires a `TableId` |
| GT-006 | Takeaway origin is a vehicle | `RequestService` with `Vehicle` requires a `VehicleId` |
