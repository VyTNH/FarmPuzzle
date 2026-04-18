using UnityEngine;

[System.Serializable]
public class Order
{
    public ProductData product;
    public int requiredAmount;

    public int currentAmount;
}