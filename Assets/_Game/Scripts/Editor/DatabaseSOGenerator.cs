using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using SQLite;
using FarmPuzzle.Core.Database;
using System.Linq;

namespace FarmPuzzle.EditorTools
{
    public class DatabaseSOGenerator : EditorWindow
    {
        private SQLiteConnection _db;
        private List<SeedItemModel> _seeds = new List<SeedItemModel>();
        private List<CropDataModel> _crops = new List<CropDataModel>();
        
        private int _selectedIndex = -1;
        private Vector2 _scrollPos;

        // Dữ liệu thủ công để móc nối từ giao diện
        private Sprite _manualSeedIcon;
        private Sprite _manualGrowthStage1;
        private Sprite _manualGrowthStage2;
        private Sprite _manualGrowthStage3;
        private Sprite _manualProductIcon; // Thêm biến lưu Icon Sản Phẩm
        private int _manualYieldAmount = 1;

        // [MenuItem("FarmPuzzle/1. Database SO Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<DatabaseSOGenerator>("SO Generator");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable()
        {
            ConnectToDatabase();
        }

        private void OnDisable()
        {
            if (_db != null) _db.Close();
        }

        private void ConnectToDatabase()
        {
            string dbPath = Path.Combine(Application.persistentDataPath, "FarmPuzzleDB.db");
            if (!File.Exists(dbPath))
            {
                Debug.LogWarning($"[DB Generator] Cảnh báo: File DB chưa tồn tại ở: {dbPath}. Sếp cần Play game 1 lần để DataManager đẻ ra DB gốc nhé!");
                return;
            }

            _db = new SQLiteConnection(dbPath);
            
            // Lấy danh sách Seed và Crop ra
            try
            {
                _seeds = _db.Table<SeedItemModel>().ToList();
                _crops = _db.Table<CropDataModel>().ToList();
            }
            catch(System.Exception e)
            {
                Debug.LogWarning("[DB Generator] Chưa có bảng trong DB! " + e.Message);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();

            // CỘT 1: DANH SÁCH DATABASE
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUILayout.Label("📚 DỮ LIỆU TỪ SQLITE", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Trích xuất lại Database", GUILayout.Height(30)))
            {
                ConnectToDatabase();
            }
            
            GUILayout.Space(5);
            
            if (_seeds == null || _seeds.Count == 0)
            {
                GUILayout.Label("Trống! Hãy Play Game 1 lần để DB kết dính.", EditorStyles.helpBox);
            }
            else
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);
                for (int i = 0; i < _seeds.Count; i++)
                {
                    GUI.backgroundColor = _selectedIndex == i ? Color.cyan : Color.white;
                    if (GUILayout.Button($"{_seeds[i].SeedID}\n({_seeds[i].Name})", GUILayout.Height(40)))
                    {
                        _selectedIndex = i;
                        ClearManualData(); // Chuyển sang Item khác thì reset ô kéo hình ảnh
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            // CỘT 2: CHI TIẾT VÀ NỐI ẢNH (GIAO DIỆN CHỌN HÌNH - YÊU CẦU CỦA ĐẠI CA)
            GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            if (_selectedIndex >= 0 && _selectedIndex < _seeds.Count)
            {
                SeedItemModel selectedSeed = _seeds[_selectedIndex];
                CropDataModel relatedCrop = _crops.FirstOrDefault(c => c.SeedID == selectedSeed.SeedID);

                GUILayout.Label("🛠️ KẾT NỐI HÌNH ẢNH (VISUAL BINDING)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Dữ liệu dưới đây lấy từ SQLite, sếp không thể sửa thông số mà chỉ có thể gắn file Ảnh (Sprite Picker) vào các ô tương ứng.", MessageType.Info);

                GUILayout.Space(10);
                GUILayout.Label($"[ID Gốc]: {selectedSeed.SeedID}", EditorStyles.boldLabel);
                GUILayout.Label($"[Tên Hạt]: {selectedSeed.Name}");
                GUILayout.Label($"[Giá mua]: {selectedSeed.BuyPrice} Vàng");
                
                if (relatedCrop != null)
                {
                    GUILayout.Label($"[Cây trồng tạo ra (Product ID)]: {relatedCrop.ProductID}");
                    GUILayout.Label($"[Thời gian sinh trưởng]: {relatedCrop.GrowSeconds}s");
                }
                else
                {
                    EditorGUILayout.HelpBox($"!! Chưa thấy khai báo CropDataModel cho {selectedSeed.SeedID} trong SQLite!", MessageType.Error);
                }

                GUILayout.Space(15);
                
                // MÀN HÌNH CHỌN ẢNH (SPRITE PICKER - Cửa sổ lọc hình ảnh như yêu cầu)
                GUILayout.Label("🖼️ KÉO THẢ / CHỌN ẢNH (BẤM VÀO VÒNG TRÒN NHỎ BÊN PHẢI Ô CHỐNG)", EditorStyles.boldLabel);
                
                _manualSeedIcon = (Sprite)EditorGUILayout.ObjectField("Ảnh Hạt Giống (Kho):", _manualSeedIcon, typeof(Sprite), false);
                
                _manualProductIcon = (Sprite)EditorGUILayout.ObjectField("Ảnh Nông Sản (Thu Hoạch):", _manualProductIcon, typeof(Sprite), false);
                
                GUILayout.Space(5);
                GUILayout.Label("Ảnh 3 giai đoạn sinh trưởng khi rắc xuống đất:");
                _manualGrowthStage1 = (Sprite)EditorGUILayout.ObjectField("Giai đoạn 1 (Mầm):", _manualGrowthStage1, typeof(Sprite), false);
                _manualGrowthStage2 = (Sprite)EditorGUILayout.ObjectField("Giai đoạn 2 (Cây vỡ):", _manualGrowthStage2, typeof(Sprite), false);
                _manualGrowthStage3 = (Sprite)EditorGUILayout.ObjectField("Giai đoạn 3 (Chín):", _manualGrowthStage3, typeof(Sprite), false);

                GUILayout.Space(5);
                _manualYieldAmount = EditorGUILayout.IntSlider("Sản lượng Nhổ ra:", _manualYieldAmount, 1, 10);

                GUILayout.Space(25);
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("🔥 GENERATE SCRIPTABLE OBJECT 🔥", GUILayout.Height(50)))
                {
                    GenerateFileSO(selectedSeed, relatedCrop);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUILayout.Label("\n\n\n<-- Hãy bấm chọn 1 Hạt giống bên bảng SQLite để bắt đầu Đúc File SO", EditorStyles.centeredGreyMiniLabel);
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void ClearManualData()
        {
            _manualSeedIcon = null;
            _manualGrowthStage1 = null;
            _manualGrowthStage2 = null;
            _manualGrowthStage3 = null;
            _manualProductIcon = null;
            _manualYieldAmount = 1;
        }

        private void GenerateFileSO(SeedItemModel seedDB, CropDataModel cropDB)
        {
            // Kiểm tra và tạo folder Generated
            string savePath = "Assets/ScriptableObjects/Generated";
            if (!AssetDatabase.IsValidFolder(savePath))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Generated");
            }

            // 1. CHUẨN BỊ FILE CROP (Giai đoạn lớn)
            CropDataSO newCropSO = null;
            if (cropDB != null)
            {
                string cropFilePath = $"{savePath}/SO_Crop_{cropDB.ProductID}.asset";
                newCropSO = AssetDatabase.LoadAssetAtPath<CropDataSO>(cropFilePath);

                if (newCropSO == null)
                {
                    newCropSO = ScriptableObject.CreateInstance<CropDataSO>();
                    AssetDatabase.CreateAsset(newCropSO, cropFilePath);
                }

                // Gắng Data từ DB
                newCropSO.productID = cropDB.ProductID;
                newCropSO.totalTimeToHarvest = cropDB.GrowSeconds;
                
                // Truy vấn tên nông sản từ ProductItemModel
                var productDB = _db.Table<ProductItemModel>().FirstOrDefault(p => p.ProductID == cropDB.ProductID);
                newCropSO.cropName = productDB != null ? productDB.Name : "Nông Sản Lỗi";

                // Gắn Data từ Giao Diện Tool
                newCropSO.yieldAmount = _manualYieldAmount;
                newCropSO.productIcon = _manualProductIcon;
                newCropSO.growthStages = new Sprite[] { _manualGrowthStage1, _manualGrowthStage2, _manualGrowthStage3 };

                EditorUtility.SetDirty(newCropSO);
            }

            // 2. CHUẨN BỊ FILE SEED (Hạt giống cho Kho đồ)
            string seedFilePath = $"{savePath}/SO_Seed_{seedDB.SeedID}.asset";
            SeedItemSO newSeedSO = AssetDatabase.LoadAssetAtPath<SeedItemSO>(seedFilePath);

            if (newSeedSO == null)
            {
                newSeedSO = ScriptableObject.CreateInstance<SeedItemSO>();
                AssetDatabase.CreateAsset(newSeedSO, seedFilePath);
            }

            // Gắn Data từ DB
            newSeedSO.seedID = seedDB.SeedID;
            newSeedSO.seedName = seedDB.Name;
            newSeedSO.buyPrice = seedDB.BuyPrice;

            // Gắn Data từ Giao diện thủ công
            newSeedSO.inventoryIcon = _manualSeedIcon;
            if (newCropSO != null) newSeedSO.cropData = newCropSO;

            EditorUtility.SetDirty(newSeedSO);
            
            // XONG! LƯU LẠI Ổ CỨNG THEO ĐÚNG CHUẨN UNITY BẢO MẬT
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // CHỨC NĂNG MỚI: Tự động Bôi đen/Nhấp nháy file SO vừa đúc trong cửa sổ Project
            EditorGUIUtility.PingObject(newSeedSO);
            Selection.activeObject = newSeedSO;

            Debug.Log($"[ĐÚC SO THÀNH CÔNG] Đã sinh xong {seedFilePath} ! Sếp có thể dùng để kéo vào màn hình Nông Trại.");
        }
    }
}
