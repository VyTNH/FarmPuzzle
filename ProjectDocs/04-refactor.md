# Báo Cáo Refactor - FarmPuzzle Unity Project

**Ngày:** 2026-04-21  
**Focus Areas:** Code structure, separation of concerns, architectural patterns

---

## 1. DataManager.cs - God Class with Too Many Responsibilities

**Severity:** High | **Effort:** Large (extract 4-5 classes)

**Problem:** `DataManager` handles 6+ distinct responsibilities:
- SQLite database management
- Player authentication/login
- Session inventory (RAM cache + DB sync)
- Static data seeding
- Farm tile CRUD
- Data migration

**Đã cải thiện:**
- Extract `GetSessionQty()` và `SetSessionQty()` helpers
- Fix N+1 query trong `MigratePlayerData()`
- Thêm constants cho magic numbers

**Đề xuất refactor tiếp:**
```csharp
// Break DataManager into focused classes:
public class PlayerSessionManager { }     // Login, logout, current player
public class InventoryService { }         // AddItem, RemoveItem, session inventory
public class FarmTileRepository { }       // CRUD operations for tiles
public class CatalogInitializer { }        // InitStaticData logic
public class DataMigrationService { }     // MigratePlayerData
```

---

## 2. GridManager.cs - Deeply Nested HandleInteractionAtPos

**Severity:** Medium | **Effort:** Medium

**File:** `Assets/_Game/Scripts/FarmSystem/GridManager.cs`  
**Dòng:** 228-280

**Đã cải thiện:** Thêm constants cho tile indices, xóa commented code.

**Đề xuất refactor tiếp:**
```csharp
private bool IsPointerOverUIElement(Vector2 screenPos) { ... }
private void HandleCareOrHarvest(LandPlot plot) { ... }
```

---

## 3. GridManager.cs - LoadGridState Too Long

**File:** `Assets/_Game/Scripts/FarmSystem/GridManager.cs`  
**Dòng:** 136-201 (65 lines)

**Đã cải thiện:** Dùng `plot.gridX`/`plot.gridY` thay vì parse string.

**Đề xuất split:**
```csharp
private void LoadTileVisuals() { ... }      // Random tile sprites
private void ApplyPuzzleLevels() { ... }    // Distance-based puzzle assignment
private void SyncTileState(FarmTileModel dbTile, LandPlot plot) { ... }
```

---

## 4. LandPlot.cs - Deep Nesting in ApplyCare

**File:** `Assets/_Game/Scripts/FarmSystem/LandPlot.cs`  
**Dòng:** 175-206

**Đã sửa:** ✅ Dùng Dictionary lookup:

```csharp
private static readonly Dictionary<CropNeedType, (string itemID, bool consumable)> CareToolMap =
    new()
    {
        { CropNeedType.Water,      ("tool_watercan",    false) },
        { CropNeedType.Pest,        ("tool_pest",        true) },
        { CropNeedType.Fertilizer,  ("item_fertilizer",  true) }
    };
```

---

## 5. LandPuzzleManager.cs - Procedural UI in Code

**File:** `Assets/_Game/Scripts/LandPuzzle/LandPuzzleManager.cs`  
**Dòng:** 61-96 (60 lines)

**Đã cải thiện:** Implement `RetryPuzzle()`.

**Đề xuất refactor:**
```csharp
[SerializeField] private Button exitButtonPrefab;
private void BuildExitButton() {
    var btn = Instantiate(exitButtonPrefab, _puzzlePanel.transform);
    btn.onClick.AddListener(ExitPuzzle);
}
```

---

## 6. GridBoard.cs - Duplicate Coordinate Calculation

**File:** `Assets/_Game/Scripts/LandPuzzle/Grid/GridBoard.cs`

**Đã sửa:** ✅ Extract `GetGridOriginOffset()`:

```csharp
private Vector3 GetGridOriginOffset() {
    float step = _cellSize * (1f + _cellMargin);
    float totalSize = (_gridSize - 1) * step;
    return new Vector3(-totalSize / 2f, -totalSize / 2f, 0f);
}
```

---

## 7. Event Subscription Pattern - LandPuzzleManager.cs

**File:** `Assets/_Game/Scripts/LandPuzzle/LandPuzzleManager.cs`  
**Dòng:** 244-257

**Đề xuất:** Sử dụng `IDisposable` pattern hoặc event aggregator.

---

## 8. Tight Coupling - Direct Singleton Access

**Problem:** Xuyên suốt codebase:

```csharp
DataManager.Instance.AddGold(amount);
FarmPuzzle.Meta.QuestManager.Instance.UpdateProgress(...);
```

**Đề xuất:** Sử dụng dependency injection hoặc event-based communication.

---

## Tổng Kết

| Issue | File | Severity | Trạng thái |
|-------|------|----------|------------|
| God Class | `DataManager.cs` | High | ⚠️ Cần refactor lớn |
| Deep nesting | `GridManager.cs` | Medium | ⚠️ Cần refactor tiếp |
| Deep nesting | `LandPlot.cs` | Low | ✅ Đã fix |
| Procedural UI | `LandPuzzleManager.cs` | Medium | ⚠️ Cần prefab |
| Duplicate coord | `GridBoard.cs` | Low | ✅ Đã fix |
| Singleton abuse | Multiple | Medium | ⚠️ Cần DI |

---

## Đã Thực Hiện Trong Session Này

1. ✅ `DataManager.cs` - Add GetSessionQty/SetSessionQty helpers
2. ✅ `DataManager.cs` - Fix N+1 query in MigratePlayerData
3. ✅ `DataManager.cs` - Remove duplicate CacheInventoryFromDB call
4. ✅ `DataManager.cs` - Add constants for GRID_W, GRID_H, DEFAULT_STARTER_MONEY
5. ✅ `GridManager.cs` - Add tile index constants
6. ✅ `GridManager.cs` - Use gridX/gridY instead of string parsing
7. ✅ `LandPlot.cs` - Replace switch with CareToolMap dictionary
8. ✅ `TetrisManager.cs` - Add EMPTY_CELL_COLOR constant
9. ✅ `TetrisManager.cs` - Extract IsRowFull, CollectAndClearRow, PullRowsDown
10. ✅ `LandPuzzleManager.cs` - Implement RetryPuzzle()
11. ✅ `GridBoard.cs` - Extract GetGridOriginOffset() helper
12. ✅ `LandBlock.cs` - Make WhiteSprite static shared
13. ✅ `FarmViewportCuller.cs` - Use gridX/gridY instead of string parsing
14. ✅ `QuestManager.cs` - Add IsQuestCompleted() helper method
