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

        [Header("Global UI (Hidden until Login)")]
        public GameObject decorShopPanel;
        public GameObject seedShopPanel;
        public GameObject inventoryPanel;  // MỚI: Bảng kho đồ ở dưới cùng
        public GameObject farmTopBar;
        
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

            // Tự động tìm kiếm các bảng nếu Inspector bị trống (NULL)
            if (decorShopPanel == null) decorShopPanel = GameObject.Find("DecorShopPanel");
            if (seedShopPanel == null) seedShopPanel = GameObject.Find("SeedShopPanel");
            if (inventoryPanel == null) inventoryPanel = GameObject.Find("InventoryPanel");
            if (farmTopBar == null) farmTopBar = GameObject.Find("Canvas_FarmTopBar");

            // Nếu vẫn NULL (do đang bị Deactive), cố gắng tìm sâu hơn
            if (decorShopPanel == null) decorShopPanel = FindInactiveByName("DecorShopPanel");
            if (seedShopPanel == null) seedShopPanel = FindInactiveByName("SeedShopPanel");
            if (inventoryPanel == null) inventoryPanel = FindInactiveByName("InventoryPanel");
            if (farmTopBar == null) farmTopBar = FindInactiveByName("Canvas_FarmTopBar");

            // Đảm bảo ẩn triệt để lúc khởi đầu
            if (decorShopPanel != null) decorShopPanel.SetActive(false);
            if (seedShopPanel != null) seedShopPanel.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            if (farmTopBar != null) farmTopBar.SetActive(false);

            ShowPanel(loginPanel);
            
            if (PlayerPrefs.HasKey("FarmPuzzle_LastPlayerID"))
            {
                _lastUsedID = PlayerPrefs.GetString("FarmPuzzle_LastPlayerID");
            }
        }

        private GameObject FindInactiveByName(string name)
        {
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in all)
            {
                if (go.name == name && go.transform.parent != null)
                {
                    if (!string.IsNullOrEmpty(go.scene.name)) return go;
                }
            }
            return null;
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
            string newID = "player_" + System.DateTime.Now.Ticks.ToString();
            Login(newID, pName);
        }

        private void OnRetrieveClicked()
        {
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
                
                // HIỆN CÁC BẢNG UI TOÀN CỤC CHỈ KHI LOGIN THÀNH CÔNG
                if (decorShopPanel != null) decorShopPanel.SetActive(true);
                if (seedShopPanel != null) seedShopPanel.SetActive(true);
                if (inventoryPanel != null) inventoryPanel.SetActive(true);
                if (farmTopBar != null) farmTopBar.SetActive(true);

                ShowPanel(hudPanel);
                
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
