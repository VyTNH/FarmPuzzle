using UnityEngine;

namespace FarmPuzzle.LandPuzzle.Grid
{
    /// <summary>
    /// Visual cho 1 ô trong puzzle grid. Auto-creates SpriteRenderer + highlight child.
    /// </summary>
    public class GridCell : MonoBehaviour
    {
        [SerializeField] private Vector2Int _gridPosition;

        private SpriteRenderer _spriteRenderer;
        private SpriteRenderer _highlightRenderer;
        private CellState      _state = CellState.Empty;
        private Color          _occupiedColor;

        private static readonly Color Empty      = new Color(0.92f, 0.90f, 0.85f, 1f);
        private static readonly Color ValidHL    = new Color(0.30f, 0.85f, 0.30f, 0.55f);
        private static readonly Color InvalidHL  = new Color(0.90f, 0.25f, 0.25f, 0.55f);

        public Vector2Int GridPosition => _gridPosition;
        public CellState  State        => _state;

        // ── Initialize ──────────────────────────────────────────────────────

        public void Initialize(int row, int col)
        {
            _gridPosition = new Vector2Int(col, row);
            _state        = CellState.Empty;

            // Tìm hoặc tạo SpriteRenderer
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                _spriteRenderer.sprite = MakeSprite();
            }
            _spriteRenderer.sortingOrder = 0;

            // Tạo highlight child
            var hlGo = transform.Find("Highlight");
            if (hlGo == null)
            {
                var go = new GameObject("Highlight");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale    = Vector3.one;
                _highlightRenderer = go.AddComponent<SpriteRenderer>();
                _highlightRenderer.sprite       = MakeSprite();
                _highlightRenderer.color        = Color.clear;
                _highlightRenderer.sortingOrder = 2;
            }
            else
            {
                _highlightRenderer = hlGo.GetComponent<SpriteRenderer>();
            }

            UpdateVisual();
        }

        // ── State ────────────────────────────────────────────────────────────

        public void SetOccupied(Color blockColor)
        {
            _state         = CellState.Occupied;
            _occupiedColor = blockColor;
            UpdateVisual();
        }

        /// <summary>Đánh dấu ô có obstacle — hiển thị với màu obstacle.</summary>
        public void SetObstacle(Color obstacleColor)
        {
            _state         = CellState.Obstacle;
            _occupiedColor = obstacleColor;
            UpdateVisual();
        }

        public void Clear()
        {
            _state         = CellState.Empty;
            _occupiedColor = Color.clear;
            UpdateVisual();
        }

        // ── Highlight ─────────────────────────────────────────────────────────

        public void ShowHighlight(bool isValid)
        {
            if (_highlightRenderer == null) return;
            _highlightRenderer.color = isValid ? ValidHL : InvalidHL;
        }

        public void HideHighlight()
        {
            if (_highlightRenderer == null) return;
            _highlightRenderer.color = Color.clear;
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private void UpdateVisual()
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.color = _state == CellState.Empty ? Empty : _occupiedColor;
        }

        private static Sprite MakeSprite()
        {
            var tex    = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < 16; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
