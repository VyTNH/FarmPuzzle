using UnityEngine;

namespace FarmPuzzle.Meta
{
    public enum QuestRequestType
    {
        Plant,
        Harvest,
        Sell
    }

    [CreateAssetMenu(fileName = "NewQuestData", menuName = "FarmPuzzle/QuestData")]
    public class QuestDataSO : ScriptableObject
    {
        [Header("Thông tin mục tiêu")]
        public string questID;
        public string targetItemID; // ID của nông sản cần thu thập (ví dụ: product_01, product_02)
        [TextArea] public string description;

        [Header("Yêu cầu số lượng")]
        public int targetAmount;

        [Header("Phần thưởng")]
        public int rewardGold;

        [Header("Cấu hình lặp lại")]
        [Tooltip("Nếu TRUE, quest có thể được bốc lại sau khi đã hoàn thành. Nếu FALSE, quest sẽ bị ẩn vĩnh viễn sau khi làm xong.")]
        public bool isRepeatable = false;
    }
}
