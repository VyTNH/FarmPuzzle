using UnityEngine;
using UnityEngine.UI;
using FarmPuzzle.Core.Database;
using FarmPuzzle.FarmSystem;

namespace FarmPuzzle.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("References")]
        public GridManager gridManager;
        
        [Header("UI Panels")]
        public GameObject loginPanel;      // Nơi chứa 2 nút ban đầu
        public GameObject newPlayerPanel;  // Nơi nhập tên nếu người chơi mới
        public GameObject hudPanel;        // Nơi chứa nút Mở Tetris, v.v. khi đã vào game
        
        [Header("Login Buttons")]
        public Button btnPlayNew;          // Chơi Mới
        public Button btnRetrieve;         // Chơi Tiếp (Load dữ liệu)
        
        [Header("New Player Input")]
        public InputField inputPlayerName;
        public Button btnSubmitNewPlayer;

        [Header("HUD Controls")]
        public Button btnOpenTetris;
        private string _lastUsedID = "tester_01";

        private void Start()
        {
            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();

            if (btnPlayNew != null) btnPlayNew.onClick.AddListener(OnPlayNewClicked);
            if (btnRetrieve != null) btnRetrieve.onClick.AddListener(OnRetrieveClicked);
            if (btnSubmitNewPlayer != null) btnSubmitNewPlayer.onClick.AddListener(OnSubmitNewPlayerClicked);
            if (btnOpenTetris != null) btnOpenTetris.onClick.AddListener(OpenTetris);

            ShowPanel(loginPanel);
            
            // Tìm ID gần nhất nếu có
            if (PlayerPrefs.HasKey("FarmPuzzle_LastPlayerID"))
            {
                _lastUsedID = PlayerPrefs.GetString("FarmPuzzle_LastPlayerID");
            }
        }

        private void ShowPanel(GameObject panel)
        {
            if (loginPanel != null) loginPanel.SetActive(panel == loginPanel);
            if (newPlayerPanel != null) newPlayerPanel.SetActive(panel == newPlayerPanel);
            if (hudPanel != null) hudPanel.SetActive(panel == hudPanel);
        }

        private void OnPlayNewClicked()
        {
            ShowPanel(newPlayerPanel);
        }

        private void OnSubmitNewPlayerClicked()
        {
            string pName = inputPlayerName.text.Trim();
            if (string.IsNullOrEmpty(pName)) pName = "Nông Dân Mới";

            // Sinh ID động dựa trên thời gian
            string newID = "player_" + System.DateTime.Now.Ticks.ToString();

            Login(newID, pName);
        }

        private void OnRetrieveClicked()
        {
            // Nếu có player cũ lưu trong DB, ta lấy người gần nhất, tạm thời dùng _lastUsedID
            Login(_lastUsedID, "Khách Cũ");
        }

        private void Login(string pId, string pName)
        {
            if (DataManager.Instance == null) return;

            if (DataManager.Instance.LoginPlayer(pId, pName))
            {
                PlayerPrefs.SetString("FarmPuzzle_LastPlayerID", pId);
                PlayerPrefs.Save();

                if (gridManager != null) gridManager.LoadGridState();
                
                ShowPanel(hudPanel);
                
                // Đóng luôn SandboxCanvasUI cũ nếu còn tồn tại trong scene
                var oldSandbox = FindFirstObjectByType<FarmPuzzle.Testing.SandboxCanvasUI>();
                if (oldSandbox != null) oldSandbox.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError($"[MainMenu] Lỗi không thể đăng nhập vào DB cho {pId}");
            }
        }

        private void OpenTetris()
        {
            var tetris = FindFirstObjectByType<FarmPuzzle.Tetris.TetrisManager>(FindObjectsInactive.Include);
            if (tetris != null)
            {
                tetris.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[MainMenu] Không tìm thấy Canvas Tetris!");
            }
        }
    }
}
