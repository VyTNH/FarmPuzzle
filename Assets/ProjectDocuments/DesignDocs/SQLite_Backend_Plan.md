# Kế hoạch triển khai: Tích hợp Land Puzzle (UC3)

Kế hoạch này mô tả các bước để kết nối hệ thống **Land Puzzle** (Block Puzzle) vào scene `UC1_UC2_Sandbox`. Khi người chơi click vào ô đất đang bị khóa, game sẽ hiển thị popup xác nhận và tiêu tốn năng lượng (⚡) trước khi bắt đầu giải đố.

## Yêu cầu sếp duyệt

> [!IMPORTANT]
> Hiện tại dự án đang có 2 loại "Ô đất" khác nhau: `LandPlot` (hệ thống Nông trại) và `FarmLandTile` (hệ thống Puzzle cũ). Em sẽ thống nhất chúng bằng cách cập nhật `LandPuzzleManager` để nó điều khiển trực tiếp các ô `LandPlot`.

> [!IMPORTANT]
> Em sẽ làm thêm một Popup xác nhận "Dùng 1⚡ để mở đất?" trước khi vào chơi để tránh việc sếp lỡ tay click nhầm làm mất năng lượng.

## Các thay đổi dự kiến

### [Tích hợp Logic Giải đố]

#### [SỬA] [LandPlot.cs](file:///d:/UnityProjects/farmpuzzle/Assets/Scripts/FarmSystem/LandPlot.cs)
- Thêm biến tham chiếu đến `LevelData` (ScriptableObject) cho từng ô đất cụ thể.
- Đảm bảo hàm `UnlockPlot()` gọi đúng `DataManager` để lưu trạng thái vào SQLite.

#### [SỬA] [LandPuzzleManager.cs](file:///d:/UnityProjects/farmpuzzle/Assets/Scripts/LandPuzzle/LandPuzzleManager.cs)
- Cập nhật class này để sử dụng `LandPlot` thay vì `FarmLandTile`.
- Đảm bảo khi sếp thắng (Win), hệ thống sẽ gọi `LandPlot.UnlockPlot()`.

### [Giao diện & Tương tác]

#### [MỚI] [LandPuzzlePopupController.cs](file:///d:/UnityProjects/farmpuzzle/Assets/Scripts/LandPuzzle/UI/LandPuzzlePopupController.cs)
- Tạo bộ điều khiển UI cho Popup xác nhận "Dùng 1⚡".
- Nối nút "Đồng ý" với hệ thống trừ năng lượng và bắt đầu màn chơi.

#### [SỬA] [GridManager.cs](file:///d:/UnityProjects/farmpuzzle/Assets/Scripts/FarmSystem/GridManager.cs)
- Cập nhật hàm `HandleInteraction()` để nhận diện khi sếp click vào ô đang bị khóa (`isLocked`).
- Nếu ô bị khóa, sẽ hiện Popup xác nhận thay vì trồng cây.

### [Lưu trữ dữ liệu]

- Đảm bảo gọi `DataManager.UpdateFarmTileState()` khi ô đất được mở khóa để sếp không phải chơi lại màn đó sau khi khởi động lại game.

## Kế hoạch kiểm tra (Verification)

### Kiểm tra thủ công
1. Chạy scene `UC1_UC2_Sandbox`.
2. Click vào một ô đất tối màu (đang khóa).
3. Kiểm tra xem Popup xác nhận có hiện lên không.
4. Bấm "Đồng ý" (kiểm tra năng lượng ⚡ có bị trừ không).
5. Chơi thắng màn Puzzle và kiểm tra ô đất có sáng lên (mở khóa) không.
6. Khởi động lại scene và kiểm tra ô đất đó vẫn còn mở (đã lưu vào SQLite).
