# Asset Licenses & Attribution (ART-FIX-010)

All external 3D models and textures imported or upgraded for Project Trạm Chanh are strictly licensed under **Creative Commons Zero (CC0 1.0 Universal)** or **Creative Commons Attribution (CC-BY 3.0 / CC-BY 4.0)** from authorized open repositories (Kenney, Quaternius, Poly Pizza, Google Poly Archive).

Commercial copyrighted assets, restrictive licenses prohibiting video game use, and game rips are strictly prohibited.

---

## Asset Registry

| Asset Name | Author / Creator | Source URL | License | Download Date | Project File Path |
|---|---|---|---|---|---|
| **Urban Commuter Scooter** (`CUPIC_SCOOTER`) | Poly by Google | https://poly.pizza/m/9RqkkBOPNtd | CC-BY 3.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_Motorbike.obj`<br>`Assets/TramChanh/Art/Textures/Environment/T_Motorbike_BaseColor.png` |
| **Low-Poly Sedan Car** (`SportsCar`) | Quaternius | https://poly.pizza/m/e6410e19-a88f-4158-bc91-c19fccf85028 | CC0 1.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_Car_Sedan.obj` |
| **Plastic Stool** (`Stool_01`) | Poly by Google | https://poly.pizza/m/8c3b1e81-3d5f-4e7d-acd1-deffee372364 | CC-BY 3.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Furniture/SM_PlasticStool.obj` |
| **Beverage Bottle Crate** (`Block_Crate`) | Quaternius | https://poly.pizza/m/fbfe4b47-02fd-403e-bdfe-eca895ed7a7f | CC0 1.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Furniture/SM_YellowCrateTable.obj` |
| **Urban Street Light** (`StreetLight`) | Quaternius | https://poly.pizza/m/9a8aac3e-d1ee-4860-b294-edca62ef50be | CC0 1.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_StreetLamp.obj` |
| **Tropical Street Tree** (`Small_Trees`) | Poly by Google | https://poly.pizza/m/c1759d35-ab89-4a31-9d3f-880412b72552 | CC-BY 3.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_StreetTree_Trunk.obj`<br>`Assets/TramChanh/Art/Models/Environment/SM_StreetTree_Canopy.obj`<br>`Assets/TramChanh/Art/Textures/Environment/T_Foliage_Tree_BaseColor.png` |
| **Curbside Trash Bin** (`Trashcan_Small2`) | Quaternius | https://poly.pizza/m/ee8e1e80-d1ac-4700-add3-e0d7f1e3d5ed | CC0 1.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_TrashBin.obj` |
| **Tropical Planter Plant** (`BirdsOfParadisePottedPlant`) | Poly by Google | https://poly.pizza/m/566ed0e1-36e6-4831-84d3-ac25db2f62b5 | CC-BY 3.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_PottedPlant.obj`<br>`Assets/TramChanh/Art/Textures/Environment/T_Plant_BirdsOfParadise_BaseColor.png` |
| **Low-Poly Animated Characters & Pedestrians** | Quaternius | https://poly.pizza/m/170235d2-cdeb-4cb2-a82f-4828585138fe<br>https://poly.pizza/m/3746be88-6799-4817-929b-6bc067c47caa<br>https://poly.pizza/m/cf08b740-dd48-443e-9fde-6d3d54abf119 | CC0 1.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/People/M_Person_01_LOD0.asset`..`06_LOD2.asset`<br>`Assets/TramChanh/Art/Textures/People/T_Person_*.png` |
| **Concrete Electrical Utility Pole** | Project Tram Chanh Community Kit (inspired by Kenney Urban) | https://kenney.nl/assets/city-kit-commercial | CC0 1.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_UtilityPole.obj`<br>`Assets/TramChanh/Art/Textures/T_UtilityPole_BaseColor.png` |

---

## Technical Specifications & Polygon Budget Conformance

| Category | Budget Triangles | Implemented Model | Actual Triangles | Budget Textures | Actual Texture | Conformance |
|---|---|---|---|---|---|---|
| **Motorbike** | $\le 4,000$ tris | Urban Commuter Scooter | 2,116 tris | $\le 2048 \times 2048$ | $1024 \times 1024$ PNG | **PASS** |
| **Road Vehicle** | $\le 4,000$ tris | Low-Poly Sedan Car | 2,216 tris | $\le 2048 \times 2048$ | Palette material | **PASS** |
| **Characters** | $\le 5,000$ tris | Vietnamese Pedestrian Bodies 01..06 | 1,216 – 1,908 tris (LOD0) | $\le 2048 \times 2048$ | $512 \times 512$ PNG | **PASS** |
| **Plastic Stool** | $\le 1,500$ tris | Low Plastic Stool | 448 tris | $\le 2048 \times 2048$ | $512 \times 512$ PNG | **PASS** |
| **Crate Table** | $\le 1,500$ tris | Beverage Bottle Crate | 324 tris | $\le 2048 \times 2048$ | $512 \times 512$ PNG | **PASS** |
| **Street Lamp** | $\le 1,500$ tris | Urban Curved Mast Lamp | 292 tris | $\le 2048 \times 2048$ | Material colors | **PASS** |
| **Street Tree** | $\le 1,500$ tris | Tropical Street Tree | 874 tris | $\le 2048 \times 2048$ | $512 \times 512$ PNG | **PASS** |
| **Trash Bin** | $\le 1,500$ tris | Curbside Green Bin | 432 tris | $\le 2048 \times 2048$ | Material colors | **PASS** |
| **Potted Plant** | $\le 1,500$ tris | Birds of Paradise Planter | 972 tris | $\le 2048 \times 2048$ | $1024 \times 1024$ PNG | **PASS** |
| **Utility Pole** | $\le 1,500$ tris | Concrete Power Pole | 620 tris | $\le 2048 \times 2048$ | $512 \times 512$ PNG | **PASS** |
