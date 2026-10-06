# TRAM CHANH — ASSET INTEGRATION

**Status:** Approved for the slice (REV-000, 2026-10-06). Placeholders are first-class: gameplay never waits for finished 3D models.
**Scope:** how the 3D assets of the vertical slice go from reference photo to a validated Unity prefab that gameplay code can use.
**Primary source:** [AP] (`Docs/Reference/TramChanh_3D_Asset_Pipeline.md`). This document resolves naming conflicts between [AP] and [WF] and adds the gameplay contract (anchors, components, states) each prefab must satisfy.

---

## 1. Pipeline and ownership

From [AP] §88 and §83:

```text
PHOTO REFERENCE → ASSET SPEC → AI 3D GENERATION → BLENDER CLEANUP → FBX / TEXTURES
→ UNITY IMPORT → PREFAB → INTERACTION → QA → APPROVED
```

| Stage | Owner | Output | Gate |
|---|---|---|---|
| Asset spec | Claude Code (this doc + [AP]) | Requirements per asset | — |
| AI mesh generation | 3D Generator (Meshy/Tripo/Rodin/Hunyuan3D…) supervised by Product Owner | rough mesh in `ArtSource/AI_Generated/` | PO compares to reference |
| Blender cleanup | Blender artist | `ArtSource/Blender/*.blend`, `ArtSource/Export/*.fbx`, textures | [AP] §55 checklist |
| Branding textures | Art/Texture, artwork from Product Owner | `T_Sign_TramChanh_New_*` | PO confirms exact text |
| Unity import + prefab | Codex | `Assets/TramChanh/Art/**`, `Assets/TramChanh/Prefabs/**` | [AP] §54 checklist |
| Colliders, anchors, animator | Codex (Claude review) | prefab | `REV-005` rules below |
| Validation | Antigravity | PASS/FAIL report | [AP] §68 + §9 below |

AI-generated geometry is never treated as game-ready ([AP] §3.1). AI never produces final text/logos ([AP] §72).

### Asset lifecycle (per asset, tracked in `PROJECT_TASK_PLAN.md`)

```text
Spec → Blockout (primitive, true scale) → Generated → Cleaned → Imported → Integrated → QA PASS → Approved
```

Phases from [AP] §75–77: **3D-A Blockout** (scale, placement, reach; no textures) → **3D-B Gameplay-ready** (pivots, colliders, animation, basic materials; gameplay runs end-to-end) → **3D-C Art polish** (out of slice scope).

Gameplay code is never blocked on final art: every prefab exists first as a **blockout** with the final name, final hierarchy, final anchors and primitive meshes. Swapping in the real mesh must not change any script reference.

---

## 2. Canonical names (conflict resolution)

[AP] is the specialised asset document, so its names win unless noted.

| [WF] name | [AP] name(s) | **Canonical** | Note |
|---|---|---|---|
| `PF_BatterCup` | `PF_BatterMeasureCup` / `SM_BatterMeasureCup` | `PF_BatterMeasureCup` | |
| `PF_WrappingPaper` | `PF_CakeWrappingPaper` | `PF_CakeWrappingPaper` | |
| `PF_SauceBag_Mango/Chocolate/Cheese` | `PF_SauceBag` (§73) + three variants (§32) | Base `PF_SauceBag` (neutral geometry `SM_SauceBag`) + **prefab variants** `PF_SauceBag_Mango`, `PF_SauceBag_Chocolate`, `PF_SauceBag_Cheese` | Colour by material only ([AP] §32) |
| `PF_Stall_Wheel` | `SM_Stall_Wheel`, `SM_Stall_Wheels`, `SM_Stall_CasterWheel`, `PF_Stall_CasterWheel` | `SM_Stall_CasterWheel`, `PF_Stall_CasterWheel` | Detailed spec is [AP] §14 |
| `PF_Stall_Roof`, `PF_Stall_Frame`, `PF_Stall_Counter` | `SM_Stall_Roof`, `SM_Stall_Frame`, `SM_Stall_Counter`, `SM_Stall_Base` | Meshes `SM_Stall_*` as children of `PF_Stall_TramChanh`; no standalone prefabs | Modular meshes ([AP] §4) |
| `PF_LEDStrip` | `SM_LEDStrip`, `SM_LED_Fixture` (§4) | `SM_LEDStrip`, `PF_LEDStrip` | |
| `PF_BatterBag` | — (no spec) | `PF_Placeholder_BatterSource` until PO supplies a reference (DEC-009) | |
| — | `SM_Cake_Cut` (no prefab) | `PF_Cake_Cut` (added for consistency with the other cake states) | |
| Folder `Art/Models/{Stall,Furniture,Equipment,Food,Props}` ([WF] §7) | `Art/Models/{Stall,Branding,DrinkStation,CakeStation,Furniture,Food,Packaging,Props}` ([AP] §69) | [AP] §69 | More specific |

