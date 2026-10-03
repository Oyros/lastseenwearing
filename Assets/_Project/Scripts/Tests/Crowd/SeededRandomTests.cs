using LastSeenWearing.Core.Crowd;
using NUnit.Framework;

namespace LastSeenWearing.Tests.Crowd
{
    /// <summary>
    /// P1.01: the PRNG every client shares. The pinned values catch a change to the algorithm —
    /// any change re-rolls every crowd and must be deliberate.
    /// </summary>
    public sealed class SeededRandomTests
    {
        [Test]
        public void TheSequenceForAKnownSeedIsPinned()
        {
            var random = new SeededRandom(42);
            var actual = new[] { random.NextUInt(), random.NextUInt(), random.NextUInt() };
            Assert.That(actual, Is.EqualTo(PinnedSeed42));
        }

        [Test]
        public void DeriveIsPinned()
        {
            Assert.That(SeededRandom.Derive(42, 7), Is.EqualTo(PinnedDerive42And7));
        }

        [Test]
        public void IntRangeStaysInBounds()
        {
            var random = new SeededRandom(1);
            for (var i = 0; i < 10000; i++)
            {
                Assert.That(random.Range(3, 9), Is.InRange(3, 8));
            }
        }

        [Test]
        public void ValueStaysInTheUnitInterval()
        {
            var random = new SeededRandom(2);
            for (var i = 0; i < 10000; i++)
            {
                var value = random.Value();
                Assert.That(value, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [Test]
        public void SeedZeroStillProducesNumbers()
        {
            var random = new SeededRandom(0);
            Assert.That(random.NextUInt() != 0 || random.NextUInt() != 0, Is.True);
        }

        // Cross-checked against an independent Python xorshift64* / SplitMix64 when pinned (P1.01).
        private static readonly uint[] PinnedSeed42 = { 833678567u, 2416485297u, 2087809963u };
        private const int PinnedDerive42And7 = 425796879;
    }
}
