using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

namespace FarmPuzzle.CropEditor.Tools
{
    public class CropPrefabBaker : UnityEditor.EditorWindow
    {
        private List<CropDataSO> _cropsToBake = new List<CropDataSO>();
        private string _savePath = "Assets/Resources/";

        private Sprite _waterIcon;
        private Sprite _pestIcon;
        private Vector2 _scrollPos;

        // [MenuItem("Tools/FarmPuzzle/Crop Prefab Baker v2")]
        public static void ShowWindow()
        {
            GetWindow<CropPrefabBaker>("Crop Baker v2");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Crop Prefab Baker (Boss's Precision Layout)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Global Settings", EditorStyles.miniBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Save Path:", _savePath);
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFolderPanel("Choose Save Directory", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                        _savePath = "Assets" + path.Substring(Application.dataPath.Length) + "/";
                    else
                        _savePath = path + "/";
                }
            }
            EditorGUILayout.EndHorizontal();

            _waterIcon = (Sprite)EditorGUILayout.ObjectField("Global Water Icon:", _waterIcon, typeof(Sprite), false);
            _pestIcon  = (Sprite)EditorGUILayout.ObjectField("Global Pest Icon:", _pestIcon, typeof(Sprite), false);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.Label("Crops to Bake", EditorStyles.boldLabel);
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var so in DragAndDropArea()) { if (!_cropsToBake.Contains(so)) _cropsToBake.Add(so); }

            for (int i = 0; i < _cropsToBake.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _cropsToBake[i] = (CropDataSO)EditorGUILayout.ObjectField($"Crop {i+1}:", _cropsToBake[i], typeof(CropDataSO), false);
                if (GUILayout.Button("X", GUILayout.Width(25))) { _cropsToBake.RemoveAt(i); break; }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"BAKE {_cropsToBake.Count} CROPS (BOSS MOLD) 🔥", GUILayout.Height(40)))
            {
                foreach (var cropSO in _cropsToBake) { if (cropSO != null) BakeCrop(cropSO); }
                AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Baker v5", "Baked with your EXACT transform values, Boss!", "OK");
            }
        }

        private List<CropDataSO> DragAndDropArea()
        {
            List<CropDataSO> dragged = new List<CropDataSO>();
            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "DRAG CROP SO HERE");
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (dropArea.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (Object obj in DragAndDrop.objectReferences) if (obj is CropDataSO so) dragged.Add(so);
                    }
                    evt.Use();
                }
            }
            return dragged;
        }

        private void BakeCrop(CropDataSO so)
        {
            string cropName = "Crop_" + (string.IsNullOrEmpty(so.productID) ? so.name : so.productID);

            // 1. Root & Core
            GameObject root = new GameObject(cropName);
            root.transform.localScale = Vector3.one; // Đảm bảo root ko bị to
            
            var display = root.AddComponent<FarmPuzzle.FarmSystem.Visual.CropDisplay>();
            var sr = root.GetComponent<SpriteRenderer>(); // Tự động có nhờ RequireComponent
            sr.sortingOrder = 5; 
            var growth = root.AddComponent<FarmPuzzle.FarmSystem.Crop.CropGrowth>();
            
            if (so.growthStages != null && so.growthStages.Length > 0)
            {
                display.seedSprite = so.growthStages[0];
                display.growingSprite = so.growthStages[so.growthStages.Length > 1 ? 1 : 0];
                display.harvestableSprite = so.growthStages[so.growthStages.Length - 1];
                sr.sprite = so.growthStages[0];
            }
            growth.waterIconSprite = _waterIcon; growth.pestIconSprite = _pestIcon;

            // 2. 🛡️ Canvas - TUYỆT ĐỐI THEO ẢNH 2
            GameObject canvasObj = new GameObject("Crop_UI_Canvas");
            canvasObj.transform.SetParent(root.transform);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            var rtCanvas = canvasObj.GetComponent<RectTransform>();
            rtCanvas.anchoredPosition3D = new Vector3(0, 0.1f, 0); // 🎯 Ảnh 2: Pos Y = 0.1
            rtCanvas.sizeDelta = new Vector2(120, 140);           // 🎯 Ảnh 2: Width 120, Height 140
            rtCanvas.localScale = new Vector3(0.01f, 0.01f, 0.01f); // 🎯 Ảnh 2: Scale 0.01

            // 3. 🛡️ Slider - TUYỆT ĐỐI THEO ẢNH 3
            GameObject sliderObj = new GameObject("GrowthSlider");
            sliderObj.transform.SetParent(canvasObj.transform);
            var slider = sliderObj.AddComponent<Slider>();
            var rtSlider = sliderObj.GetComponent<RectTransform>();
            rtSlider.anchoredPosition3D = new Vector3(-10, 50, 0); // 🎯 Ảnh 3: Pos X -10, Y 50
            rtSlider.sizeDelta = new Vector2(100, 15);            // 🎯 Ảnh 3: Width 100, Height 15
            rtSlider.localScale = Vector3.one;

            // Background (Ảnh 4)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.7f);
            var rtBg = bgImg.rectTransform;
            rtBg.anchorMin = Vector2.zero; rtBg.anchorMax = Vector2.one; 
            rtBg.sizeDelta = Vector2.zero;
            rtBg.localScale = Vector3.one;

            // Fill Area (Ảnh 5 - CỰC QUAN TRỌNG)
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform);
            var rtFillArea = fillAreaObj.AddComponent<RectTransform>();
            rtFillArea.anchorMin = new Vector2(0, 0.25f); // 🎯 Ảnh 5: 0.25
            rtFillArea.anchorMax = new Vector2(1, 0.75f); // 🎯 Ảnh 5: 0.75
            rtFillArea.sizeDelta = Vector2.zero;
            rtFillArea.localScale = Vector3.one;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform);
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 1f, 0.3f, 1f);
            var rtFill = fillImg.rectTransform;
            rtFill.anchorMin = Vector2.zero; rtFill.anchorMax = new Vector2(0, 1);
            rtFill.sizeDelta = Vector2.zero;
            rtFill.localScale = Vector3.one;

            slider.targetGraphic = bgImg;
            slider.fillRect = rtFill;
            growth.progressSlider = slider;

            // 4. 🛡️ Status Icon - TUYỆT ĐỐI THEO ẢNH 6
            GameObject iconObj = new GameObject("Status_Icon");
            iconObj.transform.SetParent(canvasObj.transform);
            var iconImg = iconObj.AddComponent<Image>();
            var rtIcon = iconImg.rectTransform;
            rtIcon.anchoredPosition3D = new Vector3(50, 50, 0); // 🎯 Ảnh 6: Pos X 50, Y 50
            rtIcon.sizeDelta = new Vector2(40, 40);             // 🎯 Ảnh 6: Size 40x40
            rtIcon.localScale = Vector3.one;
            growth.needIcon = iconImg;
            iconObj.SetActive(false);

            // 5. Save & Cleanup
            if (!Directory.Exists(_savePath)) Directory.CreateDirectory(_savePath);
            string finalPath = _savePath + cropName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, finalPath);
            DestroyImmediate(root);
            Debug.Log($"<color=green>[Baker v5 Success]</color> Precision Mold Applied to {cropName}!");
        }
    }
}
