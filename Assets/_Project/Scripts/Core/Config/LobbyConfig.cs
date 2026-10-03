using LastSeenWearing.Core.Roles;
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
        [Tooltip("GDD: 4–5. [PROVISIONAL] 3 during P1 — the prototype plays Watcher, Patrol, Fugitive.")]
        [SerializeField, Range(1, 5)] private int _minPlayers = 3;
        [SerializeField, Range(1, 5)] private int _maxPlayers = 5;

        [Header("Roles")]
        [Tooltip("How a new lobby hands out roles; the host can change it in the lobby (D-021).")]
        [SerializeField] private RoleSelectionMode _defaultRoleSelection = RoleSelectionMode.Pick;

        public int MinPlayers => _minPlayers;
        public int MaxPlayers => _maxPlayers;
        public RoleSelectionMode DefaultRoleSelection => _defaultRoleSelection;

        private void OnValidate()
        {
            _maxPlayers = Mathf.Max(_maxPlayers, _minPlayers);
        }
    }
}
