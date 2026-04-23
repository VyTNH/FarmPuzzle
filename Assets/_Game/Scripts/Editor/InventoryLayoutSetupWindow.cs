using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

namespace FarmPuzzle.EditorTools
{
    /// <summary>
    /// Editor Window cho phép setup layout InventoryPanel đúng chuẩn từ Editor,
    /// thay vì hardcode RectTransform trong runtime script.
    /// Menu: FarmPuzzle ▸ 🗂️ Inventory Layout Setup
    /// </summary>
    public class InventoryLayoutSetupWindow : EditorWindow
    {
        // ─── Cấu hình có thể thay đổi từ cửa sổ ───
        private Vector2 _iconOffsetMin = new Vector2(5f, 28f);
        private Vector2 _iconOffsetMax = new Vector2(-5f, -5f);
        private bool    _useRectMask2D = true;
        private bool    _fixImageAlpha = true;
        private bool    _fixScrollRect = true;

        // ─── State ───
        private Vector2 _scroll;
        private string  _statusLog = "";
        private bool    _hasError;

        [MenuItem("FarmPuzzle/🗂️ Inventory Layout Setup")]
        public static void Open()
        {
            var w = GetWindow<InventoryLayoutSetupWindow>("Inventory Layout Setup");
            w.minSize = new Vector2(380, 480);
        }

        private void OnGUI()
        {
            // ── Header ──────────────────────────────────────────────
            EditorGUILayout.Space(6);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 14,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("🗂️ Inventory Layout Setup", headerStyle);
            EditorGUILayout.HelpBox(
                "Công cụ này áp dụng đúng các cài đặt RectTransform lên InventoryPanel ngay trong Editor " +
                "mà không cần hardcode trong runtime script.\n\n" +
                "Mở scene → Chỉnh cấu hình bên dưới → Bấm [Apply to Scene].",
                MessageType.Info);
            EditorGUILayout.Space(6);

            // ── Cấu hình Icon Layout ─────────────────────────────────
            GUILayout.Label("Icon RectTransform", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            _iconOffsetMin = EditorGUILayout.Vector2Field("Offset Min (left, bottom)", _iconOffsetMin);
            _iconOffsetMax = EditorGUILayout.Vector2Field("Offset Max (right, top)",   _iconOffsetMax);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);

            // ── Cấu hình BackGround ──────────────────────────────────
            GUILayout.Label("BackGround / Viewport", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            _useRectMask2D = EditorGUILayout.Toggle("Dùng RectMask2D (thay Mask)",  _useRectMask2D);
            _fixImageAlpha = EditorGUILayout.Toggle("Đảm bảo Image.alpha = 1",      _fixImageAlpha);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);

            // ── Cấu hình ScrollRect ──────────────────────────────────
            GUILayout.Label("ScrollRect", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            _fixScrollRect = EditorGUILayout.Toggle("Chỉ cuộn ngang (horizontal)", _fixScrollRect);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // ── Buttons ──────────────────────────────────────────────
            Color oldColor = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("✅  Apply to Scene (InventoryPanel)", GUILayout.Height(36)))
                ApplyToScene();

            GUI.backgroundColor = new Color(0.3f, 0.6f, 1.0f);
            if (GUILayout.Button("🔍  Select InventoryPanel in Hierarchy", GUILayout.Height(28)))
                SelectInventoryPanel();

            GUI.backgroundColor = new Color(1f, 0.75f, 0.2f);
            if (GUILayout.Button("🖼️  Apply to Selected Prefab / Object", GUILayout.Height(28)))
                ApplyToSelected();

            GUI.backgroundColor = oldColor;

            EditorGUILayout.Space(6);

            // ── Log output ───────────────────────────────────────────
            if (!string.IsNullOrEmpty(_statusLog))
            {
                MessageType msgType = _hasError ? MessageType.Error : MessageType.None;
                GUIStyle logStyle = new GUIStyle(EditorStyles.helpBox) { wordWrap = true, richText = true };
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(200));
                GUILayout.Label(_statusLog, logStyle);
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(4);
            GUILayout.Label("Sau khi Apply: Ctrl+S để lưu Scene.", EditorStyles.miniLabel);
        }

