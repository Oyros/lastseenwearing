using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Disguise;
using LastSeenWearing.Gameplay.Disguise;
using LastSeenWearing.Gameplay.Player;
using LastSeenWearing.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSeenWearing.UI.Field
{
    /// <summary>
    /// What the plainclothes sees inside a tent (GDD §03, P2.02): its rail, with what is gone marked — the clue the
    /// police are told to look for. Shows while they stand at the door; the server decides what is missing. P1-style
    /// placeholder look, built in code.
    /// </summary>
    public sealed class TentInspectPanel : MonoBehaviour
    {
        private const string UiTable = "UI";

        // Placeholder layout, not tuning.
        private const float Width = 460f;
        private const float Margin = 12f;
        private const float FontSize = 18f;

        [Tooltip("The door's reach: the panel closes when the plainclothes steps out of it.")]
        [SerializeField] private MovementConfig _movement;

        private GameObject _panel;
        private TextMeshProUGUI _text;
        private ChangingTent _tent;

        private void Awake() => BuildFrame();

        private void OnEnable() => ChangingTent.Inspected += Show;

        private void OnDisable()
        {
            ChangingTent.Inspected -= Show;
            _tent = null;
            _panel.SetActive(false);
        }

        private void Show(ChangingTent tent)
        {
            _tent = tent;
            var missing = TentRules.Missing(tent.Rail, tent.KnownTaken);
            var title = WardrobeWords.Text(UiTable, "tent.inspect.title", tent.Index + 1);
            var line = missing.Count == 0
                ? WardrobeWords.Text(UiTable, "tent.inspect.nothing_missing")
                : WardrobeWords.Text(UiTable, "tent.inspect.gone", missing.Count);
            _text.text = $"<b>{title}</b>\n{WardrobeWords.Rail(tent.Rail, tent.Catalog, tent.KnownTaken)}\n\n{line}";
            _panel.SetActive(true);
        }

        private void Update()
        {
            if (_tent == null)
            {
                return;
            }

            var officer = FieldOfficerController.Local;
            var reach = _movement.InteractRange;
            if (officer == null || !_tent.IsSpawned || (officer.transform.position - _tent.InteractionPoint).sqrMagnitude > reach * reach)
            {
                _tent = null;
                _panel.SetActive(false);
            }
        }

        private void BuildFrame()
        {
            var canvasObject = new GameObject("TentInspectCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _panel.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)_panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Width, 0f);
            _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
            var layout = _panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)Margin, (int)Margin, (int)Margin, (int)Margin);
            layout.childForceExpandHeight = false;
            _panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_panel.transform, false);
            _text = textObject.GetComponent<TextMeshProUGUI>();
            _text.fontSize = FontSize;
            _text.color = Color.white;
            _text.raycastTarget = false;
            _panel.SetActive(false);
        }
    }
}
