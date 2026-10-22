# Bài 5 và Bài 6: UI nâng cao và Audio Mixer (tài liệu vấn đáp)

Tài liệu này giúp trả lời vấn đáp về hai phần: UI cài đặt (Slider, Toggle, Dropdown, PlayerPrefs) và âm thanh (AudioSource, Audio Mixer, dB). Trạng thái: mã chưa được biên dịch hay chạy trong Unity. Asset `AM_TramChanh.mixer` chưa được tạo; phải chạy công cụ Editor một lần (xem mục 6).

## 1. Sơ đồ Audio Mixer

Asset: `Assets/TramChanh/Resources/Audio/AM_TramChanh.mixer` (nằm trong `Resources` để mã nạp được bằng `Resources.Load`, giống các clip `sfx_ready`...).

```
AM_TramChanh (AudioMixer)
└── Master                   tham số lộ ra: MasterVolume  (dB)
    ├── Sfx                  tham số lộ ra: SfxVolume     (dB)
    └── Ambient              tham số lộ ra: AmbientVolume (dB)
```

| Nhóm | Đường (path) | Tham số lộ ra | Nguồn phát vào nhóm |
|---|---|---|---|
| Master | `Master` | `MasterVolume` | tất cả (Sfx và Ambient là con nên đi qua Master) |
| Sfx | `Master/Sfx` | `SfxVolume` | mọi cue có `AudioBus.Sfx`: chuông phiếu, phục vụ xong, thất bại, nướng, rót... |
| Ambient | `Master/Ambient` | `AmbientVolume` | cue có `AudioBus.Ambient`: `StreetAmbientLoop` |

Đường định tuyến (từ sự kiện đến loa):

```
Sự kiện game (EventBus)
   -> AudioEventRouter  (đổi sự kiện thành AudioCue)
   -> AudioManager.Play(cue)
        - AudioMixModel quyết định: có phát không, cooldown, ưu tiên, số voice
        - lấy AudioSource trong bể (pool) 8 nguồn
        - source.outputAudioMixerGroup = nhóm của bus (Sfx hoặc Ambient)
   -> AudioMixer: nhóm Sfx/Ambient -> Master -> loa
```

Nhiều nguồn trộn vào một mixer: các AudioSource khác nhau cùng đổ vào nhóm Sfx, rồi Sfx và Ambient cùng đổ vào Master. Đây là ý nghĩa "trộn nhiều nguồn" của Audio Mixer: chỉnh một thanh trượt ảnh hưởng cả nhóm mà không phải sửa từng AudioSource.

Thanh trượt người chơi: Âm lượng (Master), Âm hiệu ứng (Sfx), Âm nền (Ambient) -> `AudioMixerController` đổi 0..1 sang dB -> `AudioMixerBridge` gọi `AudioMixer.SetFloat("SfxVolume", dB)`.

Chưa làm (có thể nói thêm khi vấn đáp): hiệu ứng như Lowpass/Reverb, Snapshot, ducking. Mixer hiện chỉ có 3 nhóm và 3 tham số.

## 2. Vì sao dùng dB?

- Tai người nghe độ lớn theo thang lôgarit, không phải tuyến tính. Mixer của Unity cũng lưu mức theo dB (từ -80 dB đến +20 dB; 0 dB là giữ nguyên).
- Nếu gán thẳng giá trị thanh trượt 0..1 vào mixer thì phần lớn thay đổi nghe được chỉ nằm ở đoạn đầu thanh trượt, rất khó chỉnh.
- Công thức: `dB = 20 * log10(v)`. Thí dụ v = 1 là 0 dB; v = 0.5 là khoảng -6 dB; v = 0.1 là -20 dB; v = 0.01 là -40 dB.
- v = 0 cho log10(0) = âm vô cực, không ghi được vào mixer, nên chặn ở -80 dB (coi như im lặng). Mã: `AudioDecibel.LinearToDb` (thuần logic, có test `AudioDecibelTests`).
- Chiều ngược lại `DbToLinear` dùng để kiểm tra vòng đi-về trong test.

## 3. Các lớp liên quan

