using System.Collections.Generic;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Randomness;
using LastSeenWearing.Core.Wardrobe;

namespace LastSeenWearing.Core.Disguise
{
    public enum ClothingSlot : byte
    {
        Top,
        Bottom,
        Hat,
    }

    /// <summary>One garment on a tent's rail: which slot, which item, which colour.</summary>
    public readonly struct StockItem
    {
        public readonly ClothingSlot Slot;
        public readonly Worn Garment;

        public StockItem(ClothingSlot slot, Worn garment)
        {
            Slot = slot;
            Garment = garment;
        }
    }

    /// <summary>
    /// A changing tent's rail for a round (GDD §05, P1.21): public at round start, so every client draws the same
    /// from the round's seed. Garments are copied from what the round's crowd wears — "tents hold common items, so a
    /// missing one narrows the search but never identifies".
    /// </summary>
    public static class TentStock
    {
        private const int TentStream = 15;

        public static StockItem[] For(int roundSeed, int tent, Outfit[] crowd, DisguiseConfig config)
        {
            var random = new SeededRandom(SeededRandom.Derive(SeededRandom.Derive(roundSeed, TentStream), tent));
            var items = new List<StockItem>();
            Add(items, random, crowd, ClothingSlot.Top, config.StockTops);
            Add(items, random, crowd, ClothingSlot.Bottom, config.StockBottoms);
            Add(items, random, crowd, ClothingSlot.Hat, config.StockHats);
            return items.ToArray();
        }

        private static void Add(List<StockItem> items, SeededRandom random, Outfit[] crowd, ClothingSlot slot, int count)
        {
            var added = 0;
            for (var tries = 0; added < count && tries < crowd.Length * 4; tries++)
            {
                var worn = Of(crowd[random.Range(0, crowd.Length)], slot);
                if (worn.IsNone || items.Exists(i => i.Slot == slot && i.Garment.Equals(worn)))
                {
                    continue; // no hat on this one, or already on the rail
                }

                items.Add(new StockItem(slot, worn));
                added++;
            }
        }

        public static Worn Of(Outfit outfit, ClothingSlot slot) => slot switch
        {
            ClothingSlot.Top => outfit.Top,
            ClothingSlot.Bottom => outfit.Bottom,
            _ => outfit.Hat,
        };
    }
}
