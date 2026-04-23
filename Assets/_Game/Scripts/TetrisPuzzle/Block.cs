using UnityEngine;

public class Block : MonoBehaviour
{
    public ProductData product;

    public void Init(ProductData data)
    {
        product = data;
        GetComponent<SpriteRenderer>().sprite = data.sprite;
    }
}