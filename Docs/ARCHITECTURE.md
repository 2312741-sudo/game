# TRAM CHANH — ARCHITECTURE

**Status:** Approved by Product Owner 2026-10-06 (REV-000). Decision defaults accepted with the amendments in §10.
**Owner:** Claude Code (Technical Lead / Architect)
**Engine:** Unity 6 (URP) · **Scale:** `1 Unity unit = 1 meter`
**Scope of this document:** the first playable vertical slice (M0–M5) and the module boundaries that later milestones build on.

Source documents (the only inputs; copied verbatim into `Docs/Reference/`):

- `Docs/Reference/TramChanh_AI_GameDev_Workflow.md` — called **[WF]** below
- `Docs/Reference/TramChanh_3D_Asset_Pipeline.md` — called **[AP]** below

Companion specs:

| Doc | Covers |
|---|---|
| `ORDER_SYSTEM.md` | Order data model, status state machine, Lobby → Stall → Lobby flow |
| `INTERACTION_SYSTEM.md` | Player interaction contract, held items, prompts, hold actions |
| `DRINK_WORKFLOW.md` | Tea-bag drink state machine and station interactions |
| `CAKE_WORKFLOW.md` | Cake + grill state machines and station interactions |
| `ASSET_INTEGRATION.md` | Canonical names, prefab hierarchies, anchors, import & validation |
| `PROJECT_TASK_PLAN.md` | Task breakdown for Codex, 3D production, Unity integration, Antigravity QA |

---

## 1. Ground truth (locked — never changed by design or implementation)

| ID | Fact | Enforced by |
|---|---|---|
| GT-001 | Stall: 1.8 m wide × 0.8 m deep × ~2.2 m tall; counter top at 1.0 m; counter → roof 1.2 m | Prefab bounds validator, `SCN_AssetScaleTest` |
| GT-002 | Only the **NEW** Tram Chanh sign (`PF_Sign_TramChanh_New`). The old illuminated letters never appear. | Asset validator, scene scan test |
| GT-003 | Tea is **already pre-portioned in bags** stored in **red racks**. There is no tea-measuring step. | Drink state machine has no measure state; no drink interactable accepts the batter cup `PF_BatterMeasureCup_500ml` (it measures cake batter only) |
| GT-004 | Every order originates through the **Lobby**. The stall never takes orders from customers. | `OrderService` API has no stall/customer entry path |
| GT-005 | Dine-in: Lobby takes the order **at the table**. | `OrderOrigin.DineIn(tableId)` |
| GT-006 | Takeaway: Lobby takes the order **at the customer's vehicle**. | `OrderOrigin.Vehicle(vehicleId)` |
| GT-007 | Drink: tea bag → open → coconut jelly → lemon jelly → ice → shake → wipe → ready | `DrinkPreparation` strict sequence |
| GT-008 | Cake: measure batter → pour → cook → flip → cut → sauce → roll vertically → wrap → ready | `CakePreparation` strict sequence |

Lobby **enters** the order and **sends** it to the stall; the stall prepares; the stall places finished items at the Ready counter; the Lobby picks them up and delivers to the correct table / vehicle.

Any implementation that adds, removes or reorders a preparation step fails review (see [WF] §31 review gate).

---

## 2. Vertical slice scope

From [WF] §27 and §36 (`TC-ARCH-001`):

```text
1 stall (with new sign)       1 tea type (one drink recipe)
1 Lobby (player — see DEC-001) 1 cake type (one cake recipe)
1 customer (at a time)         1 red tea rack
1 table (stool + crate table)  1 topping station (coconut + lemon jelly)
1 vehicle takeaway point       1 ice bin (+ scoop)
1 Ready counter                1 grill (Elmich)
1 order UI
```

End-to-end flow that must run (both for dine-in and takeaway):

```text
Customer → Lobby takes order → Lobby enters → Lobby sends to Stall
→ Stall prepares (drink and/or cake) → Ready counter
→ Lobby picks up → Lobby delivers to correct table / vehicle → Customer → Completed
```

**Out of scope for the slice** ([WF] §29, P1/P2): money/economy, patience, multiple recipes, queues of many customers, save/load, audio/VFX polish, employees, upgrades, weather, day/night, snack rack, pump bottles, menus as interactive objects, multiplayer.

---

## 3. Architectural principles

