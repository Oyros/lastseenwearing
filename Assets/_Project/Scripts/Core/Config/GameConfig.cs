using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// The root of every tunable number in the game. Each domain config is a field here, and
    /// nothing else is: a config <c>GameConfig</c> cannot reach is invisible and will be
    /// forgotten. One asset, <c>Assets/_Project/Data/Config/GameConfig.asset</c>, handed in by
    /// the bootstrap. Adding a domain: docs/DATA.md §6.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Last Seen Wearing/Config/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField] private LobbyConfig _lobby;
        [SerializeField] private MovementConfig _movement;
        [SerializeField] private CrowdConfig _crowd;
        [SerializeField] private RoundConfig _round;
        [SerializeField] private CameraConfig _camera;

        public LobbyConfig Lobby => _lobby;
        public RoundConfig Round => _round;
        public CameraConfig Camera => _camera;
        public MovementConfig Movement => _movement;
        public CrowdConfig Crowd => _crowd;
    }
}
