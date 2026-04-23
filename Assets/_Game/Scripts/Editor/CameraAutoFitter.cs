using UnityEngine;

namespace FarmPuzzle.EditorTools
{
    [RequireComponent(typeof(Camera))]
    public class CameraAutoFitter : MonoBehaviour
    {
        [Header("Grid Bounds")]
        public float gridWidth = 5.5f; // Chiều rộng lưới Nông trại
        public float gridHeight = 5.5f; // Chiều cao lưới Nông trại
        
        [Header("Margin")]
        public float margin = 0.5f; // Mép lề an toàn

        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void Update()
        {
            if (_cam == null || !_cam.orthographic) return;

            float screenRatio = (float)Screen.width / (float)Screen.height;
            float targetRatio = gridWidth / gridHeight;

            // Tính toán Orthographic Size dựa trên chiều RỘNG (Màn hình dọc)
            float requiredSizeForWidth = (gridWidth + margin) / 2f / screenRatio;
            
            // Tính toán Size dựa trên chiều CAO (Phòng khi màn hình ngang)
            float requiredSizeForHeight = (gridHeight + margin) / 2f;

            // Lấy mức tối đa để không bao giờ bị cắt mất viền
            _cam.orthographicSize = Mathf.Max(requiredSizeForWidth, requiredSizeForHeight);
        }
    }
}
