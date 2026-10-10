# ART INTEGRATION HANDOFF (MAIN-104)

**Audience:** Google Antigravity (art), lead, and Claude lanes. Coordination thread: GitHub issue #20.
**Baseline:** `wave/MAIN-001-integration` @ `248eac1`. Unity 6000.6.0f1. All facts were read from the prefab YAML and the C# in this tree. No Unity Editor was used, and no `.prefab`/`.mat`/`.asset`/`.unity` file was changed.
**Tool:** `python3 Automation/art_contract_audit.py [REPO_ROOT]`. It is read-only, uses only the Python 3 stdlib and needs no Unity. Exit code 0 means no FAIL (WARN = known gap). Exit code 1 means a contract was broken. `--dump <prefab>` prints the expanded hierarchy, `--only <label>` filters and `--verbose` also lists the passing checks.
**Related:** `Docs/ASSET_INTEGRATION.md` (asset specs and pivots, approved) and `Docs/ACCEL-01_CONTRACTS.md`. Where this handoff and the current prefabs disagree with ASSET_INTEGRATION §5, this file describes **what the code reads today**.

---

## 0. Rules of engagement

| Owner | Owns | Never touches |
|---|---|---|
| **Claude** (gameplay lanes) | Prefab roots, every `MonoBehaviour` (gameplay components, `InteractableRef`, `PlaceholderAsset`), colliders and their layers, `Anchors/*`, pivots driven by code (`LidPivot`, `CoverPivot`, `BatterLevel`, the animated `Visual` node of tools), Animator controllers/parameters, serialized references, the environment composition anchors | Meshes, materials, textures |
| **Antigravity** (art) | Meshes (`SM_*`, FBX), materials (`MAT_*`), textures, the stall/sign/equipment/furniture visual children, visual-only props in the environment | Components, colliders, anchors, the names marked **DO NOT RENAME**, root transforms |

The following count as a **Claude-side change** and go through issue #20 first: any change of hierarchy that moves a node marked DO NOT RENAME, any new or removed collider, any layer change, or any new `Visual` root on a gameplay prefab. Art can swap freely inside a visual child.

### Layers (`Scripts/Core/GroundTruth/TramChanhLayers.cs`)

| Index | Name | Meaning for art |
|---|---|---|
| 8 | Environment | stall, sign, furniture, environment props. Ignored by the interaction ray. |
| 9 | Interactable | only colliders that the player must be able to focus. `PlayerInteractor` raycasts **this layer only** (reach 1.8 m from the camera at 1.6 m), then calls `GetComponentInParent<InteractableRef>()`. A layer-9 collider without an `InteractableRef` above it **blocks** the ray and can never be focused. |
| 10 | Player | `PF_Player` |
| 11 | NPC | (customers, future) |
| 12 | HeldItem | set at runtime by `HeldItemView` on every node of a held item. All its colliders are disabled. |

Mesh nodes may keep layer 0/8, because layers only matter on nodes with colliders. Do not add colliders to art. The FBX importer must have *Generate Colliders* off (ASSET_INTEGRATION §4).

---

## 1. Requested name → actual prefab

| Requested | Status | Actual path(s) (`Assets/TramChanh/Prefabs/…`) | Variant role |
|---|---|---|---|
| PF_Stall_TramChanh | OK | `Stall/PF_Stall_TramChanh.prefab` | Visual + `StallAnchorSet`. Nested in the environment at `StallRoot/PF_Stall_TramChanh`. |
| PF_Sign_TramChanh_New | OK | `Stall/PF_Sign_TramChanh_New.prefab` | Visual-only. Nested in the stall at `Sign/`. |
| PF_RedTeaRack | OK (2 files) | `Workstations/PF_RedTeaRack.prefab` (**base, visual-only**) → `Workstations/DrinkWave/PF_RedTeaRack.prefab` (**gameplay variant**: `TeaRackController`, `InteractableRef`) | Put art in the **base**. The variant inherits it. |
| PF_TeaBag_PrePortioned | OK | `Items/PF_TeaBag_PrePortioned.prefab` | Gameplay held item |
| PF_ToppingStation | OK (2) | `Workstations/PF_ToppingStation.prefab` (base) → `Workstations/DrinkWave/PF_ToppingStation.prefab` (gameplay variant: `ToppingBin`×2, Animator) | Art in the base |
| PF_IceBin | OK (2) | `Workstations/PF_IceBin.prefab` (base) → `Workstations/DrinkWave/PF_IceBin.prefab` (gameplay variant: `IceBin`, nests `PF_IceScoop`) | Art in the base |
| PF_IceScoop | OK | `Tools/PF_IceScoop.prefab` | Animated station tool (`StationToolView`) |
| PF_Grill_Elmich | OK | `Workstations/PF_Grill_Elmich.prefab` (base, visual-only) | Nested in `PF_CakeStation`, which adds `CakeStationPoint` + trigger on its root |
| PF_BatterMeasureCup_500ml | OK | `ACCEL01/Cakes/PF_BatterMeasureCup_500ml.prefab` | Gameplay held item (`BatterMeasureCup`). Nested in `PF_CakeStation/BatterArea`. |
| PF_Spatula_WoodHandle | OK | `ACCEL01/Cakes/PF_Spatula_WoodHandle.prefab` | Visual tool (rotated by `PlaceholderFlipAction`) |
| PF_Scissors_RedGray | OK | `ACCEL01/Cakes/PF_Scissors_RedGray.prefab` | Visual tool (rotated by `CakeStation`) |
| PF_SauceBag | OK | `ACCEL01/Cakes/PF_SauceBag.prefab` | Visual tool. `PF_CakeStation` adds the Sauce `CakeStationPoint` + trigger on its nested root. The variants `PF_SauceBag_Mango/…` from ASSET_INTEGRATION §2 do **not exist yet**. |
| PF_CakeWrappingPaper | OK | `ACCEL01/Cakes/PF_CakeWrappingPaper.prefab` | Visual. Nested under `PF_CakeStation/WrappingArea`. |
| PF_PlasticStool | OK | `CustomerArea/PF_PlasticStool.prefab` | Visual-only furniture (4 instances in the environment) |
| PF_YellowCrateTable | OK (2) | `CustomerArea/PF_YellowCrateTable.prefab` (**visual**, nested in the environment at `TablePoint`/`CakeTablePoint`) and `Workstations/DrinkWave/PF_YellowCrateTable.prefab` (**gameplay point**: a prefab variant of the CustomerArea one, with `TableOrderPoint` + `InteractableRef` + root trigger, instantiated by the bootstrap with renderers hidden) | Art in `CustomerArea/` |
| PF_DrinkStation | OK | `Workstations/DrinkWave/PF_DrinkStation.prefab` | Gameplay group (nests the 4 DrinkWave variants + WipeArea) |
| PF_CakeStation | OK | `ACCEL01/Cakes/PF_CakeStation.prefab` | Gameplay group (nests the grill, cup, tools and wrapping paper) |
| PF_ReadyCounterPoint | OK (2) | `Stall/PF_ReadyCounterPoint.prefab` (**legacy blockout, no component, unused**) → `Workstations/DrinkWave/PF_ReadyCounterPoint.prefab` (**gameplay**: `ReadyCounterPoint`, `ReadyOrderPickupPoint`) | No mesh required ([AP] §49) |
| PF_Placeholder_VehiclePoint | OK | `Workstations/DrinkWave/PF_Placeholder_VehiclePoint.prefab` | Gameplay point. Renderers are hidden by the bootstrap. Vehicle art lives in the environment. |
| PF_Cake_Prepared | OK | `ACCEL01/Cakes/PF_Cake_Prepared.prefab` | Gameplay prepared item (spawned by `CakeStation._cakePrefab`) |
| PF_AccelRoadsideEnvironment | OK | `ACCEL01/Environment/PF_AccelRoadsideEnvironment.prefab` | Environment. Composition anchors are read by name. |
| PF_Player | OK | `NPC/PF_Player.prefab` | Player. `HeldItemView` + `PlayerCamera/HandSocket/HoldAnchor` are **added in `SCN_TramChanh_Main`**, not in the prefab. |

