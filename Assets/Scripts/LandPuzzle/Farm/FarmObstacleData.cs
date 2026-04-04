using UnityEngine;

namespace FarmPuzzle.LandPuzzle.Farm
{
    /// <summary>
    /// ScriptableObject định nghĩa config cho 1 loại chướng ngại vật trên Farm Map.
    /// Ví dụ: Đá tảng (3 durability), Cây khô (2 durability), Bụi gai (1 durability).
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FarmObstacle", menuName = "FarmPuzzle/Farm/Obstacle Data")]
    public class FarmObstacleData : ScriptableObject
    {
        [Tooltip("Tên chướng ngại vật (VD: Đá tảng, Cây khô, Bụi gai)")]
        public string obstacleName;

        [Tooltip("Độ bền tối đa (1-3). Cần bấy nhiêu lần line clear liền kề để phá.")]
        [Range(1, 3)]
        public int maxDurability = 1;

        [Tooltip("Sprite theo stage durability. Index 0 = nguyên vẹn, index cuối = sắp vỡ.")]
        public Sprite[] durabilitySprites;

        [Tooltip("Sprite ô đất sau khi unlock (không còn obstacle).")]
        public Sprite unlockedTileSprite;

        [Tooltip("VFX khi bị damage.")]
        public GameObject damageVFXPrefab;

        [Tooltip("VFX khi bị phá hủy (unlock đất).")]
        public GameObject destroyVFXPrefab;

        [Tooltip("Âm thanh khi bị damage.")]
        public AudioClip damageSound;

        [Tooltip("Âm thanh khi bị phá hủy.")]
        public AudioClip destroySound;

        /// <summary>
        /// Lấy sprite tương ứng với durability hiện tại.
        /// </summary>
        public Sprite GetSpriteForDurability(int currentDurability)
        {
            if (durabilitySprites == null || durabilitySprites.Length == 0)
                return null;

            int stageIndex = maxDurability - currentDurability;
            stageIndex = Mathf.Clamp(stageIndex, 0, durabilitySprites.Length - 1);
            return durabilitySprites[stageIndex];
        }

        private void OnValidate()
        {
            if (maxDurability < 1) maxDurability = 1;
            if (maxDurability > 3) maxDurability = 3;
        }
    }
}
