using System.Collections.Generic;
using LastSeenWearing.Core.Roles;

namespace LastSeenWearing.Core.Disguise
{
    /// <summary>Who does what at a tent (GDD §03, §05, P2.02).</summary>
    public static class TentRules
    {
        /// <summary>Only the fugitive changes.</summary>
        public static bool MayChange(Role role) => role == Role.Fugitive;

        /// <summary>Only the plainclothes goes in to see what is missing (GDD §03).</summary>
        public static bool MayInspect(Role role) => role == Role.Plainclothes;

        /// <summary>The rail's garments that are gone (bit i of <paramref name="takenMask"/>: item i).</summary>
        public static List<StockItem> Missing(StockItem[] rail, int takenMask)
        {
            var missing = new List<StockItem>();
            for (var i = 0; i < rail.Length; i++)
            {
                if ((takenMask & (1 << i)) != 0)
                {
                    missing.Add(rail[i]);
                }
            }

            return missing;
        }
    }
}
