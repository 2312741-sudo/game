> **Errata (2026-10-06):** this source document is kept verbatim. The 500 ml measuring cup is the interactive cake-batter tool **`PF_BatterMeasureCup_500ml`** / **`SM_BatterMeasureCup_500ml`**. The names `PF_MeasuringCup_500ml`, `SM_MeasuringCup_500ml`, `PF_BatterCup`, `PF_BatterMeasureCup` and `SM_BatterMeasureCup` below are superseded. See `Docs/Reference/README.md`.

# TRẠM CHANH — 3D ASSET PIPELINE & PROMPT PACK
## Tài liệu chuyên biệt cho dựng 3D, chuẩn hóa asset và tích hợp Unity 6

**Project:** Trạm Chanh — Vietnamese Roadside Lemon Tea Simulator  
**Engine:** Unity 6  
**Phong cách:** Semi-realistic stylized Vietnamese street-food simulator  
**Đơn vị:** `1 Unity Unit = 1 meter`  
**Mục tiêu:** Biến toàn bộ quán thật thành bộ asset 3D modular, tối ưu, có thể tương tác trong gameplay.

---

# 1. Mục tiêu tài liệu

Tài liệu này dùng để:

- xác định toàn bộ asset 3D cần tạo;
- chia asset theo nhóm;
- quy định asset nào là static / interactive / animated;
- quy định kích thước gần đúng;
- quy định polycount;
- quy định texture;
- quy định pivot;
- quy định collider;
- quy định tên model/prefab;
- viết prompt chuẩn cho AI 3D;
- quy định cách cleanup trong Blender;
- quy định cách import Unity;
- chỉ rõ asset nào cần tách mesh vì gameplay;
- tránh AI tự bịa chi tiết không có trong quán thật.

---

# 2. Ground Truth — BẮT BUỘC GIỮ

## 2.1 Kích thước quầy thật

```text
Chiều ngang:          1.8 m
Chiều sâu:            0.8 m
Chiều cao mặt quầy:   1.0 m
Mặt quầy → mái:       1.2 m
Tổng chiều cao:       ~2.2 m
```

Unity:

```text
1 Unity Unit = 1 meter
```

Không scale model tùy ý sau khi đã khóa kích thước.

---

## 2.2 Bảng hiệu

Quầy thật cũ có chữ nổi/phát sáng `TRẠM CHANH`.

**KHÔNG DÙNG bảng hiệu cũ trong model cuối.**

Bắt buộc dùng bảng hiệu mới:

```text
- dạng hộp ngang
- nền trắng
- mặt trên vàng cam
- chữ “Trạm” màu đen
- chữ “Chanh” màu vàng cam
- dải trang trí màu ở mép dưới
- bo nhẹ hai đầu
```

Tên prefab:

```text
PF_Sign_TramChanh_New
```

Tên model:

```text
SM_Sign_TramChanh_New
```

Không để AI 3D tự tạo chữ tiếng Việt cuối cùng.

Geometry chỉ cần bảng hộp.

Chữ/logo phải dùng texture/decal chính xác.

---

# 3. Phân vai trong pipeline 3D

## 3.1 AI tạo 3D / Image-to-3D

Dùng một trong các tool phù hợp:

```text
Meshy
Tripo
Rodin
Hunyuan3D
hoặc tool image-to-3D tương đương
```

Vai trò:

```text
reference image
→ rough geometry
→ first-pass materials
```

Không coi output AI là asset game-ready ngay lập tức.

---

## 3.2 Blender

Blender là bước cleanup bắt buộc cho asset quan trọng.

Dùng Blender để:

- sửa topology;
- xóa geometry thừa;
- scale chính xác;
- đặt pivot;
- tách mesh;
- UV unwrap;
- chỉnh normals;
- tạo LOD nếu cần;
- tạo collider proxy nếu cần;
- chỉnh material slots;
- kiểm tra transform;
- export FBX/GLB.

---

## 3.3 Claude Code

Trong 3D pipeline, Claude Code không tạo mesh.

Vai trò:

- xác định interaction requirements;
- quyết định asset nào cần tách mesh;
- xác định state animation;
- review naming;
- review hierarchy;
- review gameplay requirements.

Ví dụ:

```text
Grill lid must be separate because gameplay opens/closes it.
```

---

## 3.4 Codex

Vai trò:

- import model vào Unity;
- set scale;
- set material;
- tạo prefab;
- tạo collider;
- tạo Animator;
- tạo interaction anchors;
- viết scripts;
- kết nối asset với gameplay.

---

## 3.5 Antigravity

Vai trò:

- validate file;
- validate missing material;
- validate missing mesh;
- validate scale;
- validate collider;
- validate prefab references;
- run scene/test;
- báo PASS/FAIL.

---

# 4. Quy tắc tạo asset

Không tạo cả quán thành một mesh.

Dùng modular asset.

Sai:

```text
SM_WholeStall_WithEverything
```

Đúng:

```text
SM_Stall_Base
SM_Stall_Frame
SM_Stall_Roof
SM_Stall_Counter
SM_Stall_Wheels
SM_Sign_TramChanh_New
SM_LED_Fixture
SM_EdisonBulb
```

Các item gameplay phải tách riêng.

---

# 5. Quy chuẩn mesh

## Static prop nhỏ

```text
300 – 3,000 tris
```

## Hero prop

```text
3,000 – 15,000 tris
```

## Quầy hoàn chỉnh

```text
15,000 – 30,000 tris
```

## NPC furniture

```text
500 – 5,000 tris
```

Không tăng polycount nếu silhouette không cần.

---

# 6. Quy chuẩn texture

## Hero asset

```text
2048 x 2048
```

