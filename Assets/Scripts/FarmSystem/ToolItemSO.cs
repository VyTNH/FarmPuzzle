using UnityEngine;

/// <summary>
/// ScriptableObject lưu thông tin hiển thị của công cụ/vật phẩm trong kho đồ.
/// Đặt file vào Resources/ToolData/ với tên SO_Tool_<toolID>.asset
/// Ví dụ: SO_Tool_tool_hoe.asset, SO_Tool_item_fertilizer.asset
/// </summary>
[CreateAssetMenu(fileName = "NewToolItem", menuName = "Farm/Tool Item")]
public class ToolItemSO : ScriptableObject
{
    [Tooltip("ID khớp với ItemID trong bảng Inventory (VD: tool_hoe, item_fertilizer)")]
    public string toolID;

    [Tooltip("Tên hiển thị trong UI Kho Hàng")]
    public string toolName;

    [Tooltip("Icon hiển thị trong ô Kho Hàng")]
    public Sprite inventoryIcon;
}
