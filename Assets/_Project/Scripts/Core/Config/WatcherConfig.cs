using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// The Watcher's camera wall (docs/DATA.md §3, GDD §04.1). Every camera's pan and zoom rates (P1.17a; how far a camera zooms is its CctvFilterProfile's). Rewind,
    /// announcements and festival controls join with their tasks.
    /// </summary>
    [CreateAssetMenu(fileName = "WatcherConfig", menuName = "Last Seen Wearing/Config/Watcher")]
    public sealed class WatcherConfig : ScriptableObject
    {
        [Header("Monitors")]
        [Tooltip("Seconds a monitor shows static after it is switched to another camera. GDD §04.1: switching cameras costs time. [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _feedSwitchSeconds = 1.5f;

        [Header("Pan and zoom, every camera (P1.17a, D-036) — GDD §04.1: slow pan + zoom; how far is the camera's profile")]
        [Tooltip("Doublings of magnification per second the camera can zoom. [PROVISIONAL]")]
        [SerializeField, Range(0.1f, 4f)] private float _zoomOctavesPerSecond = 1f;
        [Tooltip("Doublings one mouse-wheel notch asks for. [PROVISIONAL]")]
        [SerializeField, Range(0.05f, 1f)] private float _zoomOctavesPerNotch = 0.25f;
        [Tooltip("Degrees the camera can turn left or right of its mount. [PROVISIONAL]")]
        [SerializeField, Range(0f, 90f)] private float _panYawRange = 35f;
        [Tooltip("Degrees the camera can tilt up or down of its mount. [PROVISIONAL]")]
        [SerializeField, Range(0f, 45f)] private float _panPitchRange = 15f;
        [Tooltip("Degrees per second the camera turns, unzoomed (slower when zoomed in). [PROVISIONAL]")]
        [SerializeField, Range(1f, 120f)] private float _panDegreesPerSecond = 20f;
        [Tooltip("Degrees one pixel of right-drag asks for, unzoomed.")]
        [SerializeField, Range(0.01f, 1f)] private float _panDegreesPerPixel = 0.1f;

        [Header("Last seen (P1.20)")]
        [Tooltip("How old the witness's report already is when the round starts, seconds. [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _witnessReportAge = 20f;
        [Tooltip("Metres a mark on a feed reaches into the festival.")]
        [SerializeField, Min(1f)] private float _markRange = 80f;
        [Tooltip("Seconds the \"marked\" note stays on the monitor.")]
        [SerializeField, Min(0f)] private float _markNoteSeconds = 1.5f;

        public float FeedSwitchSeconds => _feedSwitchSeconds;
        public float WitnessReportAge => _witnessReportAge;
        public float MarkRange => _markRange;
        public float MarkNoteSeconds => _markNoteSeconds;
        public float ZoomOctavesPerSecond => _zoomOctavesPerSecond;
        public float ZoomOctavesPerNotch => _zoomOctavesPerNotch;
        public float PanYawRange => _panYawRange;
        public float PanPitchRange => _panPitchRange;
        public float PanDegreesPerSecond => _panDegreesPerSecond;
        public float PanDegreesPerPixel => _panDegreesPerPixel;
    }
}
