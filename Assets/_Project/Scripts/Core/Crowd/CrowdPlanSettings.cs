namespace LastSeenWearing.Core.Crowd
{
    /// <summary>The numbers <see cref="CrowdPlanner"/> needs, copied out of <c>CrowdConfig</c> and the layout.</summary>
    public readonly struct CrowdPlanSettings
    {
        public int NpcCount { get; }
        public int WaypointCount { get; }
        public int RouteLength { get; }
        public float Spread { get; }
        public float DwellMin { get; }
        public float DwellMax { get; }

        public CrowdPlanSettings(int npcCount, int waypointCount, int routeLength, float spread, float dwellMin, float dwellMax)
        {
            NpcCount = npcCount;
            WaypointCount = waypointCount;
            RouteLength = routeLength;
            Spread = spread;
            DwellMin = dwellMin;
            DwellMax = dwellMax;
        }
    }
}
