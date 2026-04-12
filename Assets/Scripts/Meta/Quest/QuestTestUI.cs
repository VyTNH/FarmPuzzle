using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace FarmPuzzle.Meta
{
    /// <summary>
    /// Công cụ Debug/Test: Tự động tạo các nút bấm dựa trên danh sách QUEST hiện có.
    /// Giúp bạn giả lập việc nhặt item mà không cần viết code gameplay.
    /// </summary>
    public class QuestTestUI : MonoBehaviour
    {
        [Header("UI References")]
        public Transform buttonParent;  // Nơi chứa các nút bấm
        public GameObject buttonPrefab; // Prefab một cái Button đơn giản

        void Start()
        {
            // Đợi 1 chút để QuestManager kịp khởi tạo dữ liệu
            Invoke(nameof(SyncAndCreateButtons), 0.2f);
        }

        [ContextMenu("Sync Buttons")]
        public void SyncAndCreateButtons()
        {
            if (QuestManager.Instance == null || buttonParent == null || buttonPrefab == null)
            {
                Debug.LogWarning("[QuestTest] Thiếu tham chiếu UI hoặc QuestManager chưa sẵn sàng.");
                return;
            }

            // 1. Xóa các nút cũ
            foreach (Transform child in buttonParent)
            {
                Destroy(child.gameObject);
            }

            // 2. Duyệt danh sách nhiệm vụ đang hoạt động
            foreach (var item in QuestManager.Instance.activeQuests)
            {
                if (item == null) continue;

                GameObject btnObj = Instantiate(buttonPrefab, buttonParent);
                string currentID = item.targetItemID;

                // Đổi chữ trên nút thành "Add [Tên Item]"
                Text btnText = btnObj.GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = "Add " + currentID;

                // Gán sự kiện Gửi tín hiệu nhặt đồ qua QuestEvents
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        // KÍCH HOẠT SỰ KIỆN TOÀN CỤC
                        QuestEvents.OnItemCollected?.Invoke(currentID, 1);
                        Debug.Log("<color=orange>[Test]</color> Giả lập nhặt: " + currentID);
                    });
                }
            }
        }
    }
}
