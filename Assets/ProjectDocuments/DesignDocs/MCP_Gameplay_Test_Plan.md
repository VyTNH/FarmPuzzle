# Ke hoach Kiem thu Gameplay bang MCP cho du an Farm Puzzle (Phien ban thuc thi)

## Cong cu MCP se dung

| Tool MCP (co san) | Vai tro trong kiem thu |
|---|---|
| `execute_code` | Driver chinh - chay C# tuy y trong Unity runtime |
| `read_console` | Xac nhan output sau moi hanh dong |
| `clear_console` | Reset log truoc moi buoc test |
| `find_game_objects` | Kiem tra cay/block co xuat hien tren scene |
| `manage_components` | Doc/ghi truc tiep gia tri Energy, State |
| `manage_scriptable_object` | Kiem tra Ban do mau, CropDataSO |
| `execute_menu_item` | Play/Pause/Stop Unity |

> Khong can custom tool vi `execute_code` co the goi truc tiep moi method: `EnergySystem.AddEnergy()`, `FarmLandTile.UnlockPlot()`, v.v.

---

## Buoc 1 - Chuan bi moi truong & Ket noi MCP (HOAN THANH)

- manifest.json: khong co BOM OK
- Newtonsoft.Json: co OK
- Runtime/Editor asmdef: reference Newtonsoft OK
- Cache sach OK
- Unity mo binh thuong, console sach OK
- MCP Server active tai http://127.0.0.1:8080 OK
- Client "Antigravity" configured OK

---

## Buoc 2 - Kiem thu Farm System

### 2.1 Trong cay

Lenh MCP (execute_code):
```csharp
var gm = UnityEngine.Object.FindObjectOfType<GridManager>();
gm.PlantSeed(seedId: "tomato", gridPos: new Vector2Int(0, 0));
return "PlantRequested";
```
Xac nhan (read_console): Log PlantCreated hoac [Farm] Seed planted xuat hien
Xac nhan scene: find_game_objects tim duoc GameObject "Crop" tai vi tri (0,0)

### 2.2 Giai doan 1 -> 2 (kich hoat nhu cau cham soc)

```csharp
var tile = UnityEngine.Object.FindObjectsOfType<FarmLandTile>()[0];
tile.ForceGrowthStage(2);
return tile.CurrentStage.ToString();
```
Ket qua mong doi: Stage = 2, co the xuat hien NeedType ngau nhien (tuoi/phan/sau)

### 2.3 Tuoi nuoc / Bon phan / Bat sau

```csharp
var tile = UnityEngine.Object.FindObjectsOfType<FarmLandTile>()[0];
tile.ApplyCare(CareType.Water);
return tile.NeedResolved.ToString();
```
Ket qua mong doi: Log Watered/Fertilized/PestKilled, nhu cau bien mat

Kiem thu that bai (khong cham soc):
```csharp
var tile = UnityEngine.Object.FindObjectsOfType<FarmLandTile>()[0];
tile.ForceHarvest();
return tile.HarvestedAmount.ToString();
```
Ket qua mong doi: So luong thu hoach thap hon binh thuong (tham hut ngau nhien %)

### 2.4 Thu hoach

```csharp
var tile = UnityEngine.Object.FindObjectsOfType<FarmLandTile>()[0];
tile.Harvest();
return "Harvested";
```
Xac nhan SQLite:
```csharp
var db = DataManager.Instance.GetDatabase();
var rows = db.Query("SELECT * FROM FarmData WHERE gridPos='0,0'");
return rows[0]["state"].ToString();
```
Ket qua mong doi: state = 0 (o trong), nong san cong vao kho

---

## Buoc 3 - Kiem thu Block Puzzle (Land Puzzle)

### 3.1 Kiem tra Energy truoc khi mo Block Puzzle

```csharp
var es = UnityEngine.Object.FindObjectOfType<EnergySystem>();
return es.CurrentEnergy.ToString();
```
Neu energy = 0, them bang:
```csharp
var es = UnityEngine.Object.FindObjectOfType<EnergySystem>();
es.AddEnergy(5);
return es.CurrentEnergy.ToString();
```

### 3.2 An vao o dat bi khoa -> ton Energy -> mo Block Puzzle

```csharp
var lockedTile = UnityEngine.Object.FindObjectsOfType<FarmLandTile>()
    .FirstOrDefault(t => t.isLocked);
if (lockedTile == null) return "No locked tile found";
lockedTile.OnClick();
return "ClickTriggered";
```
Ket qua mong doi:
- Popup "Tieu 1 energy de mo dat?" xuat hien
- Energy giam 1 don vi sau khi nhan OK
- UI Block Puzzle xuat hien

### 3.3 Ngan tuong tac Farm khi Block Puzzle dang mo

```csharp
var pm = UnityEngine.Object.FindObjectOfType<LandPuzzleManager>();
bool isActive = pm != null && pm.IsPlaying;
var tile = UnityEngine.Object.FindObjectsOfType<FarmLandTile>()
    .FirstOrDefault(t => !t.isLocked);
if (tile != null) tile.ApplyCare(CareType.Water);
return "PuzzleActive:" + isActive.ToString();
```
Ket qua mong doi: Farm khong nhan input khi Puzzle dang mo (GameplayLocked trong log)

