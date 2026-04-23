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
    // [MỚI] Đã gỡ bỏ RequireComponent BoxCollider2D để dùng Composite Collider xịn hơn
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
        [SerializeField] private float _dragYOffset = 0f; // Bỏ Offset kéo để trùng 100% tay người chơi
        [SerializeField] private float _dragScale = 1.0f; // Kích thước giữ nguyên 100% khi kéo
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
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            float cellSize = _gridBoard != null ? _gridBoard.CellSize : 2.0f;
            float margin = _gridBoard != null ? _gridBoard.CellMargin : 0.1f;

            float step = cellSize * (1f + margin);
            float spacing = step - cellSize;

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
                    cellObj.transform.localScale     = Vector3.one * cellSize; // Đảm bảo cell nhận đúng CellSize

                    var sr       = cellObj.AddComponent<SpriteRenderer>();
                    sr.sprite    = MakeWhiteSprite();
                    sr.color     = _shapeData.blockColor;
                    sr.sortingLayerName = "Puzzle"; // Đảm bảo luôn nằm ở layer Puzzle
                    sr.sortingOrder = 1;

                    // MỚI: Thêm Collider cho từng ô con và đánh dấu dùng cho Composite (Chuẩn Unity 6)
                    var box = cellObj.AddComponent<BoxCollider2D>();
                    box.compositeOperation = Collider2D.CompositeOperation.Merge;

                    _cellRenderers[idx++] = sr;
                }
            }
            // 🔍 Kiểm tra Scale đồng nhất để đảm bảo căn chỉnh đúng
            if (!transform.TryGetUniformScale(out float s))
            {
                Debug.LogError($"[LandBlock] Scale của '{gameObject.name}' không đồng nhất! (X:{transform.localScale.x}, Y:{transform.localScale.y}). Vui lòng để Vector3(x, x, x).");
                return;
            }

            // Căn pivot về center của toàn shape (Dùng một nửa scale đồng nhất sếp đã kiểm tra)
            CenterPivotOffset(s, spacing);

            // Cập nhật BoxCollider2D bao phủ toàn shape
            UpdateCollider(s, spacing);
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
        /// Thiết lập CompositeCollider2D trên cha để gộp các collider con lại.
        /// </summary>
        private void UpdateCollider(float cellSize, float spacing)
        {
            // 1. Loại bỏ BoxCollider2D cũ trên cha (nếu có và KHÔNG tham gia Composite) để tránh xung đột
            var oldBox = GetComponent<BoxCollider2D>();
            if (oldBox != null && oldBox.compositeOperation == Collider2D.CompositeOperation.None) 
                DestroyImmediate(oldBox);

            // 2. Đảm bảo có Rigidbody2D (bắt buộc cho Composite)
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            // 3. Đảm bảo có CompositeCollider2D
            var composite = GetComponent<CompositeCollider2D>();
            if (composite == null) composite = gameObject.AddComponent<CompositeCollider2D>();
            
            // CHỈNH LẠI: Dùng Polygons để click vào giữa khối gạch cũng dính, thay vì Outlines (chỉ dính ở mép)
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous; // Cập nhật ngay lập tức
            
            Debug.Log($"[LandBlock] Đã gộp {transform.childCount} ô con vào Composite Collider (Dạng Polygon).");
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

            // ─── Đặt block KHỚP 100% vào ngón tay (không nâng Y) ───
            Vector3 blockWorld  = cursorWorld + new Vector3(0f, _dragYOffset, 0f);
            transform.position  = blockWorld;

            // ─── Preview: anchor ───
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

                // 🛡️ BƯỚC ĐỆM: Khởi hành hoạt ảnh trước khi báo cáo kết quả
                if (gameObject.activeInHierarchy)
                    StartCoroutine(SnapAndHide());

                // MỚI: Báo cáo kết quả sau cùng
                OnBlockPlaced?.Invoke(this, _shapeData, anchor);
            }
            else
            {
                Debug.Log($"[LandBlock] ❌ Cannot place at {anchor} — returning to origin");
                if (gameObject.activeInHierarchy)
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
        /// <summary>
        /// Tính toán tọa độ Anchor (Góc dưới-trái) từ vị trí trung tâm World của khối gạch.
        /// GridBoard coi anchor là (x: left-most, y: bottom-most).
        /// </summary>
        private Vector2Int GetAnchorFromBlockCenter(Vector3 blockCenterWorld)
        {
            // Lấy scale của bàn cờ để bù trừ tỉ lệ
            float gridScale = _gridBoard.transform.localScale.x;
            float step = _gridBoard.CellSize * (1f + _gridBoard.CellMargin) * gridScale;
            
            // Tính toán độ lệch từ Tâm đến ô Góc (Bottom-Left) chuẩn xác theo tỉ lệ World
            float offsetX = (_shapeData.columns - 1) * step / 2f;
            float offsetY = (_shapeData.rows    - 1) * step / 2f;

            // Vị trí World thực tế của ô dưới trái của Shape
            Vector3 bottomLeftCellWorld = blockCenterWorld - new Vector3(offsetX, offsetY, 0f);

            // Hỏi GridBoard xem cái tọa độ World đó là ô nào trong Grid
            return _gridBoard.WorldToGridPosition(bottomLeftCellWorld);
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

        private static Sprite _whiteSprite;
        private static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    Color32[] pixels = new Color32[16];
                    for (int i = 0; i < 16; i++) pixels[i] = new Color32(255, 255, 255, 255);
                    tex.SetPixels32(pixels);
                    tex.Apply();
                    _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                }
                return _whiteSprite;
            }
        }

        private static Sprite MakeWhiteSprite() => WhiteSprite;
    }

    // --- PHƯƠNG THỨC MỞ RỘNG (EXTENSION) ---
    public static class TransformExtensions
    {
        public static bool TryGetUniformScale(this Transform t, out float scaleValue)
        {
            Vector3 scale = t.localScale;
            if (Mathf.Approximately(scale.x, scale.y) && Mathf.Approximately(scale.x, scale.z))
            {
                scaleValue = scale.x;
                return true;
            }
            scaleValue = 0f;
            return false;
        }
    }
}
