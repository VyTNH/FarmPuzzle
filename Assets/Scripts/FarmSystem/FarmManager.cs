using UnityEngine;
using System;

public class FarmManager : MonoBehaviour
{
    // Giả lập một ô đất đơn giản
    public bool isOccupied = false;
    private CropDataSO currentCrop;
    private float plantTime;

    public void PlantSeed(SeedItemSO seed)
    {
        if (isOccupied) return;

        currentCrop = seed.cropData;
        plantTime = Time.time;
        isOccupied = true;
        Debug.Log($"Da trong {seed.seedName}");
    }

    public float GetGrowthProgress()
    {
        if (!isOccupied) return 0;

        float elapsed = Time.time - plantTime;
        return Mathf.Clamp01(elapsed / currentCrop.totalTimeToHarvest);
    }

    public void HarvestCrop()
    {
        if (isOccupied && GetGrowthProgress() >= 1.0f)
        {
            Debug.Log($"Thu hoach đc {currentCrop.yieldAmount} nong san");
            isOccupied = false;
            currentCrop = null;
        }
        else
        {
            Debug.Log("k the thu hoach bay gio");
        }
    }
}