using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// The Watcher's camera wall (docs/DATA.md §3, GDD §04.1). Rewind, zoom, pan, announcements and
    /// festival controls join with their tasks.
    /// </summary>
    [CreateAssetMenu(fileName = "WatcherConfig", menuName = "Last Seen Wearing/Config/Watcher")]
    public sealed class WatcherConfig : ScriptableObject
    {
        [Header("Monitors")]
        [Tooltip("Seconds a monitor shows static after it is switched to another camera. GDD §04.1: switching cameras costs time. [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _feedSwitchSeconds = 1.5f;

        public float FeedSwitchSeconds => _feedSwitchSeconds;
    }
}
