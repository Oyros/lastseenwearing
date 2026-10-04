using System.Collections.Generic;
using LastSeenWearing.Core.Disguise;
using LastSeenWearing.Core.Wardrobe;
using LastSeenWearing.Gameplay.Disguise;
using LastSeenWearing.Gameplay.Player;
using LastSeenWearing.UI.Common;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace LastSeenWearing.UI.Field
{
    /// <summary>
    /// The fugitive's choice in a tent (GDD §05, P1.21): one row per slot — keep what you wear, or one of the rail's
    /// garments still there — then change or leave. It asks <see cref="ChangingTent"/>; the server decides, and a
    /// refusal is said here. While it is open the fugitive stands still. P1 placeholder look, built in code.
    /// </summary>
    public sealed class TentPanel : MonoBehaviour
    {
        private const string UiTable = "UI";
        private const string FestivalTable = "Festival";

        // Placeholder layout, not tuning.
        private const float Width = 420f;
        private const float Margin = 10f;
        private const float FontSize = 18f;
        private const float ButtonHeight = 32f;

        private static readonly ClothingSlot[] Slots = { ClothingSlot.Top, ClothingSlot.Bottom, ClothingSlot.Hat };

        private GameObject _panel;
        private RectTransform _content;
        private ChangingTent _tent;
        private FugitiveController _fugitive;
        private readonly int[] _choice = new int[Slots.Length];
        private string _note;
        private bool _sent;

        private void Awake()
        {
            BuildFrame();
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            ChangingTent.Opened += Open;
            ChangingTent.Refused += OnRefused;
        }

        private void OnDisable()
        {
            ChangingTent.Opened -= Open;
            ChangingTent.Refused -= OnRefused;
            Close();
        }

        private void Open(ChangingTent tent)
        {
            var player = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClient?.PlayerObject : null;
            if (player == null || !player.TryGetComponent(out _fugitive))
            {
                return;
            }

            _tent = tent;
            _fugitive.Paused = true;
            for (var i = 0; i < _choice.Length; i++)
            {
                _choice[i] = TentChange.Keep;
            }

            _note = null;
            _sent = false;
            _panel.SetActive(true);
            Rebuild();
        }

        private void Close()
        {
            if (_fugitive != null)
            {
                _fugitive.Paused = false;
            }

            _tent = null;
            _fugitive = null;
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void Update()
        {
            if (_tent == null)
            {
                return;
            }

            // In, or gone: the choice is over. A tent used meanwhile is the server's refusal to say.
            if (_fugitive == null || !_tent.IsSpawned || _fugitive.IsChanging)
            {
                Close();
            }
        }

        private void OnRefused(ChangeRefusal refusal)
        {
            if (_tent == null)
            {
                return;
            }

            _sent = false;
            _note = Text(UiTable, $"tent.refused.{refusal.ToString().ToLowerInvariant()}");
            Rebuild();
        }

        private void Rebuild()
        {
            for (var i = _content.childCount - 1; i >= 0; i--)
            {
                Destroy(_content.GetChild(i).gameObject);
            }

            AddLabel($"<b>{Text(UiTable, "tent.title")}</b>");
            for (var s = 0; s < Slots.Length; s++)
            {
                var slot = s;
                AddButton(Text(UiTable, "tent.row", Text(UiTable, $"tent.slot.{Slots[s].ToString().ToLowerInvariant()}"), ChoiceName(Slots[s], _choice[s])),
                    !_sent, () => Cycle(slot));
            }

            if (!string.IsNullOrEmpty(_note))
            {
                AddLabel(_note);
            }

            AddButton(Text(UiTable, "tent.button.change"), !_sent, Confirm);
            AddButton(Text(UiTable, "tent.button.leave"), true, Close);
        }

        // Keep, then each garment of this slot still on the rail, then keep again.
        private void Cycle(int slot)
        {
            var rail = _tent.Rail;
            var options = new List<int> { TentChange.Keep };
            for (var i = 0; i < rail.Length; i++)
            {
                if (rail[i].Slot == Slots[slot] && !_tent.IsTaken(i))
                {
                    options.Add(i);
                }
            }

            _choice[slot] = options[(options.IndexOf(_choice[slot]) + 1) % options.Count];
            _note = null;
            Rebuild();
        }

        private void Confirm()
        {
            _sent = true;
            _note = null;
            _tent.RequestChange(_choice[0], _choice[1], _choice[2]);
            Rebuild();
        }

        private string ChoiceName(ClothingSlot slot, int choice)
        {
            if (choice == TentChange.Keep)
            {
                var worn = TentStock.Of(_fugitive.Outfit, slot);
                return Text(UiTable, "tent.keep", worn.IsNone ? Text(UiTable, "lastseen.nohat") : Garment(worn, slot));
            }

            return Garment(_tent.Rail[choice].Garment, slot);
        }

        private string Garment(Worn worn, ClothingSlot slot) => WardrobeWords.Garment(worn, slot, _tent.Catalog);

        private static string Text(string table, string key, params object[] arguments)
        {
            return new LocalizedString(table, key).GetLocalizedString(arguments);
        }

        private void BuildFrame()
        {
            var canvasObject = new GameObject("TentCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // over the HUD and the composite

            _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _panel.transform.SetParent(canvasObject.transform, false);
            _content = (RectTransform)_panel.transform;
            _content.anchorMin = _content.anchorMax = _content.pivot = new Vector2(0.5f, 0.5f);
            _content.sizeDelta = new Vector2(Width, 0f);
            _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

            var layout = _panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)Margin, (int)Margin, (int)Margin, (int)Margin);
            layout.spacing = Margin / 2f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            _panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void AddLabel(string text)
        {
            NewText(_content, text);
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
            label.raycastTarget = false;
            return label;
        }
    }
}
