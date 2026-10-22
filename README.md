# Trạm Chanh — Game mô phỏng quầy trà chanh vỉa hè

Bạn đứng sau một quầy trà trái cây và bánh lăn nướng trên một con phố dài 500 m, từ 18:00 đến 24:00 mỗi đêm. Khách ngồi ở bàn nhựa bên đường hoặc chạy xe máy tấp vào mua mang đi. Bạn nhận đơn, pha trà, nướng bánh, đặt lên quầy rồi mang ra giao đúng người.

Dự án Unity 6 (URP). Thư mục gốc của repo cũng là thư mục gốc của project Unity.

> Trạng thái: bản chơi thử cho bạn bè. Game chạy được trọn một đêm và nhiều đêm liên tiếp, nhưng cân bằng giá, độ khó và nội dung vẫn đang chờ chủ quán chơi thử và góp ý.

---

## 1. Cách chơi

### Điều khiển

| Phím | Việc |
|---|---|
| `W A S D` | Di chuyển |
| Chuột | Nhìn quanh |
| `E` hoặc chuột trái | Dùng thứ đang ngắm vào (quầy, bàn, dụng cụ) |
| `F` hoặc chuột phải | Dùng món đang cầm trên tay |
| `R` | Bước tiếp theo khi làm bánh. Cũng là mua gấp một gói nguyên liệu ở cửa hàng góc phố khi quầy đang mở (mỗi lần cách nhau 30 giây trong game) |
| `V` | Đổi góc nhìn thứ nhất / thứ ba |
| `Esc` | Tạm dừng (có chỉnh âm lượng và tốc độ chuột) |
| `F1` | Bảng ghi công |

### Một đêm trôi qua thế nào

1. **Nhận đơn:** đi tới bàn có khách đang chờ (hoặc xe máy đỗ ở lề đường), nhìn vào bàn, bấm `E`. Cửa sổ gọi món mở ra: nhập đơn rồi gửi vào quầy.
2. **Làm món:** lấy túi trà, thêm thạch, đá, lắc, lau túi; hoặc đổ bột, nướng, lật, cắt, phết sốt, cuộn, gói bánh.
3. **Đặt lên quầy:** đặt món đã xong lên quầy giao.
4. **Giao:** nhìn vào quầy khi tay trống rồi bấm `E` để cầm món lên, mang tới đúng bàn và bấm `E` để giao. Giao nhầm bàn sẽ bị từ chối.
5. **Giao từng phần:** nếu đơn có cả nước và bánh, bạn có thể mang nước ra giao trước khi bánh xong. Đơn chỉ hoàn tất và tính tiền khi giao nốt phần còn lại.
6. **Hết đêm (24:00):** bảng tổng kết ngày, sau đó cửa hàng nhà cung cấp để mua nguyên liệu và nâng cấp rồi mở hàng cho đêm sau.

### Những thứ cần để ý

- **Khách mất kiên nhẫn:** mỗi đơn có một thanh kiên nhẫn dưới thẻ đơn (xanh, rồi cam, rồi đỏ). Hết thanh là khách bỏ đi. Thời gian chờ đã được tăng 1,5 lần so với bản đầu. Khi bạn cầm đơn đi giao thì khách thôi mất kiên nhẫn.
- **Kho nguyên liệu:** đá, thạch, bột bánh và giấy gói có hạn. Hết thì khách bỏ đi ngay. Đá tan qua đêm.
- **Uy tín (sao):** phục vụ nhanh và ngon thì lên sao. Sao cao hơn mở thêm khách quen, nhà phê bình và các vị trà mới.
- **Nâng cấp:** mua ở cửa hàng nhà cung cấp, ví dụ thùng đá lớn, kho rộng hơn, đèn dây, giảm giá sỉ, bảng thực đơn vàng.
- **Nhân viên phụ:** nâng cấp cao nhất (cần 3 sao và 350.000đ, lương 30.000đ mỗi đêm). Nhân viên đứng cạnh bạn ở quầy, đi tới bàn để nhận đơn, gửi vào quầy, rồi mang món ra giao khi đơn đủ món mà bạn chưa lấy.
- **Sự kiện đêm, mục tiêu ngày, độ khó:** mỗi đêm có một sự kiện (trời quang, mưa, đêm nóng, mất điện, lễ hội hoặc đoàn kiểm tra), một mục tiêu trong ngày và ba mức độ khó (Chill, Normal, Rush).

