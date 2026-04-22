using UnityEngine;
using System;

namespace FarmPuzzle.LandPuzzle
{
    /// <summary>
    /// Quản lý năng lượng của người chơi.
    /// Mỗi lần mở ô đất chơi Block Puzzle tốn 1 năng lượng.
    /// Singleton — hỗ trợ hồi phục tự động theo thời gian (Online & Offline).
    /// </summary>
    public class EnergySystem : MonoBehaviour
    {
        public static EnergySystem Instance { get; private set; }

        [Header("Energy Settings")]
        [SerializeField] private int _maxEnergy = 5;
        [SerializeField] private int _currentEnergy = 5;
        [SerializeField] private float _regenDuration = 120f; // 2 phút mỗi điểm

        // Events
        public System.Action<int, int> OnEnergyChanged; 
        public System.Action OnEnergyEmpty;

        private float _timer;
        private const string PREF_LAST_TIME = "Energy_LastRegenTime";

        public int MaxEnergy => _maxEnergy;
        public int CurrentEnergy => _currentEnergy;
        public bool HasEnergy => _currentEnergy > 0;

        /// <summary>Trả về số giây còn lại cho lần hồi tiếp theo.</summary>
        public float TimeToNextRegen => _currentEnergy < _maxEnergy ? (_regenDuration - _timer) : 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadAndCalculateOfflineRegen();
        }

        private void Update()
        {
            if (_currentEnergy < _maxEnergy)
            {
                _timer += Time.deltaTime;
                if (_timer >= _regenDuration)
                {
                    _timer = 0;
                    AddEnergy(1);
                    SaveCurrentTime();
                }
            }
            else
            {
                _timer = 0;
            }
        }

        private void LoadAndCalculateOfflineRegen()
        {
            if (PlayerPrefs.HasKey(PREF_LAST_TIME))
            {
                string lastTimeStr = PlayerPrefs.GetString(PREF_LAST_TIME);
                if (long.TryParse(lastTimeStr, out long lastTicks))
                {
                    DateTime lastTime = new DateTime(lastTicks);
                    double elapsedSeconds = (DateTime.UtcNow - lastTime).TotalSeconds;

                    if (elapsedSeconds > 0 && _currentEnergy < _maxEnergy)
                    {
                        int pointsToAdd = (int)(elapsedSeconds / _regenDuration);
                        float remainder = (float)(elapsedSeconds % _regenDuration);

                        if (pointsToAdd > 0)
                        {
                            AddEnergy(pointsToAdd);
                        }
                        
                        // Kế thừa thời gian lẻ từ đợt offline
                        _timer = remainder;
                    }
                }
            }
            else
            {
                SaveCurrentTime();
            }
        }

        private void SaveCurrentTime()
        {
            PlayerPrefs.SetString(PREF_LAST_TIME, DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveCurrentTime();
            else LoadAndCalculateOfflineRegen();
        }

        /// <summary>Tiêu năng lượng. Trả về false nếu không đủ.</summary>
        public bool ConsumeEnergy(int amount = 1)
        {
            if (_currentEnergy < amount) return false;
            
            bool wasFull = (_currentEnergy == _maxEnergy);
            _currentEnergy -= amount;
            
            // Nếu vừa bắt đầu hụt Max, khởi động timer từ 0
            if (wasFull) _timer = 0;
            
            SaveCurrentTime();
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
            
            if (_currentEnergy <= 0) OnEnergyEmpty?.Invoke();
            return true;
        }

        public void AddEnergy(int amount)
        {
            _currentEnergy = Mathf.Min(_currentEnergy + amount, _maxEnergy);
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
        }

        public void RefillAll()
        {
            _currentEnergy = _maxEnergy;
            _timer = 0;
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
            SaveCurrentTime();
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Consume 1")]
        private void DebugConsume() => ConsumeEnergy(1);
        [ContextMenu("Debug: Refill")]
        private void DebugRefill() => RefillAll();
#endif
    }
}
