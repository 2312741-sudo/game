# Bài 3 (Địa hình / Terrain): tài liệu vấn đáp

Môn: Phát triển ứng dụng Game nâng cao. Đồ án: "Trạm Chanh".
Trạng thái: mã và texture đã viết, test logic thuần đã chạy ngoài Unity (xem mục 9). Công cụ Editor và các asset Terrain CHƯA được biên dịch/chạy trong Unity; phải bấm menu một lần để sinh asset.

## 1. Vì sao Terrain nằm ở hai đầu phố

- Phố 500 m dựng bằng mô hình mesh (vỉa hè, lòng đường, nhà). Các mô hình này phẳng và có ranh giới cứng nên không cần Terrain.
- Hai đầu phố (cổng `PF_WorldEdge_BoulevardGate` ở x = ±248) mở ra một đường thông thoáng cao tới 6,2 m cho tầm nhìn. Phía ngoài cổng là chỗ hợp lý cho "bãi đất ven đường / ngoại ô": địa hình lên xuống nhẹ, cỏ, vài cây.
- Terrain là công cụ đúng cho kiểu địa hình tự nhiên rộng (heightmap, splat map, cây, cỏ), còn mesh tay thì tốn công. Đặt ngoài vùng người chơi đi lại (giới hạn đi bộ x = ±245) nên không ảnh hưởng gameplay.
- Terrain bắt đầu tại x = ±256 (nơi tấm nền đường của cổng kết thúc), dài 96 m ra ngoài (tới ±352), rộng 100 m theo z (z từ -40 đến 60, phủ hết bề rộng cổng z từ -11,9 đến 28,1).
- Sương mù ban đêm của game là Exponential Squared, mật độ 0,0055: ở 256 m còn thấy khoảng 14 %, ở 300 m còn khoảng 7 %. Vì vậy mọi chi tiết xa đều được giảm.

Lựa chọn nơi đặt: **trong scene (`SCN_TramChanh_Main`), dưới root `StreetEndTerrains`**, không đặt trong prefab `PF_TramChanh_StreetExtended`. Lý do: prefab đó do `ExtendedStreetBuilder` + `seal_world_edges_and_fix_street.py` sinh lại từ đầu (Terrain trong prefab sẽ mất khi dựng lại), và các test cấu trúc phố (`WaypointGraphBuilderTests`, `BuildingFoundationGapTests`) đọc prefab đó. Scene root không động vào chúng.

## 2. Công cụ

Menu (Editor):

- `Tram Chanh > Terrain > Build Street End Terrains`
- `Tram Chanh > Terrain > Remove Street End Terrains`

Mã: `Assets/TramChanh/Scripts/Editor/TerrainBuilder.cs` (tạo asset và đối tượng scene) và `TerrainBuilderMath.cs` (hàm thuần: độ cao, splat, cây, cỏ).

Việc công cụ làm khi Build (mọi bước đều `Debug.Log`):

1. Tạo thư mục `Assets/TramChanh/Art/Terrain/`, đặt import setting cho texture (normal map, sRGB, mipmap, alpha cutout cho cỏ).
2. Tạo/cập nhật 3 Terrain Layer: `TL_Terrain_Dirt/Grass/Gravel.terrainlayer`.
3. Tạo/cập nhật `TD_StreetEnd_West.asset` và `TD_StreetEnd_East.asset` (TerrainData).
4. Xóa root cũ (nếu có) rồi tạo lại `StreetEndTerrains` với hai Terrain (`Terrain.CreateTerrainGameObject`, có Terrain Collider). Chạy hai lần vẫn chỉ có một root (idempotent).
5. Đánh dấu scene "dirty"; cần bấm Ctrl+S để lưu.

Remove: xóa root trong scene, xóa 2 TerrainData và 3 Terrain Layer, xóa thư mục `Art/Terrain` nếu rỗng. Texture nguồn giữ lại.

Trước khi bấm Build, texture phải có sẵn (đã sinh bằng `python3 ArtSource/Scripts/generate_terrain_textures.py`, nằm trong `Assets/TramChanh/Art/Textures/Terrain/`).

