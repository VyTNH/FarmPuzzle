using UnityEngine;

public class UI_Setting : MonoBehaviour
{
    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;

    public void TogglePanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void OpenPanel() => settingsPanel?.SetActive(true);
    public void ClosePanel() => settingsPanel?.SetActive(false);
}
