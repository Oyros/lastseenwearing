using System.Linq;
using LastSeenWearing.Core.Composite;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Randomness;
using LastSeenWearing.Core.Wardrobe;
using LastSeenWearing.Editor.Import;
using LastSeenWearing.Gameplay.Round;
using NUnit.Framework;
using UnityEditor;

namespace LastSeenWearing.Tests.Composite
{
    /// <summary>
    /// P1.19: the witness sketch (GDD §04.1, D-008) — the case's fugitive and their sketch from the case seed, 1–2
    /// traits wrong, revealed round by round; the fugitive stays the same person all case; the Watcher's copy says
    /// nothing about mistakes; each round plants 2–3 partial lookalikes.
    /// </summary>
    public sealed class CompositeTests
    {
        private static WardrobeCatalog Catalog => AssetDatabase.LoadAssetAtPath<WardrobeCatalog>(WardrobeImporter.CatalogPath);
        private static WardrobeConfig Odds => AssetDatabase.LoadAssetAtPath<WardrobeConfig>("Assets/_Project/Data/Config/WardrobeConfig.asset");
        private static CrowdConfig Crowd => AssetDatabase.LoadAssetAtPath<CrowdConfig>("Assets/_Project/Data/Config/CrowdConfig.asset");
        private static CompositeConfig Config => AssetDatabase.LoadAssetAtPath<CompositeConfig>("Assets/_Project/Data/Config/CompositeConfig.asset");

        private const int Rounds = 3;

        private static (Suspect, CompositeSketch) Case(int seed)
        {
            var suspect = CompositeBuilder.SuspectFor(seed, Catalog, Odds, Crowd.Gait);
            return (suspect, CompositeBuilder.Build(seed, suspect, Catalog, Crowd.Gait, Config, Rounds));
        }

        [Test]
        public void TheSameCaseGivesTheSameFugitiveAndSketch()
        {
            var (a, sketchA) = Case(4242);
            var (b, sketchB) = Case(4242);
            Assert.That(a.Body, Is.EqualTo(b.Body));
            Assert.That(a.Walk, Is.EqualTo(b.Walk));
            Assert.That(sketchA.Claims.Select(c => (c.Trait, c.Value, c.Wrong, c.Confidence)),
                Is.EqualTo(sketchB.Claims.Select(c => (c.Trait, c.Value, c.Wrong, c.Confidence))));
        }

        [Test]
        public void OneOrTwoTraitsAreWrongAndOnlyThose()
        {
            var counts = new int[3];
            for (var seed = 0; seed < 200; seed++)
            {
                var (suspect, sketch) = Case(seed);
                var wrong = sketch.Claims.Count(c => c.Wrong);
                Assert.That(wrong, Is.InRange(1, 2), $"seed {seed}");
                counts[wrong]++;
                foreach (var claim in sketch.Claims)
                {
                    if (claim.Trait == CompositeTrait.Walk)
                    {
                        Assert.That(claim.Walk.Equals(suspect.Walk), Is.EqualTo(!claim.Wrong), $"seed {seed} walk");
                        continue;
                    }

                    Assert.That(claim.True, Is.EqualTo(suspect.ValueOf(claim.Trait)));
                    Assert.That(claim.Value == claim.True, Is.EqualTo(!claim.Wrong), $"seed {seed} {claim.Trait}");
                }

                var sex = (Sex)sketch.Find(CompositeTrait.Sex).Value.Value;
                Assert.That(Catalog.HairStyles[sketch.Find(CompositeTrait.HairStyle).Value.Value].Fits(sex), Is.True,
                    "the claimed hair suits the claimed sex");
            }

            Assert.That(counts[1], Is.GreaterThan(0));
            for (var seed = 0; seed < 200; seed++)
            {
                var (_, sketch) = Case(seed);
                var shown = sketch.Revealed(Rounds - 1, Config.Schedule).Select(c => c.Trait).ToHashSet();
                Assert.That(sketch.Claims.Where(c => c.Wrong).All(c => shown.Contains(c.Trait)), Is.True,
                    $"seed {seed}: every mistake is on a trait the {Rounds}-round case shows");
            }

            Assert.That(counts[2], Is.GreaterThan(0));
        }

        [Test]
        public void TheSketchRevealsRoundByRound()
        {
            var (_, sketch) = Case(7);
            var schedule = Config.Schedule;
            var traits = Enumerable.Range(0, 5).Select(r => sketch.Revealed(r, schedule).Select(c => c.Trait).ToArray()).ToArray();
            Assert.That(traits[0], Is.EquivalentTo(new[] { CompositeTrait.Sex, CompositeTrait.Height, CompositeTrait.Build }), "round 1: height and build (D-008)");
            Assert.That(traits[1].Except(traits[0]), Is.SubsetOf(new[] { CompositeTrait.HairStyle, CompositeTrait.HairColour }), "round 2: hair");
            Assert.That(traits[1], Does.Contain(CompositeTrait.HairStyle));
            Assert.That(traits[2].Except(traits[1]), Is.EquivalentTo(new[] { CompositeTrait.Walk }), "round 3: walk");
            Assert.That(traits[4].Length, Is.EqualTo(sketch.Claims.Count), "then everything");
        }

