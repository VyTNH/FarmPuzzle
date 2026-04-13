using UnityEngine;
#pragma warning disable 0618 // Tắt cảnh báo hàm cũ để sếp đỡ nhức mắt
using UnityEditor;
using UnityEngine.UI;
using FarmPuzzle.LandPuzzle.UI;
using FarmPuzzle.LandPuzzle;
using FarmPuzzle.LandPuzzle.Grid;
using FarmPuzzle.LandPuzzle.Block;
using UnityEngine.Rendering;

namespace FarmPuzzle.EditorTools
{
    public class AutoSetupLandPuzzleUI : UnityEditor.Editor
    {
        // [MenuItem("FarmPuzzle/6. 🔥 BÊ NGUYÊN PUZZLE CỦA QUYẾT VÀO SCENE NÀY")]
        public static void SetupUI()
        {
            Debug.Log("<color=orange>[Setup Tool]</color> Bắt đầu cài đặt hệ thống Puzzle...");

            // --- PHẦN 1: SETUP ENERGY & CORE SYSTEM ---
            SetupCoreSystems();

            // --- PHẦN 2: SETUP PUZZLE BOARD & MANAGERS ---
            SetupPuzzleInfrastructure();

            // --- PHẦN 3: SETUP CONFIRM POPUP UI ---
            SetupConfirmPopupUI();

            Debug.Log("<color=cyan>[Setup Tool]</color> 🔥 HOÀN TẤT CÀI ĐẶT! Sếp hãy kiểm tra Hierarchy xem đã có đủ các bộ phận chưa.");
        }

        private static void SetupCoreSystems()
        {
            Debug.Log("[Setup Tool] Đang dựng Energy System...");
#if UNITY_2023_1_OR_NEWER
            EnergySystem energy = Object.FindAnyObjectByType<EnergySystem>();
#else
            EnergySystem energy = Object.FindObjectOfType<EnergySystem>();
#endif
            if (energy == null)
            {
                GameObject energyObj = new GameObject("--- ENERGY SYSTEM ---");
                energy = energyObj.AddComponent<EnergySystem>();
                Debug.Log("<color=green>+ Đã tạo mới EnergySystem.</color>");
            }

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject evObj = new GameObject("EventSystem");
                evObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                evObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("<color=green>+ Đã tạo mới EventSystem.</color>");
            }
        }

        private static void SetupPuzzleInfrastructure()
        {
            Debug.Log("[Setup Tool] Đang dựng Puzzle Infrastructure...");
#if UNITY_2023_1_OR_NEWER
            LandPuzzleManager manager = Object.FindAnyObjectByType<LandPuzzleManager>();
#else
            LandPuzzleManager manager = Object.FindObjectOfType<LandPuzzleManager>();
#endif
            
            // Nếu đã có manager, xóa sạch dựng lại cho chắc (Force Reset)
            if (manager != null) 
            {
                Debug.Log("<color=yellow>! Phát hiện Manager cũ, đang dọn dẹp để dựng lại bản mới...</color>");
                Object.DestroyImmediate(manager.gameObject);
            }

            GameObject puzzleRoot = new GameObject("--- LAND PUZZLE SYSTEM ---");
            puzzleRoot.transform.localScale = Vector3.one * 0.5f; // Thu nhỏ xuống 0.5 theo ý sếp
            manager = puzzleRoot.AddComponent<LandPuzzleManager>();
            
            // Đẩy SortingGroup lên kịch trần (10000) để không vật thể nào đè được
            SortingGroup sg = puzzleRoot.AddComponent<SortingGroup>();
            sg.sortingLayerName = "Puzzle"; 
            sg.sortingOrder = 10000; 
            Debug.Log("<color=green>+ Đã thiết lập ROOT Puzzle (Scale: 0.5) với Layer 'Puzzle' (Order: 10000).</color>");

            // 1. Grid Board
            GameObject gridObj = new GameObject("GridBoard");
            gridObj.transform.SetParent(puzzleRoot.transform, false);
            gridObj.transform.localPosition = Vector3.zero;
            GridBoard gridBoard = gridObj.AddComponent<GridBoard>();
            
            GameObject blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Puzzles/Block.prefab");
            if (blockPrefab == null) Debug.LogError("[Setup Tool] KHÔNG TÌM THẤY Block.prefab tại Assets/Prefabs/Puzzles/Block.prefab!");
            
            SerializedObject soGrid = new SerializedObject(gridBoard);
            soGrid.FindProperty("_cellPrefab").objectReferenceValue = blockPrefab;
            soGrid.ApplyModifiedProperties();

            // 2. Block Spawner
            GameObject spawnerObj = new GameObject("BlockSpawner");
            spawnerObj.transform.SetParent(puzzleRoot.transform, false);
            spawnerObj.transform.localPosition = new Vector3(0, -5f, 0); // Đưa xuống chân grid
            BlockSpawner spawner = spawnerObj.AddComponent<BlockSpawner>();
            
            Transform[] slots = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject slot = new GameObject($"Slot_{i}");
                slot.transform.SetParent(spawnerObj.transform, false);
                slot.transform.localPosition = new Vector3((i - 1) * 3f, 0, 0); 
                slots[i] = slot.transform;
            }

