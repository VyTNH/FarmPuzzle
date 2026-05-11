using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FarmPuzzle.Core.Database;
using FarmPuzzle.FarmSystem;
using FarmPuzzle.FarmSystem.Crop;

namespace FarmPuzzle.UI
{
    [RequireComponent(typeof(Image))]
    public class DraggableTool : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public CropNeedType toolType; // Gán trong Inspector (Water, Pest, Fertilizer, Seed)
        public SeedItemSO seedData;   // Nếu toolType = None thì có thể dùng để kéo hạt giống
        public bool isHarvestTool = false; // Đánh dấu tool dùng để thu hoạch

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Vector2 _startPosition;
        private Transform _originalParent;
        private LandPlot _lastHighlightedPlot;

        // Cache Canvas root để SetParent đúng trong OnBeginDrag (tránh InvalidCastException)
        private Transform _dragCanvas;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Tìm Canvas cha gần nhất để làm container kéo thả
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
                _dragCanvas = parentCanvas.transform;
            else
                _dragCanvas = transform.root; // Fallback an toàn
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _startPosition = _rectTransform.anchoredPosition;
            _originalParent = transform.parent;

            // FIX: Phải SetParent lên Canvas (RectTransform), KHÔNG phải transform.root (Transform thường)
            // Nếu set lên transform.root (scene root), cast sang RectTransform ở OnDrag sẽ crash!
            transform.SetParent(_dragCanvas);
            transform.SetAsLastSibling();
            _canvasGroup.alpha = 0.7f;
            _canvasGroup.blocksRaycasts = false;
            if (CameraDrag.Instance != null) CameraDrag.IsLockedByTool = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // An toàn: chỉ cast khi transform.parent thực sự là RectTransform
            if (transform.parent is RectTransform parentRect)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPosition))
                {
                    _rectTransform.localPosition = localPointerPosition;
                }
            }

            Vector2 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            
            LandPlot currentPlot = hit.collider != null ? hit.collider.GetComponentInParent<LandPlot>() : null;
            if (currentPlot != _lastHighlightedPlot)
            {
                if (_lastHighlightedPlot != null) _lastHighlightedPlot.SetHighlight(false, false);
                _lastHighlightedPlot = currentPlot;
            }

            if (_lastHighlightedPlot != null)
            {
                bool isValid = false;
                if (isHarvestTool) {
                    isValid = _lastHighlightedPlot.CanHarvest() || _lastHighlightedPlot.isLocked;
                }
                else if (toolType != CropNeedType.None) isValid = _lastHighlightedPlot.isOccupied;
                else if (seedData != null) isValid = !_lastHighlightedPlot.isOccupied && !_lastHighlightedPlot.isLocked;
                _lastHighlightedPlot.SetHighlight(true, isValid);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_lastHighlightedPlot != null)
            {
                _lastHighlightedPlot.SetHighlight(false, false);
                _lastHighlightedPlot = null;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            transform.SetParent(_originalParent);
            _rectTransform.anchoredPosition = _startPosition;
            if (CameraDrag.Instance != null) CameraDrag.IsLockedByTool = false;

            Vector2 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            if (hit.collider != null)
            {
                LandPlot clickedPlot = hit.collider.GetComponentInParent<LandPlot>();
                if (clickedPlot != null)
                {
                    if (isHarvestTool)
                    {
                        if (clickedPlot.isLocked) HandleUnlockForest(clickedPlot);
                        else if (clickedPlot.CanHarvest())
                        {
                            clickedPlot.Harvest();
                            if (GridManager.Instance != null) GridManager.Instance.SavePlotState(clickedPlot);
                            // MỚI: Commit ngay để lưu nông sản nhặt được vào SQLite
                            if (DataManager.Instance != null) DataManager.Instance.CommitSessionInventory();
                        }
                    }
                    else if (toolType != CropNeedType.None)
                    {
                        if (clickedPlot.isOccupied)
                        {
                            clickedPlot.ApplyCare(toolType);
                            if (GridManager.Instance != null) GridManager.Instance.SavePlotState(clickedPlot);
                            // Commit nếu là tool tiêu hao (như phân bón)
                            if (DataManager.Instance != null) DataManager.Instance.CommitSessionInventory();
                        }
                    }
                    else if (seedData != null)
                    {
                        if (!clickedPlot.isOccupied && !clickedPlot.isLocked)
                        {
                            if (GridManager.Instance != null) GridManager.Instance.selectedSeed = seedData;
                            bool hasItem = DataManager.Instance != null && DataManager.Instance.RemoveItem(seedData.seedID, 1);
                            
                            // Hack nạp đạn cho sếp test
                            if (!hasItem) {
                                if (DataManager.Instance != null) DataManager.Instance.AddItem(seedData.seedID, 1);
                                hasItem = true;
                            }
                            
                            if (hasItem && clickedPlot.Plant(seedData))
                            {
                                if (GridManager.Instance != null) GridManager.Instance.SavePlotState(clickedPlot);
                                // CỰC KỲ QUAN TRỌNG: Ghi xuống file DB ngay lập tức để trừ hạt giống vĩnh viễn
                                if (DataManager.Instance != null) DataManager.Instance.CommitSessionInventory();
                                Debug.Log($"[DraggableTool] Đã trừ vĩnh viễn 1 {seedData.seedName} vào DB.");
                            }
                        }
                    }
                }
            }
        }

        private void HandleUnlockForest(LandPlot clickedPlot)
        {
            int skipPopup = PlayerPrefs.GetInt("SkipUnlockConfirm", 0);
            var popup = Object.FindFirstObjectByType<FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController>();
            
            if (skipPopup == 1 && FarmPuzzle.LandPuzzle.EnergySystem.Instance != null && FarmPuzzle.LandPuzzle.EnergySystem.Instance.HasEnergy)
            {
                FarmPuzzle.LandPuzzle.EnergySystem.Instance.ConsumeEnergy(1);
                var pm = Object.FindFirstObjectByType<FarmPuzzle.LandPuzzle.LandPuzzleManager>();
                if (pm != null && clickedPlot.puzzleLevel != null) pm.StartPuzzle(clickedPlot.puzzleLevel, clickedPlot);
            }
            else
            {
                if (popup != null) popup.ShowConfirmPopup(clickedPlot);
            }
        }
    }
}
