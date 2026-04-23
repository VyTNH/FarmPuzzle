// Script này đã được thay thế bởi InventoryLayoutSetupWindow.
// Menu cũ được giữ lại và redirect sang tool mới để tránh nhầm lẫn.
using UnityEditor;

namespace FarmPuzzle.EditorTools
{
    public class FixInventoryScrollView
    {
        [MenuItem("FarmPuzzle/🔧 Fix Inventory ScrollView Mask")]
        public static void Fix()
        {
            // Redirect sang tool mới có đầy đủ chức năng hơn
            InventoryLayoutSetupWindow.Open();
        }
    }
}