Placeholders for which no asset spec exists (DEC-010): `PF_Placeholder_Customer` (1.7 m capsule) and `PF_Placeholder_VehiclePoint` — a **generic vehicle interaction point** (trigger volume + anchors, size `[Tbd]`) with no specific vehicle model; any later vehicle art is purely visual and is not referenced by gameplay. Prefix `PF_Placeholder_` is reserved for these and is reported as a warning (not an error) by the validator.

---

## 3. Folder layout

```text
ArtSource/                         (outside Assets/, Git LFS)
├── References/  ├── Blender/  ├── AI_Generated/  ├── Export/  └── Textures/

Assets/TramChanh/
├── Art/
│   ├── Models/{Stall,Branding,DrinkStation,CakeStation,Furniture,Food,Packaging,Props}/
│   ├── Materials/        MAT_*  (shared library, §7)
│   ├── Textures/         T_[Asset]_{BaseColor,Normal,Mask,AO}
│   └── Animations/       AN_*  + Animator controllers
├── Prefabs/
│   ├── Stall/            PF_Stall_TramChanh, PF_Sign_TramChanh_New, PF_ReadyCounterPoint, PF_LEDStrip, PF_EdisonBulb
│   ├── Workstations/     PF_DrinkStation, PF_CakeStation, PF_RedTeaRack, PF_ToppingStation, PF_IceBin, PF_Grill_Elmich
│   ├── Items/            PF_TeaBag_PrePortioned, PF_BatterMeasureCup, PF_Cake_*, PF_SauceBag*, tools, PF_CakeWrappingPaper
│   ├── NPC/              PF_Placeholder_Customer
│   └── UI/
└── Scenes/Test/SCN_AssetScaleTest
```

Prefabs that are scene furniture (`PF_PlasticStool`, `PF_YellowCrateTable`, `PF_StainlessTrayTabletop`, `PF_Placeholder_VehiclePoint`) go in `Prefabs/CustomerArea/` (folder added; [WF] §7 has no furniture prefab folder).

---

## 4. Import settings

From [AP] §53, applied by an `AssetPostprocessor` (`TramChanhModelImporter`) for everything under `Art/Models/`:

| Setting | Value |
|---|---|
| Scale Factor | 1 (Blender exports in meters, "Apply Unit" on) |
| Convert Units | On if needed |
| Read/Write | Off (On only if listed as runtime-needed: none in slice) |
| Generate Colliders | Off |
| Import Cameras / Lights | Off |
| Normals | Import (Calculate only if listed per asset) |
| Tangents | Calculate Mikktspace |
| Materials | Use external `MAT_` library; embedded materials not extracted |
| Animation | Import only for assets with an `AN_` clip; otherwise off |
| Rig | None (no skinned meshes in slice) |

Transforms must be applied in Blender (rotation 0, scale 1) so the imported root has identity transform. Unity `+Z` forward, `+Y` up.

---

