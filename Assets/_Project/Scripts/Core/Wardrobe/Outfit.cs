using System;

namespace LastSeenWearing.Core.Wardrobe
{
    /// <summary>A garment's pick and colour: the catalog entry, the cloth hue and its value.</summary>
    public readonly struct Worn : IEquatable<Worn>
    {
        public const int None = -1;

        public readonly int Item;
        public readonly int Hue;
        public readonly Tone Tone;

        public Worn(int item, int hue, Tone tone)
        {
            Item = item;
            Hue = hue;
            Tone = tone;
        }

        public bool IsNone => Item == None;

        public static Worn Nothing => new(None, 0, Tone.Light);

        public bool Equals(Worn other) => Item == other.Item && (IsNone || (Hue == other.Hue && Tone == other.Tone));

        public override bool Equals(object obj) => obj is Worn other && Equals(other);

        public override int GetHashCode() => IsNone ? None : HashCode.Combine(Item, Hue, Tone);
    }

    /// <summary>
    /// What one character looks like (GDD §05): the permanent body (sex, build, skin), the coverable head (hair and
    /// its colour, a hat) and the changeable clothes (top, bottom). Indices into a <see cref="WardrobeCatalog"/>.
    /// The crowd's are drawn from the seed on every client (<see cref="OutfitPlanner"/>), never sent.
    /// </summary>
    public readonly struct Outfit : IEquatable<Outfit>
    {
        public readonly Sex Sex;
        public readonly Build Build;
        public readonly int Skin;
        public readonly int Hair;
        public readonly int HairColour;
        public readonly Worn Top;
        public readonly Worn Bottom;
        public readonly Worn Hat;

        public Outfit(Sex sex, Build build, int skin, int hair, int hairColour, Worn top, Worn bottom, Worn hat)
        {
            Sex = sex;
            Build = build;
            Skin = skin;
            Hair = hair;
            HairColour = hairColour;
            Top = top;
            Bottom = bottom;
            Hat = hat;
        }

        /// <summary>The words a watcher might use, for logs: "female, heavy; dark hoodie, light jeans; crew cut; cap".</summary>
        public string Describe(WardrobeCatalog catalog)
        {
            var hat = Hat.IsNone ? "no hat" : $"{Shade(Hat)} {catalog.Hats[Hat.Item].Id}";
            return $"{Sex.ToString().ToLowerInvariant()}, {Build.ToString().ToLowerInvariant()}, {catalog.Skins[Skin].Name}; " +
                   $"{Shade(Top)} {catalog.Tops[Top.Item].Id}, {Shade(Bottom)} {catalog.Bottoms[Bottom.Item].Id}; " +
                   $"{catalog.HairStyles[Hair].Id} ({catalog.HairColours[HairColour].Name}); {hat}";
        }

        private static string Shade(Worn worn) => worn.Tone == Tone.Light ? "light" : "dark";

        public bool Equals(Outfit other) =>
            Sex == other.Sex && Build == other.Build && Skin == other.Skin && Hair == other.Hair &&
            HairColour == other.HairColour && Top.Equals(other.Top) && Bottom.Equals(other.Bottom) && Hat.Equals(other.Hat);

        public override bool Equals(object obj) => obj is Outfit other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(HashCode.Combine(Sex, Build, Skin, Hair, HairColour), Top, Bottom, Hat);
    }
}
