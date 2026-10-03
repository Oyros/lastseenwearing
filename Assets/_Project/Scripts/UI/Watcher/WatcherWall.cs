using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Core.Round;
using LastSeenWearing.Core.Watcher;
using LastSeenWearing.Gameplay.Cameras;
using LastSeenWearing.Gameplay.Player;
using LastSeenWearing.Gameplay.Roles;
using LastSeenWearing.Gameplay.Round;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace LastSeenWearing.UI.Watcher
{
    /// <summary>
    /// The Watcher's camera wall (GDD §03, §04.1, P1.13): two monitors and the camera list, on the Watcher's
    /// screen only, from Briefing on. A number key (or a click on the list) sends a camera to the left monitor;
    /// with SecondMonitor held, to the right one. Switching shows static for <see cref="WatcherConfig"/>'s
    /// switch time (<see cref="FeedSwitcher"/>). Only the cameras on a monitor render; the Watcher has no body,
    /// so the main camera is off while the wall is up. P1 placeholder look, built in code like <c>RoundHud</c>.
    /// </summary>
    public sealed class WatcherWall : MonoBehaviour
    {
        private const string UiTable = "UI";

        // Placeholder layout, not tuning.
        private const float Margin = 12f;
        private const float ListHeight = 56f;
        private const float FontSize = 20f;
        private const int StaticWidth = 96;
        private const int StaticHeight = 54;

        // SelectFeed's bindings: 1–8 on the keyboard, then the d-pad (docs/DATA.md §5).
        private const int KeyboardFeedBindings = 8;
        private static readonly Color Background = new(0.02f, 0.02f, 0.02f, 1f);
        private static readonly Color ButtonIdle = new(0.15f, 0.15f, 0.15f, 1f);
        private static readonly Color ButtonOnMonitor = new(0.45f, 0.45f, 0.45f, 1f);

        [SerializeField] private WatcherConfig _config;
        [SerializeField] private RoleRosterSync _roster;
        [SerializeField] private RoundDirector _director;
        [SerializeField] private Material _feedMaterial;
        [Tooltip("CAM 1, CAM 2, … in this order.")]
        [SerializeField] private CctvCamera[] _cameras;

        private FeedSwitcher _switcher;
        private LastSeenWearingControls _controls;
        private GameObject _wall;
        private readonly Monitor[] _monitors = new Monitor[FeedSwitcher.MonitorCount];
        private Image[] _buttons;
        private Texture2D _static;
        private Color32[] _staticPixels;
        private Camera _mainCamera;
        private bool _up;

        private void Awake()
        {
            _switcher = new FeedSwitcher(_config.FeedSwitchSeconds, _cameras.Length);
            _controls = new LastSeenWearingControls();
            _static = new Texture2D(StaticWidth, StaticHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            _staticPixels = new Color32[StaticWidth * StaticHeight];
            foreach (var feedCamera in _cameras)
            {
                feedCamera.GetComponent<Camera>().enabled = false; // nothing renders until a monitor shows it
            }

            BuildFrame();
            _wall.SetActive(false);
        }

        private void OnDestroy()
        {
            _controls.Dispose();
            Destroy(_static);
        }

        private void Update()
        {
            var shouldShow = IsWatcher() && _director.IsSpawned && _director.Phase != RoundPhase.Lobby;
            if (shouldShow != _up)
            {
                SetUp(shouldShow);
            }

            if (!_up)
            {
                return;
            }

            ReadInput();
            Refresh(Time.timeAsDouble);
        }

        private bool IsWatcher()
        {
            var network = NetworkManager.Singleton;
            return network != null && network.IsConnectedClient && _roster != null && _roster.IsSpawned
                   && _roster.RoleOf(network.LocalClientId) == Role.Watcher;
        }

        private void SetUp(bool up)
        {
            _up = up;
            _wall.SetActive(up);
            if (up)
            {
                _controls.Watcher.Enable();
                _mainCamera = Camera.main;
            }
            else
            {
                _controls.Watcher.Disable();
                foreach (var feedCamera in _cameras)
                {
                    feedCamera.GetComponent<Camera>().enabled = false;
                }
            }

            if (_mainCamera != null)
            {
                _mainCamera.enabled = !up;
            }
        }

        private void ReadInput()
        {
            var select = _controls.Watcher.SelectFeed;
            if (!select.WasPressedThisFrame() || select.activeControl == null)
            {
                return;
            }

            var binding = select.GetBindingIndexForControl(select.activeControl);
            var cameraIndex = binding < KeyboardFeedBindings ? binding : binding - KeyboardFeedBindings;
            Choose(cameraIndex);
        }

        private void Choose(int cameraIndex)
        {
            var monitor = _controls.Watcher.SecondMonitor.IsPressed() ? 1 : 0;
            _switcher.Select(monitor, cameraIndex, Time.timeAsDouble);
        }

        private void Refresh(double now)
        {
            var anySwitching = false;
            for (var m = 0; m < _monitors.Length; m++)
            {
                var showing = _switcher.Showing(m, now);
                var monitor = _monitors[m];
                var switching = _switcher.IsSwitching(m, now);
                anySwitching |= switching;
                monitor.Static.gameObject.SetActive(switching || showing == FeedSwitcher.NoCamera);
                monitor.Status.text = switching ? Text("ui.watcher.switching") : string.Empty;
                if (showing != FeedSwitcher.NoCamera && monitor.Feed.Showing != _cameras[showing])
                {
                    monitor.Feed.Show(_cameras[showing]); // the new camera's own filter (P1.14)
                }

                monitor.Label.text = _switcher.Target(m) == FeedSwitcher.NoCamera
                    ? string.Empty
                    : Text("ui.watcher.camera_n", _switcher.Target(m) + 1);
            }

            // Render only what a monitor shows.
            for (var c = 0; c < _cameras.Length; c++)
            {
                var onMonitor = _switcher.Showing(0, now) == c || _switcher.Showing(1, now) == c;
                _cameras[c].GetComponent<Camera>().enabled = onMonitor;
                var targeted = _switcher.Target(0) == c || _switcher.Target(1) == c;
                _buttons[c].color = targeted ? ButtonOnMonitor : ButtonIdle;
            }

            if (anySwitching)
            {
                Snow();
            }
        }

        private void Snow()
        {
            for (var i = 0; i < _staticPixels.Length; i++)
            {
                var grey = (byte)Random.Range(0, 256);
                _staticPixels[i] = new Color32(grey, grey, grey, 255);
            }

            _static.SetPixels32(_staticPixels);
            _static.Apply(false);
        }

        private static string Text(string key, params object[] arguments)
        {
            return new LocalizedString(UiTable, key).GetLocalizedString(arguments);
        }

        private void BuildFrame()
        {
            var canvasObject = new GameObject("WatcherCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -1; // under the round HUD
            _wall = canvasObject;

            var background = NewRect("Background", canvasObject.transform, Vector2.zero, Vector2.one);
            background.gameObject.AddComponent<Image>().color = Background;

            for (var m = 0; m < _monitors.Length; m++)
            {
                var left = m / (float)_monitors.Length;
                var right = (m + 1) / (float)_monitors.Length;
                // A half-screen column, and the 16:9 monitor fitted inside it (the fitter takes over its own anchors).
                var column = NewRect($"Column{m + 1}", canvasObject.transform, new Vector2(left, 0f), new Vector2(right, 1f));
                column.offsetMin = new Vector2(Margin, ListHeight + Margin * 2f);
                column.offsetMax = new Vector2(-Margin, -Margin);
                var frame = NewRect($"Monitor{m + 1}", column, Vector2.zero, Vector2.one);
                var fitter = frame.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = 16f / 9f;

                var feedObject = NewRect("Feed", frame, Vector2.zero, Vector2.one).gameObject;
                feedObject.AddComponent<RawImage>().raycastTarget = false;
                var feed = feedObject.AddComponent<CctvFeedView>();
                feed.FeedMaterial = _feedMaterial;

                var staticImage = NewRect("Static", frame, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
                staticImage.texture = _static;
                staticImage.raycastTarget = false;

                var label = NewText("Label", frame, TextAlignmentOptions.TopLeft);
                var status = NewText("Status", frame, TextAlignmentOptions.Center);
                _monitors[m] = new Monitor(feed, staticImage, label, status);
            }

            var list = NewRect("CameraList", canvasObject.transform, Vector2.zero, new Vector2(1f, 0f));
            list.pivot = new Vector2(0.5f, 0f);
            list.sizeDelta = new Vector2(-Margin * 2f, ListHeight);
            list.anchoredPosition = new Vector2(0f, Margin);
            var layout = list.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = Margin;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _buttons = new Image[_cameras.Length];
            for (var c = 0; c < _cameras.Length; c++)
            {
                var index = c;
                var buttonRect = NewRect($"Camera{c + 1}", list, Vector2.zero, Vector2.one);
                _buttons[c] = buttonRect.gameObject.AddComponent<Image>();
                buttonRect.gameObject.AddComponent<Button>().onClick.AddListener(() => Choose(index));
                var text = NewText("Text", buttonRect, TextAlignmentOptions.Center);
                text.text = Text("ui.watcher.camera_n", c + 1);
            }
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, TextAlignmentOptions alignment)
        {
            var rect = NewRect(name, parent, Vector2.zero, Vector2.one);
            rect.offsetMin = new Vector2(Margin, Margin);
            rect.offsetMax = new Vector2(-Margin, -Margin);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = FontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.fontStyle = FontStyles.UpperCase; // CCTV overlay style; capitals are never in the string
            text.raycastTarget = false;
            return text;
        }

        private readonly struct Monitor
        {
            public readonly CctvFeedView Feed;
            public readonly RawImage Static;
            public readonly TextMeshProUGUI Label;
            public readonly TextMeshProUGUI Status;

            public Monitor(CctvFeedView feed, RawImage staticImage, TextMeshProUGUI label, TextMeshProUGUI status)
            {
                Feed = feed;
                Static = staticImage;
                Label = label;
                Status = status;
            }
        }
    }
}
