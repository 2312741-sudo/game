# TRAM CHANH — PROJECT TASK PLAN (Vertical Slice)

**Status:** Draft for Product Owner approval
**Owner:** Claude Code (plan) · executed by Codex, 3D production, Antigravity
**Goal:** the first playable vertical slice defined in `ARCHITECTURE.md` §2. Both the dine-in and the vehicle-takeaway order must run end-to-end with one drink and one cake.

---

## 0. How to read this plan

### Owners

| Code | Owner | Role ([WF] §1) |
|---|---|---|
| **CC** | Claude Code | Architect, spec, review gates |
| **CX** | Codex | Unity/C# implementation **and** Unity integration (import, prefabs, colliders, anchors, animators, scenes — [AP] §3.4) |
| **3D** | 3D production: AI 3D generator + Blender artist, supervised by the Product Owner | Mesh, UV, textures, pivots, FBX |
| **AG** | Antigravity | Orchestration, batch compile/test, validation, PASS/FAIL reports |
| **PO** | Product Owner (you) | Decisions (DEC-xxx), reference photos, branding artwork, final "is it faithful?" call |

### Task id prefixes

| Prefix | Track |
|---|---|
| `REV-` | Review / decision gates (CC, PO) |
| `CX-` | Codex implementation (code + tests) |
| `ART-` | 3D asset production (ids from [AP] where they exist) |
| `INT-` | Unity integration (owner CX) |
| `QA-` | Antigravity QA |

### Fields used for every task

**Owner · Objective · Depends on · Files/modules · 3D assets · Acceptance · Tests · Parallel · Review before impl.**

- *Parallel* = can run at the same time as other tasks in its wave without touching the same files.
- *Review before impl.* = **Yes** means the owner must not start until the named gate is passed (spec or decision confirmed). **No** means the spec in `Docs/` is sufficient.
- Every task also follows the per-feature Definition of Done ([WF] §20) and ends with an Antigravity validation and a Claude review before merge to `develop`.

### Standard task flow ([WF] §21)

```text
CC spec (Docs) → CX implement in feature/<task> worktree → AG validate (PASS/FAIL)
→ CC review → CX fix → AG re-validate → merge to develop
```

---

## 1. Waves and critical path

```text
Wave 0  Decisions & setup     REV-000, REV-DEC, CX-001, QA-000, QA-001, ART-STALL-001(A)
Wave 1  Foundation            CX-002, CX-003, CX-004, CX-005, INT-001, ART-BRAND-001/002
Wave 2  Interaction + Orders  CX-010…CX-012, CX-020…CX-022, INT-002, INT-003, ART-CAKE-001, ART-DRINK-001/002
Wave 3  Lobby + stations      CX-023…CX-025, CX-030, CX-040, INT-004(A), INT-005(A), INT-006, remaining ART
Wave 4  Station gameplay      CX-031, CX-041, CX-050, INT-004(B), INT-005(B), INT-007
Wave 5  Slice integration     INT-008, CX-060, QA-050, REV-010
```

Critical path (code can always proceed on blockouts, so art is **not** on it):

```text
REV-000 → CX-001 → CX-002 → CX-010 → CX-020 → CX-021 → CX-022 → CX-030/CX-040
→ CX-031/CX-041 → INT-008 → QA-050 → REV-010
```

---

## 2. Summary table

| ID | Owner | Title | Depends on | Parallel | Review before |
|---|---|---|---|---|---|
| REV-000 | CC+PO | Approve architecture docs | — | — | — |
| REV-DEC | PO | Answer decision register DEC-001…016 | REV-000 | Yes | — |
| QA-000 | AG | Repo, branches, docs present | — | Yes | No |
| CX-001 | CX | Unity 6 project skeleton | REV-000 | No | Yes (REV-000) |
| QA-001 | AG | Batch compile/test automation | CX-001 | Yes | No |
| CX-002 | CX | Core services | CX-001 | Yes | No |
| CX-003 | CX | Content ScriptableObjects | CX-001, REV-DEC (DEC-007/008) | Yes | Yes (DEC-008) |
| CX-004 | CX | Bootstrap, GameState, SceneLoader | CX-002 | No | Yes (REV-001 lifecycle) |
| CX-005 | CX | Asset import postprocessor + validator | CX-001 | Yes | No |
| QA-002 | AG | Foundation validation (M0) | CX-002…CX-005, QA-001 | — | No |
| REV-001 | CC | Review interaction contracts before CX-010 | REV-000 | Yes | — |
| CX-010 | CX | Interaction core | CX-002, REV-001 | No | Yes |
| CX-011 | CX | First-person player + input | CX-010, DEC-002 | Yes | Yes (DEC-002) |
| CX-012 | CX | Interaction prompt UI + test scene | CX-010 | Yes | No |
| QA-010 | AG | Interaction validation (M2) | CX-010…CX-012 | — | No |
| REV-002 | CC | Review order domain API | REV-000 | Yes | — |
| CX-020 | CX | Order domain + state machine | CX-002, REV-002 | Yes | Yes |
| CX-021 | CX | OrderService + StallTicketQueue | CX-020 | No | No |
| CX-022 | CX | ReadyShelf + ReadyCounterPoint | CX-021, CX-010 | No | No |
| CX-023 | CX | Lobby order points + LobbyOrderController | CX-021, CX-010, DEC-001/003 | Yes | Yes (DEC-001, DEC-003) |
| CX-024 | CX | Order entry UI + ticket UI + ready UI | CX-021, CX-012 | Yes | Yes (UI layout review) |
| CX-025 | CX | Delivery + ServedOrder held bundle | CX-022, CX-023 | No | No |
| QA-020 | AG | Order system validation (M3) | CX-020…CX-025 | — | No |
| REV-003 | CC | Drink spec sign-off | REV-000 | Yes | — |
| CX-030 | CX | Drink domain | CX-021, REV-003 | Yes | Yes |
| CX-031 | CX | Drink station runtime | CX-030, CX-022, INT-004(A) | No | No |
| QA-030 | AG | Drink validation (M4) | CX-031, INT-004 | — | No |
| REV-004 | CC | Cake spec sign-off | REV-000, DEC-004/005/006/013 | Yes | — |
| CX-040 | CX | Cake + grill domain | CX-021, REV-004 | Yes | Yes |
| CX-041 | CX | Cake station runtime | CX-040, CX-022, INT-005(A) | No | No |
| QA-040 | AG | Cake validation (M5) | CX-041, INT-005 | — | No |
| CX-050 | CX | Slice customer (scripted) | CX-023, CX-025, DEC-010 | Yes | Yes (REV-006) |
| CX-060 | CX | Ground-truth + E2E test suite | CX-031, CX-041, CX-050, INT-008 | No | No |
| REV-005 | CC | Asset integration rules sign-off | REV-000 | Yes | — |
| REV-006 | CC | Slice customer behaviour review | REV-000, DEC-010 | Yes | — |
| REV-007 | CC+PO | Station layout on the counter | DEC-011 photos | Yes | — |
| ART-STALL-001 | 3D | Stall (blockout → gameplay-ready) | REV-005, DEC-011 | Yes | Yes (A: no; B: DEC-011) |
| ART-BRAND-001 | 3D | New sign geometry | REV-005 | Yes | No |
| ART-BRAND-002 | 3D+PO | New sign branding texture | PO artwork | Yes | Yes (PO artwork) |
| ART-CAKE-001 | 3D | Elmich grill | REV-005 | Yes | No |
| ART-DRINK-001 | 3D | Red tea rack | REV-005 | Yes | No |
| ART-DRINK-002 | 3D | Pre-portioned tea bag | ART-DRINK-001 (fit) | Yes | No |
| ART-DRINK-003 | 3D | Topping station + jellies | ART-STALL-001(A) | Yes | No |
| ART-DRINK-004 | 3D | Ice bin + scoop | ART-STALL-001(A) | Yes | No |
| ART-DRINK-005 | 3D | Wipe cloth | REV-005 | Yes | No |
| ART-CAKE-002 | 3D | Batter measure cup + batter | REV-005 | Yes | No |
| ART-CAKE-003 | 3D | Spatula | REV-005 | Yes | No |
| ART-CAKE-004 | 3D | Scissors | REV-005 | Yes | No |
| ART-CAKE-005 | 3D | Sauce bag (+ variants) | REV-005, DEC-008 | Yes | No |
| ART-CAKE-006 | 3D | Cake states raw/cooked/cut/rolled | ART-CAKE-001 (plate size) | Yes | Yes (roll axis vs reference) |
| ART-CAKE-007 | 3D | Wrapping paper + wrapped cake | ART-CAKE-006 | Yes | No |
| ART-FURN-001 | 3D | Plastic stool | REV-005 | Yes | No |
| ART-FURN-002 | 3D | Yellow crate table + stainless tray | REV-005 | Yes | No |
| INT-001 | CX | `SCN_AssetScaleTest` | CX-001, CX-005 | Yes | No |
| INT-002 | CX | `PF_Stall_TramChanh` (blockout → final) | INT-001, ART-STALL-001 | Yes | Yes (DEC-011 layout) |
| INT-003 | CX | `PF_Sign_TramChanh_New` | INT-002, ART-BRAND-001/002 | Yes | No |
| INT-004 | CX | Drink station prefabs | INT-002, ART-DRINK-001…005 | Yes | No |
| INT-005 | CX | Cake station prefabs + grill animator | INT-002, ART-CAKE-001…007 | Yes | No |
| INT-006 | CX | Customer area + placeholders | INT-001, ART-FURN-001/002, DEC-010 | Yes | No |
| INT-007 | CX | `PF_ReadyCounterPoint` | INT-002, CX-022 | Yes | No |
| INT-008 | CX | `SCN_VerticalSlice` composition | INT-002…INT-007, CX-031, CX-041, CX-050 | No | Yes (REV-007 layout) |
| QA-011 | AG | Asset validation: stall + sign | INT-002, INT-003 | Yes | No |
| QA-012 | AG | Asset validation: drink assets | INT-004 | Yes | No |
| QA-013 | AG | Asset validation: cake assets | INT-005 | Yes | No |
| QA-014 | AG | Asset validation: customer area | INT-006 | Yes | No |
| QA-050 | AG | Vertical slice end-to-end + GT suite | INT-008, CX-060 | No | No |
| REV-010 | CC+PO | Slice review & sign-off | QA-050 | — | — |

