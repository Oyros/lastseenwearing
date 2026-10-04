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
        /// <summary>Animator parameters of the crowd controller — see <see cref="WalkCycle"/>.</summary>
        public const string StyleParameter = WalkCycle.StyleParameter;
        public const string PhaseParameter = WalkCycle.PhaseParameter;
        public const string WalkingParameter = WalkCycle.WalkingParameter;
        public const float IdleBlendSeconds = WalkCycle.IdleBlendSeconds;

        public static string SideParameter(WalkTrait trait) => WalkCycle.SideParameter(trait);

        private NpcSchedule _schedule;
        private float _groundY;
        private WalkCycle _walk;

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
            _walk = new WalkCycle(GetComponentInChildren<Animator>(), strideLength);
            _walk.SetBase(walk);
        }

        /// <summary>A walk trait's signed strength (see <see cref="WalkCycle.SetTrait"/>).</summary>
        public void SetTrait(WalkTrait trait, float signedStrength)
        {
            _walk.SetTrait(trait, signedStrength);
        }

        public void FollowSchedule(double crowdTime, float deltaTime)
        {
            var at = _schedule.Evaluate(crowdTime, out var heading, out var walked);
            transform.SetPositionAndRotation(new Vector3(at.X, _groundY, at.Z), Quaternion.Euler(0f, heading, 0f));

            // The cycle runs on distance (D-022); lingering at a waypoint, the distance stands still and the
            // body goes to its idle, taking its traits with it.
            _walk.Advance(walked, deltaTime);
        }

        /// <summary>A one-shot over the walk (P1.25): an NPC cuffed by mistake, for now.</summary>
        public void Act(BodyAction action) => _walk.PlayAction(action);

        /// <summary>How long a one-shot runs on this body.</summary>
        public float ActionLength(BodyAction action) => _walk.ActionLength(action);

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
