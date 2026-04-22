using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using FarmPuzzle.UI;

namespace FarmPuzzle.EditorTools
{
    public class AutoSetupSeedShopUI
    {
        [MenuItem("Farm Puzzle/Thiết lập Seed Shop UI (Trái)")]
        public static void CreateSeedShopUIHierarchy()
        {
            Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Cảnh chưa có Canvas! Hãy tạo một Canvas trước.");
                return;
            }

            var oldShop = GameObject.Find("SeedShopPanel");
            if (oldShop != null) GameObject.DestroyImmediate(oldShop);

            // 1. Tạo Shop Panel (neo lề TRÁI màn hình)
            GameObject shopPanel = new GameObject("SeedShopPanel", typeof(RectTransform), typeof(Image), typeof(SeedShopToggle), typeof(SeedShopUI));
            shopPanel.transform.SetParent(canvas.transform, false);

            RectTransform shopRect = shopPanel.GetComponent<RectTransform>();
            shopRect.anchorMin = new Vector2(0f, 0.12f); // Neo cách đáy 12%
            shopRect.anchorMax = new Vector2(0.15f, 0.88f); // Rộng 15% bề ngang, neo trần 88%
            shopRect.offsetMin = Vector2.zero;
            shopRect.offsetMax = Vector2.zero;
            shopPanel.GetComponent<Image>().color = new Color(0.1f, 0.3f, 0.1f, 0.9f); // Màu xanh lá đục

            // Script kéo cửa
            SeedShopToggle toggleScript = shopPanel.GetComponent<SeedShopToggle>();
            toggleScript.shopPanel = shopRect;

            // ---- Tạo Cấu trúc ScrollView Chuẩn ----
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(shopPanel.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(10, 10);
            viewportRect.offsetMax = new Vector2(-10, -10);
            Mask mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.5f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.MinSize;

            VerticalLayoutGroup vl = content.GetComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlHeight = false;
            vl.childControlWidth = true;
            vl.spacing = 15;

            ScrollRect sr = shopPanel.AddComponent<ScrollRect>();
            sr.content = contentRect;
            sr.viewport = viewportRect;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;

            // Nút bấm Mở/Đóng Gắn Viền Ngoài Panel
            GameObject btnOpenObj = new GameObject("Btn_ToggleShop", typeof(RectTransform), typeof(Image), typeof(Button));
            btnOpenObj.transform.SetParent(shopPanel.transform, false);
            RectTransform btnRect = btnOpenObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1f, 0.5f); // Gắn cạnh phải của mảng (lòi ra ngoài nửa)
            btnRect.anchorMax = new Vector2(1f, 0.5f);
            btnRect.sizeDelta = new Vector2(30, 80);
            btnRect.anchoredPosition = new Vector2(15, 0); // Đẩy ra ngoài 15 pixel
            btnOpenObj.GetComponent<Image>().color = new Color(1f, 0.5f, 0f, 1f); // Nút màu cam

            GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnTextObj.transform.SetParent(btnOpenObj.transform, false);
            Text btnTxt = btnTextObj.GetComponent<Text>();
            btnTxt.text = ">";
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.color = Color.white;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 80);

            Button btnToggle = btnOpenObj.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnToggle.onClick, toggleScript.ToggleShop);

            // ---- Tạo Prefab Ảo Giao Diện Dùng Chung Cho Từng Lô ----
            GameObject itemPrefab = new GameObject("SeedShopItem_Template", typeof(RectTransform), typeof(Image));
            itemPrefab.transform.SetParent(shopPanel.transform, false); // Ném tạm vào shopPanel để hide đi
            itemPrefab.SetActive(false);
            RectTransform itemRect = itemPrefab.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(50, 100);

            GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(itemPrefab.transform, false);
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(0, 20);
            iconRect.sizeDelta = new Vector2(40, 40);

            GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameObj.transform.SetParent(itemPrefab.transform, false);
            Text nameTxt = nameObj.GetComponent<Text>();
            nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.color = Color.white;
            nameTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameTxt.verticalOverflow = VerticalWrapMode.Overflow;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.fontSize = 12;
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchoredPosition = new Vector2(0, -10);
            nameRect.sizeDelta = new Vector2(50, 20);

            GameObject buyBtnObj = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buyBtnObj.transform.SetParent(itemPrefab.transform, false);
            buyBtnObj.GetComponent<Image>().color = Color.yellow;
            RectTransform buyBtnRect = buyBtnObj.GetComponent<RectTransform>();
            buyBtnRect.anchoredPosition = new Vector2(0, -35);
            buyBtnRect.sizeDelta = new Vector2(40, 20);

            GameObject priceObj = new GameObject("Price", typeof(RectTransform), typeof(Text));
            priceObj.transform.SetParent(buyBtnObj.transform, false);
            Text priceTxt = priceObj.GetComponent<Text>();
            priceTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            priceTxt.color = Color.black;
            priceTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            priceTxt.verticalOverflow = VerticalWrapMode.Overflow;
            priceTxt.alignment = TextAnchor.MiddleCenter;
            priceTxt.fontSize = 10;
            priceTxt.text = "10";
            priceObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 20);

            // Hút vào Logic UI
            SeedShopUI uiScript = shopPanel.GetComponent<SeedShopUI>();
            uiScript.contentContainer = content.transform;
            uiScript.shopItemPrefab = itemPrefab;

            Debug.Log("[SeedShopUI] Đã dán thành công Cửa hàng Hạt Giống lề trái!");
        }
    }
}
