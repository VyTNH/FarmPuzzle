// FILE NÀY ĐÃ HOÀN THÀNH NHIỆM VỤ VÀ ĐƯỢC ARCHIVE LẠI.
// Để dùng lại, bỏ comment toàn bộ code bên dưới.

/* ===== ARCHIVED: MainMenuSetupEditor =====

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
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) { Debug.LogError("Không tìm thấy Canvas trong Scene!"); return; }

            GameObject targetObject = canvas.gameObject;

            var oldSandbox = canvas.GetComponentInChildren<FarmPuzzle.Testing.SandboxCanvasUI>(true);
            if (oldSandbox != null) { targetObject = oldSandbox.gameObject; DestroyImmediate(oldSandbox); }

            MainMenuUI mainMenu = targetObject.GetComponent<MainMenuUI>();
            if (mainMenu == null) mainMenu = targetObject.AddComponent<MainMenuUI>();
            mainMenu.gridManager = FindFirstObjectByType<GridManager>();

            GameObject loginPanel = CreateUIObject("LoginPanel", targetObject.transform);
            mainMenu.loginPanel = loginPanel;
            mainMenu.btnPlayNew     = CreateUIButton("BtnPlayNew",    loginPanel.transform, "Fire Choi Moi!", new Vector2(0,  40), new Color(0.95f, 0.35f, 0.45f));
            mainMenu.btnRetrieve    = CreateUIButton("BtnRetrieve",   loginPanel.transform, "Choi Tiep!",     new Vector2(0, -40), new Color(0.25f, 0.65f, 0.95f));

            GameObject newPlayerPanel = CreateUIObject("NewPlayerPanel", targetObject.transform);
            newPlayerPanel.SetActive(false);
            mainMenu.newPlayerPanel = newPlayerPanel;
            mainMenu.inputPlayerName    = CreateUIInputField("InputName",   newPlayerPanel.transform, new Vector2(0, 40));
            mainMenu.btnSubmitNewPlayer = CreateUIButton("BtnSubmit", newPlayerPanel.transform, "Tao Nong Trai!", new Vector2(0, -40), new Color(0.2f, 0.8f, 0.3f));

            GameObject hudPanel = CreateUIObject("HUDPanel", targetObject.transform);
            hudPanel.SetActive(false);
            mainMenu.hudPanel = hudPanel;
            Button btnOpenTetris = CreateUIButton("BtnOpenTetris", hudPanel.transform, "Mo Tetris", Vector2.zero, new Color(0.9f, 0.5f, 0.1f));
            RectTransform rt = btnOpenTetris.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0); rt.anchoredPosition = new Vector2(-20, 20);
            mainMenu.btnOpenTetris = btnOpenTetris;

            EditorUtility.SetDirty(mainMenu);
            Debug.Log("<color=cyan>Setup xong!</color>");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return go;
        }

        private static Button CreateUIButton(string name, Transform parent, string textStr, Vector2 pos, Color btnColor)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<Button>();
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(280, 60); r.anchoredPosition = pos;
            go.GetComponent<Image>().color = btnColor;
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(go.transform, false);
            var tr = txtObj.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
            var txt = txtObj.GetComponent<Text>(); txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = textStr; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.fontSize = 22;
            return go.GetComponent<Button>();
        }

        private static InputField CreateUIInputField(string name, Transform parent, Vector2 pos)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<InputField>();
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(280, 50); r.anchoredPosition = pos;
            go.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(go.transform, false);
            var tr = txtObj.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(15, 0); tr.offsetMax = new Vector2(-15, 0);
            var txt = txtObj.GetComponent<Text>(); txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleLeft; txt.color = Color.black; txt.fontSize = 20;
            InputField input = go.GetComponent<InputField>(); input.textComponent = txt;
            return input;
        }
    }
}

===== END ARCHIVED ===== */
