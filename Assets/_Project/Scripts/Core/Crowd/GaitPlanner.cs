using System;
using System.Collections.Generic;
using LastSeenWearing.Core.Randomness;

namespace LastSeenWearing.Core.Crowd
{
    /// <summary>
    /// Every character's walk, from the crowd seed (LSW_WalkSystem.md §3, GDD §05) — computed on every
    /// client, never sent. Character <c>i</c> draws from its own gait stream, <c>Derive(Derive(seed, i),
    /// GaitStream)</c>, so gait draws never move a route. Walks are unique (GDD §05): a character whose
    /// draw is already taken keeps drawing from its stream until it is not, so a walk depends on the
    /// walks before it — the same on every client, and a smaller crowd is always a prefix of a larger one.
    /// </summary>
    public static class GaitPlanner
    {
        private const int GaitStream = 1;
        private static readonly WalkTrait[] Traits = (WalkTrait[])Enum.GetValues(typeof(WalkTrait));
        private static readonly int BaseWalkCount = Enum.GetValues(typeof(BaseWalk)).Length;
        private static readonly int TempoCount = Enum.GetValues(typeof(Tempo)).Length;

        /// <summary>
        /// How many different walks exist: base walks × paces × trait sets (none; each trait in each of its
        /// buckets; each pair of different traits). 828 with the current buckets — a crowd cannot ask for more.
        /// </summary>
        public static int Capacity
        {
            get
            {
                var options = new List<int>();
                foreach (var trait in Traits)
                {
                    options.Add(trait == WalkTrait.Limp ? 4 : 2); // limp: 2 sides × 2 strengths; arm swing: stiff/big; others: 2 strengths
                }

                var sets = 1;
                for (var a = 0; a < options.Count; a++)
                {
                    sets += options[a];
                    for (var b = a + 1; b < options.Count; b++)
                    {
                        sets += options[a] * options[b];
                    }
                }

                return BaseWalkCount * TempoCount * sets;
            }
        }

        public static GaitSignature[] SignaturesFor(int crowdSeed, int count, GaitSettings settings)
        {
            // Every walk is unique, so a crowd larger than the number of walks would redraw forever.
            if (count > Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"{count} walks asked for; only {Capacity} exist.");
            }

            var taken = new HashSet<GaitSignature>();
            var signatures = new GaitSignature[count];
            for (var i = 0; i < count; i++)
            {
                var random = new SeededRandom(SeededRandom.Derive(SeededRandom.Derive(crowdSeed, i), GaitStream));
                GaitSignature signature;
                do
                {
                    signature = Draw(random, settings);
                }
                while (!taken.Add(signature));

                signatures[i] = signature;
            }

            return signatures;
        }

        /// <summary>
        /// The walk of a character who is not in the crowd — the fugitive (slot 0), later the plainclothes —
        /// drawn after the crowd's, so it is unique among them too (GDD §05). Every client gets the same.
        /// </summary>
        public static GaitSignature CharacterSignature(int crowdSeed, int crowdSize, int slot, GaitSettings settings)
        {
            return SignaturesFor(crowdSeed, crowdSize + slot + 1, settings)[crowdSize + slot];
        }

        private static GaitSignature Draw(SeededRandom random, GaitSettings settings)
        {
            var walk = (BaseWalk)random.Range(0, BaseWalkCount);
            var tempo = (Tempo)random.Range(0, TempoCount);

            var roll = random.Value() * (settings.NoTrait + settings.OneTrait + settings.TwoTraits);
            var count = roll < settings.NoTrait ? 0 : roll < settings.NoTrait + settings.OneTrait ? 1 : 2;

            var pool = new List<WalkTrait>(Traits);
            var traits = new List<TraitStrength>(count);
            for (var t = 0; t < count; t++)
            {
                var pick = random.Range(0, pool.Count);
                var trait = pool[pick];
                pool.RemoveAt(pick);
                traits.Add(new TraitStrength(trait, Strength(trait, random, settings)));
            }

            return new GaitSignature(walk, tempo, traits);
        }

        private static float Strength(WalkTrait trait, SeededRandom random, GaitSettings settings)
        {
            var side = random.Range(0, 2) == 0 ? -1f : 1f;
            if (trait == WalkTrait.ArmSwing)
            {
                return side * TraitStrength.Strong; // stiff or big; no slight bucket
            }

            var magnitude = random.Value() < settings.StrongChance ? TraitStrength.Strong : TraitStrength.Slight;
            return trait == WalkTrait.Limp ? side * magnitude : magnitude;
        }
    }
}
