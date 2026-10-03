using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// Who can be in a case (docs/DATA.md §3, GDD §03, §06). A 4-player lobby drops the dog (D-007).
    /// Rounds per case joins this asset once GDD §10 settles the default.
    /// </summary>
    [CreateAssetMenu(fileName = "LobbyConfig", menuName = "Last Seen Wearing/Config/Lobby")]
    public sealed class LobbyConfig : ScriptableObject
    {
        [Header("Players")]
        [SerializeField, Range(1, 5)] private int _minPlayers = 4;
        [SerializeField, Range(1, 5)] private int _maxPlayers = 5;

        public int MinPlayers => _minPlayers;
        public int MaxPlayers => _maxPlayers;

        private void OnValidate()
        {
            _maxPlayers = Mathf.Max(_maxPlayers, _minPlayers);
        }
    }
}
