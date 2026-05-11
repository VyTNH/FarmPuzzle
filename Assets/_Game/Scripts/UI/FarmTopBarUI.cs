using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using FarmPuzzle.Meta;
using FarmPuzzle.Core;
using FarmPuzzle.Core.Database;
using FarmPuzzle.LandPuzzle;
using FarmPuzzle.LandPuzzle.Data;
using TMPro;

namespace FarmPuzzle.UI
{
    /// <summary>
    /// Controller cho Top Bar của màn hình Farm.
    /// Tự động bind: Energy, Gold (Money), Quest hiện tại.
    /// </summary>
    public class FarmTopBarUI : MonoBehaviour
    {
        [Header("Energy Bar")]
        public TextMeshProUGUI energyText;
        public Text energyTimerText; // Mới: Hiển thị đếm ngược (01:45)
        public Slider energySlider;

        [Header("Gold & EXP")]
        public TextMeshProUGUI goldText;   // Bind vào PlayerModel.Money
        public TextMeshProUGUI gemText;    // Bind vào PlayerModel.EXP (giả Gem/XP)

        [Header("Quest Tracker")]
        public GameObject questTrackerPanel;
        public Image questItemIcon;       // Icon của item cần thu thập
        public Text questTitleText;
        public TextMeshProUGUI questProgressText;
        public Slider questProgressSlider;

        [Header("Player Info")]
        public Text playerLevelText; // Hiển thị EXP tạm

        private void OnEnable()
        {
            DataManager.OnPlayerLoggedIn         += Refresh;
            DataManager.OnInventoryChanged       += OnInventoryChangedHandler;
            DataManager.OnInventoryItemAdded     += OnInventoryChangedHandler;
            QuestManager.OnProgressUpdated       += RefreshQuest;

            // MỚI: Đăng ký lắng nghe sự kiện thay đổi năng lượng để cập nhật UI ngay lập tức
            if (EnergySystem.Instance != null)
            {
                EnergySystem.Instance.OnEnergyChanged += UpdateEnergyUI;
            }

            Refresh();
            RefreshQuest();
        }

        private void OnDisable()
        {
            DataManager.OnPlayerLoggedIn         -= Refresh;
            DataManager.OnInventoryChanged       -= OnInventoryChangedHandler;
            DataManager.OnInventoryItemAdded     -= OnInventoryChangedHandler;
            QuestManager.OnProgressUpdated       -= RefreshQuest;

            if (EnergySystem.Instance != null)
            {
                EnergySystem.Instance.OnEnergyChanged -= UpdateEnergyUI;
            }
        }

        private void OnInventoryChangedHandler(string itemID, int qty) => RefreshGold();

        private void Start()
        {
            // Refresh được gọi trong OnEnable khi login thành công
        }

        private void Update()
        {
            UpdateEnergyTimer();
        }

        private void UpdateEnergyUI(int current, int max)
        {
            RefreshEnergy();
        }

        private void UpdateEnergyTimer()
        {
            var es = EnergySystem.Instance;
            if (es == null || energyTimerText == null) return;

            if (es.CurrentEnergy >= es.MaxEnergy)
            {
                energyTimerText.text = ""; // Đầy thì ẩn
            }
            else
            {
                float time = es.TimeToNextRegen;
                int minutes = Mathf.FloorToInt(time / 60);
                int seconds = Mathf.FloorToInt(time % 60);
                energyTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }

        public void Refresh()
        {
            RefreshEnergy();
            RefreshGold();
            RefreshQuest();
            RefreshPlayerLevel();
        }

        public void RefreshEnergy()
        {
            var es = EnergySystem.Instance;
            if (es == null) return;

            if (energyText != null)
                energyText.text = $"{es.CurrentEnergy} / {es.MaxEnergy}";

            if (energySlider != null)
            {
                energySlider.maxValue = es.MaxEnergy;
                energySlider.value    = es.CurrentEnergy;
            }
        }

        public void RefreshGold()
        {
            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null) return;
            var player = DataManager.Instance.CurrentPlayer;

            if (goldText != null)
                goldText.text = $"{player.Money:N0}";

            if (gemText != null)
                gemText.text = $"{player.EXP}";
        }

