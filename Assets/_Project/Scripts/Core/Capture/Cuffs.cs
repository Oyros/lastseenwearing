namespace LastSeenWearing.Core.Capture
{
    /// <summary>What an arrest came to.</summary>
    public enum ArrestResult : byte
    {
        /// <summary>No cuff left, or not now.</summary>
        Refused,

        /// <summary>The fugitive: the police win (GDD §06).</summary>
        Caught,

        /// <summary>Someone else: a cuff is gone, the fugitive gains time (GDD §04.3).</summary>
        Wrong,

        /// <summary>Someone else, with the last cuff: the last-cuff chase begins (GDD §06).</summary>
        LastCuffSpent,
    }

    /// <summary>The patrol's cuffs in one round (GDD §04.3, P1.24). Pure state — the server holds it.</summary>
    public sealed class Cuffs
    {
        public Cuffs(int perRound)
        {
            Left = perRound;
        }

        public int Left { get; private set; }

        public ArrestResult Arrest(bool isFugitive)
        {
            if (Left <= 0)
            {
                return ArrestResult.Refused;
            }

            if (isFugitive)
            {
                return ArrestResult.Caught;
            }

            Left--;
            return Left == 0 ? ArrestResult.LastCuffSpent : ArrestResult.Wrong;
        }
    }
}