1. **Domain logic is plain C#.** Order, drink, cake and grill state machines contain no `MonoBehaviour`, no `UnityEngine.Object` references, no `Time.time`. They are fully testable in EditMode.
2. **MonoBehaviours are thin adapters.** They translate input/physics into domain calls and domain events into visuals (animator parameters, mesh toggles, UI).
3. **One interaction framework.** Every interactable object goes through `IInteractable` (see `INTERACTION_SYSTEM.md`). No object reads input directly.
4. **Strict sequence for the prototype.** Out-of-order actions are *blocked* with a reason shown in the prompt ([WF] §11 recommendation). The domain exposes a `SequenceMode` so "free action + quality scoring" can be added later without restructuring; only `Strict` is implemented in the slice.
5. **Explicit composition, no hidden singletons.** A single composition root (`GameBootstrap`) builds services and registers them in a scene-scoped `ServiceRegistry`. Domain classes receive dependencies via constructor.
6. **Time is injected.** All timers (cooking, preheat, hold actions) read `IGameClock`. Tests use `ManualClock`.
7. **Data in ScriptableObjects, state in C# objects.** Recipes, item definitions and balance numbers are `SO_` assets; runtime state is never written back into ScriptableObjects.
8. **Events are typed and synchronous.** Cross-module notification goes through `IEventBus` with `readonly struct` events. Modules do not hold references to each other's MonoBehaviours.
9. **Real-world fidelity beats genre convention** ([WF] §34). When unsure, the item goes to the Decision Register (§10), not into code.
10. **Unknown real-world values are never ground truth.** Only the facts in §1 may become code constants (`TramChanh.Core.GroundTruth`). Everything not yet confirmed from the real shop — station positions, sign size and artwork, batter amount, cooking/burn time, topping quantities, hold durations, sauce-to-cake mapping, vehicle form, the physical form of *Flip* — is **provisional data**: ScriptableObject fields marked `[Tbd("DEC-xxx")]`, prefab/scene transforms (`StallAnchor` with *Position Confirmed* off) or placeholder prefabs (`PlaceholderAsset`). Replacing a provisional value must never require a code change.

---

## 4. Module map (assemblies)

Unity project root = repository root (`Assets/`, `Packages/`, `ProjectSettings/` beside `Docs/`).
All runtime code lives under `Assets/TramChanh/Scripts/<Module>/` ([WF] §7), one assembly definition per module:

| Assembly | Folder | Responsibility | May reference |
|---|---|---|---|
| `TramChanh.Core` | `Scripts/Core` | `IEventBus`, `ServiceRegistry`, `IGameClock`/`UnityGameClock`/`ManualClock`, strongly-typed IDs, `GameState`, `Result`/`Availability` helpers | — |
| `TramChanh.Content` | `Scripts/Core/Content` *(own asmdef)* | ScriptableObject definitions: `ItemDefinition`, `DrinkRecipe`, `CakeRecipe`, `MenuItem`, `BalanceConfig` | Core |
| `TramChanh.Interaction` | `Scripts/Interaction` | `IInteractable`, `InteractionContext`, `PlayerInteractor`, `HeldItemSlot`, `IHoldable`, hold-action driver, prompt model | Core |
| `TramChanh.Orders` | `Scripts/Orders` | `Order`, `OrderItem`, `OrderStateMachine`, `OrderService`, `StallTicketQueue`, `ReadyShelf` (domain), `IPreparedItem` contract | Core, Content |
| `TramChanh.Stall` | `Scripts/Stall` *(new folder; see note)* | `ReadyCounterPoint`, stall anchors, `PlaceholderDiscard` | Core, Interaction, Orders |
| `TramChanh.Drinks` | `Scripts/Drinks` | `DrinkPreparation` (domain) + rack, bag, bins, ice, shake, wipe adapters | Core, Content, Interaction, Orders |
| `TramChanh.Cakes` | `Scripts/Cakes` | `CakePreparation`, `GrillModel` (domain) + batter, grill, spatula, scissors, sauce, roll, wrap adapters | Core, Content, Interaction, Orders |
| `TramChanh.Lobby` | `Scripts/Lobby` | `TableOrderPoint`, `VehicleOrderPoint`, `LobbyOrderController`, delivery targets | Core, Interaction, Orders |
| `TramChanh.Customers` | `Scripts/Customers` | Slice customer controller (scripted) | Core, Orders, Lobby |
| `TramChanh.UI` | `Scripts/UI` | Interaction prompt, order entry UI, stall ticket UI, ready list, debug HUD | Core, Interaction, Orders, Content |
| `TramChanh.App` | `Scripts/Core/Bootstrap` *(own asmdef)* | `GameBootstrap` (composition root), `SceneLoader` | all of the above |
| `TramChanh.DevTools` | `Scripts/Debug` | Dev cheats (spawn customer, skip cook timer), only in Editor/Development builds (define constraint) | all of the above |
| `TramChanh.Editor` | `Scripts/Editor` | Project setup, placeholder builders, prefab/asset validators, scale-test tools (namespace `TramChanh.EditorTools`) | all runtime assemblies |
| `TramChanh.Tests.EditMode` | `Tests/EditMode` | Domain unit tests, GT tests, asset validators | all runtime + Editor |
| `TramChanh.Tests.PlayMode` | `Tests/PlayMode` | Smoke + end-to-end slice tests | all runtime |

