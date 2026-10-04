using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// How the crowd dresses (docs/DATA.md §3, GDD §05, P1.18): the odds behind every drawn outfit. What a
    /// garment is lives in the generated <c>WardrobeCatalog</c>; these are only the dice.
    /// </summary>
    [CreateAssetMenu(fileName = "WardrobeConfig", menuName = "Last Seen Wearing/Config/Wardrobe")]
    public sealed class WardrobeConfig : ScriptableObject
    {
        [Header("Body")]
        [Tooltip("Share of women in the crowd. [PROVISIONAL] 50/50 (team, P1.18).")]
        [SerializeField, Range(0f, 1f)] private float _femaleShare = 0.5f;
        [Tooltip("Relative odds of average, slim and heavy builds. Heavy is the build the camera reads (PL.16). [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _averageOdds = 0.6f;
        [SerializeField, Min(0f)] private float _slimOdds = 0.2f;
        [SerializeField, Min(0f)] private float _heavyOdds = 0.2f;

        [Header("Clothes")]
        [Tooltip("Chance of a hat — never with a hood, which covers the head (PL.16: 35 %). [PROVISIONAL]")]
        [SerializeField, Range(0f, 1f)] private float _hatOdds = 0.35f;
        [Tooltip("Chance a garment is the light of its colour pair rather than the dark. [PROVISIONAL]")]
        [SerializeField, Range(0f, 1f)] private float _lightShare = 0.5f;

        public float FemaleShare => _femaleShare;
        public float AverageOdds => _averageOdds;
        public float SlimOdds => _slimOdds;
        public float HeavyOdds => _heavyOdds;
        public float HatOdds => _hatOdds;
        public float LightShare => _lightShare;
    }
}
