using System;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Objective;
using LastSeenWearing.Core.Round;
using LastSeenWearing.Gameplay.Player;
using LastSeenWearing.Gameplay.Round;
using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Objective
{
    /// <summary>
    /// The fugitive's targets this round (GDD §04.4, P1.22). The server holds the progress and the job in hand; only
    /// the fugitive hears it — nobody else gets a meter (GDD §04.4: "only players' eyes give the fugitive away"). A
    /// job runs while the fugitive stands at the target; walking off drops it. The target that makes it enough
    /// opens an exit (<see cref="TargetProgress.ExitFor"/>); reaching it is P1.23.
    /// </summary>
    public sealed class Objectives : NetworkBehaviour
    {
        [SerializeField] private FugitiveConfig _config;
        [SerializeField] private RoundDirector _director;
        [Tooltip("The layout's targets, by index (Editor: Layouts › Place Targets In Open Scene).")]
        [SerializeField] private TargetSpot[] _targets = Array.Empty<TargetSpot>();
        [Tooltip("The layout's exits: where each stands and its name (objective.exit.<name> is its word).")]
        [SerializeField] private Vector3[] _exits = Array.Empty<Vector3>();
        [SerializeField] private string[] _exitNames = Array.Empty<string>();

        // Server.
        private TargetProgress _progress;
        private ulong? _fugitive;
        private FugitiveController _worker;
        private int _job = NoJob;
        private Vector3 _jobFrom;
        private double _jobUntil;

        private const int NoJob = -1;

        /// <summary>Something the fugitive's screen shows changed.</summary>
        public event Action Changed;

        public FugitiveConfig Config => _config;
        public TargetSpot[] Targets => _targets;

        // What the fugitive's client knows (and the server).
        public int DoneMask { get; private set; }
        public int DoneCount { get; private set; }
        public int Needed => _config.TargetsNeeded;
        public int OpenExit { get; private set; } = TargetProgress.NoExit;
        public string OpenExitName => OpenExit >= 0 && OpenExit < _exitNames.Length ? _exitNames[OpenExit] : null;
        public int Job { get; private set; } = NoJob;
        public double JobFrom { get; private set; }
        public double JobUntil { get; private set; }

        /// <summary>Where the open exit stands, where it is known (the server, the fugitive).</summary>
        public Vector3? OpenExitPosition => OpenExit >= 0 && OpenExit < _exits.Length ? _exits[OpenExit] : null;

        public Vector3[] Exits => _exits;

        public bool IsDone(int target) => (DoneMask & (1 << target)) != 0;

        /// <summary>Server, when a round goes live: a fresh start for this round's fugitive.</summary>
        public void BeginRound(ulong? fugitive)
        {
            _progress = new TargetProgress(_targets.Length, _config.TargetsNeeded);
            _fugitive = fugitive;
            _job = NoJob;
            _worker = null;
            ClearRpc();
            Send();
        }

        /// <summary>Server, from a target the fugitive used: start its job.</summary>
        public void Begin(ulong clientId, TargetSpot target)
        {
            if (_progress == null || clientId != _fugitive || _job != NoJob || _progress.IsDone(target.Index) || _progress.Complete
                || !NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null
                || !client.PlayerObject.TryGetComponent(out _worker))
            {
                return;
            }

            _job = target.Index;
            _jobFrom = _worker.transform.position;
            var now = NetworkManager.ServerTime.Time;
            _jobUntil = now + _config.SecondsFor(target.Kind);
            Send(now);
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            CheckEscape();
            if (_job == NoJob)
            {
                return;
            }

            if (_worker == null || _worker.IsChanging || (_worker.transform.position - _jobFrom).sqrMagnitude > _config.WorkDrift * _config.WorkDrift)
            {
                _job = NoJob; // walked off, or gone: the job is dropped
                Send();
                return;
            }

            if (NetworkManager.ServerTime.Time >= _jobUntil)
            {
                var target = _targets[_job];
                _job = NoJob;
                _progress.Finish(target.Index, target.transform.position, _exits, _config.ExitRule);
                if (_progress.Complete)
                {
                    Debug.Log($"[Objectives] {_progress.Count} targets done; exit {OpenExitNameOf(_progress.OpenExit)} opens.");
                }

                Send();
            }
        }

        // Out of the open exit with the targets done — or, in the last-cuff chase, out of any exit (team, P1.23).
        private void CheckEscape()
        {
            var phase = _director.Phase;
            var chase = phase == RoundPhase.LastCuff;
            if (_progress == null || _fugitive is not { } fugitive || !(chase || (phase == RoundPhase.Live && _progress.Complete))
                || !NetworkManager.ConnectedClients.TryGetValue(fugitive, out var client) || client.PlayerObject == null
                || !client.PlayerObject.TryGetComponent<FugitiveController>(out var body) || body.IsChanging)
            {
                return;
            }

            if (TargetProgress.Escapes(body.transform.position, _exits, _progress.OpenExit, chase, _config.ExitRadius))
            {
                Debug.Log($"[Objectives] The fugitive is out ({(chase ? "the chase" : OpenExitNameOf(_progress.OpenExit))}).");
                _director.ReportEscape();
            }
        }

        private string OpenExitNameOf(int exit) => exit >= 0 && exit < _exitNames.Length ? _exitNames[exit] : "none";

        // The server's state to the fugitive alone (and to the server's own copy).
        private void Send(double jobFrom = 0d)
        {
            if (_progress == null)
            {
                return;
            }

            var state = new State
            {
                Done = _progress.Mask,
                Count = (byte)_progress.Count,
                Exit = (sbyte)_progress.OpenExit,
                Job = (sbyte)_job,
                JobFrom = jobFrom > 0d ? jobFrom : JobFrom,
                JobUntil = _jobUntil,
            };
            Apply(state);
            if (_fugitive is { } fugitive && fugitive != NetworkManager.LocalClientId)
            {
                StateRpc(state, RpcTarget.Single(fugitive, RpcTargetUse.Temp));
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void StateRpc(State state, RpcParams rpcParams) => Apply(state);

        [Rpc(SendTo.Everyone)]
        private void ClearRpc() => Apply(new State { Exit = TargetProgress.NoExit, Job = NoJob });

        private void Apply(State state)
        {
            DoneMask = state.Done;
            DoneCount = state.Count;
            OpenExit = state.Exit;
            Job = state.Job;
            JobFrom = state.JobFrom;
            JobUntil = state.JobUntil;
            Changed?.Invoke();
        }

        private struct State : INetworkSerializable
        {
            public int Done;
            public byte Count;
            public sbyte Exit;
            public sbyte Job;
            public double JobFrom;
            public double JobUntil;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Done);
                serializer.SerializeValue(ref Count);
                serializer.SerializeValue(ref Exit);
                serializer.SerializeValue(ref Job);
                serializer.SerializeValue(ref JobFrom);
                serializer.SerializeValue(ref JobUntil);
            }
        }
    }
}
