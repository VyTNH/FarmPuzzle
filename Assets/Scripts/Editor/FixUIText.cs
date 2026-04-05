using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FixUIText
{
    static FixUIText()
    {
        EditorApplication.delayCall += RenameText;
    }

    private static void RenameText()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorPrefs.GetBool("DecorScene_Text_Fixed", false)) return;
        EditorPrefs.SetBool("DecorScene_Text_Fixed", true);

        try
        {
            string scenePath = "Assets/Scenes/DecorTestScene.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath);

            GameObject btnObj = GameObject.Find("Btn_TestPlacement");
            if (btnObj != null)
            {
                TextMeshProUGUI tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "Test Đặt Vật Trang Trí";
                }
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("<color=cyan>[FarmPuzzle] Đã đổi tên UI thành công!</color>");
        }
        catch (System.Exception e) { }
    }
}
