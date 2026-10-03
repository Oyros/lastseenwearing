using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// The festival crowd (docs/DATA.md §3, GDD §04.1). P1.01 needs size, routes and pace; lookalikes,
    /// height bands and reaction radii join as their tasks land.
    /// </summary>
    [CreateAssetMenu(fileName = "CrowdConfig", menuName = "Last Seen Wearing/Config/Crowd")]
    public sealed class CrowdConfig : ScriptableObject
    {
        [Header("Size")]
        [Tooltip("NPCs per round. GDD: 100–150.")]
        [SerializeField, Range(0, 300)] private int _npcCount = 150;

        [Header("Routes")]
        [Tooltip("Legs per NPC before its route loops.")]
        [SerializeField, Min(1)] private int _routeLength = 32;
        [Tooltip("Metres around a waypoint an NPC may stand, so a waypoint is an area.")]
        [SerializeField, Min(0f)] private float _waypointSpread = 2.5f;
        [Tooltip("Seconds an NPC lingers at a waypoint. [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _dwellMin = 0f;
        [SerializeField, Min(0f)] private float _dwellMax = 6f;

        [Header("Pace")]
        [Tooltip("Metres per second. [PROVISIONAL] — the gait system (P1.04–P1.06) refines this.")]
        [SerializeField, Min(0f)] private float _walkSpeed = 1.3f;

        public int NpcCount => _npcCount;
        public int RouteLength => _routeLength;
        public float WaypointSpread => _waypointSpread;
        public float DwellMin => _dwellMin;
        public float DwellMax => _dwellMax;
        public float WalkSpeed => _walkSpeed;

        private void OnValidate()
        {
            _dwellMax = Mathf.Max(_dwellMax, _dwellMin);
        }
    }
}
