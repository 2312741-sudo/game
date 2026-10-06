# TRAM CHANH — CAKE WORKFLOW

**Status:** Draft for review gate `REV-004`
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
| C1 | `Waiting` | `BatterMeasured` | Fill cup (`BatterSource`, Continuous hold) while holding `PF_BatterMeasureCup`; on release the amount is evaluated | `\|amount − target\| ≤ tolerance`; `HasPending(Cake)` → claims item | `stall.no_ticket.cake`; if out of tolerance the state stays `Waiting` and the cup shows the wrong level |
| C2 | `BatterMeasured` | `BatterPoured` | Pour (`Grill`) holding the measured cup | Grill `Open`, lower plate empty | `grill.preheating`, `grill.lid_closed` (prompt offers *Open lid* instead), `grill.occupied`, `cake.batter_wrong_amount` |
| C3 | `BatterPoured` | `Cooking` | Close lid (`Grill`) | Grill `Open` with this cake | — |
| C4 | `Cooking` | `Cooked` | automatic: doneness ≥ `CookedThreshold` | — | — |
| C5 | `Cooked` | `Flipped` | Flip (`Grill`, spatula animation) | Grill `Open`; doneness < `BurnThreshold` | `cake.not_cooked` (when attempted in `Cooking`), `grill.lid_closed` |
| C6 | `Flipped` | `Cut` | Cut (`RollArea`, Hold, scissors animation) | Cake on `RollArea` | `cake.need_flip_first` |
| C7 | `Cut` | `Sauced` | Sauce (`SauceBag_<Flavour>`, Hold) | Flavour == order item's `SauceType` | `cake.need_cut_first`, `cake.wrong_sauce` |
| C8 | `Sauced` | `Rolled` | Roll vertically (`RollArea`, Hold) | — | `cake.need_sauce_first` |
| C9 | `Rolled` | `Wrapped` | Wrap (`WrappingArea`) → wrapped cake into hands | Hands empty | `cake.need_roll_first`, `hands.full` |
| C10 | `Wrapped` | `Ready` | Place at `PF_ReadyCounterPoint/CakePlacement` | Bound; slot free | `ready.not_finished`, `ready.no_order`, `ready.slot_full` |
| C11 | `Ready` | `Delivered` | (order delivered) | `OrderStatusChanged → Delivered` | — |
| C12 | `Cooking`/`Cooked` | `Ruined` | automatic: doneness ≥ `BurnThreshold` | — | — |
| C13 | `Ruined` | (destroyed) | Remove (`Grill`, spatula) → into hands → Discard | Grill `Open` | `grill.lid_closed` |

`IsFinished` (for `IPreparedItem`) = state `Wrapped`. A cake without sauce cannot be rolled, and an unwrapped cake cannot be placed at Ready ([WF] Phase 5 "Missing sauce", "Missing wrap").

### 2.2 Batter measurement

- The player picks up `PF_BatterMeasureCup` from its spot at `BatterArea` (part of step 2, not a new step).
- `BatterSource` is a **Continuous** hold: the cup fills at `FillRate` while the button is held; the amount is evaluated on release.
- Under target: the player may hold again to add more. Over tolerance: `UseHeld` on the cup returns the batter to the source (amount → 0). These are corrections within step 2, not additional steps.
- After pouring (C2) the cup returns automatically to its spot at `BatterArea`, so hands are free for the grill.
- Values `TargetAmount`, `Tolerance`, `FillRate` are TBD (DEC-007).

### 2.3 "Flip" semantics (DEC-013)

On the contact grill both plates cook at once ([AP] §27, ridged upper and lower plates). *Flip* is therefore the spatula turning the cooked sheet out of the open grill onto the `RollArea`; it does not start a second cooking phase. Grill-mark lines from both plates are visible on `SM_Cake_Cooked`.

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
| `Open` | `Cooked` | nothing | Flip (C5) |
| `Open` | `Ruined` | nothing | Remove burnt cake (C13) |
| `Cooking`/`Finished`/`Overcooked` | yes | nothing | Open lid (G8) |

---

## 4. Cooking model

Inputs listed in [WF] Phase 5: batter amount, cook duration, heat, doneness, burn threshold, sauce type, wrapping.

```text
doneness += dt × heatFactor
heatFactor = 1.0                when grill closed and heated (Cooking/Finished/Overcooked)
           = OpenLidHeatFactor  when grill Open with the cake on the lower plate
           = 0                  once the cake has been flipped off the grill

doneness < CookedThreshold                      → undercooked (state Cooking)
CookedThreshold ≤ doneness < BurnThreshold      → Cooked
doneness ≥ BurnThreshold                        → Ruined
```

