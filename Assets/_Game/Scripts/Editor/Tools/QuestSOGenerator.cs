// AUTO-GENERATED — Xóa file này sau khi đã chạy xong menu item
using UnityEditor;
using UnityEngine;
using FarmPuzzle.Meta;

public static class QuestSOGenerator
{
    [MenuItem("FarmPuzzle/Tools/Generate Quest SOs (DB-Linked)")]
    public static void GenerateQuestSOs()
    {
        string folder = "Assets/ScriptableObjects/Meta";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Meta");

        var defs = new[]
        {
            // (questID, fileName, targetItemID, targetAmount, rewardGold)
            ("quest_sell_potato_10",  "SO_Quest_Sell_Potato",  "product_01", 10,  80),
            ("quest_sell_carrot_10",  "SO_Quest_Sell_Carrot",  "product_02", 10, 100),
            ("quest_sell_cabbage_8",  "SO_Quest_Sell_Cabbage", "product_03",  8, 120),
            ("quest_sell_tomato_5",   "SO_Quest_Sell_Tomato",  "product_04",  5, 150),
        };

        int created = 0, skipped = 0;
        foreach (var (id, name, item, amount, reward) in defs)
        {
            string path = $"{folder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<QuestDataSO>(path);

            // Skip nếu SO hợp lệ đã tồn tại
            if (existing != null && !string.IsNullOrEmpty(existing.questID))
            {
                Debug.Log($"[QuestGen] SKIP: {name} (questID={existing.questID})");
                skipped++;
                continue;
            }

            // Xóa file rỗng/cũ nếu có
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            var so = ScriptableObject.CreateInstance<QuestDataSO>();
            so.questID      = id;
            so.targetItemID = item;
            so.targetAmount = amount;
            so.rewardGold   = reward;
            so.description  = $"Xuất {amount}x {item} qua Tetris. Thưởng: {reward} Vàng.";

            AssetDatabase.CreateAsset(so, path);
            EditorUtility.SetDirty(so);
            Debug.Log($"<color=cyan>[QuestGen] ĐÃ TẠO: {name}</color> | targetItemID={item} | amount={amount} | reward={reward}G");
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Quest SO Generator",
            $"Hoàn tất!\n✅ Tạo mới: {created} SO\n⏩ Bỏ qua: {skipped} SO (đã tồn tại)\n\nThư mục: {folder}",
            "OK"
        );
    }
}
