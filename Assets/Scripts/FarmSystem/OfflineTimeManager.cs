using UnityEngine;
using System;
using FarmPuzzle.Core.Database;

public class OfflineTimeManager : MonoBehaviour
{
    void Start()
    {
        // Tính thời gian offline dựa trên DB (không dùng PlayerPrefs)
        CalculateOfflineTime();
    }

    void OnApplicationQuit()
    {
        SaveFarmStateToDB();
    }

    void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
            SaveFarmStateToDB();
    }

    private void CalculateOfflineTime()
    {
        // Chỉ tính khi đã đăng nhập
        if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null) return;

        // Lấy thời gian offline từ các ô đất đang trồng trong DB
        var tiles = DataManager.Instance.GetFarmTiles();
        foreach (var tile in tiles)
        {
            if (!string.IsNullOrEmpty(tile.PlantedSeedID) && tile.PlantTimeTicks > 0)
            {
                DateTime plantedTime = new DateTime(tile.PlantTimeTicks, DateTimeKind.Utc);
                double elapsed = (DateTime.UtcNow - plantedTime).TotalSeconds;
                Debug.Log($"[Offline] Ô {tile.TileID}: đã trồng {elapsed:F0}s trước.");
            }
        }
    }

    // Lưu trạng thái cây trồng vào SQLite (FARM_TILE) thay vì PlayerPrefs
    private void SaveFarmStateToDB()
    {
        if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null) return;

        LandPlot[] allPlots = FindObjectsByType<LandPlot>(FindObjectsSortMode.None);
        string playerID = DataManager.Instance.CurrentPlayer.PlayerID;

        foreach (var plot in allPlots)
        {
            string coords = plot.gameObject.name.Replace("LandPlot_", "");
            string tileID = $"tile_{playerID}_{coords}";

            var dbTile = DataManager.Instance.GetFarmTile(tileID);
            if (dbTile == null) continue;

            // Đồng bộ trạng thái từ Scene → DB
            if (plot.isLocked)
                dbTile.State = 0;
            else if (plot.isOccupied)
                dbTile.State = 2;
            else
                dbTile.State = 1;

            // Lưu thông tin cây trồng
            if (plot.isOccupied && plot.GetCropData() != null)
            {
                dbTile.PlantedSeedID = plot.plantedSeedID ?? "";
                dbTile.PlantTimeTicks = plot.GetPlantedTime().Ticks;
            }
            else
            {
                dbTile.PlantedSeedID = "";
                dbTile.PlantTimeTicks = 0;
            }

            DataManager.Instance.UpdateFarmTile(dbTile);
        }
        Debug.Log("Đã lưu trạng thái toàn bộ khu vườn vào SQLite!");
    }
}