# TRAM CHANH — DRINK WORKFLOW

**Status:** Approved for the slice (REV-000, 2026-10-06). Workflow unchanged; quantities are provisional data (DEC-007).
**Module:** `TramChanh.Drinks`
**Ground truth:** GT-003, GT-007

---

## 1. Real-world workflow (locked)

From [WF] §5.2 and §32 GT-007:

```text
1. Receive ticket from Lobby
2. Take the correct PRE-PORTIONED tea bag from the RED rack
3. Open the tea bag
4. Add coconut jelly
5. Add lemon jelly
6. Scoop ice
7. Shake the bag
8. Wipe the bag
9. Place at Ready / hand-off area
```

Tea is already portioned. **There is no tea-measuring step**, no tea pouring, no syrup pumping, no sealing step. Pump bottles exist on the counter as static props only. The 500 ml measuring cup belongs to the **cake** workflow (batter); drink interactables never accept it.

---

## 2. State machine

States from [WF] §11, used unchanged (`TeaBagState`):

```text
Stored → PickedUp → Opened → CoconutJellyAdded → LemonJellyAdded
       → IceAdded → Shaken → Wiped → Ready → Delivered
```

[AP] §18 lists a coarser visual set (`ToppingsAdded`). It is a *visual* grouping only: both jelly states use the toppings visual. The gameplay state machine keeps the two jelly states separate so the coconut → lemon order is enforced.

### 2.1 Transition table (strict mode — slice)

| # | From | To | Action | Where | Guard | Blocked reason key (otherwise) |
|---|---|---|---|---|---|---|
| D1 | `Stored` | `PickedUp` | Take bag | `PF_RedTeaRack` | Hands empty; rack count > 0; `StallTicketQueue.HasPending(Drink)` → claims item | `stall.no_ticket.drink`, `hands.full`, `drink.rack.empty` |
| D2 | `PickedUp` | `Opened` | Open (UseHeld) | in hand | — | — |
| D3 | `Opened` | `CoconutJellyAdded` | Add coconut jelly | `CoconutJellyBin` | Holding this bag | `drink.need_open` |
| D4 | `CoconutJellyAdded` | `LemonJellyAdded` | Add lemon jelly | `LemonJellyBin` | Holding this bag | `drink.need_coconut_first` |
| D5 | `LemonJellyAdded` | `IceAdded` | Scoop ice | `PF_IceBin` (+ `PF_IceScoop` anim) | Holding this bag | `drink.need_lemon_first` |
| D6 | `IceAdded` | `Shaken` | Shake (UseHeld, Hold) | in hand | Hold completes | `drink.need_ice_first` |
| D7 | `Shaken` | `Wiped` | Wipe (Hold) | `WipeArea` (+ `PF_WipeCloth`) | Hold completes | `drink.need_shake_first` |
| D8 | `Wiped` | `Ready` | Place at Ready | `PF_ReadyCounterPoint/DrinkPlacement` | Bound to an order item; slot free | `ready.not_finished`, `ready.no_order`, `ready.slot_full` |
| D9 | `Ready` | `Delivered` | (order delivered) | — | Driven by `OrderStatusChanged → Delivered` | — |

Each interactable is **Blocked** (not Hidden) when the player holds a bag in the wrong state, so the prompt teaches the correct next step. Interactables are **Hidden** when the player holds nothing relevant.

`IsFinished` (for `IPreparedItem`) = state `Wiped`. This enforces [WF] Phase 4 acceptance: cannot be Ready before shaking, cannot complete before wiping.

### 2.2 Order binding

- D1 calls `StallTicketQueue.ClaimNext(Drink, preparationId)`. The bag is bound to that `OrderItem` for its whole life.
- If the bound order fails (`CustomerLeft`), the bag becomes unbound; it can still be finished but cannot be placed at Ready (`ready.no_order`) until a later rebind feature (P1). In the slice the player discards it.

### 2.3 Sequence mode

`DrinkPreparation` takes a `SequenceMode`. Only `Strict` is implemented. `FreeWithScoring` (P1) would allow out-of-order actions and reduce `Quality`; the interface reserves this but the slice must not ship any free-mode code path.

---

## 3. Domain class (sketch)

```csharp
public sealed class DrinkPreparation
{
    public int PreparationId { get; }
    public TeaBagState State { get; }
    public OrderItemRef BoundItem { get; }
    public int Quality { get; }                  // 100 in Strict when Wiped

    public Availability CanTakeFromRack(...);    // D1 guard (pure)
    public Result Open();                        // D2
    public Result AddCoconutJelly();             // D3
    public Result AddLemonJelly();               // D4
    public Result AddIce();                      // D5
    public Result Shake();                       // D6 (called on hold completion)
    public Result Wipe();                        // D7
    public Result MarkReady();                   // D8 (called by ReadyShelf)
    public Result MarkDelivered();               // D9
}
```

