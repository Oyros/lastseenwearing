using System;

namespace LastSeenWearing.Core.Crowd
{
    /// <summary>
    /// One trait of a gait signature with its bucketed, signed strength (see <see cref="WalkTrait"/> for the
    /// sign): ±0.5 slight or ±1 strong; arm swing is always ±1 (−stiff, +big).
    /// </summary>
    public readonly struct TraitStrength : IEquatable<TraitStrength>
    {
        public const float Slight = 0.5f;
        public const float Strong = 1f;

        public WalkTrait Trait { get; }
        public float Strength { get; }

        public TraitStrength(WalkTrait trait, float strength)
        {
            Trait = trait;
            Strength = strength;
        }

        public bool IsStrong => Math.Abs(Strength) >= Strong;

        public bool Equals(TraitStrength other) => Trait == other.Trait && Strength.Equals(other.Strength);

        public override bool Equals(object obj) => obj is TraitStrength other && Equals(other);

        public override int GetHashCode() => ((int)Trait * 397) ^ Strength.GetHashCode();

        /// <summary>Debug words — the way a Watcher would say it. Player-facing text goes through localization (P1.19).</summary>
        public string Describe()
        {
            var slight = !IsStrong;
            switch (Trait)
            {
                case WalkTrait.Limp:
                    return (slight ? "slight limp " : "limps ") + (Strength < 0f ? "left" : "right");
                case WalkTrait.Hunch:
                    return slight ? "slightly hunched" : "hunched";
                case WalkTrait.Sway:
                    return slight ? "sways a little" : "sways";
                case WalkTrait.Bounce:
                    return slight ? "a bit bouncy" : "bouncy";
                case WalkTrait.ArmSwing:
                    return Strength < 0f ? "stiff arms" : "swings arms";
                default:
                    return Trait.ToString();
            }
        }
    }
}
