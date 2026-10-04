using System;
using LastSeenWearing.Core.Composite;
using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// The witness sketch (docs/DATA.md §3, GDD §04.1, D-008, P1.19): how many traits are wrong, how sure the
    /// witness sounds, which traits each round of a case reveals, and the lookalikes planted in each round's crowd.
    /// </summary>
    [CreateAssetMenu(fileName = "CompositeConfig", menuName = "Last Seen Wearing/Config/Composite")]
    public sealed class CompositeConfig : ScriptableObject
    {
        [Serializable]
        public struct RoundReveal
        {
            public CompositeTrait[] Traits;
        }

        [Header("Errors (GDD §04.1: 1–2)")]
        [Tooltip("Chance the witness gets two traits wrong rather than one. [PROVISIONAL]")]
        [SerializeField, Range(0f, 1f)] private float _twoErrorChance = 0.5f;

        [Header("Witness confidence — relative odds")]
        [SerializeField, Min(0f)] private float _highOdds = 0.4f;
        [SerializeField, Min(0f)] private float _mediumOdds = 0.4f;
        [SerializeField, Min(0f)] private float _lowOdds = 0.2f;

        [Header("Reveal order (D-008): round 1, round 2, …; past the list, everything")]
        [SerializeField] private RoundReveal[] _reveals =
        {
            new() { Traits = new[] { CompositeTrait.Sex, CompositeTrait.Height, CompositeTrait.Build } },
            new() { Traits = new[] { CompositeTrait.HairStyle, CompositeTrait.HairColour } },
            new() { Traits = new[] { CompositeTrait.Walk } },
            new() { Traits = new[] { CompositeTrait.Skin } },
        };

        [Header("Lookalikes (GDD §04.1: 2–3 partial, on purpose)")]
        [SerializeField, Range(0, 10)] private int _lookalikesMin = 2;
        [SerializeField, Range(0, 10)] private int _lookalikesMax = 3;
        [Tooltip("A lookalike matches every revealed trait but this many (it is partial). [PROVISIONAL]")]
        [SerializeField, Range(0, 3)] private int _lookalikeMisses = 1;

        public float TwoErrorChance => _twoErrorChance;
        public float HighOdds => _highOdds;
        public float MediumOdds => _mediumOdds;
        public float LowOdds => _lowOdds;
        public int LookalikesMin => _lookalikesMin;
        public int LookalikesMax => _lookalikesMax;
        public int LookalikeMisses => _lookalikeMisses;

        public CompositeTrait[][] Schedule => Array.ConvertAll(_reveals, r => r.Traits);
    }
}
