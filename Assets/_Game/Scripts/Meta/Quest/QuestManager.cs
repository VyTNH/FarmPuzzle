using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FarmPuzzle.Core.Database;

namespace FarmPuzzle.Meta
{
    /// <summary>
    /// QuestManager - Bộ điều khiển hệ thống nhiệm vụ.
    /// Tính năng chính:
    ///   + Lưu trữ tiến độ quest xuống SQLite (DB Persistence)
    ///   + Hệ thống xếp hạng sao và phần thưởng Gold/EXP
    ///   + Tra cứu O(1) bằng Dictionary (_questsByItemId)
    ///   + Bốc quest thông minh theo kho đồ (Inventory-Aware Selection)
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        // --- Sự kiện công khai ---
        /// <summary>Kích hoạt khi danh sách quest thay đổi (ví dụ: sang màn mới).</summary>
        public static System.Action OnQuestsChanged;

        /// <summary>Kích hoạt khi bất kỳ tiến độ quest nào thay đổi.</summary>
        public static System.Action OnProgressUpdated;

        /// <summary>Kích hoạt khi game kết thúc. Tham số: (xếpHạngSao, vàngNhận, tổngĐiểm)</summary>
        public static System.Action<int, int, int> OnGameEnded;

        /// <summary>Kích hoạt khi tiến độ của một quest cụ thể thay đổi. Tham số: (questID, tiếnĐộHiệnTại, mụcTiêu)</summary>
        public static System.Action<string, int, int> OnQuestProgressUpdated;

        /// <summary>Kích hoạt khi tất cả quest đều hoàn thành. Tham số: (tổngVàngThưởng)</summary>
        public static System.Action<int> OnLevelWin;

        // --- Inspector ---
        [Header("Danh sách quest màn chơi")]
        public List<QuestDataSO> activeQuests = new List<QuestDataSO>();

        [Header("Cấu hình bốc Quest")]
        [Tooltip("Tiền tố ID của nông sản trong Inventory (mặc định: product_)")]
        public string productIDPrefix = "product_";

        [Tooltip("Số lượng quest tối đa mỗi phiên chơi")]
        public int maxQuestsPerSession = 3;

        // --- Trạng thái runtime ---
        /// <summary>Lưu tiến độ theo questID. Truy xuất O(1).</summary>
        private Dictionary<string, int> _questProgress = new Dictionary<string, int>();

        /// <summary>Tra cứu ngược: itemID → danh sách quest cần item đó. Tối ưu hàm UpdateProgress.</summary>
        private Dictionary<string, List<QuestDataSO>> _questsByItemId = new Dictionary<string, List<QuestDataSO>>();

        private int  _currentScore = 0;
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

                // Nếu chưa có quest nào được gán trực tiếp trên Inspector → tự động bốc từ Resources
                if (activeQuests.Count == 0)
                    AutoLoadQuestsFromResources();

