using UnityEngine;

[CreateAssetMenu(fileName = "Product", menuName = "Farm/Product")]
public class ProductData : ScriptableObject
{
    public string productID;
    public string productName;

    public Sprite sprite;

    public int sellPrice;
}