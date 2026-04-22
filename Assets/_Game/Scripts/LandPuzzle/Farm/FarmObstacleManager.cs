using System.Collections.Generic;
using UnityEngine;
using FarmPuzzle.LandPuzzle.Grid;

namespace FarmPuzzle.LandPuzzle.Farm
{
    /// <summary>
    /// FarmObstacleManager — giữ lại để không break references trong Editor.
    /// Với flow mới, obstacles đã chuyển vào Puzzle Grid (GridBoard).
    /// Class này chỉ giữ các tile references và AutoFindTiles cho Editor Inspector.
    /// </summary>
    public class FarmObstacleManager : MonoBehaviour
    {


        [Header("References")]
        [SerializeField] private GridBoard _gridBoard;

        [Header("Tiles")]
        [SerializeField] private List<FarmLandTile> _tiles = new List<FarmLandTile>();

        // ── Properties ──
        public int TotalLockedTiles  => CountLocked();
        public int UnlockedCount     => CountUnlocked();
        public int RemainingLocked   => TotalLockedTiles - UnlockedCount;
        public bool AllUnlocked      => RemainingLocked <= 0;

        private int CountLocked()
        {
            int n = 0;
            foreach (var t in _tiles) if (t != null && !t.IsUnlocked) n++;
            return n;
        }

        private int CountUnlocked()
        {
            int n = 0;
            foreach (var t in _tiles) if (t != null && t.IsUnlocked) n++;
            return n;
        }

        // ── Editor helpers ───────────────────────────────────────────────────

        [ContextMenu("Auto Find Tiles")]
        public void AutoFindTiles()
        {
            _tiles.Clear();
            _tiles.AddRange(GetComponentsInChildren<FarmLandTile>());
            Debug.Log($"[FarmObstacleManager] Found {_tiles.Count} tiles");
        }

        public void ResetAllTiles()
        {
            foreach (var tile in _tiles)
                tile?.ResetToLocked();
        }
    }
}
