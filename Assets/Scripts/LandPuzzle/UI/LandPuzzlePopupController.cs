using UnityEngine;
using UnityEngine.UI;
using FarmPuzzle.LandPuzzle.Data;

namespace FarmPuzzle.LandPuzzle.UI
{
    /// <summary>
    /// Điều khiển Popup xác nhận trước khi bắt đầu chơi Block Puzzle để mở đất.
    /// </summary>
    public class LandPuzzlePopupController : MonoBehaviour
    {
        public static LandPuzzlePopupController Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject _confirmPopupPanel;
        [SerializeField] private Button _btnYes;
        [SerializeField] private Button _btnNo;
        [SerializeField] private Text _messageText; // (Tùy chọn) Hiện thông báo tốn bao nhiêu năng lượng

        private LandPlot _pendingPlot;

        // MỚI: Check xem popup có đang hiện không
        public bool IsShowing => _confirmPopupPanel != null && _confirmPopupPanel.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Đảm bảo ẩn lúc đầu
            if (_confirmPopupPanel != null) _confirmPopupPanel.SetActive(false);

            // Gán sự kiện
            if (_btnYes != null) _btnYes.onClick.AddListener(OnYesClicked);
            if (_btnNo != null) _btnNo.onClick.AddListener(OnNoClicked);
        }

        /// <summary>Hiện popup xác nhận mở đất.</summary>
        public void ShowConfirmPopup(LandPlot plot)
        {
            _pendingPlot = plot;
            if (_confirmPopupPanel != null) _confirmPopupPanel.SetActive(true);
            
            if (_messageText != null) 
                _messageText.text = "Bạn có muốn dùng 1⚡ để giải đố mở ô đất này không?";
            
            Debug.Log($"<color=cyan>[Puzzle UI]</color> Hiện Popup mở đất cho ô: {plot.gameObject.name}");
        }

        private void OnYesClicked()
        {
            Debug.Log($"<color=yellow>[Puzzle UI]</color> Nút ĐỒNG Ý được bấm. PendingPlot: {(_pendingPlot != null ? _pendingPlot.gameObject.name : "NULL")}");
            
            if (_pendingPlot == null) return;

            // 1. Kiểm tra năng lượng
            if (EnergySystem.Instance != null)
            {
                Debug.Log($"<color=yellow>[Puzzle UI]</color> Năng lượng hiện tại: {EnergySystem.Instance.CurrentEnergy}/{EnergySystem.Instance.MaxEnergy}");
                
                if (EnergySystem.Instance.HasEnergy)
                {
                    // 2. Tiêu năng lượng
                    bool consumed = EnergySystem.Instance.ConsumeEnergy(1);
                    Debug.Log($"<color=yellow>[Puzzle UI]</color> Tiêu 1 năng lượng: {(consumed ? "THÀNH CÔNG" : "THẤT BẠI")}");

                    // 3. Bắt đầu Puzzle
                    var puzzleManager = Object.FindFirstObjectByType<LandPuzzleManager>();
                    if (puzzleManager != null)
                    {
                        if (_pendingPlot.puzzleLevel != null)
                        {
                            Debug.Log($"<color=green>[Puzzle UI]</color> Gọi StartPuzzle với level: {_pendingPlot.puzzleLevel.name}");
                            puzzleManager.StartPuzzle(_pendingPlot.puzzleLevel, _pendingPlot);
                        }
                        else
                        {
                            Debug.LogError($"[Puzzle UI] Ô đất {_pendingPlot.gameObject.name} chưa được gán puzzleLevel! Sếp hãy kéo file SO_Level vào Inspector của nó.");
                        }
                    }
                    else
                    {
                        Debug.LogError("[Puzzle UI] KHÔNG TÌM THẤY LandPuzzleManager trong Scene! Sếp hãy kiểm tra xem Manager có đang Active không.");
                    }
                }
                else
                {
                    Debug.LogWarning("[Puzzle UI] Hết sạch năng lượng rồi sếp ơi!");
                }
            }
            else
            {
                Debug.LogError("[Puzzle UI] KHÔNG TÌM THẤY EnergySystem! Sếp đã chạy Tool chưa?");
            }

            HidePopup();
        }

        private void OnNoClicked()
        {
            HidePopup();
        }

        private void HidePopup()
        {
            if (_confirmPopupPanel != null) _confirmPopupPanel.SetActive(false);
            _pendingPlot = null;
        }
    }
}
