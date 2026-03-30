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
        [Header("References")]
        [SerializeField] private GridBoard    _gridBoard;
        [SerializeField] private BlockSpawner _blockSpawner;
        [SerializeField] private BlockPuzzleScoring _scoring;
        [SerializeField] private GameObject   _puzzlePanel;   // Panel chứa toàn bộ puzzle UI

        // ── State ──
        private PuzzleState  _state = PuzzleState.Idle;
        private FarmLandTile _currentTargetTile;

        // ── Events ──
        public static System.Action<GridObstacleData, int> OnResourceGained;
        public static System.Action OnPuzzleWin;
        public static System.Action OnPuzzleGameOver;
        public static System.Action<int, int> OnEnergyChanged;

        public PuzzleState CurrentState => _state;
        public int         CurrentScore => _scoring != null ? _scoring.CurrentScore : 0;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_puzzlePanel != null) _puzzlePanel.SetActive(false);
        }

        private void Start()
        {
            // Subscribe farm tiles
            foreach (var tile in FindObjectsByType<FarmLandTile>(FindObjectsSortMode.None))
                tile.OnTileClicked += OnFarmTileClicked;

            // Subscribe energy
            if (EnergySystem.Instance != null)
                EnergySystem.Instance.OnEnergyChanged += (cur, max) => OnEnergyChanged?.Invoke(cur, max);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Gọi trực tiếp (editor test hoặc farm tile click).</summary>
        public void StartPuzzle(LevelData levelData, FarmLandTile targetTile = null)
        {
            if (_state == PuzzleState.Playing) return;

            _currentTargetTile = targetTile;
            _state = PuzzleState.Playing;

            // Hiện puzzle panel
            if (_puzzlePanel != null) _puzzlePanel.SetActive(true);

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

        // ─────────────────────────────────────────────────────────────────────
        // FARM TILE → PUZZLE
        // ─────────────────────────────────────────────────────────────────────

        private void OnFarmTileClicked(FarmLandTile tile)
        {
            if (tile.LevelData == null)
            {
                Debug.LogWarning($"[Puzzle] Tile {tile.FarmGridPosition} chưa có LevelData!");
                return;
            }
            StartPuzzle(tile.LevelData, tile);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GAME EVENTS
        // ─────────────────────────────────────────────────────────────────────

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
            OnResourceGained?.Invoke(data, amount);
            _scoring?.AddObstacleScore(amount);
        }

        private void HandleAllObstaclesDestroyed()
        {
            if (_state == PuzzleState.Win) return;
            _state = PuzzleState.Win;

            Debug.Log("[Puzzle] ✅ WIN! All obstacles cleared.");

            // Mở khóa ô đất đã chọn
            _currentTargetTile?.Unlock();

            OnPuzzleWin?.Invoke();
            CleanUpPuzzle();

            // Ẩn puzzle sau delay nhỏ (có thể play animation trước)
            Invoke(nameof(HidePuzzlePanel), 1.5f);
        }

        private void HandleGameOver()
        {
            _state = PuzzleState.GameOver;
            Debug.Log("[Puzzle] ❌ GAME OVER — no valid moves.");
            OnPuzzleGameOver?.Invoke();
        }

        private void HidePuzzlePanel()
        {
            if (_puzzlePanel != null) _puzzlePanel.SetActive(false);
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
