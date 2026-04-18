using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using FarmPuzzle.Core.Database;
using FarmPuzzle.Decor;
using FarmPuzzle.FarmSystem;

namespace FarmPuzzle.UI
{
    [RequireComponent(typeof(Image))]
    public class DecorDraggableTool : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image iconImage;
        public TextMeshProUGUI priceText;
        public TextMeshProUGUI nameText;

        private DecorItemModel _itemData;
        private Sprite _decorSprite;
        
        // Kéo thả parameters
        private GameObject _shadowObject;
        private SpriteRenderer _shadowRenderer;
        private bool _isDragging = false;
        
        // Isometric offsets from FarmTileInspector / LandPlot logic
        // Ta sử dụng GridManager để giải mã tọa độ.
        private Grid _mainGrid;

        public void SetupItem(DecorItemModel itemData)
        {
            _itemData = itemData;
            if (nameText != null) 
                nameText.text = itemData.IsFlatTop ? $"[Nền] {itemData.Name}" : $"[Mái] {itemData.Name}";
                
            if (priceText != null) 
            {
                priceText.text = $"{itemData.BuyPrice} Vàng";
                // Lợi dụng price hiển thị loại tầng luôn
                priceText.text = itemData.IsFlatTop ? $"{itemData.BuyPrice}V (Đáy)" : $"{itemData.BuyPrice}V (Mái)";
                priceText.color = itemData.IsFlatTop ? Color.white : new Color(1f, 0.7f, 0f); // Mái màu cam
            }
            
            _decorSprite = DecorationManager.Instance.GetSprite(itemData.DecorID);
            if (iconImage != null && _decorSprite != null) 
                iconImage.sprite = _decorSprite;

            // Đổi màu nền của tấm thẻ để dễ phân biệt
            Image bg = GetComponent<Image>();
            if (bg != null)
            {
                bg.color = itemData.IsFlatTop ? new Color(0.2f, 0.3f, 0.2f, 0.9f) : new Color(0.3f, 0.2f, 0.2f, 0.9f);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_itemData == null) return;
            _isDragging = true;

            // Chặn kéo camera trong lúc kéo item
            if (CameraDrag.Instance != null) CameraDrag.Instance.enabled = false;

            // Tìm component Grid trong Game
            if (_mainGrid == null && GridManager.Instance != null)
                _mainGrid = GridManager.Instance.GetComponent<Grid>();

            // Tạo Shadow Object nằm trong Scene (World Space)
            _shadowObject = new GameObject($"Shadow_{_itemData.DecorID}");
            _shadowRenderer = _shadowObject.AddComponent<SpriteRenderer>();
            _shadowRenderer.sprite = _decorSprite;
            
            // Material Transparent, Color nửa mờ
            _shadowRenderer.color = new Color(1f, 1f, 1f, 0.5f);
            
            // Chỉnh Pivot (do gốc tọa độ đã bottom-center nên shadow Renderer không cần offset quá phức tạp)
            // Tuy nhiên Decor thì cần dịch Y lên để leo cầu thang (HeightLevel)
            _shadowRenderer.sortingOrder = 30000; // Đẩy lên trên cùng để dễ nhìn
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _shadowObject == null || _mainGrid == null) return;

            // Soi tia raycast xuống Plane mặt đất / Isometric Grid Z=0
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
            
            // Tính toán ra Cell Position chính xác trên hệ tọa độ Isometric (Grid Component)
            Vector3Int cellPos = _mainGrid.WorldToCell(worldPos);

            // Giới hạn trong kích thước GridW x GridH (VD: 50x50) 
            int maxW = GridManager.Instance.gridWidth;
            int maxH = GridManager.Instance.gridHeight;
            if (cellPos.x >= 0 && cellPos.x < maxW && cellPos.y >= 0 && cellPos.y < maxH)
            {
                // Cho phép kéo thả
                Vector3 centerPos = _mainGrid.GetCellCenterWorld(cellPos);
                
                // Đọc xem ô này đang cao từng nào
                int hLvl = DecorationManager.Instance.GetHeightLevelAt(cellPos.x, cellPos.y);
                float offsetForStack = DecorationManager.Instance.stackHeightOffset * hLvl;
                
                // Cập nhật vị trí bóng mờ
                _shadowObject.transform.position = new Vector3(centerPos.x, centerPos.y + offsetForStack, 0);

                // Nếu là khối chặn -> hiển thị đèn Đỏ, cấm đặt
                if (DecorationManager.Instance.CanStackAt(cellPos.x, cellPos.y))
                    _shadowRenderer.color = new Color(0.5f, 1f, 0.5f, 0.6f); // Xanh mờ
                else
                    _shadowRenderer.color = new Color(1f, 0.5f, 0.5f, 0.6f); // Đỏ mờ
            }
            else
            {
                _shadowRenderer.color = new Color(1f, 0f, 0f, 0.2f); // Qua biên giới -> đỏ tịt
                _shadowObject.transform.position = new Vector3(worldPos.x, worldPos.y, 0f); // Fallback
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            if (CameraDrag.Instance != null) CameraDrag.Instance.enabled = true;

            if (_shadowObject != null)
            {
                // Lúc thả chuột, lấy chính tọa độ World ném ngược lại hàm tính
                Vector3 finalWorld = _shadowObject.transform.position;
                Destroy(_shadowObject);

                if (_mainGrid != null)
                {
                    // Lần nữa tính xem con trỏ / vật quy đổi Grid X,Y
                    // Do lúc kéo ta đẩy cao Y lên vì stack (tạo ảo ảnh mắt), ta nên dùng mouse real_world pos
                    Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
                    Vector3Int cellPos = _mainGrid.WorldToCell(worldPoint);
                    
                    int maxW = GridManager.Instance.gridWidth;
                    int maxH = GridManager.Instance.gridHeight;

                    // Nếu lọt trong map & có thể đặt lên
                    if (cellPos.x >= 0 && cellPos.x < maxW && cellPos.y >= 0 && cellPos.y < maxH)
                    {
                        if (DecorationManager.Instance.CanStackAt(cellPos.x, cellPos.y))
                        {
                            Vector3 centerBase = _mainGrid.GetCellCenterWorld(cellPos);
                            // Gọi Popup Verify!
                            if (DecorConfirmPopup.Instance == null)
                            {
                                var popups = Resources.FindObjectsOfTypeAll<DecorConfirmPopup>();
                                if (popups.Length > 0) DecorConfirmPopup.Instance = popups[0];
                            }

                            if (DecorConfirmPopup.Instance != null)
                            {
                                DecorConfirmPopup.Instance.Show(_itemData.DecorID, _itemData.Name, _itemData.BuyPrice, cellPos.x, cellPos.y, centerBase);
                            }
                            else
                            {
                                Debug.LogError("Không tìm thấy DecorConfirmPopup. Xóa cái panel đó và dùng Mũi tên Tools > Tự động vẽ lại UI nhé.");
                            }
                        }
                    }
                }
            }
        }
    }
}
