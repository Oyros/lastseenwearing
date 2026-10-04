using System;
using System.Linq;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Disguise;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Gameplay.Disguise;
using LastSeenWearing.Core.Wardrobe;
using LastSeenWearing.Editor.Import;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;

namespace LastSeenWearing.Tests.Disguise
{
    /// <summary>
    /// P1.21: changing in a tent (GDD §05) — a rail every client draws alike from the round's crowd, one use per
    /// round, only what is on the rail, the wardrobe's rules kept, the old clothes left behind.
    /// </summary>
    public sealed class TentTests
    {
        private const int Seed = 4242;

        private static WardrobeCatalog Catalog => AssetDatabase.LoadAssetAtPath<WardrobeCatalog>(WardrobeImporter.CatalogPath);
        private static WardrobeConfig Odds => AssetDatabase.LoadAssetAtPath<WardrobeConfig>("Assets/_Project/Data/Config/WardrobeConfig.asset");
        private static DisguiseConfig Config => AssetDatabase.LoadAssetAtPath<DisguiseConfig>("Assets/_Project/Data/Config/DisguiseConfig.asset");
        private static Outfit[] Crowd => OutfitPlanner.OutfitsFor(Seed, 200, Catalog, Odds);

        [Test]
        public void TheRailIsTheSameEverywhereAndHoldsWhatTheCrowdWears()
        {
            var crowd = Crowd;
            var rail = TentStock.For(Seed, 0, crowd, Config);
            Assert.That(TentStock.For(Seed, 0, crowd, Config).Select(i => (i.Slot, i.Garment)), Is.EqualTo(rail.Select(i => (i.Slot, i.Garment))),
                "drawn from the seed, not sent");
            Assert.That(rail.Count(i => i.Slot == ClothingSlot.Top), Is.EqualTo(Config.StockTops));
            Assert.That(rail.Count(i => i.Slot == ClothingSlot.Bottom), Is.EqualTo(Config.StockBottoms));
            Assert.That(rail.Count(i => i.Slot == ClothingSlot.Hat), Is.EqualTo(Config.StockHats));
            foreach (var item in rail)
            {
                Assert.That(item.Garment.IsNone, Is.False);
                Assert.That(crowd.Count(o => TentStock.Of(o, item.Slot).Equals(item.Garment)), Is.GreaterThanOrEqualTo(1),
                    "common items: someone in the crowd wears it (GDD §05)");
            }

            Assert.That(rail.Select(i => (i.Slot, i.Garment)).Distinct().Count(), Is.EqualTo(rail.Length), "no garment twice");
            var other = TentStock.For(Seed, 1, crowd, Config);
            Assert.That(other.Select(i => (i.Slot, i.Garment)), Is.Not.EqualTo(rail.Select(i => (i.Slot, i.Garment))), "each tent its own rail");
        }

        [Test]
        public void AChangeTakesFromTheRailAndLeavesTheOldClothesBehind()
        {
            var (wearer, rail, top) = ATopTheyCanChangeInto();
            var result = TentChange.Apply(wearer, rail, new bool[rail.Length], 1, top, TentChange.Keep, TentChange.Keep, Catalog);

            Assert.That(result.Done, Is.True, result.Refusal.ToString());
            Assert.That(result.Outfit.Top, Is.EqualTo(rail[top].Garment));
            Assert.That(result.Outfit.Bottom, Is.EqualTo(wearer.Bottom), "kept");
            Assert.That(result.Outfit.WithClothesOf(wearer), Is.EqualTo(wearer), "the same body: only clothes change");
            Assert.That(result.LeftBehind, Does.Contain(wearer.Top), "the old top stays in the tent");
            Assert.That(result.Taken, Is.EqualTo(new[] { top }));
        }

        [Test]
        public void ATentRefusesWhatItCannotDo()
        {
            var (wearer, rail, top) = ATopTheyCanChangeInto();
            var none = new bool[rail.Length];
            var keep = TentChange.Keep;

            Assert.That(TentChange.Apply(wearer, rail, none, 0, top, keep, keep, Catalog).Refusal, Is.EqualTo(ChangeRefusal.TentUsed), "one use per round");
            Assert.That(TentChange.Apply(wearer, rail, none, 1, keep, keep, keep, Catalog).Refusal, Is.EqualTo(ChangeRefusal.NothingChosen));

            var taken = new bool[rail.Length];
            taken[top] = true;
            Assert.That(TentChange.Apply(wearer, rail, taken, 1, top, keep, keep, Catalog).Refusal, Is.EqualTo(ChangeRefusal.NotInStock));
            Assert.That(TentChange.Apply(wearer, rail, none, 1, rail.Length, keep, keep, Catalog).Refusal, Is.EqualTo(ChangeRefusal.NotInStock));
            Assert.That(TentChange.Apply(wearer, rail, none, 1, keep, top, keep, Catalog).Refusal, Is.EqualTo(ChangeRefusal.WrongSlot),
                "a top is not trousers");
        }

