namespace LastSeenWearing.Core.Voice
{
    /// <summary>The two ways a voice travels (GDD §04.2): the Watcher's radio, and proximity (P1.16).</summary>
    public enum VoiceChannel : byte
    {
        Radio = 1,
        Proximity = 2,
    }
}
