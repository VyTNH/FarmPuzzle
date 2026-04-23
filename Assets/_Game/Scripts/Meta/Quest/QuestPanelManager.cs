using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FarmPuzzle.Meta;

namespace FarmPuzzle.UI
{
    /// <summary>
    /// Gắn vào Panel_Quests trong scene.
    /// Tự spawn QuestCardUI và nút Kết Thúc Phiên, xử lý popup.
    /// </summary>
    public class QuestPanelManager : MonoBehaviour
    {
        [Tooltip("Container để chứa các quest card. Nếu trống sẽ tự tạo.")]
        public RectTransform questListContainer;

        private Text _scoreText;
        private GameObject _overlay; // popup container

        static readonly Color ColRed    = new Color(0.82f, 0.20f, 0.16f, 1f);
        static readonly Color ColGreen  = new Color(0.18f, 0.68f, 0.30f, 1f);
        static readonly Color ColGray   = new Color(0.32f, 0.32f, 0.32f, 1f);
        static readonly Color ColDark   = new Color(0.06f, 0.06f, 0.06f, 0.97f);

        // ──────────────────────────────────────────────────────────────────
        private void Start()
        {
            ClearLegacyChildren();
            BuildScoreLabel();
            BuildQuestList();
            BuildSettingsButton();
        }

        private void OnEnable()
        {
            QuestManager.OnProgressUpdated += OnProgressUpdated;
            QuestManager.OnGameEnded       += ShowResultPopup;
            QuestManager.OnQuestsChanged    += BuildQuestList;

            // Đảm bảo list luôn mới nhất khi mở panel
            BuildQuestList();
        }

        private void OnDisable()
        {
            QuestManager.OnProgressUpdated -= OnProgressUpdated;
            QuestManager.OnGameEnded       -= ShowResultPopup;
            QuestManager.OnQuestsChanged    -= BuildQuestList;
        }

        // ──────────────────────────────────────────────────────────────────
        #region Build

        private void ClearLegacyChildren()
        {
            // Xóa Text cũ
            var toRemove = new List<Transform>();
            foreach (Transform ch in transform)
                if (ch.name is "QuestText" or "Title" or "BtnStart")
                    toRemove.Add(ch);
            foreach (var c in toRemove) Destroy(c.gameObject);
        }

