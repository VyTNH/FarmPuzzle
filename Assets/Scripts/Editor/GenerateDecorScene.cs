using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FarmPuzzle.LandPuzzle.Farm; // Nhúng script FarmLandTile

[InitializeOnLoad]
public static class GenerateDecorScene
{
    static GenerateDecorScene()
    {
        EditorApplication.delayCall += BuildSceneOnce;
    }

    private static void BuildSceneOnce()
    {
        if (EditorPrefs.GetBool("DecorScene_Generated", false)) return;
        EditorPrefs.SetBool("DecorScene_Generated", true);

        try
        {
            // 1. Tạo Scene mới
            string scenePath = "Assets/Scenes/DecorTestScene.unity";
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 2. Load Dirt prefab
            string prefabPath = "Assets/Prefabs/Farm/Dirt.prefab";
            GameObject dirtPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (dirtPrefab == null)
            {
                Debug.LogError($"[DecorSceneGen] Không tìm thấy {prefabPath}! Đang tạo các ô đất trống giả lập.");
            }

            // 3. Tạo GameObject cha chứa toàn bộ Grid
            GameObject gridRoot = new GameObject("FarmTilemapGrid");
            gridRoot.transform.position = Vector3.zero;

            // 4. Sinh ra lưới 5x5 ô Dirt để test đặt đồ (Placement)
            int width = 5;
            int height = 5;

            // Offset để Farm nằm chính diện Camera
            Vector3 offset = new Vector3(-width / 2f, -height / 2f, 0);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GameObject tileObj;
                    if (dirtPrefab != null)
                    {
                        tileObj = (GameObject)PrefabUtility.InstantiatePrefab(dirtPrefab, newScene);
                    }
                    else
                    {
                        tileObj = new GameObject($"Dirt_{x}_{y}");
                        tileObj.AddComponent<SpriteRenderer>(); // Dummy
                        tileObj.AddComponent<BoxCollider2D>();
                    }

                    tileObj.name = $"Tile_{x}_{y}";
                    tileObj.transform.SetParent(gridRoot.transform);
                    
                    // Giả sử mỗi ô cách nhau 1 đơn vị
                    tileObj.transform.position = new Vector3(x, y, 0) + offset;

                    // Gắn Script FarmLandTile nếu chưa có (mặc định Dirt prefab đang gắn)
                    FarmLandTile tileScript = tileObj.GetComponent<FarmLandTile>();
                    if (tileScript == null) 
                    {
                        tileScript = tileObj.AddComponent<FarmLandTile>();
                    }

                    // Force unlock để test hệ thống xây nhà (Decor placer) mà không cần chơi puzzle
                    tileScript.ForceUnlock();
                }
            }

            // 5. Điều chỉnh Camera để nhìn vừa vặn Grid 5x5
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj != null)
            {
                camObj.transform.position = new Vector3(0, 0, -10);
                Camera cam = camObj.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.orthographic = true;
                    cam.orthographicSize = 4f; 
                }
            }

            // 6. Lưu Scene
            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log($"<color=cyan>[FarmPuzzle] Đã tạo thành công Scene '{scenePath}' chứa Grid 5x5 (Farm Tilemap) để phục vụ test đặt Decor!</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DecorSceneGen] Có lỗi: {e.Message}");
        }
    }
}