## 5. Prefab contracts (vertical slice)

Pivot rules from [AP] §7: furniture = ground centre; counter equipment = bottom centre; lids = hinge axis; doors/flaps = edge hinge; sign = rear centre; handheld = grip point.
Collider rules from [AP] §8: primitives / compound primitives; `MeshCollider` only where listed.
Anchor names from [AP] §58: `Anchors/{HandGrip, InteractionPoint, PlacementPoint, PourPoint, OutputPoint}`.

### 5.1 Stall

```text
PF_Stall_TramChanh                       pivot: ground centre · layer Environment · static
├── Structure      SM_Stall_Base
├── Counter        SM_Stall_Counter      top surface at exactly y = 1.00 m; openings for recessed topping station + ice bin (DEC-011)
├── Frame          SM_Stall_Frame        A-frame / trapezoid supports
├── Roof           SM_Stall_Roof         top at ~2.2 m
├── Sign           PF_Sign_TramChanh_New (nested prefab)
├── Lights         PF_LEDStrip, PF_EdisonBulb (+ Unity Light components)
├── Wheels         PF_Stall_CasterWheel ×N
├── Colliders      compound BoxColliders (counter body, frame posts, roof)
└── Anchors
    ├── TeaRackAnchor      ├── ToppingAnchor      ├── IceBinAnchor     ├── WipeAreaAnchor*
    ├── GrillAnchor        ├── BatterAreaAnchor*  ├── RollAreaAnchor*  ├── SauceAnchor
    ├── WrapAnchor         └── ReadyCounterAnchor
```

Every anchor carries a `StallAnchor` component (id, *Position Confirmed*, *Position Source*). **Anchor transforms are the only source of station positions**: they are edited in the prefab or scene, never hard-coded, and stay *Position Confirmed = off* until the real stall reference is measured (DEC-011). The stall root carries `StallAnchorSet` for lookup by id. Rebuilding the placeholder keeps edited anchor poses.

Hierarchy from [WF] Phase 2. `*` = anchors added because [AP] §59–60 station hierarchies contain areas (`WipeArea`, `BatterArea`, `RollArea`) that the [WF] anchor list lacks. Anchor positions come from the reference photos (DEC-011).

Phase A placeholder (built by `Tram Chanh ▸ Placeholders ▸ Build Stall + New Sign Placeholder`, or `ArtSource/Blender/Scripts/stall_blockout.py`): GT-001 dimensions exact; wheel/post/slab/roof sizes, A-frame lean and counter openings are provisional and come from the real reference in phase B.

Acceptance (GT-001, [AP] §79): renderer bounds 1.80 × 0.80 × ~2.20 m (±0.02 m on width/depth, ±0.05 m on height), counter top 1.00 m (±0.01), old sign absent, new sign mounted, no equipment merged into stall meshes, workspace fits both stations.

### 5.2 Sign

```text
PF_Sign_TramChanh_New     pivot: rear centre · BoxCollider · layer Environment · static
└── SM_Sign_TramChanh_New  geometry only; 500–2,000 tris; MAT_Sign_TramChanh_New with T_Sign_TramChanh_New_BaseColor (2048×512)
```

Phase A placeholder: a box volume (size `[Tbd]`, DEC-017) with the final names, rear-centre pivot, BoxCollider, `PlaceholderAsset` marker, and the **`MAT_Sign_TramChanh_New` material slot** that the final artwork texture is assigned to later. No text, no logo, never the old illuminated letters.

Branding: white housing, mustard-orange top, "Trạm" black, "Chanh" mustard-orange, coloured strip under the front edge, softly rounded ends ([AP] §2.2, §11). Text is texture only, from artwork supplied by the PO. GT-002: no other `SM_Sign_*`/`PF_Sign_*` asset may exist in the project.

### 5.3 Drink station

