# TRAM CHANH — CAKE WORKFLOW

**Status:** Approved for the slice (REV-000, 2026-10-06). Corrected 2026-10-06: the 500 ml measuring cup is the interactive batter tool (§2.2). DEC-007, DEC-008, DEC-013 and DEC-018 remain open: their values/forms are provisional data, not ground truth.
**Module:** `TramChanh.Cakes`
**Ground truth:** GT-008

---

## 1. Real-world workflow (locked)

From [WF] §5.3 and §32 GT-008:

```text
1. Receive cake ticket
2. Measure the correct amount of batter
3. Pour the batter
4. Wait for the cake to cook
5. Flip the cake
6. Cut the cake
7. Apply sauce
8. Roll the cake VERTICALLY
9. Put it in the wrapping paper
10. Place at Ready / hand-off area
```

Grill operation sequence from [AP] §27 (this is how steps 3–5 are performed on the hinged grill; it adds no preparation step):

```text
Open → Pour → Close → Cook → Open → Remove/Flip
```

---

## 2. Cake state machine

States from [WF] §12, plus one failure state:

```text
Waiting → BatterMeasured → BatterPoured → Cooking → Cooked → Flipped
        → Cut → Sauced → Rolled → Wrapped → Ready → Delivered

Cooking/Cooked ──(doneness ≥ burn threshold)──► Ruined   (failure, not a step)
```

`Ruined` is added because [WF] §12 requires that the cake can burn ("có khả năng cháy nếu để quá lâu") but the state list has no state for it. It is a terminal failure state handled by Discard (DEC-006), not a preparation step. Review item for `REV-004`.

### 2.1 Transition table (strict mode — slice)

| # | From | To | Action (interactable) | Guard | Blocked reason key |
|---|---|---|---|---|---|
| C1 | `Waiting` | `BatterMeasured` | Measure: fill `PF_BatterMeasureCup_500ml` at `BatterSource` (Continuous hold); on release the measured ml are recorded and evaluated against the bound recipe (§2.2) | `MeasuredMl > 0`; `HasPending(Cake)` → claims item (and so the recipe/size) | `stall.no_ticket.cake`, `cake.cup_not_held` |
| C2 | `BatterMeasured` | `BatterPoured` | Pour (`Grill`) holding the measured cup; the poured ml = `MeasuredMl` | Batter was measured (an unmeasured/empty cup can never be poured); grill `Open`, lower plate empty; if policy = `BlockPour`, measurement within tolerance | `cake.batter_not_measured`, `grill.preheating`, `grill.lid_closed` (prompt offers *Open lid* instead), `grill.occupied`, `cake.batter_out_of_tolerance` (`BlockPour` only) |
| C3 | `BatterPoured` | `Cooking` | Close lid (`Grill`) | Grill `Open` with this cake | — |
| C4 | `Cooking` | `Cooked` | automatic: doneness ≥ `CookedThreshold` | — | — |
| C5 | `Cooked` | `Flipped` | Flip (performed through `IFlipAction`, §2.3) | Grill `Open`; doneness < `BurnThreshold`; `IFlipAction.CanFlip` | `cake.not_cooked` (when attempted in `Cooking`), `grill.lid_closed` |
| C6 | `Flipped` | `Cut` | Cut (`RollArea`, Hold, scissors animation) | Cake on `RollArea` | `cake.need_flip_first` |
| C7 | `Cut` | `Sauced` | Sauce (`SauceBag_<Flavour>`, Hold) | Sauce == the cake recipe's configured `Sauce` (data, DEC-008) | `cake.need_cut_first`, `cake.wrong_sauce`, `cake.sauce_unconfigured` |
| C8 | `Sauced` | `Rolled` | Roll vertically (`RollArea`, Hold) | — | `cake.need_sauce_first` |
| C9 | `Rolled` | `Wrapped` | Wrap (`WrappingArea`) → wrapped cake into hands | Hands empty | `cake.need_roll_first`, `hands.full` |
| C10 | `Wrapped` | `Ready` | Place at `PF_ReadyCounterPoint/CakePlacement` | Bound; slot free | `ready.not_finished`, `ready.no_order`, `ready.slot_full` |
| C11 | `Ready` | `Delivered` | (order delivered) | `OrderStatusChanged → Delivered` | — |
| C12 | `Cooking`/`Cooked` | `Ruined` | automatic: doneness ≥ `BurnThreshold` | — | — |
| C13 | `Ruined` | (destroyed) | Remove (`Grill`, spatula) → into hands → Discard | Grill `Open` | `grill.lid_closed` |

