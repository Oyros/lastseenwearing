using System.Text;
using LastSeenWearing.Core.Round;
using LastSeenWearing.Gameplay.Round;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace LastSeenWearing.UI.Round
{
    /// <summary>
    /// The end of a round on every screen (GDD §06, P1.26): which side won and why, what the fugitive did unseen —
    /// targets, tents — the cuffs spent, the case's score and the wait for the next round. At the end of the case,
    /// the case's winner. It renders <see cref="RoundDirector"/>; it decides nothing. P1 placeholder look, built in
    /// code like <c>RoundHud</c>.
    /// </summary>
    public sealed class ResultsScreen : MonoBehaviour
    {
        private const string UiTable = "UI";

        // Placeholder layout, not tuning.
        private const float Width = 560f;
        private const float Margin = 16f;
        private const float FontSize = 20f;
        private const float TitleSize = 40f;
        private static readonly Color PoliceColour = new(0.55f, 0.75f, 1f, 1f);
        private static readonly Color FugitiveColour = new(1f, 0.75f, 0.35f, 1f);

        [SerializeField] private RoundDirector _director;

        private GameObject _panel;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
        private int _shownSeconds = -1;

        private void Awake() => BuildFrame();

        private void Update()
        {
            var phase = _director.IsSpawned ? _director.Phase : RoundPhase.Lobby;
            var visible = phase is RoundPhase.Result or RoundPhase.CaseEnd;
            _panel.SetActive(visible);
            if (!visible)
            {
                _shownSeconds = -1;
                return;
            }

            var seconds = (int)System.Math.Ceiling(_director.PhaseRemaining);
            if (seconds == _shownSeconds)
            {
                return;
            }

            _shownSeconds = seconds;
            if (phase == RoundPhase.Result)
            {
                ShowRound(seconds);
            }
            else
            {
                ShowCase(seconds);
            }
        }

        private void ShowRound(int seconds)
        {
            var winner = RoundOutcomes.Winner(_director.Outcome);
            SetTitle(winner, winner == Side.Police ? "results.police_win" : "results.fugitive_win");

            var summary = _director.LastSummary;
            var text = new StringBuilder();
            text.AppendLine(Text($"ui.round.result.{Key(_director.Outcome)}"));
            text.AppendLine();
            text.AppendLine(Text("results.targets", summary.TargetsDone, summary.TargetsNeeded));
            text.AppendLine(Text("results.tents", summary.TentsUsed));
            text.AppendLine(Text("results.cuffs", summary.CuffsSpent, summary.CuffsPerRound));
            if (summary.SecondsLeft >= 1f)
            {
                var left = (int)summary.SecondsLeft;
                text.AppendLine(Text("results.time_left", left / 60, left % 60));
            }

            text.AppendLine();
            text.AppendLine(Score());
            var last = _director.Round + 1 >= _director.RoundsPerCase;
            text.Append(Text(last ? "ui.round.lobby_in" : "ui.round.next_round_in", seconds));
            _body.text = text.ToString();
        }

        private void ShowCase(int seconds)
        {
            var leader = RoundOutcomes.Leader(_director.PoliceRounds, _director.FugitiveRounds);
            SetTitle(leader, leader switch
            {
                Side.Police => "results.case.police",
                Side.Fugitive => "results.case.fugitive",
                _ => "results.case.draw",
            });
            _body.text = Score() + "\n" + Text("ui.round.lobby_in", seconds);
        }

        private string Score() => Text("results.score", _director.PoliceRounds, _director.FugitiveRounds);

        private void SetTitle(Side side, string key)
        {
            _title.text = Text(key);
            _title.color = side switch
            {
                Side.Police => PoliceColour,
                Side.Fugitive => FugitiveColour,
                _ => Color.white,
            };
        }

        private static string Key(RoundOutcome outcome) => outcome switch
        {
            RoundOutcome.Arrested => "arrested",
            RoundOutcome.Escaped => "escaped",
            RoundOutcome.OutOfCuffs => "out_of_cuffs",
            _ => "time_up",
        };

        private static string Text(string key, params object[] arguments)
        {
            return new LocalizedString(UiTable, key).GetLocalizedString(arguments);
        }

        private void BuildFrame()
        {
            var canvasObject = new GameObject("ResultsCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20; // over every play HUD

            _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _panel.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)_panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Width, 0f);
            _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            var layout = _panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)Margin, (int)Margin, (int)Margin, (int)Margin);
            layout.spacing = Margin / 2f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            _panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _title = NewText(TitleSize, FontStyles.Bold);
            _body = NewText(FontSize, FontStyles.Normal);
            _panel.SetActive(false);
        }

        private TextMeshProUGUI NewText(float size, FontStyles style)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_panel.transform, false);
            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }
    }
}
