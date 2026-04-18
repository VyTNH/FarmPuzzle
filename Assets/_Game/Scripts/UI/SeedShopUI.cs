using UnityEngine;
using UnityEngine.UI;
using FarmPuzzle.Core.Database;
using FarmPuzzle.FarmSystem.Crop;
using FarmPuzzle.Core;

namespace FarmPuzzle.UI
{
    public class SeedShopUI : MonoBehaviour
    {
        public Transform contentContainer;
        public GameObject shopItemPrefab;

        private void Start()
        {
            Invoke(nameof(PopulateShop), 0.5f);
        }

        private void PopulateShop()
        {
            if (contentContainer == null || shopItemPrefab == null) return;
            
            foreach(Transform t in contentContainer) Destroy(t.gameObject);

            Debug.Log($"<color=cyan>[SHOP - STAGE 1: Disk -> RAM]</color> Bắt đầu nạp dữ liệu cửa hàng tĩnh từ Database_Resources...");

            // 1. Tải Hạt Giống
            var seeds = Resources.LoadAll<SeedItemSO>(ProjectPaths.RS_FOLDER_SEED_DATA);
            Debug.Log($"<color=cyan>[SHOP - STAGE 1]</color> Đã nạp thành công {seeds.Length} loại Hạt Giống.");
            foreach(var s in seeds)
            {
                if (s == null) continue;
                CreateShopItem(s.seedID, s.seedName, s.inventoryIcon, s.buyPrice);
            }

            // 2. Tải Nông Cụ (Bỏ qua Bình Tưới và Cuốc)
            var tools = Resources.LoadAll<ToolItemSO>(ProjectPaths.RS_FOLDER_TOOL_DATA);
            Debug.Log($"<color=cyan>[SHOP - STAGE 1]</color> Đã nạp thành công {tools.Length} loại Nông Cụ.");
            foreach(var t in tools)
            {
                if (t == null) continue;
                // Cuốc = tool_hoe, Bình tưới = tool_water
                if (t.toolID == ProjectPaths.ID_TOOL_HOE || t.toolID == ProjectPaths.ID_TOOL_WATER) continue;
                CreateShopItem(t.toolID, t.toolName, t.inventoryIcon, t.buyPrice);
            }

            Debug.Log($"<color=yellow>[SHOP - STAGE 2: RAM -> UI]</color> Đã Render thành công lên Màn hình Shop!");
        }

        private void CreateShopItem(string itemID, string itemName, Sprite icon, int price)
        {
            GameObject go = Instantiate(shopItemPrefab, contentContainer);
            go.SetActive(true); // <-- BẬT ACTIVE VÌ PREFAB GỐC BỊ ẨN
            go.name = "ShopItem_" + itemID;

            // Gán thông tin hiển thị (Layout phụ thuộc vào Prefab: [0]=Icon, [1]=Name, [2]=PriceBtn)
            var image = go.transform.Find("Icon")?.GetComponent<Image>();
            if (image != null) image.sprite = icon;

            var txtName = go.transform.Find("Name")?.GetComponent<Text>();
            if (txtName != null) txtName.text = itemName;

            var btnObj = go.transform.Find("BuyButton");
            if (btnObj != null)
            {
                var txtPrice = btnObj.Find("Price")?.GetComponent<Text>();
                if (txtPrice != null) txtPrice.text = price > 0 ? "💎 " + price : "FREE";

                var btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => {
                        BuyItem(itemID, price);
                    });
                }
            }
        }

        private void BuyItem(string itemID, int price)
        {
            if (DataManager.Instance == null) return;

            // Kiểm tra Gold. Giả sử biến kiểm tra lấy gold:
            int currentGold = DataManager.Instance.GetGold();
            Debug.Log($"<color=orange>[SHOP - STAGE 3: Action CRUD]</color> Bấm mua {itemID}. Cần {price}💎. Đang có: {currentGold}💎");
            
            if (currentGold >= price)
            {
                Debug.Log($"<color=green>[SHOP - STAGE 3]</color> Trừ {price}💎. Bơm {itemID} (x1) vào RAM Pocket!");
                DataManager.Instance.AddGold(-price);
                DataManager.Instance.AddItem(itemID, 1);
                
                Debug.Log($"<color=magenta>[SHOP - STAGE 4: RAM -> Disk]</color> Commit dữ liệu Inventory vĩnh viễn xuống SQLite!");
                DataManager.Instance.CommitSessionInventory();
            }
            else
            {
                Debug.LogWarning($"<color=red>[Shop]</color> Không đủ Xèng! Cần {price}💎 nhưng chỉ có {currentGold}💎");
            }
        }
    }
}
