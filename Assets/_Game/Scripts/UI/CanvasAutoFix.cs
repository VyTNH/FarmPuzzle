using UnityEngine;
using UnityEngine.UI;

namespace FarmPuzzle.UI
{
    /// <summary>
    /// Tu dong Force Scale With Screen Size (1080x1920) cho toan bo Canvas trong man choi.
    /// Giup fix loi UI bi qua nho tren thiet bi Android thuc te.
    /// </summary>
    public static class CanvasAutoFix
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void OnGameStart()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Fix cho phien ban Unity moi: Dung FindObjectsInactive.Include thay vi truyen bool true/false
            CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (var scaler in scalers)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f; 
            }
            
            if (scalers.Length > 0)
                Debug.Log($"<color=cyan>[CanvasAutoFix]</color> Da tu dong Scale {scalers.Length} Canvas ve 1080x1920.");
        }
    }
}
