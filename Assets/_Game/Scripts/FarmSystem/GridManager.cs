using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using FarmPuzzle.FarmSystem.Crop;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FarmPuzzle.FarmSystem
{
    [RequireComponent(typeof(Grid))]
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance;

        [Header("Data-Driven Config (12 Tile Prefabs)")]
        public int gridWidth = 50;
        public int gridHeight = 50;
        [Tooltip("Thả 12 prefabs Spritesheet từ 0 tới 11 vào đây")]
        public GameObject[] tilePrefabs; 

        [Header("Runtime State")]
        public List<LandPlot> plots = new List<LandPlot>();
        public SeedItemSO selectedSeed; 
        public SeedItemSO[] registeredSeeds; 
        public CropNeedType selectedCareTool = CropNeedType.None;

        private Grid _parentGrid;

        private void Awake() { Instance = this; }

        private async void Start()
        {
            _parentGrid = GetComponent<Grid>();
            if (_parentGrid == null) Debug.LogError("GridManager requires a Grid component!");

            // ĐỢI DATAMANAGER LOAD XONG TÀI KHOẢN RỒI MỚI SINH FARM
            while (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null)
            {
                await System.Threading.Tasks.Task.Yield();
                if (this == null) return; // Bảo vệ nếu Object bị hủy lúc chờ
            }

            // KHÔNG CÒN HARD-CODE: Xóa sạch map cũ và Render Bản đồ theo Data
            GenerateGridRuntime();
            LoadGridState();

            // Kích hoạt Viewport Culling nếu được gắn (tối ưu 100x100)
            var culler = GetComponent<FarmViewportCuller>();
            if (culler != null) culler.RegisterPlots(plots);
        }

        private float _lastClickTime = 0f;
        private const float DOUBLE_CLICK_INTERVAL = 0.35f;

        private void Update()
        {
            // Kiểm tra trên MouseButtonUp để biết chắc CHƯA TỪNG kéo (Dragging)
            if (Input.GetMouseButtonUp(0))
            {
                // Nếu đang/vừa kéo camera xong thì bỏ qua click
                if (CameraDrag.IsDragging) return;

                float now = Time.unscaledTime;
                if (now - _lastClickTime <= DOUBLE_CLICK_INTERVAL)
                {
                    // Xác nhận Double Click
                    HandleInteractionAtPos(Input.mousePosition);
                    _lastClickTime = 0f; 
                }
                else
                {
                    _lastClickTime = now;
                }
            }
        }


        // ═════════════════════════════════════════
        // TRÌNH TẠO MÀN CHƠI (DATA-DRIVEN RENDERER)
        // ═════════════════════════════════════════
        private void GenerateGridRuntime()
        {
            if (_parentGrid == null) _parentGrid = GetComponent<Grid>();
            if (tilePrefabs == null || tilePrefabs.Length < 12) {
                Debug.LogWarning("⚠️ Hãy nạp đủ 12 Tile Prefabs vào GridManager trước khi Render!");
                return;
            }

            // Dọn rác
            foreach (Transform child in transform) {
                if (child.GetComponent<LandPlot>() != null) Destroy(child.gameObject);
            }
            plots.Clear();

            string playerID = "local_player"; // Cũ
            if (DataManager.Instance != null && DataManager.Instance.CurrentPlayer != null)
                playerID = DataManager.Instance.CurrentPlayer.PlayerID;

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    // Lấy Tile Mặc Định là Tile 0 (Khóa / Cỏ)
                    GameObject newTileGo = Instantiate(tilePrefabs[0], transform);
                    newTileGo.name = $"LandPlot_{x}_{y}";
                    
                    // Căn Tọa Độ Chuẩn Isometric
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    newTileGo.transform.position = _parentGrid.GetCellCenterWorld(cellPos);
                    
                    // XỬ LÝ CHỐNG ĐÈ HÌNH THEO TỌA ĐỘ
                    SpriteRenderer sr = newTileGo.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = -(x + y);

                    // Add components
                    LandPlot lp = newTileGo.GetComponent<LandPlot>();
                    if (lp == null) lp = newTileGo.AddComponent<LandPlot>();
                    
                    lp.plotID = "tile_" + playerID + "_" + x + "_" + y;
                    lp.isLocked = true; // SỬA LỖI: Mặc định phải là KHÓA
                    
                    plots.Add(lp);
                }
            }
            // Debug.Log($"<color=green>✅ Rendered {gridWidth * gridHeight} plots Data-Driven for {playerID}.</color>");
        }

        [Header("Puzzle Config")]
        [Tooltip("Cấp độ mở đất: xếp từ Dễ (0) đến Khó (N)")]
        public FarmPuzzle.LandPuzzle.Data.LevelData[] availablePuzzleLevels;

        // ═════════════════════════════════════════
        // DATABASE ĐỒNG BỘ HÓA LOGIC & HÌNH ẢNH
        // ═════════════════════════════════════════
        public void LoadGridState()
        {
            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null) return;
            if (registeredSeeds == null || registeredSeeds.Length == 0)
                registeredSeeds = Resources.LoadAll<SeedItemSO>("");

            // Debug.Log("<color=cyan>[Giai Đoạn 1: Đọc DB Sinh Nông Trại]</color> Chuẩn bị nạp dữ liệu SQLite và rải Plot lên màn hình...");

            // 🚀 TỐI ƯU HÓA 100x100: Kéo toàn bộ Database Tile 1 lần duy nhất thay vì 10.000 lần
            var allDbTiles = DataManager.Instance.GetFarmTiles();
            var tileDict = new System.Collections.Generic.Dictionary<string, FarmPuzzle.Core.Database.FarmTileModel>();
            if (allDbTiles != null) {
                foreach(var t in allDbTiles) {
                    tileDict[t.TileID] = t;
                }
            }

            // Đo tọa độ tâm để tính độ khó
            int centerX = gridWidth / 2;
            int centerY = gridHeight / 2;

            foreach (var plot in plots)
            {
                FarmPuzzle.Core.Database.FarmTileModel dbTile = null;
                tileDict.TryGetValue(plot.plotID, out dbTile);
                
                // --- TÍNH TỌA ĐỘ VÀ RANDOM TILE ---
                string[] parts = plot.gameObject.name.Split('_');
                int px = 0, py = 0;
                if (parts.Length >= 3) {
                    int.TryParse(parts[1], out px);
                    int.TryParse(parts[2], out py);
                }
                
                UnityEngine.Random.InitState(px * 123 + py * 456);
                int wildTileIndex = UnityEngine.Random.Range(0, 6);   // Tile cỏ dại & Rêu (0 -> 5)
                int dirtTileIndex = UnityEngine.Random.Range(6, 12);  // Tile đất trồng (6 -> 11)

                // --- GÁN PUZZLE LỖI (Vòng tròn từ trong ra ngoài) ---
                if (availablePuzzleLevels != null && availablePuzzleLevels.Length > 0)
                {
                    // Công thức tính khoảng cách so với tâm (distance lớn -> viền ngoài -> càng khó)
                    int distance = Mathf.Max(Mathf.Abs(px - centerX), Mathf.Abs(py - centerY));
                    int levelIndex = Mathf.Clamp(distance, 0, availablePuzzleLevels.Length - 1);
                    plot.puzzleLevel = availablePuzzleLevels[levelIndex];
                }
                
                // --- THIẾT LẬP GRAPHIC VÀ DATA ---
                if (dbTile != null)
                {
                    bool isLocked = dbTile.State == 0;
                    SeedItemSO foundSeed = null;
                    if (registeredSeeds != null && !string.IsNullOrEmpty(dbTile.PlantedSeedID))
                        foundSeed = System.Array.Find(registeredSeeds, s => s != null && s.seedID == dbTile.PlantedSeedID);

                    plot.SetData(dbTile.TileID, isLocked, foundSeed, dbTile.PlantTimeTicks);
                    ChangeTileArtState(plot, isLocked ? wildTileIndex : dirtTileIndex);
                }
                else 
                {
                    SavePlotState(plot); 
                    ChangeTileArtState(plot, wildTileIndex); 
                }
            }
            // Debug.Log("<color=cyan>[Giai Đoạn 1: Đọc DB Sinh Nông Trại]</color> Đã nạp và render và đồng bộ xong Farm Grid!");
        }

        public void ChangeTileArtState(LandPlot plot, int spritePrefabIndex)
        {
             if (tilePrefabs == null || spritePrefabIndex < 0 || spritePrefabIndex >= tilePrefabs.Length) return;
             
             SpriteRenderer targetSr = plot.GetComponent<SpriteRenderer>();
             SpriteRenderer sourceSr = tilePrefabs[spritePrefabIndex].GetComponent<SpriteRenderer>();
             
             if (targetSr != null && sourceSr != null) {
                 targetSr.sprite = sourceSr.sprite;
             }
        }

        public void SavePlotState(LandPlot plot)
        {
            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null) return;
            
            DataManager.Instance.UpdateFarmTile(new FarmPuzzle.Core.Database.FarmTileModel {
                TileID = plot.plotID,
                PlayerID = DataManager.Instance.CurrentPlayer.PlayerID,
                State = plot.isLocked ? 0 : 1,
                PlantedSeedID = plot.isOccupied ? plot.plantedSeedID : "",
                PlantTimeTicks = plot.isOccupied ? plot.plantedTime.Ticks : 0
            });
        }

        public void HandleInteractionAtPos(Vector2 screenPos)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = screenPos };
                var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);
                
                if (results.Count > 0)
                {
                    if (results[0].gameObject.GetComponent<LandPlot>() != null || (results[0].module != null && results[0].module.rootRaycaster is UnityEngine.EventSystems.Physics2DRaycaster)) { /* Xuyên qua Tile 2D */ }
                    else { return; } // Bị đè UI
                }
                else return;
            }

            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            Physics2D.queriesHitTriggers = true;
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            
            if (hit.collider == null) return;

            LandPlot clickedPlot = hit.collider.GetComponent<LandPlot>();
            if (clickedPlot == null) return;
            
            if (clickedPlot.isLocked) { 
                if (FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController.Instance != null)
                    FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController.Instance.ShowConfirmPopup(clickedPlot);
                // TEST: Thử tự động MỞ KHÓA nếu Admin test
                // ChangeTileArtState(clickedPlot, 6);
                return; 
            }

            if (clickedPlot.isOccupied) { 
                HandleCareOrHarvest(clickedPlot); 
                return; 
            }

            if (selectedSeed == null) return; 

            bool hasItem = DataManager.Instance != null && DataManager.Instance.RemoveItem(selectedSeed.seedID, 1);
            if (!hasItem && DataManager.Instance != null)
            {
                DataManager.Instance.AddItem(selectedSeed.seedID, 1);
                hasItem = DataManager.Instance.RemoveItem(selectedSeed.seedID, 1);
            }

            if (hasItem)
            {
                if (clickedPlot.Plant(selectedSeed)) SavePlotState(clickedPlot);
            }
        }

        private void HandleCareOrHarvest(LandPlot plot)
        {
             if (selectedCareTool != CropNeedType.None) {
                plot.ApplyCare(selectedCareTool);
                SavePlotState(plot);
                return;
            }
            if (plot.CanHarvest()) {
                plot.Harvest();
                SavePlotState(plot);
            }
        }

        // ═════════════════════════════════════════
        // EDITOR TOOL: Chỉnh Rộng / Hẹp Màn Chơi Chữa Cháy
        // ═════════════════════════════════════════
#if UNITY_EDITOR
        [ContextMenu("✨ [Dev] Mở Khoá Toàn Bộ (Test)")]
        public void AdminUnlockAll()
        {
            foreach (var plot in plots) {
                plot.isLocked = false;
                ChangeTileArtState(plot, 6); // Unlock to Dirt
                SavePlotState(plot);
            }
        }
#endif
    }
}