using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using FarmPuzzle.FarmSystem.Crop;

namespace FarmPuzzle.FarmSystem
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance;

        public List<LandPlot> plots = new List<LandPlot>();
        public SeedItemSO selectedSeed; 
        public SeedItemSO[] registeredSeeds; 
        public CropNeedType selectedCareTool = CropNeedType.None;

        private void Awake() { Instance = this; }

        private void Start()
        {
            if (plots.Count == 0) plots.AddRange(FindObjectsByType<LandPlot>(FindObjectsSortMode.None));

            // LoadGridState se duoc goi sau khi login tu UI hoặc neu da login
            if (DataManager.Instance != null && DataManager.Instance.CurrentPlayer != null)
            {
                LoadGridState();
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Kiểm tra xem có đang click đè lên UI thật không
                if (UnityEngine.EventSystems.EventSystem.current != null && 
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                    {
                        position = Input.mousePosition
                    };
                    var results = new List<UnityEngine.EventSystems.RaycastResult>();
                    UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);
                    
                    if (results.Count > 0)
                    {
                        // CHỈ CHẶN nếu object trúng Raycast nằm ở Layer "UI" (thường là layer 5)
                        // Hoặc sếp có thể kiểm tra nếu nó không phải là một LandPlot
                        int uiLayer = LayerMask.NameToLayer("UI");
                        if (results[0].gameObject.layer == uiLayer)
                        {
                            Debug.Log($"<color=yellow>[Grid]</color> Click bị chặn bởi UI thật: <b>{results[0].gameObject.name}</b> (Layer UI)");
                            return;
                        }
                        else
                        {
                            // Nếu trúng cái gì đó không phải layer UI (như chính ô đất), ta vẫn cho qua
                            Debug.Log($"<color=white>[Grid]</color> Click trúng <b>{results[0].gameObject.name}</b> qua EventSystem nhưng không phải Layer UI. Tiếp tục xử lý...");
                        }
                    }
                }

                HandleInteraction();
            }
        }

        public void LoadGridState()
        {
            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null) return;

            string playerID = DataManager.Instance.CurrentPlayer.PlayerID;
            Debug.Log("[Grid] Bat dau dong bo du lieu tu SQLite cho nguoi choi: " + playerID);

            // Moi: Tu dong nap danh sach hat giong neu dang trong
            if (registeredSeeds == null || registeredSeeds.Length == 0)
            {
                registeredSeeds = Resources.LoadAll<SeedItemSO>("");
                Debug.Log("[Grid] Da tu dong nap " + registeredSeeds.Length + " loai hat giong tu Resources.");
            }

            foreach (var plot in plots)
            {
                // Chuan hoa ID giong nhu OfflineTimeManager va DataManager
                string coords = plot.gameObject.name.Replace("LandPlot_", "");
                plot.plotID = "tile_" + playerID + "_" + coords;

                var dbTile = DataManager.Instance.GetFarmTile(plot.plotID);
                if (dbTile != null)
                {
                    bool isLocked = dbTile.State == 0;
                    SeedItemSO foundSeed = null;
                    if (registeredSeeds != null && !string.IsNullOrEmpty(dbTile.PlantedSeedID))
                        foundSeed = System.Array.Find(registeredSeeds, s => s != null && s.seedID == dbTile.PlantedSeedID);

                    plot.SetData(dbTile.TileID, isLocked, foundSeed, dbTile.PlantTimeTicks);
                }
                else 
                {
                    Debug.LogWarning("[Grid] Khong tim thay du lieu cho " + plot.plotID + ". Khoi tao moi.");
                    SavePlotState(plot);
                }
            }
        }

        public void SavePlotState(LandPlot plot)
        {
            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null) return;
            
            // Sử dụng UpdateFarmTile mới đã được nâng cấp với InsertOrReplace
            DataManager.Instance.UpdateFarmTile(new FarmPuzzle.Core.Database.FarmTileModel {
                TileID = plot.plotID,
                PlayerID = DataManager.Instance.CurrentPlayer.PlayerID,
                State = plot.isLocked ? 0 : 1,
                PlantedSeedID = plot.isOccupied ? plot.plantedSeedID : "",
                PlantTimeTicks = plot.isOccupied ? plot.plantedTime.Ticks : 0
            });
        }

        public void SaveGridToDB()
        {
            foreach (var plot in plots) SavePlotState(plot);
        }

        private void HandleInteraction()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            
            if (hit.collider == null) 
            {
                Debug.Log($"<color=white>[Grid Interaction]</color> Click vào khoảng không (Raycast không trúng collider nào tại {ray.origin}).");
                return;
            }

            LandPlot clickedPlot = hit.collider.GetComponent<LandPlot>();
            if (clickedPlot == null) 
            {
                Debug.Log($"<color=white>[Grid Interaction]</color> Click trúng vật thể <b>{hit.collider.name}</b> nhưng không có script LandPlot.");
                return;
            }

            Debug.Log($"<color=cyan>[Grid Interaction]</color> Người chơi click vào ô: <b>{clickedPlot.plotID}</b>");

            if (clickedPlot.isLocked) { 
                Debug.LogWarning($"<color=orange>[Grid Interaction]</color> Ô '{clickedPlot.plotID}' đang BỊ KHÓA! Cần giải đố để mở."); 
                if (FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController.Instance != null)
                    FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController.Instance.ShowConfirmPopup(clickedPlot);
                return; 
            }

            if (clickedPlot.isOccupied) { 
                Debug.Log($"<color=cyan>[Grid Interaction]</color> Ô '{clickedPlot.plotID}' đã có cây. Kiểm tra nhu cầu chăm sóc hoặc thu hoạch.");
                HandleCareOrHarvest(clickedPlot); 
                return; 
            }

            if (selectedSeed == null) { 
                Debug.LogWarning("<color=orange>[Grid Interaction]</color> Sếp chưa CHỌN loại hạt giống để gieo!"); 
                return; 
            }

            Debug.Log($"<color=cyan>[Grid Interaction]</color> Đang thử gieo hạt: <b>{selectedSeed.seedName}</b> (ID: {selectedSeed.seedID})");
            
            // Nếu sếp đang ở trạng thái Test, tôi sẽ tự động nạp đạn cho sếp nếu kho trống
            bool hasItem = DataManager.Instance != null && DataManager.Instance.RemoveItem(selectedSeed.seedID, 1);
            if (!hasItem && DataManager.Instance != null)
            {
                Debug.Log($"<color=yellow>[Grid Interaction]</color> Kho hết hạt giống <b>{selectedSeed.seedName}</b>. Tự động nạp 1 hạt để sếp Test thuận tiện.");
                DataManager.Instance.AddItem(selectedSeed.seedID, 1);
                hasItem = DataManager.Instance.RemoveItem(selectedSeed.seedID, 1);
            }

            if (hasItem)
            {
                Debug.Log($"<color=cyan>[Grid Interaction]</color> Đã trừ 1 hạt giống từ kho. Bắt đầu gọi logic Plant trên ô {clickedPlot.plotID}...");
                bool success = clickedPlot.Plant(selectedSeed);
                if (success) 
                {
                    SavePlotState(clickedPlot);
                    Debug.Log($"<color=green>[Grid Interaction]</color> <b>THÀNH CÔNG!</b> Đã gieo {selectedSeed.seedName} và lưu vào database.");
                }
                else 
                {
                    Debug.LogError($"<color=red>[Grid Interaction]</color> <b>THẤT BẠI!</b> Logic Plant của LandPlot trả về false.");
                }
            }
            else
            {
                Debug.LogError($"<color=red>[Grid Interaction]</color> Không thể gieo hạt. Kho đồ trống và chế độ tự nạp đạn gặp lỗi.");
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
    }
}