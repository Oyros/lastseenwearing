namespace LastSeenWearing.Core.Round
{
    /// <summary>Who wins by which outcome (GDD §06).</summary>
    public static class RoundRules
    {
        public static RoundWinner WinnerOf(RoundOutcome outcome)
        {
            switch (outcome)
            {
                // The fugitive did not get out: the festival closes around them (D-028).
                case RoundOutcome.TimeUp:
                    return RoundWinner.Police;
                default:
                    return RoundWinner.None;
            }
        }
    }
}
