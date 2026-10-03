namespace LastSeenWearing.Core.Round
{
    /// <summary>
    /// The round's one transition table (ARCHITECTURE "Round state machine", GDD §06):
    /// <code>
    /// Lobby -StartCase-> Briefing -BriefingOver-> Live -TimeUp|Outcome-> Result
    ///                                              Live -CuffsSpent-> LastCuff -ChaseOver|Outcome-> Result
    /// Result -ResultOver-> Briefing (next round) | CaseEnd (last round) -CaseEndOver-> Lobby
    /// </code>
    /// An event the current phase does not take changes nothing.
    /// </summary>
    public static class RoundCycle
    {
        /// <param name="round">The round just played, 0-based.</param>
        public static RoundPhase Next(RoundPhase phase, RoundEvent roundEvent, int round, int roundsPerCase)
        {
            switch (phase)
            {
                case RoundPhase.Lobby when roundEvent == RoundEvent.StartCase:
                    return RoundPhase.Briefing;
                case RoundPhase.Briefing when roundEvent == RoundEvent.BriefingOver:
                    return RoundPhase.Live;
                case RoundPhase.Live when roundEvent == RoundEvent.TimeUp || roundEvent == RoundEvent.Outcome:
                    return RoundPhase.Result;
                case RoundPhase.Live when roundEvent == RoundEvent.CuffsSpent:
                    return RoundPhase.LastCuff;
                case RoundPhase.LastCuff when roundEvent == RoundEvent.ChaseOver || roundEvent == RoundEvent.Outcome:
                    return RoundPhase.Result;
                case RoundPhase.Result when roundEvent == RoundEvent.ResultOver:
                    return round + 1 < roundsPerCase ? RoundPhase.Briefing : RoundPhase.CaseEnd;
                case RoundPhase.CaseEnd when roundEvent == RoundEvent.CaseEndOver:
                    return RoundPhase.Lobby;
                default:
                    return phase;
            }
        }
    }
}