## Prop trung bình

```text
1024 x 1024
```

## Prop nhỏ

```text
512 x 512
```

## Branding / Menu

```text
2048 px hoặc cao hơn nếu chữ nhỏ
```

PBR tối thiểu:

```text
BaseColor
Normal
Metallic
Roughness/Smoothness
AO
```

Unity URP có thể pack map sau.

---

# 7. Quy chuẩn Pivot

## Furniture

Pivot:

```text
ground center
```

## Equipment trên bàn

Pivot:

```text
bottom center
```

## Nắp máy

Pivot:

```text
hinge axis
```

## Door / flap

Pivot:

```text
edge hinge
```

## Sign

Pivot:

```text
rear center
```

## Handheld item

Pivot:

```text
logical grip point
```

---

# 8. Quy chuẩn collider

Không dùng MeshCollider cho mọi thứ.

Ưu tiên:

```text
BoxCollider
CapsuleCollider
SphereCollider
compound primitive colliders
```

MeshCollider chỉ dùng khi thật sự cần.

Interactive prop nhỏ:

```text
simple collider
+
InteractionPoint
```

---

# 9. Asset hierarchy tổng

```text
TramChanh_3D/
├── Stall/
├── Branding/
├── Lighting/
├── DrinkStation/
├── CakeStation/
├── Furniture/
├── Food/
├── Packaging/
├── Tools/
├── CustomerArea/
└── Environment/
```

---

# 10. ASSET 001 — QUẦY TRẠM CHANH

## ID

```text
ART-STALL-001
```

## Model

```text
SM_Stall_Base
SM_Stall_Counter
SM_Stall_Frame
SM_Stall_Roof
SM_Stall_Wheel
```

## Prefab

```text
PF_Stall_TramChanh
```

## Kích thước

```text
1.8m x 0.8m x 2.2m
```

## Polycount

```text
15k – 30k tris
```

## Texture

```text
2K
```

## Material

```text
dark painted metal
dark weathered wood
corrugated metal
stainless steel
```

## Collider

```text
compound BoxCollider
```

## Gameplay

Quầy là static environment.

Không merge equipment vào mesh.

## Prompt 3D

```text
Create a game-ready 3D model of a compact Vietnamese roadside lemon tea
and snack stall based on the supplied reference photos.

REAL DIMENSIONS:
width 1.8 meters
depth 0.8 meters
countertop height 1.0 meter
total height approximately 2.2 meters

STYLE:
semi-realistic stylized Vietnamese street-food simulator

STRUCTURE:
dark weathered wood and dark painted metal frame,
compact rectangular lower counter,
corrugated dark exterior panels,
dark brown wooden countertop,
angular A-frame style vertical supports,
corrugated metal roof,
small caster wheels,
handmade roadside-vendor construction,
slightly worn surfaces from outdoor daily use.

IMPORTANT:
Do not model the old illuminated TRAM CHANH letters.
Do not permanently attach small equipment.
Leave clean areas for interactive equipment.

GAME READY:
clean topology,
UV unwrapped,
PBR materials,
optimized,
correct real-world scale,
separate logical meshes,
no unnecessary interior geometry.

No people, no chairs, no tables, no street, no building.
```

## Negative Prompt

```text
food truck,
modern kiosk,
shipping container,
old glowing TRAM CHANH letters,
people,
cars,
chairs,
tables,
random logos,
cartoon proportions,
oversized wheels,
clean luxury restaurant
```

---

# 11. ASSET 002 — BẢNG HIỆU TRẠM CHANH MỚI

## ID

```text
ART-BRAND-001
```

## Model

```text
SM_Sign_TramChanh_New
```

## Prefab

```text
PF_Sign_TramChanh_New
```

## Shape

```text
horizontal rectangular lightbox
softly rounded side edges
```

## Material

```text
white body
mustard-orange top
```

## Branding

```text
Trạm = black
Chanh = mustard-orange
colored stripe under sign
```

## Important

AI tạo geometry.

Logo/text dùng texture thật.

## Polycount

```text
500 – 2,000 tris
```

## Texture

```text
2048 x 512
```

## Pivot

```text
rear center
```

## Collider

```text
BoxCollider
```

## Prompt

```text
Create only the physical 3D lightbox shape of the supplied Tram Chanh sign.

Long horizontal rectangular box,
slightly rounded side edges,
white main housing,
warm mustard-orange top surface,
thin decorative strip area along the lower front edge.

Do not generate final text or logos as geometry.
The final “Trạm Chanh” branding will be applied as an exact texture in Unity.

Game-ready low-poly asset,
clean UVs,
flat rear mounting surface,
correct normals,
PBR material,
isolated object.
```

---

# 12. ASSET 003 — MÁI TÔN

## Model

```text
SM_Stall_Roof
```

## Material

```text
dark corrugated sheet metal
```

## Polycount

```text
1k – 4k tris
```

## Notes

Không cần model từng sóng tôn cực chi tiết nếu normal map đủ.

Silhouette quan trọng hơn.

## Prompt

```text
Create a modular corrugated metal roof panel for a small Vietnamese
street-food stall.

Dark painted sheet metal,
visible corrugated ridges,
slightly worn edges,
thin real sheet-metal thickness,
semi-realistic stylized look,
optimized game-ready topology.

No support frame.
No sign.
No lights.
Isolated roof component.
```

---

# 13. ASSET 004 — KHUNG QUẦY

## Model

```text
SM_Stall_Frame
```

## Material

```text
dark painted metal / dark wood
```

## Prompt

