using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Movement;
using NUnit.Framework;

namespace LastSeenWearing.Tests.Movement
{
    /// <summary>P1.11: walking from a stick or WASD, relative to the camera.</summary>
    public sealed class GroundMotionTests
    {
        private static void AssertVelocity(GroundPoint actual, float x, float z)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(1e-4f), "x");
            Assert.That(actual.Z, Is.EqualTo(z).Within(1e-4f), "z");
        }

        [Test]
        public void ForwardGoesWhereTheCameraLooks()
        {
            AssertVelocity(GroundMotion.DesiredVelocity(0f, 1f, 0f, 2f), 0f, 2f);
            AssertVelocity(GroundMotion.DesiredVelocity(0f, 1f, 90f, 2f), 2f, 0f);
            AssertVelocity(GroundMotion.DesiredVelocity(0f, 1f, 180f, 2f), 0f, -2f);
        }

        [Test]
        public void RightIsTheCamerasRight()
        {
            AssertVelocity(GroundMotion.DesiredVelocity(1f, 0f, 0f, 2f), 2f, 0f);
            AssertVelocity(GroundMotion.DesiredVelocity(1f, 0f, 90f, 2f), 0f, -2f);
        }

        [Test]
        public void ADiagonalIsNoFasterThanStraight()
        {
            var diagonal = GroundMotion.DesiredVelocity(1f, 1f, 0f, 2f);
            Assert.That(GroundPoint.Distance(new GroundPoint(0f, 0f), diagonal), Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void HalfAStickIsHalfTheSpeed()
        {
            AssertVelocity(GroundMotion.DesiredVelocity(0f, 0.5f, 0f, 2f), 0f, 1f);
        }

        [Test]
        public void SpeedChangesAtMostByTheStep()
        {
            var next = GroundMotion.StepToward(new GroundPoint(0f, 0f), new GroundPoint(0f, 2f), 0.5f);
            AssertVelocity(next, 0f, 0.5f);
            AssertVelocity(GroundMotion.StepToward(next, new GroundPoint(0f, 0.6f), 0.5f), 0f, 0.6f);
        }
    }
}
