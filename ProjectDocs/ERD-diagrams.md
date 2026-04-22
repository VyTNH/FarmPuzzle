erDiagram
    PLAYER ||--o{ FARM_TILE : "quản lý"
    PLAYER ||--o{ INVENTORY : "sở hữu"
    PLAYER ||--o{ DECOR_PLACED : "trang trí"
    PLAYER ||--o{ PLAYER_QUEST : "thực hiện"
    PLAYER ||--o{ ACTIVE_ORDER : "nhận"

    FARM_TILE ||--o| OBSTACLE : "chứa"

    PLAYER {
        string PlayerID PK
        int Gold "Tiền vàng"
        int Wood "Tài nguyên Gỗ"
        int Stone "Tài nguyên Đá"
        datetime LastExitTime "Offline Progress"
    }

    FARM_TILE {
        string TileID PK
        string PlayerID FK
        int GridX
        int GridY
        boolean IsUnlocked
        string CropID FK "Trống nếu không trồng cây"
        datetime PlantTime
    }

    PLAYER_QUEST {
        string PlayerID FK
        string QuestID FK
        int CurrentProgress
        boolean IsClaimed
    }

    ACTIVE_ORDER {
        string OrderID PK
        string PlayerID FK
        string RequiredItemID FK
        int RequiredAmount
        int RewardGold
    }

    OBSTACLE {
        string ObstacleID PK
        string TileID FK
        string Type "Đá, Cây khô..."
        int Durability
        string DropItemID FK
    }

    INVENTORY {
        string PlayerID FK
        string ReferenceID FK "ID Hạt/Nông sản/Trang trí"
        int Quantity
    }

    DECOR_PLACED {
        string PlacedID PK
        string PlayerID FK
        string DecorID FK
        int GridX
        int GridY
    }

    SEED_ITEM {
        string SeedID PK
    }