namespace LastSeenWearing.Core.Crowd
{
    /// <summary>The odds a gait signature is drawn with, copied out of <c>CrowdConfig</c>.</summary>
    public readonly struct GaitSettings
    {
        /// <summary>Relative odds of 0, 1 and 2 traits.</summary>
        public float NoTrait { get; }
        public float OneTrait { get; }
        public float TwoTraits { get; }

        /// <summary>Chance a trait is strong rather than slight (arm swing has no slight bucket).</summary>
        public float StrongChance { get; }

        public GaitSettings(float noTrait, float oneTrait, float twoTraits, float strongChance)
        {
            NoTrait = noTrait;
            OneTrait = oneTrait;
            TwoTraits = twoTraits;
            StrongChance = strongChance;
        }
    }
}
