# Bài 5 (UI nâng cao): Kéo và thả trong màn "Nhà cung cấp"

Tài liệu ôn cho buổi vấn đáp môn "Phát triển ứng dụng Game nâng cao". Mục kéo thả (Drag and Drop) của bài 5.

Lưu ý trung thực: phần code này được viết khi chưa mở Unity, chưa biên dịch, chưa chạy test và chưa thử bằng tay. Trước buổi vấn đáp cần mở Unity, chạy EditMode test và thử kéo thả thật (xem mục 6).

## 1. Tính năng

Ở màn "Nhà cung cấp" (cửa hàng giữa hai đêm), người chơi:

- Nhấn giữ một thẻ nguyên liệu (Đá, Thạch, Bột bánh, Giấy gói) hoặc một thẻ nâng cấp, kéo ra khỏi khay hàng.
- Thả vào ô "Giỏ mua" để mua (nguyên liệu: một gói 10; nâng cấp: một lần).
- Trong lúc kéo: một "ghost" (nhãn nổi) bám theo con trỏ; ô "Giỏ mua" sáng cam khi đang kéo, chuyển xanh lá nếu thả được, đỏ nếu không (không đủ tiền, kho đầy, thiếu sao uy tín, đã có rồi). Dòng chữ trong ô nói rõ lý do.
- Thả ra ngoài ô, hoặc thả khi không hợp lệ: ghost bay về chỗ thẻ cũ rồi biến mất, không mất tiền.
- Các nút "Mua 10" và "Mua" cũ vẫn còn, dùng song song (bàn phím, chuột bấm đơn giản).

## 2. Cơ chế kéo thả trong UI Toolkit (so với uGUI trong slide)

Slide dạy uGUI (Canvas + EventSystem): gắn script lên GameObject, cài các interface `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler` cho thẻ bị kéo và `IDropHandler` cho ô nhận. EventSystem tự raycast, tự gọi hàm tương ứng, và tự biết ô nào đang nằm dưới con trỏ.

UI Toolkit (màn Nhà cung cấp dùng cái này) không có các interface đó. Thay vào đó:

| uGUI (slide) | UI Toolkit (code của game) |
|---|---|
| `IBeginDragHandler.OnBeginDrag` | `PointerDownEvent` + `CapturePointer`, rồi `PointerMoveEvent` vượt ngưỡng 6 px thì mới tính là bắt đầu kéo (`DragSession.StartDistance`) |
| `IDragHandler.OnDrag` | `PointerMoveEvent` (chỉ xử lý khi `HasPointerCapture`) |
| `IEndDragHandler.OnEndDrag` | `PointerUpEvent` + `ReleasePointer` |
| `IDropHandler.OnDrop` trên ô nhận | Không có. Lúc `PointerUp`, tự kiểm tra `worldBound.Contains(vị trí)` của ô "Giỏ mua" (hàm `ZoneAt` trong `SupplierPanel`) |
| Hủy kéo (mất focus) | `PointerCaptureOutEvent` |
| Ghost: nhân bản Image, đổi `parent` sang Canvas | Một `Label` tuyệt đối (`Position.Absolute`) ở lớp trên cùng, `pickingMode = Ignore` để không chặn con trỏ; đặt `left`/`top` bằng `WorldToLocal(evt.position)` |
| Component gắn vào GameObject | `PointerManipulator` gắn vào VisualElement bằng `AddManipulator` |

Ý chính để trả lời vấn đáp:

- `CapturePointer`: sau khi nhấn, mọi sự kiện con trỏ (kể cả khi con trỏ rời khỏi thẻ) được gửi về thẻ cho tới khi thả. Đây là thứ thay cho việc EventSystem "nhớ" đối tượng đang kéo trong uGUI. Nếu thiếu capture, kéo nhanh ra ngoài thẻ sẽ mất sự kiện.
- `evt.position` là toạ độ trong hệ panel (toạ độ thế giới của UI), `evt.localPosition` là toạ độ trong thẻ. Chuyển sang toạ độ lớp ghost bằng `WorldToLocal`.
- Cùng một đường sự kiện (`Pointer*Event`) phục vụ chuột, cảm ứng và bút, nên một code chạy cho cả ba.
- Ghost quay về: dùng `transitionProperty` / `transitionDuration` của style (USS transition) cho `left` và `top`, rồi hẹn giờ ẩn bằng `schedule`.

## 3. Vì sao tách logic ra khỏi UI

Quy tắc trong dự án: luật chơi nằm ở lớp C# thuần, view chỉ nối sự kiện. Lý do:

1. Test EditMode không cần mở scene, không cần UI Toolkit panel, chạy trong vài mili giây.
2. "Một nơi duy nhất quyết định luật mua": thả thật gọi lại đúng `Supplier.TryBuyPack` và `UpgradeBook.TryBuy` (Core), cùng hàm mà nút "Mua" gọi. Không có bản luật thứ hai để lệch.
3. Dễ trả lời vấn đáp: mỗi lớp một việc.

