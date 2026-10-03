using System.Linq;
using LastSeenWearing.Core.Crowd;
using NUnit.Framework;

namespace LastSeenWearing.Tests.Crowd
{
    /// <summary>
    /// P1.01: the crowd is a function of the seed (GDD §04.1, D-005). Same seed, same crowd;
    /// another seed, another crowd; every plan stays inside the layout it was built for.
    /// </summary>
    public sealed class CrowdPlannerTests
    {
        private static readonly CrowdPlanSettings Settings = new(150, 16, 32, 2.5f, 0f, 6f);

        [Test]
        public void TheSameSeedBuildsTheSameCrowd()
        {
            var a = CrowdPlanner.Build(1234, Settings);
            var b = CrowdPlanner.Build(1234, Settings);
            Assert.That(Describe(a), Is.EqualTo(Describe(b)));
        }

        [Test]
        public void ADifferentSeedBuildsADifferentCrowd()
        {
            Assert.That(Describe(CrowdPlanner.Build(1234, Settings)), Is.Not.EqualTo(Describe(CrowdPlanner.Build(1235, Settings))));
        }

        [Test]
        public void AnNpcsPlanDoesNotDependOnTheCrowdSize()
        {
            var small = CrowdPlanner.Build(77, new CrowdPlanSettings(10, 16, 32, 2.5f, 0f, 6f));
            var large = CrowdPlanner.Build(77, Settings);
            Assert.That(Describe(small), Is.EqualTo(Describe(large.Take(10).ToArray())));
        }

        [Test]
        public void EveryPlanStaysInsideTheLayout()
        {
            foreach (var plan in CrowdPlanner.Build(99, Settings))
            {
                Assert.That(plan.SpawnWaypoint, Is.InRange(0, Settings.WaypointCount - 1));
                Assert.That(plan.Route, Has.Length.EqualTo(Settings.RouteLength));
                var previous = plan.SpawnWaypoint;
                foreach (var leg in plan.Route)
                {
                    Assert.That(leg.Waypoint, Is.InRange(0, Settings.WaypointCount - 1));
                    Assert.That(leg.Waypoint, Is.Not.EqualTo(previous), "a leg must go somewhere");
                    Assert.That(leg.OffsetX, Is.InRange(-Settings.Spread, Settings.Spread));
                    Assert.That(leg.OffsetZ, Is.InRange(-Settings.Spread, Settings.Spread));
                    Assert.That(leg.DwellSeconds, Is.InRange(Settings.DwellMin, Settings.DwellMax));
                    previous = leg.Waypoint;
                }
            }
        }

        [Test]
        public void TheCrowdUsesEveryWaypoint()
        {
            var used = CrowdPlanner.Build(5, Settings).SelectMany(p => p.Route).Select(l => l.Waypoint).Distinct().Count();
            Assert.That(used, Is.EqualTo(Settings.WaypointCount));
        }

        private static string Describe(NpcPlan[] plans)
        {
            return string.Join(";", plans.Select(p =>
                $"{p.SpawnWaypoint},{p.SpawnOffsetX:R},{p.SpawnOffsetZ:R}:" +
                string.Join("|", p.Route.Select(l => $"{l.Waypoint},{l.OffsetX:R},{l.OffsetZ:R},{l.DwellSeconds:R}"))));
        }
    }
}
