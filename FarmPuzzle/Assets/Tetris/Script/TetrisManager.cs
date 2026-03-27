using UnityEngine;

public class TetrisManager : MonoBehaviour
{
    public GameObject[] tetrominoPrefabs;
    public Transform spawnPoint;

    public void Start()
    {
        SpawnPiece();
    }

    public void SpawnPiece()
    {
        int index = Random.Range(0, tetrominoPrefabs.Length);

        GameObject piece =
            Instantiate(tetrominoPrefabs[index], spawnPoint.position, Quaternion.identity);

        ProductData product =
            Inventory.Instance.GetRandomProduct();

        foreach (Block block in piece.GetComponentsInChildren<Block>())
        {
            block.Init(product);
        }
    }
}