using UnityEngine;

[CreateAssetMenu(fileName = "NewSeedItem", menuName = "Farm/Seed Item")]
public class SeedItemSO : ScriptableObject
{
    public string seedName;
    public Sprite inventoryIcon;
    public int buyPrice;
    public CropDataSO cropData;      // Tham chiếu đến dữ liệu cây trồng tương ứng 
}