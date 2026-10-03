using LastSeenWearing.Core.Randomness;

namespace LastSeenWearing.Core.Crowd
{
    /// <summary>
    /// An NPC's walk, from the crowd seed and its index (LSW_WalkSystem.md §3) — computed on every
    /// client, never sent. P1.04 draws only the base walk; P1.06 adds the traits. Its own stream,
    /// <c>Derive(Derive(seed, index), GaitStream)</c>, so adding gait draws never moves a route.
    /// </summary>
    public static class GaitPlanner
    {
        private const int GaitStream = 1;
        private const int BaseWalkCount = 4;

        public static BaseWalk BaseWalkFor(int crowdSeed, int npcIndex)
        {
            var random = new SeededRandom(SeededRandom.Derive(SeededRandom.Derive(crowdSeed, npcIndex), GaitStream));
            return (BaseWalk)random.Range(0, BaseWalkCount);
        }
    }
}
