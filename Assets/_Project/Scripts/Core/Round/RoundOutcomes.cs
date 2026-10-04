namespace LastSeenWearing.Core.Round
{
    /// <summary>Who a round or a case went to.</summary>
    public enum Side : byte
    {
        None,
        Police,
        Fugitive,
    }

    /// <summary>The round's verdict (GDD §06, P1.26): which side each ending goes to.</summary>
    public static class RoundOutcomes
    {
        public static Side Winner(RoundOutcome outcome) => outcome switch
        {
            RoundOutcome.Arrested => Side.Police,
            RoundOutcome.TimeUp => Side.Police, // the fugitive was still inside when the festival closed (D-028)
            RoundOutcome.Escaped => Side.Fugitive,
            RoundOutcome.OutOfCuffs => Side.Fugitive,
            _ => Side.None,
        };

        /// <summary>The case so far: the side with more rounds, or none on a tie.</summary>
        public static Side Leader(int policeRounds, int fugitiveRounds) =>
            policeRounds > fugitiveRounds ? Side.Police : fugitiveRounds > policeRounds ? Side.Fugitive : Side.None;
    }

    /// <summary>
    /// What happened in a round, told to everyone when it ends (P1.26) — some of it was the fugitive's secret while it
    /// was played (targets done, tents used).
    /// </summary>
    public struct RoundSummary
    {
        public byte TargetsDone;
        public byte TargetsNeeded;
        public byte CuffsLeft;
        public byte CuffsPerRound;
        public byte TentsUsed;

        /// <summary>Seconds of the round's play left when it ended; 0 when it ran out or ended in the chase.</summary>
        public float SecondsLeft;

        public int CuffsSpent => CuffsPerRound - CuffsLeft;
    }
}
