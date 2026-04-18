// FILE NÀY ĐÃ HOÀN THÀNH NHIỆM VỤ VÀ ĐƯỢC ARCHIVE LẠI.
// Để dùng lại, bỏ comment toàn bộ code bên dưới.

/* ===== ARCHIVED: DynamicIsoConverter =====

using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Linq;

namespace FarmPuzzle.EditorTools
{
    public class DynamicIsoConverter : EditorWindow
    {
        [MenuItem("FarmPuzzle/🔧 2-Click: Cấu Trúc Lại Đất Động Isometric")]
        public static void TransformPlots()
        {
            // 1. TÌM VÀ XÓA BỎ LỚP TILEMAP TĨNH CỦA FARM
            var gridObj = GameObject.Find("Grid_Isometric_Merged");
            if (gridObj == null) { Debug.LogError("❌ Không tìm thấy Grid_Isometric_Merged!"); return; }

            var tilemapFarm = gridObj.transform.Find("Tilemap-Farm");
            if (tilemapFarm != null) DestroyImmediate(tilemapFarm.gameObject);

            // 2. TÌM 25 Ô ĐẤT CŨ NGOÀI HIERARCHY VÀ ĐƯA VÀO GRID
            var parentOld = GameObject.Find("Land Plots");
            if (parentOld == null) { Debug.LogError("❌ Không tìm thấy thư mục 'Land Plots' cũ!"); return; }

            Grid isoGrid = gridObj.GetComponent<Grid>();
            var allPlots = parentOld.GetComponentsInChildren<LandPlot>();

            parentOld.transform.SetParent(gridObj.transform);

            // 3. TÍNH TOÁN QUY ĐỔI VỊ TRÍ (Snap to Isometric Grid)
            foreach(var plot in allPlots)
            {
                string[] parts = plot.name.Split('_');
                if(parts.Length >= 3)
                {
                    if(int.TryParse(parts[1], out int gridX) && int.TryParse(parts[2], out int gridY))
                    {
                        Vector3Int cellPos = new Vector3Int(gridX, gridY, 0);
                        plot.transform.position = isoGrid.GetCellCenterWorld(cellPos);
                        plot.transform.localScale = Vector3.one;

                        var sr = plot.GetComponent<SpriteRenderer>();
                        if(sr != null) sr.sortingOrder = -(gridX + gridY);

                        var box = plot.GetComponent<BoxCollider2D>();
                        if (box != null) DestroyImmediate(box);

                        if (plot.GetComponent<PolygonCollider2D>() == null)
                            plot.gameObject.AddComponent<PolygonCollider2D>();
                    }
                }
            }

            // 4. KẾT NỐI LẠI GRID MANAGER
            var gm = gridObj.GetComponent<FarmPuzzle.FarmSystem.GridManager>();
            if (gm != null) gm.plots = allPlots.ToList();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=cyan>✅ TÁI CẤU TRÚC ĐẤT_ĐỘNG THÀNH CÔNG!</color>");
        }
    }
}

===== END ARCHIVED ===== */
