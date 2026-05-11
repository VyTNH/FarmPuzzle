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
            if (_itemData == null)
            {
                Debug.LogError("[DecorDrag] OnBeginDrag: _itemData == null! Gọi SetupItem() trước khi sử dụng.");
                return;
            }
            _isDragging = true;

            // Chặn kéo camera trong lúc kéo item
            if (CameraDrag.Instance != null) CameraDrag.Instance.enabled = false;

            // Tìm component Grid trong Game
            if (_mainGrid == null && GridManager.Instance != null)
            {
                _mainGrid = GridManager.Instance.GetComponent<Grid>();
                if (_mainGrid == null)
                    Debug.LogError("[DecorDrag] Không tìm thấy component Grid trên GridManager! Hãy gắn Grid component vào GameObject 'Grid Manager'.");
                else
                    Debug.Log("[DecorDrag] Tìm thấy Grid: " + _mainGrid.name);
            }
            else if (_mainGrid == null)
            {
                Debug.LogError("[DecorDrag] GridManager.Instance == null! Đảm bảo GridManager có trong scene.");
            }

            // Tạo Shadow Object nằm trong Scene (World Space)
            _shadowObject = new GameObject($"Shadow_{_itemData.DecorID}");
            _shadowRenderer = _shadowObject.AddComponent<SpriteRenderer>();
            
            if (_decorSprite == null)
            {
                _decorSprite = DecorationManager.Instance?.GetSprite(_itemData.DecorID);
                Debug.LogWarning($"[DecorDrag] _decorSprite null khi BeginDrag, thử lấy lại: {(_decorSprite != null ? "Tìm thấy" : "Vẫn null! Kiểm tra allDecorSprites trong DecorationManager Inspector")}");
            }
            _shadowRenderer.sprite = _decorSprite;
            _shadowRenderer.color = new Color(1f, 1f, 1f, 0.5f);
            _shadowRenderer.sortingOrder = 30000;

            Debug.Log($"[DecorDrag] Bắt đầu kéo: {_itemData.Name} | Sprite: {(_decorSprite != null ? _decorSprite.name : "NULL")} | Grid: {(_mainGrid != null ? "OK" : "NULL")}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _shadowObject == null) return;

            if (_mainGrid == null)
            {
                Debug.LogWarning("[DecorDrag] OnDrag: _mainGrid null, shadow sẽ không di chuyển theo chuột!");
                return;
            }

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
            Vector3Int cellPos = _mainGrid.WorldToCell(worldPos);

            int maxW = GridManager.Instance.gridWidth;
            int maxH = GridManager.Instance.gridHeight;
            if (cellPos.x >= 0 && cellPos.x < maxW && cellPos.y >= 0 && cellPos.y < maxH)
            {
                Vector3 centerPos = _mainGrid.GetCellCenterWorld(cellPos);
                int hLvl = DecorationManager.Instance.GetHeightLevelAt(cellPos.x, cellPos.y);
                float offsetForStack = DecorationManager.Instance.stackHeightOffset * hLvl;
                _shadowObject.transform.position = new Vector3(centerPos.x, centerPos.y + offsetForStack, 0);

                if (DecorationManager.Instance.CanStackAt(cellPos.x, cellPos.y))
                    _shadowRenderer.color = new Color(0.5f, 1f, 0.5f, 0.6f);
                else
                    _shadowRenderer.color = new Color(1f, 0.5f, 0.5f, 0.6f);
            }
            else
            {
                _shadowRenderer.color = new Color(1f, 0f, 0f, 0.2f);
                _shadowObject.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            if (CameraDrag.Instance != null) CameraDrag.Instance.enabled = true;

            if (_shadowObject != null)
            {
                Vector3 finalWorld = _shadowObject.transform.position;
                Destroy(_shadowObject);
                _shadowObject = null;

                if (_mainGrid == null)
                {
                    Debug.LogError("[DecorDrag] OnEndDrag: _mainGrid null, không thể xác định vị trí thả!");
                    return;
                }

                Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
                Vector3Int cellPos = _mainGrid.WorldToCell(worldPoint);
                int maxW = GridManager.Instance.gridWidth;
                int maxH = GridManager.Instance.gridHeight;

                Debug.Log($"[DecorDrag] Thả tại cellPos={cellPos} | InBounds={cellPos.x >= 0 && cellPos.x < maxW && cellPos.y >= 0 && cellPos.y < maxH} | CanStack={DecorationManager.Instance?.CanStackAt(cellPos.x, cellPos.y)}");

                if (cellPos.x >= 0 && cellPos.x < maxW && cellPos.y >= 0 && cellPos.y < maxH)
                {
                    if (DecorationManager.Instance.CanStackAt(cellPos.x, cellPos.y))
                    {
                        Vector3 centerBase = _mainGrid.GetCellCenterWorld(cellPos);

                        if (DecorConfirmPopup.Instance == null)
                        {
                            var popups = Resources.FindObjectsOfTypeAll<DecorConfirmPopup>();
                            if (popups.Length > 0) DecorConfirmPopup.Instance = popups[0];
                            Debug.LogWarning($"[DecorDrag] Tìm kiếm DecorConfirmPopup: tìm được {popups.Length} instance.");
                        }

                        if (DecorConfirmPopup.Instance != null)
                        {
                            Debug.Log($"[DecorDrag] Hiển popup xác nhận: {_itemData.Name} tại ({cellPos.x},{cellPos.y}) worldPos={centerBase}");
                            DecorConfirmPopup.Instance.Show(_itemData.DecorID, _itemData.Name, _itemData.BuyPrice, cellPos.x, cellPos.y, centerBase);
                        }
                        else
                        {
                            Debug.LogError("[DecorDrag] Không tìm thấy DecorConfirmPopup trong scene! Kiểm tra: 1) Popup có trong Hierarchy 2) Script DecorConfirmPopup được gắn 3) SetActive(false) không dùng Destroy.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[DecorDrag] Vị trí ({cellPos.x},{cellPos.y}) không cho phép đặt chồng (có mái nhọn bên trên).");
                    }
                }
                else
                {
                    Debug.LogWarning($"[DecorDrag] Thả ngoài giới hạn map: cellPos={cellPos}");
                }
            }
        }
    }
}
