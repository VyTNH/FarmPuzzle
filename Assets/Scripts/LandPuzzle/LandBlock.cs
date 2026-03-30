using UnityEngine;
using UnityEngine.EventSystems;
using FarmPuzzle.LandPuzzle.Data;
using FarmPuzzle.LandPuzzle.Grid;

namespace FarmPuzzle.LandPuzzle.Block
{
    /// <summary>
    /// Component gắn vào mỗi block piece. Xử lý drag & drop vào grid.
    ///
    /// Yêu cầu setup trong scene:
    /// - Camera: có Physics2DRaycaster
    /// - Scene: có EventSystem + Physics2DRaycaster trên Camera
    /// - Block GameObject: có BoxCollider2D (tự cập nhật sau BuildVisual)
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class LandBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer[] _cellRenderers;

        // --- Data ---
        private ShapeData _shapeData;
        private GridBoard _gridBoard;
        private bool _isPlaced = false;
        private bool _isDragging = false;

        // --- Drag State ---
        private Vector3 _originalPosition;
        private Vector3 _originalScale;

        [Header("Drag Settings")]
        [Tooltip("Offset theo trục Y khi kéo (nâng block lên khỏi ngón tay)")]
        [SerializeField] private float _dragYOffset = 1.5f;
        [SerializeField] private float _dragScale = 1.1f;
        [SerializeField] private int _dragSortingOrder = 100;
        [SerializeField] private float _snapAnimSpeed = 20f;

        // --- Callbacks ---
        public System.Action<LandBlock, ShapeData, Vector2Int> OnBlockPlaced;

        // --- Properties ---
        public ShapeData ShapeData => _shapeData;
        public bool IsPlaced => _isPlaced;

        // =====================================================================
        // INITIALIZE
        // =====================================================================

        public void Initialize(ShapeData shapeData, GridBoard gridBoard)
        {
            _shapeData = shapeData;
            _gridBoard = gridBoard;
            _isPlaced = false;
            _originalPosition = transform.position;
            _originalScale = transform.localScale;

            BuildVisual();
        }

        // =====================================================================
        // VISUAL BUILD
        // =====================================================================

        private void BuildVisual()
        {
            // Xóa visual cũ
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            float cellSize = _gridBoard != null ? _gridBoard.CellSize : 1f;
            float spacing  = _gridBoard != null ? _gridBoard.CellSpacing : 0.05f;
            float step     = cellSize + spacing;

            int activeCount = _shapeData.GetActiveCellCount();
            _cellRenderers  = new SpriteRenderer[activeCount];
            int idx = 0;

            for (int r = 0; r < _shapeData.rows; r++)
            {
                for (int c = 0; c < _shapeData.columns; c++)
                {
                    if (!_shapeData.GetCell(r, c)) continue;

                    GameObject cellObj = new GameObject($"Cell_{r}_{c}");
                    cellObj.transform.SetParent(transform, false);

                    // Vị trí local: col đi phải, row đi xuống
                    cellObj.transform.localPosition = new Vector3(c * step, -r * step, 0f);
                    cellObj.transform.localScale     = Vector3.one * cellSize;

                    var sr       = cellObj.AddComponent<SpriteRenderer>();
                    sr.sprite    = MakeWhiteSprite();
                    sr.color     = _shapeData.blockColor;
                    sr.sortingOrder = 1;

                    _cellRenderers[idx++] = sr;
                }
            }

            // Căn pivot về center của toàn shape
            CenterPivotOffset(cellSize, spacing);

            // Cập nhật BoxCollider2D bao phủ toàn shape
            UpdateCollider(cellSize, spacing);
        }

        /// <summary>
        /// Dịch chuyển các cell con để center nằm ở (0,0) của parent.
        /// </summary>
        private void CenterPivotOffset(float cellSize, float spacing)
        {
            float step    = cellSize + spacing;
            float offsetX = (_shapeData.columns - 1) * step / 2f;
            float offsetY = (_shapeData.rows    - 1) * step / 2f;

            foreach (Transform child in transform)
                child.localPosition -= new Vector3(offsetX, -offsetY, 0f);
        }

        /// <summary>
        /// Resize BoxCollider2D để bao phủ toàn bộ shape.
        /// </summary>
        private void UpdateCollider(float cellSize, float spacing)
        {
            var col = GetComponent<BoxCollider2D>();
            float step = cellSize + spacing;
            col.size   = new Vector2(_shapeData.columns * step - spacing,
                                     _shapeData.rows    * step - spacing);
            col.offset = Vector2.zero; // center đã được căn ở BuildVisual
        }

        // =====================================================================
        // DRAG & DROP
        // =====================================================================

