using UnityEngine;
using UnityEngine.UI;
using FarmPuzzle.Meta;
using FarmPuzzle.Core;
using FarmPuzzle.LandPuzzle;
using FarmPuzzle.LandPuzzle.Data;

namespace FarmPuzzle.UI
{
    /// <summary>
    /// Kịch bản tự động Bind dữ liệu thực tế cho KHO HÀNG (Canvas_Inventory) và ĐƠN HÀNG (QuestHUD / Panel_Quests)
    /// </summary>
    public class UIDataBinder : MonoBehaviour
    {
        // ─── CACHED REFERENCES ───
        private Text _questContentText;
        private Transform _inventoryPanel;
        private Canvas _inventoryCanvas;
        private Canvas _tetrisPopupCanvas;

        // ─── CACHED GAME OBJECT NAMES ───
        private const string NAME_INVENTORY_PANEL = "InventoryPanel";
        private const string NAME_TETRIS_POPUP = "Canvas_TetrisPopupUI";

        private void OnEnable()
        {
            // Đăng ký nghe sự kiện
            QuestManager.OnProgressUpdated += RefreshQuestUI;
            DataManager.OnInventoryChanged += HandleInventoryChanged;
            DataManager.OnInventoryItemAdded += HandleInventoryItemAdded;
            DataManager.OnPlayerLoggedIn += HandlePlayerLoggedIn;
        }

        private void OnDisable()
        {
            QuestManager.OnProgressUpdated -= RefreshQuestUI;
            DataManager.OnInventoryChanged -= HandleInventoryChanged;
            DataManager.OnInventoryItemAdded -= HandleInventoryItemAdded;
            DataManager.OnPlayerLoggedIn -= HandlePlayerLoggedIn;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // --- XỬ LÝ QUEST HUD ---
            GameObject questHud = FindGameObjectCached("QuestHUD", "Panel_Quests");
            if (questHud != null)
            {
                Transform contentTr = questHud.transform.Find("QuestContent");
                if (contentTr == null) contentTr = questHud.transform.Find("QuestText");
                if (contentTr != null) _questContentText = contentTr.GetComponent<Text>();
            }

            // --- CACHED: Tìm và cache InventoryPanel một lần duy nhất ---
            TryFindInventoryPanel();

            // --- CACHED: Tìm và cache TetrisPopupCanvas một lần duy nhất ---
            _tetrisPopupCanvas = FindGameObjectCached(NAME_TETRIS_POPUP)?.GetComponent<Canvas>();

            RefreshQuestUI();
            RefreshInventoryUI();
        }

        private void OnDestroy()
        {
            QuestManager.OnProgressUpdated -= RefreshQuestUI;
            DataManager.OnInventoryChanged -= HandleInventoryChanged;
            DataManager.OnInventoryItemAdded -= HandleInventoryItemAdded;
            DataManager.OnPlayerLoggedIn -= HandlePlayerLoggedIn;
        }

        private void Update()
        {
            // Chỉ tìm lại InventoryPanel khi bị mất VÀ cần thiết (scene transition)
            if (_inventoryPanel == null) TryFindInventoryPanel();

            // Dùng cached reference thay vì GameObject.Find() mỗi frame
            bool isPuzzleActive = LandPuzzleManager.Instance != null && LandPuzzleManager.Instance.IsPuzzleActive;
            if (_tetrisPopupCanvas != null && _tetrisPopupCanvas.enabled) isPuzzleActive = true;

            if (_inventoryCanvas != null)
                _inventoryCanvas.enabled = !isPuzzleActive;
        }

        private void TryFindInventoryPanel()
        {
            GameObject invGo = FindGameObjectCached(NAME_INVENTORY_PANEL);
            if (invGo != null)
            {
                _inventoryCanvas = invGo.GetComponentInParent<Canvas>();
                _inventoryPanel = invGo.transform;
                RefreshInventoryUI();
            }
        }

        private static GameObject FindGameObjectCached(params string[] names)
        {
            foreach (string name in names)
            {
                var go = GameObject.Find(name);
                if (go != null) return go;
            }
            return null;
        }

        // ─── QUEST UI ───
        public void RefreshQuestUI()
        {
            if (_questContentText == null || QuestManager.Instance == null) return;

            var quests = QuestManager.Instance.activeQuests;
            if (quests == null || quests.Count == 0)
            {
                _questContentText.text = "✔️ Chưa có đơn hàng nào.";
                return;
            }

            string txt = "";
            bool allDone = true;
            foreach (var q in quests)
            {
                int current = QuestManager.Instance.GetQuestProgress(q.questID);
                bool done = current >= q.targetAmount;
                if (!done) allDone = false;

                string checkmark = done ? "<color=green>✅</color>" : "◻️";
                txt += $"{checkmark} <b>{q.targetItemID}</b>: {current}/{q.targetAmount}\n";
            }

            if (allDone) txt += "\n<color=yellow>🎉 BẠN ĐÃ HOÀN THÀNH TẤT CẢ ĐƠN HÀNG!</color>";
            _questContentText.text = txt;
        }

