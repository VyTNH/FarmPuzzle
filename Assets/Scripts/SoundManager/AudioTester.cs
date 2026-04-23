using UnityEngine;


public class AudioTester : MonoBehaviour
{
    private void Update()
    {
        // Kiểm tra nếu phím số 1 được nhấn (Legacy Input)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AudioObserver.Instance.PlayBackgroundMusic();
        }

        // Phím số 2
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AudioObserver.Instance.PlayCoinSound();
        }

        // Phím số 3
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            AudioObserver.Instance.PlayWinSound();
        }
    }

    /// <summary>
    /// Các hàm này vẫn giữ nguyên để bạn gán vào UI Button
    /// </summary>
    public void TestBackgroundMusic() => AudioObserver.Instance.PlayBackgroundMusic();
    public void TestCoinSound() => AudioObserver.Instance.PlayCoinSound();
    public void TestWinSound() => AudioObserver.Instance.PlayWinSound();
}
