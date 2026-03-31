using UnityEngine;
using SQLite;
using System.IO;
using FarmPuzzle.Core.Database;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    public SQLiteConnection DB { get; private set; }

    // Thông tin người chơi hiện tại đang kết nối (Giống PlayerSession)
    public PlayerModel CurrentPlayer { get; private set; }

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

        InitStaticData(); // Khởi tạo Cửa hàng Hạt giống nếu chưa có
    }

    // ==== HỆ THỐNG ĐĂNG NHẬP / TẠO TÀI KHOẢN (SINGLE-PLAYER) ====
    public bool LoginPlayer(string userID, string userName)
    {
        // 1. Dùng Database quét xem ID này đã đăng ký Game chưa
        var player = DB.Table<PlayerModel>().Where(p => p.PlayerID == userID).FirstOrDefault();
        
        if (player != null)
        {
            // TÌM THẤY TÀI KHOẢN -> LOAD XONG
            CurrentPlayer = player;
            Debug.Log($"[LOGIN] Đăng nhập thành công! Chào mừng trở lại {CurrentPlayer.Name}");
            
            // Kiểm tra FARM_TILE có đúng format 5x5 (x_y) không, nếu lỗi → tái tạo
            ValidateAndRepairFarmTiles(userID);
            return true;
        }
        else
        {
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

    // Kiểm tra FARM_TILE đúng format 5x5 (tile_playerID_x_y). Nếu lỗi → xóa tạo lại.
    private void ValidateAndRepairFarmTiles(string userID)
    {
        var existingTiles = DB.Table<FarmTileModel>().Where(t => t.PlayerID == userID).ToList();
        
        // Kiểm tra: phải có đúng 25 ô VÀ tile đầu tiên phải có format _x_y (2 số cuối)
        bool needRepair = false;
        if (existingTiles.Count != 25)
        {
            needRepair = true;
            Debug.LogWarning($"[DataManager] FARM_TILE có {existingTiles.Count} ô (cần 25). Sẽ tái tạo.");
        }
        else
        {
            // Kiểm tra format: tile cuối phải kết thúc bằng _4_4
            string expectedLast = $"tile_{userID}_4_4";
            if (!existingTiles.Exists(t => t.TileID == expectedLast))
            {
                needRepair = true;
                Debug.LogWarning("[DataManager] FARM_TILE sai format (thiếu tọa độ x_y). Sẽ tái tạo.");
            }
        }

        if (needRepair)
        {
            // Xóa tiles cũ
            foreach (var old in existingTiles) DB.Delete(old);
            
            // Tạo lại 25 ô đúng format
            int gridW = 5, gridH = 5;
            for (int x = 0; x < gridW; x++)
            {
                for (int y = 0; y < gridH; y++)
                {
                    bool isEdge = (x == 0 || x == gridW - 1 || y == 0 || y == gridH - 1);
                    DB.Insert(new FarmTileModel {
                        TileID = $"tile_{userID}_{x}_{y}",
                        PlayerID = userID,
                        State = isEdge ? 0 : 1,
                        PlantedSeedID = "",
                        PlantTimeTicks = 0,
                        HasObstacle = isEdge,
                        ObstacleID = isEdge ? "rock_default" : ""
                    });
                }
            }
            Debug.Log("[DataManager] Đã tái tạo 25 ô FARM_TILE đúng format 5x5!");
        }
    }

    private bool CreateNewPlayer(string userID, string userName)
    {
        // 1. Tạo Tài khoản Người chơi
        var newPlayer = new PlayerModel {
            PlayerID = userID,
            Name = userName,
            EXP = 0,
            Money = 500 // Tiền khởi nghiệp
        };
        DB.Insert(newPlayer);

        // 2. Tặng Vũ Khí & Hạt giống đầu tay (Bảng Inventory)
        DB.Insert(new InventoryModel { PlayerID = userID, ItemID = "seed_carrot", Quantity = 10 });
        DB.Insert(new InventoryModel { PlayerID = userID, ItemID = "tool_hoe", Quantity = 1 });
        DB.Insert(new InventoryModel { PlayerID = userID, ItemID = "tool_watercan", Quantity = 1 });

        // 3. Phân chia 25 lô đất mặc định 5x5 (Bảng Farm_Tile)
        int gridW = 5, gridH = 5;
        for (int x = 0; x < gridW; x++)
        {
            for (int y = 0; y < gridH; y++)
            {
                // Rìa ngoài (State=0 khóa), giữa 3x3 (State=1 mở sẵn)
                bool isEdge = (x == 0 || x == gridW - 1 || y == 0 || y == gridH - 1);
                DB.Insert(new FarmTileModel {
                    TileID = $"tile_{userID}_{x}_{y}",
                    PlayerID = userID,
                    State = isEdge ? 0 : 1, // 0=khóa, 1=mở
                    PlantedSeedID = "",
                    PlantTimeTicks = 0,
                    HasObstacle = isEdge, // Rìa có chướng ngại mặc định
                    ObstacleID = isEdge ? "rock_default" : ""
                });
            }
        }

        // 4. Nhồi nhiệm vụ tân thủ (Bảng Player_Quest)
        DB.Insert(new PlayerQuestModel {
            PlayerID = userID,
            QuestID = "quest_first_harvest",
            QuestProgress = 0,
            IsBanned = false
        });

        // Đổ data vào RAM để dùng trong quá trình Game Loop chạy
        CurrentPlayer = newPlayer;
        Debug.Log($"[LOGIN] Đã tạo thành công Nông dân mới: {userName}. Cấu hình toàn bộ ERD tân thủ thành công!");
        return true;
    }

    // ==== CÁC HÀM GIAO TIẾP VỚI CÁC MODULE KHÁC ====
    public void AddGold(int amount)
    {
        if (CurrentPlayer == null) return;
        CurrentPlayer.Money += amount;
        DB.Update(CurrentPlayer);
        Debug.Log($"[DataManager] Tiền Update: {CurrentPlayer.Money} vàng (+{amount})");
    }

    public void AddItem(string itemID, int amount)
    {
        if (CurrentPlayer == null) return;
        var inv = DB.Table<InventoryModel>().FirstOrDefault(i => i.PlayerID == CurrentPlayer.PlayerID && i.ItemID == itemID);
        if (inv != null)
        {
            inv.Quantity += amount;
            DB.Update(inv);
        }
        else
        {
            DB.Insert(new InventoryModel { PlayerID = CurrentPlayer.PlayerID, ItemID = itemID, Quantity = amount });
        }
        Debug.Log($"[DataManager] Kho đồ Update: Nhận {amount}x {itemID}");
    }

    public bool RemoveItem(string itemID, int amount)
    {
        if (CurrentPlayer == null) return false;
        var inv = DB.Table<InventoryModel>().FirstOrDefault(i => i.PlayerID == CurrentPlayer.PlayerID && i.ItemID == itemID);
        if (inv != null && inv.Quantity >= amount)
        {
            inv.Quantity -= amount;
            DB.Update(inv);
            Debug.Log($"[DataManager] Kho đồ Update: Trừ {amount}x {itemID}. Còn {inv.Quantity}");
            return true;
        }
        Debug.LogWarning($"[DataManager] Không đủ {amount}x {itemID} trong kho để trừ!");
        return false;
    }

    // ==== API NÔNG TRẠI (FARM_TILE) ====
    public System.Collections.Generic.List<FarmTileModel> GetFarmTiles()
    {
        if (CurrentPlayer == null) return new System.Collections.Generic.List<FarmTileModel>();
        return DB.Table<FarmTileModel>().Where(t => t.PlayerID == CurrentPlayer.PlayerID).ToList();
    }

    public void UpdateFarmTile(FarmTileModel tile)
    {
        if (CurrentPlayer == null) return;
        DB.Update(tile);
        Debug.Log($"[DataManager] FARM_TILE Update: {tile.TileID} State={tile.State}");
    }

    public FarmTileModel GetFarmTile(string tileID)
    {
        if (CurrentPlayer == null) return null;
        return DB.Table<FarmTileModel>().FirstOrDefault(t => t.TileID == tileID);
    }

    private void InitStaticData()
    {
        if (DB.Table<SeedItemModel>().Count() == 0)
        {
            DB.Insert(new SeedItemModel { SeedID = "seed_carrot", Name = "Hạt Cà Rốt", BuyPrice = 10 });
            DB.Insert(new ProductItemModel { ProductID = "prod_carrot", Name = "Củ Cà Rốt", Type = "Vegetable", SellPrice = 25 });
        }
    }

    private void OnApplicationQuit()
    {
        if (DB != null) DB.Close();
    }
}