Every method returns `Result.Fail(reasonKey)` for an invalid source state and leaves the state unchanged. Publishes `DrinkStepCompleted` on success.

---

## 4. Station layout and adapters

Station hierarchy from [AP] §59:

```text
PF_DrinkStation
├── TeaRack            → TeaRackController       (PF_RedTeaRack)
├── ToppingStation     → ToppingBin ×2           (PF_ToppingStation: CoconutJellyBin, LemonJellyBin, TransparentCover)
│   ├── CoconutJellyBin
│   └── LemonJellyBin
├── IceBin             → IceBin                  (PF_IceBin: Bin, Lid optional, IceVolume)
├── IceScoop           → station tool, animated by IceBin (PF_IceScoop)
├── PumpBottles        → static, non-interactive (P1 asset)
├── WipeArea           → WipeInteraction (+ PF_WipeCloth visual)
└── ReadyPoint         → shared PF_ReadyCounterPoint (Stall module)
```

| Adapter (Codex) | Responsibility |
|---|---|
| `TeaRackController` | Holds `capacity` (DEC-015), spawns `PF_TeaBag_PrePortioned` into hands on D1, shows remaining bags visually |
| `TeaBagItem` | `IHoldable` + `IHeldItemAction` (open, shake) + `IPreparedItem`; owns a `DrinkPreparation` |
| `TeaBagStateView` | Maps state → visuals (below), drives shake animation |
| `ToppingBin` | One per bin, configured with `ToppingType`; opens the `TransparentCover` visually during the action (no extra step) |
| `IceBin` | Plays `PF_IceScoop` scoop animation, then D5 |
| `WipeInteraction` | Hold action at `WipeArea`, plays cloth animation, then D7 |
| `DrinkReadyZone` | Not a separate component: placement goes through `ReadyCounterPoint` in `Stall` |

---

## 5. Visual state mapping (tea bag)

From [AP] §18 and §81 (bag needs closed/open representation; toppings and ice must visually appear; shake and wipe must be animatable):

| State | `Bag_Closed` | `Bag_Open` | `Contents/CoconutJelly` | `Contents/LemonJelly` | `Contents/Ice` | Extra |
|---|---|---|---|---|---|---|
| Stored, PickedUp | on | off | off | off | off | tea liquid |
| Opened | off | on | off | off | off | |
| CoconutJellyAdded | off | on | on | off | off | |
| LemonJellyAdded | off | on | on | on | off | |
| IceAdded | off | on | on | on | on | |
| Shaken | off | on | on | on | on | `AN_TeaBag_Shake` played; condensation material param on |
| Wiped / Ready | off | on | on | on | on | condensation param off |

`Contents/*` children are child meshes inside the bag prefab (see `ASSET_INTEGRATION.md`). Whether the open bag is visually re-closed after shaking is not specified by the sources and is **not** modelled.

---

## 6. Data

`SO_Recipe_Drink_Slice` (`DrinkRecipe`): `ItemDefinition`, `TeaBagType` (one in slice), `CoconutJellyPortion`, `LemonJellyPortion`, `IcePortion`, `ShakeHoldSeconds`, `WipeHoldSeconds` — all provisional `[Tbd("DEC-007")]` data. Portions only drive visuals/scoring later; they never add or remove a step. The step sequence is a code constant, not data. The drink's real menu name is pending (DEC-008).

---

## 7. Tests

From [WF] Phase 4 acceptance, plus transition coverage:

| ID | Type | Case | Expected |
|---|---|---|---|
| TC-DRINK-001 | EditMode | No tea measuring | `TeaBagState` has no measure/pour state; the only way to obtain tea is D1 from the rack; every drink interactable is Hidden/Blocked while `PF_BatterMeasureCup_500ml` is held (GT-003) |
| TC-DRINK-002 | EditMode | Bag starts in red rack | New `DrinkPreparation` is `Stored`; D1 only via `TeaRackController` |
| TC-DRINK-003 | EditMode | Cannot Ready without shake | Place from `IceAdded` → `ready.not_finished` |
| TC-DRINK-004 | EditMode | Cannot Complete without wipe | Place from `Shaken` → `ready.not_finished` |
| TC-DRINK-005 | EditMode | Correct order marked Ready | With two drink tickets, the bag claims the oldest; placing marks exactly that `OrderItem` Ready |
| TC-DRINK-006 | PlayMode | Wrong delivery detected | Covered by TC-ORDER-004 with a drink-only order |
| TC-DRINK-007 | EditMode | Every out-of-order action blocked | Matrix of (state × action) → only the table's transitions succeed |
| TC-DRINK-008 | EditMode | No ticket → cannot take bag | `stall.no_ticket.drink` |
| GT-007 | EditMode | Exact sequence | The only successful path through the state machine is the GT-007 order |
| TC-DRINK-009 | PlayMode | Full drink at the real station prefab | Rack → … → Ready counter with real interactables and anchors |
