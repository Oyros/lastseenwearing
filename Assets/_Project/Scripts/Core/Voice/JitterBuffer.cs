namespace LastSeenWearing.Core.Voice
{
    /// <summary>
    /// Smooths a voice stream that arrives in packets for an audio callback that wants it sample by sample. It
    /// waits for <c>prebuffer</c> samples before it plays, plays silence and waits again when it runs dry, and
    /// drops the oldest audio when full rather than fall further behind. Thread-safe: the network writes on
    /// the main thread, the audio thread reads.
    /// </summary>
    public sealed class JitterBuffer
    {
        private readonly float[] _ring;
        private readonly int _prebuffer;
        private readonly object _gate = new();
        private int _read;
        private int _count;
        private bool _playing;

        public JitterBuffer(int capacity, int prebuffer)
        {
            _ring = new float[capacity];
            _prebuffer = System.Math.Min(prebuffer, capacity);
        }

        public int Buffered
        {
            get
            {
                lock (_gate)
                {
                    return _count;
                }
            }
        }

        public bool IsPlaying
        {
            get
            {
                lock (_gate)
                {
                    return _playing;
                }
            }
        }

        public void Write(float[] samples, int count)
        {
            lock (_gate)
            {
                for (var i = 0; i < count; i++)
                {
                    if (_count == _ring.Length)
                    {
                        _read = (_read + 1) % _ring.Length; // full: drop the oldest
                        _count--;
                    }

                    _ring[(_read + _count) % _ring.Length] = samples[i];
                    _count++;
                }

                if (_count >= _prebuffer)
                {
                    _playing = true;
                }
            }
        }

        /// <summary>Fills <paramref name="into"/>; silence where there is nothing to play.</summary>
        public void Read(float[] into)
        {
            lock (_gate)
            {
                for (var i = 0; i < into.Length; i++)
                {
                    if (!_playing || _count == 0)
                    {
                        _playing = false;
                        into[i] = 0f;
                        continue;
                    }

                    into[i] = _ring[_read];
                    _read = (_read + 1) % _ring.Length;
                    _count--;
                }
            }
        }
    }
}
