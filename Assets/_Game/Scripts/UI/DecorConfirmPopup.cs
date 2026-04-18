using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmPuzzle.Core;

namespace FarmPuzzle.UI
{
    public class DecorConfirmPopup : MonoBehaviour
    {
        public static DecorConfirmPopup Instance;

        public TextMeshProUGUI titleText;
        public TextMeshProUGUI priceText;
        
        private string _pendingDecorID;
        private int _pendingPrice;
        private int _pendingGridX;
        private int _pendingGridY;
        private Vector3 _pendingWorldPos;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            gameObject.SetActive(false);
        }

        public void Show(string decorID, string decorName, int price, int gridX, int gridY, Vector3 worldPos)
        {
            _pendingDecorID = decorID;
            _pendingPrice = price;
            _pendingGridX = gridX;
            _pendingGridY = gridY;
            _pendingWorldPos = worldPos;

            if (titleText != null) titleText.text = $"Mua {decorName}?";
            if (priceText != null) priceText.text = $"{price} Vàng";
            
            gameObject.SetActive(true);
        }

        public void OnClickConfirm()
        {
            // Kiểm tra tiền
            if (DataManager.Instance.CurrentPlayer.Money >= _pendingPrice)
            {
                DataManager.Instance.CurrentPlayer.Money -= _pendingPrice;
                DataManager.Instance.DB.Update(DataManager.Instance.CurrentPlayer);

                // Triệu hồi lệnh đặt
                Decor.DecorationManager.Instance.PlaceDecor(_pendingDecorID, _pendingGridX, _pendingGridY, _pendingWorldPos);
                
                // Refresh TopBar (Gọi event hoặc delegate)
                
                var topBar = FindFirstObjectByType<FarmTopBarUI>();
                if (topBar != null) topBar.Refresh();
            }
            else
            {
                Debug.LogWarning("Không đủ tiền mua Decor!");
            }

            gameObject.SetActive(false);
        }

        public void OnClickCancel()
        {
            gameObject.SetActive(false);
        }
    }
}
