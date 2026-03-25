# LandPuzzle (Block Puzzle) - Gameplay Flow

Tài liệu mô tả luồng chơi cốt lõi (Core Logic), không tính các lớp UI/VFX.

## 1. Các thành phần chính
- **Farm Grid**: Chứa các ô đất có 2 trạng thái: Khóa (🟫) và Mở (🟩).
- **Puzzle Grid**: Bàn cờ 10x10.
- **Obstacles**: Chướng ngại vật (Đá, Gỗ, Bụi) nằm cố định trên bàn cờ, có Máu (HP) riêng.
- **Energy System**: Năng lượng để mở các màn chơi.
- **Blocks**: Gói 3 khối hình (như Tetris) cho ngẫu nhiên mỗi lượt.

## 2. Luồng hoạt động

### 🎮 Bắt đầu màn chơi
1. **Click ô đất bị Khóa (🟫)** trên Farm Grid.
2. Hệ thống trừ **1 Năng lượng (Energy)**. Nếu đủ $\rightarrow$ Mở bàn Puzzle Grid 10x10.

### 🧩 Cơ chế chơi (Core Loop)
- **Kéo - Thả**: Kéo 1 trong 3 khối (Blocks) đặt vào vị trí trống trên bàn cờ.
- **Ăn Điểm (Line Clear)**: Xếp kín 1 hàng ngang hoặc dọc (10 ô) $\rightarrow$ Hàng đó biến mất.
- **Reload khối**: Khi đặt hết 3 khối, hệ thống sẽ tự sinh ra 3 khối mới (Spawn).

### ⚔️ Chiến đấu & Tài nguyên (Adjacent Damage)
Mục tiêu là **phá hủy Chướng ngại vật (Obstacles)**, không phải sinh tồn tính điểm.
- **Gây sát thương**: Mỗi khi dọn 1 hàng liền kề (trên, dưới, trái, phải) với Obstacle $\rightarrow$ Obstacle mất **1 HP** (biến đổi màu sắc).
- **Phần thưởng**: Khi Obstacle bị phá hủy (HP = 0) $\rightarrow$ Rơi ra tài nguyên tương ứng (VD: Đá rớt Stone, Gỗ rớt Wood).

## 3. Điều kiện kết thúc (End Game)
Trạng thái màn chơi quyết định kết quả của ô đất vừa click.

| Kết quả | Điều kiện | Hệ quả |
|---|---|---|
| **🏆 THẮNG (WIN)** | Phá hủy **TOÀN BỘ 100% Obstacles** trên bàn. | Màn chơi khép lại. Ô đất chuyển sang **Mở Khóa (🟩)** vĩnh viễn. |
| **💀 THUA (GAME OVER)** | Còn Obstacles trên bàn, nhưng **KHÔNG CÒN VỊ TRÍ HỢP LỆ** để đặt bất kỳ khối bào trong 3 khối đang có. | Bàn chơi đóng lại. Ô đất giữ nguyên trạng thái **Khóa (🟫)**. Mất 1 Energy nếu muốn chơi lại. |

## 4. Cấu trúc Code chính hiện tại
- `LandPuzzleManager`: Nơi điều phối luồng trò chơi (khởi chạy, xét Thắng/Thua).
- `EnergySystem`: File Singleton trừ Năng lượng khi click ô.
- `FarmLandTile`: Đoạn script gắn lên ô đất để bắt tương tác click.
- `GridBoard` / `LandBlock`: Bộ não xử lý logic check hàng ngang dọc, fix lỗi đặt khối và sát thương lên Obstacles.
