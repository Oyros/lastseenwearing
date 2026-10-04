using System;
using System.Collections.Generic;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Randomness;

namespace LastSeenWearing.Core.Wardrobe
{
    /// <summary>
    /// Every character's outfit, from the crowd seed (GDD §05, P1.18) — computed on every client, never sent (D-005).
    /// Character <c>i</c> draws from its own outfit stream, <c>Derive(Derive(seed, i), OutfitStream)</c>, so an
    /// outfit never moves a route or a walk. Rules from the art: a hood covers the head, so no hat with it; the
    /// facial-hair styles are men's; no skirt under the trench coat (it shows through, PL.33). Outfits may repeat
    /// in the crowd — lookalikes are part of the game (§04.1).
    /// </summary>
    public static class OutfitPlanner
    {
        private const int OutfitStream = 2;

        // Garments that clip through each other (art, PL.33): the bottom is redrawn.
        private static readonly (string Top, string Bottom)[] Clashes = { ("Trench", "Skirt") };

        public static Outfit[] OutfitsFor(int crowdSeed, int count, WardrobeCatalog catalog, WardrobeConfig odds)
        {
            var outfits = new Outfit[count];
            for (var i = 0; i < count; i++)
            {
                outfits[i] = Draw(new SeededRandom(SeededRandom.Derive(SeededRandom.Derive(crowdSeed, i), OutfitStream)), catalog, odds);
            }

            return outfits;
        }

        /// <summary>
        /// The outfit of a character who is not in the crowd — the fugitive at the start of a round (slot 0) —
        /// drawn after the crowd's and redrawn until no NPC wears exactly the same. With a <paramref name="body"/>
        /// (the fugitive's, fixed for the case — P1.19) only the clothes are drawn. Every client gets the same.
        /// </summary>
        public static Outfit CharacterOutfit(int crowdSeed, int crowdSize, int slot, WardrobeCatalog catalog, WardrobeConfig odds,
            Outfit? body = null)
        {
            var crowd = new HashSet<Outfit>(OutfitsFor(crowdSeed, crowdSize, catalog, odds));
            var random = new SeededRandom(SeededRandom.Derive(SeededRandom.Derive(crowdSeed, crowdSize + slot), OutfitStream));
            while (true)
            {
                var outfit = Draw(random, catalog, odds);
                if (body is { } person)
                {
                    outfit = person.WithClothesOf(outfit); // the drawn clothes keep their own rules (hood, clash)
                }

                if (!crowd.Contains(outfit))
                {
                    return outfit;
                }
            }
        }

        /// <summary>A whole person drawn from their own seed — the fugitive's body for a case (P1.19).</summary>
        public static Outfit Person(int seed, WardrobeCatalog catalog, WardrobeConfig odds) =>
            Draw(new SeededRandom(SeededRandom.Derive(seed, OutfitStream)), catalog, odds);

        private static Outfit Draw(SeededRandom random, WardrobeCatalog catalog, WardrobeConfig odds)
        {
            var sex = random.Value() < odds.FemaleShare ? Sex.Female : Sex.Male;
            var heightRoll = random.Value() * (odds.AverageHeightOdds + odds.ShortOdds + odds.TallOdds);
            var height = heightRoll < odds.AverageHeightOdds ? Height.Average
                : heightRoll < odds.AverageHeightOdds + odds.ShortOdds ? Height.Short
                : Height.Tall;
            var buildRoll = random.Value() * (odds.AverageOdds + odds.SlimOdds + odds.HeavyOdds);
            var build = buildRoll < odds.AverageOdds ? Build.Average
                : buildRoll < odds.AverageOdds + odds.SlimOdds ? Build.Slim
                : Build.Heavy;
            var skin = random.Range(0, catalog.Skins.Length);

            var styles = new List<int>();
            for (var s = 0; s < catalog.HairStyles.Length; s++)
            {
                if (catalog.HairStyles[s].Fits(sex))
                {
                    styles.Add(s);
                }
            }

            var hair = styles[random.Range(0, styles.Count)];
            var hairColour = random.Range(0, catalog.HairColours.Length);

            var top = Garment(random, catalog.Tops.Length, catalog, odds);
            Worn bottom;
            do
            {
                bottom = Garment(random, catalog.Bottoms.Length, catalog, odds);
            }
            while (Clash(catalog.Tops[top.Item].Id, catalog.Bottoms[bottom.Item].Id));

            var hatRoll = random.Value();
            var hat = Garment(random, catalog.Hats.Length, catalog, odds); // drawn either way, so the stream stays aligned
            var hooded = catalog.Tops[top.Item].Covers(WardrobeCatalog.HairRegion);
            if (hooded || hatRoll >= odds.HatOdds)
            {
                hat = Worn.Nothing;
            }

            return new Outfit(sex, height, build, skin, hair, hairColour, top, bottom, hat);
        }

        public static bool Clash(string top, string bottom) => Array.Exists(Clashes, c => c.Top == top && c.Bottom == bottom);

        private static Worn Garment(SeededRandom random, int choices, WardrobeCatalog catalog, WardrobeConfig odds)
        {
            var item = random.Range(0, choices);
            var hue = random.Range(0, catalog.Cloth.Length);
            var tone = random.Value() < odds.LightShare ? Tone.Light : Tone.Dark;
            return new Worn(item, hue, tone);
        }
    }
}