        private void BuildScoreLabel()
        {
            RectTransform rt = (RectTransform)transform;
            _scoreText = MakeLabel("ScoreLabel", rt, "🎯 Điểm: 0", 12,
                new Color(1f, 0.85f, 0.2f), TextAnchor.UpperCenter,
                new Vector4(4, -44, -4, -4));
            _scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _scoreText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void BuildQuestList()
        {
            if (questListContainer == null)
            {
                GameObject go = new GameObject("QuestList", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                questListContainer = (RectTransform)go.transform;
                questListContainer.anchorMin = new Vector2(0, 0);
                questListContainer.anchorMax = new Vector2(1, 1);
                questListContainer.offsetMin = new Vector2(4, 44);
                questListContainer.offsetMax = new Vector2(-4, -60); // Đẩy top xuống né ScoreLabel

                var vlg = go.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 8;
                vlg.childForceExpandWidth  = true;
                vlg.childForceExpandHeight = false;
                vlg.childControlHeight     = true;
                vlg.childControlWidth      = true;
                vlg.childAlignment = TextAnchor.LowerCenter; // Chuyển thành Bottom Center
            }
            else
            {
                // Xóa các card hiện tại trước khi rebuild (để tránh bị lặp khi chơi lại lần 2)
                foreach (Transform child in questListContainer)
                {
                    child.gameObject.name = "Destroying"; // Đổi tên để tránh nhầm lẫn nếu debug
                    Destroy(child.gameObject);
                }
                // Giải phóng ngay lập tức trong frame này để tránh GetChild đếm nhầm
                questListContainer.DetachChildren();
            }

            if (QuestManager.Instance == null) return;
            foreach (var q in QuestManager.Instance.activeQuests)
            {
                if (q == null) continue;
                GameObject cardGO = new GameObject($"Card_{q.questID}", typeof(RectTransform));
                cardGO.transform.SetParent(questListContainer, false);
                var le = cardGO.AddComponent<LayoutElement>();
                le.minHeight = 90; le.preferredHeight = 90;
                cardGO.AddComponent<QuestCardUI>().Setup(q);
            }
            
            // Cập nhật điểm ngay khi rebuild
            OnProgressUpdated();
        }

        private void BuildSettingsButton()
        {
            Transform bgDim = transform.parent; // Vào chung parent với BtnClose (Panel_BackgroundDim)

            // Tiêu diệt BtnClose cũ
            if (bgDim != null)
            {
                Transform oldClose = bgDim.Find("BtnClose");
                if (oldClose != null) Destroy(oldClose.gameObject);
            }

            GameObject btnGO = new GameObject("BtnSettings", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(bgDim != null ? bgDim : transform, false);
            
            RectTransform bRT = (RectTransform)btnGO.transform;
            bRT.anchorMin = new Vector2(1, 1); bRT.anchorMax = new Vector2(1, 1);
            bRT.pivot = new Vector2(0.5f, 0.5f); // Pivot của button cũ
            bRT.sizeDelta = new Vector2(200, 80);
            bRT.anchoredPosition = new Vector2(-150, -100); // Vị trí chính xác của BtnClose cũ

            btnGO.GetComponent<Image>().color = new Color(0.9f, 0.1f, 0.1f, 1f); // Đỏ như cũ
            btnGO.GetComponent<Button>().onClick.AddListener(OnSettingsButtonClicked);

            // Gắn icon cài đặt kết hợp chữ cho dễ hiểu
            Text t = MakeLabel("Label", bRT, "⚙ KẾT THÚC / CÀI ĐẶT", 14, Color.white,
                TextAnchor.MiddleCenter, new Vector4(0, 0, 0, 0), isStretch: true);
            t.fontStyle = FontStyle.Bold;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region End Game

        private void OnSettingsButtonClicked()
        {
            // Mở Popup Settings kiểu CC
            ShowPopup("⚙ Cài Đặt (Tetris)",
                ("Âm thanh: BẬT/TẮT", ColGray, (System.Action)null),
                ("KẾT THÚC VÁN (END GAME)", ColRed, OnEndGameConfirmed),
                ("Đóng", ColGreen, ClosePopup)
            );
        }

        private void OnEndGameConfirmed()
        {
            if (QuestManager.Instance == null) return;

            int done  = QuestManager.Instance.GetCompletedCount();
            int total = QuestManager.Instance.activeQuests.Count;
            int stars = QuestManager.Instance.GetStarRating();
            int gold  = QuestManager.Instance.GetFinalGoldReward();

            string starStr = new string('⭐', stars) + new string('☆', Mathf.Max(0, 3 - stars));

            string msg = done switch
            {
                0 => "Bạn chưa hoàn thành đơn hàng nào.\nKết thúc phiên sẽ không nhận được thưởng.\n\nVẫn muốn kết thúc?",
                _ when done == total =>
                    $"Xuất sắc! Hoàn thành TẤT CẢ {total}/{total} đơn hàng!\n\n{starStr} (3 sao)\nThưởng: +{gold} 🪙 Gold\n\nGiao hàng và kết thúc?",
                _ =>
                    $"Hoàn thành {done}/{total} đơn hàng.\n{starStr} ({stars} sao)\nThưởng: +{gold} 🪙 Gold\n\nCác đơn chưa xong sẽ không được thưởng.\nXác nhận kết thúc?"
            };

            ShowPopup(msg,
                ("Xác Nhận", ColGreen, () =>
                {
                    ClosePopup();
                    QuestManager.Instance.TriggerEndGame();
                    FarmPuzzle.Tetris.TetrisManager.Instance?.GameOver();
                }),
                ("Hủy", ColGray, ClosePopup)
            );
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Popups

        private void ShowResultPopup(int stars, int gold, int score)
        {
            string starStr = new string('⭐', stars) + new string('☆', Mathf.Max(0, 3 - stars));
            string msg = $"<size=20>{starStr}</size>\n\nĐiểm phá hàng: <b>{score}</b>\nGold nhận được: <color=#FFD700><b>+{gold} 🪙</b></color>";

            ShowPopup(msg,
                ("✅  Nhận Thưởng & Về Farm", ColGreen, () =>
                {
                    ClosePopup();
                    Debug.Log("[QuestPanel] Chuyển về Farm. Committing DB Session.");
                    if (DataManager.Instance != null) DataManager.Instance.CommitSessionInventory();
                    var tetrisHUD = GetComponentInParent<Canvas>();
                    if (tetrisHUD != null) tetrisHUD.gameObject.SetActive(false);
                    else if (FarmPuzzle.Tetris.TetrisManager.Instance != null)
                        FarmPuzzle.Tetris.TetrisManager.Instance.gameObject.SetActive(false);
                })
            );
        }

        private void ShowPopup(string message,
            params (string label, Color col, System.Action action)[] buttons)
        {
            ClosePopup();

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            RectTransform rootRT = rootCanvas.GetComponent<RectTransform>();

            // Overlay mờ
            _overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            _overlay.transform.SetParent(rootRT, false);
            RectTransform ovRT = (RectTransform)_overlay.transform;
            ovRT.anchorMin = Vector2.zero; ovRT.anchorMax = Vector2.one;
            ovRT.offsetMin = Vector2.zero; ovRT.offsetMax = Vector2.zero;
            _overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            _overlay.transform.SetAsLastSibling();

            // Box
            float boxH = 220 + buttons.Length * 48f;
            GameObject box = new GameObject("PopupBox", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(_overlay.transform, false);
            RectTransform boxRT = (RectTransform)box.transform;
            boxRT.sizeDelta = new Vector2(360, boxH);
            box.GetComponent<Image>().color = ColDark;

            // Message
            float btnArea = buttons.Length * 48f + 12f;
            MakeLabel("Msg", boxRT, message, 13, Color.white,
                TextAnchor.MiddleCenter, new Vector4(16, btnArea, -16, 0), isStretch: true);

            // Buttons
            for (int i = 0; i < buttons.Length; i++)
            {
                var (lbl, col, act) = buttons[i];
                float yOff = 8f + i * 48f;

                GameObject bGO = new GameObject($"Btn{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                bGO.transform.SetParent(box.transform, false);
                RectTransform bRT = (RectTransform)bGO.transform;
                bRT.anchorMin = new Vector2(0, 0); bRT.anchorMax = new Vector2(1, 0);
                bRT.pivot = new Vector2(0.5f, 0);
                bRT.offsetMin = new Vector2(16, yOff);
                bRT.offsetMax = new Vector2(-16, yOff + 40);
                bGO.GetComponent<Image>().color = col;
                var capturedAct = act;
                bGO.GetComponent<Button>().onClick.AddListener(() => capturedAct?.Invoke());
                MakeLabel("L", (RectTransform)bGO.transform, lbl, 13, Color.white,
                    TextAnchor.MiddleCenter, Vector4.zero, isStretch: true);
            }
        }

        private void ClosePopup()
        {
            if (_overlay != null) { Destroy(_overlay); _overlay = null; }
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Event Handlers

        private void OnProgressUpdated()
        {
            if (_scoreText != null && QuestManager.Instance != null)
                _scoreText.text = $"🎯 Điểm: {QuestManager.Instance.CurrentScore}";
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        #region Helpers

        static Text MakeLabel(string n, RectTransform parent, string txt, int size,
            Color col, TextAnchor align, Vector4 offsets, bool isStretch = false)
        {
            GameObject go = new GameObject(n, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;

            if (isStretch)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(offsets.x, offsets.y);
                rt.offsetMax = new Vector2(offsets.z, offsets.w);
            }
            else
            {
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.offsetMin = new Vector2(offsets.x, offsets.y);
                rt.offsetMax = new Vector2(offsets.z, offsets.w);
            }

            Text t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size; t.color = col; t.alignment = align;
            t.text = txt; t.supportRichText = true;
            return t;
        }

        #endregion
    }
}