        public void OnPointerDown(PointerEventData eventData) { }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isPlaced) return;

            _isDragging       = true;
            _originalPosition = transform.position;
            _originalScale    = transform.localScale;

            transform.localScale = _originalScale * _dragScale;
            SetSortingOrder(_dragSortingOrder);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isPlaced || !_isDragging) return;

            // ─── Vị trí cursor trong world space ───
            Vector3 cursorWorld = GetCursorWorldPos(eventData);

            // ─── Nâng block lên khỏi ngón tay ───
            Vector3 blockWorld  = cursorWorld + new Vector3(0f, _dragYOffset, 0f);
            transform.position  = blockWorld;

            // ─── Preview: anchor = góc trên-trái của shape trong grid ───
            Vector2Int anchor = GetAnchorFromBlockCenter(blockWorld);
            _gridBoard.ShowPlacementPreview(_shapeData, anchor);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isPlaced || !_isDragging) return;

            _isDragging = false;
            _gridBoard.ClearAllHighlights();

            Vector3 cursorWorld = GetCursorWorldPos(eventData);
            Vector3 blockWorld  = cursorWorld + new Vector3(0f, _dragYOffset, 0f);
            Vector2Int anchor   = GetAnchorFromBlockCenter(blockWorld);

            Debug.Log($"[LandBlock] Drop at screenPos={eventData.position} → cursorWorld={cursorWorld:F2} → blockWorld={blockWorld:F2} → anchor={anchor}");

            if (_gridBoard.CanPlaceShape(_shapeData, anchor))
            {
                Debug.Log($"[LandBlock] ✅ Placed shape '{_shapeData.name}' at anchor {anchor}");
                _gridBoard.PlaceShape(_shapeData, anchor);
                _isPlaced = true;
                OnBlockPlaced?.Invoke(this, _shapeData, anchor);
                StartCoroutine(SnapAndHide());
            }
            else
            {
                Debug.Log($"[LandBlock] ❌ Cannot place at {anchor} — returning to origin");
                StartCoroutine(ReturnToOrigin());
            }
        }

        // =====================================================================
        // COORDINATE HELPERS
        // =====================================================================

        /// <summary>
        /// Lấy vị trí cursor trong world space (Camera ortho / perspective đều đúng).
        /// </summary>
        private Vector3 GetCursorWorldPos(PointerEventData eventData)
        {
            Vector3 screenPos = eventData.position;
            screenPos.z = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 world = Camera.main.ScreenToWorldPoint(screenPos);
            world.z = 0f;
            return world;
        }

        /// <summary>
        /// Tính anchor (góc DƯỚI-TRÁI của shape trong grid coords).
        ///
        /// GridBoard.PlaceShape mapping:
        ///   gridRow = anchor.y + (shape.rows - 1 - shapeRow)
        ///   → anchor.y = gridRow tương ứng với shapeRow CUỐI (bottom visual = lowest grid row)
        ///
        /// Vì blockCenter trong world → centerCell (col, row),
        /// và bottom của shape = center - (rows-1)/2 grid rows:
        ///   anchorRow = centerCell.y - (shape.rows - 1) / 2
        /// </summary>
        private Vector2Int GetAnchorFromBlockCenter(Vector3 blockCenterWorld)
        {
            Vector2Int centerCell = _gridBoard.WorldToGridPosition(blockCenterWorld);

            // anchor.y = bottom row of shape in grid (shape bottom-visual = lowest gridRow)
            int anchorCol = centerCell.x - Mathf.FloorToInt(_shapeData.columns / 2f);
            int anchorRow = centerCell.y - Mathf.FloorToInt((_shapeData.rows - 1) / 2f);

            return new Vector2Int(anchorCol, anchorRow);
        }

        // =====================================================================
        // ANIMATIONS
        // =====================================================================

        private System.Collections.IEnumerator SnapAndHide()
        {
            float t = 0f;
            Vector3 startScale = transform.localScale;
            while (t < 1f)
            {
                t += Time.deltaTime * _snapAnimSpeed;
                transform.localScale = Vector3.Lerp(startScale, _originalScale, Mathf.Clamp01(t));
                yield return null;
            }
            gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator ReturnToOrigin()
        {
            float t = 0f;
            Vector3 startPos   = transform.position;
            Vector3 startScale = transform.localScale;

            while (t < 1f)
            {
                t += Time.deltaTime * _snapAnimSpeed;
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                transform.position   = Vector3.Lerp(startPos,   _originalPosition, s);
                transform.localScale = Vector3.Lerp(startScale, _originalScale,    s);
                yield return null;
            }

            transform.position   = _originalPosition;
            transform.localScale = _originalScale;
            SetSortingOrder(1);
        }

        // =====================================================================
        // UTILITY
        // =====================================================================

        private void SetSortingOrder(int order)
        {
            if (_cellRenderers == null) return;
            foreach (var sr in _cellRenderers)
                if (sr != null) sr.sortingOrder = order;
        }

        private static Sprite MakeWhiteSprite()
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[16];
            for (int i = 0; i < 16; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
