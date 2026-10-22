# Bài 7: Hệ thống hạt (Particle System) trong "Trạm Chanh"

Tài liệu này để trả lời vấn đáp: mỗi hiệu ứng dùng module nào, vì sao chọn giá trị đó, nằm ở file nào, bật tắt thế nào để demo.

**Trạng thái trung thực:** toàn bộ mã và công cụ này được viết khi không có Unity. Chưa biên dịch, chưa chạy, chưa có ảnh chụp. Các giá trị bên dưới là giá trị khởi điểm có lý do, chưa được chỉnh bằng mắt. Mọi chỗ cần kiểm tra khi mở Unity được liệt kê ở mục 8.

## 1. Tổng quan

| Hiệu ứng | Thành phần Unity | Prefab (do công cụ tạo) |
|---|---|---|
| Mưa + nước bắn | Particle System (Stretched Billboard, Collision, Sub Emitter) | `PF_NightEffects` (con `Rain`, `Splash`, `GroundPlane`) |
| Hơi nước vỉ nướng | Particle System (Billboard) | `PF_FX_Steam_Grill` |
| Hơi nước trà nóng | Particle System (Billboard) | `PF_FX_Steam_Cup` |
| Vệt đèn hậu xe máy | Trail Renderer | `PF_FX_Taillight` |
| Dây điện võng | Line Renderer | `PF_FX_PowerCord` |

Tất cả dùng shader `Universal Render Pipeline/Particles/Unlit` (dây dùng `Universal Render Pipeline/Unlit`), không bóng đổ, không đọc ánh sáng, nên rẻ. Texture hạt do `ArtSource/Scripts/generate_particle_textures.py` sinh bằng numpy + PIL (giọt mưa, tia nước, hơi, đốm sáng, dải vệt). Giấy phép: Project Original, CC0, đã ghi trong `Docs/Reference/ASSET_LICENSES.md`.

## 2. Các thuộc tính chung (khái niệm bài 7 ứng với đâu)

| Khái niệm | Nơi dùng | Giá trị | Lý do |
|---|---|---|---|
| Duration, Looping | mưa 5 s lặp; hơi 2,5 s lặp; tia nước 0,5 s không lặp | | Hiệu ứng liên tục thì Looping. Tia nước chỉ là một cú bắn nên không lặp. |
| Start Lifetime | mưa 0,8 đến 1,0 s; hơi 1,8 đến 2,6 s (vỉ), 1,2 đến 1,8 s (ly); tia nước 0,25 đến 0,4 s | | Hạt mưa rơi 8 m ở tốc độ 13 đến 17 m/s mất khoảng 0,55 s nên sống 0,8 s là đủ chạm đất. Hơi thì lâu hơn để kịp lan và tan. |
| Start Speed | mưa 13 đến 17; hơi 0,3 đến 0,6; tia nước 0,5 đến 1,3 | m/s | Khoảng ngẫu nhiên để các hạt không đều tăm tắp. |
| Start Color (Gradient) | mưa: hai màu xanh xám nhạt, alpha 0,30 và 0,60 (Two Colors, mỗi hạt một màu ngẫu nhiên giữa hai màu) | | Mưa ban đêm phải mờ, không được trắng gắt. |
| Max Particles | mưa 600; tia nước 150; hơi vỉ 80; hơi ly 30 | | Trần cứng cho ngân sách. Mưa: 700 hạt/s x 0,55 s sống thực tế khoảng 385, dưới trần. |
| Emission: Rate over Time | mưa 700/s (nhân với cường độ 0..1); hơi vỉ 9/s; hơi ly 4/s | | Cường độ mưa là thứ duy nhất mã đổi lúc chạy. |
| Emission: Bursts | hơi: 2 đến 3 hạt đầu mỗi vòng; tia nước: 2 đến 3 hạt mỗi lần chạm đất; vỉ nướng: `Emit(14)` khi lật bánh | | Sub emitter không có rate riêng, chỉ có burst. Cú lật bánh là sự kiện nên bắn tay bằng `Emit`. |
| Shape | mưa: Box 20 x 20 x 0,5 xoay 90 độ quanh X để hướng phát chỉ xuống; hơi: Cone góc 8 độ; tia nước: Cone góc 35 độ chỉ lên | | Hộp phẳng phía trên người chơi phủ vùng nhìn thấy mà không phải mưa cả phố. |
| Render Mode, Material | mưa: Stretched Billboard (lengthScale 8, velocityScale 0,02); còn lại Billboard | | Stretched làm hạt thành vệt kéo dài theo vận tốc nên gió làm vệt nghiêng. |
| Force over Lifetime | mưa: X từ 4 đến 8 m/s² (gió); hơi: Y 0,10 đến 0,20 (bốc lên), X, Z trôi nhẹ | World space | Ba trục bắt buộc cùng kiểu đường cong (cùng là Two Constants). |
| Color over Lifetime | hơi: trắng, alpha 0 rồi 1 (ở 15 %) rồi 0; tia nước: alpha 1 rồi 0 | | Trắng đục sang trong suốt. Alpha 0 ở hai đầu để hạt không bật hiện ra hay biến mất đột ngột. |
| Size over Lifetime | hơi: 0,45 đến 1,6 lần kích thước đầu | | Hơi nở ra khi bốc lên. |
| Rotation over Lifetime | hơi: ngẫu nhiên từ -30 đến +30 độ/s (lưu theo radian) | | Các hạt chồng nhau không giống hệt nhau. |
| Size by Speed | tia nước: 0,5 đến 1 lần, khoảng vận tốc 0 đến 1,5 m/s | | Giọt đang bắn nhanh to hơn giọt gần dừng. |
| Collision | mưa: kiểu Planes, một mặt phẳng tại mặt đất, Dampen 1, Bounce 0, Lifetime Loss 1 | | Rẻ hơn World collision (không cần collider của phố); hạt chết ngay khi chạm đất. |
| Sub Emitters | mưa gọi `Splash` khi Collision, xác suất 0,15 | | Chỉ 15 % giọt tạo tia nước, đủ thấy mà không tốn hạt. |
| Simulation Space | World cho cả mưa và hơi | | Người chơi đi thì giọt đã rơi vẫn ở lại, không bị kéo theo. |

