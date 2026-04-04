using UnityEngine;
using FarmPuzzle.LandPuzzle.Data;

namespace FarmPuzzle.LandPuzzle.Grid
{
    /// <summary>
    /// Runtime state của 1 chướng ngại vật trong puzzle grid.
    /// Gắn vào cùng GameObject với GridCell.
    /// </summary>
    public class GridObstacleInstance : MonoBehaviour
    {
        private GridObstacleData _data;
        private int              _currentDurability;
        private SpriteRenderer   _obstacleRenderer;

        // Màu theo durability
        private static readonly Color[] DurabilityColors = {
            new Color(0.95f, 0.85f, 0.4f),   // 1 HP — vàng
            new Color(0.75f, 0.55f, 0.3f),   // 2 HP — nâu
            new Color(0.5f,  0.5f,  0.5f),   // 3 HP — xám
            new Color(0.35f, 0.25f, 0.25f),  // 4 HP — tối
            new Color(0.2f,  0.15f, 0.15f),  // 5 HP — đen
        };

        public bool IsDestroyed     => _currentDurability <= 0;
        public GridObstacleData Data => _data;

        /// <summary>Tải dữ liệu obstacle và tạo renderer.</summary>
        public void Initialize(GridObstacleData data, float cellSize)
        {
            _data              = data;
            _currentDurability = data.maxDurability;

            // Tạo hoặc lấy renderer
            _obstacleRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_obstacleRenderer == null)
            {
                var go = new GameObject("ObstacleVisual");
                go.transform.SetParent(transform, false);
                _obstacleRenderer = go.AddComponent<SpriteRenderer>();
                _obstacleRenderer.sprite = MakeSprite();
            }

            _obstacleRenderer.sortingLayerName = "Puzzle";
            _obstacleRenderer.sortingOrder = 2;
            UpdateColor();
        }

        /// <summary>Nhận sát thương 1 lần. Trả về true nếu bị phá hủy.</summary>
        public bool TakeDamage()
        {
            if (_currentDurability <= 0) return true;
            _currentDurability--;
            UpdateColor();

            if (_currentDurability <= 0)
            {
                // Đặt renderer thành transparent - cell sẽ hiển thị như bình thường
                if (_obstacleRenderer != null)
                    _obstacleRenderer.color = Color.clear;
                return true;
            }
            return false;
        }

        private void UpdateColor()
        {
            if (_obstacleRenderer == null) return;
            int idx = Mathf.Clamp(_currentDurability - 1, 0, DurabilityColors.Length - 1);
            _obstacleRenderer.color = DurabilityColors[idx];
        }

        private static Sprite MakeSprite()
        {
            var tex    = new Texture2D(6, 6, TextureFormat.RGBA32, false);
            var pixels = new Color32[36];
            for (int i = 0; i < 36; i++) pixels[i] = new Color32(200, 200, 200, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 6, 6), new Vector2(0.5f, 0.5f), 6f);
        }
    }
}