| File (Assets/TramChanh/Scripts/UI/Menu) | Việc | Có Unity? |
|---|---|---|
| `DragRules.cs` | `Evaluate` (thả được không, vì sao không), `Drop` (mua thật qua Core), `GhostText`, `MessageFor` | không |
| `DragSession.cs` | Máy trạng thái một lần kéo: `Press`, `Move` (ngưỡng), `Hover`, `Release`, `Cancel` | không |
| `DragPayload.cs`, `DragKind.cs`, `DropZone.cs`, `DropVerdict.cs`, `DropOutcome.cs`, `DragRelease.cs`, `DragState.cs` | Kiểu dữ liệu nhỏ | không |
| `DragCardManipulator.cs` | `PointerManipulator`: nối sự kiện con trỏ vào `DragSession`, vẽ ghost, phát sự kiện cho view | có (UI Toolkit) |
| `SupplierPanel.cs` | Dựng thẻ, ô "Giỏ mua", tô màu theo kết quả, làm mới số liệu sau khi mua | có (UI Toolkit) |

Luồng một lần kéo:

```
PointerDown   -> DragSession.Press            (CapturePointer)
PointerMove   -> DragSession.Move             (qua 6 px: hiện ghost, phát DragStarted)
              -> DragSession.Hover(zone)      -> DropVerdict -> view tô ô Giỏ mua xanh/đỏ
PointerUp     -> DragSession.Release(zone)    -> DragRules.Drop -> Supplier/UpgradeBook (Core)
              -> Mua được: ẩn ghost.  Không được: ghost bay về thẻ.
              -> SupplierPanel.Refresh + context.Changed (lưu, áp hiệu ứng)
```

Một cú nhấp không di chuyển quá 6 px không phải là kéo, nên không mua gì (tránh mua nhầm).

## 4. Test

`Assets/TramChanh/Tests/EditMode/UI/DragDropTests.cs` (NUnit, EditMode), chỉ dùng lớp thuần:

- Thả ngoài ô là `OutsideZone`; nguyên liệu: hợp lệ / thiếu tiền / kho đầy.
- `Evaluate` khớp `Supplier.TryBuyPack` trên nhiều tổ hợp tiền và kho (bảo đảm hai đường mua không lệch luật).
- `Drop` hợp lệ trừ tiền, cộng kho, thông báo đúng; `Drop` bị từ chối không đổi gì.
- Nâng cấp: mua được, sau đó `AlreadyOwned`; thiếu sao uy tín; thiếu tiền.
- `DragSession`: nhấp không kéo thì không mua; kéo vào ô thì mua; thả ngoài hoặc không đủ tiền thì không mua; `Cancel`; `Hover` trước khi kéo bị bỏ qua.

Các test cũ (`SupplierPanelTests`, test khoá văn bản `PromptTextCoverageTests`) không bị đụng: mọi chuỗi mới là câu tiếng Việt, không có dạng khoá chấm (`abc.def`).

## 5. Câu hỏi vấn đáp dự kiến

- Khác biệt `IDropHandler` và cách làm ở đây? UI Toolkit không có handler cho ô nhận; ta tự hit-test bằng `worldBound.Contains` lúc thả.
- Tại sao cần `CapturePointer`? Để thẻ vẫn nhận `PointerMove`/`PointerUp` khi con trỏ đã ra khỏi thẻ.
- Tại sao ghost đặt `pickingMode = Ignore`? Để ghost không che con trỏ, nếu không các sự kiện đi vào ghost thay vì thẻ/ô.
- Tại sao có ngưỡng 6 px? Phân biệt nhấp với kéo.
- Tại sao không tính luật trong `SupplierPanel`? Để test được và chỉ có một nơi quyết định luật mua.
- Vì sao vẫn giữ nút "Mua"? Người chơi bàn phím/chuột đơn giản vẫn dùng được, và các test cũ vẫn đúng.

## 6. Cách demo (sau khi mở Unity)

1. Mở Test Runner, chạy EditMode, xác nhận `DragDropTests` và các test cũ đều xanh.
2. Chơi hết một đêm để vào màn "Nhà cung cấp".
3. Kéo thẻ "Đá" vào ô "Giỏ mua": ô xanh, thả thì kho Đá tăng 10, tiền giảm một gói.
4. Kéo thẻ khi tiền không đủ: ô đỏ, chữ "Không đủ tiền.", thả thì ghost bay về.
5. Kéo thẻ ra ngoài ô rồi thả: ghost bay về, không mất tiền.
6. Kéo thẻ nâng cấp tương tự; thẻ thiếu sao uy tín báo "Chưa đủ sao uy tín.".
7. Bấm nút "Mua 10" để cho thấy đường cũ vẫn chạy.

Chưa kiểm chứng: cảm giác kéo trên màn hình thật, hiệu ứng ghost quay về (transition), vị trí ghost so với con trỏ, kéo bằng cảm ứng.
