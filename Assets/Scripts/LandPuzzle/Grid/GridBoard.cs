using System;
using System.Collections.Generic;
using UnityEngine;
using FarmPuzzle.LandPuzzle.Data;

namespace FarmPuzzle.LandPuzzle.Grid
{
    /// <summary>
    /// Quản lý lưới 10×10 Block Puzzle.
    ///  • Đặt block, xóa hàng/cột hoàn chỉnh
    ///  • Obstacle trong grid — bị damage khi hàng/cột liền kề bị xóa
    ///  • Fire events để LandPuzzleManager xử lý win/score/resource
    ///
    /// FIX ROW-FLIP: grid row 0 = DƯỚI cùng (Y thấp), row tăng lên trên.
    ///               shape  row 0 = TRÊN cùng (Y cao),  row tăng xuống dưới.
    ///               → PlaceShape dùng (anchor.y + shape.rows-1-shapeRow) để flip đúng.
    /// </summary>
    public class GridBoard : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int   _gridSize    = 10;
        [SerializeField] private float _cellSize    = 2.0f; // Chỉnh về 2.0 khớp với tỉ lệ Block(Clone)
        [SerializeField] private float _cellMargin  = 0.1f; // 10% lề để lưới khít

        [Header("Prefabs")]
        [SerializeField] private GameObject _cellPrefab;

        // ── Events ──
        /// <param name="clearedRows">Danh sách chỉ số hàng bị xóa</param>
        /// <param name="clearedCols">Danh sách chỉ số cột bị xóa</param>
        public event Action<List<int>, List<int>> OnLinesCleared;

        /// <param name="data">Obstacle vừa bị phá</param>
        /// <param name="resourceAmount">Số tài nguyên thưởng</param>
        public event Action<GridObstacleData, int> OnObstacleDestroyed;

        /// <summary>Tất cả obstacle trong grid đã bị phá.</summary>
        public event Action OnAllObstaclesDestroyed;

        // ── Internal state ──
        private GridCell[,]             _cells;
        private CellState[,]            _logicGrid;
        private GridObstacleInstance[,] _obstacles;
        private int                     _remainingObstacles;

        // ── Properties ──
        public int   GridSize   => _gridSize;
        public float CellSize   => _cellSize;
        public float CellMargin => _cellMargin;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        public void InitializeGrid(LevelData levelData)
        {
            _gridSize = levelData.gridSize;
            ClearGrid();

            _cells     = new GridCell[_gridSize, _gridSize];
            _logicGrid = new CellState[_gridSize, _gridSize];
            _obstacles = new GridObstacleInstance[_gridSize, _gridSize];

            CreateCells();

            // Đặt obstacles
            _remainingObstacles = 0;
            if (levelData.puzzleObstacles != null)
            {
                foreach (var placement in levelData.puzzleObstacles)
                {
                    PlaceObstacle(placement);
                }
            }
        }

        private void CreateCells()
        {
            // Tỷ lệ cố định: Khoảng cách giữa các tâm ô (Step)
            float step = _cellSize * (1f + _cellMargin);
            
            // Tính toán điểm bắt đầu (Local) sao cho lưới nằm giữa bàn cờ
            float totalSize = (_gridSize - 1) * step;
            Vector3 startOffset = new Vector3(-totalSize / 2f, -totalSize / 2f, 0f);

            for (int row = 0; row < _gridSize; row++)
            {
                for (int col = 0; col < _gridSize; col++)
                {
                    // Sử dụng Local Position thay vì World Position
                    Vector3 localPos = startOffset + new Vector3(col * step, row * step, 0f);

                    GameObject cellObj;
                    if (_cellPrefab != null)
                    {
                        // Khởi tạo và đưa vào làm con của GridBoard ngay lập tức
                        cellObj = Instantiate(_cellPrefab, transform);
                        cellObj.transform.localPosition = localPos;
                    }
                    else
                    {
                        cellObj = new GameObject($"Cell_{row}_{col}");
                        cellObj.transform.SetParent(transform, false);
                        cellObj.transform.localPosition = localPos;
                    }

                    // Ép Scale cố định dựa trên CellSize để lấp đầy không gian
                    cellObj.transform.localScale = Vector3.one * _cellSize;

                    var cell = cellObj.GetComponent<GridCell>() ?? cellObj.AddComponent<GridCell>();
                    cell.Initialize(row, col);
                    _cells[row, col]     = cell;
                    _logicGrid[row, col] = CellState.Empty;
                }
            }
        }

