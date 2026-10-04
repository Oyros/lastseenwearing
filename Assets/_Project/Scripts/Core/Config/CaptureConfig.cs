using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// Arrests (docs/DATA.md §3, GDD §04.3, P1.24): cuffs per round, how close an arrest is made and what a wrong one
    /// gives the fugitive. Heat, stops and complaints join in P2.05–P2.06.
    /// </summary>
    [CreateAssetMenu(fileName = "CaptureConfig", menuName = "Last Seen Wearing/Config/Capture")]
    public sealed class CaptureConfig : ScriptableObject
    {
        [Header("Cuffs (GDD §04.3)")]
        [Tooltip("Cuffs per round. GDD §04.3: three.")]
        [SerializeField, Min(1)] private int _cuffsPerRound = 3;
        [Tooltip("Metres from the patrol to the one arrested. [PROVISIONAL]")]
        [SerializeField, Min(0.5f)] private float _arrestRange = 2.5f;
        [Tooltip("Extra metres the server allows for where a moving target was on the patrol's screen. [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _arrestRangeSlack = 1f;
        [Tooltip("Seconds a wrong arrest adds to the round — the fugitive's time bonus (GDD §04.3). [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _wrongArrestBonusSeconds = 30f;

        public int CuffsPerRound => _cuffsPerRound;
        public float ArrestRange => _arrestRange;
        public float ArrestRangeSlack => _arrestRangeSlack;
        public float WrongArrestBonusSeconds => _wrongArrestBonusSeconds;
    }
}
