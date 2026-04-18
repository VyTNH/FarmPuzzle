using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace FarmPuzzle.Meta
{
    /// <summary>
    /// Script hỗ trợ Demo: Tự động tạo các nút bấm dựa trên danh sách QUEST có sẵn trong QuestManager.
    /// CHỨA CÁC BIẾN SỰ KIỆN GIẢ LẬP ĐỂ TEST.
    /// </summary>
    public class QuestTestUI : MonoBehaviour
    {
        // --- CÁC SỰ KIỆN GIẢ LẬP (Thay thế cho file QuestTestEvents cũ) ---
        public static Action<string, int> OnTestBlockCleared; // Khi bấm nút Add
        public static Action<int> OnTestLevelWin;           // Khi thắng màn chơi

        [Header("UI References")]
        public Transform buttonParent;  // Nơi chứa các nút bấm
        public GameObject buttonPrefab; // Prefab nút bấm đơn giản

        void Start()
        {
            // Đợi 1 chút để QuestManager kịp Awake
            Invoke(nameof(SyncAndCreateButtons), 0.1f);
        }

        private void SyncAndCreateButtons()
        {
            if (QuestManager.Instance == null || buttonParent == null || buttonPrefab == null)
            {
                return;
            }

            // 1. Xóa các nút cũ
            foreach (Transform child in buttonParent)
            {
                Destroy(child.gameObject);
            }

            // 2. Duyệt danh sách nhiệm vụ và tạo nút
            foreach (var item in QuestManager.Instance.activeQuests)
            {
                if (item == null) continue;

                GameObject btnObj = Instantiate(buttonPrefab, buttonParent);
                string currentID = item.targetItemID;

                // Đổi chữ trên nút
                Text btnText = btnObj.GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = "Add " + currentID;

                // Gán sự kiện
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    SetupButton(btn, currentID);
                }
            }
        }

        private void SetupButton(Button btn, string id)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                // Gửi tín hiệu cộng 1 vật phẩm dùng chính biến static trong class này
                OnTestBlockCleared?.Invoke(id, 1);
                Debug.Log("<color=orange>[Demo]</color> Clicked Add: " + id);
            });
        }
    }
}
