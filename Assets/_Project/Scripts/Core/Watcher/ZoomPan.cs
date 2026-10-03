using System;

namespace LastSeenWearing.Core.Watcher
{
    /// <summary>
    /// The zoom camera's aim (GDD §04.1: "slow pan + zoom", P1.17a): yaw and pitch around the camera's mounted
    /// direction within a range, and a magnification from 1× up to what the narrowest field of view gives. Input
    /// moves a target; the camera follows it no faster than the configured rates, so it always pans and zooms
    /// slowly, and pans finer the further it is zoomed in. Local to the Watcher's client, like
    /// <see cref="FeedSwitcher"/>.
    /// </summary>
    public sealed class ZoomPan
    {
        private readonly float _baseFieldOfView;
        private readonly float _maxOctaves;
        private readonly float _yawRange;
        private readonly float _pitchRange;
        private readonly float _panDegreesPerSecond;
        private readonly float _zoomOctavesPerSecond;
        private float _targetYaw;
        private float _targetPitch;
        private float _targetOctaves;

        /// <param name="baseFieldOfView">Vertical degrees unzoomed.</param>
        /// <param name="minFieldOfView">Vertical degrees fully zoomed.</param>
        public ZoomPan(float baseFieldOfView, float minFieldOfView, float yawRange, float pitchRange,
            float panDegreesPerSecond, float zoomOctavesPerSecond)
        {
            _baseFieldOfView = baseFieldOfView;
            _maxOctaves = (float)Math.Log(MagnificationOf(baseFieldOfView, minFieldOfView), 2d);
            _yawRange = yawRange;
            _pitchRange = pitchRange;
            _panDegreesPerSecond = panDegreesPerSecond;
            _zoomOctavesPerSecond = zoomOctavesPerSecond;
        }

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        /// <summary>Zoom in doublings: 0 unzoomed, 1 = 2×.</summary>
        public float Octaves { get; private set; }

        public float Magnification => (float)Math.Pow(2d, Octaves);

        /// <summary>The vertical field of view that gives <see cref="Magnification"/>.</summary>
        public float FieldOfView => (float)(2d * Math.Atan(Math.Tan(Radians(_baseFieldOfView) / 2d) / Magnification) * 180d / Math.PI);

        /// <summary>How many times larger <paramref name="narrow"/> shows things than <paramref name="wide"/>.</summary>
        public static float MagnificationOf(float wide, float narrow) =>
            (float)(Math.Tan(Radians(wide) / 2d) / Math.Tan(Radians(narrow) / 2d));

        /// <summary>Moves the aim's target by degrees (already scaled by the input device).</summary>
        public void PanBy(float yawDegrees, float pitchDegrees)
        {
            // The further in, the finer: a degree of input is a degree of the unzoomed view.
            _targetYaw = Clamp(_targetYaw + yawDegrees / Magnification, _yawRange);
            _targetPitch = Clamp(_targetPitch + pitchDegrees / Magnification, _pitchRange);
        }

        /// <summary>Moves the zoom's target by doublings: positive zooms in.</summary>
        public void ZoomBy(float octaves)
        {
            _targetOctaves = Math.Clamp(_targetOctaves + octaves, 0f, _maxOctaves);
        }

        /// <summary>One tick: the camera follows its target at the slow rates.</summary>
        public void Advance(float deltaTime)
        {
            var pan = _panDegreesPerSecond / Magnification * deltaTime;
            Yaw = Toward(Yaw, _targetYaw, pan);
            Pitch = Toward(Pitch, _targetPitch, pan);
            Octaves = Toward(Octaves, _targetOctaves, _zoomOctavesPerSecond * deltaTime);
        }

        private static float Clamp(float value, float range) => Math.Clamp(value, -range, range);

        private static float Toward(float current, float target, float maxDelta) =>
            Math.Abs(target - current) <= maxDelta ? target : current + Math.Sign(target - current) * maxDelta;

        private static double Radians(float degrees) => degrees * Math.PI / 180d;
    }
}
