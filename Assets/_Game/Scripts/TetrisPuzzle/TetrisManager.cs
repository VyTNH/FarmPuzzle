using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using FarmPuzzle.Core.Database;

namespace FarmPuzzle.Tetris
{
    public class TetrisManager : MonoBehaviour
    {
        public static TetrisManager Instance;

        [Header("UI")]
        public RectTransform boardContainer;
        public Image blockPrefab;
        public TetrisCropMapSO cropMapSO;
        public Text inventoryText;
        [HideInInspector] public Text questText; // Deprecated — dùng QuestPanelManager

        [Header("Settings")]
        public float fallSpeed = 1f;

        [Header("Grid Size")]
        public int width = 10;
        public int height = 24; // Mở rộng 4 dòng trên đỉnh làm Vùng Khởi Tạo Ẩn (Buffer Zone) -> Khi rơi qua Mask mới hiện!

        // ─── CONSTANTS ───
        private static readonly Color EMPTY_CELL_COLOR = new Color(0f, 0f, 0f, 0.2f);

        private Image[,] boardCells;
        private string[,] boardTypes; // Lưu ProductID của từng ô

        // Mảnh hiện tại
        private List<Vector2Int> currentPieceCoords = new List<Vector2Int>();
        public string currentProductID { get; private set; }
        private Sprite currentSprite;
        private Color currentColor;
        private Vector2Int currentPos;
        private float fallTimer;
        private GameObject[] currentPieceBlocks;

        public bool isPlaying = false;

