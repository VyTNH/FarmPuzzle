using UnityEngine;

public class GridSystem : MonoBehaviour
{
    public static int width = 10;
    public static int height = 10;

    public static Transform[,] grid = new Transform[width, height];

    public static bool InsideBorder(Vector2 pos)
    {
        return ((int)pos.x >= 0 &&
                (int)pos.x < width &&
                (int)pos.y >= 0);
    }
}