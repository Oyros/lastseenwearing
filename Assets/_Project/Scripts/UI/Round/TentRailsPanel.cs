using System.Linq;
using System.Text;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Core.Round;
using LastSeenWearing.Gameplay.Crowd;
using LastSeenWearing.Gameplay.Disguise;
using LastSeenWearing.Gameplay.Roles;
using LastSeenWearing.Gameplay.Round;
using LastSeenWearing.UI.Common;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LastSeenWearing.UI.Round
{
    /// <summary>
    /// The tents' rails, public at round start (GDD §05, P2.02): everyone reads them in the briefing; the Watcher keeps
    /// them in the dossier column through the round, to set against what the field reports. What is missing is not
    /// here — only the plainclothes learns that, inside. Each tent is named by the number on its door. P1-style
    /// placeholder look, built in code.
    /// </summary>
    public sealed class TentRailsPanel : MonoBehaviour
    {
        private const string UiTable = "UI";

        // Placeholder layout, not tuning.
        private const float Width = 460f;
        private const float Margin = 10f;
        private const float FontSize = 16f;

        [SerializeField] private RoundDirector _director;
        [SerializeField] private RoleRosterSync _roster;
        [SerializeField] private CrowdSpawner _crowd;

        private GameObject _panel;
        private TextMeshProUGUI _text;
        private ChangingTent[] _tents;
        private int _shownSeed;
        private bool _docked;

        private void Awake() => BuildFrame();

        private void Update()
        {
            var network = NetworkManager.Singleton;
            var phase = _director.IsSpawned ? _director.Phase : RoundPhase.Lobby;
            var watcher = network != null && _roster.RoleOf(network.LocalClientId) == Role.Watcher;
            var visible = _crowd.IsSpawned && (phase == RoundPhase.Briefing || (watcher && phase is RoundPhase.Live or RoundPhase.LastCuff));
            _panel.SetActive(visible);
            if (!visible)
            {
                return;
            }

            Dock(watcher);
            if (_shownSeed == _crowd.Seed && _tents != null)
            {
                return;
            }

            _shownSeed = _crowd.Seed;
            _tents = FindObjectsByType<ChangingTent>(FindObjectsSortMode.None).OrderBy(t => t.Index).ToArray();
            var text = new StringBuilder();
            text.AppendLine($"<b>{WardrobeWords.Text(UiTable, "tent.rails.title")}</b>");
            foreach (var tent in _tents)
            {
                text.AppendLine(WardrobeWords.Text(UiTable, "tent.rails.line", tent.Index + 1, WardrobeWords.Rail(tent.Rail, tent.Catalog)));
            }

            _text.text = text.ToString();
        }

        // The Watcher's copy sits at the bottom of the dossier column, beside the monitors; everyone else's at the left.
        private void Dock(bool besideTheWall)
        {
            if (_docked == besideTheWall && _tents != null)
            {
                return;
            }

            _docked = besideTheWall;
            var rect = (RectTransform)_panel.transform;
            if (besideTheWall)
            {
                rect.anchorMin = new Vector2(1f - Watcher.WatcherWall.DossierShare, 0f);
                rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-Margin, Margin);
                rect.sizeDelta = new Vector2(-Margin, 0f);
            }
            else
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(Margin, 0f);
                rect.sizeDelta = new Vector2(Width, 0f);
            }
        }

        private void BuildFrame()
        {
            var canvasObject = new GameObject("TentRailsCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5; // over the watcher wall

            _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _panel.transform.SetParent(canvasObject.transform, false);
            _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
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
