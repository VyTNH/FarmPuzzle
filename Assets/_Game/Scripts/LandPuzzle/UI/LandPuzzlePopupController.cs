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
        private Toggle _skipToggle; // Checkbox Đừng Hỏi Lại

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

            // Tự Động Rút Gọn: Tạo UI Checkbox "Don't ask me again" (Chống trôi dạt lề)
            if (_confirmPopupPanel != null && _skipToggle == null)
            {
                GameObject toggleObj = new GameObject("Toggle_SkipPopup", typeof(RectTransform));
                toggleObj.transform.SetParent(_confirmPopupPanel.transform, false);
                var tRect = toggleObj.GetComponent<RectTransform>();
                tRect.anchoredPosition = new Vector2(0, -60); // Nằm dưới các nút
                tRect.sizeDelta = new Vector2(160, 30);

                // Background
                var bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgObj.transform.SetParent(toggleObj.transform, false);
                bgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
                bgObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-60, 0);
                bgObj.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f, 1f);

                // Checkmark
                var checkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
                checkObj.transform.SetParent(bgObj.transform, false);
                checkObj.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
                checkObj.GetComponent<Image>().color = Color.green;

                // Label
                var labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObj.transform.SetParent(toggleObj.transform, false);
                var txt = labelObj.GetComponent<Text>();
                txt.text = "Không hỏi lại";
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.color = Color.black;
                txt.alignment = TextAnchor.MiddleLeft;
                var lblRect = labelObj.GetComponent<RectTransform>();
                lblRect.sizeDelta = new Vector2(120, 30);
                lblRect.anchoredPosition = new Vector2(20, 0);

                // Logic
                _skipToggle = toggleObj.AddComponent<Toggle>();
                _skipToggle.targetGraphic = bgObj.GetComponent<Image>();
                _skipToggle.graphic = checkObj.GetComponent<Image>();
                _skipToggle.isOn = false;
            }
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

            // Lưu tùy chọn Không hỏi lại nếu người chơi Tick
            if (_skipToggle != null && _skipToggle.isOn)
            {
                PlayerPrefs.SetInt("SkipUnlockConfirm", 1);
                PlayerPrefs.Save();
                Debug.Log("<color=green>[Puzzle UI]</color> Đã lưu cài đặt: AUTO SKIP POPUP!");
            }

            // 1. Kiểm tra năng lượng
            if (EnergySystem.Instance != null)
            {
                Debug.Log($"<color=yellow>[Puzzle UI]</color> Năng lượng hiện tại: {EnergySystem.Instance.CurrentEnergy}/{EnergySystem.Instance.MaxEnergy}");
                
                if (EnergySystem.Instance.HasEnergy)
                {
                    // 2. Tiêu năng lượng
                    bool consumed = EnergySystem.Instance.ConsumeEnergy(1);
                    Debug.Log($"<color=orange>[Giai Đoạn 2: Tiêu hao Năng lượng]</color> Đã trừ 1⚡ -> Năng lượng còn lại: {EnergySystem.Instance.CurrentEnergy}. Truyền Data sang Bàn Cờ!");

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
            if (_confirmPopupPanel != null)
            {
                // FIX: Destroy() là async, SetActive(false) xảy ra ngay → OnDisable của PanelSimpleCasual crash.
                // Giải pháp đúng: Dùng Reflection set otherPanels = [] trước khi SetActive
                var panelSimple = _confirmPopupPanel.GetComponent<LayerLab.PanelSimpleCasual>();
                if (panelSimple != null)
                {
                    var field = panelSimple.GetType().GetField(
                        "otherPanels",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(panelSimple, new GameObject[0]); // Ép mảng rỗng → OnDisable không crash
                        Debug.Log("[LandPuzzlePopup] Đã khởi tạo otherPanels = [] cho PanelSimpleCasual.");
                    }
                }
                _confirmPopupPanel.SetActive(false);
            }
            _pendingPlot = null;
        }
    }
}