Giá món và thực đơn là của chủ quán, nằm trong dữ liệu chứ không viết cứng trong mã.

---

## 2. Chạy project trong Unity

**Cần có:** Unity `6000.6.0f1` (xem `ProjectSettings/ProjectVersion.txt`) và Git LFS.

```bash
git clone https://github.com/2312741-sudo/game.git
cd game
git lfs pull
```

1. Mở thư mục bằng Unity Hub, chờ nhập xong tài nguyên.
2. `TramChanhProjectSetup` tự chạy lần đầu: tạo các layer vật lý, không gian màu tuyến tính và asset URP (trong `Assets/TramChanh/Settings/Rendering/`). Chạy lại bằng **Tram Chanh ▸ Setup ▸ Apply Project Settings**.
3. Mở cảnh `Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity` rồi bấm Play. Màn hình tiêu đề hiện ra, bấm **Chơi**.

**Lưu ý:** các file cấu hình URP và `ProjectSettings/` không được git theo dõi vì Unity tự sinh ra ở máy mỗi người. Vì vậy muốn gửi game cho người không dùng Unity, hãy gửi **bản build** chứ không gửi repo.

### Build cho người chơi

**File ▸ Build Profiles** rồi chọn nền tảng. Máy Mac mặc định chỉ có module macOS và WebGL. Muốn build cho Windows phải cài thêm **Windows Build Support (Mono)** trong Unity Hub.

---

## 3. Kiểm tra chất lượng

**Trong Unity:** Window ▸ General ▸ Test Runner, chạy EditMode và PlayMode.

Một test PlayMode (`TC_ORDER_UI_MousePipelineActivatesEnterSendAndCloseWithoutRecapturingGameplay`) mô phỏng chuột thật qua UI Toolkit nên đôi khi đỏ ngẫu nhiên tuỳ cửa sổ Unity có được focus hay không. Nó cũng đỏ như vậy trên bản gốc, không phải lỗi của một thay đổi cụ thể.

**Không cần Unity:**

```bash
python3 Automation/static_preflight.py          # kiểm tra GUID, fileID, meta (phải PASS)
python3 Automation/art_contract_audit.py        # kiểm tra hợp đồng art (phải 0 FAIL)
python3 Automation/qa000_repo_check.py          # kiểm tra repo, assembly, tên chuẩn
```

Các script của gói registry (URP...) nằm ngoài repo, được liệt kê trong `Automation/preflight_external_guids.txt`. Thêm một dòng chỉ khi bạn đã tra GUID trong thư mục cache của gói.

**Công cụ trong menu Unity (`Tram Chanh ▸ ...`):**

| Menu | Dùng để |
|---|---|
| QA ▸ Capture Standard Views | Chụp 12 góc chuẩn trong Play Mode, báo cáo hiệu năng cả con phố |
| QA ▸ Check Assembled Stall Overlaps | Kiểm tra vật thể trong quầy có chồng nhau không |
| Dev ▸ Grant Stall Helper (Play Mode) | Cấp nhân viên phụ để thử, không lưu |
| Art ▸ Apply ... Pass 2 | Áp dụng các đợt nâng cấp chất liệu, mô hình và ánh sáng (chạy lại sau khi chạy các builder môi trường) |

Danh sách kiểm tra khi chơi thử: `Docs/QA/PLAYTEST_CHECKLIST.md`.

---

## 4. Cấu trúc thư mục