Naming note: the debug assembly is `TramChanh.DevTools` and the editor namespace is `TramChanh.EditorTools`, because `TramChanh.Debug` / `TramChanh.Editor` namespaces would hide `UnityEngine.Debug` / `UnityEditor.Editor` inside project code.

Note: `Scripts/Stall` is not listed in [WF] §7. It is added because the Ready counter must depend on `Orders` + `Interaction` but must not depend on `Drinks`/`Cakes` (they depend on it via `IPreparedItem`). Review item for `REV-000`.

Dependency direction (arrows = "depends on"):

```text
                 ┌──────────── App (composition root) ────────────┐
                 ▼                                                 ▼
 Customers ─► Lobby ─┐        Drinks ──┐      Cakes ──┐           UI
                     ▼                 ▼              ▼            │
                   Orders ◄────────────┴──────────────┘◄───────────┤
                     │   ▲                                         │
                     │   └── Stall (ReadyCounterPoint)             │
                     ▼                                             ▼
                  Content ─────────► Core ◄──────────────── Interaction
```

Rules:

- `Orders` never references `Drinks`, `Cakes`, `Lobby`, `UI`.
- `Drinks` and `Cakes` never reference each other.
- `Interaction` knows nothing about orders, drinks or cakes.
- Only `App` and `Debug` may reference everything.

---

## 5. Runtime services

Created once in `GameBootstrap`, registered in `ServiceRegistry`, disposed on scene unload.

| Service (interface) | Implementation | Module | Purpose |
|---|---|---|---|
| `IEventBus` | `EventBus` | Core | Typed publish/subscribe |
| `IGameClock` | `UnityGameClock` (runtime), `ManualClock` (tests) | Core | Time source, pause-aware |
| `IOrderService` | `OrderService` | Orders | Creates orders (Lobby only), drives `OrderStateMachine`, validates delivery |
| `IStallTicketQueue` | `StallTicketQueue` | Orders | FIFO of orders in `SentToStall`/`InPreparation`; claims pending items for preparation |
| `IReadyShelf` | `ReadyShelf` | Orders | Slots on the Ready counter; marks items Ready; releases complete orders to Lobby pickup |
| `IContentDatabase` | `ContentDatabase` | Content | Lookup of `SO_` definitions by id |
| `IIdGenerator` | `SequentialIdGenerator` | Core | Deterministic ids (seedable for tests) |

`ServiceRegistry` is scene-scoped (lives on the bootstrap GameObject). MonoBehaviours resolve services in `Awake`/`OnEnable` through a single accessor; they never call `FindObjectOfType` for services and never use static mutable singletons.

### Scene lifecycle

```text
SCN_Bootstrap (Scenes/Bootstrap)
  └─ GameBootstrap: build services → load content → SceneLoader.Load(SCN_VerticalSlice) additively
SCN_VerticalSlice (Scenes/Gameplay)
  └─ Scene objects register their interactables/points; nothing creates services here
SCN_AssetScaleTest, SCN_InteractionTest (Scenes/Test)
  └─ Self-contained test scenes; may use a TestBootstrap that builds the same services
```

`GameState`: `Booting → Playing ⇄ Paused → Ending`. Paused stops `IGameClock` (cooking and hold actions freeze).

---

## 6. Event catalogue (slice)

All are `readonly struct`, published synchronously on the main thread. Subscribers subscribe in `OnEnable` and unsubscribe in `OnDisable`.