| Lớp | Vai trò | Có test |
|---|---|---|
| `AudioDecibel` | Đổi 0..1 sang dB và ngược lại (thuần logic) | `AudioDecibelTests` |
| `AudioMixerController` | Thuần logic: tên tham số, đường nhóm, gửi 3 mức dB qua `IMixerFloatSink`; trả `false` nếu không có mixer hoặc tham số chưa lộ ra | `AudioMixerControllerTests` |
| `AudioMixerBridge` | MonoBehaviour mỏng: giữ `AudioMixer`, gọi `SetFloat`, tìm nhóm bằng `FindMatchingGroups` | (lớp Unity, kiểm tra bằng tay) |
| `AudioManager.ApplySettings(master, sfx, ambient)` | Nếu mixer nhận đủ 3 tham số: `AudioListener.volume = 1` và mức dB do mixer giữ. Nếu không có mixer: giữ hành vi cũ (`AudioListener.volume = master`, bus volume trên nguồn phát) | gián tiếp |
| `AudioMixerBuilder` (Editor) | Tạo asset mixer, menu `Tram Chanh/Audio/Build Audio Mixer` | `AudioMixerAssetTests` (bỏ qua nếu asset chưa có) |

Vì sao có đường dự phòng: nếu asset mixer chưa tạo, game vẫn chạy với âm lượng tổng như trước, không báo lỗi.

## 4. PlayerPrefs lưu gì

Tất cả đi qua `GameSettings` và `PlayerPrefsSettingsStore` (mỗi lần đổi, `PlayerPrefs.SetFloat` rồi `PlayerPrefs.Save`). Giao diện lưu trữ `ISettingsStore` chỉ có `TryGetFloat/SetFloat` nên mọi giá trị được lưu dạng số thực.

| Khoá PlayerPrefs | Ý nghĩa | Mặc định | Miền giá trị |
|---|---|---|---|
| `settings.master_volume` | Âm lượng tổng | 0.8 | 0..1 |
| `settings.sfx_volume` | Âm hiệu ứng | 1 | 0..1 |
| `settings.ambient_volume` | Âm nền | 1 | 0..1 |
| `settings.look_scale` | Tốc độ xoay chuột | 1 | 0.5..2 |
| `settings.show_key_hints_flag` | Toggle hiện gợi ý phím (1 là bật, 0 là tắt) | bật | 0 hoặc 1 |
| `settings.quality_level_index` | Mức chất lượng đồ hoạ (chỉ số trong `QualitySettings.names`; -1 là mặc định của dự án) | -1 | -1..63, kẹp theo số mức thực tế khi áp dụng |

Ghi chú để không bị hỏi vặn: khoá mới có hậu tố rõ nghĩa (`_volume`, `_flag`, `_index`). Đây là một lựa chọn thiết kế: bài giảng nhắc `SetInt/SetFloat/SetString`; dự án chọn một giao diện duy nhất (float) cho cài đặt để các cửa hàng lưu trữ cũ và test cũ không phải đổi, nên Toggle lưu 0/1 và mức chất lượng lưu số nguyên dạng float. Chuỗi JSON phím điều khiển (rebind) lưu riêng bằng `SetString` trong `PlayerPrefsRebindTextStore`.

Cài đặt cũ trong máy người chơi vẫn đọc được: khoá nào chưa có thì dùng mặc định.

## 5. Màn Cài đặt (PauseMenu, UI Toolkit)

Hộp thoại tạm dừng và màn hình tiêu đề dùng chung `PauseMenu`. Phần tuỳ chọn nằm trong `ScrollView` để cửa sổ thấp vẫn thấy các nút phía dưới.

| Điều khiển | Kiểu UI Toolkit | Tên (`name`) | Tác dụng |
|---|---|---|---|
| Âm lượng | `Slider` | `MasterVolume` | Master |
| Âm hiệu ứng | `Slider` | `SfxVolume` | Sfx |
| Âm nền | `Slider` | `AmbientVolume` | Ambient |
| Tốc độ xoay chuột | `Slider` | `LookScale` | độ nhạy chuột |
| Hiện gợi ý phím | `Toggle` | `ShowKeyHints` | cờ `GameSettings.ShowKeyHints` |
| Chất lượng đồ hoạ | `DropdownField` | `QualityLevel` | `QualitySettings.names`, đổi bằng `QualitySettings.SetQualityLevel` (qua `GraphicsQuality.Apply`); vô hiệu hoá nếu chỉ có 1 mức |
| Mặc định | `Button` | | `ResetToDefaults()`: đặt lại cả 6 giá trị, áp dụng lại chất lượng đồ hoạ mặc định của dự án |

