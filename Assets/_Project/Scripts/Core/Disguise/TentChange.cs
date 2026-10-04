using System.Collections.Generic;
using LastSeenWearing.Core.Wardrobe;

namespace LastSeenWearing.Core.Disguise
{
    /// <summary>Why a tent turned a change down.</summary>
    public enum ChangeRefusal : byte
    {
        None,
        TentUsed,
        NotInStock,
        WrongSlot,
        NothingChosen,
        HatOverHood,
        Clash,
        TooFar,
    }

    /// <summary>The outcome of one change: the new outfit, what stays behind in the tent, what leaves the rail.</summary>
    public readonly struct ChangeResult
    {
        public readonly ChangeRefusal Refusal;
        public readonly Outfit Outfit;
        public readonly IReadOnlyList<Worn> LeftBehind;
        public readonly IReadOnlyList<int> Taken;

        public ChangeResult(ChangeRefusal refusal, Outfit outfit, IReadOnlyList<Worn> leftBehind, IReadOnlyList<int> taken)
        {
            Refusal = refusal;
            Outfit = outfit;
            LeftBehind = leftBehind;
            Taken = taken;
        }

        public bool Done => Refusal == ChangeRefusal.None;
    }

    /// <summary>
    /// A change in a tent (GDD §05, P1.21) — the server's rule: the tent has a use left, each chosen garment is on
    /// its rail and in its slot, the result keeps the wardrobe's rules (no hat over a hood, no skirt under the
    /// trench). What comes off stays behind in the tent (the dog's trail, later); what goes on leaves the rail.
    /// Putting on a hood takes the hat off — it stays behind too.
    /// </summary>
    public static class TentChange
    {
        public const int Keep = -1;

        public static ChangeResult Apply(Outfit current, StockItem[] rail, bool[] taken, int usesLeft, int top, int bottom, int hat,
            WardrobeCatalog catalog)
        {
            if (usesLeft <= 0)
            {
                return Refused(ChangeRefusal.TentUsed, current);
            }

            if (top == Keep && bottom == Keep && hat == Keep)
            {
                return Refused(ChangeRefusal.NothingChosen, current);
            }

            var chosen = new[] { (top, ClothingSlot.Top), (bottom, ClothingSlot.Bottom), (hat, ClothingSlot.Hat) };
            foreach (var (index, slot) in chosen)
            {
                if (index == Keep)
                {
                    continue;
                }

                if (index < 0 || index >= rail.Length || taken[index])
                {
                    return Refused(ChangeRefusal.NotInStock, current);
                }

                if (rail[index].Slot != slot)
                {
                    return Refused(ChangeRefusal.WrongSlot, current);
                }
            }

            var newTop = top == Keep ? current.Top : rail[top].Garment;
            var newBottom = bottom == Keep ? current.Bottom : rail[bottom].Garment;
            var newHat = hat == Keep ? current.Hat : rail[hat].Garment;
            var hooded = catalog.Tops[newTop.Item].Covers(WardrobeCatalog.HairRegion);
            if (hooded && hat != Keep)
            {
                return Refused(ChangeRefusal.HatOverHood, current);
            }

            if (OutfitPlanner.Clash(catalog.Tops[newTop.Item].Id, catalog.Bottoms[newBottom.Item].Id))
            {
                return Refused(ChangeRefusal.Clash, current);
            }

            if (hooded)
            {
                newHat = Worn.Nothing; // the hood goes up, the hat comes off
            }

            var left = new List<Worn>();
            AddIfChanged(left, current.Top, newTop);
            AddIfChanged(left, current.Bottom, newBottom);
            AddIfChanged(left, current.Hat, newHat);

            var takenNow = new List<int>();
            foreach (var (index, _) in chosen)
            {
                if (index != Keep)
                {
                    takenNow.Add(index);
                }
            }

            var outfit = current.WithClothesOf(new Outfit(current.Sex, current.Height, current.Build, current.Skin, current.Hair,
                current.HairColour, newTop, newBottom, newHat));
            return new ChangeResult(ChangeRefusal.None, outfit, left, takenNow);
        }

        private static void AddIfChanged(List<Worn> left, Worn before, Worn after)
        {
            if (!before.IsNone && !before.Equals(after))
            {
                left.Add(before);
            }
        }

        private static ChangeResult Refused(ChangeRefusal refusal, Outfit current) =>
            new(refusal, current, new List<Worn>(), new List<int>());
    }
}
