# TRAM CHANH — INTERACTION SYSTEM

**Status:** Approved for the slice (REV-000, 2026-10-06)
**Module:** `TramChanh.Interaction`
**Purpose:** one framework through which every interactive object in the game is used ([WF] §10). No object reads input or raycasts on its own.

---

## 1. Review of the interface proposed in [WF] §10

```csharp
public interface IInteractable
{
    bool CanInteract(PlayerInteractor interactor);
    void Interact(PlayerInteractor interactor);
    string GetPrompt();
}
```

Findings:

| # | Issue | Change |
|---|---|---|
| 1 | `bool CanInteract` cannot explain *why* an action is blocked. Strict-sequence gameplay needs "Open the bag first", "No drink ticket", etc. | Return `Availability` (`Available` / `Blocked(reasonKey)` / `Hidden`). |
| 2 | Depends on concrete `PlayerInteractor` → an NPC Lobby (DEC-001) or a test cannot drive it. | Take an `InteractionContext` (actor, role, held item, clock) instead. |
| 3 | `GetPrompt()` without context cannot vary with what the player holds (e.g. rack shows "Take tea bag" only when hands are empty). | Prompt is part of the availability query, computed from the context. |
| 4 | Only instant press. Shake, wipe and roll are timed actions; measuring batter depends on how long the player keeps filling. | Add `InteractionKind` (`Press`, `Hold` = fixed duration, `Continuous` = runs while held, result on release) and hold-progress callbacks. |
| 5 | Some objects offer more than one action (grill: open/close lid vs pour vs flip). | An interactable returns **one** current action (the one valid for the current state). Multiple simultaneous options are not needed in the slice. |
| 6 | Returns `string` → localization and GC per frame. | Prompt is a `PromptKey` (string id resolved by UI, cached). |
| 7 | Actions on the held item itself (open bag, shake) have no target object. | `IHeldItemAction` on the held item, triggered by a separate input. |

---

## 2. Contracts (sketch — final signatures confirmed in `CX-010`)

```csharp
public enum InteractionKind { Press, Hold, Continuous }

public readonly struct InteractionQuery
{
    public readonly Availability Availability;  // Available | Blocked | Hidden
    public readonly string PromptKey;           // e.g. "drink.rack.take_bag"
    public readonly string BlockedReasonKey;    // e.g. "drink.need_open_first"
    public readonly InteractionKind Kind;
    public readonly float HoldDuration;         // seconds, Hold only (from SO data)
}

public sealed class InteractionContext
{
    public ActorRef Actor { get; }              // player now; NPC later
    public ActorRole Role { get; }              // Lobby | Stall (DEC-001: player has both)
    public IHeldItemSlot Hands { get; }
    public IGameClock Clock { get; }
}

public interface IInteractable
{
    InteractableId Id { get; }
    Transform InteractionPoint { get; }         // Anchors/InteractionPoint
    InteractionQuery Query(InteractionContext ctx);
    void Execute(InteractionContext ctx);       // Press: on press; Hold: on completion
    void OnHoldStarted(InteractionContext ctx) {}   // default no-op (animation start)
    void OnHoldCancelled(InteractionContext ctx) {} // default no-op
    void OnContinuousReleased(InteractionContext ctx, float heldSeconds) {} // Continuous only
}

public interface IHoldable
{
    Transform HandGrip { get; }                 // Anchors/HandGrip — pivot = grip point ([AP] §7)
    void OnPickedUp(IHeldItemSlot hands);
    void OnReleased();
}

public interface IHeldItemAction                 // actions on the item in hand
{
    InteractionQuery QueryUse(InteractionContext ctx);
    void ExecuteUse(InteractionContext ctx);
}
```

Rules:

- `Query` is **pure**: no side effects, no allocations (it is called every frame for the focused object).
- `Execute` re-checks availability; if it changed, it does nothing and publishes `ActionBlocked`.
- Domain state changes happen only inside `Execute` / `ExecuteUse`, by calling the module's domain object (e.g. `DrinkPreparation.AddCoconutJelly()`). The interactable never mutates state itself.

---

## 3. Player interactor

`PlayerInteractor` (MonoBehaviour on the player):

1. Every frame: raycast from camera centre, `Interactable` layer only, max distance `BalanceConfig.ReachDistance` (provisional `[Tbd]` data, tuned in `INT-008` against the 0.8 m counter depth; DEC-007).
2. Focus = the `IInteractable` on the hit collider (or its parent via `InteractableRef`).
3. Calls `Query(ctx)` on the focused object → publishes prompt state to UI only when it changed (no per-frame events).
4. Input (Unity Input System, action map `Gameplay`):

| Action | Default binding | Effect |
|---|---|---|
| `Interact` | `E` / left mouse | Press → `Execute`; Hold → starts hold driver |
| `UseHeld` | `F` / right mouse | `IHeldItemAction` on the held item (Press or Hold) |
| `Discard` | `Q` (hold 0.5 s) | Discard a held ruined product (DEC-006) — only offered when the held item allows it |
| `Move` / `Look` | WASD / mouse | First-person (DEC-002) |

5. Hold driver: progress `0→1` over `HoldDuration` using `IGameClock`; cancelled if focus changes, the button is released, or availability becomes Blocked. On completion → `Execute`.
6. Continuous driver: accumulates held time with `IGameClock` while the button is down; on release (or focus loss) → `OnContinuousReleased(ctx, heldSeconds)`. Used only by the batter source.