```text
Create the structural upper frame of a compact Vietnamese roadside stall.

Use thick dark rectangular beams.
The side supports lean outward slightly and create an angular A-frame /
trapezoid silhouette similar to handmade mobile street stalls.

Realistic dimensions for a stall 1.8m wide and 2.2m total height.
Game-ready, clean topology, separate from roof and counter.
```

---

# 14. ASSET 005 — BÁNH XE QUẦY

## Model

```text
SM_Stall_CasterWheel
```

## Prefab

```text
PF_Stall_CasterWheel
```

## Polycount

```text
300 – 1,200 tris
```

## Pivot

```text
wheel axle
```

## Prompt

```text
Small heavy-duty caster wheel for a mobile Vietnamese food stall,
black rubber wheel,
simple metal bracket,
slightly worn,
practical commercial hardware,
game-ready low-poly,
isolated object.
```

---

# 15. ASSET 006 — LED STRIP FIXTURE

## Model

```text
SM_LEDStrip
```

## Prefab

```text
PF_LEDStrip
```

## Note

Actual Light component tạo trong Unity.

Prompt:

```text
Slim LED strip fixture mounted underneath a street-food stall roof,
simple aluminum/plastic channel,
white translucent diffuser,
game-ready low-poly,
no emitted lighting baked into geometry.
```

---

# 16. ASSET 007 — BÓNG EDISON

## Model

```text
SM_EdisonBulb
```

## Prefab

```text
PF_EdisonBulb
```

## Prompt

```text
Small hanging Edison-style exposed bulb with black cable and simple socket,
warm vintage roadside food-stall appearance,
transparent glass bulb,
optimized game-ready geometry,
isolated object.
```

---

# 17. ASSET 008 — RACK ĐỎ ĐỰNG TÚI TRÀ

## ID

```text
ART-DRINK-001
```

## Model

```text
SM_RedTeaRack
```

## Prefab

```text
PF_RedTeaRack
```

## Gameplay

Interactive container.

Chứa các túi trà được đong sẵn.

## Polycount

```text
500 – 2,000 tris
```

## Collider

```text
BoxCollider
```

## Prompt

```text
Create a red plastic storage rack / basket used in a Vietnamese drink stall.

Rectangular open-top plastic rack,
bright red molded plastic,
practical cheap commercial kitchen storage,
slotted or perforated side walls,
slightly worn from daily use,
sized to store multiple pre-portioned drink bags.

Game-ready,
low-poly,
clean topology,
isolated object.
```

---

# 18. ASSET 009 — TÚI TRÀ ĐONG SẴN

## ID

```text
ART-DRINK-002
```

## Model

```text
SM_TeaBag_PrePortioned
```

## Prefab

```text
PF_TeaBag_PrePortioned
```

## Gameplay states

```text
Stored
PickedUp
Opened
ToppingsAdded
IceAdded
Shaken
Wiped
Ready
```

## Required meshes

Tốt nhất dùng:

```text
Bag_Closed
Bag_Open
```

hoặc blendshape/animation.

## Polycount

```text
300 – 1,500 tris
```

## Material

```text
transparent flexible plastic
tea liquid
```

## Prompt

```text
Create a clear flexible plastic drink bag used for Vietnamese takeaway tea.

The bag contains a pre-portioned amount of amber tea liquid.
Soft transparent plastic,
slightly wrinkled,
openable top seam,
realistic gravity shape,
suitable for handheld gameplay.

No straw.
No toppings.
No ice.
No printed text.

Game-ready,
low-poly,
clean UV,
transparent material,
isolated object.
```

---

# 19. ASSET 010 — KHAY TOPPING ÂM BÀN

## Model

```text
SM_ToppingStation
```

## Prefab

```text
PF_ToppingStation
```

## Components

```text
StationFrame
CoconutJellyBin
LemonJellyBin
TransparentCover
```

## Gameplay

Mỗi topping bin là interaction target riêng.

## Prompt

```text
Create a compact stainless-steel recessed topping station for a Vietnamese
street drink stall.

Rectangular stainless-steel insert,
multiple food bins,
transparent acrylic cover,
clean commercial food-prep design,
slightly used but sanitary,
optimized for game use.

Separate the acrylic cover and each topping bin into separate meshes.
```

---

# 20. ASSET 011 — THẠCH DỪA

## Model

```text
SM_Topping_CoconutJelly
```

## Prefab

```text
PF_Topping_CoconutJelly
```

## Note

Không cần mỗi viên thạch là rigidbody riêng.

Dùng volume mesh / instancing.

## Prompt

```text
Small translucent white coconut jelly cubes for a Vietnamese tea topping,
slightly irregular cubes,
wet glossy surface,
semi-transparent,
game-ready food prop,
clustered serving portion,
isolated.
```

---

# 21. ASSET 012 — THẠCH CHANH

## Model

```text
SM_Topping_LemonJelly
```

## Prompt

```text
Small translucent pale yellow-green lemon jelly pieces,
wet glossy food texture,
slightly irregular,
semi-transparent,
game-ready topping cluster,
isolated object.
```

---

# 22. ASSET 013 — THÙNG ĐÁ ÂM BÀN

## Model

```text
SM_IceBin
```

## Prefab

```text
PF_IceBin
```

## Components

```text
Bin
Lid optional
IceVolume
```

## Prompt

```text
Compact recessed stainless-steel ice bin used in a Vietnamese drink stall,
rectangular commercial food-service container,
brushed stainless steel,
filled with rough crushed ice,
slightly wet condensation,
game-ready.
Separate ice volume from metal bin.
```

---

# 23. ASSET 014 — MUỖNG XÚC ĐÁ

## Model

```text
SM_IceScoop
```

## Prefab

```text
PF_IceScoop
```

## Pivot

```text
handle grip
```

