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

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isHarvestTool) Debug.Log($"<color=cyan>[DraggableTool]</color> Bắt đầu kéo: THU HOẠCH");
            else if (seedData != null) Debug.Log($"<color=cyan>[DraggableTool]</color> Bắt đầu kéo HẠT: {seedData.seedName}");
            else Debug.Log($"<color=cyan>[DraggableTool]</color> Bắt đầu kéo CÔNG CỤ: {toolType}");

            _startPosition = _rectTransform.anchoredPosition;
            _originalParent = transform.parent;
            
            // Đưa object ra khỏi layout để kéo thả không bị vướng
            transform.SetParent(transform.root); 
            transform.SetAsLastSibling();

            _canvasGroup.alpha = 0.7f;
            _canvasGroup.blocksRaycasts = false; // Xuyên thủng UI để tia Raycast lọt xuống Nông trại
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Di chuyển icon theo chuột
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform.parent, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPosition))
            {
                _rectTransform.localPosition = localPointerPosition;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Trả icon về chỗ cũ
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            transform.SetParent(_originalParent);
            _rectTransform.anchoredPosition = _startPosition;

            // Bắn tia Raycast xuống World 2D để tìm ô đất DƯỚI ngón tay
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null)
            {
                LandPlot clickedPlot = hit.collider.GetComponent<LandPlot>();
                if (clickedPlot != null)
                {
                    if (isHarvestTool)
                    {
                        // Kéo thả Tool Thu hoạch
                        if (clickedPlot.CanHarvest())
                        {
                            Debug.Log($"<color=cyan>[DraggableTool]</color> Thu hoạch ô {clickedPlot.plotID}");
                            clickedPlot.Harvest();
                            if (GridManager.Instance != null) GridManager.Instance.SavePlotState(clickedPlot);
                        }
                    }
                    else if (toolType != CropNeedType.None)
                    {
                        // Kéo thả Tool chăm sóc
                        if (clickedPlot.isOccupied)
                        {
                            Debug.Log($"<color=cyan>[DraggableTool]</color> Thả công cụ {toolType} lên ô {clickedPlot.plotID}");
                            clickedPlot.ApplyCare(toolType);
                            if (GridManager.Instance != null) GridManager.Instance.SavePlotState(clickedPlot);
                        }
                    }
                    else if (seedData != null)
                    {
                        // Kéo thả Hạt giống
                        if (!clickedPlot.isOccupied && !clickedPlot.isLocked)
                        {
                            if (GridManager.Instance != null) GridManager.Instance.selectedSeed = seedData;
                            bool hasItem = DataManager.Instance != null && DataManager.Instance.RemoveItem(seedData.seedID, 1);
                            if (!hasItem) {
                                Debug.LogWarning($"<color=orange>[DraggableTool]</color> Tự động nạp đạn (Hack) {seedData.seedName}");
                                if (DataManager.Instance != null) DataManager.Instance.AddItem(seedData.seedID, 1);
                                hasItem = true;
                            }
                            
                            if (hasItem && clickedPlot.Plant(seedData))
                            {
                                if (GridManager.Instance != null) GridManager.Instance.SavePlotState(clickedPlot);
                                Debug.Log($"<color=green>[DraggableTool]</color> Gieo thành công {seedData.seedName}");
                            }
                        }
                    }
                }
            }
        }
    }
}
