using UnityEngine;
using FarmPuzzle.LandPuzzle.Data;
using FarmPuzzle.LandPuzzle.Grid;
using FarmPuzzle.LandPuzzle.Block;
using FarmPuzzle.LandPuzzle.Farm;

namespace FarmPuzzle.LandPuzzle
{
    public enum PuzzleState
    {
        Idle,
        Playing,
        CheckingLines,
        ClearingAnimation,
        GameOver,
        Win
    }

    /// <summary>
    /// Quản lý toàn bộ luồng chơi Block Puzzle.
    ///
    /// FLOW MỚI:
    /// 1. FarmLandTile bị click → gọi StartPuzzle(levelData, targetTile)
    /// 2. Người chơi kéo block vào grid
    /// 3. Khi xóa hàng/cột → damage obstacles lân cận → cho tài nguyên
    /// 4. Tất cả obstacles phá hết → WIN → tileTarget.Unlock()
    /// 5. Hết nước đi → GAME OVER (ô đất vẫn bị khóa)
    /// </summary>
    public class LandPuzzleManager : MonoBehaviour
    {
        public static LandPuzzleManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private GridBoard    _gridBoard;
        [SerializeField] private BlockSpawner _blockSpawner;
        [SerializeField] private BlockPuzzleScoring _scoring;
        [SerializeField] private GameObject   _puzzlePanel;   // Panel chứa toàn bộ puzzle UI

        // ── State ──
        private PuzzleState  _state = PuzzleState.Idle;
        private LandPlot _currentTargetTile;

        // ── Events ──
        public static System.Action<GridObstacleData, int> OnResourceGained;
        public static System.Action OnPuzzleWin;
        public static System.Action OnPuzzleGameOver;
        public static System.Action<int, int> OnEnergyChanged;

        public PuzzleState CurrentState => _state;
        public int         CurrentScore => _scoring != null ? _scoring.CurrentScore : 0;
        
        // MỚI: Kiểm tra xem sếp có đang bận giải đố không
        public bool IsPuzzleActive => _state != PuzzleState.Idle;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (_puzzlePanel != null) _puzzlePanel.SetActive(false);
        }

        private void Start()
        {
            // Subscribe energy
            if (EnergySystem.Instance != null)
                EnergySystem.Instance.OnEnergyChanged += (cur, max) => OnEnergyChanged?.Invoke(cur, max);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────────

        private void BuildExitButton()
        {
            if (_puzzlePanel == null) return;
            Transform existing = _puzzlePanel.transform.Find("Btn_ExitLandPuzzle");
            if (existing != null) return;

            GameObject btnObj = new GameObject("Btn_ExitLandPuzzle", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            btnObj.transform.SetParent(_puzzlePanel.transform, false);
            
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -20);
            rt.sizeDelta = new Vector2(60, 60);

            var img = btnObj.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.8f, 0.2f, 0.2f); // Đỏ nhạt

            var btn = btnObj.GetComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(ExitPuzzle);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            
            var txt = txtObj.GetComponent<UnityEngine.UI.Text>();
            txt.text = "X";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 30;
            txt.color = Color.white;
            txt.fontStyle = FontStyle.Bold;
        }

        /// <summary>Gọi trực tiếp (editor test hoặc farm tile click).</summary>
        public void StartPuzzle(LevelData levelData, LandPlot targetTile = null)
        {
            if (_state == PuzzleState.Playing) return;

            _currentTargetTile = targetTile;
            _state = PuzzleState.Playing;

            // Hiện puzzle panel
            if (_puzzlePanel != null) 
            {
                _puzzlePanel.SetActive(true);
                BuildExitButton();
            }

            // Init grid
            _gridBoard.InitializeGrid(levelData);
            _gridBoard.OnLinesCleared       += HandleLinesCleared;
            _gridBoard.OnObstacleDestroyed  += HandleObstacleDestroyed;
            _gridBoard.OnAllObstaclesDestroyed += HandleAllObstaclesDestroyed;

            // Init spawner
            _blockSpawner.Initialize(levelData, _gridBoard);
            _blockSpawner.OnBatchExhausted += HandleBatchExhausted;
            _blockSpawner.OnBlockPlaced    += HandleBlockPlaced;
            _blockSpawner.SpawnNewBatch();

            _scoring?.ResetScore();

            // 🚀 TELEPORT BÀN CỜ ĐẾN VỊ TRÍ CAMERA HIỆN TẠI ĐỂ LUÔN CĂN GIỮA MÀN HÌNH!
            if (Camera.main != null)
            {
                var camPos = Camera.main.transform.position;
                // Nhờ thiết kế GridBoard chuẩn xác (đã chia trung bình offset), 
                // ta chỉ việc ốp thẳng X, Y của Root vào Camera là lưới sẽ cân bằng tuyệt đối!
                transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
            }

            Debug.Log($"[Puzzle] Started level {levelData.levelId}");
        }

        public void RetryPuzzle()
        {
            // TODO: retry với cùng level
        }

        public void ExitPuzzle()
        {
            CleanUpPuzzle();
            if (_puzzlePanel != null) _puzzlePanel.SetActive(false);
            _state = PuzzleState.Idle;
        }

        // ── GAME EVENTS ──

        private void HandleBlockPlaced(ShapeData shape, Vector2Int anchor)
        {
            if (_state != PuzzleState.Playing) return;
            _scoring?.AddPlacementScore(shape.GetActiveCellCount());

            // Kiểm tra và xóa hàng/cột hoàn chỉnh ngay sau khi đặt block
            int linesCleared = _gridBoard.CheckAndClearLines();
            if (linesCleared > 0)
            {
                _scoring?.AddLineClearScore(linesCleared);
            }
        }

