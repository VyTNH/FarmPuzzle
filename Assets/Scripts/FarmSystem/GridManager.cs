using UnityEngine;
using UnityEngine.InputSystem;
using static CropStatus;

public class GridManager : MonoBehaviour
{
    [Header("Setup")]
    public GameObject plotPrefab;
    public int width = 10;
    public int height = 10;

    [Header("Trồng cây")]
    public SeedItemSO selectedSeed;

    [Header("Chăm sóc thủ công")]
    [Tooltip("Tool đang cầm trên tay. None = không chăm sóc")]
    public CropNeed selectedCareTool = CropNeed.None;

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                Instantiate(plotPrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            HandleInteraction();
    }

    void HandleInteraction()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider == null) return;

        LandPlot clickedPlot = hit.collider.GetComponent<LandPlot>();
        if (clickedPlot == null) return;

        // --- Ô đất trống → Trồng cây ---
        if (!clickedPlot.isOccupied)
        {
            if (selectedSeed != null)
            {
                clickedPlot.Plant(selectedSeed);
                Debug.Log($"Đã trồng: {selectedSeed.seedName}");
            }
            return;
        }

        // --- Ô đã có cây ---

        // Nếu đang cầm tool chăm sóc → chăm sóc
        if (selectedCareTool != CropNeed.None)
        {
            clickedPlot.ApplyCare(selectedCareTool);
            return;
        }

        // Nếu không cầm tool → thử thu hoạch
        if (clickedPlot.CanHarvest())
        {
            clickedPlot.Harvest();
        }
        else
        {
            // Thông báo lý do chưa harvest được
            float progress = clickedPlot.GetGrowthProgress();
            if (progress < 1f)
                Debug.Log($"Cây chưa chín! Tiến độ: {(progress * 100f):F0}%");
            else if (clickedPlot.currentNeed != CropNeed.None)
                Debug.Log($"Cây cần chăm sóc trước: {clickedPlot.currentNeed}");
        }
    }
}