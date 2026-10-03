namespace LastSeenWearing.Core.Watcher
{
    /// <summary>
    /// Which camera each of the Watcher's monitors shows (GDD §03, §04.1). Switching a monitor to another
    /// camera costs <c>switchSeconds</c> of static before the new picture arrives; asking for the camera a monitor
    /// already shows or is already switching to costs nothing, and a new choice mid-switch starts the wait again.
    /// Local to the Watcher's client — what they look at is no one else's truth.
    /// </summary>
    public sealed class FeedSwitcher
    {
        public const int MonitorCount = 2;
        public const int NoCamera = -1;

        private readonly float _switchSeconds;
        private readonly int[] _target = new int[MonitorCount];
        private readonly double[] _readyAt = new double[MonitorCount];

        public FeedSwitcher(float switchSeconds, int cameraCount)
        {
            _switchSeconds = switchSeconds;
            CameraCount = cameraCount;
            for (var monitor = 0; monitor < MonitorCount; monitor++)
            {
                // The wall starts live: monitor n on camera n, when there is one.
                _target[monitor] = monitor < cameraCount ? monitor : NoCamera;
            }
        }

        public int CameraCount { get; }

        /// <summary>The camera the monitor shows or is switching to.</summary>
        public int Target(int monitor) => _target[monitor];

        public bool IsSwitching(int monitor, double now) => now < _readyAt[monitor];

        /// <summary>The camera the monitor shows now, or <see cref="NoCamera"/> while it switches.</summary>
        public int Showing(int monitor, double now) => IsSwitching(monitor, now) ? NoCamera : _target[monitor];

        /// <summary>Sends <paramref name="camera"/> to <paramref name="monitor"/>. False if nothing changed.</summary>
        public bool Select(int monitor, int camera, double now)
        {
            if (camera < 0 || camera >= CameraCount || camera == _target[monitor])
            {
                return false;
            }

            _target[monitor] = camera;
            _readyAt[monitor] = now + _switchSeconds;
            return true;
        }
    }
}
