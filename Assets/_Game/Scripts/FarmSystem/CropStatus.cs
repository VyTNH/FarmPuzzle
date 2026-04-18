using UnityEngine;

public class CropStatus : MonoBehaviour
{
    public enum CropNeed
    {
        None,       // Bình thường
        Water,      // Tưới nước
        PestControl,// Trừ sâu
        Weeding     // Dọn cỏ
    }
}

