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
            _growth = GetComponentInParent<CropGrowth>();
            if (_growth == null) _growth = GetComponent<CropGrowth>();
        }

        private void Update()
        {
            if (_growth == null || _sr == null) return;

            // Cập nhật Sprite dựa trên thanh slider hoặc progress
            float progress = 0;
            if (_growth.progressSlider != null) 
                progress = _growth.progressSlider.value;

            if (progress >= 1f)
            {
                _sr.sprite = harvestableSprite;
            }
            else if (progress > 0.4f)
            {
                _sr.sprite = growingSprite;
            }
            else
            {
                _sr.sprite = seedSprite;
            }
        }
    }
}
