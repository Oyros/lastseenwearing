using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// The Watcher's camera wall (docs/DATA.md §3, GDD §04.1). The zoom camera's pan and zoom (P1.17a). Rewind,
    /// announcements and festival controls join with their tasks.
    /// </summary>
    [CreateAssetMenu(fileName = "WatcherConfig", menuName = "Last Seen Wearing/Config/Watcher")]
    public sealed class WatcherConfig : ScriptableObject
    {
        [Header("Monitors")]
        [Tooltip("Seconds a monitor shows static after it is switched to another camera. GDD §04.1: switching cameras costs time. [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _feedSwitchSeconds = 1.5f;

        [Header("Zoom camera (P1.17a) — GDD §04.1: slow pan + zoom")]
        [Tooltip("Vertical field of view fully zoomed, degrees. The layout's 58.7° → 15° is about 4×. [PROVISIONAL]")]
        [SerializeField, Range(3f, 40f)] private float _zoomMinFieldOfView = 15f;
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

        public float FeedSwitchSeconds => _feedSwitchSeconds;
        public float ZoomMinFieldOfView => _zoomMinFieldOfView;
        public float ZoomOctavesPerSecond => _zoomOctavesPerSecond;
        public float ZoomOctavesPerNotch => _zoomOctavesPerNotch;
        public float PanYawRange => _panYawRange;
        public float PanPitchRange => _panPitchRange;
        public float PanDegreesPerSecond => _panDegreesPerSecond;
        public float PanDegreesPerPixel => _panDegreesPerPixel;
    }
}