## 3. Mưa

- File: `Assets/TramChanh/Scripts/Effects/NightEffectsController.cs` (gắn hệ hạt vào người chơi, đổi Emission), `NightEffectsModel.cs` (quyết định cường độ), `SmoothedLevel.cs` (tăng giảm mượt). Cấu hình hạt nằm trong `Assets/TramChanh/Scripts/Editor/EffectsBuilder.cs`, hàm `ConfigureRain` và `ConfigureSplash`.
- Bám theo người chơi: mỗi `LateUpdate`, hệ hạt được đặt ở (x, mặt đất + 8 m, z) của mục tiêu; mặt phẳng va chạm đặt đúng độ cao mặt đất. World space nên không có cảm giác mưa dính vào người.
- Chỉ chạy khi `NightEvent.Rain` (hàm thuần `NightEffectsModel.EventHasRain`). Mở: cường độ lên 1 trong 4 s. Đóng: về 0 trong 3 s, rồi tắt Emission, đợi hạt cuối rơi hết (`particleCount == 0`) mới `Stop`. GameObject không bao giờ bị `SetActive(false)` giữa chừng nên không giật.
- `Play(false)` và `Splash` là hệ anh em chứ không phải con của `Rain`: để `Play()` của mưa không vô tình kích hoạt burst của tia nước ở trên cao.
- Giới hạn đã biết: mưa không bị mái che chặn (hạt rơi xuyên qua mái quầy). Chấp nhận để giữ rẻ; muốn chặn thì thêm mặt phẳng thứ hai ở độ cao mái.
- Demo: xem mục 7.

## 4. Hơi nước

- Vỉ nướng: `PF_FX_Steam_Grill`, lớn hơn (bán kính 0,18 m, hạt 0,25 đến 0,42 m). Hơi đều chạy khi có ít nhất một bánh đang ở trạng thái Cooking, Cooked hoặc Flipped (lớp thuần `GrillSteamTracker`, đếm theo `PreparationId` để hai bánh trên vỉ không tắt hơi của nhau). Khi nhận `CakeStepCompleted` có `Flipped` thì bắn thêm 14 hạt; khi `Cooked` bắn 6 hạt.
- Ly/túi trà nóng: `PF_FX_Steam_Cup`, nhỏ (bán kính 0,03 m, hạt 0,08 đến 0,14 m), luôn chạy khi hơi nước được bật. Gắn vào điểm neo `TeaRack`.
- Rate hơi thay đổi theo mức `SmoothedLevel` (1,2 s lên, 2 s xuống) để bật tắt không giật.
- Không bịa món: hiệu ứng chỉ bám vào bánh lăn và trà đang có.

## 5. Vệt đèn hậu (Trail Renderer)

