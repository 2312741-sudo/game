# New Menu Art Assets Specification (ART-FIX-028)

This document provides detailed technical specifications, dimensions, triangle counts, texture resolutions, material shaders, and exact asset paths for the **4 New Menu Items** introduced in `ART-FIX-028`.

These assets are authored as **pure art assets** (models, textures, materials, and prefabs) with **no gameplay scripts or recipe formulas**, allowing gameplay programmers and designers to easily wire ScriptableObjects, recipes, workstation animations, and customer order logic later.

---

## 1. Nguồn gốc tài sản ART-FIX-028 & Đối chiếu nguồn chủ game (Asset Provenance & Source Audit)

> [!IMPORTANT]
> **Quy tắc bắt buộc từ chủ game:**
> - Thứ nào không có file nguồn của chủ game thì ghi rõ **"KHÔNG CÓ NGUỒN"**.
> - **Logo, chữ trên bảng giá và giá tiền phải dùng đúng texture thật của chủ game (`IMG_5205.JPG` cho Logo, `IMG_4932 2.JPG` cho bảng giá và chữ viết/giá tiền thật); không vẽ lại chữ hay logo mới.**
> - Mọi tài sản tạo mới trong đợt thử nghiệm ART-FIX-028 được liệt kê chi tiết dưới đây nhằm minh bạch nguồn gốc đối sánh giữa tư liệu gốc của chủ game và phần do Anti tự thêm.

### Bảng đối chiếu nguồn gốc tài sản ART-FIX-028

