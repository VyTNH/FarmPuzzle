using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor Tool: Tự động sắp xếp và gom nhóm các GameObject trong Hierarchy.
/// Menu: FarmPuzzle → Organize Hierarchy
/// 
/// AN TOÀN TUYỆT ĐỐI:
///   - Chỉ thay đổi cấu trúc cha-con (Transform parent), KHÔNG đụng đến Component.
///   - Không phá vỡ bất kỳ tham chiếu SerializeField nào.
///   - Có thể chạy lại nhiều lần mà không bị trùng lặp (Idempotent).
///   - Hỗ trợ Undo: Ctrl+Z để hoàn tác toàn bộ.
/// </summary>
public class HierarchyOrganizer : Editor
{
    // ─── Tên các nhóm folder ────────────────────────────────────────────────
    private const string GROUP_CORE       = "[--- CORE MANAGERS ---]";
    private const string GROUP_FARM       = "[--- FARM SYSTEM ---]";
    private const string GROUP_LAND       = "[--- LAND PUZZLE SYSTEM ---]";
    private const string GROUP_TETRIS     = "[--- TETRIS SYSTEM ---]";
    private const string GROUP_UI         = "[--- UI ROOT ---]";

    // ─── Menu Entry ─────────────────────────────────────────────────────────
    [MenuItem("FarmPuzzle/🗂️ Organize Hierarchy", priority = 1)]
    public static void OrganizeHierarchy()
    {
        // Đăng ký toàn bộ thao tác vào Undo stack → Ctrl+Z hoàn tác được
        Undo.SetCurrentGroupName("Organize Hierarchy");
        int undoGroup = Undo.GetCurrentGroup();

        Scene scene = SceneManager.GetActiveScene();
        Debug.Log($"<color=cyan>[HierarchyOrganizer]</color> Bắt đầu sắp xếp scene: <b>{scene.name}</b>");

        // ── Bước 1: Tạo (hoặc tái sử dụng) các folder nhóm ─────────────────
        GameObject groupCore   = GetOrCreateFolder(GROUP_CORE);
        GameObject groupFarm   = GetOrCreateFolder(GROUP_FARM);
        GameObject groupLand   = GetOrCreateFolder(GROUP_LAND);
        GameObject groupTetris = GetOrCreateFolder(GROUP_TETRIS);
        GameObject groupUI     = GetOrCreateFolder(GROUP_UI);

        // ── Bước 2: Di chuyển CORE MANAGERS ─────────────────────────────────
        MoveToGroup("DataManager",       groupCore);
        MoveToGroup("QuestManager",      groupCore);
        MoveToGroup("DecorationManager", groupCore);
        MoveToGroup("[UIDataBinder]",    groupCore);

        // ── Bước 3: Di chuyển FARM SYSTEM ───────────────────────────────────
        MoveToGroup("Grid Manager",          groupFarm);
        MoveToGroup("--- ENERGY SYSTEM ---", groupFarm);

        // ── Bước 4: Di chuyển LAND PUZZLE SYSTEM ────────────────────────────
        // Chú ý: Canvas_LandPuzzle thuộc về hệ thống này
        MoveToGroup("--- LAND PUZZLE SYSTEM ---", groupLand);
        MoveToGroup("Canvas_LandPuzzle",          groupLand);

        // ── Bước 5: Di chuyển TETRIS SYSTEM ─────────────────────────────────
        // Đổi tên Canvas_TetriaPopupUI → Canvas_TetrisPopupUI nếu còn tên cũ
        RenameIfExists("Canvas_TetriaPopupUI", "Canvas_TetrisPopupUI");
        MoveToGroup("Canvas_TetrisPopupUI", groupTetris);
        MoveToGroup("Canvas_TetriaPopupUI", groupTetris); // Phòng khi chưa đổi tên

        // ── Bước 6: Di chuyển UI ROOT ────────────────────────────────────────
        // Đổi tên Canvas_Father → Canvas_MainPanels
        RenameIfExists("Canvas_Father", "Canvas_MainPanels");
        MoveToGroup("Canvas_MainPanels",  groupUI);
        MoveToGroup("Canvas_Father",      groupUI); // Phòng khi chưa đổi tên
        MoveToGroup("Canvas_FarmTopBar",  groupUI);
        MoveToGroup("Canvas_SandboxUI",   groupUI);

        // ── Bước 7: Giữ Camera và EventSystem ở gốc (không di chuyển) ───────
        // Main Camera và EventSystem nên ở gốc scene để dễ tìm
        EnsureAtRoot("Main Camera");
        EnsureAtRoot("EventSystem");

        // ── Bước 8: Sắp xếp thứ tự hiển thị trong Hierarchy ────────────────
        SetSiblingOrder("Main Camera",   0);
        SetSiblingOrder("EventSystem",   1);
        SetSiblingOrder(GROUP_CORE,      2);
        SetSiblingOrder(GROUP_FARM,      3);
        SetSiblingOrder(GROUP_LAND,      4);
        SetSiblingOrder(GROUP_TETRIS,    5);
        SetSiblingOrder(GROUP_UI,        6);

        // Kết thúc nhóm Undo
        Undo.CollapseUndoOperations(undoGroup);

        // Đánh dấu scene đã thay đổi để Unity biết cần lưu
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log("<color=lime>[HierarchyOrganizer]</color> ✅ Sắp xếp Hierarchy hoàn tất! Nhấn Ctrl+S để lưu scene.");
        EditorUtility.DisplayDialog(
            "✅ Hoàn tất!",
            "Hierarchy đã được sắp xếp thành công!\n\n" +
            "• Ctrl+Z để hoàn tác\n" +
            "• Ctrl+S để lưu scene",
            "OK"
        );
    }