| Event | Publisher | Typical subscribers |
|---|---|---|
| `OrderCreated { OrderId, OrderOrigin }` | Orders | UI, Customers |
| `OrderStatusChanged { OrderId, From, To }` | Orders | UI, Customers, Lobby |
| `OrderItemStatusChanged { OrderId, OrderItemId, From, To }` | Orders | UI |
| `DeliveryRejected { OrderId, AttemptedTarget, Reason }` | Orders | UI, QA log |
| `DrinkStepCompleted { PreparationId, DrinkState }` | Drinks | UI, audio hooks |
| `CakeStepCompleted { PreparationId, CakeState }` | Cakes | UI, audio hooks |
| `GrillStateChanged { GrillId, From, To }` | Cakes | Grill view, UI |
| `ActionBlocked { InteractableId, ReasonKey }` | Interaction | Prompt UI, QA log |
| `HeldItemChanged { Previous, Current }` | Interaction | Prompt UI, hand view |

---

## 7. Content (ScriptableObjects)

Stored under `Assets/TramChanh/ScriptableObjects/` ([WF] §7), prefix `SO_` ([WF] §16).

| Type | Fields (slice) | Instances in slice |
|---|---|---|
| `ItemDefinition` | `Id`, `DisplayName` (real menu name), `Kind` (`Drink`/`Cake`), `PreparedPrefab` | one drink (name TBD), the cake items below |
| `DrinkRecipe` | `ItemDefinition`, `TeaBagType`, portions (`CoconutJellyPortion`, `LemonJellyPortion`, `IcePortion`) `[Tbd]`, hold durations `[Tbd]` | `SO_Recipe_Drink_Slice` |
| `SauceDefinition` | `Id`, `DisplayName`, `SauceBagPrefab`, `MaterialVariant` | Mango, Chocolate, Cheese bags exist as assets ([AP] §32); **no cake mapping is assumed** |
| `CakeRecipe` | `ItemDefinition`, `SizeVariant` (`[Tbd("DEC-018")]`), `Sauce` (`SauceDefinition`, `[Tbd("DEC-008")]`, may be empty until confirmed), **`TargetBatterMl`**, **`BatterToleranceMl`**, `CookedThreshold`, `BurnThreshold`, hold durations — all `[Tbd("DEC-007")]` | one per cake menu item **and size** |
| `MeasureCupDefinition` | `CapacityMl` (500 — the real cup's nominal capacity), `FillRateMlPerSecond` `[Tbd]`, `LevelCurve` (ml → liquid height in the cup mesh) `[Tbd]` until the final mesh exists | `SO_MeasureCup_500ml` |
| `MenuItem` | `ItemDefinition`, `Recipe`, `AvailableInSlice` | see below |
| `BalanceConfig` | Grill preheat duration, open-lid cooking factor, reach distance, rack capacity, batter-deviation quality weight, `BatterOutOfTolerancePolicy` — all `[Tbd]` | `SO_Balance_Slice` |

Known real cake menu names (Product Owner, 2026-10-06) — used verbatim as `DisplayName`:

| Menu item | `SO_` asset | Sauce mapping |
|---|---|---|
| Bánh Lăn Truyền Thống | `SO_Item_BanhLan_TruyenThong` | unconfirmed |
| Bánh Lăn Phô Mai Chảy | `SO_Item_BanhLan_PhoMaiChay` | unconfirmed |
| Bánh Lăn Choco Chip | `SO_Item_BanhLan_ChocoChip` | unconfirmed |
| Bánh Lăn Cốm Dẻo | `SO_Item_BanhLan_ComDeo` | unconfirmed |

**Batter quantity is recipe-driven.** Each `CakeRecipe` (menu item × size) owns its `TargetBatterMl` and `BatterToleranceMl`; there is no global batter amount anywhere in code or data. Different cakes and sizes therefore need different measured amounts with the same 500 ml cup. All real amounts are `[Tbd("DEC-007")]` until measured at the real shop; the sizes themselves are DEC-018. See `CAKE_WORKFLOW.md` §2.2.

The names suggest flavours, but no sauce, topping or recipe difference is inferred from them. Until the PO confirms each recipe, a `CakeRecipe` with no sauce assigned is valid data and content validation reports every unconfirmed mapping as a warning; the sauce step uses whatever sauce the recipe data names once filled in. Which cake item is enabled for the slice is the `MenuItem.AvailableInSlice` flag, not code. The drink's real menu name is still to be supplied.

Display names are **not invented**: only names supplied by the Product Owner are used. All numeric preparation and balance values are provisional `[Tbd]` data until confirmed (DEC-007); gameplay code reads them from these assets and never contains guessed numbers.

The step order for drinks and cakes is **not data-driven** in the slice: it is the ground truth and lives in code as a constant sequence covered by GT-007/GT-008 tests. Recipes only parameterise amounts, durations and variants.

---

## 8. Cross-cutting conventions

- **Naming:** `PF_`, `SM_`, `MAT_`, `T_`, `SO_`, `AN_`, `SFX_/AMB_/MUS_` ([WF] §16). Scenes `SCN_`.
- **C#:** namespace `TramChanh.<Module>`; one public type per file; file name = type name; `PascalCase` public, `_camelCase` private fields; `[SerializeField] private` over public fields.
- **No per-frame allocations** in update loops (no LINQ, no closures, no string concatenation in `Update`).
- **Physics layers:** `Environment`, `Interactable`, `Player`, `NPC`, `HeldItem` (no collision with Player). Interaction raycasts use the `Interactable` mask only.
- **Localization-ready:** UI shows `ReasonKey`/`PromptKey` strings resolved through a single table (Vietnamese + English). No hard-coded Vietnamese strings in scripts.
- **Git:** `main` / `develop` / `feature/*` / `review/*` / `qa/*`; one worktree per task ([WF] §17–18). File ownership per [WF] §19 (Claude: `Docs/**`; Codex: `Assets/TramChanh/Scripts|Tests|Prefabs/**`; Antigravity: `Automation/**`, `QA/**`, `BuildScripts/**`).
- **Binary assets:** FBX/PNG/PSD/TGA/WAV through Git LFS (set up in `CX-001`).

---

## 9. Testing strategy

| Layer | Tool | What |
|---|---|---|
| Domain unit | Unity Test Framework, EditMode | Every state transition (valid + blocked) of Order, Drink, Cake, Grill; `ManualClock` for time |
| Ground-truth | EditMode | GT-001…GT-008 as named tests (`GroundTruthTests`) |
| Asset validation | EditMode + Editor validator | Prefab names, bounds, pivots, colliders, anchors, missing materials/scripts |
| Integration smoke | PlayMode | Bootstrap loads; player interacts with a test cube; each station step executes |
| End-to-end | PlayMode | Scripted dine-in and takeaway orders from customer arrival to `Completed` |
| QA run | Antigravity (`Automation/`) | Batch-mode compile, EditMode, PlayMode, report PASS/FAIL per acceptance criterion |

Every test name carries its spec id, e.g. `TC_ORDER_001_DineIn_FullFlow`, `GT_007_DrinkSequence_IsExact`.

---

## 10. Decision register

Product Owner decision 2026-10-06: **all architectural defaults accepted**, with the amendments below. "Provisional" means the value or form is data/prefab-configurable and must not be treated as ground truth (principle 10).

| ID | Question | Decision for the slice | Status | Affects |
|---|---|---|---|---|
| DEC-001 | Who plays the Lobby? | Player performs both Lobby and Stall actions; `ActorRole` keeps an NPC Lobby possible later. | Accepted | Lobby, UI, Customers |
| DEC-002 | Camera perspective | First-person, keyboard + mouse. | Accepted | Interaction, Player |
| DEC-003 | Where the Lobby enters/sends the order | Order-entry UI at the table/vehicle; *Enter* and *Send to stall* are two confirmations. No device asset. | Accepted | Lobby, UI |
| DEC-004 | Grill power-on | Automatic at shift start; no player power action. | Accepted | Cakes |
| DEC-005 | Tools held or station-driven | Station-driven tools; only tea bag, batter cup and finished items are held. | Accepted | Interaction, Drinks, Cakes |
| DEC-006 | Ruined product handling | `Discard` on the held ruined item; order item returns to Pending. | Accepted | Cakes, Stall |
| DEC-007 | Real numbers (`TargetBatterMl` / `BatterToleranceMl` per recipe and size, cup fill rate, cook time, burn time, topping/ice portions, hold durations, open-lid rate, reach, batter-deviation quality weight) | **Provisional `[Tbd]` data** in recipes / `SO_Balance_Slice`. No guessed number in gameplay code. | Open — values pending | Cakes, Drinks, Interaction |
| DEC-008 | Menu names and sauce mapping | Cake names known (§7). Drink name pending. **Sauce-to-cake mapping unconfirmed**: configurable `CakeRecipe.Sauce`, not inferred from names. | Partially confirmed | Content, Cakes |
| DEC-009 | Batter source container | Correctly sized placeholder (`PF_Placeholder_BatterSource`, size `[Tbd]`) until reference arrives; gameplay never waits for art. | Accepted (placeholder) | Cakes, assets |
| DEC-010 | Customer and vehicle visuals | Capsule customer placeholder. Vehicle = **generic vehicle interaction point** (`VehicleOrderPoint` on `PF_Placeholder_VehiclePoint`, a sized trigger volume); no specific vehicle model is part of the architecture. | Accepted (placeholder) | Customers, Lobby, assets |
| DEC-011 | Station positions on the counter | **Adjustable placeholder anchors** (`StallAnchor`, *Position Confirmed* off). Positions are prefab/scene data, never code; final positions come from the real stall reference. | Open — positions pending | Stall prefab |
| DEC-012 | Lobby entering a different item than requested | Not in the slice; entry pre-filled from the request. `Requested` and `Entered` kept separately. | Accepted | Orders, UI |
| DEC-013 | Physical form of the cake *Flip* step | **Not defined yet.** The workflow step stays named `Flip` (state `Flipped`); its physical action is isolated behind `IFlipAction` (`CAKE_WORKFLOW.md` §2.3). The slice ships a placeholder implementation that can be replaced without changing the state machine. | Open — real action pending | Cakes |
| DEC-014 | Ready counter capacity | 1 drink slot + 1 cake slot; domain supports N. | Accepted | Stall, Orders |
| DEC-015 | Tea rack capacity / refill | Capacity is `[Tbd]` data; no refill gameplay in the slice. | Accepted | Drinks |
| DEC-016 | Takeaway packaging | None in the slice; no packaging step added. | Accepted | Lobby |
| DEC-018 | Cake sizes | Recipes support several size variants per menu item, each with its own batter amount. Which sizes exist (names, count) is **not yet known**; the slice ships one unnamed size per item until confirmed. | Open — sizes pending | Content, Cakes |
| DEC-019 | Out-of-tolerance batter | Measuring is mandatory before pouring. A wrong quantity is **detected and recorded** on the cake (`BatterMeasurement`) and lowers quality. Whether a wrong amount may still be poured is `BatterOutOfTolerancePolicy` data: slice default `AllowWithPenalty`; `BlockPour` available. | Accepted (policy is data) | Cakes, Orders (quality) |
| DEC-017 | New sign size and artwork | Placeholder volume with the `MAT_Sign_TramChanh_New` material slot; size `[Tbd]`; artwork swapped in later. The old illuminated sign is never recreated. | Open — artwork pending | Stall, assets |

---

## 11. Milestone mapping

| [WF] milestone | Slice deliverable | Primary tasks (see `PROJECT_TASK_PLAN.md`) |
|---|---|---|
| M0 Project foundation | Unity project, asmdefs, services, bootstrap, tests running | CX-001…CX-004, QA-001…QA-002 |
| M1 Stall blockout | `PF_Stall_TramChanh` at true scale, new sign, scale test scene | ART-STALL-001, ART-BRAND-001/002, INT-001…INT-003 |
| M2 Player interaction | Interactor, held item, prompts, test cube | CX-010…CX-012 |
| M3 Lobby + order | Orders domain, Lobby points, entry UI, ticket UI, Ready counter | CX-020…CX-025 |
| M4 Drink vertical slice | Full drink workflow at the counter | CX-030…CX-031, ART-DRINK-*, INT-004 |
| M5 Cake vertical slice | Full cake workflow with grill | CX-040…CX-041, ART-CAKE-*, INT-005 |
| Slice integration | One customer, dine-in + takeaway end-to-end | CX-050, INT-006…INT-008, QA-050 |

---

## 12. Change control

- Changes to ground truth: not allowed.
- Changes to module boundaries, service interfaces, state machines: proposal in `Docs/` by the agent who found the problem → Claude Code review → Product Owner sign-off.
- Codex may refactor *inside* a module without review if public contracts and tests are unchanged.
