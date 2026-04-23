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
        public string targetItemID; // Nhập "Apple", "Orange", "Radish"...
        [TextArea] public string description;

        [Header("Yêu cầu số lượng")]
        public int targetAmount;

        [Header("Phần thưởng")]
        public int rewardGold;
    }
}