        [Test]
        public void AHoodTakesTheHatOffAndNoHatGoesOverIt()
        {
            var catalog = Catalog;
            var hood = Array.FindIndex(catalog.Tops, t => t.Covers(WardrobeCatalog.HairRegion));
            var plain = Array.FindIndex(catalog.Tops, t => !t.Covers(WardrobeCatalog.HairRegion));
            var bottom = Array.FindIndex(catalog.Bottoms, b => !OutfitPlanner.Clash(catalog.Tops[hood].Id, b.Id) && !OutfitPlanner.Clash(catalog.Tops[plain].Id, b.Id));
            Assume.That(hood, Is.GreaterThanOrEqualTo(0));

            var hatted = new Outfit(Sex.Male, Height.Average, Build.Average, 0, 0, 0, new Worn(plain, 0, Tone.Dark), new Worn(bottom, 0, Tone.Light),
                new Worn(0, 0, Tone.Dark));
            var rail = new[] { new StockItem(ClothingSlot.Top, new Worn(hood, 1, Tone.Light)), new StockItem(ClothingSlot.Hat, new Worn(1, 2, Tone.Light)) };

            var hooded = TentChange.Apply(hatted, rail, new bool[2], 1, 0, TentChange.Keep, TentChange.Keep, catalog);
            Assert.That(hooded.Done, Is.True, hooded.Refusal.ToString());
            Assert.That(hooded.Outfit.Hat.IsNone, Is.True, "the hood goes up, the hat comes off");
            Assert.That(hooded.LeftBehind, Does.Contain(hatted.Hat), "and stays in the tent");

            Assert.That(TentChange.Apply(hatted, rail, new bool[2], 1, 0, TentChange.Keep, 1, catalog).Refusal, Is.EqualTo(ChangeRefusal.HatOverHood));
        }

        [Test]
        public void TheTrenchAndTheSkirtStillClash()
        {
            var catalog = Catalog;
            var trench = Array.FindIndex(catalog.Tops, t => catalog.Bottoms.Any(b => OutfitPlanner.Clash(t.Id, b.Id)));
            Assume.That(trench, Is.GreaterThanOrEqualTo(0), "the catalog has a clash");
            var skirt = Array.FindIndex(catalog.Bottoms, b => OutfitPlanner.Clash(catalog.Tops[trench].Id, b.Id));
            var plain = Array.FindIndex(catalog.Tops, t => !OutfitPlanner.Clash(t.Id, catalog.Bottoms[skirt].Id) && !t.Covers(WardrobeCatalog.HairRegion));

            var inSkirt = new Outfit(Sex.Female, Height.Average, Build.Average, 0, 0, 0, new Worn(plain, 0, Tone.Dark), new Worn(skirt, 0, Tone.Light),
                Worn.Nothing);
            var rail = new[] { new StockItem(ClothingSlot.Top, new Worn(trench, 0, Tone.Dark)) };
            Assert.That(TentChange.Apply(inSkirt, rail, new bool[1], 1, 0, TentChange.Keep, TentChange.Keep, Catalog).Refusal, Is.EqualTo(ChangeRefusal.Clash));
        }

        [Test]
        public void OnlyTheFugitiveChangesAndOnlyThePlainclothesLooksIn()
        {
            Assert.That(TentRules.MayChange(Role.Fugitive), Is.True);
            Assert.That(TentRules.MayChange(Role.Plainclothes), Is.False);
            Assert.That(TentRules.MayInspect(Role.Plainclothes), Is.True, "GDD §03");
            foreach (var role in new[] { Role.Patrol, Role.Dog, Role.Watcher, Role.Fugitive })
            {
                Assert.That(TentRules.MayInspect(role), Is.False, role.ToString());
            }
        }

        [Test]
        public void WhatIsMissingIsWhatWasTaken()
        {
            var rail = TentStock.For(Seed, 0, Crowd, Config);
            Assert.That(TentRules.Missing(rail, 0), Is.Empty);
            var missing = TentRules.Missing(rail, (1 << 0) | (1 << 3));
            Assert.That(missing.Select(i => (i.Slot, i.Garment)), Is.EqualTo(new[] { (rail[0].Slot, rail[0].Garment), (rail[3].Slot, rail[3].Garment) }));
        }

        [Test]
        public void ATentSharesNothingButItsRail()
        {
            // P2.02: whether a tent is used and what is gone are the server's, told only on opening or inspecting it.
            var shared = typeof(ChangingTent).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                .Where(f => typeof(NetworkVariableBase).IsAssignableFrom(f.FieldType)).Select(f => f.Name);
            Assert.That(shared, Is.Empty);
        }

        // Someone from the crowd and a top on the rail they can wear: different from theirs, no clash.
        private static (Outfit Wearer, StockItem[] Rail, int Top) ATopTheyCanChangeInto()
        {
            var catalog = Catalog;
            var crowd = Crowd;
            var rail = TentStock.For(Seed, 0, crowd, Config);
            foreach (var wearer in crowd)
            {
                for (var i = 0; i < rail.Length; i++)
                {
                    if (rail[i].Slot == ClothingSlot.Top && !rail[i].Garment.Equals(wearer.Top)
                        && !catalog.Tops[rail[i].Garment.Item].Covers(WardrobeCatalog.HairRegion)
                        && !OutfitPlanner.Clash(catalog.Tops[rail[i].Garment.Item].Id, catalog.Bottoms[wearer.Bottom.Item].Id))
                    {
                        return (wearer, rail, i);
                    }
                }
            }

            Assert.Fail("no one in the crowd can change into a top on the rail");
            return default;
        }
    }
}