`IsFinished` (for `IPreparedItem`) = state `Wrapped`. A cake without sauce cannot be rolled, and an unwrapped cake cannot be placed at Ready ([WF] Phase 5 "Missing sauce", "Missing wrap").

### 2.2 Batter measurement — `PF_BatterMeasureCup_500ml`

The real shop measures batter with a **500 ml measuring cup** — canonical `PF_BatterMeasureCup_500ml` / `SM_BatterMeasureCup_500ml`. It is an **interactive gameplay tool** (held item) belonging to cake preparation, not a prop. Different recipes and sizes may require different target batter amounts, so *how much* to measure always comes from the recipe of the order item being prepared: each `CakeRecipe` (menu item × size) carries its own target batter amount, `TargetBatterMl`, and tolerance, `BatterToleranceMl`. All real quantities stay TBD until supplied by the Product Owner.

**Data (no global amount, no code constants):**

| Where | Field | Status |
|---|---|---|
| `CakeRecipe` (per menu item × size) | `TargetBatterMl` | `[Tbd("DEC-007")]` — measured at the real shop |
| `CakeRecipe` | `BatterToleranceMl` | `[Tbd("DEC-007")]` |
| `SO_MeasureCup_500ml` (`MeasureCupDefinition`) | `CapacityMl` = 500 | nominal capacity of the real cup (equipment data, not a recipe value) |
| `SO_MeasureCup_500ml` | `FillRateMlPerSecond` | `[Tbd("DEC-007")]` |
| `SO_MeasureCup_500ml` | `LevelCurve` (ml → liquid height) | `[Tbd]` until the final cup mesh exists; linear placeholder |
| `SO_Balance_Slice` | `BatterOutOfTolerancePolicy`, batter-deviation quality weight | `[Tbd]`; policy default `AllowWithPenalty` (DEC-019) |

**Interaction (all inside workflow step 2 "measure batter"; nothing here is a new step):**

1. Pick up `PF_BatterMeasureCup_500ml` from its spot at `BatterArea` (Press).
2. Hold *Fill* at `BatterSource` (Continuous): the cup gains `FillRateMlPerSecond × heldSeconds` ml, capped at `CapacityMl`. The visible level follows `LevelCurve`, read against the cup's graduation markings — the player judges the amount from the cup, as in the real shop.
3. On release the domain records a `BatterMeasurement` and the cake enters `BatterMeasured` (C1). Corrections stay possible until pouring: hold *Fill* again to top up, or `UseHeld` → *Empty back* (returns batter to the source, measured ml → 0, cake back to `Waiting`).
4. Pour at the open grill (C2). **Pouring is impossible without a measurement**; the poured quantity is exactly the measured ml.
5. After pouring the cup returns automatically to its spot at `BatterArea`.

**Detection of a wrong quantity:**

```csharp
public readonly struct BatterMeasurement
{
    public readonly float MeasuredMl;
    public readonly float TargetMl;         // from the bound CakeRecipe
    public readonly float ToleranceMl;      // from the bound CakeRecipe
    public float DeviationMl => MeasuredMl - TargetMl;
    public BatterMeasureResult Result { get; } // WithinTolerance | Under | Over
}
```

- Evaluated on every measure/top-up/empty; published as `BatterMeasured { PreparationId, BatterMeasurement }` for UI feedback, QA logs and later scoring.
- Stored on `CakePreparation` and carried to the order item, so quality/scoring can use it (§6) and later systems (economy, feedback, daily report) can read it.
- `BatterOutOfTolerancePolicy` decides what a wrong amount means at pour time: `AllowWithPenalty` (slice default: pour allowed, quality reduced) or `BlockPour` (pour blocked with `cake.batter_out_of_tolerance` until corrected).
- Changing a recipe's numbers, adding a size, or switching the policy never requires a code change.

**GT-003 guard:** the cup measures **batter only**. No drink interactable accepts it, and no tea-measuring state exists.

### 2.3 *Flip* step — name fixed, physical action open (DEC-013)

