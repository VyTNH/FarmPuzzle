using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FarmPuzzle.Core.Database;
using FarmPuzzle.Core;

namespace FarmPuzzle.Decor
{
    public class DecorationManager : MonoBehaviour
    {
        public static DecorationManager Instance { get; private set; }

        [Header("Config")]
        public GameObject decoPrefab;
        public float stackHeightOffset = 0.5f;
        
        [Header("Sprite Database")]
        public Sprite[] allDecorSprites; // Sẽ được tự động inject bởi Editor Tool

        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<int, DecorationItem> _spawnedItems = new Dictionary<int, DecorationItem>();
        
        // Cache để biết tầng hiện tại (đếm số lượng đồ vật)
        private readonly Dictionary<string, int> _stackMap = new Dictionary<string, int>();
        
        // Cache để biết DecorID nào đang nằm CAO NHẤT trên ô grid đó
        private readonly Dictionary<string, string> _topDecorMap = new Dictionary<string, string>();


        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            if (DataManager.Instance != null)
            {
                DataManager.OnPlayerLoggedIn += RebuildScene;
            }
        }

        private void OnDisable()
        {
            DataManager.OnPlayerLoggedIn -= RebuildScene;
        }

        private void Start()
        {
            // Dự phòng lỡ Start chạy sau khi đã Login
            DataManager.OnPlayerLoggedIn += RebuildScene;

            CacheSprites();
            RebuildScene();
        }

        void CacheSprites()
        {
            _spriteCache.Clear();
            if (allDecorSprites != null)
            {
                foreach (var s in allDecorSprites)
                {
                    if (s != null) _spriteCache[s.name] = s;
                }
            }
            Debug.Log($"<color=cyan>[Deco]</color> Cache {_spriteCache.Count} sprites.");
        }

        public void RebuildScene()
        {
            if (DataManager.Instance?.DB == null) return;
            string pid = DataManager.Instance.CurrentPlayer?.PlayerID;
            if (string.IsNullOrEmpty(pid)) return;

            foreach (var kv in _spawnedItems)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            _spawnedItems.Clear();
            _stackMap.Clear();
            _topDecorMap.Clear();

            var rows = DataManager.Instance.DB.Table<DecorRecordModel>()
                       .Where(r => r.PlayerID == pid)
                       .OrderBy(r => r.HeightLevel) // Order để build đúng cái nào ở trên cùng
                       .ToList();

            foreach (var r in rows)
            {
                DoSpawn(r);
                string k = Key(r.GridX, r.GridY);
                _stackMap[k] = r.HeightLevel + 1;
                _topDecorMap[k] = r.DecorID; // Ghi đè liên tục, do đã OrderBy(HeightLevel) nên cuối cùng map giữ ID ở tầng cao nhất
            }
            Debug.Log($"<color=cyan>[Deco]</color> Spawn {rows.Count} records.");
        }

        public Sprite GetSprite(string decorID)
        {
            if (_spriteCache.TryGetValue(decorID, out Sprite s)) return s;
            return null;
        }

        // Kiểm tra xem vị trí (x,y) có cho phép đặt đè thêm không
        public bool CanStackAt(int x, int y)
        {
            string k = Key(x, y);
            // Nếu ô trống -> được đặt
            if (!_topDecorMap.ContainsKey(k)) return true;
            
            // Nếu có đồ, check item ở tầng cao nhất xem IsFlatTop có = true không
            string topDecorID = _topDecorMap[k];
            var def = DataManager.Instance.DB.Table<DecorItemModel>().FirstOrDefault(d => d.DecorID == topDecorID);
            
            if (def != null && def.IsFlatTop) return true;
            
            return false; // Có mái nhọn nên ko được đặt đè
        }

        public int GetHeightLevelAt(int x, int y)
        {
            string k = Key(x, y);
            return _stackMap.ContainsKey(k) ? _stackMap[k] : 0;
        }

        // ─── Public API ─────────────────────────────────────────────