| Prefab | Pivot | Collider | Anchors | Moving parts / states | Component (Codex) |
|---|---|---|---|---|---|
| `PF_RedTeaRack` | bottom centre | Box | `InteractionPoint`, `OutputPoint` (bag spawn), `BagSlots/*` (visual bag positions) | visible bag count | `TeaRackController` |
| `PF_TeaBag_PrePortioned` | grip point | Box or Capsule (on `HeldItem` layer when held) | `HandGrip`, `PlacementPoint` | `Visual/Bag_Closed`, `Visual/Bag_Open`, `Visual/TeaLiquid`, `Visual/Contents/{CoconutJelly,LemonJelly,Ice}`; `AN_TeaBag_Shake` | `TeaBagItem`, `TeaBagStateView` |
| `PF_ToppingStation` | bottom centre (recessed: top flush with counter) | Box per bin (must not block scoop motion, [AP] §82) | per bin: `InteractionPoint` | `TransparentCover` separate mesh (hinge pivot) | `ToppingBin` ×2 |
| `PF_Topping_CoconutJelly` / `PF_Topping_LemonJelly` | bottom centre | none | — | volume mesh, no per-piece rigidbody ([AP] §20) | — (visual) |
| `PF_IceBin` | bottom centre (recessed) | Box | `InteractionPoint`, `ScoopRestPoint` | `IceVolume` separate mesh; optional lid | `IceBin` |
| `PF_IceScoop` | handle grip | none (station tool) | `HandGrip` | `AN_IceScoop_Scoop` | — (animated by `IceBin`) |
| `PF_WipeCloth` | bottom centre | none (area has collider) | — | `AN_WipeCloth_Wipe` | — (animated by `WipeInteraction`) |
| `PF_DrinkStation` | — (logical group) | — | children placed at stall anchors | — | — |

`PF_DrinkStation` hierarchy from [AP] §59 (TeaRack, ToppingStation/{CoconutJellyBin, LemonJellyBin}, IceBin, IceScoop, PumpBottles, WipeArea, ReadyPoint). `WipeArea` = BoxCollider on `Interactable` layer + `PF_WipeCloth` visual + `WipeInteraction`. `PumpBottles` and `PF_MeasuringCup_500ml` are static, non-interactive (GT-003).

Tea-bag acceptance ([AP] §81): fits in the rack slots; open state exists; jellies and ice visually appear; shake animation possible; wipe possible.

### 5.4 Cake station

| Prefab | Pivot | Collider | Anchors | Moving parts / states | Component (Codex) |
|---|---|---|---|---|---|
| `PF_Grill_Elmich` | bottom centre | Box (body) + Box on `LidPivot` (moves with lid) | `InteractionPoint`, `CakePlacementPoint`, `SpatulaPoint`, `AudioPoint` | `LidPivot` at the **rear hinge axis**, children `Lid`, `UpperPlate`, `Handle`; `LowerPlate` fixed; `Display` (red LED, emissive + text driven at runtime); `AN_Grill_LidOpen/Close` | `GrillController` |
| `PF_BatterMeasureCup` | grip point | Box/Capsule | `HandGrip`, `PourPoint`, `PlacementPoint` | child `BatterLevel` = `SM_BatterVolume` (scale/morph by amount) | `BatterMeasureCup` |
| `PF_BatterPortion` | bottom centre | none | — | pour stream / poured volume (shader or morph, [AP] §29) | — (visual) |
| `PF_Placeholder_BatterSource` | bottom centre (size `[Tbd]`, DEC-009) | Box | `InteractionPoint` | — | `BatterSourceInteractable` |
| `PF_Spatula_WoodHandle` | grip point | none (station tool) | `HandGrip` | `AN_Spatula_Flip` (provisional, DEC-013) | — (used by the active `IFlipAction`) |
| `PF_Scissors_RedGray` | grip point | none (station tool) | `HandGrip` | blades separate meshes, pivot at screw; `AN_Scissors_Cut` | — (animated by `RollAreaInteractable`) |
| `PF_SauceBag` (+3 variants) | grip point | Box | `InteractionPoint`, `PourPoint` | `AN_SauceBag_Squeeze` | `SauceBagInteractable` (sauce identity from `SauceDefinition` data; cake mapping unconfirmed, DEC-008) |
| `PF_Cake_Raw` / `PF_Cake_Cooked` / `PF_Cake_Cut` / `PF_Cake_Rolled` | bottom centre | none (lives on grill/roll area) | — | `Doneness01` material param on Raw/Cooked; `AN_Cake_RollVertical` | — (shown by `CakeView`) |
| `PF_CakeWrappingPaper` | bottom centre | Box (area) | `InteractionPoint` | `AN_Paper_Wrap` | `WrapInteraction` |
| `PF_Cake_Wrapped` | grip point | Capsule | `HandGrip`, `PlacementPoint` | — | `CakeView` (+ `IPreparedItem`) |
| `PF_CakeStation` | — (logical group) | — | children at stall anchors | — | — |

