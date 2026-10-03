using LastSeenWearing.Core.Crowd;
using NUnit.Framework;

namespace LastSeenWearing.Tests.Crowd
{
    /// <summary>P1.04: the walk cycle's clock — distance walked.</summary>
    public sealed class GaitAndDistanceTests
    {
        // As NpcScheduleTests: 10 m legs at 2 m/s (5 s), 3 s dwell.
        private static NpcSchedule Schedule()
        {
            var waypoints = new[] { new GroundPoint(0f, 0f), new GroundPoint(10f, 0f) };
            var plan = new NpcPlan(0, 0f, 0f, new[] { new NpcLeg(1, 0f, 0f, 3f), new NpcLeg(0, 0f, 0f, 3f) });
            return new NpcSchedule(plan, waypoints, 2f, (from, to) => new[] { from, to });
        }

        [Test]
        public void DistanceGrowsAtWalkingSpeed()
        {
            Schedule().Evaluate(2.5d, out _, out var walked);
            Assert.That(walked, Is.EqualTo(5d).Within(1e-6));
        }

        [Test]
        public void DistanceStandsStillWhileLingering()
        {
            var schedule = Schedule();
            schedule.Evaluate(5.5d, out _, out var early);
            schedule.Evaluate(7.9d, out _, out var late);
            Assert.That(early, Is.EqualTo(10d).Within(1e-6));
            Assert.That(late, Is.EqualTo(early).Within(1e-9));
        }

        [Test]
        public void DistanceKeepsCountingAcrossLegs()
        {
            // 8 s: leg 0 done (10 m) + dwell; 9 s is 1 s into leg 1.
            Schedule().Evaluate(9d, out _, out var walked);
            Assert.That(walked, Is.EqualTo(12d).Within(1e-6));
        }
    }
}
