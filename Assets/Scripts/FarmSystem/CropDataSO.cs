using UnityEngine;

[CreateAssetMenu(fileName = "NewCropData", menuName = "Farm/Crop Data")]
public class CropDataSO : ScriptableObject
{
    public float totalTimeToHarvest;
    public Sprite[] growthStages;    // Các hình ảnh giai đoạn lớn 
    public int yieldAmount;          // Sản lượng thu hoạch 
}