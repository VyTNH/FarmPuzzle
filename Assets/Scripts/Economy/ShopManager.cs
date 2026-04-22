using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPuzzle.Economy
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("Shop Data")]
        [Tooltip("Database ScriptableObject chứa danh sách vật phẩm cửa hàng.")]
        public ShopCatalogSO ShopCatalog;

        // Sự kiện kích hoạt khi có vật phẩm được mua thành công
        public Action<string, int, int> OnItemPurchased; // tham số: referenceID, số lượng, tổng chi phí
        public Action<string> OnPurchaseFailed; // tham số: lý do thất bại

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Tùy chọn: DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Lấy tất cả vật phẩm có sẵn để hiển thị lên UI
        /// </summary>
        public List<ShopItemEntry> GetAllItems()
        {
            return ShopCatalog != null ? ShopCatalog.AvailableItems : new List<ShopItemEntry>();
        }

        /// <summary>
        /// Kiểm tra xem người chơi có đủ Vàng không
        /// </summary>
        public bool CanAfford(string referenceID, int quantity = 1)
        {
            if (ShopCatalog == null) return false;

            ShopItemEntry entry = ShopCatalog.GetItem(referenceID);
            if (entry == null) return false;

            int totalCost = entry.Price * quantity;
            return DataManager.Instance.CurrentPlayer != null && DataManager.Instance.CurrentPlayer.Money >= totalCost;
        }

        /// <summary>
        /// Thực hiện giao dịch mua hàng
        /// </summary>
        public bool BuyItem(string referenceID, int quantity = 1)
        {
            if (ShopCatalog == null)
            {
                OnPurchaseFailed?.Invoke("Thiếu dữ liệu cửa hàng (Shop catalog).");
                return false;
            }

            ShopItemEntry entry = ShopCatalog.GetItem(referenceID);
            if (entry == null)
            {
                OnPurchaseFailed?.Invoke($"Không tìm thấy vật phẩm {referenceID} trong cửa hàng.");
                return false;
            }

            if (!CanAfford(referenceID, quantity))
            {
                OnPurchaseFailed?.Invoke("Không đủ tiền.");
                return false;
            }

            int totalCost = entry.Price * quantity;

            // 1. Trừ tiền
            DataManager.Instance.AddGold(-totalCost);

            // 2. Thêm vật phẩm vào túi đồ của người chơi
            DataManager.Instance.AddItem(referenceID, quantity);

            Debug.Log($"[ShopManager] Đã mua {quantity}x {entry.DisplayName} với giá {totalCost} vàng.");
            
            // 3. Kích hoạt sự kiện cho UI và m thanh
            OnItemPurchased?.Invoke(referenceID, quantity, totalCost);

            return true;
        }
    }
}
