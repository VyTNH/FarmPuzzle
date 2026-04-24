using UnityEngine;
using FarmPuzzle.FarmSystem.Crop;

namespace FarmPuzzle.FarmSystem.Visual
{
    /// <summary>
    /// Thay đổi Sprite của cây dựa trên tiến trình lớn lên (Growth Stages).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CropDisplay : MonoBehaviour
    {
        [Header("Sprites Stages")]
        public Sprite seedSprite;         // Giai đoạn mới trồng (0%)
        public Sprite growingSprite;      // Giai đoạn đang lớn (50%)
        public Sprite harvestableSprite;  // Giai đoạn đã chín (100%)

        private SpriteRenderer _sr;
        private CropGrowth     _growth;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();

            // [FIX Bug #2] Tìm CropGrowth theo cả 3 hướng để tránh miss
            _growth = GetComponent<CropGrowth>();                        // Chính nó
            if (_growth == null) _growth = GetComponentInParent<CropGrowth>();  // Cha
            if (_growth == null) _growth = GetComponentInChildren<CropGrowth>(); // Con

            if (_growth == null)
                Debug.LogWarning($"[CropDisplay] Không tìm thấy CropGrowth trên '{gameObject.name}' hoặc parent/children của nó!");
        }

        private void Update()
        {
            if (_growth == null || _sr == null) return;

            // [FIX] Đọc progress trực tiếp từ CropGrowth (không phụ thuộc slider)
            float progress = _growth.GetProgress();

            // Nếu cây chưa được Initialize (ticks = 0) → progress = 0 → seedSprite, OK

            Sprite target;
            if (progress >= 1f)
                target = harvestableSprite;
            else if (progress > 0.4f)
                target = growingSprite;
            else
                target = seedSprite;

            // Chỉ gán khi thực sự thay đổi để tránh dirty mark liên tục
            if (_sr.sprite != target)
                _sr.sprite = target;
        }
    }
}
