using UnityEngine;
using System.Collections.Generic;

public class RowClear : MonoBehaviour
{
    public void CheckRows()
    {
        for (int y = 0; y < GridSystem.height; y++)
        {
            if (IsRowFull(y))
            {
                ClearRow(y);
                DropRows(y);
                y--;
            }
        }
    }
    bool IsRowFull(int y)
    {
        for (int x = 0; x < GridSystem.width; x++)
        {
            if (GridSystem.grid[x, y] == null)
                return false;
        }
        return true;
    }
    void ClearRow(int y)
    {
        Dictionary<ProductData, int> products = new();

        for (int x = 0; x < GridSystem.width; x++)
        {
            Block block = GridSystem.grid[x, y].GetComponent<Block>();

            if (!products.ContainsKey(block.product))
                products[block.product] = 0;

            products[block.product]++;

            Destroy(GridSystem.grid[x, y].gameObject);
            GridSystem.grid[x, y] = null;
        }

        OrderManager.Instance.AddProducts(products);
    }

    void DropRows(int y)
    {
        for (int i = y + 1; i < GridSystem.height; i++)
        {
            for (int x = 0; x < GridSystem.width; x++)
            {
                if (GridSystem.grid[x, i] != null)
                {
                    GridSystem.grid[x, i - 1] = GridSystem.grid[x, i];
                    GridSystem.grid[x, i] = null;

                    GridSystem.grid[x, i - 1].position += Vector3.down;
                }
            }
        }
    }
}