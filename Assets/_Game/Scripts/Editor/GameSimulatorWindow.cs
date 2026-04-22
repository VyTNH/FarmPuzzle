using UnityEngine;
using UnityEditor;
using SQLite;
using System.IO;
using FarmPuzzle.Core.Database;

public class GameSimulatorWindow : EditorWindow
{
    private string userIdInput = "user_888888";
    private string userNameInput = "Nguyễn Văn Nông Dân";
    private bool isLoggedIn = false;
    private PlayerModel loggedPlayer;
    private SQLiteConnection db;
    private string dbPath;
    private Vector2 leftScroll, rightScroll;

    // [MenuItem("FarmPuzzle/📱 Giả Lập Hệ Thống Gameplay")]
    public static void ShowWindow()
    {
        GetWindow<GameSimulatorWindow>("Máy Game Giả Lập", true, typeof(EditorWindow));
    }

    private void OnEnable()
    {
        dbPath = Path.Combine(Application.persistentDataPath, "FarmPuzzleDB.db");
    }

    private void OnGUI()
    {
        // Fix for Domain Reloads where db or player might be lost
        if (isLoggedIn && (db == null || loggedPlayer == null)) {
            isLoggedIn = false;
        }

        GUILayout.Label("GIẢ LẬP ĐĂNG NHẬP \n(HỆ THỐNG CSDL)", EditorStyles.boldLabel);
        
        if (!isLoggedIn) { DrawLoginScreen(); }
        else { DrawGameHUD(); }
        
        if (db != null && !isLoggedIn) { db.Close(); db = null; }
    }

    // ============================================
    // MÀN HÌNH CHƯA ĐĂNG NHẬP (OAUTH MÔ PHỎNG)
    // ============================================
    private void DrawLoginScreen()
    {
        GUILayout.Space(20);
        EditorGUILayout.HelpBox("Nhập thông tin User ảo để đăng nhập. Nếu ID chưa từng chơi, Hệ thống ERD sẽ tự động chèn Dữ Liệu Tân Thủ (Túi, Đất, Đồ).", MessageType.Info);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("User ID:", GUILayout.Width(150));
        userIdInput = EditorGUILayout.TextField(userIdInput);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Hiển Thị Tên:", GUILayout.Width(150));
        userNameInput = EditorGUILayout.TextField(userNameInput);
        GUILayout.EndHorizontal();

        GUILayout.Space(20);
        
        // Nút Đăng nhập 
        GUI.backgroundColor = new Color(0.2f, 0.6f, 0.3f); 
        if (GUILayout.Button("🌐 ĐĂNG NHẬP HỆ THỐNG", GUILayout.Height(40))) { SimulateLogin(); }
        GUI.backgroundColor = Color.white; // Trả lại màu
    }

    private void SimulateLogin()
    {
        if (!File.Exists(dbPath)) {
            Debug.LogError("Chưa thể lập Database! Hãy bấm nút Play trên cùng màn hình Unity 1 lần để hệ thống nhả ổ cứng!");
            return;
        }

        // Mở kết nối
        db = new SQLiteConnection(dbPath);

        // 1. Quét DB xem User đã chơi chưa
        var p = db.Table<PlayerModel>().Where(x => x.PlayerID == userIdInput).FirstOrDefault();
        if (p != null) {
            loggedPlayer = p; 
            isLoggedIn = true;
            Debug.Log($"[SERVER] Welcome back {p.Name}!");
        } else {
            // Giả lập Tân thủ
            loggedPlayer = new PlayerModel { PlayerID = userIdInput, Name = userNameInput, Money = 500, EXP = 0 };
            db.Insert(loggedPlayer);
            
            // Ép đồ Tân Thủ
            db.Insert(new InventoryModel { PlayerID = userIdInput, ItemID = "seed_carrot", Quantity = 10 });
            db.Insert(new InventoryModel { PlayerID = userIdInput, ItemID = "tool_hoe", Quantity = 1 });
            
            // Chia 4 miếng đất trống mặc định
            for (int i=0; i<4; i++) 
                db.Insert(new FarmTileModel { TileID = $"t_{userIdInput}_0{i}", PlayerID = userIdInput, State = 0 });

            isLoggedIn = true;
            Debug.Log($"[SERVER] Đã khởi tạo vùng dữ liệu ERD Mới cho Khách (ID: {userIdInput})!");
        }
    }

    // ============================================
    // MÀN HÌNH TRONG GAME (HUD) -> Thao tác cập nhật DB Cục bộ
    // ============================================
    private void DrawGameHUD()
    {
        // Thanh Tên Khách Hàng (Đỉnh màn hình)
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"🧑‍🌾 MÃ NGƯỜI CHƠI: {loggedPlayer.PlayerID} | TÊN: {loggedPlayer.Name}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("🚫 LOGOUT (Thoát Game)", EditorStyles.toolbarButton)) {
            isLoggedIn = false;
            if (db != null) { db.Close(); db = null; }
            GUIUtility.ExitGUI();
        }
        GUILayout.EndHorizontal();

