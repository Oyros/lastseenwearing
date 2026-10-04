using System;
using System.Collections.Generic;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Randomness;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Core.Round;
using LastSeenWearing.Gameplay.Crowd;
using LastSeenWearing.Gameplay.Player;
using LastSeenWearing.Gameplay.Roles;
using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Round
{
    /// <summary>
    /// The one authority over the round (CLAUDE.md rule 5, ARCHITECTURE "Round state machine"). On the server
    /// it times each phase, moves through <see cref="RoundCycle"/> and nothing else may; it raises the
    /// festival programme, spawns each player's body for Live by role and clears them after, and gives every
    /// round a new crowd (GDD §06). Clients read the phase, its start and the round from network variables.
    /// </summary>
    public sealed class RoundDirector : NetworkBehaviour
    {
        [Serializable]
        private struct RoleSpawn
        {
            public Role Role;

            /// <summary>Where the role starts; none for the fugitive, who starts inside the crowd.</summary>
            public Transform Point;

            /// <summary>The role's body; none falls back to the placeholder player prefab.</summary>
            public NetworkObject Prefab;
        }

        [SerializeField] private RoundConfig _config;
        [SerializeField] private RoleRosterSync _roster;
        [SerializeField] private CrowdSpawner _crowd;
        [SerializeField] private CompositeSync _composite;
        [SerializeField] private NetworkObject _playerPrefab;
        [SerializeField] private RoleSpawn[] _spawns;

        private readonly NetworkVariable<RoundPhase> _phase = new();
        private readonly NetworkVariable<double> _phaseStart = new();
        private readonly NetworkVariable<int> _round = new();
        private readonly NetworkVariable<RoundOutcome> _outcome = new();

        private readonly List<NetworkObject> _bodies = new();
        private double _programmeClock = -1d;
        private int _caseSeed;

        /// <summary>Every client, on every phase change.</summary>
        public event Action<RoundPhase> PhaseChanged;

        /// <summary>Every client, when the programme reaches an event.</summary>
        public event Action<FestivalEvent> FestivalEventRaised;

        public RoundPhase Phase => _phase.Value;
        public int Round => _round.Value;
        public int RoundsPerCase => _config.RoundsPerCase;
        public RoundOutcome Outcome => _outcome.Value;
        public double PhaseElapsed => NetworkManager.ServerTime.Time - _phaseStart.Value;

        /// <summary>Seconds left in a timed phase; 0 in one that waits (Lobby).</summary>
        public double PhaseRemaining => Math.Max(0d, _config.SecondsOf(_phase.Value) - PhaseElapsed);

        public override void OnNetworkSpawn()
        {
            _phase.OnValueChanged += OnPhaseChanged;
        }

        public override void OnNetworkDespawn()
        {
            _phase.OnValueChanged -= OnPhaseChanged;
        }

        /// <summary>The host starts the case; it needs the roles locked (P1.09).</summary>
        public void StartCase() => StartCaseRpc();

        [Rpc(SendTo.Server)]
        private void StartCaseRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId || !_roster.IsLocked)
            {
                return;
            }

            _caseSeed = Environment.TickCount;
            _round.Value = 0;
            _composite.BeginCase(_caseSeed, _config.RoundsPerCase, ClientOf(Role.Watcher), ClientOf(Role.Fugitive));
            Fire(RoundEvent.StartCase);
        }

        [Rpc(SendTo.Everyone)]
        private void FestivalEventRpc(FestivalEvent festivalEvent)
        {
            FestivalEventRaised?.Invoke(festivalEvent);
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            var phase = _phase.Value;
            var elapsed = PhaseElapsed;
            if (phase == RoundPhase.Live)
            {
                foreach (var festivalEvent in _config.Programme.Between(_programmeClock, elapsed))
                {
                    Debug.Log($"[RoundDirector] Round {_round.Value + 1}: {festivalEvent} at {elapsed:F1} s.");
                    FestivalEventRpc(festivalEvent);
                }

                _programmeClock = elapsed;
            }

            var length = _config.SecondsOf(phase);
            if (length <= 0f || elapsed < length)
            {
                return;
            }

            switch (phase)
            {
                case RoundPhase.Briefing:
                    Fire(RoundEvent.BriefingOver);
                    break;
                case RoundPhase.Live:
                    _outcome.Value = RoundOutcome.TimeUp;
                    Fire(RoundEvent.TimeUp);
                    break;
                case RoundPhase.LastCuff:
                    Fire(RoundEvent.ChaseOver);
                    break;
                case RoundPhase.Result:
                    Fire(RoundEvent.ResultOver);
                    break;
                case RoundPhase.CaseEnd:
                    Fire(RoundEvent.CaseEndOver);
                    break;
            }
        }

        // The only place the phase changes.
        private void Fire(RoundEvent roundEvent)
        {
            var from = _phase.Value;
            var to = RoundCycle.Next(from, roundEvent, _round.Value, _config.RoundsPerCase);
            if (to == from)
            {
                return;
            }

            if (from == RoundPhase.Result && to == RoundPhase.Briefing)
            {
                _round.Value++;
            }

            _phaseStart.Value = NetworkManager.ServerTime.Time;
            _phase.Value = to;
            Enter(to);
            Debug.Log($"[RoundDirector] {from} -> {to} on {roundEvent} (round {_round.Value + 1} of {_config.RoundsPerCase}).");
        }

        private void Enter(RoundPhase phase)
        {
            switch (phase)
            {
                case RoundPhase.Briefing:
                    _outcome.Value = RoundOutcome.None;
                    // A new crowd every round, from the case's seed and the round number, with the round's lookalikes
                    // of what the composite has revealed so far (P1.19).
                    _crowd.Reseed(SeededRandom.Derive(_caseSeed, _round.Value), _caseSeed, _composite.Revealed(_round.Value));
                    break;
                case RoundPhase.Live:
                    _programmeClock = -1d;
                    SpawnBodies();
                    break;
                case RoundPhase.Result:
                    // Bodies stay through a last-cuff chase and go when the round is decided.
                    DespawnBodies();
                    break;
                case RoundPhase.Lobby:
                    _roster.Unlock(); // roles rotate next case (GDD §06)
                    break;
            }
        }

        private void SpawnBodies()
        {
            var random = new SeededRandom(SeededRandom.Derive(_caseSeed, 1000 + _round.Value));
            for (var i = 0; i < _roster.Count; i++)
            {
                var entry = _roster[i];
                if (entry.Role == Role.Watcher || entry.Role == Role.None)
                {
                    continue; // the Watcher sits at the camera wall (GDD §03)
                }

                var position = SpawnPoint(entry.Role, random);
                var body = Instantiate(PrefabFor(entry.Role), position, Quaternion.identity);
                body.SpawnAsPlayerObject(entry.ClientId, true);
                if (body.TryGetComponent<FugitiveController>(out var fugitive))
                {
                    // After the spawn: a network variable written before it is not tied to the object yet.
                    fugitive.SetSeeds(_caseSeed, _crowd.Seed); // who they are is the case's; what they wear, the round's
                }
                _bodies.Add(body);
            }
        }

        private ulong? ClientOf(Role role)
        {
            for (var i = 0; i < _roster.Count; i++)
            {
                if (_roster[i].Role == role)
                {
                    return _roster[i].ClientId;
                }
            }

            return null;
        }

        // The fugitive starts where an NPC stands, inside the crowd; everyone else at their role's point.
        private Vector3 SpawnPoint(Role role, SeededRandom random)
        {
            if (role == Role.Fugitive && _crowd.Agents != null && _crowd.Agents.Length > 0)
            {
                return _crowd.Agents[random.Range(0, _crowd.Agents.Length)].transform.position;
            }

            foreach (var spawn in _spawns)
            {
                if (spawn.Role == role && spawn.Point != null)
                {
                    return spawn.Point.position;
                }
            }

            return transform.position;
        }

        private NetworkObject PrefabFor(Role role)
        {
            foreach (var spawn in _spawns)
            {
                if (spawn.Role == role && spawn.Prefab != null)
                {
                    return spawn.Prefab;
                }
            }

            return _playerPrefab;
        }

        private void DespawnBodies()
        {
            foreach (var body in _bodies)
            {
                if (body != null && body.IsSpawned)
                {
                    body.Despawn();
                }
            }

            _bodies.Clear();
        }

        private void OnPhaseChanged(RoundPhase previous, RoundPhase current)
        {
            PhaseChanged?.Invoke(current);
        }
    }
}
