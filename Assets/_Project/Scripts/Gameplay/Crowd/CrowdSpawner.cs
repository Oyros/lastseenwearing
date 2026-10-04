using System.Collections.Generic;
using System.Linq;
using LastSeenWearing.Core.Composite;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Wardrobe;
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
        [SerializeField] private WardrobeCatalog _wardrobe;
        [SerializeField] private WardrobeConfig _wardrobeOdds;
        [SerializeField] private CompositeConfig _composite;

        // Before the seed: a client rebuilding on the seed's change must already hold its lookalikes and case.
        private readonly NetworkVariable<LookalikeSet> _lookalikes = new();
        private readonly NetworkVariable<int> _caseSeed = new();
        private readonly NetworkVariable<int> _seed = new();
        private readonly NetworkVariable<double> _startTime = new();
        private readonly List<CrowdAgent> _dirty = new();
        private readonly List<ulong> _recipients = new();

        private CrowdAgent[] _agents;
        private NpcPlan[] _plans;
        private GroundPoint[] _waypoints;
        private int _raisedSeed;
        private float _syncTimer;
        private float _logTimer;
        private int _logMinute;
        private long _bytesThisMinute;

        public int Seed => _seed.Value;
        public CrowdAgent[] Agents => _agents;

        /// <summary>Each NPC's walk, by index; the same on every client.</summary>
        public GaitSignature[] Signatures { get; private set; }

        /// <summary>Each NPC's outfit, by index; the same on every client (P1.18).</summary>
        public Outfit[] Outfits { get; private set; }
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
                SetSeed(System.Environment.TickCount);
            }
            else
            {
                NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(PosesMessage, OnPosesReceived);
            }

            _seed.OnValueChanged += OnSeedChanged;
            _lookalikes.OnValueChanged += OnLookalikesChanged;
            Raise(_seed.Value);

            if (!IsServer)
            {
                RequestTakenOverRpc();
            }
        }

        public override void OnNetworkDespawn()
        {
            _seed.OnValueChanged -= OnSeedChanged;
            _lookalikes.OnValueChanged -= OnLookalikesChanged;
            if (!IsServer && NetworkManager.CustomMessagingManager != null)
            {
                NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(PosesMessage);
            }

            ClearAgents();
        }

        /// <summary>
        /// Server: a new crowd from <paramref name="seed"/>, starting now — every round has its own (GDD §06).
        /// Clients rebuild when the seed reaches them; the start time travels with it.
        /// </summary>
        public void Reseed(int seed)
        {
            if (IsServer)
            {
                SetSeed(seed);
            }
        }

        /// <summary>
        /// Server, each round of a case (P1.19): a new crowd that reserves the fugitive's walk (the case's) and
        /// holds the round's partial lookalikes of what the composite has <paramref name="revealed"/> so far.
        /// </summary>
        public void Reseed(int seed, int caseSeed, IReadOnlyList<CompositeClaim> revealed)
        {
            if (!IsServer)
            {
                return;
            }

            var reserved = GaitPlanner.CaseWalk(caseSeed, _config.Gait);
            var walks = GaitPlanner.SignaturesFor(seed, _config.NpcCount, _config.Gait, reserved);
            var outfits = OutfitPlanner.OutfitsFor(seed, _config.NpcCount, _wardrobe, _wardrobeOdds);
            var planted = LookalikePlanner.Plan(seed, outfits, walks, reserved, revealed, _wardrobe, _composite);
            _lookalikes.Value = new LookalikeSet { Seed = seed, Entries = planted.Select(LookalikeSet.Entry.Of).ToArray() };
            _caseSeed.Value = caseSeed;
            SetSeed(seed);
            Debug.Log($"[CrowdSpawner] {planted.Count} lookalike(s): " +
                      string.Join(", ", planted.Select(l => $"Npc_{l.Npc:000}")));
        }

        private void SetSeed(int seed)
        {
            // Start first: a client rebuilding on the seed's change must already read the new start.
            _startTime.Value = NetworkManager.ServerTime.Time;
            _seed.Value = seed;
            Debug.Log($"[CrowdSpawner] Crowd seed {seed}, start {_startTime.Value:F3} s server time.");
        }

        private void OnSeedChanged(int previous, int current)
        {
            if (_agents == null)
            {
                return; // not raised yet: OnNetworkSpawn raises with the current seed
            }

            ClearAgents();
            Raise(current);
        }

        // Lookalikes arriving after their crowd was raised: rebuild just those NPCs.
        private void OnLookalikesChanged(LookalikeSet previous, LookalikeSet current)
        {
            if (_agents == null || current.Seed != _raisedSeed || current.Entries == null)
            {
                return;
            }

            foreach (var entry in current.Entries)
            {
                Destroy(_agents[entry.Npc].gameObject);
                Outfits[entry.Npc] = entry.Apply(Outfits[entry.Npc]);
                Signatures[entry.Npc] = entry.Apply(Signatures[entry.Npc]);
                BuildAgent(entry.Npc);
            }
        }

        private void ClearAgents()
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

        /// <summary>
        /// Server: NPC <paramref name="npc"/> stops where it stands and every client sees it play
        /// <paramref name="action"/> (P1.25 — someone cuffed by mistake). It stays off its schedule after, like a
        /// bumped NPC.
        /// </summary>
        public void HoldAndAct(int npc, BodyAction action)
        {
            if (!IsServer || _agents == null || npc < 0 || npc >= _agents.Length)
            {
                return;
            }

            var agent = _agents[npc];
            agent.Bump(agent.transform.position, 0f, Mathf.Max(agent.ActionLength(action), 0.1f));
            agent.PoseDirty = true;
            ActRpc(npc, action);
        }

        [Rpc(SendTo.Everyone)]
        private void ActRpc(int npc, BodyAction action)
        {
            if (_agents != null && npc < _agents.Length)
            {
                _agents[npc].Act(action);
            }
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
                    agent.FollowSchedule(crowdTime, deltaTime);
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
            _raisedSeed = seed;
            _waypoints = new GroundPoint[_waypointRoot.childCount];
            for (var i = 0; i < _waypoints.Length; i++)
            {
                var position = _waypointRoot.GetChild(i).position;
                _waypoints[i] = new GroundPoint(position.x, position.z);
            }

            var settings = new CrowdPlanSettings(_config.NpcCount, _waypoints.Length, _config.RouteLength,
                _config.WaypointSpread, _config.DwellMin, _config.DwellMax);
            _plans = CrowdPlanner.Build(seed, settings);

            // Every walk unique, from the same seed (GDD §05, D-025), and never the fugitive's, which is the case's
            // (P1.19); the pace bucket scales the walking speed and the step follows (D-022). Outfits from the seed
            // too (P1.18), then the server's planted lookalikes for this round.
            Signatures = GaitPlanner.SignaturesFor(seed, _plans.Length, _config.Gait, GaitPlanner.CaseWalk(_caseSeed.Value, _config.Gait));
            Outfits = OutfitPlanner.OutfitsFor(seed, _plans.Length, _wardrobe, _wardrobeOdds);
            var lookalikes = _lookalikes.Value;
            if (lookalikes.Seed == seed && lookalikes.Entries != null)
            {
                foreach (var entry in lookalikes.Entries)
                {
                    Outfits[entry.Npc] = entry.Apply(Outfits[entry.Npc]);
                    Signatures[entry.Npc] = entry.Apply(Signatures[entry.Npc]);
                }
            }

            _agents = new CrowdAgent[_plans.Length];
            var log = new System.Text.StringBuilder($"[CrowdSpawner] {_plans.Length} walks (seed {seed}):");
            for (var i = 0; i < _plans.Length; i++)
            {
                BuildAgent(i);
                log.Append($"\n  Npc_{i:000}: {Signatures[i].Describe()} | {Outfits[i].Describe(_wardrobe)}");
            }

            Debug.Log(log.ToString());
        }

        private void BuildAgent(int i)
        {
            var groundY = transform.position.y;
            var gait = Signatures[i];
            var outfit = Outfits[i];
            var speed = _config.WalkSpeed * _config.TempoMultiplier(gait.Tempo);
            var schedule = new NpcSchedule(_plans[i], _waypoints, speed, (from, to) => FindPath(from, to, groundY));
            _agents[i] = Instantiate(_agentPrefab, transform);
            _agents[i].name = $"Npc_{i:000}";

            // A taller body takes a longer step (P1.19): the stride scales with the height.
            _agents[i].Begin(i, schedule, groundY, gait.Base, _config.StrideLength * _wardrobeOdds.ScaleOf(outfit.Height));
            foreach (var trait in gait.Traits)
            {
                _agents[i].SetTrait(trait.Trait, trait.Strength);
            }

            _agents[i].GetComponent<OutfitView>().Apply(outfit);
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