- Heat is fixed at the grill's target temperature in the slice (no temperature control action exists in the workflow).
- `dt` comes from `IGameClock`; paused game = no cooking.
- All thresholds live in `SO_Recipe_Cake_Slice` and are TBD (DEC-007).

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
    public Result<CakePreparation> Flip();     // C5 → cake leaves the grill
    public Result<CakePreparation> RemoveRuined(); // C13
}

public sealed class CakePreparation
{
    public CakeState State { get; }
    public float BatterAmount { get; }
    public float Doneness { get; }
    public SauceType RequiredSauce { get; }
    public OrderItemRef BoundItem { get; }
    public int Quality { get; }
    public Result Measure(float amount);       // C1
    public Result Cut();                       // C6
    public Result ApplySauce(SauceType s);     // C7
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
batterScore   = 100 − 50 × |amount − target| / tolerance                  (50…100)
donenessScore = 100 − 50 × |doneness − mid| / ((Burn − Cooked) / 2)       (50…100)
                mid = (CookedThreshold + BurnThreshold) / 2
Quality       = round((batterScore + donenessScore) / 2)
```

Coefficients are placeholders (DEC-007); the formula's inputs are fixed by [WF] Phase 5.

---

## 7. Station layout and adapters

Station hierarchy from [AP] §60:

```text
PF_CakeStation
├── Grill          → GrillController                (PF_Grill_Elmich)
├── BatterArea     → BatterSourceInteractable        (placeholder source, DEC-009)
├── MeasuringCup   → BatterMeasureCup (held)          (PF_BatterMeasureCup + SM_BatterVolume)
├── Spatula        → station tool, animated by Grill  (PF_Spatula_WoodHandle)
├── Scissors       → station tool, animated by RollArea (PF_Scissors_RedGray)
├── SauceArea      → SauceBagInteractable ×1 in slice (PF_SauceBag_<Flavour>, DEC-008)
├── RollArea       → RollAreaInteractable (cut → roll), cake placement point
├── WrappingArea   → WrapInteraction                  (PF_CakeWrappingPaper)
└── ReadyPoint     → shared PF_ReadyCounterPoint
```

| Adapter (Codex) | Responsibility |
|---|---|
| `BatterMeasureCup` | `IHoldable` + `IHeldItemAction` (empty back); shows fill level via `SM_BatterVolume` scale/morph |
| `BatterSourceInteractable` | Continuous fill; on release calls `CakePreparation.Measure` (creating + binding the preparation) |
| `GrillController` | Owns `GrillModel`; lid `Animator` on `LidPivot`; display; cake visual spawn at `CakePlacementPoint` |
| `CakeView` | Swaps `PF_Cake_Raw` / `PF_Cake_Cooked` / `SM_Cake_Cut` / `PF_Cake_Rolled` / `PF_Cake_Wrapped` by state; doneness param |
| `RollAreaInteractable` | Cut (scissors anim) and roll (`AN_Cake_RollVertical`) |
| `SauceBagInteractable` | Sauce application anim; flavour from its config |
| `WrapInteraction` | Wrap anim; puts the wrapped cake (an `IPreparedItem`) in hands |

---

## 8. Tests

From [WF] Phase 5:

| ID | Type | Case | Expected |
|---|---|---|---|
| TC-CAKE-001 | EditMode | Wrong batter amount | Release outside tolerance → stays `Waiting`; pour blocked `cake.batter_wrong_amount`; empty-back resets to 0 |
| TC-CAKE-002 | EditMode | Undercooked | Open lid before `CookedThreshold`; flip → `cake.not_cooked`; close again resumes cooking |
| TC-CAKE-003 | EditMode | Correct cook | Doneness in window → flip succeeds → `Flipped` on `RollArea` |
| TC-CAKE-004 | EditMode | Overcooked | Doneness ≥ burn → `Ruined`, grill `Overcooked`; remove + discard → order item back to `Pending` |
| TC-CAKE-005 | EditMode | Missing sauce | Roll from `Cut` → `cake.need_sauce_first` |
| TC-CAKE-006 | EditMode | Missing wrap | Place `Rolled` cake at Ready → impossible (not held; `Wrapped` required) / `ready.not_finished` |
| TC-CAKE-007 | EditMode | Correct finished product | Full path → `Ready`; quality in range; correct order item marked Ready |
| TC-CAKE-008 | EditMode | Wrong sauce flavour | `cake.wrong_sauce` |
| TC-CAKE-009 | EditMode | Grill transitions | G1–G8 valid; preheating blocks everything; pause freezes doneness |
| TC-CAKE-010 | EditMode | No ticket → cannot measure | `stall.no_ticket.cake` |
| GT-008 | EditMode | Exact sequence | The only successful path is the GT-008 order |
| TC-CAKE-011 | PlayMode | Full cake at the real station | Cup → source → grill (open/pour/close/open/flip) → cut → sauce → roll → wrap → Ready counter; lid rotates about the hinge without deformation |
