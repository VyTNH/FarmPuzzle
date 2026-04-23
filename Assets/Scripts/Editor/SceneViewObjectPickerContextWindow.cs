using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

public class SceneViewObjectPickerContextWindow : EditorWindow
{
    private class Entry
    {
        public readonly Transform Transform;
        public readonly List<Entry> Children;

        public Entry(Transform transform)
        {
            Transform = transform;
            Children = new List<Entry>(2);
        }
    }

    private readonly List<Transform> transforms = new(16);
    private readonly List<string> labels = new(16);

    private static Transform hoveredTransform;
    private static readonly Vector3[] hoveredTransformCorners = new Vector3[4];

    private static double lastRightClickTime;
    private static Vector2 lastRightPos;
    private static bool blockSceneViewInput;

    private static MethodInfo screenFittedRectGetter;
    private static FieldInfo editorWindowHostViewGetter;
    private static PropertyInfo hostViewContainerWindowGetter;

    private const float Padding = 1f;
    private float RowHeight => EditorGUIUtility.singleLineHeight;
    private GUIStyle RowGUIStyle => "MenuItem";

    private static bool CanPickUIObjects
    {
        get => EditorPrefs.GetBool("SVOPCWUI", true);
        set => EditorPrefs.SetBool("SVOPCWUI", value);
    }

    private static bool CanPickSpriteRenderers
    {
        get => EditorPrefs.GetBool("SVOPCWSR", true);
        set => EditorPrefs.SetBool("SVOPCWSR", value);
    }

    private static bool CanPickOtherRenderers
    {
        get => EditorPrefs.GetBool("SVOPCWOR", true);
        set => EditorPrefs.SetBool("SVOPCWOR", value);
    }

#if UNITY_2022_1_OR_NEWER
    private static bool ShowPreciseRendererOutline
    {
        get => EditorPrefs.GetBool("SVOPCWPRO", true);
        set => EditorPrefs.SetBool("SVOPCWPRO", value);
    }
#endif

