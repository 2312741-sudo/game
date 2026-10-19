# New Menu Art Assets Specification (ART-FIX-028)

This document provides detailed technical specifications, dimensions, triangle counts, texture resolutions, material shaders, and exact asset paths for the **4 New Menu Items** introduced in `ART-FIX-028`.

These assets are authored as **pure art assets** (models, textures, materials, and prefabs) with **no gameplay scripts or recipe formulas**, allowing gameplay programmers and designers to easily wire ScriptableObjects, recipes, workstation animations, and customer order logic later.

---

## 1. Executive Summary & Asset Matrix

| Item Name | Category | Model File (.obj) | Triangles | Dimensions (W × H × D) | Material (.mat) | Shader & Surface | Prefab (.prefab) |
|---|---|---|---|---|---|---|---|
| **Honey Lemon Tea** (*Trà Chanh Mật Ong*) | Drink | `SM_Drink_HoneyLemonTea.obj` | 606 tris | $0.092 \times 0.210 \times 0.092\,\text{m}$ | `MAT_Drink_HoneyLemonTea.mat` | URP Lit (Transparent) | `PF_Item_Drink_HoneyLemonTea.prefab` |
| **Passionfruit Tea** (*Trà Chanh Dây Nhiệt Đới*) | Drink | `SM_Drink_PassionfruitTea.obj` | 606 tris | $0.092 \times 0.210 \times 0.092\,\text{m}$ | `MAT_Drink_PassionfruitTea.mat` | URP Lit (Transparent) | `PF_Item_Drink_PassionfruitTea.prefab` |
| **Custard Cream Rolled Cake** (*Bánh Kẹp Cuộn Kem Trứng*) | Cake | `SM_Cake_CustardCream.obj` | 384 tris | $0.054 \times 0.054 \times 0.178\,\text{m}$ | `MAT_Cake_CustardCream.mat` | URP Lit (Opaque) | `PF_Item_Cake_CustardCream.prefab` |
| **Banana Flambée Rolled Cake** (*Bánh Kẹp Cuộn Chuối Đốt Rượu*) | Cake | `SM_Cake_BananaFlambee.obj` | 560 tris | $0.054 \times 0.054 \times 0.178\,\text{m}$ | `MAT_Cake_BananaFlambee.mat` | URP Lit (Opaque) | `PF_Item_Cake_BananaFlambee.prefab` |

All 4 models strictly satisfy the polygon budget: **< 2,500 triangles each** (all items are under 650 triangles, achieving >70% budget margin).

---

## 2. Drinks Specification

### 2.1 SM_Drink_HoneyLemonTea (Trà Chanh Mật Ong)
- **Visual Description**: Clear takeaway plastic cup with domed lid (*nắp cầu*), golden amber honey tea liquid volume, fresh sliced lemon wheels floating on the tea surface, floating crystal ice cubes, and a green-striped cafe straw angled through the domed lid opening.
- **Model Path**: `Assets/TramChanh/Art/Models/Food/SM_Drink_HoneyLemonTea.obj`
- **Vertices**: 423 | **Triangles**: 606 (Budget: < 2,500 tris)
- **Bounding Dimensions**:
  - Cup Base Diameter: $\approx 58\,\text{mm}$ ($r = 0.029\,\text{m}$)
  - Cup Rim Diameter: $\approx 88\,\text{mm}$ ($r = 0.044\,\text{m}$)
  - Cup Body Height: $0.135\,\text{m}$
  - Domed Lid Apex: $Y = 0.175\,\text{m}$
  - Straw Tip: $Y = 0.210\,\text{m}$
  - Total Bounds: $X \in [-0.046, 0.046], Y \in [0.000, 0.210], Z \in [-0.046, 0.046]$
- **Material**: `Assets/TramChanh/Art/Materials/Food/MAT_Drink_HoneyLemonTea.mat`
  - Shader: Universal Render Pipeline / Lit (`guid: 933532a4fcc9baf4fa0491de14d08ed7`)
  - Surface Type: `Transparent` (`_Surface = 1`, `_Blend = 0`, `RenderType = Transparent`, Custom Render Queue = `3000`)
  - Smoothness: $0.92$, Specular Highlights: Enabled
- **Texture Atlas**: `Assets/TramChanh/Art/Textures/Food/T_NewDrinks_BaseColor.png` ($1024 \times 1024$ RGBA PNG)
  - Clear cup & domed lid: UV quadrant $[0.0, 0.5] \times [0.5, 1.0]$ with subtle frosted reflections
  - Golden amber liquid: UV quadrant $[0.5, 1.0] \times [0.5, 1.0]$ rich honey tea gradient
  - Floating lemon slices: UV sub-region $[0.52, 0.74] \times [0.02, 0.24]$ authentic 8-segment citrus wheel
  - Ice cubes: UV sub-region $[0.52, 0.72] \times [0.28, 0.48]$ crystal ice highlights
  - Straw: UV sub-region $[0.88, 0.98] \times [0.05, 0.45]$ vibrant green spiral bands
