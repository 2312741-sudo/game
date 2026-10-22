# VẬT LÝ TRONG "TRẠM CHANH" - TÀI LIỆU VẤN ĐÁP BÀI 4 (INTERACTIONS) VÀ BÀI 6 (PHYSICS)

**Trạng thái:** mã và công cụ Editor đã viết nhưng CHƯA được biên dịch hay chạy trong Unity (người viết không mở được Unity). Mọi thông số bên dưới là giá trị khởi đầu, cần chỉnh khi chạy thử.
**Mã:** `Assets/TramChanh/Scripts/Props` (assembly `TramChanh.Props`). **Công cụ dựng:** menu `Tram Chanh > Physics > Build Physics Props`.

## 1. Bản đồ nhanh: dùng ở đâu

| Khái niệm | Dùng ở | File |
|---|---|---|
| Rigidbody (dynamic) | Vật rác bị bỏ; vật treo (bảng hiệu, bóng đèn) | `DiscardedPropSpawner.cs`, `PhysicsPropsBuilder.cs` |
| Rigidbody (kinematic) | Điểm neo của dây treo; vũng nước (để trigger hoạt động) | `PhysicsPropsBuilder.cs` |
| Box/Sphere Collider | Rác (Box khớp bounds), bóng đèn (Sphere), bảng hiệu (Box) | như trên |
| Is Trigger | Vũng nước (Box), vùng chạm nhẹ quanh vật treo (Sphere) | `PuddleSplashTrigger.cs`, `HangingNudgeZone.cs` |
| OnCollisionEnter | Rác chạm đất, phát cue âm thanh/hiệu ứng | `DiscardedProp.cs` |
| OnTriggerEnter/Stay/Exit | Bắn nước khi đi qua vũng; đẩy nhẹ vật treo | `PuddleSplashTrigger.cs`, `HangingNudgeZone.cs` |
| Raycast (có sẵn) | `PlayerInteractor` bắn tia vào layer Interactable | `Scripts/Interaction/PlayerInteractor.cs` |
| Physics Material | `PM_Debris` (rác), `PM_HangingProp` (vật treo) | `Art/PhysicsMaterials/` |
| Hinge Joint | Bảng hiệu treo (lắc một trục, giới hạn góc) | `PF_Physics_HangingSign_Hinge` |
| Spring Joint | Bóng đèn treo (đàn hồi, giảm chấn) | `PF_Physics_HangingBulb_Spring` |
| Fixed Joint | Không dùng trong game; chỉ giải thích ở mục 7 | - |

## 2. Rigidbody và Collider (Bài 4)

- **Collider** cho Unity biết hình dạng va chạm. **Rigidbody** cho vật tham gia mô phỏng vật lý (trọng lực, lực, va chạm).
- **Collider không có Rigidbody = "static collider"**: đứng yên (sàn, tường, quầy). Người chơi dùng `CharacterController`, không dùng Rigidbody, nên game hầu như không có Rigidbody trước đây.
- **Kinematic Rigidbody** (`isKinematic = true`): có Rigidbody nhưng không bị lực tác động, ta tự di chuyển nó. Dùng làm điểm neo dây và làm "chủ" của trigger.
- Chọn loại collider: Box/Sphere/Capsule là primitive, rẻ nhất. Mesh Collider tốn hơn và vật động (non-kinematic) chỉ dùng được Mesh Collider loại *Convex*. Vì vậy rác dùng một Box khớp bounds của mesh, không dùng Mesh Collider.

## 3. Vật rác bị bỏ (DiscardedProp)