### 3.4 Keo-tha Block (kiem thu thu cong bang chuot)

Do keo-tha bi do la loi touch/mouse behavior kho simulate qua code:
- Kiem thu thu cong: keo khoi tu khay vao grid
- Dung MCP xac nhan ket qua:
```csharp
var board = UnityEngine.Object.FindObjectOfType<GridBoard>();
return board.GetGridState();
```
Loi can kiem tra:
- DragStuck: Keo block khong di chuyen, kiem tra log
- ShadowOffset: Shadow lech vi tri so voi block dang cam, do offset qua component position

### 3.5 Thang Block Puzzle -> Mo khoa o dat

```csharp
var pm = UnityEngine.Object.FindObjectOfType<LandPuzzleManager>();
pm.HandleAllObstaclesDestroyed();
```
Xac nhan:
```csharp
var tile = UnityEngine.Object.FindObjectsOfType<FarmLandTile>()
    .FirstOrDefault(t => t.wasLocked); 
return "IsLocked:" + tile.isLocked.ToString();
```
Ket qua mong doi:
- Tile isLocked = false
- SQLite FarmData.state = 1
- UI Block Puzzle dong, quay ve Farm

---

## Buoc 4 - Kiem thu Tetris & Ket noi Kho nong san

### 4.1 Kiem tra Ban do mau

```csharp
var colorMap = Resources.Load<TetrisCropMap>("TetrisCropMap");
foreach (var entry in colorMap.entries)
    Debug.Log(entry.cropId + " => " + entry.color.ToString());
return colorMap.entries.Count.ToString();
```
Ket qua mong doi: Hien thi dung mapping: SO_Crop_product_01 -> mau 1, SO_Crop_product_02 -> mau 2...

### 4.2 Spawn block voi mau dung theo kho

```csharp
var inv = DataManager.Instance.CurrentPlayer.Inventory;
var crops = inv.GetAll();
var tetris = UnityEngine.Object.FindObjectOfType<TetrisManager>();
tetris.SpawnBlockWithCrop(crops[0].cropId);
return crops[0].cropId;
```
Xac nhan: find_game_objects tim block vua spawn, kiem tra mau = mau trong Ban do mau

### 4.3 Pha hang -> Dem nong san xuat kho

```csharp
var tetris = UnityEngine.Object.FindObjectOfType<TetrisManager>();
tetris.ForceRow(new[] { "blueberry","blueberry","blueberry","tomato","tomato",
    "blueberry","tomato","blueberry","tomato","blueberry" });
tetris.ClearLine(0);
return "LineClearTriggered";
```
Xac nhan inventory:
```csharp
var inv = DataManager.Instance.CurrentPlayer.Inventory;
return "blueberry:" + inv.Get("blueberry") + ", tomato:" + inv.Get("tomato");
```
Ket qua mong doi: Doi chieu voi don hang, log InventoryUpdated xuat hien

### 4.4 UI Quest hien thi o cuoi man hinh

Kiem tra thu cong: Quest panel co hien thi o phan duoi man hinh khong
Xac nhan du lieu quest bang MCP:
```csharp
var qm = UnityEngine.Object.FindObjectOfType<QuestManager>();
var active = qm.GetActiveQuests();
foreach (var q in active)
    Debug.Log("Quest: " + q.name + " - Progress: " + q.current + "/" + q.target);
return active.Count.ToString();
```
Ket qua mong doi: Quest cap nhat dung so luong sau khi xoa hang Tetris

---

## Buoc 5 - Regression Test & Don dep

### 5.1 Chay script tong hop

Sau khi hoan thanh Buoc 2-4, chay lai script Python/JS goi toan bo execute_code theo thu tu.

### 5.2 Kiem tra SQLite toan bo

```csharp
var db = DataManager.Instance.GetDatabase();
var farmRows = db.Query("SELECT * FROM FarmData");
var orderRows = db.Query("SELECT * FROM ACTIVE_ORDER");
Debug.Log("Farm: " + farmRows.Count + " rows, Orders: " + orderRows.Count + " rows");
return "DBCheck";
```

### 5.3 Don dep
- Tat MCP Server
- Kiem tra Unity khong con ket noi mo

---

## So sanh Co MCP vs Khong co MCP

| Tac vu | Khong co MCP | Co MCP (execute_code) |
|--------|-------------|------------------------|
| Trigger hanh dong game | Choi tay, cho dung thoi diem | Goi method truc tiep, tuc thi |
| Kiem tra state | Nhin man hinh | Doc gia tri field qua code |
| Test edge case | Phai cho dieu kien tu xuat hien | Force qua ForceGrowthStage(), ForceRow() |
| Debug loi | Chay lai game nhieu lan | Inject state cu the bang execute_code |
| Regression test | Thuc hien thu cong moi lan | Script tu dong chay lai |
| Test SQLite | Dung DB Browser rieng | Query thang qua execute_code |

---

*Buoc 1 da hoan thanh. Cho xac nhan de bat dau Buoc 2.*
