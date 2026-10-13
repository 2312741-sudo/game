# Asset Licenses & Attribution (ART-FIX-010)

All external 3D models and textures imported or upgraded for Project Trạm Chanh are strictly licensed under **Creative Commons Zero (CC0 1.0 Universal)** or **Creative Commons Attribution (CC-BY 3.0 / CC-BY 4.0)** from authorized open repositories (Quaternius, Poly Pizza, Google Poly Archive).

Commercial copyrighted assets, restrictive licenses prohibiting video game use, and game rips are strictly prohibited.

---

## Asset Registry

| Asset Name | Author / Creator | Source URL | License | Download Date | Project File Path |
|---|---|---|---|---|---|
| **Metal Sheet Roof "New" & "Old" \| Part-1** | CHEESE ARG 3D | https://sketchfab.com/3d-models/metal-sheet-roof-new-old-part-1-c7c46bf23a7e41068ab302cbc8152046 | CC-BY 4.0 | 2026-10-14 | `Assets/TramChanh/Art/Textures/T_CorrugatedMetal_BaseColor.png`<br>`Assets/TramChanh/Art/Textures/T_CorrugatedMetal_Normal.png`<br>`Assets/TramChanh/Art/Textures/T_CorrugatedMetal_MaskMap.png` |
| **Urban Commuter Scooter** (`CUPIC_SCOOTER`) | Poly by Google | https://poly.pizza/m/9RqkkBOPNtd | CC-BY 3.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_Motorbike.obj`<br>`Assets/TramChanh/Art/Textures/T_Motorbike_BaseColor.png` |
| **Plastic Stool** (`Stool`) | Poly by Google | https://poly.pizza/m/2UeLZTX5nHr | CC-BY 3.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Furniture/SM_PlasticStool.obj` |
| **Tropical Planter Plant** (`Birds of paradise potted plant`) | Poly by Google | https://poly.pizza/m/folDA1yLeEF | CC-BY 3.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_PottedPlant.obj`<br>`Assets/TramChanh/Art/Textures/T_Plant_BirdsOfParadise_BaseColor.png` |
| **Urban Street Light** (`Street Light`) | Quaternius | https://poly.pizza/m/nFwrlcLvM5 | CC0 1.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_StreetLamp.obj` |
| **Curbside Trash Bin** (`Trashcan Small`) | Quaternius | https://poly.pizza/m/i7HDuYDLkx | CC0 1.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_TrashBin.obj` |
| **Low-Poly Sedan Car** (`Sports Car`) | Quaternius | https://poly.pizza/m/OyqKvX9xNh | CC0 1.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_Car_Sedan.obj`<br>`Assets/TramChanh/Prefabs/Environment/PF_Car_Sedan.prefab` |
| **Urban Street Dog** (`Dog`) | Quaternius | https://poly.pizza/m/2kUk0QqpCg | CC0 1.0 | 2026-10-14 | `Assets/TramChanh/Art/Models/Environment/SM_Street_Dog.fbx`<br>`Assets/TramChanh/Prefabs/Environment/PF_Street_Dog.prefab` |
| **Urban Street Cat** (`Cat`) | Quaternius | https://poly.pizza/m/2f54vbV0In | CC0 1.0 | 2026-10-14 | `Assets/TramChanh/Art/Models/Environment/SM_Street_Cat.fbx`<br>`Assets/TramChanh/Prefabs/Environment/PF_Street_Cat.prefab` |
| **Urban Street Chicken** (`Chicken`) | Quaternius | https://poly.pizza/m/ineV9pU5VL | CC0 1.0 | 2026-10-14 | `Assets/TramChanh/Art/Models/Environment/SM_Street_Chicken.fbx`<br>`Assets/TramChanh/Prefabs/Environment/PF_Street_Chicken.prefab` |
| **Urban Street Pigeon** (`Pigeon`) | Quaternius | https://poly.pizza/m/9NGlBTpDEr | CC0 1.0 | 2026-10-14 | `Assets/TramChanh/Art/Models/Environment/SM_Street_Pigeon.fbx`<br>`Assets/TramChanh/Prefabs/Environment/PF_Street_Pigeon.prefab` |

---

## Technical Specifications & Polygon Budget Conformance

| Category | Budget Triangles | Implemented Model | Actual Triangles | Budget Textures | Actual Texture | Conformance |
|---|---|---|---|---|---|---|
| **Motorbike** | $\le 4,000$ tris | Urban Commuter Scooter | 2,116 tris | $\le 2048 \times 2048$ | $1024 \times 1024$ PNG | **PASS** |
| **Road Vehicle** | $\le 4,000$ tris | Low-Poly Sedan Car | 2,216 tris | $\le 2048 \times 2048$ | Palette material | **PASS** |
| **Plastic Stool** | $\le 1,500$ tris | Low Plastic Stool | 448 tris | $\le 2048 \times 2048$ | $512 \times 512$ PNG | **PASS** |
| **Street Lamp** | $\le 1,500$ tris | Urban Curved Mast Lamp | 292 tris | $\le 2048 \times 2048$ | Material colors | **PASS** |
| **Street Tree (Trunk + Canopy)** | $\le 1,500$ tris | Tropical Street Tree (2 separate meshes) | 165 tris (79 trunk + 86 canopy) | $\le 2048 \times 2048$ | $512 \times 512$ PNG | **PASS** |
| **Trash Bin** | $\le 1,500$ tris | Curbside Green Bin | 432 tris | $\le 2048 \times 2048$ | Material colors | **PASS** |
| **Potted Plant** | $\le 1,500$ tris | Birds of Paradise Planter | 972 tris | $\le 2048 \times 2048$ | $1024 \times 1024$ PNG | **PASS** |
| **Street Dog** | $\le 5,000$ tris | Urban Street Dog | 3,604 tris | $\le 2048 \times 2048$ | Vertex colors / URP Lit | **PASS** |
| **Street Cat** | $\le 5,000$ tris | Urban Street Cat | 1,772 tris | $\le 2048 \times 2048$ | Material colors / URP Lit | **PASS** |
| **Street Chicken** | $\le 5,000$ tris | Urban Street Chicken | 2,904 tris | $\le 2048 \times 2048$ | Material colors / URP Lit | **PASS** |
| **Street Pigeon** | $\le 5,000$ tris | Urban Street Pigeon | 2,184 tris | $\le 2048 \times 2048$ | Material colors / URP Lit | **PASS** |

---

## Bắt Buộc Ghi Nhận Bản Quyền (Credits Attribution Notice)

> [!IMPORTANT]
> **Quy định bản quyền thương mại và phân phối:**
> Các tài sản mang giấy phép **Creative Commons Attribution (CC-BY 3.0)** liệt kê ở trên (bao gồm các mô hình do **Poly by Google** sáng tác: *Xe máy tay ga CUPIC_SCOOTER, Ghế nhựa Stool, Chậu cây Birds of Paradise*) **bắt buộc phải được hiển thị rõ ràng trong màn hình Credits / Ghi nhận tác giả** của game (hoặc tài liệu kèm theo khi phát hành) với đầy đủ thông tin: tên tác phẩm, tên tác giả (Poly by Google), giấy phép CC-BY 3.0 và liên kết nguồn tương ứng.


> **Ghi chú (lead):** mô hình cây "Small Trees" của Poly by Google đã được thử trong ART-FIX-010 nhưng bị trả về bản cũ của dự án vì hình dạng bị méo; chưa dùng trong game, nên không có trong danh sách trên. Khi dùng lại phải thêm vào.