Only one interaction runs at a time; while a hold is in progress, focus is locked.

---

## 4. Held item slot

- One slot ("hands"). The slot holds at most one `IHoldable`.
- Held object is re-parented to the camera's `HandSocket`, aligned by its `HandGrip` anchor, on the `HeldItem` layer (no collision with player).
- Held in the slice: `TeaBag` (drink in preparation), `BatterMeasureCup`, wrapped cake, `ServedOrder` bundle (Lobby carrying a Ready order).
- **Not held** (DEC-005): ice scoop, spatula, scissors, sauce bag, wipe cloth. These are *station tools*: the station interaction plays the tool's animation at its anchor and returns it.
- Dropping arbitrary items on the floor is not supported. Items leave the hands only through a valid interaction (place, pour, deliver, discard).

---

## 5. Interactable catalogue (slice)

Every row maps to exactly one canonical workflow step or Lobby action; nothing here is an extra preparation step.

| Interactable | Prefab | Kind | Holding | Canonical step |
|---|---|---|---|---|
| `TeaRackInteractable` | `PF_RedTeaRack` | Press | nothing | Drink: take pre-portioned tea bag |
| `TeaBagItem` (held action) | `PF_TeaBag_PrePortioned` | Press (UseHeld) | the bag | Drink: open |
| `ToppingBinInteractable` (coconut) | `PF_ToppingStation/CoconutJellyBin` | Press | opened bag | Drink: coconut jelly |
| `ToppingBinInteractable` (lemon) | `PF_ToppingStation/LemonJellyBin` | Press | bag with coconut jelly | Drink: lemon jelly |
| `IceBinInteractable` (+ scoop anim) | `PF_IceBin` + `PF_IceScoop` | Press | bag with both jellies | Drink: ice |
| `TeaBagItem` (held action) | `PF_TeaBag_PrePortioned` | Hold (UseHeld) | bag with ice | Drink: shake |
| `WipeAreaInteractable` | `PF_DrinkStation/WipeArea` (+ `PF_WipeCloth`) | Hold | shaken bag | Drink: wipe |
| `ReadyCounterPoint` | `PF_ReadyCounterPoint` | Press | finished drink / cake | Drink & cake: ready; Lobby: pick up |
| `BatterMeasureCupInteractable` | `PF_BatterMeasureCup` | Press | nothing | Cake: measure (pick up cup) |
| `BatterSourceInteractable` | `BatterSource` placeholder (DEC-009) | Continuous | the cup | Cake: measure (fill) |
| `GrillInteractable` | `PF_Grill_Elmich` | Press | varies | Cake: pour / cook (lid close/open) / flip (via `IFlipAction`, DEC-013) — see `CAKE_WORKFLOW.md` |
| `RollAreaInteractable` (+ scissors anim) | `PF_CakeStation/RollArea` + `PF_Scissors_RedGray` | Hold | nothing | Cake: cut |
| `SauceBagInteractable` | `PF_SauceBag_<Flavour>` | Hold | nothing | Cake: sauce |
| `RollAreaInteractable` | `PF_CakeStation/RollArea` | Hold | nothing | Cake: roll vertically |
| `WrappingAreaInteractable` | `PF_CakeStation/WrappingArea` + `PF_CakeWrappingPaper` | Press | nothing | Cake: wrap (cake goes into hands) |
| `TableOrderPoint` | `PF_YellowCrateTable` | Press | nothing / `ServedOrder` | Lobby: take order / deliver |
| `VehicleOrderPoint` | `PF_Placeholder_VehiclePoint` (generic) | Press | nothing / `ServedOrder` | Lobby: take order / deliver |

`RollArea` is one interactable whose current action follows the cake state (Cut → Roll), with the scissors animated only for Cut.

Non-interactive in the slice (static props only): `PF_MeasuringCup_500ml` (must never imply tea measuring — GT-003), `PF_PumpBottle`, menus, snack rack, cooler.

---

## 6. Prompt UI

- `InteractionPromptView` (UI module) subscribes to the interactor's prompt state.
- Shows: prompt text, key glyph, hold progress ring, blocked reason in a muted style.
- Hidden when nothing is focused or availability is `Hidden`.

---

## 7. Anchors used by the interaction system

From [AP] §58: `Anchors/HandGrip`, `Anchors/InteractionPoint`, `Anchors/PlacementPoint`, `Anchors/PourPoint`, `Anchors/OutputPoint`. Not every prefab has all five. Requirements per prefab are in `ASSET_INTEGRATION.md` §5.

---

## 8. Tests

| ID | Type | Case |
|---|---|---|
| TC-INT-001 | EditMode | `Query` on a test interactable returns Available/Blocked/Hidden according to a fake context |
| TC-INT-002 | EditMode | `Execute` re-checks availability and does nothing when Blocked; `ActionBlocked` published |
| TC-INT-003 | EditMode | Hold driver: completes after `HoldDuration` on `ManualClock`; cancels on release / focus change / became Blocked |
| TC-INT-008 | EditMode | Continuous driver reports the exact held time on release and on focus loss |
| TC-INT-004 | EditMode | Held slot: pick up, cannot pick up second item, release |
| TC-INT-005 | PlayMode | `SCN_InteractionTest`: player looks at test cube within reach → prompt; beyond reach → none; press → cube reacts |
| TC-INT-006 | PlayMode | Paused game: hold progress does not advance |
| TC-INT-007 | EditMode | `Query` allocates 0 bytes (GC alloc test) |
