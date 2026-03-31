using UnityEngine;
using UnityEngine.EventSystems;
using FarmPuzzle.LandPuzzle.Data;

namespace FarmPuzzle.LandPuzzle.Farm
{
    /// <summary>
    /// Component trên mỗi ô đất (FarmLandTile).
    /// • Hiển thị trạng thái đất (khóa / mở)
    /// • Click vào ô bị khóa → kiểm tra năng lượng → mở Block Puzzle
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class FarmLandTile : MonoBehaviour, IPointerClickHandler
    {
        [Header("Config")]
        [SerializeField] private Vector2Int _farmGridPosition;
        [SerializeField] private string     _zoneId = "zone_1";
        [SerializeField] private LevelData  _levelData;   // Level puzzle gắn với ô đất này

        [Header("State")]
        [SerializeField] private bool _isUnlocked = false;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Color _lockedColor   = new Color(0.60f, 0.45f, 0.30f);
        [SerializeField] private Color _unlockedColor = new Color(0.55f, 0.78f, 0.35f);

        // Events
        /// <summary>Fired khi người chơi click vào ô đất bị khóa và đủ năng lượng.</summary>
        public System.Action<FarmLandTile> OnTileClicked;
        public System.Action<FarmLandTile> OnTileUnlocked;

        // Properties
        public bool       IsUnlocked        => _isUnlocked;
        public Vector2Int FarmGridPosition  => _farmGridPosition;
        public string     ZoneId            => _zoneId;
        public LevelData  LevelData         => _levelData;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            var col = GetComponent<BoxCollider2D>();
            col.size = Vector2.one;  // Tự fit theo scale
            UpdateVisual();
        }

        // ── Click Handler ──────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isUnlocked) return;   // Đã mở → không cần click

            var energy = EnergySystem.Instance;
            if (energy == null || !energy.HasEnergy)
            {
                Debug.Log("[FarmTile] Hết năng lượng!");
                // TODO: Hiển thị popup "Hết năng lượng"
                return;
            }

            // Tiêu 1 năng lượng
            energy.ConsumeEnergy(1);

            // Notify manager mở puzzle
            OnTileClicked?.Invoke(this);
            Debug.Log($"[FarmTile] Clicked tile {_farmGridPosition} — opening Block Puzzle");
        }

        // ── State Change ──────────────────────────────────────────────────────

        /// <summary>Mở khóa ô đất sau khi thắng puzzle.</summary>
        public void Unlock()
        {
            if (_isUnlocked) return;
            _isUnlocked = true;
            UpdateVisual();
            OnTileUnlocked?.Invoke(this);
            Debug.Log($"[FarmTile] Tile {_farmGridPosition} UNLOCKED!");
        }

        // ── Editor helpers ─────────────────────────────────────────────────────
        public void ForceUnlock()  => Unlock();
        public void ResetToLocked() { _isUnlocked = false; UpdateVisual(); }

        // ── Visual ────────────────────────────────────────────────────────────

        private void UpdateVisual()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
                _spriteRenderer.color = _isUnlocked ? _unlockedColor : _lockedColor;
        }

#if UNITY_EDITOR
        private void OnValidate() => UpdateVisual();
#endif
    }
}
