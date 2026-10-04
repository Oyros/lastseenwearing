using System.Collections.Generic;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Randomness;
using LastSeenWearing.Core.Wardrobe;

namespace LastSeenWearing.Core.Composite
{
    /// <summary>
    /// The fugitive for a case and the witness's sketch of them (GDD §04.1, §05, P1.19), from the case seed.
    /// The body and the walk are drawn once for the case — they are permanent; only clothes change round to
    /// round. The sketch gives every trait with a confidence and gets 1–2 of them wrong. The server builds it and
    /// sends each role its copy; no client derives it.
    /// </summary>
    public static class CompositeBuilder
    {
        private const int SuspectStream = 12;
        private const int SketchStream = 13;

        // The order claims are made in: sex first, so a wrong hair style is still one the claimed sex can wear.
        private static readonly CompositeTrait[] Order =
        {
            CompositeTrait.Sex, CompositeTrait.Height, CompositeTrait.Build, CompositeTrait.HairStyle,
            CompositeTrait.HairColour, CompositeTrait.Walk, CompositeTrait.Skin,
        };

        public static Suspect SuspectFor(int caseSeed, WardrobeCatalog catalog, WardrobeConfig odds, GaitSettings gait) =>
            new(OutfitPlanner.Person(SeededRandom.Derive(caseSeed, SuspectStream), catalog, odds), GaitPlanner.CaseWalk(caseSeed, gait));

        /// <param name="rounds">The case's rounds: a mistake goes only on a trait the case will reveal (D-008).</param>
        public static CompositeSketch Build(int caseSeed, Suspect suspect, WardrobeCatalog catalog, GaitSettings gait, CompositeConfig config,
            int rounds)
        {
            var random = new SeededRandom(SeededRandom.Derive(caseSeed, SketchStream));

            // A bald head has no hair colour to describe.
            var traits = new List<CompositeTrait>(Order);
            if (catalog.HairStyles[suspect.Body.Hair].Bald)
            {
                traits.Remove(CompositeTrait.HairColour);
            }

            // A witness does not take a bearded man for a woman: sex can be wrong only if the hair suits both.
            var mistakable = new List<CompositeTrait>(traits);
            var revealed = new HashSet<CompositeTrait>();
            var schedule = config.Schedule;
            for (var r = 0; r < rounds; r++)
            {
                if (r >= schedule.Length)
                {
                    revealed.UnionWith(traits); // past the list, everything
                    break;
                }

                revealed.UnionWith(schedule[r]);
            }

            mistakable.RemoveAll(t => !revealed.Contains(t));
            var hair = catalog.HairStyles[suspect.Body.Hair];
            if (!hair.Fits(Sex.Male) || !hair.Fits(Sex.Female))
            {
                mistakable.Remove(CompositeTrait.Sex);
            }

            var errorCount = System.Math.Min(random.Value() < config.TwoErrorChance ? 2 : 1, mistakable.Count);
            var wrong = new HashSet<CompositeTrait>();
            while (wrong.Count < errorCount)
            {
                wrong.Add(mistakable[random.Range(0, mistakable.Count)]);
            }

            var claims = new List<CompositeClaim>();
            var claimedSex = suspect.Body.Sex;
            foreach (var trait in traits)
            {
                var confidence = Draw(random, config);
                var isWrong = wrong.Contains(trait);
                if (trait == CompositeTrait.Walk)
                {
                    var walk = suspect.Walk;
                    while (isWrong && walk.Equals(suspect.Walk))
                    {
                        walk = GaitPlanner.Draw(random, gait);
                    }

                    claims.Add(new CompositeClaim(trait, 0, walk, confidence, isWrong, 0, suspect.Walk));
                    continue;
                }

                var truth = suspect.ValueOf(trait);
                var value = isWrong ? Other(random, trait, truth, claimedSex, catalog) : truth;
                if (trait == CompositeTrait.Sex)
                {
                    claimedSex = (Sex)value;
                }

                claims.Add(new CompositeClaim(trait, value, null, confidence, isWrong, truth, null));
            }

            return new CompositeSketch(claims);
        }

        private static int Other(SeededRandom random, CompositeTrait trait, int truth, Sex claimedSex, WardrobeCatalog catalog)
        {
            var options = new List<int>();
            for (var v = 0; v < CompositeSketch.OptionCount(trait, catalog); v++)
            {
                if (v != truth && (trait != CompositeTrait.HairStyle || catalog.HairStyles[v].Fits(claimedSex)))
                {
                    options.Add(v);
                }
            }

            return options[random.Range(0, options.Count)];
        }

        private static Confidence Draw(SeededRandom random, CompositeConfig config)
        {
            var roll = random.Value() * (config.HighOdds + config.MediumOdds + config.LowOdds);
            return roll < config.HighOdds ? Confidence.High : roll < config.HighOdds + config.MediumOdds ? Confidence.Medium : Confidence.Low;
        }
    }
}
