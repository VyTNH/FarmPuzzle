using UnityEngine;
using FarmPuzzle.Core.Database;

namespace FarmPuzzle.Testing
{
    public class SandboxTestUI : MonoBehaviour
    {
        private GridManager gridManager;
        public SeedItemSO[] availableSeeds;

        // --- Trạng thái Đăng Nhập ---
        private bool isLoggedIn = false;
        private string inputPlayerID = "tester_01";
        private string inputPlayerName = "Sếp Dương";

        void Start()
        {
            gridManager = FindFirstObjectByType<GridManager>();

            // Tự cầm sẵn hạt giống đầu tiên vào tay
            if (availableSeeds != null && availableSeeds.Length > 0 && gridManager != null)
            {
                gridManager.selectedSeed = availableSeeds[0];
            }
        }

        void OnGUI()
        {
            GUIStyle boldStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 16 };
            GUIStyle statusStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };

            GUILayout.BeginArea(new Rect(20, 20, 420, 650));
            GUILayout.BeginVertical("box");

            GUILayout.Label("🎮 BẢNG ĐIỀU KHIỂN TEST (UC1/UC2)", headerStyle);
            GUILayout.Space(5);

            // ============================================
            // PHẦN 0: ĐĂNG NHẬP (UC1 - Khởi tạo dữ liệu)
            // ============================================
            GUILayout.Label("0. ĐĂNG NHẬP HỆ THỐNG (UC1)", boldStyle);

            if (!isLoggedIn)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Player ID:", GUILayout.Width(80));
                inputPlayerID = GUILayout.TextField(inputPlayerID, GUILayout.Height(25));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Tên:", GUILayout.Width(80));
                inputPlayerName = GUILayout.TextField(inputPlayerName, GUILayout.Height(25));
                GUILayout.EndHorizontal();

                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("🔑 ĐĂNG NHẬP / TẠO MỚI", GUILayout.Height(40)))
                {
                    if (DataManager.Instance != null)
                    {
                        bool result = DataManager.Instance.LoginPlayer(inputPlayerID, inputPlayerName);
                        if (result)
                        {
                            isLoggedIn = true;
                            Debug.Log($"<color=green>[UC1 PASS]</color> Đăng nhập OK! Player: {inputPlayerID}");
                            
                            // Đồng bộ Nông trại từ DB ngay sau khi login
                            if (gridManager != null)
                            {
                                gridManager.LoadFarmFromDB();
                            }
                        }
                        else
                        {
                            Debug.LogError("[UC1 FAIL] LoginPlayer trả về false! Kiểm tra DataManager.");
                        }
                    }
                    else
                    {
                        Debug.LogError("[UC1 FAIL] DataManager.Instance == null! DB chưa khởi tạo.");
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                // Hiện trạng thái đã login
                var player = DataManager.Instance.CurrentPlayer;
                statusStyle.normal.textColor = Color.green;
                GUILayout.Label($"✅ Đã đăng nhập: {player.Name} | 💰 {player.Money}G", statusStyle);
                statusStyle.normal.textColor = Color.white;
            }

            GUILayout.Space(10);

            // Nếu chưa đăng nhập thì khóa hết các chức năng phía sau
            if (!isLoggedIn)
            {
                GUILayout.Label("⛔ Vui lòng ĐĂNG NHẬP trước khi test các chức năng bên dưới!", GUI.skin.label);
                GUILayout.EndVertical();
                GUILayout.EndArea();
                return;
            }

            // ============================================
            // PHẦN 1: CHỌN HẠT GIỐNG (UC2)
            // ============================================
            GUILayout.Label("1. CHỌN HẠT ĐỂ CẦM TRÊN TAY:", boldStyle);
            if (availableSeeds != null && availableSeeds.Length > 0)
            {
                GUILayout.BeginHorizontal();
                foreach (var seed in availableSeeds)
                {
                    if (seed != null)
                    {
                        GUI.backgroundColor = (gridManager != null && gridManager.selectedSeed == seed) ? Color.cyan : Color.white;
                        if (GUILayout.Button(seed.seedName, GUILayout.Height(35)))
                        {
                            gridManager.selectedSeed = seed;
                            gridManager.selectedCareTool = CropStatus.CropNeed.None;
                            Debug.Log($"<color=cyan>[Tay]</color> Đang cầm: {seed.seedName} (ID: {seed.seedID})");
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("(Chưa có File SO Hạt Giống nào! Dùng Tool SO Generator đúc trước.)", GUI.skin.label);
            }

            GUILayout.Space(10);

            // ============================================
            // PHẦN 2: HACK DATABASE (Bơm đạn test)
            // ============================================
            GUILayout.Label("2. HACK DATABASE (Test nhanh):", boldStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("💸 +1000 Vàng", GUILayout.Height(35)))
            {
                DataManager.Instance.AddGold(1000);
            }
            if (GUILayout.Button("🌱 +5 Hạt đang chọn", GUILayout.Height(35)))
            {
                if (gridManager != null && gridManager.selectedSeed != null)
                {
                    DataManager.Instance.AddItem(gridManager.selectedSeed.seedID, 5);
                    Debug.Log($"<color=green>[Hack]</color> +5 '{gridManager.selectedSeed.seedID}' vào Kho!");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // ============================================
            // PHẦN 3: HƯỚNG DẪN TEST
            // ============================================
            GUILayout.Label("📖 HƯỚNG DẪN:", boldStyle);
            GUILayout.Label("B1: Bấm '+5 Hạt đang chọn' để nạp đạn vào kho.");
            GUILayout.Label("B2: Click TRÁI vào ô đất XANH để gieo hạt.");
            GUILayout.Label("B3: Ô TỐI = BỊ KHÓA (dùng ET FARM_TILE Inspector để mở).");
            GUILayout.Label("B4: Đợi cây chín -> Click lần 2 để thu hoạch.");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
