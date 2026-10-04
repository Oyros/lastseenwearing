using System.Collections.Generic;
using System.Text;
using LastSeenWearing.Core.Disguise;
using LastSeenWearing.Core.Wardrobe;
using UnityEngine.Localization;

namespace LastSeenWearing.UI.Common
{
    /// <summary>
    /// How a garment is said on a black-and-white feed (P1.20): light or dark, and what it is — "dark hoodie". One
    /// place for the last-seen line, the tent panel and the tent rails.
    /// </summary>
    public static class WardrobeWords
    {
        private const string UiTable = "UI";
        private const string FestivalTable = "Festival";

        public static string Garment(Worn worn, WardrobeCatalog.Garment[] items) =>
            Text(UiTable, "lastseen.garment", Text(FestivalTable, $"person.tone.{worn.Tone.ToString().ToLowerInvariant()}"),
                Text(FestivalTable, items[worn.Item].NameKey));

        public static string Garment(Worn worn, ClothingSlot slot, WardrobeCatalog catalog) => Garment(worn, Items(slot, catalog));

        public static WardrobeCatalog.Garment[] Items(ClothingSlot slot, WardrobeCatalog catalog) => slot switch
        {
            ClothingSlot.Top => catalog.Tops,
            ClothingSlot.Bottom => catalog.Bottoms,
            _ => catalog.Hats,
        };

        /// <summary>A tent's rail in one line (P2.02); the items in <paramref name="missingMask"/> marked as gone.</summary>
        public static string Rail(StockItem[] rail, WardrobeCatalog catalog, int missingMask = 0)
        {
            var parts = new List<string>(rail.Length);
            for (var i = 0; i < rail.Length; i++)
            {
                var word = Garment(rail[i].Garment, rail[i].Slot, catalog);
                parts.Add((missingMask & (1 << i)) != 0 ? Text(UiTable, "tent.inspect.missing", word) : word);
            }

            return string.Join(", ", parts);
        }

        public static string Text(string table, string key, params object[] arguments) =>
            new LocalizedString(table, key).GetLocalizedString(arguments);
    }
}
