using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SQLite;
using FarmPuzzle.Core.Database;

namespace FarmPuzzle.EditorTools
{
    public class FarmTileInspector : EditorWindow
    {
        private SQLiteConnection _db;
        private List<FarmTileModel> _tiles = new List<FarmTileModel>();
        private string _statusMsg = "Bấm 'Tải dữ liệu' để bắt đầu.";
        private FarmTileModel _selectedTile = null;
        private Vector2 _detailScroll;

        [MenuItem("FarmPuzzle/4. Quản Lý Ô Đất (FARM_TILE Inspector)")]
        public static void ShowWindow()
        {
            var window = GetWindow<FarmTileInspector>("FARM_TILE Inspector");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable() { ConnectDB(); }
        private void OnDisable() { if (_db != null) _db.Close(); }

        private void ConnectDB()
        {
            string dbPath = Path.Combine(Application.persistentDataPath, "FarmPuzzleDB.db");
            if (!File.Exists(dbPath)) { _statusMsg = "DB chưa tồn tại! Play game 1 lần để khởi tạo."; return; }
            _db = new SQLiteConnection(dbPath);
            _statusMsg = "Kết nối DB OK.";
        }

        private void LoadTiles()
        {
            if (_db == null) { ConnectDB(); if (_db == null) return; }
            try
            {
                _tiles = _db.Table<FarmTileModel>().ToList();
                _statusMsg = $"Đã tải {_tiles.Count} dòng FARM_TILE.";
            }
            catch (System.Exception e) { _statusMsg = "Lỗi: " + e.Message; }
        }

        private void OnGUI()
        {
            GUILayout.Label("🗺️ QUẢN LÝ Ô ĐẤT NÔNG TRẠI (FARM_TILE)", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍 Tải dữ liệu", GUILayout.Height(25), GUILayout.Width(130)))
                LoadTiles();
            GUILayout.Label(_statusMsg);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (_tiles.Count == 0)
            {
                EditorGUILayout.HelpBox("Chưa có dữ liệu. Bấm 'Tải dữ liệu'.", MessageType.Info);
                return;
            }

            // Chia 2 cột: Trái = Bản đồ trực quan | Phải = Chi tiết ô đang chọn
            GUILayout.BeginHorizontal();

            // ========== CỘT TRÁI: BẢN ĐỒ LƯỚI 5x5 ==========
            GUILayout.BeginVertical("box", GUILayout.Width(320));
            GUILayout.Label("📍 BẢN ĐỒ NÔNG TRẠI (Click ô để chỉnh sửa)", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // Header cột tọa độ X
            GUILayout.BeginHorizontal();
            GUILayout.Label("Y\\X", GUILayout.Width(30));
            for (int x = 0; x < 5; x++)
                GUILayout.Label($"  {x}", EditorStyles.boldLabel, GUILayout.Width(52));
            GUILayout.EndHorizontal();

            // Vẽ lưới từ Y=4 (trên) xuống Y=0 (dưới) cho khớp với màn hình Game
            for (int y = 4; y >= 0; y--)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($" {y}", EditorStyles.boldLabel, GUILayout.Width(30));

                for (int x = 0; x < 5; x++)
                {
                    var tile = FindTileByCoords(x, y);
                    
                    // Tô màu theo trạng thái
                    if (tile == null)
                        GUI.backgroundColor = Color.gray;
                    else if (_selectedTile != null && _selectedTile.TileID == tile.TileID)
                        GUI.backgroundColor = Color.white; // Đang chọn
                    else if (tile.State == 0)
                        GUI.backgroundColor = new Color(0.5f, 0.3f, 0.3f); // Khóa = đỏ tối
                    else if (!string.IsNullOrEmpty(tile.PlantedSeedID))
                        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f); // Đang trồng = xanh lá
                    else
                        GUI.backgroundColor = new Color(0.6f, 0.85f, 0.5f); // Mở trống = xanh nhạt

                    string label = "";
                    if (tile == null) label = "?";
                    else if (tile.State == 0) label = "🔒";
                    else if (!string.IsNullOrEmpty(tile.PlantedSeedID)) label = "🌱";
                    else label = "✅";

                    if (GUILayout.Button(label, GUILayout.Width(52), GUILayout.Height(45)))
                    {
                        if (tile != null) _selectedTile = tile;
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            // Chú thích
            GUILayout.Label("Chú thích:", EditorStyles.miniLabel);
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.5f, 0.3f, 0.3f);
            GUILayout.Button("🔒", GUILayout.Width(30), GUILayout.Height(20));
            GUILayout.Label("Khóa", GUILayout.Width(40));
            GUI.backgroundColor = new Color(0.6f, 0.85f, 0.5f);
            GUILayout.Button("✅", GUILayout.Width(30), GUILayout.Height(20));
            GUILayout.Label("Trống", GUILayout.Width(40));
            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
            GUILayout.Button("🌱", GUILayout.Width(30), GUILayout.Height(20));
            GUILayout.Label("Đang trồng", GUILayout.Width(70));
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Nút hành động hàng loạt
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Mở ALL", GUILayout.Height(25)))
            {
                foreach (var t in _tiles) { t.State = 1; t.HasObstacle = false; t.ObstacleID = ""; _db.Update(t); }
                _statusMsg = "Đã mở khóa toàn bộ!"; LoadTiles();
            }
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Khóa ALL", GUILayout.Height(25)))
            {
                foreach (var t in _tiles) { t.State = 0; _db.Update(t); }
                _statusMsg = "Đã khóa toàn bộ!"; LoadTiles();
            }
            GUI.backgroundColor = new Color(1f, 0.5f, 0f);
            if (GUILayout.Button("Xóa cây ALL", GUILayout.Height(25)))
            {
                foreach (var t in _tiles) { t.PlantedSeedID = ""; t.PlantTimeTicks = 0; if (t.State == 2) t.State = 1; _db.Update(t); }
                _statusMsg = "Đã dọn sạch cây!"; LoadTiles();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // ========== CỘT PHẢI: CHI TIẾT Ô ĐANG CHỌN ==========
            GUILayout.BeginVertical("box");
            if (_selectedTile != null)
            {
                GUILayout.Label($"📋 CHI TIẾT Ô: {_selectedTile.TileID}", EditorStyles.boldLabel);
                GUILayout.Space(5);

                // Tọa độ
                string coords = ExtractCoords(_selectedTile.TileID);
                GUILayout.Label($"Tọa độ trên lưới: ({coords.Replace("_", ", ")})", EditorStyles.largeLabel);
                GUILayout.Space(10);

                _detailScroll = GUILayout.BeginScrollView(_detailScroll);

                // State
                GUILayout.Label("Trạng thái (State):", EditorStyles.boldLabel);
                int newState = EditorGUILayout.IntPopup(_selectedTile.State, 
                    new string[] { "0 - Khóa 🔒", "1 - Mở ✅", "2 - Đang trồng 🌱" }, 
                    new int[] { 0, 1, 2 });

                GUILayout.Space(5);
                GUILayout.Label("Hạt giống đang trồng:", EditorStyles.boldLabel);
                string newSeed = EditorGUILayout.TextField(_selectedTile.PlantedSeedID ?? "");

                GUILayout.Space(5);
                GUILayout.Label("Chướng ngại vật:", EditorStyles.boldLabel);
                bool newObstacle = EditorGUILayout.Toggle("Có chướng ngại", _selectedTile.HasObstacle);
                string newObsID = EditorGUILayout.TextField("Obstacle ID", _selectedTile.ObstacleID ?? "");

                GUILayout.Space(10);

                // Phát hiện thay đổi
                bool changed = (newState != _selectedTile.State || 
                               newSeed != (_selectedTile.PlantedSeedID ?? "") ||
                               newObstacle != _selectedTile.HasObstacle || 
                               newObsID != (_selectedTile.ObstacleID ?? ""));

                if (changed)
                {
                    GUI.backgroundColor = Color.yellow;
                    if (GUILayout.Button("💾 LƯU THAY ĐỔI", GUILayout.Height(35)))
                    {
                        _selectedTile.State = newState;
                        _selectedTile.PlantedSeedID = newSeed;
                        _selectedTile.HasObstacle = newObstacle;
                        _selectedTile.ObstacleID = newObsID;
                        _db.Update(_selectedTile);
                        _statusMsg = $"Đã lưu: {_selectedTile.TileID}";
                        LoadTiles();
                        // Tìm lại tile đã chọn sau khi reload
                        _selectedTile = _tiles.FirstOrDefault(t => t.TileID == _selectedTile.TileID);
                    }
                    GUI.backgroundColor = Color.white;
                }

                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("👈 Click vào ô trên bản đồ bên trái để xem chi tiết.", EditorStyles.wordWrappedLabel);
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        // Tìm tile trong DB dựa trên tọa độ x_y (khớp với bất kỳ playerID nào)
        private FarmTileModel FindTileByCoords(int x, int y)
        {
            string suffix = $"_{x}_{y}";
            return _tiles.FirstOrDefault(t => t.TileID.EndsWith(suffix));
        }

        // Trích tọa độ từ TileID (VD: "tile_player01_2_3" → "2_3")
        private string ExtractCoords(string tileID)
        {
            var parts = tileID.Split('_');
            if (parts.Length >= 2)
                return $"{parts[parts.Length - 2]}_{parts[parts.Length - 1]}";
            return tileID;
        }
    }
}
