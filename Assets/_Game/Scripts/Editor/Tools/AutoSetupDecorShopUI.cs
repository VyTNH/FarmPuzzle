using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using FarmPuzzle.UI;

namespace FarmPuzzle.EditorTools
{
    /// <summary>
    /// Tự động tạo DecorShopPanel với layout THỐNG NHẤT với SeedShopPanel.
    /// Menu: Farm Puzzle → Thiết lập Decor Shop UI (Phải)
    /// 
    /// Cấu trúc giống hệt SeedShopPanel:
    ///   DecorShopPanel (Image + DecorShopToggle + DecorShopUI + ScrollRect)
    ///   ├── Viewport (Mask)
    ///   │   └── Content (VerticalLayoutGroup)
    ///   ├── Btn_ToggleShop (nút tam giác lòi ra bên trái)
    ///   └── DecorShopItem_Template (ẩn, dùng làm prefab runtime)
    ///       ├── Icon
    ///       ├── Name
    ///       └── BuyButton
    ///           └── Price
    /// </summary>
    public class AutoSetupDecorShopUI
    {
        [MenuItem("Farm Puzzle/Thiết lập Decor Shop UI (Phải)")]
        public static void CreateDecorShopUIHierarchy()
        {
            Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[DecorShopUI] Chưa có Canvas trong scene! Hãy tạo Canvas trước.");
                return;
            }

            // Nếu đã tồn tại → xóa và dựng lại
            var old = GameObject.Find("DecorShopPanel");
            if (old != null)
            {
                if (!EditorUtility.DisplayDialog("Cảnh báo", "DecorShopPanel đã tồn tại. Xóa và dựng lại?", "OK", "Hủy"))
                    return;
                GameObject.DestroyImmediate(old);
            }

            Undo.SetCurrentGroupName("Create DecorShopPanel");
            int undoGroup = Undo.GetCurrentGroup();

            // ════════════════════════════════════════════════════════
            // 1. SHOP PANEL — Neo lề PHẢI (đối xứng với SeedShopPanel bên trái)
            // ════════════════════════════════════════════════════════
            GameObject shopPanel = new GameObject("DecorShopPanel",
                typeof(RectTransform), typeof(Image), typeof(DecorShopToggle), typeof(DecorShopUI));
            Undo.RegisterCreatedObjectUndo(shopPanel, "Create DecorShopPanel");
            shopPanel.transform.SetParent(canvas.transform, false);

            RectTransform shopRect = shopPanel.GetComponent<RectTransform>();
            shopRect.anchorMin = new Vector2(0.85f, 0.12f); // Neo cách phải, cách đáy 12%
            shopRect.anchorMax = new Vector2(1.0f,  0.88f); // Rộng 15% từ phải, neo trần 88%
            shopRect.offsetMin = Vector2.zero;
            shopRect.offsetMax = Vector2.zero;

            // Màu nền: nâu đất giống khung gỗ trong ảnh SeedShopPanel
            shopPanel.GetComponent<Image>().color = new Color(0.35f, 0.22f, 0.10f, 0.95f);

            // Toggle script
            DecorShopToggle toggleScript = shopPanel.GetComponent<DecorShopToggle>();
            toggleScript.shopPanel = shopRect;

            // ════════════════════════════════════════════════════════
            // 2. VIEWPORT + CONTENT (ScrollView chuẩn — giống SeedShop)
            // ════════════════════════════════════════════════════════
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            Undo.RegisterCreatedObjectUndo(viewport, "Create Viewport");
            viewport.transform.SetParent(shopPanel.transform, false);

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8, 8);
            viewportRect.offsetMax = new Vector2(-8, -8);

