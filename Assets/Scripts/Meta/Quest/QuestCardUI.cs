using UnityEngine;
using UnityEngine.UI;
using FarmPuzzle.Meta;
using FarmPuzzle.Core.Database;

namespace FarmPuzzle.UI
{
    /// <summary>
    /// UI card hiển thị 1 QuestDataSO. Tự tạo hierarchy bằng code.
    /// Được spawn bởi QuestPanelManager.
    /// </summary>
    public class QuestCardUI : MonoBehaviour
    {
        private Text  _titleText;
        private Text  _progressText;
        private Image _progressFill;
        private Text  _rewardText;
        private GameObject _completedBadge;
        private Image _cardBg;

        private QuestDataSO _questSO;

        static readonly Color ColBgDefault   = new Color(0.08f, 0.08f, 0.08f, 0.92f);
        static readonly Color ColBgCompleted = new Color(0.05f, 0.22f, 0.05f, 0.95f);
        static readonly Color ColBarBg       = new Color(0.15f, 0.15f, 0.15f, 1f);
        static readonly Color ColBarFill     = new Color(0.27f, 0.76f, 0.35f, 1f);
        static readonly Color ColBarDone     = new Color(0.18f, 0.95f, 0.42f, 1f);
        static readonly Color ColGold        = new Color(1f, 0.82f, 0.18f, 1f);

        // ──────────────────────────────────────────────────────────────────
        public void Setup(QuestDataSO questSO)
        {
            _questSO = questSO;
            CreateUI();
            Refresh();
        }

        private void OnEnable()  => QuestManager.OnProgressUpdated += Refresh;
        private void OnDisable() => QuestManager.OnProgressUpdated -= Refresh;

        // ──────────────────────────────────────────────────────────────────
        private void CreateUI()
        {
            // Card background
            _cardBg = gameObject.AddComponent<Image>();
            _cardBg.color = ColBgDefault;

            RectTransform rt = (RectTransform)transform;

            // Title
            _titleText = MakeText("Title", rt, 14, FontStyle.Bold, Color.white,
                new Vector2(10, -26), new Vector2(-10, -6));

            // Reward text
            _rewardText = MakeText("Reward", rt, 11, FontStyle.Normal, ColGold,
                new Vector2(10, -42), new Vector2(-10, -26));

            // Bar background
            RectTransform barBg = MakeRect("BarBg", rt, new Vector2(10, -66), new Vector2(-10, -50));
            barBg.gameObject.AddComponent<Image>().color = ColBarBg;

            // Bar fill
            GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(barBg, false);
            _progressFill = fillGO.GetComponent<Image>();
            _progressFill.color = ColBarFill;
            _progressFill.type  = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal;
            RectTransform fRT = (RectTransform)fillGO.transform;
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;

            // Progress text (over bar)
            _progressText = MakeText("ProgText", rt, 10, FontStyle.Normal,
                new Color(0.8f, 0.8f, 0.8f), new Vector2(-80, -66), new Vector2(-12, -50));
            _progressText.alignment = TextAnchor.MiddleRight;

            // Completed badge
            _completedBadge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            _completedBadge.transform.SetParent(rt, false);
            _completedBadge.GetComponent<Image>().color = new Color(0.15f, 0.7f, 0.3f);
            RectTransform bdRT = (RectTransform)_completedBadge.transform;
            bdRT.anchorMin = new Vector2(1, 1); bdRT.anchorMax = new Vector2(1, 1);
            bdRT.pivot = new Vector2(1, 1);
            bdRT.sizeDelta = new Vector2(68, 24);
            bdRT.anchoredPosition = new Vector2(-10, -10);
            Text bdTxt = MakeText("BadgeTxt", bdRT, 10, FontStyle.Bold, Color.white,
                Vector2.zero, Vector2.zero);
            bdTxt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bdTxt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bdTxt.alignment = TextAnchor.MiddleCenter;
            bdTxt.text = "✅ Đã Đủ";

            _completedBadge.SetActive(false);
        }

        // ──────────────────────────────────────────────────────────────────
        public void Refresh()
        {
            if (_questSO == null || _titleText == null) return;

            string name = GetProductName(_questSO.targetItemID);
            _titleText.text  = $"📦 {name}";
            _rewardText.text = $"Thưởng: {_questSO.rewardGold} 🪙 Gold";

            int cur    = QuestManager.Instance?.GetQuestProgress(_questSO.questID) ?? 0;
            int target = _questSO.targetAmount;
            bool done  = cur >= target;

            _progressText.text   = $"{cur}/{target}";
            _progressFill.fillAmount = target > 0 ? (float)cur / target : 0f;
            _progressFill.color  = done ? ColBarDone : ColBarFill;
            _cardBg.color        = done ? ColBgCompleted : ColBgDefault;
            _completedBadge.SetActive(done);
        }

        private string GetProductName(string id)
        {
            if (DataManager.Instance != null && DataManager.Instance.IsReady)
            {
                var p = DataManager.Instance.DB
                    .Table<ProductItemModel>()
                    .FirstOrDefault(x => x.ProductID == id);
                if (p != null) return p.Name;
            }
            return id;
        }

        // ──────────────────────────────────────────────────────────────────
        static Text MakeText(string n, RectTransform parent, int size,
            FontStyle style, Color col, Vector2 oMin, Vector2 oMax)
        {
            GameObject go = new GameObject(n, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.offsetMin = oMin; rt.offsetMax = oMax;
            Text t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size; t.fontStyle = style; t.color = col;
            t.alignment = TextAnchor.MiddleLeft;
            t.supportRichText = true;
            return t;
        }

        static RectTransform MakeRect(string n, RectTransform parent, Vector2 oMin, Vector2 oMax)
        {
            GameObject go = new GameObject(n, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.offsetMin = oMin; rt.offsetMax = oMax;
            return rt;
        }
    }
}