---

## 3. Review and decision gates

### REV-000 — Approve architecture docs
- **Owner:** Claude Code (author) + Product Owner (approver)
- **Objective:** confirm `ARCHITECTURE.md`, `ORDER_SYSTEM.md`, `INTERACTION_SYSTEM.md`, `DRINK_WORKFLOW.md`, `CAKE_WORKFLOW.md`, `ASSET_INTEGRATION.md` and this plan reflect the real shop.
- **Depends on:** —
- **Files/modules:** `Docs/**`
- **3D assets:** none
- **Acceptance:** PO confirms GT-001…008 wording; architecture review items (added `Stall` module, `Ruined` cake state, added anchors, `PF_Cake_Cut`, `Continuous` interaction kind) accepted or changed.
- **Tests:** n/a
- **Parallel:** n/a · **Review before impl.:** n/a

### REV-DEC — Decision register answers
- **Owner:** Product Owner
- **Objective:** answer DEC-001…DEC-016 (`ARCHITECTURE.md` §10) or accept the proposed defaults; supply reference photos for station layout (DEC-011), batter source (DEC-009), vehicle size (DEC-010), sign artwork, menu names (DEC-008), balance numbers (DEC-007).
- **Depends on:** REV-000
- **Files/modules:** `ARCHITECTURE.md` §10 (CC records answers)
- **3D assets:** reference photos into `ArtSource/References/`
- **Acceptance:** every DEC has status *Confirmed* or *Default accepted*; DEC-007 numbers may stay TBD until tuning in `INT-008`.
- **Tests:** n/a
- **Parallel:** Yes (does not block Wave 0 code)

### REV-001 … REV-007 — Spec reviews (Claude Code)
| Gate | Reviews | Blocks |
|---|---|---|
| REV-001 | Interaction contracts (`INTERACTION_SYSTEM.md` §2) + scene lifecycle (`ARCHITECTURE.md` §5) | CX-004, CX-010 |
| REV-002 | Order API and transition table | CX-020 |
| REV-003 | Drink state machine | CX-030 |
| REV-004 | Cake + grill state machines (after DEC-004/005/006/013) | CX-040 |
| REV-005 | Asset contracts and canonical names | ART-*, INT-* |
| REV-006 | Slice customer behaviour (scripted, no NavMesh complexity) | CX-050 |
| REV-007 | Station layout on the 1.8 × 0.8 m counter (with DEC-011 photos) | INT-002 (B), INT-008 |

Each produces either "approved" or a short list of required doc changes. Owner CC; PO joins REV-007.

### REV-010 — Slice sign-off
- **Owner:** Claude Code (technical review) + Product Owner ("fun and faithful")
- **Objective:** approve the vertical slice for expansion.
- **Depends on:** QA-050
- **Acceptance:** QA-050 all PASS; [WF] §31 review gate clean; PO plays dine-in and takeaway once each and confirms the workflows match the shop.
- **Tests:** manual play-through + QA-050 report.

---

## 4. Codex implementation tasks

### CX-001 — Unity 6 project skeleton
- **Owner:** Codex (GPT-5.6 Sol High)
- **Objective:** create the Unity 6 URP project at repo root with the folder structure, assemblies, test assemblies, packages and repo hygiene.
- **Depends on:** REV-000
- **Files/modules:** `Assets/TramChanh/**` folders ([WF] §7 + `Scripts/Stall`, `Prefabs/CustomerArea`), all `.asmdef` from `ARCHITECTURE.md` §4, `Packages/manifest.json` (URP, Input System, Test Framework, UI Toolkit), `ProjectSettings/` (layers from `ASSET_INTEGRATION.md` §6, linear colour, URP asset), `.gitignore` (Unity), `.gitattributes` (LFS for fbx/blend/png/tga/psd/wav), `.editorconfig`, `Docs/CODING_CONVENTIONS.md`.
- **3D assets:** none
- **Acceptance:** project opens in the pinned Unity 6 version with zero errors/warnings from project code; asmdef reference graph matches `ARCHITECTURE.md` §4 exactly; an empty EditMode and PlayMode test each run green; `ProjectVersion.txt` committed.
- **Tests:** `Smoke_EditMode_Runs`, `Smoke_PlayMode_Runs`; AG verifies asmdef graph.
- **Parallel:** No (everything depends on it) · **Review before impl.:** Yes — REV-000.

