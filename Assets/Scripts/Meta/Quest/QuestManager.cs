using System.Collections.Generic;
using UnityEngine;

namespace FarmPuzzle.Meta
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }
        public static System.Action OnProgressUpdated; // Báo cho UI QuestWindowUI cập nhật số liệu

        [Header("Danh sách mục tiêu màn chơi")]
        public List<QuestDataSO> activeQuests = new List<QuestDataSO>();

        private Dictionary<string, int> questProgress = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Tự động khởi tạo nếu đã kéo sẵn trong Inspector
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
            // Nghe tín hiệu giả lập từ QuestTestUI
            QuestTestUI.OnTestBlockCleared += UpdateProgress;
        }

        private void OnDisable()
        {
            QuestTestUI.OnTestBlockCleared -= UpdateProgress;
        }

        /// <summary>
        /// Khởi tạo lại bảng Tiến độ
        /// </summary>
        private void InitializeProgress()
        {
            questProgress.Clear();
            foreach (var q in activeQuests)
            {
                if (q != null && !string.IsNullOrEmpty(q.questID))
                {
                    questProgress[q.questID] = 0;
                }
            }
        }

        public void StartLevel(List<QuestDataSO> levelQuests)
        {
            activeQuests.Clear();
            foreach (var q in levelQuests) activeQuests.Add(q);

            InitializeProgress();
            Debug.Log("<color=green>[Level]</color> Khởi động màn chơi mới.");
        }

        public void UpdateProgress(string itemID, int amount)
        {
            bool hasChanged = false;
            foreach (var quest in activeQuests)
            {
                if (quest.targetItemID == itemID)
                {
                    int currentVal = questProgress[quest.questID];
                    questProgress[quest.questID] = Mathf.Min(currentVal + amount, quest.targetAmount);
                    
                    Debug.Log($"<color=yellow>[Goal]</color> {itemID}: {questProgress[quest.questID]}/{quest.targetAmount}");
                    hasChanged = true;
                }
            }

            if (hasChanged)
            {
                OnProgressUpdated?.Invoke();
                CheckAllGoals();
            }
        }

        public int GetQuestProgress(string questID)
        {
            if (questProgress.TryGetValue(questID, out int progress))
            {
                return progress;
            }
            return 0;
        }

        private void CheckAllGoals()
        {
            bool allDone = true;
            foreach (var quest in activeQuests)
            {
                if (questProgress[quest.questID] < quest.targetAmount)
                {
                    allDone = false;
                    break;
                }
            }

            if (allDone && activeQuests.Count > 0)
            {
                int totalReward = 0;
                foreach (var q in activeQuests) totalReward += q.rewardGold;

                Debug.Log($"<color=cyan>[WIN]</color> Hoàn thành màn chơi! Thưởng: {totalReward} Vàng.");
                
                // Gọi sự kiện thắng thông qua QuestTestUI
                QuestTestUI.OnTestLevelWin?.Invoke(totalReward);
                
                activeQuests.Clear();
            }
        }

        #region Context Menu Testing
        [ContextMenu("Test - Add Apple")]
        private void Test_AddApple()
        {
            QuestTestUI.OnTestBlockCleared?.Invoke("Apple", 1);
        }
        #endregion
    }
}
