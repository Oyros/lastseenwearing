namespace LastSeenWearing.Core.Crowd
{
    /// <summary>A walk's pace bucket (LSW_WalkSystem.md §3 "stride"): it scales the walking speed; the step follows.</summary>
    public enum Tempo : byte
    {
        Slow = 0,
        Mid = 1,
        Fast = 2,
    }
}