### CX-002 — Core services
- **Owner:** Codex
- **Objective:** implement `IEventBus`/`EventBus`, `ServiceRegistry`, `IGameClock`/`UnityGameClock`/`ManualClock`, `IIdGenerator`, strongly-typed ids, `Result`/`Result<T>`, `Availability`.
- **Depends on:** CX-001
- **Files/modules:** `Scripts/Core/**`, `Tests/EditMode/Core/**`
- **3D assets:** none
- **Acceptance:** zero allocations on publish of a struct event with ≤8 subscribers; unsubscribe during publish is safe; `ManualClock` advance/pause semantics; registry throws a clear error on missing service.
- **Tests:** `EventBusTests`, `ServiceRegistryTests`, `GameClockTests`.
- **Parallel:** Yes (with CX-005, QA-001) · **Review before impl.:** No.

### CX-003 — Content ScriptableObjects
- **Owner:** Codex (GPT-5.6 Terra acceptable)
- **Objective:** `ItemDefinition`, `DrinkRecipe`, `CakeRecipe`, `MenuItem`, `BalanceConfig`, `ContentDatabase` + slice instances `SO_Item_*`, `SO_Recipe_Drink_Slice`, `SO_Recipe_Cake_Slice`, `SO_Balance_Slice`.
- **Depends on:** CX-001; DEC-008 (which drink/sauce), DEC-007 (values may be TBD placeholders)
- **Files/modules:** `Scripts/Core/Content/**`, `ScriptableObjects/**`, `Tests/EditMode/Content/**`
- **3D assets:** none
- **Acceptance:** step sequences are **not** editable data (GT-007/008 are code constants); every TBD value carries a `[Tooltip("TBD — DEC-007")]`; database validates unique ids.
- **Tests:** `ContentDatabaseTests` (unique ids, all references resolved).
- **Parallel:** Yes · **Review before impl.:** Yes — DEC-008 confirmed or default accepted.

### CX-004 — Bootstrap, GameState, SceneLoader
- **Owner:** Codex
- **Objective:** `GameBootstrap` composition root, `GameState` machine (`Booting → Playing ⇄ Paused → Ending`), additive `SceneLoader`, `SCN_Bootstrap`, `TestBootstrap` for test scenes.
- **Depends on:** CX-002, REV-001
- **Files/modules:** `Scripts/Core/Bootstrap/**` (`TramChanh.App`), `Scenes/Bootstrap/SCN_Bootstrap.unity`, `Tests/PlayMode/Bootstrap/**`
- **3D assets:** none
- **Acceptance:** entering Play from `SCN_Bootstrap` builds all services and loads the gameplay scene; pause stops `IGameClock`; no `FindObjectOfType` for services; no static mutable singletons.
- **Tests:** `BootstrapTests`, `SceneLoadTests` ([WF] Phase 1).
- **Parallel:** No · **Review before impl.:** Yes — REV-001.

### CX-005 — Asset import postprocessor + validator
- **Owner:** Codex
- **Objective:** `TramChanhModelImporter` (settings in `ASSET_INTEGRATION.md` §4) and `TramChanhAssetValidator` (rules in §9.1) with a batch-mode entry point for Antigravity.
- **Depends on:** CX-001
- **Files/modules:** `Scripts/Editor/Import/**`, `Scripts/Editor/Validation/**`, `Tests/EditMode/Validation/**`
- **3D assets:** none (tested with generated primitives)
- **Acceptance:** validator outputs a machine-readable report (JSON) with rule id, prefab, severity; exit code non-zero on any Error; canonical prefab list is a single data file shared with tests.
- **Tests:** `AssetValidatorTests` with good/bad fixture prefabs for each rule; `GT_002_OnlyNewSign`.
- **Parallel:** Yes · **Review before impl.:** No.

### CX-010 — Interaction core
- **Owner:** Codex (Sol High)
- **Objective:** `IInteractable`, `InteractionQuery`, `InteractionContext`, `IHoldable`, `IHeldItemAction`, `HeldItemSlot`, `PlayerInteractor` (raycast/focus), Hold and Continuous drivers.
- **Depends on:** CX-002, REV-001
- **Files/modules:** `Scripts/Interaction/**`, `Tests/EditMode/Interaction/**`
- **3D assets:** none
- **Acceptance:** `INTERACTION_SYSTEM.md` §2–4; `Query` zero-alloc; hold/continuous use `IGameClock`.
- **Tests:** TC-INT-001…004, 007, 008.
- **Parallel:** No (on critical path) · **Review before impl.:** Yes — REV-001.

### CX-011 — First-person player + input
- **Owner:** Codex
- **Objective:** first-person controller (CharacterController), camera, `HandSocket`, Input System action map `Gameplay` (Move, Look, Interact, UseHeld, Discard, Pause).
- **Depends on:** CX-010, DEC-002
- **Files/modules:** `Scripts/Interaction/Player/**` (or `Scripts/Core/Player` — decided at REV-001), `Settings/Input/TramChanh.inputactions`, `Prefabs/NPC/PF_Player.prefab`
- **3D assets:** none (capsule)
- **Acceptance:** eye height ~1.6 m so the 1.0 m counter reads at waist height; can reach the back of the 0.8 m counter from the front within reach distance; no input read outside the action map.
- **Tests:** `PlayerReachTests` (PlayMode, ray from standing position reaches a target at counter depth).
- **Parallel:** Yes (with CX-012) · **Review before impl.:** Yes — DEC-002.

### CX-012 — Interaction prompt UI + test scene
- **Owner:** Codex
- **Objective:** `InteractionPromptView` (UI Toolkit), localization table stub (vi/en), `SCN_InteractionTest` with test cube interactables (Press, Hold, Continuous, Blocked).
- **Depends on:** CX-010
- **Files/modules:** `Scripts/UI/Prompt/**`, `Scripts/UI/Localization/**`, `Scenes/Test/SCN_InteractionTest.unity`, `Tests/PlayMode/Interaction/**`
- **3D assets:** none
- **Acceptance:** prompt shows text, key, hold ring, blocked reason; no per-frame UI rebuild when the prompt is unchanged.
- **Tests:** TC-INT-005, TC-INT-006.
- **Parallel:** Yes · **Review before impl.:** No.

### CX-020 — Order domain + state machine
- **Owner:** Codex (Sol High)
- **Objective:** `Order`, `OrderItem`, `OrderOrigin`, `ItemRequest`, `OrderStatus`, `OrderItemStatus`, `OrderStateMachine` implementing T1–T10 and item transitions.
- **Depends on:** CX-002, REV-002
- **Files/modules:** `Scripts/Orders/Domain/**`, `Tests/EditMode/Orders/**`
- **3D assets:** none
- **Acceptance:** `ORDER_SYSTEM.md` §2–4; no UnityEngine types in `Domain/`; illegal transitions return `Result.Fail` with reason key and leave state unchanged.
- **Tests:** TC-ORDER-006 (full transition matrix), TC-ORDER-008, GT-005, GT-006.
- **Parallel:** Yes (with CX-010) · **Review before impl.:** Yes — REV-002.

### CX-021 — OrderService + StallTicketQueue
- **Owner:** Codex
- **Objective:** `IOrderService`, `IStallTicketQueue` with FIFO claim/release, event publication.
- **Depends on:** CX-020
- **Files/modules:** `Scripts/Orders/Services/**`, `Tests/EditMode/Orders/**`
- **3D assets:** none
- **Acceptance:** `ORDER_SYSTEM.md` §5, §9; stall sees nothing before `SentToStall`; claim binds oldest pending item of kind.
- **Tests:** TC-ORDER-003, TC-ORDER-007, TC-ORDER-009, GT-004 (reflection test on API surface).
- **Parallel:** No · **Review before impl.:** No.

