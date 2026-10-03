using System;
using System.Linq;
using LastSeenWearing.Core.Crowd;
using NUnit.Framework;

namespace LastSeenWearing.Tests.Crowd
{
    /// <summary>P1.06: every character's walk, from the seed (GDD §05, LSW_WalkSystem.md §3, D-025).</summary>
    public sealed class GaitSignatureTests
    {
        private static readonly GaitSettings Settings = new(0.25f, 0.45f, 0.30f, 0.4f);

        [Test]
        public void TheSameSeedGivesTheSameWalks()
        {
            Assert.That(GaitPlanner.SignaturesFor(31, 150, Settings), Is.EqualTo(GaitPlanner.SignaturesFor(31, 150, Settings)));
        }

        [Test]
        public void ADifferentSeedGivesDifferentWalks()
        {
            Assert.That(GaitPlanner.SignaturesFor(31, 30, Settings), Is.Not.EqualTo(GaitPlanner.SignaturesFor(32, 30, Settings)));
        }

        [TestCase(1)]
        [TestCase(77)]
        [TestCase(-5)]
        public void EveryWalkInACrowdIsUnique(int seed)
        {
            var walks = GaitPlanner.SignaturesFor(seed, 150, Settings);
            Assert.That(walks.Distinct().Count(), Is.EqualTo(walks.Length));
        }

        [Test]
        public void ASmallerCrowdIsAPrefixOfALargerOne()
        {
            var small = GaitPlanner.SignaturesFor(8, 40, Settings);
            var large = GaitPlanner.SignaturesFor(8, 150, Settings);
            Assert.That(small, Is.EqualTo(large.Take(40)));
        }

        [Test]
        public void EveryWalkStaysInsideTheBuckets()
        {
            foreach (var walk in GaitPlanner.SignaturesFor(4, 150, Settings))
            {
                Assert.That(walk.Traits.Count, Is.LessThanOrEqualTo(GaitSignature.MaxTraits));
                Assert.That(walk.Traits.Select(t => t.Trait).Distinct().Count(), Is.EqualTo(walk.Traits.Count), "a trait at most once");
                foreach (var trait in walk.Traits)
                {
                    var magnitude = Math.Abs(trait.Strength);
                    if (trait.Trait == WalkTrait.ArmSwing)
                    {
                        Assert.That(magnitude, Is.EqualTo(TraitStrength.Strong), "arm swing is stiff or big");
                    }
                    else
                    {
                        Assert.That(magnitude, Is.EqualTo(TraitStrength.Slight).Or.EqualTo(TraitStrength.Strong), $"{trait.Trait}");
                    }

                    if (trait.Trait != WalkTrait.Limp && trait.Trait != WalkTrait.ArmSwing)
                    {
                        Assert.That(trait.Strength, Is.GreaterThan(0f), $"{trait.Trait} has no side");
                    }
                }
            }
        }

        /// <summary>
        /// Uniqueness bends the odds (D-025): only 12 walks have no trait (4 base walks × 3 paces), so a
        /// 150-NPC crowd has at most 12 plain walkers and the rest lean to two traits. Measured over twenty
        /// crowds in P1.06: 12 / 71 / 67 for none / one / two — pinned loosely so a change is noticed.
        /// </summary>
        [Test]
        public void UniquenessCapsThePlainWalks()
        {
            var crowds = Enumerable.Range(0, 20).Select(seed => GaitPlanner.SignaturesFor(seed, 150, Settings)).ToArray();
            foreach (var crowd in crowds)
            {
                Assert.That(crowd.Count(w => w.Traits.Count == 0), Is.LessThanOrEqualTo(12), "only 12 trait-less walks exist");
            }

            var walks = crowds.SelectMany(c => c).ToArray();
            var two = walks.Count(w => w.Traits.Count == 2) / (float)walks.Length;
            Assert.That(two, Is.InRange(0.35f, 0.55f), "two-trait share");
        }

        [Test]
        public void ThereAreFarMoreWalksThanACrowd()
        {
            Assert.That(GaitPlanner.Capacity, Is.EqualTo(828));
            Assert.Throws<ArgumentOutOfRangeException>(() => GaitPlanner.SignaturesFor(1, GaitPlanner.Capacity + 1, Settings));
        }

        [Test]
        public void TheCrowdUsesEveryBaseWalkPaceAndTrait()
        {
            var walks = GaitPlanner.SignaturesFor(3, 150, Settings);
            Assert.That(walks.Select(w => w.Base).Distinct().Count(), Is.EqualTo(Enum.GetValues(typeof(BaseWalk)).Length));
            Assert.That(walks.Select(w => w.Tempo).Distinct().Count(), Is.EqualTo(Enum.GetValues(typeof(Tempo)).Length));
            Assert.That(walks.SelectMany(w => w.Traits).Select(t => t.Trait).Distinct().Count(), Is.EqualTo(Enum.GetValues(typeof(WalkTrait)).Length));
        }

        [Test]
        public void AWalkReadsInWords()
        {
            var walk = new GaitSignature(BaseWalk.Brisk, Tempo.Fast,
                new[] { new TraitStrength(WalkTrait.ArmSwing, 1f), new TraitStrength(WalkTrait.Limp, -0.5f) });
            Assert.That(walk.Describe(), Is.EqualTo("brisk, fast; slight limp left, swings arms"));
        }
    }
}
