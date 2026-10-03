using LastSeenWearing.Core.Crowd;
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
        [Tooltip("Metres per walk cycle (two steps). Art data, not tuning: it must equal the clips' stride " +
                 "(LSW_WalkSystem.md §5) — a test checks it against the body's JSON. The cycle runs on distance (D-022).")]
        [SerializeField, Min(0.01f)] private float _strideLength = 1f;

        [Header("Gait (LSW_WalkSystem.md §3)")]
        [Tooltip("Relative odds of a walk with no trait, one trait and two traits. [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _noTraitOdds = 0.25f;
        [SerializeField, Min(0f)] private float _oneTraitOdds = 0.45f;
        [SerializeField, Min(0f)] private float _twoTraitOdds = 0.30f;
        [Tooltip("Chance a trait is strong rather than slight. [PROVISIONAL]")]
        [SerializeField, Range(0f, 1f)] private float _strongTraitChance = 0.4f;
        [Tooltip("Walking speed × this, per pace bucket: slow, mid, fast. [PROVISIONAL]")]
        [SerializeField, Min(0.1f)] private float _slowTempo = 0.85f;
        [SerializeField, Min(0.1f)] private float _midTempo = 1f;
        [SerializeField, Min(0.1f)] private float _fastTempo = 1.15f;

        [Header("Taken-over NPCs (D-005, D-019)")]
        [Tooltip("Pose updates per second for NPCs the server has taken over from their schedule.")]
        [SerializeField, Range(1f, 30f)] private float _takenOverSyncRate = 10f;
        [Tooltip("Metres a bumped NPC is shoved. [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _bumpDistance = 2f;
        [Tooltip("Seconds the shove takes.")]
        [SerializeField, Min(0.01f)] private float _bumpDuration = 0.4f;

        public int NpcCount => _npcCount;
        public int RouteLength => _routeLength;
        public float WaypointSpread => _waypointSpread;
        public float DwellMin => _dwellMin;
        public float DwellMax => _dwellMax;
        public float WalkSpeed => _walkSpeed;
        public float StrideLength => _strideLength;

        public GaitSettings Gait => new(_noTraitOdds, _oneTraitOdds, _twoTraitOdds, _strongTraitChance);

        public float TempoMultiplier(Tempo tempo) => tempo switch
        {
            Tempo.Slow => _slowTempo,
            Tempo.Fast => _fastTempo,
            _ => _midTempo,
        };
        public float TakenOverSyncRate => _takenOverSyncRate;
        public float BumpDistance => _bumpDistance;
        public float BumpDuration => _bumpDuration;

        private void OnValidate()
        {
            _dwellMax = Mathf.Max(_dwellMax, _dwellMin);
        }
    }
}
