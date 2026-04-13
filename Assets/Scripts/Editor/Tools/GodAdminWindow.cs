using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;
using FarmPuzzle.Core.Database;
using FarmPuzzle.FarmSystem.Crop;
using FarmPuzzle.EditorTools;

/// <summary>
/// GOD ADMIN WINDOW v2 — Cửa sổ quản trị thống nhất toàn bộ dự án FarmPuzzle.
/// Menu: FarmPuzzle > 👑 God Admin Board
/// Bao gồm: Farm, Block Puzzle, Tetris, UI Builder, Scene Diagnostics
/// </summary>
public class GodAdminWindow : EditorWindow
{
    // ─── TAB STATE ───
    private int _tab = 0;
    private readonly string[] _tabNames = {
        "🏠 Dashboard",
        "🌾 Farm System",
        "⛏️ Block Puzzle",
        "🧩 Tetris",
        "📦 UI Builder",
        "🗄️ DB Sync",
        "🔬 Diagnostics"
    };

    // ─── DB SYNC TAB STATE ───
    private SQLiteConnection _syncDb;
    private List<SeedItemModel>     _syncSeeds = new();
    private List<CropDataModel>     _syncCrops = new();
    private Vector2 _syncScroll;
    private string  _syncLog = "Ready. Bấm 'Scan DB' để bắt đầu.";
    private bool    _syncAutoMove = true;

    private Vector2 _scroll;
    private string  _logBuffer = "";

    // ─── FARM TAB STATE ───
    private int _farmEnergy = 5;
    private CropNeedType _farmCareType = CropNeedType.None;

    // ─── UI BUILDER STATE ───
    private bool _uiDestroyOld = true;



    [MenuItem("FarmPuzzle/👑 God Admin Board %#g")]
    public static void ShowWindow()
    {
        var w = GetWindow<GodAdminWindow>("👑 God Admin v2");
        w.minSize = new Vector2(860, 620);
    }

    private void OnEnable()  => Application.logMessageReceived += CaptureLog;
    private void OnDisable() => Application.logMessageReceived -= CaptureLog;

    private void CaptureLog(string msg, string stack, LogType type)
    {
        if (type == LogType.Warning) return;
        string icon = type == LogType.Error ? "❌" : "✔";
        _logBuffer += $"[{System.DateTime.Now:HH:mm:ss}] {icon} {msg}\n";
        if (_logBuffer.Length > 8000) _logBuffer = _logBuffer.Substring(_logBuffer.Length - 6000);
    }

    private void OnGUI()
    {
        DrawHeader();
        GUILayout.BeginHorizontal();
        DrawSidebar();
        DrawContent();
        GUILayout.EndHorizontal();
        DrawLogPanel();
    }

