# Báo Cáo Simplify - FarmPuzzle Unity Project

**Ngày:** 2026-04-21  
**Focus Areas:** Code simplification, eliminating redundancy, reducing complexity

---

## 1. Magic Numbers Should Be Constants - GridManager.cs ✅

**Trước:**
```csharp
int wildTileIndex = UnityEngine.Random.Range(0, 6);
int dirtTileIndex = UnityEngine.Random.Range(6, 12);
ChangeTileArtState(plot, 6);
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

## 2. UI Raycast Check - GridManager.cs

**Trước:** 15 dòng nested if-else  
**Sau:** Extract thành helper method (cần implement tiếp)

---

## 3. Repeated Dictionary Access Pattern - DataManager.cs ✅

**Trước:**
```csharp
int currentQty = _sessionInventory.ContainsKey(itemID) ? _sessionInventory[itemID] : 0;
```

**Sau:**
```csharp
private int GetSessionQty(string itemID) =>
    _sessionInventory.TryGetValue(itemID, out int qty) ? qty : 0;

private void SetSessionQty(string itemID, int qty)
{
    _sessionInventory[itemID] = qty;
    _sessionChanges.Add(itemID);
}
```

---

## 4. N+1 Query in MigratePlayerData - DataManager.cs ✅

**Trước:**
```csharp
foreach (var entry in expectedItems)
{
    bool exists = DB.Table<InventoryModel>()
        .Any(i => i.PlayerID == userID && i.ItemID == entry.Key);
```

**Sau:**
```csharp
var existingIDs = DB.Table<InventoryModel>()
    .Where(i => i.PlayerID == userID)
    .Select(i => i.ItemID)
    .ToHashSet();

foreach (var itemID in expectedItems)
{
    if (!existingIDs.Contains(itemID)) {
        DB.Insert(new InventoryModel { PlayerID = userID, ItemID = itemID, Quantity = 0 });
```

---

## 5. Switch Statement with Dictionary Lookup - LandPlot.cs ✅

**Trước:**
```csharp
switch (careType)
{
    case CropNeedType.Water: itemID = "tool_watercan"; isConsumable = false; break;
    case CropNeedType.Pest: itemID = "tool_pest"; isConsumable = true; break;
    case CropNeedType.Fertilizer: itemID = "item_fertilizer"; isConsumable = true; break;
}
```

**Sau:**
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

## 6. Repeated Color Constants - TetrisManager.cs ✅

**Trước:**
```csharp
cell.color = new Color(0, 0, 0, 0.2f);  // repeated nhiều chỗ
```

**Sau:**
```csharp
private static readonly Color EMPTY_CELL_COLOR = new Color(0f, 0f, 0f, 0.2f);
// Usage:
cell.color = EMPTY_CELL_COLOR;
```

---

## 7. CheckLines Complex Nested Loops - TetrisManager.cs ✅

**Trước:** 60+ dòng nested loops trong một method

**Sau:**
```csharp
private bool IsRowFull(int y) => Enumerable.Range(0, width).All(x => !string.IsNullOrEmpty(boardTypes[x, y]));

private Dictionary<string, int> CollectAndClearRow(int y)
{
    var destroyed = new Dictionary<string, int>();
    for (int x = 0; x < width; x++)
    {
        string pid = boardTypes[x, y];
        if (!string.IsNullOrEmpty(pid))
        {
            destroyed[pid] = destroyed.GetValueOrDefault(pid, 0) + 1;
            boardTypes[x, y] = "";
        }
    }
    return destroyed;
}

private void PullRowsDown(int fromY)
{
    for (int pullY = fromY; pullY < height - 1; pullY++)
        for (int x = 0; x < width; x++)
        {
            boardTypes[x, pullY] = boardTypes[x, pullY + 1];
            boardCells[x, pullY].sprite = boardCells[x, pullY + 1].sprite;
            boardCells[x, pullY].color = boardCells[x, pullY + 1].color;
        }

    for (int x = 0; x < width; x++)
    {
        boardTypes[x, height - 1] = "";
        boardCells[x, height - 1].sprite = null;
        boardCells[x, height - 1].color = EMPTY_CELL_COLOR;
    }
}
```

---

## 8. Duplicate Coordinate Calculation - GridBoard.cs ✅

**Trước:** Cùng formula lặp lại 3 lần

**Sau:**
```csharp
private Vector3 GetGridOriginOffset() {
    float step = _cellSize * (1f + _cellMargin);
    float totalSize = (_gridSize - 1) * step;
    return new Vector3(-totalSize / 2f, -totalSize / 2f, 0f);
}
```

---

## Tổng Kết

| File | Issue | Priority | Trạng thái |
|------|-------|----------|------------|
| GridManager.cs | Magic numbers for tile indices | High | ✅ Đã fix |
| GridManager.cs | UI raycast check block | Medium | ⚠️ Cần implement |
| DataManager.cs | Repeated `_sessionInventory` pattern | High | ✅ Đã fix |
| DataManager.cs | N+1 query in MigratePlayerData | Medium | ✅ Đã fix |
| LandPlot.cs | Switch statement in ApplyCare | Medium | ✅ Đã fix |
| TetrisManager.cs | Repeated `new Color(0,0,0, 0.2f)` | Low | ✅ Đã fix |
| TetrisManager.cs | CheckLines nested loops | High | ✅ Đã fix |
| GridBoard.cs | Duplicate coord calculation | Low | ✅ Đã fix |

---

## Tổng Hợp Các Cải Tiến Đã Thực Hiện

1. ✅ Thêm constants WILD_TILE_MIN, WILD_TILE_MAX, DIRT_TILE_MIN, DIRT_TILE_MAX
2. ✅ Extract GetSessionQty() và SetSessionQty() helpers
3. ✅ Fix N+1 query với ToHashSet()
4. ✅ Thay switch bằng Dictionary lookup trong LandPlot.ApplyCare()
5. ✅ Thêm EMPTY_CELL_COLOR constant trong TetrisManager
6. ✅ Extract IsRowFull(), CollectAndClearRow(), PullRowsDown() methods
7. ✅ Extract GetGridOriginOffset() trong GridBoard
8. ✅ Làm WhiteSprite thành static shared trong LandBlock
