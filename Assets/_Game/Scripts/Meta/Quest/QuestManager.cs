using System.Collections.Generic;
using UnityEngine;
using FarmPuzzle.Core.Database;

namespace FarmPuzzle.Meta
{
    /// <summary>
    /// QuestManager v2 — DB-backed progress, score system, end-game reward.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        // ── Events ──────────────────────────────────────────────────────────
        /// <summary>Fire khi 1 quest progress thay đổi.</summary>
        public static System.Action OnProgressUpdated;

        /// <summary>Fire khi game kết thúc. Param: (starRating, goldEarned, totalScore)</summary>
        public static System.Action<int, int, int> OnGameEnded;

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Danh sách quest màn chơi")]
        public List<QuestDataSO> activeQuests = new List<QuestDataSO>();

        // ── Runtime state ────────────────────────────────────────────────────
        private Dictionary<string, int> questProgress = new Dictionary<string, int>();
        private int _currentScore = 0;
        private bool _sessionEnded = false;

        public int CurrentScore => _currentScore;

        // ─────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                if (activeQuests.Count == 0)
                    AutoLoadQuestsFromResources();

                if (activeQuests.Count > 0)
                    InitializeProgress();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            QuestTestUI.OnTestBlockCleared += UpdateProgress;
            DataManager.OnPlayerLoggedIn += OnPlayerLoggedIn;
        }

        private void OnDisable()
        {
            QuestTestUI.OnTestBlockCleared -= UpdateProgress;
            DataManager.OnPlayerLoggedIn -= OnPlayerLoggedIn;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Init & Level

        private void AutoLoadQuestsFromResources()
        {
            var loaded = Resources.LoadAll<QuestDataSO>("QuestData");
            if (loaded == null || loaded.Length == 0)
            {
                Debug.LogWarning("[QuestManager] Không tìm thấy SO nào trong Resources/QuestData!");
                return;
            }
            
            activeQuests.Clear();
            string pid = DataManager.Instance?.CurrentPlayer?.PlayerID;

            // Bốc tối đa 3 Quest chưa hoàn thành (chưa Banned)
            foreach (var q in loaded)
            {
                if (q == null || string.IsNullOrEmpty(q.questID)) continue;

                if (pid != null && DataManager.Instance.IsReady)
                {
                    var rec = DataManager.Instance.DB.Table<PlayerQuestModel>()
                               .FirstOrDefault(r => r.PlayerID == pid && r.QuestID == q.questID);
                    if (rec != null && rec.IsBanned) continue; // Đã làm xong
                }

                activeQuests.Add(q);
                if (activeQuests.Count >= 3) break; // Chỉ bốc 3 cái
            }

            Debug.Log($"<color=cyan>[QuestManager]</color> Auto-loaded {activeQuests.Count} quest(s) từ Resources/QuestData.");
        }

        private void InitializeProgress()
        {
            _sessionEnded = false;
            _currentScore = 0;
            questProgress.Clear();
            foreach (var q in activeQuests)
                if (q != null && !string.IsNullOrEmpty(q.questID))
                    questProgress[q.questID] = 0;

            RestoreProgressFromDB();
        }

        public void ReloadQuestsAfterCompletion()
        {
            AutoLoadQuestsFromResources();
            InitializeProgress();
        }

        public void StartLevel(List<QuestDataSO> levelQuests)
        {
            activeQuests.Clear();
            foreach (var q in levelQuests) activeQuests.Add(q);
            InitializeProgress();
            Debug.Log("<color=green>[QuestManager]</color> Phiên mới bắt đầu.");
        }

        private void OnPlayerLoggedIn()
        {
            ReloadQuestsAfterCompletion();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Progress
        
        public void ResetSessionQuests()
        {
            _sessionEnded = false;
            _currentScore = 0;

            if (activeQuests == null || activeQuests.Count == 0)
                AutoLoadQuestsFromResources();

            questProgress.Clear();
            foreach (var q in activeQuests)
            {
                if (q != null)
                {
                    questProgress[q.questID] = 0;
                    SaveProgressToDB(q.questID, 0, false); // Resets progress & unbans
                }
            }
            Debug.Log("<color=green>[QuestManager]</color> Session reset: Progress = 0.");
            OnProgressUpdated?.Invoke();
        }

        public void UpdateProgress(string itemID, int amount)
        {
            if (activeQuests.Count == 0 || _sessionEnded) return;

            bool changed = false;
            foreach (var q in activeQuests)
            {
                if (q == null || q.targetItemID != itemID) continue;
                if (!questProgress.ContainsKey(q.questID)) continue;

                int prev = questProgress[q.questID];
                questProgress[q.questID] = Mathf.Min(prev + amount, q.targetAmount);

                Debug.Log($"<color=orange>[QUEST - STAGE 3: Action Gameplay Tetris]</color> Diệt dọn Thành Công! Tích lũy vào Tiến Độ ({q.questID}): {questProgress[q.questID]}/{q.targetAmount}");
                
                SaveProgressToDB(q.questID, questProgress[q.questID], false);
                changed = true;
            }

            if (changed) 
            {
                Debug.Log("<color=yellow>[QUEST - STAGE 6: Trả về UI]</color> Đã báo Tín Hiệu Reload lại UI nhiệm vụ cho mượt!");
                OnProgressUpdated?.Invoke();
            }
        }

        public int GetQuestProgress(string questID)
        {
            questProgress.TryGetValue(questID, out int p);
            return p;
        }

        public bool IsQuestCompleted(string questID)
        {
            var q = activeQuests.Find(x => x != null && x.questID == questID);
            if (q == null) return false;
            return GetQuestProgress(questID) >= q.targetAmount;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Score

        /// <summary>Gọi mỗi khi Tetris xóa 1 hàng. Points = 10 cố định.</summary>
        public void AddScore(int points)
        {
            if (_sessionEnded) return;
            _currentScore += points;
            OnProgressUpdated?.Invoke(); // UI lắng nghe để refresh score display
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region End Game & Reward

        /// <summary>Số quest đã hoàn thành đủ target.</summary>
        public int GetCompletedCount()
        {
            int count = 0;
            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                if (questProgress.TryGetValue(q.questID, out int p) && p >= q.targetAmount)
                    count++;
            }
            return count;
        }

        /// <summary>Số sao: 0/1/2/3 dựa trên số quest hoàn thành.</summary>
        public int GetStarRating()
        {
            int done = GetCompletedCount();
            int total = activeQuests.Count;
            if (total == 0 || done == 0) return 0;
            if (done >= total)    return 3; // tất cả → 3 sao
            if (done >= total - 1) return 2; // thiếu 1 → 2 sao
            return 1; // ít nhất 1 → 1 sao
        }

        /// <summary>Hệ số nhân gold: 1×/1.5×/2× theo số sao.</summary>
        public float GetGoldMultiplier()
        {
            return GetStarRating() switch
            {
                3 => 2.0f,
                2 => 1.5f,
                1 => 1.0f,
                _ => 0f
            };
        }

        /// <summary>Tổng gold sẽ nhận được (chưa nhân multiplier = để UI preview).</summary>
        public int GetBaseGoldReward()
        {
            int total = 0;
            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                if (questProgress.TryGetValue(q.questID, out int p) && p >= q.targetAmount)
                    total += q.rewardGold;
            }
            return total;
        }

        public int GetFinalGoldReward() =>
            Mathf.RoundToInt(GetBaseGoldReward() * GetGoldMultiplier());

        /// <summary>
        /// Kết thúc phiên chơi: tính thưởng, ghi DB, fire OnGameEnded.
        /// Gọi sau khi người chơi xác nhận popup.
        /// </summary>
        public void TriggerEndGame()
        {
            if (_sessionEnded) return;
            _sessionEnded = true;

            int stars    = GetStarRating();
            int gold     = GetFinalGoldReward();
            int score    = _currentScore;

            // Ghi vàng vào DB
            if (gold > 0 && DataManager.Instance != null && DataManager.Instance.IsReady)
            {
                DataManager.Instance.AddGold(gold);
                Debug.Log($"<color=cyan>[QuestManager]</color> Kết thúc phiên: ⭐{stars} | +{gold}G | {score} điểm");
            }

            // Đánh dấu quest hoàn thành trong DB
            Debug.Log($"<color=magenta>[QUEST - STAGE 4/5: RAM -> Disk]</color> Ván chơi kết thúc. Chuẩn bị đóng băng dữ liệu Quest xuống SQLite bọc thép!");
            MarkCompletedQuestsInDB();

            // Tính điểm kinh nghiệm: 10 EXP mỗi sao + 10 EXP cho mỗi ván
            int expEarned = 10 + (stars * 10);
            if (DataManager.Instance != null && DataManager.Instance.CurrentPlayer != null)
            {
                var player = DataManager.Instance.CurrentPlayer;
                player.EXP += expEarned;
                // Thăng cấp logic sẽ được implement mở rộng sau, tạm thời cộng dồn
                DataManager.Instance.DB.Update(player);
                Debug.Log($"<color=green>[Level System]</color> Nhận {expEarned} EXP! Tổng: {player.EXP}");
            }

            // RESET VÀ RESTOCK
            activeQuests.Clear();
            questProgress.Clear();
            ReloadQuestsAfterCompletion();

            // Thông báo UI và kết thúc vòng đời
            OnGameEnded?.Invoke(stars, gold, score);
            OnProgressUpdated?.Invoke();
        }

        /// <summary>Game Over (bảng đầy) — kết thúc với điểm hiện tại.</summary>
        public void TriggerGameOver()
        {
            Debug.Log("[QuestManager] Game Over — tự động kết thúc phiên.");
            TriggerEndGame();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region DB Bridge

        private void RestoreProgressFromDB()
        {
            if (DataManager.Instance == null || !DataManager.Instance.IsReady) return;
            if (DataManager.Instance.CurrentPlayer == null) return;
            string pid = DataManager.Instance.CurrentPlayer.PlayerID;

            foreach (var q in activeQuests)
            {
                if (q == null || string.IsNullOrEmpty(q.questID)) continue;
                var rec = DataManager.Instance.DB
                    .Table<PlayerQuestModel>()
                    .FirstOrDefault(r => r.PlayerID == pid && r.QuestID == q.questID);
                if (rec != null)
                    questProgress[q.questID] = rec.QuestProgress;
            }
            Debug.Log($"<color=cyan>[QUEST - STAGE 1/2: Disk -> RAM]</color> Phục hồi Dữ Liệu chơi trước từ DB cho tài khoản {pid} thành công. Chuẩn bị pháo sáng OnProgressUpdated!");
            OnProgressUpdated?.Invoke();
        }

        private void SaveProgressToDB(string questID, int currentAmount, bool isBanned = false)
        {
            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null) return;
            string pId = DataManager.Instance.CurrentPlayer.PlayerID;

            var table = DataManager.Instance.DB.Table<FarmPuzzle.Core.Database.PlayerQuestModel>();
            var rec = table.FirstOrDefault(r => r.PlayerID == pId && r.QuestID == questID);
            
            if (rec != null)
            {
                rec.QuestProgress = currentAmount;
                rec.IsBanned = isBanned;
                DataManager.Instance.DB.Update(rec);
            }
            else
            {
                DataManager.Instance.DB.Insert(new FarmPuzzle.Core.Database.PlayerQuestModel {
                    PlayerID = pId,
                    QuestID = questID,
                    QuestProgress = currentAmount,
                    IsBanned = isBanned
                });
            }
            Debug.Log($"<color=magenta>[QUEST - STAGE 5: Lưu Rải Rác]</color> Đã nện tiến độ vào Database cho quest: {questID} ({currentAmount})");
        } 
        
        private void MarkCompletedQuestsInDB()
        {
            if (DataManager.Instance == null || !DataManager.Instance.IsReady) return;
            if (DataManager.Instance.CurrentPlayer == null) return;
            string pid = DataManager.Instance.CurrentPlayer.PlayerID;

            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                if (!IsQuestCompleted(q.questID)) continue;

                var rec = DataManager.Instance.DB
                    .Table<PlayerQuestModel>()
                    .FirstOrDefault(r => r.PlayerID == pid && r.QuestID == q.questID);
                if (rec != null) { rec.IsBanned = true; DataManager.Instance.DB.Update(rec); }
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Editor Helpers
        [ContextMenu("Test — Sell 5 Carrot")]
        private void Test_SellCarrot() => UpdateProgress("product_02", 5);
        [ContextMenu("Test — Sell 5 Potato")]
        private void Test_SellPotato() => UpdateProgress("product_01", 5);
        [ContextMenu("Test — End Game")]
        private void Test_EndGame() => TriggerEndGame();
        #endregion
    }
}
