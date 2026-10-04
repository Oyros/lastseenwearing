using LastSeenWearing.Core.Round;
using LastSeenWearing.Gameplay.Capture;
using LastSeenWearing.Gameplay.Round;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace LastSeenWearing.UI.Round
{
    /// <summary>
    /// The round on everyone's screen (GDD §06, P1.10): phase, time left, which round of the case, the
    /// festival programme as it happens, and the end-of-round line. It renders <see cref="RoundDirector"/>;
    /// it decides nothing. Hidden in the lobby. P1 placeholder look, built in code — the results screen
    /// proper is P1.26.
    /// </summary>
    public sealed class RoundHud : MonoBehaviour
    {
        private const string UiTable = "UI";
        private const string FestivalTable = "Festival";

        // Placeholder layout, not tuning.
        private const float Width = 420f;
        private const float Margin = 10f;
        private const float FontSize = 20f;
        private const float BannerSeconds = 4f;

        [SerializeField] private RoundDirector _director;
        [Tooltip("The cuffs left this round (P1.24) — everyone sees them; an arrest is public.")]
        [SerializeField] private Arrests _arrests;

        private GameObject _panel;
        private TextMeshProUGUI _phase;
        private TextMeshProUGUI _clock;
        private TextMeshProUGUI _round;
        private TextMeshProUGUI _line;
        private TextMeshProUGUI _cuffs;
        private float _bannerUntil;
        private int _shownSeconds = -1;

        private void Awake()
        {
            BuildFrame();
        }

        private void OnEnable()
        {
            _director.PhaseChanged += OnPhaseChanged;
            _director.FestivalEventRaised += OnFestivalEvent;
            if (_arrests != null)
            {
                _arrests.WrongArrest += OnWrongArrest;
            }
        }

        private void OnDisable()
        {
            _director.PhaseChanged -= OnPhaseChanged;
            _director.FestivalEventRaised -= OnFestivalEvent;
            if (_arrests != null)
            {
                _arrests.WrongArrest -= OnWrongArrest;
            }
        }

        private void Update()
        {
            var visible = _director.IsSpawned && _director.Phase != RoundPhase.Lobby;
            _panel.SetActive(visible);
            if (!visible)
            {
                return;
            }

            var seconds = (int)System.Math.Ceiling(_director.PhaseRemaining);
            if (seconds == _shownSeconds && Time.time < _bannerUntil + 0.1f)
            {
                return;
            }

            _shownSeconds = seconds;
            _clock.text = $"{seconds / 60}:{seconds % 60:00}";
            _phase.text = Text(UiTable, PhaseKey(_director.Phase));
            _round.text = Text(UiTable, "ui.round.round_of", _director.Round + 1, _director.RoundsPerCase);
            var inPlay = _director.Phase is RoundPhase.Live or RoundPhase.LastCuff;
            _cuffs.gameObject.SetActive(inPlay && _arrests != null);
            if (inPlay && _arrests != null)
            {
                _cuffs.text = Text(UiTable, "capture.cuffs", _arrests.CuffsLeft, _arrests.CuffsPerRound);
            }

            switch (_director.Phase)
            {
                case RoundPhase.Result:
                    var next = _director.Round + 1 < _director.RoundsPerCase ? "ui.round.next_round_in" : "ui.round.lobby_in";
                    _line.text = OutcomeLine() + "\n" + Text(UiTable, next, seconds);
                    break;
                case RoundPhase.CaseEnd:
                    _line.text = Text(UiTable, "ui.round.lobby_in", seconds);
                    break;
                default:
                    if (Time.time >= _bannerUntil)
                    {
                        _line.text = string.Empty;
                    }

                    break;
            }
        }

        private string OutcomeLine()
        {
            return _director.Outcome switch
            {
                RoundOutcome.TimeUp => Text(UiTable, "ui.round.result.time_up"),
                RoundOutcome.Escaped => Text(UiTable, "ui.round.result.escaped"),
                RoundOutcome.Arrested => Text(UiTable, "ui.round.result.arrested"),
                RoundOutcome.OutOfCuffs => Text(UiTable, "ui.round.result.out_of_cuffs"),
                _ => string.Empty,
            };
        }

        private void OnPhaseChanged(RoundPhase phase)
        {
            _shownSeconds = -1;
            _bannerUntil = 0f;
        }

        private void OnFestivalEvent(FestivalEvent festivalEvent)
        {
            _line.text = Text(FestivalTable, $"festival.event.{festivalEvent.ToString().ToLowerInvariant()}");
            _bannerUntil = Time.time + BannerSeconds;
        }

        private void OnWrongArrest()
        {
            _line.text = Text(UiTable, "capture.wrong_arrest");
            _bannerUntil = Time.time + BannerSeconds;
            _shownSeconds = -1;
        }

        private static string PhaseKey(RoundPhase phase) => phase switch
        {
            RoundPhase.Briefing => "ui.round.phase.briefing",
            RoundPhase.Live => "ui.round.phase.live",
            RoundPhase.LastCuff => "ui.round.phase.last_cuff",
            RoundPhase.Result => "ui.round.phase.result",
            _ => "ui.round.phase.case_end",
        };

        private static string Text(string table, string key, params object[] arguments)
        {
            return new LocalizedString(table, key).GetLocalizedString(arguments);
        }

        private void BuildFrame()
        {
            var canvasObject = new GameObject("RoundCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _panel.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)_panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -Margin);
            rect.sizeDelta = new Vector2(Width, 0f);
            _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            var layout = _panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)Margin, (int)Margin, (int)Margin, (int)Margin);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;
            _panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _phase = NewText(FontStyles.Bold);
            _clock = NewText(FontStyles.Bold);
            _clock.fontSize = FontSize * 1.6f;
            _round = NewText(FontStyles.Normal);
            _cuffs = NewText(FontStyles.Normal);
            _line = NewText(FontStyles.Italic);
        }

        private TextMeshProUGUI NewText(FontStyles style)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_panel.transform, false);
            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = FontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            return label;
        }
    }
}
