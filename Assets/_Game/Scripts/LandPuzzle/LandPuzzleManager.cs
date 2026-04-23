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
            img.color = new Color(0.8f, 0.2f, 0.2f); 

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
            CleanUpPuzzle();
            if (_puzzlePanel != null) _puzzlePanel.SetActive(false);
            _state = PuzzleState.Idle;
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
