namespace LastSeenWearing.Core.Voice
{
    /// <summary>
    /// Brings a microphone's rate to the radio's (D-032). Downsampling averages each output sample's span of
    /// input — a box filter, enough against aliasing for a voice band that ends at 3.4 kHz; upsampling
    /// interpolates linearly.
    /// </summary>
    public static class Resampler
    {
        /// <summary>Output samples <paramref name="inputCount"/> input samples become (rounded down).</summary>
        public static int OutputCount(int inputCount, int fromRate, int toRate) => (int)((long)inputCount * toRate / fromRate);

        public static int Resample(float[] input, int inputCount, int fromRate, float[] output, int toRate)
        {
            var count = OutputCount(inputCount, fromRate, toRate);
            var step = fromRate / (double)toRate;
            for (var o = 0; o < count; o++)
            {
                var start = o * step;
                if (step <= 1d)
                {
                    var i = (int)start;
                    var t = (float)(start - i);
                    var next = System.Math.Min(i + 1, inputCount - 1);
                    output[o] = input[i] + (input[next] - input[i]) * t;
                    continue;
                }

                var first = (int)start;
                var last = System.Math.Min((int)System.Math.Ceiling(start + step), inputCount);
                var sum = 0f;
                for (var i = first; i < last; i++)
                {
                    sum += input[i];
                }

                output[o] = sum / System.Math.Max(1, last - first);
            }

            return count;
        }
    }
}