        public DecorationItem PlaceDecor(string decorID, int gx, int gy, Vector3 worldPos)
        {
            // ── Debug: Kiểm tra từng điều kiện để trace lỗi decor không hiện ──
            Debug.Log($"[Deco:PlaceDecor] Bắt đầu đặt decorID='{decorID}' tại grid({gx},{gy}) worldPos={worldPos}");

            if (DataManager.Instance?.DB == null)
            {
                Debug.LogError("[Deco:PlaceDecor] DataManager.DB == null! Không thể ghi DB.");
                return null;
            }

            if (DataManager.Instance.CurrentPlayer == null)
            {
                Debug.LogError("[Deco:PlaceDecor] CurrentPlayer == null! Người chơi chưa đăng nhập.");
                return null;
            }

            if (!CanStackAt(gx, gy))
            {
                Debug.LogWarning($"[Deco:PlaceDecor] Vị trí ({gx},{gy}) không cho phép xếp chồng (mái nhọn bên trên).");
                return null;
            }

            // Kiểm tra xem decorID có tồn tại trong bảng DecorItemModel chưa
            var def = DataManager.Instance.DB.Table<DecorItemModel>().FirstOrDefault(d => d.DecorID == decorID);
            if (def == null)
            {
                Debug.LogError($"[Deco:PlaceDecor] Không tìm thấy DecorItemModel với DecorID='{decorID}' trong DB! " +
                               $"Kiểm tra: 1) ShopManager đã gọi SeedDecorItemsToDB() chưa, 2) DecorID có đúng không.");
                return null;
            }
            Debug.Log($"[Deco:PlaceDecor] Tìm thấy def: Name={def.Name}, IsFlatTop={def.IsFlatTop}");

            string k = Key(gx, gy);
            int lvl = _stackMap.ContainsKey(k) ? _stackMap[k] : 0;

            var rec = new DecorRecordModel
            {
                PlayerID    = DataManager.Instance.CurrentPlayer.PlayerID,
                DecorID     = decorID,
                X           = worldPos.x,
                Y           = worldPos.y,
                GridX       = gx,
                GridY       = gy,
                HeightLevel = lvl
            };
            DataManager.Instance.DB.Insert(rec);
            Debug.Log($"[Deco:PlaceDecor] Đã Insert DB: PlayerID={rec.PlayerID}, ID={rec.Id}, HeightLevel={lvl}");

            _stackMap[k] = lvl + 1;
            _topDecorMap[k] = decorID;

            var spawned = DoSpawn(rec);
            Debug.Log($"[Deco:PlaceDecor] DoSpawn trả về: {(spawned != null ? spawned.gameObject.name : "NULL — XEM LOG DOSPAWN BÊN DƯỚI")}");
            return spawned;
        }

        public void RemoveDecor(int recordId)
        {
            if (DataManager.Instance?.DB == null) return;
            var player = DataManager.Instance.CurrentPlayer;

            // Truy vấn lấy record để biết tọa độ
            var target = DataManager.Instance.DB.Table<DecorRecordModel>().FirstOrDefault(r => r.Id == recordId);
            if (target == null) return;

            DataManager.Instance.DB.Delete<DecorRecordModel>(recordId);

            if (_spawnedItems.TryGetValue(recordId, out var item) && item != null)
            {
                Destroy(item.gameObject);
                _spawnedItems.Remove(recordId);
            }

            // DO XÓA CÓ THỂ LÀM SAI LỆCH STACK -> Tái cấu trúc lại cache của stack
            string k = Key(target.GridX, target.GridY);
            var remainingInCol = DataManager.Instance.DB.Table<DecorRecordModel>()
                                     .Where(r => r.PlayerID == player.PlayerID && r.GridX == target.GridX && r.GridY == target.GridY)
                                     .OrderBy(r => r.HeightLevel).ToList();
            
            if (remainingInCol.Count == 0)
            {
                _stackMap.Remove(k);
                _topDecorMap.Remove(k);
            }
            else
            {
                // Hạ HeightLevel của các thớt bên trên xuống 
                // Cập nhật lại cache
                _stackMap[k] = remainingInCol.Count;
                for (int i = 0; i < remainingInCol.Count; i++)
                {
                    if (remainingInCol[i].HeightLevel != i)
                    {
                        var updatedRecord = remainingInCol[i];
                        updatedRecord.HeightLevel = i;
                        DataManager.Instance.DB.Update(updatedRecord);
                        
                        // Kéo GameObject rớt xuống 
                        if (_spawnedItems.TryGetValue(updatedRecord.Id, out var cItem))
                        {
                            cItem.record.HeightLevel = i;
                            cItem.RefreshSorting();
                        }
                    }
                }
                _topDecorMap[k] = remainingInCol.Last().DecorID;
            }

            Debug.Log($"<color=orange>[Deco]</color> Xoa record Id={recordId}");
        }

