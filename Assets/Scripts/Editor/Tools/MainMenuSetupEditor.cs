using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using FarmPuzzle.UI;
using FarmPuzzle.FarmSystem;

namespace FarmPuzzle.EditorTools
{
    public class MainMenuSetupEditor : EditorWindow
    {
        [MenuItem("FarmPuzzle/Cài Đặt Candy Crush UI (Auto Setup)", false, 1)]
        public static void SetupCandyCrushUI()
        {
            // 1. Tìm Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Không tìm thấy Canvas trong Scene. Hãy tạo Canvas trước!");
                return;
            }

            GameObject targetObject = canvas.gameObject;

            // 2. Rút cùi rút lõi SandboxCanvasUI
            var oldSandbox = canvas.GetComponentInChildren<FarmPuzzle.Testing.SandboxCanvasUI>(true);
            if (oldSandbox != null)
            {
                targetObject = oldSandbox.gameObject;
                DestroyImmediate(oldSandbox);
                Debug.Log("- Đã tiêu diệt SandboxCanvasUI cũ.");
            }

            // 3. Gắn MainMenuUI
            MainMenuUI mainMenu = targetObject.GetComponent<MainMenuUI>();
            if (mainMenu == null) mainMenu = targetObject.AddComponent<MainMenuUI>();

            mainMenu.gridManager = FindFirstObjectByType<GridManager>();

            // 4. Tạo Login Panel
            GameObject loginPanel = CreateUIObject("LoginPanel", targetObject.transform);
            mainMenu.loginPanel = loginPanel;
            mainMenu.btnPlayNew = CreateUIButton("BtnPlayNew", loginPanel.transform, "🔥 Chơi Mới (Play!)", new Vector2(0, 40), new Color(0.95f, 0.35f, 0.45f));
            mainMenu.btnRetrieve = CreateUIButton("BtnRetrieve", loginPanel.transform, "🔁 Chơi Tiếp (Retrieve)", new Vector2(0, -40), new Color(0.25f, 0.65f, 0.95f));

            // 5. Tạo New Player Panel
            GameObject newPlayerPanel = CreateUIObject("NewPlayerPanel", targetObject.transform);
            newPlayerPanel.SetActive(false);
            mainMenu.newPlayerPanel = newPlayerPanel;
            
            mainMenu.inputPlayerName = CreateUIInputField("InputName", newPlayerPanel.transform, new Vector2(0, 40));
            mainMenu.btnSubmitNewPlayer = CreateUIButton("BtnSubmit", newPlayerPanel.transform, "Tạo Nông Trại!", new Vector2(0, -40), new Color(0.2f, 0.8f, 0.3f));

            // 6. Tạo HUD Panel (Giao diện khi đang đứng ở Map)
            GameObject hudPanel = CreateUIObject("HUDPanel", targetObject.transform);
            hudPanel.SetActive(false);
            mainMenu.hudPanel = hudPanel;

            Button btnOpenTetris = CreateUIButton("BtnOpenTetris", hudPanel.transform, "🧩 MỞ TETRIS", Vector2.zero, new Color(0.9f, 0.5f, 0.1f));
            RectTransform rt = btnOpenTetris.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-20, 20); // Góc dưới bên phải
            mainMenu.btnOpenTetris = btnOpenTetris;

            EditorUtility.SetDirty(mainMenu);
            Debug.Log("<color=cyan>✨ QUÁ TRÌNH SETUP CANDY CRUSH UI ĐÃ HOÀN TẤT. BẠN CÓ THỂ BẤM PLAY ĐỂ THỬ!</color>");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        private static Button CreateUIButton(string name, Transform parent, string textStr, Vector2 pos, Color btnColor)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<Button>();

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280, 60);
            rt.anchoredPosition = pos;

            Image img = go.GetComponent<Image>();
            img.color = btnColor;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(go.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            
            Text txt = txtObj.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = textStr;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = 22;

            return go.GetComponent<Button>();
        }

        private static InputField CreateUIInputField(string name, Transform parent, Vector2 pos)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<InputField>();

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280, 50);
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(go.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(15, 0); txtRt.offsetMax = new Vector2(-15, 0);

            Text txt = txtObj.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.black;
            txt.fontSize = 20;

            InputField input = go.GetComponent<InputField>();
            input.textComponent = txt;
            
            return input;
        }
    }
}