## 3. Từng bước Terrain Editor tương ứng với mã

| Bước trong Terrain Editor | Cách làm trong Inspector | Mã tương ứng |
|---|---|---|
| Create Neighbor Terrain | Nút "Create Neighbor Terrains" ở thanh công cụ Terrain | Hai Terrain đặt cạnh phố ở hai phía; mỗi Terrain tự dựng bằng `Terrain.CreateTerrainGameObject(data)` đặt tại x = ±256 (không nối nhau nên `allowAutoConnect = false`) |
| Raise or Lower Terrain | Cọ nâng/hạ | `TerrainBuilderMath.HeightMeters`: hàm nhiễu fBm 4 tầng (value noise, bước sóng 42 m) nhân với `HillMask`; kết quả ghi bằng `TerrainData.SetHeights` |
| Set Height | Đặt độ cao cố định | Dải 6 m sát đường được đặt đúng `FlatNormalized = 0,5` tương ứng y = 0 (Terrain đặt ở y = -8, cao 16 m) |
| Smooth Height | Làm mượt | `Smoothstep` trong `HillMask`: từ 6 m đến 28 m kể từ đường, đồi mọc dần; biên ngoài và hai bên cũng về y = 0 trong 14 m; trục đường (z quanh 8,1) giảm biên độ còn 15 % để tầm nhìn dọc phố êm |
| Stamp Terrain | Đóng dấu hình | Không dùng cọ thật; nhiễu thay cho stamp (đồi 2 đến 3 m). Khi demo có thể đóng thêm stamp bằng tay lên Terrain đã sinh |
| Paint Holes | Khoét lỗ | Không dùng. Ghi trong demo: bật `Terrain > Draw Holes`/cọ Paint Holes nếu cần hầm |
| Paint Texture | Cọ Terrain Layer | `BuildSplat` + `SplatWeights` + `TerrainData.SetAlphamaps`: dốc lớn là đất, nền đường ngay sau cổng và vùng cao là đất sỏi, phần thấp phẳng là cỏ. Ba layer: đất, cỏ, đất sỏi |
| Paint Trees | Cọ cây | `PlaceTrees` (lưới 12 m có xê dịch, xác suất 0,45, loại bỏ vùng gần cổng, trục đường và dốc trên 22 độ, tối đa 40 cây) rồi `TerrainData.SetTreeInstances`; prototype dùng prefab có sẵn `PF_StreetTree` |
| Paint Details | Cọ cỏ, hoa | `BuildDetailLayer` + `TerrainData.SetDetailLayer`; hai `DetailPrototype` dạng billboard: `T_Terrain_GrassBlades` (mật độ 0 đến 2) và `T_Terrain_WildFlowers` (hiếm, 0 hoặc 1) |
| Terrain Settings | Tab bánh răng | Hàm `PlaceTerrain` trong `TerrainBuilder.cs`, bảng ở mục 5 |
| Terrain Collider | Component tự thêm | Thêm bởi `CreateTerrainGameObject`; tắt `enableTreeColliders` |

Chi tiết quan trọng khi giải thích độ cao:

- TerrainData lưu độ cao dạng số 16 bit (tối đa 32766). Chọn `FlatNormalized = 0,5` và tổng chiều cao 16 m, gốc y = -8 thì 0,5 x 32766 = 16383 là số nguyên chính xác, nên đáy nối với đường đúng y = 0, không sai số lượng tử hóa (có test kiểm tra).
- Công thức: `h(m) = A x (noise - 0,3) x mask`, A = 5 m. Phần lớn là đồi, có chỗ trũng nhẹ (tối thiểu -1,5 m, nhưng thực tế gần 0 vì mask nhỏ ở vùng sát đường). Mô phỏng ngoài Unity: cao nhất khoảng 2,9 m (tây) và 2,3 m (đông).
- Tây dùng chỉ số x đảo chiều (mirror) để trục "ra ngoài" luôn là u tăng dần.

