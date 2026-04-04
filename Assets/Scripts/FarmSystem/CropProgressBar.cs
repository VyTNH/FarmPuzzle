using UnityEngine;

public class CropProgressBar : MonoBehaviour
{
    [Header("References - kéo vào Inspector")]
    public SpriteRenderer bgRenderer;  // Nền xám của bar
    public SpriteRenderer fillRenderer; // Phần xanh lá fill theo tiến độ

    [Header("Cài đặt")]
    public float barWidth = 0.8f;   // Chiều ngang tối đa của bar
    public float barHeight = 0.08f; // Chiều cao bar
    public Vector3 offset = new Vector3(0, 0.6f, -0.2f); // Vị trí so với ô đất

    private LandPlot landPlot;

    void Awake()
    {
        landPlot = GetComponentInParent<LandPlot>();
        CreateBar();
    }

    void CreateBar()
    {
        // --- Nền xám ---
        GameObject bg = new GameObject("ProgressBar_BG");
        bg.transform.SetParent(this.transform);
        bg.transform.localPosition = offset;
        bgRenderer = bg.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = CreatePixelSprite(Color.gray);
        bgRenderer.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        bgRenderer.sortingOrder = 2;

        // --- Fill xanh lá ---
        GameObject fill = new GameObject("ProgressBar_Fill");
        fill.transform.SetParent(bg.transform);
        fill.transform.localPosition = new Vector3(-0.5f, 0, -0.1f); // Bắt đầu từ cạnh trái
        fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = CreatePixelSprite(Color.green);
        fillRenderer.transform.localScale = new Vector3(0f, 1f, 1f); // Width = 0 lúc đầu
        fillRenderer.sortingOrder = 3;

        SetVisible(false); // Ẩn khi chưa trồng
    }

    private float lastLogTime = 0f;

    void Update()
    {
        if (landPlot == null)
        {
            SetVisible(false);
            if (Time.time - lastLogTime > 2f) { Debug.LogWarning("[CropProgressBar] landPlot is null!"); lastLogTime = Time.time; }
            return;
        }

        if (!landPlot.isOccupied)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        float progress = landPlot.GetGrowthProgress();
        
        if (Time.time - lastLogTime > 2f)
        {
            Debug.Log($"[CropProgressBar] Plot: {landPlot.plotID} | Occupied: true | Crop: {(landPlot.GetCropData() != null ? landPlot.GetCropData().productID : "NULL")} | Progress: {progress * 100:F1}%");
            lastLogTime = Time.time;
        }

        UpdateBar(progress);
    }

    void UpdateBar(float progress)
    {
        if (fillRenderer == null) return;

        // Scale fill từ 0 → 1 theo chiều ngang
        // pivot ở giữa nên phải dịch chuyển localPosition để fill từ trái sang phải
        fillRenderer.transform.localScale = new Vector3(progress, 1f, 1f);
        fillRenderer.transform.localPosition = new Vector3(-0.5f + progress * 0.5f, 0, -0.1f);

        // Đổi màu theo tiến độ
        if (progress < 0.5f)
            fillRenderer.color = Color.green;
        else if (progress < 1f)
            fillRenderer.color = Color.yellow;
        else
            fillRenderer.color = new Color(1f, 0.5f, 0f); // Cam = sẵn sàng thu hoạch
    }

    void SetVisible(bool visible)
    {
        if (bgRenderer != null) bgRenderer.enabled = visible;
        if (fillRenderer != null) fillRenderer.enabled = visible;
    }

    // Tạo sprite 1x1 pixel đơn giản
    Sprite CreatePixelSprite(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}