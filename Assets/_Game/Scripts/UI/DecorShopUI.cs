using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmPuzzle.Core.Database;
using FarmPuzzle.Decor;
using FarmPuzzle.Core;

namespace FarmPuzzle.UI
{
    /// <summary>
    /// DecorShopUI - Hiển thị danh sách đồ trang trí theo cùng layout với SeedShopUI.
    /// Mỗi item có: Icon + Tên + Nút kéo thả (DecorDraggableTool).
    /// Layout thống nhất: contentContainer (ScrollView) + decorItemPrefab.
    /// </summary>
    public class DecorShopUI : MonoBehaviour
    {
        [Header("References — cùng cấu trúc với SeedShopUI")]
        public Transform contentContainer;  // Content của ScrollView
        public GameObject decorItemPrefab;  // Prefab chứa Icon + Name + DecorDraggableTool

        private void Start()
        {
            // Delay giống SeedShopUI để đảm bảo DataManager và DecorationManager đã sẵn sàng
            Invoke(nameof(PopulateShop), 0.5f);
        }

        private void PopulateShop()
        {
            if (contentContainer == null || decorItemPrefab == null)
            {
                Debug.LogWarning("[DecorShopUI] Chưa gán contentContainer hoặc decorItemPrefab!");
                return;
            }

            // Xóa items cũ nếu có
            foreach (Transform t in contentContainer) Destroy(t.gameObject);

            if (DecorationManager.Instance == null)
            {
                Debug.LogWarning("[DecorShopUI] DecorationManager chưa sẵn sàng, thử lại sau 1s...");
                Invoke(nameof(PopulateShop), 1.0f);
                return;
            }

            var items = DecorationManager.Instance.GetAvailableItems();
            Debug.Log($"<color=cyan>[DecorShop]</color> Bắt đầu render {items.Count} đồ decor lên cửa hàng.");

            foreach (var item in items)
            {
                if (item == null) continue;
                CreateDecorItem(item);
            }

            Debug.Log($"<color=yellow>[DecorShop]</color> Đã render xong {items.Count} item lên UI.");
        }

        private void CreateDecorItem(DecorItemModel item)
        {
            GameObject go = Instantiate(decorItemPrefab, contentContainer);
            go.SetActive(true);
            go.name = "DecorItem_" + item.DecorID;

            // ── Gán Icon (cùng cách SeedShopUI) ──
            var icon = go.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                var sprite = DecorationManager.Instance.GetSprite(item.DecorID);
                if (sprite != null) icon.sprite = sprite;
            }

            // ── Gán Tên ──
            var txtName = go.transform.Find("Name")?.GetComponent<Text>();
            if (txtName != null)
            {
                // Hiển thị loại tầng trong tên giống DecorDraggableTool cũ
                txtName.text = item.IsFlatTop ? $"[Đáy] {item.Name}" : $"[Mái] {item.Name}";
            }

            // ── Gán Giá + Kéo thả (BuyButton → DecorDraggableTool) ──
            var btnObj = go.transform.Find("BuyButton");
            if (btnObj != null)
            {
                // Hiển thị giá
                var txtPrice = btnObj.Find("Price")?.GetComponent<Text>();
                if (txtPrice != null)
                    txtPrice.text = item.BuyPrice > 0 ? $"💰 {item.BuyPrice}" : "FREE";

                // Màu nền nút: Cam = Mái, Xanh = Đáy (giống logic cũ của DecorDraggableTool)
                var btnImg = btnObj.GetComponent<Image>();
                if (btnImg != null)
                    btnImg.color = item.IsFlatTop
                        ? new Color(0.2f, 0.6f, 0.2f) // Xanh lá = Đáy (đặt được trên đất)
                        : new Color(0.8f, 0.4f, 0.1f); // Cam = Mái (đặt trên khối khác)
            }

            // ── Gán DecorDraggableTool (kéo thả vào farm) ──
            var draggable = go.GetComponent<DecorDraggableTool>();
            if (draggable != null)
                draggable.SetupItem(item);
            else
                Debug.LogWarning($"[DecorShopUI] Prefab '{decorItemPrefab.name}' thiếu component DecorDraggableTool!");
        }
    }
}
