using UnityEngine;
using System;
using static CropStatus;

public class LandPlot : MonoBehaviour
{
    public bool isOccupied = false;
    public bool isLocked = false;
    public string plantedSeedID = ""; // ID hạt giống đã gieo (để lưu/load từ DB)
    private CropDataSO currentCrop;
    private DateTime plantedTime;

    [Header("Care System")]
    public CropNeed currentNeed = CropNeed.None;
    public SpriteRenderer needIconRenderer;
    public Sprite waterSprite, pestSprite, weedSprite;

    private SpriteRenderer plantRenderer;
    private SpriteRenderer tileRenderer; // Renderer của chính ô đất (để đổi màu khóa/mở)
    private Color originalTileColor;
    public bool needHasSpawned = false;

    public CropDataSO GetCropData() => currentCrop;
    public DateTime GetPlantedTime() => plantedTime;

    void Awake()
    {
        // Lấy renderer của chính ô đất để đổi màu khi lock/unlock
        tileRenderer = GetComponent<SpriteRenderer>();
        if (tileRenderer == null)
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                originalTileColor = meshRenderer.material.color;
        }
        else
        {
            originalTileColor = tileRenderer.color;
        }

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

        // Cập nhật visual khóa ngay khi Awake
        ApplyLockVisual();
    }

    // ======= HỆ THỐNG KHÓA Ô ĐẤT (UC3 - Land Puzzle) =======
    public void LockPlot()
    {
        isLocked = true;
        ApplyLockVisual();
    }

    public void UnlockPlot()
    {
        isLocked = false;
        ApplyLockVisual();
        Debug.Log($"<color=yellow>[Mở đất]</color> Ô {gameObject.name} đã được mở khóa!");
    }

    private void ApplyLockVisual()
    {
        // Ô khóa → tối xám đi, ô mở → trả về màu gốc
        Color targetColor = isLocked ? new Color(0.25f, 0.22f, 0.2f, 1f) : originalTileColor;
        
        if (tileRenderer != null)
        {
            tileRenderer.color = targetColor;
        }
        else
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderer.material.color = targetColor;
        }
    }

    // ======= END KHÓA =======

    void Update()
    {
        if (!isOccupied || currentCrop == null) return;

        float progress = GetGrowthProgress();
        UpdateVisualByProgress(progress);

        if (progress >= 0.5f && !needHasSpawned && currentNeed == CropNeed.None)
        {
            GenerateRandomNeed();
            needHasSpawned = true;
        }
    }

    public float GetGrowthProgress()
    {
        if (!isOccupied || currentCrop == null) return 0f;
        double elapsed = (DateTime.UtcNow - plantedTime).TotalSeconds;
        return Mathf.Clamp01((float)(elapsed / currentCrop.totalTimeToHarvest));
    }

    public bool CanHarvest()
    {
        if (!isOccupied || currentCrop == null) return false;
        return GetGrowthProgress() >= 1f && currentNeed == CropNeed.None;
    }

    public void Plant(SeedItemSO seed)
    {
        if (isOccupied) return;
        if (isLocked)
        {
            Debug.LogWarning($"Ô đất {gameObject.name} đang bị khóa! Hãy chơi Land Puzzle để mở.");
            return;
        }

        currentCrop = seed.cropData;
        plantedSeedID = seed.seedID; // Ghi nhớ ID hạt giống để lưu DB
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

        // --- LIÊN KẾT DATAMANAGER & QUESTMANAGER (Bước 2) ---
        if (DataManager.Instance != null && currentCrop != null && !string.IsNullOrEmpty(currentCrop.productID))
        {
            // 1. Thêm thu hoạch vào tuí đồ (Database)
            DataManager.Instance.AddItem(currentCrop.productID, currentCrop.yieldAmount);
            
            // 2. Chuyển thông tin cho QuestManager để tính điểm tiến độ nhiệm vụ
            if (FarmPuzzle.Meta.QuestManager.Instance != null)
            {
                FarmPuzzle.Meta.QuestManager.Instance.UpdateProgress(currentCrop.productID, currentCrop.yieldAmount);
            }
        }

        Debug.Log($"Thu hoạch được: {currentCrop.yieldAmount} nông sản ({ (currentCrop != null ? currentCrop.productID : "None") })");
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
        plantedSeedID = "";
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