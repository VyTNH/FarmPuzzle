using UnityEngine;
using System.Collections;
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

        // ─── OBJECT POOL ───
        private Queue<LandPlot> _tilePool = new Queue<LandPlot>();
        private int _tilesToSpawnRemaining = 0;
        private int _spawnBatchCounter = 0;
        [SerializeField] private int poolBatchSize = DEFAULT_POOL_BATCH_SIZE;

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
            StartCoroutine(GenerateGridWithPooling());
        }

        // ═════════════════════════════════════════
        // TRÌNH TẠO MÀN CHƠI VỚI OBJECT POOLING (DATA-DRIVEN RENDERER)
        // ═════════════════════════════════════════
        private IEnumerator GenerateGridWithPooling()
        {
            if (_parentGrid == null) _parentGrid = GetComponent<Grid>();
            if (tilePrefabs == null || tilePrefabs.Length < 12) {
                Debug.LogWarning("⚠️ Hãy nạp đủ 12 Tile Prefabs vào GridManager trước khi Render!");
                yield break;
            }

            // Dọn rác
            foreach (Transform child in transform) {
                if (child.GetComponent<LandPlot>() != null) Destroy(child.gameObject);
            }
            plots.Clear();
            ClearPool();

            string playerID = "local_player";
            if (DataManager.Instance != null && DataManager.Instance.CurrentPlayer != null)
                playerID = DataManager.Instance.CurrentPlayer.PlayerID;

            int totalTiles = gridWidth * gridHeight;
            _tilesToSpawnRemaining = totalTiles;
            _spawnBatchCounter = 0;

            // Spawn tiles in batches to avoid frame drops
            while (_tilesToSpawnRemaining > 0)
            {
                int spawnedThisFrame = 0;
                int batchEnd = Mathf.Min(_spawnBatchCounter + poolBatchSize, totalTiles);

                for (int i = _spawnBatchCounter; i < batchEnd; i++)
                {
                    int x = i / gridWidth;
                    int y = i % gridWidth;

                    LandPlot lp = GetTileFromPool();
                    lp.gameObject.SetActive(true);
                    lp.gameObject.name = $"LandPlot_{x}_{y}";
                    lp.transform.SetParent(transform, true); // worldPositionStays = true for existing pooled objects

                    // Căn Tọa Độ Chuẩn Isometric (dùng local position vì parent đã set)
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    lp.transform.localPosition = _parentGrid.GetCellCenterWorld(cellPos);
                    lp.transform.localScale = Vector3.one;

                    // XỬ LÝ CHỐNG ĐÈ HÌNH THEO TỌA ĐỘ
                    SpriteRenderer sr = lp.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = -(x + y);

                    lp.plotID = "tile_" + playerID + "_" + x + "_" + y;
                    lp.gridX = x;
                    lp.gridY = y;
                    lp.isLocked = true;

                    plots.Add(lp);
                    spawnedThisFrame++;
                    _tilesToSpawnRemaining--;
                }

                _spawnBatchCounter = batchEnd;

                // Yield một frame giữa các batch để tránh frame drop
                if (_tilesToSpawnRemaining > 0)
                    yield return null;
            }

            LoadGridState();

            // Kích hoạt Viewport Culling nếu được gắn (tối ưu 100x100)
            var culler = GetComponent<FarmViewportCuller>();
            if (culler != null) culler.RegisterPlots(plots);
        }

        // ─── POOL HELPERS ───
        private LandPlot GetTileFromPool()
        {
            if (_tilePool.Count > 0)
            {
                LandPlot pooled = _tilePool.Dequeue();
                // Reset pooled tile state
                pooled.isOccupied = false;
                pooled.isLocked = false;
                pooled.ClearPlot();
                return pooled;
            }

            // Pool empty - instantiate new tile
            GameObject newTileGo = Instantiate(tilePrefabs[0], transform);
            LandPlot lp = newTileGo.GetComponent<LandPlot>();
            if (lp == null) lp = newTileGo.AddComponent<LandPlot>();
            return lp;
        }

        private void ReturnTileToPool(LandPlot plot)
        {
            if (plot == null) return;
            plot.gameObject.SetActive(false);
            _tilePool.Enqueue(plot);
        }

        private void ClearPool()
        {
            _tilePool.Clear();
        }

        /// <summary>
        /// Return tất cả plots vào pool (dùng khi cần reset toàn bộ grid)
        /// </summary>
        public void ReturnAllPlotsToPool()
        {
            foreach (var plot in plots)
            {
                if (plot != null && plot.gameObject != null)
                    ReturnTileToPool(plot);
            }
            plots.Clear();
        }

        private float _lastClickTime = 0f;
        private const float DOUBLE_CLICK_INTERVAL = 0.35f;

        // ─── TILE INDEX CONSTANTS ───
        private const int WILD_TILE_MIN = 0;
        private const int WILD_TILE_MAX = 6;
        private const int DIRT_TILE_MIN = 6;
        private const int DIRT_TILE_MAX = 12;
        private const int DEFAULT_UNLOCK_TILE_INDEX = 6;

        // ─── POOLING CONSTANTS ───
        private const int DEFAULT_POOL_BATCH_SIZE = 100;

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

                // Dùng gridX/gridY đã lưu sẵn thay vì parse string
                int px = plot.gridX;
                int py = plot.gridY;

                UnityEngine.Random.InitState(px * 123 + py * 456);
                int wildTileIndex = UnityEngine.Random.Range(WILD_TILE_MIN, WILD_TILE_MAX);
                int dirtTileIndex = UnityEngine.Random.Range(DIRT_TILE_MIN, DIRT_TILE_MAX);

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

        /// <summary>
        /// Kiểm tra xem GameObject bị UI Raycast hit có thuộc về Crop Canvas hay không.
        /// Nếu đúng thì bỏ qua (xuyên qua) để Physics2D Raycast vẫn chạm được LandPlot bên dưới.
        /// </summary>
        private bool IsCropUIElement(GameObject go)
        {
            if (go == null) return false;
            // Đi ngược lên cây hierarchy để tìm LandPlot (cây chứa Crop_UI_Canvas)
            Transform t = go.transform;
            int depth = 0;
            while (t != null && depth < 8)
            {
                if (t.GetComponent<LandPlot>() != null) return true;   // thuộc LandPlot → cho xuyên qua
                if (t.GetComponent<CropGrowth>() != null) return true;  // thuộc CropGrowth → cho xuyên qua
                t = t.parent;
                depth++;
            }
            return false;
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
                    // Duyệt tất cả kết quả UI:
                    // - Nếu gặp LandPlot hoặc Physics2DRaycaster → cho qua (tile 2D)
                    // - Nếu gặp UI thuộc Crop Canvas → cũng cho qua (xuyên qua crop UI)
                    // - Nếu gặp UI thật sự của HUD/Menu → chặn lại
                    bool blocked = false;
                    foreach (var result in results)
                    {
                        if (result.gameObject.GetComponent<LandPlot>() != null) break; // tile → cho qua
                        if (result.module != null && result.module.rootRaycaster is UnityEngine.EventSystems.Physics2DRaycaster) break; // physics → cho qua
                        if (IsCropUIElement(result.gameObject)) continue; // crop UI → bỏ qua, kiểm tra tiếp
                        // Đây là UI thật sự (HUD, Button, Panel...) → chặn
                        blocked = true;
                        break;
                    }
                    if (blocked) return;
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