    private void ShowContextWindow(List<Entry> entries)
    {
        StringBuilder sb = new StringBuilder(100);
        InitializeResultsRecursive(entries, 0, sb);

        GUIStyle rowGUIStyle = RowGUIStyle;
        float preferredWidth = 0f;
        foreach (string label in labels)
            preferredWidth = Mathf.Max(preferredWidth, rowGUIStyle.CalcSize(new GUIContent(label)).x);

        Vector2 size = new Vector2(preferredWidth + Padding * 2f, transforms.Count * RowHeight + Padding * 2f);
        ShowAsDropDown(new Rect(), size);
        Rect rect = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition) - new Vector2(0f, size.y), size); // Show dropdown above the cursor instead of below the cursor
        minSize = maxSize = size; /// These values are changed by <see cref="EditorWindow.ShowAsDropDown"/> so reset them once again
        position = GetScreenFittedRect(rect, this);
    }

    private void InitializeResultsRecursive(List<Entry> entries, int depth, StringBuilder sb)
    {
        foreach (Entry entry in entries)
        {
            sb.Length = 0;

            transforms.Add(entry.Transform);
            labels.Add(sb.Append(' ', depth * 4).Append(entry.Transform.name).ToString());

            if (entry.Children.Count > 0)
                InitializeResultsRecursive(entry.Children, depth + 1, sb);
        }
    }

    protected void OnEnable()
    {
        wantsMouseMove = wantsMouseEnterLeaveWindow = true;
        wantsLessLayoutEvents = false;
        blockSceneViewInput = true;
    }

    protected void OnDisable()
    {
        hoveredTransform = null;
        SceneView.RepaintAll();
    }

    protected void OnGUI()
    {
        Event ev = Event.current;

        float rowWidth = position.width - Padding * 2f, rowHeight = RowHeight;
        GUIStyle rowGUIStyle = RowGUIStyle;
        int hoveredRowIndex = -1;
        for (int i = 0; i < transforms.Count; i++)
        {
            Rect rect = new Rect(Padding, Padding + i * rowHeight, rowWidth, rowHeight);
            if (GUI.Button(rect, labels[i], rowGUIStyle))
            {
                if (transforms[i] != null)
                    Selection.activeTransform = transforms[i];

                blockSceneViewInput = false;
                ev.Use();
                Close();
                GUIUtility.ExitGUI();
            }

            if (hoveredRowIndex < 0 && ev.type == EventType.MouseMove && rect.Contains(ev.mousePosition))
                hoveredRowIndex = i;
        }

        if (ev.type == EventType.MouseMove || ev.type == EventType.MouseLeaveWindow)
        {
            Transform newHoveredTransform = (hoveredRowIndex >= 0) ? transforms[hoveredRowIndex] : null;
            if (newHoveredTransform != hoveredTransform)
            {
                hoveredTransform = newHoveredTransform;
                Repaint();
                SceneView.RepaintAll();
            }
        }
    }

    [InitializeOnLoadMethod]
    private static void InitializeDuringSceneGUI() => SceneView.duringSceneGui += DuringSceneGUI;
    private static void DuringSceneGUI(SceneView sceneView)
    {
        /// Couldn't get <see cref="EventType.ContextClick"/> to work here in Unity 5.6 so implemented context click detection manually
        Event ev = Event.current;
        if (ev.type == EventType.MouseDown)
        {
            if (ev.button == 1)
            {
                lastRightClickTime = EditorApplication.timeSinceStartup;
                lastRightPos = ev.mousePosition;
            }
            else if (blockSceneViewInput)
            {
                // User has clicked outside the context window to close it. Ignore this click in Scene view if it's left click
                blockSceneViewInput = false;

                if (ev.button == 0)
                {
                    GUIUtility.hotControl = 0;
                    ev.Use();
                }
            }
        }
        else if (ev.type == EventType.MouseUp)
        {
            if (ev.button == 1 && EditorApplication.timeSinceStartup - lastRightClickTime < 0.2 && (ev.mousePosition - lastRightPos).magnitude < 2f)
                OnSceneViewRightClicked(sceneView);
        }
        else if (ev.type == EventType.Repaint)
            HighlightHoveredTransformOnSceneView();
    }

    private static void HighlightHoveredTransformOnSceneView()
    {
        if (hoveredTransform == null)
            return;

        if (hoveredTransform.TryGetComponent(out Renderer renderer) && renderer.sharedMaterial != null)
        {
            // Clear depth buffer so that the highlight will be drawn above everything else (i.e. not blocked by z-testing)
            GL.Clear(true, false, default);

            // First, redraw the object above everything else (thanks to clearing the depth buffer before)
            if (hoveredTransform.TryGetComponent(out MeshFilter meshFilter) && meshFilter.sharedMesh != null)
            {
                if (renderer.sharedMaterial.SetPass(0))
                    Graphics.DrawMeshNow(meshFilter.sharedMesh, hoveredTransform.localToWorldMatrix);
            }
            else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh != null)
            {
                Mesh mesh = new();
                try
                {
                    skinnedMeshRenderer.BakeMesh(mesh, true);

                    if (renderer.sharedMaterial.SetPass(0))
                        Graphics.DrawMeshNow(mesh, hoveredTransform.localToWorldMatrix);
                }
                finally
                {
                    DestroyImmediate(mesh);
                }
            }
            else if (renderer is SpriteRenderer spriteRenderer && spriteRenderer.sprite != null)
            {
                Mesh spriteMesh = new()
                {
                    vertices = Array.ConvertAll(spriteRenderer.sprite.vertices, (e) => (Vector3)e),
                    uv = spriteRenderer.sprite.uv,
                    triangles = Array.ConvertAll(spriteRenderer.sprite.triangles, (e) => (int)e),
                };
                Material material = new(renderer.sharedMaterial) { mainTexture = spriteRenderer.sprite.texture };

                try
                {
                    if (material.SetPass(0))
                        Graphics.DrawMeshNow(spriteMesh, hoveredTransform.localToWorldMatrix);
                }
                finally
                {
                    DestroyImmediate(spriteMesh);
                    DestroyImmediate(material);
                }
            }

#if UNITY_2022_1_OR_NEWER
            if (ShowPreciseRendererOutline)
            {
                GameObject[] rendererGO = new GameObject[] { renderer.gameObject };
                Handles.DrawOutline(rendererGO, new Color(1f, 1f, 0f, 1f), 0.35f);
                Handles.DrawOutline(rendererGO, Color.black, 0f);
            }
            else
#endif
            {
                Bounds bounds = renderer.localBounds;
                if (renderer is SpriteRenderer)
                {
                    hoveredTransformCorners[0] = hoveredTransform.TransformPoint(bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, 0f));
                    hoveredTransformCorners[1] = hoveredTransform.TransformPoint(bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, 0f));
                    hoveredTransformCorners[2] = hoveredTransform.TransformPoint(bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, 0f));
                    hoveredTransformCorners[3] = hoveredTransform.TransformPoint(bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, 0f));

                    Handles.DrawSolidRectangleWithOutline(hoveredTransformCorners, new Color(1f, 1f, 0f, 0.25f), Color.black);
                }
                else
                {
                    // Draw the bounds of the renderer
                    Transform pivot = (renderer is SkinnedMeshRenderer skinnedMeshRenderer) ? skinnedMeshRenderer.rootBone : hoveredTransform;
                    using (new Handles.DrawingScope(new Color(1f, 1f, 0f, 0.25f), Matrix4x4.TRS(pivot.TransformPoint(bounds.center), pivot.rotation, Vector3.Scale(pivot.lossyScale, bounds.size))))
                    {
                        bool lighting = Handles.lighting;
                        Handles.lighting = false;
                        Handles.CubeHandleCap(0, Vector3.zero, Quaternion.identity, 1f, EventType.Repaint);
                        Handles.color = Color.black;
                        Handles.DrawWireCube(Vector3.zero, Vector3.one);
                        Handles.lighting = lighting;
                    }
                }
            }
        }
        else if (hoveredTransform is RectTransform rectTransform)
        {
            rectTransform.GetWorldCorners(hoveredTransformCorners);
            Handles.DrawSolidRectangleWithOutline(hoveredTransformCorners, new Color(1f, 1f, 0f, 0.25f), Color.black);
        }
    }

    private static void OnSceneViewRightClicked(SceneView sceneView)
    {
        Vector2 eventMousePos = Event.current.mousePosition;
        Vector2 pointerPos = HandleUtility.GUIPointToScreenPixelCoordinate(eventMousePos);
        List<Entry> entries = new(8);
        List<GameObject> iteratedGameObjects = new(8);
        while (true)
        {
            // Find all GameObjects under the cursor. Logic copied from Unity's own context menu that shows up with CTRL+RMB on Unity 6+.
            // https://github.com/Unity-Technologies/UnityCsReference/blob/59b03b8a0f179c0b7e038178c90b6c80b340aa9f/Editor/Mono/SceneView/SceneViewPicking.cs#L147-L178
            GameObject gameObject = HandleUtility.PickGameObject(eventMousePos, false, iteratedGameObjects.ToArray(), null);
            if (gameObject == null || (entries.Count > 0 && gameObject == entries[^1].Transform.gameObject))
                break;

            iteratedGameObjects.Add(gameObject);
            if (IsGameObjectValid(gameObject, pointerPos, sceneView.camera))
                entries.Add(new(gameObject.transform));
        }

        // Form parent-child relationships
        List<Entry> rootEntries = new(entries.Count);
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry entry = entries[i];
            Entry parentEntry = null;
            int distanceToParentEntry = int.MaxValue;
            foreach (Entry candidateParentEntry in entries)
            {
                if (entry == candidateParentEntry)
                    continue;

                if (entry.Transform.IsChildOf(candidateParentEntry.Transform))
                {
                    int distance = 1;
                    for (Transform parent = entry.Transform.parent; parent != candidateParentEntry.Transform; parent = parent.parent)
                        distance++;

                    if (distance < distanceToParentEntry)
                        (parentEntry, distanceToParentEntry) = (candidateParentEntry, distance);
                }
            }

            if (parentEntry != null)
                parentEntry.Children.Add(entry);
            else
                rootEntries.Add(entry);
        }

        // Remove invisible root entries with no children from the results
        rootEntries.RemoveAll((canvasEntry) => canvasEntry.Children.Count == 0 && !canvasEntry.Transform.TryGetComponent(out Graphic _) && !canvasEntry.Transform.TryGetComponent(out Renderer _));

        // Sort root entries in reverse order (Transform that is closest to cursor should be located at the bottom)
        rootEntries.Sort((e1, e2) =>
        {
            if (e1.Transform.GetComponentInParent<Canvas>(true) is Canvas c1 && e2.Transform.GetComponentInParent<Canvas>(true) is Canvas c2)
            {
                // Both entries are UI elements, sort by their Canvas sorting orders
                int sortingOrderComparison = c1.sortingOrder.CompareTo(c2.sortingOrder);
                if (sortingOrderComparison != 0)
                    return sortingOrderComparison;
            }

            // Sort by the entries' original order returned by Unity
            return entries.IndexOf(e2).CompareTo(entries.IndexOf(e1));
        });

        // If any results are found, show the context window
        if (rootEntries.Count > 0)
            CreateInstance<SceneViewObjectPickerContextWindow>().ShowContextWindow(rootEntries);
    }

    private static bool IsGameObjectValid(GameObject gameObject, Vector2 pointerPos, Camera camera)
    {
        if (SceneVisibilityManager.instance.IsHidden(gameObject, false))
            return false;

        if (SceneVisibilityManager.instance.IsPickingDisabled(gameObject, false))
            return false;

        if (gameObject.TryGetComponent(out Renderer renderer))
        {
            if (!renderer.enabled)
                return false;

            return (renderer is SpriteRenderer) ? CanPickSpriteRenderers : CanPickOtherRenderers;
        }

        if (gameObject.transform is not RectTransform)
            return false;

        if (!CanPickUIObjects)
            return false;

        if (gameObject.TryGetComponent(out CanvasRenderer canvasRenderer) && canvasRenderer.cull)
            return false;

        if (gameObject.TryGetComponent(out Graphic graphic) && !graphic.enabled)
            return false;

        if (gameObject.GetComponentInParent<Canvas>(true) is Canvas canvas && !canvas.enabled)
            return false;

        using (ListPool<CanvasGroup>.Get(out var canvasGroups))
        {
            gameObject.GetComponentsInParent(false, canvasGroups);
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                if (!canvasGroup.enabled)
                    continue;
                else if (canvasGroup.alpha == 0f)
                    return false;
                else if (canvasGroup.ignoreParentGroups)
                    break;
            }
        }

        // If the target is a MaskableGraphic that ignores masks (i.e. visible outside masks) and isn't fully transparent, accept it
        if (gameObject.TryGetComponent(out MaskableGraphic maskableGraphic) && !maskableGraphic.maskable && maskableGraphic.color.a > 0f)
            return true;

        using (ListPool<ICanvasRaycastFilter>.Get(out var raycastFilters))
        {
            gameObject.GetComponentsInParent(false, raycastFilters);
            foreach (ICanvasRaycastFilter raycastFilter in raycastFilters)
            {
                if (raycastFilter is not CanvasGroup && !raycastFilter.IsRaycastLocationValid(pointerPos, camera))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Restricts the given Rect within the screen's bounds.
    /// </summary>
    private static Rect GetScreenFittedRect(Rect originalRect, EditorWindow editorWindow)
    {
        screenFittedRectGetter ??= typeof(EditorWindow).Assembly.GetType("UnityEditor.ContainerWindow").GetMethod("FitRectToScreen", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        if (screenFittedRectGetter.GetParameters().Length == 3)
            return (Rect)screenFittedRectGetter.Invoke(null, new object[] { originalRect, true, true });
        else
        {
            // New version introduced in Unity 2022.3.62f1, Unity 6.0.49f1 and Unity 6.1.0f1.
            // Usage example: https://github.com/Unity-Technologies/UnityCsReference/blob/10f8718268a7e34844ba7d59792117c28d75a99b/Editor/Mono/EditorWindow.cs#L1264
            editorWindowHostViewGetter ??= typeof(EditorWindow).GetField("m_Parent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            hostViewContainerWindowGetter ??= typeof(EditorWindow).Assembly.GetType("UnityEditor.HostView").GetProperty("window", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            return (Rect)screenFittedRectGetter.Invoke(null, new object[] { originalRect, originalRect.center, true, hostViewContainerWindowGetter.GetValue(editorWindowHostViewGetter.GetValue(editorWindow), null) });
        }
    }

    [SettingsProvider]
    public static SettingsProvider CreatePreferencesGUI()
    {
        return new SettingsProvider("Preferences/yasirkula/Scene View Object Picker Context Window", SettingsScope.User)
        {
            guiHandler = (searchContext) => PreferencesGUI(),
            keywords = new HashSet<string>() { "Scene", "View", "Object", "Picker", "Context", "Window" }
        };
    }

    public static void PreferencesGUI()
    {
        EditorGUI.BeginChangeCheck();

        CanPickUIObjects = EditorGUILayout.ToggleLeft("Can pick UI objects", CanPickUIObjects);
        CanPickSpriteRenderers = EditorGUILayout.ToggleLeft("Can pick SpriteRenderers", CanPickSpriteRenderers);
        CanPickOtherRenderers = EditorGUILayout.ToggleLeft("Can pick other Renderers (e.g. MeshRenderer)", CanPickOtherRenderers);
#if UNITY_2022_1_OR_NEWER
        ShowPreciseRendererOutline = EditorGUILayout.ToggleLeft("Show precise outline for hovered Renderers", ShowPreciseRendererOutline);
#endif
    }
}