using UnityEngine;
using UnityEngine.Rendering;
using FarmPuzzle.Core.Database;

namespace FarmPuzzle.Decor
{
    /// <summary>
    /// Gắn vào Prefab Root của vật trang trí (Deco_Root).
    /// Nhận DecorationData runtime và setup visual + sorting đúng chuẩn isometric.
    ///
    /// Cấu trúc Prefab bắt buộc:
    ///   Deco_Root  [SortingGroup + DecorationItem]
    ///     └── Deco_Visual  [SpriteRenderer]
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    public class DecorationItem : MonoBehaviour
    {
        // Dữ liệu runtime (không lưu DB trực tiếp — DecorationManager lưu qua DecorRecordModel)
        [HideInInspector] public DecorRecordModel record;

        private SortingGroup _sortingGroup;
        private SpriteRenderer _visualRenderer;

        private void Awake()
        {
            _sortingGroup = GetComponent<SortingGroup>();

            Transform visual = transform.Find("Deco_Visual");
            if (visual != null)
                _visualRenderer = visual.GetComponent<SpriteRenderer>();
            else
                Debug.LogWarning("[DecorationItem] Không tìm thấy child 'Deco_Visual' — " +
                                 "Prefab phải có cấu trúc: Deco_Root > Deco_Visual (SpriteRenderer)");
        }

        /// <summary>
        /// Gọi sau khi Instantiate để thiết lập vị trí, sorting và visual offset.
        /// </summary>
        /// <param name="rec">DB record chứa GridX, GridY, HeightLevel, DecorID</param>
        /// <param name="sprite">Sprite tương ứng với DecorID (load từ Resources)</param>
        /// <param name="tileHeightOffset">Chiều cao 1 tầng stack theo đơn vị Unity (ví dụ 0.5f)</param>
        public void Setup(DecorRecordModel rec, Sprite sprite, float tileHeightOffset = 0.5f)
        {
            record = rec;

            // ── 1. Sorting Order — nhất quán với GridManager (công thức: -(x + y))
            // Trừ thêm HeightLevel * 10 để tầng cao hơn ĐÈ lên tầng thấp hơn
            // (số âm hơn = render sau = đè lên trên trong Unity SortingGroup)
            int baseOrder = -(rec.GridX + rec.GridY);
            _sortingGroup.sortingOrder = baseOrder - (rec.HeightLevel * 10);

            // ── 2. Visual Offset — đẩy hình ảnh lên cao để tạo ảo giác xếp chồng
            // Pivot đã được set ở Bottom Center → điểm gốc = đáy hộp → đẩy Y lên = hộp nổi lên
            if (_visualRenderer != null)
            {
                _visualRenderer.sprite = sprite;
                Transform visual = _visualRenderer.transform;
                // Áp dụng chính xác toạ độ Y = 0.372 cho tầng 1 như yêu cầu + Scale = 2
                visual.localScale = new Vector3(2f, 2f, 2f);
                visual.localPosition = new Vector3(0f, 0.372f + (rec.HeightLevel * tileHeightOffset), 0f);
            }
        }

        /// <summary>Cập nhật lại sorting nếu HeightLevel thay đổi runtime</summary>
        public void RefreshSorting()
        {
            if (record == null) return;
            _sortingGroup.sortingOrder = -(record.GridX + record.GridY) - (record.HeightLevel * 10);
            if (_visualRenderer != null)
            {
                Transform visual = _visualRenderer.transform;
                visual.localScale = new Vector3(2f, 2f, 2f);
                visual.localPosition = new Vector3(0f, 0.372f + (record.HeightLevel * 0.5f), 0f);
            }
        }
    }
}
