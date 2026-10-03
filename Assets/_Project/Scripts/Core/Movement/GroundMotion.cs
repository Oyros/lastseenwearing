using System;
using LastSeenWearing.Core.Crowd;

namespace LastSeenWearing.Core.Movement
{
    /// <summary>
    /// Walking on the festival ground from a stick or WASD, relative to where the camera looks (P1.11).
    /// Ground-plane metres; heading in degrees, 0 = +Z, clockwise (as <see cref="NpcSchedule"/>).
    /// </summary>
    public static class GroundMotion
    {
        /// <summary>
        /// The velocity a player asks for: input (x right, y forward; clamped to length 1) turned by the
        /// camera's yaw, at <paramref name="speed"/>. Half a stick is half the speed; full WASD is full speed.
        /// </summary>
        public static GroundPoint DesiredVelocity(float inputX, float inputY, float cameraYawDegrees, float speed)
        {
            var length = Math.Sqrt(inputX * inputX + inputY * inputY);
            if (length > 1d)
            {
                inputX = (float)(inputX / length);
                inputY = (float)(inputY / length);
            }

            var yaw = cameraYawDegrees * Math.PI / 180d;
            var sin = Math.Sin(yaw);
            var cos = Math.Cos(yaw);
            // Forward is (sin, cos), right is (cos, −sin) on (x, z).
            var x = inputY * sin + inputX * cos;
            var z = inputY * cos - inputX * sin;
            return new GroundPoint((float)(x * speed), (float)(z * speed));
        }

        /// <summary><paramref name="current"/> moved toward <paramref name="target"/> by at most <paramref name="maxDelta"/>.</summary>
        public static GroundPoint StepToward(GroundPoint current, GroundPoint target, float maxDelta)
        {
            var dx = target.X - current.X;
            var dz = target.Z - current.Z;
            var distance = Math.Sqrt(dx * dx + dz * dz);
            if (distance <= maxDelta || distance < 1e-9)
            {
                return target;
            }

            var k = maxDelta / distance;
            return new GroundPoint((float)(current.X + dx * k), (float)(current.Z + dz * k));
        }
    }
}
