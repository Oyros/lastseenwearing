namespace LastSeenWearing.Core.Round
{
    /// <summary>What can move a round on (<see cref="RoundCycle"/>).</summary>
    public enum RoundEvent : byte
    {
        /// <summary>The host starts the case (roles locked).</summary>
        StartCase,

        /// <summary>The briefing's time is up.</summary>
        BriefingOver,

        /// <summary>The round's programme ran out.</summary>
        TimeUp,

        /// <summary>A win or loss was reached in play (exit, arrest — P1.23, P1.24).</summary>
        Outcome,

        /// <summary>The last cuff was spent on a wrong arrest: identity revealed, chase on (GDD §06).</summary>
        CuffsSpent,

        /// <summary>The last-cuff chase ran out.</summary>
        ChaseOver,

        /// <summary>The result screen's time is up.</summary>
        ResultOver,

        /// <summary>The case-end screen's time is up.</summary>
        CaseEndOver,
    }
}
