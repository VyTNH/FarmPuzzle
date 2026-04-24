using UnityEngine;
using System;
using FarmPuzzle.FarmSystem.Crop;

[RequireComponent(typeof(PolygonCollider2D))] 
public class LandPlot : MonoBehaviour
{
    public bool isOccupied = false;
    public bool isLocked = false;
    public string plotID;
    public int gridX;
    public int gridY;
    public string plantedSeedID = ""; 
    public DateTime plantedTime; 
    
    private CropDataSO currentCrop;

    [Header("Puzzle Settings (UC3)")]
    public FarmPuzzle.LandPuzzle.Data.LevelData puzzleLevel;

    [Header("Care System")]
    public CropNeedType currentNeed = CropNeedType.None;

    // ─── CARE TOOL MAPPING ───
    private static readonly System.Collections.Generic.Dictionary<CropNeedType, (string itemID, bool consumable)> CareToolMap =
        new()
        {
            { CropNeedType.Water,      ("tool_watercan",    false) },
            { CropNeedType.Pest,        ("tool_pest",        true) },
            { CropNeedType.Fertilizer,  ("item_fertilizer",  true) }
        };

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
        PolygonCollider2D col = GetComponent<PolygonCollider2D>();
        // KHÔNG set isTrigger = true ở đây — Physics2D.Raycast cần isTrigger=false để detect click!
        // isTrigger được quản lý từ Inspector (mặc định false).

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
            }
            
            if (currentNeed != _cropGrowth.CurrentNeed)
            {
                currentNeed = _cropGrowth.CurrentNeed;
            }
        }
    }

    public void LockPlot() { isLocked = true; ApplyLockVisual(); }
    public void UnlockPlot() { isLocked = false; ApplyLockVisual(); }

    public void ApplyLockVisual()
    {
        // Yêu cầu mới: Không đổi sang màu tối nữa vì đã quản lý hình ảnh riêng
        Color targetColor = Color.white; 
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
        // Debug.Log($"<color=cyan>[LandPlot]</color> Đang tải Prefab từ Resources: <b>{prefabPath}</b>");

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
                
                // Debug.Log($"<color=green>[LandPlot]</color> Đã Instantiate vật thể cây trồng thành công tại {transform.position}.");
                return true;
            }
            else {
                Debug.LogError($"<color=red>[LandPlot]</color> KHÔNG TÌM THẤY file Prefab <b>'{prefabPath}'</b> trong thư mục Resources! Hãy kiểm tra lại tên file.");
                return false;
            }
        } catch (Exception e) {
            Debug.LogError($"<color=red>[LandPlot]</color> Lỗi Runtime khi sinh cây: {e}"); // Dùng e.ToString() để lấy full stack trace
            return false;
        }
    }

    public void ApplyCare(CropNeedType careType)
    {
        if (!CareToolMap.TryGetValue(careType, out var tool)) return;

        if (tool.consumable)
        {
            if (DataManager.Instance == null || !DataManager.Instance.RemoveItem(tool.itemID, 1))
            {
                Debug.LogWarning($"<color=orange>[LandPlot]</color> Không đủ <b>'{tool.itemID}'</b> tiêu hao để chăm sóc cây!");
                return;
            }
        }
        ApplyCareEffect(careType, tool.itemID);
    }

    private void ApplyCareEffect(CropNeedType careType, string itemID)
    {
        currentNeed = CropNeedType.None;
        if (_cropGrowth != null) 
        {
            _cropGrowth.ResolveNeed(careType);
            if (careType == CropNeedType.Fertilizer) _cropGrowth.ApplyTimeBoost(60f);
        }
        Debug.Log($"<color=green>[LandPlot]</color> Đã dùng <b>'{itemID}'</b>. Nhu cầu {careType} đã được giải tỏa.");
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
                
            // [QUAN TRỌNG] Lưu ngay vào ô cứng (SQLite) để tránh mất dữ liệu khi Tắt game đột ngột!
            DataManager.Instance.CommitSessionInventory();
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

    // ─── THÊM: XỬ LÝ HIGHLIGHT MÀU SẮC KHI DRAG KÉO QUA ───
    public void SetHighlight(bool active, bool isValid)
    {
        if (tileRenderer == null) return;
        if (!active) 
        {
            tileRenderer.color = originalTileColor;
        }
        else 
        {
            tileRenderer.color = isValid ? new Color(0.7f, 1.0f, 0.7f) : new Color(1.0f, 0.7f, 0.7f);
        }
    }
}