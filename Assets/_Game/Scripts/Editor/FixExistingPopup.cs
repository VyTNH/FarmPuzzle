// using UnityEngine;
// using UnityEditor;

// public class FixExistingPopup
// {
//     [InitializeOnLoadMethod]
//     static void RunFix()
//     {
//         // 1. T�m GameObject t�n l� LandPuzzle_ConfirmPopup k? c? khi n� dang b? t?t
//         var popups = Resources.FindObjectsOfTypeAll<GameObject>();
//         GameObject popupGo = null;
//         foreach(var go in popups) {
//             // Find in current scene, ignore prefabs
//             if (go.name == "LandPuzzle_ConfirmPopup" && go.scene.IsValid()) {
//                 popupGo = go;
//                 break;
//             }
//         }

//         if (popupGo != null)
//         {
//             // B?T TR?NG TH�I ACTIVE
//             Undo.RecordObject(popupGo, "Activate Popup");
//             popupGo.SetActive(true);
            
//             // T�m controller
//             var ctrl = popupGo.GetComponent<FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController>();
//             if (ctrl == null) {
//                 ctrl = popupGo.AddComponent<FarmPuzzle.LandPuzzle.UI.LandPuzzlePopupController>();
//                 Debug.Log("[AutoFix] �� g?n th�m LandPuzzlePopupController v�o " + popupGo.name);
//             }

//             // G?n l?i panel n?u chua c�
//             var so = new SerializedObject(ctrl);
//             if (so.FindProperty("_confirmPopupPanel").objectReferenceValue == null) {
//                 so.FindProperty("_confirmPopupPanel").objectReferenceValue = popupGo;
//             }
//             so.ApplyModifiedPropertiesWithoutUndo();

//             Debug.Log("<color=green>[AutoFix]</color> �� k�ch ho?t Root GameObject " + popupGo.name + ".Code Awake() s? du?c ch?y d? set Instance!");
//             EditorUtility.SetDirty(popupGo);
//         }
//         else
//         {
//             Debug.LogWarning("[AutoFix] Kh�ng t�m th?y LandPuzzle_ConfirmPopup trong scene.");
//         }
//     }
// }
