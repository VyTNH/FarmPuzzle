using UnityEditor;
using UnityEngine;
using FarmPuzzle.LandPuzzle.Farm;

namespace FarmPuzzle.LandPuzzle.Editor
{
    /// <summary>
    /// Custom Inspector cho FarmObstacleManager.
    /// Hiển thị visual farm grid, cho phép chỉnh sửa obstacle trực quan.
    /// </summary>
    [CustomEditor(typeof(FarmObstacleManager))]
    public class FarmObstacleManagerEditor : UnityEditor.Editor
    {
        private FarmObstacleManager _manager;

        private void OnEnable()
        {
            _manager = (FarmObstacleManager)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            GUILayout.Label("🌾 Farm Grid Tools", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🔎 Auto Find Tiles", GUILayout.Height(28)))
            {
                _manager.AutoFindTiles();
                EditorUtility.SetDirty(_manager);
            }

            if (Application.isPlaying)
            {
                if (GUILayout.Button("🔄 Reset All", GUILayout.Height(28)))
                {
                    _manager.ResetAllTiles();
                }
            }

            EditorGUILayout.EndHorizontal();

            // Runtime info
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Total Locked: {_manager.TotalLockedTiles}");
                EditorGUILayout.LabelField($"Unlocked: {_manager.UnlockedCount}");
                EditorGUILayout.LabelField($"Remaining: {_manager.RemainingLocked}");

                if (_manager.AllUnlocked)
                {
                    EditorGUILayout.HelpBox("✅ TẤT CẢ ĐẤT ĐÃ MỞ KHÓA!", MessageType.Info);
                }

                Repaint(); // Auto refresh in play mode
            }
        }
    }

    /// <summary>
    /// Custom Inspector cho FarmLandTile.
    /// Hiển thị trạng thái tile và actions nhanh.
    /// </summary>
    [CustomEditor(typeof(FarmLandTile))]
    public class FarmLandTileEditor : UnityEditor.Editor
    {
        private FarmLandTile _tile;

        private void OnEnable()
        {
            _tile = (FarmLandTile)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // Status badge
            if (_tile.IsUnlocked)
            {
                EditorGUILayout.HelpBox("✅ ĐÃ MỞ KHÓA", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("🟢 Chưa mở khóa", MessageType.None);
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();

                if (!_tile.IsUnlocked)
                {
                    if (GUILayout.Button("🔓 Force Unlock", GUILayout.Height(25)))
                    {
                        _tile.ForceUnlock();
                    }
                }

                if (_tile.IsUnlocked)
                {
                    if (GUILayout.Button("🔒 Re-Lock", GUILayout.Height(25)))
                    {
                        _tile.ResetToLocked();
                    }
                }

                EditorGUILayout.EndHorizontal();

                Repaint();
            }
        }
    }

    /// <summary>
    /// Custom Inspector cho LandPuzzleManager.
    /// Hiển thị game state và actions debug.
    /// </summary>
    [CustomEditor(typeof(LandPuzzleManager))]
    public class LandPuzzleManagerEditor : UnityEditor.Editor
    {
        private LandPuzzleManager _manager;

        private void OnEnable()
        {
            _manager = (LandPuzzleManager)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                GUILayout.Label("🎮 Game State", EditorStyles.boldLabel);

                // State badge
                Color stateColor = Color.white;
                string stateEmoji = "⬜";
                switch (_manager.CurrentState)
                {
                    case PuzzleState.Playing:
                        stateColor = new Color(0.3f, 0.9f, 0.3f);
                        stateEmoji = "▶";
                        break;
                    case PuzzleState.Win:
                        stateColor = new Color(1f, 0.84f, 0f);
                        stateEmoji = "🏆";
                        break;
                    case PuzzleState.GameOver:
                        stateColor = new Color(0.9f, 0.3f, 0.3f);
                        stateEmoji = "💀";
                        break;
                    case PuzzleState.CheckingLines:
                    case PuzzleState.ClearingAnimation:
                        stateColor = new Color(0.9f, 0.7f, 0.3f);
                        stateEmoji = "⏳";
                        break;
                }

                GUI.backgroundColor = stateColor;
                EditorGUILayout.HelpBox(
                    $"{stateEmoji} State: {_manager.CurrentState}\n" +
                    $"💰 Score: {_manager.CurrentScore}",
                    MessageType.None
                );
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("🔄 Retry", GUILayout.Height(25)))
                {
                    _manager.RetryPuzzle();
                }

                if (GUILayout.Button("🚪 Exit", GUILayout.Height(25)))
                {
                    _manager.ExitPuzzle();
                }

                EditorGUILayout.EndHorizontal();

                Repaint();
            }
        }
    }
}
