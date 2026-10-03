using System.Collections.Generic;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace LastSeenWearing.Gameplay.Crowd
{
    /// <summary>
    /// The crowd on every client (D-005, D-019). The host picks the seed and the crowd's start on the
    /// server clock; both reach clients with this object's spawn, and from then on every untouched NPC
    /// is <c>f(seed, index, server time)</c> — computed locally, never sent. Only NPCs a player has
    /// affected are taken over by the server, and only their poses cross the network, at
    /// <see cref="CrowdConfig.TakenOverSyncRate"/>. Bytes sent for the crowd are logged each minute.
    /// </summary>
    public sealed class CrowdSpawner : NetworkBehaviour
    {
        private const string PosesMessage = "LSW.CrowdPoses";
        private const float LogIntervalSeconds = 60f;

        [SerializeField] private CrowdConfig _config;
        [SerializeField] private CrowdAgent _agentPrefab;
        [SerializeField] private Transform _waypointRoot;

        private readonly NetworkVariable<int> _seed = new();
        private readonly NetworkVariable<double> _startTime = new();
        private readonly List<CrowdAgent> _dirty = new();
        private readonly List<ulong> _recipients = new();

        private CrowdAgent[] _agents;
        private float _syncTimer;
        private float _logTimer;
        private int _logMinute;
        private long _bytesThisMinute;

        public int Seed => _seed.Value;
        public CrowdAgent[] Agents => _agents;
        public double CrowdTime => NetworkManager.ServerTime.Time - _startTime.Value;
        public long BytesLastMinute { get; private set; }

        public int TakenOverCount
        {
            get
            {
                var count = 0;
                if (_agents != null)
                {
                    foreach (var agent in _agents)
                    {
                        count += agent.IsTakenOver ? 1 : 0;
                    }
                }

                return count;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _seed.Value = System.Environment.TickCount;
                _startTime.Value = NetworkManager.ServerTime.Time;
                Debug.Log($"[CrowdSpawner] Crowd seed {_seed.Value}, start {_startTime.Value:F3} s server time.");
            }
            else
            {
                NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(PosesMessage, OnPosesReceived);
            }

            Raise(_seed.Value);

            if (!IsServer)
            {
                RequestTakenOverRpc();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer && NetworkManager.CustomMessagingManager != null)
            {
                NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(PosesMessage);
            }

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

        /// <summary>Shove the NPC nearest to <paramref name="from"/>. Any client may ask; the server decides.</summary>
        public void BumpNearest(Vector3 from)
        {
            if (IsServer)
            {
                BumpNearestOnServer(from);
            }
            else
            {
                BumpNearestRpc(from);
            }
        }

        [Rpc(SendTo.Server)]
        private void BumpNearestRpc(Vector3 from)
        {
            BumpNearestOnServer(from);
        }

        // A late joiner asks once for every NPC already taken over; the rest it computes itself.
        [Rpc(SendTo.Server)]
        private void RequestTakenOverRpc(RpcParams rpcParams = default)
        {
            var all = new List<CrowdAgent>();
            foreach (var agent in _agents)
            {
                if (agent.IsTakenOver)
                {
                    all.Add(agent);
                }
            }

            if (all.Count > 0)
            {
                _recipients.Clear();
                _recipients.Add(rpcParams.Receive.SenderClientId);
                SendPoses(all, _recipients);
            }
        }

        private void Update()
        {
            if (_agents == null || !IsSpawned)
            {
                return;
            }

            var crowdTime = CrowdTime;
            var deltaTime = Time.deltaTime;
            foreach (var agent in _agents)
            {
                if (!agent.IsTakenOver)
                {
                    agent.FollowSchedule(crowdTime);
                }
                else if (IsServer)
                {
                    agent.TickServer(deltaTime);
                }
                else
                {
                    agent.TickClient(deltaTime, _config.TakenOverSyncRate);
                }
            }

            if (IsServer)
            {
                TickSync(deltaTime);
                TickLog(deltaTime);
            }
        }

        private void BumpNearestOnServer(Vector3 from)
        {
            CrowdAgent nearest = null;
            var best = float.MaxValue;
            foreach (var agent in _agents)
            {
                var distance = (agent.transform.position - from).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = agent;
                }
            }

            if (nearest != null)
            {
                nearest.Bump(from, _config.BumpDistance, _config.BumpDuration);
                nearest.PoseDirty = true;
            }
        }

        private void TickSync(float deltaTime)
        {
            _syncTimer += deltaTime;
            if (_syncTimer < 1f / _config.TakenOverSyncRate)
            {
                return;
            }

            _syncTimer = 0f;
            _dirty.Clear();
            foreach (var agent in _agents)
            {
                if (agent.PoseDirty)
                {
                    _dirty.Add(agent);
                    agent.PoseDirty = false;
                }
            }

            if (_dirty.Count == 0)
            {
                return;
            }

            _recipients.Clear();
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                if (clientId != NetworkManager.ServerClientId)
                {
                    _recipients.Add(clientId);
                }
            }

            if (_recipients.Count > 0)
            {
                SendPoses(_dirty, _recipients);
            }
        }

        private void SendPoses(List<CrowdAgent> agents, List<ulong> recipients)
        {
            // Per NPC: index (2) + x, z (8) + heading (4).
            using var writer = new FastBufferWriter(2 + agents.Count * 14, Allocator.Temp);
            writer.WriteValueSafe((ushort)agents.Count);
            foreach (var agent in agents)
            {
                var position = agent.transform.position;
                writer.WriteValueSafe((ushort)agent.Index);
                writer.WriteValueSafe(position.x);
                writer.WriteValueSafe(position.z);
                writer.WriteValueSafe(agent.transform.eulerAngles.y);
            }

            NetworkManager.CustomMessagingManager.SendNamedMessage(PosesMessage, recipients, writer, NetworkDelivery.ReliableSequenced);
            _bytesThisMinute += (long)writer.Length * recipients.Count;
        }

        private void OnPosesReceived(ulong senderId, FastBufferReader reader)
        {
            if (_agents == null)
            {
                return;
            }

            reader.ReadValueSafe(out ushort count);
            for (var i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out ushort index);
                reader.ReadValueSafe(out float x);
                reader.ReadValueSafe(out float z);
                reader.ReadValueSafe(out float heading);
                if (index < _agents.Length)
                {
                    _agents[index].ReceivePose(new Vector3(x, transform.position.y, z), heading);
                }
            }
        }

        private void TickLog(float deltaTime)
        {
            _logTimer += deltaTime;
            if (_logTimer < LogIntervalSeconds)
            {
                return;
            }

            _logTimer = 0f;
            _logMinute++;
            BytesLastMinute = _bytesThisMinute;
            Debug.Log($"[CrowdSync] Minute {_logMinute}: {_bytesThisMinute} B of crowd poses to " +
                      $"{NetworkManager.ConnectedClientsIds.Count - 1} client(s); {TakenOverCount} NPC(s) taken over.");
            _bytesThisMinute = 0;
        }

        private void Raise(int seed)
        {
            var groundY = transform.position.y;
            var waypoints = new GroundPoint[_waypointRoot.childCount];
            for (var i = 0; i < waypoints.Length; i++)
            {
                var position = _waypointRoot.GetChild(i).position;
                waypoints[i] = new GroundPoint(position.x, position.z);
            }

            var settings = new CrowdPlanSettings(_config.NpcCount, waypoints.Length, _config.RouteLength,
                _config.WaypointSpread, _config.DwellMin, _config.DwellMax);
            var plans = CrowdPlanner.Build(seed, settings);

            _agents = new CrowdAgent[plans.Length];
            for (var i = 0; i < plans.Length; i++)
            {
                var schedule = new NpcSchedule(plans[i], waypoints, _config.WalkSpeed, (from, to) => FindPath(from, to, groundY));
                _agents[i] = Instantiate(_agentPrefab, transform);
                _agents[i].name = $"Npc_{i:000}";
                _agents[i].Begin(i, schedule, groundY, GaitPlanner.BaseWalkFor(seed, i), _config.StrideLength);
            }
        }

        // The NavMesh lays out each leg; the same mesh gives the same corners on every client (P1.01).
        private static GroundPoint[] FindPath(GroundPoint from, GroundPoint to, float groundY)
        {
            if (!NavMesh.SamplePosition(new Vector3(from.X, groundY, from.Z), out var start, 3f, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(new Vector3(to.X, groundY, to.Z), out var end, 3f, NavMesh.AllAreas))
            {
                return null;
            }

            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path) || path.corners.Length == 0)
            {
                return null;
            }

            var corners = new GroundPoint[path.corners.Length];
            for (var i = 0; i < corners.Length; i++)
            {
                corners[i] = new GroundPoint(path.corners[i].x, path.corners[i].z);
            }

            return corners;
        }
    }
}