        [Test]
        public void ABaldFugitiveHasNoHairColourOnTheSketch()
        {
            for (var seed = 0; seed < 300; seed++)
            {
                var (suspect, sketch) = Case(seed);
                if (Catalog.HairStyles[suspect.Body.Hair].Bald)
                {
                    Assert.That(sketch.Has(CompositeTrait.HairColour), Is.False);
                    return;
                }
            }

            Assert.Inconclusive("no bald fugitive in 300 cases");
        }

        [Test]
        public void TheWatchersCopySaysNothingAboutMistakes()
        {
            var (_, sketch) = Case(99);
            var watcher = sketch.ForWatcher();
            Assert.That(watcher.Claims.Any(c => c.Wrong), Is.False);
            Assert.That(watcher.Claims.Select(c => c.True), Is.EqualTo(watcher.Claims.Select(c => c.Value)), "no truth leaks");
            Assert.That(watcher.Claims.Select(c => c.Value), Is.EqualTo(sketch.Claims.Select(c => c.Value)));

            var wire = CompositeSync.Message.Of(sketch).ToSketch();
            Assert.That(wire.Claims.Select(c => (c.Trait, c.Value, c.Wrong, c.True, c.Confidence)),
                Is.EqualTo(sketch.Claims.Select(c => (c.Trait, c.Value, c.Wrong, c.True, c.Confidence))), "the message carries it whole");
            Assert.That(wire.Find(CompositeTrait.Walk).Value.Walk, Is.EqualTo(sketch.Find(CompositeTrait.Walk).Value.Walk));
        }

        [Test]
        public void TheFugitiveIsTheSamePersonEveryRoundAndNoNpcWalksLikeThem()
        {
            const int caseSeed = 31337;
            var suspect = CompositeBuilder.SuspectFor(caseSeed, Catalog, Odds, Crowd.Gait);
            for (var round = 0; round < 4; round++)
            {
                var roundSeed = SeededRandom.Derive(caseSeed, round);
                var walks = GaitPlanner.SignaturesFor(roundSeed, 150, Crowd.Gait, GaitPlanner.CaseWalk(caseSeed, Crowd.Gait));
                Assert.That(walks, Has.No.Member(suspect.Walk), $"round {round}: the fugitive's walk is theirs alone");

                var outfit = OutfitPlanner.CharacterOutfit(roundSeed, 150, 0, Catalog, Odds, suspect.Body);
                Assert.That((outfit.Sex, outfit.Height, outfit.Build, outfit.Skin, outfit.Hair, outfit.HairColour),
                    Is.EqualTo((suspect.Body.Sex, suspect.Body.Height, suspect.Body.Build, suspect.Body.Skin, suspect.Body.Hair, suspect.Body.HairColour)),
                    $"round {round}: the same body");
                Assert.That(OutfitPlanner.OutfitsFor(roundSeed, 150, Catalog, Odds), Has.No.Member(outfit), "dressed unlike any NPC");
            }
        }

        [Test]
        public void EachRoundPlantsPartialLookalikes()
        {
            const int caseSeed = 555;
            var (suspect, sketch) = Case(caseSeed);
            for (var round = 0; round < 4; round++)
            {
                var roundSeed = SeededRandom.Derive(caseSeed, round);
                var reserved = GaitPlanner.CaseWalk(caseSeed, Crowd.Gait);
                var walks = GaitPlanner.SignaturesFor(roundSeed, 150, Crowd.Gait, reserved);
                var crowd = OutfitPlanner.OutfitsFor(roundSeed, 150, Catalog, Odds);
                var revealed = sketch.Revealed(round, Config.Schedule);
                var planted = LookalikePlanner.Plan(roundSeed, crowd, walks, reserved, revealed, Catalog, Config);

                Assert.That(planted.Count, Is.InRange(Config.LookalikesMin, Config.LookalikesMax), $"round {round}");
                Assert.That(planted.Select(l => l.Npc).Distinct().Count(), Is.EqualTo(planted.Count));
                foreach (var lookalike in planted)
                {
                    var matched = revealed.Count(c => c.Trait == CompositeTrait.Walk
                        ? lookalike.Walk != null && lookalike.Walk.Base == c.Walk.Base && lookalike.Walk.Tempo == c.Walk.Tempo
                        : new Suspect(lookalike.Outfit, null).ValueOf(c.Trait) == c.Value);
                    Assert.That(matched, Is.GreaterThanOrEqualTo(revealed.Count - Config.LookalikeMisses - 1), $"round {round} Npc {lookalike.Npc} is a lookalike");
                    Assert.That(Catalog.HairStyles[lookalike.Outfit.Hair].Fits(lookalike.Outfit.Sex), Is.True);
                    if (lookalike.Walk != null)
                    {
                        Assert.That(lookalike.Walk, Is.Not.EqualTo(suspect.Walk), "never the fugitive's walk");
                        Assert.That(walks.Where((w, i) => i != lookalike.Npc), Has.No.Member(lookalike.Walk), "still a walk of its own");
                    }
                }
            }
        }
    }
}