`PF_Grill_Elmich` hierarchy from [WF] §15 and [AP] §27:

```text
PF_Grill_Elmich
├── Base            SM_Grill_Base
├── LowerPlate      SM_Grill_LowerPlate        (parallel vertical ridges)
├── LidPivot        (empty, at rear hinge axis)
│   ├── Lid         SM_Grill_Lid
│   ├── UpperPlate  SM_Grill_UpperPlate        (parallel ridges; moves with lid)
│   └── Handle      SM_Grill_Handle            (horizontal metal)
├── Display         SM_Grill_Display           (red LED; temperature/time)
└── Anchors/{InteractionPoint, CakePlacementPoint, SpatulaPoint, AudioPoint}
```

Grill acceptance ([AP] §80, §86): lid separate; hinge pivot correct; upper plate follows lid; lower plate fixed; parallel ridges; metal handle; front LED display; cake placement point; open/close animation works in Unity without mesh deformation.

### 5.5 Ready counter and customer area

```text
PF_ReadyCounterPoint                   ([AP] §49 — no dedicated mesh required)
├── InteractionTrigger  BoxCollider, Interactable layer
├── DrinkPlacement      slot (1 in slice)
├── CakePlacement       slot (1 in slice)
└── OrderIndicator      world-space marker for UI
Component: ReadyCounterPoint
```

| Prefab | Dimensions | Pivot | Collider | Anchors / components |
|---|---|---|---|---|
| `PF_PlasticStool` | 0.28–0.32 × 0.28–0.32 × 0.25–0.30 m ([AP] §39) | ground centre | Box | `SeatPoint` |
| `PF_YellowCrateTable` (+ `PF_StainlessTrayTabletop` on top) | from reference (crate upside-down) | ground centre | Box | `InteractionPoint`, `DeliveryPoint`, `Seats/*`; `TableOrderPoint` |
| `PF_Placeholder_VehiclePoint` | generic volume, size `[Tbd]` (DEC-010) | ground centre | Box (trigger) | `InteractionPoint`, `CustomerWaitPoint`, optional `Visual` child (any vehicle art, not referenced by code); `VehicleOrderPoint` |
| `PF_Placeholder_Customer` | 1.7 m capsule | ground centre | Capsule, NPC layer | `SliceCustomerController` |

---

## 6. Layers, tags, static flags

| Layer | Used by |
|---|---|
| `Environment` | Stall structure, sign, furniture (static flags: Batching, Occluder/Occludee, Contribute GI as appropriate) |
| `Interactable` | Every collider that the player can focus (one per interactable, on the object or a child `InteractionCollider`) |
| `HeldItem` | Item while in hands (no collision with Player) |
| `Player`, `NPC` | Characters |

No gameplay logic uses tags in the slice (components are used instead), so no tags are required.

---

## 7. Material library

