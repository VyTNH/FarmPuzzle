using System.Collections.Generic;
using UnityEngine;
using FarmPuzzle.LandPuzzle.Data;
using FarmPuzzle.LandPuzzle.Grid;

namespace FarmPuzzle.LandPuzzle.Block
{
    /// <summary>
    /// Sinh ra batch gồm N block pieces (mặc định 3).
    /// Khi tất cả block trong batch đã dùng xong → sinh batch mới.
    /// </summary>
    public class BlockSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridBoard _gridBoard;
        [SerializeField] private GameObject _blockPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private Transform[] _spawnSlots;
        //[SerializeField] private float _spawnScale = 0.6f;

        // --- State ---
        private LevelData _currentLevel;
        private List<LandBlock> _currentBatch = new List<LandBlock>();
        private int _usedBlockCount = 0;

        // --- Events ---
        /// <summary>Khi tất cả block trong batch đã dùng xong.</summary>
        public System.Action OnBatchExhausted;

        /// <summary>Khi block được đặt thành công.</summary>
        public System.Action<ShapeData, Vector2Int> OnBlockPlaced;

        /// <summary>
        /// Khởi tạo spawner với level data.
        /// </summary>
        public void Initialize(LevelData levelData, GridBoard gridBoard)
        {
            _currentLevel = levelData;
            _gridBoard = gridBoard;
            _usedBlockCount = 0;
        }

        /// <summary>
        /// Sinh ra 1 batch block mới.
        /// </summary>
        public void SpawnNewBatch()
        {
            ClearCurrentBatch();

            int batchSize = _currentLevel.blocksPerBatch;

            // Đảm bảo có đủ spawn slots
            if (_spawnSlots == null || _spawnSlots.Length < batchSize)
            {
                Debug.LogError("[BlockSpawner] Không đủ spawn slots!");
                return;
            }

            for (int i = 0; i < batchSize; i++)
            {
                // Random chọn shape từ pool
                ShapeData randomShape = GetRandomShape();

                // Tạo block
                LandBlock block = CreateBlock(randomShape, _spawnSlots[i]);
                _currentBatch.Add(block);
            }

            _usedBlockCount = 0;
        }

        /// <summary>
        /// Random chọn 1 shape từ pool available shapes.
        /// </summary>
        private ShapeData GetRandomShape()
        {
            if (_currentLevel.availableShapes == null || _currentLevel.availableShapes.Length == 0)
            {
                Debug.LogError("[BlockSpawner] Không có shapes trong level data!");
                return null;
            }

            int randomIndex = Random.Range(0, _currentLevel.availableShapes.Length);
            return _currentLevel.availableShapes[randomIndex];
        }

        /// <summary>
        /// Tạo 1 block GameObject tại vị trí spawn slot.
        /// </summary>
        private LandBlock CreateBlock(ShapeData shapeData, Transform spawnSlot)
        {
            GameObject blockObj;

            if (_blockPrefab != null)
            {
                blockObj = Instantiate(_blockPrefab, spawnSlot.position, Quaternion.identity, transform);
            }
            else
            {
                blockObj = new GameObject($"Block_{shapeData.name}");
                blockObj.transform.position = spawnSlot.position;
                blockObj.transform.SetParent(transform);

                // Thêm collider cho drag detection
                var collider = blockObj.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(2f, 2f);
            }

                // Đặt Scale của Parent bằng gốc GridBoard (Child cells bên trong LandBlock đã tự nạp CellSize)
                blockObj.transform.localScale = _gridBoard.transform.localScale;

            LandBlock block = blockObj.GetComponent<LandBlock>();
            if (block == null) block = blockObj.AddComponent<LandBlock>();

            block.Initialize(shapeData, _gridBoard);
            block.OnBlockPlaced = HandleBlockPlaced;

            return block;
        }

        /// <summary>
        /// Xử lý khi 1 block trong batch được đặt thành công.
        /// </summary>
        private void HandleBlockPlaced(LandBlock block, ShapeData shape, Vector2Int anchor)
        {
            _usedBlockCount++;

            OnBlockPlaced?.Invoke(shape, anchor);

            // Kiểm tra hết batch chưa
            if (_usedBlockCount >= _currentBatch.Count)
            {
                OnBatchExhausted?.Invoke();
            }
        }

        /// <summary>
        /// Xóa batch hiện tại.
        /// </summary>
        private void ClearCurrentBatch()
        {
            foreach (var block in _currentBatch)
            {
                if (block != null)
                    Destroy(block.gameObject);
            }
            _currentBatch.Clear();
        }

        /// <summary>
        /// Kiểm tra có block nào trong batch hiện tại có thể đặt được không.
        /// Dùng để check Game Over.
        /// </summary>
        public bool HasAnyValidMove()
        {
            foreach (var block in _currentBatch)
            {
                if (block == null || block.IsPlaced) continue;

                if (_gridBoard.CanPlaceShapeAnywhere(block.ShapeData))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Lấy số block chưa dùng trong batch hiện tại.
        /// </summary>
        public int GetRemainingBlockCount()
        {
            int count = 0;
            foreach (var block in _currentBatch)
            {
                if (block != null && !block.IsPlaced) count++;
            }
            return count;
        }

        private void OnDestroy()
        {
            ClearCurrentBatch();
        }
    }
}
