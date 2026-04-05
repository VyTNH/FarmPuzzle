using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPuzzle.Economy
{
    public enum ItemCategory
    {
        Seed,
        Decor
    }

    [Serializable]
    public class ShopItemEntry
    {
        [Tooltip("ID liên kết với SEED_ITEM hoặc DECOR_ITEM trong cơ sở dữ liệu")]
        public string ReferenceID;
        
        [Tooltip("Danh mục của vật phẩm (Seed hoặc Decor)")]
        public ItemCategory Category;
        
        [Tooltip("Giá mua vật phẩm bằng Vàng")]
        public int Price;
        
        [Tooltip("Tên hoặc Tiêu đề hiển thị trên UI")]
        public string DisplayName;
        
        [Tooltip("Biểu tượng hiển thị trên UI")]
        public Sprite Icon;
    }

    [CreateAssetMenu(fileName = "New Shop Catalog", menuName = "FarmPuzzle/Economy/Shop Catalog")]
    public class ShopCatalogSO : ScriptableObject
    {
        [Tooltip("Danh sách tất cả các vật phẩm có sẵn trong cửa hàng")]
        public List<ShopItemEntry> AvailableItems = new List<ShopItemEntry>();

        public ShopItemEntry GetItem(string referenceID)
        {
            return AvailableItems.Find(item => item.ReferenceID == referenceID);
        }

        public List<ShopItemEntry> GetItemsByCategory(ItemCategory category)
        {
            return AvailableItems.FindAll(item => item.Category == category);
        }
    }
}