| STT | Tên tài sản (Asset Name) | Đường dẫn tài sản trong dự án | File nguồn của chủ game (hoặc "KHÔNG CÓ NGUỒN") | Phần do Anti tự thêm | Ghi chú & Đánh giá tuân thủ |
|:---|:---|:---|:---|:---|:---|
| 1 | **Honey Lemon Tea** (*Trà Chanh Mật Ong*) | `Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Drink_HoneyLemonTea.prefab`<br>`SM_Drink_HoneyLemonTea.obj`<br>`MAT_Drink_HoneyLemonTea.mat` | `IMG_5214.JPG`<br>(Ảnh cốc trà chanh hổ phách tại quầy, màu nước amber, lát chanh thật, túi trà stand-up pouch / `MAT_TeaLiquid_AmberLime.mat`) | - Mô hình 3D cốc nhựa take-away nắp cầu (`SM_Drink_HoneyLemonTea.obj`, 416 tris)<br>- Ống hút nghiêng xanh sọc xoắn<br>- Lát chanh 8 múi vẽ procedural bằng PIL<br>- Khối đá viên crystal<br>- Prefab và BoxCollider | Dựa trên màu sắc và lát chanh từ ảnh thực tế `IMG_5214.JPG`. Khung cốc nhựa và phụ kiện do Anti dựng 3D. |
| 2 | **Passionfruit Tea** (*Trà Chanh Dây*) | `Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Drink_PassionfruitTea.prefab`<br>`SM_Drink_PassionfruitTea.obj`<br>`MAT_Drink_PassionfruitTea.mat` | `IMG_5213.JPG`<br>(Ảnh cốc trà chanh dây vàng óng nhiệt đới tại quầy, màu cam vàng `#EDA417`, hạt chanh dây tự nhiên / `MAT_TeaLiquid_PassionFruit.mat`) | - Mô hình 3D cốc nhựa take-away nắp cầu (`SM_Drink_PassionfruitTea.obj`, 468 tris)<br>- Hạt chanh dây dạng đĩa nổi và chìm vẽ bằng PIL<br>- Ống hút, đá viên 3D<br>- Prefab và BoxCollider | Dựa trên sắc nước vàng cam và hạt chanh dây từ ảnh thực tế `IMG_5213.JPG`. Khung cốc nhựa và phụ kiện do Anti dựng 3D. |
| 3 | **Custard Cream cake** (*Bánh Kẹp Cuộn Kem Trứng*) | `Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Cake_CustardCream.prefab`<br>`SM_Cake_CustardCream.obj`<br>`MAT_Cake_CustardCream.mat` | `IMG_5210.JPG`<br>(Ảnh bánh kẹp nướng cuộn vỏ vàng nâu nẹp vân vỉ nướng Elmich thực tế tại quầy, kèm `IMG_5211.JPG`, `IMG_5244.JPG`) | - Ý tưởng và texture nhân kem trứng custard vàng mềm (chủ game chỉ chụp bánh nướng cuộn thực tế, chưa có ảnh cận nhân kem trứng riêng)<br>- Mô hình 3D bánh cuộn ống rỗng lộ nhân 2 đầu (340 tris)<br>- Texture vệt nướng và vừng rắc vẽ bằng PIL<br>- Prefab và BoxCollider | Cốt bánh lấy từ ảnh bánh cuộn thật `IMG_5210.JPG`. Nhân kem trứng do Anti tự sáng tạo và vẽ texture. |
| 4 | **Banana Flambée cake** (*Bánh Kẹp Cuộn Chuối Đốt Rượu*) | `Assets/TramChanh/Prefabs/Items/NewMenu/PF_Item_Cake_BananaFlambee.prefab`<br>`SM_Cake_BananaFlambee.obj`<br>`MAT_Cake_BananaFlambee.mat` | `IMG_5210.JPG`<br>(Ảnh bánh kẹp nướng cuộn vỏ vàng nâu nẹp vân vỉ nướng Elmich thực tế tại quầy làm cốt bánh) | - Ý tưởng và texture nhân chuối đốt rượu caramel flambée (tự sáng tạo nhân, vẽ lát chuối cắt tròn và vệt caramel cháy xém bằng PIL)<br>- Mô hình 3D bánh cuộn đính lát chuối ở 2 đầu hở (356 tris)<br>- Prefab và BoxCollider | Cốt bánh lấy từ ảnh bánh cuộn thật `IMG_5210.JPG`. Nhân chuối đốt rượu do Anti tự sáng tạo và vẽ texture. |
| 5 | **LOGO_TramChanh** | `Assets/TramChanh/Art/UI/Logo/LOGO_TramChanh.png`<br>`LOGO_TramChanh.png.meta` | `IMG_5205.JPG`<br>(Ảnh logo tròn thương hiệu chính thức của chủ quán Trạm Chanh, chữ "Trạm Chanh" viết tay tròn, quả chanh vàng; từng dùng trong biển hiệu `PF_Sign_TramChanh_New`) | - Anti đã **tự vẽ lại chữ và logo mới** bằng PIL (font sans-serif kỹ thuật số "TRẠM CHANH" in hoa kèm slogan "TRÀ CHANH PHỐ CỔ" và icon lát chanh vector tự vẽ) | **CẦN SỬA ĐỔI:** Logo phải dùng đúng texture thật `IMG_5205.JPG` của chủ game; không vẽ lại chữ hay logo mới. |
| 6 | **BG_Title** | `Assets/TramChanh/Art/UI/Backgrounds/BG_Title.png`<br>`BG_Title.png.meta` | **KHÔNG CÓ NGUỒN**<br>(Chủ game không cung cấp ảnh màn hình tiêu đề 1920x1080 ban đêm; chỉ có ảnh xe hàng ban ngày `IMG_5201.JPG` - `IMG_5204.JPG`) | - 100% đồ họa giả lập ban đêm vẽ bằng PIL (bầu trời đêm, quầy xe gỗ, dây bóng đèn LED vàng phát sáng, bóng đổ vỉa hè, hiệu ứng ánh sáng mờ vignette) | **KHÔNG CÓ NGUỒN.** Toàn bộ hình ảnh do script PIL tự tạo. |
| 7 | **T_MenuBoard_Prices** | `Assets/TramChanh/Art/Textures/Props/T_MenuBoard_Prices.png`<br>`T_MenuBoard_Prices.png.meta` | `IMG_4932 2.JPG`<br>(Ảnh tấm bảng đen viết phấn thực tế tại quầy Trạm Chanh, chứa chữ viết tay và giá tiền thật của quán; đã từng dùng tạo `T_Menu_Board_BaseColor.png`) | - Anti đã **tự vẽ lại chữ trên bảng giá và giá tiền mới** bằng PIL (dùng font Arial vi tính viết danh sách 8 món và giá tiền giả định 15k-25k trên nền đen số hóa) | **CẦN SỬA ĐỔI:** Chữ trên bảng giá và giá tiền phải dùng đúng texture thật `IMG_4932 2.JPG` của chủ game; không vẽ lại chữ hay logo mới. |
| 8 | **IC_Ice** (Icon Đá viên) | `Assets/TramChanh/Art/UI/Icons/IC_Ice.png` | **KHÔNG CÓ NGUỒN** | - Vẽ khối lập phương băng xanh cyan trong suốt bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 9 | **IC_Jelly** (Icon Thạch) | `Assets/TramChanh/Art/UI/Icons/IC_Jelly.png` | **KHÔNG CÓ NGUỒN** | - Vẽ khối thạch vàng hổ phách bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 10 | **IC_Batter** (Icon Bột bánh) | `Assets/TramChanh/Art/UI/Icons/IC_Batter.png` | **KHÔNG CÓ NGUỒN** | - Vẽ tô bột bánh và phới lồng đánh trứng bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 11 | **IC_Wrap** (Icon Giấy gói) | `Assets/TramChanh/Art/UI/Icons/IC_Wrap.png` | **KHÔNG CÓ NGUỒN**<br>(Tham chiếu gián tiếp chất liệu giấy từ `IMG_5212.JPG` nhưng không có icon đồ họa) | - Vẽ cuộn giấy kraft nâu bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 12 | **IC_Up_BiggerIceBin** (Nâng cấp Thùng đá to) | `Assets/TramChanh/Art/UI/Icons/IC_Up_BiggerIceBin.png` | **KHÔNG CÓ NGUỒN** | - Vẽ thùng đá kim loại viền vàng bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 13 | **IC_Up_Pantry** (Nâng cấp Kho nguyên liệu) | `Assets/TramChanh/Art/UI/Icons/IC_Up_Pantry.png` | **KHÔNG CÓ NGUỒN** | - Vẽ kệ tủ kho nguyên liệu gỗ bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 14 | **IC_Up_StringLights** (Nâng cấp Dây đèn lung linh) | `Assets/TramChanh/Art/UI/Icons/IC_Up_StringLights.png` | **KHÔNG CÓ NGUỒN** | - Vẽ chuỗi bóng đèn vàng phát sáng bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 15 | **IC_Up_WholesaleDeal** (Nâng cấp Mua sỉ giá hời) | `Assets/TramChanh/Art/UI/Icons/IC_Up_WholesaleDeal.png` | **KHÔNG CÓ NGUỒN** | - Vẽ biểu tượng bắt tay và túi tiền vàng bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 16 | **IC_Up_GoldenMenuBoard** (Nâng cấp Biển menu vàng) | `Assets/TramChanh/Art/UI/Icons/IC_Up_GoldenMenuBoard.png` | **KHÔNG CÓ NGUỒN** | - Vẽ bảng menu viền mạ vàng bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 17 | **IC_Star_Full** (Sao đầy uy tín) | `Assets/TramChanh/Art/UI/Icons/IC_Star_Full.png` | **KHÔNG CÓ NGUỒN** | - Vẽ ngôi sao vàng kim 5 cánh đổ bóng bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 18 | **IC_Star_Empty** (Sao rỗng) | `Assets/TramChanh/Art/UI/Icons/IC_Star_Empty.png` | **KHÔNG CÓ NGUỒN** | - Vẽ khung viền sao rỗng màu xám mờ bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 19 | **IC_Money** (Tiền tệ) | `Assets/TramChanh/Art/UI/Icons/IC_Money.png` | **KHÔNG CÓ NGUỒN** | - Vẽ đồng xu vàng và tờ tiền polymer bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 20 | **IC_Patience** (Độ kiên nhẫn) | `Assets/TramChanh/Art/UI/Icons/IC_Patience.png` | **KHÔNG CÓ NGUỒN** | - Vẽ đồng hồ cát cát vàng chỉ độ kiên nhẫn bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 21 | **IC_Goal** (Mục tiêu ngày) | `Assets/TramChanh/Art/UI/Icons/IC_Goal.png` | **KHÔNG CÓ NGUỒN** | - Vẽ hồng tâm bia bắn cắm mũi tên vàng bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 22 | **IC_Rain** (Thời tiết Mưa) | `Assets/TramChanh/Art/UI/Icons/IC_Rain.png` | **KHÔNG CÓ NGUỒN** | - Vẽ đám mây xám và những giọt mưa xanh lam bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 23 | **IC_Hot** (Thời tiết Nắng nóng) | `Assets/TramChanh/Art/UI/Icons/IC_Hot.png` | **KHÔNG CÓ NGUỒN** | - Vẽ mặt trời đỏ cam tỏa nhiệt bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 24 | **IC_PowerCut** (Sự kiện Mất điện) | `Assets/TramChanh/Art/UI/Icons/IC_PowerCut.png` | **KHÔNG CÓ NGUỒN** | - Vẽ bóng đèn xám tắt và tia chớp nổ bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 25 | **IC_Festival** (Sự kiện Lễ hội) | `Assets/TramChanh/Art/UI/Icons/IC_Festival.png` | **KHÔNG CÓ NGUỒN** | - Vẽ đèn lồng đỏ và pháo hoa rực rỡ bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 26 | **IC_Inspector** (Sự kiện Thanh tra) | `Assets/TramChanh/Art/UI/Icons/IC_Inspector.png` | **KHÔNG CÓ NGUỒN** | - Vẽ huy hiệu kiểm tra an toàn và bìa kẹp hồ sơ bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 27 | **IC_Clear** (Thời tiết Trong lành) | `Assets/TramChanh/Art/UI/Icons/IC_Clear.png` | **KHÔNG CÓ NGUỒN** | - Vẽ mặt trời dịu nhẹ và mây trắng bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 28 | **IC_Cust_Student** (Khách Học sinh) | `Assets/TramChanh/Art/UI/Icons/IC_Cust_Student.png` | **KHÔNG CÓ NGUỒN** | - Vẽ học sinh đeo ba lô đồng phục bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 29 | **IC_Cust_Family** (Khách Gia đình) | `Assets/TramChanh/Art/UI/Icons/IC_Cust_Family.png` | **KHÔNG CÓ NGUỒN** | - Vẽ nhóm gia đình phụ huynh và em bé bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 30 | **IC_Cust_Office** (Khách Văn phòng) | `Assets/TramChanh/Art/UI/Icons/IC_Cust_Office.png` | **KHÔNG CÓ NGUỒN** | - Vẽ nhân viên công sở áo sơ mi cà vạt bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |
| 31 | **IC_Cust_Regular** (Khách Quen) | `Assets/TramChanh/Art/UI/Icons/IC_Cust_Regular.png` | **KHÔNG CÓ NGUỒN** | - Vẽ khách quen giơ tay chào thân thiện bằng PIL (256x256) | Không có ảnh icon gốc từ chủ game. |

---

## 2. Executive Summary & Asset Matrix

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
