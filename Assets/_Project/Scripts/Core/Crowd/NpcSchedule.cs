using System;
using System.Collections.Generic;

namespace LastSeenWearing.Core.Crowd
{
    /// <summary>
    /// Where an NPC is at a given crowd time (D-019). Its <see cref="NpcPlan"/> becomes a timeline:
    /// walk a leg's path at a fixed speed, linger, next leg, looping the route. Position is a pure
    /// function of time, so every client agrees on it without a single byte of sync, and a player who
    /// joins late reads the same answer as one who was there from the start.
    /// Paths come from the caller (the NavMesh in Gameplay) and are asked for once per leg, in order.
    /// </summary>
    public sealed class NpcSchedule
    {
        private readonly NpcPlan _plan;
        private readonly GroundPoint[] _waypoints;
        private readonly float _speed;
        private readonly Func<GroundPoint, GroundPoint, GroundPoint[]> _findPath;
        private readonly GroundPoint _spawn;
        private readonly List<Leg> _legs = new();
        private int _cursor;

        public NpcSchedule(NpcPlan plan, GroundPoint[] waypoints, float speed,
            Func<GroundPoint, GroundPoint, GroundPoint[]> findPath)
        {
            _plan = plan;
            _waypoints = waypoints;
            _speed = Math.Max(speed, 0.01f);
            _findPath = findPath;
            _spawn = waypoints[plan.SpawnWaypoint].Offset(plan.SpawnOffsetX, plan.SpawnOffsetZ);
        }

        /// <summary>Position and heading (degrees, 0 = +Z, clockwise) at <paramref name="time"/> seconds since the crowd started.</summary>
        public GroundPoint Evaluate(double time, out float headingDegrees)
        {
            if (time <= 0d)
            {
                headingDegrees = 0f;
                return _spawn;
            }

            ExtendTo(time);
            while (_cursor > 0 && time < _legs[_cursor].Start)
            {
                _cursor--;
            }

            while (time >= _legs[_cursor].End)
            {
                _cursor++;
            }

            return _legs[_cursor].At(time, _speed, out headingDegrees);
        }

        private void ExtendTo(double time)
        {
            while (_legs.Count == 0 || _legs[_legs.Count - 1].End <= time)
            {
                var index = _legs.Count;
                var from = index == 0 ? _spawn : _legs[index - 1].Last;
                var start = index == 0 ? 0d : _legs[index - 1].End;
                var leg = _plan.Route[index % _plan.Route.Length];
                var to = _waypoints[leg.Waypoint].Offset(leg.OffsetX, leg.OffsetZ);

                var path = _findPath(from, to);
                if (path == null || path.Length == 0)
                {
                    path = new[] { from, to };
                }

                _legs.Add(new Leg(path, start, _speed, leg.DwellSeconds));
            }
        }

        private sealed class Leg
        {
            private readonly GroundPoint[] _path;
            private readonly double[] _distanceAt;
            private readonly double _walkEnd;

            public double Start { get; }
            public double End { get; }
            public GroundPoint Last => _path[_path.Length - 1];

            public Leg(GroundPoint[] path, double start, float speed, float dwell)
            {
                _path = path;
                _distanceAt = new double[path.Length];
                for (var i = 1; i < path.Length; i++)
                {
                    _distanceAt[i] = _distanceAt[i - 1] + GroundPoint.Distance(path[i - 1], path[i]);
                }

                Start = start;
                _walkEnd = start + _distanceAt[path.Length - 1] / speed;
                End = _walkEnd + dwell;
            }

            public GroundPoint At(double time, float speed, out float headingDegrees)
            {
                var distance = Math.Min((time - Start) * speed, _distanceAt[_path.Length - 1]);
                var segment = 1;
                while (segment < _path.Length - 1 && distance > _distanceAt[segment])
                {
                    segment++;
                }

                if (_path.Length < 2)
                {
                    headingDegrees = 0f;
                    return _path[0];
                }

                var a = _path[segment - 1];
                var b = _path[segment];
                var length = _distanceAt[segment] - _distanceAt[segment - 1];
                var t = length > 0d ? (distance - _distanceAt[segment - 1]) / length : 1d;
                headingDegrees = (float)(Math.Atan2(b.X - a.X, b.Z - a.Z) * (180d / Math.PI));
                return new GroundPoint((float)(a.X + (b.X - a.X) * t), (float)(a.Z + (b.Z - a.Z) * t));
            }
        }
    }
}
