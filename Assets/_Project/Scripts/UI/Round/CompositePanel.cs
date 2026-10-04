using System.Collections.Generic;
using System.Text;
using LastSeenWearing.Core.Composite;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Round;
using LastSeenWearing.Core.Wardrobe;
using LastSeenWearing.Core.Watcher;
using LastSeenWearing.Gameplay.Round;
using LastSeenWearing.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace LastSeenWearing.UI.Round
{
    /// <summary>
    /// The composite on the two screens that get one (GDD §04.1, P1.19): the Watcher reads the witness's claims
    /// and how sure the witness is; the fugitive reads the same and sees which claims are wrong and what is true.
    /// Only what this round has revealed (D-008). Under it the Watcher reads "last seen" (P1.20): the clothes, where
    /// the sighting came from (the witness, or their own mark) and its age, counting up. It renders
    /// <see cref="CompositeSync"/>; it decides nothing. Built in code like <c>RoundHud</c>.
    /// </summary>
    public sealed class CompositePanel : MonoBehaviour
    {
        private const string UiTable = "UI";
        private const string FestivalTable = "Festival";

        // Placeholder layout, not tuning.
        private const float Width = 420f;
        private const float Margin = 10f;
        private const float FontSize = 18f;

        [SerializeField] private CompositeSync _composite;
        [SerializeField] private RoundDirector _director;

        private GameObject _panel;
        private TextMeshProUGUI _text;
        private int _shownRound = -1;
        private int _shownSecond = -1;
        private bool _dirty = true;

        private void Awake()
        {
            BuildFrame();
            _panel.SetActive(false);
        }

        private void OnEnable() => _composite.Changed += MarkDirty;

        private void OnDisable() => _composite.Changed -= MarkDirty;

        private void MarkDirty() => _dirty = true;

        private void Update()
        {
            var phase = _director.IsSpawned ? _director.Phase : RoundPhase.Lobby;
            var visible = _composite.Sketch != null && phase is RoundPhase.Briefing or RoundPhase.Live or RoundPhase.LastCuff;
            _panel.SetActive(visible);
            var second = _composite.LastSeen is { } seen ? (int)seen.Age(_composite.NetworkManager.ServerTime.Time) : -1;
            if (!visible || (!_dirty && _shownRound == _director.Round && _shownSecond == second))
            {
                return;
            }

            _dirty = false;
            _shownRound = _director.Round;
            _shownSecond = second;
            Dock(!_composite.SeesErrors);
            var text = Compose(_composite.Sketch.Revealed(_director.Round, _composite.Config.Schedule), _composite.SeesErrors);
            if (!_composite.SeesErrors && _composite.LastSeen is { } sighting)
            {
                text += LastSeen(sighting, sighting.Age(_composite.NetworkManager.ServerTime.Time));
            }

            _text.text = text;
        }

        private string Compose(List<CompositeClaim> claims, bool seesErrors)
        {
            var text = new StringBuilder();
            text.AppendLine($"<b>{Text(UiTable, seesErrors ? "composite.title.fugitive" : "composite.title.watcher")}</b>");
            foreach (var claim in claims)
            {
                var line = Text(UiTable, "composite.line", Text(UiTable, $"composite.trait.{claim.Trait.ToString().ToLowerInvariant()}"),
                    Value(claim.Trait, claim.Value, claim.Walk), Text(UiTable, $"composite.confidence.{claim.Confidence.ToString().ToLowerInvariant()}"));
                text.Append(line);
                if (seesErrors)
                {
                    text.Append("  ").Append(claim.Wrong
                        ? Text(UiTable, "composite.wrong", Value(claim.Trait, claim.True, claim.TrueWalk))
                        : Text(UiTable, "composite.right"));
                }

                text.AppendLine();
            }

            return text.ToString();
        }

        // The Watcher's copy fills the dossier column beside the monitors; the fugitive's sits in a corner.
        private void Dock(bool besideTheWall)
        {
            var rect = (RectTransform)_panel.transform;
            if (besideTheWall)
            {
                rect.anchorMin = new Vector2(1f - Watcher.WatcherWall.DossierShare, 1f);
                rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
                rect.sizeDelta = new Vector2(-Margin, 0f);
            }
            else
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
                rect.sizeDelta = new Vector2(Width, 0f);
            }
        }

        private string LastSeen(Sighting sighting, double age)
        {
            var catalog = _composite.Catalog;
            var (minutes, seconds) = Sighting.Clock(age);
            var text = new StringBuilder();
            text.AppendLine();
            text.AppendLine($"<b>{Text(UiTable, "lastseen.title")}</b>");
            text.AppendLine(Text(UiTable, "lastseen.clothes", WardrobeWords.Garment(sighting.Top, catalog.Tops), WardrobeWords.Garment(sighting.Bottom, catalog.Bottoms),
                sighting.Hat.IsNone ? Text(UiTable, "lastseen.nohat") : WardrobeWords.Garment(sighting.Hat, catalog.Hats)));
            text.AppendLine(Text(UiTable, "lastseen.when", Text(UiTable, $"lastseen.source.{sighting.Source.ToString().ToLowerInvariant()}"), minutes, seconds));
            return text.ToString();
        }

        private string Value(CompositeTrait trait, int value, GaitSignature walk)
        {
            var catalog = _composite.Catalog;
            return trait switch
            {
                CompositeTrait.Sex => Text(FestivalTable, $"person.sex.{((Sex)value).ToString().ToLowerInvariant()}"),
                CompositeTrait.Height => Text(FestivalTable, $"person.height.{((Height)value).ToString().ToLowerInvariant()}"),
                CompositeTrait.Build => Text(FestivalTable, $"person.build.{((Build)value).ToString().ToLowerInvariant()}"),
                CompositeTrait.HairStyle => Text(FestivalTable, catalog.HairStyles[value].NameKey),
                CompositeTrait.HairColour => Text(FestivalTable, $"person.haircolour.{catalog.HairColours[value].Name.ToLowerInvariant()}"),
                CompositeTrait.Skin => Text(FestivalTable, $"person.skin.{catalog.Skins[value].Name.ToLowerInvariant()}"),
                CompositeTrait.Walk => Walk(walk),
                _ => string.Empty,
            };
        }

        private static string Walk(GaitSignature walk)
        {
            var parts = new List<string>
            {
                Text(FestivalTable, $"gait.base.{walk.Base.ToString().ToLowerInvariant()}"),
                Text(FestivalTable, $"gait.tempo.{walk.Tempo.ToString().ToLowerInvariant()}"),
            };
            foreach (var trait in walk.Traits)
            {
                parts.Add(Text(FestivalTable, TraitKey(trait)));
            }

            return string.Join(", ", parts);
        }

        /// <summary>The word for one walk trait: its kind, its side where it has one, and slight or strong.</summary>
        public static string TraitKey(TraitStrength trait)
        {
            var strength = trait.IsStrong ? "strong" : "slight";
            return trait.Trait switch
            {
                WalkTrait.Limp => $"gait.limp.{(trait.Strength < 0f ? "left" : "right")}.{strength}",
                WalkTrait.ArmSwing => trait.Strength < 0f ? "gait.armswing.stiff" : "gait.armswing.big",
                _ => $"gait.{trait.Trait.ToString().ToLowerInvariant()}.{strength}",
            };
        }

        private static string Text(string table, string key, params object[] arguments)
        {
            return new LocalizedString(table, key).GetLocalizedString(arguments);
        }

        private void BuildFrame()
        {
            var canvasObject = new GameObject("CompositeCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5; // over the watcher wall

            _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
            _panel.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)_panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-Margin, -Margin);
            rect.sizeDelta = new Vector2(Width, 0f);
            _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            _panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = _panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)Margin, (int)Margin, (int)Margin, (int)Margin);
            layout.childForceExpandHeight = false;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_panel.transform, false);
            _text = textObject.GetComponent<TextMeshProUGUI>();
            _text.fontSize = FontSize;
            _text.color = Color.white;
            _text.raycastTarget = false;
        }
    }
}
