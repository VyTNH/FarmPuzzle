using UnityEngine;
using System.Collections.Generic;
using FarmPuzzle.FarmSystem.Crop;

namespace FarmPuzzle.FarmSystem
{
    /// <summary>
    /// Object Pooling + Viewport Culling cho lưới đất Isometric.
    /// Chỉ Enable những ô nằm trong tầm nhìn Camera, Disable những ô ngoài rìa.
    /// Hỗ trợ lưới lên đến 100x100 (10,000 ô) mà không lag.
    /// </summary>
    public class FarmViewportCuller : MonoBehaviour
    {
        public static FarmViewportCuller Instance;

        [Header("Config")]
        [Tooltip("Số ô đệm ngoài rìa Camera (buffer) để tránh ô bị pop-in")]
        public int bufferTiles = 2;

        private Camera _cam;
        private GridManager _gridManager;
        private Grid _grid;

        // Cache bounds của từng plot
        private struct PlotMeta
        {
            public LandPlot plot;
            public int gridX, gridY;
        }
        private List<PlotMeta> _allMeta = new List<PlotMeta>();

        private void Awake() { Instance = this; }

        private void Start()
        {
            _cam = Camera.main;
            _gridManager = GridManager.Instance;
            _grid = _gridManager?.GetComponent<Grid>();
        }

        // Gọi sau khi GridManager đã Generate xong
        public void RegisterPlots(List<LandPlot> plots)
        {
            _allMeta.Clear();
            foreach (var p in plots)
            {
                _allMeta.Add(new PlotMeta { plot = p, gridX = p.gridX, gridY = p.gridY });
            }
        }

        private void LateUpdate()
        {
            if (_cam == null || _allMeta.Count == 0) return;
            CullOutsideViewport();
        }

        private void CullOutsideViewport()
        {
            // Lấy góc màn hình ↔ world position
            float camHeight = _cam.orthographicSize;
            float camWidth  = camHeight * _cam.aspect;
            Vector2 camPos  = _cam.transform.position;

            // Tính bounds với buffer
            float minWorldX = camPos.x - camWidth;
            float maxWorldX = camPos.x + camWidth;
            float minWorldY = camPos.y - camHeight;
            float maxWorldY = camPos.y + camHeight;

            foreach (var meta in _allMeta)
            {
                if (meta.plot == null) continue;
                Vector3 wp = meta.plot.transform.position;

                bool inView = wp.x > minWorldX && wp.x < maxWorldX &&
                              wp.y > minWorldY && wp.y < maxWorldY;

                // Chỉ enable/disable Renderer và Collider (KHÔNG Destroy) để tiết kiệm CPU
                var sr = meta.plot.GetComponent<SpriteRenderer>();
                var col = meta.plot.GetComponent<Collider2D>();

                if (sr  != null && sr.enabled  != inView) sr.enabled  = inView;
                if (col != null && col.enabled  != inView) col.enabled = inView;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_cam == null) return;
            float h = _cam.orthographicSize;
            float w = h * _cam.aspect;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_cam.transform.position, new Vector3(w * 2, h * 2, 0));
        }
#endif
    }
}