### CX-022 — ReadyShelf + ReadyCounterPoint
- **Owner:** Codex
- **Objective:** `IReadyShelf`, `IPreparedItem`, `ReadyCounterPoint` (in `TramChanh.Stall`), `PlaceholderDiscard` handling (DEC-006).
- **Depends on:** CX-021, CX-010
- **Files/modules:** `Scripts/Orders/Ready/**`, `Scripts/Stall/**`, `Tests/EditMode/Orders/ReadyShelfTests.cs`
- **3D assets:** `PF_ReadyCounterPoint` (no mesh; INT-007)
- **Acceptance:** `ORDER_SYSTEM.md` §6 placement rules 1–4; order becomes Ready only when all items placed; pickup takes the whole order.
- **Tests:** TC-ORDER-005, `ReadyShelf_RejectsUnfinished`, `ReadyShelf_RejectsUnbound`, `ReadyShelf_SlotFull`.
- **Parallel:** No · **Review before impl.:** No.

### CX-023 — Lobby order points + LobbyOrderController
- **Owner:** Codex
- **Objective:** `TableOrderPoint`, `VehicleOrderPoint`, `LobbyOrderController`, `ActorRole` handling per DEC-001.
- **Depends on:** CX-021, CX-010, DEC-001, DEC-003
- **Files/modules:** `Scripts/Lobby/**`, `Tests/EditMode/Lobby/**`
- **3D assets:** uses blockouts of `PF_YellowCrateTable`, `PF_Placeholder_Vehicle`
- **Acceptance:** prompt table in `ORDER_SYSTEM.md` §7; take order only at the order's own point; stall interactables never create or modify orders.
- **Tests:** `TableOrderPoint_TakeOrder`, `VehicleOrderPoint_TakeOrder`, GT-005, GT-006 (integration level).
- **Parallel:** Yes (with CX-024) · **Review before impl.:** Yes — DEC-001, DEC-003.

### CX-024 — Order entry UI, ticket UI, ready UI
- **Owner:** Codex
- **Objective:** `OrderEntryUI` (pre-filled request, *Enter*, *Send to stall*), `StallTicketUI`, `ReadyOrdersUI`.
- **Depends on:** CX-021, CX-012
- **Files/modules:** `Scripts/UI/Orders/**`, `Prefabs/UI/**`, UXML/USS under `Assets/TramChanh/UI/` (created here)
- **3D assets:** none
- **Acceptance:** UI only calls `IOrderService`; readable at 16:9, 16:10, 21:9 ([WF] Phase 7); no text overflow with Vietnamese strings.
- **Tests:** `OrderEntryUI_EnterThenSend` (PlayMode), layout screenshot checks by AG.
- **Parallel:** Yes · **Review before impl.:** Yes — CC reviews wireframe (short) before build.

### CX-025 — Delivery + ServedOrder bundle
- **Owner:** Codex
- **Objective:** `ServedOrder` held bundle from Ready pickup; delivery at table/vehicle via T8; wrong-target rejection.
- **Depends on:** CX-022, CX-023
- **Files/modules:** `Scripts/Lobby/Delivery/**`, `Tests/EditMode/Lobby/**`
- **3D assets:** finished item prefabs (blockouts acceptable)
- **Acceptance:** `ORDER_SYSTEM.md` §3.2; items stay in hands on rejection; `DeliveryRejected` published.
- **Tests:** TC-ORDER-004, `Deliver_CorrectTable_Completes`, `Deliver_CorrectVehicle_Completes`.
- **Parallel:** No · **Review before impl.:** No.

### CX-030 — Drink domain
- **Owner:** Codex (Sol High)
- **Objective:** `DrinkPreparation`, `TeaBagState`, strict sequence D1–D9, order binding, `IPreparedItem` data.
- **Depends on:** CX-021, REV-003
- **Files/modules:** `Scripts/Drinks/Domain/**`, `Tests/EditMode/Drinks/**`
- **3D assets:** none
- **Acceptance:** `DRINK_WORKFLOW.md` §2–3; no tea-measuring state exists; only GT-007 path succeeds.
- **Tests:** TC-DRINK-001…005, 007, 008, GT-007.
- **Parallel:** Yes (with CX-040) · **Review before impl.:** Yes — REV-003.

### CX-031 — Drink station runtime
- **Owner:** Codex
- **Objective:** `TeaRackController`, `TeaBagItem`, `TeaBagStateView`, `ToppingBin`, `IceBin`, `WipeInteraction`; animation hooks for scoop, shake, wipe.
- **Depends on:** CX-030, CX-022, INT-004 (A: blockout prefabs)
- **Files/modules:** `Scripts/Drinks/Runtime/**`, `Prefabs/Workstations/*Drink*`, `Prefabs/Items/PF_TeaBag_PrePortioned`, `Tests/PlayMode/Drinks/**`
- **3D assets:** `PF_RedTeaRack`, `PF_TeaBag_PrePortioned`, `PF_ToppingStation`, `PF_Topping_*`, `PF_IceBin`, `PF_IceScoop`, `PF_WipeCloth` (blockout first, real later)
- **Acceptance:** each interactable offers exactly the action in the `INTERACTION_SYSTEM.md` §5 table; wrong state → Blocked with the reason key; visuals per `DRINK_WORKFLOW.md` §5; `PF_MeasuringCup_500ml` and pump bottles have no interactable.
- **Tests:** TC-DRINK-009 (PlayMode full drink), TC-DRINK-006 via E2E.
- **Parallel:** No · **Review before impl.:** No.

### CX-040 — Cake + grill domain
- **Owner:** Codex (Sol High)
- **Objective:** `CakePreparation`, `CakeState` (incl. `Ruined`), `GrillModel`, `GrillState`, cooking model, quality formula.
- **Depends on:** CX-021, REV-004
- **Files/modules:** `Scripts/Cakes/Domain/**`, `Tests/EditMode/Cakes/**`
- **3D assets:** none
- **Acceptance:** `CAKE_WORKFLOW.md` §2–6; all timing via `IGameClock`; only GT-008 path succeeds.
- **Tests:** TC-CAKE-001…010, GT-008.
- **Parallel:** Yes (with CX-030) · **Review before impl.:** Yes — REV-004.

### CX-041 — Cake station runtime
- **Owner:** Codex
- **Objective:** `BatterMeasureCup`, `BatterSourceInteractable`, `GrillController` (lid animator, display, sizzle hook), `CakeView`, `RollAreaInteractable`, `SauceBagInteractable`, `WrapInteraction`, discard of ruined cake.
- **Depends on:** CX-040, CX-022, INT-005 (A)
- **Files/modules:** `Scripts/Cakes/Runtime/**`, `Prefabs/Workstations/*Cake*`, `Prefabs/Items/PF_Cake_*`, `Tests/PlayMode/Cakes/**`
- **3D assets:** `PF_Grill_Elmich`, `PF_BatterMeasureCup`, `PF_BatterPortion`, `PF_Placeholder_BatterSource`, `PF_Spatula_WoodHandle`, `PF_Scissors_RedGray`, `PF_SauceBag_*`, `PF_Cake_Raw/Cooked/Cut/Rolled/Wrapped`, `PF_CakeWrappingPaper`
- **Acceptance:** grill offers one action per state (`CAKE_WORKFLOW.md` §3.1); lid rotates about the hinge; display shows temperature during preheat and time while cooking; cake visuals per `ASSET_INTEGRATION.md` §8.
- **Tests:** TC-CAKE-011 (PlayMode full cake).
- **Parallel:** No · **Review before impl.:** No.

