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

            // MỚI: Khóa cứng Camera không cho chạy theo lúc kéo Tool
            if (CameraDrag.Instance != null) CameraDrag.IsLockedByTool = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Di chuyển icon theo chuột
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform.parent, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPosition))
            {
                _rectTransform.localPosition = localPointerPosition;
            }

            // Highlight logic
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            
            LandPlot currentPlot = hit.collider != null ? hit.collider.GetComponent<LandPlot>() : null;
            if (currentPlot != _lastHighlightedPlot)
            {
                if (_lastHighlightedPlot != null) _lastHighlightedPlot.SetHighlight(false, false);
                _lastHighlightedPlot = currentPlot;
            }

            if (_lastHighlightedPlot != null)
            {
                bool isValid = false;
                if (isHarvestTool) {
                    // Tool Cuốc sáng lên khi Đất Trồng Chờ Thu Hoạch HOẶC Đất Rừng Đang Khóa
                    isValid = _lastHighlightedPlot.CanHarvest() || _lastHighlightedPlot.isLocked;
                }
                else if (toolType != CropNeedType.None) isValid = _lastHighlightedPlot.isOccupied && !isHarvestTool;
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

            // Trả icon về chỗ cũ
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            transform.SetParent(_originalParent);
            _rectTransform.anchoredPosition = _startPosition;

            // MỞ: Mở khóa Camera
            if (CameraDrag.Instance != null) CameraDrag.IsLockedByTool = false;

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
                        // Kéo thả Tool Cuốc
                        if (clickedPlot.isLocked)
                        {
                            // 1. MỞ KHÓA ĐẤT (PUZZLE)
                            Debug.Log($"<color=cyan>[DraggableTool]</color> Cuốc được thả lên {clickedPlot.plotID} để MỞ RỪNG");
                            
                            // Kiểm tra Skip Popup (Dont Ask Me Again)
                            int skipPopup = PlayerPrefs.GetInt("SkipUnlockConfirm", 0);
                            var popup = Object.FindFirstObjectByType<FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController>();
                            
                            if (skipPopup == 1 && FarmPuzzle.LandPuzzle.EnergySystem.Instance != null && FarmPuzzle.LandPuzzle.EnergySystem.Instance.HasEnergy)
                            {
                                // Chơi ngay và luôn
                                FarmPuzzle.LandPuzzle.EnergySystem.Instance.ConsumeEnergy(1);
                                var pm = Object.FindFirstObjectByType<FarmPuzzle.LandPuzzle.LandPuzzleManager>();
                                if (pm != null && clickedPlot.puzzleLevel != null) pm.StartPuzzle(clickedPlot.puzzleLevel, clickedPlot);
                            }
                            else
                            {
                                // Hiện Popup
                                if (popup != null) popup.ShowConfirmPopup(clickedPlot);
                            }
                        }
                        else if (clickedPlot.CanHarvest())
                        {
                            // 2. THU HOẠCH NÔNG SẢN
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
