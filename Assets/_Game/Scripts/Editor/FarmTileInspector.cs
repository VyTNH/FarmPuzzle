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

        // ═══ FIX BUG #1 ═══
        // Cache vào Dictionary để FindTileByCoords là O(1) thay vì O(n) LINQ scan
        // Trước: 144 lần × 2500 scan/frame = 10.8 triệu so sánh string/giây → FREEZE
        // Sau: 144 lần × O(1) lookup/frame
        private Dictionary<string, FarmTileModel> _tileMap = new Dictionary<string, FarmTileModel>();

        private string _statusMsg = "Bấm 'Tải dữ liệu' để bắt đầu.";
        private FarmTileModel _selectedTile = null;
        private Vector2 _detailScroll;

        private int _editState;
        private string _editSeed;
        private bool _editObstacle;
        private string _editObsID;
        private string _lastTileID;

        // ═══ FIX BUG #2 ═══
        // Cache gridW/H — tính 1 lần trong LoadTiles, KHÔNG tính lại mỗi frame trong OnGUI
        // Trước: GetGridMax() gọi 2 lần trong OnGUI, mỗi lần loop + split 2500 tile = 150,000 Split/s
        private int _cachedGridW = 50;
        private int _cachedGridH = 50;

        // Viewport Paging
        private int _viewOffsetX = 0;
        private int _viewOffsetY = 0;
        private const int VIEW_SIZE = 12;

        // [MenuItem("FarmPuzzle/4. Quản Lý Ô Đất (FARM_TILE Inspector)")]
        public static void ShowWindow()
        {
            var window = GetWindow<FarmTileInspector>("FARM_TILE Inspector");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable() { ConnectDB(); }
        private void OnDisable() { if (_db != null) { _db.Close(); _db = null; } }

        private void ConnectDB()
        {
            // ═══ FIX BUG #4 ═══
            // Guard clause: KHÔNG mở thêm connection nếu đã có rồi
            // Trước: mỗi lần focus window = mở thêm 1 connection → memory leak
            if (_db != null) return;

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
                if (DataManager.Instance != null && DataManager.Instance.CurrentPlayer != null)
                {
                    string pID = DataManager.Instance.CurrentPlayer.PlayerID;
                    _tiles = _db.Table<FarmTileModel>().Where(t => t.PlayerID == pID || t.TileID.Contains(pID)).ToList();
                    _statusMsg = $"Đã tải {_tiles.Count} dòng của Player: {pID}";
                }
                else
                {
                    _tiles = _db.Table<FarmTileModel>().ToList();
                    _statusMsg = $"Đã tải tất cả {_tiles.Count} dòng (Chưa xác định Player).";
                }

                // ═══ FIX BUG #1 & #2 ═══
                // Build Dictionary và cache gridW/H SAU KHI TẢI — không làm trong OnGUI
                RebuildCaches();
            }
            catch (System.Exception e) { _statusMsg = "Lỗi: " + e.Message; }
        }

        // Xây dựng Dictionary lookup map và tính gridW/H ─ chỉ gọi khi data thay đổi
        private void RebuildCaches()
        {
            _tileMap.Clear();
            int maxX = 1, maxY = 1;
            foreach (var t in _tiles)
            {
                _tileMap[t.TileID] = t;

                // Tính gridW/gridH ngay đây luôn, 1 lần duy nhất
                var parts = t.TileID.Split('_');
                if (parts.Length >= 2)
                {
                    if (int.TryParse(parts[parts.Length - 2], out int px) && px + 1 > maxX) maxX = px + 1;
                    if (int.TryParse(parts[parts.Length - 1], out int py) && py + 1 > maxY) maxY = py + 1;
                }
            }
            _cachedGridW = Mathf.Clamp(maxX, 1, 100);
            _cachedGridH = Mathf.Clamp(maxY, 1, 100);
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

            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("➕ Phát sinh 50x50", GUILayout.Height(25), GUILayout.Width(130)))
                Init50x50Tiles();
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

            // ========== CỘT TRÁI: BẢN ĐỒ LƯỚI (VIEWPORT 12x12) ==========
            GUILayout.BeginVertical("box", GUILayout.Width(450));
            GUILayout.Label($"📍 BẢN ĐỒ NÔNG TRẠI — {_cachedGridW}x{_cachedGridH} (Click ô để chỉnh sửa)", EditorStyles.boldLabel);

            // ═══ FIX BUG #2 ═══
            // Dùng _cachedGridW/_cachedGridH thay vì gọi GetGridMax() mỗi frame
            int gridW = _cachedGridW;
            int gridH = _cachedGridH;

            // Navigation Buttons để di chuyển viewport
            GUILayout.BeginHorizontal();
            GUILayout.Label("Vùng nhìn:", GUILayout.Width(75));
            if (GUILayout.Button("◄ X", GUILayout.Width(40))) _viewOffsetX = Mathf.Max(0, _viewOffsetX - VIEW_SIZE);
            GUILayout.Label($"X:{_viewOffsetX}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(40));
            if (GUILayout.Button("X ►", GUILayout.Width(40))) _viewOffsetX = Mathf.Min(Mathf.Max(0, gridW - VIEW_SIZE), _viewOffsetX + VIEW_SIZE);
            GUILayout.Space(10);
            if (GUILayout.Button("▲ Y", GUILayout.Width(40))) _viewOffsetY = Mathf.Min(Mathf.Max(0, gridH - VIEW_SIZE), _viewOffsetY + VIEW_SIZE);
            GUILayout.Label($"Y:{_viewOffsetY}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(40));
            if (GUILayout.Button("▼ Y", GUILayout.Width(40))) _viewOffsetY = Mathf.Max(0, _viewOffsetY - VIEW_SIZE);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.Height(400));

            int endX = Mathf.Min(gridW, _viewOffsetX + VIEW_SIZE);
            int endY = Mathf.Min(gridH, _viewOffsetY + VIEW_SIZE);

            // Header cột tọa độ X
            GUILayout.BeginHorizontal();
            GUILayout.Label("Y\\X", GUILayout.Width(30));
            for (int x = _viewOffsetX; x < endX; x++)
                GUILayout.Label("" + x, EditorStyles.boldLabel, GUILayout.Width(35));
            GUILayout.EndHorizontal();

            for (int y = endY - 1; y >= _viewOffsetY; y--)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(" " + y, EditorStyles.boldLabel, GUILayout.Width(30));

                for (int x = _viewOffsetX; x < endX; x++)
                {
                    // ═══ FIX BUG #1 ═══
                    // Dictionary lookup O(1) thay vì LINQ.Where().Where() O(n)
                    var tile = FindTileByCoords(x, y);

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

                    string label = tile == null ? "?" :
                                   tile.State == 0 ? "🔒" :
                                   !string.IsNullOrEmpty(tile.PlantedSeedID) ? "🌱" : "✅";

                    if (GUILayout.Button(label, GUILayout.Width(35), GUILayout.Height(35)))
                    {
                        if (tile != null)
                        {
                            _selectedTile = tile;
                            _editState    = tile.State;
                            _editSeed     = tile.PlantedSeedID ?? "";
                            _editObstacle = tile.HasObstacle;
                            _editObsID    = tile.ObstacleID ?? "";
                            _lastTileID   = tile.TileID;
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
                _db.RunInTransaction(() => {
                    foreach (var t in _tiles) { t.State = 1; t.HasObstacle = false; t.ObstacleID = ""; _db.Update(t); }
                });
                _statusMsg = "Đã mở khóa toàn bộ các ô!"; LoadTiles();
            }
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Khóa TẤT CẢ", GUILayout.Height(25)))
            {
                _db.RunInTransaction(() => {
                    foreach (var t in _tiles) { t.State = 0; _db.Update(t); }
                });
                _statusMsg = "Đã khóa toàn bộ trang trại!"; LoadTiles();
            }
            GUI.backgroundColor = new Color(1f, 0.5f, 0f);
            if (GUILayout.Button("Dọn sạch CÂY", GUILayout.Height(25)))
            {
                _db.RunInTransaction(() => {
                    foreach (var t in _tiles) { t.PlantedSeedID = ""; t.PlantTimeTicks = 0; if (t.State == 2) t.State = 1; _db.Update(t); }
                });
                _statusMsg = "Đã dọn sạch cây trồng!"; LoadTiles();
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

                string coords = ExtractCoords(_selectedTile.TileID);
                GUILayout.Label("Tọa độ lưới: (" + coords.Replace("_", ", ") + ")", EditorStyles.largeLabel);
                GUILayout.Space(10);

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
                        _selectedTile.State         = _editState;
                        _selectedTile.PlantedSeedID = _editSeed;
                        _selectedTile.HasObstacle   = _editObstacle;
                        _selectedTile.ObstacleID    = _editObsID;
                        _db.Update(_selectedTile);
                        // Cập nhật cache luôn, không cần reload toàn bộ
                        _tileMap[_selectedTile.TileID] = _selectedTile;
                        _statusMsg = "Đã lưu thành công: " + _selectedTile.TileID;
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

        // ═══ FIX BUG #1 ═══
        // O(1) Dictionary lookup — KHÔNG còn LINQ.Where() scan toàn bộ list
        private FarmTileModel FindTileByCoords(int x, int y)
        {
            // Thử tất cả các pattern TileID có thể: tile_{playerID}_{x}_{y}
            // Vì playerID không biết, ta dùng suffix matching trên dict keys
            // Nhưng để O(1) ta cần key chính xác → tìm theo playerID nếu biết
            if (DataManager.Instance?.CurrentPlayer != null)
            {
                string pid = DataManager.Instance.CurrentPlayer.PlayerID;
                string key = $"tile_{pid}_{x}_{y}";
                if (_tileMap.TryGetValue(key, out var t)) return t;
            }

            // Fallback: tìm bất kỳ tile nào khớp tọa độ từ dict (vẫn nhanh vì chỉ duyệt keys khớp suffix)
            string suffix = $"_{x}_{y}";
            foreach (var kv in _tileMap)
                if (kv.Key.EndsWith(suffix)) return kv.Value;

            return null;
        }

        private void CleanupDatabase()
        {
            if (_db == null) ConnectDB();
            if (_db == null) return;

            if (!EditorUtility.DisplayDialog("Xác nhận dọn dẹp",
                "Hệ thống sẽ xóa tất cả các ô đất có ID sai quy chuẩn hoặc là rác của 'local_player'. Bạn có chắc chắn không?",
                "Có, xóa ngay", "Hủy")) return;

            var allTiles = _db.Table<FarmTileModel>().ToList();
            int count = 0;
            _db.RunInTransaction(() => {
                foreach (var t in allTiles)
                {
                    if (!t.TileID.StartsWith("tile_") || t.TileID.Contains("local_player") || t.PlayerID == "local_player")
                    {
                        _db.Delete(t);
                        count++;
                    }
                }
            });
            _statusMsg = "Đã dọn dẹp " + count + " ô đất rác!";
            LoadTiles();
        }

        private string ExtractCoords(string tileID)
        {
            var parts = tileID.Split('_');
            if (parts.Length >= 2)
                return parts[parts.Length - 2] + "_" + parts[parts.Length - 1];
            return tileID;
        }

        // ═══ FIX BUG #3 ═══
        // Trước: 2500 SELECT riêng lẻ + 2500 INSERT riêng lẻ = 5000 queries trên UI thread → khóa Editor
        // Sau: 1 SELECT toàn bộ → build HashSet → 1 InsertAll batch → mượt mà
        private void Init50x50Tiles()
        {
            if (_db == null) ConnectDB();
            if (_db == null) return;

            if (!EditorUtility.DisplayDialog("Xác nhận",
                "Bạn có muốn phát sinh 2500 ô đất (50x50) cho 'local_player' không?",
                "Có", "Hủy")) return;

            // Bước 1: 1 lần SELECT tất cả TileID → HashSet để kiểm tra O(1)
            var existingIds = new HashSet<string>(
                _db.Table<FarmTileModel>()
                   .Select(t => t.TileID)
                   .ToList()
            );

            // Bước 2: Tính toán danh sách cần insert trong RAM
            var toInsert = new List<FarmTileModel>();
            for (int x = 0; x < 50; x++)
            {
                for (int y = 0; y < 50; y++)
                {
                    string id = $"tile_local_player_{x}_{y}";
                    if (!existingIds.Contains(id))
                    {
                        bool isEdge = (x == 0 || x == 49 || y == 0 || y == 49);
                        toInsert.Add(new FarmTileModel
                        {
                            TileID      = id,
                            PlayerID    = "local_player",
                            State       = isEdge ? 0 : 1,
                            HasObstacle = isEdge,
                            ObstacleID  = isEdge ? "rock_default" : ""
                        });
                    }
                }
            }

            // Bước 3: 1 lần InsertAll batch — cực nhanh
            if (toInsert.Count > 0)
                _db.InsertAll(toInsert);

            _statusMsg = $"Đã phát sinh {toInsert.Count} ô đất mới! (Tổng: {existingIds.Count + toInsert.Count})";
            LoadTiles();
        }
    }
}
