using UnityEngine;
using SQLite;
using System.IO;
using FarmPuzzle.Core.Database;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    public SQLiteConnection DB { get; private set; }

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

        // ==== TẠO FULL 11 BẢNG TRONG BẢN VẼ ERD ====
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

        // ==== TIÊM DOSE FAKE DATA HẠNG NẶNG (ÍT NHẤT 5 DỮ LIỆU/BẢNG) ====
        if (DB.Table<PlayerModel>().Count() == 0)
        {
            SeedFakeDatabase();
        }
    }

    private void SeedFakeDatabase()
    {
        // 1. TẠO 5 PHIÊN BẢN PLAYER (CÓ KHÓA CHÍNH)
        for (int i = 1; i <= 5; i++) {
            DB.Insert(new PlayerModel { PlayerID = $"p_{i:00}", Name = $"Lão Nông {i}", EXP = 100 * i, Money = 1000 * i });
        }

        // 2 + 3. SEED_ITEM & CROP_DATA (Tạo 5 Hạt Giống liên kết 5 Mùa Màng)
        string[] cropNames = { "Cà Rốt", "Khoai Tây", "Bắp Cải", "Cà Chua", "Bí Ngô" };
        string[] cropTypeNames = { "Củ", "Củ", "Lá", "Quả", "Quả" };
        int[] cropPrices = { 10, 15, 20, 25, 40 };
        for (int i = 0; i < 5; i++) {
            string sID = $"seed_{i:00}";
            string pID = $"product_{i:00}";
            DB.Insert(new SeedItemModel { SeedID = sID, Name = $"Hạt Giống {cropNames[i]}", BuyPrice = cropPrices[i] });
            DB.Insert(new CropDataModel { SeedID = sID, ProductID = pID, GrowSeconds = 60 * (i + 1) });
            // 4. PRODUCT_ITEM tương ứng
            DB.Insert(new ProductItemModel { ProductID = pID, Name = cropNames[i], Type = cropTypeNames[i], SellPrice = cropPrices[i] * 3 });
        }

        // 5. DECOR_ITEM (Tạo 5 Món Decor Store)
        string[] decorNames = { "Ghế Đá", "Hàng Rào", "Hồ Cá", "Bù Nhìn", "Xe Kéo" };
        for (int i = 0; i < 5; i++) {
            DB.Insert(new DecorItemModel { DecorID = $"decor_{i:00}", Name = decorNames[i], BuyPrice = 100 * (i + 1) });
        }

        // 6. INVENTORY (Bỏ 5 món ngẫu nhiên vào túi ông p_01)
        DB.Insert(new InventoryModel { PlayerID = "p_01", ItemID = "seed_00", Quantity = 50 });
        DB.Insert(new InventoryModel { PlayerID = "p_01", ItemID = "seed_02", Quantity = 15 });
        DB.Insert(new InventoryModel { PlayerID = "p_01", ItemID = "product_03", Quantity = 5 });
        DB.Insert(new InventoryModel { PlayerID = "p_01", ItemID = "tool_watercan", Quantity = 1 });
        DB.Insert(new InventoryModel { PlayerID = "p_01", ItemID = "tool_hoe", Quantity = 1 });

        // 7. DECOR_RECORD (Ông p_01 đặt 5 món trên sân khấu)
        for (int i = 0; i < 5; i++) {
            DB.Insert(new DecorRecordModel { PlayerID = "p_01", DecorID = $"decor_0{i}", X = i * 2f, Y = i * -1.5f });
        }

        // 8. PLAYER_QUEST (5 Nhiệm vụ đang chạy)
        string[] questIDs = { "q_harvest_carrot", "q_buy_bench", "q_sell_potato", "q_reach_lvl5", "q_water_crop" };
        for (int i = 0; i < 5; i++) {
            DB.Insert(new PlayerQuestModel { PlayerID = "p_01", QuestID = questIDs[i], QuestProgress = i * 2, IsBanned = false });
        }

        // 9. FARM_TILE (5 Ô đất trồng ngẫu nhiên cho p_01)
        // Ô 0: Trống | Ô 1: Có Hạt Cà rốt | Ô 2: Có hạt Khoai Tây | Ô 3, 4: Đất bị đá đè
        DB.Insert(new FarmTileModel { TileID = "tile_00", PlayerID = "p_01", State = 0, PlantedSeedID = "", PlantTimeTicks = 0, HasObstacle = false, ObstacleID = "" });
        DB.Insert(new FarmTileModel { TileID = "tile_01", PlayerID = "p_01", State = 1, PlantedSeedID = "seed_00", PlantTimeTicks = System.DateTime.Now.Ticks, HasObstacle = false, ObstacleID = "" });
        DB.Insert(new FarmTileModel { TileID = "tile_02", PlayerID = "p_01", State = 1, PlantedSeedID = "seed_01", PlantTimeTicks = System.DateTime.Now.Ticks - 10000, HasObstacle = false, ObstacleID = "" });
        DB.Insert(new FarmTileModel { TileID = "tile_03", PlayerID = "p_01", State = 0, PlantedSeedID = "", PlantTimeTicks = 0, HasObstacle = true, ObstacleID = "obs_rock_01" });
        DB.Insert(new FarmTileModel { TileID = "tile_04", PlayerID = "p_01", State = 0, PlantedSeedID = "", PlantTimeTicks = 0, HasObstacle = true, ObstacleID = "obs_weed_02" });

        // 10. OBSTACLE (5 Khối đá/Cỏ rải rác trên bản đồ)
        for (int i = 0; i < 5; i++) {
            DB.Insert(new ObstacleModel { ObstacleID = $"obs_{(i%2==0 ? "rock" : "weed")}_{i:00}", TileID = $"tile_1{i}", HasObstacle = true });
        }

        // 11. ACTIVE_ORDER (5 Đơn Hàng Giao Dịch đang neo trên Bảng)
        for (int i = 0; i < 5; i++) {
            DB.Insert(new ActiveOrderModel { 
                OrderID = $"order_{i:00}", 
                PlayerID = "p_01", 
                RewardMoney = 500 * (i+1), 
                RewardExp = 100 * (i+1), 
                ReqItemListJSON = $"{{\"items\":[{{\"id\":\"product_0{i}\",\"qty\":{i+3}}}]}}" 
            });
        }
        
        Debug.Log("🚀 [SQLite] TOÀN BỘ 11 BẢNG & 5 DỮ LIỆU/BẢNG ĐÃ ĐƯỢC PHỦ KÍN MỘT CÁCH LOGIC!");
    }

    private void OnApplicationQuit()
    {
        if (DB != null) DB.Close();
    }
}