        private readonly Vector2Int[][] Tetrominoes = new Vector2Int[][]
        {
            new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) }, // O
            new Vector2Int[] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) }, // I
            new Vector2Int[] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1) }, // T
            new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(-1,1) }, // S
            new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(1,1) }, // Z
            new Vector2Int[] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1) }, // L
            new Vector2Int[] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(-1,1) } // J
        };

        private void Awake()
        {
            Instance = this;
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            boardCells = new Image[width, height];
            boardTypes = new string[width, height];
            currentPieceBlocks = new GameObject[4];

            // Dọn container
            foreach (Transform child in boardContainer) Destroy(child.gameObject);

            float blockSize = boardContainer.rect.width / width;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Image cell = Instantiate(blockPrefab, boardContainer);
                    cell.gameObject.SetActive(true); // QUAN TRỌNG: Phải bật lên vì prefab đang tắt!
                    RectTransform rt = cell.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(blockSize - 2, blockSize - 2);
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(0, 0);
                    rt.pivot = new Vector2(0, 0);
                    rt.anchoredPosition = new Vector2(x * blockSize, y * blockSize);
                    
                    cell.color = EMPTY_CELL_COLOR;
                    boardCells[x, y] = cell;
                    boardTypes[x, y] = "";
                }
            }

            for (int i = 0; i < 4; i++)
            {
                Image pt = Instantiate(blockPrefab, boardContainer);
                RectTransform rt = pt.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(blockSize - 2, blockSize - 2);
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0, 0);
                pt.gameObject.SetActive(false);
                currentPieceBlocks[i] = pt.gameObject;
            }
        }

        private void OnEnable()
        {
            if (isPlaying) return;
            if (CameraDrag.Instance != null) CameraDrag.Instance.ResetZoom();
            StartGame();
        }

        public void StartGame()
        {
            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayer == null)
            {
                Debug.LogWarning("[Tetris] Cần đăng nhập để chơi.");
                return;
            }

            if (cropMapSO == null)
            {
                cropMapSO = Resources.Load<TetrisCropMapSO>("TetrisCropMap");
            }

            // Dọn bàn
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    boardTypes[x, y] = "";
                    boardCells[x, y].color = EMPTY_CELL_COLOR;
                    boardCells[x, y].sprite = null;
                }

            UpdateQuestUI();
            isPlaying = true;
            
            // 🚀 CĂN GIỮA TETRIS LÊN GIỮA MÀN HÌNH BẤT KỂ CAMERA VÀ RESOLUTION
            // Đảm bảo Pivot và AnchoredPosition của Object cha của BoardContainer được ôm sát camera gốc!
            if (boardContainer != null)
            {
                Canvas myCanvas = GetComponentInParent<Canvas>();
                if (myCanvas != null && myCanvas.renderMode == RenderMode.WorldSpace)
                {
                    if (Camera.main != null)
                    {
                        var camPos = Camera.main.transform.position;
                        myCanvas.transform.position = new Vector3(camPos.x, camPos.y, myCanvas.transform.position.z);
                    }
                }
                else if (myCanvas != null && (myCanvas.renderMode == RenderMode.ScreenSpaceOverlay || myCanvas.renderMode == RenderMode.ScreenSpaceCamera))
                {
                    // Nếu là UI phẳng, ép Root Panel về tọa độ tâm (0,0)
                    RectTransform panelRt = boardContainer.parent?.GetComponent<RectTransform>();
                    if (panelRt != null)
                    {
                        panelRt.anchoredPosition = Vector2.zero;
                    }
                }
            }
            
            SpawnPiece();
        }

        /// <summary>
        /// Deprecated — QuestPanelManager tự refresh thông qua QuestManager.OnProgressUpdated.
        /// Giữ lại để tránh compile error ở các script cũ gọi method này.
        /// </summary>
        public void UpdateQuestUI() { }

        public void GameOver()
        {
            if (!isPlaying) return;
            isPlaying = false;
            Debug.Log("<color=red>[Tetris]</color> GAME OVER!");
            // Kết thúc phiên quest (nếu chưa kết thúc)
            FarmPuzzle.Meta.QuestManager.Instance?.TriggerGameOver();
        }

        public void SpawnPiece()
        {
            // Lấy tất cả nông sản trong Kho từ RAM Session thay vì Database cứng (để bắt kịp nhịp test)
            var sessionInv = DataManager.Instance.GetSessionInventory();
            var cropInv = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>();
            
            foreach (var kvp in sessionInv)
            {
                if (kvp.Key.StartsWith("product_") && kvp.Value > 0)
                {
                    cropInv.Add(kvp);
                }
            }

            int totalStock = 0;
            if (cropInv.Count == 0)
            {
                Debug.LogWarning("[Tetris] KHO NÔNG SẢN RỖNG!!! Không thể sinh mảnh Tetris. Vui lòng trồng thêm nông sản.");
                if (inventoryText != null) inventoryText.text = "Bạn đã hết nông sản để chơi!";

                // MỚI: Game Over ngay lập tức vì không còn đạn để xếp!
                GameOver();
                return;
            }

            // Chọn ngẫu nhiên nông sản đang có trong kho
            var randomCrop = cropInv[Random.Range(0, cropInv.Count)];
            currentProductID = randomCrop.Key;
            totalStock = randomCrop.Value;
            if (inventoryText != null) inventoryText.text = $"Rơi: {currentProductID} | Kho: {totalStock}";

            if (cropMapSO != null)
            {
                var map = cropMapSO.GetMapping(currentProductID);
                if (map != null) { currentSprite = map.tetrisSprite; currentColor = map.fallBackColor; }
                else             { currentSprite = null; currentColor = Color.gray; }
            }
            else { currentSprite = null; currentColor = Color.gray; }
            

            int shapeIdx = Random.Range(0, Tetrominoes.Length);
            currentPieceCoords.Clear();
            foreach (var v in Tetrominoes[shapeIdx]) currentPieceCoords.Add(v);

            currentPos = new Vector2Int(width / 2, height - 2);

            if (!IsValidPosition(currentPos, currentPieceCoords))
            {
                GameOver();
                return;
            }

            UpdatePieceVisuals();
            fallTimer = fallSpeed;
        }

        private Vector2 touchStartPos;
        private bool isSwiping = false;

        private void Update()
        {
            if (!isPlaying) return;

            fallTimer -= Time.deltaTime;
            if (fallTimer <= 0)
            {
                fallTimer = fallSpeed;
                MoveDown();
            }

            // Keyboard testing fallbacks
            if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveLeft();
            if (Input.GetKeyDown(KeyCode.RightArrow)) MoveRight();
            if (Input.GetKeyDown(KeyCode.UpArrow)) Rotate();
            if (Input.GetKeyDown(KeyCode.DownArrow)) MoveDown();
            if (Input.GetKeyDown(KeyCode.Space)) HardDrop();

            // Mobile Swipe Logic
            if (Input.GetMouseButtonDown(0))
            {
                touchStartPos = Input.mousePosition;
                isSwiping = true;
            }
            if (Input.GetMouseButtonUp(0) && isSwiping)
            {
                Vector2 swipeDelta = (Vector2)Input.mousePosition - touchStartPos;
                isSwiping = false;
                
                // Mức độ nhạy (threshhold dài 50px)
                if (swipeDelta.magnitude > 50f) 
                {
                    if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
                    {
                        if (swipeDelta.x < 0) MoveLeft();
                        else MoveRight();
                    }
                    else
                    {
                        if (swipeDelta.y > 0) Rotate(); // Lên -> Xoay
                        else HardDrop();                // Xuống -> Thả Rơi
                    }
                }
            }
        }

        #region Điều Khiển
        public void MoveLeft()
        {
            if (!isPlaying) return;
            Debug.Log($"<color=cyan>[Tetris]</color> Bấm MOVE LEFT! currentPos: {currentPos}");
            Vector2Int newPos = currentPos + Vector2Int.left;
            if (IsValidPosition(newPos, currentPieceCoords))
            {
                currentPos = newPos;
                UpdatePieceVisuals();
            }
            else Debug.Log($"<color=red>[Tetris ERROR]</color> ValidPosition Left bị từ chối! Các ô kề đã có vật cản.");
        }

        public void MoveRight()
        {
            if (!isPlaying) return;
            Debug.Log($"<color=cyan>[Tetris]</color> Bấm MOVE RIGHT! currentPos: {currentPos}");
            Vector2Int newPos = currentPos + Vector2Int.right;
            if (IsValidPosition(newPos, currentPieceCoords))
            {
                currentPos = newPos;
                UpdatePieceVisuals();
            }
            else Debug.Log($"<color=red>[Tetris ERROR]</color> ValidPosition Right bị từ chối!");
        }

        public void Rotate()
        {
            if (!isPlaying) return;
            Debug.Log($"<color=orange>[Tetris]</color> Bấm ROTATE! Đang xoay khối tại {currentPos}");
            List<Vector2Int> newCoords = new List<Vector2Int>();
            foreach (var v in currentPieceCoords) newCoords.Add(new Vector2Int(v.y, -v.x)); // Rotate 90 deg cw

            if (IsValidPosition(currentPos, newCoords))
            {
                currentPieceCoords = newCoords;
                UpdatePieceVisuals();
            }
        }

        public void MoveDown()
        {
            if (!isPlaying) return;
            Vector2Int newPos = currentPos + Vector2Int.down;
            if (IsValidPosition(newPos, currentPieceCoords))
            {
                currentPos = newPos;
                UpdatePieceVisuals();
            }
            else LockPiece();
        }

        public void HardDrop()
        {
            if (!isPlaying) return;
            Debug.Log($"<color=red>[Tetris]</color> Bấm HARD DROP! Thả rơi tự do từ {currentPos}");
            while (IsValidPosition(currentPos + Vector2Int.down, currentPieceCoords))
            {
                currentPos += Vector2Int.down;
            }
            LockPiece();
        }
        #endregion

        private bool IsValidPosition(Vector2Int pos, List<Vector2Int> coords)
        {
            foreach (var c in coords)
            {
                int x = pos.x + c.x;
                int y = pos.y + c.y;

                if (x < 0 || x >= width || y < 0) return false;
                if (y < height && boardTypes[x, y] != "") return false;
            }
            return true;
        }

        private void UpdatePieceVisuals()
        {
            float blockSize = boardContainer.rect.width / width;

            for (int i = 0; i < 4; i++)
            {
                if (i < currentPieceCoords.Count)
                {
                    currentPieceBlocks[i].SetActive(true);
                    int x = currentPos.x + currentPieceCoords[i].x;
                    int y = currentPos.y + currentPieceCoords[i].y;
                    
                    RectTransform rt = currentPieceBlocks[i].GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(x * blockSize, y * blockSize);
                    
                    Image img = currentPieceBlocks[i].GetComponent<Image>();
                    img.sprite = currentSprite;
                    // Luôn dùng fallBackColor làm tint — kể cả khi có sprite để phân biệt loại nông sản
                    // (img.color trên Image = màu TINTing, không xóa sprite)
                    img.color = currentColor;
                }
                else currentPieceBlocks[i].SetActive(false);
            }
        }

        private void LockPiece()
        {
            foreach (var c in currentPieceCoords)
            {
                int x = currentPos.x + c.x;
                int y = currentPos.y + c.y;

                if (y >= height)
                {
                    GameOver();
                    return;
                }

                boardTypes[x, y] = currentProductID;
                boardCells[x, y].sprite = currentSprite;
                // Tô màu fallBackColor lên ô đã lock — giữ nguyên màu map cho ô đã rơi
                boardCells[x, y].color = currentColor;
            }

            Debug.Log($"<color=orange>[Tetris]</color> Khối đã khóa tại {currentPos}. Đổi màu mặt đất thành White.");

            foreach (var b in currentPieceBlocks) b.SetActive(false);

            CheckLines();
            SpawnPiece();
        }

        private void CheckLines()
        {
            for (int y = 0; y < height; y++)
            {
                if (!IsRowFull(y)) continue;

                // ── Score: +10 điểm cố định mỗi hàng xóa ──
                FarmPuzzle.Meta.QuestManager.Instance?.AddScore(10);

                var destroyedCrops = CollectAndClearRow(y);

                foreach (var kvp in destroyedCrops)
                {
                    Debug.Log($"<color=green>[Tetris]</color> Xóa hàng thành công: {kvp.Value}x {kvp.Key}");
                    DataManager.Instance?.RemoveItem(kvp.Key, kvp.Value);
                    FarmPuzzle.Meta.QuestManager.Instance?.UpdateProgress(kvp.Key, kvp.Value);
                }

                PullRowsDown(y);
                y--;
            }

            UpdateQuestUI();
        }

        private bool IsRowFull(int y)
        {
            for (int x = 0; x < width; x++)
                if (boardTypes[x, y] == "") return false;
            return true;
        }

        private Dictionary<string, int> CollectAndClearRow(int y)
        {
            var destroyed = new Dictionary<string, int>();
            for (int x = 0; x < width; x++)
            {
                string pid = boardTypes[x, y];
                if (!string.IsNullOrEmpty(pid))
                {
                    destroyed[pid] = destroyed.GetValueOrDefault(pid, 0) + 1;
                    boardTypes[x, y] = "";
                }
            }
            return destroyed;
        }

        private void PullRowsDown(int fromY)
        {
            for (int pullY = fromY; pullY < height - 1; pullY++)
                for (int x = 0; x < width; x++)
                {
                    boardTypes[x, pullY] = boardTypes[x, pullY + 1];
                    boardCells[x, pullY].sprite = boardCells[x, pullY + 1].sprite;
                    boardCells[x, pullY].color = boardCells[x, pullY + 1].color;
                }

            for (int x = 0; x < width; x++)
            {
                boardTypes[x, height - 1] = "";
                boardCells[x, height - 1].sprite = null;
                boardCells[x, height - 1].color = EMPTY_CELL_COLOR;
            }
        }
    }
}