        // Prefab của Slot (kéo vào Inspector hoặc đặt vào Resources/UI/InventorySlot)
        [SerializeField] private GameObject slotPrefabOverride;
        private GameObject _slotPrefab;

        // ─── EVENT HANDLERS ───

        /// <summary>Gọi ngay sau khi player login xong — rebuild toàn bộ inventory sau khi Canvas layout ổn định</summary>
        private void HandlePlayerLoggedIn()
        {
            TryFindInventoryPanel();
            // Canvas layout cần ít nhất 2 frame để tính xong RectTransform.
            StartCoroutine(RefreshInventoryAfterLayout());
        }

        private System.Collections.IEnumerator RefreshInventoryAfterLayout()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            RefreshInventoryUI();
        }

        /// <summary>Gọi khi 1 item đã có trong kho thay đổi số lượng → chỉ cập nhật đúng 1 slot</summary>
        private void HandleInventoryChanged(string itemID, int newQty)
        {
            Transform container = GetSlotContainer();
            if (container == null) return;

            Transform slotTr = container.Find("Slot_" + itemID);
            if (slotTr == null) { RefreshInventoryUI(); return; } // Slot mới → full rebuild

            slotTr.gameObject.SetActive(newQty > 0);
            if (newQty > 0)
            {
                string qtyStr = newQty.ToString();
                if (itemID == "tool_watercan" || itemID == "tool_hoe") qtyStr = "∞";

                foreach (Text t in slotTr.GetComponentsInChildren<Text>(true))
                    if (t.name == "txt_count" || t.name.StartsWith("txt_count"))
                        t.text = qtyStr;
            }
        }

        /// <summary>Gọi khi item hoàn toàn mới xuất hiện trong kho → full rebuild để tạo slot mới</summary>
        private void HandleInventoryItemAdded(string itemID, int initialQty)
        {
            RefreshInventoryUI();
        }

