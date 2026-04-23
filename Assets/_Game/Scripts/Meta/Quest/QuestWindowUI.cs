using System.Collections.Generic;
using UnityEngine;

namespace FarmPuzzle.Meta
{
    public class QuestWindowUI : MonoBehaviour
    {
        [Header("UI References")]
        public Transform contentParent; // Nơi chứa danh sách (ScrollContent)
        public GameObject questEntryPrefab; // Prefab của QuestEntryUI

        private List<QuestEntryUI> _spawnedEntries = new List<QuestEntryUI>();

        private void OnEnable()
        {
            // Lắng nghe sự kiện tiến độ tổng quát (Để Refresh nếu có Reset màn chơi)
            QuestEvents.OnGeneralProgressUpdated += RefreshUI;
            RefreshUI();
        }

        private void OnDisable()
        {
            QuestEvents.OnGeneralProgressUpdated -= RefreshUI;
        }

        private void Start()
        {
            RefreshUI();
        }

        [ContextMenu("Refresh UI")]
        public void RefreshUI()
        {
            if (QuestManager.Instance == null || contentParent == null || questEntryPrefab == null)
            {
                return; 
            }

            // 1. Lấy danh sách nhiệm vụ từ Manager
            var activeQuests = QuestManager.Instance.activeQuests;

            // 2. Đảm bảo số lượng vật thể UI đủ dùng (KHÔNG Destroy)
            while (_spawnedEntries.Count < activeQuests.Count)
            {
                GameObject go = Instantiate(questEntryPrefab, contentParent);
                QuestEntryUI entry = go.GetComponent<QuestEntryUI>();
                if (entry != null) _spawnedEntries.Add(entry);
                else Destroy(go); // Đề phòng prefab thiếu component
            }

            // 3. Tái sử dụng: Bật những cái cần dùng, tắt những cái thừa
            for (int i = 0; i < _spawnedEntries.Count; i++)
            {
                if (i < activeQuests.Count)
                {
                    _spawnedEntries[i].gameObject.SetActive(true);
                    _spawnedEntries[i].Setup(activeQuests[i]);
                }
                else
                {
                    // Thừa thì tắt đi, Layout Group sẽ tự động bỏ qua cái này
                    _spawnedEntries[i].gameObject.SetActive(false);
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
