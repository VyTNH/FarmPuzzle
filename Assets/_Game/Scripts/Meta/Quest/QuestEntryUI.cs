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
        public Image iconImage;                 // (Tùy chọn) Hình ảnh vật phẩm

        private QuestDataSO questData;

        /// <summary>
        /// Được gọi bởi QuestWindowUI để thiết lập thông tin ban đầu.
        /// </summary>
        public void Setup(QuestDataSO data)
        {
            questData = data;
            RefreshDisplay();
        }

        /// <summary>
        /// Cập nhật lại con số hiển thị dựa trên dữ liệu từ QuestManager.
        /// </summary>
        public void RefreshDisplay()
        {
            if (questData == null) return;

            // 1. Hiển thị tiêu đề
            if (goalTitleText != null) goalTitleText.text = questData.targetItemID;

            // 2. Lấy tiến độ hiện tại từ Manager và hiển thị dạng 0/10
            int currentAmount = QuestManager.Instance.GetQuestProgress(questData.questID);
            if (progressText != null) 
            {
                progressText.text = $"{currentAmount}/{questData.targetAmount}";
            }

            // (Gợi ý thêm) Bạn có thể đổi màu chữ nếu đã hoàn thành
            if (currentAmount >= questData.targetAmount && progressText != null)
            {
                progressText.color = Color.green;
            }
        }
    }
}