        // ─── CORE INVENTORY BUILDER ───
        public void RefreshInventoryUI()
        {
            if (_inventoryPanel == null || global::DataManager.Instance == null) return;
            if (global::DataManager.Instance.CurrentPlayer == null) return;

            // Bước 1: Tìm SlotContainer
            Transform container = FindChildRecursive(_inventoryPanel, "SlotContainer");
            if (container == null) container = FindChildRecursive(_inventoryPanel, "Content");
            if (container == null) container = FindChildRecursive(_inventoryPanel, "BackGround");
            if (container == null) return;

            RectTransform containerRect = container.GetComponent<RectTransform>();

            // Bước 2: Load prefab từ Resources/UI/InventorySlot.prefab
            if (_slotPrefab == null)
                _slotPrefab = slotPrefabOverride != null
                    ? slotPrefabOverride
                    : Resources.Load<GameObject>(ProjectPaths.RS_UI_INVENTORY_SLOT);

            if (_slotPrefab == null)
            {
                Debug.LogWarning("[UIDataBinder] Slot Prefab NOT FOUND. Đặt vào Resources/UI/InventorySlot.prefab hoặc kéo vào slotPrefabOverride.");
                return;
            }

            // Bước 3: Đọc DB và build slots
            var sessionItems = global::DataManager.Instance.DB
                .Table<Core.Database.InventoryModel>()
                .Where(i => i.PlayerID == global::DataManager.Instance.CurrentPlayer.PlayerID)
                .ToList();

            var activeSlotNames = new System.Collections.Generic.HashSet<string>();

            foreach (var dbItem in sessionItems)
            {
                int qty = global::DataManager.Instance.GetItemAmount(dbItem.ItemID);
                if (qty <= 0 && dbItem.ItemID != "tool_watercan" && dbItem.ItemID != "tool_hoe") continue;

                string slotName = "Slot_" + dbItem.ItemID;
                activeSlotNames.Add(slotName);

                Transform existingSlot = container.Find(slotName);
                GameObject slotGo;

                if (existingSlot == null)
                {
                    // Tạo slot mới — giữ nguyên toàn bộ layout từ Prefab, không override RectTransform
                    slotGo = Instantiate(_slotPrefab, container);
                    slotGo.SetActive(true);
                    slotGo.name = slotName;

                    // Gán Sprite icon — tìm Image đầu tiên con của "Icon"
                    Image imgComp = slotGo.transform.Find("Icon")?.GetComponent<Image>();
                    if (imgComp != null)
                    {
                        Sprite finalSprite = null;
                        if      (dbItem.ItemID.StartsWith(ProjectPaths.PREFIX_PRODUCT)) { var so = Resources.Load<CropDataSO>(ProjectPaths.RS_PREFIX_CROP_SO + dbItem.ItemID); finalSprite = so?.productIcon; }
                        else if (dbItem.ItemID.StartsWith(ProjectPaths.PREFIX_SEED))    { var so = Resources.Load<SeedItemSO>(ProjectPaths.RS_PREFIX_SEED_SO + dbItem.ItemID); finalSprite = so?.inventoryIcon; }
                        else if (dbItem.ItemID.StartsWith(ProjectPaths.PREFIX_TOOL) || dbItem.ItemID.StartsWith(ProjectPaths.PREFIX_ITEM))
                                                                      { var so = Resources.Load<ToolItemSO>(ProjectPaths.RS_PREFIX_TOOL_SO + dbItem.ItemID); finalSprite = so?.inventoryIcon; }

                        imgComp.sprite = finalSprite;
                        imgComp.color  = finalSprite != null ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);

                        // ===== DRAGGABLE TOOL SETUP =====
                        var draggable = imgComp.gameObject.GetComponent<FarmPuzzle.UI.DraggableTool>();
                        if (draggable == null) draggable = imgComp.gameObject.AddComponent<FarmPuzzle.UI.DraggableTool>();

                        draggable.isHarvestTool = false;
                        draggable.toolType = FarmPuzzle.FarmSystem.Crop.CropNeedType.None;
                        draggable.seedData = null;

                        if      (dbItem.ItemID == ProjectPaths.ID_TOOL_HOE)      { draggable.isHarvestTool = true; }
                        else if (dbItem.ItemID == ProjectPaths.ID_TOOL_WATERCAN) { draggable.toolType = FarmPuzzle.FarmSystem.Crop.CropNeedType.Water; }
                        else if (dbItem.ItemID == ProjectPaths.ID_TOOL_PEST)     { draggable.toolType = FarmPuzzle.FarmSystem.Crop.CropNeedType.Pest; }
                        else if (dbItem.ItemID == ProjectPaths.ID_ITEM_FERTILIZER) { draggable.toolType = FarmPuzzle.FarmSystem.Crop.CropNeedType.Fertilizer; }
                        else if (dbItem.ItemID.StartsWith(ProjectPaths.PREFIX_SEED)) { draggable.seedData = Resources.Load<SeedItemSO>(ProjectPaths.RS_PREFIX_SEED_SO + dbItem.ItemID); }
                        else
                        {
                            if (Application.isPlaying) Destroy(draggable);
                            else DestroyImmediate(draggable);
                        }
                    }
                }
                else
                {
                    slotGo = existingSlot.gameObject;
                    if (!slotGo.activeSelf) slotGo.SetActive(true);
                }

                // Cập nhật text số lượng
                string qtyStr = (dbItem.ItemID == ProjectPaths.ID_TOOL_WATERCAN || dbItem.ItemID == ProjectPaths.ID_TOOL_HOE)
                    ? "∞" : qty.ToString();

                foreach (UnityEngine.UI.Text t in slotGo.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    if (t.name == "txt_count" || t.name.StartsWith("txt_count"))
                        t.text = qtyStr;
            }

            // Ẩn slot không còn trong kho
            foreach (Transform child in container)
                if (!activeSlotNames.Contains(child.name)) child.gameObject.SetActive(false);

            // Force layout rebuild để LayoutGroup cập nhật vị trí
            if (containerRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }

        // ─── HELPERS ───

        private string GetItemName(string itemID)
        {
            if (itemID.StartsWith(ProjectPaths.PREFIX_PRODUCT))
            {
                CropDataSO crop = Resources.Load<CropDataSO>(ProjectPaths.RS_PREFIX_CROP_SO + itemID);
                if (crop != null && !string.IsNullOrEmpty(crop.cropName)) return crop.cropName;
            }
            else if (itemID.StartsWith(ProjectPaths.PREFIX_SEED))
            {
                SeedItemSO seed = Resources.Load<SeedItemSO>(ProjectPaths.RS_PREFIX_SEED_SO + itemID);
                if (seed != null && !string.IsNullOrEmpty(seed.seedName)) return seed.seedName;
            }
            else if (itemID.StartsWith("tool_") || itemID.StartsWith("item_"))
            {
                ToolItemSO tool = Resources.Load<ToolItemSO>(ProjectPaths.RS_PREFIX_TOOL_SO + itemID);
                if (tool != null && !string.IsNullOrEmpty(tool.toolName)) return tool.toolName;
            }
            // Fallback hardcode cho tools chưa có SO
            if (itemID == "tool_hoe")        return "Cái Cuốc";
            if (itemID == "tool_watercan")   return "Bình Tưới";
            if (itemID == "tool_pest")       return "Thuốc Sâu";
            if (itemID == "item_fertilizer") return "Phân Bón";
            return itemID;
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private Transform GetSlotContainer()
        {
            if (_inventoryPanel == null) return null;
            Transform container = FindChildRecursive(_inventoryPanel, "SlotContainer");
            if (container == null) container = FindChildRecursive(_inventoryPanel, "Content");
            if (container == null) container = FindChildRecursive(_inventoryPanel, "BackGround");
            return container;
        }
    }
}
