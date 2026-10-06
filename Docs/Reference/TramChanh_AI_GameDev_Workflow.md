> **Errata (2026-10-06):** copy of the Product Owner's source document. Content is verbatim except that the 500 ml measuring cup now uses its canonical names **`PF_BatterMeasureCup_500ml`** / **`SM_BatterMeasureCup_500ml`**: it is the interactive cake-batter measuring tool, not a drink-station item. See `Docs/Reference/README.md`.

# TRẠM CHANH — AI GAME DEV WORKFLOW
## Phân chia công việc cho Claude Code + Codex + Antigravity và model nên dùng

**Ngày lập kế hoạch:** 2026-10-06  
**Engine mục tiêu:** Unity 6  
**Thể loại:** Simulator / mô phỏng bán trà chanh vỉa hè Việt Nam  
**Phong cách hình ảnh:** Semi-realistic stylized Vietnamese street-food simulator  
**Quy ước scale:** `1 Unity Unit = 1 mét`

---

# 1. Mục tiêu tài liệu

Tài liệu này là “luật làm việc” chung cho toàn bộ project Trạm Chanh.

Mục tiêu:

1. Chia đúng vai cho từng AI.
2. Quy định model nên dùng trong từng AI.
3. Không để nhiều AI cùng sửa một file.
4. Xây game theo từng hệ thống nhỏ, có test và review.
5. Giữ đúng dữ liệu quán thật đã cung cấp.
6. Chuẩn hóa asset 3D để dễ đưa vào Unity.
7. Xây gameplay bám sát quy trình vận hành thật của Trạm Chanh.

Ba AI chính:

- **Claude Code** → kiến trúc, thiết kế hệ thống, review.
- **Codex** → triển khai Unity/C#, sửa lỗi, test, tích hợp.
- **Antigravity** → điều phối workflow, chạy task song song, build/test/xác minh tổng thể.

Không hiểu theo kiểu “3 AI cùng code”.

Hiểu theo kiểu:

```text
Claude Code = Technical Lead / Architect
Codex       = Unity Gameplay Engineer
Antigravity = Build + QA + Orchestrator
Bạn         = Game Director / Product Owner
```

---

# 2. Model nên sử dụng

> Ghi chú: tên model có thể thay đổi theo gói và thời điểm. Nếu model dưới đây không xuất hiện trong tài khoản, dùng model mạnh nhất tương đương trong cùng sản phẩm.

## 2.1 Claude Code

### Model chính: Claude Opus 5.5

Dùng cho:

- phân tích toàn project;
- thiết kế architecture;
- thiết kế state machine;
- thiết kế data model;
- review code nhiều hệ thống;
- tìm lỗi logic khó;
- refactor lớn;
- phân tích dependency;
- review trước khi merge;
- quyết định khi có nhiều phương án kỹ thuật.

**Vai trò:** Architect / Senior Reviewer.

### Model phụ: Claude Sonnet 5.5

Dùng cho:

- tài liệu;
- task nhỏ đã được định nghĩa rõ;
- review file đơn lẻ;
- tạo checklist;
- chỉnh prompt;
- phân tích UI/layout;
- công việc lặp lại không cần Opus.

**Không nên dùng model mạnh nhất cho mọi task nhỏ.**

---

## 2.2 Codex

### Model chính: GPT-5.6 Sol

Khuyến nghị:

```text
Reasoning: High
```

Dùng cho:

- Unity C#;
- gameplay;
- interaction;
- ScriptableObject;
- state machine;
- editor tools;
- prefab workflow;
- unit/integration tests;
- debug compile errors;
- performance;
- refactor code;
- AI/NPC;
- save/load;
- UI logic;
- scene integration.

**Vai trò:** Main Implementation Engineer.

### Model phụ: GPT-5.6 Terra

Dùng cho:

- script đơn giản;
- boilerplate;
- rename/refactor nhỏ;
- tạo test hàng loạt;
- documentation code;
- sửa warning;
- repetitive implementation.

### Model nhanh: GPT-5.6 Luna

Dùng cho:

- format code;
- tạo file mẫu;
- thay tên;
- generate repetitive mappings;
- xử lý hàng loạt công việc ít suy luận.

### Quy tắc

Không giao architecture lớn trực tiếp cho Luna.

Không để Sol viết lại architecture nếu Claude Code đã khóa kiến trúc, trừ khi phát hiện vấn đề và tạo proposal review.

---

## 2.3 Antigravity

### Model mặc định nên dùng: Gemini 3.5 Flash

Dùng cho:

