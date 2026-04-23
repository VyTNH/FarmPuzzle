using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FarmPuzzle.Meta
{
    public class WinPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panelRoot;      // Kéo thả toàn bộ Panel cha vào đây
        public TextMeshProUGUI rewardText; // Ô chữ hiện số vàng (Ví dụ: +100 GOLD)
        public Button continueButton;     // Nút Tiếp tục
        public Button mainMenuButton;     // Nút Về Menu

        private void Awake()
        {
            // Mặc định ẩn bảng khi vào game
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            // Nghe tín hiệu thắng từ hệ thống Quest trung gian
            QuestEvents.OnLevelWin += HandleWinDisplay;

            if (continueButton != null) continueButton.onClick.AddListener(OnContinueClick);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClick);
        }

        private void OnDisable()
        {
            QuestEvents.OnLevelWin -= HandleWinDisplay;
            
            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClick);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuClick);
        }

        /// <summary>
        /// Hàm xử lý khi nhận được tín hiệu thắng.
        /// </summary>
        private void HandleWinDisplay(int goldAmount)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                if (rewardText != null) rewardText.text = $"+{goldAmount} GOLD";
                Debug.Log($"<color=cyan>[UI]</color> HIỆN BẢNG THẮNG: Nhận được {goldAmount} vàng.");
            }
        }

        private void OnContinueClick()
        {
            Debug.Log("Sự kiện: Bấm nút Tiếp tục màn mới.");
            // Thêm logic chuyển màn ở đây nếu cần
        }

        private void OnMainMenuClick()
        {
            Debug.Log("Sự kiện: Bấm nút Quay về Menu chính.");
            // Thêm logic quay về Menu ở đây nếu cần
        }
    }
}