`GraphicsQuality` ghi nhớ mức chất lượng mặc định của dự án lúc khởi động (trước khi nạp cảnh) để nút Mặc định quay về đúng mức đó.

Lưu ý khi vấn đáp: cờ "Hiện gợi ý phím" đã được lưu và có trong `GameSettings`; việc ẩn/hiện gợi ý trên HUD thực tế cần nối thêm ở phía HUD/bootstrap (xem báo cáo giao việc). Phần UI cài đặt và lưu trữ đã xong, còn tác dụng lên HUD thì chưa.

## 6. Cách demo

Chuẩn bị (một lần, trong Unity Editor): menu `Tram Chanh > Audio > Build Audio Mixer`. Console in dòng `[AudioMixerBuilder] Created ... Groups Master/Sfx/Ambient found: True. Exposed: MasterVolume, SfxVolume, AmbientVolume.` Sau đó commit file `.mixer` và `.meta`.

Xem trong Unity: mở `AM_TramChanh` bằng cửa sổ Audio Mixer (`Window > Audio > Audio Mixer`): thấy 3 nhóm; mở `Exposed Parameters` ở góc trên phải thấy 3 tham số. Nhấn Play, mở Pause, kéo thanh Âm hiệu ứng: trong cửa sổ Audio Mixer đồng hồ mức của nhóm Sfx đổi theo.

Kịch bản trên màn hình:
1. Vào game, nhấn Esc để tạm dừng, kéo Âm lượng, Âm hiệu ứng, Âm nền (nghe thay đổi khi có chuông phiếu hoặc nền phố).
2. Bật/tắt "Hiện gợi ý phím", đổi "Chất lượng đồ hoạ" trong danh sách thả xuống.
3. Thoát game, mở lại: các giá trị còn nguyên (PlayerPrefs). Xoá bằng `Edit > Clear All PlayerPrefs` nếu cần bắt đầu lại.
4. Nhấn "Mặc định": mọi thanh trượt, Toggle, Dropdown trở về giá trị mặc định.
5. Con trỏ chuột: lúc tạm dừng con trỏ được mở khoá để thao tác UI, quay lại chơi thì khoá lại (do `PlayerInputReader.SetCaptured`).

Chạy test: Window > General > Test Runner > EditMode: `AudioDecibelTests`, `AudioMixerControllerTests`, `AudioMixerAssetTests` (bỏ qua nếu chưa build mixer), `GameSettingsTests`, `PauseMenuTests`, `PromptTextCoverageTests`.

## 7. Công cụ Editor tạo mixer (phục vụ câu hỏi "tạo bằng mã được không?")

Unity không có API công khai để tạo AudioMixer. `AudioMixerBuilder` gọi lớp nội bộ `UnityEditor.Audio.AudioMixerController` bằng reflection, đúng các lời gọi mà cửa sổ Audio Mixer dùng. Tên phương thức được đọc trực tiếp từ siêu dữ liệu `UnityEditor.CoreModule.dll` của Unity 6000.6.0f1 (không đoán): `CreateMixerControllerAtPath`, `masterGroup`, `CreateNewGroup`, `AddChildToParent`, `GetGUIDForVolume`, `AddExposedParameter`, `exposedParameters`. Công cụ chạy lại nhiều lần an toàn (chỉ thêm phần còn thiếu). Nếu bản Unity khác đổi tên, công cụ báo lỗi rõ ràng; khi đó tạo tay: `Assets > Create > Audio Mixer`, thêm nhóm Sfx và Ambient dưới Master, chuột phải vào thanh Volume của từng nhóm chọn `Expose ... to script`, đổi tên tham số thành `MasterVolume`, `SfxVolume`, `AmbientVolume`.
