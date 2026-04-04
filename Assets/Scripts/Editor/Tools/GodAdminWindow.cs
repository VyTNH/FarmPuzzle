using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FarmPuzzle.Core.Database;
using FarmPuzzle.EditorTools;

public class GodAdminWindow : EditorWindow
{
    private int selectedTab = 0;
    private string[] tabs = { "Trang Chủ", "Nông Trại", "Mở Đất", "Tetris Xuất Hàng" };
    
    private int subTab = 0;
    private string[] subTabs = { "1. Khởi Tạo Dữ Liệu", "2. Quản Lý Trạng Thái", "3. Setup Scene" };

    // Log captures
    private string logBuffer = "";
    private Vector2 scrollPos;

    [MenuItem("FarmPuzzle/👑 God Admin Board")]
    public static void ShowWindow()
    {
        var window = GetWindow<GodAdminWindow>("God Admin");
        window.minSize = new Vector2(900, 600);
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Warning) return; // Bỏ qua warning giùm cho rảnh rỗi mắt
        logBuffer += $"[{System.DateTime.Now:HH:mm:ss}] {logString}\n";
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        
        // --- SIDEBAR ---
        GUILayout.BeginVertical("box", GUILayout.Width(200), GUILayout.ExpandHeight(true));
        GUILayout.Label("FARM PUZZLE ADMIN", EditorStyles.boldLabel);
        GUILayout.Space(20);
        
        for (int i = 0; i < tabs.Length; i++)
        {
            if (selectedTab == i) GUI.backgroundColor = Color.green;
            else GUI.backgroundColor = Color.white;
            
            if (GUILayout.Button(tabs[i], GUILayout.Height(40)))
            {
                selectedTab = i;
                subTab = 0; // reset sub tab
            }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndVertical();

        // --- CONTENT ---
        GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        
        if (selectedTab != 0) // Các tab gameplay
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < subTabs.Length; i++)
            {
                if (subTab == i) GUI.backgroundColor = Color.cyan;
                else GUI.backgroundColor = Color.white;

                if (GUILayout.Button(subTabs[i], GUILayout.Height(30))) subTab = i;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            
            DrawSubTabContent();
        }
        else
        {
            DrawDashboard();
        }

        GUILayout.FlexibleSpace(); // Cục đẩy
        
