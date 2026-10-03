namespace LastSeenWearing.Core.Randomness
{
    /// <summary>
    /// The game's own PRNG (xorshift64*). Every client must draw the same numbers from the same
    /// seed, so the algorithm is ours rather than <c>System.Random</c>'s, whose sequence is a
    /// runtime detail (CONVENTIONS.md rule 3, ARCHITECTURE "Randomness").
    /// </summary>
    public sealed class SeededRandom
    {
        private const ulong Multiplier = 0x2545F4914F6CDD1DUL;
        private const ulong ZeroStateFallback = 0x9E3779B97F4A7C15UL;

        private ulong _state;

        public SeededRandom(int seed)
        {
            _state = Mix((ulong)(uint)seed);
            if (_state == 0)
            {
                _state = ZeroStateFallback;
            }
        }

        /// <summary>A seed for stream <paramref name="index"/> of <paramref name="seed"/>: <c>f(seed, index)</c>.</summary>
        public static int Derive(int seed, int index)
        {
            return (int)(uint)Mix(((ulong)(uint)seed << 32) | (uint)index);
        }

        public uint NextUInt()
        {
            var x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return (uint)((x * Multiplier) >> 32);
        }

        /// <summary>An int in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            var span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % span);
        }

        /// <summary>A float in [0, 1).</summary>
        public float Value()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * Value();
        }

        // SplitMix64 finaliser: spreads nearby seeds far apart.
        private static ulong Mix(ulong z)
        {
            z += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
