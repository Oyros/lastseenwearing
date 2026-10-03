using LastSeenWearing.Core.Roles;
using LastSeenWearing.Gameplay.Roles;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace LastSeenWearing.UI.Lobby
{
    /// <summary>
    /// The lobby's roles, for everyone (GDD §03, D-006, D-021): who holds which role, the role buttons
    /// in pick mode, and the host's mode and lock controls. It renders <see cref="RoleRosterSync"/> and
    /// sends requests; every rule is the roster's. P1 placeholder look — built in code, rebuilt on
    /// every change; the real lobby screen is later UI work.
    /// </summary>
    public sealed class RosterPanel : MonoBehaviour
    {
        private const string Table = "UI";
        private const string RoleTable = "Roles";

        // Placeholder layout, not tuning.
        private const float Width = 320f;
        private const float Margin = 10f;
        private const float FontSize = 18f;
        private const float ButtonHeight = 32f;

        [SerializeField] private RoleRosterSync _roster;

        private RectTransform _content;

        private void Awake()
        {
            BuildFrame();
        }

        private void OnEnable()
        {
            _roster.Changed += Refresh;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            Refresh();
        }

        private void OnDisable()
        {
            _roster.Changed -= Refresh;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(Locale locale) => Refresh();

        private void Refresh()
        {
            foreach (Transform child in _content)
            {
                Destroy(child.gameObject);
            }

            if (_roster == null || !_roster.IsSpawned)
            {
                return;
            }

            var network = NetworkManager.Singleton;
            var me = network.LocalClientId;
            var myRole = _roster.RoleOf(me);

            AddLabel(Text("ui.lobby.title"), FontStyles.Bold);
            AddLabel(Text(_roster.Mode == RoleSelectionMode.Pick ? "ui.lobby.mode.pick" : "ui.lobby.mode.random"), FontStyles.Italic);

            for (var i = 0; i < _roster.Count; i++)
            {
                var entry = _roster[i];
                var player = Text("ui.lobby.player", entry.ClientId + 1);
                if (entry.ClientId == me)
                {
                    player = Text("ui.lobby.player_you", player);
                }

                var role = entry.Role == Role.None ? Text("ui.lobby.no_role") : RoleName(entry.Role);
                AddLabel(Text("ui.lobby.row", player, role), FontStyles.Normal);
            }

            if (_roster.IsLocked)
            {
                AddLabel(Text("ui.lobby.locked"), FontStyles.Bold);
            }
            else if (_roster.Mode == RoleSelectionMode.Pick)
            {
                foreach (var role in RoleRules.RolesFor(_roster.Count))
                {
                    var claimable = role != myRole && !_roster.IsTaken(role);
                    var chosen = role;
                    AddButton(RoleName(role), claimable, () => _roster.Claim(chosen));
                }

                if (myRole != Role.None)
                {
                    AddButton(Text("ui.lobby.button.release"), true, _roster.Release);
                }
            }

            if (network.IsHost)
            {
                if (!_roster.IsLocked)
                {
                    var toRandom = _roster.Mode == RoleSelectionMode.Pick;
                    AddButton(Text(toRandom ? "ui.lobby.button.mode_random" : "ui.lobby.button.mode_pick"), true,
                        () => _roster.SetMode(toRandom ? RoleSelectionMode.Random : RoleSelectionMode.Pick));
                    AddButton(Text("ui.lobby.button.lock"), true, _roster.Lock);
                }
                else
                {
                    AddButton(Text("ui.lobby.button.unlock"), true, _roster.Unlock);
                }
            }
        }

        private static string Text(string key, params object[] arguments)
        {
            return new LocalizedString(Table, key).GetLocalizedString(arguments);
        }

        private static string RoleName(Role role)
        {
            return new LocalizedString(RoleTable, $"role.{role.ToString().ToLowerInvariant()}.name").GetLocalizedString();
        }

        private void BuildFrame()
        {
            var canvasObject = new GameObject("RosterCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panel.transform.SetParent(canvasObject.transform, false);
            _content = (RectTransform)panel.transform;
            _content.anchorMin = _content.anchorMax = _content.pivot = new Vector2(1f, 1f);
            _content.anchoredPosition = new Vector2(-Margin, -Margin);
            _content.sizeDelta = new Vector2(Width, 0f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)Margin, (int)Margin, (int)Margin, (int)Margin);
            layout.spacing = Margin / 2f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void AddLabel(string text, FontStyles style)
        {
            var label = NewText(_content, text);
            label.fontStyle = style;
        }

        private void AddButton(string text, bool interactable, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(_content, false);
            buttonObject.GetComponent<LayoutElement>().minHeight = ButtonHeight;
            var button = buttonObject.GetComponent<Button>();
            button.interactable = interactable;
            button.onClick.AddListener(onClick);

            var label = NewText(buttonObject.transform, text);
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.black;
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI NewText(Transform parent, string text)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = FontSize;
            label.color = Color.white;
            return label;
        }
    }
}
