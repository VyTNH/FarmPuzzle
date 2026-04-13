using UnityEngine;
using UnityEngine.UI;
using FarmPuzzle.Core.Database;
using FarmPuzzle.FarmSystem;
using FarmPuzzle.FarmSystem.Crop;

namespace FarmPuzzle.Testing
{
    public class SandboxCanvasUI : MonoBehaviour
    {
        [Header("References")]
        public GridManager gridManager;
        public SeedItemSO[] availableSeeds;

        [Header("UI - Login Panel")]
        public GameObject loginPanel;
        public InputField inputPlayerID;
        public InputField inputPlayerName;
        public Button btnLogin;

        [Header("UI - Control Panel")]
        public GameObject controlPanel;
        public Text statusText;
        public Text inventoryText;
        public Transform seedButtonContainer;
        public GameObject seedButtonPrefab; // Prefab có Button + Text
        
        public Button btnAddGold;
        public Button btnAddSeed;
        public Button btnTetris;

        void Start()
        {
            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();

            if (btnLogin != null) btnLogin.onClick.AddListener(OnLoginClicked);
            if (btnAddGold != null) btnAddGold.onClick.AddListener(() => DataManager.Instance.AddGold(100));
            if (btnAddSeed != null) btnAddSeed.onClick.AddListener(OnAddSeedClicked);
            if (btnTetris != null) btnTetris.onClick.AddListener(OpenTetris);

            if (controlPanel != null) controlPanel.SetActive(false);
            if (loginPanel != null) loginPanel.SetActive(true);
            
            // Set default login info
            if (inputPlayerID != null) inputPlayerID.text = "tester_01";
            if (inputPlayerName != null) inputPlayerName.text = "Sếp Dương";

            if (availableSeeds == null || availableSeeds.Length == 0)
            {
                availableSeeds = Resources.LoadAll<SeedItemSO>("");
                Debug.Log($"[SandboxCanvasUI] Tự động nạp {availableSeeds.Length} hạt giống từ Resources.");
            }
        }

        private float invRefreshTimer = 0f;
        private void Update()
        {
            if (controlPanel == null || !controlPanel.activeSelf || inventoryText == null) return;
            
            invRefreshTimer -= Time.deltaTime;
            if (invRefreshTimer <= 0)
            {
                invRefreshTimer = 3f; // Query DB every 3 seconds
                if (DataManager.Instance != null && DataManager.Instance.CurrentPlayer != null)
                {
                    var inv = DataManager.Instance.DB.Table<InventoryModel>().Where(i => i.PlayerID == DataManager.Instance.CurrentPlayer.PlayerID).ToList();
                    string txt = "Kho đồ:\n";
                    foreach(var item in inv) txt += $"- {item.ItemID}: {item.Quantity}\n";
                    if (inv.Count == 0) txt += "Trống";
                    inventoryText.text = txt;
                }
            }
        }

        private void OpenTetris()
        {
            var tetris = FindFirstObjectByType<FarmPuzzle.Tetris.TetrisManager>(FindObjectsInactive.Include);
            if (tetris != null)
            {
                tetris.gameObject.SetActive(true);
                Debug.Log("<color=yellow>Đã mở giao diện Tetris!</color>");
            }
            else
            {
                Debug.LogWarning("Không tìm thấy Tetris! Hãy chắc chắn sếp đã tạo Canvas Tetris từ công cụ Editor.");
            }
        }

        private void OnLoginClicked()
        {
            if (DataManager.Instance == null)
            {
                Debug.LogError("[LOG-UI] THẤT BẠI: DataManager.Instance == null! DB chưa sẵn sàng.");
                return;
            }

            string pId = inputPlayerID.text;
            string pName = inputPlayerName.text;
            
            Debug.Log($"[LOG-UI] Người dùng bấm nút Login. ID: {pId}, Tên: {pName}");

            if (DataManager.Instance.LoginPlayer(pId, pName))
            {
                Debug.Log($"<color=green>[LOG-UI] ĐĂNG NHẬP THÀNH CÔNG!</color> Chuyển đổi giao diện sang Control Panel cho {pId}");
                if (gridManager != null) gridManager.LoadGridState();
                
                loginPanel.SetActive(false);
                controlPanel.SetActive(true);
                UpdateStatusText();
                PopulateSeedButtons();
            }
            else
            {
                Debug.LogError($"[LOG-UI] ĐĂNG NHẬP THẤT BẠI cho ID: {pId}. Kiểm tra lại DataManager.");
            }
        }

        private void UpdateStatusText()
        {
            var player = DataManager.Instance.CurrentPlayer;
            statusText.text = $"✅ Đã đăng nhập: {player.Name} | 💰 {player.Money}G";
        }

        private void PopulateSeedButtons()
        {
            // Clear old buttons
            foreach (Transform child in seedButtonContainer) Destroy(child.gameObject);

            // --- Sinh tool thu hoạch & chăm sóc cây (Draggable) ---
            CreateDraggableToolItem("✂ Thu hoạch", CropNeedType.None, null, true);
            CreateDraggableToolItem("💧 Tưới nước", CropNeedType.Water, null, false);
            CreateDraggableToolItem("🐛 Bắt sâu", CropNeedType.Pest, null, false);
            CreateDraggableToolItem("💖 Bón phân", CropNeedType.Fertilizer, null, false);

            // --- Sinh hạt giống (Draggable) ---
            if (availableSeeds == null || availableSeeds.Length == 0) return;

            foreach (var seed in availableSeeds)
            {
                if (seed == null) continue;
                CreateDraggableToolItem(seed.seedName, CropNeedType.None, seed, false);
            }
        }

        private void CreateDraggableToolItem(string label, CropNeedType type, SeedItemSO seed, bool isHarvest = false)
        {
            GameObject btnObj = Instantiate(seedButtonPrefab, seedButtonContainer);
            btnObj.SetActive(true);
            
            Text txt = btnObj.GetComponentInChildren<Text>();
            if (txt != null) txt.text = label;

            var draggable = btnObj.AddComponent<FarmPuzzle.UI.DraggableTool>();
            draggable.toolType = type;
            draggable.seedData = seed;
            draggable.isHarvestTool = isHarvest;
            
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null) Destroy(btn);
        }

        private void OnAddSeedClicked()
        {
            if (gridManager != null && gridManager.selectedSeed != null)
            {
                DataManager.Instance.AddItem(gridManager.selectedSeed.seedID, 5);
                Debug.Log($"<color=green>[Hack]</color> +5 '{gridManager.selectedSeed.seedID}' vào Kho!");
            }
        }

    }
}