        private void HandleBatchExhausted()
        {
            if (_state != PuzzleState.Playing) return;

            // Kiểm tra game over (nếu vẫn còn obstacle chưa chết = chưa win)
            if (_gridBoard.HasObstaclesRemaining)
            {
                _blockSpawner.SpawnNewBatch();

                if (!_blockSpawner.HasAnyValidMove())
                {
                    HandleGameOver();
                }
            }
        }

        private void HandleLinesCleared(System.Collections.Generic.List<int> rows,
                                         System.Collections.Generic.List<int> cols)
        {
            Debug.Log($"[Puzzle] Cleared {rows.Count} rows + {cols.Count} cols");
        }

        private void HandleObstacleDestroyed(GridObstacleData data, int amount)
        {
            Debug.Log($"[Puzzle] Obstacle '{data.obstacleName}' destroyed → +{amount} {data.resourceType}");
            
            // LƯU NGAY VÀO SQLITE/RAM THÔNG QUA DATAMANAGER
            if (DataManager.Instance != null)
            {
                if (data.resourceType == ResourceType.Gold) 
                    DataManager.Instance.AddGold(amount);
                else 
                    DataManager.Instance.AddItem("item_" + data.resourceType.ToString().ToLower(), amount);
            }

            OnResourceGained?.Invoke(data, amount);
            _scoring?.AddObstacleScore(amount);
        }

        private void HandleAllObstaclesDestroyed()
        {
            if (_state == PuzzleState.Win) return;
            _state = PuzzleState.Win;

            Debug.Log("<color=green>[Giai Đoạn 3: Check Win Condition]</color> TẤT CẢ Chướng ngại vật trên Bàn Cờ đã bị dọn sạch! Kích hoạt Luồng Chiến thắng!");

            // LƯU NGAY LƯỢNG VẬT PHẨM ĐÃ TRÚNG DO ĐÁNH BLOCK XUỐNG DB
            if (DataManager.Instance != null) DataManager.Instance.CommitSessionInventory();

            // Mở khóa ô đất đã chọn
            if (_currentTargetTile != null) 
            {
                _currentTargetTile.UnlockPlot();
                
                // MỚI: Update Cập Nhập DB & Đồ Họa Của GridManager!
                if (FarmPuzzle.FarmSystem.GridManager.Instance != null) 
                {
                    // Lấy random 1 ô Đất Trồng (Từ index 6 dến 11)
                    int dirtTileIndex = UnityEngine.Random.Range(6, 12);
                    
                    // Lệnh 1: Thúc GridManager Vẽ Lớp Đất Mới Che Đi Lớp Cỏ Cũ
                    FarmPuzzle.FarmSystem.GridManager.Instance.ChangeTileArtState(_currentTargetTile, dirtTileIndex);
                    
                    // Lệnh 2: Thúc GridManager Đẩy Thông Tin (IsLocked=false) xuống SQLite
                    FarmPuzzle.FarmSystem.GridManager.Instance.SavePlotState(_currentTargetTile);
                    
                    Debug.Log($"<color=magenta>[Giai Đoạn 4: Lưu KQ & Cập nhật Graphic]</color> Ghi Đè Database: Đất {_currentTargetTile.plotID} ĐÃ MỞ KHÓA -> Vẽ Lại Graphic Thành Đất Trồng!");
                }
            }

            OnPuzzleWin?.Invoke();
            CleanUpPuzzle();
 
            // Ẩn TOÀN BỘ hệ thống puzzle (Board, Spawner, UI)
            HideEntirePuzzleSystem();
        }

        private void HandleGameOver()
        {
            _state = PuzzleState.GameOver;
            Debug.Log("[Puzzle] ❌ GAME OVER — no valid moves.");
            OnPuzzleGameOver?.Invoke();
        }

        /// <summary>
        /// Ẩn toàn bộ hệ thống Puzzle (Bao gồm cả Bàn cờ, UI, và nền tối).
        /// </summary>
        private void HideEntirePuzzleSystem()
        {
            // Tắt cái Panel UI
            if (_puzzlePanel != null) _puzzlePanel.SetActive(false);

            // Tắt luôn cái Bàn cờ và Spawner (nếu chúng nó không nằm trong Panel)
            if (_gridBoard != null) _gridBoard.gameObject.SetActive(false);
            if (_blockSpawner != null) _blockSpawner.gameObject.SetActive(false);

            // Nếu sếp có cái Background tối (thường là parent của hệ thống)
            // Em sẽ tắt luôn cái object chứa cái script này (thường là Parent Puzzle System)
            // gameObject.SetActive(false); // Cẩn thận: Nếu script này nằm trên Core thì không được tắt
            
            _state = PuzzleState.Idle;
            Debug.Log("<color=cyan>[Puzzle]</color> Đã dọn dẹp và ẩn toàn bộ hệ thống. Quay lại Farm!");
        }

        private void CleanUpPuzzle()
        {
            if (_gridBoard != null)
            {
                _gridBoard.OnLinesCleared          -= HandleLinesCleared;
                _gridBoard.OnObstacleDestroyed     -= HandleObstacleDestroyed;
                _gridBoard.OnAllObstaclesDestroyed -= HandleAllObstaclesDestroyed;
            }
            if (_blockSpawner != null)
            {
                _blockSpawner.OnBatchExhausted -= HandleBatchExhausted;
                _blockSpawner.OnBlockPlaced    -= HandleBlockPlaced;
            }
        }

        private void OnDestroy() => CleanUpPuzzle();
    }
}
