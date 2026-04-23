# Báo Cáo Clean Code Review - FarmPuzzle Unity Project

**Ngày:** 2026-04-21  
**Focus Areas:** SOLID principles, DRY principle, anti-patterns

---

## SOLID Principle Violations

---

### 1. Single Responsibility Principle (SRP)

#### DataManager.cs - God Class

**Violation:** Handles 6+ distinct responsibilities.

**Đã cải thiện:**
- Extract `GetSessionQty()` và `SetSessionQty()` helpers
- Fix N+1 query pattern
- Remove duplicate `CacheInventoryFromDB()` call

**Đề xuất:** Extract vào:
- `PlayerSessionManager` - Login, logout, current player
- `InventoryService` - AddItem, RemoveItem, session inventory
- `FarmTileRepository` - CRUD cho tiles
- `CatalogInitializer` - InitStaticData logic
- `DataMigrationService` - MigratePlayerData

---

#### LandPlot.cs - Domain Object Doing Too Much

**Violation:** Handles crop growth state, visual rendering, care tool application, harvest logic, AND database operations.

**Đã cải thiện:** Dùng `CareToolMap` dictionary thay vì switch statement.

**Đề xuất:** Sử dụng event-driven architecture.

---

### 2. Open/Closed Principle (OCP)

#### GridManager.cs - Hardcoded Difficulty Calculation

**Lines:** 175-181

```csharp
int distance = Mathf.Max(Mathf.Abs(px - centerX), Mathf.Abs(py - centerY));
int levelIndex = Mathf.Clamp(distance, 0, availablePuzzleLevels.Length - 1);
```

**Đề xuất:** Sử dụng strategy pattern:
```csharp
public interface IDifficultyCalculator {
    int CalculateLevel(int x, int y, int centerX, int centerY, int[] availableLevels);
}
```

---

### 3. Dependency Inversion Principle (DIP)

#### LandPlot.cs - Direct Singleton Access

**Violation:**
```csharp
DataManager.Instance.AddItem(...);
FarmPuzzle.Meta.QuestManager.Instance.UpdateProgress(...);
```

**Đề xuất:** Sử dụng events:
```csharp
public static event Action<string, int> OnItemHarvested;
public static event Action<string, int> OnQuestProgressUpdated;
```

---

### 4. Interface Segregation

**Đề xuất:** Định nghĩa focused interfaces:
```csharp
public interface IInventory {
    void AddItem(string itemID, int amount);
    bool RemoveItem(string itemID, int amount);
    int GetItemAmount(string itemID);
}
```

---

## DRY Principle Violations

---

### 1. Quest Progress Check Pattern - QuestManager.cs ✅

**Trước:** Cùng condition lặp lại 3 lần:
```csharp
if (questProgress.TryGetValue(q.questID, out int p) && p >= q.targetAmount)
```

**Đã sửa:** ✅ Extract thành helper:
```csharp
private bool IsQuestCompleted(QuestDataSO q) =>
    q != null && questProgress.TryGetValue(q.questID, out int p) && p >= q.targetAmount;
```

---

### 2. Duplicate Coordinate Calculation - GridBoard.cs ✅

**Trước:** Cùng formula ở 3 chỗ:
- `CreateCells()` (line 84)
- `WorldToGridPosition()` (line 347)
- `GridToWorldPosition()` (line 361)

**Đã sửa:** ✅ Extract thành `GetGridOriginOffset()` helper.

---

### 3. Duplicate RectTransform Setup - TetrisManager.cs

**Lines:** 68-85 và 87-97

**Đề xuất:**
```csharp
private void SetupCellRect(RectTransform rt, float blockSize, Vector2 pos, bool active = true)
{
    rt.sizeDelta = new Vector2(blockSize - 2, blockSize - 2);
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.zero;
    rt.pivot = Vector2.zero;
    rt.anchoredPosition = pos;
    rt.GetComponent<Image>()?.gameObject.SetActive(active);
}
```

---

## Anti-Patterns

---

### 1. Singleton Abuse

**Found in:** Tất cả 5 key files

**Đã cải thiện một phần:** Extract helper methods giảm direct singleton calls.

**Đề xuất:** Sử dụng dependency injection hoặc service locator pattern.

---

### 2. Direct Database Access from Domain Objects

**File:** `LandPlot.cs`

```csharp
DataManager.Instance.AddItem(...);
DataManager.Instance.CommitSessionInventory();
```

**Đề xuất:** Raise events cho listeners xử lý persistence.

---

### 3. Event Subscription Sprawl

**File:** `LandPuzzleManager.cs`

**Đề xuất:** Sử dụng `IDisposable` pattern hoặc event aggregator.

---

### 4. Magic Numbers

**Đã sửa:** ✅ Thêm constants:
- `GRID_W = 50`, `GRID_H = 50` (DataManager)
- `WILD_TILE_MIN = 0`, `WILD_TILE_MAX = 6` (GridManager)
- `DIRT_TILE_MIN = 6`, `DIRT_TILE_MAX = 12` (GridManager)
- `DEFAULT_STARTER_MONEY = 500` (DataManager)
- `EMPTY_CELL_COLOR` (TetrisManager)

---

## Tổng Kết

| Pattern | Location | Status |
|---------|----------|--------|
| Singleton abuse | All 5 files | ⚠️ Cần refactor lớn |
| Direct DB access | LandPlot.cs | ⚠️ Cần event-driven |
| Hardcoded logic | GridManager.cs | ⚠️ Cần strategy pattern |
| Duplicate condition | QuestManager.cs | ✅ Đã fix |
| Duplicate coord calculation | GridBoard.cs | ✅ Đã fix |
| Magic numbers | Multiple files | ✅ Đã fix |

---

## Các Cải Tiến Đã Thực Hiện

### SOLID
1. ✅ Extract GetSessionQty/SetSessionQty trong DataManager
2. ✅ Fix N+1 query trong MigratePlayerData
3. ✅ Replace switch bằng Dictionary lookup trong LandPlot

### DRY
4. ✅ Extract IsQuestCompleted() helper trong QuestManager
5. ✅ Extract GetGridOriginOffset() trong GridBoard
6. ✅ Thêm EMPTY_CELL_COLOR constant trong TetrisManager
7. ✅ Extract IsRowFull/CollectAndClearRow/PullRowsDown trong TetrisManager
8. ✅ Làm WhiteSprite static shared trong LandBlock

### Anti-Patterns
9. ✅ Thêm constants cho magic numbers
10. ✅ Remove duplicate CacheInventoryFromDB call
11. ✅ Implement RetryPuzzle()
