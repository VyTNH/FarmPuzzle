using UnityEngine;
using SQLite;
using System;
using System.IO;
using FarmPuzzle.Core.Database;
using System.Linq;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    public SQLiteConnection DB { get; private set; }

    // ─── GRID CONFIG — Single source of truth cho kích thước farm ───
    private const int GRID_W = 50;
    private const int GRID_H = 50;
    public static int GridWidth  => Instance != null ? GRID_W : 50;
    public static int GridHeight => Instance != null ? GRID_H : 50;

    // ─── EVENTS: Bắn khi kho đồ thay đổi (để UI lắng nghe, không cần polling) ───
    /// <summary>Fire khi số lượng item đã có thay đổi. (itemID, newQuantity)</summary>
    public static event Action<string, int> OnInventoryChanged;
    /// <summary>Fire khi item hoàn toàn mới xuất hiện trong kho (lần đầu). (itemID, initialQty)</summary>
    public static event Action<string, int> OnInventoryItemAdded;
    /// <summary>Fire sau khi player login xong — UI có thể bắt đầu build toàn bộ inventory.</summary>
    public static event Action OnPlayerLoggedIn;

    // Thông tin người chơi hiện tại đang kết nối (Giống PlayerSession)
    public PlayerModel CurrentPlayer { get; private set; }

    // Mới: Kiểm tra xem DB có đang mở và sẵn sàng làm việc không
    public bool IsReady => (DB != null && !_isClosing);
    private bool _isClosing = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoStart()
    {
        var go = new GameObject("[DataManager_Auto]");
        go.AddComponent<DataManager>();
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        string dbPath = Path.Combine(Application.persistentDataPath, "FarmPuzzleDB.db");
        DB = new SQLiteConnection(dbPath);

        // ==== ĐẢM BẢO ERD Tables CÓ MẶT ====
        DB.CreateTable<PlayerModel>();
        DB.CreateTable<InventoryModel>();
        DB.CreateTable<DecorRecordModel>();
        DB.CreateTable<PlayerQuestModel>();
        DB.CreateTable<FarmTileModel>();
        DB.CreateTable<ActiveOrderModel>();
        
        DB.CreateTable<SeedItemModel>();
        DB.CreateTable<CropDataModel>();
        DB.CreateTable<DecorItemModel>();
        DB.CreateTable<ObstacleModel>();
        DB.CreateTable<ProductItemModel>();
        DB.CreateTable<ToolItemModel>();

        InitStaticData(); // Khởi tạo Cửa hàng Hạt giống nếu chưa có
    }

    // ==== HỆ THỐNG ĐĂNG NHẬP / TẠO TÀI KHOẢN (SINGLE-PLAYER) ====
    public bool LoginPlayer(string userID, string userName)
    {
        Debug.Log($"[LOG-LOGIN] Bắt đầu quy trình kiểm tra Database cho ID: {userID}...");
        
        // 1. Dùng Database quét xem ID này đã đăng ký Game chưa
        var player = DB.Table<PlayerModel>().Where(p => p.PlayerID == userID).FirstOrDefault();
        
        if (player != null)
        {
            // TÌM THẤY TÀI KHOẢN -> LOAD XONG
            CurrentPlayer = player;
            CacheInventoryFromDB();
            Debug.Log($"<color=green>[LOG-LOGIN] TÀI KHOẢN TỒN TẠI!</color> Chào mừng trở lại {CurrentPlayer.Name} (ID: {userID})");

            // Kiểm tra FARM_TILE có đúng format (x_y) không, nếu lỗi → tái tạo
            ValidateAndRepairFarmTiles(userID);

            // Patch item slot mới (nếu game vừa thêm item mới kể từ lần chơi trước)
            MigratePlayerData(userID);

            // Nạp dữ liệu kho đồ (Inventory) từ Database vào Session RAM!
            CacheInventoryFromDB();

            OnPlayerLoggedIn?.Invoke();
            return true;
        }
        else
        {
            Debug.Log($"<color=yellow>[LOG-LOGIN] ID CHƯA TỒN TẠI!</color> Hệ thống sẽ tiến hành xóa dữ liệu Single-player cũ và tạo tài khoản mới cho: {userName}");
            // SINGLE-PLAYER: Xóa sạch dữ liệu cũ trước khi tạo mới
            WipeAllPlayerData();
            return CreateNewPlayer(userID, userName);
        }
    }

    // Xóa toàn bộ dữ liệu người chơi cũ (Game single-player chỉ giữ 1 tài khoản)
    private void WipeAllPlayerData()
    {
        DB.DeleteAll<PlayerModel>();
        DB.DeleteAll<InventoryModel>();
        DB.DeleteAll<FarmTileModel>();
        DB.DeleteAll<PlayerQuestModel>();
        DB.DeleteAll<DecorRecordModel>();
        DB.DeleteAll<ActiveOrderModel>();
        Debug.Log("[DataManager] Đã xóa sạch dữ liệu tài khoản cũ (Single-player reset).");
    }

    // Kiểm tra FARM_TILE đúng format tile_playerID_x_y. Nếu lỗi -> tái tạo.
    private void ValidateAndRepairFarmTiles(string userID)
    {
        var existingTiles = DB.Table<FarmTileModel>().Where(t => t.PlayerID == userID).ToList();

        int expectedCount = GRID_W * GRID_H;
        bool needRepair = false;
        if (existingTiles.Count < expectedCount)
        {
            needRepair = true;
            Debug.LogWarning($"[DataManager] FARM_TILE chỉ có {existingTiles.Count} ô (cần ít nhất {expectedCount}). Sẽ tái tạo.");
        }
        else
        {
            string expectedSample = $"tile_{userID}_0_0";
            if (!existingTiles.Exists(t => t.TileID == expectedSample))
            {
                needRepair = true;
                Debug.LogWarning("[DataManager] FARM_TILE sai format (thiếu tọa độ x_y). Sẽ tái tạo.");
            }
        }

        if (needRepair)
        {
            foreach (var old in existingTiles) DB.Delete(old);
            CreateFarmTiles(userID);    // dùng hàm chung
            Debug.Log($"[LOG-REPAIR] Đã tái tạo {GRID_W * GRID_H} ô FARM_TILE đúng format {GRID_W}x{GRID_H}!");
        }
    }

    /// <summary>Tạo các FARM_TILE mới cho player theo cấu hình GRID_W x GRID_H.</summary>
    private void CreateFarmTiles(string userID)
    {
        for (int x = 0; x < GRID_W; x++)
        {
            for (int y = 0; y < GRID_H; y++)
            {
                bool isEdge = (x == 0 || x == GRID_W - 1 || y == 0 || y == GRID_H - 1);
                DB.Insert(new FarmTileModel {
                    TileID         = $"tile_{userID}_{x}_{y}",
                    PlayerID       = userID,
                    State          = isEdge ? 0 : 1,
                    PlantedSeedID  = "",
                    PlantTimeTicks = 0,
                    HasObstacle    = isEdge,
                    ObstacleID     = isEdge ? "rock_default" : ""
                });
            }
        }
    }

    private bool CreateNewPlayer(string userID, string userName)
    {
        Debug.Log($"[LOG-INIT] Bắt đầu khởi tạo dữ liệu ERD cho người chơi: {userName}...");
        
        // 1. Tạo Tài khoản Người chơi
        var newPlayer = new PlayerModel {
            PlayerID = userID,
            Name = userName,
            EXP = 0,
            Money = 500 // Tiền khởi nghiệp
        };
        DB.Insert(newPlayer);
        Debug.Log($"- Đã tạo dòng mới trong bảng PLAYER: {userName} | 💰 khởi tạo: 500G");

        // 2. Tặng Vũ Khí & Hạt giống đầu tay (Bảng Inventory)
        DB.Insert(new InventoryModel { PlayerID = userID, ItemID = "seed_01",        Quantity = 5  });  // Khoai Tây
        DB.Insert(new InventoryModel { PlayerID = userID, ItemID = "seed_02",        Quantity = 5  });  // Cà rốt
        DB.Insert(new InventoryModel { PlayerID = userID, ItemID = "tool_hoe",       Quantity = 1  });
        DB.Insert(new InventoryModel { PlayerID = userID, ItemID = "tool_watercan",  Quantity = 1  });
        DB.Insert(new InventoryModel { PlayerID = userID, ItemID = "tool_pest",      Quantity = 3  });
        DB.Insert(new InventoryModel { PlayerID = userID, ItemID = "item_fertilizer",Quantity = 5  });
        Debug.Log("- Đã thêm Starter Kit: seed_01 x5, seed_02 x5, tool_hoe, tool_watercan, tool_pest x3, item_fertilizer x5.");

        // 3. Phân chia lô đất mặc định (lấy từ GRID_W x GRID_H)
        CreateFarmTiles(userID);
        Debug.Log($"- Đã phân phối {GRID_W * GRID_H} ô đất FARM_TILE ({GRID_W}x{GRID_H}).");

        // Đổ data vào RAM để dùng trong quá trình Game Loop chạy
        CurrentPlayer = newPlayer;
        CacheInventoryFromDB();
        CacheInventoryFromDB();

        Debug.Log("- Da phan phoi day du Starter Kit vao SQLite.");
        OnPlayerLoggedIn?.Invoke();
        return true;
    }

    // ==== SESSION INVENTORY (RAM) ====
    private System.Collections.Generic.Dictionary<string, int> _sessionInventory = new System.Collections.Generic.Dictionary<string, int>();
    private System.Collections.Generic.HashSet<string> _sessionChanges = new System.Collections.Generic.HashSet<string>();

    public System.Collections.Generic.Dictionary<string, int> GetSessionInventory()
    {
        return _sessionInventory;
    }

    private void CacheInventoryFromDB()
    {
        _sessionInventory.Clear();
        _sessionChanges.Clear();
        if (CurrentPlayer == null) return;
        var items = DB.Table<InventoryModel>().Where(i => i.PlayerID == CurrentPlayer.PlayerID).ToList();
        foreach (var item in items) _sessionInventory[item.ItemID] = item.Quantity;
        
        Debug.Log($"<color=cyan>[STAGE 1: Khởi động (Disk -> RAM)]</color> Đã nạp thành công {items.Count} loại mặt hàng từ File SQLite lên Bộ nhớ Tạm (RAM) cho UI đọc.");
    }

    public void CommitSessionInventory()
    {
        if (CurrentPlayer == null || _sessionChanges.Count == 0) return;
        DB.RunInTransaction(() =>
        {
            foreach (var itemID in _sessionChanges)
            {
                int newQty = _sessionInventory.ContainsKey(itemID) ? _sessionInventory[itemID] : 0;
                var inv = DB.Table<InventoryModel>().FirstOrDefault(i => i.PlayerID == CurrentPlayer.PlayerID && i.ItemID == itemID);
                if (inv != null)
                {
                    inv.Quantity = newQty;
                    DB.Update(inv);
                }
                else if (newQty > 0)
                {
                    DB.Insert(new InventoryModel { PlayerID = CurrentPlayer.PlayerID, ItemID = itemID, Quantity = newQty });
                }
            }
        });
        Debug.Log($"<color=magenta>[STAGE 4: Kết thúc (RAM -> Disk)]</color> Đã mở ổ cứng (SQLite)! Tiến hành GHI ĐÈ vĩnh viễn {_sessionChanges.Count} biến động từ RAM xuống File DB an toàn.");
        _sessionChanges.Clear();
    }

    // ==== CÁC HÀM GIAO TIẾP VỚI CÁC MODULE KHÁC ====
    public void AddGold(int amount)
    {
        if (CurrentPlayer == null) return;
        CurrentPlayer.Money += amount;
        DB.Update(CurrentPlayer);
        Debug.Log($"[DataManager] Tiền Update: {CurrentPlayer.Money} vàng (+{amount})");
    }

    public int GetGold()
    {
        return CurrentPlayer != null ? CurrentPlayer.Money : 0;
    }

    public void AddItem(string itemID, int amount)
    {
        if (CurrentPlayer == null || amount <= 0) return;
        
        int currentQty = _sessionInventory.ContainsKey(itemID) ? _sessionInventory[itemID] : 0;
        int newQty = currentQty + amount;
        _sessionInventory[itemID] = newQty;
        _sessionChanges.Add(itemID);

        Debug.Log($"<color=orange>[STAGE 3: Gameplay -> RAM]</color> Nhặt được {amount}x {itemID}! Tổng lượng trong RAM: {newQty}. CHƯA LƯU CỨNG, LÀM ƠN COMMIT NGAY SAU ĐÓ!");
        
        // --- STAGE 2 INJECTION LOGGING ---
        Debug.Log($"<color=yellow>[STAGE 2: RAM -> UI]</color> Đang bắn tín hiệu OnInventoryChanged cho Thằng UI Kho Hàng biết để nó Cập nhật Vẽ lại màn hình!");
        if (currentQty == 0) OnInventoryItemAdded?.Invoke(itemID, amount);
        else OnInventoryChanged?.Invoke(itemID, newQty);
    }

    public bool RemoveItem(string itemID, int amount)
    {
        if (CurrentPlayer == null) return false;
        
        int currentQty = _sessionInventory.ContainsKey(itemID) ? _sessionInventory[itemID] : 0;
        if (currentQty >= amount)
        {
            int newQty = currentQty - amount;
            _sessionInventory[itemID] = newQty;
            _sessionChanges.Add(itemID);
            Debug.Log($"<color=orange>[STAGE 3: Gameplay -> RAM]</color> Bị trừ tiêu hao -{amount}x {itemID}. Còn {newQty}. CHƯA LƯU CỨNG, chờ COMMIT!");
            Debug.Log($"<color=yellow>[STAGE 2: RAM -> UI]</color> Đang bắn tín hiệu OnInventoryChanged để UI cập nhật số lượng mới!");
            OnInventoryChanged?.Invoke(itemID, newQty);
            return true;
        }
        
        Debug.LogWarning($"[DataManager-RAM] Không đủ {amount}x {itemID} trong kho để trừ! (Có: {currentQty})");
        return false;
    }

    // ==== TRUY VẤN KHO ĐỒ ====
    public int GetItemAmount(string itemID)
    {
        if (!IsReady || CurrentPlayer == null) return 0;
        var inv = DB.Table<InventoryModel>().FirstOrDefault(i => i.PlayerID == CurrentPlayer.PlayerID && i.ItemID == itemID);
        return inv != null ? inv.Quantity : 0;
    }

    // ==== API NÔNG TRẠI (FARM_TILE) ====
    public System.Collections.Generic.List<FarmTileModel> GetFarmTiles()
    {
        if (CurrentPlayer == null) return new System.Collections.Generic.List<FarmTileModel>();
        return DB.Table<FarmTileModel>().Where(t => t.PlayerID == CurrentPlayer.PlayerID).ToList();
    }

    public void UpdateFarmTile(FarmTileModel tile)
    {
        if (!IsReady || CurrentPlayer == null) return;
        DB.InsertOrReplace(tile);
        Debug.Log($"[DataManager] FARM_TILE Saved: {tile.TileID} State={tile.State}");
    }

    public FarmTileModel GetFarmTile(string tileID)
    {
        if (!IsReady || CurrentPlayer == null) return null;
        return DB.Table<FarmTileModel>().FirstOrDefault(t => t.TileID == tileID);
    }

    private void InitStaticData()
    {
        // ── Bước 1: Đảm bảo catalog hạt giống / nông sản tồn tại ──
        if (DB.Table<SeedItemModel>().Count() == 0)
        {
            DB.Insert(new SeedItemModel { SeedID = "seed_01", Name = "Hạt Giống Khoai Tây", BuyPrice = 15 });
            DB.Insert(new SeedItemModel { SeedID = "seed_02", Name = "Hạt Giống Cà Rốt",   BuyPrice = 20 });
            DB.Insert(new SeedItemModel { SeedID = "seed_03", Name = "Hạt Giống Bắp Cải",  BuyPrice = 25 });
            DB.Insert(new SeedItemModel { SeedID = "seed_04", Name = "Hạt Giống Cà Chua",  BuyPrice = 40 });

            DB.Insert(new ProductItemModel { ProductID = "product_01", Name = "Khoai Tây", Type = "Vegetable", SellPrice = 30 });
            DB.Insert(new ProductItemModel { ProductID = "product_02", Name = "Cà Rốt",   Type = "Vegetable", SellPrice = 40 });
            DB.Insert(new ProductItemModel { ProductID = "product_03", Name = "Bắp Cải",  Type = "Vegetable", SellPrice = 50 });
            DB.Insert(new ProductItemModel { ProductID = "product_04", Name = "Cà Chua",  Type = "Vegetable", SellPrice = 60 });
        }
        
        // ── Bước 2: Đảm bảo catalog vật trang trí (Decor) tồn tại ──
        if (DB.Table<DecorItemModel>().Count() == 0)
        {
            // Các index được phép chồng (IsFlatTop = true): 0, 1, 20, 21, 30, 34-43, 47-53
            var flatTopIndices = new System.Collections.Generic.HashSet<int> {
                0, 1, 20, 21, 30, 
                34, 35, 36, 37, 38, 39, 40, 41, 42, 43,
                47, 48, 49, 50, 51, 52, 53
            };

            DB.RunInTransaction(() => 
            {
                for (int i = 0; i <= 60; i++) 
                {
                    // Chỉ giới hạn sinh mẫu một số hộp điển hình, hoặc sinh tất cả tới 60. Ở đây map toàn bộ.
                    bool isFlat = flatTopIndices.Contains(i);
                    DB.Insert(new DecorItemModel { 
                        DecorID = "castles_Sheet_" + i, 
                        Name = isFlat ? "Thùng Cát-tông" : "Mái Cát-tông", 
                        BuyPrice = isFlat ? 50 : 150,
                        IsFlatTop = isFlat
                    });
                }
            });
        }
        // → Để thêm seed mới: chỉ cần thêm DB.Insert() ở đây + chạy Migrate.
        // → Mission:
        //    DB.Insert(new SeedItemModel { SeedID = "seed_05", Name = "Cầu Vồng", BuyPrice = 60 });

        // ── Bước 3: Đảm bảo catalog Nông cụ (Tool) tồn tại ──
        if (DB.Table<ToolItemModel>().Count() == 0)
        {
            DB.Insert(new ToolItemModel { ToolID = "item_fertilizer", Name = "Phân Bón", BuyPrice = 30, IsPermanent = false });
            DB.Insert(new ToolItemModel { ToolID = "tool_pest",       Name = "Thuốc Trừ Sâu", BuyPrice = 15, IsPermanent = false });
            DB.Insert(new ToolItemModel { ToolID = "tool_hoe",        Name = "Cái Cuốc", BuyPrice = 0, IsPermanent = true });
            DB.Insert(new ToolItemModel { ToolID = "tool_watercan",   Name = "Bình Tưới Nước", BuyPrice = 0, IsPermanent = true });
        }
    }

    /// <summary>
    /// Migration: Phát hiện và vá các item mới trong Starter Kit cho player cũ.
    /// Gọi sau LoginPlayer() khi tài khoản đã tồn tại.
    /// KHAI TE tài khoản cũ: chỉ thêm item mới với qty=0, KHÔNG reset định mức cũ.
    /// </summary>
    private void MigratePlayerData(string userID)
    {
        // Danh sách tất cả item mà player bất kỳ đều nên có slot (dù đang = 0)
        var expectedItems = new System.Collections.Generic.Dictionary<string, int>
        {
            { "seed_01",         0 },   // slot rỗng nếu chưa có
            { "seed_02",         0 },
            { "seed_03",         0 },
            { "seed_04",         0 },
            { "tool_hoe",        0 },
            { "tool_watercan",   0 },
            { "tool_pest",       0 },
            { "item_fertilizer", 0 },
            // → thêm item mới ở đây khi data mở rộng
        };

        int patched = 0;
        foreach (var entry in expectedItems)
        {
            bool exists = DB.Table<InventoryModel>()
                .Any(i => i.PlayerID == userID && i.ItemID == entry.Key);
            if (!exists)
            {
                DB.Insert(new InventoryModel { PlayerID = userID, ItemID = entry.Key, Quantity = entry.Value });
                patched++;
                Debug.Log($"[Migration] Vá slot mới cho player cũ: {entry.Key} (qty={entry.Value})");
            }
        }
        if (patched > 0)
            Debug.Log($"<color=cyan>[Migration] Hoàn tất: đã patch {patched} item slot mới cho {userID}.</color>");
    }

    private void OnApplicationQuit()
    {
        _isClosing = true; // Báo hiệu cho các script khác là DB sắp đóng cửa
        if (DB != null) 
        {
            DB.Close();
            Debug.Log("[DataManager] SQLite Connection Closed safely.");
        }
    }
}
