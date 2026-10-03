namespace LastSeenWearing.Core.Roles
{
    /// <summary>The five roles of a case (GDD §03). <see cref="None"/> is a player without one yet.</summary>
    public enum Role : byte
    {
        None = 0,
        Watcher = 1,
        Patrol = 2,
        Plainclothes = 3,
        Dog = 4,
        Fugitive = 5,
    }
}
