using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FarmPuzzle.Economy
{
    /// <summary>
    /// Điều khiển giao diện cửa hàng (Shop) trong Scene hiện tại.
    /// Tự động tạo nút mua dựa trên dữ liệu Catalog và cập nhật số dư tiền vàng.
    /// </summary>
    public class ShopUIController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text hiển thị tiền vàng hiện tại của người chơi")]
        public TextMeshProUGUI txtGoldBalance;

        [Header("Dynamic Buttons Generation")]
        [Tooltip("Prefab nút mua vật phẩm (Có chứa Button và TextMeshProUGUI)")]
        public GameObject shopButtonPrefab;
        [Tooltip("Container chứa các nút Hạt Giống (thường là một Layout Group)")]
        public Transform seedsContainer;
        [Tooltip("Container chứa các nút Trang Trí (thường là một Layout Group)")]
        public Transform decorContainer;

        private void Start()
        {
            // 1. Force Login to load Player Data locally in this test scene
            if (DataManager.Instance != null && DataManager.Instance.CurrentPlayer == null)
            {
                DataManager.Instance.LoginPlayer("test_user_1", "Test Player 1");
                // Cho thêm tiền để dễ test thủ công
                DataManager.Instance.AddGold(1000); 
            }

            // 2. Tự động sinh ra 12 nút (6 seeds, 6 decors) từ Catalog
            GenerateShopButtons();

            // 3. Cập nhật UI lần đầu
            UpdateGoldDisplay();

            // 4. Lắng nghe các sự kiện mua sắm
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnItemPurchased += HandleItemPurchased;
                ShopManager.Instance.OnPurchaseFailed += HandlePurchaseFailed;
            }
        }

        private void OnDestroy()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnItemPurchased -= HandleItemPurchased;
                ShopManager.Instance.OnPurchaseFailed -= HandlePurchaseFailed;
            }
        }

        private void GenerateShopButtons()
        {
            if (shopButtonPrefab == null || seedsContainer == null || decorContainer == null)
            {
                Debug.LogWarning("[ShopUI] Chưa gán Prefab hoặc Container trong Inspector, bỏ qua tạo nút tự động.");
                return;
            }

            if (ShopManager.Instance == null || ShopManager.Instance.ShopCatalog == null) return;

            var allItems = ShopManager.Instance.GetAllItems();
            foreach (var item in allItems)
            {
                Transform targetContainer = (item.Category == ItemCategory.Seed) ? seedsContainer : decorContainer;
                
                // Sinh ra Prefab Nút
                GameObject btnObj = Instantiate(shopButtonPrefab, targetContainer);
                btnObj.name = $"Btn_Buy_{item.ReferenceID}";

                // Lấy component Text để gắn Label
                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = $"{item.DisplayName}\n({item.Price} G)";
                }

                // Lấy component Button và gắn Event OnClick
                Button btnComponent = btnObj.GetComponent<Button>();
                if (btnComponent != null)
                {
                    string refID = item.ReferenceID; // Cache lại ID cho delegate
                    btnComponent.onClick.AddListener(() => BuyItemButtonClicked(refID));
                }
            }
        }

        private void HandleItemPurchased(string refID, int quantity, int totalCost)
        {
            UpdateGoldDisplay();
            Debug.Log($"<color=green>[ShopUI] Mua thành công! Cập nhật lại UI tiền: {DataManager.Instance.CurrentPlayer.Money}</color>");
        }

        private void HandlePurchaseFailed(string reason)
        {
            Debug.LogWarning($"<color=orange>[ShopUI] Từ chối mua: {reason}</color>");
        }

        private void UpdateGoldDisplay()
        {
            if (txtGoldBalance != null && DataManager.Instance != null && DataManager.Instance.CurrentPlayer != null)
            {
                txtGoldBalance.text = $"Gold: {DataManager.Instance.CurrentPlayer.Money}";
            }
            else if (txtGoldBalance != null)
            {
                txtGoldBalance.text = "Gold: ---";
            }
        }

        public void BuyItemButtonClicked(string referenceID)
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.BuyItem(referenceID, 1);
            }
        }
    }
}