No requested prefab is MISSING and none has a differing name. Extra related prefab: `Tools/PF_WipeCloth.prefab`, which is nested in `PF_DrinkStation/WipeArea`. Spec names that do not exist yet: `PF_SauceBag_{Mango,Chocolate,Cheese}`, `PF_Cake_{Raw,Cooked,Cut,Rolled,Wrapped}` (one `PF_Cake_Prepared` with state children is used instead), `PF_LEDStrip`, `PF_EdisonBulb`, `PF_Stall_CasterWheel`, `PF_StainlessTrayTabletop` and `PF_Placeholder_Customer`.

**What the main scene uses** (`SCN_TramChanh_Main` → `TramChanhMainBootstrap`): `_environmentPrefab` = PF_AccelRoadsideEnvironment, `_drinkStationPrefab` = DrinkWave/PF_DrinkStation, `_cakeStationPrefab` = ACCEL01/Cakes/PF_CakeStation, `_tablePointPrefab` = DrinkWave/PF_YellowCrateTable, `_vehiclePointPrefab` = DrinkWave/PF_Placeholder_VehiclePoint, and `_cakeStationLocalPosition` = (0.39, 1, −0.08).

---

## 2. Target structure (all gameplay prefabs)

```text
PF_<Name>  (Prefab Root: identity scale, pivot per ASSET_INTEGRATION §5)   ← Claude
├── [gameplay components on the root or on a named interaction child]      ← Claude
├── Anchors/                                                              ← Claude  (InteractionPoint, HandGrip, PlacementPoint, PourPoint, …)
├── Colliders/  or a trigger BoxCollider on the root / interaction child   ← Claude  (layer 9 for focus, layer 8 for physics)
├── <code-driven pivots>  e.g. LidPivot, CoverPivot, BatterLevel           ← Claude  (art nests UNDER them)
└── Visual/  (identity local transform, no colliders, no scripts)          ← Antigravity
    └── SM_* meshes + MAT_* materials (any internal hierarchy)
```

Art is replaceable only when no script or collider lives on a mesh node. **Today most equipment prefabs put the collider on the mesh node itself** (for example `Rack/Side`, `Bin/Bottom`, `SM_Scissors_RedGray`). If such a mesh node is deleted, its collider goes with it. §5 lists the Claude-side restructure that is proposed so art can be swapped without touching colliders. Until that lands, use the **"keep the node, swap the mesh"** procedure in §6.

---

## 3. Environment anchor contract (`PF_AccelRoadsideEnvironment`)

| Anchor (direct child of the root, scale 1) | Local position today | Read by | Contract |
|---|---|---|---|
| `StallRoot` | (0, 0, 0) | `TramChanhMainBootstrap.RequireAnchor` | Must contain the canonical nested `PF_Stall_TramChanh`, whose root carries `StallAnchorSet`. **Both stations are placed in StallAnchorSet space** (the stall root transform). |
| `PlayerSpawn` | (−1.5, 0, 2.7) | bootstrap (teleports the player, yaw only) | Clear body space above solid ground (ACCEL_SCENE_003) |
| `TablePoint` | (−2.1, 0, 1.7) | bootstrap → instantiates DrinkWave/PF_YellowCrateTable here (drink table, id 21) | Visual table art = the nested `TablePoint/PF_YellowCrateTable` (must keep `DeliveryPoint`) |
| `CakeTablePoint` | (−3.35, 0, 0.2) | bootstrap → second table point (cake table, id 23) | Same as TablePoint |
| `VehiclePoint` | (2.6, 0, 2.5) | bootstrap → instantiates PF_Placeholder_VehiclePoint here (id 22) | Must keep the children `GenericVehicleVisual` and `TakeawayCustomer` (tests). Any vehicle model goes under `GenericVehicleVisual`. |
| `LobbyPosition` | (−1.5, 0, 1.5) | ACCEL contract + `AccelRoadsideSceneTests` (not read by the bootstrap today) | Clear body space |
| `ReadyHandoff` | (0, 1, 0.65) | ACCEL contract + tests (not read by the bootstrap today) | Marks the customer-side handoff in front of the Ready counter |

Lookup is `Environment.transform.Find(name)` with a deep-search fallback, so names must be unique. Tests require them as **direct children** with scale 1. Placement uses the anchor's **position and rotation**: rotate the anchor to turn a table, and never scale it.

Runtime behaviour the art must expect:
- **Renderers of the gameplay point prefabs are hidden.** `PlacePoint` disables every `Renderer` on the instantiated DrinkWave/PF_YellowCrateTable and PF_Placeholder_VehiclePoint, because the environment already shows the furniture or vehicle at that anchor. Table and vehicle art therefore belongs in the **environment** (or in `CustomerArea/PF_YellowCrateTable`), never in the gameplay point prefabs. Colliders of the point prefabs stay active.
- **Children of the stall anchors are deactivated.** `HideStallEquipment` calls `SetActive(false)` on every child of the stall anchors TeaRack, Topping, IceBin, WipeArea, ReadyCounter, Grill, BatterArea, RollArea, Sauce and Wrap. Never parent art under `Anchors/*Anchor` in the stall or the environment. It will vanish in Play Mode.
- Environment `Camera`/`AudioListener` components are disabled at runtime. Environment colliders must be on layer 8.
- **Cake station stall-local offset:** `PF_CakeStation` is placed at `anchors.TransformPoint(0.39, 1, −0.08)` with the stall rotation (`_cakeStationLocalPosition`, serialized in the scene). `PF_DrinkStation` is placed at the stall root (0, 0, 0). Its children carry stall-local offsets. **Moving a `StallAnchor` does not move any station today.** The anchors only drive which equipment gets hidden.

Resulting stall-local positions (stall +Z = customer/front side; counter top y = 1.0):

| Station part | Stall-local position | Nearest StallAnchor (position) |
|---|---|---|
| Drink: PF_RedTeaRack | (−0.72, 1.00, −0.13) | TeaRackAnchor (−0.75, 1, −0.15) |
| Drink: PF_ToppingStation (recessed, bins top flush at 1.00) | (−0.41, 0.91, −0.13) | ToppingAnchor (−0.55, 1, −0.15) |
| Drink: PF_IceBin (recessed, top at 1.00) | (−0.10, 0.82, −0.13) | IceBinAnchor (−0.35, 1, −0.15) |
| Drink: WipeArea | (−0.15, 1.00, −0.15) | WipeAreaAnchor (−0.15, 1, −0.15) |
| Drink: PF_ReadyCounterPoint (LobbyPickup +0.3 z) | (0, 1.00, 0.25) | ReadyCounterAnchor (0, 1, 0.3) |
| Cake: BatterArea / cup | (0.07, 1.00, −0.15) | BatterAreaAnchor (0.15, 1, −0.15) |
| Cake: BatterSource | (0.07, 1.04, 0.06) | — |
| Cake: PF_Grill_Elmich | (0.35, 1.00, −0.05) | GrillAnchor (0.35, 1, −0.15) |
| Cake: RollArea | (0.64, 1.015, −0.15) | RollAreaAnchor (0.52, 1, −0.15) |
| Cake: WrappingArea | (0.64, 1.02, −0.36) | WrapAnchor (0.8, 1, −0.15) |
| Cake: PF_SauceBag | (0.82, 1.04, −0.06) | SauceAnchor (0.67, 1, −0.15) |
| Cake: PF_Spatula_WoodHandle / PF_Scissors_RedGray | (0.55, 1.02, 0.07) / (0.74, 1.02, 0.07) | — |

Art implications: the stall counter mesh (`SM_Stall_Counter`, y 0.96–1.00) and `SM_Stall_Base` are solid today. The recessed topping station (x −0.57…−0.25, z −0.27…0.01) and ice bin (x −0.22…0.02, z −0.27…0.01) sit **inside** them, so the final counter art needs openings at those footprints (DEC-011). Layout gap (Claude-side): the WipeArea trigger (x −0.225…−0.075, y 1.0) lies over the ice-bin opening and partly shadows the IceBin trigger.

---

## 4. Per-prefab contracts

