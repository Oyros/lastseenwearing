namespace LastSeenWearing.Gameplay.Network
{
    /// <summary>
    /// Which transport a session runs on. Steam is the game (D-002); Local is Unity Transport on
    /// this machine, for the editor and Multiplayer Play Mode, where one Steam account cannot
    /// connect to itself (D-016).
    /// </summary>
    public enum SessionTransport
    {
        Local,
        Steam,
    }
}