- điều phối agent;
- chạy task song song;
- terminal workflow;
- kiểm tra file thay đổi;
- chạy Unity batch/compile/test;
- kiểm tra Git status;
- thu thập log;
- xác nhận acceptance criteria;
- báo task nào pass/fail.

**Vai trò:** Orchestrator + QA Automation.

Nếu trong model picker của Antigravity có model Flash mới hơn được hỗ trợ chính thức, có thể dùng model đó cho các task routine, nhưng không thay đổi architecture chỉ vì đổi model.

### Không mặc định dùng Gemini 3.5 Pro

Tại thời điểm lập tài liệu này, nguồn công khai của Google xác nhận Gemini 3.5 Flash dùng được trong Antigravity; 3.5 Pro vẫn được mô tả là đang thử nghiệm/chuẩn bị phát hành trong các cập nhật công khai gần nhất.

---

# 3. Quy tắc quyền hạn

## Claude Code được quyền

- tạo hoặc sửa tài liệu architecture;
- đề xuất interface;
- đề xuất data flow;
- review PR;
- yêu cầu Codex sửa implementation;
- xác định acceptance criteria;
- xác định module boundaries.

Claude Code **không tự ý sửa hàng chục file gameplay trong branch chính** nếu task có thể giao Codex.

---

## Codex được quyền

- tạo C# scripts;
- sửa scripts;
- tạo tests;
- triển khai prefab/editor tooling;
- debug;
- refactor trong phạm vi architecture đã duyệt;
- compile và sửa compile errors;
- triển khai gameplay.

Codex **không tự ý đổi core architecture** mà không ghi rõ lý do.

---

## Antigravity được quyền

- chia task;
- chạy subagent;
- chạy lệnh build/test;
- kiểm tra Git;
- đọc log;
- xác minh kết quả;
- tạo báo cáo QA;
- phát hiện task nào chưa đạt.

Antigravity **không phải Technical Lead**.  
Không để Antigravity tự phát minh lại gameplay hoặc architecture khi chưa có spec.

---

# 4. Dữ liệu quán thật — KHÔNG ĐƯỢC TỰ Ý THAY ĐỔI

## 4.1 Kích thước quầy

```text
Ngang:              1.8 m
Sâu:                0.8 m
Cao mặt quầy:       1.0 m
Mặt quầy → mái:     1.2 m
Tổng chiều cao:     ~2.2 m
Unity scale:        1 unit = 1 m
```

---

## 4.2 Bảng hiệu

Ảnh quầy cũ có bảng chữ nổi/phát sáng `TRẠM CHANH`.

**KHÔNG DÙNG BẢNG CŨ.**

Bắt buộc thay bằng bảng hiệu mới:

- dạng hộp ngang;
- nền trắng;
- mặt trên vàng cam;
- chữ `Trạm` màu đen;
- chữ `Chanh` màu vàng cam;
- dải màu trang trí phía dưới;
- asset riêng, không merge vào mesh quầy;
- texture/logo chính xác, không cho AI tự bịa chữ tiếng Việt.

Tên prefab:

```text
PF_Sign_TramChanh_New
```

---

# 5. Core gameplay thật

# 5.1 Luồng order

Có hai dạng khách.

## Dine-in

```text
Khách ngồi bàn
→ Lobby tới bàn
→ nhận order
→ Lobby nhập đơn
→ báo quầy
→ quầy làm món
→ báo Ready
→ Lobby lấy món
→ giao đúng bàn
```

## Takeaway / khách mua tại xe

```text
Khách tại xe
→ Lobby ra nhận order
→ Lobby nhập đơn
→ báo quầy
→ quầy làm món
→ báo Ready
→ Lobby lấy món
→ giao cho đúng khách/xe
```

Không để quầy tự nhận đơn trực tiếp từ khách nếu không có mode gameplay đặc biệt.

---

# 5.2 Quy trình làm nước

Trà đã được chuẩn bị và đong sẵn trong túi.

Các túi được xếp trong **rack/sọt nhựa đỏ**.

Gameplay chuẩn:

```text
1. Nhận ticket từ Lobby
2. Lấy đúng túi trà đã đong sẵn từ rack đỏ
3. Mở túi trà
4. Thêm thạch dừa
5. Thêm thạch chanh
6. Múc đá
7. Lắc bịch
8. Lau bịch
9. Đặt vào khu Ready / trả đơn
```

Không thêm bước “đong trà”.

---

# 5.3 Quy trình làm bánh