        public void RefreshQuest()
        {
            var qm = QuestManager.Instance;

            if (qm == null || qm.activeQuests == null || qm.activeQuests.Count == 0)
            {
                if (questTrackerPanel != null) questTrackerPanel.SetActive(false);
                return;
            }
            if (questTrackerPanel != null) questTrackerPanel.SetActive(true);

            // ── Xóa QuestRow cũ ──
            for (int i = questTrackerPanel.transform.childCount - 1; i >= 0; i--)
            {
                var ch = questTrackerPanel.transform.GetChild(i);
                if (ch.name.StartsWith("QuestRow_")) Destroy(ch.gameObject);
            }

            // ── VerticalLayout cho panel ──
            var vlg = questTrackerPanel.GetComponent<VerticalLayoutGroup>()
                   ?? questTrackerPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing            = 3;
            vlg.childAlignment     = TextAnchor.UpperLeft;
            vlg.childControlWidth  = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(0, 0, 2, 2);

            var csf = questTrackerPanel.GetComponent<ContentSizeFitter>()
                   ?? questTrackerPanel.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── Màu sắc giống QuestPanel ──
            // Fill đang làm: cam/vàng = new Color(0.95f, 0.65f, 0.1f)
            // Fill xong:     xanh lá  = new Color(0.25f, 0.75f, 0.25f)
            // Nền slider:    xám tối  = new Color(0.18f, 0.18f, 0.18f, 0.85f)

            foreach (var q in qm.activeQuests)
            {
                if (q == null) continue;

                float prog = qm.GetQuestProgress(q.questID);
                bool  done = prog >= q.targetAmount;
                Color fillColor = done
                    ? new Color(0.25f, 0.75f, 0.25f, 1f)   // Xanh lá = xong ✓
                    : new Color(0.95f, 0.65f, 0.1f,  1f);  // Cam/vàng = đang làm (giống QuestPanel gốc)

                // ── ROW CONTAINER ─ giống QuestPanel về chiều cao ──
                var row = new GameObject($"QuestRow_{q.questID}", typeof(RectTransform));
                row.transform.SetParent(questTrackerPanel.transform, false);
                var rowLE = row.AddComponent<LayoutElement>();
                rowLE.minHeight       = 26;
                rowLE.preferredHeight = 26;

                // ── ICON TRÒN bên trái (giống QuestionIcon trong QuestPanel) ──
                // Dùng absolute anchor thay vì LayoutGroup để layout giống QuestPanel
                var iconGO = new GameObject("QuestionIcon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(row.transform, false);
                var iconRT = iconGO.GetComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0f, 0.5f);
                iconRT.anchorMax = new Vector2(0f, 0.5f);
                iconRT.pivot     = new Vector2(0.5f, 0.5f);
                iconRT.sizeDelta        = new Vector2(26f, 26f);
                iconRT.anchoredPosition = new Vector2(13f, 0f); // Căn giữa dọc, sát trái

                var iconImg = iconGO.GetComponent<Image>();
                // Load crop icon
                Sprite cropSprite = null;
                if (!string.IsNullOrEmpty(q.targetItemID) &&
                    q.targetItemID.StartsWith(ProjectPaths.PREFIX_PRODUCT))
                {
                    var crop = Resources.Load<CropDataSO>(ProjectPaths.RS_PREFIX_CROP_SO + q.targetItemID);
                    if (crop != null) cropSprite = crop.productIcon;
                }
                if (cropSprite != null)
                {
                    iconImg.sprite = cropSprite;
                    iconImg.color  = Color.white;
                }
                else
                {
                    // Fallback: hình tròn màu cam (giống QuestionIcon gốc)
                    iconImg.color = done ? new Color(0.25f, 0.75f, 0.25f) : new Color(0.95f, 0.5f, 0.1f);
                }

                // ── SLIDER (chiếm phần còn lại) — cùng kiểu QuestPanel ──
                var sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
                sliderGO.transform.SetParent(row.transform, false);
                var sliderRT = sliderGO.GetComponent<RectTransform>();
                sliderRT.anchorMin = new Vector2(0f, 0f);
                sliderRT.anchorMax = new Vector2(1f, 1f);
                sliderRT.offsetMin = new Vector2(28f, 0f);  // Để chỗ cho icon
                sliderRT.offsetMax = new Vector2(0f,  0f);

                // Background (nền tối giống QuestPanel)
                var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgGO.transform.SetParent(sliderGO.transform, false);
                var bgRT = bgGO.GetComponent<RectTransform>();
                bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
                bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
                bgGO.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.85f);

                // Fill Area (giống cấu trúc Unity Slider chuẩn)
                var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
                fillAreaGO.transform.SetParent(sliderGO.transform, false);
                var fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
                fillAreaRT.anchorMin = Vector2.zero; fillAreaRT.anchorMax = Vector2.one;
                fillAreaRT.offsetMin = Vector2.zero; fillAreaRT.offsetMax = Vector2.zero;

                // Fill (màu cam/xanh giống QuestPanel)
                var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGO.transform.SetParent(fillAreaGO.transform, false);
                var fillRT = fillGO.GetComponent<RectTransform>();
                fillRT.anchorMin = new Vector2(0f, 0f);
                fillRT.anchorMax = new Vector2(0f, 1f); // Unity Slider tự điều chỉnh anchorMax.x
                fillRT.sizeDelta = Vector2.zero;
                fillGO.GetComponent<Image>().color = fillColor;

                // Slider component
                var slider = sliderGO.GetComponent<Slider>();
                slider.interactable = false;
                slider.fillRect     = fillRT;
                slider.minValue     = 0;
                slider.maxValue     = Mathf.Max(1f, q.targetAmount);
                slider.value        = Mathf.Clamp(prog, 0, q.targetAmount);
                slider.direction    = Slider.Direction.LeftToRight;

                // ── TEXT "X / Y" ĐÈ GIỮA SLIDER (giống QuestPanel) ──
                var txtGO = new GameObject("ProgressText", typeof(RectTransform), typeof(Text));
                txtGO.transform.SetParent(sliderGO.transform, false);
                var txtRT = txtGO.GetComponent<RectTransform>();
                txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
                txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;

                var txt = txtGO.GetComponent<Text>();
                txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize  = 11;
                txt.fontStyle = done ? FontStyle.Bold : FontStyle.Normal;
                txt.color     = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.text      = done
                    ? $"✓ {(int)prog} / {q.targetAmount}"
                    : $"{(int)prog} / {q.targetAmount}";
            }
        }

        public void RefreshPlayerLevel()
        {
            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null) return;
            if (playerLevelText != null)
                playerLevelText.text = $"EXP: {DataManager.Instance.CurrentPlayer.EXP}";
        }
    }
}