## 4. Texture (Terrain Layer)

Sinh bằng `ArtSource/Scripts/generate_terrain_textures.py` (numpy + PIL, nhiễu tuần hoàn nên liền mạch không thấy đường nối; normal map dùng `np.roll`):

- `T_Terrain_Dirt_Albedo/Normal.png`, `T_Terrain_Grass_*`, `T_Terrain_Gravel_*`: 512 x 512.
- `T_Terrain_GrassBlades.png`, `T_Terrain_WildFlowers.png`: 256 x 256 RGBA (nền trong suốt) cho Detail.
- Giấy phép: Project Original (procedurally generated), CC0, ghi trong `Docs/Reference/ASSET_LICENSES.md`.
- Layer: `tileSize` 8 m (đất, cỏ) và 5 m (sỏi), metallic 0, smoothness 0.

## 5. Terrain Settings và hiệu năng

| Thiết lập | Giá trị | Lý do |
|---|---|---|
| Draw Instanced | bật | Vẽ patch bằng GPU instancing, ít draw call |
| Pixel Error | 12 | Chấp nhận lưới thô hơn mặc định (5), vì sương mù dày |
| Base Map Distance | 80 m | Xa hơn dùng base map rẻ |
| Detail Distance | 40 m | Cỏ chỉ vẽ gần |
| Detail Density | 0,5 | Mật độ thấp |
| Tree Distance / Billboard | 120 m / 80 m | Cây xa không cần |
| Cast Shadows | tắt | Bỏ pass đổ bóng của Terrain |
| Reflection Probe | tắt | Tiết kiệm |
| Gió cho cỏ (TerrainData) | strength 0,25; amount 0,2; speed 0,3 | Gió nhẹ |
| Tree bend factor | 0,6 | Cây rung nhẹ |
| Tree collider | tắt | Ngoài vùng đi lại |
| Layer | Environment (8) | Cùng layer môi trường |

Con số mô hình/ảnh:

- Cây `PF_StreetTree`: 2 mesh (tán 56 mặt, thân 192 mặt trong file OBJ, tối đa khoảng 250 tam giác/cây), 2 material, không có LODGroup (không cần ở mức này); công cụ đếm tam giác khi chạy, nếu hơn 2000 thì bỏ cây và ghi cảnh báo. 18 đến 21 cây mỗi Terrain, tức nhiều nhất khoảng 5.000 tam giác.
- Heightmap 129 x 129 (ô khoảng 0,75 m), alphamap 128 x 128, detail map 128 x 128 (patch 16).
- Mật độ cỏ trong mô phỏng: khoảng 13.700 đến 13.900 "đơn vị mật độ" mỗi Terrain, hoa khoảng 630 đến 680 (chỉ vẽ trong 40 m).

Ước tính draw call (CHƯA đo trong Unity):

- Mặt đất: 1 đến 3 draw call mỗi Terrain (instanced, không đổ bóng).
- Cây: 2 material, instanced: khoảng 2 đến 4.
- Cỏ/hoa: billboard theo patch trong 40 m: khoảng 5 đến 15 (hai prototype x vài patch).
- Tổng khoảng 10 đến 20 mỗi Terrain khi nhìn thấy, tối đa khoảng 40 cho cả hai đầu cùng lúc (không thể cùng thấy cả hai đầu). So với báo cáo `street_performance_report.txt` (x = -240: 626 draw call; x = +240: 19 draw call; ngân sách đã đạt), phần thêm là nhỏ, nhưng phải đo bằng Frame Debugger/Stats trước khi khẳng định. Tại x = 0 Terrain gần như nằm ngoài tầm nhìn/sương mù nên chi phí hầu như bằng 0.

Chú ý khi demo: sau cổng ở x = ±256 có mô hình tòa nhà nền (backdrop tower) do `seal_world_edges_and_fix_street.py` đặt tại x = ±256, có thể che phần lớn Terrain khi nhìn từ trong phố. Dải thấy được rõ nhất là đường thông qua cổng (cao 6,2 m, rộng khoảng 9,8 m) và từ trên cao/góc nghiêng. Nếu cần lộ rõ hơn phải xem lại vị trí các tòa nhà đó (việc của chủ phần art, không sửa trong bài này).

