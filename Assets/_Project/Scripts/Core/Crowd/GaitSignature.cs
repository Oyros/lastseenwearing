using System;
using System.Collections.Generic;
using System.Linq;

namespace LastSeenWearing.Core.Crowd
{
    /// <summary>
    /// A character's walk (LSW_WalkSystem.md §3, GDD §05): a base walk, a pace, and at most two traits in
    /// buckets a player can say out loud. Traits are kept in <see cref="WalkTrait"/> order, so two equal
    /// walks compare equal. Every character's is unique within a round (<see cref="GaitPlanner"/>).
    /// </summary>
    public sealed class GaitSignature : IEquatable<GaitSignature>
    {
        public const int MaxTraits = 2;

        public BaseWalk Base { get; }
        public Tempo Tempo { get; }
        public IReadOnlyList<TraitStrength> Traits { get; }

        public GaitSignature(BaseWalk walk, Tempo tempo, IEnumerable<TraitStrength> traits)
        {
            Base = walk;
            Tempo = tempo;
            Traits = traits.OrderBy(t => t.Trait).ToArray();
            if (Traits.Count > MaxTraits)
            {
                throw new ArgumentException($"a walk has at most {MaxTraits} traits");
            }
        }

        public bool Equals(GaitSignature other)
        {
            return other != null && Base == other.Base && Tempo == other.Tempo && Traits.SequenceEqual(other.Traits);
        }

        public override bool Equals(object obj) => Equals(obj as GaitSignature);

        public override int GetHashCode()
        {
            var hash = ((int)Base * 31) ^ ((int)Tempo * 7);
            foreach (var trait in Traits)
            {
                hash = hash * 397 ^ trait.GetHashCode();
            }

            return hash;
        }

        /// <summary>Debug words, e.g. "brisk, fast; limps left, swings arms". Not player-facing (P1.19 localizes).</summary>
        public string Describe()
        {
            var head = $"{Base.ToString().ToLowerInvariant()}, {Tempo.ToString().ToLowerInvariant()}";
            return Traits.Count == 0 ? head + "; no trait" : head + "; " + string.Join(", ", Traits.Select(t => t.Describe()));
        }

        public override string ToString() => Describe();
    }
}
