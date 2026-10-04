using LastSeenWearing.Core.Layouts;
using LastSeenWearing.Core.Objective;
using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// The fugitive's objective (docs/DATA.md §3, GDD §04.4, P1.22): how long each target job takes, how many open
    /// the way out and which exit the last one opens. Fake walk, NPC control and the camera panel join later.
    /// </summary>
    [CreateAssetMenu(fileName = "FugitiveConfig", menuName = "Last Seen Wearing/Config/Fugitive")]
    public sealed class FugitiveConfig : ScriptableObject
    {
        [Header("Targets (GDD §04.4)")]
        [Tooltip("Seconds standing at an open target (lift a wallet). [PROVISIONAL]")]
        [SerializeField, Min(0.1f)] private float _openSeconds = 2f;
        [Tooltip("Seconds at a fixed target (swap a poster). [PROVISIONAL]")]
        [SerializeField, Min(0.1f)] private float _fixedSeconds = 3f;
        [Tooltip("Seconds at a social target (talk to a vendor). [PROVISIONAL]")]
        [SerializeField, Min(0.1f)] private float _socialSeconds = 4f;
        [Tooltip("Seconds at a hidden target (pick a safe). [PROVISIONAL]")]
        [SerializeField, Min(0.1f)] private float _hiddenSeconds = 5f;
        [Tooltip("Targets done before an exit opens. GDD §04.4: three of five.")]
        [SerializeField, Min(1)] private int _targetsNeeded = 3;
        [Tooltip("Metres the fugitive may drift while working before the job is dropped. [PROVISIONAL]")]
        [SerializeField, Min(0.05f)] private float _workDrift = 0.5f;
        [Tooltip("Which exit the last target opens (team: the farthest, so the escape crosses the cameras).")]
        [SerializeField] private ExitRule _exitRule = ExitRule.Farthest;
        [Tooltip("Metres from an exit's spot that count as out (GDD §06). [PROVISIONAL]")]
        [SerializeField, Min(0.1f)] private float _exitRadius = 2f;

        public int TargetsNeeded => _targetsNeeded;
        public float WorkDrift => _workDrift;
        public ExitRule ExitRule => _exitRule;
        public float ExitRadius => _exitRadius;

        public float SecondsFor(TargetKind kind) => kind switch
        {
            TargetKind.Open => _openSeconds,
            TargetKind.Fixed => _fixedSeconds,
            TargetKind.Social => _socialSeconds,
            _ => _hiddenSeconds,
        };
    }
}
