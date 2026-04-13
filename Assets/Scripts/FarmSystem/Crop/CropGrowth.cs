using UnityEngine;
using UnityEngine.UI;
using System;
using FarmPuzzle.Core.Database;

namespace FarmPuzzle.FarmSystem.Crop
{
    public enum CropNeedType { None, Water, Pest, Fertilizer }

    /// <summary>
    /// Quản lý vòng đời trưởng thành và nhu cầu của cây trồng.
    /// </summary>
    public class CropGrowth : MonoBehaviour
    {
        [Header("UI References")]
        public Slider progressSlider;
        public Image  needIcon;
        public GameObject harvestIcon;    // Hiện lên khi chín
        public Text   timerText;

        [Header("Icons")]
        public Sprite waterIconSprite;
        public Sprite pestIconSprite;
        public Sprite fertilizerIconSprite;

        private CropDataModel _cropData;
        private DateTime      _plantedTime;
        private float         _totalGrowthSeconds;
        
        private CropNeedType  _currentNeed = CropNeedType.None;
        private bool          _isHarvestable = false;
        private float         _bonusSeconds = 0f; // 🚀 Thời gian đã được "buff"

        public bool IsHarvestable => _isHarvestable && _currentNeed == CropNeedType.None; 
        public CropNeedType CurrentNeed => _currentNeed;

        public void Initialize(CropDataModel data, long plantedTicks)
        {
            _cropData = data;
            _plantedTime = new DateTime(plantedTicks, DateTimeKind.Utc);
            _totalGrowthSeconds = data.GrowSeconds; 
            _bonusSeconds = 0f;

            if (needIcon != null) needIcon.gameObject.SetActive(false);
            if (harvestIcon != null) harvestIcon.gameObject.SetActive(false);
            
            UpdateGrowth();
        }

        private void Update()
        {
            if (_isHarvestable && _currentNeed == CropNeedType.None) return;
            UpdateGrowth();
        }

        private void UpdateGrowth()
        {
            // 🎯 TIẾN ĐỘ = (Thời gian đã trôi qua + Thời gian được buff) / Tổng thời gian
            double actualElapsed = (DateTime.UtcNow - _plantedTime).TotalSeconds;
            float progress = Mathf.Clamp01((float)(actualElapsed + _bonusSeconds) / _totalGrowthSeconds);

            if (progressSlider != null) 
            {
                progressSlider.gameObject.SetActive(progress < 1f || _currentNeed != CropNeedType.None);
                progressSlider.value = progress;
            }
            
            if (timerText != null)
            {
                float remaining = Mathf.Max(0, _totalGrowthSeconds - (float)(actualElapsed + _bonusSeconds));
                TimeSpan t = TimeSpan.FromSeconds(remaining);
                timerText.text = progress >= 1f ? "CHÍN RỒI!" : string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
            }

            CheckForNeeds(progress);

            if (progress >= 1f && !_isHarvestable)
            {
                _isHarvestable = true;
                UpdateHarvestVisual();
            }
        }

        public void ApplyTimeBoost(float seconds)
        {
            _bonusSeconds += seconds;
            Debug.Log($"[LOG-CROP] Đã bón phân! Giảm {seconds}s. Tổng bonus: {_bonusSeconds}s");
            UpdateGrowth();
        }

        private void CheckForNeeds(float progress)
        {
            // Chỉ hiện nhu cầu nếu chưa có nhu cầu nào đang chờ xử lý
            if (_currentNeed != CropNeedType.None) return;

            if (progress > 0.4f && progress < 0.5f) ShowNeed(CropNeedType.Water);
            else if (progress > 0.8f && progress < 0.9f) ShowNeed(CropNeedType.Pest);
        }

        public void ShowNeed(CropNeedType type)
        {
            _currentNeed = type;
            if (needIcon == null) return;

            needIcon.gameObject.SetActive(true);
            switch (type)
            {
                case CropNeedType.Water:      needIcon.sprite = waterIconSprite; break;
                case CropNeedType.Pest:       needIcon.sprite = pestIconSprite; break;
                case CropNeedType.Fertilizer: needIcon.sprite = fertilizerIconSprite; break;
            }
            UpdateHarvestVisual(); // Ẩn cái icon thu hoạch đi cho đến khi chăm sóc xong!
        }

        public void ResolveNeed(CropNeedType toolType)
        {
            if (toolType == _currentNeed)
            {
                _currentNeed = CropNeedType.None;
                if (needIcon != null) needIcon.gameObject.SetActive(false);
                UpdateHarvestVisual(); 
            }
        }

        private void UpdateHarvestVisual()
        {
            bool ready = _isHarvestable && _currentNeed == CropNeedType.None;
            if (harvestIcon != null) harvestIcon.SetActive(ready);
        }
    }
}
