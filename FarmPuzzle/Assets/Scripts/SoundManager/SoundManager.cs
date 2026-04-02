using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Apply saved volumes on start
        ApplyVolumes();
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void SetMusicVolume(float value)
    {
        SetVolume(GameConstants.MUSIC_VOLUME_KEY, value);
    }

    public void SetSFXVolume(float value)
    {
        SetVolume(GameConstants.SFX_VOLUME_KEY, value);
    }

    public void SetVolume(string parameterName, float sliderValue)
    {
        // Formula to convert linear slider value to decibels
        float dB = (sliderValue > 0) ? Mathf.Log10(sliderValue) * 20 : -80f;
        mainMixer.SetFloat(parameterName, dB);
        
        // Save to data manager
        SettingsManager.SetFloat(parameterName, sliderValue);
    }

    private void ApplyVolumes()
    {
        SetMusicVolume(SettingsManager.GetFloat(GameConstants.MUSIC_VOLUME_KEY, 1f));
        SetSFXVolume(SettingsManager.GetFloat(GameConstants.SFX_VOLUME_KEY, 1f));
    }
}
