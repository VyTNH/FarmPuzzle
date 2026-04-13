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

            if (DataManager.Instance != null && DataManager.Instance.CurrentPlayer != null)
            {
                LoadGridState();
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleInteractionAtPos(Input.mousePosition);
            }
        }

        public void LoadGridState()
        {
            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null) return;

            string playerID = DataManager.Instance.CurrentPlayer.PlayerID;
            Debug.Log("[Grid] LoadGridState for: " + playerID);

            if (registeredSeeds == null || registeredSeeds.Length == 0)
            {
                registeredSeeds = Resources.LoadAll<SeedItemSO>("");
            }

            foreach (var plot in plots)
            {
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
                    SavePlotState(plot);
                }
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
                    // Tránh trường hợp Main Camera có Physics2DRaycaster gây hiểu nhầm EventSystem đang chặn UI
                    if (results[0].gameObject.GetComponent<LandPlot>() != null || (results[0].module != null && results[0].module.rootRaycaster is UnityEngine.EventSystems.Physics2DRaycaster))
                    {
                        // Đây là event bắt trúng Model 2D dưới đất, không phải UI => Bỏ qua phần check Block
                    }
                    else 
                    {
                        Debug.Log($"<color=red>[Grid - INPUT BLOCK]</color> Bị chặn bởi UI: <b>{results[0].gameObject.name}</b>");
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            // Bật QueryHitTriggers để bắt cả isTrigger collider (phòng ngừa)
            Physics2D.queriesHitTriggers = true;
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            Debug.Log($"<color=yellow>[Grid Raycast]</color> Ray origin={ray.origin:F2} dir={ray.direction:F2} hit={hit.collider?.gameObject.name ?? "NULL"}");
            
            if (hit.collider == null) return;

            LandPlot clickedPlot = hit.collider.GetComponent<LandPlot>();
            if (clickedPlot == null) return;

            Debug.Log($"<color=cyan>[Grid Interaction]</color> Clicked on: <b>{clickedPlot.plotID}</b>");
            
            if (clickedPlot.isLocked) { 
                if (FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController.Instance != null)
                    FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController.Instance.ShowConfirmPopup(clickedPlot);
                return; 
            }

            if (clickedPlot.isOccupied) { 
                HandleCareOrHarvest(clickedPlot); 
                return; 
            }

            if (selectedSeed == null) { 
                Debug.LogWarning("<color=orange>[Grid Interaction]</color> No Seed Selected!"); 
                return; 
            }

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
    }
}