## Prompt

```text
Small stainless-steel ice scoop,
simple commercial kitchen tool,
slightly scratched,
realistic proportions,
game-ready low-poly,
pivot suitable for hand gripping.
```

---

# 24. ASSET 015 — CA ĐONG 500ML

## Model

```text
SM_MeasuringCup_500ml
```

## Prefab

```text
PF_MeasuringCup_500ml
```

## Prompt

```text
Transparent plastic 500ml measuring cup used in a street-food stall,
clear molded plastic,
simple handle,
subtle measurement markings,
slightly scratched from use,
game-ready low-poly,
isolated object.
```

---

# 25. ASSET 016 — BÌNH PUMP

## Model

```text
SM_PumpBottle
```

## Prefab

```text
PF_PumpBottle
```

## Prompt

```text
Clear commercial syrup pump bottle with black pump dispenser,
used in a small Vietnamese drink stall,
transparent container,
slightly viscous colored syrup inside,
simple practical design,
game-ready low-poly.
```

---

# 26. ASSET 017 — KHĂN LAU BỊCH

## Model

```text
SM_WipeCloth
```

## Prefab

```text
PF_WipeCloth
```

## Gameplay

Dùng cho bước:

```text
lau bịch
```

## Prompt

```text
Small reusable cleaning cloth used at a drink preparation counter,
soft folded fabric,
slightly damp,
simple practical shape,
game-ready low-poly.
```

---

# 27. ASSET 018 — MÁY NƯỚNG BÁNH ELMICH

## ID

```text
ART-CAKE-001
```

## Model hierarchy

```text
SM_Grill_Base
SM_Grill_LowerPlate
SM_Grill_Lid
SM_Grill_UpperPlate
SM_Grill_Handle
SM_Grill_Display
```

## Prefab

```text
PF_Grill_Elmich
```

## Critical

Nắp phải tách mesh.

Pivot nắp đặt tại bản lề.

## Gameplay

```text
Open
Pour
Close
Cook
Open
Remove/Flip
```

## Appearance

- thân máy đen;
- nắp trên;
- tay cầm kim loại;
- bản lề phía sau;
- mặt trên và dưới đều có rãnh dọc song song;
- bảng điện tử LED đỏ;
- có hiển thị nhiệt độ/thời gian;
- reference cho thấy khoảng 160°C.

## Polycount

```text
5k – 12k tris
```

## Texture

```text
2K
```

## Prompt

```text
Create a game-ready electric contact grill / panini press based closely on
the supplied reference images.

Black rectangular appliance body.
Hinged upper lid.
Horizontal metal handle.
Rear hinge.
Both the upper and lower cooking plates contain parallel vertical grill
ridges.
Front electronic control panel with small red LED display and power button.

The lid must be a separate mesh.
The upper grill plate must move with the lid.
Place the lid pivot exactly along the rear hinge axis.

Semi-realistic stylized game asset.
Dark plastic and black coated metal,
brushed stainless-steel handle,
slightly used commercial kitchen appearance.

Do not bake food into the grill.
No environment.
No hands.
No extra appliances.

Clean topology,
PBR,
proper UV,
Unity-ready scale.
```

---

# 28. ASSET 019 — CA ĐONG BỘT

## Model

```text
SM_BatterMeasureCup
```

## Prefab

```text
PF_BatterMeasureCup
```

## Gameplay

Có mức bột.

Prompt:

```text
Small practical measuring cup used to portion cake batter,
transparent or semi-transparent food-safe plastic,
simple handle,
game-ready,
handheld,
isolated object.
```

---

# 29. ASSET 020 — BỘT BÁNH

## Model

```text
SM_BatterVolume
```

## Prefab

```text
PF_BatterPortion
```

## Note

Batter có thể là shader/mesh morph.

Prompt:

```text
Viscous pale cake batter portion,
smooth thick liquid,
slightly glossy,
game-ready food material,
suitable for pour animation.
```

---

# 30. ASSET 021 — SPATULA CÁN GỖ

## Model

```text
SM_Spatula_WoodHandle
```

## Prefab

```text
PF_Spatula_WoodHandle
```

## Pivot

```text
grip point
```

## Prompt

```text
Flat metal cooking spatula with a short worn wooden handle,
used at a Vietnamese street-food grill,
simple practical kitchen utensil,
slightly scratched metal,
game-ready low-poly.
```

---

# 31. ASSET 022 — KÉO ĐỎ XÁM

## Model

```text
SM_Scissors_RedGray
```

## Prefab

```text
PF_Scissors_RedGray
```

## Prompt

```text
Kitchen scissors with red and gray plastic handles,
short stainless-steel blades,
slightly worn from food preparation,
game-ready handheld prop.
```

---

# 32. ASSET 023 — TÚI XỐT / PIPING BAG

## Models

```text
SM_SauceBag_Mango
SM_SauceBag_Chocolate
SM_SauceBag_Cheese
```

## Prefabs

```text
PF_SauceBag_Mango
PF_SauceBag_Chocolate
PF_SauceBag_Cheese
```

## Prompt

```text
Soft plastic piping bag filled with thick food sauce,
twisted or clipped top,
small cut dispensing tip,
realistic flexible bag shape,
slightly wrinkled,
game-ready handheld prop.

Generate a neutral bag geometry.
Sauce color will be controlled by material.
```

---

# 33. ASSET 024 — BÁNH RAW

## Model

```text
SM_Cake_Raw
```

## Prefab

```text
PF_Cake_Raw
```

Prompt:

```text
Thin freshly poured uncooked cake batter layer on a flat grill,
soft pale surface,
slightly irregular rectangular shape,
game-ready food mesh,
no plate,
no environment.
```

