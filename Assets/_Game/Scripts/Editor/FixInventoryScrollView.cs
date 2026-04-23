using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace FarmPuzzle.EditorTools
{
    public class FixInventoryScrollView
    {
        [MenuItem("FarmPuzzle/🔧 Fix Inventory ScrollView Mask")]
        public static void Fix()
        {
            // Tìm Canvas_Inventory trong Scene
            var inventoryCanvas = GameObject.Find("Canvas_Inventory");
            if (inventoryCanvas == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas_Inventory trong Scene!\nHãy mở Scene Final và chạy lại.", "OK");
                return;
            }

            // Tìm Viewport (BackGround - con của Scroll View)
            var scrollView = FindChildRecursive(inventoryCanvas.transform, "Scroll View");
            if (scrollView == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy 'Scroll View' bên trong Canvas_Inventory!", "OK");
                return;
            }

            Transform viewport = null;
            foreach (Transform child in scrollView)
            {
                // Viewport là con của ScrollView có ScrollRect.Viewport trỏ tới
                var scrollRect = scrollView.GetComponent<ScrollRect>();
                if (scrollRect != null && scrollRect.viewport != null)
                {
                    viewport = scrollRect.viewport;
                    break;
                }
                // Fallback: tìm con tên BackGround hoặc Viewport
                if (child.name == "BackGround" || child.name == "Viewport")
                {
                    viewport = child;
                    break;
                }
            }

            if (viewport == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Viewport!\nKiểm tra ScrollRect.Viewport đã được gán chưa.", "OK");
                return;
            }

            // Gắn Mask nếu chưa có
            Mask mask = viewport.GetComponent<Mask>();
            if (mask == null)
            {
                mask = Undo.AddComponent<Mask>(viewport.gameObject);
                mask.showMaskGraphic = false; // Ẩn viền trắng của mask
                Debug.Log("<color=green>[Fix ScrollView]</color> Đã thêm Mask vào: " + viewport.name);
            }
            else
            {
                Debug.Log("<color=yellow>[Fix ScrollView]</color> Viewport đã có Mask rồi, bỏ qua.");
            }

            // Đảm bảo Image trên Viewport có Source Image (yêu cầu để Mask hoạt động)
            Image img = viewport.GetComponent<Image>();
            if (img == null)
            {
                img = Undo.AddComponent<Image>(viewport.gameObject);
            }
            // Dùng Sprite mặc định Unity (trắng) để Mask cắt đúng
            if (img.sprite == null)
            {
                img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            }
            img.color = new Color(1, 1, 1, 0); // Trong suốt hoàn toàn (ẩn Image, chỉ giữ Mask)

            // Đảm bảo ScrollRect bật Horizontal chứ không bật Vertical
            var sr = scrollView.GetComponent<ScrollRect>();
            if (sr != null)
            {
                sr.horizontal = true;
                sr.vertical = false;
            }

            EditorUtility.SetDirty(viewport.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "✅ Hoàn thành!",
                $"Đã gắn Mask vào '{viewport.name}'.\nContent sẽ bị clip đúng trong Scroll View.\n\nCtrl+S để lưu Scene.",
                "OK");
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var result = FindChildRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
