using UnityEngine;
using UnityEngine.InputSystem;
using static CropStatus;

public class GridManager : MonoBehaviour
{
    [Header("Setup")]
    public GameObject plotPrefab;
    public int width = 10;
    public int height = 10;

    [Header("Trồng cây")]
    public SeedItemSO selectedSeed;
    public SeedItemSO[] registeredSeeds; // Tất cả hạt giống SO (nạp bởi SceneGenerator)

    [Header("Chăm sóc thủ công")]
    [Tooltip("Tool đang cầm trên tay. None = không chăm sóc")]
    public CropNeed selectedCareTool = CropNeed.None;

    void Start()
    {
        // Ưu tiên quét các ô đất đã có sẵn trong Scene (do Tool hoặc Dev đặt tay)
        LandPlot[] existingPlots = FindObjectsByType<LandPlot>(FindObjectsSortMode.None);
        if (existingPlots.Length > 0)
        {
            Debug.Log($"[GridManager] Phát hiện {existingPlots.Length} ô đất có sẵn → Bỏ qua GenerateGrid.");
            return;
        }

        // Không có ô đất nào trong Scene → Tự sinh lưới mới từ Prefab
        GenerateGrid();
    }

    void GenerateGrid()
    {
        if (plotPrefab == null)
        {
            Debug.LogWarning("[GridManager] plotPrefab trống VÀ không có ô đất sẵn → Không thể tạo nông trại!");
            return;
        }
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                Instantiate(plotPrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
        
        Debug.Log($"[GridManager] Đã sinh lưới đất mới {width}x{height} từ Prefab.");
    }

    // Đọc bảng FARM_TILE từ SQLite và đồng bộ trạng thái cho từng ô đất
    public void LoadFarmFromDB()
    {
        if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null)
        {
            Debug.LogWarning("[GridManager] Chưa đăng nhập! Không thể đọc FARM_TILE.");
            return;
        }

        var tiles = DataManager.Instance.GetFarmTiles();
        LandPlot[] allPlots = FindObjectsByType<LandPlot>(FindObjectsSortMode.None);

        Debug.Log($"[GridManager] Đọc DB: {tiles.Count} dòng FARM_TILE | Scene: {allPlots.Length} ô đất");

        int matched = 0;
        foreach (var plot in allPlots)
        {
            // Ghép tên ô đất trong Scene (LandPlot_x_y) với TileID trong DB (tile_playerID_x_y)
            string plotName = plot.gameObject.name; // VD: "LandPlot_2_3"
            string coords = plotName.Replace("LandPlot_", ""); // VD: "2_3"
            string expectedTileID = $"tile_{DataManager.Instance.CurrentPlayer.PlayerID}_{coords}";

            var dbTile = tiles.Find(t => t.TileID == expectedTileID);
            if (dbTile != null)
            {
                matched++;
                
                // Đồng bộ trạng thái KHÓA/MỞ
                if (dbTile.State == 0)
                    plot.LockPlot();
                else
                    plot.UnlockPlot();

                // KHÔI PHỤC CÂY TRỒNG từ DB (nếu có)
                if (!string.IsNullOrEmpty(dbTile.PlantedSeedID) && dbTile.PlantTimeTicks > 0)
                {
                    SeedItemSO seed = FindSeedByID(dbTile.PlantedSeedID);
                    if (seed != null && seed.cropData != null)
                    {
                        System.DateTime plantedTime = new System.DateTime(dbTile.PlantTimeTicks, System.DateTimeKind.Utc);
                        plot.plantedSeedID = dbTile.PlantedSeedID;
                        plot.SetData(true, seed.cropData, plantedTime, CropNeed.None, false);
                        Debug.Log($"  🌱 {plotName}: Khôi phục cây {seed.seedName}");
                    }
                }
            }
            else
            {
                plot.UnlockPlot();
                Debug.LogWarning($"  ⚠ {plotName}: Không tìm thấy DB ({expectedTileID}) → Mở mặc định");
            }
        }

        Debug.Log($"<color=green>[GridManager] Đồng bộ HOÀN TẤT! Khớp {matched}/{allPlots.Length} ô đất.</color>");
    }

    // Tra cứu SeedItemSO theo seedID
    private SeedItemSO FindSeedByID(string seedID)
    {
        if (registeredSeeds == null) return null;
        foreach (var s in registeredSeeds)
        {
            if (s != null && s.seedID == seedID) return s;
        }
        return null;
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            HandleInteraction();
    }

    void HandleInteraction()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider == null) return;

        LandPlot clickedPlot = hit.collider.GetComponent<LandPlot>();
        if (clickedPlot == null) return;

        // --- Ô đất trống → Trồng cây (Bước 1) ---
        if (!clickedPlot.isOccupied)
        {
            if (selectedSeed != null && !string.IsNullOrEmpty(selectedSeed.seedID))
            {
                // Gọi DataManager check xem kho còn hạt giống này không (Trừ luôn 1 hạt)
                if (DataManager.Instance != null && DataManager.Instance.RemoveItem(selectedSeed.seedID, 1))
                {
                    clickedPlot.Plant(selectedSeed);
                    Debug.Log($"Đã trồng: {selectedSeed.seedName} (-1 trong kho)");
                }
                else
                {
                    Debug.LogWarning($"Kho chứa của bạn không còn đủ: {selectedSeed.seedName} để trồng!");
                }
            }
            return;
        }

        // --- Ô đã có cây ---

        // Nếu đang cầm tool chăm sóc → chăm sóc
        if (selectedCareTool != CropNeed.None)
        {
            clickedPlot.ApplyCare(selectedCareTool);
            return;
        }

        // Nếu không cầm tool → thử thu hoạch
        if (clickedPlot.CanHarvest())
        {
            clickedPlot.Harvest();
        }
        else
        {
            // Thông báo lý do chưa harvest được
            float progress = clickedPlot.GetGrowthProgress();
            if (progress < 1f)
                Debug.Log($"Cây chưa chín! Tiến độ: {(progress * 100f):F0}%");
            else if (clickedPlot.currentNeed != CropNeed.None)
                Debug.Log($"Cây cần chăm sóc trước: {clickedPlot.currentNeed}");
        }
    }
}