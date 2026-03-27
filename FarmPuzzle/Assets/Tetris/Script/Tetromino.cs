using UnityEngine;

public class Tetromino : MonoBehaviour
{
    float fallTime = 0.5f;
    float previousTime;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            Move(Vector3.left);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            Move(Vector3.right);

        if (Input.GetKeyDown(KeyCode.UpArrow))
            Rotate();

        if (Time.time - previousTime > fallTime)
        {
            Move(Vector3.down);
            previousTime = Time.time;
        }
    }

    void Move(Vector3 dir)
    {
        transform.position += dir;

        if (!ValidMove())
        {
            transform.position -= dir;

            if (dir == Vector3.down)
            {
                AddToGrid();
                FindObjectOfType<TetrisManager>().SpawnPiece();
                enabled = false;
            }
        }
    }

    void Rotate()
    {
        transform.Rotate(0, 0, -90);

        if (!ValidMove())
            transform.Rotate(0, 0, 90);
    }
    bool ValidMove()
    {
        foreach (Transform child in transform)
        {
            Vector2 pos = Vector2Int.RoundToInt(child.position);

            if (!GridSystem.InsideBorder(pos))
                return false;

            if (pos.y < GridSystem.height &&
                GridSystem.grid[(int)pos.x, (int)pos.y] != null)
                return false;
        }

        return true;
    }
    void AddToGrid()
    {
        foreach (Transform child in transform)
        {
            Vector2 pos = Vector2Int.RoundToInt(child.position);

            GridSystem.grid[(int)pos.x, (int)pos.y] = child;
        }

        FindObjectOfType<RowClear>().CheckRows();
    }
}