```text
1. Nhận ticket bánh
2. Đong đúng lượng bột
3. Đổ bột
4. Chờ bánh chín
5. Lật bánh
6. Cắt bánh
7. Xịt xốt
8. Cuộn bánh theo chiều dọc
9. Bỏ vào giấy gói
10. Đặt vào khu Ready / trả bánh
```

---

# 6. Asset 3D đã xác định

## Stall

```text
PF_Stall_TramChanh
PF_Sign_TramChanh_New
PF_Stall_Roof
PF_Stall_Frame
PF_Stall_Counter
PF_Stall_Wheel
PF_LEDStrip
PF_EdisonBulb
```

## Drink workstation

```text
PF_TeaBag_PrePortioned
PF_RedTeaRack
PF_ToppingStation
PF_Topping_CoconutJelly
PF_Topping_LemonJelly
PF_IceBin
PF_IceScoop
PF_PumpBottle
PF_WipeCloth
```

## Cake workstation

```text
PF_Grill_Elmich
PF_BatterMeasureCup_500ml
PF_BatterBag
PF_SauceBag_Mango
PF_SauceBag_Chocolate
PF_SauceBag_Cheese
PF_Scissors_RedGray
PF_Spatula_WoodHandle
PF_WrappingPaper
PF_Cake_Raw
PF_Cake_Cooked
PF_Cake_Rolled
PF_Cake_Wrapped
```

## Customer area

```text
PF_PlasticStool
PF_YellowCrateTable
PF_StainlessTrayTabletop
PF_MenuBoard_Large
PF_MenuBoard_Counter
```

---

# 7. Folder structure Unity

```text
Assets/
└── TramChanh/
    ├── Art/
    │   ├── Models/
    │   │   ├── Stall/
    │   │   ├── Furniture/
    │   │   ├── Equipment/
    │   │   ├── Food/
    │   │   └── Props/
    │   ├── Materials/
    │   ├── Textures/
    │   ├── Animations/
    │   └── VFX/
    │
    ├── Audio/
    │   ├── SFX/
    │   ├── Ambient/
    │   └── Music/
    │
    ├── Prefabs/
    │   ├── Stall/
    │   ├── Workstations/
    │   ├── Items/
    │   ├── NPC/
    │   └── UI/
    │
    ├── Scenes/
    │   ├── Bootstrap/
    │   ├── Gameplay/
    │   └── Test/
    │
    ├── Scripts/
    │   ├── Core/
    │   ├── Orders/
    │   ├── Interaction/
    │   ├── Drinks/
    │   ├── Cakes/
    │   ├── Lobby/
    │   ├── Customers/
    │   ├── Inventory/
    │   ├── UI/
    │   ├── Save/
    │   └── Debug/
    │
    ├── ScriptableObjects/
    │   ├── Items/
    │   ├── Recipes/
    │   ├── Customers/
    │   └── Balance/
    │
    └── Tests/
        ├── EditMode/
        └── PlayMode/
```

---

# 8. Architecture cần Claude Code thiết kế trước

Claude Code phải tạo tài liệu trước khi Codex implement.

```text
Docs/
├── GAMEPLAY_OVERVIEW.md
├── ARCHITECTURE.md
├── ORDER_SYSTEM.md
├── INTERACTION_SYSTEM.md
├── DRINK_WORKFLOW.md
├── CAKE_WORKFLOW.md
├── NPC_CUSTOMER_SYSTEM.md
├── UI_FLOW.md
├── SAVE_SYSTEM.md
└── DEFINITION_OF_DONE.md
```

---

# 9. Hệ thống Order

## Data model đề xuất

```text
Order
├── OrderId
├── OrderType
├── CustomerId
├── TableId
├── VehicleId
├── Items[]
├── CreatedAt
├── Status
└── QualityScore
```

## OrderType

```text
DineIn
TakeawayVehicle
```

## OrderStatus

```text
WaitingForLobby
TakingOrder
Entered
SentToStall
InPreparation
Ready
PickedUpByLobby
Delivered
Completed
Failed
```

Claude Code chịu trách nhiệm thiết kế state transition.

Codex chịu trách nhiệm implement.

Antigravity chạy tests xác minh transition.

---

# 10. Interaction system

Nên dùng một interaction framework chung thay vì mỗi object viết một kiểu.

Interface gợi ý:

```csharp
public interface IInteractable
{
    bool CanInteract(PlayerInteractor interactor);
    void Interact(PlayerInteractor interactor);
    string GetPrompt();
}
```

Không coi đoạn code này là specification cuối cùng. Claude Code cần review trước.

Các object như:

