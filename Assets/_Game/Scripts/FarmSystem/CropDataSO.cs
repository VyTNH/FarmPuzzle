using UnityEngine;

[CreateAssetMenu(fileName = "NewCropData", menuName = "Farm/Crop Data")]
public class CropDataSO : ScriptableObject
{
    public float totalTimeToHarvest;
    public string productID;         // ID Nông sản nhận được (để lưu DB)
    public string cropName;          // Tên nông sản để hiển thị in-game
    public Sprite productIcon;       // Ảnh Sprite để hiển thị trong Inventory UI
    public Sprite[] growthStages;    // Các hình ảnh giai đoạn lớn 
    public int yieldAmount;          // Sản lượng thu hoạch 
    public GameObject cropPrefab;    // Prefab cây trồng thực tế gắn trên scene
}