### CX-050 — Slice customer (scripted)
- **Owner:** Codex
- **Objective:** `SliceCustomerController`: spawn → go to table seat or vehicle (scenario config) → `RequestService` → wait → receive → `Complete` → leave. Simple NavMesh or waypoint movement; no patience, no payment.
- **Depends on:** CX-023, CX-025, DEC-010, REV-006
- **Files/modules:** `Scripts/Customers/**`, `Prefabs/NPC/PF_Placeholder_Customer`, `Tests/PlayMode/Customers/**`
- **3D assets:** `PF_Placeholder_Customer`, `PF_Placeholder_Vehicle`, `PF_PlasticStool`, `PF_YellowCrateTable`
- **Acceptance:** customer never interacts with the stall; one customer at a time; scenario config selects dine-in or takeaway and the requested items.
- **Tests:** `Customer_DineIn_Lifecycle`, `Customer_Vehicle_Lifecycle`.
- **Parallel:** Yes (with CX-031/041) · **Review before impl.:** Yes — REV-006.

### CX-060 — Ground-truth + end-to-end test suite
- **Owner:** Codex (Terra acceptable for repetitive tests)
- **Objective:** `GroundTruthTests` GT-001…GT-008 as named tests, and PlayMode E2E tests driving the player through both slice scenarios via a test input driver.
- **Depends on:** CX-031, CX-041, CX-050, INT-008
- **Files/modules:** `Tests/EditMode/GroundTruth/**`, `Tests/PlayMode/EndToEnd/**`, `Scripts/Debug/TestInputDriver.cs`
- **3D assets:** final or blockout prefabs in `SCN_VerticalSlice`
- **Acceptance:** GT-001 reads the stall prefab bounds; GT-002 scans project + scene; GT-003…008 use domain + scene; E2E dine-in and takeaway reach `Completed`.
- **Tests:** itself; must run in batch mode under QA-001 scripts.
- **Parallel:** No · **Review before impl.:** No.

---

## 5. 3D asset production tasks

Every ART task follows the [AP] §84 template and delivers: `.blend` in `ArtSource/Blender/` (versioned `_v001…`), `.fbx` in `ArtSource/Export/`, textures in `ArtSource/Textures/`, and a turntable/screenshot next to the reference for PO comparison. Prompts: use the per-asset prompt and negative prompt in [AP] plus the generic template [AP] §64–65. Cleanup instructions: [AP] §66. Common acceptance = [AP] §74 Definition of Done (up to "prefab created", which is the INT task).

Phase **A** (blockout) = primitive, true scale, final pivot, no texture — can be done by the Blender artist in hours and unblocks INT tasks. Phase **B** = gameplay-ready mesh. Phase C (polish) is out of slice scope.

### ART-STALL-001 — Tram Chanh stall
- **Owner:** 3D (Blender artist; AI generation optional for B)
- **Objective:** modular stall `SM_Stall_Base`, `SM_Stall_Counter`, `SM_Stall_Frame`, `SM_Stall_Roof`, `SM_Stall_CasterWheel`, `SM_LEDStrip`, `SM_EdisonBulb`.
- **Depends on:** REV-005; B needs DEC-011 (counter openings for recessed topping station and ice bin)
- **Files/modules:** `ArtSource/**/Stall/*`, export → `Assets/TramChanh/Art/Models/Stall/`
- **3D assets:** itself ([AP] §10, §12–16)
- **Acceptance:** 1.8 × 0.8 m footprint, counter top 1.0 m, total ~2.2 m; old illuminated letters absent; equipment not merged; A-frame silhouette; 15–30k tris; 2K textures; pivot ground centre; [AP] §79 checklist.
- **Tests:** QA-011 (validator bounds + scale-test scene).
- **Parallel:** Yes · **Review before impl.:** A: No; B: Yes — DEC-011 layout.

### ART-BRAND-001 — New sign geometry
- **Owner:** 3D
- **Objective:** `SM_Sign_TramChanh_New` lightbox shape (no text geometry).
- **Depends on:** REV-005
- **Files/modules:** `Art/Models/Branding/`
- **3D assets:** [AP] §11
- **Acceptance:** horizontal box, softly rounded ends, white housing, mustard-orange top, strip area along lower front edge; flat rear; 500–2,000 tris; pivot rear centre; UVs laid out for a 2048×512 texture.
- **Tests:** QA-011.
- **Parallel:** Yes · **Review before impl.:** No.

### ART-BRAND-002 — New sign branding texture
- **Owner:** 3D (texture artist) with PO artwork
- **Objective:** `T_Sign_TramChanh_New_BaseColor` (+ Normal/Mask if needed) with exact "Trạm" (black) "Chanh" (mustard-orange) and the coloured strip.
- **Depends on:** PO supplies the real new-sign artwork/photo; ART-BRAND-001 UVs
- **Files/modules:** `ArtSource/Textures/Branding/`, `Art/Textures/`
- **3D assets:** texture only
- **Acceptance:** text and diacritics exactly match the PO artwork; never AI-generated text; ≥2048 px wide; PO sign-off.
- **Tests:** QA-011 visual check + PO approval.
- **Parallel:** Yes · **Review before impl.:** Yes — PO artwork.

### ART-CAKE-001 — Elmich grill
- **Owner:** 3D
- **Objective:** `SM_Grill_Base`, `SM_Grill_LowerPlate`, `SM_Grill_Lid`, `SM_Grill_UpperPlate`, `SM_Grill_Handle`, `SM_Grill_Display`.
- **Depends on:** REV-005
- **Files/modules:** `Art/Models/CakeStation/Grill/`
- **3D assets:** [AP] §27
- **Acceptance:** [AP] §80 checklist; lid separate with pivot exactly on the rear hinge axis; upper plate parented to lid; parallel ridges top and bottom; front red LED display area; 5–12k tris; 2K.
- **Tests:** QA-013 (lid rotation without deformation).
- **Parallel:** Yes · **Review before impl.:** No.

### ART-DRINK-001 — Red tea rack
- **Owner:** 3D
- **Objective:** `SM_RedTeaRack`.
- **Depends on:** REV-005
- **Files/modules:** `Art/Models/DrinkStation/`
- **3D assets:** [AP] §17
- **Acceptance:** red open-top molded plastic, slotted sides, sized for multiple pre-portioned bags (bag slot positions exported as empties `BagSlot_##`); 500–2,000 tris; bottom-centre pivot.
- **Tests:** QA-012.
- **Parallel:** Yes · **Review before impl.:** No.

### ART-DRINK-002 — Pre-portioned tea bag
- **Owner:** 3D
- **Objective:** `SM_TeaBag_PrePortioned` with `Bag_Closed`, `Bag_Open` meshes (or blendshape) and tea liquid; internal content slots for jelly/ice portions.
- **Depends on:** ART-DRINK-001 (must fit the rack)
- **Files/modules:** `Art/Models/DrinkStation/`
- **3D assets:** [AP] §18; reuse jelly/ice portion meshes from ART-DRINK-003/004
- **Acceptance:** [AP] §81 checklist; transparent plastic; no straw, no text; 300–1,500 tris; pivot at grip point.
- **Tests:** QA-012 (fits rack; open state; contents visible).
- **Parallel:** Yes · **Review before impl.:** No.