---

# 34. ASSET 025 — BÁNH CHÍN

## Model

```text
SM_Cake_Cooked
```

## Prefab

```text
PF_Cake_Cooked
```

## Appearance

Có vệt nướng dài do rãnh máy.

Prompt:

```text
Thin cooked Vietnamese grilled rolled cake sheet,
golden-brown surface,
distinct parallel dark grill lines from a contact grill,
slightly crisp edges,
flexible enough to roll,
game-ready food mesh.
```

---

# 35. ASSET 026 — BÁNH ĐÃ CẮT / CHUẨN BỊ CUỘN

## Model

```text
SM_Cake_Cut
```

Prompt:

```text
Thin grilled cake sheet cut into preparation sections,
golden surface,
parallel grill marks,
ready for sauce and vertical rolling,
game-ready food asset.
```

---

# 36. ASSET 027 — BÁNH CUỘN

## Model

```text
SM_Cake_Rolled
```

## Prefab

```text
PF_Cake_Rolled
```

Prompt:

```text
Long vertically rolled grilled cake,
thin golden-brown layers,
parallel toasted grill marks visible on outer surface,
compact street-food serving shape,
game-ready food mesh.
```

---

# 37. ASSET 028 — GIẤY GÓI BÁNH

## Model

```text
SM_CakeWrappingPaper
```

## Prefab

```text
PF_CakeWrappingPaper
```

## Prompt

```text
Simple rectangular food wrapping paper for a Vietnamese street snack,
thin slightly flexible paper,
clean cream or white paper,
subtle folds,
game-ready low-poly.
```

---

# 38. ASSET 029 — BÁNH ĐÃ GÓI

## Model

```text
SM_Cake_Wrapped
```

## Prefab

```text
PF_Cake_Wrapped
```

Prompt:

```text
A long rolled grilled cake partially wrapped in simple food paper,
easy to hold,
street-food presentation,
semi-realistic stylized,
game-ready.
```

---

# 39. ASSET 030 — GHẾ NHỰA THẤP

## Model

```text
SM_PlasticStool
```

## Prefab

```text
PF_PlasticStool
```

## Approx dimensions

```text
width 0.28 – 0.32m
depth 0.28 – 0.32m
height 0.25 – 0.30m
```

## Polycount

```text
500 – 2,000 tris
```

## Prompt

```text
Small low Vietnamese plastic street stool,
stackable molded plastic,
square seat with rounded corners,
short legs,
cheap practical outdoor furniture,
slightly scratched and worn,
semi-realistic stylized,
game-ready low-poly.
```

---

# 40. ASSET 031 — SỌT VÀNG LÀM BÀN

## Model

```text
SM_YellowCrate
```

## Prefab

```text
PF_YellowCrateTable
```

## Prompt

```text
Yellow plastic commercial storage crate used upside-down as a low street
table base in Vietnam,
molded plastic grid sides,
slightly worn,
game-ready low-poly,
correct practical proportions.
```

---

# 41. ASSET 032 — KHAY INOX MẶT BÀN

## Model

```text
SM_StainlessTrayTabletop
```

## Prefab

```text
PF_StainlessTrayTabletop
```

## Prompt

```text
Shallow rectangular stainless-steel serving tray used as the tabletop on
top of a plastic crate,
brushed metal,
slightly scratched,
thin raised rim,
game-ready.
```

---

# 42. ASSET 033 — MENU LỚN

## Model

```text
SM_MenuBoard_Large
```

## Prefab

```text
PF_MenuBoard_Large
```

## Important

Không để AI generate chữ.

Dùng texture menu thật.

## Prompt

```text
Simple freestanding rectangular menu board for a Vietnamese roadside drink
stall,
thin black or dark frame,
flat front surface prepared for a texture,
simple tripod or stand,
game-ready low-poly.

Do not generate menu text.
```

---

# 43. ASSET 034 — MENU NHỎ TRÊN QUẦY

## Model

```text
SM_MenuBoard_Counter
```

## Prefab

```text
PF_MenuBoard_Counter
```

Prompt:

```text
Small countertop menu display,
rectangular flat card holder,
slightly angled for customer viewing,
simple black/dark frame,
game-ready.
Do not generate text.
```

---

# 44. ASSET 035 — MÓC / RACK TREO ĐỒ ĂN VẶT

## Model

```text
SM_SnackDisplayRack
```

## Prefab

```text
PF_SnackDisplayRack
```

Prompt:

```text
Simple metal/wire hanging display rack attached to the front of a
Vietnamese street-food stall,
thin black metal rods,
multiple hooks for packaged snacks,
game-ready low-poly.
No snack bags included.
```

---

# 45. ASSET 036 — TÚI ĐỒ ĂN VẶT

Các loại:

```text
PF_SnackBag_BanhTrang
PF_SnackBag_ChipChip
PF_SnackBag_BanhGau
PF_SnackBag_SunflowerSeed
```

Dùng chung geometry nếu có thể.

Chỉ đổi texture.

Prompt:

```text
Small sealed transparent or printed plastic snack bag,
soft slightly wrinkled packaging,
lightweight hanging product,
simple game-ready geometry,
prepared for interchangeable texture labels.
```

---

# 46. ASSET 037 — THÙNG ĐÁ ĐỎ

## Model

```text
SM_RedCooler
```

## Prefab

```text
PF_RedCooler
```

Prompt:

```text
Small red insulated cooler box used behind a roadside drink stall,
rectangular plastic body,
hinged lid,
slightly worn,
practical commercial use,
game-ready low-poly.
```

---

# 47. ASSET 038 — GIỎ NHỰA

## Model

