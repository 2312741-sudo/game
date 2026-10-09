# Chơi thử bằng tay — SCN_TramChanh_Main (tiếng Việt)

Đây là bước bắt buộc trước khi PR #22 thôi ở trạng thái draft. Phải **tự chơi bằng chuột và bàn phím** trong cửa sổ Game.
Mục nào chưa làm thì ghi **CHƯA LÀM**. **Không** đánh dấu đạt cho mục chưa làm.
Bản đầy đủ (tiếng Anh) nằm ở `Docs/QA/PLAYTEST_MAIN_LOOP.md`.

## Chuẩn bị (khoảng 5 phút)

1. Chỉ mở thư mục **`/Users/nthtam/Lưu trữ/game-QA-22`** trong Unity Hub, bằng Unity **6000.6.0f1**.
   Không mở `Lưu trữ/game`, vì Antigravity đang làm việc ở đó.
2. Trong Terminal, gõ `git -C "/Users/nthtam/Lưu trữ/game-QA-22" rev-parse --short HEAD` và ghi lại mã commit vào báo cáo.
3. Trong Unity, chọn menu **Tram Chanh ▸ Scenes ▸ Open Main Game**.
4. Mở **Window ▸ General ▸ Console** và bấm **Clear**.
5. Bấm **Play**, rồi click một lần vào cửa sổ Game để khóa chuột.

| Phím | Tác dụng |
|---|---|
| W A S D | Đi lại |
| Chuột | Nhìn |
| **E** hoặc chuột trái | Dùng thứ đang nhìn vào (quầy, khách, kệ) |
| **F** hoặc chuột phải | Dùng món đang cầm |
| Giữ E hoặc F | Thao tác cần giữ (có thanh tiến độ) |
| **Esc** | Tạm dừng, nhả chuột |

Màn hình có ba vùng:
- **Góc trái trên:** danh sách đơn, có dòng "Tiếp theo: …" chỉ chỗ cần đến.
- **Góc phải trên:** món đang cầm và bước tiếp theo.
- **Giữa màn hình:** việc có thể làm với thứ đang nhìn, kèm lý do nếu không làm được.

## 1. Đơn chỉ có nước (Bàn 1)

- [ ] Nhìn vào **Bàn 1** và bấm E. Cửa sổ đơn hiện ra. Bấm **Nhập đơn**, rồi **Gửi tới quầy**.
- [ ] Đi ra sau quầy, nhìn **kệ trà đỏ** và bấm E. Bạn cầm được một túi trà; góc phải ghi "Cho Bàn 1".
- [ ] Bấm F để **mở túi**. Lần lượt bấm E vào **thạch dừa**, rồi **thạch chanh**, rồi **đá**.
- [ ] **Giữ F để lắc**, sau đó **giữ E ở chỗ lau**. Cả hai lần đều có thanh tiến độ.
- [ ] Nhìn **quầy Ready** và bấm E. Túi trà rời tay và nằm trên quầy.
- [ ] Bấm E ở **chỗ lấy đơn**. Bạn cầm khay đơn; màn hình ghi mang tới Bàn 1.

## 2. Đơn chỉ có bánh (Bàn 2)

- [ ] Nhận đơn ở **Bàn 2** và gửi tới quầy.
- [ ] Bấm E vào **ca đong 500 ml** để cầm lên. **Giữ E ở chỗ bột** để đong.
- [ ] Ở **khuôn nướng**, bấm E lần lượt: mở nắp → đổ bột → đóng nắp. Chờ bánh chín, rồi E mở nắp → E **lật bánh**.
- [ ] **Giữ E** để cắt. **Giữ E ở túi sốt** để rưới sốt. **Giữ E** để cuộn dọc. Ở **giấy gói**, bấm E để gói.
- [ ] Đặt bánh lên quầy Ready, lấy đơn, rồi mang tới Bàn 2.

## 3. Đơn kết hợp nước + bánh (xe mang đi)

- [ ] Từ vỉa hè, nhìn **chiếc xe** và bấm E để nhận đơn, rồi gửi tới quầy.
- [ ] Làm xong nước và đặt lên Ready. Lúc này **chưa lấy được đơn**, và màn hình ghi 1/2 món xong.
- [ ] Làm xong bánh và đặt lên Ready. Giờ **lấy được đơn**; mang tới xe.

## 4. Quầy Ready đã đầy

- [ ] Để một ly nước của đơn kết hợp nằm chờ bánh trên quầy Ready, và có thêm một đơn nước khác đã gửi tới quầy. Thử lấy túi trà mới: bị từ chối, màn hình báo "Chỗ đặt đang bận".
- [ ] Lấy đơn trên quầy đi (sau khi làm xong bánh). Kệ trà lại cho lấy túi bình thường.

## 5. Kệ trà tự bổ sung

- [ ] Phục vụ liền **ít nhất 3 đơn nước**. Kệ chỉ chứa 2 túi nhưng không bao giờ bị hết hẳn.

## 6. Gỡ kẹt món đang cầm

- [ ] Đang cầm một món, bấm E vào kệ, hũ thạch hoặc khách khác: bị từ chối, có ghi lý do, không mất món.
- [ ] Cầm **ca đong rỗng** khi chưa có phiếu bánh, bấm F: ca được đặt về chỗ cũ ("Đặt ca đong về chỗ cũ"), và bạn nhận đơn tiếp được.
- [ ] Làm sai thứ tự (ví dụ thạch chanh trước thạch dừa, hoặc lau trước khi lắc): bị từ chối, có lý do dễ hiểu, không bị kẹt.

## 7. Giao đúng khách

- [ ] Cầm khay của **Bàn 1** rồi bấm E ở **Bàn 2** hoặc **xe**: thông báo đỏ "Sai chỗ…", khay vẫn trên tay.
- [ ] Bấm E ở **đúng Bàn 1**: thông báo xanh "Hoàn tất đơn Bàn 1!", tay trống.

## 8. Khách tiếp theo

- [ ] Sau khi hoàn tất đơn ở Bàn 1, Bàn 2 và xe, vài giây sau mỗi chỗ lại có **khách mới** ("Chờ nhận đơn").

## Kết thúc

- [ ] Bấm dừng Play. Ghi lại **mọi dòng đỏ** trong Console.
- [ ] Chụp màn hình cửa sổ Game (`Cmd+Shift+4`, nhấn Space, click vào cửa sổ Unity) ở các bước 1, 2, 3, 4, 7. Lưu vào `~/TramChanh-validation/choi-thu/`.
- [ ] Chạy `git -C "/Users/nthtam/Lưu trữ/game-QA-22" status --short`. Nếu Unity tự sửa file `.mat`, chỉ ghi lại tên file, không commit.

## Mẫu báo cáo (gửi lại cho Claude để đăng lên PR #22 và Issue #20)

```
Chơi thử SCN_TramChanh_Main — commit: ______ — Unity 6000.6.0f1 — người chơi: ______
1 Nước: ĐẠT / LỖI / CHƯA LÀM   2 Bánh: ...   3 Kết hợp: ...   4 Ready đầy: ...
5 Kệ trà: ...   6 Gỡ kẹt: ...   7 Giao đúng khách: ...   8 Khách tiếp theo: ...
Lỗi Console: (không có / liệt kê)
Lỗi gặp phải (các bước để lặp lại): ...
Ảnh: (đính kèm)
```
