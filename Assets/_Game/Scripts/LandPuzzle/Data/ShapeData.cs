using UnityEngine;

namespace FarmPuzzle.LandPuzzle.Data
{
    /// <summary>
    /// ScriptableObject định nghĩa hình dạng của 1 block piece.
    /// Cells là mảng 1 chiều flatten từ 2D (row-major order).
    /// Ví dụ: shape chữ L (2x2) → rows=2, columns=2, cells={true, false, true, true}
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Shape", menuName = "FarmPuzzle/Block Puzzle/Shape Data")]
    public class ShapeData : ScriptableObject
    {
        [Tooltip("Số hàng của shape")]
        public int rows = 1;

        [Tooltip("Số cột của shape")]
        public int columns = 1;

        [Tooltip("Mảng 1D (row-major). true = ô có block, false = ô trống")]
        public bool[] cells;

        [Tooltip("Màu block khi hiển thị trên grid")]
        public Color blockColor = Color.cyan;

        /// <summary>
        /// Kiểm tra ô (row, col) có phải ô block hay không.
        /// </summary>
        public bool GetCell(int row, int col)
        {
            if (row < 0 || row >= rows || col < 0 || col >= columns)
                return false;

            int index = row * columns + col;
            if (index < 0 || index >= cells.Length)
                return false;

            return cells[index];
        }

        /// <summary>
        /// Đếm tổng số ô block trong shape.
        /// </summary>
        public int GetActiveCellCount()
        {
            int count = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i]) count++;
            }
            return count;
        }

        /// <summary>
        /// Validate dữ liệu shape.
        /// </summary>
        private void OnValidate()
        {
            if (rows < 1) rows = 1;
            if (columns < 1) columns = 1;

            int expectedLength = rows * columns;
            if (cells == null || cells.Length != expectedLength)
            {
                cells = new bool[expectedLength];
            }
        }
    }
}