- **Prefab**: `Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Drink_HoneyLemonTea.prefab`
  - Pivot: Bottom center $(0, 0, 0)$ (sits flush on table / tray surfaces)
  - Scale: $1\,\text{unit} = 1\,\text{m}$
  - Collider: `BoxCollider` Center $(0, 0.09, 0)$, Size $(0.10, 0.18, 0.10)$

---

### 2.2 SM_Drink_PassionfruitTea (Trà Chanh Dây Nhiệt Đới)
- **Visual Description**: Clear takeaway plastic cup with domed lid, vibrant tropical yellow-orange passionfruit tea liquid, 16 dark passionfruit seeds/jelly clusters distributed throughout the liquid column and clustered at the surface, floating crystal ice cubes, and an angled cafe straw.
- **Model Path**: `Assets/TramChanh/Art/Models/Food/SM_Drink_PassionfruitTea.obj`
- **Vertices**: 449 | **Triangles**: 606 (Budget: < 2,500 tris)
- **Bounding Dimensions**:
  - Cup Base Diameter: $\approx 58\,\text{mm}$ ($r = 0.029\,\text{m}$)
  - Cup Rim Diameter: $\approx 88\,\text{mm}$ ($r = 0.044\,\text{m}$)
  - Total Bounds: $X \in [-0.046, 0.046], Y \in [0.000, 0.210], Z \in [-0.046, 0.046]$
- **Material**: `Assets/TramChanh/Art/Materials/Food/MAT_Drink_PassionfruitTea.mat`
  - Shader: Universal Render Pipeline / Lit
  - Surface Type: `Transparent` (`_Surface = 1`, Custom Render Queue = `3000`)
  - Smoothness: $0.92$
- **Texture Atlas**: `Assets/TramChanh/Art/Textures/Food/T_NewDrinks_BaseColor.png`
  - Tropical passionfruit liquid: UV quadrant $[0.0, 0.5] \times [0.0, 0.5]$ vibrant tropical orange-yellow
  - Dark passionfruit seeds & jelly: UV sub-region $[0.76, 0.88] \times [0.05, 0.48]$ glossy dark seeds with golden aril halos
- **Prefab**: `Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Drink_PassionfruitTea.prefab`
  - Pivot: Bottom center $(0, 0, 0)$
  - Scale: $1\,\text{unit} = 1\,\text{m}$
  - Collider: `BoxCollider` Center $(0, 0.09, 0)$, Size $(0.10, 0.18, 0.10)$

---

## 3. Cake Fillings Specification

### 3.1 SM_Cake_CustardCream (Bánh Kẹp Cuộn Kem Trứng)
- **Visual Description**: Authentic Vietnamese grilled rolled pancake (*bánh kẹp cuộn nướng*) with crispy golden browned waffle exterior lattice texture and grill compression ribs. Rich, smooth, billowing dollop of golden egg custard cream filling visible at both open cylindrical ends.
- **Model Path**: `Assets/TramChanh/Art/Models/Food/SM_Cake_CustardCream.obj`
- **Vertices**: 722 | **Triangles**: 384 (Budget: < 2,500 tris)
- **Bounding Dimensions**:
  - Roll Outer Diameter: $54\,\text{mm}$ ($r = 0.027\,\text{m}$)
  - Roll Length: $170\,\text{mm}$ ($Z = -0.085\,\text{m}$ to $+0.085\,\text{m}$), with custard cream tips protruding to $\pm 0.089\,\text{m}$
  - Roll Center: $Y = 0.027\,\text{m}$ (resting on counter surface at $Y = 0.000\,\text{m}$)
  - Total Bounds: $X \in [-0.027, 0.027], Y \in [0.000, 0.054], Z \in [-0.089, 0.089]$
- **Material**: `Assets/TramChanh/Art/Materials/Food/MAT_Cake_CustardCream.mat`
  - Shader: Universal Render Pipeline / Lit
  - Surface Type: `Opaque` (`_Surface = 0`, Custom Render Queue = `-1`)
  - Smoothness: $0.30$
- **Texture Atlas**: `Assets/TramChanh/Art/Textures/Food/T_NewCakes_BaseColor.png` ($1024 \times 1024$ RGBA PNG)
  - Golden browned waffle crust: UV quadrant $[0.0, 0.5] \times [0.5, 1.0]$ crisp lattice grill lines
  - Custard cream filling: UV quadrant $[0.0, 0.5] \times [0.0, 0.5]$ warm egg-yolk custard with vanilla bean specks
