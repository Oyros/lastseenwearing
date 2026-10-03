using LastSeenWearing.Core.Crowd;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Crowd
{
    /// <summary>
    /// One crowd NPC on this client. Untouched, it stands wherever its <see cref="NpcSchedule"/> says
    /// it is at the current crowd time (D-019) — nothing is sent for it. Once a player affects it, the
    /// server takes it over: the server moves it and every client follows the poses it sends (D-005).
    /// Driven by <see cref="CrowdSpawner"/>, which ticks every agent in one loop.
    /// </summary>
    public sealed class CrowdAgent : MonoBehaviour
    {
        /// <summary>Animator parameters of the crowd controller (built by CrowdAnimatorBuilder).</summary>
        public const string StyleParameter = "Style";
        public const string PhaseParameter = "Phase";
        public const string WalkingParameter = "Walking";

        private static readonly int StyleId = Animator.StringToHash(StyleParameter);
        private static readonly int PhaseId = Animator.StringToHash(PhaseParameter);
        private static readonly int WalkingId = Animator.StringToHash(WalkingParameter);

        private NpcSchedule _schedule;
        private float _groundY;
        private Animator _animator;
        private float _strideLength;
        private double _lastWalked = -1d;

        // Server: an active shove.
        private Vector3 _bumpFrom;
        private Vector3 _bumpTo;
        private float _bumpElapsed;
        private float _bumpDuration;
        private bool _bumping;

        // Client: the latest pose the server sent.
        private Vector3 _syncedPosition;
        private Quaternion _syncedRotation;

        public int Index { get; private set; }
        public bool IsTakenOver { get; private set; }

        /// <summary>Server: the pose changed since it was last sent.</summary>
        public bool PoseDirty { get; set; }

        public void Begin(int index, NpcSchedule schedule, float groundY, BaseWalk walk, float strideLength)
        {
            Index = index;
            _schedule = schedule;
            _groundY = groundY;
            _strideLength = strideLength;
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                _animator.SetFloat(StyleId, (float)walk);
            }
        }

        public void FollowSchedule(double crowdTime)
        {
            var at = _schedule.Evaluate(crowdTime, out var heading, out var walked);
            transform.SetPositionAndRotation(new Vector3(at.X, _groundY, at.Z), Quaternion.Euler(0f, heading, 0f));

            // The cycle runs on distance, not time (D-022): one stride per cycle, so a planted foot moves
            // exactly as far back as the body moves forward — at any speed — and stops when the body stops.
            if (_animator != null)
            {
                _animator.SetFloat(PhaseId, (float)(walked / _strideLength % 1d));
                // Lingering at a waypoint: the distance stands still, and the body goes to its idle.
                _animator.SetBool(WalkingId, walked > _lastWalked);
            }

            _lastWalked = walked;
        }

        /// <summary>Server: take the NPC off its schedule and shove it away from <paramref name="from"/>.</summary>
        public void Bump(Vector3 from, float distance, float duration)
        {
            IsTakenOver = true;
            var away = transform.position - from;
            away.y = 0f;
            if (away.sqrMagnitude < 1e-4f)
            {
                away = transform.forward;
            }

            _bumpFrom = transform.position;
            _bumpTo = transform.position + away.normalized * distance;
            _bumpElapsed = 0f;
            _bumpDuration = duration;
            _bumping = true;
        }

        /// <summary>Client: the server says this NPC is taken over and stands here.</summary>
        public void ReceivePose(Vector3 position, float heading)
        {
            if (!IsTakenOver)
            {
                IsTakenOver = true;
                transform.SetPositionAndRotation(position, Quaternion.Euler(0f, heading, 0f));
            }

            _syncedPosition = position;
            _syncedRotation = Quaternion.Euler(0f, heading, 0f);
        }

        public void TickServer(float deltaTime)
        {
            if (!_bumping)
            {
                return;
            }

            _bumpElapsed += deltaTime;
            var t = Mathf.Clamp01(_bumpElapsed / _bumpDuration);
            transform.position = Vector3.Lerp(_bumpFrom, _bumpTo, Mathf.SmoothStep(0f, 1f, t));
            PoseDirty = true;
            _bumping = t < 1f;
        }

        /// <summary>Client: ease toward the last received pose, at the rate poses arrive.</summary>
        public void TickClient(float deltaTime, float syncRate)
        {
            var k = 1f - Mathf.Exp(-syncRate * deltaTime);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, _syncedPosition, k),
                Quaternion.Slerp(transform.rotation, _syncedRotation, k));
        }
    }
}
