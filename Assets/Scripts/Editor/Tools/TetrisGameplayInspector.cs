using UnityEngine;
using UnityEditor;

public class TetrisGameplayInspector : EditorWindow
{
    private string simulatedRowsCleared = "1";

    [MenuItem("FarmPuzzle/Công cụ / Tetris Xuất Hàng Inspector")]
    public static void ShowWindow()
    {
        GetWindow<TetrisGameplayInspector>("Tetris Inspector");
    }

    private void OnGUI()
    {
        GUILayout.Label("🎮 TRÌNH QUẢN LÝ TETRIS XUẤT HÀNG", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Chức năng này chỉ khả dụng khi Game đang chạy (Play Mode)!", MessageType.Warning);
            return;
        }

        GUILayout.Label("THÔNG SỐ PLAYTEST", EditorStyles.boldLabel);
        
        // Hiện tại tính năng Tetris chưa được implement hoàn toàn,
        // Dưới đây là các mockup mô phỏng luồng gọi sự kiện.
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Simulate Rows Cleared:", GUILayout.Width(150));
        simulatedRowsCleared = GUILayout.TextField(simulatedRowsCleared);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("🔥 Gửi sự kiện Xóa Hàng (Hoàn thành đơn)"))
        {
            if (int.TryParse(simulatedRowsCleared, out int rows))
            {
                Debug.Log($"<color=orange>[Tetris Inspector]</color> Giả lập gọi sự kiện xóa {rows} hàng! Truyền tín hiệu cho OrderManager để tổng hợp vào Tiến trình xuất hàng.");
                // TODO: Gọi TetrisManager.Instance.SimulateLinesCleared(rows)
            }
        }

        GUILayout.Space(10);
        
        if (GUILayout.Button("🎲 Tạo khối rơi mới (Force Spawn)"))
        {
            Debug.Log($"<color=orange>[Tetris Inspector]</color> Bắt buộc tạo khối rơi mới.");
            // TODO: Bắt TetrisSpawner thả một khối
        }

        if (GUILayout.Button("⏱ Tạm dừng/Tiếp tục Game rơi"))
        {
            Debug.Log($"<color=orange>[Tetris Inspector]</color> Đổi trạng thái Pause của Tetris.");
            // TODO: TetrisManager.Instance.TogglePause()
        }
    }
}
