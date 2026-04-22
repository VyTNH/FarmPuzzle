using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FarmPuzzle.Meta
{
    /// <summary>
    /// Script điều khiển từng dòng mục tiêu (Goal) trên giao diện.
    /// </summary>
    public class QuestEntryUI : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI goalTitleText;   // Tên vật phẩm (ví dụ: Apple)
        public TextMeshProUGUI progressText;    // Con số tiến độ (ví dụ: 0/10)
        public Image iconImage;                 // Hình ảnh hiển thị trên UI

        private QuestDataSO questData;

        private void OnEnable()
        {
            QuestEvents.OnQuestProgressUpdated += HandleProgressUpdated;
        }

        private void OnDisable()
        {
            QuestEvents.OnQuestProgressUpdated -= HandleProgressUpdated;
        }

        /// <summary>
        /// Được gọi bởi QuestWindowUI để thiết lập thông tin ban đầu.
        /// </summary>
        public void Setup(QuestDataSO data)
        {
            questData = data;
            RefreshDisplay();
        }

        private void HandleProgressUpdated(string questID, int currentAmount, int targetAmount)
        {
            // Chỉ cập nhật nếu đúng là Quest mà Entry này đang hiển thị
            if (questData != null && questData.questID == questID)
            {
                UpdateUI(currentAmount, targetAmount);
            }
        }

        /// <summary>
        /// Cập nhật nội dung hiển thị (Dùng cho lần đầu Setup)
        /// </summary>
        public void RefreshDisplay()
        {
            if (questData == null) return;

            if (goalTitleText != null) goalTitleText.text = questData.targetItemID;
            
            // Cập nhật Hình ảnh nếu có
            if (iconImage != null && questData.questIcon != null)
            {
                iconImage.sprite = questData.questIcon;
            }

            int current = QuestManager.Instance.GetQuestProgress(questData.questID);
            UpdateUI(current, questData.targetAmount);
        }

        private void UpdateUI(int current, int target)
        {
            if (progressText != null) 
            {
                progressText.text = $"{current}/{target}";
                
                // Đổi màu nếu hoàn thành
                progressText.color = (current >= target) ? Color.green : Color.white;
            }
        }
    }
}