The workflow step is **Flip** (state `Flipped`), exactly as in the source workflow. What the worker physically does at this step on the real contact grill is **not yet confirmed**, so it is not defined here. The architecture isolates it:

```csharp
public interface IFlipAction                     // TramChanh.Cakes
{
    Availability CanFlip(GrillModel grill, CakePreparation cake);
    FlipOutcome Perform(GrillModel grill, CakePreparation cake);   // where the cake ends up, which visuals/anim to play
}
```

- The cake/grill state machines only know "Flip happened" (`C5`). They never encode *how*.
- Where the cake is afterwards (still on the grill, on the roll area, ...), whether more cooking follows, which tool animation plays — all come from the `IFlipAction` implementation and its data, selected in the cake station prefab.
- The slice ships `PlaceholderFlipAction`: spatula animation, then the cake is moved to the `RollArea` so cut/sauce/roll can proceed. It is marked provisional and can be replaced by the confirmed real action without changing `CakeState`, the transition table, tests of the step order, or any other station.
- If the confirmed real action needs extra cooking after the flip, that is recipe data consumed by the `IFlipAction` implementation, not a new workflow step.

### 2.4 Roll direction

"Roll vertically" (GT-008) is a hard requirement on the **roll animation and `SM_Cake_Rolled` shape** ("long vertically rolled grilled cake", [AP] §36). The exact roll axis relative to the grill marks is taken from the reference photos and checked at asset review (`ART-CAKE-006`); code only plays `AN_Cake_RollVertical`.

---

## 3. Grill state machine

States from [WF] §12 (`GrillState`), unchanged:

```text
Off → Preheating → Ready ⇄ Open ⇄ Cooking → Finished → Overcooked
                                    ▲          │            │
                                    └── Open ◄─┴────────────┘
```

| # | From | To | Trigger | Notes |
|---|---|---|---|---|
| G1 | `Off` | `Preheating` | Shift start (DEC-004) | No player action |
| G2 | `Preheating` | `Ready` | `PreheatSeconds` elapsed | Display counts temperature up to the target (reference shows ~160 °C, [AP] §27) |
| G3 | `Ready` | `Open` | Open lid (Press) | Lid animation about the hinge pivot |
| G4 | `Open` | `Ready` | Close lid, plate empty | |
| G5 | `Open` | `Cooking` | Close lid, cake on plate and not `Ruined` | Same action as cake C3 (first close) |
| G6 | `Cooking` | `Finished` | Cake doneness ≥ `CookedThreshold` | Display shows elapsed time |
| G7 | `Finished` | `Overcooked` | Cake doneness ≥ `BurnThreshold` | Cake → `Ruined` (C12) |
| G8 | `Cooking`/`Finished`/`Overcooked` | `Open` | Open lid | Cooking continues at `OpenLidHeatFactor` (TBD) while a cake is on the lower plate |

`Ready` here is the grill's *heated and closed* state and is unrelated to order `Ready`.

While the grill is `Preheating` every grill interaction is Blocked with `grill.preheating`. In `Open` with an undercooked cake the available action is *Close lid*; attempting flip through the domain returns `cake.not_cooked`.

### 3.1 Single grill action per state (for `IInteractable.Query`)

| Grill state | Cake on plate | Holding | Offered action |
|---|---|---|---|
| `Ready` | — | anything | Open lid |
| `Open` | none | measured cup | Pour (C2) |
| `Open` | none | nothing / other | Close lid (G4) |
| `Open` | `BatterPoured` / `Cooking` | nothing | Close lid (C3 / G5) |
| `Open` | `Cooked` | nothing | Flip (C5, via `IFlipAction`) |
| `Open` | `Ruined` | nothing | Remove burnt cake (C13) |
| `Cooking`/`Finished`/`Overcooked` | yes | nothing | Open lid (G8) |

---

## 4. Cooking model

Inputs listed in [WF] Phase 5: batter amount, cook duration, heat, doneness, burn threshold, sauce type, wrapping.

```text
doneness += dt × heatFactor
heatFactor = 1.0                when grill closed and heated (Cooking/Finished/Overcooked)
           = OpenLidHeatFactor  when grill Open with the cake on the lower plate
           = 0                  once the cake has left the grill (decided by IFlipAction — DEC-013)

doneness < CookedThreshold                      → undercooked (state Cooking)
CookedThreshold ≤ doneness < BurnThreshold      → Cooked
doneness ≥ BurnThreshold                        → Ruined
```

