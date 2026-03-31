using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace FarmPuzzle.EditorTools
{
    public class SceneAssembler : EditorWindow
    {
        [MenuItem("FarmPuzzle/2. Auto-Assemble MainFarm Scene")]
        public static void AssembleScenes()
        {
            if (!EditorUtility.DisplayDialog("Xác nhận Gộp Scene", 
                "Tao sẽ gộp HvuScene (Nông trại) và featVy-QuestSystem (Nhiệm vụ) thành Cảnh mới toanh: 01_MainFarm.unity.\n\nSếp đã lưu lại những thay đổi đang làm chưa?", "Quất luôn!", "Khoan, Hủy"))
                return;

            string mainPath = "Assets/Scenes/01_MainFarm.unity";
            string farmPath = "Assets/Scenes/HvuScene.unity";
            string questPath = "Assets/Scenes/featVy-QuestSystem.unity";

            // Tạo Scene mới toanh (sạch sẽ, không có Main Camera/Light mặc định bị trùng)
            Scene mainScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            mainScene.name = "01_MainFarm";

            // Nạp Additive Farm của nhóc Vũ
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(farmPath) != null)
            {
                Scene farmScene = EditorSceneManager.OpenScene(farmPath, OpenSceneMode.Additive);
                MergeScene(farmScene, mainScene);
                EditorSceneManager.CloseScene(farmScene, true);
            }
            else
            {
                Debug.LogWarning($"[Mảnh ghép 1] Trượt! Không tìm thấy: {farmPath}");
            }

            // Nạp Additive Quest của nhỏ Vy
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(questPath) != null)
            {
                Scene questScene = EditorSceneManager.OpenScene(questPath, OpenSceneMode.Additive);
                MergeScene(questScene, mainScene);
                EditorSceneManager.CloseScene(questScene, true);
            }
            else
            {
                Debug.LogWarning($"[Mảnh ghép 2] Trượt! Không tìm thấy: {questPath}");
            }

            // Dọn rác (Clean Duplicates: Tránh bị 2 EventSystem hoặc 2 Camera đè nhau)
            CleanDuplicates(mainScene);

            // Lưu Scene mới
            EditorSceneManager.SaveScene(mainScene, mainPath);
            Debug.Log($"[Assembler] BÙM! Đã đúc thành công Scene cuối cùng tại: {mainPath}!");
            
            // Ping luôn cho sếp dễ thấy
            var newSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(mainPath);
            if (newSceneAsset != null)
            {
                EditorGUIUtility.PingObject(newSceneAsset);
                Selection.activeObject = newSceneAsset;
            }
        }

        private static void MergeScene(Scene source, Scene target)
        {
            GameObject[] roots = source.GetRootGameObjects();
            foreach (var go in roots)
            {
                SceneManager.MoveGameObjectToScene(go, target);
            }
        }

        private static void CleanDuplicates(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            int cameraCount = 0;
            int eventSystemCount = 0;

            foreach (var go in roots)
            {
                // Dọn Camera bị thừa (Nông trại xài Camera riêng, UI Canvas xài Camera riêng đụng nhau sập lều)
                if (go.GetComponent<Camera>() != null)
                {
                    cameraCount++;
                    if (cameraCount > 1) 
                    {
                        Debug.Log($"[DonDep] Đã chém Camera thừa của: {go.name}");
                        DestroyImmediate(go);
                        continue; // Xác đã cháy rụi, rút lui ngay khỏi vòng lặp kiểm tra
                    }
                }
                
                // Dọn EventSystem bị thừa (UI sẽ bị liệt liệt nếu có 2 EventSystem)
                if (go.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
                {
                    eventSystemCount++;
                    if (eventSystemCount > 1)
                    {
                        Debug.Log($"[DonDep] Đã dập EventSystem thừa của: {go.name}");
                        DestroyImmediate(go);
                        continue;
                    }
                }
            }
        }
    }
}