- File: `PF_FX_Taillight` (dựng trong `EffectsBuilder.BuildTaillight`), `TaillightTrail.cs`, tìm xe trong `NightEffectsController.ScanVehicles`, luật đặt tên `VehicleNameRule.cs`.
- Time 0,5 s (ngắn, vệt khoảng 4 m khi xe chạy 8 m/s); Min Vertex Distance 0,1; Width 0,14 m giảm dần về 0,15 lần theo `widthCurve`; Color Gradient đỏ tươi sang đỏ sẫm, alpha 0,95 sang 0; vật liệu cộng sáng (additive); Alignment View; không đổ bóng.
- Tự tìm xe: mỗi giây quét con của đối tượng `People`, đối tượng tên bắt đầu bằng `Motorbike_` hoặc đúng `VehicleCustomer` (đúng tên `PeopleController` đặt) thì gắn một trail ở đuôi (offset mặc định (0; 0,65; -0,85), chưa đo trên mô hình thật). Không phải sửa `PeopleController`.
- Xe tái sử dụng/dịch chuyển tức thời làm vệt kéo ngang phố: `TaillightTrail` gọi `Clear()` khi xe nhảy hơn 3 m trong một khung hình (`TrailResetRule`).

## 6. Dây điện (Line Renderer)

- File: `SagWire.cs` (đặt Positions), `CatenaryCurve.cs` (toán), prefab `PF_FX_PowerCord`.
- Dây là một dây nguồn từ mép mái quầy xuống ổ cắm thấp (mặc định từ (0,9; 2,1; 0) đến (3,3; 0,45; 0) trong toạ độ của quầy), võng 0,5 m, 24 đoạn (25 Positions). Tính một lần khi tạo, không tốn gì mỗi khung hình, 1 draw call.
- Lý do chọn dây nguồn thấp: nó không chồng lên hai thứ có sẵn là dây cáp trên cột điện (cao, nằm trong `PF_UtilityPole_Cables`) và `PF_StringLights_Warm` (đèn dây ở độ cao 2,5 đến 3 m, là nâng cấp "Đèn dây"). Cần xem bằng mắt xem có xuyên qua đồ nội thất không (mục 8).
- Toán: catenary thật, y = a (cosh(h/a) - cosh(x/a)); với h là nửa nhịp ngang, tham số a giải bằng chia đôi sao cho điểm thấp nhất đúng bằng độ võng đã chọn. Khi hai đầu khác độ cao, độ võng đo thẳng xuống từ dây cung (xấp xỉ tốt cho dây chùng).
- Chỉnh: kéo hai đối tượng `End_A`, `End_B` trong prefab, đổi `Sag`, bấm chuột phải component `SagWire` chọn "Rebuild wire".

## 7. Bật tắt để demo

`NightEffectsController` có mục "Demo switches" trong Inspector (đổi khi đang chạy):

| Ô | Tác dụng |
|---|---|
| Force Rain | Mưa dù đêm đó là gì (chuột phải component: "Demo: rain on/off") |
| Rain Enabled / Steam Enabled / Taillights Enabled / Wire Enabled | Tắt từng nhóm |

Hoặc bằng mã: `SetNightEvent(NightEvent.Rain)`, `SetForceRain(true)`, `SetEffectEnabled(EffectKinds.Steam, false)`, `SetWind(8f)`. Chọn đối tượng `Rain` trong Hierarchy khi đang chạy, Unity hiện cửa sổ Particle Effect để xem từng module, số hạt đang sống (so với Max Particles).

Dựng prefab: menu **Tram Chanh > Effects > Build Effect Prefabs** (chạy lại an toàn, ghi log tiền tố `[EffectsBuilder]`); xoá: **Remove Effect Prefabs**. Cần sinh texture trước: `python3 ArtSource/Scripts/generate_particle_textures.py`.

## 8. Chưa kiểm chứng (kiểm tra khi mở Unity)

1. Mọi file chưa biên dịch; công cụ chưa chạy; prefab và vật liệu chưa tồn tại cho tới khi chạy menu.
2. Stretched Billboard kéo dài theo trục Y hay X của texture: texture hiện là vệt dọc. Nếu vệt nằm ngang, chạy lại script với `--raindrop-horizontal`.
3. Thông số mưa (kích thước, lengthScale, tốc độ gió) chưa chỉnh bằng mắt.
4. Offset đèn hậu và vị trí dây điện chưa đo trên mô hình thật.
5. Chi phí chưa đo: ước tính thêm khoảng 1 draw call mưa, 1 tia nước, 2 hơi, 1 dây, 1 mỗi xe (trail dùng chung vật liệu nên SRP Batcher có thể gộp). Phố 500 m đang gần ngưỡng draw call nên cần đo lại bằng `Capture Standard Views` trước và sau.
6. Mưa xuyên mái quầy (mục 3).