- **Prefab**: `Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Cake_CustardCream.prefab`
  - Pivot: Bottom center $(0, 0, 0)$ (resting horizontally on counter/plate)
  - Scale: $1\,\text{unit} = 1\,\text{m}$
  - Collider: `BoxCollider` Center $(0, 0.028, 0)$, Size $(0.060, 0.056, 0.180)$

---

### 3.2 SM_Cake_BananaFlambee (Bánh Kẹp Cuộn Chuối Đốt Rượu)
- **Visual Description**: Grilled rolled pancake with fragrant caramelized butter browning and dark torch flambée toast marks on the outer waffle shell. Caramelized sliced banana disks and rich flambée caramel glaze visible at both open ends.
- **Model Path**: `Assets/TramChanh/Art/Models/Food/SM_Cake_BananaFlambee.obj`
- **Vertices**: 810 | **Triangles**: 560 (Budget: < 2,500 tris)
- **Bounding Dimensions**:
  - Roll Outer Diameter: $54\,\text{mm}$ ($r = 0.027\,\text{m}$)
  - Roll Length: $170\,\text{mm}$, with caramelized banana rounds extending to $\pm 0.089\,\text{m}$
  - Total Bounds: $X \in [-0.027, 0.027], Y \in [0.000, 0.054], Z \in [-0.089, 0.089]$
- **Material**: `Assets/TramChanh/Art/Materials/Food/MAT_Cake_BananaFlambee.mat`
  - Shader: Universal Render Pipeline / Lit
  - Surface Type: `Opaque` (`_Surface = 0`, Custom Render Queue = `-1`)
  - Smoothness: $0.30$
- **Texture Atlas**: `Assets/TramChanh/Art/Textures/Food/T_NewCakes_BaseColor.png`
  - Flambée charred waffle crust: UV quadrant $[0.5, 1.0] \times [0.5, 1.0]$ toasted char blisters
  - Caramelized banana filling: UV quadrant $[0.5, 1.0] \times [0.0, 0.5]$ sliced banana discs in brown-sugar caramel
- **Prefab**: `Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Cake_BananaFlambee.prefab`
  - Pivot: Bottom center $(0, 0, 0)$
  - Scale: $1\,\text{unit} = 1\,\text{m}$
  - Collider: `BoxCollider` Center $(0, 0.028, 0)$, Size $(0.060, 0.056, 0.180)$

---

## 4. Hierarchy & Prefab Structure

Each item prefab follows Trạm Chanh's standardized `Visual` child architecture:

```
PF_Item_Drink_HoneyLemonTea
├── Transform (0, 0, 0)
├── BoxCollider (Center: [0, 0.09, 0], Size: [0.10, 0.18, 0.10])
└── Visual
    ├── Transform (0, 0, 0)
    ├── MeshFilter (SM_Drink_HoneyLemonTea.obj)
    └── MeshRenderer (MAT_Drink_HoneyLemonTea.mat)
```

```
PF_Item_Cake_CustardCream
├── Transform (0, 0, 0)
├── BoxCollider (Center: [0, 0.028, 0], Size: [0.06, 0.056, 0.18])
└── Visual
    ├── Transform (0, 0, 0)
    ├── MeshFilter (SM_Cake_CustardCream.obj)
    └── MeshRenderer (MAT_Cake_CustardCream.mat)
```

### Key Design Benefits:
1. **Pivot at Bottom Center**: When spawned at workstation placement anchors (`_drinkPlacementPoint` or `_cakePlacementPoint` on `ReadyCounterPoint`), items sit naturally on counter/tray surfaces without floating or intersecting.
2. **Standardized Colliders**: Box colliders provide clean raycasting bounds for player interaction and item pickup without obstructing customer line-of-sight.
3. **Decoupled Visual Root**: Allows artists or gameplay systems to toggle visual states, attach condensation/steam VFX, or swap LODs under the `Visual` child without breaking root transforms or colliders.

---

## 5. Wiring Instructions for Gameplay / Recipes

To integrate these items into gameplay:
1. **ScriptableObject Recipe Definition**:
   - Create `SO_Recipe_Drink_HoneyLemonTea` / `SO_Recipe_Drink_PassionfruitTea` (under `Assets/TramChanh/ScriptableObjects/Recipes/`).
   - Create `SO_CakeRecipe_CustardCream` / `SO_CakeRecipe_BananaFlambee` (under `Assets/TramChanh/Data/ACCEL01/Cakes/`).
2. **Item Definition**:
   - Create corresponding `SO_Item_*` assets referencing the respective prefab in `Assets/TramChanh/Prefabs/Items/NewMenu/`.
3. **Serving Counter Placement**:
   - The prefabs fit into `ReadyCounterPoint` (`DrinkPlacement` and `CakePlacement` anchors).
