using UnityEngine;

namespace FarmPuzzle.LandPuzzle.Data
{
    /// <summary>
    /// Cấu hình 1 level Block Puzzle.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Level", menuName = "FarmPuzzle/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Grid")]
        public int gridSize = 10;

        [Header("Blocks")]
        public ShapeData[] availableShapes;
        public int         blocksPerBatch = 3;

        [Header("Obstacles trong Grid")]
        public ObstaclePlacement[] puzzleObstacles;

        [Header("Farm Link")]
        public string farmZoneId = "zone_1";
        public int    levelId    = 1;
    }
}