```text
SM_PlasticBasket
```

## Prefab

```text
PF_PlasticBasket
```

Prompt:

```text
Small rectangular molded plastic utility basket used on a drink stall
counter,
open top,
perforated/slotted sides,
cheap practical design,
slightly worn,
game-ready.
```

---

# 48. ASSET 039 — TÚI GIẤY

## Model

```text
SM_PaperBag
```

## Prefab

```text
PF_PaperBag
```

Prompt:

```text
Small kraft paper takeaway bag,
simple folded paper construction,
slightly creased,
openable top,
game-ready low-poly,
isolated object.
```

---

# 49. ASSET 040 — KHU READY / TRẢ ĐƠN

## Model

Có thể chỉ dùng counter area + interaction marker.

Không nhất thiết cần mesh riêng.

Prefab:

```text
PF_ReadyCounterPoint
```

Hierarchy:

```text
PF_ReadyCounterPoint
├── InteractionTrigger
├── DrinkPlacement
├── CakePlacement
└── OrderIndicator
```

---

# 50. LOD policy

## Hero equipment

```text
LOD0 100%
LOD1 50–60%
LOD2 20–30%
```

## Small props

Không cần LOD nếu prop nhỏ và số lượng ít.

## Repeated furniture

Ghế, bàn crate nên có LOD.

---

# 51. Material library

Không tạo material mới cho từng object nếu giống nhau.

Dùng material library:

```text
MAT_Wood_Dark
MAT_Metal_BlackPainted
MAT_Metal_Stainless
MAT_Plastic_Red
MAT_Plastic_Yellow
MAT_Plastic_Transparent
MAT_Glass_Clear
MAT_Food_JellyWhite
MAT_Food_JellyLemon
MAT_Food_Tea
MAT_Food_Cake
MAT_Paper_Kraft
```

---

# 52. Texture naming

```text
T_[Asset]_BaseColor
T_[Asset]_Normal
T_[Asset]_Mask
T_[Asset]_AO
```

Ví dụ:

```text
T_Grill_BaseColor
T_Grill_Normal
T_Grill_Mask
```

---

# 53. Unity import settings

## FBX

```text
Scale Factor: 1
Convert Units: On nếu cần
Read/Write: Off trừ khi runtime cần
Generate Colliders: Off
Import Cameras: Off
Import Lights: Off
```

## Normals

```text
Import hoặc Calculate theo asset
```

## Tangents

```text
Calculate Mikktspace
```

---

# 54. Prefab setup checklist

Mỗi prefab cần:

```text
[ ] correct scale
[ ] correct pivot
[ ] collider
[ ] material
[ ] no missing texture
[ ] no missing script
[ ] interaction point
[ ] correct layer
[ ] correct tag nếu dùng
[ ] static flag nếu phù hợp
[ ] prefab name đúng convention
```

---

# 55. Asset validation checklist

Trước khi accept:

```text
[ ] giống reference thật
[ ] không tự bịa logo
[ ] không có bảng hiệu cũ
[ ] real-world scale đúng
[ ] normals đúng
[ ] UV không overlap sai
[ ] texture không mờ
[ ] polycount hợp lý
[ ] pivot đúng
[ ] collider đơn giản
[ ] hierarchy hợp lý
[ ] animation part được tách mesh
```

---

# 56. Animation-required assets

Bắt buộc tách geometry cho:

```text
Grill lid
Tea bag closed/open
Ice scoop
Scissors
Spatula
Sauce bag
Cake roll
Wrapping paper
```

---

# 57. Không bake gameplay state vào một mesh duy nhất

Ví dụ sai:

```text
Grill_With_Cake_Baked_In
```

Đúng:

```text
PF_Grill_Elmich
PF_Cake_Raw
PF_Cake_Cooked
```

---

# 58. Interaction anchor convention

Trong prefab:

```text
Anchors/
├── HandGrip
├── InteractionPoint
├── PlacementPoint
├── PourPoint
└── OutputPoint
```

Không nhất thiết asset nào cũng có đủ 5.

---

# 59. Drink station final hierarchy

```text
PF_DrinkStation
├── TeaRack
├── ToppingStation
│   ├── CoconutJellyBin
│   └── LemonJellyBin
├── IceBin
├── IceScoop
├── PumpBottles
├── WipeArea
└── ReadyPoint
```

---

# 60. Cake station final hierarchy

```text
PF_CakeStation
├── Grill
├── BatterArea
├── MeasuringCup
├── Spatula
├── Scissors
├── SauceArea
├── RollArea
├── WrappingArea
└── ReadyPoint
```

---

# 61. Scene scale test

Tạo Test Scene:

```text
SCN_AssetScaleTest
```

Có:

```text
1.7m mannequin
1m cube
stall
stool
crate table
grill
tea rack
```

Mục tiêu:

check scale bằng mắt trước khi integrate.

---

# 62. 3D production priority

## P0

```text
[ ] Stall
[ ] New Tram Chanh Sign
[ ] Grill
[ ] Tea Rack
[ ] Tea Bag
[ ] Topping Station
[ ] Ice Bin
[ ] Ice Scoop
[ ] Measuring Cup
[ ] Spatula
[ ] Scissors
[ ] Sauce Bag
[ ] Wrapping Paper
[ ] Cake Raw/Cooked/Rolled
```

## P1

```text
[ ] Stool
[ ] Yellow Crate Table
[ ] Stainless Tray
[ ] Large Menu
[ ] Counter Menu
[ ] Snack Rack
[ ] Snack Bags
[ ] Pump Bottles
[ ] Red Cooler
```

## P2

```text
[ ] Additional storage
[ ] additional cups
[ ] decorative props
[ ] street environment
[ ] building facade
[ ] roadside props
```

