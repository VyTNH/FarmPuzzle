using System.Collections.Generic;
using UnityEngine;

namespace FarmPuzzle.Meta
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Danh sách mục tiêu màn chơi")]
        public List<QuestDataSO> activeQuests = new List<QuestDataSO>();

        private Dictionary<string, int> questProgress = new Dictionary<string, int>();
        private Dictionary<string, List<QuestDataSO>> _questsByItemId = new Dictionary<string, List<QuestDataSO>>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                if (activeQuests.Count > 0)
                {
                    InitializeProgress();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            // Nghe tín hiệu thu thập vật phẩm từ hệ thống Gameplay
            QuestEvents.OnItemCollected += UpdateProgress;
        }

        private void OnDisable()
        {
            QuestEvents.OnItemCollected -= UpdateProgress;
        }

        private void InitializeProgress()
        {
            questProgress.Clear();
            _questsByItemId.Clear();

            foreach (var q in activeQuests)
            {
                if (q == null || string.IsNullOrEmpty(q.questID)) continue;

                // 1. Khởi tạo tiến độ về 0
                questProgress[q.questID] = 0;

                // 2. Mapping từ ItemID sang Quest để tìm kiếm nhanh
                if (!_questsByItemId.ContainsKey(q.targetItemID))
                {
                    _questsByItemId[q.targetItemID] = new List<QuestDataSO>();
                }
                _questsByItemId[q.targetItemID].Add(q);
            }
        }

        public void StartLevel(List<QuestDataSO> levelQuests)
        {
            activeQuests.Clear();
            activeQuests.AddRange(levelQuests);

            InitializeProgress();
            Debug.Log("<color=green>[Level]</color> Khởi động màn chơi mới.");
        }

        public void UpdateProgress(string itemID, int amount)
        {
            // TỐI ƯU: Chỉ tìm trong những Quest cần Item này thay vì lặp toàn bộ list
            if (!_questsByItemId.TryGetValue(itemID, out List<QuestDataSO> relevantQuests))
            {
                return;
            }

            bool hasChanged = false;
            foreach (var quest in relevantQuests)
            {
                int currentVal = questProgress[quest.questID];
                int newVal = Mathf.Min(currentVal + amount, quest.targetAmount);
                
                if (newVal != currentVal)
                {
                    questProgress[quest.questID] = newVal;
                    // Báo hiệu cập nhật cho từng Quest cụ thể
                    QuestEvents.OnQuestProgressUpdated?.Invoke(quest.questID, newVal, quest.targetAmount);
                    
                    Debug.Log($"<color=yellow>[Goal]</color> {itemID}: {newVal}/{quest.targetAmount}");
                    hasChanged = true;
                }
            }

            if (hasChanged)
            {
                // Báo hiệu UI chung cập nhật nếu cần
                QuestEvents.OnGeneralProgressUpdated?.Invoke();
                CheckAllGoals();
            }
        }

        public int GetQuestProgress(string questID)
        {
            return questProgress.TryGetValue(questID, out int progress) ? progress : 0;
        }

        private void CheckAllGoals()
        {
            if (activeQuests.Count == 0) return;

            bool allDone = true;
            int totalReward = 0;

            foreach (var quest in activeQuests)
            {
                if (questProgress[quest.questID] < quest.targetAmount)
                {
                    allDone = false;
                    break;
                }
                totalReward += quest.rewardGold;
            }

            if (allDone)
            {
                Debug.Log($"<color=cyan>[WIN]</color> Hoàn thành màn chơi! Thưởng: {totalReward} Vàng.");
                QuestEvents.OnLevelWin?.Invoke(totalReward);
            }
        }

        #region Context Menu Testing
        [ContextMenu("Test - Add Apple")]
        private void Test_AddApple()
        {
             QuestEvents.OnItemCollected?.Invoke("Apple", 1);
        }
        #endregion
    }
}