```text
Assets/TramChanh/
  Scripts/        mã chạy, mỗi module một assembly (xem Docs/ARCHITECTURE.md §4)
    Core/ Orders/ Interaction/ Drinks/ Cakes/ Lobby/ Customers/ People/ Stall/ UI/
    Audio/ Save/  (Audio đã nối vào game; Save là mô-đun lưu JSON chưa nối, game đang lưu bằng PlayerPrefs)
    Editor/       công cụ Editor, builder, applier
  Tests/          EditMode và PlayMode
  Art/ Prefabs/ Scenes/ ScriptableObjects/ UI/  tài nguyên
ArtSource/        script sinh mô hình/texture, ảnh tham chiếu của chủ quán
Automation/       script kiểm tra không cần Unity
Docs/             tài liệu thiết kế, kiến trúc, quy trình, QA
QA/               báo cáo và ảnh chụp kiểm tra
```

Các assembly chính: `Core` (sự kiện, đồng hồ, kiểu ID), `Orders` (miền đơn hàng thuần C#), `Interaction` (tương tác và tay cầm), `Drinks` và `Cakes` (làm món), `Lobby` (điểm nhận đơn và giao), `Customers`, `People` (đường đi của người và xe), `UI`, `Audio`, `App` (nơi ghép toàn bộ game, `TramChanhMainBootstrap`).

---

## 5. Tài liệu

| Chủ đề | Tệp |
|---|---|
| Kiến trúc và sự thật nền | `Docs/ARCHITECTURE.md` |
| Đơn hàng, tương tác, làm nước, làm bánh | `Docs/ORDER_SYSTEM.md`, `Docs/INTERACTION_SYSTEM.md`, `Docs/DRINK_WORKFLOW.md`, `Docs/CAKE_WORKFLOW.md` |
| Thiết kế game và lộ trình | `Docs/Design/GAMEPLAY_DESIGN.md`, `Docs/Design/ROADMAP.md` |
| Đường phố 500 m | `Docs/Design/STREET_500M_SPEC.md` |
| Quy trình art, tài nguyên | `Docs/Design/ART_PIPELINE.md`, `Docs/ASSET_INTEGRATION.md`, `ArtSource/README.md` |
| Đợt nâng cấp đồ hoạ 2 | `Docs/Design/NIGHT_ATMOSPHERE_PASS2.md`, `Docs/Design/CHARACTER_PBR_PASS2.md` |
| Quy ước mã | `Docs/CODING_CONVENTIONS.md` |
| Kế hoạch và trạng thái | `Docs/PROJECT_TASK_PLAN.md` |

---

## 6. Quy tắc làm việc

- Chỉ có một nhánh chính là `main`. Tính năng mới làm trên nhánh riêng rồi gộp vào `main` qua pull request hoặc fast-forward, không force-push.
- Đọc `AGENTS.md` trước khi làm: quy tắc git, bằng chứng khi báo "xong", quy ước tài nguyên và hợp đồng giữa các phần.
- Tài nguyên 3D, texture hoặc âm thanh nào không do dự án tự tạo đều phải ghi vào `Docs/Reference/ASSET_LICENSES.md`. Chỉ nhận giấy phép **CC0** và **CC-BY**. Tài nguyên CC-BY còn phải thêm vào bảng ghi công trong `Assets/TramChanh/Scripts/Core/CreditsText.cs`.
- Thực đơn, giá, thương hiệu và hình ảnh quầy là của chủ quán. Không tự bịa món, công thức, logo hay khẩu hiệu. Ảnh tham chiếu của chủ quán trong `ArtSource/References` được ưu tiên hơn ý riêng.

---

## 7. Ghi công và giấy phép

Các mô hình và texture bên ngoài (Quaternius, Poly by Google, Kay Lousberg, Sketchfab...) được liệt kê đầy đủ kèm tác giả, nguồn và giấy phép trong `Docs/Reference/ASSET_LICENSES.md`. Trong game, bấm `F1` để xem bảng ghi công CC-BY.
