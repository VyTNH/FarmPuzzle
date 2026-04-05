using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FixButtonLayout
{
    static FixButtonLayout()
    {
        EditorApplication.delayCall += FixLayoutOnce;
    }

    private static void FixLayoutOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorPrefs.GetBool("DecorScene_Btn_Fixed", false)) return;
        EditorPrefs.SetBool("DecorScene_Btn_Fixed", true);

        try
        {
            string scenePath = "Assets/Scenes/DecorTestScene.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath);

            GameObject btnObj = GameObject.Find("Btn_TestPlacement");
            if (btnObj != null)
            {
                // Đưa nút lên giữa màn hình trên cùng
                RectTransform rT = btnObj.GetComponent<RectTransform>();
                if (rT != null)
                {
                    rT.anchorMin = new Vector2(0.5f, 1f);
                    rT.anchorMax = new Vector2(0.5f, 1f);
                    rT.anchoredPosition = new Vector2(0, -60);
                }

                // Sửa lỗi cảnh báo Font
                TextMeshProUGUI tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null && tmp.font == null)
                {
                    var defaultFont = TMP_Settings.defaultFontAsset;
                    if (defaultFont != null) tmp.font = defaultFont;
                }
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("<color=cyan>[FarmPuzzle] Đã căn giữa nút và gắn Font thành công! (Recompiled)</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FarmPuzzle] Lỗi sửa UI: {e.Message}");
        }
    }
}
