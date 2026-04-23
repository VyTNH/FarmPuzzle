using UnityEngine;

public static class SettingsManager
{
    public static void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public static float GetFloat(string key, float defaultValue = 1f)
    {
        return PlayerPrefs.GetFloat(key, defaultValue);
    }

    public static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }
}