Shared materials from [AP] §51 (no per-object duplicates): `MAT_Wood_Dark`, `MAT_Metal_BlackPainted`, `MAT_Metal_Stainless`, `MAT_Plastic_Red`, `MAT_Plastic_Yellow`, `MAT_Plastic_Transparent`, `MAT_Glass_Clear`, `MAT_Food_JellyWhite`, `MAT_Food_JellyLemon`, `MAT_Food_Tea`, `MAT_Food_Cake`, `MAT_Paper_Kraft`.
Added for slice needs: `MAT_Sign_TramChanh_New` (material slot for the final branding texture; plain white until the artwork arrives), `MAT_Placeholder_Stall`, `MAT_Placeholder_Wheel`, `MAT_Placeholder_SignTop`, `MAT_Food_Batter`, `MAT_Food_Sauce` (colour per variant via material variant), `MAT_Grill_Display` (emissive red), `MAT_Placeholder` (blockouts).
URP Lit; masks packed per URP conventions (`T_*_Mask`).

---

## 8. Asset → gameplay state map

| Gameplay state (spec) | Asset representation |
|---|---|
| Tea bag `Stored…PickedUp` | `Bag_Closed` |
| Tea bag `Opened…` | `Bag_Open` |
| `CoconutJellyAdded` / `LemonJellyAdded` | `Contents/CoconutJelly`, `Contents/LemonJelly` (portions of `SM_Topping_*`) |
| `IceAdded` | `Contents/Ice` |
| `Shaken` | `AN_TeaBag_Shake` |
| `Wiped` | `AN_WipeCloth_Wipe`; condensation param off |
| Grill `Open`/closed | `LidPivot` rotation via `AN_Grill_LidOpen/Close` |
| Cake `BatterPoured`/`Cooking` | `PF_Cake_Raw` + `Doneness01` |
| Cake `Cooked`/`Flipped` | `PF_Cake_Cooked` (grill lines) |
| Cake `Cut` | `PF_Cake_Cut` |
| Cake `Sauced` | `PF_Cake_Cut` + sauce decal/mesh layer in `MAT_Food_Sauce` |
| Cake `Rolled` | `PF_Cake_Rolled` via `AN_Cake_RollVertical` |
| Cake `Wrapped` | `PF_Cake_Wrapped` |
| Cake `Ruined` | `PF_Cake_Cooked` with burnt tint |

Never bake a gameplay state into another asset's mesh (e.g. no `Grill_With_Cake_Baked_In`, [AP] §57).

---

## 9. Validation

### 9.1 Automated (Editor tool `TramChanhAssetValidator`, run by Antigravity in batch mode)

| Rule | Severity |
|---|---|
| Prefab name in canonical list (§2, §5) and correct prefix | Error |
| No missing script / missing material / missing texture / missing mesh | Error |
| Root transform identity; scale 1 on imported meshes | Error |
| Bounds within spec (stall GT-001; stool; others ±10 % of spec where spec exists) | Error |
| Required anchors present (§5 tables) | Error |
| Required gameplay component present on interactive prefabs | Error |
| Interactable collider on `Interactable` layer | Error |
| `MeshCollider` only on whitelisted prefabs (none in slice) | Error |
| Only `PF_Sign_TramChanh_New` sign exists (GT-002) | Error |
| `LidPivot` exists on grill and its position lies on the rear edge of `Base` bounds | Error |
| Triangle count within [AP] §5 budgets / per-asset budgets | Warning |
| Texture size within [AP] §6 | Warning |
| `PF_Placeholder_*` present | Warning |

### 9.2 Visual (Antigravity + PO) in `SCN_AssetScaleTest`

From [AP] §61: 1.7 m mannequin, 1 m cube, stall, stool, crate table, grill, tea rack — checked by eye against reference photos before integration.

### 9.3 Definition of Done (per asset)

[AP] §74 checklist + [AP] §54 prefab checklist + all §9.1 errors at zero + QA PASS report attached to the task.
