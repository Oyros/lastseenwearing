namespace LastSeenWearing.Core.Voice
{
    /// <summary>
    /// Whether the radio is on air: it is from a packet until <c>holdSeconds</c> pass without one (GDD §04.2:
    /// the radio light shows while the officers' radios talk). Server side; the result is broadcast.
    /// </summary>
    public sealed class OnAir
    {
        private readonly double _holdSeconds;
        private double _lastPacket = double.NegativeInfinity;

        public OnAir(double holdSeconds)
        {
            _holdSeconds = holdSeconds;
        }

        public void Packet(double now) => _lastPacket = now;

        public bool IsOnAir(double now) => now - _lastPacket <= _holdSeconds;
    }
}