        public List<DecorItemModel> GetAvailableItems()
        {
            if (DataManager.Instance?.DB == null) return new List<DecorItemModel>();
            return DataManager.Instance.DB.Table<DecorItemModel>().ToList();
        }

        // ─── Internals ───────────────────────────────────────────────

        DecorationItem DoSpawn(DecorRecordModel rec)
        {
            Debug.Log($"[Deco:DoSpawn] Bắt đầu spawn: DecorID={rec.DecorID} tại WorldPos=({rec.X},{rec.Y}) GridPos=({rec.GridX},{rec.GridY}) HeightLvl={rec.HeightLevel}");

            GameObject basePrefab = decoPrefab;
            if (basePrefab == null)
            {
                // ⚠️ CẢNH BÁO: decoPrefab chưa được gán trong Inspector!
                // Tạo tạm một GameObject nhưng object này KHÔNG có Sprite và KHÔNG hiển thị gì!
                Debug.LogError("[Deco:DoSpawn] decoPrefab == NULL! Hãy gán 'Deco Prefab' vào Inspector của DecorationManager. " +
                               "Decor sẽ spawn nhưng không có visual!");
                basePrefab = new GameObject("Deco_Root_Template");
                basePrefab.AddComponent<SpriteRenderer>(); // Thêm SR để có thể hiển thị sprite
            }

            var go = Instantiate(basePrefab, new Vector3(rec.X, rec.Y, 0f), Quaternion.identity, transform);
            go.SetActive(true);
            go.name = $"Deco_{rec.DecorID}_{rec.GridX}x{rec.GridY}_h{rec.HeightLevel}";
            Debug.Log($"[Deco:DoSpawn] Instantiate thành công: {go.name} tại WorldPos=({rec.X:F2},{rec.Y:F2})");

            // Vá lỗi: Nếu Prefab user tự chế bị thiếu Deco_Visual -> Code tự đẻ ra luôn!
            Transform visual = go.transform.Find("Deco_Visual");
            if (visual == null)
            {
                Debug.LogWarning($"[Deco:DoSpawn] Prefab '{basePrefab.name}' thiếu child 'Deco_Visual' → tự tạo.");
                var vGo = new GameObject("Deco_Visual");
                vGo.transform.SetParent(go.transform, false);
                vGo.AddComponent<SpriteRenderer>();
                visual = vGo.transform;
            }

            var item = go.GetComponent<DecorationItem>() ?? go.AddComponent<DecorationItem>();
            if (go.GetComponent<UnityEngine.Rendering.SortingGroup>() == null)
                go.AddComponent<UnityEngine.Rendering.SortingGroup>();

            // Tìm sprite theo tên DecorID trong cache
            _spriteCache.TryGetValue(rec.DecorID, out Sprite sprite);
            if (sprite == null)
            {
                Debug.LogWarning($"[Deco:DoSpawn] Không tìm thấy sprite cho DecorID='{rec.DecorID}' trong _spriteCache ({_spriteCache.Count} entries). " +
                                 $"Các key có trong cache: [{string.Join(", ", _spriteCache.Keys)}]. " +
                                 $"Sprite trong Inspector 'allDecorSprites' phải có tên TRÙNG với DecorID!");
                // Fallback: lấy sprite đầu tiên để vẫn hiển thị cái gì đó
                if (_spriteCache.Count > 0) sprite = _spriteCache.Values.First();
            }
            else
            {
                Debug.Log($"[Deco:DoSpawn] Tìm thấy sprite: '{sprite.name}' cho DecorID='{rec.DecorID}'");
            }

            item.Setup(rec, sprite, stackHeightOffset);
            _spawnedItems[rec.Id] = item;
            Debug.Log($"[Deco:DoSpawn] ✅ Hoàn thành spawn item: {go.name} | Sprite={(sprite != null ? sprite.name : "NULL")} | Active={go.activeSelf}");
            return item;
        }

        static string Key(int x, int y) => $"{x}_{y}";
    }
}
