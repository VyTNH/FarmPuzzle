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

    // ==== MÔ PHỎNG HỆ THỐNG ĐĂNG NHẬP (OAUTH META / FACEBOOK) ====
    public bool LoginViaMetaFacebook(string fbUserID, string fbUserName)
    {
        // 1. Dùng Database quét xem Meta ID này đã đăng ký Game chưa
        var player = DB.Table<PlayerModel>().Where(p => p.PlayerID == fbUserID).FirstOrDefault();
        
        if (player != null)
        {
            // TÌM THẤY TÀI KHOẢN -> LOAD XONG
            CurrentPlayer = player;
            Debug.Log($"[META LOGIN] Đăng nhập thành công! Chào mừng trở lại {CurrentPlayer.Name}");
            return true;
        }
        else
        {
            // KHÔNG TÌM THẤY -> CẤP DỮ LIỆU TÂN THỦ (NEW GAME)
            return CreateNewPlayer(fbUserID, fbUserName);
        }
    }

    private bool CreateNewPlayer(string fbUserID, string fbUserName)
    {
        // 1. Tạo Tài khoản Người chơi
        var newPlayer = new PlayerModel {
            PlayerID = fbUserID,
            Name = fbUserName,
            EXP = 0,
            Money = 500 // Tiền trợ cấp khởi nghiệp Meta
        };
        DB.Insert(newPlayer);

        // 2. Tặng Vũ Khí & Hạt giống đầu tay (Bảng Inventory)
        DB.Insert(new InventoryModel { PlayerID = fbUserID, ItemID = "seed_carrot", Quantity = 10 });
        DB.Insert(new InventoryModel { PlayerID = fbUserID, ItemID = "tool_hoe", Quantity = 1 });
        DB.Insert(new InventoryModel { PlayerID = fbUserID, ItemID = "tool_watercan", Quantity = 1 });

        // 3. Phân chia 4 lô đất mặc định (Bảng Farm_Tile)
        for (int i = 0; i < 4; i++) {
            DB.Insert(new FarmTileModel { 
                TileID = $"tile_{fbUserID}_{i}", 
                PlayerID = fbUserID, 
                State = 0, 
                PlantedSeedID = "", 
                PlantTimeTicks = 0, 
                HasObstacle = false, 
                ObstacleID = "" 
            });
        }

        // 4. Nhồi nhiệm vụ tân thủ (Bảng Player_Quest)
        DB.Insert(new PlayerQuestModel {
            PlayerID = fbUserID,
            QuestID = "quest_first_harvest",
            QuestProgress = 0,
            IsBanned = false
        });

        // Đổ data vào RAM để dùng trong quá trình Game Loop chạy
        CurrentPlayer = newPlayer;
        Debug.Log($"[META LOGIN] Đã tạo thành công Nông dân mới: {fbUserName}. Cấu hình toàn bộ ERD tân thủ thành công!");
        return true;
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
