using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Crowd
{
    /// <summary>
    /// Raises the crowd from one seed on every client (D-005). The host picks and logs the seed;
    /// it reaches clients with this object's spawn, and each client builds the same plans with
    /// <see cref="CrowdPlanner"/> and walks them locally. No NPC is a network object.
    /// </summary>
    public sealed class CrowdSpawner : NetworkBehaviour
    {
        [SerializeField] private CrowdConfig _config;
        [SerializeField] private CrowdAgent _agentPrefab;
        [SerializeField] private Transform _waypointRoot;

        private readonly NetworkVariable<int> _seed = new();
        private CrowdAgent[] _agents;

        public int Seed => _seed.Value;
        public CrowdAgent[] Agents => _agents;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _seed.Value = System.Environment.TickCount;
                Debug.Log($"[CrowdSpawner] Crowd seed {_seed.Value}.");
            }

            Raise(_seed.Value);
        }

        public override void OnNetworkDespawn()
        {
            if (_agents == null)
            {
                return;
            }

            foreach (var agent in _agents)
            {
                if (agent != null)
                {
                    Destroy(agent.gameObject);
                }
            }

            _agents = null;
        }

        private void Raise(int seed)
        {
            var waypoints = new Vector3[_waypointRoot.childCount];
            for (var i = 0; i < waypoints.Length; i++)
            {
                waypoints[i] = _waypointRoot.GetChild(i).position;
            }

            var settings = new CrowdPlanSettings(_config.NpcCount, waypoints.Length, _config.RouteLength,
                _config.WaypointSpread, _config.DwellMin, _config.DwellMax);
            var plans = CrowdPlanner.Build(seed, settings);

            _agents = new CrowdAgent[plans.Length];
            for (var i = 0; i < plans.Length; i++)
            {
                _agents[i] = Instantiate(_agentPrefab, transform);
                _agents[i].name = $"Npc_{i:000}";
                _agents[i].Begin(plans[i], waypoints, _config.WalkSpeed);
            }
        }
    }
}
