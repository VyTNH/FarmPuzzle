using UnityEngine;

[CreateAssetMenu(fileName = "NewCropData", menuName = "Farm/Crop Data")]
public class CropDataSO : ScriptableObject
{
    public float totalTimeToHarvest;
    public string productID;         // ID Nông sản nhận được (để lưu DB)
    public Sprite[] growthStages;    // Các hình ảnh giai đoạn lớn 
    public int yieldAmount;          // Sản lượng thu hoạch 
}