using UnityEngine;

namespace FarmPuzzle.Meta
{
    [CreateAssetMenu(fileName = "NewQuestData", menuName = "FarmPuzzle/QuestData")]
    public class QuestDataSO : ScriptableObject
    {
        [Header("Thông tin mục tiêu")]
        public string questID;
        public string targetItemID; // Nhập "Apple", "Orange", "Radish"...
        public Sprite questIcon;    // Hình ảnh hiển thị trên UI
        [TextArea] public string description;

        [Header("Yêu cầu số lượng")]
        public int targetAmount;

        [Header("Phần thưởng")]
        public int rewardGold;
    }
}
