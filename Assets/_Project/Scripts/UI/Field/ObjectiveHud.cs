using System.Text;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Core.Round;
using LastSeenWearing.Gameplay.Objective;
using LastSeenWearing.Gameplay.Roles;
using LastSeenWearing.Gameplay.Round;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace LastSeenWearing.UI.Field
{
    /// <summary>
    /// The fugitive's targets (GDD §04.4, P1.22): how many are done of how many, the five jobs and which are done,
    /// the job in hand as a bar, and the exit once it is open; over each target still to do, a marker with its job
    /// and distance, held to the screen's edge when it is out of view. Only on the fugitive's screen — nobody else gets a
    /// meter. It renders <see cref="Objectives"/>; it decides nothing. P1 placeholder look, built in code.
    /// </summary>
    public sealed class ObjectiveHud : MonoBehaviour
    {
        private const string UiTable = "UI";

        // Placeholder layout, not tuning.
        private const float Width = 320f;
        private const float Margin = 10f;
        private const float FontSize = 18f;
        private const float BarHeight = 10f;
        private const float MarkerFontSize = 16f;
        private const float MarkerWidth = 200f;
        private const float EdgeMargin = 0.04f; // of the screen, so an edge marker stays readable
        private static readonly Color MarkerColour = new(1f, 0.85f, 0.2f, 1f);

        [SerializeField] private Objectives _objectives;
        [SerializeField] private RoleRosterSync _roster;
        [SerializeField] private RoundDirector _director;

        private GameObject _panel;
        private TextMeshProUGUI _text;
        private RectTransform _bar;
        private GameObject _barFrame;
        private RectTransform _canvas;
        private TextMeshProUGUI[] _markers = new TextMeshProUGUI[0];
        private bool _dirty = true;

        private void Awake() => BuildFrame();

        private void OnEnable() => _objectives.Changed += MarkDirty;

        private void OnDisable() => _objectives.Changed -= MarkDirty;

        private void MarkDirty() => _dirty = true;

        private void Update()
        {
            var network = NetworkManager.Singleton;
            var visible = network != null && _director.IsSpawned && _director.Phase is RoundPhase.Live or RoundPhase.LastCuff
                          && _roster.RoleOf(network.LocalClientId) == Role.Fugitive;
            _panel.SetActive(visible);
            PlaceMarkers(visible, network);
            if (!visible)
            {
                return;
            }

            var working = _objectives.Job >= 0;
            _barFrame.SetActive(working);
            if (working)
            {
                var now = network.ServerTime.Time;
                var length = _objectives.JobUntil - _objectives.JobFrom;
                var done = length > 0d ? Mathf.Clamp01((float)((now - _objectives.JobFrom) / length)) : 1f;
                _bar.anchorMax = new Vector2(done, 1f);
            }

            if (_dirty)
            {
                _dirty = false;
                _text.text = Compose();
            }
        }

        // A marker over each target still to do; behind the camera or off screen, it holds to the nearest edge.
        private void PlaceMarkers(bool visible, NetworkManager network)
        {
            var targets = _objectives.Targets;
            if (_markers.Length != targets.Length)
            {
                BuildMarkers(targets.Length);
            }

            var camera = Camera.main;
            var body = network != null && network.LocalClient?.PlayerObject != null ? network.LocalClient.PlayerObject.transform : null;
            var open = _objectives.DoneCount < _objectives.Needed;
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                var shown = visible && open && camera != null && body != null && target != null && !_objectives.IsDone(target.Index);
                _markers[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                var viewport = camera.WorldToViewportPoint(target.InteractionPoint + Vector3.up * 2f);
                if (viewport.z < 0f)
                {
                    viewport = new Vector3(1f - viewport.x, 0f, 0f); // behind: along the bottom, on the side it lies
                }

                viewport.x = Mathf.Clamp(viewport.x, EdgeMargin, 1f - EdgeMargin);
                viewport.y = Mathf.Clamp(viewport.y, EdgeMargin, 1f - EdgeMargin);
                var rect = _markers[i].rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(viewport.x, viewport.y);
                var distance = Mathf.RoundToInt(Vector3.Distance(body.position, target.InteractionPoint));
                _markers[i].text = Text("objective.marker", Text($"objective.kind.{target.Kind.ToString().ToLowerInvariant()}"), distance);
            }
        }

        private void BuildMarkers(int count)
        {
            foreach (var marker in _markers)
            {
                Destroy(marker.gameObject);
            }

            _markers = new TextMeshProUGUI[count];
            for (var i = 0; i < count; i++)
            {
                var markerObject = new GameObject("Marker", typeof(RectTransform), typeof(TextMeshProUGUI));
                markerObject.transform.SetParent(_canvas, false);
                var marker = markerObject.GetComponent<TextMeshProUGUI>();
                marker.fontSize = MarkerFontSize;
                marker.color = MarkerColour;
                marker.alignment = TextAlignmentOptions.Center;
                marker.fontStyle = FontStyles.Bold;
                marker.raycastTarget = false;
                marker.rectTransform.sizeDelta = new Vector2(MarkerWidth, MarkerFontSize * 3f);
                markerObject.SetActive(false);
                _markers[i] = marker;
            }
        }

        private string Compose()
        {
            var text = new StringBuilder();
            text.AppendLine($"<b>{Text("objective.count", _objectives.DoneCount, _objectives.Needed)}</b>");
            foreach (var target in _objectives.Targets)
            {
                var job = Text($"objective.kind.{target.Kind.ToString().ToLowerInvariant()}");
                text.AppendLine(_objectives.IsDone(target.Index) ? Text("objective.done", job) : job);
            }

            if (_objectives.OpenExitName is { } exit)
            {
                text.AppendLine();
                text.AppendLine($"<b>{Text("objective.exit_open", Text($"objective.exit.{exit}"))}</b>");
            }

            return text.ToString();
        }

        private static string Text(string key, params object[] arguments)
        {
            return new LocalizedString(UiTable, key).GetLocalizedString(arguments);
        }

        private void BuildFrame()
        {
            var canvasObject = new GameObject("ObjectiveCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4;
            _canvas = (RectTransform)canvasObject.transform;

            _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _panel.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)_panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f); // left middle, clear of the corners' panels
            rect.anchoredPosition = new Vector2(Margin, 0f);
            rect.sizeDelta = new Vector2(Width, 0f);
            _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            var layout = _panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)Margin, (int)Margin, (int)Margin, (int)Margin);
            layout.spacing = Margin / 2f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            _panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_panel.transform, false);
            _text = textObject.GetComponent<TextMeshProUGUI>();
            _text.fontSize = FontSize;
            _text.color = Color.white;
            _text.raycastTarget = false;

            _barFrame = new GameObject("Bar", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            _barFrame.transform.SetParent(_panel.transform, false);
            _barFrame.GetComponent<LayoutElement>().minHeight = BarHeight;
            _barFrame.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(_barFrame.transform, false);
            _bar = (RectTransform)fill.transform;
            _bar.anchorMin = Vector2.zero;
            _bar.anchorMax = new Vector2(0f, 1f);
            _bar.offsetMin = _bar.offsetMax = Vector2.zero;
            _barFrame.SetActive(false);
        }
    }
}
