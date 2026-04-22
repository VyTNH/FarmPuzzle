using UnityEngine;

namespace FarmPuzzle.Core
{
    /// <summary>
    /// Chứa toàn bộ hằng số đường dẫn (Paths) để tránh hardcode trong dự án.
    /// Giúp việc tái cấu trúc thư mục an toàn hơn.
    /// </summary>
    public static class ProjectPaths
    {
        // ─── RESOURCES PATHS (Relative to Resources/ folder) ───
        public const string RS_UI_INVENTORY_SLOT = "UI/InventorySlot";
        public const string RS_TETRIS_CROP_MAP   = "TetrisCropMap";
        
        // Folders
        public const string RS_FOLDER_SEED_DATA  = "SeedData";
        public const string RS_FOLDER_TOOL_DATA  = "ToolData";
        public const string RS_FOLDER_QUEST_DATA = "QuestData";
        public const string RS_FOLDER_CROP_DATA  = "CropData";

        // Prefixes for dynamic loading
        public const string RS_PREFIX_CROP_SO = "CropData/SO_Crop_";
        public const string RS_PREFIX_SEED_SO = "SeedData/SO_Seed_";
        public const string RS_PREFIX_TOOL_SO = "ToolData/SO_Tool_";

        // ─── ASSET PATHS (Full path from Assets/) ───
        // Dùng cho Editor Scripts (AssetDatabase.LoadAssetAtPath)
        public const string ASSET_PREFAB_BLOCK = "Assets/_Game/Prefabs/Prefabs/Puzzles/Block.prefab";
        public const string ASSET_PREFAB_DECOR_UI_ITEM = "Assets/_Game/Prefabs/Prefabs/UI/DecorUIItem_Template.prefab";
        
        // Scenes
        public const string SCENE_MAIN_FARM = "Assets/_Game/Scenes/Scenes/01_MainFarm.unity";
        
        // Folders (Dành cho Editor scripts tạo file mới)
        public const string ASSET_FOLDER_RESOURCES_SEED = "Assets/Resources/SeedData/";
        public const string ASSET_FOLDER_RESOURCES_CROP = "Assets/Resources/CropData/";
        public const string ASSET_FOLDER_RESOURCES_TOOL = "Assets/Resources/ToolData/";

        // ─── ITEM ID PREFIXES & SPECIAL IDS ───
        public const string PREFIX_PRODUCT = "product_";
        public const string PREFIX_SEED    = "seed_";
        public const string PREFIX_TOOL    = "tool_";
        public const string PREFIX_ITEM    = "item_";

        public const string ID_TOOL_HOE      = "tool_hoe";
        public const string ID_TOOL_WATERCAN = "tool_watercan";
        public const string ID_TOOL_WATER    = "tool_water"; // Một số chỗ dùng tool_water
        public const string ID_TOOL_PEST     = "tool_pest";
        public const string ID_ITEM_FERTILIZER = "item_fertilizer";
    }
}
