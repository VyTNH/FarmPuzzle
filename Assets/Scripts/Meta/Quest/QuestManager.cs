using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FarmPuzzle.Core.Database;

namespace FarmPuzzle.Meta
{
    /// <summary>
    /// QuestManager — Phiên bản kết hợp tối ưu.
    /// Kết hợp:
    ///   + DB Persistence + Star/Gold/EXP system (từ nhóm trưởng)
    ///   + Dictionary O(1) lookup + QuestEvents decoupling (từ member)
    ///   + Inventory-Aware Quest Selection (tính năng mới — fix bug quest không thể hoàn thành)
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        /// <summary>Fire khi danh sách quest thay đổi (VD: qua màn mới).</summary>
        public static System.Action OnQuestsChanged;

        /// <summary>Fire khi bất kỳ quest progress nào thay đổi.</summary>
        public static System.Action OnProgressUpdated;

        /// <summary>Fire khi game kết thúc. Param: (starRating, goldEarned, totalScore)</summary>
        public static System.Action<int, int, int> OnGameEnded;

        // ── Inspector ──────────────────────────────────────────────────────────────
        [Header("Danh sách quest màn chơi")]
        public List<QuestDataSO> activeQuests = new List<QuestDataSO>();

        [Header("Inventory-Aware Selection")]
        [Tooltip("Prefix của nông sản trong Inventory (mặc định: product_)")]
        public string productIDPrefix = "product_";

        [Tooltip("Số quest tối đa mỗi phiên")]
        public int maxQuestsPerSession = 3;

        // ── Runtime State ──────────────────────────────────────────────────────────
        /// <summary>Tiến độ theo questID → O(1) truy cập.</summary>
        private Dictionary<string, int> _questProgress  = new Dictionary<string, int>();

        /// <summary>Lookup ngược: itemID → danh sách quest cần item đó (tối ưu UpdateProgress).</summary>
        private Dictionary<string, List<QuestDataSO>> _questsByItemId = new Dictionary<string, List<QuestDataSO>>();

