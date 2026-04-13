using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FarmPuzzle.Tetris;

public class TetrisUICreator : Editor
{
    // [MenuItem("FarmPuzzle/Tạo UI Tetris Xuất Hàng (Canvas)", false, 11)]
    public static void CreateTetrisUI()
    {
        // Xóa cũ
        GameObject old = GameObject.Find("Canvas_TetrisPopupUI");
        if (old != null) DestroyImmediate(old);

        // Tạo Canvas TetrisPopupUI
        GameObject canvasObj = new GameObject("Canvas_TetrisPopupUI");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Ép nằm trên MỌI CANVAS ĐỂ CHẶN CLICK
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasObj.SetActive(false);

        // Tạo EventSystem nếu chưa có
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // --- Panel Nền che mờ Game ---
        GameObject panelObj = new GameObject("Panel_BackgroundDim");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = panelObj.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.9f); 
        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero; panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;

        // Header
        Text headerTxt = CreateText(panelObj.transform, "Header", "TETRIS (XẾP NÔNG SẢN)", 40, true, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100));
        headerTxt.color = Color.yellow;

        // Inventory Display
        Text invTxt = CreateText(panelObj.transform, "InventoryText", "Kho: Đang tải...", 24, false, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -160));
        invTxt.color = Color.cyan;

        // Button TẮT
        Button btnClose = CreateButton(panelObj.transform, "BtnClose", "X ĐÓNG TẠM", new Vector2(1,1), new Vector2(-150, -100), 200, 80);
        btnClose.GetComponent<Image>().color = Color.red;
        btnClose.onClick.AddListener(() => canvasObj.SetActive(false));

        // --- Board Container (10x20) ---
        // Khung chuẩn 600x1200 (60x60 1 ô) để khớp 10x20 tỷ lệ 1:2
        GameObject boardObj = new GameObject("BoardContainer");
        boardObj.transform.SetParent(panelObj.transform, false);
        Image boardBg = boardObj.AddComponent<Image>();
        boardBg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        boardObj.AddComponent<RectMask2D>(); // Che Giấu Điểm Rơi của Tetris (Buffer Zone)
        RectTransform boardRT = boardObj.GetComponent<RectTransform>();
        boardRT.anchorMin = new Vector2(0.5f, 0.5f); boardRT.anchorMax = new Vector2(0.5f, 0.5f);
        boardRT.pivot = new Vector2(0, 0); // Đặt pivot về góc dưới trái
        boardRT.anchoredPosition = new Vector2(-300, -500); // Đưa tâm khung về giữa
        boardRT.sizeDelta = new Vector2(600, 1200); // 10x20 chuẩn

        // Tạo Block Prefab
        GameObject blockPrefab = new GameObject("TetrisBlockPrefab");
        blockPrefab.SetActive(false);
        blockPrefab.transform.SetParent(canvasObj.transform, false);
        Image blockImg = blockPrefab.AddComponent<Image>();
        RectTransform bRt = blockPrefab.GetComponent<RectTransform>();
        bRt.sizeDelta = new Vector2(60, 60); // 600 / 10 = 60
        bRt.pivot = new Vector2(0,0);
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.zero;

        // --- Quests / Orders Panel (Thay thế Nút Điều Khiển) ---
        // Nằm ngang bên dưới
        GameObject questPanel = new GameObject("Panel_Quests");
        questPanel.transform.SetParent(panelObj.transform, false);
        Image qBg = questPanel.AddComponent<Image>();
        qBg.color = new Color(0, 0, 0, 0.8f);
        RectTransform qRt = questPanel.GetComponent<RectTransform>();
        qRt.anchorMin = new Vector2(0.1f, 0); qRt.anchorMax = new Vector2(0.9f, 0);
        qRt.sizeDelta = new Vector2(0, 200); // Cao 200px
        qRt.anchoredPosition = new Vector2(0, 150); // Cách đáy 150px
        
        Text questTitle = CreateText(questPanel.transform, "Title", "ĐƠN HÀNG CẦN GIAO LÁI BUÔN", 22, true, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30));
        questTitle.color = Color.yellow;

        Text questTxt = CreateText(questPanel.transform, "QuestText", "Đang tải...", 20, false, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20));
        questTxt.color = Color.white;
        questTxt.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 120);

        // --- Gắn references ---
        TetrisManager manager = canvasObj.AddComponent<TetrisManager>();
        manager.boardContainer = boardRT;
        manager.blockPrefab = blockImg;
        manager.inventoryText = invTxt;
        manager.questText = questTxt;

        // --- Nút Start nhỏ gọn ---
        Button btnStartRow = CreateButton(panelObj.transform, "BtnStart", "BẮT ĐẦU CHẠY TETRIS", new Vector2(0.5f, 1), new Vector2(0, -90), 200, 50);
        btnStartRow.GetComponent<Image>().color = Color.green;
        btnStartRow.onClick.AddListener(() => manager.StartGame());

        Selection.activeGameObject = canvasObj;
        Debug.Log("Đã tạo UI Tetris Thao Tác Kéo Thả Trực Tiếp thành công!");
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

        if (anchorMin != anchorMax) rt.sizeDelta = Vector2.zero; 
        else rt.sizeDelta = new Vector2(350, 60);

        return txt;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, float width, float height)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = Color.white;
        Button btn = go.AddComponent<Button>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, height);

        Text txt = CreateText(go.transform, "Text", label, 14, true, new Vector2(0,0), new Vector2(1,1), Vector2.zero);
        txt.color = Color.black;
        return btn;
    }
}