        // ──────────────────────────────────────────────────────────────
        // APPLY TO SCENE
        // ──────────────────────────────────────────────────────────────
        private void ApplyToScene()
        {
            _statusLog = "";
            _hasError  = false;

            // Tìm InventoryPanel (kể cả đang inactive)
            GameObject invPanel = FindInactiveByName("InventoryPanel");
            if (invPanel == null)
            {
                Log("❌ Không tìm thấy 'InventoryPanel' trong scene.", isError: true);
                return;
            }

            Log($"✅ Tìm thấy: <b>{GetPath(invPanel.transform)}</b>");

            // Bật toàn bộ ancestor chain để Apply hoạt động đúng
            // (Nếu Canvas_Father đang inactive thì SetActive(true) trên InventoryPanel không hiệu lực)
            var ancestorStates = EnableAncestorChain(invPanel.transform);

            Apply(invPanel.transform);

            // Khôi phục trạng thái ancestor
            RestoreAncestorChain(ancestorStates);

            EditorUtility.SetDirty(invPanel);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Log("\n<b>Hoàn tất! Nhấn Ctrl+S để lưu scene.</b>");
        }

        // ──────────────────────────────────────────────────────────────
        // APPLY TO SELECTED OBJECT (dùng khi mở Prefab Mode)
        // ──────────────────────────────────────────────────────────────
        private void ApplyToSelected()
        {
            _statusLog = "";
            _hasError  = false;

            if (Selection.activeGameObject == null)
            {
                Log("❌ Chưa chọn object nào trong Hierarchy / Prefab Mode.", isError: true);
                return;
            }

            GameObject sel = Selection.activeGameObject;
            Log($"✅ Áp dụng lên: <b>{GetPath(sel.transform)}</b>");
            Apply(sel.transform);
            EditorUtility.SetDirty(sel);
            Log("\n<b>Hoàn tất!</b>");
        }

