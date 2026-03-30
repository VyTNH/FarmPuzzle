using UnityEngine;

namespace FarmPuzzle.LandPuzzle
{
    /// <summary>
    /// Tính điểm cho Block Puzzle.
    /// </summary>
    public class BlockPuzzleScoring : MonoBehaviour
    {
        private int _currentScore;
        private int _combo;

        public int CurrentScore => _currentScore;

        public void ResetScore()
        {
            _currentScore = 0;
            _combo        = 0;
        }

        public void AddPlacementScore(int cellCount)
        {
            _currentScore += cellCount * 10;
        }

        public void AddLineClearScore(int lineCount)
        {
            _combo++;
            int baseScore = lineCount * 100;
            int comboBonus = (int)(baseScore * (_combo - 1) * 0.5f);
            _currentScore += baseScore + comboBonus;
        }

        public void AddObstacleScore(int resourceAmount)
        {
            _currentScore += resourceAmount * 50;
        }

        public void ResetCombo() => _combo = 0;
    }
}
