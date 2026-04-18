using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using FarmPuzzle.FarmSystem;

namespace FarmPuzzle.EditorTools
{
    public class UC1UC2TestSceneGenerator : EditorWindow
    {
        // [MenuItem("FarmPuzzle/3. TẠO PHÒNG RÈN TEST UC1 & UC2 (Clean State)")]
        public static void GenerateTestPlayground()
        {
            if (!EditorUtility.DisplayDialog("Xác nhận xé nháp", 
                "Hệ thống sẽ dọn dẹp và tạo 1 Cảnh Test Sạch (Sandbox) chỉ chứa não bộ Cấu trúc để Test riêng UC1 (Database) và UC2 (Trồng trọt).\nSếp đã lưu đống dang dở chưa?", "Tạo Phòng Test", "Khoan"))
                return;

            string scenePath = "Assets/Scenes/UC1_UC2_Sandbox.unity";
            
            // 1. Dọn dẹp và đẻ Scene mới 100%
            Scene sandboxScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            sandboxScene.name = "UC1_UC2_Sandbox";

            // 2. Chỉnh Camera về 2D (Orthographic) cho Game Nông trại
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographic = true;
                mainCam.orthographicSize = 5f;
                mainCam.backgroundColor = new Color(0.2f, 0.6f, 0.3f); // Màu cỏ úa nông trại
                mainCam.transform.position = new Vector3(2.5f, 2.5f, -10f);
                mainCam.gameObject.AddComponent<CameraAutoFitter>();
            }

            // 3. ĐẺ LÕI FARM (GridManager & OfflineTimeManager)
            GameObject farmCore = new GameObject("--- FARM CORE ---");
            var gridMgr = farmCore.AddComponent<GridManager>();
            farmCore.AddComponent<OfflineTimeManager>();
            
            SandboxUICreator.CreateSandboxUI();
            var testUI = Object.FindFirstObjectByType<FarmPuzzle.Testing.SandboxCanvasUI>();

            // Quét tìm tất cả Hạt Giống SO trong dự án tự động nạp đạn vào Bảng UI
            string[] seedGuids = AssetDatabase.FindAssets("t:SeedItemSO");
            var seedList = new SeedItemSO[seedGuids.Length];
            for (int i = 0; i < seedGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(seedGuids[i]);
                seedList[i] = AssetDatabase.LoadAssetAtPath<SeedItemSO>(path);
            }
            if (testUI != null) testUI.availableSeeds = seedList;
            gridMgr.registeredSeeds = seedList; // Để GridManager tra cứu khi restore cây từ DB

            // 4. ĐẺ LÕI META (QuestManager)
            GameObject questCore = new GameObject("--- QUEST CORE ---");
            questCore.AddComponent<FarmPuzzle.Meta.QuestManager>();

            // 5. ĐẺ ĐẤT (Lưới bàn cờ 5x5 đậm/nhạt cho dễ phân biệt từng ô)
            int gridW = 5, gridH = 5;
            float tileSize = 1.0f;
            float gap = 0.08f; // Khoảng cách giữa các ô
            
            // Tạo 2 Material màu sáng/tối xen kẽ kiểu bàn cờ
            Material matLight = new Material(Shader.Find("Sprites/Default"));
            matLight.color = new Color(0.55f, 0.82f, 0.35f); // Xanh cỏ sáng
            
            Material matDark = new Material(Shader.Find("Sprites/Default"));
            matDark.color = new Color(0.40f, 0.65f, 0.25f); // Xanh cỏ đậm

            GameObject landContainer = new GameObject("Land Plots");
            // Canh giữa lưới đất vào Camera
            float offsetX = -(gridW * (tileSize + gap)) / 2f + (tileSize + gap) / 2f + 2.5f;
            float offsetY = -(gridH * (tileSize + gap)) / 2f + (tileSize + gap) / 2f + 2.5f;

            for (int x = 0; x < gridW; x++)
            {
                for (int y = 0; y < gridH; y++)
                {
                    GameObject land = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    land.name = $"LandPlot_{x}_{y}";
                    land.transform.SetParent(landContainer.transform);
                    land.transform.localScale = new Vector3(tileSize, tileSize, 1f);
                    land.transform.position = new Vector3(
                        offsetX + x * (tileSize + gap),
                        offsetY + y * (tileSize + gap),
                        0);

                    // Tô màu bàn cờ xen kẽ
                    var renderer = land.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = ((x + y) % 2 == 0) ? matLight : matDark;

                    // Thêm Collider 2D để Click Chuột được
                    DestroyImmediate(land.GetComponent<MeshCollider>());
                    land.AddComponent<BoxCollider2D>().isTrigger = true;

                    // Thêm Script Nông Trại gốc rễ
                    var landPlot = land.AddComponent<LandPlot>();
                    
                    // Khóa vòng ngoài (rìa) → chỉ 3x3 ở giữa được mở sẵn
                    bool isEdgeTile = (x == 0 || x == gridW - 1 || y == 0 || y == gridH - 1);
                    landPlot.isLocked = isEdgeTile;
                }
            }

            // 6. THIẾT LẬP PHYSICS & EVENT SYSTEM (Để Click chuột xuống Đất được)
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            // Thêm Raycaster vào Camera để bắn tia trúng Ô Đất
            if (mainCam.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null)
            {
                mainCam.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
            }

            // Luộc chín và Lưu ra Cảnh thực
            EditorSceneManager.SaveScene(sandboxScene, scenePath);
            Debug.Log($"[TẠO PHÒNG TEST THÀNH CÔNG] - Đã tạo cảnh {scenePath} với độ tinh khiết 100%!");
            
            // Ping
            var newSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (newSceneAsset != null)
            {
                EditorGUIUtility.PingObject(newSceneAsset);
                Selection.activeObject = newSceneAsset;
            }
        }
    }
}
