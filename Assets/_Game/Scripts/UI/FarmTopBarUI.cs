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

            var q    = qm.activeQuests[0];
            float prog = qm.GetQuestProgress(q.questID);

            // Hiển thị icon của item
            if (questItemIcon != null)
            {
                Sprite icon = null;
                if (q.targetItemID.StartsWith(ProjectPaths.PREFIX_PRODUCT))
                {
                    var crop = Resources.Load<CropDataSO>(ProjectPaths.RS_PREFIX_CROP_SO + q.targetItemID);
                    if (crop != null) icon = crop.productIcon;
                }
                questItemIcon.sprite  = icon;
                questItemIcon.enabled = icon != null;
            }

            if (questTitleText != null)
                questTitleText.text = "📜 Quest";
            if (questProgressText != null) questProgressText.text = $"{prog} / {q.targetAmount}";

            if (questProgressSlider != null)
            {
                questProgressSlider.maxValue = q.targetAmount;
                questProgressSlider.value    = prog;
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