- Heat is fixed at the grill's target temperature in the slice (no temperature control action exists in the workflow).
- `dt` comes from `IGameClock`; paused game = no cooking.
- All thresholds live in the cake recipe ScriptableObject and are provisional `[Tbd("DEC-007")]` data. Gameplay code contains no cooking, burn or batter numbers.

Visuals: `SM_Cake_Raw` with a material parameter `Doneness01` (pale → golden); swap to `SM_Cake_Cooked` at `Cooked`; burnt tint as the value approaches `BurnThreshold`. Audio hook `SFX_Grill_Sizzle` on while cooking ([WF] §12).

---

## 5. Domain classes (sketch)

```csharp
public sealed class GrillModel
{
    public GrillState State { get; }
    public CakePreparation CakeOnPlate { get; }
    public void PowerOn(double now);           // G1 (shift start)
    public void Advance(double now);           // preheat + doneness accumulation, G2/G6/G7, C4/C12
    public Result OpenLid();                   // G3/G8
    public Result CloseLid();                  // G4/G5 (+C3)
    public Result Pour(CakePreparation cake);  // C2
    public Result<CakePreparation> Flip(IFlipAction action); // C5 — physical form delegated (DEC-013)
    public Result<CakePreparation> RemoveRuined(); // C13
}

public sealed class CakePreparation
{
    public CakeState State { get; }
    public CakeRecipe Recipe { get; }             // bound order item's recipe (menu item × size)
    public BatterMeasurement Batter { get; }      // §2.2; default = not measured
    public float Doneness { get; }
    public SauceDefinition RequiredSauce { get; } // from recipe data; may be unconfigured (DEC-008)
    public OrderItemRef BoundItem { get; }
    public int Quality { get; }
    public Result Measure(float measuredMl);   // C1 (and top-up); evaluates against Recipe
    public Result EmptyBatter();               // correction: back to Waiting
    public bool CanPour(BatterOutOfTolerancePolicy policy); // C2 guard
    public Result Cut();                       // C6
    public Result ApplySauce(SauceDefinition s); // C7
    public Result Roll();                      // C8
    public Result Wrap();                      // C9
    public Result MarkReady();                 // C10
    public Result MarkDelivered();             // C11
}
```

Both are plain C#; `GrillController` (MonoBehaviour) calls `Advance(clock.Now)` each frame and maps state to the lid animator and display.

---

## 6. Quality (slice)

```text
batterScore   = 100 − w × min(|MeasuredMl − TargetBatterMl| / BatterToleranceMl, k)   (w, k = [Tbd] weights)
donenessScore = 100 − 50 × |doneness − mid| / ((Burn − Cooked) / 2)       (50…100)
                mid = (CookedThreshold + BurnThreshold) / 2
Quality       = round((batterScore + donenessScore) / 2)
```

All weights (`w`, `k`, 50/100) are provisional data in `SO_Balance_Slice`, not constants; the formula's inputs are fixed by [WF] Phase 5. A wrong batter quantity always lowers `batterScore`; with `AllowWithPenalty` that is how it reaches the order's quality/scoring.

---

## 7. Station layout and adapters

Station hierarchy from [AP] §60:

```text
PF_CakeStation
├── Grill          → GrillController                (PF_Grill_Elmich)
├── BatterArea     → BatterSourceInteractable        (placeholder source, DEC-009)
├── MeasuringCup   → BatterMeasureCup (held)          (PF_BatterMeasureCup_500ml + SM_BatterVolume)
├── Spatula        → station tool used by the active IFlipAction (PF_Spatula_WoodHandle)
├── Scissors       → station tool, animated by RollArea (PF_Scissors_RedGray)
├── SauceArea      → SauceBagInteractable per configured SauceDefinition (PF_SauceBag_<Flavour>, DEC-008)
├── RollArea       → RollAreaInteractable (cut → roll), cake placement point
├── WrappingArea   → WrapInteraction                  (PF_CakeWrappingPaper)
└── ReadyPoint     → shared PF_ReadyCounterPoint
```

