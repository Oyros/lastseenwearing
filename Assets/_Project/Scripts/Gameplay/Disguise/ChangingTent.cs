using System;
using System.Collections.Generic;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Disguise;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Core.Round;
using LastSeenWearing.Core.Wardrobe;
using LastSeenWearing.Gameplay.Crowd;
using LastSeenWearing.Gameplay.Interaction;
using LastSeenWearing.Gameplay.Player;
using LastSeenWearing.Gameplay.Roles;
using LastSeenWearing.Gameplay.Round;
using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Disguise
{
    /// <summary>
    /// A changing tent (GDD §05, P1.21). Its rail is public: every client draws it from the round's crowd seed
    /// (<see cref="TentStock"/>); the server keeps what has been taken and whether the tent is used, and is the only
    /// one that changes anyone. The fugitive interacts, picks garments on their own screen
    /// (<see cref="Opened"/>), and the server checks the choice (<see cref="TentChange"/>) before the fugitive goes
    /// in. What came off stays here — the dog's scent later (GDD §03).
    /// </summary>
    public sealed class ChangingTent : NetworkBehaviour, IInteractable
    {
        [Tooltip("Which of the layout's tent spots this is: the rail is drawn per tent.")]
        [SerializeField] private int _index;
        [SerializeField] private DisguiseConfig _config;
        [SerializeField] private MovementConfig _movement;
        [SerializeField] private CrowdConfig _crowdConfig;
        [SerializeField] private WardrobeConfig _wardrobeOdds;
        [SerializeField] private WardrobeCatalog _catalog;
        [SerializeField] private CrowdSpawner _crowd;
        [SerializeField] private RoleRosterSync _roster;
        [SerializeField] private RoundDirector _director;

        private readonly NetworkVariable<int> _usesLeft = new();
        private readonly NetworkVariable<int> _taken = new(); // bit i: rail item i is gone

        private readonly List<Worn> _leftBehind = new();
        private StockItem[] _rail = Array.Empty<StockItem>();
        private int _railSeed;
        private int? _stockedFor;

        /// <summary>The local fugitive opened this tent: show them its rail.</summary>
        public static event Action<ChangingTent> Opened;

        /// <summary>The server turned the local fugitive's change down.</summary>
        public static event Action<ChangeRefusal> Refused;

        public Vector3 InteractionPoint => transform.position;

        public WardrobeCatalog Catalog => _catalog;

        /// <summary>Server only: what changing fugitives left here this round.</summary>
        public IReadOnlyList<Worn> LeftBehind => _leftBehind;

        public bool Used => _usesLeft.Value <= 0;

        /// <summary>This round's rail, the same on every client.</summary>
        public StockItem[] Rail
        {
            get
            {
                var seed = _crowd.Seed;
                if (seed != _railSeed || _rail.Length == 0)
                {
                    _railSeed = seed;
                    var crowd = OutfitPlanner.OutfitsFor(seed, _crowdConfig.NpcCount, _catalog, _wardrobeOdds);
                    _rail = TentStock.For(seed, _index, crowd, _config);
                }

                return _rail;
            }
        }

        public bool IsTaken(int item) => (_taken.Value & (1 << item)) != 0;

        // Server: a new round's crowd seed is a new round.
        private void Update()
        {
            if (IsSpawned && IsServer && _crowd.IsSpawned && _crowd.Seed != _stockedFor)
            {
                _stockedFor = _crowd.Seed;
                Restock();
            }
        }

        // A new round's crowd is a new rail; the tent opens again (GDD §05: one use per round).
        private void Restock()
        {
            _usesLeft.Value = _config.UsesPerTent;
            _taken.Value = 0;
            _leftBehind.Clear();
        }

        public bool CanInteract(ulong clientId) =>
            !Used && _roster.RoleOf(clientId) == Role.Fugitive && _director.Phase is RoundPhase.Live or RoundPhase.LastCuff;

        public void Interact(ulong clientId) => OpenRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));

        [Rpc(SendTo.SpecifiedInParams)]
        private void OpenRpc(RpcParams rpcParams) => Opened?.Invoke(this);

        /// <summary>The local fugitive's choice: a rail index per slot, or <see cref="TentChange.Keep"/>.</summary>
        public void RequestChange(int top, int bottom, int hat) => ChangeRpc(top, bottom, hat);

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void ChangeRpc(int top, int bottom, int hat, RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;
            if (!CanInteract(sender) || !NetworkManager.ConnectedClients.TryGetValue(sender, out var client)
                || client.PlayerObject == null || !client.PlayerObject.TryGetComponent<FugitiveController>(out var fugitive)
                || fugitive.IsChanging)
            {
                RefusedRpc(ChangeRefusal.TentUsed, RpcTarget.Single(sender, RpcTargetUse.Temp));
                return;
            }

            var range = _movement.InteractRange;
            if ((fugitive.transform.position - InteractionPoint).sqrMagnitude > range * range)
            {
                RefusedRpc(ChangeRefusal.TooFar, RpcTarget.Single(sender, RpcTargetUse.Temp));
                return;
            }

            var taken = new bool[Rail.Length];
            for (var i = 0; i < taken.Length; i++)
            {
                taken[i] = IsTaken(i);
            }

            var result = TentChange.Apply(fugitive.Outfit, Rail, taken, _usesLeft.Value, top, bottom, hat, _catalog);
            if (!result.Done)
            {
                RefusedRpc(result.Refusal, RpcTarget.Single(sender, RpcTargetUse.Temp));
                return;
            }

            _usesLeft.Value--;
            var mask = _taken.Value;
            foreach (var item in result.Taken)
            {
                mask |= 1 << item;
            }

            _taken.Value = mask;
            _leftBehind.AddRange(result.LeftBehind);
            fugitive.ChangeInto(result.Outfit, _config.ChangeSeconds);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void RefusedRpc(ChangeRefusal refusal, RpcParams rpcParams) => Refused?.Invoke(refusal);
    }
}