            Mask mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);

            // Content
            GameObject content = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(content, "Create Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot     = new Vector2(0.5f, 1);
            contentRect.sizeDelta = Vector2.zero;

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.MinSize;

            VerticalLayoutGroup vl = content.GetComponent<VerticalLayoutGroup>();
            vl.childAlignment    = TextAnchor.UpperCenter;
            vl.childControlHeight = false;
            vl.childControlWidth  = true;
            vl.spacing = 12;
            vl.padding = new RectOffset(4, 4, 6, 6);

            // ScrollRect
            ScrollRect sr = shopPanel.AddComponent<ScrollRect>();
            sr.content      = contentRect;
            sr.viewport     = viewportRect;
            sr.horizontal   = false;
            sr.vertical     = true;
            sr.movementType = ScrollRect.MovementType.Elastic;

            // ════════════════════════════════════════════════════════
            // 3. NÚT TOGGLE (lòi ra bên TRÁI, đối xứng SeedShop lòi bên phải)
            // ════════════════════════════════════════════════════════
            GameObject btnToggleObj = new GameObject("Btn_ToggleShop",
                typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(btnToggleObj, "Create Toggle Button");
            btnToggleObj.transform.SetParent(shopPanel.transform, false);

            RectTransform btnRect = btnToggleObj.GetComponent<RectTransform>();
            btnRect.anchorMin       = new Vector2(0f, 0.5f); // Bên TRÁI panel
            btnRect.anchorMax       = new Vector2(0f, 0.5f);
            btnRect.sizeDelta       = new Vector2(30, 80);
            btnRect.anchoredPosition = new Vector2(-15, 0); // Đẩy ra ngoài bên trái

            btnToggleObj.GetComponent<Image>().color = new Color(0.8f, 0.5f, 0.1f); // Cam = trang trí

            // Text nút: "<" (ngược lại ">" của SeedShop)
            GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(btnTxtObj, "Create Button Text");
            btnTxtObj.transform.SetParent(btnToggleObj.transform, false);

            Text btnTxt = btnTxtObj.GetComponent<Text>();
            btnTxt.text      = "<";
            btnTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.color     = Color.white;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 80);

            Button btnComp = btnToggleObj.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnComp.onClick, toggleScript.ToggleShop);

            // ════════════════════════════════════════════════════════
            // 4. ITEM TEMPLATE — cùng cấu trúc SeedShopItem (Icon + Name + BuyButton/Price)
            // ════════════════════════════════════════════════════════
            GameObject itemTemplate = new GameObject("DecorShopItem_Template",
                typeof(RectTransform), typeof(Image), typeof(DecorDraggableTool));
            Undo.RegisterCreatedObjectUndo(itemTemplate, "Create Item Template");
            itemTemplate.transform.SetParent(shopPanel.transform, false);
            itemTemplate.SetActive(false); // Ẩn template, clone khi runtime

            itemTemplate.GetComponent<Image>().color = new Color(0.25f, 0.16f, 0.08f, 0.85f);

            RectTransform itemRect = itemTemplate.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 105); // Cao hơn SeedShop vì có thêm thông tin loại tầng

            // 4a. Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(iconObj, "Create Icon");
            iconObj.transform.SetParent(itemTemplate.transform, false);
            iconObj.GetComponent<Image>().color = Color.white;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(0, 28);
            iconRect.sizeDelta        = new Vector2(50, 50);

            // 4b. Name
            GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(nameObj, "Create Name");
            nameObj.transform.SetParent(itemTemplate.transform, false);
            Text nameTxt = nameObj.GetComponent<Text>();
            nameTxt.font              = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.color             = new Color(0.95f, 0.85f, 0.6f); // Màu vàng nhạt giống bảng gỗ
            nameTxt.alignment         = TextAnchor.MiddleCenter;
            nameTxt.fontSize          = 11;
            nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameTxt.verticalOverflow   = VerticalWrapMode.Overflow;
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchoredPosition = new Vector2(0, -14);
            nameRect.sizeDelta        = new Vector2(85, 28);

            // 4c. BuyButton (nút kéo thả — không phải mua ngay, mà kéo vào farm)
            GameObject buyBtnObj = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(buyBtnObj, "Create BuyButton");
            buyBtnObj.transform.SetParent(itemTemplate.transform, false);
            buyBtnObj.GetComponent<Image>().color = new Color(0.15f, 0.55f, 0.15f); // Xanh lá
            RectTransform buyBtnRect = buyBtnObj.GetComponent<RectTransform>();
            buyBtnRect.anchoredPosition = new Vector2(0, -44);
            buyBtnRect.sizeDelta        = new Vector2(80, 24);

            // 4d. Price text trong BuyButton
            GameObject priceObj = new GameObject("Price", typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(priceObj, "Create Price");
            priceObj.transform.SetParent(buyBtnObj.transform, false);
            Text priceTxt = priceObj.GetComponent<Text>();
            priceTxt.font              = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            priceTxt.color             = Color.white;
            priceTxt.alignment         = TextAnchor.MiddleCenter;
            priceTxt.fontSize          = 11;
            priceTxt.fontStyle         = FontStyle.Bold;
            priceTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            priceTxt.verticalOverflow   = VerticalWrapMode.Overflow;
            priceTxt.text              = "💰 50";
            priceObj.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 24);

            // Gắn references vào DecorShopUI script
            DecorShopUI uiScript = shopPanel.GetComponent<DecorShopUI>();
            uiScript.contentContainer = content.transform;
            uiScript.decorItemPrefab  = itemTemplate;

            // Kết thúc undo group
            Undo.CollapseUndoOperations(undoGroup);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[DecorShopUI] ✅ Đã tạo DecorShopPanel thống nhất với SeedShopPanel! Ctrl+Z để hoàn tác.");
            EditorUtility.DisplayDialog(
                "✅ Hoàn tất!",
                "DecorShopPanel đã được tạo thành công!\n\n" +
                "Layout giống hệt SeedShopPanel:\n" +
                "• ScrollView + VerticalLayout\n" +
                "• Icon + Name + BuyButton/Price\n" +
                "• Nút Toggle lòi ra bên trái\n\n" +
                "Ctrl+Z để hoàn tác | Ctrl+S để lưu.",
                "OK");
        }

        [MenuItem("Farm Puzzle/Thiết lập Decor Shop UI (Phải)", true)]
        private static bool Validate() => !Application.isPlaying;
    }
}
