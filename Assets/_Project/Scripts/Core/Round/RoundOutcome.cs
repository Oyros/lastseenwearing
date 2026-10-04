namespace LastSeenWearing.Core.Round
{
    /// <summary>How a round ended. P1.10 has only <see cref="TimeUp"/>; the rest arrive with their tasks.</summary>
    public enum RoundOutcome : byte
    {
        None = 0,

        /// <summary>The programme ran out with the fugitive still inside (D-028: the police win).</summary>
        TimeUp = 1,

        /// <summary>The fugitive walked out of an open exit (GDD §06, P1.23): the fugitive wins.</summary>
        Escaped = 2,

        /// <summary>The patrol cuffed the fugitive (GDD §06, P1.24): the police win.</summary>
        Arrested = 3,

        /// <summary>Every cuff went on someone else and the chase ran out (GDD §06, P1.24): the fugitive wins.</summary>
        OutOfCuffs = 4,
    }
}
