using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FarmPuzzle.LandPuzzle;
using FarmPuzzle.LandPuzzle.Data;
using FarmPuzzle.LandPuzzle.Grid;
using FarmPuzzle.LandPuzzle.Block;
using FarmPuzzle.LandPuzzle.Farm;

namespace FarmPuzzle.LandPuzzle.Editor
{
    /// <summary>
    /// Editor window: Setup Block Puzzle scene với flow mới.
    ///
    /// FLOW: Click FarmLandTile → tiêu 1 NL → mở puzzle panel
    ///       (có obstacle trong grid) → xóa obstacle → win → unlock tile
    /// </summary>
    public class BlockPuzzleEditorSetup : EditorWindow
    {
        // ── Grid ──
        private int   _gridSize    = 10;
        private float _cellSize    = 1f;
        private float _cellSpacing = 0.05f;

        // ── Farm ──
        private int    _farmGridWidth   = 5;
        private int    _farmGridHeight  = 5;
        private float  _farmTileSize    = 1f;
        private float  _farmTileSpacing = 0.05f;
        private string _zoneId          = "zone_1";

        // ── Obstacles in Puzzle Grid ──
        private int _obstacleCountInGrid = 4;
        private int _obstacleMaxHP       = 2;

        // ── Energy ──
        private int _maxEnergy = 5;

        // ── Data assets ──
        private LevelData        _levelData;
        private GridObstacleData _obstacleData1;
        private GridObstacleData _obstacleData2;

        [MenuItem("FarmPuzzle/Block Puzzle/Setup Scene", false, 1)]
        public static void ShowWindow()
        {
            var w = GetWindow<BlockPuzzleEditorSetup>("Block Puzzle Setup");
            w.minSize = new Vector2(400, 640);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GUI
        // ─────────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            GUILayout.Label("🧩 Block Puzzle — Scene Setup", EditorStyles.largeLabel);
            EditorGUILayout.Space(6);

            DrawSection("📐 Puzzle Grid", DrawPuzzleGrid);
            EditorGUILayout.Space(4);
            DrawSection("🪨 Obstacles in Grid", DrawObstacles);
            EditorGUILayout.Space(4);
            DrawSection("🌾 Farm Grid", DrawFarmGrid);
            EditorGUILayout.Space(4);
            DrawSection("⚡ Energy", DrawEnergy);
            EditorGUILayout.Space(4);
            DrawSection("📦 Data Assets", DrawDataAssets);
            EditorGUILayout.Space(8);
            DrawActionButtons();
            EditorGUILayout.Space(8);
            DrawQuickTest();
        }