```text
TeaRack
TeaBag
ToppingBin
IceBin
IceScoop
Grill
Scissors
Spatula
SauceBag
WrappingPaper
ReadyCounter
```

đều đi qua interaction framework chung.

---

# 11. State machine làm nước

Claude Code thiết kế state machine.

Codex implement.

State đề xuất:

```text
TeaBagState
├── Stored
├── PickedUp
├── Opened
├── CoconutJellyAdded
├── LemonJellyAdded
├── IceAdded
├── Shaken
├── Wiped
├── Ready
└── Delivered
```

Có thể cho phép sai thứ tự nhưng trừ điểm, hoặc khóa interaction tùy thiết kế game.

Khuyến nghị cho prototype đầu:

**khóa theo đúng thứ tự** để giảm complexity.

Sau khi gameplay ổn, mở chế độ “free action + quality scoring”.

---

# 12. State machine làm bánh

```text
CakeState
├── Waiting
├── BatterMeasured
├── BatterPoured
├── Cooking
├── Cooked
├── Flipped
├── Cut
├── Sauced
├── Rolled
├── Wrapped
├── Ready
└── Delivered
```

## Grill state

```text
Off
Preheating
Ready
Open
Cooking
Finished
Overcooked
```

Máy nướng cần:

- nắp mở/đóng;
- mặt trên + mặt dưới có rãnh;
- nhiệt độ;
- timer;
- bánh đổi màu;
- audio sizzling;
- có khả năng cháy nếu để quá lâu.

---

# 13. Phân chia task theo PHASE

# PHASE 0 — Khóa thiết kế

## Claude Code / Opus 5.5

Task:

- đọc toàn bộ reference;
- viết `GAMEPLAY_OVERVIEW.md`;
- viết architecture;
- khóa naming;
- khóa order flow;
- khóa interaction flow;
- xác định interface;
- xác định ScriptableObject schema;
- xác định event/data flow.

Output bắt buộc:

```text
Docs/ARCHITECTURE.md
Docs/ORDER_SYSTEM.md
Docs/INTERACTION_SYSTEM.md
Docs/DRINK_WORKFLOW.md
Docs/CAKE_WORKFLOW.md
```

## Codex

Chưa implement feature lớn.

Chỉ:

- kiểm tra project Unity;
- tạo folder;
- setup assemblies;
- setup tests;
- setup coding convention.

## Antigravity

- kiểm tra Git clean;
- tạo branch/worktree;
- xác minh docs tồn tại;
- báo thiếu requirement.

---

# PHASE 1 — Project foundation

## Claude Code

Review:

- bootstrap;
- service lifetime;
- event system;
- scene lifecycle.

## Codex / GPT-5.6 Sol

Implement:

```text
GameBootstrap
SceneLoader
GameState
PlayerInteractor
InteractionPrompt
EventBus hoặc event architecture đã duyệt
```

Tests:

```text
BootstrapTests
InteractionTests
SceneLoadTests
```

## Antigravity

Chạy:

```text
compile
EditMode tests
PlayMode smoke test
Git diff
```

Acceptance:

- project compile;
- zero error;
- interaction với test cube hoạt động;
- không hard-code reference scene lung tung.

---

# PHASE 2 — Quầy và scene blockout

## Claude Code

Không cần model 3D.

Review hierarchy và interaction anchor.

## Codex

- import stall;
- scale đúng `1.8 x 0.8 x 2.2 m`;
- tạo prefab;
- collider;
- anchor cho workstation;
- lighting placeholder;
- scene test.

Hierarchy:

```text
PF_Stall_TramChanh
├── Structure
├── Counter
├── Frame
├── Roof
├── Sign
├── Lights
├── Wheels
└── Anchors
    ├── TeaRackAnchor
    ├── ToppingAnchor
    ├── IceBinAnchor
    ├── GrillAnchor
    ├── SauceAnchor
    ├── WrapAnchor
    └── ReadyCounterAnchor
```

## Antigravity

- mở test scene;
- xác minh scale;
- kiểm tra missing material;
- kiểm tra missing script;
- chạy screenshot/scene validation nếu workflow hỗ trợ.

---

# PHASE 3 — Lobby + Order System

## Claude Code / Opus 5.5

Thiết kế:

- Order data;
- dine-in;
- takeaway at vehicle;
- table assignment;
- vehicle/customer identification;
- lobby queue;
- order status transitions;
- event từ Lobby → Stall.

## Codex / Sol

Implement:

```text
Order.cs
OrderItem.cs
OrderManager.cs
OrderQueue.cs
LobbyOrderController.cs
TableOrderPoint.cs
VehicleOrderPoint.cs
OrderTicketUI.cs
ReadyOrderController.cs
```