### ART-DRINK-003 — Topping station + jellies
- **Owner:** 3D
- **Objective:** `SM_ToppingStation` (StationFrame, CoconutJellyBin, LemonJellyBin, TransparentCover as separate meshes), `SM_Topping_CoconutJelly`, `SM_Topping_LemonJelly` (bin volume + small bag portion).
- **Depends on:** ART-STALL-001 (A) for recess size
- **Files/modules:** `Art/Models/DrinkStation/`, `Art/Models/Food/`
- **3D assets:** [AP] §19–21
- **Acceptance:** [AP] §82 checklist; fits 0.8 m counter depth; cover separate with hinge pivot; jelly as volume meshes.
- **Tests:** QA-012.
- **Parallel:** Yes · **Review before impl.:** No.

### ART-DRINK-004 — Ice bin + scoop
- **Owner:** 3D
- **Objective:** `SM_IceBin` (bin + separate `IceVolume`, optional lid), `SM_IceScoop`, small ice portion for the bag.
- **Depends on:** ART-STALL-001 (A)
- **Files/modules:** `Art/Models/DrinkStation/`
- **3D assets:** [AP] §22–23
- **Acceptance:** recessed bin fits counter; ice volume separate; scoop pivot at handle grip; scoop clip `AN_IceScoop_Scoop` keyframed or authored in Unity (INT-004 decides).
- **Tests:** QA-012.
- **Parallel:** Yes · **Review before impl.:** No.

### ART-DRINK-005 — Wipe cloth
- **Owner:** 3D
- **Objective:** `SM_WipeCloth`.
- **Depends on:** REV-005
- **Files/modules:** `Art/Models/DrinkStation/`
- **3D assets:** [AP] §26 (not in [AP] §73 slice list, but required for the wipe step; a blockout is sufficient for the slice)
- **Acceptance:** folded cloth, low-poly, bottom-centre pivot.
- **Tests:** QA-012.
- **Parallel:** Yes · **Review before impl.:** No.

### ART-CAKE-002 — Batter measure cup + batter
- **Owner:** 3D
- **Objective:** `SM_BatterMeasureCup`, `SM_BatterVolume` (fill level), pour stream/poured portion.
- **Depends on:** REV-005
- **Files/modules:** `Art/Models/CakeStation/`, `Art/Models/Food/`
- **3D assets:** [AP] §28–29
- **Acceptance:** transparent/semi-transparent cup with handle, grip pivot; batter volume can scale from 0 to full without visual artefacts; `PourPoint` empty at the lip.
- **Tests:** QA-013.
- **Parallel:** Yes · **Review before impl.:** No.

### ART-CAKE-003 — Spatula
- **Owner:** 3D
- **Objective:** `SM_Spatula_WoodHandle`.
- **Depends on:** REV-005
- **Files/modules:** `Art/Models/CakeStation/`
- **3D assets:** [AP] §30
- **Acceptance:** flat metal blade, short worn wooden handle; grip pivot; low-poly.
- **Tests:** QA-013.
- **Parallel:** Yes · **Review before impl.:** No.

### ART-CAKE-004 — Scissors
- **Owner:** 3D
- **Objective:** `SM_Scissors_RedGray` with two blade/handle halves as separate meshes, pivot at the screw.
- **Depends on:** REV-005
- **Files/modules:** `Art/Models/CakeStation/`
- **3D assets:** [AP] §31, §56 (animation-required)
- **Acceptance:** red and gray handles; halves open/close about the screw without intersection.
- **Tests:** QA-013.
- **Parallel:** Yes · **Review before impl.:** No.

### ART-CAKE-005 — Sauce bag
- **Owner:** 3D
- **Objective:** neutral `SM_SauceBag` (colour by material) for the three variants.
- **Depends on:** REV-005; DEC-008 decides which variant is used first
- **Files/modules:** `Art/Models/CakeStation/`
- **3D assets:** [AP] §32
- **Acceptance:** piping bag with twisted/clipped top and cut tip; one geometry for all flavours; grip pivot; `PourPoint` at tip.
- **Tests:** QA-013.
- **Parallel:** Yes · **Review before impl.:** No.

### ART-CAKE-006 — Cake states
- **Owner:** 3D
- **Objective:** `SM_Cake_Raw`, `SM_Cake_Cooked`, `SM_Cake_Cut`, `SM_Cake_Rolled` (+ roll animation `AN_Cake_RollVertical` or a rolling-sequence approach agreed with Codex).
- **Depends on:** ART-CAKE-001 (plate size)
- **Files/modules:** `Art/Models/Food/`, `Art/Animations/`
- **3D assets:** [AP] §33–36
- **Acceptance:** raw sheet fits the lower plate; cooked shows parallel dark grill lines matching the plate ridges; rolled shape = "long vertically rolled" per reference; shared UVs so `Doneness01` lerps raw → cooked.
- **Tests:** QA-013 + PO compares roll direction to reference.
- **Parallel:** Yes · **Review before impl.:** Yes — roll axis confirmed against reference photos (PO) before modelling `SM_Cake_Rolled`.

### ART-CAKE-007 — Wrapping paper + wrapped cake
- **Owner:** 3D
- **Objective:** `SM_CakeWrappingPaper`, `SM_Cake_Wrapped`.
- **Depends on:** ART-CAKE-006 (rolled dimensions)
- **Files/modules:** `Art/Models/Packaging/`, `Art/Models/Food/`
- **3D assets:** [AP] §37–38
- **Acceptance:** cream/white thin paper; wrapped cake easy to hold, grip pivot; no printed text.
- **Tests:** QA-013.
- **Parallel:** Yes · **Review before impl.:** No.

### ART-FURN-001 — Plastic stool
- **Owner:** 3D
- **Objective:** `SM_PlasticStool` (+ LOD1/LOD2 per [AP] §50, repeated furniture).
- **Depends on:** REV-005
- **Files/modules:** `Art/Models/Furniture/`
- **3D assets:** [AP] §39
- **Acceptance:** 0.28–0.32 × 0.28–0.32 × 0.25–0.30 m; 500–2,000 tris; ground-centre pivot.
- **Tests:** QA-014.
- **Parallel:** Yes · **Review before impl.:** No.

### ART-FURN-002 — Yellow crate table + stainless tray
- **Owner:** 3D
- **Objective:** `SM_YellowCrate` (upside-down crate) and `SM_StainlessTrayTabletop` (+ LODs).
- **Depends on:** REV-005
- **Files/modules:** `Art/Models/Furniture/`
- **3D assets:** [AP] §40–41
- **Acceptance:** practical crate proportions from reference; tray sits flush on crate; ground-centre pivot.
- **Tests:** QA-014.
- **Parallel:** Yes · **Review before impl.:** No.

Not produced in the slice (P1/P2 per [AP] §62): menus, snack rack/bags, pump bottles (static blockout only if visible), red cooler, plastic basket, paper bag, environment. Customer and vehicle have no spec → placeholders (DEC-010).

---

## 6. Unity integration tasks (owner: Codex)