    // ══════════════════════════════════════════
    //  HEADER
    // ══════════════════════════════════════════
    private void DrawHeader()
    {
        var style = new GUIStyle(EditorStyles.toolbar);
        style.fixedHeight = 32;
        GUILayout.BeginHorizontal(style);
        GUILayout.Label("  👑  FARM PUZZLE — GOD ADMIN v2", new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });
        GUILayout.FlexibleSpace();
        string playStatus = Application.isPlaying ? "🟢 PLAY MODE" : "⬜ EDIT MODE";
        GUILayout.Label(playStatus, EditorStyles.miniLabel);
        GUILayout.Space(10);
        GUILayout.EndHorizontal();
    }

    // ══════════════════════════════════════════
    //  SIDEBAR
    // ══════════════════════════════════════════
    private void DrawSidebar()
    {
        GUILayout.BeginVertical("box", GUILayout.Width(170), GUILayout.ExpandHeight(true));
        GUILayout.Space(6);
        for (int i = 0; i < _tabNames.Length; i++)
        {
            bool active = _tab == i;
            GUI.backgroundColor = active ? new Color(0.3f, 0.8f, 0.4f) : Color.white;
            var s = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = active ? FontStyle.Bold : FontStyle.Normal, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 4, 4, 4) };
            if (GUILayout.Button(_tabNames[i], s, GUILayout.Height(38))) _tab = i;
        }
        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        GUILayout.Label("v2.0 — " + System.DateTime.Now.ToString("HH:mm"), EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(4);
        GUILayout.EndVertical();
    }

    // ══════════════════════════════════════════
    //  CONTENT AREA
    // ══════════════════════════════════════════
    private void DrawContent()
    {
        GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        _scroll = GUILayout.BeginScrollView(_scroll);

        switch (_tab)
        {
            case 0: DrawDashboard();    break;
            case 1: DrawFarmTab();      break;
            case 2: DrawPuzzleTab();    break;
            case 3: DrawTetrisTab();    break;
            case 4: DrawUIBuilder();    break;
            case 5: DrawDBSync();       break;
            case 6: DrawDiagnostics();  break;
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    // ══════════════════════════════════════════
    //  LOG PANEL (dưới cùng)
    // ══════════════════════════════════════════
    private void DrawLogPanel()
    {
        GUILayout.BeginVertical("box", GUILayout.Height(130));
        GUILayout.BeginHorizontal();
        GUILayout.Label("📋 REAL-TIME LOG", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear", GUILayout.Width(55))) _logBuffer = "";
        GUILayout.EndHorizontal();
        GUILayout.TextArea(_logBuffer, GUILayout.ExpandHeight(true));
        GUILayout.EndVertical();
    }

    // ══════════════════════════════════════════
    // TAB 0 — DASHBOARD
    // ══════════════════════════════════════════
    private void DrawDashboard()
    {
        H2("🏠 Tổng Quan Dự Án");
        EditorGUILayout.HelpBox(
            "God Admin v2 — Cửa sổ thống nhất toàn bộ công cụ FarmPuzzle.\n" +
            "• Farm System  •  Block Puzzle  •  Tetris  •  UI Builder  •  Diagnostics",
            MessageType.Info);

        GUILayout.Space(10);
        H3("Công Cụ Cũ (Legacy)");
        if (Btn("Mở Database Tester Window")) DatabaseTesterWindow.ShowWindow();
        if (Btn("Mở Farm Tile Inspector"))    FarmTileInspector.ShowWindow();
        if (Btn("Mở Database SO Generator"))  DatabaseSOGenerator.ShowWindow();
        if (Btn("Mở Tetris Color Config"))    TetrisCropColorConfigurator.ShowWindow();

        GUILayout.Space(10);
        H3("Thao Tác Nhanh (Quick Actions)");
        GUILayout.BeginHorizontal();
        if (Btn("▶ Play", 80))  EditorApplication.isPlaying = true;
        if (Btn("■ Stop", 80))  EditorApplication.isPlaying = false;
        if (Btn("🔄 Refresh", 95)) { AssetDatabase.Refresh(); Debug.Log("[Admin] Refreshed assets."); }
        if (Btn("🗑 Clear Log", 95)) _logBuffer = "";
        GUILayout.EndHorizontal();
    }

    // ══════════════════════════════════════════
    // TAB 1 — FARM SYSTEM
    // ══════════════════════════════════════════
    private void DrawFarmTab()
    {
        H2("🌾 Farm System");

        // ── Scene Status ──
        H3("Trạng Thái Scene");
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Cần ở PLAY MODE để kiểm tra runtime state.", MessageType.Warning);
        }
        else
        {
            var gm = FindFirstObjectByType<FarmPuzzle.FarmSystem.GridManager>();
            if (gm == null) { EditorGUILayout.HelpBox("Không tìm thấy GridManager!", MessageType.Error); }
            else
            {
                int locked = 0, occ = 0, empty = 0;
                foreach (var p in gm.plots)
                {
                    if (p.isLocked) locked++;
                    else if (p.isOccupied) occ++;
                    else empty++;
                }
                GUILayout.BeginHorizontal();
                StatBox("Bị Khóa", locked.ToString(), Color.gray);
                StatBox("Đang Trồng", occ.ToString(), new Color(0.2f, 0.7f, 0.2f));
                StatBox("Trống", empty.ToString(), new Color(0.8f, 0.8f, 0.2f));
                GUILayout.EndHorizontal();
            }
        }

        // ── Energy Control ──
        GUILayout.Space(8);
        H3("Energy System");
        var es = FarmPuzzle.LandPuzzle.EnergySystem.Instance;
        if (es != null)
        {
            EditorGUILayout.LabelField("Energy Hiện Tại:", $"{es.CurrentEnergy} / {es.MaxEnergy}");
            GUILayout.BeginHorizontal();
            _farmEnergy = EditorGUILayout.IntSlider("Số Lượng Thêm:", _farmEnergy, 1, 20);
            if (Btn("+ Thêm Energy", 100))
            {
                es.AddEnergy(_farmEnergy);
                Debug.Log($"[Admin] Added {_farmEnergy} energy. Now: {es.CurrentEnergy}");
            }
            if (Btn("Refill Max", 80)) es.RefillAll();
            GUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("EnergySystem chưa khởi động (cần Play Mode).", MessageType.Info);
            if (Btn("Test Thêm Energy (Runtime)"))
            {
                if (Application.isPlaying) Debug.Log("[Admin] EnergySystem.Instance null!");
            }
        }

        // ── Care Tool Selector ──
        GUILayout.Space(8);
        H3("Tool Chăm Sóc — Chọn & Áp Dụng");
        EditorGUILayout.HelpBox(
            "FIX T2: Vấn đề nhấn nhiều lần mới nhận: do collider isTrigger chặn raycast.\n" +
            "Giải pháp: Bỏ isTrigger cho BoxCollider2D của LandPlot.",
            MessageType.Warning);

        _farmCareType = (CropNeedType)EditorGUILayout.EnumPopup("Tool Đang Chọn:", _farmCareType);
        if (Btn("Áp Dụng Care Cho TẤT CẢ Ô Có Nhu Cầu (Debug)"))
        {
            if (Application.isPlaying)
            {
                var plots = FindObjectsByType<LandPlot>(FindObjectsSortMode.None);
                int count = 0;
                foreach (var p in plots)
                {
                    if (p.isOccupied && p.currentNeed != CropNeedType.None)
                    {
                        p.ApplyCare(p.currentNeed);
                        count++;
                    }
                }
                Debug.Log($"[Admin] Áp dụng care cho {count} ô đất.");
            }
        }

        // ── FIX: Collider isTrigger ──
        GUILayout.Space(8);
        H3("🔧 Fix T2 — Collider LandPlot");
        EditorGUILayout.HelpBox(
            "BoxCollider2D.isTrigger = true sẽ bị EventSystem ignore trong một số trường hợp.\n" +
            "Bấm nút bên dưới để đặt tất cả LandPlot collider về isTrigger = false và đổi Layer thành 'Interactable'.",
            MessageType.Info);
        if (Btn("🔧 Fix Collider isTrigger (All LandPlots)", height: 35))
        {
            var plots = FindObjectsByType<LandPlot>(FindObjectsSortMode.None);
            int fixed2 = 0;
            foreach (var p in plots)
            {
                var col = p.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    Undo.RecordObject(col, "Fix isTrigger");
                    col.isTrigger = false;
                    fixed2++;
                }
            }
            Debug.Log($"[Admin] Fixed isTrigger on {fixed2} LandPlots.");
        }
    }

    // ══════════════════════════════════════════
    // TAB 2 — BLOCK PUZZLE
    // ══════════════════════════════════════════
    private void DrawPuzzleTab()
    {
        H2("⛏️ Block Puzzle (Land Puzzle)");

        // ── Diagnose T8: Tại sao click ô đất không mở popup ──
        H3("🔍 Chẩn đoán T8 — Click ô đất không mở Popup");
        EditorGUILayout.HelpBox(
            "Nguyên nhân T8:\n" +
            "GridManager.HandleInteractionAtPos() gọi LandPuzzlePopupController.Instance.ShowConfirmPopup()\n" +
            "→ Nếu LandPuzzlePopupController không có trong scene → KHÔNG CÓ GÌ XẢY RA!\n\n" +
            "Giải pháp: Bấm 'Tạo Popup Controller' bên dưới.",
            MessageType.Warning);

        var popupCtrl = FindFirstObjectByType<FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController>();
        if (popupCtrl != null)
        {
            EditorGUILayout.HelpBox("✅ LandPuzzlePopupController đang có trong scene: " + popupCtrl.gameObject.name, MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("❌ LandPuzzlePopupController KHÔNG CÓ trong scene! Đây là nguyên nhân T8.", MessageType.Error);
            if (Btn("🔧 Tạo LandPuzzlePopupController + Popup UI", height: 40))
                CreateLandPuzzlePopupUI();
        }

        GUILayout.Space(8);

        // ── Check LandPuzzleManager ──
        H3("LandPuzzleManager");
        var pm = FindFirstObjectByType<FarmPuzzle.LandPuzzle.LandPuzzleManager>();
        if (pm == null)
        {
            EditorGUILayout.HelpBox("Không có LandPuzzleManager trong scene!", MessageType.Error);
            if (Btn("Tạo LandPuzzleManager Object", height: 35))
            {
                var go = new GameObject("LandPuzzleManager");
                go.AddComponent<FarmPuzzle.LandPuzzle.LandPuzzleManager>();
                Undo.RegisterCreatedObjectUndo(go, "Create LandPuzzleManager");
                Debug.Log("[Admin] Created LandPuzzleManager.");
            }
        }
        else
        {
            EditorGUILayout.HelpBox($"✅ LandPuzzleManager: {pm.gameObject.name} | State: {pm.CurrentState}", MessageType.Info);
            if (Application.isPlaying)
            {
                GUILayout.BeginHorizontal();
                if (Btn("Force Exit Puzzle")) pm.ExitPuzzle();
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(8);
        H3("Energy Check trước khi mở puzzle");
        var es = FarmPuzzle.LandPuzzle.EnergySystem.Instance;
        if (es != null)
        {
            EditorGUILayout.LabelField("Energy:", $"{es.CurrentEnergy}/{es.MaxEnergy}");
            if (Btn("+ Thêm 3 Energy (Test)")) { es.AddEnergy(3); }
        }

        GUILayout.Space(8);
        H3("Setup Scene Block Puzzle");
        if (Btn("AUTO SETUP LAND PUZZLE UI (AutoSetupLandPuzzleUI)", height: 40))
            AutoSetupLandPuzzleUI.SetupUI();
    }

    // ══════════════════════════════════════════
    // TAB 3 — TETRIS
    // ══════════════════════════════════════════
    private void DrawTetrisTab()
    {
        H2("🧩 Tetris Xuất Hàng");

        H3("Bản Đồ Màu (T14 Fix)");
        EditorGUILayout.HelpBox(
            "T14: Khối màu trắng → TetrisCropMapSO chưa được assign vào TetrisManager.\n" +
            "Bấm nút dưới để mở cấu hình màu.",
            MessageType.Warning);
        if (Btn("Mở Bản Đồ Màu (TetrisCropColorConfigurator)"))
            TetrisCropColorConfigurator.ShowWindow();

        GUILayout.Space(8);

        // ── Live dashboard ──
        H3("Live Dashboard Tetris");
        var tm = FindFirstObjectByType<FarmPuzzle.Tetris.TetrisManager>();
        if (tm == null)
        {
            EditorGUILayout.HelpBox("TetrisManager chưa active. Cần Play Mode và mở màn Tetris.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField("Khối Hiện Tại:", tm.currentProductID);
            if (Application.isPlaying)
            {
                if (Btn("🔥 Spawn Khối Mới Ngay")) tm.SpawnPiece();
                GUILayout.Space(5);
                var qm = FarmPuzzle.Meta.QuestManager.Instance;
                if (qm != null && qm.activeQuests.Count > 0)
                {
                    H3("Đơn Hàng Đang Chờ (Quest)");
                    foreach (var q in qm.activeQuests)
                    {
                        float prog = qm.GetQuestProgress(q.questID);
                        EditorGUILayout.LabelField($"  • {q.targetItemID}", $"{prog} / {q.targetAmount}");
                    }
                }
                else EditorGUILayout.LabelField("Chưa có đơn hàng nào.");
            }
        }

        GUILayout.Space(8);
        H3("Setup Scene Tetris");
        if (Btn("TẠO UI TETRIS (TetrisUICreator)", height: 40)) TetrisUICreator.CreateTetrisUI();

        GUILayout.Space(8);
        H3("Missing UI Fixes");
        EditorGUILayout.HelpBox(
            "T17: Thiếu Game Over Popup\n" +
            "T16: Quest UI chỉ có background, chưa bind data\n" +
            "T15: Không có hiệu ứng khi phá hàng\n\n" +
            "→ Bấm nút dưới để tạo các UI này vào scene.",
            MessageType.Warning);
        if (Btn("🔧 Tạo Tetris Game Over Popup", height: 35)) CreateTetrisGameOverPopup();
    }

    // ══════════════════════════════════════════
    // TAB 4 — UI BUILDER
    // ══════════════════════════════════════════
    private void DrawUIBuilder()
    {
        H2("📦 UI Builder — Tạo GameObject UI Vào Scene");
        EditorGUILayout.HelpBox(
            "Mỗi nút bên dưới sẽ tạo ra một cụm GameObject + Component + Canvas đầy đủ vào scene hiện tại.\n" +
            "Không cần kéo thả Prefab thủ công. Nhấn 1 cái là xong.",
            MessageType.Info);

        _uiDestroyOld = EditorGUILayout.Toggle("Xóa cũ trước khi tạo mới", _uiDestroyOld);
        GUILayout.Space(8);

        H3("Farm UI");
        if (Btn("📦 Tạo UI Kho Hàng (Inventory Panel)", height: 40)) CreateInventoryUI();
        if (Btn("🌾 Tạo Sandbox Canvas (Farm Controls)", height: 35)) SandboxUICreator.CreateSandboxUI();

        GUILayout.Space(8);
        H3("Block Puzzle UI");
        if (Btn("⛏️ Tạo Popup Xác Nhận Mở Đất (LandPuzzlePopup)", height: 40)) CreateLandPuzzlePopupUI();
        if (Btn("🔧 Auto Setup Toàn Bộ Land Puzzle UI", height: 35)) AutoSetupLandPuzzleUI.SetupUI();

        GUILayout.Space(8);
        H3("Tetris UI");
        if (Btn("🧩 Tạo Tetris Game Over Popup", height: 35)) CreateTetrisGameOverPopup();
        if (Btn("📋 Tạo Quest HUD (bottom bar)", height: 35)) CreateQuestHUD();
        if (Btn("✨ Tạo Line Clear VFX Text", height: 35)) CreateLineClearVFX();
        if (Btn("🎮 Tạo Toàn Bộ Tetris UI (All-in-One)", height: 50)) TetrisUICreator.CreateTetrisUI();

        GUILayout.Space(8);
        H3("Camera & EventSystem");
        if (Btn("Tạo Camera Fitter + EventSystem"))
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
                Debug.Log("[Admin] Created EventSystem.");
            }
            else Debug.Log("[Admin] EventSystem already exists.");
        }
    }

    // ══════════════════════════════════════════
    // TAB 5 — DIAGNOSTICS
    // ══════════════════════════════════════════
    private void DrawDiagnostics()
    {
        H2("🔬 Scene Diagnostics");

        bool hasEventSystem = FindFirstObjectByType<EventSystem>() != null;
        bool hasCamera = Camera.main != null;
        bool hasGridManager = FindFirstObjectByType<FarmPuzzle.FarmSystem.GridManager>() != null;
        bool hasLandPuzzleManager = FindFirstObjectByType<FarmPuzzle.LandPuzzle.LandPuzzleManager>() != null;
        bool hasPopupController = FindFirstObjectByType<FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController>() != null;
        bool hasEnergySystem = FindFirstObjectByType<FarmPuzzle.LandPuzzle.EnergySystem>() != null;
        bool hasTetrisManager = FindFirstObjectByType<FarmPuzzle.Tetris.TetrisManager>() != null;
        bool hasDataManager = FindFirstObjectByType<DataManager>() != null;

        CheckRow("EventSystem",               hasEventSystem);
        CheckRow("Camera.main",               hasCamera);
        CheckRow("GridManager",               hasGridManager);
        CheckRow("LandPuzzleManager",         hasLandPuzzleManager);
        CheckRow("LandPuzzlePopupController", hasPopupController);
        CheckRow("EnergySystem",              hasEnergySystem);
        CheckRow("TetrisManager",             hasTetrisManager);
        CheckRow("DataManager",               hasDataManager);

        GUILayout.Space(12);
        H3("LandPlot Colliders (T2 Fix Status)");
        var plots = FindObjectsByType<LandPlot>(FindObjectsSortMode.None);
        int triggerCount = 0;
        foreach (var p in plots)
        {
            var col = p.GetComponent<BoxCollider2D>();
            if (col != null && col.isTrigger) triggerCount++;
        }
        if (triggerCount > 0)
            EditorGUILayout.HelpBox($"⚠️ {triggerCount}/{plots.Length} LandPlot có isTrigger=true → Có thể gây miss click!\nVào tab Farm System → sửa.", MessageType.Warning);
        else
            EditorGUILayout.HelpBox($"✅ Tất cả {plots.Length} LandPlot: isTrigger=false → OK!", MessageType.Info);

        GUILayout.Space(8);
        if (Btn("🔄 Refresh Diagnostics")) Repaint();
    }

    // ══════════════════════════════════════════
    //  UI CREATOR FUNCTIONS
    // ══════════════════════════════════════════

    /// <summary>T8/T9 Fix: Tạo LandPuzzlePopupController nếu chưa có.</summary>
    private static void CreateLandPuzzlePopupUI()
    {
        if (FindFirstObjectByType<FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController>() != null)
        {
            Debug.Log("[Admin] LandPuzzlePopupController đã tồn tại trong scene.");
            return;
        }

        // Tìm canvas gốc hoặc tạo mới
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasGo = new GameObject("Canvas_Main");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
        }

        // Panel overlay tối
        var overlay = new GameObject("LandPuzzle_Popup");
        overlay.transform.SetParent(canvas.transform, false);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.6f);
        SetFullscreen(overlay.GetComponent<RectTransform>());

        // Popup box
        var box = new GameObject("PopupBox");
        box.transform.SetParent(overlay.transform, false);
        var boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.15f, 0.12f, 0.1f, 0.97f);
        var boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(480, 260);

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(box.transform, false);
        var titleTxt = titleGo.AddComponent<Text>();
        titleTxt.text = "⛏️ Mở Khóa Ô Đất";
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 22; titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = Color.white;
        var titleRT = titleGo.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.65f); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        // Message
        var msgGo = new GameObject("Message");
        msgGo.transform.SetParent(box.transform, false);
        var msgTxt = msgGo.AddComponent<Text>();
        msgTxt.text = "Mở đất này tốn 1 ⚡ Năng lượng.\nBạn có muốn tiếp tục không?";
        msgTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        msgTxt.fontSize = 16; msgTxt.alignment = TextAnchor.MiddleCenter;
        msgTxt.color = new Color(0.9f, 0.85f, 0.7f);
        var msgRT = msgGo.GetComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0.05f, 0.3f); msgRT.anchorMax = new Vector2(0.95f, 0.65f);
        msgRT.offsetMin = msgRT.offsetMax = Vector2.zero;

        // OK Button
        var okGo = new GameObject("Btn_OK");
        okGo.transform.SetParent(box.transform, false);
        var okImg = okGo.AddComponent<Image>();
        okImg.color = new Color(0.2f, 0.7f, 0.2f);
        var okBtn = okGo.AddComponent<Button>();
        var okCB = new ColorBlock();
        okCB.normalColor = new Color(0.2f, 0.7f, 0.2f);
        okCB.highlightedColor = new Color(0.3f, 0.9f, 0.3f);
        okCB.pressedColor = new Color(0.1f, 0.5f, 0.1f);
        okCB.colorMultiplier = 1f; okCB.fadeDuration = 0.1f;
        okBtn.colors = okCB;
        var okRT = okGo.GetComponent<RectTransform>();
        okRT.anchorMin = new Vector2(0.05f, 0.05f); okRT.anchorMax = new Vector2(0.48f, 0.28f);
        okRT.offsetMin = okRT.offsetMax = Vector2.zero;
        var okTxtGo = new GameObject("Label"); okTxtGo.transform.SetParent(okGo.transform, false);
        var okTxt = okTxtGo.AddComponent<Text>();
        okTxt.text = "✅ Mở Đất"; okTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        okTxt.fontSize = 16; okTxt.fontStyle = FontStyle.Bold;
        okTxt.alignment = TextAnchor.MiddleCenter; okTxt.color = Color.white;
        SetFullscreen(okTxtGo.GetComponent<RectTransform>());

        // Cancel Button
        var cancelGo = new GameObject("Btn_Cancel");
        cancelGo.transform.SetParent(box.transform, false);
        var cancelImg = cancelGo.AddComponent<Image>();
        cancelImg.color = new Color(0.7f, 0.2f, 0.2f);
        cancelGo.AddComponent<Button>();
        var cancelRT = cancelGo.GetComponent<RectTransform>();
        cancelRT.anchorMin = new Vector2(0.52f, 0.05f); cancelRT.anchorMax = new Vector2(0.95f, 0.28f);
        cancelRT.offsetMin = cancelRT.offsetMax = Vector2.zero;
        var cancelTxtGo = new GameObject("Label"); cancelTxtGo.transform.SetParent(cancelGo.transform, false);
        var cancelTxt = cancelTxtGo.AddComponent<Text>();
        cancelTxt.text = "❌ Huỷ"; cancelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cancelTxt.fontSize = 16; cancelTxt.fontStyle = FontStyle.Bold;
        cancelTxt.alignment = TextAnchor.MiddleCenter; cancelTxt.color = Color.white;
        SetFullscreen(cancelTxtGo.GetComponent<RectTransform>());

        // Attach Controller và wire references tự động
        var ctrl = overlay.AddComponent<FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("_confirmPopupPanel").objectReferenceValue = overlay;
        so.FindProperty("_btnYes").objectReferenceValue = okBtn;
        so.FindProperty("_btnNo").objectReferenceValue = cancelGo.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();

        overlay.SetActive(false);
        Undo.RegisterCreatedObjectUndo(overlay, "Create LandPuzzle Popup");
        Debug.Log("[Admin] Tao LandPuzzlePopupController thanh cong! _confirmPopupPanel, _btnYes, _btnNo da wire.");
        Selection.activeGameObject = overlay;
    }

    /// <summary>T3/T5/T6 Fix: Tạo UI Kho Hàng cơ bản.</summary>
    private static void CreateInventoryUI()
    {
        if (GameObject.Find("Canvas_Inventory") != null && true)
            DestroyImmediate(GameObject.Find("Canvas_Inventory"));

        var canvasGo = new GameObject("Canvas_Inventory");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Panel dưới cùng
        var panelGo = new GameObject("InventoryPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.08f, 0.06f, 0.88f);
        var panelRT = panelGo.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0, 0);
        panelRT.anchorMax = new Vector2(1, 0);
        panelRT.pivot = new Vector2(0.5f, 0);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(0, 160);

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleTxt = titleGo.AddComponent<Text>();
        titleTxt.text = "📦 KHO HÀNG";
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 18; titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color = new Color(1f, 0.85f, 0.4f);
        titleTxt.alignment = TextAnchor.UpperCenter;
        var titleRT = titleGo.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.65f); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        // Slot container (horizontal layout)
        var slotsGo = new GameObject("SlotContainer");
        slotsGo.transform.SetParent(panelGo.transform, false);
        var layout = slotsGo.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 12;
        layout.padding = new RectOffset(20, 20, 0, 0);
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var slotsRT = slotsGo.GetComponent<RectTransform>();
        slotsRT.anchorMin = new Vector2(0, 0); slotsRT.anchorMax = new Vector2(1, 0.65f);
        slotsRT.offsetMin = slotsRT.offsetMax = Vector2.zero;

        // Tạo 6 slot mẫu
        string[] items = { "🍅", "🫐", "🌽", "🥕", "⚗️", "🐛" };
        string[] labels = { "Cà Chua", "Việt Quất", "Ngô", "Cà Rốt", "Phân", "Thuốc Sâu" };
        for (int i = 0; i < items.Length; i++)
        {
            var slot = new GameObject($"Slot_{i}");
            slot.transform.SetParent(slotsGo.transform, false);
            var slotImg = slot.AddComponent<Image>();
            slotImg.color = new Color(0.2f, 0.18f, 0.15f, 0.9f);
            var slotLayout = slot.AddComponent<LayoutElement>();
            slotLayout.preferredWidth = 100;
            slotLayout.preferredHeight = 90;

            // Icon
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(slot.transform, false);
            var iconTxt = iconGo.AddComponent<Text>();
            iconTxt.text = items[i];
            iconTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconTxt.fontSize = 28; iconTxt.alignment = TextAnchor.UpperCenter;
            iconTxt.color = Color.white;
            var iconRT = iconGo.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0, 0.4f); iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

            // Count
            var countGo = new GameObject("Count");
            countGo.transform.SetParent(slot.transform, false);
            var countTxt = countGo.AddComponent<Text>();
            countTxt.text = "0";
            countTxt.name = $"txt_count_{labels[i].Replace(" ", "")}";
            countTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countTxt.fontSize = 20; countTxt.fontStyle = FontStyle.Bold;
            countTxt.alignment = TextAnchor.LowerCenter;
            countTxt.color = new Color(0.9f, 0.85f, 0.5f);
            var countRT = countGo.GetComponent<RectTransform>();
            countRT.anchorMin = Vector2.zero; countRT.anchorMax = new Vector2(1, 0.4f);
            countRT.offsetMin = countRT.offsetMax = Vector2.zero;
        }

        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Inventory UI");
        Debug.Log("[Admin] ✅ Đã tạo Inventory UI Panel. Cần kết nối Count texts với InventoryDisplay script.");
        Selection.activeGameObject = canvasGo;
    }

    /// <summary>T17 Fix: Tạo Game Over Popup cho Tetris.</summary>
    private static void CreateTetrisGameOverPopup()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>() ?? CreateCanvas("Canvas_TetrisPopup", 999);

        var popupGo = new GameObject("TetrisGameOverPopup");
        popupGo.transform.SetParent(canvas.transform, false);
        var overlayImg = popupGo.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.75f);
        SetFullscreen(popupGo.GetComponent<RectTransform>());

        // Box
        var box = new GameObject("PopupBox"); box.transform.SetParent(popupGo.transform, false);
        var boxImg = box.AddComponent<Image>(); boxImg.color = new Color(0.12f, 0.06f, 0.06f, 0.98f);
        var boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(500, 340);

        MakeText(box.transform, "Title", "💀 GAME OVER", 32, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0, 0.65f), Vector2.one, Color.red);
        MakeText(box.transform, "Message", "Lưới đã đầy!\nNông sản chưa xuất xong sẽ bị giữ lại.",
            16, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.65f), new Color(0.9f, 0.8f, 0.8f));

        MakeButton(box.transform, "Btn_Retry", "🔄 Chơi Lại",
            new Vector2(0.05f, 0.05f), new Vector2(0.48f, 0.28f), new Color(0.2f, 0.5f, 0.9f));
        MakeButton(box.transform, "Btn_Quit", "🚪 Thoát",
            new Vector2(0.52f, 0.05f), new Vector2(0.95f, 0.28f), new Color(0.55f, 0.55f, 0.55f));

        popupGo.SetActive(false);
        Undo.RegisterCreatedObjectUndo(popupGo, "Create Tetris GameOver Popup");
        Debug.Log("[Admin] ✅ Tạo TetrisGameOverPopup. Cần wire vào TetrisManager.OnGameOver event.");
        Selection.activeGameObject = popupGo;
    }

    /// <summary>T16 Fix: Tạo Quest HUD dưới màn hình.</summary>
    private static void CreateQuestHUD()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>() ?? CreateCanvas("Canvas_QuestHUD", 60);

        var hudGo = new GameObject("QuestHUD");
        hudGo.transform.SetParent(canvas.transform, false);
        var hudImg = hudGo.AddComponent<Image>();
        hudImg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        var hudRT = hudGo.GetComponent<RectTransform>();
        hudRT.anchorMin = new Vector2(0, 0); hudRT.anchorMax = new Vector2(1, 0);
        hudRT.pivot = new Vector2(0.5f, 0);
        hudRT.anchoredPosition = Vector2.zero;
        hudRT.sizeDelta = new Vector2(0, 120);

        MakeText(hudGo.transform, "QuestTitle", "📋 ĐƠN HÀNG ĐANG NHẬN", 15, FontStyle.Bold,
            TextAnchor.UpperCenter, new Vector2(0, 0.65f), Vector2.one, new Color(1f, 0.9f, 0.4f));
        MakeText(hudGo.transform, "QuestContent", "Chưa có đơn hàng nào", 13, FontStyle.Normal,
            TextAnchor.MiddleCenter, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.65f), Color.white);

        Undo.RegisterCreatedObjectUndo(hudGo, "Create Quest HUD");
        Debug.Log("[Admin] ✅ Tạo QuestHUD. Cần thêm QuestDisplayController script để bind data từ QuestManager.");
        Selection.activeGameObject = hudGo;
    }

    /// <summary>T15 Fix: Tạo hiệu ứng text khi phá hàng.</summary>
    private static void CreateLineClearVFX()
    {
        var vfxGo = new GameObject("LineClearVFX");
        var rectT = vfxGo.AddComponent<RectTransform>();
        rectT.anchorMin = rectT.anchorMax = new Vector2(0.5f, 0.5f);
        rectT.sizeDelta = new Vector2(300, 80);

        var txt = vfxGo.AddComponent<Text>();
        txt.text = "+100 🎉";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 36; txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(1f, 0.9f, 0f);

        Canvas canvas = FindFirstObjectByType<Canvas>() ?? CreateCanvas("Canvas_VFX", 999);
        vfxGo.transform.SetParent(canvas.transform, false);
        vfxGo.SetActive(false);

        Undo.RegisterCreatedObjectUndo(vfxGo, "Create LineClear VFX");
        Debug.Log("[Admin] ✅ Tạo LineClearVFX Text. Cần gọi SetActive(true) + animation từ TetrisManager.");
        Selection.activeGameObject = vfxGo;
    }

    // ── HELPER METHODS ──

    private static Canvas CreateCanvas(string name, int order)
    {
        var go = new GameObject(name);
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        var s = go.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1080, 1920);
        go.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        return c;
    }

    private static void SetFullscreen(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Text MakeText(Transform parent, string name, string content, int size,
        FontStyle style, TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.fontStyle = style; t.alignment = align; t.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return t;
    }

    private static Button MakeButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = bgColor;
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var lblGo = new GameObject("Label"); lblGo.transform.SetParent(go.transform, false);
        var lbl = lblGo.AddComponent<Text>();
        lbl.text = label; lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lbl.fontSize = 15; lbl.fontStyle = FontStyle.Bold;
        lbl.alignment = TextAnchor.MiddleCenter; lbl.color = Color.white;
        SetFullscreen(lblGo.GetComponent<RectTransform>());
        return btn;
    }

    // ── GUI HELPERS ──
    private bool Btn(string label, int width = 0, int height = 28)
    {
        if (width > 0) return GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(height));
        return GUILayout.Button(label, GUILayout.Height(height));
    }
    private void H2(string t) { GUILayout.Space(6); GUILayout.Label(t, new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 }); GUILayout.Space(4); }
    private void H3(string t) { GUILayout.Space(4); GUILayout.Label(t, EditorStyles.boldLabel); }

    private void StatBox(string label, string value, Color color)
    {
        GUI.backgroundColor = color;
        GUILayout.BeginVertical("box", GUILayout.Height(55));
        GUILayout.Label(value, new GUIStyle(EditorStyles.boldLabel) { fontSize = 22, alignment = TextAnchor.MiddleCenter });
        GUILayout.Label(label, new GUIStyle(EditorStyles.centeredGreyMiniLabel));
        GUILayout.EndVertical();
        GUI.backgroundColor = Color.white;
    }

    private void CheckRow(string name, bool ok)
    {
        string icon = ok ? "✅" : "❌";
        Color c = ok ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.7f, 0.7f);
        GUI.backgroundColor = c;
        GUILayout.BeginHorizontal("box");
        GUILayout.Label($"{icon}  {name}", GUILayout.ExpandWidth(true));
        GUILayout.Label(ok ? "CÓ" : "THIẾU", GUILayout.Width(55));
        GUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;
    }

    // ══════════════════════════════════════════
    //  TAB: DB SYNC
    // ══════════════════════════════════════════
    private void DrawDBSync()
    {
        GUILayout.Label("🗄️ DB SYNC — ScriptableObject Manager", new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });
        EditorGUILayout.HelpBox(
            "Scan SQLite → tự động Generate SeedItemSO / ToolItemSO / CropDataSO vào đúng Resources/.\n" +
            "Tools (hoe, watercan, pest, item_fertilizer) không có SO riêng trong DB → được tạo thủ công từ đây.",
            MessageType.Info);

        GUILayout.Space(6);

        // ── CONTROLS ──
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 Scan DB + Preview", GUILayout.Height(34)))
            SyncScanDB();

        GUI.backgroundColor = new Color(0.3f, 1f, 0.4f);
        if (GUILayout.Button("⚡ Generate ALL Missing SOs", GUILayout.Height(34)))
            SyncGenerateAll();
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("📁 Move SOs → Resources", GUILayout.Height(34)))
            SyncMigrateOldSOs();
        GUILayout.EndHorizontal();

        _syncAutoMove = EditorGUILayout.Toggle("Auto-move SOs sau khi generate", _syncAutoMove);

        GUILayout.Space(6);

        // ── STATUS GRID ──
        if (_syncSeeds.Count > 0 || _syncCrops.Count > 0)
        {
            GUILayout.Label("📊 TRẠNG THÁI SO HIỆN TẠI", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();

            // Cột Seeds
            GUILayout.BeginVertical("box", GUILayout.Width(260));
            GUILayout.Label("🌱 SeedItemSO", EditorStyles.boldLabel);
            foreach (var seed in _syncSeeds)
            {
                string path = $"Assets/Resources/SeedData/SO_Seed_{seed.SeedID}.asset";
                bool exists = File.Exists(Path.Combine(Application.dataPath.Replace("Assets",""), path));
                var c = exists ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.8f, 0.5f);
                GUI.backgroundColor = c;
                GUILayout.BeginHorizontal("box");
                GUILayout.Label(seed.SeedID, GUILayout.ExpandWidth(true));
                GUILayout.Label(exists ? "✅" : "❌ thiếu", GUILayout.Width(65));
                if (!exists && GUILayout.Button("Gen", GUILayout.Width(38)))
                    SyncGenerateSeedSO(seed);
                GUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
            }
            GUILayout.EndVertical();

            // Cột Crops
            GUILayout.BeginVertical("box", GUILayout.Width(260));
            GUILayout.Label("🌾 CropDataSO", EditorStyles.boldLabel);
            foreach (var crop in _syncCrops)
            {
                string path = $"Assets/Resources/CropData/SO_Crop_{crop.ProductID}.asset";
                bool exists = File.Exists(Path.Combine(Application.dataPath.Replace("Assets",""), path));
                var c = exists ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.8f, 0.5f);
                GUI.backgroundColor = c;
                GUILayout.BeginHorizontal("box");
                GUILayout.Label(crop.ProductID, GUILayout.ExpandWidth(true));
                GUILayout.Label(exists ? "✅" : "❌ thiếu", GUILayout.Width(65));
                if (!exists && GUILayout.Button("Gen", GUILayout.Width(38)))
                    SyncGenerateCropSO(crop);
                GUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
            }
            GUILayout.EndVertical();

            // Cột Tools (hardcoded — không có trong DB)
            GUILayout.BeginVertical("box", GUILayout.Width(260));
            GUILayout.Label("🔧 ToolItemSO", EditorStyles.boldLabel);
            string[] tools = { "tool_hoe", "tool_watercan", "tool_pest", "item_fertilizer" };
            string[] toolNames = { "Cái Cuốc", "Bình Tưới", "Thuốc Sâu", "Phân Bón" };
            for (int i = 0; i < tools.Length; i++)
            {
                string path = $"Assets/Resources/ToolData/SO_Tool_{tools[i]}.asset";
                bool exists = File.Exists(Path.Combine(Application.dataPath.Replace("Assets",""), path));
                var c = exists ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.8f, 0.5f);
                GUI.backgroundColor = c;
                GUILayout.BeginHorizontal("box");
                GUILayout.Label(tools[i], GUILayout.ExpandWidth(true));
                GUILayout.Label(exists ? "✅" : "❌ thiếu", GUILayout.Width(65));
                if (!exists && GUILayout.Button("Gen", GUILayout.Width(38)))
                    SyncGenerateToolSO(tools[i], toolNames[i]);
                GUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(6);
        GUILayout.Label("📋 LOG", EditorStyles.boldLabel);
        _syncScroll = GUILayout.BeginScrollView(_syncScroll, GUILayout.Height(120));
        GUILayout.TextArea(_syncLog, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();
    }

    // ── SYNC HELPERS ──

    private void SyncScanDB()
    {
        _syncLog = "[Scan] Đang quét DB...\n";
        try
        {
            string dbPath = Path.Combine(Application.persistentDataPath, "FarmPuzzleDB.db");
            if (!File.Exists(dbPath)) { _syncLog += $"❌ DB không tồn tại: {dbPath}\nPlay game 1 lần để tạo DB."; return; }
            _syncDb?.Close();
            _syncDb = new SQLiteConnection(dbPath);
            _syncSeeds = _syncDb.Table<SeedItemModel>().ToList();
            _syncCrops = _syncDb.Table<CropDataModel>().ToList();
            _syncLog += $"✅ Seeds: {_syncSeeds.Count} | Crops: {_syncCrops.Count}\n";

            int missing = 0;
            foreach (var s in _syncSeeds)
                if (!File.Exists(Path.Combine(Application.dataPath, $"Resources/SeedData/SO_Seed_{s.SeedID}.asset"))) missing++;
            foreach (var c in _syncCrops)
                if (!File.Exists(Path.Combine(Application.dataPath, $"Resources/CropData/SO_Crop_{c.ProductID}.asset"))) missing++;
            string[] tools = { "tool_hoe", "tool_watercan", "tool_pest", "item_fertilizer" };
            foreach (var t in tools)
                if (!File.Exists(Path.Combine(Application.dataPath, $"Resources/ToolData/SO_Tool_{t}.asset"))) missing++;
            _syncLog += $"⚠️ Thiếu {missing} SO file(s). Bấm 'Generate ALL' để tạo tự động.";
            Repaint();
        }
        catch (System.Exception e) { _syncLog += $"❌ Lỗi: {e.Message}"; }
    }

    private void SyncGenerateAll()
    {
        if (_syncDb == null) SyncScanDB();
        if (_syncDb == null) return;

        _syncLog = "[Generate ALL]\n";
        int count = 0;

        foreach (var seed in _syncSeeds)
            if (SyncGenerateSeedSO(seed)) count++;
        foreach (var crop in _syncCrops)
            if (SyncGenerateCropSO(crop)) count++;

        string[] tools    = { "tool_hoe",  "tool_watercan", "tool_pest",    "item_fertilizer" };
        string[] names    = { "Cái Cuốc",  "Bình Tưới",     "Thuốc Sâu",    "Phân Bón" };
        for (int i = 0; i < tools.Length; i++)
            if (SyncGenerateToolSO(tools[i], names[i])) count++;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (_syncAutoMove) SyncMigrateOldSOs();

        _syncLog += $"\n✅ Hoàn tất! Đã generate {count} SO file(s).";
        Repaint();
    }

    private bool SyncGenerateSeedSO(SeedItemModel seed)
    {
        string folder = "Assets/Resources/SeedData";
        string path   = $"{folder}/SO_Seed_{seed.SeedID}.asset";
        if (File.Exists(Path.Combine(Application.dataPath.Replace("Assets",""), path))) return false;

        EnsureFolder(folder);
        SeedItemSO so = AssetDatabase.LoadAssetAtPath<SeedItemSO>(path) ?? ScriptableObject.CreateInstance<SeedItemSO>();
        so.seedID   = seed.SeedID;
        so.seedName = seed.Name;
        so.buyPrice = seed.BuyPrice;

        if (!AssetDatabase.Contains(so)) AssetDatabase.CreateAsset(so, path);
        EditorUtility.SetDirty(so);
        _syncLog += $"  🌱 Created {path}\n";
        return true;
    }

    private bool SyncGenerateCropSO(CropDataModel crop)
    {
        string folder = "Assets/Resources/CropData";
        string path   = $"{folder}/SO_Crop_{crop.ProductID}.asset";
        if (File.Exists(Path.Combine(Application.dataPath.Replace("Assets",""), path))) return false;

        EnsureFolder(folder);
        CropDataSO so = AssetDatabase.LoadAssetAtPath<CropDataSO>(path) ?? ScriptableObject.CreateInstance<CropDataSO>();
        so.productID          = crop.ProductID;
        so.totalTimeToHarvest = crop.GrowSeconds;

        // Lấy tên nông sản từ ProductItemModel nếu có
        try
        {
            var prod = _syncDb?.Table<ProductItemModel>().FirstOrDefault(p => p.ProductID == crop.ProductID);
            so.cropName = prod?.Name ?? crop.ProductID;
        }
        catch { so.cropName = crop.ProductID; }

        if (!AssetDatabase.Contains(so)) AssetDatabase.CreateAsset(so, path);
        EditorUtility.SetDirty(so);
        _syncLog += $"  🌾 Created {path}\n";
        return true;
    }

    private bool SyncGenerateToolSO(string toolID, string displayName)
    {
        string folder = "Assets/Resources/ToolData";
        string path   = $"{folder}/SO_Tool_{toolID}.asset";
        if (File.Exists(Path.Combine(Application.dataPath.Replace("Assets",""), path))) return false;

        EnsureFolder(folder);
        ToolItemSO so = AssetDatabase.LoadAssetAtPath<ToolItemSO>(path) ?? ScriptableObject.CreateInstance<ToolItemSO>();
        so.toolID   = toolID;
        so.toolName = displayName;

        if (!AssetDatabase.Contains(so)) AssetDatabase.CreateAsset(so, path);
        EditorUtility.SetDirty(so);
        _syncLog += $"  🔧 Created {path}\n";
        return true;
    }

    private void SyncMigrateOldSOs()
    {
        _syncLog += "\n[Migrate] Đang tìm SO sai vị trí...\n";
        int moved = 0, skipped = 0, deleted = 0;

        var rules = new (string destFolder, string prefix)[]
        {
            ("Assets/Resources/SeedData",  "SO_Seed_"),
            ("Assets/Resources/CropData",  "SO_Crop_"),
            ("Assets/Resources/ToolData",  "SO_Tool_"),
        };

        foreach (var rule in rules)
        {
            string[] typeGuids = AssetDatabase.FindAssets(rule.prefix, new[] { "Assets" });
            EnsureFolder(rule.destFolder);

            foreach (string guid in typeGuids)
            {
                string srcPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileName(srcPath).StartsWith(rule.prefix)) continue;
                if (!srcPath.EndsWith(".asset")) continue;                       // bỏ meta, png...

                // ── Guard 1: Bỏ qua file rỗng/corrupted ──
                string fullSrc = Path.Combine(
                    Application.dataPath.Replace("Assets", ""), srcPath);
                long fileSize = new FileInfo(fullSrc).Length;
                if (fileSize == 0)
                {
                    // Xóa file rỗng thay vì move để không spread corruption
                    AssetDatabase.DeleteAsset(srcPath);
                    _syncLog += $"  🗑️ Deleted empty: {srcPath}\n";
                    deleted++;
                    continue;
                }

                // ── Guard 2: Đúng folder rồi, skip ──
                if (srcPath.StartsWith(rule.destFolder + "/")) continue;

                string fileName = Path.GetFileName(srcPath);
                string destPath = $"{rule.destFolder}/{fileName}";
                string fullDest = Path.Combine(
                    Application.dataPath.Replace("Assets", ""), destPath);

                // ── Guard 3: Dest đã có file có nội dung → không overwrite ──
                if (File.Exists(fullDest) && new FileInfo(fullDest).Length > 0)
                {
                    _syncLog += $"  ⏭️ Skip (dest exists): {fileName}\n";
                    // Xóa source trùng lặp rỗng hoặc dư
                    if (fileSize == 0) AssetDatabase.DeleteAsset(srcPath);
                    skipped++;
                    continue;
                }

                string err = AssetDatabase.MoveAsset(srcPath, destPath);
                if (string.IsNullOrEmpty(err))
                {
                    _syncLog += $"  📦 Moved: {Path.GetDirectoryName(srcPath)} → {rule.destFolder}/{fileName}\n";
                    moved++;
                }
                else
                {
                    _syncLog += $"  ⚠️ Move failed ({fileName}): {err}\n";
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        _syncLog += $"✅ Moved: {moved} | Skipped: {skipped} | Deleted empty: {deleted}";
        Repaint();
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
        string child  = Path.GetFileName(folderPath);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, child);
    }

}