## Antigravity

Test case:

```text
TC-ORDER-001 Dine-in order
TC-ORDER-002 Vehicle takeaway
TC-ORDER-003 Multiple orders
TC-ORDER-004 Wrong delivery
TC-ORDER-005 Ready order
```

---

# PHASE 4 — Drink gameplay

## Claude Code

Review:

- exact state;
- invalid action behavior;
- scoring;
- recipe structure.

## Codex

Implement:

```text
TeaRackController
TeaBagItem
TeaBagStateController
ToppingBin
IceBin
IceScoop
ShakeInteraction
WipeInteraction
DrinkReadyZone
```

## Gameplay exact

```text
Ticket
→ Tea Rack
→ Pick Pre-Portioned Tea Bag
→ Open
→ Coconut Jelly
→ Lemon Jelly
→ Ice
→ Shake
→ Wipe
→ Ready Counter
```

## Antigravity acceptance

Phải test:

1. Không đong trà.
2. Túi bắt đầu ở rack đỏ.
3. Không thể Ready khi chưa lắc.
4. Không thể Complete khi chưa lau.
5. đúng order được mark Ready.
6. giao nhầm order bị phát hiện.

---

# PHASE 5 — Cake gameplay

## Claude Code

Thiết kế cooking model:

```text
Batter amount
Cook duration
Heat
Doneness
Burn threshold
Sauce type
Wrapping
```

## Codex

Implement:

```text
MeasuringCup
BatterContainer
BatterPourInteraction
GrillController
CakeCookState
CakeFlipInteraction
CakeCutInteraction
SauceInteraction
CakeRollInteraction
WrapInteraction
CakeReadyZone
```

## Antigravity

Test:

```text
Wrong batter amount
Undercooked
Correct cook
Overcooked
Missing sauce
Missing wrap
Correct finished product
```

---

# PHASE 6 — Customer AI

## Claude Code / Opus

Thiết kế AI state:

```text
Arrive
ChooseDineInOrTakeaway
FindTable / WaitAtVehicle
WaitForLobby
Ordering
WaitingForFood
Eating / ReceiveTakeaway
Pay
Leave
```

## Codex

Implement:

- NavMesh;
- customer spawn;
- seat reservation;
- table slots;
- patience;
- lobby interaction;
- order ownership;
- leave logic.

## Antigravity

Stress test:

- 1 customer;
- 5 customers;
- 15 customers;
- full tables;
- queue;
- missing order;
- delayed order.

---

# PHASE 7 — UI

## Claude Code / Sonnet 5.5 hoặc Opus nếu flow phức tạp

Thiết kế:

```text
HUD
Interaction prompt
Order list
Order detail
Lobby input
Ready orders
Clock
Money
Customer patience
Pause
Settings
```

## Codex

Implement UI Toolkit/UI system đã chọn.

## Antigravity

Kiểm tra:

- 16:9;
- 16:10;
- 21:9;
- text overflow;
- input keyboard/mouse;
- gamepad sau này nếu cần.

---

# PHASE 8 — Economy + progression

Claude Code:

- economy design;
- upgrades;
- price balance;
- unlock system.

Codex:

```text
MoneySystem
UpgradeSystem
ShopSystem
UnlockSystem
DailyReport
```

Possible upgrades:

```text
faster lobby
larger tea rack
larger ice bin
second grill
more tables
better lighting
additional employee
faster preparation
```

Antigravity:

- regression tests;
- save/load upgrade state.

---

# PHASE 9 — Save / Load

Claude Code:

- schema;
- versioning;
- migration strategy.

Codex:

- JSON/binary according to architecture;
- autosave;
- manual save;
- settings;
- progression.

Antigravity:

- corrupt save test;
- old save test;
- new save test;
- quit/reopen test.

---

# PHASE 10 — Polish + Optimization

## Claude Code

Review:

- coupling;
- GC allocations;
- architecture debt;
- event leaks.

## Codex

Optimize:

- object pooling;
- customer pooling;
- UI allocations;
- physics layers;
- collider complexity;
- LOD;
- texture memory;
- draw calls;
- async load.

## Antigravity

Build matrix:

```text
Development Build
Release Build
Fresh save
Existing save
Low NPC count
High NPC count
```

---

# 14. 3D workflow

AI coding agents không nên là bên tự sinh geometry cuối cùng.

Pipeline:

```text
Reference ảnh thật
→ prompt asset
→ 3D generator / Blender cleanup
→ UV/PBR
→ export FBX/GLB
→ Unity import
→ Codex prefab setup
→ Antigravity validation
```

