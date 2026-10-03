namespace LastSeenWearing.Core.Crowd
{
    /// <summary>
    /// One NPC's day at the festival, derived from the round seed: where it appears and the
    /// waypoints it walks between, each with an offset so a waypoint is an area, not a point.
    /// Offsets are metres on the ground plane; the route loops.
    /// </summary>
    public sealed class NpcPlan
    {
        public int SpawnWaypoint { get; }
        public float SpawnOffsetX { get; }
        public float SpawnOffsetZ { get; }
        public NpcLeg[] Route { get; }

        public NpcPlan(int spawnWaypoint, float spawnOffsetX, float spawnOffsetZ, NpcLeg[] route)
        {
            SpawnWaypoint = spawnWaypoint;
            SpawnOffsetX = spawnOffsetX;
            SpawnOffsetZ = spawnOffsetZ;
            Route = route;
        }
    }
}
