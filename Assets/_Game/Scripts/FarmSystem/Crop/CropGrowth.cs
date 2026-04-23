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
        public Image  needIconUI; // Dành cho ai dùng Canvas cũ
        public SpriteRenderer needIconSprite; // ƯU TIÊN SỬ DỤNG CHO PIXEL GAME!
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
        private float         _bonusSeconds = 0f; 

        public bool IsHarvestable => _isHarvestable && _currentNeed == CropNeedType.None; 
        public CropNeedType CurrentNeed => _currentNeed;

        public void Initialize(CropDataModel data, long plantedTicks)
        {
            _cropData = data;
            _plantedTime = new DateTime(plantedTicks, DateTimeKind.Utc);
            _totalGrowthSeconds = data.GrowSeconds; 
            _bonusSeconds = 0f;

            if (needIconUI != null) needIconUI.gameObject.SetActive(false);
            if (needIconSprite != null) needIconSprite.gameObject.SetActive(false);
            if (harvestIcon != null) harvestIcon.gameObject.SetActive(false);
            
            // Tắt raycastTarget trên tất cả UI con để không chặn click vào LandPlot bên dưới
            DisableCropCanvasRaycast();
            
            UpdateGrowth();
        }

        /// <summary>
        /// Tắt raycastTarget trên mọi Image/Text trong Crop Canvas.
        /// Điều này đảm bảo Physics2DRaycaster vẫn detect được PolygonCollider2D của LandPlot
        /// dù Canvas WorldSpace đang hiển thị UI cây trồng bên trên.
        /// </summary>
        private void DisableCropCanvasRaycast()
        {
            // Tìm tất cả Graphic (Image, Text, RawImage...) trong cùng GameObject và con cháu
            var graphics = GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            foreach (var g in graphics)
                g.raycastTarget = false;
            
            // Tắt luôn GraphicRaycaster trên Canvas nếu có (nguồn gốc chặn UI)
            var raycasters = GetComponentsInChildren<UnityEngine.UI.GraphicRaycaster>(true);
            foreach (var r in raycasters)
                r.enabled = false;
        }

        private void Update()
        {
            if (_isHarvestable && _currentNeed == CropNeedType.None) return;
            UpdateGrowth();
        }

        private void UpdateGrowth()
        {
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
                // Xóa mọi nhu cầu còn tồn đọng khi cây đã chín hoàn toàn.
                // Nếu không làm điều này, _currentNeed != None sẽ khiến IsHarvestable = false
                // dù _isHarvestable = true → player không thu hoạch được trong cùng phiên chơi.
                if (_currentNeed != CropNeedType.None)
                {
                    _currentNeed = CropNeedType.None;
                    if (needIconUI != null)     needIconUI.gameObject.SetActive(false);
                    if (needIconSprite != null) needIconSprite.gameObject.SetActive(false);
                }
                _isHarvestable = true;
                UpdateHarvestVisual();
            }
        }

        public void ApplyTimeBoost(float seconds)
        {
            _bonusSeconds += seconds;
            UpdateGrowth();
        }

        private void CheckForNeeds(float progress)
        {
            if (_currentNeed != CropNeedType.None) return;

            if (progress > 0.4f && progress < 0.5f) ShowNeed(CropNeedType.Water);
            else if (progress > 0.8f && progress < 0.9f) ShowNeed(CropNeedType.Pest);
        }

        public void ShowNeed(CropNeedType type)
        {
            // Không áp dụng nhu cầu chăm sóc nếu cây đã chín rồi
            if (_isHarvestable) return;

            _currentNeed = type;
            
            if (needIconUI != null) 
            {
                needIconUI.gameObject.SetActive(true);
                switch (type)
                {
                    case CropNeedType.Water:      needIconUI.sprite = waterIconSprite; break;
                    case CropNeedType.Pest:       needIconUI.sprite = pestIconSprite; break;
                    case CropNeedType.Fertilizer: needIconUI.sprite = fertilizerIconSprite; break;
                }
            }

            if (needIconSprite != null)
            {
                needIconSprite.gameObject.SetActive(true);
                switch (type)
                {
                    case CropNeedType.Water:      needIconSprite.sprite = waterIconSprite; break;
                    case CropNeedType.Pest:       needIconSprite.sprite = pestIconSprite; break;
                    case CropNeedType.Fertilizer: needIconSprite.sprite = fertilizerIconSprite; break;
                }
            }
            
            UpdateHarvestVisual(); 
        }

        public void ResolveNeed(CropNeedType toolType)
        {
            if (toolType == _currentNeed)
            {
                _currentNeed = CropNeedType.None;
                if (needIconUI != null) needIconUI.gameObject.SetActive(false);
                if (needIconSprite != null) needIconSprite.gameObject.SetActive(false);
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