Claude Code chỉ quyết định:

- asset boundaries;
- interaction requirements;
- pivot needs;
- animation requirements.

Codex quyết định:

- Unity import settings;
- prefab;
- collider;
- animator;
- interaction anchor.

---

# 15. Quy chuẩn asset tương tác

Một asset gameplay phải có đủ:

```text
Mesh
Material
Collider
Pivot
InteractionPoint
State representation
Audio hooks
Animation hooks
Prefab
```

Ví dụ `PF_Grill_Elmich`:

```text
PF_Grill_Elmich
├── Base
├── LowerPlate
├── LidPivot
│   ├── Lid
│   ├── UpperPlate
│   └── Handle
├── Display
├── InteractionPoint
├── CakePlacementPoint
└── AudioPoint
```

Không merge nắp máy với thân nếu gameplay cần mở/đóng.

---

# 16. Naming convention

## Prefab

```text
PF_
```

## Static mesh

```text
SM_
```

## Material

```text
MAT_
```

## Texture

```text
T_
```

## ScriptableObject instance

```text
SO_
```

## Animation

```text
AN_
```

## Audio

```text
SFX_
AMB_
MUS_
```

Ví dụ:

```text
PF_Grill_Elmich
SM_Grill_Base
SM_Grill_Lid
MAT_Grill_BlackMetal
SFX_Grill_Sizzle
```

---

# 17. Git workflow

Không cho cả 3 AI làm trực tiếp trên một branch.

## Branches

```text
main
develop
feature/*
review/*
qa/*
```

Ví dụ:

```text
feature/order-system
feature/drink-workflow
feature/cake-workflow
feature/customer-ai
```

---

# 18. Worktree

Nên dùng worktree khi Codex và agent khác làm song song.

Ví dụ:

```text
worktrees/
├── order-system/
├── drink-system/
├── cake-system/
└── qa/
```

Một worktree = một nhiệm vụ.

Không để:

```text
Codex sửa OrderManager.cs
Claude Code sửa OrderManager.cs
Antigravity agent sửa OrderManager.cs
```

cùng lúc.

---

# 19. File ownership

## Claude Code

Ưu tiên ownership:

```text
/Docs/**
/Architecture/**
```

Review scripts nhưng tránh implementation trực tiếp nếu không cần.

## Codex

Ownership:

```text
/Assets/TramChanh/Scripts/**
/Assets/TramChanh/Tests/**
/Assets/TramChanh/Prefabs/**
```

## Antigravity

Ownership:

```text
/Automation/**
/QA/**
/BuildScripts/**
```

Không sửa gameplay scripts trừ task sửa lỗi được giao rõ.

---

# 20. Definition of Done cho mọi feature

Feature chưa được coi là xong chỉ vì “code chạy”.

Phải đủ:

```text
[ ] Spec tồn tại
[ ] Architecture approved
[ ] Code compile
[ ] Không Console Error
[ ] Test chính pass
[ ] Không missing reference
[ ] Prefab sạch
[ ] Naming đúng
[ ] Không hard-coded path vô lý
[ ] Không tạo GC nghiêm trọng mỗi frame
[ ] Review Claude Code pass
[ ] Antigravity validation pass
[ ] Commit rõ ràng
```

---

# 21. Quy trình một task chuẩn

Ví dụ: `Làm thùng đá`

## Bước 1 — Claude Code

Yêu cầu:

```text
Review gameplay requirement for IceBin.
Define state, interaction contract, required interfaces,
and acceptance criteria. Do not implement yet.
```

Output:

```text
ICE_BIN_SPEC.md
```

## Bước 2 — Codex

Yêu cầu:

```text
Implement IceBin based strictly on ICE_BIN_SPEC.md.
Add tests.
Compile project.
Do not modify unrelated architecture.
```

## Bước 3 — Antigravity

Yêu cầu:

```text
Validate IceBin implementation.
Run compile/tests.
Check prefab references.
Compare against acceptance criteria.
Report PASS/FAIL.
Do not redesign the feature.
```

## Bước 4 — Claude Review

Nếu FAIL:

```text
Review only the failed points and produce corrective actions.
```

## Bước 5 — Codex Fix

Sửa đúng failure.

---

# 22. Prompt gốc cho Claude Code

