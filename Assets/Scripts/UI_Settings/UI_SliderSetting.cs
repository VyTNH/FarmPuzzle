using UnityEngine;
using UnityEngine.UI;

public class UI_SliderSetting : UI_SettingBase
{
    [SerializeField] private Slider slider;

    protected override void InitializeSetting()
    {
        if (slider == null) slider = GetComponent<Slider>();
        
        // Load value from SettingsManager
        slider.value = SettingsManager.GetFloat(settingKey, 1f);

        // Add listener
        slider.onValueChanged.AddListener(OnSettingChanged);
    }

    protected override void OnSettingChanged(float value)
    {
        // Special case for Sound: Delegate to SoundManager for Mixer control
        if (settingKey == GameConstants.MUSIC_VOLUME_KEY || settingKey == GameConstants.SFX_VOLUME_KEY)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetVolume(settingKey, value);
            }
        }
        else
        {
            // Generic case: Just save to SettingsManager
            SettingsManager.SetFloat(settingKey, value);
        }
    }

    private void OnDestroy()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSettingChanged);
    }
}
