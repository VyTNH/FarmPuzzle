using UnityEngine;
using UnityEditor;

public class AutoInjectUIDataBinder
{
    [InitializeOnLoadMethod]
    static void Inject()
    {
        EditorApplication.delayCall += () => {
            if (GameObject.Find("[UIDataBinder]") != null) return;

            var go = new GameObject("[UIDataBinder]");
            go.AddComponent<FarmPuzzle.UI.UIDataBinder>();
            Debug.Log("[AutoInject] Ðã t? d?ng g?n UIDataBinder System vào Scene d? b?t d?u móc n?i d? li?u Kho Hàng và Quest! C? Play là ch?y.");
        };
    }
}