### INT-001 — `SCN_AssetScaleTest`
- **Owner:** Codex
- **Objective:** test scene from [AP] §61: 1.7 m mannequin, 1 m cube, and slots for stall, stool, crate table, grill, tea rack (blockouts until real assets land).
- **Depends on:** CX-001, CX-005
- **Files/modules:** `Scenes/Test/SCN_AssetScaleTest.unity`, `Scripts/Editor/ScaleTest/**`
- **3D assets:** blockouts of the five listed assets
- **Acceptance:** scene opens with zero errors; reference objects at exact sizes; a measuring overlay shows the bounds of the selected object.
- **Tests:** validator run on the scene; AG screenshot.
- **Parallel:** Yes · **Review before impl.:** No.

### INT-002 — `PF_Stall_TramChanh`
- **Owner:** Codex
- **Objective:** A: blockout prefab with the full `ASSET_INTEGRATION.md` §5.1 hierarchy, compound colliders and all anchors at proposed positions. B: swap in ART-STALL-001 meshes without changing hierarchy or anchor names.
- **Depends on:** INT-001; B on ART-STALL-001 (B) and REV-007 / DEC-011
- **Files/modules:** `Prefabs/Stall/PF_Stall_TramChanh.prefab`, `Art/Models/Stall/**`, materials
- **3D assets:** ART-STALL-001
- **Acceptance:** GT-001 bounds; counter top 1.00 m; all 10 anchors present; no MeshCollider; equipment not part of the prefab meshes; lights are Unity Light components on `Lights`.
- **Tests:** QA-011; `GT_001_StallDimensions`.
- **Parallel:** Yes · **Review before impl.:** A: No; B: Yes — REV-007 layout.

### INT-003 — `PF_Sign_TramChanh_New`
- **Owner:** Codex
- **Objective:** import sign mesh + branding texture, nest under stall `Sign`.
- **Depends on:** INT-002, ART-BRAND-001, ART-BRAND-002
- **Files/modules:** `Prefabs/Stall/PF_Sign_TramChanh_New.prefab`, `Art/Models/Branding/**`, `MAT_Sign_TramChanh_New`
- **3D assets:** ART-BRAND-001/002
- **Acceptance:** rear-centre pivot; BoxCollider; texture crisp at gameplay distance; GT-002 passes; no other sign asset in the project.
- **Tests:** QA-011; `GT_002_OnlyNewSign`.
- **Parallel:** Yes · **Review before impl.:** No.

### INT-004 — Drink station prefabs
- **Owner:** Codex
- **Objective:** A: blockouts of `PF_RedTeaRack`, `PF_TeaBag_PrePortioned`, `PF_ToppingStation`, `PF_Topping_*`, `PF_IceBin`, `PF_IceScoop`, `PF_WipeCloth`, `PF_DrinkStation` with final hierarchy/anchors/colliders/layers (unblocks CX-031). B: swap in ART-DRINK-001…005 and author/import animation clips (`AN_TeaBag_Shake`, `AN_IceScoop_Scoop`, `AN_WipeCloth_Wipe`).
- **Depends on:** INT-002 (anchors); B on ART-DRINK-001…005
- **Files/modules:** `Prefabs/Workstations/**`, `Prefabs/Items/**`, `Art/Models/DrinkStation/**`, `Art/Animations/**`
- **3D assets:** ART-DRINK-001…005
- **Acceptance:** `ASSET_INTEGRATION.md` §5.3 table; recessed bins flush with the counter; bag fits rack slots; swapping A → B requires no script/serialized-reference change.
- **Tests:** QA-012; TC-DRINK-009 still passes after the swap.
- **Parallel:** Yes · **Review before impl.:** No.

### INT-005 — Cake station prefabs + grill animator
- **Owner:** Codex
- **Objective:** A: blockouts of `PF_Grill_Elmich` (with real `LidPivot` hinge behaviour), `PF_BatterMeasureCup`, `PF_BatterPortion`, `PF_Placeholder_BatterSource`, `PF_Spatula_WoodHandle`, `PF_Scissors_RedGray`, `PF_SauceBag` + variants, `PF_Cake_*`, `PF_CakeWrappingPaper`, `PF_CakeStation`. B: swap in ART-CAKE-001…007; grill Animator (`AN_Grill_LidOpen/Close`), display material, roll/cut/squeeze/wrap clips.
- **Depends on:** INT-002; B on ART-CAKE-001…007
- **Files/modules:** `Prefabs/Workstations/**`, `Prefabs/Items/**`, `Art/Models/CakeStation/**`, `Art/Models/Food/**`, `Art/Animations/**`
- **3D assets:** ART-CAKE-001…007
- **Acceptance:** `ASSET_INTEGRATION.md` §5.4 tables; lid opens/closes about the rear hinge with the upper plate following and no deformation; display shows runtime text; A → B swap without reference changes.
- **Tests:** QA-013; TC-CAKE-011 still passes after the swap.
- **Parallel:** Yes · **Review before impl.:** No.

### INT-006 — Customer area + placeholders
- **Owner:** Codex
- **Objective:** `PF_PlasticStool`, `PF_YellowCrateTable` (+ tray), `PF_Placeholder_Vehicle`, `PF_Placeholder_Customer` with `TableOrderPoint` / `VehicleOrderPoint` anchors (`SeatPoint`, `DeliveryPoint`, `CustomerWaitPoint`).
- **Depends on:** INT-001; ART-FURN-001/002 for B; DEC-010
- **Files/modules:** `Prefabs/CustomerArea/**`, `Prefabs/NPC/**`, `Art/Models/Furniture/**`
- **3D assets:** ART-FURN-001/002
- **Acceptance:** `ASSET_INTEGRATION.md` §5.5; stool dimensions in range; LODs on stool/crate.
- **Tests:** QA-014.
- **Parallel:** Yes · **Review before impl.:** No.

### INT-007 — `PF_ReadyCounterPoint`
- **Owner:** Codex
- **Objective:** prefab with `InteractionTrigger`, `DrinkPlacement`, `CakePlacement`, `OrderIndicator` at `ReadyCounterAnchor`.
- **Depends on:** INT-002, CX-022
- **Files/modules:** `Prefabs/Stall/PF_ReadyCounterPoint.prefab`
- **3D assets:** none (marker only, [AP] §49)
- **Acceptance:** reachable from both the stall side and the Lobby side of the counter; placed items snap to placement points.
- **Tests:** QA-011; covered by TC-ORDER-005 PlayMode variant.
- **Parallel:** Yes · **Review before impl.:** No.

### INT-008 — `SCN_VerticalSlice` composition
- **Owner:** Codex
- **Objective:** gameplay scene: stall with sign, drink + cake stations at anchors, ready counter, one table with stools, one vehicle point, customer spawn, player spawn, placeholder lighting (URP), NavMesh/waypoints, debug HUD; tune `ReachDistance` and hold durations (DEC-007 placeholders).
- **Depends on:** INT-002…INT-007, CX-031, CX-041, CX-050
- **Files/modules:** `Scenes/Gameplay/SCN_VerticalSlice.unity`, `ScriptableObjects/Balance/SO_Balance_Slice.asset`, lighting settings
- **3D assets:** all slice prefabs (blockout acceptable where B is not ready)
- **Acceptance:** both scenarios playable start to finish by a human; 60 FPS at 1080p on the target PC ([WF] §30); zero Console errors; no missing references.
- **Tests:** CX-060 E2E suite; QA-050.
- **Parallel:** No · **Review before impl.:** Yes — REV-007 layout.

---

## 7. Antigravity QA tasks

