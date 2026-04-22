using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using FarmPuzzle.UI;
using FarmPuzzle.Decor;

namespace FarmPuzzle.EditorTools
{
    public class AutoSetupDecorUI
    {
        [MenuItem("Farm Puzzle/Thiết lập Decor UI (Tự động)")]
        public static void CreateDecorUIHierarchy()
        {
            DecorationManager dm = GameObject.FindFirstObjectByType<DecorationManager>();
            if (dm == null)
            {
                GameObject dmObj = new GameObject("DecorationManager", typeof(DecorationManager));
                dm = dmObj.GetComponent<DecorationManager>();
            }

            // Tự động load và gán Sprites từ gốc (Bỏ qua thư mục Resources lằng nhằng vì hay bị mất Slice meta)
            string spritePath = "Assets/Art/Sprites/Decor/cardboard castles - free assetpack/cardboard castles - free assetpack/castles_Sheet.png";
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            var spriteList = new System.Collections.Generic.List<Sprite>();
            foreach(var a in allAssets) 
            {
                if (a is Sprite spr) spriteList.Add(spr);
            }
            dm.allDecorSprites = spriteList.ToArray();
            EditorUtility.SetDirty(dm);

            Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Cảnh chưa có Canvas! Hãy tạo một Canvas trước (GameObject -> UI -> Canvas).");
                return;
            }

            // Xóa cái cũ nếu bấm nhiều lần
            var oldShop = GameObject.Find("DecorShopPanel");
            if (oldShop != null) GameObject.DestroyImmediate(oldShop);

            // 1. Tạo Shop Panel (neo lề PHẢI màn hình)
            GameObject shopPanel = new GameObject("DecorShopPanel", typeof(RectTransform), typeof(Image), typeof(DecorShopUI));
            shopPanel.transform.SetParent(canvas.transform, false);

            RectTransform shopRect = shopPanel.GetComponent<RectTransform>();
            shopRect.anchorMin = new Vector2(0.85f, 0.12f); // Neo cách đáy 12% để chừa inventory
            shopRect.anchorMax = new Vector2(1, 0.88f);     // Neo cách trần 12% để chừa top bar
            shopRect.offsetMin = Vector2.zero;
            shopRect.offsetMax = Vector2.zero;

            shopPanel.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Gắn hệ thống Kéo Cửa Trượt (Slide)
            DecorShopToggle toggleScript = shopPanel.AddComponent<DecorShopToggle>();
            toggleScript.shopPanel = shopRect;

            // ---- Tạo Cấu trúc ScrollView Chuẩn ----
            // 1. Đối tượng Viewport (đóng vai trò Mask)
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(shopPanel.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(10, 10); // Padding trong panel
            viewportRect.offsetMax = new Vector2(-10, -10);
            Mask mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.5f); // Bắt buộc phải có ảnh để Mask hoạt động

            // 2. Đối tượng Content
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); // Neo góc trên
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.MinSize;

            GridLayoutGroup gl = content.GetComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(80, 80);
            gl.spacing = new Vector2(10, 10);
            gl.startAxis = GridLayoutGroup.Axis.Horizontal;
            gl.childAlignment = TextAnchor.UpperCenter;

            // 3. Gắn ScrollRect vào Panel Ngoài Cùng
            ScrollRect sr = shopPanel.AddComponent<ScrollRect>();
            sr.content = contentRect;
            sr.viewport = viewportRect;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;


            // Nút bấm Mở/Đóng Gắn Viền Ngoài Panel
            GameObject toggleBtnObj = new GameObject("Btn_ToggleShop", typeof(RectTransform), typeof(Image), typeof(Button));
            toggleBtnObj.transform.SetParent(shopPanel.transform, false);
            RectTransform toggleBtnRect = toggleBtnObj.GetComponent<RectTransform>();
            toggleBtnRect.anchorMin = new Vector2(0, 0.5f);
            toggleBtnRect.anchorMax = new Vector2(0, 0.5f); // Neo mép bên trái của Panel Shop
            toggleBtnRect.pivot = new Vector2(1, 0.5f);     // Đẩy ra ngoài
            toggleBtnRect.sizeDelta = new Vector2(40, 80);
            toggleBtnRect.anchoredPosition = new Vector2(0, 0);
            toggleBtnObj.GetComponent<Image>().color = new Color(1, 0.5f, 0, 1); // Góc viền
            
