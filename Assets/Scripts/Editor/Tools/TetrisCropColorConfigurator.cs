using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FarmPuzzle.FarmSystem.Crop;
using FarmPuzzle.Tetris;
using System.Linq;

public class TetrisCropColorConfigurator : EditorWindow
{
    private TetrisCropMapSO _mapSO;
    private List<CropDataSO> _allCrops = new List<CropDataSO>();
    private Vector2 _scrollPos;

    [MenuItem("FarmPuzzle/Công cụ / Bản đồ màu Tetris Nông Sản")]
    public static void ShowWindow()
    {
        GetWindow<TetrisCropColorConfigurator>("Tetris Crop Map");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void LoadData()
    {
        // Find existing SO or create one in memory (user can save it)
        string[] guids = AssetDatabase.FindAssets("t:TetrisCropMapSO");
        if (guids.Length > 0)
        {
            _mapSO = AssetDatabase.LoadAssetAtPath<TetrisCropMapSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        // Find all CropDataSO
        _allCrops.Clear();
        string[] cropGuids = AssetDatabase.FindAssets("t:CropDataSO");
        foreach (string guid in cropGuids)
        {
            _allCrops.Add(AssetDatabase.LoadAssetAtPath<CropDataSO>(AssetDatabase.GUIDToAssetPath(guid)));
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("🛠 CẤU HÌNH HÌNH ẢNH BLOCK TETRIS THEO NÔNG SẢN", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (_mapSO == null)
        {
            EditorGUILayout.HelpBox("Không tìm thấy file TetrisCropMapSO nào! Hãy bấm nút bên dưới để tạo 1 file.", MessageType.Error);
            if (GUILayout.Button("Tạo File TetrisCropMapSO gốc", GUILayout.Height(30)))
            {
                _mapSO = ScriptableObject.CreateInstance<TetrisCropMapSO>();
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateAsset(_mapSO, "Assets/Resources/TetrisCropMap.asset");
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[Tetris Config]</color> Đã tạo file tại Assets/Resources/TetrisCropMap.asset");
            }
            return;
        }

        EditorGUILayout.LabelField("Đang sửa file:", AssetDatabase.GetAssetPath(_mapSO));
        GUILayout.Space(10);

        if (GUILayout.Button("🔄 Làm mới danh sách nông sản", GUILayout.Height(30)))
        {
            LoadData();
            
            // Auto add missing crops
            foreach(var crop in _allCrops)
            {
                if (_mapSO.GetMapping(crop.productID) == null)
                {
                    _mapSO.mappings.Add(new TetrisCropMapSO.CropMapping() {
                        productID = crop.productID,
                        fallBackColor = Color.white
                    });
                }
            }
            EditorUtility.SetDirty(_mapSO);
        }

        GUILayout.Space(10);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        
        foreach (var crop in _allCrops)
        {
            var mapping = _mapSO.GetMapping(crop.productID);
            if (mapping == null) continue;

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();

            // Hiển thị tên nông sản
            GUILayout.Label(crop.name + " (" + crop.productID + ")", GUILayout.Width(150), GUILayout.Height(50));

            // Hiển thị chọn Sprite cho Tetris
            GUILayout.Label("Sprite (Thay Block):", GUILayout.Width(120), GUILayout.Height(50));
            mapping.tetrisSprite = (Sprite)EditorGUILayout.ObjectField(mapping.tetrisSprite, typeof(Sprite), false, GUILayout.Width(64), GUILayout.Height(64));

            GUILayout.FlexibleSpace();

            // Hiển thị màu dự phòng (fallback)
            GUILayout.BeginVertical();
            GUILayout.Label("Màu hiển thị phụ:");
            mapping.fallBackColor = EditorGUILayout.ColorField(mapping.fallBackColor, GUILayout.Width(80));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(_mapSO);
        }
    }
}
