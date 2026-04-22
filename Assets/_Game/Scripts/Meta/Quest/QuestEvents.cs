using System;

namespace FarmPuzzle.Meta
{
    /// <summary>
    /// Class trung gian chứa tất cả các sự kiện của hệ thống Quest.
    /// Giúp tách biệt logic Gameplay/Test khỏi QuestManager và UI.
    /// </summary>
    public static class QuestEvents
    {
        // Khi một vật phẩm được thu thập (ID vật phẩm, số lượng)
        public static Action<string, int> OnItemCollected;

        // Khi tiến độ của một Quest cụ thể thay đổi (Quest ID, tiến độ hiện tại, số lượng mục tiêu)
        public static Action<string, int, int> OnQuestProgressUpdated;

        // Khi tiến độ chung thay đổi (Dùng để UI tổng quát như QuestWindowUI cập nhật)
        public static Action OnGeneralProgressUpdated;

        // Khi màn chơi kết thúc thắng (Số vàng nhận được)
        public static Action<int> OnLevelWin;
    }
}
