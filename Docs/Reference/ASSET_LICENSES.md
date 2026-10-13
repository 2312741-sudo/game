# Asset Licenses & Attribution (ART-FIX-010)

All external 3D models and textures imported or upgraded for Project Trạm Chanh are strictly licensed under **Creative Commons Zero (CC0 1.0 Universal)** or **Creative Commons Attribution (CC-BY 3.0 / CC-BY 4.0)** from authorized open repositories (Quaternius, Poly Pizza, Google Poly Archive).

Commercial copyrighted assets, restrictive licenses prohibiting video game use, and game rips are strictly prohibited.

---

## Asset Registry

| Asset Name | Author / Creator | Source URL | License | Download Date | Project File Path |
|---|---|---|---|---|---|
| **Metal Sheet Roof "New" & "Old" \| Part-1** | CHEESE ARG 3D | https://sketchfab.com/3d-models/metal-sheet-roof-new-old-part-1-c7c46bf23a7e41068ab302cbc8152046 | CC-BY 4.0 | 2026-10-14 | `Assets/TramChanh/Art/Textures/T_CorrugatedMetal_BaseColor.png`<br>`Assets/TramChanh/Art/Textures/T_CorrugatedMetal_Normal.png`<br>`Assets/TramChanh/Art/Textures/T_CorrugatedMetal_MaskMap.png` |
| **Table & Chair Plastic (Bàn Ghế Nhựa Việt Nam)** | minchou19 | https://sketchfab.com/3d-models/table-chair-plastic-ban-ghe-nhua-viet-nam-7eb899988b4845adbe9458ea8245bf55 | CC-BY 4.0 | 2026-10-14 | `Assets/TramChanh/Art/Models/Furniture/SM_PlasticTableChairs_VN.obj`<br>`Assets/TramChanh/Art/Models/Furniture/SM_PlasticTableChairs_VN.mtl`<br>`Assets/TramChanh/Prefabs/Environment/PF_Street_PlasticTableChairs_VN.prefab` |
| **PSX Plastic Crate (Két Nhựa Vỉa Hè)** | ziro | https://sketchfab.com/3d-models/psx-plastic-crate-d227b9c97b83416db9fe090ae25ba11e | CC-BY 4.0 | 2026-10-14 | `Assets/TramChanh/Art/Models/Environment/SM_PlasticCrate_Street.obj`<br>`Assets/TramChanh/Art/Textures/T_PlasticCrate_Street_BaseColor.png`<br>`Assets/TramChanh/Prefabs/Environment/PF_PlasticCrate_Street.prefab` |
| **Urban Commuter Scooter** (`CUPIC_SCOOTER`) | Poly by Google | https://poly.pizza/m/9RqkkBOPNtd | CC-BY 3.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_Motorbike.obj`<br>`Assets/TramChanh/Art/Textures/T_Motorbike_BaseColor.png` |
| **Plastic Stool** (`Stool`) | Poly by Google | https://poly.pizza/m/2UeLZTX5nHr | CC-BY 3.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Furniture/SM_PlasticStool.obj` |
| **Tropical Planter Plant** (`Birds of paradise potted plant`) | Poly by Google | https://poly.pizza/m/folDA1yLeEF | CC-BY 3.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_PottedPlant.obj`<br>`Assets/TramChanh/Art/Textures/T_Plant_BirdsOfParadise_BaseColor.png` |
| **Urban Street Light** (`Street Light`) | Quaternius | https://poly.pizza/m/nFwrlcLvM5 | CC0 1.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_StreetLamp.obj` |
| **Curbside Trash Bin** (`Trashcan Small`) | Quaternius | https://poly.pizza/m/i7HDuYDLkx | CC0 1.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_TrashBin.obj` |
| **Low-Poly Sedan Car** (`Sports Car`) | Quaternius | https://poly.pizza/m/OyqKvX9xNh | CC0 1.0 | 2026-10-13 | `Assets/TramChanh/Art/Models/Environment/SM_Car_Sedan.obj`<br>`Assets/TramChanh/Prefabs/Environment/PF_Car_Sedan.prefab` |

