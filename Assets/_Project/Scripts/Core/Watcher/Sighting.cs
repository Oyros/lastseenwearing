using System;
using LastSeenWearing.Core.Wardrobe;

namespace LastSeenWearing.Core.Watcher
{
    /// <summary>Where the Watcher's "last seen" came from (P1.20).</summary>
    public enum SightingSource : byte
    {
        /// <summary>The witness at the start of the round: true, and already a little old.</summary>
        Witness,

        /// <summary>The Watcher's own mark on a feed: whoever they clicked — never checked by the server.</summary>
        Mark,
    }

    /// <summary>
    /// What the fugitive was last seen wearing and when (GDD §04.1, glossary "Last seen"): the clothes only — the
    /// body is the composite's — and the server time of the sighting, so its age counts up from there.
    /// </summary>
    public readonly struct Sighting
    {
        public readonly Worn Top;
        public readonly Worn Bottom;
        public readonly Worn Hat;
        public readonly double At;
        public readonly SightingSource Source;

        public Sighting(Worn top, Worn bottom, Worn hat, double at, SightingSource source)
        {
            Top = top;
            Bottom = bottom;
            Hat = hat;
            At = at;
            Source = source;
        }

        public static Sighting Of(Outfit outfit, double at, SightingSource source) => new(outfit.Top, outfit.Bottom, outfit.Hat, at, source);

        /// <summary>Seconds since the sighting; never negative.</summary>
        public double Age(double now) => Math.Max(0d, now - At);

        /// <summary>Whole minutes and seconds of an age, for "2:31 ago".</summary>
        public static (int Minutes, int Seconds) Clock(double age)
        {
            var whole = (int)Math.Floor(age);
            return (whole / 60, whole % 60);
        }
    }
}