Khi người chơi bỏ một món, nơi bỏ gọi `DiscardedPropSpawner.SpawnCopyOf(món, vị trí, hướng, vận tốc ban đầu)`. Spawner dựng bản sao từ các MeshFilter/MeshRenderer của món (không chép script hay collider nên không thể bị tương tác nhầm), thêm BoxCollider, Rigidbody, Physics Material. Rác rơi, nảy, nằm yên, thu nhỏ dần trong 1,5 giây cuối rồi bị huỷ sau 6 giây. Tối đa 8 vật; vật thứ 9 làm vật cũ nhất biến mất (logic trong `DebrisBudget`, thuần C#, có test).

| Thông số | Giá trị | Lý do |
|---|---|---|
| Mass | 0,15 | Món cầm tay nhẹ; không đẩy được gì khác |
| Linear Damping | 0,1 | Gần như rơi tự do, hơi cản gió |
| Angular Damping | 0,6 | Lăn vài vòng rồi dừng, không quay mãi |
| Collision Detection | ContinuousDynamic | Vật nhỏ, rơi nhanh có thể xuyên sàn ở Discrete |
| Interpolation | Interpolate | Chuyển động mượt giữa các bước vật lý |
| Layer | HeldItem | Không nằm trong layer Interactable (tia của `PlayerInteractor` không bắt trúng), cũng không phải Environment (camera third-person không bị vướng) |
| Exclude Layers (collider) | Player, NPC, Interactable | Rác không cản CharacterController và không đẩy vật tương tác; chỉ va chạm với mặt đất/Environment |

### Physics Material `PM_Debris` (Bài 6)

| Trường | Giá trị | Ý nghĩa |
|---|---|---|
| Dynamic Friction | 0,5 | Ma sát khi đang trượt: vừa phải |
| Static Friction | 0,6 | Ma sát khi nằm yên: đủ để không trượt trên mặt dốc nhẹ |
| Bounciness | 0,35 | Nảy nhẹ (0 = không nảy, 1 = nảy mãi) |
| Friction Combine | Average | Trung bình với vật còn lại |
| Bounce Combine | Maximum | Xem dưới |

**Vì sao Bounce Combine = Maximum:** mặt đất dùng material mặc định (Dynamic 0,6; Static 0,6; Bounciness 0; Combine Average; đã đối chiếu tài liệu Unity). Nếu cả hai Average thì độ nảy = (0,35 + 0)/2 = 0,175, rác gần như không nảy. Khi hai bên khác chế độ, Unity chọn chế độ có độ ưu tiên cao hơn: `Average < Minimum < Multiply < Maximum`. Rác đặt Maximum nên thắng, độ nảy = max(0,35; 0) = 0,35, và không cần sửa material của sàn.

### Cue âm thanh/hiệu ứng (không phụ thuộc Audio/Effects)

`OnCollisionEnter` dùng `ImpactCuePolicy` (bỏ qua va chạm dưới 0,6 m/s, có cooldown 0,08 s để rác nảy không spam) rồi phát `PropCueRaised(Kind = DebrisImpact, Position, Intensity 0..1)` theo hai đường: sự kiện C# `DiscardedProp.CueRaised` và `IEventBus.Publish`. Audio về sau chỉ cần `bus.Subscribe<PropCueRaised>(...)`. Assembly `TramChanh.Props` chỉ tham chiếu `TramChanh.Core`.

## 4. Vũng nước (PuddleSplashTrigger)

Mỗi vũng (`SM_Puddle_Oval`, `SM_Puddle_Irregular`, vật liệu `MAT_Puddle_Water`) có một prefab trigger riêng đặt chồng lên: `PF_Physics_PuddleTrigger_Oval/Irregular`. Prefab vũng cũ không bị sửa. BoxCollider trigger cao 0,3 m, bề rộng lấy từ bounds mesh.

**Cấu hình đúng (đã đối chiếu tài liệu Unity 6, trang "Interaction between collider types"):**
- Tài liệu nêu: thông báo trigger xảy ra khi "một trigger collider dynamic hoặc kinematic va chạm với bất kỳ loại collider nào" và khi "một trigger collider static va chạm với collider dynamic hoặc kinematic". Trigger không có Rigidbody (static) chỉ báo khi đối tượng kia có Rigidbody.
- Tài liệu `CharacterController` không nói nó được tính như Rigidbody. Vì vậy KHÔNG dựa vào việc CharacterController tự kích hoạt trigger tĩnh. Thay vào đó đặt **Rigidbody kinematic lên chính vũng nước**. Khi đó trigger là "kinematic trigger collider" và theo bảng sẽ báo với mọi loại collider, kể cả CharacterController của người chơi. Cách này không phải sửa prefab người chơi.
- Layer của trigger là `Ignore Raycast`, không phải `Interactable`: `PlayerInteractor` bắn raycast layer Interactable với `QueryTriggerInteraction.Collide` nên trigger ở layer đó sẽ hiện thành vật tương tác giả.
- Chưa kiểm chứng trong Unity thật. Nếu trigger không báo với người chơi khi chạy thử, phương án dự phòng là thêm Rigidbody kinematic vào prefab người chơi (cần lead đồng ý).

Logic: `PuddleSplashPolicy` (thuần C#): chỉ bắn nước khi tốc độ ngang >= 0,5 m/s, mỗi đối tượng có cooldown 0,35 s riêng, rời vũng thì quên đối tượng. Người chơi lấy tốc độ từ `CharacterController.velocity`; xe không có Rigidbody thì tính từ độ dịch chuyển vị trí giữa các bước `OnTriggerStay`. Sự kiện: `PropCueRaised(PuddleSplash, vị trí mặt nước, cường độ)`. Layer chấp nhận: Player và NPC (chỉnh trong Inspector; layer xe lead cần xác nhận).

## 5. Dây treo (Hinge Joint và Spring Joint, Bài 6)

Hai prefab độc lập, đặt cạnh quầy, không đụng vào prefab quầy (hợp đồng art, AGENTS.md mục 4). Mỗi prefab có `Anchor` là Rigidbody kinematic ở gốc prefab (điểm treo) và vật treo là Rigidbody dynamic nối vào anchor bằng joint. Break Force và Break Torque = Infinity nên joint không bao giờ đứt, vật không rơi.

**`PF_Physics_HangingSign_Hinge` (bảng hiệu nhỏ trống chữ, vật liệu xám trung tính; không có chữ hay logo):**

| Thông số | Giá trị | Lý do |
|---|---|---|
| Hinge Axis | (0, 0, 1) | Chỉ lắc trong mặt phẳng X-Y, như bảng treo bằng móc |
| Use Limits | -30 đến +30 độ | Lắc nhẹ, không lật vòng qua đầu |
| Limit Bounciness | 0,1 | Chạm giới hạn thì không bật mạnh |
| Mass | 0,4 kg | Nhẹ nên gió và cú chạm nhẹ làm nó lắc |
| Angular Damping (của Rigidbody) | 0,8 | Làm tắt dao động sau vài nhịp |
| Enable Collision | tắt | Không va với chính anchor |

**`PF_Physics_HangingBulb_Spring` (bóng đèn):**

| Thông số | Giá trị | Lý do |
|---|---|---|
| Spring | 40 N/m | Độ cứng. Sa xuống khoảng m*g/k = 0,3*9,81/40 = 7 cm do trọng lượng |
| Damper | 4 | Giảm chấn: đủ để bóng dập dềnh vài lần rồi yên |
| Min / Max Distance | 0,5 / 0,5 m | Chiều dài tự nhiên của dây; lực lò xo chỉ sinh ra khi kéo giãn hay nén ngoài khoảng này |
| Tolerance | 0,025 | Sai số cho solver |
| Mass | 0,3 kg | |
| Linear / Angular Damping | 0,2 / 0,5 | Tắt dần cả lắc ngang |

Dây là hình trụ được `JointRopeVisual` kéo giãn giữa anchor và bóng mỗi `LateUpdate`.

**Lực nhỏ để lắc (`HangingSwayDriver`):**
- Gió: mỗi `FixedUpdate` thêm lực `SwayModel.Gust` (hai sóng sin chu kỳ 4,1 s và 2,3 s, luôn trong khoảng +/- cường độ) theo hướng gió. `Wind` mặc định 0,15 (0 = lặng gió, 1 = giật mạnh). Lead có thể nối `Wind` với mưa/thời tiết.
- Người đi sát: vùng `NudgeZone` (SphereCollider trigger, con của vật treo, bán kính 0,75 m cho bảng và 0,6 m cho bóng). `OnTriggerEnter` gọi `Nudge` với xung lực tỉ lệ tốc độ người chơi nhưng bị chặn tối đa 0,25 N.s (`SwayModel.NudgeImpulse`), nên không thể hất văng vật. Trigger này thuộc Rigidbody dynamic của vật treo, do đó là "dynamic trigger collider" và báo được với CharacterController theo bảng tài liệu.
- Collider rắn của vật treo loại trừ layer Player/NPC/Interactable/HeldItem, nên không cản người đi qua và không bị camera hay raycast tương tác bắt.

Vị trí đề xuất: bảng hiệu treo ở mép mái che phía khách, bóng đèn treo dưới mái hoặc cột đèn gần quầy. Đặt cách chân quầy ít nhất 0,4 m ngoài footprint (1,8 x 0,8 m, GT-001), điểm treo cao tối thiểu 2,2 m so với sàn để vật treo ở trên đầu người chơi (CharacterController cao 1,7 m). Kiểm bằng `python3 Automation/prefab_bounds.py <prefab> --overlaps --floating`. Vị trí cụ thể do lead và chủ game chốt, không có chữ trên bảng nếu chưa có văn bản được duyệt.

## 6. Collision và Trigger khác nhau thế nào

| | Collision (`OnCollisionEnter`) | Trigger (`OnTriggerEnter`) |
|---|---|---|
| Collider | `Is Trigger` tắt | `Is Trigger` bật |
| Vật lý | Hai vật chặn nhau, có lực và nảy | Không chặn, vật đi xuyên qua |
| Tham số | `Collision` (điểm chạm, `relativeVelocity`, lực) | `Collider other` (chỉ biết ai vào/ra) |
| Dùng cho | Rác chạm đất, nảy | Vùng cảm biến: vũng nước, vùng chạm nhẹ |
| Sự kiện | Enter / Stay / Exit | Enter / Stay / Exit |

## 7. Vì sao OnTrigger/OnCollision cần Rigidbody

Unity chỉ phát thông báo khi mô phỏng vật lý thực sự "xử lý" cặp collider, mà engine chỉ theo dõi chuyển động của collider gắn Rigidbody. Hai collider tĩnh (không có Rigidbody) không bao giờ phát sự kiện với nhau vì chúng không di chuyển. Quy tắc từ tài liệu "Interaction between collider types":
- Va chạm (`OnCollision`): cần ít nhất một bên là Rigidbody dynamic hoặc kinematic.
- Trigger (`OnTrigger`): trigger dynamic hoặc kinematic báo với mọi loại collider; trigger static chỉ báo khi đối tượng kia dynamic hoặc kinematic. Cần có Rigidbody ở ít nhất một bên.
- Vì thế vũng nước mang Rigidbody kinematic (xem mục 4) và vùng chạm nhẹ nằm trên Rigidbody của vật treo.

Fixed Joint (không dùng trong game): khoá cứng hai vật vào nhau như hàn, dùng cho vật gắn cố định có thể bị đứt khi lực vượt Break Force. Hinge cho xoay quanh một trục (cửa, bảng treo). Spring là lò xo kéo về khoảng cách tự nhiên với Spring (độ cứng), Damper (giảm chấn), Min/Max Distance.

## 8. Cách demo cho thầy cô

1. Mở Unity, chạy menu `Tram Chanh > Physics > Build Physics Props` (chạy lại nhiều lần vẫn an toàn). Xem Console để thấy danh sách tạo/cập nhật.
2. Mở `Art/PhysicsMaterials/PM_Debris`, chỉ Bounciness 0,35 và Bounce Combine Maximum; chỉnh Bounciness về 0 rồi chạy lại để thấy rác không nảy nữa.
3. Rác: kéo `PF_Physics_DiscardedPropSpawner` vào scene, cầm một món rồi bỏ (đoạn nối của lead). Quan sát rơi, nảy, mờ, biến mất; bỏ hơn 8 món liên tiếp để thấy giới hạn. Mở Scene view và chọn một vật rác để thấy Rigidbody, BoxCollider, material.
4. Vũng nước: đặt `PF_Physics_PuddleTrigger_Oval` trùng một vũng; đi vào và xem Console/âm thanh nhận `PropCueRaised`. Bỏ Rigidbody kinematic khỏi prefab để chứng minh trigger tĩnh không báo.
5. Dây treo: đặt hai prefab treo cạnh quầy, đứng yên thấy lắc nhẹ do gió; đi sát để bị đẩy nhẹ; chọn joint trong Inspector chỉnh Damper (4 sang 0) để thấy dao động kéo dài, chỉnh Limits của bảng hiệu để thấy chặn góc.
6. Trong Scene view bật Gizmos để thấy collider (xanh lá) và trigger.

## 9. Kiểm thử

EditMode: `Assets/TramChanh/Tests/EditMode/Props/PropsLogicTests.cs` (assembly `TramChanh.Tests.EditMode.Props`) kiểm `DebrisBudget`, `ImpactCuePolicy`, `PuddleSplashPolicy`, `SwayModel`. Hành vi vật lý (rơi, nảy, trigger, joint) chưa có test tự động vì cần chạy Unity; kiểm bằng demo ở mục 8.
