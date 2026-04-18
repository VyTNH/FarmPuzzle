using UnityEngine;

namespace FarmPuzzle.UI
{
    public class SeedShopToggle : MonoBehaviour
    {
        public RectTransform shopPanel;
        public float slideSpeed = 10f;
        
        private bool _isOpen = false;
        private Vector2 _closedPosition;
        private Vector2 _openPosition;

        private void Start()
        {
            if (shopPanel == null) return;
            
            // Tính toán vị trí mở: Nằm ở vị trí hiện tại (neo dính lề trái)
            _openPosition = shopPanel.anchoredPosition;
            // Vị trí đóng: Dịch mảng này sang trái màn hình một khoảng bằng đúng độ rộng của nó
            _closedPosition = new Vector2(_openPosition.x - shopPanel.rect.width, _openPosition.y);
            
            // Bắt đầu với trạng thái đóng
            shopPanel.anchoredPosition = _closedPosition;
        }

        private void Update()
        {
            if (shopPanel == null) return;
            Vector2 target = _isOpen ? _openPosition : _closedPosition;
            shopPanel.anchoredPosition = Vector2.Lerp(shopPanel.anchoredPosition, target, Time.deltaTime * slideSpeed);
        }

        public void ToggleShop()
        {
            _isOpen = !_isOpen;
        }
    }
}
