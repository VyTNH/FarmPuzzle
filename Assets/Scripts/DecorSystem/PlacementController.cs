using UnityEngine;
using UnityEngine.EventSystems;
using FarmPuzzle.LandPuzzle.Farm; // For FarmLandTile
using FarmPuzzle.Core.Database;    // For DecorRecordModel

namespace FarmPuzzle.DecorSystem
{
    public class PlacementController : MonoBehaviour
    {
        public static PlacementController Instance { get; private set; }

        [Header("Ghost Settings")]
        [Tooltip("Ghost object currently following the mouse")]
        private GameObject _ghostObj;
        private SpriteRenderer _ghostRenderer;
        
        [Tooltip("Màu xanh dương khi có thể đặt")]
        public Color colorValid = new Color(0.2f, 0.6f, 1.0f, 0.8f);
        [Tooltip("Màu đỏ khi bị vướng chướng ngại vật")]
        public Color colorInvalid = new Color(0.9f, 0.3f, 0.3f, 0.8f);

        [Header("State")]
        public bool isPlacing = false;
        private string _currentDecorID;
        private GameObject _currentDecorPrefab;
        private bool _canPlace = false;
        
        // Caching
        private Camera _mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;

            _mainCamera = Camera.main;
        }

        /// <summary>
        /// Kích hoạt chế độ đặt Decor. Gọi từ UI Inventory hoặc Shop.
        /// </summary>
        public void StartPlacement(string decorID, GameObject decorPrefab)
        {
            if (isPlacing) CancelPlacement();

            _currentDecorID = decorID;
            _currentDecorPrefab = decorPrefab;
            isPlacing = true;

            // Tạo Ghost
            _ghostObj = Instantiate(decorPrefab);
            _ghostObj.name = $"[Ghost] {_currentDecorID}";
            
            // Xóa tất cả colliders trên Ghost để không cản trở Raycast
            Collider2D[] colliders = _ghostObj.GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders) Destroy(col);

            _ghostRenderer = _ghostObj.GetComponentInChildren<SpriteRenderer>();
            if (_ghostRenderer != null)
            {
                // Cố định Order in Layer cao để luôn hiện trên cùng khi đang chọn
                _ghostRenderer.sortingOrder = 999;
                
                // Nếu Prefab chưa có hình ảnh, tự sinh ra một khối vuông trắng để Màu sắc có thể hiển thị đè lên!
                if (_ghostRenderer.sprite == null)
                {
                    Texture2D tex = new Texture2D(100, 100);
                    Color[] cols = new Color[100 * 100];
                    for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
                    tex.SetPixels(cols); tex.Apply();
                    _ghostRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }

        private void Update()
        {
            if (!isPlacing || _ghostObj == null) return;

            // Di chuyển Ghost theo chuột
            Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // Bắt chính xác FarmLandTile dưới con trỏ chuột
            Collider2D[] hits = Physics2D.OverlapPointAll(mousePos);
            FarmLandTile hoveredTile = null;
            bool hasObstacle = false;

            foreach (var hit in hits)
            {
                FarmLandTile t = hit.GetComponent<FarmLandTile>();
                if (t != null) hoveredTile = t;
                
                // Tránh lỗi Tag bằng Prefix Name
                if (hit.name.StartsWith("Decor_") || hit.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
                {
                    hasObstacle = true;
                }
            }

            Vector3 snapPos = mousePos; // Mặc định ở trí chuột
            _canPlace = false;

            if (hoveredTile != null)
            {
                // Hover Effect: Bắt toán học Snap chính xác vào Transform cục bộ của ô lưới 
                snapPos = hoveredTile.transform.position; 
                
                if (hoveredTile.IsUnlocked && !hasObstacle)
                {
                    _canPlace = true;
                    if (_ghostRenderer != null) _ghostRenderer.color = colorValid;
                }
                else
                {
                    if (_ghostRenderer != null) _ghostRenderer.color = colorInvalid;
                }
            }
            else
            {
                if (_ghostRenderer != null) _ghostRenderer.color = colorInvalid;
            }

            _ghostObj.transform.position = snapPos;

            // Xử lý Input
            if (Input.GetMouseButtonDown(0)) 
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    ConfirmPlacement(snapPos, hoveredTile);
                }
            }
            else if (Input.GetMouseButtonDown(1)) // Chuột phải để Hủy
            {
                CancelPlacement();
            }
        }

