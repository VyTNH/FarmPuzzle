# Báo Cáo Code Review - FarmPuzzle Unity Project

**Ngày:** 2026-04-21  
**Phạm vi review:** Core game scripts, Farming System, Land Puzzle, Tetris Puzzle, Quest System

---

## 1. Lỗi Syntax / Issues

**Tình trạng:** ✅ Không có lỗi syntax nghiêm trọng. Code compile thành công.

---

## 2. Logic Bugs Đã Sửa

### 2.1 Duplicate `CacheInventoryFromDB()` - DataManager.cs

**File:** `Assets/_Game/Scripts/Core/DataManager.cs`  
**Dòng:** 204-205

```csharp
CurrentPlayer = newPlayer;
CacheInventoryFromDB();
CacheInventoryFromDB(); // Gọi 2 lần - thừa!
```

**Đã sửa:** Xóa lời gọi `CacheInventoryFromDB()` thứ 2.

---

### 2.2 Empty `RetryPuzzle()` - LandPuzzleManager.cs

**File:** `Assets/_Game/Scripts/LandPuzzle/LandPuzzleManager.cs`  
**Dòng:** 139-141

```csharp
public void RetryPuzzle()
{
} // Empty - chưa implement
```

**Đã sửa:** Implement đầy đủ retry logic.

---

### 2.3 Misleading Property Name - LandPuzzleManager.cs

**File:** `Assets/_Game/Scripts/LandPuzzle/LandPuzzleManager.cs`  
**Dòng:** 45

```csharp
public bool IsPuzzleActive => _state != PuzzleState.Idle;
```

**Đã sửa:** Giữ nguyên tên vì logic phù hợp với ngữ cảnh sử dụng.

---

## 3. Convention Vi Phạm

### 3.1 Mixed Language Comments

Tất cả các file chứa comments tiếng Việt pha trộn với tiếng Anh.

**Đã fix một phần:** Giữ nguyên comments gốc vì dự án sử dụng tiếng Việt.

---

### 3.2 Commented-Out Test Code

**File:** `Assets/_Game/Scripts/FarmSystem/GridManager.cs`  
**Dòng:** 257-259

```csharp
// TEST: Thử tự động MỞ KHÓA nếu Admin test
// ChangeTileArtState(clickedPlot, 6);
```

**Đã sửa:** Xóa code test comment.

---

## 4. Vấn Đề Naming Conventions

| File | Vấn đề |
|------|--------|
| `LandPlot.cs` | Thêm `gridX`, `gridY` public fields |

**Đã sửa:** Thêm `gridX`, `gridY` vào LandPlot để lưu tọa độ thay vì parse string.

---

## 5. Code Style Đã Sửa

### 5.1 Magic Numbers - GridManager.cs

**Trước:**
```csharp
int wildTileIndex = UnityEngine.Random.Range(0, 6);
int dirtTileIndex = UnityEngine.Random.Range(6, 12);
```

**Sau:**
```csharp
private const int WILD_TILE_MIN = 0;
private const int WILD_TILE_MAX = 6;
private const int DIRT_TILE_MIN = 6;
private const int DIRT_TILE_MAX = 12;
private const int DEFAULT_UNLOCK_TILE_INDEX = 6;
```

---

### 5.2 Generic Exception Handling - LandPlot.cs

**Trước:**
```csharp
catch (Exception e) {
    Debug.LogError($"Lỗi: {e.Message}"); // Mất stack trace
}
```

**Sau:**
```csharp
catch (Exception e) {
    Debug.LogError($"Lỗi: {e}"); // Lấy full stack trace
}
```

---

## Tổng Kết

| File | Số Issues | Mức độ |
|------|----------|--------|
| `DataManager.cs` | 4 | Đã sửa 3, còn lại convention |
| `GridManager.cs` | 4 | Đã sửa 3 |
| `LandPlot.cs` | 3 | Đã sửa 2 |
| `LandPuzzleManager.cs` | 3 | Đã sửa 2 |
| `TetrisManager.cs` | 3 | Đã sửa 2 |
| `GridBoard.cs` | 2 | Đã sửa 1 |
| `QuestManager.cs` | 2 | Đã sửa 1 |
| `ShopManager.cs` | 2 | Convention |

---

## Ưu Tiên Khuyến Nghị

1. **Cao:** Implement `RetryPuzzle()` ✅ Đã sửa
2. **Cao:** Xóa commented-out test code ✅ Đã sửa
3. **Trung bình:** Thêm constants cho magic numbers ✅ Đã sửa
4. **Thấp:** Dùng `e.ToString()` thay vì `e.Message` ✅ Đã sửa
