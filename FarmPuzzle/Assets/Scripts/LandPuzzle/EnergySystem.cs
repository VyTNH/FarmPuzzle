using UnityEngine;

namespace FarmPuzzle.LandPuzzle
{
    /// <summary>
    /// Quản lý năng lượng của người chơi.
    /// Mỗi lần mở ô đất chơi Block Puzzle tốn 1 năng lượng.
    /// Singleton — truy cập qua EnergySystem.Instance.
    /// </summary>
    public class EnergySystem : MonoBehaviour
    {
        public static EnergySystem Instance { get; private set; }

        [Header("Energy Settings")]
        [SerializeField] private int _maxEnergy     = 5;
        [SerializeField] private int _currentEnergy = 5;

        // Events
        public System.Action<int, int> OnEnergyChanged; // (current, max)
        public System.Action           OnEnergyEmpty;

        public int MaxEnergy     => _maxEnergy;
        public int CurrentEnergy => _currentEnergy;
        public bool HasEnergy    => _currentEnergy > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>Tiêu 1 năng lượng. Trả về false nếu hết năng lượng.</summary>
        public bool ConsumeEnergy(int amount = 1)
        {
            if (_currentEnergy < amount) return false;
            _currentEnergy -= amount;
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
            if (_currentEnergy <= 0) OnEnergyEmpty?.Invoke();
            return true;
        }

        /// <summary>Thêm năng lượng (refill, hoặc mua bổ sung).</summary>
        public void AddEnergy(int amount)
        {
            _currentEnergy = Mathf.Min(_currentEnergy + amount, _maxEnergy);
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
        }

        /// <summary>Đặt lại về max (dùng trong Editor test).</summary>
        public void RefillAll()
        {
            _currentEnergy = _maxEnergy;
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Consume 1")]
        private void DebugConsume() => ConsumeEnergy(1);
        [ContextMenu("Debug: Refill")]
        private void DebugRefill() => RefillAll();
#endif
    }
}
