using UnityEngine;

namespace FarmPuzzle.LandPuzzle.Data
{
    /// <summary>
    /// Vị trí đặt obstacle trong puzzle grid.
    /// </summary>
    [System.Serializable]
    public class ObstaclePlacement
    {
        public int gridRow;
        public int gridCol;
        public GridObstacleData obstacleData;
    }
}