        // 1. Các chỉ số Nhanh (PlayerModel)
        GUILayout.BeginHorizontal("box");
        GUILayout.Label($"💰 Vàng: {loggedPlayer.Money} $", EditorStyles.boldLabel, GUILayout.Width(150));
        GUILayout.Label($"⭐ Kinh nghiệm: {loggedPlayer.EXP} XP", EditorStyles.boldLabel, GUILayout.Width(150));
        
        // Mini game kiếm V/XP (Chạm chạy lệnh UPDATE Table PLAYER)
        if (GUILayout.Button("+ 100 Vàng")) { loggedPlayer.Money += 100; db.Update(loggedPlayer); }
        if (GUILayout.Button("+ 50 EXP", GUILayout.Width(80))) { loggedPlayer.EXP += 50; db.Update(loggedPlayer); }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        
        // --- Giao diện HUD song song: (TRÁI: TÚI ĐỒ) --- (PHẢI: MẶT ĐẤT) ---
        GUILayout.BeginHorizontal();
        
        // ---------------------------------
        // CỘT TRÁI: TÚI ĐỒ (INVENTORY)
        // ---------------------------------
        GUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.45f));
        GUILayout.Label("🎒 TÚI ĐỒ ĐANG MANG (INVENTORY)", EditorStyles.boldLabel);
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll, GUILayout.Height(300));
        
        var invs = db.Table<InventoryModel>().Where(x => x.PlayerID == loggedPlayer.PlayerID).ToList();
        foreach(var i in invs) {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label($"- {i.ItemID} (Sl: {i.Quantity})");
            // Nút Ăn/Bán vật phẩm (UPDATE Table INVENTORY, PLAYER)
            if (GUILayout.Button("Vứt Bỏ -1", GUILayout.Width(80))) {
                i.Quantity--;
                if(i.Quantity <= 0) db.Delete(i); else db.Update(i);
            }
            GUILayout.EndHorizontal();
        }
        
        GUILayout.Space(10);
        if (GUILayout.Button("🎁 Nhặt Hạt Cà Rốt Trôi Nổi")) {
            var exist = db.Table<InventoryModel>().Where(x => x.PlayerID == loggedPlayer.PlayerID && x.ItemID == "seed_carrot").FirstOrDefault();
            if (exist != null) { exist.Quantity++; db.Update(exist); }
            else { db.Insert(new InventoryModel { PlayerID = loggedPlayer.PlayerID, ItemID = "seed_carrot", Quantity = 1 }); }
        }
        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();

        // ---------------------------------
        // CỘT PHẢI: KHU TRANG TRẠI (FARM TILES)
        // ---------------------------------
        GUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.5f));
        GUILayout.Label("🌱 KHU VỰC ĐẤT TRỒNG TRỌT", EditorStyles.boldLabel);
        rightScroll = EditorGUILayout.BeginScrollView(rightScroll, GUILayout.Height(300));

        var tiles = db.Table<FarmTileModel>().Where(x => x.PlayerID == loggedPlayer.PlayerID).ToList();
        foreach(var t in tiles) {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label($"Mảnh ({t.TileID})", GUILayout.Width(100));
            
            if (t.State == 0) { // Đất bỏ hoang
                GUILayout.Label("[Trống]", GUILayout.Width(100));
                
                // Mua Hạt Cà Rốt và trồng
                if (GUILayout.Button("🚜 Gieo Hạt (-5 Vàng)")) {
                    if (loggedPlayer.Money >= 5) {
                        loggedPlayer.Money -= 5;
                        db.Update(loggedPlayer);

                        t.State = 1; t.PlantedSeedID = "seed_carrot"; 
                        db.Update(t);
                    }
                }
            } else { // Đã trồng cây
                GUILayout.Label($"[Cây: {t.PlantedSeedID}]", GUILayout.Width(100));
                
                // Cắt cây thu tiền kinh nghiệm
                if (GUILayout.Button("🌾 THU HOẠCH LẤY VÀNG")) {
                    t.State = 0; t.PlantedSeedID = ""; 
                    db.Update(t);

                    loggedPlayer.EXP += 10; loggedPlayer.Money += 20;
                    db.Update(loggedPlayer);
                }
            }
            GUILayout.EndHorizontal();
        }
        
        GUILayout.Space(10);
        // Nút mua đất ảo (UPDATE Table FARM TILE, PLAYER)
        if (GUILayout.Button($"🚜 Mua Thêm Mảnh Đất Mới (Giá: 100$)")) {
            if (loggedPlayer.Money >= 100) {
                loggedPlayer.Money -= 100; db.Update(loggedPlayer);
                db.Insert(new FarmTileModel { TileID = $"t_{loggedPlayer.PlayerID}_{tiles.Count}", PlayerID = loggedPlayer.PlayerID, State = 0 });
            }
        }
        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }
}
