using UnityEngine;

public class AudioObserver : MonoBehaviour
{
    public static AudioObserver Instance { get; private set; }

    [Header("Âm thanh chính")]
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField] private AudioClip coinSoundClip;
    [SerializeField] private AudioClip winSoundClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Phát Nhạc nền (BGM)
    /// </summary>
    public void PlayBackgroundMusic()
    {
        SoundManager.Instance.PlayMusic(backgroundMusicClip);
    }

    /// <summary>
    /// Phát tiếng nhận xu (Coin)
    /// </summary>
    public void PlayCoinSound()
    {
        SoundManager.Instance.PlaySFX(coinSoundClip);
    }

    /// <summary>
    /// Phát tiếng thắng cuộc (Win)
    /// </summary>
    public void PlayWinSound()
    {
        SoundManager.Instance.PlaySFX(winSoundClip);
    }
}
