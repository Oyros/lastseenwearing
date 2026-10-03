using LastSeenWearing.Core.Round;
using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// A round's clock and programme (docs/DATA.md §3, GDD §06). Seconds throughout. Sunset and the
    /// fireworks flare join with their tasks.
    /// </summary>
    [CreateAssetMenu(fileName = "RoundConfig", menuName = "Last Seen Wearing/Config/Round")]
    public sealed class RoundConfig : ScriptableObject
    {
        [Header("Case")]
        [Tooltip("Rounds in a case. [PROVISIONAL] GDD §10 leaves the default open; a lobby setting later (D-028).")]
        [SerializeField, Range(1, 8)] private int _roundsPerCase = 3;

        [Header("Phases")]
        [Tooltip("Live length. GDD: 5–7 min. [PROVISIONAL] (D-028)")]
        [SerializeField, Min(10f)] private float _roundSeconds = 360f;
        [Tooltip("Roles and composite on screen before Live.")]
        [SerializeField, Min(0f)] private float _briefingSeconds = 10f;
        [SerializeField, Min(0f)] private float _resultSeconds = 10f;
        [SerializeField, Min(0f)] private float _caseEndSeconds = 10f;
        [Tooltip("The last-cuff chase (GDD §06).")]
        [SerializeField, Min(0f)] private float _lastCuffChaseSeconds = 45f;

        [Header("Programme (from Live's start)")]
        [SerializeField, Min(0f)] private float _concertAt = 120f;
        [SerializeField, Min(0f)] private float _fireworksAt = 240f;
        [SerializeField, Min(0f)] private float _closingAt = 300f;

        public int RoundsPerCase => _roundsPerCase;
        public float RoundSeconds => _roundSeconds;
        public float BriefingSeconds => _briefingSeconds;
        public float ResultSeconds => _resultSeconds;
        public float CaseEndSeconds => _caseEndSeconds;
        public float LastCuffChaseSeconds => _lastCuffChaseSeconds;

        public Programme Programme => new(_concertAt, _fireworksAt, _closingAt);

        /// <summary>How long <paramref name="phase"/> lasts; 0 for one that waits on someone (Lobby).</summary>
        public float SecondsOf(RoundPhase phase) => phase switch
        {
            RoundPhase.Briefing => _briefingSeconds,
            RoundPhase.Live => _roundSeconds,
            RoundPhase.LastCuff => _lastCuffChaseSeconds,
            RoundPhase.Result => _resultSeconds,
            RoundPhase.CaseEnd => _caseEndSeconds,
            _ => 0f,
        };

        private void OnValidate()
        {
            _fireworksAt = Mathf.Max(_fireworksAt, _concertAt);
            _closingAt = Mathf.Max(_closingAt, _fireworksAt);
        }
    }
}