---

# 63. Asset production order

Khuyến nghị:

```text
01 Stall blockout
02 Sign
03 Grill
04 Tea rack
05 Tea bag
06 Topping station
07 Ice bin
08 Tools
09 Cake assets
10 Stool
11 Crate table
12 Menus
13 Snack display
14 Decorative props
15 Environment
```

---

# 64. AI 3D prompt template chung

Dùng format:

```text
OBJECT:
[asset name]

REFERENCE:
Follow supplied real-world photos closely.

REAL SIZE:
[dimensions]

STYLE:
Semi-realistic stylized Vietnamese street-food simulator.

SHAPE:
[geometry details]

MATERIAL:
[PBR material]

GAMEPLAY:
[state/interaction]

MESH REQUIREMENTS:
clean topology
optimized
correct normals
UV unwrapped
logical mesh separation
Unity-ready scale

PIVOT:
[pivot description]

DO NOT:
[negative constraints]

ISOLATION:
isolated object
neutral background
no people
no unrelated props
```

---

# 65. Negative prompt template

```text
extra objects,
random logos,
invented Vietnamese text,
distorted text,
wrong proportions,
fantasy design,
luxury restaurant design,
cartoon exaggeration,
floating geometry,
broken topology,
unnecessary high-poly details,
people,
hands,
background environment
```

---

# 66. Prompt cho Blender cleanup agent / artist

```text
Clean this AI-generated model for Unity 6.

Requirements:
- preserve silhouette from the reference
- real-world scale
- remove hidden/internal geometry
- fix non-manifold geometry
- reduce unnecessary triangles
- apply transforms
- correct normals
- create clean UVs
- separate animated/interactable parts
- set correct pivots
- create simple collider proxies where useful
- keep material slots minimal
- export FBX with meter scale

Do not redesign the asset.
```

---

# 67. Prompt cho Codex khi import asset

```text
Import this approved 3D asset into Unity 6.

Requirements:
- preserve 1 unit = 1 meter
- create prefab using project naming conventions
- assign correct materials
- create simple colliders
- add interaction anchors if required
- configure Animator only if asset has moving parts
- do not modify mesh geometry unless required for Unity compatibility
- report all imported files and prefab changes
- verify no missing materials or references
```

---

# 68. Prompt cho Antigravity QA asset

```text
Validate the imported 3D asset against its asset specification.

Check:
- correct prefab name
- real-world scale
- missing materials
- missing textures
- missing scripts
- collider quality
- pivot behavior
- animation hierarchy
- interaction anchor existence
- Console errors
- prefab overrides

Return PASS/FAIL for each requirement.
Do not redesign the asset.
```

---

# 69. File structure 3D

```text
Assets/TramChanh/Art/
├── Models/
│   ├── Stall/
│   ├── Branding/
│   ├── DrinkStation/
│   ├── CakeStation/
│   ├── Furniture/
│   ├── Food/
│   ├── Packaging/
│   └── Props/
├── Materials/
├── Textures/
└── Animations/
```

---

# 70. Source file structure

Ngoài Unity:

```text
ArtSource/
├── References/
├── Blender/
├── AI_Generated/
├── Export/
└── Textures/
```

---

# 71. Version naming

Ví dụ:

```text
SM_Grill_Elmich_v001.blend
SM_Grill_Elmich_v002.blend
SM_Grill_Elmich_FINAL.fbx
```

Trong Unity không cần version suffix nếu dùng source control.

---

# 72. Không dùng text generated by AI cho branding

Các asset:

```text
Sign
Menu
Snack label
Packaging
```

phải dùng texture chính xác.

AI thường làm sai:

```text
Trạm
dấu tiếng Việt
giá
tên sản phẩm
```

---

# 73. Thứ tự làm asset cho vertical slice đầu tiên

Vertical slice chỉ cần:

```text
PF_Stall_TramChanh
PF_Sign_TramChanh_New
PF_RedTeaRack
PF_TeaBag_PrePortioned
PF_ToppingStation
PF_IceBin
PF_IceScoop
PF_Grill_Elmich
PF_BatterMeasureCup
PF_Spatula_WoodHandle
PF_Scissors_RedGray
PF_SauceBag
PF_Cake_Raw
PF_Cake_Cooked
PF_Cake_Rolled
PF_CakeWrappingPaper
PF_ReadyCounterPoint
PF_PlasticStool
PF_YellowCrateTable
```

Chưa cần toàn bộ snack và decoration.

---

# 74. Definition of Done cho asset 3D

Asset chỉ được coi là xong khi:

```text
[ ] model giống reference
[ ] scale đúng
[ ] pivot đúng
[ ] topology sạch
[ ] normals đúng
[ ] UV đúng
[ ] material đúng
[ ] texture đủ nét
[ ] polycount hợp lý
[ ] collider phù hợp
[ ] prefab được tạo
[ ] interaction point đúng
[ ] animation part tách mesh
[ ] Unity không Console Error
[ ] QA pass
```

---

# 75. PHASE 3D-A — Blockout

Mục tiêu:

- kiểm tra kích thước;
- kiểm tra gameplay placement;
- kiểm tra player reach;
- không cần texture đẹp.

Asset dùng primitive/low detail.

Không mất thời gian polish.

---

# 76. PHASE 3D-B — Gameplay-ready

Mục tiêu:

- asset interaction hoàn chỉnh;
- pivot đúng;
- collider đúng;
- animation đúng;
- material cơ bản.

Gameplay phải chạy end-to-end.

---

# 77. PHASE 3D-C — Art polish

Sau khi gameplay ổn:

