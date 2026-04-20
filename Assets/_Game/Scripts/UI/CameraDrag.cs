using UnityEngine;

public class CameraDrag : MonoBehaviour
{
    public static CameraDrag Instance;

    // ─── Kéo map ───
    [Header("Giới Hạn Lướt (Clamp Bounds)")]
    [Tooltip("Tương ứng với bản đồ 50x50 Isometric")]
    public float minX = -40f;
    public float maxX = 40f;
    public float minY = -10f;
    public float maxY = 45f;

    [Header("Độ Nhạy")]
    public float panSpeed = 1f;

    [Header("Phân Biệt Kéo / Bấm")]
    public float dragThresholdPixels = 10f;

    public static bool IsDragging { get; private set; }

    private Vector3 _dragOriginWorld;
    private Vector2 _mouseDownScreen;
    private Camera cam;

    // ─── Zoom ───
    [Header("Giới Hạn Zoom")]
    public float minZoom = 2f;
    public float maxZoom = 12f;
    public float zoomSpeedMouse = 2f;
    public float zoomSpeedTouch = 0.01f;

    private void Awake() { Instance = this; }

    public void ResetZoom()
    {
        if (cam != null) cam.orthographicSize = maxZoom;
    }

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    public static bool IsLockedByTool = false;

    private void LateUpdate()
    {
        bool isTetris = FarmPuzzle.Tetris.TetrisManager.Instance != null && FarmPuzzle.Tetris.TetrisManager.Instance.isPlaying;
        bool isLand = FarmPuzzle.LandPuzzle.LandPuzzleManager.Instance != null && FarmPuzzle.LandPuzzle.LandPuzzleManager.Instance.IsPuzzleActive;
        
        if (isTetris || isLand || IsLockedByTool) return;

        PanCamera();
        ZoomCamera();
    }

    private void PanCamera()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _dragOriginWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            _mouseDownScreen = Input.mousePosition;
            IsDragging = false;
        }

        if (Input.GetMouseButton(0))
        {
            float pixelDelta = Vector2.Distance((Vector2)Input.mousePosition, _mouseDownScreen);
            if (pixelDelta > dragThresholdPixels)
                IsDragging = true;

            if (IsDragging)
            {
                Vector3 diff = _dragOriginWorld - cam.ScreenToWorldPoint(Input.mousePosition);
                Vector3 newPos = cam.transform.position + diff * panSpeed;
                cam.transform.position = new Vector3(
                    Mathf.Clamp(newPos.x, minX, maxX),
                    Mathf.Clamp(newPos.y, minY, maxY),
                    cam.transform.position.z);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            IsDragging = false;
        }
    }

    private void ZoomCamera()
    {
        if (!cam.orthographic) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            cam.orthographicSize -= scroll * zoomSpeedMouse;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }

        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float prevMag = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
            float curMag  = (t0.position - t1.position).magnitude;
            cam.orthographicSize -= (curMag - prevMag) * zoomSpeedTouch;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);
        Vector3 size   = new Vector3(maxX - minX, maxY - minY, 0);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