        private void PlaceObstacle(ObstaclePlacement placement)
        {
            int r = placement.gridRow;
            int c = placement.gridCol;
            if (!IsInBounds(r, c) || placement.obstacleData == null) return;

            _logicGrid[r, c] = CellState.Obstacle;

            // Tạo obstacle instance
            var inst = _cells[r, c].gameObject.AddComponent<GridObstacleInstance>();
            inst.Initialize(placement.obstacleData, _cellSize);
            _obstacles[r, c] = inst;
            _remainingObstacles++;

            // Visual cho cell
            _cells[r, c].SetObstacle(placement.obstacleData.obstacleColor);
        }

        private void ClearGrid()
        {
            if (_cells == null) return;
            for (int r = 0; r < _cells.GetLength(0); r++)
                for (int c = 0; c < _cells.GetLength(1); c++)
                    if (_cells[r, c] != null) Destroy(_cells[r, c].gameObject);
        }

        // =====================================================================
        // BLOCK PLACEMENT  (FIX: shape row 0 = TOP = high grid row)
        // =====================================================================

        /// <summary>
        /// anchor = (col, row) của ô DƯỚI-TRÁI của shape trong grid.
        /// Shape row 0 (top visual) → gridRow = anchor.y + shape.rows - 1 (cao nhất).
        /// Shape row n-1 (bottom visual) → gridRow = anchor.y (thấp nhất).
        /// </summary>
        public bool CanPlaceShape(ShapeData shape, Vector2Int anchor)
        {
            for (int sr = 0; sr < shape.rows; sr++)
            {
                for (int sc = 0; sc < shape.columns; sc++)
                {
                    if (!shape.GetCell(sr, sc)) continue;

                    // Flip: shapeRow 0 → highest grid row
                    int gridRow = anchor.y + (shape.rows - 1 - sr);
                    int gridCol = anchor.x + sc;

                    if (!IsInBounds(gridRow, gridCol)) return false;
                    if (_logicGrid[gridRow, gridCol] != CellState.Empty) return false;
                }
            }
            return true;
        }

        public bool CanPlaceShapeAnywhere(ShapeData shape)
        {
            for (int r = 0; r < _gridSize; r++)
                for (int c = 0; c < _gridSize; c++)
                    if (CanPlaceShape(shape, new Vector2Int(c, r))) return true;
            return false;
        }

        public void PlaceShape(ShapeData shape, Vector2Int anchor)
        {
            for (int sr = 0; sr < shape.rows; sr++)
            {
                for (int sc = 0; sc < shape.columns; sc++)
                {
                    if (!shape.GetCell(sr, sc)) continue;

                    int gridRow = anchor.y + (shape.rows - 1 - sr);
                    int gridCol = anchor.x + sc;

                    _logicGrid[gridRow, gridCol] = CellState.Occupied;
                    _cells[gridRow, gridCol].SetOccupied(shape.blockColor);
                }
            }
        }

        // =====================================================================
        // LINE CLEARING
        // =====================================================================

        public int CheckAndClearLines()
        {
            var completedRows = new List<int>();
            var completedCols = new List<int>();

            for (int r = 0; r < _gridSize; r++)
                if (IsRowComplete(r)) completedRows.Add(r);
            for (int c = 0; c < _gridSize; c++)
                if (IsColumnComplete(c)) completedCols.Add(c);

            if (completedRows.Count + completedCols.Count == 0) return 0;

            // Tìm obstacles tiếp xúc với dòng bị xóa TRƯỚC KHI xóa
            DamageAdjacentObstacles(completedRows, completedCols);

            foreach (int r in completedRows) ClearRow(r);
            foreach (int c in completedCols) ClearColumn(c);

            OnLinesCleared?.Invoke(completedRows, completedCols);
            return completedRows.Count + completedCols.Count;
        }

        private bool IsRowComplete(int row)
        {
            for (int c = 0; c < _gridSize; c++)
                if (_logicGrid[row, c] != CellState.Occupied) return false;
            return true;
        }

        private bool IsColumnComplete(int col)
        {
            for (int r = 0; r < _gridSize; r++)
                if (_logicGrid[r, col] != CellState.Occupied) return false;
            return true;
        }

        private void ClearRow(int row)
        {
            for (int c = 0; c < _gridSize; c++)
                if (_logicGrid[row, c] == CellState.Occupied)
                {
                    _logicGrid[row, c] = CellState.Empty;
                    _cells[row, c].Clear();
                }
        }