            // Text cho Nút Open
            GameObject t_text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            t_text.transform.SetParent(toggleBtnObj.transform, false);
            t_text.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 80);
            TextMeshProUGUI tmpText = t_text.GetComponent<TextMeshProUGUI>();
            tmpText.text = "<";
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;
            
            UnityEngine.Events.UnityAction actionToggle = toggleScript.ToggleShop;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(toggleBtnObj.GetComponent<Button>().onClick, actionToggle);

            // 2. Tạo Prefab Item Mẫu
            GameObject itemTemplate = new GameObject("DecorUIItem_Template", typeof(RectTransform), typeof(Image), typeof(DecorDraggableTool));
            RectTransform itemRect = itemTemplate.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(80, 80);
            itemTemplate.GetComponent<Image>().color = new Color(1, 1, 1, 0.2f); // Nền xám
            
            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(itemTemplate.transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.3f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Image img = iconObj.GetComponent<Image>();
            img.preserveAspect = true; // Giữ nguyên tỉ lệ để hình Cát tông không bị bóp méo!
            itemTemplate.GetComponent<DecorDraggableTool>().iconImage = img;

            // Price Text
            GameObject priceObj = new GameObject("Price", typeof(RectTransform), typeof(TextMeshProUGUI));
            priceObj.transform.SetParent(itemTemplate.transform, false);
            RectTransform priceRect = priceObj.GetComponent<RectTransform>();
            priceRect.anchorMin = new Vector2(0, 0);
            priceRect.anchorMax = new Vector2(1, 0.3f);
            priceRect.offsetMin = Vector2.zero;
            priceRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI tmp = priceObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "100 Vàng";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.yellow;
            tmp.enableAutoSizing = true;
            itemTemplate.GetComponent<DecorDraggableTool>().priceText = tmp;

            // Setup References
            shopPanel.GetComponent<DecorShopUI>().contentContainer = content.transform;
            
            // Save as Prefab & Hide Original
            string prefabPath = "Assets/Prefabs/UI/DecorUIItem_Template.prefab";
            if (!System.IO.Directory.Exists("Assets/Prefabs/UI"))
                System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
                
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(itemTemplate, prefabPath);
            shopPanel.GetComponent<DecorShopUI>().decorItemPrefab = savedPrefab;
            GameObject.DestroyImmediate(itemTemplate);

            // 3. Tạo Confirm Popup (nếu chưa có)
            if (GameObject.Find("DecorConfirmPopup") == null)
            {
                GameObject popupObj = new GameObject("DecorConfirmPopup", typeof(RectTransform), typeof(Image), typeof(DecorConfirmPopup));
                popupObj.transform.SetParent(canvas.transform, false);
                RectTransform popupRect = popupObj.GetComponent<RectTransform>();
                popupRect.anchorMin = new Vector2(0.5f, 0.5f);
                popupRect.anchorMax = new Vector2(0.5f, 0.5f);
                popupRect.sizeDelta = new Vector2(400, 300);
                popupObj.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

                GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleObj.transform.SetParent(popupObj.transform, false);
                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0, 0.7f);
                titleRect.anchorMax = new Vector2(1, 1);
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;
                TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
                titleTmp.text = "Bạn có muốn mua không?";
                titleTmp.alignment = TextAlignmentOptions.Center;
                popupObj.GetComponent<DecorConfirmPopup>().titleText = titleTmp;

                GameObject costObj = new GameObject("Cost", typeof(RectTransform), typeof(TextMeshProUGUI));
                costObj.transform.SetParent(popupObj.transform, false);
                RectTransform costRect = costObj.GetComponent<RectTransform>();
                costRect.anchorMin = new Vector2(0, 0.4f);
                costRect.anchorMax = new Vector2(1, 0.7f);
                costRect.offsetMin = Vector2.zero;
                costRect.offsetMax = Vector2.zero;
                TextMeshProUGUI costTmp = costObj.GetComponent<TextMeshProUGUI>();
                costTmp.text = "100 Vàng";
                costTmp.color = Color.yellow;
                costTmp.alignment = TextAlignmentOptions.Center;
                popupObj.GetComponent<DecorConfirmPopup>().priceText = costTmp;

                GameObject btnYesObj = new GameObject("Btn_Yes", typeof(RectTransform), typeof(Image), typeof(Button));
                btnYesObj.transform.SetParent(popupObj.transform, false);
                RectTransform btnYesRect = btnYesObj.GetComponent<RectTransform>();
                btnYesRect.anchorMin = new Vector2(0.1f, 0.1f);
                btnYesRect.anchorMax = new Vector2(0.45f, 0.3f);
                btnYesRect.offsetMin = Vector2.zero;
                btnYesRect.offsetMax = Vector2.zero;
                btnYesObj.GetComponent<Image>().color = Color.green;
                UnityEngine.Events.UnityAction actionYes = popupObj.GetComponent<DecorConfirmPopup>().OnClickConfirm;
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btnYesObj.GetComponent<Button>().onClick, actionYes);

                GameObject btnNoObj = new GameObject("Btn_No", typeof(RectTransform), typeof(Image), typeof(Button));
                btnNoObj.transform.SetParent(popupObj.transform, false);
                RectTransform btnNoRect = btnNoObj.GetComponent<RectTransform>();
                btnNoRect.anchorMin = new Vector2(0.55f, 0.1f);
                btnNoRect.anchorMax = new Vector2(0.9f, 0.3f);
                btnNoRect.offsetMin = Vector2.zero;
                btnNoRect.offsetMax = Vector2.zero;
                btnNoObj.GetComponent<Image>().color = Color.red;
                UnityEngine.Events.UnityAction actionNo = popupObj.GetComponent<DecorConfirmPopup>().OnClickCancel;
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btnNoObj.GetComponent<Button>().onClick, actionNo);

                popupObj.SetActive(false); // Ẩn
            }

            Debug.Log("<color=green>Hoàn tất Auto Setup Decor UI! Shop giờ nằm ở Lề Phải và có cửa trượt.</color>");
            Selection.activeGameObject = shopPanel;
            EditorGUIUtility.PingObject(shopPanel);
        }
    }
}
