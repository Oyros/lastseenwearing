using System;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Network
{
    /// <summary>
    /// The Steam side of a session: the host opens a friends-only lobby and invites through the
    /// overlay; a friend who accepts joins the lobby and is told whose Steam id to connect to.
    /// Steam must already be initialised. Owned and disposed by <see cref="NetworkSession"/>.
    /// </summary>
    public sealed class SteamLobby : IDisposable
    {
        private Lobby? _lobby;

        /// <summary>Raised on a joining player when they enter someone else's lobby.</summary>
        public event Action<SteamId> HostFound;

        public bool HasLobby => _lobby.HasValue;

        public SteamLobby()
        {
            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        }

        public async void Create(int maxMembers)
        {
            var created = await SteamMatchmaking.CreateLobbyAsync(maxMembers);
            if (!created.HasValue)
            {
                Debug.LogError("[SteamLobby] Steam could not create a lobby.");
                return;
            }

            var lobby = created.Value;
            lobby.SetFriendsOnly();
            lobby.SetJoinable(true);
            _lobby = lobby;
        }

        public void OpenInviteOverlay()
        {
            if (_lobby.HasValue)
            {
                SteamFriends.OpenGameInviteOverlay(_lobby.Value.Id);
            }
        }

        public void Leave()
        {
            _lobby?.Leave();
            _lobby = null;
        }

        public void Dispose()
        {
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
            SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
            Leave();
        }

        private static async void OnGameLobbyJoinRequested(Lobby lobby, SteamId friend)
        {
            var result = await lobby.Join();
            if (result != RoomEnter.Success)
            {
                Debug.LogError($"[SteamLobby] Could not join the lobby: {result}.");
            }
        }

        private void OnLobbyEntered(Lobby lobby)
        {
            _lobby = lobby;
            if (lobby.Owner.Id != SteamClient.SteamId)
            {
                HostFound?.Invoke(lobby.Owner.Id);
            }
        }
    }
}
