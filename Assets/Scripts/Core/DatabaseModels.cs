using SQLite;
using System;

namespace FarmPuzzle.Core.Database
{
    // ==========================================
    // BẢNG DỮ LIỆU ĐỘNG (NGƯỜI CHƠI)
    // ==========================================
    [Table("PLAYER")]
    [Serializable]
    public class PlayerModel {
        [PrimaryKey] public string PlayerID { get; set; }
        public string Name { get; set; }
        public int EXP { get; set; }
        public int Money { get; set; }
    }

    [Table("INVENTORY")]
    [Serializable]
    public class InventoryModel {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public string PlayerID { get; set; }
        [Indexed] public string ItemID { get; set; }
        public int Quantity { get; set; }
    }

    [Table("DECOR_RECORD")]
    [Serializable]
    public class DecorRecordModel {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public string PlayerID { get; set; }
        [Indexed] public string DecorID { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
    }

    [Table("PLAYER_QUEST")]
    [Serializable]
    public class PlayerQuestModel {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public string PlayerID { get; set; }
        [Indexed] public string QuestID { get; set; }
        public int QuestProgress { get; set; }
        public bool IsBanned { get; set; }
    }

    [Table("FARM_TILE")]
    [Serializable]
    public class FarmTileModel {
        [PrimaryKey] public string TileID { get; set; }
        [Indexed] public string PlayerID { get; set; }
        public int State { get; set; }
        public string PlantedSeedID { get; set; }
        public long PlantTimeTicks { get; set; }
        public bool HasObstacle { get; set; }
        public string ObstacleID { get; set; }
    }

    [Table("ACTIVE_ORDER")]
    [Serializable]
    public class ActiveOrderModel {
        [PrimaryKey] public string OrderID { get; set; }
        [Indexed] public string PlayerID { get; set; }
        public int RewardMoney { get; set; }
        public int RewardExp { get; set; }
        public string ReqItemListJSON { get; set; } 
    }

    // ==========================================
    // BẢNG DỮ LIỆU TĨNH (HỆ THỐNG / STORE)
    // ==========================================
    [Table("SEED_ITEM")]
    [Serializable]
    public class SeedItemModel {
        [PrimaryKey] public string SeedID { get; set; }
        public string Name { get; set; }
        public int BuyPrice { get; set; }
    }

    [Table("CROP_DATA")]
    [Serializable]
    public class CropDataModel {
        [PrimaryKey] public string SeedID { get; set; }
        public string ProductID { get; set; }
        public int GrowSeconds { get; set; }
    }

    [Table("DECOR_ITEM")]
    [Serializable]
    public class DecorItemModel {
        [PrimaryKey] public string DecorID { get; set; }
        public string Name { get; set; }
        public int BuyPrice { get; set; }
    }

    [Table("OBSTACLE")]
    [Serializable]
    public class ObstacleModel {
        [PrimaryKey] public string ObstacleID { get; set; }
        [Indexed] public string TileID { get; set; }
        public bool HasObstacle { get; set; }
    }

    [Table("PRODUCT_ITEM")]
    [Serializable]
    public class ProductItemModel {
        [PrimaryKey] public string ProductID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int SellPrice { get; set; }
    }
}