All QA tasks: read acceptance criteria → check git status → compile → run tests → inspect warnings → verify files/prefabs → check missing references → report **PASS/FAIL per criterion** with the smallest reproducible failure and logs ([WF] §24). Reports go to `QA/Reports/<task>-<date>.md`. QA never modifies gameplay code or architecture.

### QA-000 — Repo and docs check
- **Owner:** Antigravity (Gemini 3.5 Flash)
- **Objective:** verify branches (`main`, `develop`), worktree convention, `Docs/` files present and non-empty, report missing requirements.
- **Depends on:** —
- **Files/modules:** read-only; `QA/Reports/`
- **3D assets:** none
- **Acceptance:** the 7 docs + `Docs/Reference/*` present; git clean; any requirement gap listed.
- **Tests:** n/a · **Parallel:** Yes · **Review before impl.:** No.

### QA-001 — Batch automation
- **Owner:** Antigravity
- **Objective:** scripts to run Unity batch-mode compile, EditMode, PlayMode, `TramChanhAssetValidator`, and collect logs + JUnit/JSON into one report.
- **Depends on:** CX-001 (CX-005 for validator step)
- **Files/modules:** `Automation/**`, `BuildScripts/**`
- **3D assets:** none
- **Acceptance:** one command produces a PASS/FAIL summary; non-zero exit on any failure; works from a clean worktree.
- **Tests:** run against CX-001 smoke tests (PASS) and a deliberately broken branch (FAIL detected).
- **Parallel:** Yes · **Review before impl.:** No.

### QA-002 — Foundation validation (M0 / [WF] Phase 1)
- **Owner:** Antigravity
- **Depends on:** CX-001…CX-005, QA-001
- **Acceptance:** project compiles with zero errors; EditMode + PlayMode smoke green; asmdef graph matches `ARCHITECTURE.md` §4; no hard-coded scene references outside `SceneLoader`.
- **Files/modules:** read-only · **3D assets:** none · **Tests:** QA-001 suite · **Parallel:** No · **Review before impl.:** No.

### QA-010 — Interaction validation (M2)
- **Owner:** Antigravity
- **Depends on:** CX-010…CX-012
- **Acceptance:** TC-INT-001…008 PASS; interaction with the test cube works in `SCN_InteractionTest` ([WF] Phase 1 acceptance).
- **Files/modules:** read-only · **3D assets:** none · **Tests:** TC-INT-* · **Parallel:** Yes · **Review before impl.:** No.

### QA-011 … QA-014 — Asset validation
- **Owner:** Antigravity
- **Objective:** run [AP] §68 checks + `ASSET_INTEGRATION.md` §9.1 validator + visual check in `SCN_AssetScaleTest`, for: QA-011 stall + sign + ready point; QA-012 drink assets; QA-013 cake assets (incl. lid animation); QA-014 customer area.
- **Depends on:** INT-002/003/007, INT-004, INT-005, INT-006 respectively
- **Files/modules:** read-only; screenshots in `QA/Reports/`
- **3D assets:** the respective prefabs
- **Acceptance:** zero validator Errors; PASS for: prefab name, real-world scale, materials, textures, scripts, collider quality, pivot behaviour, animation hierarchy, interaction anchors, Console errors, prefab overrides; GT-001 (QA-011), GT-002 (QA-011).
- **Tests:** validator JSON + scale-scene screenshots.
- **Parallel:** Yes (each other) · **Review before impl.:** No.

### QA-020 — Order system validation (M3)
- **Owner:** Antigravity
- **Depends on:** CX-020…CX-025
- **Acceptance:** TC-ORDER-001…009 PASS; GT-004, GT-005, GT-006 PASS.
- **Files/modules:** read-only · **3D assets:** blockouts · **Tests:** EditMode + PlayMode order suites · **Parallel:** Yes · **Review before impl.:** No.

### QA-030 — Drink validation (M4)
- **Owner:** Antigravity
- **Depends on:** CX-031, INT-004
- **Acceptance:** [WF] Phase 4 list — (1) no tea measuring, (2) bag starts in red rack, (3) cannot Ready without shake, (4) cannot complete without wipe, (5) correct order marked Ready, (6) wrong delivery detected; GT-003, GT-007 PASS.
- **Files/modules:** read-only · **3D assets:** drink prefabs · **Tests:** TC-DRINK-001…009, GT-003, GT-007 · **Parallel:** Yes (with QA-040) · **Review before impl.:** No.

### QA-040 — Cake validation (M5)
- **Owner:** Antigravity
- **Depends on:** CX-041, INT-005
- **Acceptance:** [WF] Phase 5 list — wrong batter amount, undercooked, correct cook, overcooked, missing sauce, missing wrap, correct finished product; GT-008 PASS; grill lid hinge check.
- **Files/modules:** read-only · **3D assets:** cake prefabs · **Tests:** TC-CAKE-001…011, GT-008 · **Parallel:** Yes · **Review before impl.:** No.

### QA-050 — Vertical slice end-to-end
- **Owner:** Antigravity
- **Depends on:** INT-008, CX-060, QA-011…QA-040
- **Files/modules:** read-only; `QA/Reports/QA-050-*.md`
- **3D assets:** all slice prefabs
- **Acceptance:** dine-in E2E and takeaway E2E reach `Completed`; GT-001…GT-008 all PASS; [WF] §31 review gate clean (no compile/test failure, no missing reference, no old sign, no tea-measuring step, Lobby not bypassed, dine-in/takeaway distinguished, cake order correct); zero Console errors over a 10-minute scripted run; 60 FPS at 1080p on target PC.
- **Tests:** full QA-001 suite + manual play-through checklist.
- **Parallel:** No · **Review before impl.:** No.

---

## 8. Parallel lanes and worktrees

Concurrent worktrees that never touch the same files ([WF] §18–19):

| Lane | Worktree | Tasks | Owns |
|---|---|---|---|
| Core | `worktrees/core` | CX-002, CX-004 | `Scripts/Core/**` |
| Interaction | `worktrees/interaction` | CX-010…CX-012 | `Scripts/Interaction/**`, `Scripts/UI/Prompt/**` |
| Orders | `worktrees/order-system` | CX-020…CX-025 | `Scripts/Orders/**`, `Scripts/Stall/**`, `Scripts/Lobby/**`, `Scripts/UI/Orders/**` |
| Drinks | `worktrees/drink-system` | CX-030, CX-031, INT-004 | `Scripts/Drinks/**`, drink prefabs |
| Cakes | `worktrees/cake-system` | CX-040, CX-041, INT-005 | `Scripts/Cakes/**`, cake prefabs |
| Stall/Scene | `worktrees/stall-scene` | INT-001…003, INT-006…008 | `Prefabs/Stall/**`, `Prefabs/CustomerArea/**`, `Scenes/**` |
| Art | outside Unity | all ART-* | `ArtSource/**` |
| QA | `worktrees/qa` | QA-* | `Automation/**`, `QA/**`, `BuildScripts/**` |

Shared files that only one task may edit at a time: `ProjectSettings/TagManager.asset` (CX-001 only), `Packages/manifest.json` (CX-001; later changes need CC review), `SO_Balance_Slice.asset` (INT-008).

---

## 9. Not in this plan (deliberately)

Customer patience, money, multiple recipes, multiple simultaneous customers beyond tests, save/load, audio/VFX beyond hooks, snack display, menus as interactables, upgrades, employees, environment art — all P1/P2 per [WF] §28 and [AP] §62. They start only after REV-010.
