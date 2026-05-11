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
        
        public bool IsPuzzleActive => _state != PuzzleState.Idle;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (_puzzlePanel != null) _puzzlePanel.SetActive(false);
        }

        private void Start()
        {
            if (EnergySystem.Instance != null)
                EnergySystem.Instance.OnEnergyChanged += (cur, max) => OnEnergyChanged?.Invoke(cur, max);
        }

        private void BuildExitButton()
        {
            if (_puzzlePanel == null)
            {
                Debug.LogError("[LandPuzzle] BuildExitButton: _puzzlePanel == null! Gán Puzzle_UI_Panel vào Inspector.");
                return;
            }

            // ── Tìm Canvas đúng để đặt nút (ưu tiên Canvas_LandPuzzle) ──
            Canvas targetCanvas = null;

            // Ưu tiên 1: Canvas_LandPuzzle theo tên — đây là Canvas chuyên cho puzzle UI
            var puzzleCanvasGO = GameObject.Find("Canvas_LandPuzzle");
            if (puzzleCanvasGO != null)
                targetCanvas = puzzleCanvasGO.GetComponent<Canvas>();

            // Ưu tiên 2: Canvas cha gần nhất không phải WorldSpace
            if (targetCanvas == null)
            {
                Canvas[] parents = _puzzlePanel.GetComponentsInParent<Canvas>(true);
                foreach (var c in parents)
                {
                    if (c.renderMode != RenderMode.WorldSpace)
                    { targetCanvas = c; break; }
                }
            }

            // Ưu tiên 3: Canvas anh em (sibling) của _puzzlePanel
            if (targetCanvas == null && _puzzlePanel.transform.parent != null)
            {
                foreach (Transform sibling in _puzzlePanel.transform.parent)
                {
                    var c = sibling.GetComponent<Canvas>();
                    if (c != null && c.renderMode != RenderMode.WorldSpace)
                    { targetCanvas = c; break; }
                }
            }

            if (targetCanvas == null)
            {
                Debug.LogError("[LandPuzzle] Không tìm được Canvas! Đặt tên canvas puzzle thành 'Canvas_LandPuzzle'.");
                return;
            }

            // Kiểm tra nút đã tồn tại trong Canvas chưa
            var existingT = targetCanvas.transform.Find("Btn_ExitLandPuzzle");
            if (existingT != null)
            {
                existingT.gameObject.SetActive(true);
                existingT.SetAsLastSibling();
                return;
            }

            // ★ BắT BUỘC: Canvas phải có GraphicRaycaster thì button mới nhận được click!
            if (targetCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                targetCanvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Debug.Log($"[LandPuzzle] Đã thêm GraphicRaycaster vào '{targetCanvas.name}' → button mới nhận được click!");
            }

            Debug.Log($"[LandPuzzle] Tạo nút thoát trong Canvas: '{targetCanvas.name}' (RenderMode={targetCanvas.renderMode})");

            // Tạo nút
            GameObject btnObj = new GameObject("Btn_ExitLandPuzzle",
                typeof(RectTransform),
                typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.Button));
            btnObj.transform.SetParent(targetCanvas.transform, false);
            btnObj.transform.SetAsLastSibling();

            // Vị trí: Góc trên PHẢI
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -200f); // Pos Y = -200 theo yêu cầu
            rt.sizeDelta        = new Vector2(70f, 70f);

            var img   = btnObj.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.85f, 0.15f, 0.15f, 0.95f);

            // Click: Dùng Instance thạy vì closure có thể stale
            var btn = btnObj.GetComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(() =>
            {
                Debug.Log("[LandPuzzle] Nút thoát được nhấn!");
                var manager = LandPuzzleManager.Instance;
                if (manager != null)
                    manager.ExitPuzzle();
                else
                    Debug.LogError("[LandPuzzle] LandPuzzleManager.Instance == null khi thoát!");
                btnObj.SetActive(false); // Ẩn đi để tái sử dụng lần sau
            });

            // Chữ ✕
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var txt       = txtObj.GetComponent<UnityEngine.UI.Text>();
            txt.text      = "✕";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 36;
            txt.color     = Color.white;
            txt.fontStyle = FontStyle.Bold;

            Debug.Log($"<color=lime>[LandPuzzle]</color> ✅ Nút thoát đã tạo trong '{targetCanvas.name}'");
        }

        /// <summary>Gọi trực tiếp (editor test hoặc farm tile click).</summary>
        public void StartPuzzle(LevelData levelData, LandPlot targetTile = null)
        {
            if (_state == PuzzleState.Playing) return;

            // MỚI: Reset Zoom camera về 0 (MaxZoom) khi vào game puzzle
            if (CameraDrag.Instance != null)
            {
                CameraDrag.Instance.ResetZoom();
            }

            _currentTargetTile = targetTile;
            _state = PuzzleState.Playing;

            if (_puzzlePanel != null) 
            {
                _puzzlePanel.SetActive(true);
                BuildExitButton();
            }

            // QUAN TRỌNG: Bật lại GridBoard & BlockSpawner vì HideEntirePuzzleSystem() đã tắt chúng sau khi thắng
            if (_gridBoard != null) _gridBoard.gameObject.SetActive(true);
            if (_blockSpawner != null) _blockSpawner.gameObject.SetActive(true);

            _gridBoard.InitializeGrid(levelData);
            _gridBoard.OnLinesCleared       += HandleLinesCleared;
            _gridBoard.OnObstacleDestroyed  += HandleObstacleDestroyed;
            _gridBoard.OnAllObstaclesDestroyed += HandleAllObstaclesDestroyed;

            _blockSpawner.Initialize(levelData, _gridBoard);
            _blockSpawner.OnBatchExhausted += HandleBatchExhausted;
            _blockSpawner.OnBlockPlaced    += HandleBlockPlaced;
            _blockSpawner.SpawnNewBatch();

            _scoring?.ResetScore();

            if (Camera.main != null)
            {
                var camPos = Camera.main.transform.position;
                transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
            }

            Debug.Log($"[Puzzle] Started level {levelData.levelId}");
        }

        public void RetryPuzzle()
        {
            if (_currentTargetTile != null && _currentTargetTile.puzzleLevel != null)
            {
                ExitPuzzle();
                StartPuzzle(_currentTargetTile.puzzleLevel, _currentTargetTile);
            }
        }

        public void ExitPuzzle()
        {
            Debug.Log("<color=orange>[Puzzle]</color> ExitPuzzle: Thoát không có thưởng → Ẩn toàn bộ hệ thống.");
            CleanUpPuzzle();
            HideEntirePuzzleSystem(); // Tắt _puzzlePanel + _gridBoard + _blockSpawner + set Idle
        }

        private void HandleBlockPlaced(ShapeData shape, Vector2Int anchor)
        {
            if (_state != PuzzleState.Playing) return;
            _scoring?.AddPlacementScore(shape.GetActiveCellCount());

            int linesCleared = _gridBoard.CheckAndClearLines();
            if (linesCleared > 0)
            {
                _scoring?.AddLineClearScore(linesCleared);
            }
        }

        private void HandleBatchExhausted()
        {
            if (_state != PuzzleState.Playing) return;

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

            if (DataManager.Instance != null) DataManager.Instance.CommitSessionInventory();

            if (_currentTargetTile != null) 
            {
                _currentTargetTile.UnlockPlot();
                
                if (FarmPuzzle.FarmSystem.GridManager.Instance != null) 
                {
                    int dirtTileIndex = UnityEngine.Random.Range(6, 12);
                    FarmPuzzle.FarmSystem.GridManager.Instance.ChangeTileArtState(_currentTargetTile, dirtTileIndex);
                    FarmPuzzle.FarmSystem.GridManager.Instance.SavePlotState(_currentTargetTile);
                    
                    Debug.Log($"<color=magenta>[Giai Đoạn 4: Lưu KQ & Cập nhật Graphic]</color> Ghi Đè Database: Đất {_currentTargetTile.plotID} ĐÃ MỞ KHÓA -> Vẽ Lại Graphic Thành Đất Trồng!");
                }
            }

            OnPuzzleWin?.Invoke();
            CleanUpPuzzle();
            HideEntirePuzzleSystem();
        }

        private void HandleGameOver()
        {
            _state = PuzzleState.GameOver;
            Debug.Log("[Puzzle] ❌ GAME OVER — no valid moves.");
            OnPuzzleGameOver?.Invoke();
        }

        private void HideEntirePuzzleSystem()
        {
            if (_puzzlePanel != null) _puzzlePanel.SetActive(false);
            if (_gridBoard != null) _gridBoard.gameObject.SetActive(false);
            if (_blockSpawner != null) _blockSpawner.gameObject.SetActive(false);
            
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