| Adapter (Codex) | Responsibility |
|---|---|
| `BatterMeasureCup` | On `PF_BatterMeasureCup_500ml`: `IHoldable` + `IHeldItemAction` (empty back); reads `SO_MeasureCup_500ml`; shows the level via `SM_BatterVolume` and `LevelCurve` |
| `BatterSourceInteractable` | Continuous fill of the held cup; on release calls `CakePreparation.Measure(measuredMl)` (creating + binding the preparation on first measure) |
| `GrillController` | Owns `GrillModel`; lid `Animator` on `LidPivot`; display; cake visual spawn at `CakePlacementPoint`; holds the station's `IFlipAction` reference |
| `PlaceholderFlipAction` | Provisional `IFlipAction` (DEC-013): spatula anim, cake to `RollArea`. Replaceable without state-machine changes |
| `CakeView` | Swaps `PF_Cake_Raw` / `PF_Cake_Cooked` / `SM_Cake_Cut` / `PF_Cake_Rolled` / `PF_Cake_Wrapped` by state; doneness param |
| `RollAreaInteractable` | Cut (scissors anim) and roll (`AN_Cake_RollVertical`) |
| `SauceBagInteractable` | Sauce application anim; flavour from its config |
| `WrapInteraction` | Wrap anim; puts the wrapped cake (an `IPreparedItem`) in hands |

---

## 8. Tests

From [WF] Phase 5:

| ID | Type | Case | Expected |
|---|---|---|---|
| TC-CAKE-001 | EditMode | Wrong batter amount | Measure under/over a fake recipe's tolerance → `BatterMeasurement.Result` = Under/Over, event published; `AllowWithPenalty`: pour allowed and `batterScore` < within-tolerance score; `BlockPour`: pour blocked `cake.batter_out_of_tolerance`; empty-back → `Waiting`, 0 ml |
| TC-CAKE-014 | EditMode | Measure before pour | Pour with an unmeasured or emptied cup → `cake.batter_not_measured` |
| TC-CAKE-015 | EditMode | Recipe-driven quantity | Two recipes (or two sizes) with different `TargetBatterMl`: the same measured ml is within tolerance for one and out for the other |
| TC-CAKE-016 | EditMode | Cup capacity | Filling past `CapacityMl` caps at capacity |
| TC-CAKE-017 | EditMode | Batter cup never measures tea (GT-003) | No drink interactable offers an action while the cup is held |
| TC-CAKE-002 | EditMode | Undercooked | Open lid before `CookedThreshold`; flip → `cake.not_cooked`; close again resumes cooking |
| TC-CAKE-003 | EditMode | Correct cook | Doneness in window → flip succeeds → `Flipped` on `RollArea` |
| TC-CAKE-004 | EditMode | Overcooked | Doneness ≥ burn → `Ruined`, grill `Overcooked`; remove + discard → order item back to `Pending` |
| TC-CAKE-005 | EditMode | Missing sauce | Roll from `Cut` → `cake.need_sauce_first` |
| TC-CAKE-006 | EditMode | Missing wrap | Place `Rolled` cake at Ready → impossible (not held; `Wrapped` required) / `ready.not_finished` |
| TC-CAKE-007 | EditMode | Correct finished product | Full path → `Ready`; quality in range; correct order item marked Ready |
| TC-CAKE-008 | EditMode | Wrong sauce / unconfigured sauce | `cake.wrong_sauce`; recipe without sauce → `cake.sauce_unconfigured` and a content-validation warning |
| TC-CAKE-012 | EditMode | Flip isolation | State machine with a fake `IFlipAction` reaches `Flipped` regardless of the action's physical outcome; swapping implementations needs no state-machine change |
| TC-CAKE-013 | EditMode | No guessed numbers | Cooking/burn/batter values (incl. `TargetBatterMl`, `BatterToleranceMl`, fill rate) are read only from recipe/cup data (a fake recipe with arbitrary values drives all thresholds) |
| TC-CAKE-009 | EditMode | Grill transitions | G1–G8 valid; preheating blocks everything; pause freezes doneness |
| TC-CAKE-010 | EditMode | No ticket → cannot measure | `stall.no_ticket.cake` |
| GT-008 | EditMode | Exact sequence | The only successful path is the GT-008 order |
| TC-CAKE-011 | PlayMode | Full cake at the real station | 500 ml cup → source (measure) → grill (open/pour/close/open/flip) → cut → sauce → roll → wrap → Ready counter; lid rotates about the hinge without deformation |
