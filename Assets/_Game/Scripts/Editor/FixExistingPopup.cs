using UnityEngine;
using UnityEditor;

public class FixExistingPopup
{
    [InitializeOnLoadMethod]
    static void RunFix()
    {
        // 1. Tìm GameObject tên là LandPuzzle_ConfirmPopup k? c? khi nó dang b? t?t
        var popups = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject popupGo = null;
        foreach(var go in popups) {
            // Find in current scene, ignore prefabs
            if (go.name == "LandPuzzle_ConfirmPopup" && go.scene.IsValid()) {
                popupGo = go;
                break;
            }
        }

        if (popupGo != null)
        {
            // B?T TR?NG THÁI ACTIVE
            Undo.RecordObject(popupGo, "Activate Popup");
            popupGo.SetActive(true);
            
            // Tìm controller
            var ctrl = popupGo.GetComponent<FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController>();
            if (ctrl == null) {
                ctrl = popupGo.AddComponent<FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController>();
                Debug.Log("[AutoFix] Ðã g?n thêm LandPuzzlePopupController vào " + popupGo.name);
            }

            // G?n l?i panel n?u chua có
            var so = new SerializedObject(ctrl);
            if (so.FindProperty("_confirmPopupPanel").objectReferenceValue == null) {
                so.FindProperty("_confirmPopupPanel").objectReferenceValue = popupGo;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("<color=green>[AutoFix]</color> Ðã kích ho?t Root GameObject " + popupGo.name + ".Code Awake() s? du?c ch?y d? set Instance!");
            EditorUtility.SetDirty(popupGo);
        }
        else
        {
            Debug.LogWarning("[AutoFix] Không tìm th?y LandPuzzle_ConfirmPopup trong scene.");
        }
    }
}
