using UnityEngine;
using UnityEditor;
using SQLite;
using System.IO;
using FarmPuzzle.Core.Database;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class DatabaseTesterWindow : EditorWindow
{
    private SQLiteConnection db;
    private string dbPath;
    
    private string[] tableNames = { 
        "👉 [1] BẢNG KHÁCH HÀNG (PLAYER)", 
        "👉 [2] BẢNG TÚI ĐỒ (INVENTORY)", 
        "👉 [3] BẢNG TRANG TRÍ (DECOR_RECORD)", 
        "👉 [4] BẢNG NHIỆM VỤ (PLAYER_QUEST)", 
        "👉 [5] BẢNG MIẾNG ĐẤT (FARM_TILE)", 
        "👉 [6] BẢNG ĐƠN HÀNG (ACTIVE_ORDER)",
        "👉 [7] CỬA HÀNG HẠT (SEED_ITEM)",
        "👉 [8] CƠ CHẾ SINH TRƯỞNG (CROP_DATA)",
        "👉 [9] CỬA HÀNG TRANG TRÍ (DECOR_ITEM)",
        "👉 [10] BẢNG THU HOẠCH (PRODUCT_ITEM)",
        "👉 [11] BẢNG CHƯỚNG NGẠI VẬT (OBSTACLE)" 
    };
    private int selectedTableIndex = 0;
    
    private Vector2 gridScrollPos;
    private Vector2 jsonScrollPos;

    private string currentJsonOutput = "";
    private string sqlInput = "INSERT INTO PRODUCT_ITEM (ProductID, Name, Type, SellPrice) VALUES ('prod_lemon', 'Chanh', 'Fruit', 50);";
    
    // Lưu trữ dữ liệu thô để vẽ UI Bảng Excel
    private object[] currentTableData;
    private PropertyInfo[] currentProperties;
    private string statusMessage = "Bấm 'Đọc Dữ Liệu Bảng Này' để xem...";

    private void ExecuteCustomQuery(string query)
    {
        if (string.IsNullOrEmpty(query)) return;
        if (!File.Exists(dbPath)) return;

        using (var db = new SQLiteConnection(dbPath))
        {
            try {
                db.Execute(query);
                statusMessage = "✅ THÀNH CÔNG: Đã thực thi lệnh SQL!";
                FetchTableData(selectedTableIndex); 
            } catch (System.Exception ex) {
                statusMessage = "❌ LỖI SQL: " + ex.Message;
                Debug.LogError(ex.Message);
            }
        }
    }

    // [MenuItem("FarmPuzzle/Tra Cứu Database (Chuẩn ERD) & JSON")]
    public static void ShowWindow()
    {
        GetWindow<DatabaseTesterWindow>("Tra Cứu ERD", true, typeof(EditorWindow));
    }

    private void OnEnable()
    {
        dbPath = Path.Combine(Application.persistentDataPath, "FarmPuzzleDB.db");
    }

    private void OnGUI()
    {
        GUILayout.Label("CÔNG CỤ PHÂN TÍCH QUẢN TRỊ SQLITE", EditorStyles.boldLabel);
        GUILayout.Label("DB Path: " + dbPath, EditorStyles.wordWrappedMiniLabel);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("♻️ RESET (Xóa DB Cũ để Chơi Lại)", GUILayout.Height(25), GUILayout.Width(250))) {
            if (File.Exists(dbPath)) { 
                File.Delete(dbPath); 
                currentJsonOutput = ""; 
                currentTableData = null; 
                statusMessage = "Đã xóa DB. Vui lòng bấm Play game để hệ thống sinh file mới."; 
            }
        }
        GUILayout.Label("  (Dữ liệu test tự động nhồi 5 dòng/bảng khi Play)", EditorStyles.miniLabel);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        
        // --- CHỌN BẢNG ---
        GUILayout.BeginHorizontal();
        GUILayout.Label("CHỌN BẢNG ERD:", EditorStyles.boldLabel, GUILayout.Width(130));
        selectedTableIndex = EditorGUILayout.Popup(selectedTableIndex, tableNames, GUILayout.Width(300));
        
        if (GUILayout.Button("🔍 MỞ BẢNG DỮ LIỆU", GUILayout.Height(25))) {
            FetchTableData(selectedTableIndex);
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Trạng thái: " + statusMessage, EditorStyles.helpBox);
        
        // ---- QUICK ADD ROW ----
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("➕ DÙNG DB BROWSER MỞ FILE CSDL", GUILayout.Height(25), GUILayout.Width(250))) {
            EditorUtility.RevealInFinder(dbPath);
        }
        GUILayout.Label(" (Dùng phần mềm SQLite Studio / DB Browser chép đè hoặc Thêm Bảng Dữ Liệu mới cực lẹ!)", EditorStyles.miniLabel);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // ================= GIAO DIỆN HIỂN THỊ CHÍNH =================
        GUILayout.BeginHorizontal();
        
        // --- Cột Trái (75% Dài): Vẽ Bảng Lưới Excel ---
        GUILayout.BeginVertical(GUILayout.Width(position.width * 0.7f - 5));
        GUILayout.Label($"📊 BẢNG DỮ LIỆU {(currentTableData != null ? $"({currentTableData.Length} dòng)" : "")}", EditorStyles.boldLabel);
        
        // Bắt đầu vùng cuộn ngang dọc cho bảng Excel
        gridScrollPos = EditorGUILayout.BeginScrollView(gridScrollPos, "box", GUILayout.ExpandHeight(true));
        if (currentTableData != null && currentProperties != null)
        {
            // HEADER CỦA BẢNG (MÀU NỀN TỐI)
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            for (int i = 0; i < currentProperties.Length; i++) {
                GUILayout.Label(currentProperties[i].Name, EditorStyles.toolbarButton, GUILayout.MinHeight(25), GUILayout.Width(130));
            }
            GUILayout.EndHorizontal();

            // CÁC HÀNG DỮ LIỆU (ROW)
            for (int row = 0; row < currentTableData.Length; row++)
            {
                // Làm nổi bật màu xen kẽ cho giống Excel
                GUI.backgroundColor = (row % 2 == 0) ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.8f, 0.8f, 0.8f);
                GUILayout.BeginHorizontal("box");
                GUI.backgroundColor = Color.white; // Phục hồi màu mặc định

                for (int col = 0; col < currentProperties.Length; col++)
                {
                    object val = currentProperties[col].GetValue(currentTableData[row]);
                    string displayVal = val == null ? "NULL" : val.ToString();
                    
                    // TextField thay vì Label để chủ nhân có thể quét bôi đen/copy chữ trong ô
                    EditorGUILayout.SelectableLabel(displayVal, EditorStyles.textField, GUILayout.Height(20), GUILayout.Width(130));
                }
                GUILayout.EndHorizontal();
            }
        }
        else
        {
            GUILayout.Label("Chưa có Dữ Liệu. Hãy ấn nút [🔍 MỞ BẢNG DỮ LIỆU]...", EditorStyles.centeredGreyMiniLabel);
        }
        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();

        // --- Cột Phải (25% Ngắn): View JSON & SQL Execute ---
        GUILayout.BeginVertical(GUILayout.Width(position.width * 0.3f - 10));
        
        GUILayout.Label("⌨️ THỰC THI SQL QUERY (JSON/SQL)", EditorStyles.boldLabel);
        sqlInput = EditorGUILayout.TextArea(sqlInput, GUILayout.Height(100));
        if (GUILayout.Button("⚡ CHẠY TRUY VẤN (EXECUTE)", GUILayout.Height(30))) {
            ExecuteCustomQuery(sqlInput);
        }
        GUILayout.Space(10);

        GUILayout.Label("⚙️ XUẤT MÃ JSON", EditorStyles.boldLabel);
        jsonScrollPos = EditorGUILayout.BeginScrollView(jsonScrollPos, "box", GUILayout.ExpandHeight(true));
        currentJsonOutput = EditorGUILayout.TextArea(currentJsonOutput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        
        if (GUILayout.Button("📋 COPY TOÀN BỘ JSON", GUILayout.Height(35))) {
            GUIUtility.systemCopyBuffer = currentJsonOutput;
            statusMessage = "Đã Copy JSON!";
        }
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    // --- LOGIC ĐỌC CSDL BIẾN THÀNH OBJECT KHUNG LƯỚI ---
    private void FetchTableData(int tableIndex)
    {
        if (!File.Exists(dbPath)) { 
            statusMessage = "LỖI: Xảy ra tình trạng mất File Database! Hãy ấn Play Game 1 lần."; 
            currentTableData = null;
            return; 
        }
        
        using (var db = new SQLiteConnection(dbPath))
        {
            try {
                switch (tableIndex)
                {
                    case 0: ProcessTable<PlayerModel>(db.Table<PlayerModel>().ToList()); break;
                    case 1: ProcessTable<InventoryModel>(db.Table<InventoryModel>().ToList()); break;
                    case 2: ProcessTable<DecorRecordModel>(db.Table<DecorRecordModel>().ToList()); break;
                    case 3: ProcessTable<PlayerQuestModel>(db.Table<PlayerQuestModel>().ToList()); break;
                    case 4: ProcessTable<FarmTileModel>(db.Table<FarmTileModel>().ToList()); break;
                    case 5: ProcessTable<ActiveOrderModel>(db.Table<ActiveOrderModel>().ToList()); break;
                    case 6: ProcessTable<SeedItemModel>(db.Table<SeedItemModel>().ToList()); break;
                    case 7: ProcessTable<CropDataModel>(db.Table<CropDataModel>().ToList()); break;
                    case 8: ProcessTable<DecorItemModel>(db.Table<DecorItemModel>().ToList()); break;
                    case 9: ProcessTable<ProductItemModel>(db.Table<ProductItemModel>().ToList()); break;
                    case 10: ProcessTable<ObstacleModel>(db.Table<ObstacleModel>().ToList()); break;
                }
                statusMessage = $"Đã tải xuất sắc mảng CSDL Bảng {tableIndex + 1}";
            }
            catch (System.Exception ex) {
                statusMessage = "Xảy ra lỗi CSDL (Tạm thời): " + ex.Message;
            }
        }
    }

    private void ProcessTable<T>(List<T> dataList)
    {
        // Phục vụ vế Bảng Lưới Excel bên trái
        currentTableData = dataList.Cast<object>().ToArray();
        currentProperties = typeof(T).GetProperties();

        // Phục vụ vế JSON bên phải (Tự viết Reflection để phá khóa get/set của Unity)
        System.Text.StringBuilder sbJson = new System.Text.StringBuilder();
        sbJson.AppendLine("{");
        sbJson.AppendLine($"  \"TableName\": \"{typeof(T).Name}\",");
        sbJson.AppendLine("  \"Items\": [");
        
        for (int i = 0; i < dataList.Count; i++) {
            sbJson.Append("    {\n");
            sbJson.Append(SmartJsonify(dataList[i], "      "));
            sbJson.Append("\n    }");
            if (i < dataList.Count - 1) sbJson.Append(",");
            sbJson.AppendLine();
        }

        sbJson.AppendLine("  ]");
        sbJson.AppendLine("}");
        currentJsonOutput = sbJson.ToString();
    }

    // Dùng Reflection tự quét JSON thay cho JsonUtility cục súc vướng get set
    private string SmartJsonify(object obj, string indent)
    {
        List<string> parts = new List<string>();
        foreach (var p in currentProperties) {
            object val = p.GetValue(obj);
            if (val == null) parts.Add($"{indent}\"{p.Name}\": null");
            else if (val is string || val is System.DateTime) {
                // Thoát bỏ dấu Quote lỗi nếu bên trong có JSON lồng (như ReqItemListJSON)
                string safeVal = val.ToString().Replace("\"", "\\\""); 
                parts.Add($"{indent}\"{p.Name}\": \"{safeVal}\"");
            }
            else if (val is bool) parts.Add($"{indent}\"{p.Name}\": {val.ToString().ToLower()}");
            else parts.Add($"{indent}\"{p.Name}\": {val}"); // Kể cả số, float, integer
        }
        return string.Join(",\n", parts);
    }
}
