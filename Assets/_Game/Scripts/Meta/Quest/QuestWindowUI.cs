using System.Collections.Generic;
using UnityEngine;

namespace FarmPuzzle.Meta
{
    public class QuestWindowUI : MonoBehaviour
    {
        [Header("UI References")]
        public Transform contentParent; // Nơi chứa danh sách (ScrollContent)
        public GameObject questEntryPrefab; // Prefab của QuestEntryUI

        [Header("Settings")]
        public bool showActiveOnly = false; // Tùy chọn chỉ hiện nhiệm vụ đang làm

        private void OnEnable()
        {
            // Nghe tín hiệu từ MANAGER (sau khi đã cập nhật xong dữ liệu) 
            // Hoặc nghe trực tiếp từ QuestTestUI nếu cần Refresh thô
            QuestManager.OnProgressUpdated += HandleProgressUpdated;
            RefreshUI();
        }

        private void OnDisable()
        {
            QuestManager.OnProgressUpdated -= HandleProgressUpdated;
        }

        private void Start()
        {
            // Đảm bảo chạy Refresh lần đầu sau khi tất cả các Awake đã hoàn tất
            RefreshUI();
        }

        private void HandleProgressUpdated()
        {
            // Khi Manager báo có điểm mới, UI mới vẽ lại
            RefreshUI();
        }

        [ContextMenu("Refresh UI")]
        public void RefreshUI()
        {
            // Kiểm tra an toàn: Tránh lỗi NullReferenceException khi Manager chưa khởi tạo xong
            if (QuestManager.Instance == null || contentParent == null)
            {
                return; 
            }

            // 1. Xóa các item cũ trong danh sách
            foreach (Transform child in contentParent)
            {
                if (child != null) Destroy(child.gameObject);
            }

            // 2. Lấy danh sách nhiệm vụ của màn chơi hiện tại
            List<QuestDataSO> questsToShow = QuestManager.Instance.activeQuests;

            // 3. Tạo mới các item UI (Thanh mục tiêu ở dưới màn hình)
            foreach (var quest in questsToShow)
            {
                if (questEntryPrefab == null) break;

                GameObject go = Instantiate(questEntryPrefab, contentParent);
                QuestEntryUI entry = go.GetComponent<QuestEntryUI>();
                
                if (entry != null)
                {
                    entry.Setup(quest);
                }
            }
        }

        public void ToggleWindow(bool isOpen)
        {
            gameObject.SetActive(isOpen);
            if (isOpen) RefreshUI();
        }
    }
}
