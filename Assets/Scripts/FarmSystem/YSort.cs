using UnityEngine;

public class YSort : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    // Đặt bằng số cột tối đa trong map của bạn (VD map 10x10 thì để 20 cho an toàn)
    [SerializeField] private int gridWidth = 10;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        int x = Mathf.RoundToInt(transform.position.x);
        int y = Mathf.RoundToInt(transform.position.y);

        // Công thức đảm bảo mỗi ô 1 số unique tuyệt đối
        spriteRenderer.sortingOrder = x * gridWidth - y;
    }
}