```text
You are the Technical Lead and Software Architect for a Unity 6 game
called Tram Chanh, a Vietnamese roadside lemon-tea shop simulator.

Your responsibility is architecture, system boundaries, state machines,
data flow, specifications, and code review.

Do not implement large features unless explicitly requested.
Do not invent real-world shop workflows that conflict with project docs.

Always treat the following as ground truth:
- Stall: 1.8m wide, 0.8m deep, 1.0m counter height, 2.2m total.
- Use the NEW Tram Chanh sign, never the old illuminated sign.
- Lobby receives dine-in orders at tables or takeaway orders at vehicles.
- Lobby enters and sends orders to the stall.
- Tea is pre-portioned in bags stored in red racks.
- Drink workflow: take bag, open, coconut jelly, lemon jelly, ice,
  shake, wipe, ready.
- Cake workflow: measure batter, pour, cook, flip, cut, sauce,
  roll vertically, wrap, ready.

Before approving an implementation:
1. Check architecture.
2. Check state transitions.
3. Check edge cases.
4. Check coupling.
5. Check testability.
6. Produce actionable review findings.
```

---

# 23. Prompt gốc cho Codex

```text
You are the primary Unity 6 implementation engineer for Tram Chanh.

Implement features according to the approved project documentation.

Responsibilities:
- C# gameplay
- prefabs
- Unity editor integration
- tests
- debugging
- performance
- refactoring within approved architecture

Rules:
1. Read relevant Docs before editing.
2. Inspect existing code before making changes.
3. Do not redesign architecture silently.
4. Make minimal scoped changes.
5. Add or update tests.
6. Compile after implementation.
7. Fix your compile errors before reporting completion.
8. Report all changed files.
9. Do not modify unrelated files.
10. Preserve real-world Tram Chanh workflows.

Unity scale is 1 unit = 1 meter.
```

---

# 24. Prompt gốc cho Antigravity

```text
You are the workflow orchestrator and QA agent for Tram Chanh Unity 6.

Your job is to execute and verify work, not redesign gameplay.

For every assigned feature:
1. Read the task acceptance criteria.
2. Check Git status.
3. Run compile.
4. Run relevant EditMode tests.
5. Run relevant PlayMode tests.
6. Inspect errors/warnings.
7. Verify expected files/prefabs exist.
8. Check for missing references.
9. Report PASS or FAIL for every acceptance criterion.
10. Do not modify architecture unless explicitly assigned.

If a task fails, return the smallest reproducible failure and relevant logs.
```

---

# 25. Quy tắc tránh “AI đạp code”

## Không dùng

```text
"Claude, Codex và Antigravity cùng làm order system."
```

## Dùng

```text
Claude:
Design Order System.

Codex:
Implement approved Order System.

Antigravity:
Validate Order System.

Claude:
Review implementation.

Codex:
Fix review findings.
```

---

# 26. Thứ tự phát triển đề xuất

```text
M0  Project foundation
M1  Stall blockout
M2  Player interaction
M3  Lobby + order
M4  Drink vertical slice
M5  Cake vertical slice
M6  Customer AI
M7  Tables + vehicle takeaway
M8  UI
M9  Economy
M10 Save/load
M11 Audio/VFX
M12 Optimization
M13 Content expansion
M14 Release build
```

---

# 27. Vertical Slice đầu tiên

Đừng làm toàn game ngay.

Vertical Slice đầu tiên chỉ cần:

```text
1 quầy
1 Lobby NPC hoặc player lobby
1 khách
1 bàn
1 loại trà
1 loại bánh
1 rack trà
1 topping station
1 ice bin
1 grill
1 Ready Counter
1 order UI
```

Flow phải chạy end-to-end:

```text
Customer
→ Lobby
→ Order
→ Stall
→ Prepare
→ Ready
→ Lobby
→ Customer
→ Complete
```

Nếu vertical slice này tốt thì mới mở rộng.

---

# 28. Task backlog ưu tiên

## P0 — bắt buộc

```text
[ ] Bootstrap
[ ] Interaction system
[ ] Order system
[ ] Lobby system
[ ] Tea rack
[ ] Tea bag
[ ] Topping station
[ ] Ice bin
[ ] Drink workflow
[ ] Grill
[ ] Cake workflow
[ ] Ready counter
[ ] Customer basic AI
[ ] Table
[ ] Vehicle takeaway point
```

## P1

```text
[ ] Money
[ ] Customer patience
[ ] Multiple recipes
[ ] Multiple customers
[ ] Queue
[ ] Save/load
[ ] Audio
[ ] VFX
```

## P2

```text
[ ] Employees
[ ] Shop upgrade
[ ] More tables
[ ] Weather
[ ] Day/night
[ ] Traffic ambience
[ ] Random events
```

---

# 29. Không làm sớm

