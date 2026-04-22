using UnityEngine;
using UnityEditor;
using FarmPuzzle.FarmSystem.Crop;

namespace FarmPuzzle.EditorTools
{
    public class SeedToCropInspector : EditorWindow
    {
        [MenuItem("Farm Puzzle/Diệt Lỗi Hiển Thị Hạt Giống (Seed-Crop Data Check)")]
        public static void ShowWindow()
        {
            GetWindow<SeedToCropInspector>("Database Linker");
        }

        private Vector2 scrollPos;

        private void OnGUI()
        {
            GUILayout.Label("🛠️ KIỂM TRA MẮC CÁ (SEED -> CROP -> PREFAB)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Phát hiện lỗi sai giống: Hạt bắp cải nhưng mọc ra Cà rốt. Tool này sẽ rà soát sự chéo ngoe giữa SO_Seed -> SO_Crop -> Prefab Product.", MessageType.Info);

            if (GUILayout.Button("Chẩn đoán lỗi Link Data", GUILayout.Height(40)))
            {
                CheckDataLinks();
            }

            // Reload assets if needed
            var seeds = Resources.LoadAll<SeedItemSO>("");
            if (seeds == null || seeds.Length == 0)
            {
                GUILayout.Label("Không tìm thấy SeedItemSO nào trong Cây thư mục!");
                return;
            }

            GUILayout.Space(10);
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            GUIStyle errorStyle = new GUIStyle(EditorStyles.label);
            errorStyle.normal.textColor = Color.red;
            errorStyle.fontStyle = FontStyle.Bold;

            GUIStyle okStyle = new GUIStyle(EditorStyles.label);
            okStyle.normal.textColor = Color.green;

            GUILayout.BeginHorizontal("box");
            GUILayout.Label("SEED ID", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("TÊN HẠT", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label("LINK CROP DATA", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label("PREFAB CÂY", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label("TRẠNG THÁI", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            foreach (var seed in seeds)
            {
                GUILayout.BeginHorizontal("box");
                GUILayout.Label(seed.seedID, GUILayout.Width(100));
                GUILayout.Label(seed.seedName, GUILayout.Width(150));

                if (seed.cropData == null)
                {
                    GUILayout.Label("NULL", errorStyle, GUILayout.Width(150));
                    GUILayout.Label("NONE", errorStyle, GUILayout.Width(200));
                    GUILayout.Label("LỖI: Chưa kéo SO_Crop vào thẻ Seed này!", errorStyle);
                }
                else
                {
                    string cropLink = seed.cropData.productID;
                    GUILayout.Label(cropLink, GUILayout.Width(150));

                    string expectedObj = "Crop_" + cropLink;
                    GameObject loadedPrefab = seed.cropData.cropPrefab != null ? seed.cropData.cropPrefab : Resources.Load<GameObject>(expectedObj);

                    bool hasPrefab = loadedPrefab != null;
                    if (hasPrefab)
                    {
                        GUILayout.Label(loadedPrefab.name, GUILayout.Width(200));
                        
                        // Check if SeedID matches Product logic. Ví dụ: seed_03 -> product_03
                        string expectedProductID = seed.seedID.Replace("seed_", "product_");
                        if (cropLink != expectedProductID)
                        {
                            GUILayout.Label("⚠ CẢNH BÁO: Râu ông nọ cắm cằm bà kia (Gắn nhầm SO_Crop)", errorStyle);
                            if (GUILayout.Button("Sửa tự động"))
                            {
                                FixLink(seed, expectedProductID);
                            }
                        }
                        else
                        {
                            GUILayout.Label("OK. Mọi thứ ĐỒNG BỘ NHAU.", okStyle);
                        }
                    }
                    else
                    {
                        GUILayout.Label("KHÔNG CÓ PREFAB", errorStyle, GUILayout.Width(200));
                        GUILayout.Label($"LỖI: Mất file Prefab '{expectedObj}' trong Resources!", errorStyle);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private void FixLink(SeedItemSO seed, string targetProductID)
        {
            var allCrops = Resources.LoadAll<CropDataSO>("");
            foreach (var c in allCrops)
            {
                if (c.productID == targetProductID)
                {
                    seed.cropData = c;
                    EditorUtility.SetDirty(seed);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"Đã nới dây: {seed.seedID} -> {c.productID}");
                    return;
                }
            }
            Debug.LogError($"Không tìm thấy tệp CropDataSO nào có ProductID = {targetProductID} để link!");
        }

        private void CheckDataLinks()
        {
            AssetDatabase.Refresh();
        }
    }
}