- texture;
- wear;
- dirt;
- lighting response;
- LOD;
- material optimization;
- decals;
- branding.

---

# 78. Thứ tự ưu tiên chất lượng

```text
1. Scale
2. Silhouette
3. Gameplay compatibility
4. Pivot
5. Collider
6. Material
7. Texture
8. Micro detail
```

Không hy sinh gameplay để lấy chi tiết thừa.

---

# 79. Checklist riêng cho quầy

```text
[ ] width = 1.8m
[ ] depth = 0.8m
[ ] counter = 1.0m
[ ] total height ~2.2m
[ ] old sign removed
[ ] new sign mounted
[ ] roof silhouette correct
[ ] A-frame support correct
[ ] wheels correct
[ ] counter usable
[ ] enough workspace for stations
```

---

# 80. Checklist riêng cho grill

```text
[ ] lid separate
[ ] hinge pivot correct
[ ] upper plate follows lid
[ ] lower plate fixed
[ ] parallel grill ridges
[ ] metal handle
[ ] front LED display
[ ] gameplay cake placement point
[ ] open/close animation possible
```

---

# 81. Checklist riêng cho tea bag

```text
[ ] tea pre-portioned
[ ] no manual tea measuring
[ ] bag fits red rack
[ ] open state exists
[ ] topping can visually appear
[ ] ice can visually appear
[ ] shake animation possible
[ ] wipe interaction possible
```

---

# 82. Checklist riêng cho topping station

```text
[ ] coconut jelly bin
[ ] lemon jelly bin
[ ] cover separate
[ ] player can reach both bins
[ ] collider does not block scoop interaction
[ ] fits counter depth 0.8m
```

---

# 83. 3D tasks ownership table

| Task | Primary | Support | QA |
|---|---|---|---|
| Asset spec | Claude Code | Bạn | Antigravity |
| AI mesh generation | 3D Generator | Bạn | — |
| Blender cleanup | Blender/Artist | — | — |
| Unity import | Codex | — | Antigravity |
| Collider | Codex | Claude review | Antigravity |
| Interaction anchors | Codex | Claude review | Antigravity |
| Animation hierarchy | Codex + Blender | Claude review | Antigravity |
| Branding texture | Art/Texture | Bạn | Antigravity |
| Scene placement | Codex | Bạn | Antigravity |

---

# 84. Asset task template

Mỗi asset có task riêng:

```text
TASK ID:
ART-XXX-000

ASSET:
[name]

REFERENCE:
[images]

REAL DIMENSIONS:
[size]

TARGET POLYCOUNT:
[count]

TEXTURE:
[size]

PIVOT:
[pivot]

COLLIDER:
[type]

ANIMATION:
[yes/no]

INTERACTION:
[description]

OUTPUT:
.blend
.fbx
textures
Unity prefab

ACCEPTANCE:
[checklist]
```

---

# 85. Task đầu tiên nên chạy

```text
TASK ID:
ART-STALL-001

ASSET:
Tram Chanh Stall

REAL DIMENSIONS:
1.8m x 0.8m x 2.2m

GOAL:
Create clean modular blockout first.

DO NOT:
Polish textures yet.
Do not create old sign.
Do not merge equipment.

OUTPUT:
SM_Stall_Base
SM_Stall_Frame
SM_Stall_Roof
SM_Stall_Counter
SM_Stall_Wheels
SM_Sign_TramChanh_New

ACCEPTANCE:
Scale test passes in Unity.
```

---

# 86. Task thứ hai

```text
TASK ID:
ART-CAKE-001

ASSET:
Elmich Grill

GOAL:
Create gameplay-ready hinged grill.

REQUIRE:
separate lid,
correct hinge pivot,
upper/lower ridged plates,
front LED display,
handle,
cake placement point.

ACCEPTANCE:
Unity lid animation works without mesh deformation.
```

---

# 87. Task thứ ba

```text
TASK ID:
ART-DRINK-001

ASSET:
Red Tea Rack + Pre-Portioned Tea Bag

GOAL:
Create the first drink interaction pair.

ACCEPTANCE:
Tea bag visually fits rack.
Player can pick bag from rack.
Bag has closed/open representation.
```

---

# 88. Final pipeline

```text
PHOTO REFERENCE
      ↓
ASSET SPEC
      ↓
AI 3D GENERATION
      ↓
BLENDER CLEANUP
      ↓
FBX / TEXTURES
      ↓
UNITY IMPORT
      ↓
PREFAB
      ↓
INTERACTION
      ↓
QA
      ↓
APPROVED
```

---

# 89. Quy tắc quan trọng cuối cùng

1. Reference thật quan trọng hơn output AI.
2. Không để AI tự bịa branding.
3. Không merge object cần tương tác.
4. Không làm chi tiết trước khi scale đúng.
5. Không làm cả quán thành một mesh.
6. Quầy thật là chuẩn kích thước.
7. Bảng hiệu mới là branding chính thức.
8. Túi trà đã được đong sẵn.
9. Rack đỏ là điểm lấy trà.
10. Grill phải có nắp tương tác.
11. Asset phải phục vụ gameplay trước khi phục vụ hình ảnh.
12. Vertical slice trước, polish sau.

---

# 90. NEXT ACTION

Bắt đầu bằng:

```text
ART-STALL-001
→ Stall blockout
```

Sau đó:

```text
ART-BRAND-001
→ New Tram Chanh Sign
```

Sau đó:

```text
ART-CAKE-001
→ Grill
```

Sau đó:

```text
ART-DRINK-001
→ Red Tea Rack + Tea Bag
```

Khi 4 nhóm này xong, có thể đưa vào Unity để dựng first playable blockout.
