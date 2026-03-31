using UnityEngine;
using System;
using static CropStatus;

public class LandPlot : MonoBehaviour
{
    public bool isOccupied = false;
    private CropDataSO currentCrop;
    private DateTime plantedTime;

    [Header("Care System")]
    public CropNeed currentNeed = CropNeed.None;
    public SpriteRenderer needIconRenderer;
    public Sprite waterSprite, pestSprite, weedSprite;

    private SpriteRenderer plantRenderer;
    public bool needHasSpawned = false; // Tránh spawn nhu cầu nhiều lần

    public CropDataSO GetCropData() => currentCrop;
    public DateTime GetPlantedTime() => plantedTime;

    void Awake()
    {
        Transform visualChild = transform.Find("PlantVisual");
        if (visualChild == null)
        {
            GameObject plantObj = new GameObject("PlantVisual");
            plantObj.transform.SetParent(this.transform);
            plantObj.transform.localPosition = new Vector3(0, 0, -0.1f);
            plantRenderer = plantObj.AddComponent<SpriteRenderer>();
        }
        else
        {
            plantRenderer = visualChild.GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        if (!isOccupied || currentCrop == null) return;

        float progress = GetGrowthProgress();

        // Cập nhật sprite theo giai đoạn
        UpdateVisualByProgress(progress);

        // Khi cây đạt 50% thì spawn nhu cầu chăm sóc (1 lần duy nhất)
        if (progress >= 0.5f && !needHasSpawned && currentNeed == CropNeed.None)
        {
            GenerateRandomNeed();
            needHasSpawned = true;
        }
    }

    // Tính tiến độ sinh trưởng theo thời gian thực
    public float GetGrowthProgress()
    {
        if (!isOccupied || currentCrop == null) return 0f;
        double elapsed = (DateTime.UtcNow - plantedTime).TotalSeconds;
        return Mathf.Clamp01((float)(elapsed / currentCrop.totalTimeToHarvest));
    }

    // Cây có thể thu hoạch khi đủ 100% VÀ không còn nhu cầu chăm sóc
    public bool CanHarvest()
    {
        if (!isOccupied || currentCrop == null) return false;
        return GetGrowthProgress() >= 1f && currentNeed == CropNeed.None;
    }

    public void Plant(SeedItemSO seed)
    {
        if (isOccupied) return;

        currentCrop = seed.cropData;
        plantedTime = DateTime.UtcNow;
        isOccupied = true;
        needHasSpawned = false;
        currentNeed = CropNeed.None;

        UpdateVisualByProgress(0f);
        UpdateNeedUI();
    }

    public void Harvest()
    {
        if (!CanHarvest())
        {
            Debug.Log("Chưa thể thu hoạch! Kiểm tra cây đã chín và đã chăm sóc chưa.");
            return;
        }
        Debug.Log($"Thu hoạch được: {currentCrop.yieldAmount} nông sản");
        ClearPlot();
    }

    // Chăm sóc thủ công — gọi khi player click vào cây ở đúng mode
    public void ApplyCare(CropNeed careType)
    {
        if (currentNeed == careType)
        {
            ClearNeed();
            Debug.Log($"Đã chăm sóc xong: {careType}");
        }
        else
        {
            Debug.Log($"Sai loại chăm sóc! Cây cần: {currentNeed}, bạn dùng: {careType}");
        }
    }

    public void ClearNeed()
    {
        currentNeed = CropNeed.None;
        UpdateNeedUI();
    }

    void GenerateRandomNeed()
    {
        currentNeed = (CropNeed)UnityEngine.Random.Range(1, 4);
        UpdateNeedUI();
        Debug.Log($"Cây cần chăm sóc: {currentNeed}");
    }

    void UpdateVisualByProgress(float progress)
    {
        if (currentCrop == null || currentCrop.growthStages.Length == 0) return;
        int index = Mathf.Clamp(
            Mathf.FloorToInt(progress * currentCrop.growthStages.Length),
            0,
            currentCrop.growthStages.Length - 1
        );
        if (plantRenderer != null)
            plantRenderer.sprite = currentCrop.growthStages[index];
    }

    public void UpdateNeedUI()
    {
        if (needIconRenderer == null) return;
        switch (currentNeed)
        {
            case CropNeed.Water: needIconRenderer.sprite = waterSprite; break;
            case CropNeed.PestControl: needIconRenderer.sprite = pestSprite; break;
            case CropNeed.Weeding: needIconRenderer.sprite = weedSprite; break;
            default: needIconRenderer.sprite = null; break;
        }
    }

    public void ClearPlot()
    {
        isOccupied = false;
        currentCrop = null;
        plantedTime = default;
        currentNeed = CropNeed.None;
        needHasSpawned = false;
        if (plantRenderer != null) plantRenderer.sprite = null;
        if (needIconRenderer != null) needIconRenderer.sprite = null;
    }

    // Dùng cho Save/Load và Swap
    public void SetData(bool occupied, CropDataSO data, DateTime time, CropNeed need, bool needSpawned)
    {
        isOccupied = occupied;
        currentCrop = data;
        plantedTime = time;
        currentNeed = need;
        needHasSpawned = needSpawned;

        if (isOccupied && currentCrop != null)
        {
            UpdateVisualByProgress(GetGrowthProgress());
            UpdateNeedUI();
        }
        else
        {
            if (plantRenderer != null) plantRenderer.sprite = null;
            if (needIconRenderer != null) needIconRenderer.sprite = null;
        }
    }
}