    // ── Kiểm tra điều kiện trước khi chạy ───────────────────────────────────
    [MenuItem("FarmPuzzle/🗂️ Organize Hierarchy", true)]
    private static bool ValidateOrganize()
    {
        // Chỉ cho phép chạy khi đang ở Edit Mode (không phải Play Mode)
        return !Application.isPlaying;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CÁC HÀM TIỆN ÍCH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tìm folder nhóm theo tên. Nếu chưa có → tạo mới Empty GameObject.
    /// Đảm bảo Transform ở gốc (0,0,0) và không có component thừa.
    /// </summary>
    private static GameObject GetOrCreateFolder(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            go.transform.SetParent(null); // Đảm bảo ở gốc scene
            Undo.RegisterCreatedObjectUndo(go, $"Create folder: {name}");
            Debug.Log($"<color=yellow>[HierarchyOrganizer]</color> Tạo mới folder: <b>{name}</b>");
        }
        else
        {
            Debug.Log($"<color=gray>[HierarchyOrganizer]</color> Tái sử dụng folder: <b>{name}</b>");
        }

        // Đảm bảo Transform sạch
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;

        return go;
    }

    /// <summary>
    /// Tìm GameObject theo tên và đặt nó vào trong folder nhóm.
    /// Nếu đã trong đúng nhóm rồi → bỏ qua (không làm gì).
    /// Nếu không tìm thấy → log cảnh báo, không crash.
    /// </summary>
    private static void MoveToGroup(string objectName, GameObject targetParent)
    {
        // Tìm chính xác ở gốc scene trước, sau đó mới tìm toàn scene
        GameObject go = FindRootObject(objectName) ?? GameObject.Find(objectName);

        if (go == null)
        {
            Debug.Log($"<color=gray>[HierarchyOrganizer]</color> Không tìm thấy: '{objectName}' — bỏ qua.");
            return;
        }

        // Đã là con của đúng nhóm rồi → không làm gì
        if (go.transform.parent == targetParent.transform)
        {
            Debug.Log($"<color=gray>[HierarchyOrganizer]</color> '{objectName}' đã trong đúng nhóm.");
            return;
        }

        // Đăng ký Undo trước khi thay đổi
        Undo.SetTransformParent(go.transform, targetParent.transform, $"Move {objectName} to {targetParent.name}");
        Debug.Log($"<color=white>[HierarchyOrganizer]</color> Di chuyển '<b>{objectName}</b>' → <b>{targetParent.name}</b>");
    }

    /// <summary>
    /// Đổi tên GameObject nếu nó tồn tại. An toàn — không crash nếu không tìm thấy.
    /// </summary>
    private static void RenameIfExists(string oldName, string newName)
    {
        // Kiểm tra tên mới đã tồn tại chưa (tránh đổi tên trùng)
        if (GameObject.Find(newName) != null) return;

        GameObject go = GameObject.Find(oldName);
        if (go == null) return;

        Undo.RecordObject(go, $"Rename {oldName} to {newName}");
        go.name = newName;
        Debug.Log($"<color=orange>[HierarchyOrganizer]</color> Đổi tên: '<b>{oldName}</b>' → '<b>{newName}</b>'");
    }

    /// <summary>
    /// Đảm bảo GameObject nằm ở gốc scene (không có cha nào).
    /// </summary>
    private static void EnsureAtRoot(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null) return;
        if (go.transform.parent == null) return; // Đã ở gốc rồi

        Undo.SetTransformParent(go.transform, null, $"Move {objectName} to root");
        Debug.Log($"<color=cyan>[HierarchyOrganizer]</color> Đưa '<b>{objectName}</b>' về gốc scene.");
    }

    /// <summary>
    /// Đặt thứ tự hiển thị của GameObject trong Hierarchy.
    /// </summary>
    private static void SetSiblingOrder(string objectName, int order)
    {
        GameObject go = FindRootObject(objectName) ?? GameObject.Find(objectName);
        if (go == null) return;

        Undo.RecordObject(go.transform, $"Reorder {objectName}");
        go.transform.SetSiblingIndex(order);
    }

    /// <summary>
    /// Tìm GameObject chỉ ở cấp gốc của scene (không tìm trong con cháu).
    /// Tránh nhầm lẫn khi có nhiều GO cùng tên ở các cấp khác nhau.
    /// </summary>
    private static GameObject FindRootObject(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (var go in rootObjects)
        {
            if (go.name == name) return go;
        }
        return null;
    }
}