Notation: **[DNR]** = DO NOT RENAME and DO NOT MOVE OUT OF ITS PARENT, because code, a serialized reference, an animation clip or a test reads it. Paths are relative to the prefab root. "Real size" values are the current blockout renderer bounds (W × D × H, metres, 1 unit = 1 m). Final art must match the real object ([AP] specs). Values marked GT are ground truth.

### 4.1 PF_Stall_TramChanh — `Stall/PF_Stall_TramChanh.prefab`

- **Real size (GT-001, `Scripts/Core/GroundTruth/StallDimensions.cs`):** W **1.80** × D **0.80** m, counter top **1.00** m, counter→roof 1.20, total **≈ 2.20** m. Tolerances: ±0.02 W/D, ±0.01 counter, ±0.05 height. Root scale 1, pivot at ground centre, layer 8.
- **Current hierarchy:** root [`PlaceholderAsset`, `StallAnchorSet`] → `Structure/SM_Stall_Base`, `Counter/SM_Stall_Counter`, `Frame/SM_Stall_Frame_Post0..3`, `Roof/SM_Stall_Roof`, `Wheels/SM_Stall_CasterWheel0..3` (cylinders, rotated), `Sign/PF_Sign_TramChanh_New` (nested, at (0, 2, 0.28)), `Lights` (empty), `Colliders/{Body, Post0..3, Roof}` (BoxColliders on empty nodes, layer 8), `Anchors/{10 × *Anchor}` (`StallAnchor` each).
- **Load-bearing:** [DNR] `Structure`, `Counter`, `Frame`, `Roof`, `Wheels`, `Sign`, `Sign/PF_Sign_TramChanh_New`, `Anchors`, `Colliders`. The GT-001 tests compute bounds over exactly these five groups: `Structure/Counter/Frame/Roof/Wheels` (renderer bounds 1.80 × 0.80 × 2.20, min y 0) and `Counter` (renderer top = 1.000 ± 0.001 in `AccelRoadsideSceneTests`). Anchors: ids 0..9 must all exist (`StallAnchorSet` looks them up by id; names are informational). Canonical names: TeaRackAnchor, ToppingAnchor, IceBinAnchor, WipeAreaAnchor, GrillAnchor, BatterAreaAnchor, RollAreaAnchor, SauceAnchor, WrapAnchor, ReadyCounterAnchor. All stay `PositionConfirmed = false` until DEC-011.
- **Gap vs target:** the structure already matches the target (colliders are separate from meshes). No `Visual` root is needed. Each group *is* the visual root for its part.
- **Colliders (Claude):** `Colliders/Body` 1.8 × 0.9 × 0.8 (y 0.10–1.00), 4 posts 0.05 × 1.16 × 0.05, `Colliders/Roof` 1.8 × 0.04 × 0.8 at y 2.18. All on layer 8, so a stall collider never blocks the interaction ray.
- **Art (Antigravity):** replace the `SM_*` meshes inside each group. Anything with a renderer must stay inside the five bounded groups and inside the GT-001 envelope. Lights/LED/bulbs go in `Lights` (outside the bounded groups). Counter openings for the recessed stations are needed (§3). **Never** put art under `Anchors/*`.
- **Moving parts / animation:** none.

### 4.2 PF_Sign_TramChanh_New — `Stall/PF_Sign_TramChanh_New.prefab`

- **Size:** 1.70 × 0.12 × 0.30 m blockout (size `[Tbd]`, DEC-017). Pivot at rear centre (z 0 = rear face, mesh extends +z). Layer 8. Root `BoxCollider` 1.7 × 0.3 × 0.12, centre z 0.06 (Claude).
- **Hierarchy:** root [`PlaceholderAsset`, BoxCollider] → `SM_Sign_TramChanh_New` (`MAT_Sign_TramChanh_New`), `SM_Sign_TramChanh_New_TopCap` (`MAT_Placeholder_SignTop`). In the environment, the nested instance adds `BrandingPlaceholder/{BrandName, BrandSubline}` (TextMesh).
- **Load-bearing:** [DNR] the prefab name `PF_Sign_TramChanh_New` (exactly one in the environment, GT-002). [DNR] `BrandingPlaceholder` in the environment instance, plus one TextMesh with the text "Trạm Chanh" (`GT_002` test). These can only be removed once the final branding texture replaces them **and** the test is updated (Claude-side). Keep the `MAT_Sign_TramChanh_New` slot. No other `PF_Sign_*`/`SM_Sign_*` asset may exist. Never reintroduce the old illuminated letters (names containing `OldSign`/`IlluminatedLetters` fail).
- **Gap:** none structurally. Text goes in the texture only ([AP] §72).

### 4.3 PF_RedTeaRack — base `Workstations/PF_RedTeaRack.prefab`, gameplay `Workstations/DrinkWave/PF_RedTeaRack.prefab`

