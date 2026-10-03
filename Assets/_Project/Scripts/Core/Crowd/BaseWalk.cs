namespace LastSeenWearing.Core.Crowd
{
    /// <summary>
    /// The base walk clips (LSW_WalkSystem.md §2). The order is the crowd animator's blend-tree
    /// threshold, and the name after <c>Walk_</c> is the clip's name — both part of the art contract.
    /// </summary>
    public enum BaseWalk : byte
    {
        Normal = 0,
        Brisk = 1,
        Stroll = 2,
        Heavy = 3,
    }
}
