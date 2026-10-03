namespace LastSeenWearing.Core.Roles
{
    /// <summary>
    /// How a lobby hands out roles — a lobby setting the host chooses (D-021).
    /// </summary>
    public enum RoleSelectionMode : byte
    {
        /// <summary>Players claim roles; whoever has none when roles lock gets a random free one.</summary>
        Pick = 0,

        /// <summary>No claims; every role is drawn at random when roles lock.</summary>
        Random = 1,
    }
}
