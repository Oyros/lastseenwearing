using LastSeenWearing.Core.Crowd;
using NUnit.Framework;

namespace LastSeenWearing.Tests.Crowd
{
    /// <summary>
    /// P1.02: an NPC's position is a pure function of crowd time (D-019). Straight-line paths stand
    /// in for the NavMesh, so every expected position can be worked out by hand.
    /// </summary>
    public sealed class NpcScheduleTests
    {
        // Waypoints 0 and 1 are 10 m apart on X. Speed 2 m/s → 5 s per leg; dwell 3 s.
        private static readonly GroundPoint[] Waypoints = { new(0f, 0f), new(10f, 0f) };

        private static NpcSchedule Schedule()
        {
            var plan = new NpcPlan(0, 0f, 0f, new[] { new NpcLeg(1, 0f, 0f, 3f), new NpcLeg(0, 0f, 0f, 3f) });
            return new NpcSchedule(plan, Waypoints, 2f, (from, to) => new[] { from, to });
        }

        [Test]
        public void BeforeTheStartItStandsAtItsSpawn()
        {
            var at = Schedule().Evaluate(-4d, out _);
            Assert.That(at.X, Is.EqualTo(0f));
            Assert.That(at.Z, Is.EqualTo(0f));
        }

        [Test]
        public void ItWalksAtItsSpeed()
        {
            var at = Schedule().Evaluate(2.5d, out var heading);
            Assert.That(at.X, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(heading, Is.EqualTo(90f).Within(1e-3f), "walking +X is a 90° heading");
        }

        [Test]
        public void ItLingersAtTheWaypoint()
        {
            var schedule = Schedule();
            Assert.That(schedule.Evaluate(5.5d, out _).X, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(schedule.Evaluate(7.9d, out _).X, Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void TheNextLegStartsAfterTheDwell()
        {
            // Leg 1 starts at 8 s, walking back toward 0.
            Assert.That(Schedule().Evaluate(9d, out _).X, Is.EqualTo(8f).Within(1e-4f));
        }

        [Test]
        public void TheRouteLoops()
        {
            // One loop = 2 × (5 + 3) = 16 s; 18.5 s is 2.5 s into the first leg again.
            Assert.That(Schedule().Evaluate(18.5d, out _).X, Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void TimeGoingBackwardsGivesTheSameAnswer()
        {
            var schedule = Schedule();
            schedule.Evaluate(40d, out _);
            Assert.That(schedule.Evaluate(2.5d, out _).X, Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void TwoSchedulesAgreeHoweverTheyAreSampled()
        {
            var steady = Schedule();
            var jumpy = Schedule();
            for (var t = 0d; t < 420d; t += 0.02d)
            {
                steady.Evaluate(t, out _);
            }

            var a = steady.Evaluate(420d, out _);
            var b = jumpy.Evaluate(420d, out _);
            Assert.That(a.X, Is.EqualTo(b.X));
            Assert.That(a.Z, Is.EqualTo(b.Z));
        }

        [Test]
        public void ItNeverJumps()
        {
            var schedule = Schedule();
            var previous = schedule.Evaluate(0d, out _);
            for (var t = 0.05d; t < 60d; t += 0.05d)
            {
                var now = schedule.Evaluate(t, out _);
                Assert.That(GroundPoint.Distance(previous, now), Is.LessThanOrEqualTo(2f * 0.05f + 1e-4f), $"jump at {t:F2} s");
                previous = now;
            }
        }
    }
}
