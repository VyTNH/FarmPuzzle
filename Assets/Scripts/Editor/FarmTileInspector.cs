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

        // Bo nho tam de chinh sua
        private int _editState;
        private string _editSeed;
        private bool _editObstacle;
        private string _editObsID;
        private string _lastTileID;

        // [MenuItem("FarmPuzzle/4. Quản Lý Ô Đất (FARM_TILE Inspector)")]
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
            
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("🗑️ Dọn dẹp DB", GUILayout.Height(25), GUILayout.Width(120)))
                CleanupDatabase();
            GUI.backgroundColor = Color.white;

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

            // ========== CỘT TRÁI: BẢN ĐỒ LƯỚI 10x10 ==========
            GUILayout.BeginVertical("box", GUILayout.Width(450));
            GUILayout.Label("📍 BẢN ĐỒ NÔNG TRẠI (Click ô để chỉnh sửa)", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // Thêm ScrollView cho lưới 10x10
            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.Height(400));

            // Header cột tọa độ X
            GUILayout.BeginHorizontal();
            GUILayout.Label("Y\\X", GUILayout.Width(30));
            for (int x = 0; x < 10; x++)
                GUILayout.Label("  " + x, EditorStyles.boldLabel, GUILayout.Width(35));
            GUILayout.EndHorizontal();

            // Vẽ lưới từ Y=9 (trên) xuống Y=0 (dưới) cho khớp với màn hình Game
            for (int y = 9; y >= 0; y--)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(" " + y, EditorStyles.boldLabel, GUILayout.Width(30));

                for (int x = 0; x < 10; x++)
                {
                    var tile = FindTileByCoords(x, y);
                    
                    // Tô màu theo trạng thái
                    if (tile == null)
                        GUI.backgroundColor = Color.gray;
                    else if (_selectedTile != null && _selectedTile.TileID == tile.TileID)
                        GUI.backgroundColor = Color.white; 
                    else if (tile.State == 0)
                        GUI.backgroundColor = new Color(0.5f, 0.3f, 0.3f); 
                    else if (!string.IsNullOrEmpty(tile.PlantedSeedID))
                        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f); 
                    else
                        GUI.backgroundColor = new Color(0.6f, 0.85f, 0.5f); 

                    string label = "";
                    if (tile == null) label = "?";
                    else if (tile.State == 0) label = "🔒";
                    else if (!string.IsNullOrEmpty(tile.PlantedSeedID)) label = "🌱";
                    else label = "✅";

                    if (GUILayout.Button(label, GUILayout.Width(35), GUILayout.Height(35)))
                    {
                        if (tile != null) 
                        {
                            _selectedTile = tile;
                            // Reset bo nho tam khi chon o moi
                            _editState = tile.State;
                            _editSeed = tile.PlantedSeedID ?? "";
                            _editObstacle = tile.HasObstacle;
                            _editObsID = tile.ObstacleID ?? "";
                            _lastTileID = tile.TileID;
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

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
            if (GUILayout.Button("Mở TẤT CẢ", GUILayout.Height(25)))
            {
                foreach (var t in _tiles) { t.State = 1; t.HasObstacle = false; t.ObstacleID = ""; _db.Update(t); }
                _statusMsg = "Đã mở khóa toàn bộ các ô!"; LoadTiles();
            }
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Khóa TẤT CẢ", GUILayout.Height(25)))
            {
                foreach (var t in _tiles) { t.State = 0; _db.Update(t); }
                _statusMsg = "Đã khóa toàn bộ trang trại!"; LoadTiles();
            }
            GUI.backgroundColor = new Color(1f, 0.5f, 0f);
            if (GUILayout.Button("Dọn sạch CÂY", GUILayout.Height(25)))
            {
                foreach (var t in _tiles) { t.PlantedSeedID = ""; t.PlantTimeTicks = 0; if (t.State == 2) t.State = 1; _db.Update(t); }
                _statusMsg = "Đã dọn sạch cây trồng trên toàn bộ ô mở!"; LoadTiles();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // ========== CỘT PHẢI: CHI TIẾT Ô ĐANG CHỌN ==========
            GUILayout.BeginVertical("box");
            if (_selectedTile != null)
            {
                GUILayout.Label("📋 CHI TIẾT Ô: " + _selectedTile.TileID, EditorStyles.boldLabel);
                GUILayout.Space(5);

                // Tọa độ
                string coords = ExtractCoords(_selectedTile.TileID);
                GUILayout.Label("Tọa độ lưới: (" + coords.Replace("_", ", ") + ")", EditorStyles.largeLabel);
                GUILayout.Space(10);

                // State
                GUILayout.Label("Trạng thái (State):", EditorStyles.boldLabel);
                _editState = EditorGUILayout.IntPopup(_editState, 
                    new string[] { "0 - Khóa 🔒", "1 - Mở ✅", "2 - Đang trồng 🌱" }, 
                    new int[] { 0, 1, 2 });

                GUILayout.Space(5);
                GUILayout.Label("Hạt giống đang trồng:", EditorStyles.boldLabel);
                _editSeed = EditorGUILayout.TextField(_editSeed);

                GUILayout.Space(5);
                GUILayout.Label("Chướng ngại vật:", EditorStyles.boldLabel);
                _editObstacle = EditorGUILayout.Toggle("Có chướng ngại", _editObstacle);
                _editObsID = EditorGUILayout.TextField("Obstacle ID", _editObsID);

                GUILayout.Space(10);

                // Phát hiện thay đổi giua Bo nho tam va Du lieu goc
                bool changed = (_editState != _selectedTile.State || 
                               _editSeed != (_selectedTile.PlantedSeedID ?? "") ||
                               _editObstacle != _selectedTile.HasObstacle || 
                               _editObsID != (_selectedTile.ObstacleID ?? ""));

                if (changed)
                {
                    EditorGUILayout.HelpBox("⚠️ DỮ LIỆU ĐÃ THAY ĐỔI - Vui lòng bấm nút Lưu phía dưới", MessageType.Warning);
                    
                    GUI.backgroundColor = Color.yellow;
                    if (GUILayout.Button("💾 LƯU THAY ĐỔI NGAY", GUILayout.Height(45)))
                    {
                        Debug.Log("[Inspector Log] Bat dau cap nhat vao SQLite cho o: " + _selectedTile.TileID + " | State Moi: " + _editState);
                        _selectedTile.State = _editState;
                        _selectedTile.PlantedSeedID = _editSeed;
                        _selectedTile.HasObstacle = _editObstacle;
                        _selectedTile.ObstacleID = _editObsID;
                        
                        int affectedRows = _db.Update(_selectedTile);
                        Debug.Log("[Inspector Log] Ket qua SQLite: Da cap nhat " + affectedRows + " dong.");
                        
                        _statusMsg = "Đã lưu thành công: " + _selectedTile.TileID;
                        LoadTiles();
                        // Tim lai tile da chon sau khi reload
                        _selectedTile = _tiles.FirstOrDefault(t => t.TileID == _selectedTile.TileID);
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
            else
            {
                GUILayout.Label("👈 Click vào ô trên bản đồ bên trái để xem chi tiết.", EditorStyles.wordWrappedLabel);
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        // Tim tile trong DB dua tren toa do x_y (Uu tien ID bat dau bang 'tile_')
        private FarmTileModel FindTileByCoords(int x, int y)
        {
            string suffix = "_" + x + "_" + y;
            var matches = _tiles.Where(t => t.TileID.EndsWith(suffix)).ToList();
            if (matches.Count == 0) return null;
            
            var standard = matches.FirstOrDefault(t => t.TileID.StartsWith("tile_"));
            return standard ?? matches[0];
        }

        private void CleanupDatabase()
        {
            if (_db == null) ConnectDB();
            if (_db == null) return;

            if (!EditorUtility.DisplayDialog("Xác nhận dọn dẹp", 
                "Hệ thống sẽ xóa tất cả các ô đất có ID sai quy chuẩn (không bắt đầu bằng 'tile_'). Bạn có chắc chắn không?", 
                "Có, xóa ngay", "Hủy")) return;

            var allTiles = _db.Table<FarmTileModel>().ToList();
            int count = 0;
            foreach (var t in allTiles)
            {
                if (!t.TileID.StartsWith("tile_"))
                {
                    _db.Delete(t);
                    count++;
                }
            }
            _statusMsg = "Đã dọn dẹp " + count + " ô đất sai định dạng!";
            LoadTiles();
        }

        // Trich toa do tu TileID (VD: "tile_player01_2_3" -> "2_3")
        private string ExtractCoords(string tileID)
        {
            var parts = tileID.Split('_');
            if (parts.Length >= 2)
                return parts[parts.Length - 2] + "_" + parts[parts.Length - 1];
            return tileID;
        }
    }
}
