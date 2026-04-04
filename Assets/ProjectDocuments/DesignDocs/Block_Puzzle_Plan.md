# KẾ HOẠCH TÍCH HỢP BLOCK PUZZLE VÀO FARM (UC3)

## 📦 NHỮNG GÌ QUYẾT ĐÃ LÀM (SẴN CÓ):

| Component | File | Trạng thái |
|:--|:--|:--|
| **ShapeData** (SO) | `Data/ShapeData.cs` | ✅ Hoàn chỉnh — định nghĩa hình dạng block (rows, cols, cells[], blockColor) |
| **LevelData** (SO) | `Data/LevelData.cs` | ✅ Hoàn chỉnh — gridSize, availableShapes[], blocksPerBatch, puzzleObstacles[] |
| **GridObstacleData** (SO) | `Data/GridObstacleData.cs` | ✅ Hoàn chỉnh — tên, màu, durability, resource reward |
| **ObstaclePlacement** | `Data/ObstaclePlacement.cs` | ✅ Hoàn chỉnh — vị trí (row, col) + loại obstacle |
| **GridBoard** | `Grid/GridBoard.cs` | ✅ Hoàn chỉnh — lưới 10×10, đặt block, xóa hàng/cột, damage obstacles liền kề |
| **GridCell** | `Grid/GridCell.cs` | ✅ Có — visual cell (highlight, obstacle color) |
| **GridObstacleInstance** | `Grid/GridObstacleInstance.cs` | ✅ Có — HP, TakeDamage(), visual |
| **LandBlock** | `LandBlock.cs` | ✅ Hoàn chỉnh — drag & drop, BuildVisual (shape → SpriteRenderer + Color) |
| **BlockSpawner** | `Block/BlockSpawner.cs` | ✅ Hoàn chỉnh — sinh batch 3 block, random shape, spawn slots |
| **LandPuzzleManager** | `LandPuzzleManager.cs` | ✅ Hoàn chỉnh — flow Thắng/Thua, events |
| **EnergySystem** | `EnergySystem.cs` | ⚠️ Có nhưng THIẾU energy regen (chỉ có consume/add) |
| **FarmLandTile** | `Farm/FarmLandTile.cs` | ✅ Có — click handler, energy check, unlock |
| **BlockPuzzleScoring** | `BlockPuzzleScoring.cs` | ✅ Có — score tracking |

## ❌ NHỮNG GÌ CẦN LÀM THÊM:

### 1. Energy Regen (Thiếu hoàn toàn)
- `EnergySystem` chỉ có `ConsumeEnergy()` / `AddEnergy()` → chưa tự hồi theo thời gian
- Cần thêm: Timer 2 phút hồi 1 năng lượng, tối đa 5

### 2. Tích hợp vào Farm hiện tại (GridManager ↔ LandPuzzleManager)
- Khi click ô bị khóa (`LandPlot.isLocked = true`) → hiện UI xác nhận
- Nếu OK → trừ 1 energy → mở popup Puzzle
- Khi WIN → `DataManager.UpdateFarmTile(State=1)` + `LandPlot.UnlockPlot()`

### 3. Tạo ShapeData SO assets (Hình dạng Block)
- Cần tạo ~7 shapes: Dot 1×1, Line 1×3, Line 3×1, L-shape, T-shape, Square 2×2, Z-shape
- Mỗi shape có màu riêng

### 4. Auto-generate LevelData theo độ khó tăng dần
- Ô đất mở thứ 1: 1 obstacle (HP=1)
- Ô thứ 2: 2 obstacles
- Ô thứ N: N obstacles (HP tăng dần)

### 5. Thưởng vàng khi xóa hàng
- +1 vàng mỗi hàng/cột xóa → `DataManager.Instance.CurrentPlayer.Gold += 1`

### 6. Popup UI
- Popup xác nhận: "Tiêu 1 ⚡ để mở đất?" [OK] [Hủy]
- Puzzle popup: Chứa GridBoard + BlockSpawner + 3 spawn slots
- Win/Lose popup

## 🔗 NỐI DÂY (INTEGRATION FLOW):

```
GridManager.HandleInteraction()
  ↓ (click ô bị khóa)
Hiện Popup Xác Nhận (tốn 1 ⚡)
  ↓ (OK)
EnergySystem.ConsumeEnergy(1)
  ↓
LandPuzzleManager.StartPuzzle(autoLevelData, targetLandPlot)
  ↓ (chơi puzzle...)
  ↓ (phá hết obstacles)
HandleAllObstaclesDestroyed()
  ↓
LandPlot.UnlockPlot() + DataManager.UpdateFarmTile(State=1)
  ↓
Popup biến mất → Quay lại Farm
```
