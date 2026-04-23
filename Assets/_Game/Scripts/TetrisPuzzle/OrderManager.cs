using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    public List<Order> orders = new List<Order>();

    void Awake()
    {
        Instance = this;
    }

    public void AddProducts(Dictionary<ProductData, int> products)
    {
        foreach (var item in products)
        {
            ProductData product = item.Key;
            int amount = item.Value;

            foreach (var order in orders)
            {
                if (order.product == product)
                {
                    order.currentAmount += amount;

                    if (order.currentAmount >= order.requiredAmount)
                    {
                        CompleteOrder(order);
                    }
                }
            }
        }
    }

    void CompleteOrder(Order order)
    {
        Debug.Log("Order completed: " + order.product.productName);
    }
}