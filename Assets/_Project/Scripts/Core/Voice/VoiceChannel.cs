namespace LastSeenWearing.Core.Voice
{
    /// <summary>
    /// The ways a voice travels (GDD §04.2): the Watcher's radio, proximity between bodies, and the radio's
    /// leak — the radio overheard from an officer's belt (P1.16).
    /// </summary>
    public enum VoiceChannel : byte
    {
        Radio = 1,
        Proximity = 2,
        RadioLeak = 3,
    }
}
