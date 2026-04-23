using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmPuzzle.Core.Database;
using FarmPuzzle.Decor;
using System.Collections.Generic;

namespace FarmPuzzle.UI
{
    public class DecorShopUI : MonoBehaviour
    {
        [Header("References")]
        public Transform contentContainer; // Trỏ vào Content của ScrollView
        public GameObject decorItemPrefab; // Mẫu thẻ DecorItem chứa DecorDraggableTool

        private void Start()
        {
            // Tránh chạy khi chưa khởi tạo DB
            Invoke(nameof(PopulateShop), 1.0f);
        }

        private void PopulateShop()
        {
            if (contentContainer == null || decorItemPrefab == null) return;
            
            // Xóa rác cũ nếu có
            foreach(Transform t in contentContainer) Destroy(t.gameObject);

            if (DecorationManager.Instance == null) 
            {
                Debug.LogWarning("[DecorShopUI] Đang chờ DecorationManager khởi tạo...");
                Invoke(nameof(PopulateShop), 1.0f); // Gọi lại sau 1s
                return;
            }

            var items = DecorationManager.Instance.GetAvailableItems();
            
            // Lọc bớt nếu quá nhiều (ở đây ta show mấy hộp carton)
            foreach(var item in items)
            {
                var go = Instantiate(decorItemPrefab, contentContainer);
                var draggable = go.GetComponent<DecorDraggableTool>();
                if (draggable != null)
                {
                    draggable.SetupItem(item);
                }
            }
        }
    }
}
