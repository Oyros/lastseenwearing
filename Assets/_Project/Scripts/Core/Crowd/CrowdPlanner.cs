namespace LastSeenWearing.Core.Crowd
{
    /// <summary>
    /// Builds the whole crowd's plans from one seed (GDD §04.1, D-005). NPC <c>i</c> draws from its
    /// own stream <c>Derive(seed, i)</c>, so its plan depends only on the seed and its index — not on
    /// how many NPCs came before it. Pure C#: the same inputs give the same crowd on every client.
    /// </summary>
    public static class CrowdPlanner
    {
        public static NpcPlan[] Build(int seed, CrowdPlanSettings settings)
        {
            var plans = new NpcPlan[settings.NpcCount];
            for (var i = 0; i < plans.Length; i++)
            {
                plans[i] = BuildOne(SeededRandom.Derive(seed, i), settings);
            }

            return plans;
        }

        public static NpcPlan BuildOne(int npcSeed, CrowdPlanSettings settings)
        {
            var random = new SeededRandom(npcSeed);
            var spawn = random.Range(0, settings.WaypointCount);
            var spawnX = random.Range(-settings.Spread, settings.Spread);
            var spawnZ = random.Range(-settings.Spread, settings.Spread);

            var route = new NpcLeg[settings.RouteLength];
            var previous = spawn;
            for (var leg = 0; leg < route.Length; leg++)
            {
                var waypoint = NextWaypoint(random, previous, settings.WaypointCount);
                route[leg] = new NpcLeg(
                    waypoint,
                    random.Range(-settings.Spread, settings.Spread),
                    random.Range(-settings.Spread, settings.Spread),
                    random.Range(settings.DwellMin, settings.DwellMax));
                previous = waypoint;
            }

            return new NpcPlan(spawn, spawnX, spawnZ, route);
        }

        // Never the waypoint it is standing at: a leg always goes somewhere.
        private static int NextWaypoint(SeededRandom random, int previous, int waypointCount)
        {
            if (waypointCount < 2)
            {
                return 0;
            }

            var next = random.Range(0, waypointCount - 1);
            return next >= previous ? next + 1 : next;
        }
    }
}
