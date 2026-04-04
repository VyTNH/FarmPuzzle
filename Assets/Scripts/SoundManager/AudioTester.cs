using UnityEngine;
using UnityEngine.InputSystem; // Namespace cho Input System mới

public class AudioTester : MonoBehaviour
{
    private void Update()
    {
        // Kiểm tra nếu phím số 1 được nhấn (Sử dụng Keyboard.current)
        if (Keyboard.current != null)
        {
            // Phím số 1
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                AudioObserver.Instance.PlayBackgroundMusic();
            }

            // Phím số 2
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                AudioObserver.Instance.PlayCoinSound();
            }

            // Phím số 3
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                AudioObserver.Instance.PlayWinSound();
            }
        }
    }

    /// <summary>
    /// Các hàm này vẫn giữ nguyên để bạn gán vào UI Button
    /// </summary>
    public void TestBackgroundMusic() => AudioObserver.Instance.PlayBackgroundMusic();
    public void TestCoinSound() => AudioObserver.Instance.PlayCoinSound();
    public void TestWinSound() => AudioObserver.Instance.PlayWinSound();
}
