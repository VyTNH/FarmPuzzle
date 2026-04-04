using UnityEngine;
using System;
using FarmPuzzle.FarmSystem.Crop;

[RequireComponent(typeof(BoxCollider2D))] 
public class LandPlot : MonoBehaviour
{
    public bool isOccupied = false;
    public bool isLocked = false;
    public string plotID; 
    public string plantedSeedID = ""; 
    public DateTime plantedTime; 
    
    private CropDataSO currentCrop;

    [Header("Puzzle Settings (UC3)")]
    public FarmPuzzle.LandPuzzle.Data.LevelData puzzleLevel;

    [Header("Care System")]
    public CropNeedType currentNeed = CropNeedType.None;
    public SpriteRenderer needIconRenderer;
    public Sprite waterSprite, pestSprite, weedSprite, harvestSprite;

    private SpriteRenderer plantRenderer;
    private SpriteRenderer tileRenderer; 
    private Color originalTileColor;
    
    private GameObject _spawnedCropObj;
    private FarmPuzzle.FarmSystem.Crop.CropGrowth _cropGrowth;
    private bool _wasHarvestable = false;

    public CropDataSO GetCropData() => currentCrop;
    public DateTime GetPlantedTime() => plantedTime;

    void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = true; 

        tileRenderer = GetComponent<SpriteRenderer>();
        if (tileRenderer == null)
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null) originalTileColor = meshRenderer.material.color;
        }
        else originalTileColor = tileRenderer.color;

        Transform visualChild = transform.Find("PlantVisual");
        if (visualChild == null)
        {
            GameObject plantObj = new GameObject("PlantVisual");
            plantObj.transform.SetParent(this.transform);
            plantObj.transform.localPosition = new Vector3(0, 0, -0.1f);
            plantRenderer = plantObj.AddComponent<SpriteRenderer>();
        }
        else plantRenderer = visualChild.GetComponent<SpriteRenderer>();

        ApplyLockVisual();
    }

    void Update()
    {
        if (isOccupied && _cropGrowth != null)
        {
            bool isHarvestableNow = CanHarvest();
            if (isHarvestableNow != _wasHarvestable)
            {
                _wasHarvestable = isHarvestableNow;
                UpdateNeedUI();
            }
            
            if (currentNeed != _cropGrowth.CurrentNeed)
            {
                currentNeed = _cropGrowth.CurrentNeed;
                UpdateNeedUI();
            }
        }
    }

    public void LockPlot() { isLocked = true; ApplyLockVisual(); }
    public void UnlockPlot() { isLocked = false; ApplyLockVisual(); }

    public void ApplyLockVisual()
    {
        Color targetColor = isLocked ? new Color(0.25f, 0.22f, 0.2f, 1f) : originalTileColor;
        if (tileRenderer != null) tileRenderer.color = targetColor;
        else {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.material.color = targetColor;
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
        if (!isOccupied || _cropGrowth == null) return false;
        return _cropGrowth.IsHarvestable;
    }

    public bool Plant(SeedItemSO seed)
    {
        if (isOccupied || isLocked || seed == null) 
        {
            Debug.LogWarning($"<color=orange>[LandPlot]</color> Từ chối gieo hạt tại ô <b>{plotID}</b>. Lý do: Đã có cây({isOccupied}), Đang khóa({isLocked}), Hạt giống NULL({seed == null})");
            return false;
        }

        Debug.Log($"<color=cyan>[LandPlot]</color> Khởi động quy trình gieo hạt <b>{seed.seedName}</b> cho ô {plotID}.");

        currentCrop = seed.cropData;
        plantedSeedID = seed.seedID;
        plantedTime = DateTime.UtcNow;
        
        bool success = SpawnCropVisual();
        if (success)
        {
            isOccupied = true;
            if (plantRenderer != null) plantRenderer.gameObject.SetActive(false);
            Debug.Log($"<color=green>[LandPlot]</color> Logic nội bộ hoàn tất: Ô <b>{plotID}</b> đã ở trạng thái Đang trồng.");
            return true;
        }
        else
        {
            Debug.LogError($"<color=red>[LandPlot]</color> Sinh cây thất bại tại ô {plotID}. Đã reset dữ liệu ô đất.");
            // Trả về trạng thái trống nếu sinh cây thất bại (ko tìm thấy file prefab...)
            currentCrop = null;
            plantedSeedID = "";
            return false;
        }
    }

    private bool SpawnCropVisual()
    {
        if (currentCrop == null || string.IsNullOrEmpty(currentCrop.productID)) {
             Debug.LogWarning($"<color=red>[LandPlot]</color> Lỗi nghiêm trọng: Ô '{plotID}' thiếu ProductID trong CropDataSO!");
             return false;
        }

        string prefabPath = "Crop_" + currentCrop.productID;
        Debug.Log($"<color=cyan>[LandPlot]</color> Đang tải Prefab từ Resources: <b>{prefabPath}</b>");

        try {
            // Lấy từ SO trước, nếu không có mới tìm bằng chuỗi.
            GameObject prefab = currentCrop.cropPrefab != null ? currentCrop.cropPrefab : Resources.Load<GameObject>(prefabPath);
            if (prefab != null)
            {
                if (_spawnedCropObj != null) Destroy(_spawnedCropObj);
                _spawnedCropObj = Instantiate(prefab, transform.position, Quaternion.identity, transform);
                _cropGrowth = _spawnedCropObj.GetComponent<FarmPuzzle.FarmSystem.Crop.CropGrowth>();
                
                var dbModel = new FarmPuzzle.Core.Database.CropDataModel {
                    ProductID = currentCrop.productID,
                    GrowSeconds = (int)currentCrop.totalTimeToHarvest
                };
                _cropGrowth.Initialize(dbModel, plantedTime.Ticks);
                _cropGrowth.ShowNeed(currentNeed);
                
                Debug.Log($"<color=green>[LandPlot]</color> Đã Instantiate vật thể cây trồng thành công tại {transform.position}.");
                return true;
            }
            else {
                Debug.LogError($"<color=red>[LandPlot]</color> KHÔNG TÌM THẤY file Prefab <b>'{prefabPath}'</b> trong thư mục Resources! Hãy kiểm tra lại tên file.");
                return false;
            }
        } catch (Exception e) { 
            Debug.LogError($"<color=red>[LandPlot]</color> Lỗi Runtime khi sinh cây: {e.Message}"); 
            return false;
        }
    }

    public void ApplyCare(CropNeedType careType)
    {
        if (currentNeed == careType)
        {
            currentNeed = CropNeedType.None;
            UpdateNeedUI();
            if (_cropGrowth != null) _cropGrowth.ResolveNeed(careType);
        }
    }

    public void UpdateNeedUI()
    {
        if (needIconRenderer == null) return;

        if (CanHarvest())
        {
            needIconRenderer.sprite = harvestSprite;
            return;
        }

        switch (currentNeed)
        {
            case CropNeedType.Water: needIconRenderer.sprite = waterSprite; break;
            case CropNeedType.Pest:  needIconRenderer.sprite = pestSprite; break;
            case CropNeedType.Fertilizer: needIconRenderer.sprite = weedSprite; break;
            default: needIconRenderer.sprite = null; break;
        }
    }

    public void ClearPlot()
    {
        if (_spawnedCropObj != null) Destroy(_spawnedCropObj);
        _spawnedCropObj = null;
        _cropGrowth = null;
        isOccupied = false;
        plantedSeedID = "";
        currentCrop = null;
        plantedTime = default;
        currentNeed = CropNeedType.None;
        if (plantRenderer != null) plantRenderer.sprite = null;
    }

    public void Harvest()
    {
        if (!CanHarvest()) return;
        if (DataManager.Instance != null && currentCrop != null)
        {
            int yieldBoosted = currentCrop.yieldAmount * 10;
            DataManager.Instance.AddItem(currentCrop.productID, yieldBoosted);
            if (FarmPuzzle.Meta.QuestManager.Instance != null)
                FarmPuzzle.Meta.QuestManager.Instance.UpdateProgress(currentCrop.productID, yieldBoosted);
        }
        ClearPlot();
    }

    public void SetData(string id, bool locked, SeedItemSO seed, long ticks)
    {
        this.plotID = id;
        this.isLocked = locked; 
        this.plantedSeedID = (seed != null) ? seed.seedID : "";
        this.plantedTime = new DateTime(ticks, DateTimeKind.Utc);
        
        ApplyLockVisual(); 

        if (!string.IsNullOrEmpty(plantedSeedID) && seed != null)
        {
            currentCrop = seed.cropData;
            bool success = SpawnCropVisual();
            isOccupied = success;
        }
        else ClearPlot();
    }
}