        private void DrawSection(string title, System.Action content)
        {
            GUILayout.Label(title, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            content();
            EditorGUI.indentLevel--;
        }

        private void DrawPuzzleGrid()
        {
            _gridSize    = EditorGUILayout.IntSlider("Grid Size",    _gridSize,    5, 15);
            _cellSize    = EditorGUILayout.Slider   ("Cell Size",    _cellSize,    0.5f, 2f);
            _cellSpacing = EditorGUILayout.Slider   ("Cell Spacing", _cellSpacing, 0f, 0.3f);
        }

        private void DrawObstacles()
        {
            _obstacleCountInGrid = EditorGUILayout.IntSlider("Count in Grid", _obstacleCountInGrid, 0, 20);
            _obstacleMaxHP       = EditorGUILayout.IntSlider("Default HP",    _obstacleMaxHP,      1, 5);
            _obstacleData1 = (GridObstacleData)EditorGUILayout.ObjectField("Obstacle Type 1", _obstacleData1, typeof(GridObstacleData), false);
            _obstacleData2 = (GridObstacleData)EditorGUILayout.ObjectField("Obstacle Type 2", _obstacleData2, typeof(GridObstacleData), false);
        }

        private void DrawFarmGrid()
        {
            _farmGridWidth   = EditorGUILayout.IntSlider("Farm Width",    _farmGridWidth,  3, 10);
            _farmGridHeight  = EditorGUILayout.IntSlider("Farm Height",   _farmGridHeight, 3, 10);
            _farmTileSize    = EditorGUILayout.Slider   ("Tile Size",     _farmTileSize,   0.5f, 2f);
            _farmTileSpacing = EditorGUILayout.Slider   ("Tile Spacing",  _farmTileSpacing,0f, 0.5f);
            _zoneId          = EditorGUILayout.TextField("Zone ID",       _zoneId);
        }

        private void DrawEnergy()
        {
            _maxEnergy = EditorGUILayout.IntSlider("Max Energy", _maxEnergy, 1, 20);
        }

        private void DrawDataAssets()
        {
            _levelData = (LevelData)EditorGUILayout.ObjectField("Level Data", _levelData, typeof(LevelData), false);
            if (_levelData == null)
                EditorGUILayout.HelpBox("Chưa có LevelData. Nhấn 'Create Default Data'.", MessageType.Warning);
        }

        private void DrawActionButtons()
        {
            GUILayout.Label("🚀 Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
            if (GUILayout.Button("✅ Setup Full Scene", GUILayout.Height(36))) SetupFullScene();
            GUI.backgroundColor = new Color(0.4f, 0.6f, 0.95f);
            if (GUILayout.Button("📦 Create Default Data", GUILayout.Height(36))) CreateDefaultData();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
            if (GUILayout.Button("🗑 Clear Scene", GUILayout.Height(28))) ClearScene();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawQuickTest()
        {
            GUILayout.Label("🧪 Quick Test (Play Mode)", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ Start Puzzle (direct)", GUILayout.Height(28)))
            {
                var mgr = Object.FindFirstObjectByType<LandPuzzleManager>();
                if (mgr != null && _levelData != null) { mgr.StartPuzzle(_levelData); Debug.Log("[Editor] Puzzle started directly!"); }
                else Debug.LogError("[Editor] Missing LandPuzzleManager or LevelData!");
            }
            if (GUILayout.Button("⚡ Fill Energy", GUILayout.Height(28)))
            {
                EnergySystem.Instance?.RefillAll();
                Debug.Log("[Editor] Energy refilled!");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔓 Unlock All Tiles", GUILayout.Height(28)))
            {
                foreach (var t in Object.FindObjectsByType<FarmLandTile>(FindObjectsSortMode.None)) t.ForceUnlock();
                Debug.Log("[Editor] All unlocked!");
            }
            if (GUILayout.Button("🔒 Lock All Tiles", GUILayout.Height(28)))
            {
                foreach (var t in Object.FindObjectsByType<FarmLandTile>(FindObjectsSortMode.None)) t.ResetToLocked();
                Debug.Log("[Editor] All locked!");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // SETUP FULL SCENE
        // ─────────────────────────────────────────────────────────────────────
        private void SetupFullScene()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Block Puzzle Setup");

            var root = CreateOrFind("--- BlockPuzzle ---");
            SetupPuzzleSystem(root);
            SetupFarmGrid(root);
            SetupUI(root);
            WireReferences(root);
            SetupCamera(root);

            EditorUtility.DisplayDialog("Setup Complete",
                $"✅ Scene ready!\nGrid: {_gridSize}×{_gridSize} | Obstacles: {_obstacleCountInGrid}\n" +
                $"Farm: {_farmGridWidth}×{_farmGridHeight} | Energy: {_maxEnergy}\n\n" +
                "▶ Play → Click ô đất nâu để bắt đầu puzzle!", "OK");
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUZZLE SYSTEM
        // ─────────────────────────────────────────────────────────────────────
        private void SetupPuzzleSystem(GameObject root)
        {
            // Puzzle Panel (sẽ ẩn đi, chỉ hiện khi chơi)
            var panelObj = CreateOrFindChild(root, "PuzzlePanel");

            // GridBoard
            var gridObj   = CreateOrFindChild(panelObj, "PuzzleGrid");
            var gridBoard = AddOrGet<GridBoard>(gridObj);
            ApplyFields(gridBoard, ("_gridSize", _gridSize), ("_cellSize", _cellSize), ("_cellSpacing", _cellSpacing));

            // BlockSpawner + slots
            var spawnerObj  = CreateOrFindChild(panelObj, "BlockSpawner");
            var spawner     = AddOrGet<BlockSpawner>(spawnerObj);
            float gridHalfH = _gridSize * (_cellSize + _cellSpacing) / 2f;
            float slotY     = -(gridHalfH + 2.5f);
            float slotStep  = Mathf.Max(_gridSize * (_cellSize + _cellSpacing) / 4f, 3f);
            var slots       = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = CreateOrFindChild(spawnerObj, $"SpawnSlot_{i}");
                slot.transform.localPosition = new Vector3((i - 1) * slotStep, slotY, 0f);
                slots[i] = slot.transform;
            }
            var sSO = new SerializedObject(spawner);
            var sp = sSO.FindProperty("_spawnSlots");
            sp.arraySize = 3;
            for (int i = 0; i < 3; i++) sp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            sSO.ApplyModifiedProperties();

            // Scoring
            var scoringObj = CreateOrFindChild(panelObj, "Scoring");
            AddOrGet<BlockPuzzleScoring>(scoringObj);

            // Manager on root
            AddOrGet<LandPuzzleManager>(root);

            // Energy system
            var energyObj = CreateOrFindChild(root, "EnergySystem");
            var energy    = AddOrGet<EnergySystem>(energyObj);
            ApplyFields(energy, ("_maxEnergy", _maxEnergy), ("_currentEnergy", _maxEnergy));
        }

        // ─────────────────────────────────────────────────────────────────────
        // FARM GRID
        // ─────────────────────────────────────────────────────────────────────
        private void SetupFarmGrid(GameObject root)
        {
            float farmStep    = _farmTileSize + _farmTileSpacing;
            float farmTotalW  = _farmGridWidth  * farmStep - _farmTileSpacing;
            float farmTotalH  = _farmGridHeight * farmStep - _farmTileSpacing;
            float puzzleTotalW= _gridSize      * (_cellSize + _cellSpacing) - _cellSpacing;
            float farmOffsetX = puzzleTotalW / 2f + farmTotalW / 2f + 3f;

            var farmRoot = CreateOrFindChild(root, "FarmGrid");
            farmRoot.transform.localPosition = new Vector3(farmOffsetX, 0f, 0f);

            // Xóa tiles cũ
            for (int i = farmRoot.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(farmRoot.transform.GetChild(i).gameObject);

            // Tạo tiles — tất cả bắt đầu là LOCKED (nâu)
            for (int row = 0; row < _farmGridHeight; row++)
            {
                for (int col = 0; col < _farmGridWidth; col++)
                {
                    var tileObj = new GameObject($"FarmTile_{row}_{col}");
                    tileObj.transform.SetParent(farmRoot.transform, false);
                    tileObj.transform.localPosition = new Vector3(col * farmStep, row * farmStep, 0f);
                    tileObj.transform.localScale    = Vector3.one * _farmTileSize;
                    Undo.RegisterCreatedObjectUndo(tileObj, "Create FarmTile");

                    var sr    = tileObj.AddComponent<SpriteRenderer>();
                    sr.sprite = MakeSprite();
                    sr.color  = new Color(0.60f, 0.45f, 0.30f); // nâu = locked

                    // FarmLandTile
                    var tile  = tileObj.AddComponent<FarmLandTile>();
                    var tileSO = new SerializedObject(tile);
                    tileSO.FindProperty("_farmGridPosition").vector2IntValue = new Vector2Int(col, row);
                    tileSO.FindProperty("_zoneId").stringValue               = _zoneId;
                    tileSO.FindProperty("_isUnlocked").boolValue             = false;
                    // Wire levelData nếu có
                    if (_levelData != null)
                        tileSO.FindProperty("_levelData").objectReferenceValue = _levelData;
                    tileSO.ApplyModifiedProperties();
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // UI  
        // ─────────────────────────────────────────────────────────────────────
        private void SetupUI(GameObject root)
        {
            var canvasObj = CreateOrFindChild(root, "Canvas_BlockPuzzle");
            var canvas    = AddOrGet<Canvas>(canvasObj);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            AddOrGet<CanvasScaler>(canvasObj);
            AddOrGet<GraphicRaycaster>(canvasObj);

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // WIRE REFERENCES
        // ─────────────────────────────────────────────────────────────────────
        private void WireReferences(GameObject root)
        {
            var manager  = root.GetComponent<LandPuzzleManager>();
            var panelObj = root.transform.Find("PuzzlePanel");
            var gridBoard= panelObj?.Find("PuzzleGrid")?.GetComponent<GridBoard>();
            var spawner  = panelObj?.Find("BlockSpawner")?.GetComponent<BlockSpawner>();
            var scoring  = panelObj?.Find("Scoring")?.GetComponent<BlockPuzzleScoring>();

            if (manager != null)
            {
                ApplyFields(manager,
                    ("_gridBoard",    (object)gridBoard),
                    ("_blockSpawner", (object)spawner),
                    ("_scoring",      (object)scoring),
                    ("_puzzlePanel",  (object)panelObj?.gameObject));
            }
            if (spawner != null && gridBoard != null)
                ApplyFields(spawner, ("_gridBoard", (object)gridBoard));
        }

        // ─────────────────────────────────────────────────────────────────────
        // CAMERA
        // ─────────────────────────────────────────────────────────────────────
        private void SetupCamera(GameObject root)
        {
            var cam = Camera.main;
            if (cam == null) { Debug.LogWarning("[Editor] No Main Camera!"); return; }

            float farmStep    = _farmTileSize + _farmTileSpacing;
            float farmTotalW  = _farmGridWidth  * farmStep - _farmTileSpacing;
            float farmTotalH  = _farmGridHeight * farmStep - _farmTileSpacing;
            float puzzleTotalW= _gridSize * (_cellSize + _cellSpacing);
            float puzzleTotalH= _gridSize * (_cellSize + _cellSpacing);
            float farmOffsetX = puzzleTotalW / 2f + farmTotalW / 2f + 3f;

            cam.orthographic     = true;
            cam.orthographicSize = Mathf.Max(puzzleTotalH, farmTotalH) / 2f + 2f;
            cam.transform.position = new Vector3(farmOffsetX / 2f, 0f, -10f);

            if (cam.GetComponent<Physics2DRaycaster>() == null)
            {
                cam.gameObject.AddComponent<Physics2DRaycaster>();
                Debug.Log("[Editor] ✅ Added Physics2DRaycaster to Main Camera");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE DEFAULT DATA
        // ─────────────────────────────────────────────────────────────────────
        private void CreateDefaultData()
        {
            const string basePath = "Assets/ScriptableObjects/Puzzles";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(basePath))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Puzzles");

            // Shapes
            var shapes = new ShapeData[]
            {
                MakeShape(basePath,"Single",      1,1,new bool[]{true},                                            new Color(0.2f,0.7f,0.9f)),
                MakeShape(basePath,"Domino_H",    1,2,new bool[]{true,true},                                       new Color(0.9f,0.5f,0.2f)),
                MakeShape(basePath,"Domino_V",    2,1,new bool[]{true,true},                                       new Color(0.9f,0.6f,0.3f)),
                MakeShape(basePath,"Tromino_I_H", 1,3,new bool[]{true,true,true},                                  new Color(0.3f,0.8f,0.4f)),
                MakeShape(basePath,"Tromino_I_V", 3,1,new bool[]{true,true,true},                                  new Color(0.4f,0.9f,0.5f)),
                MakeShape(basePath,"Tromino_L",   2,2,new bool[]{true,false,true,true},                            new Color(0.8f,0.3f,0.6f)),
                MakeShape(basePath,"Square_2x2",  2,2,new bool[]{true,true,true,true},                             new Color(0.9f,0.8f,0.2f)),
                MakeShape(basePath,"Tetromino_T", 2,3,new bool[]{true,true,true,false,true,false},                 new Color(0.6f,0.3f,0.9f)),
                MakeShape(basePath,"Tetromino_S", 2,3,new bool[]{false,true,true,true,true,false},                 new Color(0.3f,0.6f,0.9f)),
                MakeShape(basePath,"Plus",        3,3,new bool[]{false,true,false,true,true,true,false,true,false},new Color(0.9f,0.3f,0.3f)),
            };

            // Obstacles — HP theo _obstacleMaxHP setting từ UI
            var obs1 = MakeObstacle(basePath, "Rock",  "Đá",    _obstacleMaxHP, ResourceType.Stone, 8, new Color(0.5f,0.5f,0.5f));
            var obs2 = MakeObstacle(basePath, "Log",   "Gỗ",    _obstacleMaxHP, ResourceType.Wood,  5, new Color(0.6f,0.4f,0.2f));
            var obs3 = MakeObstacle(basePath, "Bush",  "Bụi",   _obstacleMaxHP, ResourceType.Food,  3, new Color(0.3f,0.6f,0.2f));

            // Create obstacle placements for level
            var placements = new System.Collections.Generic.List<ObstaclePlacement>();
            var rand = new System.Random(42);
            var usedPositions = new System.Collections.Generic.HashSet<int>();
            var obstaclePool = new GridObstacleData[] { obs1, obs2, obs3 };

            for (int i = 0; i < _obstacleCountInGrid; i++)
            {
                int row, col;
                int attempts = 0;
                do { row = rand.Next(1, _gridSize - 1); col = rand.Next(1, _gridSize - 1); attempts++; }
                while (usedPositions.Contains(row * 100 + col) && attempts < 50);
                usedPositions.Add(row * 100 + col);

                placements.Add(new ObstaclePlacement {
                    gridRow      = row,
                    gridCol      = col,
                    obstacleData = obstaclePool[i % obstaclePool.Length]
                });
            }

            // Level
            var level = CreateOrLoad<LevelData>($"{basePath}/SO_Level_1.asset");
            level.levelId       = 1;
            level.gridSize      = _gridSize;
            level.availableShapes = shapes;
            level.blocksPerBatch = 3;
            level.farmZoneId    = _zoneId;
            level.puzzleObstacles = placements.ToArray();
            EditorUtility.SetDirty(level);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _levelData     = level;
            _obstacleData1 = obs1;
            _obstacleData2 = obs2;

            EditorUtility.DisplayDialog("Data Created",
                $"✅ {shapes.Length} shapes | 3 obstacles | 1 level\n{_obstacleCountInGrid} obstacles placed in grid\nTại: {basePath}", "OK");
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private GameObject CreateOrFind(string name)
        {
            var go = GameObject.Find(name) ?? new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            return go;
        }

        private GameObject CreateOrFindChild(GameObject parent, string childName)
        {
            var existing = parent.transform.Find(childName);
            if (existing != null) return existing.gameObject;
            var child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
            return child;
        }

        private T AddOrGet<T>(GameObject obj) where T : Component
            => obj.GetComponent<T>() ?? Undo.AddComponent<T>(obj);

        private void ApplyFields(Object target, params (string field, object val)[] pairs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, val) in pairs)
            {
                var p = so.FindProperty(field);
                if (p == null) { Debug.LogWarning($"[Editor] Field '{field}' not found on {target}"); continue; }
                if      (val is int    i) p.intValue             = i;
                else if (val is float  f) p.floatValue           = f;
                else if (val is string s) p.stringValue          = s;
                else if (val is bool   b) p.boolValue            = b;
                else if (val is Object o) p.objectReferenceValue = o;
                else if (val == null)     p.objectReferenceValue = null;
            }
            so.ApplyModifiedProperties();
        }

        private T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            
            var instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, path);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private ShapeData MakeShape(string path, string name, int rows, int cols, bool[] cells, Color color)
        {
            var s = CreateOrLoad<ShapeData>($"{path}/SO_Shape_{name}.asset");
            s.rows = rows; s.columns = cols; s.cells = cells; s.blockColor = color;
            EditorUtility.SetDirty(s);
            return s;
        }

        private GridObstacleData MakeObstacle(string path, string id, string displayName, int hp,
            ResourceType res, int amount, Color color)
        {
            var d = CreateOrLoad<GridObstacleData>($"{path}/SO_GridObstacle_{id}.asset");
            d.obstacleName   = displayName;
            d.maxDurability  = hp;
            d.resourceType   = res;
            d.resourceAmount = amount;
            d.obstacleColor  = color;
            EditorUtility.SetDirty(d);
            return d;
        }

        private static Sprite MakeSprite()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px  = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
        }

        private void ClearScene()
        {
            var root = GameObject.Find("--- BlockPuzzle ---");
            if (root != null) { Undo.DestroyObjectImmediate(root); Debug.Log("[Editor] Cleared!"); }
            else Debug.Log("[Editor] Nothing to clear.");
        }
    }
}
