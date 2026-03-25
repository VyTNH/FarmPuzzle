using UnityEngine;

namespace FarmPuzzle.LandPuzzle.Data
{
    public enum ResourceType { Wood, Stone, Food, Gold }

    /// <summary>
    /// ScriptableObject cấu hình 1 loại chướng ngại vật trong puzzle grid.
    /// Khi bị tiêu diệt → cho tài nguyên theo type và amount.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_GridObstacle", menuName = "FarmPuzzle/Grid Obstacle Data")]
    public class GridObstacleData : ScriptableObject
    {
        [Header("Info")]
        public string obstacleName   = "Stone";
        public Color  obstacleColor  = new Color(0.5f, 0.5f, 0.5f);

        [Header("Durability")]
        [Range(1, 5)]
        public int maxDurability = 2;

        [Header("Resource Reward (khi phá hủy)")]
        public ResourceType resourceType   = ResourceType.Stone;
        public int          resourceAmount = 5;
    }
}
