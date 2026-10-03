using LastSeenWearing.Core.Crowd;
using UnityEngine;
using UnityEngine.AI;

namespace LastSeenWearing.Gameplay.Crowd
{
    /// <summary>
    /// Walks one NPC through its <see cref="NpcPlan"/> on the NavMesh: to a leg's waypoint, linger,
    /// next leg, loop. Local avoidance is off — it is not deterministic, and every client must walk
    /// the same crowd (P1.01; P1.02 measures what drift remains).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CrowdAgent : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private NpcPlan _plan;
        private Vector3[] _waypoints;
        private int _leg;
        private float _dwellRemaining;
        private bool _dwelling;

        /// <summary>The leg being walked or lingered at; the same on every client for the same seed.</summary>
        public int Leg => _leg;

        public void Begin(NpcPlan plan, Vector3[] waypoints, float walkSpeed)
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = walkSpeed;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            _agent.autoBraking = true;

            _plan = plan;
            _waypoints = waypoints;
            _agent.Warp(waypoints[plan.SpawnWaypoint] + new Vector3(plan.SpawnOffsetX, 0f, plan.SpawnOffsetZ));
            _leg = 0;
            SetDestination();
        }

        private void Update()
        {
            if (_plan == null)
            {
                return;
            }

            if (_dwelling)
            {
                _dwellRemaining -= Time.deltaTime;
                if (_dwellRemaining <= 0f)
                {
                    _dwelling = false;
                    _leg = (_leg + 1) % _plan.Route.Length;
                    SetDestination();
                }

                return;
            }

            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                _dwelling = true;
                _dwellRemaining = _plan.Route[_leg].DwellSeconds;
            }
        }

        private void SetDestination()
        {
            var leg = _plan.Route[_leg];
            _agent.SetDestination(_waypoints[leg.Waypoint] + new Vector3(leg.OffsetX, 0f, leg.OffsetZ));
        }
    }
}