            SerializedObject soSpawner = new SerializedObject(spawner);
            soSpawner.FindProperty("_gridBoard").objectReferenceValue = gridBoard;
            soSpawner.FindProperty("_blockPrefab").objectReferenceValue = blockPrefab;
            SerializedProperty slotsProp = soSpawner.FindProperty("_spawnSlots");
            slotsProp.arraySize = 3;
            for (int i = 0; i < 3; i++) slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            soSpawner.ApplyModifiedProperties();

            // 3. Scoring
            BlockPuzzleScoring scoring = puzzleRoot.AddComponent<BlockPuzzleScoring>();

            // 4. Panel UI chứa Board (Background che phủ)
            GameObject panel = new GameObject("Puzzle_UI_Panel");
            panel.transform.SetParent(puzzleRoot.transform, false);
            
            GameObject bgObj = new GameObject("PuzzleBackground");
            bgObj.transform.SetParent(panel.transform, false);
            bgObj.transform.localPosition = Vector3.forward * 10f; // Đẩy ra xa hẳn phía sau
            bgObj.transform.localScale = new Vector3(300f, 300f, 1f); // Tăng kích thước để bao phủ khi thu nhỏ 0.5
            
            SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            bgSr.color = new Color(0.02f, 0.02f, 0.02f, 0.98f); // Đậm đà hơn
            bgSr.sortingLayerName = "Puzzle";
            bgSr.sortingOrder = -999;
            Debug.Log("<color=green>+ Đã thiết lập Background tối bao phủ (Order: -999 trong lớp Puzzle).</color>");

            // 5. Kết nối Manager
            SerializedObject soManager = new SerializedObject(manager);
            soManager.FindProperty("_gridBoard").objectReferenceValue = gridBoard;
            soManager.FindProperty("_blockSpawner").objectReferenceValue = spawner;
            soManager.FindProperty("_scoring").objectReferenceValue = scoring;
            soManager.FindProperty("_puzzlePanel").objectReferenceValue = panel;
            soManager.ApplyModifiedProperties();
        }

        private static void SetupConfirmPopupUI()
        {
            Debug.Log("[Setup Tool] Đang dựng Confirm Popup UI...");
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas_LandPuzzle");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            Transform existingPopup = canvas.transform.Find("LandPuzzle_ConfirmPopup");
            if (existingPopup != null) Object.DestroyImmediate(existingPopup.gameObject);

            GameObject popupRoot = new GameObject("LandPuzzle_ConfirmPopup");
            popupRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = popupRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero; rootRect.offsetMax = Vector2.zero;

            popupRoot.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);

            GameObject dialogBox = new GameObject("DialogBox");
            dialogBox.transform.SetParent(popupRoot.transform, false);
            dialogBox.AddComponent<RectTransform>().sizeDelta = new Vector2(450, 300);
            dialogBox.AddComponent<Image>().color = new Color(0.9f, 0.95f, 1f, 1f);

            GameObject textObj = new GameObject("MessageText");
            textObj.transform.SetParent(dialogBox.transform, false);
            textObj.AddComponent<RectTransform>().sizeDelta = new Vector2(400, 150);
            textObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);
            Text msgText = textObj.AddComponent<Text>();
            msgText.text = "Bạn có muốn dùng 1⚡ để giải đố mở ô đất này không?";
            msgText.fontSize = 26; msgText.color = Color.black; msgText.alignment = TextAnchor.MiddleCenter;
            msgText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject btnYesObj = CreateButton("Btn_Yes", "ĐỒNG Ý", new Vector2(-100, -70), new Color(0.1f, 0.5f, 0.1f), dialogBox.transform);
            GameObject btnNoObj = CreateButton("Btn_No", "HỦY", new Vector2(100, -70), new Color(0.5f, 0.1f, 0.1f), dialogBox.transform);

#if UNITY_2023_1_OR_NEWER
            LandPuzzlePopupController controller = Object.FindAnyObjectByType<LandPuzzlePopupController>();
#else
            LandPuzzlePopupController controller = Object.FindObjectOfType<LandPuzzlePopupController>();
#endif
            if (controller == null)
            {
                GameObject ctrlObj = new GameObject("--- LAND PUZZLE UI CONTROLLER ---");
                controller = ctrlObj.AddComponent<LandPuzzlePopupController>();
            }

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("_confirmPopupPanel").objectReferenceValue = popupRoot;
            so.FindProperty("_btnYes").objectReferenceValue = btnYesObj.GetComponent<Button>();
            so.FindProperty("_btnNo").objectReferenceValue = btnNoObj.GetComponent<Button>();
            so.FindProperty("_messageText").objectReferenceValue = msgText;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(controller);
            if (controller.gameObject.scene.name != null)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);

            popupRoot.SetActive(false);
            Debug.Log("<color=green>+ Đã dựng xong Popup và Controller.</color>");
        }


        private static GameObject CreateButton(string name, string label, Vector2 pos, Color bgColor, Transform parent)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180, 70);
            rect.anchoredPosition = pos;

            btnObj.AddComponent<Image>().color = bgColor;
            btnObj.AddComponent<Button>();

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 22; txt.color = Color.white; txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtObj.GetComponent<RectTransform>().sizeDelta = rect.sizeDelta;

            return btnObj;
        }
    }
}
