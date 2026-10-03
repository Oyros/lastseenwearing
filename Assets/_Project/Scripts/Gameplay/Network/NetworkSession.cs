using LastSeenWearing.Core.Config;
using Netcode.Transports.Facepunch;
using Steamworks;
using Unity.Multiplayer.PlayMode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSeenWearing.Gameplay.Network
{
    /// <summary>
    /// Starts and ends a listen-server session (D-002) on the transport this instance uses: Steam in
    /// a player build, Unity Transport on this machine in the editor (D-016). Lives on the bootstrap
    /// object next to the <see cref="NetworkManager"/> and both transports.
    /// In the editor it can start itself once the session scene loads: the main editor hosts and
    /// every Multiplayer Play Mode clone joins, so pressing Play is a two-player test.
    /// </summary>
    public sealed class NetworkSession : MonoBehaviour
    {
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private UnityTransport _localTransport;
        [SerializeField] private FacepunchTransport _steamTransport;
        [SerializeField] private LobbyConfig _lobbyConfig;

        [Header("Setup")]
        [SerializeField] private SessionTransport _editorTransport = SessionTransport.Local;
        [SerializeField] private SessionTransport _playerTransport = SessionTransport.Steam;

        // Setup values, not tunables: which app Steam opens as (480 until the store page, D-002)
        // and which scene a session starts in.
        [SerializeField] private uint _steamAppId = 480;
        [SerializeField] private string _sessionSceneName;
        [SerializeField] private bool _editorAutoStart = true;

        private SteamLobby _steamLobby;

        public SessionTransport Transport => Application.isEditor ? _editorTransport : _playerTransport;
        public bool IsRunning => _networkManager != null && _networkManager.IsListening;
        public bool IsHost => _networkManager != null && _networkManager.IsHost;
        public bool CanInvite => _steamLobby != null && _steamLobby.HasLobby && IsHost;

        public int ConnectedPlayers =>
            IsRunning && _networkManager.IsServer ? _networkManager.ConnectedClientsIds.Count : 0;

        private void Awake()
        {
            _networkManager.NetworkConfig.NetworkTransport =
                Transport == SessionTransport.Steam ? _steamTransport : _localTransport;

            if (Transport == SessionTransport.Steam)
            {
                InitialiseSteam();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _steamLobby?.Dispose();
            if (SteamClient.IsValid)
            {
                SteamClient.Shutdown();
            }
        }

        private void Update()
        {
            // While a session runs the transport pumps Steam; before that, an invite still has to arrive.
            if (SteamClient.IsValid && !IsRunning)
            {
                SteamClient.RunCallbacks();
            }
        }

        public void Host()
        {
            if (IsRunning || !_networkManager.StartHost())
            {
                return;
            }

            if (Transport == SessionTransport.Steam && _steamLobby != null)
            {
                _steamLobby.Create(_lobbyConfig.MaxPlayers);
            }
        }

        /// <summary>Local transport only: Steam players join through an invite (<see cref="SteamLobby"/>).</summary>
        public void JoinLocal()
        {
            if (!IsRunning && Transport == SessionTransport.Local)
            {
                _networkManager.StartClient();
            }
        }

        public void OpenInviteOverlay()
        {
            _steamLobby?.OpenInviteOverlay();
        }

        public void Leave()
        {
            _steamLobby?.Leave();
            if (IsRunning)
            {
                _networkManager.Shutdown();
            }
        }

        private void InitialiseSteam()
        {
            try
            {
                if (!SteamClient.IsValid)
                {
                    SteamClient.Init(_steamAppId, false);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NetworkSession] Steam did not start; is the Steam client running? {e.Message}");
                return;
            }

            _steamLobby = new SteamLobby();
            _steamLobby.HostFound += OnSteamHostFound;
        }

        private void OnSteamHostFound(SteamId hostId)
        {
            if (IsRunning)
            {
                return;
            }

            _steamTransport.targetSteamId = hostId;
            _networkManager.StartClient();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Application.isEditor || !_editorAutoStart || IsRunning || scene.name != _sessionSceneName)
            {
                return;
            }

            if (CurrentPlayer.IsMainEditor)
            {
                Host();
            }
            else
            {
                JoinLocal();
            }
        }
    }
}