Tránh làm ngay từ đầu:

- multiplayer;
- procedural city;
- 30 món;
- nhân viên AI phức tạp;
- skill tree lớn;
- online leaderboard;
- full open world;
- mobile port;
- console build.

Hoàn thành core simulator trước.

---

# 30. Performance target ban đầu

PC prototype:

```text
60 FPS target
1080p
URP
```

NPC:

```text
Prototype: 5–10
Target đầu tiên: 20–30
```

Không optimize cực đoan trước khi profiler cho thấy bottleneck.

---

# 31. Review gate

Không merge feature nếu:

```text
Compile FAIL
Test FAIL
Missing reference
Gameplay khác spec thật
Đổi bảng hiệu về bản cũ
Tự thêm bước đong trà
Bỏ qua Lobby
Order không phân biệt dine-in/takeaway
Cake workflow sai thứ tự chính
```

---

# 32. Các “Ground Truth Tests” bắt buộc

## GT-001 Stall Dimensions

```text
Width  = 1.8m
Depth  = 0.8m
Height = ~2.2m
```

## GT-002 Branding

```text
New Tram Chanh sign only.
```

## GT-003 Tea

```text
Tea must already be portioned in bags inside red racks.
```

## GT-004 Lobby

```text
Order must originate through Lobby.
```

## GT-005 Dine In

```text
Lobby takes order at table.
```

## GT-006 Takeaway

```text
Lobby takes order at customer's vehicle.
```

## GT-007 Drink Workflow

```text
Tea bag
→ open
→ coconut jelly
→ lemon jelly
→ ice
→ shake
→ wipe
→ ready
```

## GT-008 Cake Workflow

```text
measure
→ pour
→ cook
→ flip
→ cut
→ sauce
→ roll vertically
→ wrap
→ ready
```

---

# 33. Model routing cheat sheet

| Công việc | AI | Model |
|---|---|---|
| Architecture toàn game | Claude Code | Opus 5.5 |
| State machine phức tạp | Claude Code | Opus 5.5 |
| Code review lớn | Claude Code | Opus 5.5 |
| Docs/task rõ | Claude Code | Sonnet 5.5 |
| Unity gameplay C# | Codex | GPT-5.6 Sol High |
| Debug khó | Codex | GPT-5.6 Sol High |
| Refactor nhỏ | Codex | GPT-5.6 Terra |
| Boilerplate | Codex | Terra/Luna |
| Test repetitive | Codex | Terra |
| Orchestration | Antigravity | Gemini 3.5 Flash |
| Compile/test/build | Antigravity | Gemini 3.5 Flash |
| Multi-agent QA | Antigravity | Gemini 3.5 Flash |

---

# 34. Nguyên tắc cuối

```text
Claude decides structure.
Codex builds it.
Antigravity proves it works.
You decide whether it is fun and faithful to Tram Chanh.
```

Không để AI tự thay đổi hoạt động thực tế của quán chỉ vì “game thường làm như vậy”.

Reference thật của Trạm Chanh có quyền ưu tiên cao hơn assumption của AI.

---

# 35. Nguồn kiểm tra model tại thời điểm lập tài liệu

Các lựa chọn model ở phần đầu được đối chiếu với thông tin công khai chính thức tại thời điểm 2026-10-06:

- OpenAI GPT-5.6 / Codex:
  - https://openai.com/index/gpt-5-6/
  - https://help.openai.com/en/articles/11369540-using-codex-with-your-chatgpt-plan

- Anthropic:
  - https://www.anthropic.com/claude-opus-5-5
  - https://www.anthropic.com/claude-sonnet-5-5

- Google Antigravity / Gemini:
  - https://developers.googleblog.com/all-the-news-from-the-google-io-2026-developer-keynote/
  - https://blog.google/innovation-and-ai/models-and-research/gemini-models/gemini-3-5/

---

# 36. NEXT ACTION

Task đầu tiên nên giao:

```text
TASK TC-ARCH-001

Owner: Claude Code
Model: Claude Opus 5.5

Goal:
Create the architecture specification for the first playable vertical slice.

Scope:
- Player interaction
- Lobby
- Order System
- One drink recipe
- One cake recipe
- Ready Counter
- One customer
- One table
- One vehicle takeaway point

Do not implement code.

Outputs:
Docs/ARCHITECTURE.md
Docs/ORDER_SYSTEM.md
Docs/INTERACTION_SYSTEM.md
Docs/DRINK_WORKFLOW.md
Docs/CAKE_WORKFLOW.md

After completion:
Send the documents to Codex for implementation planning.
```
