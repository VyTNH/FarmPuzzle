using UnityEngine;

public abstract class UI_SettingBase : MonoBehaviour
{
    [SerializeField] protected string settingKey;

    protected virtual void Start()
    {
        InitializeSetting();
    }

    protected abstract void InitializeSetting();
    protected abstract void OnSettingChanged(float value);
}