- **Size:** 0.24 × 0.22 × 0.22 m. Pivot at bottom centre. Layer 9 (all nodes).
- **Hierarchy (base):** root → `BagSlots/{Slot01 (−0.05, 0.03, 0), Slot02 (0.05, 0.03, 0)}`, `InteractionPoint` (0, 0.22, −0.11), `OutputPoint` (0, 0.24, 0), `Rack/{Side, Side, Bottom, End, End}` (cube meshes, **each with a BoxCollider**, layer 9). Variant adds on the root: `PlaceholderAsset`, `TeaRackController`, `InteractableRef`.
- **Serialized refs:** `TeaRackController._interactionPoint → InteractionPoint`, `_bagSlots → [BagSlots/Slot01, BagSlots/Slot02]`, `_bagPrefab → PF_TeaBag_PrePortioned`, `InteractableRef._behaviour → root`. Bags are instantiated under a slot at `−bag.PlacementPoint.localPosition`, so the bag's `Anchors/PlacementPoint` (bag bottom) sits on the slot.
- **Load-bearing:** [DNR] `BagSlots/Slot01`, `BagSlots/Slot02`, `InteractionPoint` (variant overrides target them by fileID; renaming breaks nothing at runtime, but deleting or recreating them breaks the refs). `OutputPoint` is spec-only.
- **Colliders:** **focus depends on the `Rack/*` mesh colliders** (there is no root trigger). If art deletes `Rack/*`, the rack becomes unfocusable.
- **Gap vs target:** no `Visual` root. Colliders sit on mesh nodes. Proposed Claude change (#20): add a root trigger BoxCollider (≈ 0.24 × 0.22 × 0.22, layer 9), move the mesh colliders off, and add `Visual/`.
- **Moving parts:** none (visible bag count = number of bag instances).

### 4.4 PF_TeaBag_PrePortioned — `Items/PF_TeaBag_PrePortioned.prefab`

- **Size:** 0.07 × 0.06 × 0.18 m. **Pivot at the grip (top)**. The bag hangs to y −0.18. Root layer 8 (layer 12 while held). Root `BoxCollider` 0.07 × 0.18 × 0.06, centre (0, −0.09, 0) (Claude).
- **Components (root):** `PlaceholderAsset`, `TeaBagItem`, `TeaBagStateView`, `Animator` (`AC_TeaBag_Shake`, bool `Shaking`).
- **Hierarchy / refs:** `Anchors/HandGrip` (0, 0, 0) ← `_handGrip`. `Anchors/PlacementPoint` (0, −0.18, 0) ← `_placementPoint` and `ReadyCounterPoint`'s `Find("Anchors/PlacementPoint")`. `Visual/Bag_Closed` ← `_bagClosed`. `Visual/Bag_Open` ← `_bagOpen`. `Visual/Contents/CoconutJelly` ← `_coconutJelly`. `Visual/Contents/LemonJelly` ← `_lemonJelly`. `Visual/Contents/Ice` ← `_ice`. `Visual/Condensation` ← `_condensation`. `Visual/TeaLiquid`.
- **Load-bearing [DNR]:** every path above. `TeaBagStateView.Apply` toggles them with `SetActive`: closed when state < Opened, open when ≥ Opened, each topping when its step is reached, condensation only when Shaken. `Visual/Bag_Closed` and `Visual/TeaLiquid` must carry a **Renderer on the node itself** (`TeaRackPickupAssetTests`). Swap the mesh on that node; do not move it into a child.
- **Animation:** `AN_TeaBag_Shake` drives `Visual` local euler z 0 → 8° → 0 while `Shaking` is true (Idle clip = 0). `Visual` must keep an identity local pose, and the art goes under it.
- **Gap vs target:** already close to the target (the `Visual` root exists). The `Condensation` node is empty, so art can supply the condensation mesh or VFX there.

### 4.5 PF_ToppingStation — base `Workstations/PF_ToppingStation.prefab`, gameplay `Workstations/DrinkWave/PF_ToppingStation.prefab`

- **Size:** 0.32 × 0.285 × 0.355 m (lid open). Recessed. Placed so the bin rims are flush at y 1.00. Layer 9.
- **Hierarchy:** root → `StationFrame` (mesh + collider), `CoverPivot` (0, 0.09, 0.14) → `TransparentCover` (mesh + collider), `LemonJellyBin` and `CoconutJellyBin` (each `Bin/{Bottom, End, End, Side, Side}` meshes with colliders + `InteractionPoint` (0, 0.07, 0)). Variant adds `Animator` (`AC_Topping_Cover`, trigger `Action`) + `StationToolView` on the root, and on each bin `ToppingBin` + `InteractableRef` + trigger BoxCollider 0.14 × 0.07 × 0.26.
- **Refs:** `ToppingBin._interactionPoint → <Bin>/InteractionPoint`. Each bin's `_onAdded` fires `StationToolView.PlayOnce` (cover animation).
- **Load-bearing [DNR]:** `CoverPivot` (animation binding), `LemonJellyBin`, `CoconutJellyBin`, `*/InteractionPoint` (and the test `GameplayBlockoutTests` uses `CoconutJellyBin`/`LemonJellyBin`/`Bin`).
- **Animation:** `AN_Topping_Cover` drives `CoverPivot` local euler x 70° → 90° → 70°. Idle holds 70°, so **the cover rests open at 70°**. `CoverPivot` sits on the hinge (rear edge). The cover art goes under `CoverPivot`, offset so the hinge stays at the pivot.
- **Gap:** `StationFrame` and `CoverPivot/TransparentCover` have layer-9 colliders with no `InteractableRef`, so they block the ray (WARN). Colliders sit on mesh nodes and there is no `Visual` root. Proposed (#20): move the frame/cover colliders to layer 8 or delete them, keep only the bin triggers, and add `Visual/` per bin.

### 4.6 PF_IceBin — base `Workstations/PF_IceBin.prefab`, gameplay `Workstations/DrinkWave/PF_IceBin.prefab`

- **Size:** 0.24 × 0.28 × 0.18 m. Recessed (top at 1.00). Layer 9.
- **Hierarchy:** root → `IceVolume` (separate mesh + collider), `ScoopRestPoint` (0.09, 0.18, 0), `Bin/{Bottom, Side, Side, End, End}` (meshes + colliders), `InteractionPoint` (0, 0.18, 0). Variant adds `IceBin` + `InteractableRef` + trigger 0.24 × 0.18 × 0.28 (centre y 0.09) on the root, and nests `PF_IceScoop` under `ScoopRestPoint`.
- **Refs:** `IceBin._interactionPoint → InteractionPoint`. `_onScooped` fires `PF_IceScoop.StationToolView.PlayOnce`.
- **Load-bearing [DNR]:** `InteractionPoint`, `ScoopRestPoint` (scoop parent), `Bin` (test bounds: the bin top must stay at y 1.00 when placed), `IceVolume` (spec: separate mesh).
- **Gap:** colliders on the bin meshes, no `Visual`. Proposed (#20): the root trigger already exists, so remove the mesh colliders or move them to layer 8, then add `Visual/`.

### 4.7 PF_IceScoop — `Tools/PF_IceScoop.prefab`  (and PF_WipeCloth — `Tools/PF_WipeCloth.prefab`)

- **Size:** scoop 0.06 × 0.10 × 0.025 m. Cloth 0.15 × 0.15 × 0.01. No colliders (station tools). Layer 0.
- **Components (root):** `PlaceholderAsset`, `Animator` (`AC_Ice_Scoop`: trigger `Action`. `AC_Cloth_Wipe`: bool `Active`), `StationToolView` (`[RequireComponent(Animator)]`).
- **Load-bearing [DNR]:** `Visual`. `AN_Ice_Scoop` and `AN_Cloth_Wipe` animate `Visual` localPosition (0,0,0) → (0.05,0,0) → 0. The clips overwrite `Visual`'s own position, so `Visual` must stay at (0,0,0) with the art nested **under** it. (Today the cube mesh is on `Visual` itself, scaled.) The spec `HandGrip` anchor is absent. That is fine, because these tools are never held.
- **Gap:** move the mesh into a child of `Visual` (art-side, allowed) and reset `Visual`'s scale to 1 in the same change. Scale is not animated, but a scaled `Visual` distorts the art.

### 4.8 PF_Grill_Elmich — `Workstations/PF_Grill_Elmich.prefab` (nested in PF_CakeStation)

- **Size:** 0.32 × 0.315 × 0.19 m (lid closed). Pivot at bottom centre. Layer 9.
- **Hierarchy:** root → `Base` (mesh + collider), `LowerPlate` (mesh + collider), `LidPivot` (0, 0.12, **0.14** = rear hinge axis) → `UpperPlate`, `Lid`, `Handle` (meshes + colliders), `Display` (mesh only, `MAT_Placeholder_Red`), `Anchors/{CakePlacementPoint (0, 0.12, 0), SpatulaPoint, AudioPoint, InteractionPoint (0, 0.12, −0.14)}`. In PF_CakeStation the nested root gains `PlaceholderAsset`, `CakeStationPoint` (Grill, id 1103) and a trigger BoxCollider 0.14 × 0.1 × 0.14.
- **Refs (from PF_CakeStation):** `CakeStation._lidPivot → PF_Grill_Elmich/LidPivot`. `_cakePlacement → PF_Grill_Elmich/Anchors/CakePlacementPoint`. The cake is instantiated **under** `CakePlacementPoint` at local zero.
- **Load-bearing [DNR]:** `LidPivot`, `LidPivot/Lid` (must keep a BoxCollider: GameplayBlockoutTests), `Base` (its renderer bounds max z must equal `LidPivot.z`), `Anchors/CakePlacementPoint`, `Anchors/InteractionPoint`. `CakeSavedAssetTests` also requires the saved `_lidPivot.localPosition` to equal the canonical one, so never move `LidPivot` in the grill prefab without coordination.
- **Moving parts:** `CakeStation.Advance` sets `LidPivot.localRotation = closedRotation × Euler(−_openLidDegrees, 0, 0)` while the grill is Open (`_openLidDegrees` = 70 DEV seed, DEC-013; `_preheatSeconds` = 1 DEV seed, DEC-007). The rotation is code-driven with **no Animator**. The lid, upper plate and handle art must be children of `LidPivot` and must hinge correctly around its local X axis at the rear edge. The lower plate stays fixed.
- **Gap:** colliders on meshes, no `Visual`. Proposed (#20): `Visual/{Base, LowerPlate, Display}` + `LidPivot/Visual/{Lid, UpperPlate, Handle}`, with colliders moved to `Colliders/` and `LidPivot/LidCollider`. The `LidPivot/Lid` test must be updated together with this change (Claude-side).

### 4.9 PF_BatterMeasureCup_500ml — `ACCEL01/Cakes/PF_BatterMeasureCup_500ml.prefab`

- **Size:** 0.094 × 0.094 × 0.113 m (real cup: 500 ml, clear plastic, handle, markings). Pivot at the bottom centre today. The spec says grip/handle.
- **Components (root):** `BatterMeasureCup` (`_handGrip → Anchors/HandGrip`, `_liquid → BatterLevel`, `_definition → SO_MeasureCup_500ml_DEV_TBD`). In PF_CakeStation, `_home → BatterArea`.
- **Hierarchy:** `Cup_Base`, `Cup_Left`, `Cup_Right`, `Cup_Back` (meshes + colliders, layer 9), `Graduation_100…500` (meshes + colliders), `BatterLevel` (inactive mesh + collider), `Anchors/{HandGrip, PourPoint, PlacementPoint}` (all at the origin).
- **Load-bearing [DNR]:** `Anchors/HandGrip` (HeldItemView aligns the grip to the camera `HoldAnchor`, and CakeSavedAssetTests checks it), `BatterLevel`.
- **Moving parts:** `ApplyLevel` overwrites `BatterLevel.localScale.y = max(0.001, 0.1 × level)` and `localPosition.y = 0.005 + 0.05 × level`, and toggles it active when level > 0. The liquid mesh must be a **unit-height (1.0) volume centred on its pivot, at x/z scale ≈ inner diameter**. Put it on `BatterLevel` itself, or as a child with y-scale 1 (the parent y-scale does the work). `ReturnHome` reparents the cup to `BatterArea` with its saved local pose and sets **every child to layer 9** (all child colliders re-enabled).
- **Gaps:** **GAMEPLAY (Claude): no `InteractableRef`, so the player cannot focus or pick up the cup in Play Mode** (tests call `Cup.Execute` directly, so they pass). `HandGrip` sits at the cup bottom, not on the handle. Markings are separate cube meshes with colliders; the spec wants a texture. No `Visual` root.

### 4.10 PF_Spatula_WoodHandle / PF_Scissors_RedGray / PF_SauceBag / PF_CakeWrappingPaper — `ACCEL01/Cakes/`

| Prefab | Size (blockout) | Hierarchy | Load-bearing | Code-driven motion |
|---|---|---|---|---|
| PF_Spatula_WoodHandle | 0.03 × 0.16 × 0.015 | `Anchors/{InteractionPoint, HandGrip, PlacementPoint}`, `SM_Spatula_WoodHandle` (mesh + collider, layer 9) | [DNR] the prefab root (`PlaceholderFlipAction._spatula`), `Anchors/HandGrip` (test) | On flip: `localRotation = Euler(0, 0, −15)` (**absolute**, not relative to rest). Keep the root rest rotation at identity. |
| PF_Scissors_RedGray | 0.06 × 0.14 × 0.015 | `SM_Scissors_RedGray` (mesh + collider), `Anchors/{PlacementPoint, HandGrip, InteractionPoint}` | [DNR] root (`CakeStation._scissors`), `Anchors/HandGrip` | While holding Cut: `localRotation = rest × Euler(0, 25°, 0)`, back to rest on release. Spec: blades as separate meshes pivoting at the screw (an optional future `AN_Scissors_Cut`). |
| PF_SauceBag | 0.055 × 0.055 × 0.12 | `SM_SauceBag` (mesh + collider), `Anchors/{HandGrip, PlacementPoint, InteractionPoint}`. In PF_CakeStation the root gains `CakeStationPoint` (Sauce, id 1105, `_sauce = DEV_TBD`) + trigger 0.14 × 0.1 × 0.14 | [DNR] root (`CakeStation._sauceBag`), `Anchors/HandGrip` | While holding Sauce: `localRotation = rest × Euler(−25°, 0, 0)` |
| PF_CakeWrappingPaper | 0.21 × 0.18 × 0.002 | `Anchors/{PlacementPoint, HandGrip, InteractionPoint}`, `SM_CakeWrappingPaper` (mesh + collider) | [DNR] `Anchors/HandGrip`. The parent `WrappingArea` in PF_CakeStation carries the Wrap point | none |

Gaps: all anchors sit at the origin (placeholder). `PF_SauceBag` has no `Anchors/PourPoint` (spec). The mesh colliders on `SM_*` are on layer 9 with no `InteractableRef` in the standalone prefab. In the station, the spatula, scissors and paper meshes sit outside any point and block rays. No `Visual` root. Art can replace the `SM_*` mesh on the same node today.

### 4.11 PF_Cake_Prepared — `ACCEL01/Cakes/PF_Cake_Prepared.prefab`

- **Size:** flat sheet 0.20 × 0.15 × 0.006 m. Rolled 0.04 × 0.035 × 0.15 (vertical). Pivot at bottom centre. Spawned under the grill's `CakePlacementPoint`, moved to `RollArea` on flip, held when wrapped, then placed on the Ready counter.
- **Components:** `CakeItem` (`_handGrip → Anchors/HandGrip`, `_flatVisual → SM_Cake_Flat`, `_rolledVisual → SM_Cake_RolledVertical`, `_paperVisual → SM_Cake_WrappingPaper`).
- **Load-bearing [DNR]:** `Anchors/HandGrip`, `Anchors/PlacementPoint` (`ReadyCounterPoint` uses `Find("Anchors/PlacementPoint")` and blocks with `ready.item_missing_placement_point` if it is missing), `SM_Cake_Flat`, `SM_Cake_RolledVertical` (also `CakeStationFlowTests` `Find`), `SM_Cake_WrappingPaper`. These must be **direct children of the root**.
- **States:** `CakeItem.Apply`: flat visible until Rolled; rolled visible from Rolled on; paper visible from Wrapped (not Ruined). Cut, Sauced and burnt states have no visual today. Spec wants `PF_Cake_Cut`, a sauce layer and a burnt tint (§8 of ASSET_INTEGRATION).
- **Test constraint:** `CakeSavedAssetTests` asserts `SM_Cake_RolledVertical.localScale.y > localScale.x`. If the art keeps that node's scale at 1, the test fails. Keep the blockout scale on the node and parent the art under it with an inverse scale, or have Claude change the test to use bounds (#20).
- **Colliders:** each state mesh has a BoxCollider on layer 9 (disabled while held, re-enabled and set to layer 8 when placed on Ready). A root collider is proposed instead.

### 4.12 PF_CakeStation — `ACCEL01/Cakes/PF_CakeStation.prefab`

- **Size:** ≈ 0.84 × 0.60 × 0.21 m footprint on the counter. Stall-local origin (0.39, 1, −0.08) (§3). Root layer 0.
- **Root components:** `CakeStation` (`_cup → BatterArea/PF_BatterMeasureCup_500ml`, `_cakePrefab → PF_Cake_Prepared`, `_cakePlacement → PF_Grill_Elmich/Anchors/CakePlacementPoint`, `_lidPivot → PF_Grill_Elmich/LidPivot`, `_scissors → PF_Scissors_RedGray`, `_sauceBag → PF_SauceBag`, `_wrappingArea → WrappingArea`, `_flipAction → root`), `PlaceholderFlipAction` (`_rollArea → RollArea`, `_spatula → PF_Spatula_WoodHandle`).
- **Interaction points** (`CakeStationPoint`: action, id, own trigger 0.14 × 0.1 × 0.14 on layer 9, `_interactionPoint` = itself): `BatterSource` Fill 1102 (+ `BatterContainer` mesh), `PF_Grill_Elmich` Grill 1103, `RollArea` Cut/Roll 1104 (+ `RollBoard`), `PF_SauceBag` Sauce 1105, `WrappingArea` Wrap 1106 (+ nested `PF_CakeWrappingPaper`). Cup = 1101.
- **Load-bearing [DNR]:** `BatterArea`, `BatterSource`, `PF_Grill_Elmich`, `RollArea`, `WrappingArea`, `PF_Spatula_WoodHandle`, `PF_Scissors_RedGray`, `PF_SauceBag` (all asserted by `CakeSavedAssetTests`), `BatterArea/PF_BatterMeasureCup_500ml`, `PF_Grill_Elmich/LidPivot`, `PF_Grill_Elmich/Anchors/CakePlacementPoint`, and exactly 5 `CakeStationPoint`s.
- **Gap: GAMEPLAY BLOCKER (Claude): none of the 5 points nor the cup has an `InteractableRef`.** `PlayerInteractor` therefore cannot focus any cake interaction in Play Mode. Tests drive `Execute` directly, so they pass. This needs a Claude-side fix before any Mac Play Mode cake run (issue #20 / MAIN-003 lane). No `Visual` roots. Meshes with colliders sit next to the points.

### 4.13 PF_DrinkStation — `Workstations/DrinkWave/PF_DrinkStation.prefab`

- **Hierarchy:** root [`PlaceholderAsset`] → `WipeArea` (−0.15, 1, −0.15) [`WipeInteraction`, `InteractableRef`, trigger 0.15 × 0.01 × 0.15] → `InteractionPoint`, `PF_WipeCloth` (§4.7). Then `PF_ReadyCounterPoint` (0, 1, 0.25) (§4.14), `PF_ToppingStation` (−0.41, 0.91, −0.13), `PF_IceBin` (−0.1, 0.82, −0.13) and `PF_RedTeaRack` (−0.72, 1, −0.13).
- **Found by type, not name:** the bootstrap uses `GetComponentsInChildren<TeaRackController / ReadyCounterPoint / ReadyOrderPickupPoint>`, so group names are informational. The animated `Visual` paths of the nested tools are [DNR].
- **Wiring:** `WipeInteraction._onStarted/_onStopped → PF_WipeCloth.StationToolView.Begin/Stop` (bool `Active`).
- **Gap:** the WipeArea/IceBin overlap (§3). Topping frame and cover colliders block rays (§4.5).

### 4.14 PF_ReadyCounterPoint — gameplay `Workstations/DrinkWave/PF_ReadyCounterPoint.prefab` (legacy `Stall/PF_ReadyCounterPoint.prefab`)

- **No mesh required** ([AP] §49). Layer 9.
- **Hierarchy:** root [`PlaceholderAsset`, `ReadyCounterPoint`, `InteractableRef`] → `InteractionTrigger` (trigger 0.32 × 0.05 × 0.2, centre y 0.025), `DrinkPlacement` (−0.08, 0, 0), `CakePlacement` (0.08, 0, 0), `OrderIndicator` (0, 0.1, 0), `LobbyPickup` (0, 0.1, 0.3) [`ReadyOrderPickupPoint`, `InteractableRef`, trigger 0.16 × 0.12 × 0.16].
- **Refs:** `_interactionPoint → InteractionTrigger`, `_drinkPlacementPoint → DrinkPlacement`, `_cakePlacementPoint → CakePlacement`, `ReadyOrderPickupPoint._interactionPoint → LobbyPickup`.
- **Behaviour:** a placed item is re-parented to the placement point so that **its own `Anchors/PlacementPoint` lands on it**. All its nodes go to layer 8 and its colliders are re-enabled. Each prepared item therefore needs a correct `Anchors/PlacementPoint` (bottom contact point).
- **Load-bearing [DNR]:** all five children. Optional art (a tray or mat) can go under a new `Visual/` child, coordinated on #20.
- **Legacy** `Stall/PF_ReadyCounterPoint.prefab`: same children, no component, not referenced by any scene or prefab. Do not put art there. Proposed: delete or deprecate it (Claude, #20).

### 4.15 PF_PlasticStool — `CustomerArea/PF_PlasticStool.prefab`

- **Size:** 0.30 × 0.30 × 0.30 m (spec 0.28–0.32 × 0.28–0.32 × 0.25–0.30, [AP] §39). Pivot at ground centre. Layer 8.
- **Hierarchy:** `Leg` ×4, `Seat` (meshes + colliders), `SeatPoint` (0, 0.3, 0).
- **Load-bearing:** `SeatPoint` (spec, not read by code today). The environment needs ≥ 3 instances named `PF_PlasticStool` (test). The environment instances override materials (`MAT_Accel_PlasticRed`).
- **Gap:** colliders on the legs and seat, no `Visual`. Visual-only, so low risk.

### 4.16 PF_YellowCrateTable — visual `CustomerArea/`, gameplay `Workstations/DrinkWave/`

- **Size:** 0.60 × 0.40 × 0.43 m (upside-down crate 0.6 × 0.4 × 0.4 + steel tray 0.03). Pivot at ground centre.
- **CustomerArea (visual, layer 8):** `Seats/{Seat01 (0, 0, −0.65), Seat02 (0, 0, 0.65)}`, `TrayTop` (mesh + collider), `InteractionPoint` (0, 0.43, −0.2), `Crate` (mesh + collider), `DeliveryPoint` (0, 0.43, 0). The environment instances add `CratePresentation/*` and steel material overrides.
- **DrinkWave (gameplay variant):** root becomes layer 9 with `TableOrderPoint`, `InteractableRef` and a root trigger 0.6 × 0.43 × 0.4. `TrayTop`/`Crate` colliders are layer 9 as well. **The bootstrap hides all its renderers** and binds `TableOrderPoint` to `transform.Find("InteractionPoint")`.
- **Load-bearing [DNR]:** `InteractionPoint` (direct child; bootstrap `Find`), `DeliveryPoint` (environment tests: `TablePoint/PF_YellowCrateTable/DeliveryPoint`, `CakeTablePoint/...`).
- **Art:** in the CustomerArea prefab, or in the environment instances. Never in the DrinkWave variant, whose visuals are hidden at runtime anyway.

### 4.17 PF_Placeholder_VehiclePoint — `Workstations/DrinkWave/PF_Placeholder_VehiclePoint.prefab`

- **Root:** layer 9 [`PlaceholderAsset`, `VehicleOrderPoint`, `InteractableRef`, trigger BoxCollider **0.16 × 0.12 × 0.16 centred at the ground**]. Root localPosition (2, 0, −1.3) is overridden by the bootstrap.
- **Hierarchy:** `Anchors/InteractionPoint` (0, 1, 0) [DNR, bootstrap `Find("Anchors/InteractionPoint")`]. `Placeholder` (capsule-ish cube 0.4 × 1.4 × 0.4, renderer hidden at runtime).
- **Contract (DEC-010):** generic vehicle point. No vehicle model belongs here. Vehicle art goes in the environment under `VehiclePoint/GenericVehicleVisual`.
- **Gaps (Claude):** the trigger is tiny and sits at ground level, so it is hard to aim at. Proposed ≈ 0.8 × 1.2 × 1.4 centred at y 0.6. Also missing: the spec anchor `Anchors/CustomerWaitPoint`, a zeroed root position, and a `Visual` root.

### 4.18 PF_AccelRoadsideEnvironment — `ACCEL01/Environment/PF_AccelRoadsideEnvironment.prefab`

- **Extent:** ≈ 26 × 18.45 m, height to 4.5 m. Layer 8. Root [`PlaceholderAsset`, notes must contain "provisional"].
- **Top level:** `StallRoot/PF_Stall_TramChanh` (nested canonical stall + material overrides, `CounterBranding`, `PresentationDetails` bulbs/LED/trims, `Sign/PF_Sign_TramChanh_New/BrandingPlaceholder`), `SnackDisplayPlaceholder`, `ReadyHandoff`, `NightLighting` (Lights), `MenuBoard`, `CakeDineInCustomer`, `TablePoint/PF_YellowCrateTable`, `Roadside` (`Street` with BoxCollider, `Sidewalk`), `PlayerSpawn`, `NeighbourhoodBackdrop`, `VehiclePoint/{TakeawayCustomer, GenericVehicleVisual, TakeawayLabel}`, `LobbyPosition`, `DineInCustomer`, `CustomerFurniture/PF_PlasticStool ×4`, `CakeTablePoint/PF_YellowCrateTable`.
- **Load-bearing [DNR]:** the §3 anchors, `StallRoot/PF_Stall_TramChanh` (must stay a nested instance of the canonical stall prefab, scale 1, GT-001 bounds), `…/Sign/PF_Sign_TramChanh_New/BrandingPlaceholder`, `TablePoint/PF_YellowCrateTable/DeliveryPoint`, `CakeTablePoint/PF_YellowCrateTable/DeliveryPoint`, `VehiclePoint/GenericVehicleVisual`, `VehiclePoint/TakeawayCustomer`, `DineInCustomer`, `CakeDineInCustomer/SeatedBody`, `Roadside/Street` (+ BoxCollider), `Roadside/Sidewalk`.
- **Rules:** no gameplay components inside `TablePoint`, `CakeTablePoint` or `VehiclePoint` (the bootstrap adds them). No children under the stall anchors. Walk zones around `PlayerSpawn` and `LobbyPosition` must stay clear above solid ground (ACCEL_SCENE_003).

### 4.19 PF_Player — `NPC/PF_Player.prefab`

- Root layer 10 [`CharacterController` r 0.22 h 1.7, `PlayerInputReader`, `FirstPersonController` (`_camera → PlayerCamera`), `PlayerInteractor`] → `PlayerCamera` (0, 1.6, 0) [Camera, AudioListener] → `HandSocket` (0.2, −0.2, 0.4).
- **Hold anchor:** `SCN_TramChanh_Main` adds `HeldItemView` to the root and a `HoldAnchor` child at `PlayerCamera/HandSocket/HoldAnchor` (0, 0.12, 0.1) (`TeaRackPickupAssetTests`). A held item is re-parented so that its `HandGrip` coincides with `HoldAnchor`. **Every held item's `HandGrip` must therefore be the real grip point**, with +Z pointing away from the player.
- **Load-bearing [DNR]:** `PlayerCamera`, `PlayerCamera/HandSocket`. No art is planned (first person). Optional hands go under `HandSocket` with no colliders. Proposed: move `HeldItemView` + `HoldAnchor` into the prefab (Claude, #20).

---

## 5. Proposed Claude-side changes (coordinate on issue #20; NOT done here)

1. **[Blocker] Cake focus:** add `InteractableRef` (pointing at the component on the same node) to the 5 `CakeStationPoint`s and to `BatterMeasureCup`. Without it the player cannot interact with the cake station in Play Mode. Add a Play Mode test that focuses via `PlayerInteractor.RefreshFocus` rather than calling `Execute` directly.
2. **Visual roots:** add `Visual/` (identity) under each equipment and furniture prefab (RedTeaRack, ToppingStation per bin, IceBin, Grill `Visual` + `LidPivot/Visual`, cup, spatula, scissors, sauce bag, wrapping paper, stool, crate table, vehicle point). Move the existing `SM_*` and cube meshes under it, and point serialized refs at pivots or anchors, never at meshes.
3. **Separate colliders from meshes:** replace the mesh BoxColliders with a root/interaction trigger (layer 9) plus optional `Colliders/` (layer 8) on RedTeaRack (no root trigger today), ToppingStation (frame and cover on layer 9 without a ref), IceBin, Grill (keep a lid collider under `LidPivot`), cup, cake tools, `PF_Cake_Prepared` (one root collider), stool and crate table.
4. **Tests that pin art details**, to be relaxed together with steps 2 and 3: `SM_Cake_RolledVertical.localScale.y > x`, `Visual/Bag_Closed` and `Visual/TeaLiquid` must have a renderer on the node itself, `LidPivot/Lid` needs a BoxCollider, and `Base` bounds must equal `LidPivot.z`.
5. **Anchors:** put `HandGrip` on the real grips (cup handle, spatula handle, scissors loop, sauce bag neck). Add `PF_SauceBag/Anchors/PourPoint` and `PF_Placeholder_VehiclePoint/Anchors/CustomerWaitPoint`. Set `PlacementPoint` at each item's bottom contact.
6. **Vehicle point:** a larger, raised trigger and a zero root position.
7. **Layout:** resolve the WipeArea/IceBin overlap. Decide whether stations follow the `StallAnchor` poses (today they are hard-coded offsets in `PF_DrinkStation` and `_cakeStationLocalPosition`). Cut the counter openings in `Colliders/Body` once the art counter has them.
8. **Housekeeping:** remove or deprecate `Stall/PF_ReadyCounterPoint.prefab`. Move `HeldItemView` + `HoldAnchor` into `PF_Player`. Create the `PF_SauceBag_*` material variants when DEC-008 is settled.

---

## 6. Safe replacement procedure (Unity)

1. Pull the latest integration branch. Run `python3 Automation/art_contract_audit.py` and confirm **0 FAIL** before you start.
2. Open the **base** prefab in Prefab Mode (for example `Workstations/PF_RedTeaRack`, not the DrinkWave variant; `CustomerArea/PF_YellowCrateTable`, not the DrinkWave one; the canonical `Stall/PF_Stall_TramChanh`, not the environment instance).
3. Import the FBX per ASSET_INTEGRATION §4: scale 1, transforms applied, *Generate Colliders off*, no cameras or lights, external `MAT_` materials.
4. **If the prefab has a `Visual/` root:** drag the model under `Visual/`, keep `Visual` at identity, and delete only the old placeholder meshes under `Visual/`.
   **If it does not (most prefabs today):** keep every node named in §4 (and every node that has a Collider). Either (a) replace the `Mesh` on the existing `MeshFilter` and the materials on its `MeshRenderer`, and adjust that node's scale to 1 only when it carries no collider, **or** (b) add the model as a **child** of the existing node, then disable (do not delete) the old `MeshRenderer`. Never delete or rename a node that has a Collider or appears in §4.
5. Keep moving art under its pivot: the lid under `LidPivot`, the cover under `CoverPivot`, tool meshes under the animated `Visual`, and the liquid on or under `BatterLevel`. Check that the pivot sits on the real hinge axis. Ask Claude to move a pivot; do not move it yourself.
6. Do not add colliders, scripts or Animators. Do not change layers on collider nodes. Do not touch `Anchors/*`. Do not put anything under the stall `Anchors/*Anchor`.
7. Save. Run `python3 Automation/art_contract_audit.py` again. It must end with `0 FAIL`. New WARN lines that mention your prefab need a note on #20.
8. In Unity, run EditMode tests `StallPlaceholderTests`, `AccelRoadsideSceneTests`, `CakeSavedAssetTests`, `TeaRackPickupAssetTests`, `GameplayBlockoutTests` and `DrinkWaveAssetTests`, then Play `SCN_TramChanh_Main` and walk the drink loop.
9. Commit only art files (`Art/**`, prefab visual changes) on your own branch. Post the audit summary line on #20.

---

## 7. Animation and code-driven motion summary

| Prefab | Driver | Parameter / call | Animated node (relative) | Motion |
|---|---|---|---|---|
| PF_TeaBag_PrePortioned | `TeaBagStateView.SetShaking` | bool `Shaking` (`AC_TeaBag_Shake`) | `Visual` | euler z 0→8°→0 |
| PF_ToppingStation (DrinkWave) | `StationToolView.PlayOnce` ← `ToppingBin._onAdded` | trigger `Action` (`AC_Topping_Cover`) | `CoverPivot` | euler x 70→90→70 (idle 70) |
| PF_IceScoop | `StationToolView.PlayOnce` ← `IceBin._onScooped` | trigger `Action` (`AC_Ice_Scoop`) | `Visual` | localPosition x 0→0.05→0 |
| PF_WipeCloth | `StationToolView.Begin/Stop` ← `WipeInteraction` | bool `Active` (`AC_Cloth_Wipe`) | `Visual` | localPosition x 0→0.05→0 (loop) |
| PF_Grill_Elmich (in PF_CakeStation) | `CakeStation.Advance` (code) | `_openLidDegrees` (70 DEV) | `LidPivot` | localRotation × Euler(−70, 0, 0) while open |
| PF_Scissors_RedGray | `CakeStation.SetToolMotion` | RollArea hold | root | rest × Euler(0, 25, 0) |
| PF_SauceBag | `CakeStation.SetToolMotion` | Sauce hold | root | rest × Euler(−25, 0, 0) |
| PF_Spatula_WoodHandle | `PlaceholderFlipAction.Present` | flip | root | **absolute** Euler(0, 0, −15) |
| PF_BatterMeasureCup_500ml | `BatterMeasureCup.ApplyLevel` | measured ml | `BatterLevel` | scale.y 0.001–0.1, pos.y 0.005–0.055, active if > 0 |
| PF_Cake_Prepared | `CakeItem.Apply` | state | `SM_Cake_Flat / _RolledVertical / _WrappingPaper` | SetActive |
| PF_TeaBag_PrePortioned | `TeaBagStateView.Apply` | state | `Visual/Bag_*`, `Visual/Contents/*`, `Visual/Condensation` | SetActive |
| All held items | `HeldItemView.Attach` | pickup | root | re-parented to `HoldAnchor` by `HandGrip`. Layer 12, colliders off |
| Prepared items | `ReadyCounterPoint.Execute` | place | root | re-parented by `Anchors/PlacementPoint`. Layer 8, colliders on |

The bootstrap freezes every Animator (`speed = 0`) while the game clock is paused. Clips bind by **name path relative to the Animator**, so renaming `Visual` or `CoverPivot` silently breaks them. The audit reports such a break as FAIL ("animation binding … has no matching child").

---

## 8. Audit output on the current tree (`248eac1` + this tool)

`python3 Automation/art_contract_audit.py --no-mapping` → exit code 0.

```text
[PASS] PF_Stall_TramChanh  (Assets/TramChanh/Prefabs/Stall/PF_Stall_TramChanh.prefab)  ok=18 warn=0 fail=0
[PASS] PF_Sign_TramChanh_New  (Assets/TramChanh/Prefabs/Stall/PF_Sign_TramChanh_New.prefab)  ok=5 warn=0 fail=0
[WARN] PF_RedTeaRack (base)  (Assets/TramChanh/Prefabs/Workstations/PF_RedTeaRack.prefab)  ok=7 warn=1 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
[WARN] PF_RedTeaRack (DrinkWave)  (Assets/TramChanh/Prefabs/Workstations/DrinkWave/PF_RedTeaRack.prefab)  ok=12 warn=2 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
    WARN focus depends on the colliders of the visual meshes under 'Rack' (no root trigger): an art swap that drops them makes the rack unfocusable (proposed: root trigger BoxCollider, issue #20)
[PASS] PF_TeaBag_PrePortioned  (Assets/TramChanh/Prefabs/Items/PF_TeaBag_PrePortioned.prefab)  ok=29 warn=0 fail=0
[WARN] PF_ToppingStation (base)  (Assets/TramChanh/Prefabs/Workstations/PF_ToppingStation.prefab)  ok=9 warn=1 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
[WARN] PF_ToppingStation (DrinkWave)  (Assets/TramChanh/Prefabs/Workstations/DrinkWave/PF_ToppingStation.prefab)  ok=16 warn=2 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
    WARN 2 Interactable-layer collider node(s) have no InteractableRef ancestor (block the interaction ray, cannot be focused): CoverPivot/TransparentCover, StationFrame
[WARN] PF_IceBin (base)  (Assets/TramChanh/Prefabs/Workstations/PF_IceBin.prefab)  ok=6 warn=1 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
[WARN] PF_IceBin (DrinkWave)  (Assets/TramChanh/Prefabs/Workstations/DrinkWave/PF_IceBin.prefab)  ok=10 warn=1 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
[PASS] PF_IceScoop  (Assets/TramChanh/Prefabs/Tools/PF_IceScoop.prefab)  ok=6 warn=0 fail=0
[PASS] PF_WipeCloth  (Assets/TramChanh/Prefabs/Tools/PF_WipeCloth.prefab)  ok=6 warn=0 fail=0
[WARN] PF_Grill_Elmich  (Assets/TramChanh/Prefabs/Workstations/PF_Grill_Elmich.prefab)  ok=14 warn=1 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
[WARN] PF_BatterMeasureCup_500ml  (Assets/TramChanh/Prefabs/ACCEL01/Cakes/PF_BatterMeasureCup_500ml.prefab)  ok=9 warn=2 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
    WARN GAMEPLAY GAP (Claude-side): no InteractableRef on the cup - it cannot be focused/picked up by the player raycast
[WARN] PF_Spatula_WoodHandle  (Assets/TramChanh/Prefabs/ACCEL01/Cakes/PF_Spatula_WoodHandle.prefab)  ok=4 warn=1 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
[WARN] PF_Scissors_RedGray  (Assets/TramChanh/Prefabs/ACCEL01/Cakes/PF_Scissors_RedGray.prefab)  ok=4 warn=1 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
[WARN] PF_SauceBag  (Assets/TramChanh/Prefabs/ACCEL01/Cakes/PF_SauceBag.prefab)  ok=4 warn=2 fail=0
    WARN missing child 'Anchors/PourPoint' - spec anchor (missing today)
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
[WARN] PF_CakeWrappingPaper  (Assets/TramChanh/Prefabs/ACCEL01/Cakes/PF_CakeWrappingPaper.prefab)  ok=4 warn=1 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
[PASS] PF_Cake_Prepared  (Assets/TramChanh/Prefabs/ACCEL01/Cakes/PF_Cake_Prepared.prefab)  ok=13 warn=0 fail=0
[WARN] PF_CakeStation  (Assets/TramChanh/Prefabs/ACCEL01/Cakes/PF_CakeStation.prefab)  ok=34 warn=1 fail=0
    WARN GAMEPLAY GAP (Claude-side): no InteractableRef on ['WrappingArea', 'BatterSource', 'RollArea', 'PF_SauceBag', 'PF_Grill_Elmich', 'BatterArea/PF_BatterMeasureCup_500ml'] - PlayerInteractor (raycast + GetComponentInParent<InteractableRef>) cannot focus them in Play Mode
[WARN] PF_DrinkStation  (Assets/TramChanh/Prefabs/Workstations/DrinkWave/PF_DrinkStation.prefab)  ok=30 warn=1 fail=0
    WARN 2 Interactable-layer collider node(s) have no InteractableRef ancestor (block the interaction ray, cannot be focused): PF_ToppingStation/CoverPivot/TransparentCover, PF_ToppingStation/StationFrame
[WARN] PF_ReadyCounterPoint (Stall, legacy)  (Assets/TramChanh/Prefabs/Stall/PF_ReadyCounterPoint.prefab)  ok=6 warn=1 fail=0
    WARN legacy blockout without ReadyCounterPoint component; the game uses Workstations/DrinkWave/PF_ReadyCounterPoint
[PASS] PF_ReadyCounterPoint (DrinkWave)  (Assets/TramChanh/Prefabs/Workstations/DrinkWave/PF_ReadyCounterPoint.prefab)  ok=17 warn=0 fail=0
[WARN] PF_PlasticStool  (Assets/TramChanh/Prefabs/CustomerArea/PF_PlasticStool.prefab)  ok=3 warn=1 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
[WARN] PF_YellowCrateTable (CustomerArea)  (Assets/TramChanh/Prefabs/CustomerArea/PF_YellowCrateTable.prefab)  ok=6 warn=1 fail=0
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
[PASS] PF_YellowCrateTable (DrinkWave)  (Assets/TramChanh/Prefabs/Workstations/DrinkWave/PF_YellowCrateTable.prefab)  ok=8 warn=0 fail=0
[WARN] PF_Placeholder_VehiclePoint  (Assets/TramChanh/Prefabs/Workstations/DrinkWave/PF_Placeholder_VehiclePoint.prefab)  ok=7 warn=4 fail=0
    WARN missing child 'Anchors/CustomerWaitPoint' - spec anchor (missing today)
    WARN target-structure gap: no 'Visual' root for replaceable art (proposed Claude-side restructure, issue #20)
    WARN root localPosition is (2, 0, -1.3), not 0 (harmless: bootstrap overrides it; reset when restructuring)
    WARN interaction trigger is only 0.16x0.12x0.16 m centred at y=0 (ground); hard to aim at - size is [Tbd] DEC-010 (Claude-side)
[PASS] PF_AccelRoadsideEnvironment  (Assets/TramChanh/Prefabs/ACCEL01/Environment/PF_AccelRoadsideEnvironment.prefab)  ok=27 warn=0 fail=0
[PASS] PF_Player  (Assets/TramChanh/Prefabs/NPC/PF_Player.prefab)  ok=9 warn=0 fail=0

SUMMARY: 323 checks ok, 25 WARN (known gaps), 0 FAIL -> PASS
```

WARN lines are the known gaps listed in §4 and §5. The tool checks: prefab existence, parse and nested/variant expansion, that every MonoBehaviour GUID resolves to a `.cs` in `Assets`, required children (FAIL for load-bearing names, WARN for spec-only names), required components, serialized references resolving to the expected child path, collider layers, focusability (each `InteractableRef` has a layer-9 collider below it; orphan layer-9 colliders are WARN), animator clip bindings and parameters, the stall GT-001 envelope (colliders always; renderer bounds when meshes are builtin primitives), sign uniqueness (GT-002) and the environment composition anchors. Custom (FBX) mesh bounds cannot be measured offline. Those cases are reported as WARN and must be checked with the Unity EditMode tests.
