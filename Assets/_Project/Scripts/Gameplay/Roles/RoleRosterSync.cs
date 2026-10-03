using System;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Roles;
using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Roles
{
    /// <summary>
    /// The case's roles on the network (GDD §03, D-006, D-021). The server owns a <see cref="RoleRoster"/>
    /// and every rule is its; this publishes the result to everyone — roles are public — and carries
    /// requests in. Players claim and release; only the host changes the selection mode or locks.
    /// </summary>
    public sealed class RoleRosterSync : NetworkBehaviour
    {
        [SerializeField] private LobbyConfig _lobbyConfig;

        private readonly NetworkList<RosterEntry> _entries = new();
        private readonly NetworkVariable<RoleSelectionMode> _mode = new();
        private readonly NetworkVariable<bool> _locked = new();

        private RoleRoster _roster;

        /// <summary>Raised on every client when anything about the roster changes.</summary>
        public event Action Changed;

        public RoleSelectionMode Mode => _mode.Value;
        public bool IsLocked => _locked.Value;
        public int Count => _entries.Count;

        public RosterEntry this[int index] => _entries[index];

        public Role RoleOf(ulong clientId)
        {
            foreach (var entry in _entries)
            {
                if (entry.ClientId == clientId)
                {
                    return entry.Role;
                }
            }

            return Role.None;
        }

        public bool IsTaken(Role role)
        {
            foreach (var entry in _entries)
            {
                if (entry.Role == role)
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnNetworkSpawn()
        {
            _entries.OnListChanged += OnListChanged;
            _mode.OnValueChanged += OnValueChanged;
            _locked.OnValueChanged += OnValueChanged;

            if (IsServer)
            {
                _roster = new RoleRoster(_lobbyConfig.DefaultRoleSelection);
                foreach (var clientId in NetworkManager.ConnectedClientsIds)
                {
                    _roster.AddPlayer(clientId);
                }

                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
                Publish();
            }

            Changed?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            _entries.OnListChanged -= OnListChanged;
            _mode.OnValueChanged -= OnValueChanged;
            _locked.OnValueChanged -= OnValueChanged;
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        public void Claim(Role role) => ClaimRpc(role);

        public void Release() => ReleaseRpc();

        public void SetMode(RoleSelectionMode mode) => SetModeRpc(mode);

        public void Lock() => LockRpc();

        public void Unlock() => UnlockRpc();

        [Rpc(SendTo.Server)]
        private void ClaimRpc(Role role, RpcParams rpcParams = default)
        {
            if (_roster.TryClaim(rpcParams.Receive.SenderClientId, role))
            {
                Publish();
            }
        }

        [Rpc(SendTo.Server)]
        private void ReleaseRpc(RpcParams rpcParams = default)
        {
            if (_roster.Release(rpcParams.Receive.SenderClientId))
            {
                Publish();
            }
        }

        [Rpc(SendTo.Server)]
        private void SetModeRpc(RoleSelectionMode mode, RpcParams rpcParams = default)
        {
            if (IsHostSender(rpcParams) && _roster.SetMode(mode))
            {
                Publish();
            }
        }

        [Rpc(SendTo.Server)]
        private void LockRpc(RpcParams rpcParams = default)
        {
            if (!IsHostSender(rpcParams))
            {
                return;
            }

            var seed = Environment.TickCount;
            if (_roster.Lock(seed))
            {
                Debug.Log($"[Roles] Locked ({_roster.Mode}, seed {seed}).");
                Publish();
            }
        }

        [Rpc(SendTo.Server)]
        private void UnlockRpc(RpcParams rpcParams = default)
        {
            if (IsHostSender(rpcParams))
            {
                _roster.Unlock();
                Publish();
            }
        }

        private bool IsHostSender(RpcParams rpcParams)
        {
            return rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId;
        }

        private void OnClientConnected(ulong clientId)
        {
            _roster.AddPlayer(clientId);
            Publish();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            _roster.RemovePlayer(clientId);
            Publish();
        }

        private void Publish()
        {
            _mode.Value = _roster.Mode;
            _locked.Value = _roster.IsLocked;
            _entries.Clear();
            foreach (var entry in _roster.Roles)
            {
                _entries.Add(new RosterEntry { ClientId = entry.Key, Role = entry.Value });
            }
        }

        private void OnListChanged(NetworkListEvent<RosterEntry> change)
        {
            Changed?.Invoke();
        }

        private void OnValueChanged<T>(T previous, T current)
        {
            Changed?.Invoke();
        }
    }
}
