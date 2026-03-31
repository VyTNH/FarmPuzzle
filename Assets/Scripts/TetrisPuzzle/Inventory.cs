using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    public List<ProductData> products = new List<ProductData>();
    public Dictionary<ProductData, int> stock =
        new Dictionary<ProductData, int>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        foreach (var p in products)
        {
            stock[p] = 20; // test data
        }

        Debug.Log("Số loại nông sản trong inventory: " + products.Count);
    }

    public ProductData GetRandomProduct()
    {
        int index = Random.Range(0, products.Count);
        return products[index];
    }

    public void AddProduct(ProductData product, int amount)
    {
        if (!stock.ContainsKey(product))
            stock[product] = 0;

        stock[product] += amount;
    }

    public bool UseProduct(ProductData product, int amount)
    {
        if (stock[product] < amount)
            return false;

        stock[product] -= amount;
        return true;
    }
}