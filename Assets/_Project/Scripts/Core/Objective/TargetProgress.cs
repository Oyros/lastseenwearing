using UnityEngine;

namespace LastSeenWearing.Core.Objective
{
    /// <summary>Which exit the last target opens (GDD §04.4: "decided by the last target").</summary>
    public enum ExitRule : byte
    {
        Farthest,
        Nearest,
    }

    /// <summary>
    /// The fugitive's targets in one round (GDD §04.4, P1.22): any of the layout's targets, each counted once; when
    /// enough are done the last one picks the exit. Pure state — the server holds it.
    /// </summary>
    public sealed class TargetProgress
    {
        public const int NoExit = -1;

        private readonly bool[] _done;

        public TargetProgress(int targets, int needed)
        {
            _done = new bool[targets];
            Needed = Mathf.Min(needed, targets);
        }

        public int Needed { get; }
        public int Count { get; private set; }
        public int OpenExit { get; private set; } = NoExit;
        public bool Complete => Count >= Needed;

        public bool IsDone(int target) => target >= 0 && target < _done.Length && _done[target];

        /// <summary>Bit i: target i is done.</summary>
        public int Mask
        {
            get
            {
                var mask = 0;
                for (var i = 0; i < _done.Length; i++)
                {
                    mask |= _done[i] ? 1 << i : 0;
                }

                return mask;
            }
        }

        /// <summary>
        /// Counts <paramref name="target"/> if it is new and the way out is not open yet; the one that makes it enough
        /// opens an exit by <paramref name="rule"/>. False if it did not count.
        /// </summary>
        public bool Finish(int target, Vector3 at, Vector3[] exits, ExitRule rule)
        {
            if (Complete || target < 0 || target >= _done.Length || _done[target])
            {
                return false;
            }

            _done[target] = true;
            Count++;
            if (Complete)
            {
                OpenExit = ExitFor(at, exits, rule);
            }

            return true;
        }

        public static int ExitFor(Vector3 lastTarget, Vector3[] exits, ExitRule rule)
        {
            var best = NoExit;
            var bestDistance = 0f;
            for (var i = 0; i < exits.Length; i++)
            {
                var distance = (exits[i] - lastTarget).sqrMagnitude;
                if (best == NoExit || (rule == ExitRule.Farthest ? distance > bestDistance : distance < bestDistance))
                {
                    best = i;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