        private int  _currentScore  = 0;
        private bool _sessionEnded  = false;

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
            // Sử dụng DataManager event để reload quest khi login
            DataManager.OnPlayerLoggedIn += OnPlayerLoggedIn;
        }

        private void OnDisable()
        {
            DataManager.OnPlayerLoggedIn -= OnPlayerLoggedIn;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Init & Level

        /// <summary>
        /// Load quest từ Resources/QuestData.
        /// ✅ Inventory-Aware: đảm bảo ít nhất 1 quest mà người chơi có thể hoàn thành ngay.
        /// </summary>
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

            // ── Bước 1: Lọc bỏ quest đã hoàn thành (IsBanned) ──────────────
            var availablePool = new List<QuestDataSO>();
            foreach (var q in loaded)
            {
                if (q == null || string.IsNullOrEmpty(q.questID)) continue;

                if (pid != null && DataManager.Instance != null && DataManager.Instance.IsReady)
                {
                    var rec = DataManager.Instance.DB.Table<PlayerQuestModel>()
                              .FirstOrDefault(r => r.PlayerID == pid && r.QuestID == q.questID);
                    
                    // Chỉ bỏ qua nếu quest đã xong VÀ không được phép lặp lại
                    if (rec != null && rec.IsBanned && !q.isRepeatable) continue; 
                }

                availablePool.Add(q);
            }

            // ── Bước 2: Lấy danh sách product player đang có trong kho ──────
            var ownedProducts = GetAvailableProductIDs();
            Debug.Log($"<color=cyan>[QuestManager]</color> Kho hiện có {ownedProducts.Count} loại nông sản: [{string.Join(", ", ownedProducts)}]");

            // ── Bước 3: Phân loại quest ──────────────────────────────────────
            var achievableQuests    = new List<QuestDataSO>(); // Có thể hoàn thành ngay (có trong kho)
            var aspirationalQuests  = new List<QuestDataSO>(); // Cần trồng thêm

            foreach (var q in availablePool)
            {
                if (ownedProducts.Contains(q.targetItemID))
                    achievableQuests.Add(q);
                else
                    aspirationalQuests.Add(q);
            }

            Debug.Log($"<color=cyan>[QuestManager]</color> Phân loại — Achievable: {achievableQuests.Count} | Aspirational: {aspirationalQuests.Count}");

            // ── Bước 4: Thuật toán bốc Smart ─────────────────────────────────
            // Quy tắc: ≥1 quest achievable + bổ sung thêm đến đủ maxQuestsPerSession
            if (achievableQuests.Count > 0)
            {
                // 4a. Đảm bảo 1 quest achievable LUÔN CÓ MẶT
                ShuffleList(achievableQuests);
                activeQuests.Add(achievableQuests[0]);

                // 4b. Bổ sung thêm achievable còn lại (nếu có)
                for (int i = 1; i < achievableQuests.Count && activeQuests.Count < maxQuestsPerSession; i++)
                    activeQuests.Add(achievableQuests[i]);

                // 4c. Nếu vẫn chưa đủ → bổ sung aspirational
                ShuffleList(aspirationalQuests);
                foreach (var q in aspirationalQuests)
                {
                    if (activeQuests.Count >= maxQuestsPerSession) break;
                    activeQuests.Add(q);
                }

                Debug.Log($"<color=lime>[QuestManager]</color> ✅ Smart Select: {activeQuests.Count} quest (đảm bảo ít nhất 1 có thể hoàn thành ngay!)");
            }
            else
            {
                // 4d. Fallback: kho rỗng / chưa đăng nhập → bốc bình thường
                ShuffleList(availablePool);
                foreach (var q in availablePool)
                {
                    if (activeQuests.Count >= maxQuestsPerSession) break;
                    activeQuests.Add(q);
                }
                Debug.LogWarning($"[QuestManager] ⚠️ Không tìm thấy quest achievable. Fallback bốc thường ({activeQuests.Count} quest). Kiểm tra lại Inventory!");
            }

            Debug.Log($"<color=cyan>[QuestManager]</color> Đã bốc {activeQuests.Count} quest cho phiên chơi.");
        }

        /// <summary>
        /// Trả về danh sách itemID nông sản mà người chơi đang có (qty > 0) trong kho.
        /// Đọc từ session inventory của DataManager (RAM — nhanh, không cần truy vấn DB).
        /// </summary>
        private List<string> GetAvailableProductIDs()
        {
            var result = new List<string>();

            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null)
                return result;

            // Dùng session inventory (RAM) đã cache sẵn — O(n) trên số item trong kho
            var sessionInventory = DataManager.Instance.GetSessionInventory();
            foreach (var pair in sessionInventory)
            {
                if (pair.Key.StartsWith(productIDPrefix) && pair.Value > 0)
                    result.Add(pair.Key);
            }

            return result;
        }

        private void InitializeProgress()
        {
            _sessionEnded = false;
            _currentScore = 0;
            _questProgress.Clear();
            _questsByItemId.Clear();

            foreach (var q in activeQuests)
            {
                if (q == null || string.IsNullOrEmpty(q.questID)) continue;

                // 1. Khởi tiến độ = 0
                _questProgress[q.questID] = 0;

                // 2. Build reverse-lookup dictionary: itemID → [quest, quest, ...]
                if (!_questsByItemId.ContainsKey(q.targetItemID))
                    _questsByItemId[q.targetItemID] = new List<QuestDataSO>();
                _questsByItemId[q.targetItemID].Add(q);
            }

            RestoreProgressFromDB();
            OnQuestsChanged?.Invoke();
        }

        public void ReloadQuestsAfterCompletion()
        {
            AutoLoadQuestsFromResources();
            
            // Đảm bảo reset toàn bộ tiến độ về 0 khi bốc đơn hàng mới
            _questProgress.Clear();
            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                _questProgress[q.questID] = 0;
                SaveProgressToDB(q.questID, 0, false);
            }

            InitializeProgress();
        }

        public void StartLevel(List<QuestDataSO> levelQuests)
        {
            activeQuests.Clear();
            activeQuests.AddRange(levelQuests);

            // Reset tiến độ cho toàn bộ quest của level mới
            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                _questProgress[q.questID] = 0;
                SaveProgressToDB(q.questID, 0, false);
            }

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
            _sessionEnded  = false;
            _currentScore  = 0;

            if (activeQuests == null || activeQuests.Count == 0)
                AutoLoadQuestsFromResources();

            _questProgress.Clear();
            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                _questProgress[q.questID] = 0;
                SaveProgressToDB(q.questID, 0, false);
            }

            Debug.Log("<color=green>[QuestManager]</color> Session reset: Progress = 0.");
            OnProgressUpdated?.Invoke();
        }

        /// <summary>
        /// Cập nhật tiến độ quest khi người chơi thực hiện thao tác (VD: Tetris xóa hàng).
        /// ✅ O(1) lookup bằng _questsByItemId — không duyệt toàn bộ list.
        /// </summary>
        public void UpdateProgress(string itemID, int amount)
        {
            if (_sessionEnded) return;
            if (activeQuests.Count == 0) return;

            // Tìm ngay danh sách quest cần item này — O(1) thay vì O(n)
            if (!_questsByItemId.TryGetValue(itemID, out List<QuestDataSO> relevantQuests))
                return;

            bool hasChanged = false;
            foreach (var q in relevantQuests)
            {
                if (!_questProgress.ContainsKey(q.questID)) continue;

                int prev   = _questProgress[q.questID];
                int newVal = Mathf.Min(prev + amount, q.targetAmount);

                if (newVal == prev) continue; // Không thay đổi gì → bỏ qua

                _questProgress[q.questID] = newVal;
                Debug.Log($"<color=orange>[QUEST]</color> Tiến độ ({q.questID}): {newVal}/{q.targetAmount}");

                // Fire event chi tiết cho từng quest (QuestCardUI có thể dùng)
                QuestEvents.OnQuestProgressUpdated?.Invoke(q.questID, newVal, q.targetAmount);

                // Lưu từng thay đổi vào DB
                SaveProgressToDB(q.questID, newVal, false);
                hasChanged = true;
            }

            if (hasChanged)
            {
                // Fire event chung để QuestPanelManager / QuestCardUI refresh
                OnProgressUpdated?.Invoke();
                QuestEvents.OnGeneralProgressUpdated?.Invoke();
                CheckAllGoals();
            }
        }

        public int GetQuestProgress(string questID)
        {
            return _questProgress.TryGetValue(questID, out int p) ? p : 0;
        }

        public bool IsQuestCompleted(string questID)
        {
            var q = activeQuests.Find(x => x != null && x.questID == questID);
            if (q == null) return false;
            return GetQuestProgress(questID) >= q.targetAmount;
        }

        /// <summary>Tự kiểm tra xem tất cả quest đã hoàn thành chưa.</summary>
        private void CheckAllGoals()
        {
            if (activeQuests.Count == 0) return;

            int totalReward = 0;
            foreach (var q in activeQuests)
            {
                if (q == null || _questProgress[q.questID] < q.targetAmount) return; // Còn quest chưa xong
                totalReward += q.rewardGold;
            }

            // Tất cả quest hoàn thành!
            Debug.Log($"<color=cyan>[WIN]</color> Hoàn thành tất cả quest! Thưởng: {totalReward} Vàng.");
            QuestEvents.OnLevelWin?.Invoke(totalReward);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Score

        /// <summary>Gọi mỗi khi Tetris xóa 1 hàng.</summary>
        public void AddScore(int points)
        {
            if (_sessionEnded) return;
            _currentScore += points;
            OnProgressUpdated?.Invoke();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region End Game & Reward

        /// <summary>Số quest đã hoàn thành (đạt targetAmount).</summary>
        public int GetCompletedCount()
        {
            int count = 0;
            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                if (_questProgress.TryGetValue(q.questID, out int p) && p >= q.targetAmount)
                    count++;
            }
            return count;
        }

        /// <summary>Xếp hạng sao: 0-3 dựa trên số quest hoàn thành.</summary>
        public int GetStarRating()
        {
            int done  = GetCompletedCount();
            int total = activeQuests.Count;
            if (total == 0 || done == 0) return 0;
            if (done >= total)     return 3;
            if (done >= total - 1) return 2;
            return 1;
        }

        /// <summary>Hệ số nhân gold: 1× / 1.5× / 2× theo số sao.</summary>
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

        public int GetBaseGoldReward()
        {
            int total = 0;
            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                if (_questProgress.TryGetValue(q.questID, out int p) && p >= q.targetAmount)
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

            int stars = GetStarRating();
            int gold  = GetFinalGoldReward();
            int score = _currentScore;

            // Ghi vàng vào DB
            if (gold > 0 && DataManager.Instance != null && DataManager.Instance.IsReady)
            {
                DataManager.Instance.AddGold(gold);
                Debug.Log($"<color=cyan>[QuestManager]</color> Kết thúc phiên: ⭐{stars} | +{gold}G | {score} điểm");
            }

            // Đánh dấu quest đã hoàn thành trong DB (IsBanned = true)
            MarkCompletedQuestsInDB();

            // Cộng EXP: 10 base + 10 mỗi sao
            int expEarned = 10 + (stars * 10);
            if (DataManager.Instance?.CurrentPlayer != null)
            {
                var player = DataManager.Instance.CurrentPlayer;
                player.EXP += expEarned;
                DataManager.Instance.DB.Update(player);
                Debug.Log($"<color=green>[Level System]</color> Nhận {expEarned} EXP! Tổng: {player.EXP}");
            }

            // Reset và bốc quest mới (Inventory-Aware)
            activeQuests.Clear();
            _questProgress.Clear();
            _questsByItemId.Clear();
            ReloadQuestsAfterCompletion();

            // Thông báo UI
            OnGameEnded?.Invoke(stars, gold, score);
            OnProgressUpdated?.Invoke();
        }

        /// <summary>Game Over (bảng đầy) — tự động kết thúc phiên.</summary>
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
                var rec = DataManager.Instance.DB.Table<PlayerQuestModel>()
                    .FirstOrDefault(r => r.PlayerID == pid && r.QuestID == q.questID);
                if (rec != null)
                    _questProgress[q.questID] = rec.QuestProgress;
            }

            Debug.Log($"<color=cyan>[QUEST - Disk→RAM]</color> Khôi phục tiến độ từ DB cho {pid} thành công.");
            OnProgressUpdated?.Invoke();
        }

        private void SaveProgressToDB(string questID, int currentAmount, bool isBanned = false)
        {
            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null) return;
            string pId = DataManager.Instance.CurrentPlayer.PlayerID;

            var table = DataManager.Instance.DB.Table<PlayerQuestModel>();
            var rec   = table.FirstOrDefault(r => r.PlayerID == pId && r.QuestID == questID);

            if (rec != null)
            {
                rec.QuestProgress = currentAmount;
                rec.IsBanned      = isBanned;
                DataManager.Instance.DB.Update(rec);
            }
            else
            {
                DataManager.Instance.DB.Insert(new PlayerQuestModel
                {
                    PlayerID      = pId,
                    QuestID       = questID,
                    QuestProgress = currentAmount,
                    IsBanned      = isBanned
                });
            }
        }

        private void MarkCompletedQuestsInDB()
        {
            if (DataManager.Instance == null || !DataManager.Instance.IsReady) return;
            if (DataManager.Instance.CurrentPlayer == null) return;
            string pid = DataManager.Instance.CurrentPlayer.PlayerID;

            foreach (var q in activeQuests)
            {
                if (q == null || !IsQuestCompleted(q.questID)) continue;

                var rec = DataManager.Instance.DB.Table<PlayerQuestModel>()
                    .FirstOrDefault(r => r.PlayerID == pid && r.QuestID == q.questID);
                
                if (rec != null) 
                { 
                    // Chỉ ban nếu không được phép lặp lại
                    rec.IsBanned = !q.isRepeatable; 
                    DataManager.Instance.DB.Update(rec); 
                }
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Helpers

        /// <summary>Xáo trộn list để bốc quest ngẫu nhiên mỗi phiên.</summary>
        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Editor Helpers & Testing

        [ContextMenu("Test — Sell 5 Carrot (product_02)")]
        private void Test_SellCarrot() => UpdateProgress("product_02", 5);

        [ContextMenu("Test — Sell 5 Potato (product_01)")]
        private void Test_SellPotato() => UpdateProgress("product_01", 5);

        [ContextMenu("Test — Log Available Products")]
        private void Test_LogProducts()
        {
            var products = GetAvailableProductIDs();
            Debug.Log($"[QuestManagerUpdate] Kho hiện có: [{string.Join(", ", products)}]");
        }

        [ContextMenu("Test — Reload Quests (Inventory-Aware)")]
        private void Test_ReloadQuests() => ReloadQuestsAfterCompletion();

        [ContextMenu("Test — End Game")]
        private void Test_EndGame() => TriggerEndGame();

        #endregion
    }
}
