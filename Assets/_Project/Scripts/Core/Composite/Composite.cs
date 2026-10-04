using System;
using System.Collections.Generic;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Wardrobe;

namespace LastSeenWearing.Core.Composite
{
    /// <summary>What the witness describes (GDD §04.1, §05): the permanent traits, and the coverable hair.</summary>
    public enum CompositeTrait : byte
    {
        Sex,
        Height,
        Build,
        HairStyle,
        HairColour,
        Walk,
        Skin,
    }

    /// <summary>How sure the witness is of one trait (GDD §04.1).</summary>
    public enum Confidence : byte
    {
        Low,
        Medium,
        High,
    }

    /// <summary>
    /// One trait as the witness gives it. <see cref="Value"/> indexes the trait's options (sex, height and build
    /// enums; the catalog's hair styles, hair colours and skins); a walk is <see cref="Walk"/>. When the witness
    /// is wrong, <see cref="True"/>/<see cref="TrueWalk"/> hold the fugitive's real one — only the server and the
    /// fugitive ever see those (GDD §04.1, ARCHITECTURE).
    /// </summary>
    public readonly struct CompositeClaim
    {
        public readonly CompositeTrait Trait;
        public readonly int Value;
        public readonly GaitSignature Walk;
        public readonly Confidence Confidence;
        public readonly bool Wrong;
        public readonly int True;
        public readonly GaitSignature TrueWalk;

        public CompositeClaim(CompositeTrait trait, int value, GaitSignature walk, Confidence confidence, bool wrong,
            int trueValue, GaitSignature trueWalk)
        {
            Trait = trait;
            Value = value;
            Walk = walk;
            Confidence = confidence;
            Wrong = wrong;
            True = trueValue;
            TrueWalk = trueWalk;
        }

        /// <summary>The Watcher's copy: the claim and its confidence, nothing about whether it is right.</summary>
        public CompositeClaim ForWatcher() => new(Trait, Value, Walk, Confidence, false, Value, Walk);
    }

    /// <summary>The fugitive for a case: the body the composite describes, and the walk (GDD §05: permanent).</summary>
    public readonly struct Suspect
    {
        public readonly Outfit Body;
        public readonly GaitSignature Walk;

        public Suspect(Outfit body, GaitSignature walk)
        {
            Body = body;
            Walk = walk;
        }

        /// <summary>The trait's value on this person.</summary>
        public int ValueOf(CompositeTrait trait) => trait switch
        {
            CompositeTrait.Sex => (int)Body.Sex,
            CompositeTrait.Height => (int)Body.Height,
            CompositeTrait.Build => (int)Body.Build,
            CompositeTrait.HairStyle => Body.Hair,
            CompositeTrait.HairColour => Body.HairColour,
            CompositeTrait.Skin => Body.Skin,
            _ => 0,
        };
    }

    /// <summary>
    /// The witness sketch the Watcher holds (GDD §04.1): every trait with its confidence, 1–2 of them wrong. It
    /// is revealed a few traits a round (D-008, <see cref="Config.CompositeConfig"/>).
    /// </summary>
    public sealed class CompositeSketch
    {
        public CompositeSketch(IReadOnlyList<CompositeClaim> claims)
        {
            Claims = claims;
        }

        public IReadOnlyList<CompositeClaim> Claims { get; }

        public bool Has(CompositeTrait trait) => Find(trait) != null;

        public CompositeClaim? Find(CompositeTrait trait)
        {
            foreach (var claim in Claims)
            {
                if (claim.Trait == trait)
                {
                    return claim;
                }
            }

            return null;
        }

        /// <summary>The Watcher's copy (<see cref="CompositeClaim.ForWatcher"/>).</summary>
        public CompositeSketch ForWatcher()
        {
            var claims = new CompositeClaim[Claims.Count];
            for (var i = 0; i < claims.Length; i++)
            {
                claims[i] = Claims[i].ForWatcher();
            }

            return new CompositeSketch(claims);
        }

        /// <summary>The claims shown in round <paramref name="round"/> (0-based), in reveal order.</summary>
        public List<CompositeClaim> Revealed(int round, IReadOnlyList<CompositeTrait[]> schedule)
        {
            var shown = new List<CompositeClaim>();
            for (var r = 0; r <= round && r < schedule.Count; r++)
            {
                foreach (var trait in schedule[r])
                {
                    if (Find(trait) is { } claim)
                    {
                        shown.Add(claim);
                    }
                }
            }

            if (round >= schedule.Count)
            {
                foreach (var claim in Claims)
                {
                    if (!shown.Exists(c => c.Trait == claim.Trait))
                    {
                        shown.Add(claim); // past the schedule: everything
                    }
                }
            }

            return shown;
        }

        public static int OptionCount(CompositeTrait trait, WardrobeCatalog catalog) => trait switch
        {
            CompositeTrait.Sex => Enum.GetValues(typeof(Sex)).Length,
            CompositeTrait.Height => Enum.GetValues(typeof(Height)).Length,
            CompositeTrait.Build => Enum.GetValues(typeof(Build)).Length,
            CompositeTrait.HairStyle => catalog.HairStyles.Length,
            CompositeTrait.HairColour => catalog.HairColours.Length,
            CompositeTrait.Skin => catalog.Skins.Length,
            _ => 0,
        };
    }
}