## 6. Skybox đang dùng

- Scene `SCN_TramChanh_Main` dùng material `Assets/TramChanh/Art/Materials/MAT_Skybox_Night.mat` (shader Skybox/Procedural dựng sẵn của Unity).
- Thông số: Atmosphere Thickness 0,6; Exposure 0,7; Sun Size 0,02; Sky Tint (0,1; 0,14; 0,24) xanh đêm; Ground Color (0,035; 0,045; 0,07).
- Ánh sáng môi trường: Ambient Mode 1 (Trilight: màu trời, đường chân trời, mặt đất), Ambient Sky (0,19; 0,24; 0,36). Sương mù: bật, Exponential Squared, màu (0,055; 0,07; 0,1), mật độ 0,0055.
- Nên mở `Window > Rendering > Lighting > Environment` khi vấn đáp để chỉ các ô này.

## 7. Cách demo (vấn đáp)

1. Mở `SCN_TramChanh_Main`, chạy `Tram Chanh > Terrain > Build Street End Terrains`. Mở Console xem bản log từng bước.
2. Trong Hierarchy mở `StreetEndTerrains`, chọn `Terrain_StreetEnd_East`: chỉ các tab Terrain: Paint Terrain (Raise/Lower, Smooth, Set Height, Stamp, Paint Holes, Paint Texture), Paint Trees, Paint Details, Settings.
3. Trong Scene view chạy tới x gần 256: chỉ chỗ nối phẳng với nền đường (y = 0, không hở, không nổi).
4. Mở Terrain Layers: 3 layer; chỉ splat theo dốc/độ cao; thử dùng cọ Paint Texture tự vẽ thêm một mảng sỏi.
5. Dùng cọ Raise/Lower và Smooth tự nâng một ngọn đồi rồi Ctrl+Z.
6. Bật `Stats` ở Game view, so sánh draw call khi nhìn ra cổng so với nhìn vào phố; chỉnh Detail Distance và Pixel Error để thấy tác dụng.
7. Chạy `Remove Street End Terrains` để chứng minh gỡ sạch (scene và asset).

## 8. Giới hạn và rủi ro

- Chưa biên dịch/chạy trong Unity. Các API Terrain dùng là API công khai (`SetHeights`, `SetAlphamaps`, `SetTreeInstances`, `SetDetailLayer`, `terrainLayers`, `detailPrototypes`, `wavingGrass*`); nếu Unity 6 đổi tên, trình biên dịch sẽ báo ngay ở `TerrainBuilder.cs`.
- Terrain dùng material mặc định của pipeline (URP Terrain Lit); chưa xác nhận trên máy.
- Tree dùng prefab có BoxCollider và 2 material; nếu Terrain không vẽ cây đúng, kiểm tra prefab và có thể cần LODGroup.
- Chưa có ảnh chụp bằng chứng, phải do người chạy Unity chụp.

## 9. Test

`Assets/TramChanh/Tests/EditMode/Terrain/StreetEndTerrainMathTests.cs` (namespace `TramChanh.Tests.EditMode.StreetEndTerrain`, 27 test, không tạo asmdef mới; asmdef EditMode đã tham chiếu `TramChanh.Editor`): rìa nối bằng đúng y = 0 trên cả hai Terrain, dải phẳng, biên ngoài, khoảng giá trị, tính xác định, đối xứng tây/đông, hành lang, độ dốc, trọng số splat tổng bằng 1 và theo dốc/độ cao, luật đặt cây và mật độ, mật độ cỏ/hoa.

Ngoài Unity, cùng file hàm thuần và file test đã biên dịch và chạy bằng .NET + NUnit 3.14 với một lớp giả `Mathf`: 27 test đạt. Điều này chưa thay thế việc chạy trong Unity Test Runner.