---

## Technical Specifications & Polygon Budget Conformance

| Category | Budget Triangles | Implemented Model | Actual Triangles | Budget Textures | Actual Texture | Conformance |
|---|---|---|---|---|---|---|
| **Plastic Table & Chairs (VN)** | $\le 3,000$ tris | Bàn Ghế Nhựa Việt Nam (minchou19) | 2,025 tris (bàn 727 + 2 ghế 1,298) | $\le 2048 \times 2048$ | Palette Materials (Red + Blue) | **PASS** |
| **Street Plastic Crate** | $\le 1,500$ tris | PSX Plastic Crate (ziro) | 68 tris | $\le 2048 \times 2048$ | $512 \times 512$ PNG | **PASS** |
| **Motorbike** | $\le 4,000$ tris | Urban Commuter Scooter | 2,116 tris | $\le 2048 \times 2048$ | $1024 \times 1024$ PNG | **PASS** |
| **Road Vehicle** | $\le 4,000$ tris | Low-Poly Sedan Car | 2,216 tris | $\le 2048 \times 2048$ | Palette material | **PASS** |
| **Plastic Stool** | $\le 1,500$ tris | Low Plastic Stool | 448 tris | $\le 2048 \times 2048$ | $512 \times 512$ PNG | **PASS** |
| **Street Lamp** | $\le 1,500$ tris | Urban Curved Mast Lamp | 292 tris | $\le 2048 \times 2048$ | Material colors | **PASS** |
| **Street Tree (Trunk + Canopy)** | $\le 1,500$ tris | Tropical Street Tree (2 separate meshes) | 165 tris (79 trunk + 86 canopy) | $\le 2048 \times 2048$ | $512 \times 512$ PNG | **PASS** |
| **Trash Bin** | $\le 1,500$ tris | Curbside Green Bin | 432 tris | $\le 2048 \times 2048$ | Material colors | **PASS** |
| **Potted Plant** | $\le 1,500$ tris | Birds of Paradise Planter | 972 tris | $\le 2048 \times 2048$ | $1024 \times 1024$ PNG | **PASS** |

---

## Bắt Buộc Ghi Nhận Bản Quyền (Credits Attribution Notice)

> [!IMPORTANT]
> **Quy định bản quyền thương mại và phân phối:**
> Các tài sản mang giấy phép **Creative Commons Attribution (CC-BY 3.0 / CC-BY 4.0)** liệt kê ở trên (bao gồm các mô hình của **minchou19**, **ziro**, **CHEESE ARG 3D**, và **Poly by Google**) **bắt buộc phải được hiển thị rõ ràng trong màn hình Credits / Ghi nhận tác giả** của game (hoặc tài liệu kèm theo khi phát hành) với đầy đủ thông tin: tên tác phẩm, tên tác giả, giấy phép và liên kết nguồn tương ứng.

### Các Asset Tạm Dừng / Chưa Đưa Vào Game:
1. **Honda Super Cub C90** (`honda_super_cub_c90.glb`): Hiện chưa có URL Sketchfab, tên tác giả và giấy phép chính thức từ người dùng -> **Không đưa vào game**, giữ nguyên mô hình `PF_Motorbike` (Google Poly CC-BY 3.0) đạt chuẩn trên đường phố.
2. **Chó Vỉa Hè** (`dog.glb`): Hiện chưa có URL Sketchfab, tên tác giả và giấy phép chính thức từ người dùng -> **Không đưa vào game**.
3. **Casual Portrait in a Green T-Shirt** (`casual_portrait_in_a_green_t-shirt.glb`): 212.354 tris, bản scan người thật -> **Đã BỎ** theo đúng chỉ đạo để tránh rủi ro quyền hình ảnh chân dung và vi phạm ngân sách đa giác.
4. **Mô hình cây "Small Trees" (Poly by Google)**: Thử nghiệm trong ART-FIX-010 nhưng không đẹp bằng cây hiện tại -> Giữ nguyên cây gốc.
