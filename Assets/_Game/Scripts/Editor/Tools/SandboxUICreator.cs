using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FarmPuzzle.Testing;

public class SandboxUICreator : Editor
{
    // [MenuItem("FarmPuzzle/Tạo UI Sandbox (Canvas)", false, 10)]
    public static void CreateSandboxUI()
    {
        // Xóa cũ
        GameObject old = GameObject.Find("Canvas_SandboxUI");
        if (old != null) DestroyImmediate(old);

        // Tạo Canvas
        GameObject canvasObj = new GameObject("Canvas_SandboxUI");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998; // Nằm sát dưới Tetris Popup
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Tạo EventSystem nếu chưa có
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Tạo Script Controller
        SandboxCanvasUI controller = canvasObj.AddComponent<SandboxCanvasUI>();

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // Màn hình dọc chuẩn
        scaler.matchWidthOrHeight = 0.5f;

        // --- Panel Nền (neo dính Cạnh Trên tản sang 2 bên) ---
        GameObject panelObj = new GameObject("Panel_Background");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = panelObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.4f, 0.1f, 0.9f);
        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.15f, 1);
        panelRT.anchorMax = new Vector2(0.85f, 1); // Co lại chiều ngang (2/3 màn hình)
        panelRT.pivot = new Vector2(0.5f, 1);
        panelRT.anchoredPosition = new Vector2(0, 0);
        panelRT.sizeDelta = new Vector2(0, 500); // Giảm chiều cao xuống 500 (2/3 của 750)

        // Header
        Text headerTxt = CreateText(panelObj.transform, "Header", "🎮 BẢNG ĐIỀU KHIỂN (TEST)", 20, true, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30));
        
        // --- Login Panel ---
        GameObject loginPanel = new GameObject("LoginPanel");
        loginPanel.transform.SetParent(panelObj.transform, false);
        RectTransform loginRT = loginPanel.AddComponent<RectTransform>();
        loginRT.anchorMin = new Vector2(0, 0); loginRT.anchorMax = new Vector2(1, 1);
        loginRT.offsetMin = new Vector2(20, 20); loginRT.offsetMax = new Vector2(-20, -70);
        
        CreateText(loginPanel.transform, "Title", "0. ĐĂNG NHẬP HỆ THỐNG", 18, true, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20));
        
        InputField inputID = CreateInputField(loginPanel.transform, "InputPlayerID", "Player ID:", new Vector2(0.5f, 1), new Vector2(0, -70));
        InputField inputName = CreateInputField(loginPanel.transform, "InputPlayerName", "Tên:", new Vector2(0.5f, 1), new Vector2(0, -120));
        
        Button btnLogin = CreateButton(loginPanel.transform, "BtnLogin", "🔑 ĐĂNG NHẬP", new Vector2(0.5f, 1), new Vector2(0, -190));

        // --- Control Panel ---
        GameObject controlPanel = new GameObject("ControlPanel");
        controlPanel.SetActive(false); // Ẩn ngay từ trong Editor để không đè lên LoginPanel
        controlPanel.transform.SetParent(panelObj.transform, false);
        RectTransform controlRT = controlPanel.AddComponent<RectTransform>();
        controlRT.anchorMin = new Vector2(0, 0); controlRT.anchorMax = new Vector2(1, 1);
        controlRT.offsetMin = new Vector2(10, 10); controlRT.offsetMax = new Vector2(-10, -70);

        Text statusTxt = CreateText(controlPanel.transform, "StatusTxt", "...Đang nạp...", 20, false, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100));
        statusTxt.color = Color.green;

        Text invTxt = CreateText(controlPanel.transform, "InventoryTxt", "Kho đồ:\nTrống", 18, false, new Vector2(0, 1), new Vector2(0, 1), new Vector2(250, -250));
        invTxt.color = Color.white;
        invTxt.alignment = TextAnchor.UpperLeft;
        invTxt.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 300);

        CreateText(controlPanel.transform, "Title1", "1. CHỌN TOOL KÉO THẢ:", 16, true, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60));
        
        // Grid Layout Group for Seeds & Tools
        GameObject seedContainer = new GameObject("SeedContainer");
        seedContainer.transform.SetParent(controlPanel.transform, false);
        RectTransform seedRT = seedContainer.AddComponent<RectTransform>();
        seedRT.anchorMin = new Vector2(0, 1); seedRT.anchorMax = new Vector2(1, 1);
        seedRT.pivot = new Vector2(0.5f, 1);
        seedRT.anchoredPosition = new Vector2(0, -100);
        seedRT.sizeDelta = new Vector2(0, 180);
        GridLayoutGroup glg = seedContainer.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(160, 45); // Nút thu nhỏ lại
        glg.spacing = new Vector2(10, 10);

        // Button Prefab for Generation
        GameObject seedBtnPrefab = CreateButton(null, "SeedButtonPrefab", "Slot", Vector2.zero, Vector2.zero).gameObject;
        seedBtnPrefab.SetActive(false); 
        seedBtnPrefab.transform.SetParent(canvasObj.transform, false);

        CreateText(controlPanel.transform, "Title2", "2. TEST NHANH & MINI GAME:", 16, true, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -320));
        
        Button btnAddGold = CreateButton(controlPanel.transform, "BtnGold", "💸 +100G", new Vector2(0.2f, 1), new Vector2(0, -380));
        Button btnAddSeed = CreateButton(controlPanel.transform, "BtnSeed", "🌱 +5 Hạt", new Vector2(0.5f, 1), new Vector2(0, -380));
        Button btnTetris = CreateButton(controlPanel.transform, "BtnTetris", "🧩 MỞ TETRIS", new Vector2(0.8f, 1), new Vector2(0, -380));
        btnTetris.GetComponent<Image>().color = Color.yellow;

        // Assign refs
        controller.loginPanel = loginPanel;
        controller.inputPlayerID = inputID;
        controller.inputPlayerName = inputName;
        controller.btnLogin = btnLogin;

        controller.controlPanel = controlPanel;
        controller.statusText = statusTxt;
        controller.inventoryText = invTxt;
        controller.seedButtonContainer = seedContainer.transform;
        controller.seedButtonPrefab = seedBtnPrefab;
        controller.btnAddGold = btnAddGold;
        controller.btnAddSeed = btnAddSeed;
        controller.btnTetris = btnTetris;

        Selection.activeGameObject = canvasObj;
        Debug.Log("Đã tạo SandboxCanvasUI thành công! Nhớ kéo mảng Available Seeds vào script controller.");
    }

    private static Text CreateText(Transform parent, string name, string content, int size, bool bold, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text txt = go.AddComponent<Text>();
        txt.text = content;
        txt.fontSize = size;
        txt.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos;
        
        // Tránh tràn viền chữ làm nút bấm bị lan vùng chọn (Raycast overlap)
        if (anchorMin != anchorMax) rt.sizeDelta = Vector2.zero; 
        else rt.sizeDelta = new Vector2(350, 60);

        return txt;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = Color.white;
        Button btn = go.AddComponent<Button>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(160, 40);

        Text txt = CreateText(go.transform, "Text", label, 14, true, new Vector2(0,0), new Vector2(1,1), Vector2.zero);
        txt.color = Color.black;
        return btn;
    }

    private static InputField CreateInputField(Transform parent, string name, string label, Vector2 anchor, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = Color.white;
        InputField input = go.AddComponent<InputField>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(250, 30);

        Text txt = CreateText(go.transform, "Text", "", 14, false, new Vector2(0,0), new Vector2(1,1), Vector2.zero);
        txt.color = Color.black;
        input.textComponent = txt;

        Text placeholder = CreateText(go.transform, "Placeholder", label, 14, false, new Vector2(0,0), new Vector2(1,1), Vector2.zero);
        placeholder.color = Color.gray;
        input.placeholder = placeholder;

        return input;
    }
}
