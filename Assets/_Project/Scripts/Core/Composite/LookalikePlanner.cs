using System.Collections.Generic;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Randomness;
using LastSeenWearing.Core.Wardrobe;

namespace LastSeenWearing.Core.Composite
{
    /// <summary>One NPC turned into a partial lookalike: its new body (and walk, when the walk is revealed).</summary>
    public readonly struct Lookalike
    {
        public readonly int Npc;
        public readonly Outfit Outfit;
        public readonly GaitSignature Walk;

        public Lookalike(int npc, Outfit outfit, GaitSignature walk)
        {
            Npc = npc;
            Outfit = outfit;
            Walk = walk;
        }
    }

    /// <summary>
    /// The crowd's planted lookalikes for one round (GDD §04.1: "2–3 partial lookalikes of the composite on
    /// purpose", P1.19): NPCs that match every trait the Watcher has so far but one — as the composite gives them,
    /// wrong ones included, so an honest witness mistake points at an innocent. Server side: it needs the composite.
    /// </summary>
    public static class LookalikePlanner
    {
        private const int LookalikeStream = 14;

        public static List<Lookalike> Plan(int roundSeed, Outfit[] crowd, GaitSignature[] walks, GaitSignature reservedWalk,
            IReadOnlyList<CompositeClaim> revealed, WardrobeCatalog catalog, CompositeConfig config)
        {
            var random = new SeededRandom(SeededRandom.Derive(roundSeed, LookalikeStream));
            var count = random.Range(config.LookalikesMin, config.LookalikesMax + 1);
            var takenWalks = new HashSet<GaitSignature>(walks) { reservedWalk };
            var chosen = new HashSet<int>();
            var lookalikes = new List<Lookalike>();
            while (lookalikes.Count < count && chosen.Count < crowd.Length)
            {
                var npc = random.Range(0, crowd.Length);
                if (!chosen.Add(npc))
                {
                    continue;
                }

                // Every revealed trait but a few: the miss is what tells them apart.
                var copied = new List<CompositeClaim>(revealed);
                for (var m = 0; m < config.LookalikeMisses && copied.Count > 1; m++)
                {
                    copied.RemoveAt(random.Range(0, copied.Count));
                }

                var outfit = crowd[npc];
                GaitSignature walk = null;
                foreach (var claim in copied)
                {
                    switch (claim.Trait)
                    {
                        case CompositeTrait.Sex: outfit = outfit.WithBody(sex: (Sex)claim.Value); break;
                        case CompositeTrait.Height: outfit = outfit.WithBody(height: (Height)claim.Value); break;
                        case CompositeTrait.Build: outfit = outfit.WithBody(build: (Build)claim.Value); break;
                        case CompositeTrait.HairColour: outfit = outfit.WithBody(hairColour: claim.Value); break;
                        case CompositeTrait.Skin: outfit = outfit.WithBody(skin: claim.Value); break;
                        case CompositeTrait.HairStyle: outfit = outfit.WithBody(hair: claim.Value); break;
                        case CompositeTrait.Walk:
                            // Same base walk and pace, the NPC's own traits — partial, and still a walk of its own.
                            var candidate = new GaitSignature(claim.Walk.Base, claim.Walk.Tempo, walks[npc].Traits);
                            if (takenWalks.Add(candidate))
                            {
                                walk = candidate;
                            }

                            break;
                    }
                }

                // A hair style the body cannot wear (a beard on a woman) falls back to the NPC's own.
                if (!catalog.HairStyles[outfit.Hair].Fits(outfit.Sex))
                {
                    outfit = outfit.WithBody(hair: crowd[npc].Hair);
                    if (!catalog.HairStyles[outfit.Hair].Fits(outfit.Sex))
                    {
                        outfit = outfit.WithBody(sex: crowd[npc].Sex);
                    }
                }

                lookalikes.Add(new Lookalike(npc, outfit, walk));
            }

            return lookalikes;
        }
    }
}
