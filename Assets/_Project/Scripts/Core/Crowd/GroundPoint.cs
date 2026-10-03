namespace LastSeenWearing.Core.Crowd
{
    /// <summary>A point on the festival ground, metres. Height is the scene's business, not Core's.</summary>
    public readonly struct GroundPoint
    {
        public float X { get; }
        public float Z { get; }

        public GroundPoint(float x, float z)
        {
            X = x;
            Z = z;
        }

        public GroundPoint Offset(float dx, float dz)
        {
            return new GroundPoint(X + dx, Z + dz);
        }

        public static float Distance(GroundPoint a, GroundPoint b)
        {
            var dx = b.X - a.X;
            var dz = b.Z - a.Z;
            return (float)System.Math.Sqrt(dx * dx + dz * dz);
        }

        public override string ToString()
        {
            return $"({X:F2}, {Z:F2})";
        }
    }
}