                if (activeQuests.Count > 0)
                    InitializeProgress();
            }
            else
            {
                // Đã có Instance rồi → hủy bản trùng
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            // Lắng nghe sự kiện Login để tải lại quest phù hợp với người chơi mới
            DataManager.OnPlayerLoggedIn += OnPlayerLoggedIn;
        }

        private void OnDisable()
        {
            DataManager.OnPlayerLoggedIn -= OnPlayerLoggedIn;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Khởi tạo và Quản lý Level

        /// <summary>
        /// Tự động bốc quest từ thư mục Resources/QuestData.
        /// Ưu tiên bốc quest mà người chơi CÓ THỂ HOÀN THÀNH NGAY (có nông sản trong kho).
        /// </summary>
        private void AutoLoadQuestsFromResources()
        {
            var loaded = Resources.LoadAll<QuestDataSO>("QuestData");
            if (loaded == null || loaded.Length == 0)
            {
                Debug.LogWarning("[QuestManager] Không tìm thấy ScriptableObject nào trong Resources/QuestData!");
                return;
            }

            activeQuests.Clear();
            string pid = DataManager.Instance?.CurrentPlayer?.PlayerID;

            // Bước 1: Loại bỏ các quest đã làm xong và không được lặp lại (IsBanned = true, isRepeatable = false)
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

            // Bước 2: Lấy danh sách nông sản người chơi đang có trong kho (từ RAM Cache)
            var ownedProducts = GetAvailableProductIDs();
            Debug.Log($"<color=cyan>[QuestManager]</color> Kho hiện có {ownedProducts.Count} loại nông sản: [{string.Join(", ", ownedProducts)}]");

            // Bước 3: Phân loại quest thành 2 nhóm
            var achievableQuests   = new List<QuestDataSO>(); // Có thể hoàn thành ngay (có đủ nông sản)
            var aspirationalQuests = new List<QuestDataSO>(); // Cần trồng thêm mới làm được

            foreach (var q in availablePool)
            {
                if (ownedProducts.Contains(q.targetItemID))
                    achievableQuests.Add(q);
                else
                    aspirationalQuests.Add(q);
            }

            Debug.Log($"<color=cyan>[QuestManager]</color> Phân loại — Làm được ngay: {achievableQuests.Count} | Cần trồng thêm: {aspirationalQuests.Count}");

            // Bước 4: Thuật toán bốc thông minh
            // Quy tắc: Đảm bảo ít nhất 1 quest có thể làm được ngay, sau đó bổ sung thêm
            if (achievableQuests.Count > 0)
            {
                // 4a. Đảm bảo luôn có 1 quest mà người chơi làm được ngay
                ShuffleList(achievableQuests);
                activeQuests.Add(achievableQuests[0]);

                // 4b. Bổ sung thêm các quest làm được ngay còn lại (nếu chưa đủ số lượng)
                for (int i = 1; i < achievableQuests.Count && activeQuests.Count < maxQuestsPerSession; i++)
                    activeQuests.Add(achievableQuests[i]);

                // 4c. Nếu vẫn chưa đủ → bổ sung thêm quest cần trồng thêm
                ShuffleList(aspirationalQuests);
                foreach (var q in aspirationalQuests)
                {
                    if (activeQuests.Count >= maxQuestsPerSession) break;
                    activeQuests.Add(q);
                }

                Debug.Log($"<color=lime>[QuestManager]</color> ✅ Bốc thông minh: {activeQuests.Count} quest (đảm bảo ít nhất 1 làm được ngay!)");
            }
            else
            {
                // 4d. Dự phòng: kho rỗng hoặc chưa đăng nhập → bốc ngẫu nhiên bình thường
                ShuffleList(availablePool);
                foreach (var q in availablePool)
                {
                    if (activeQuests.Count >= maxQuestsPerSession) break;
                    activeQuests.Add(q);
                }
                Debug.LogWarning($"[QuestManager] ⚠️ Không tìm thấy quest có thể làm ngay. Bốc ngẫu nhiên ({activeQuests.Count} quest). Hãy kiểm tra Inventory!");
            }

            Debug.Log($"<color=cyan>[QuestManager]</color> Đã bốc {activeQuests.Count} quest cho phiên chơi.");
        }

        /// <summary>
        /// Trả về danh sách ID nông sản mà người chơi đang có (số lượng > 0) trong kho.
        /// Đọc từ RAM Cache của DataManager — nhanh, không cần truy vấn SQLite.
        /// </summary>
        private List<string> GetAvailableProductIDs()
        {
            var result = new List<string>();

            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null)
                return result;

            var sessionInventory = DataManager.Instance.GetSessionInventory();
            foreach (var pair in sessionInventory)
            {
                if (pair.Key.StartsWith(productIDPrefix) && pair.Value > 0)
                    result.Add(pair.Key);
            }

            return result;
        }

        /// <summary>
        /// Khởi tạo tiến độ cho phiên chơi mới và phục hồi từ Database nếu có.
        /// </summary>
        private void InitializeProgress()
        {
            _sessionEnded = false;
            _currentScore = 0;
            _questProgress.Clear();
            _questsByItemId.Clear();

            foreach (var q in activeQuests)
            {
                if (q == null || string.IsNullOrEmpty(q.questID)) continue;

                // Khởi tạo tiến độ về 0
                _questProgress[q.questID] = 0;

                // Xây dựng bảng tra cứu ngược: itemID → [quest1, quest2, ...]
                // Giúp UpdateProgress chạy O(1) thay vì phải duyệt toàn bộ danh sách
                if (!_questsByItemId.ContainsKey(q.targetItemID))
                    _questsByItemId[q.targetItemID] = new List<QuestDataSO>();
                _questsByItemId[q.targetItemID].Add(q);
            }

            RestoreProgressFromDB();
            OnQuestsChanged?.Invoke();
        }

        /// <summary>
        /// Bốc lại quest mới sau khi phiên chơi kết thúc.
        /// </summary>
        public void ReloadQuestsAfterCompletion()
        {
            AutoLoadQuestsFromResources();

            // Reset toàn bộ tiến độ về 0 cho bộ quest mới
            _questProgress.Clear();
            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                _questProgress[q.questID] = 0;
                SaveProgressToDB(q.questID, 0, false);
            }

            InitializeProgress();
        }

        /// <summary>
        /// Bắt đầu một level với danh sách quest cụ thể (được gọi từ bên ngoài).
        /// </summary>
        public void StartLevel(List<QuestDataSO> levelQuests)
        {
            activeQuests.Clear();
            activeQuests.AddRange(levelQuests);

            // Reset tiến độ cho tất cả quest của level mới
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
            // Khi người chơi đăng nhập, bốc lại quest phù hợp với tài khoản đó
            ReloadQuestsAfterCompletion();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Tiến độ Quest

        /// <summary>
        /// Reset toàn bộ tiến độ quest về 0, dùng khi người chơi chơi lại từ đầu.
        /// </summary>
        public void ResetSessionQuests()
        {
            _sessionEnded = false;
            _currentScore = 0;

            if (activeQuests == null || activeQuests.Count == 0)
                AutoLoadQuestsFromResources();

            _questProgress.Clear();
            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                _questProgress[q.questID] = 0;
                SaveProgressToDB(q.questID, 0, false);
            }

            Debug.Log("<color=green>[QuestManager]</color> Đã reset tiến độ toàn bộ quest về 0.");
            OnProgressUpdated?.Invoke();
        }

        /// <summary>
        /// Cập nhật tiến độ quest khi người chơi thực hiện hành động (ví dụ: Tetris xóa hàng).
        /// Sử dụng tra cứu O(1) bằng _questsByItemId — không duyệt toàn bộ danh sách.
        /// </summary>
        public void UpdateProgress(string itemID, int amount)
        {
            if (_sessionEnded) return;
            if (activeQuests.Count == 0) return;

            // Tra cứu O(1): tìm ngay các quest cần item này
            if (!_questsByItemId.TryGetValue(itemID, out List<QuestDataSO> relevantQuests))
                return; // Không có quest nào cần item này → bỏ qua

            bool hasChanged = false;
            foreach (var q in relevantQuests)
            {
                if (!_questProgress.ContainsKey(q.questID)) continue;

                int prev   = _questProgress[q.questID];
                int newVal = Mathf.Min(prev + amount, q.targetAmount);

                if (newVal == prev) continue; // Tiến độ không thay đổi → bỏ qua

                _questProgress[q.questID] = newVal;
                Debug.Log($"<color=orange>[QUEST]</color> Tiến độ ({q.questID}): {newVal}/{q.targetAmount}");

                // Kích hoạt sự kiện chi tiết cho từng quest (QuestCardUI lắng nghe để cập nhật UI)
                OnQuestProgressUpdated?.Invoke(q.questID, newVal, q.targetAmount);

                // Lưu tiến độ xuống SQLite
                SaveProgressToDB(q.questID, newVal, false);
                hasChanged = true;
            }

            if (hasChanged)
            {
                // Kích hoạt sự kiện chung để QuestPanelManager refresh toàn bộ UI
                OnProgressUpdated?.Invoke();
                CheckAllGoals();
            }
        }

        /// <summary>Lấy tiến độ hiện tại của một quest theo questID.</summary>
        public int GetQuestProgress(string questID)
        {
            return _questProgress.TryGetValue(questID, out int p) ? p : 0;
        }

        /// <summary>Kiểm tra xem một quest đã đạt mục tiêu hay chưa.</summary>
        public bool IsQuestCompleted(string questID)
        {
            var q = activeQuests.Find(x => x != null && x.questID == questID);
            if (q == null) return false;
            return GetQuestProgress(questID) >= q.targetAmount;
        }

        /// <summary>
        /// Kiểm tra xem tất cả quest trong phiên đã hoàn thành chưa.
        /// Nếu có → kích hoạt sự kiện OnLevelWin để thông báo cho UI.
        /// </summary>
        private void CheckAllGoals()
        {
            if (activeQuests.Count == 0) return;

            int totalReward = 0;
            foreach (var q in activeQuests)
            {
                // Còn quest chưa xong → dừng kiểm tra
                if (q == null || _questProgress[q.questID] < q.targetAmount) return;
                totalReward += q.rewardGold;
            }

            // Tất cả quest đã hoàn thành!
            Debug.Log($"<color=cyan>[WIN]</color> Hoàn thành tất cả quest! Phần thưởng: {totalReward} Vàng.");
            OnLevelWin?.Invoke(totalReward);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Điểm số

        /// <summary>Cộng điểm mỗi khi Tetris xóa được 1 hàng.</summary>
        public void AddScore(int points)
        {
            if (_sessionEnded) return;
            _currentScore += points;
            OnProgressUpdated?.Invoke();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Kết thúc Game và Phần thưởng

        /// <summary>Trả về số quest đã đạt mục tiêu trong phiên hiện tại.</summary>
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

        /// <summary>Tính xếp hạng sao: 0-3 dựa trên số lượng quest hoàn thành.</summary>
        public int GetStarRating()
        {
            int done  = GetCompletedCount();
            int total = activeQuests.Count;
            if (total == 0 || done == 0) return 0;
            if (done >= total)     return 3; // Hoàn thành tất cả → 3 sao
            if (done >= total - 1) return 2; // Thiếu 1 → 2 sao
            return 1;                         // Còn lại → 1 sao
        }

        /// <summary>Tính hệ số nhân vàng: 1x / 1.5x / 2x tương ứng với 1/2/3 sao.</summary>
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

        /// <summary>Tính tổng vàng cơ bản từ các quest đã hoàn thành (chưa nhân hệ số).</summary>
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

        /// <summary>Tổng vàng thực nhận sau khi nhân hệ số sao.</summary>
        public int GetFinalGoldReward() =>
            Mathf.RoundToInt(GetBaseGoldReward() * GetGoldMultiplier());

        /// <summary>
        /// Kết thúc phiên chơi: tính phần thưởng, ghi vào Database, kích hoạt sự kiện OnGameEnded.
        /// Gọi hàm này sau khi người chơi xác nhận popup kết thúc.
        /// </summary>
        public void TriggerEndGame()
        {
            if (_sessionEnded) return;
            _sessionEnded = true;

            int stars = GetStarRating();
            int gold  = GetFinalGoldReward();
            int score = _currentScore;

            // Ghi vàng vào Database
            if (gold > 0 && DataManager.Instance != null && DataManager.Instance.IsReady)
            {
                DataManager.Instance.AddGold(gold);
                Debug.Log($"<color=cyan>[QuestManager]</color> Kết thúc phiên: ⭐{stars} | +{gold}G | {score} điểm");
            }

            // Đánh dấu các quest đã hoàn thành (IsBanned = true nếu không lặp lại được)
            MarkCompletedQuestsInDB();

            // Cộng EXP: 10 điểm cơ bản + 10 điểm mỗi sao
            int expEarned = 10 + (stars * 10);
            if (DataManager.Instance?.CurrentPlayer != null)
            {
                var player = DataManager.Instance.CurrentPlayer;
                player.EXP += expEarned;
                DataManager.Instance.DB.Update(player);
                Debug.Log($"<color=green>[Hệ thống Cấp độ]</color> Nhận {expEarned} EXP! Tổng EXP: {player.EXP}");
            }

            // Xóa dữ liệu phiên cũ và bốc quest mới
            activeQuests.Clear();
            _questProgress.Clear();
            _questsByItemId.Clear();
            ReloadQuestsAfterCompletion();

            // Thông báo cho UI cập nhật
            OnGameEnded?.Invoke(stars, gold, score);
            OnProgressUpdated?.Invoke();
        }

        /// <summary>Game Over khi bảng Tetris bị lấp đầy — tự động kết thúc phiên chơi.</summary>
        public void TriggerGameOver()
        {
            Debug.Log("[QuestManager] Game Over — tự động kết thúc phiên chơi.");
            TriggerEndGame();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Cầu nối với Database

        /// <summary>
        /// Khôi phục tiến độ quest từ Database về RAM khi bắt đầu phiên chơi.
        /// Đảm bảo tiến độ không bị mất khi người chơi thoát và vào lại.
        /// </summary>
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

            Debug.Log($"<color=cyan>[QUEST - Disk→RAM]</color> Khôi phục tiến độ từ DB cho người chơi {pid} thành công.");
            OnProgressUpdated?.Invoke();
        }

        /// <summary>
        /// Lưu tiến độ của một quest xuống SQLite.
        /// Tự động Insert nếu chưa có record, hoặc Update nếu đã tồn tại.
        /// </summary>
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

        /// <summary>
        /// Đánh dấu các quest đã hoàn thành trong Database (IsBanned = true).
        /// Nếu quest có isRepeatable = true → không bị ban, có thể bốc lại ở phiên sau.
        /// </summary>
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
                    // Quest có isRepeatable = true → không ban, có thể bốc lại
                    // Quest có isRepeatable = false → ban vĩnh viễn, không bốc lại
                    rec.IsBanned = !q.isRepeatable;
                    DataManager.Instance.DB.Update(rec);
                }
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Hàm hỗ trợ

        /// <summary>Xáo trộn ngẫu nhiên một danh sách (dùng thuật toán Fisher-Yates).</summary>
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
        #region Công cụ kiểm thử (chỉ dùng trong Editor)

        [ContextMenu("Test — Bán 5 Cà rốt (product_02)")]
        private void Test_SellCarrot() => UpdateProgress("product_02", 5);

        [ContextMenu("Test — Bán 5 Khoai tây (product_01)")]
        private void Test_SellPotato() => UpdateProgress("product_01", 5);

        [ContextMenu("Test — Hiển thị nông sản trong kho")]
        private void Test_LogProducts()
        {
            var products = GetAvailableProductIDs();
            Debug.Log($"[QuestManager] Kho hiện có: [{string.Join(", ", products)}]");
        }

        [ContextMenu("Test — Bốc lại Quest (Inventory-Aware)")]
        private void Test_ReloadQuests() => ReloadQuestsAfterCompletion();

        [ContextMenu("Test — Kết thúc Game")]
        private void Test_EndGame() => TriggerEndGame();

        #endregion
    }
}