        // ──────────────────────────────────────────────────────────────
        // SELECT
        // ──────────────────────────────────────────────────────────────
        private void SelectInventoryPanel()
        {
            GameObject inv = FindInactiveByName("InventoryPanel");
            if (inv != null)
            {
                Selection.activeGameObject = inv;
                EditorGUIUtility.PingObject(inv);
                _statusLog = "🔍 Đã chọn InventoryPanel trong Hierarchy.";
            }
            else
            {
                _statusLog = "❌ Không tìm thấy InventoryPanel.";
                _hasError  = true;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // CORE: Apply layout settings
        // Cấu trúc cần match:
        //   InventoryPanel
        //     └─ Scroll View          ← ScrollRect ở đây
        //          └─ BackGround      ← Viewport / Mask ở đây
        //               └─ SlotContainer  ← Content ở đây
        //                    └─ Slot_*
        //                         ├─ Icon       ← Image
        //                         └─ txt_count  ← Text
        // ──────────────────────────────────────────────────────────────
        private void Apply(Transform root)
        {
            // 1. Bật tạm root để thao tác
            bool wasActive = root.gameObject.activeSelf;
            root.gameObject.SetActive(true);

            // 2. Tìm Scroll View
            Transform scrollViewTr = FindDirectChild(root, "Scroll View");
            if (scrollViewTr == null)
            {
                Log("❌ Không tìm thấy 'Scroll View' con trực tiếp của root.", isError: true);
                root.gameObject.SetActive(wasActive);
                return;
            }
            Log($"   ▸ Scroll View: <b>{GetPath(scrollViewTr)}</b>");

            // 3. Tìm BackGround (Viewport) con của Scroll View
            Transform bgTr = FindDirectChild(scrollViewTr, "BackGround")
                          ?? FindDirectChild(scrollViewTr, "Viewport");
            if (bgTr == null)
            {
                Log("❌ Không tìm thấy 'BackGround' hoặc 'Viewport' bên trong Scroll View.", isError: true);
                root.gameObject.SetActive(wasActive);
                return;
            }
            Log($"   ▸ BackGround (Viewport): <b>{bgTr.name}</b>");

            // 4. Tìm SlotContainer (Content) con của BackGround
            Transform container = FindDirectChild(bgTr, "SlotContainer")
                               ?? FindDirectChild(bgTr, "Content");
            if (container == null)
            {
                Log("❌ Không tìm thấy 'SlotContainer' hoặc 'Content' bên trong BackGround.", isError: true);
                root.gameObject.SetActive(wasActive);
                return;
            }
            Log($"   ▸ SlotContainer (Content): <b>{container.name}</b>");

            // 5. Fix ScrollRect trên Scroll View
            if (_fixScrollRect)
                FixScrollRect(scrollViewTr);

            // 6. Fix BackGround — mask & image alpha
            FixBackGround(bgTr);

            // 7. Fix SlotContainer RectTransform
            FixSlotContainerRect(container);

            // 8. Fix tất cả child slots hiện có (preview items)
            int fixedSlots = 0;
            foreach (Transform slot in container)
            {
                FixSlotRect(slot);
                FixIconRect(slot);
                fixedSlots++;
            }
            Log($"   ▸ Đã fix {fixedSlots} slot con (Icon + txt_count).");

            root.gameObject.SetActive(wasActive);
        }

        // ──────────────────────────────────────────────────────────────
        // FIX HELPERS
        // ──────────────────────────────────────────────────────────────

        /// <summary>SlotContainer: anchor left-edge, stretch height, không overflow Y</summary>
        private void FixSlotContainerRect(Transform container)
        {
            RectTransform rt = container.GetComponent<RectTransform>();
            if (rt == null) return;

            Undo.RecordObject(rt, "Fix SlotContainer Rect");
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(rt.sizeDelta.x, 0f);

            Log("   ▸ SlotContainer RectTransform → anchor left-stretch, anchoredPos = 0.");
        }

        /// <summary>Scroll View: fix ScrollRect direction và gán viewport/content nếu chưa có</summary>
        private void FixScrollRect(Transform scrollViewTr)
        {
            ScrollRect sr = scrollViewTr.GetComponent<ScrollRect>();
            if (sr == null)
            {
                Log("   ⚠️ Scroll View không có ScrollRect component.", isError: true);
                return;
            }

            Undo.RecordObject(sr, "Fix ScrollRect");
            sr.horizontal = true;
            sr.vertical   = false;
            sr.horizontalNormalizedPosition = 0f;

            // Gán viewport nếu chưa có
            if (sr.viewport == null)
            {
                RectTransform bgRt = (FindDirectChild(scrollViewTr, "BackGround")
                                   ?? FindDirectChild(scrollViewTr, "Viewport"))
                                   ?.GetComponent<RectTransform>();
                if (bgRt != null) { sr.viewport = bgRt; Log("   ▸ ScrollRect.viewport → BackGround."); }
            }

            // Gán content nếu chưa có
            if (sr.content == null)
            {
                Transform bgTr2 = FindDirectChild(scrollViewTr, "BackGround") ?? FindDirectChild(scrollViewTr, "Viewport");
                if (bgTr2 != null)
                {
                    RectTransform contentRt = (FindDirectChild(bgTr2, "SlotContainer")
                                           ?? FindDirectChild(bgTr2, "Content"))
                                           ?.GetComponent<RectTransform>();
                    if (contentRt != null) { sr.content = contentRt; Log("   ▸ ScrollRect.content → SlotContainer."); }
                }
            }

            Log("   ▸ ScrollRect → horizontal=true, vertical=false.");
        }

        /// <summary>BackGround: swap Mask → RectMask2D, fix Image.alpha</summary>
        private void FixBackGround(Transform bgTr)
        {
            if (_useRectMask2D)
            {
                Mask oldMask = bgTr.GetComponent<Mask>();
                if (oldMask != null && oldMask.enabled)
                {
                    Undo.RecordObject(oldMask, "Disable Mask");
                    oldMask.enabled = false;
                    Log("   ▸ BackGround.Mask → disabled.");
                }

                if (bgTr.GetComponent<RectMask2D>() == null)
                {
                    Undo.AddComponent<RectMask2D>(bgTr.gameObject);
                    Log("   ▸ BackGround → thêm RectMask2D.");
                }
                else
                {
                    Log("   ▸ BackGround đã có RectMask2D.");
                }
            }

            if (_fixImageAlpha)
            {
                Image bgImg = bgTr.GetComponent<Image>();
                if (bgImg != null && bgImg.color.a < 0.01f)
                {
                    Undo.RecordObject(bgImg, "Fix BackGround Image Alpha");
                    bgImg.enabled = true;
                    bgImg.color   = new Color(bgImg.color.r, bgImg.color.g, bgImg.color.b, 1f);
                    Log("   ▸ BackGround Image.alpha → 1.");
                }
                else if (bgImg == null)
                {
                    Log("   ⚠️ BackGround không có Image component — RectMask2D cần Image để clip đúng.", isError: true);
                }
            }
        }

        /// <summary>Slot: anchor bottom-left (dùng khi HorizontalLayoutGroup không có)</summary>
        private void FixSlotRect(Transform slot)
        {
            RectTransform rt = slot.GetComponent<RectTransform>();
            if (rt == null) return;

            Undo.RecordObject(rt, "Fix Slot Rect");
            // Chỉ fix nếu sizeDelta.y = 0 (chưa được set)
            if (rt.sizeDelta.y <= 0)
                rt.sizeDelta = new Vector2(rt.sizeDelta.x > 0 ? rt.sizeDelta.x : 100f, 90f);
        }

        /// <summary>Icon: stretch full với padding đã cấu hình</summary>
        private void FixIconRect(Transform slot)
        {
            Transform iconTr = slot.Find("Icon");
            if (iconTr == null) return;

            RectTransform rt = iconTr.GetComponent<RectTransform>();
            if (rt == null) return;

            Undo.RecordObject(rt, "Fix Icon Rect");
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = _iconOffsetMin;
            rt.offsetMax = _iconOffsetMax;
        }

        // ──────────────────────────────────────────────────────────────
        // UTILITIES
        // ──────────────────────────────────────────────────────────────

        /// <summary>Tìm đệ quy toàn bộ cây con</summary>
        private static Transform FindChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>Chỉ tìm trong con trực tiếp (không đệ quy) — dùng khi navigate từng bước</summary>
        private static Transform FindDirectChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
                if (child.name == name) return child;
            return null;
        }

        private static GameObject FindInactiveByName(string targetName)
        {
            // Tìm kể cả object đang inactive
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in all)
            {
                if (go.name == targetName && !string.IsNullOrEmpty(go.scene.name))
                    return go;
            }
            return null;
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t    = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        /// <summary>
        /// Bật toàn bộ ancestor (cha, ông, ...) đang inactive để việc thao tác con hoạt động đúng.
        /// Trả về danh sách (Transform, wasActive) để restore sau.
        /// </summary>
        private static System.Collections.Generic.List<(Transform t, bool wasActive)> EnableAncestorChain(Transform start)
        {
            var states = new System.Collections.Generic.List<(Transform, bool)>();
            Transform cur = start.parent;
            while (cur != null)
            {
                states.Add((cur, cur.gameObject.activeSelf));
                if (!cur.gameObject.activeSelf)
                    cur.gameObject.SetActive(true);
                cur = cur.parent;
            }
            return states;
        }

        private static void RestoreAncestorChain(System.Collections.Generic.List<(Transform t, bool wasActive)> states)
        {
            // Khôi phục ngược từ gốc về để tránh deactivate parent làm child bị mất
            for (int i = states.Count - 1; i >= 0; i--)
                states[i].t.gameObject.SetActive(states[i].wasActive);
        }

        private void Log(string msg, bool isError = false)
        {
            if (isError) _hasError = true;
            _statusLog += msg + "\n";
            Debug.Log("[InventoryLayout] " + msg);
            Repaint();
        }
    }
}