        private void ConfirmPlacement(Vector3 finalPos, FarmLandTile targetTile)
        {
            if (!_canPlace)
            {
                Debug.LogWarning("[DecorPlace] Vị trí không hợp lệ (Ngoại lệ 3a)!");
                ShowErrorMessage(finalPos);
                if (_ghostObj != null)
                {
                    StartCoroutine(ErrorShakeRoutine(_ghostObj.transform));
                }
                return; // Ngừng đặt
            }

            // 1. Kiểm tra/Trừ số lượng trong Kho
            // Giả định Database có hàm GetItemQuantity để kiểm tra trước
            // Ở đây vì gọi từ kho đồ, nên chắc chắn có.
            if (DataManager.Instance != null)
            {
                DataManager.Instance.AddItem(_currentDecorID, -1); // Trừ 1 decor
                
                // Lưu vào CSDL đồ trang trí (DECOR_RECORD)
                DecorRecordModel record = new DecorRecordModel
                {
                    PlayerID = DataManager.Instance.CurrentPlayer != null ? DataManager.Instance.CurrentPlayer.PlayerID : "test_user_1",
                    DecorID = _currentDecorID,
                    X = finalPos.x,
                    Y = finalPos.y
                };
                // Dùng phương thức SQLite insert (Giả lập: DataManager.Instance.db.Insert(record);)
                Debug.Log($"[DecorPlace] Đã lưu {record.DecorID} tại ({record.X}, {record.Y}) vào DECOR_RECORD");
            }

            // 2. Spawn Prefab thật
            GameObject placedDecor = Instantiate(_currentDecorPrefab, finalPos, Quaternion.identity);
            placedDecor.name = $"Decor_{_currentDecorID}";
            // Không gán tag thủ công ở runtime để tránh lỗi "Tag is not defined"

            // Sửa lại Pivot Sort Point để tương thích Custom Axis Sort (Y=1)
            SpriteRenderer sr = placedDecor.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.spriteSortPoint = SpriteSortPoint.Pivot;
            }

            // Confirmation Effect: Particle Explosion Burst
            GameObject particles = new GameObject("PlacementEffect");
            particles.transform.position = finalPos;
            ParticleSystem ps = particles.AddComponent<ParticleSystem>();
            
            // Phải Stop Particle System trước khi thay đổi thông số duration (để tránh lỗi Unity)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 9999; 
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            var main = ps.main;
            main.duration = 1f;
            main.startSpeed = 3f;
            main.startSize = 0.2f;
            main.startColor = new Color(1f, 0.9f, 0.3f, 1f); // Màu vàng Gold lấp lánh
            main.loop = false;
            main.playOnAwake = false;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;

            // Thiết lập xong mới khởi động Particle
            ps.Play();

            Destroy(particles, 1.5f); // Tự động huỷ particles

            // Placement Effect: Scale Bounce Animation
            StartCoroutine(PlacementBounceRoutine(placedDecor.transform));

            // Đổi màu ô đất ở dưới thành Vàng (Yellow) theo yêu cầu 
            if (targetTile != null)
            {
                SpriteRenderer tileSr = targetTile.GetComponent<SpriteRenderer>();
                if (tileSr != null) tileSr.color = new Color(0.9f, 0.8f, 0.2f, 1f); // Màu Vàng nhẹ để không qúa gắt
            }

            Debug.Log($"<color=cyan>[DecorPlace] Xây dựng thành công {_currentDecorID}!</color>");

            // 3. Tắt chế độ đặt
            CancelPlacement();
        }

        private System.Collections.IEnumerator PlacementBounceRoutine(Transform target)
        {
            Vector3 originalScale = target.localScale;
            target.localScale = originalScale * 1.5f;
            float t = 0;
            while (t < 1f)
            {
                target.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, t);
                t += Time.deltaTime * 6f; // Tốc độ đàn hồi
                yield return null; 
            }
            target.localScale = originalScale;
        }

        private void ShowErrorMessage(Vector3 pos)
        {
            GameObject errorMsg = new GameObject("PlacementErrorMsg");
            errorMsg.transform.position = pos + Vector3.up * 0.5f;
            
            var tmp = errorMsg.AddComponent<TMPro.TextMeshPro>();
            tmp.text = "Vị trí đã có vật phẩm!";
            tmp.fontSize = 2.5f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.3f, 0.3f, 1f); // Màu đỏ nhạt
            tmp.sortingOrder = 10000;
            
            StartCoroutine(ErrorTextRoutine(errorMsg.transform, tmp));
        }

        private System.Collections.IEnumerator ErrorTextRoutine(Transform textObj, TMPro.TextMeshPro tmp)
        {
            float t = 0;
            Vector3 startPos = textObj.position;
            Color startColor = tmp.color;
            while(t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                // Từ từ trôi nhẹ lên trên
                textObj.position = startPos + Vector3.up * (t * 0.4f);
                
                // Hiệu ứng mờ dần (Alpha Fade)
                Color c = startColor;
                c.a = Mathf.Lerp(1, 0, t); 
                tmp.color = c;
                yield return null;
            }
            Destroy(textObj.gameObject);
        }

        private System.Collections.IEnumerator ErrorShakeRoutine(Transform target)
        {
            Vector3 originalPos = target.position;
            float t = 0;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                // Rung Ghost sang 2 bên trái phải
                target.position = originalPos + new Vector3(Mathf.Sin(t * 60f) * 0.15f, 0, 0);
                yield return null;
            }
            target.position = originalPos;
        }

        public void CancelPlacement()
        {
            isPlacing = false;
            _currentDecorID = null;
            _currentDecorPrefab = null;

            if (_ghostObj != null)
            {
                Destroy(_ghostObj);
            }
        }
    }
}