        // --- REALTIME LOG VIEWER ---
        GUILayout.BeginVertical("box", GUILayout.Height(150));
        GUILayout.BeginHorizontal();
        GUILayout.Label("🖥️ REAL-TIME BATTLE LOG", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear", GUILayout.Width(60))) logBuffer = "";
        GUILayout.EndHorizontal();

        scrollPos = GUILayout.BeginScrollView(scrollPos);
        GUILayout.TextArea(logBuffer, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.EndVertical(); // End content
        GUILayout.EndHorizontal(); // End main
    }

    private void DrawDashboard()
    {
        GUILayout.Label("Trang Chủ & Dữ Liệu User", EditorStyles.largeLabel);
        GUILayout.Space(10);
        if (GUILayout.Button("Mở Database Window (Cũ)", GUILayout.Height(40)))
        {
            DatabaseTesterWindow.ShowWindow();
        }
        
        GUILayout.Space(20);
        EditorGUILayout.HelpBox("God Admin Board Version 1.0\nHệ thống thống nhất khởi tạo, quản lý và layout cho FarmPuzzle.", MessageType.Info);
        if (GUILayout.Button("Xóa Log Rác"))
        {
            logBuffer = "";
        }
    }

    private void DrawSubTabContent()
    {
        switch (selectedTab)
        {
            case 1: // Nông Trại
                DrawFarmTab();
                break;
            case 2: // Mở Đất
                DrawLandPuzzleTab();
                break;
            case 3: // Tetris
                DrawTetrisTab();
                break;
        }
    }

    // --- TETRIS (Tab 3) ---
    private void DrawTetrisTab()
    {
        GUILayout.Label("🧩 CỤM QUẢN LÝ TETRIS", EditorStyles.boldLabel);
        switch (subTab)
        {
            case 0: // Khởi Tạo
                GUILayout.Label("Tạo Bản Đồ Màu / Đổi Màu Nông Sản", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Phần này quản lý ánh xạ màu từ ProductID -> Tetris Block.", MessageType.Info);
                if (GUILayout.Button("Mở Cấu Hình Màu (TetrisCropMapSO)", GUILayout.Height(40)))
                {
                    TetrisCropColorConfigurator.ShowWindow();
                }
                break;
            
            case 1: // Quản Lý
                GUILayout.Label("Live Dashboard Tetris", EditorStyles.boldLabel);
                
                var tetrisMgr = FindObjectOfType<FarmPuzzle.Tetris.TetrisManager>();
                if (tetrisMgr == null) {
                    EditorGUILayout.HelpBox("CHƯA BẬT GAME HOẶC CHƯA MỞ TETRIS!\nHãy ấn Play và đảm bảo Canvas Tetris đang bật để theo dõi dữ liệu.", MessageType.Warning);
                } else {
                    EditorGUILayout.HelpBox("ĐANG KẾT NỐI VỚI GAMEPLAY TETRIS CHẠY NGẦM...", MessageType.Info);
                    if (Application.isPlaying) {
                        EditorGUILayout.LabelField("Khối Đang Rơi:", tetrisMgr.currentProductID);
                        
                        if (GUILayout.Button("🔥 BUỘC ĐỔI KHỐI MỚI NGAY", GUILayout.Height(30))) {
                            tetrisMgr.SpawnPiece();
                        }

                        GUILayout.Space(10);
                        GUILayout.Label("Trạng Thái Đơn Hàng (Quest System):", EditorStyles.boldLabel);
                        if (FarmPuzzle.Meta.QuestManager.Instance != null && FarmPuzzle.Meta.QuestManager.Instance.activeQuests.Count > 0) {
                            foreach (var q in FarmPuzzle.Meta.QuestManager.Instance.activeQuests) {
                                float prog = FarmPuzzle.Meta.QuestManager.Instance.GetQuestProgress(q.questID);
                                EditorGUILayout.LabelField($"Đơn: {q.targetItemID}", $"{prog} / {q.targetAmount}");
                            }
                        } else {
                            EditorGUILayout.LabelField("=> Hệ thống chưa nạp Đơn hàng nào.");
                        }
                    }
                }
                break;
            
            case 2: // Setup Scene
                GUILayout.Label("Bố trí Màn Hình (Canvas Setup)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Hành động này sẽ Xóa Canvas Menu cũ, Đẻ ra Cục Canvas Mới Toanh.", MessageType.Warning);
                if (GUILayout.Button("TẠO UI TETRIS XUẤT HÀNG", GUILayout.Height(60)))
                {
                    TetrisUICreator.CreateTetrisUI();
                }
                break;
        }
    }

    // --- NÔNG TRẠI (Tab 1) ---
    private void DrawFarmTab()
    {
        GUILayout.Label("🍅 CỤM QUẢN LÝ NÔNG TRẠI", EditorStyles.boldLabel);
        switch(subTab)
        {
            case 0:
                if (GUILayout.Button("Mở Trình Quét File Database (Gen ScriptableObjects)"))
                    DatabaseSOGenerator.ShowWindow();
                break;
            case 1:
                EditorGUILayout.HelpBox("God Tool: Tra rà, thay đổi Seed, ép chín ô đất thẳng trên Database...", MessageType.Info);
                if (GUILayout.Button("MỞ TRÌNH QUẢN LÝ Ô ĐẤT (Farm Tile Realtime)", GUILayout.Height(40)))
                    FarmTileInspector.ShowWindow();
                break;
            case 2:
                if (GUILayout.Button("TẠO MỚI CANVAS UI SANDBOX (NÔNG TRẠI)", GUILayout.Height(60)))
                    SandboxUICreator.CreateSandboxUI();
                break;
        }
    }

    // --- MỞ ĐẤT (Tab 2) ---
    private void DrawLandPuzzleTab()
    {
        GUILayout.Label("⛏️ CỤM QUẢN LÝ MỞ ĐẤT PUZZLE", EditorStyles.boldLabel);
        switch(subTab)
        {
            case 0:
                EditorGUILayout.HelpBox("Sinh cấu trúc màn chơi...", MessageType.Info);
                break;
            case 1:
                EditorGUILayout.HelpBox("Bơm Energy, Quản lý vật cản...", MessageType.Info);
                break;
            case 2:
                if (GUILayout.Button("AUTO SETUP LAND PUZZLE UI", GUILayout.Height(60)))
                    AutoSetupLandPuzzleUI.SetupUI();
                break;
        }
    }
}
