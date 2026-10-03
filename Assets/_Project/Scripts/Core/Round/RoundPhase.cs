namespace LastSeenWearing.Core.Round
{
    /// <summary>Where a case is (ARCHITECTURE "Round state machine", GDD §06). Only <c>RoundDirector</c> moves it.</summary>
    public enum RoundPhase : byte
    {
        Lobby = 0,
        Briefing = 1,
        Live = 2,
        LastCuff = 3,
        Result = 4,
        CaseEnd = 5,
    }
}
