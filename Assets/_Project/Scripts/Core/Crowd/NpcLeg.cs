namespace LastSeenWearing.Core.Crowd
{
    /// <summary>One leg of an <see cref="NpcPlan"/>: walk to a waypoint (plus offset), then linger.</summary>
    public readonly struct NpcLeg
    {
        public int Waypoint { get; }
        public float OffsetX { get; }
        public float OffsetZ { get; }
        public float DwellSeconds { get; }

        public NpcLeg(int waypoint, float offsetX, float offsetZ, float dwellSeconds)
        {
            Waypoint = waypoint;
            OffsetX = offsetX;
            OffsetZ = offsetZ;
            DwellSeconds = dwellSeconds;
        }
    }
}
