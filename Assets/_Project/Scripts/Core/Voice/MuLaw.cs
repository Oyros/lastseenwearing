namespace LastSeenWearing.Core.Voice
{
    /// <summary>
    /// G.711 µ-law: one byte per sample, telephone quality — the radio's codec (D-032). Samples are floats in
    /// [-1, 1]; out-of-range input is clipped.
    /// </summary>
    public static class MuLaw
    {
        private const int Bias = 0x84;
        private const int Clip = 32635;

        public static byte Encode(float sample)
        {
            var pcm = (int)(System.Math.Clamp(sample, -1f, 1f) * short.MaxValue);
            var sign = pcm < 0 ? 0x80 : 0;
            if (pcm < 0)
            {
                pcm = -pcm;
            }

            pcm = System.Math.Min(pcm, Clip) + Bias;
            var exponent = 7;
            for (var mask = 0x4000; (pcm & mask) == 0 && exponent > 0; mask >>= 1)
            {
                exponent--;
            }

            var mantissa = (pcm >> (exponent + 3)) & 0x0F;
            return (byte)~(sign | (exponent << 4) | mantissa);
        }

        public static float Decode(byte value)
        {
            var u = ~value & 0xFF;
            var sign = u & 0x80;
            var exponent = (u >> 4) & 0x07;
            var mantissa = u & 0x0F;
            var magnitude = (((mantissa << 3) + Bias) << exponent) - Bias;
            return (sign != 0 ? -magnitude : magnitude) / (float)short.MaxValue;
        }

        public static void Encode(float[] samples, int count, byte[] into)
        {
            for (var i = 0; i < count; i++)
            {
                into[i] = Encode(samples[i]);
            }
        }

        public static void Decode(byte[] bytes, int count, float[] into)
        {
            for (var i = 0; i < count; i++)
            {
                into[i] = Decode(bytes[i]);
            }
        }
    }
}