        private void ClearColumn(int col)
        {
            for (int r = 0; r < _gridSize; r++)
                if (_logicGrid[r, col] == CellState.Occupied)
                {
                    _logicGrid[r, col] = CellState.Empty;
                    _cells[r, col].Clear();
                }
        }

        // =====================================================================
        // ADJACENT OBSTACLE DAMAGE
        // =====================================================================

        private void DamageAdjacentObstacles(List<int> clearedRows, List<int> clearedCols)
        {
            var damagedPositions = new HashSet<int>(); // row*100+col

            void TryDamage(int r, int c)
            {
                if (!IsInBounds(r, c)) return;
                if (_logicGrid[r, c] != CellState.Obstacle) return;
                int key = r * 100 + c;
                if (damagedPositions.Contains(key)) return;
                damagedPositions.Add(key);

                var inst = _obstacles[r, c];
                if (inst == null || inst.IsDestroyed) return;

                bool destroyed = inst.TakeDamage();
                if (destroyed)
                {
                    _logicGrid[r, c] = CellState.Empty;
                    _cells[r, c].Clear();  // Reset visual — cell trở thành bình thường
                    OnObstacleDestroyed?.Invoke(inst.Data, inst.Data.resourceAmount);

                    _remainingObstacles--;
                    if (_remainingObstacles <= 0)
                        OnAllObstaclesDestroyed?.Invoke();
                }
            }

            foreach (int row in clearedRows)
            {
                for (int c = 0; c < _gridSize; c++)
                {
                    TryDamage(row - 1, c);
                    TryDamage(row + 1, c);
                }
            }

            foreach (int col in clearedCols)
            {
                for (int r = 0; r < _gridSize; r++)
                {
                    TryDamage(r, col - 1);
                    TryDamage(r, col + 1);
                }
            }
        }

        // =====================================================================
        // HIGHLIGHT PREVIEW
        // =====================================================================

        public void ShowPlacementPreview(ShapeData shape, Vector2Int anchor)
        {
            ClearAllHighlights();
            bool canPlace = CanPlaceShape(shape, anchor);

            for (int sr = 0; sr < shape.rows; sr++)
                for (int sc = 0; sc < shape.columns; sc++)
                {
                    if (!shape.GetCell(sr, sc)) continue;
                    int gridRow = anchor.y + (shape.rows - 1 - sr);
                    int gridCol = anchor.x + sc;
                    if (IsInBounds(gridRow, gridCol))
                        _cells[gridRow, gridCol].ShowHighlight(canPlace);
                }
        }

        public void ClearAllHighlights()
        {
            if (_cells == null) return;
            for (int r = 0; r < _gridSize; r++)
                for (int c = 0; c < _gridSize; c++)
                    _cells[r, c]?.HideHighlight();
        }

        // =====================================================================
        // COORDINATE MAPPING
        // =====================================================================

        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            // Chuyển tọa độ thế giới về tọa độ địa phương của GridBoard
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            
            float step = _cellSize * (1f + _cellMargin);
            float totalSize = (_gridSize - 1) * step;
            Vector3 startOffset = new Vector3(-totalSize / 2f, -totalSize / 2f, 0f);

            Vector3 relativePos = localPos - startOffset;
            
            // Cộng thêm nửa bước để lấy tâm ô chính xác hơn
            int col = Mathf.RoundToInt(relativePos.x / step);
            int row = Mathf.RoundToInt(relativePos.y / step);
            
            return new Vector2Int(col, row);
        }

        public Vector3 GridToWorldPosition(int row, int col)
        {
            float step = _cellSize * (1f + _cellMargin);
            float totalSize = (_gridSize - 1) * step;
            Vector3 startOffset = new Vector3(-totalSize / 2f, -totalSize / 2f, 0f);

            Vector3 localPos = startOffset + new Vector3(col * step, row * step, 0f);
            return transform.TransformPoint(localPos);
        }

        public bool IsInBounds(int row, int col)
            => row >= 0 && row < _gridSize && col >= 0 && col < _gridSize;

        public GridCell  GetCell(int row, int col)      => IsInBounds(row, col) ? _cells[row, col] : null;
        public CellState GetCellState(int row, int col) => IsInBounds(row, col) ? _logicGrid[row, col] : CellState.Occupied;

        public bool HasObstaclesRemaining => _remainingObstacles > 0;
        public int  RemainingObstacles    => _remainingObstacles;

        private void OnDestroy() => ClearGrid();
    }
}
