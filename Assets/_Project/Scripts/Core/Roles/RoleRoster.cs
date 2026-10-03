using System.Collections.Generic;
using System.Linq;
using LastSeenWearing.Core.Randomness;

namespace LastSeenWearing.Core.Roles
{
    /// <summary>
    /// Who plays which role in a case (GDD §03, §06, D-021). The server holds one and every rule of
    /// role handing-out lives here: a role goes to one player, only roles the lobby size plays can be
    /// held, nobody claims in <see cref="RoleSelectionMode.Random"/>, and nothing changes once locked.
    /// Players are identified by their network client id.
    /// </summary>
    public sealed class RoleRoster
    {
        private readonly SortedDictionary<ulong, Role> _roles = new();

        public RoleSelectionMode Mode { get; private set; }
        public bool IsLocked { get; private set; }

        public IReadOnlyDictionary<ulong, Role> Roles => _roles;
        public IReadOnlyList<Role> Available => RoleRules.RolesFor(_roles.Count);

        /// <summary>Every player has a role and the core loop — Watcher, Patrol, Fugitive — is covered.</summary>
        public bool IsComplete =>
            _roles.Values.All(r => r != Role.None) && RoleRules.CoreRoles.All(r => _roles.ContainsValue(r));

        public RoleRoster(RoleSelectionMode mode)
        {
            Mode = mode;
        }

        public void AddPlayer(ulong player)
        {
            if (!_roles.ContainsKey(player))
            {
                _roles[player] = Role.None;
            }
        }

        /// <summary>A player who leaves frees their role. A lobby that shrinks loses roles it no longer plays.</summary>
        public void RemovePlayer(ulong player)
        {
            _roles.Remove(player);
            var available = Available;
            foreach (var other in _roles.Keys.ToArray())
            {
                if (_roles[other] != Role.None && !available.Contains(_roles[other]))
                {
                    _roles[other] = Role.None;
                }
            }
        }

        public bool SetMode(RoleSelectionMode mode)
        {
            if (IsLocked)
            {
                return false;
            }

            Mode = mode;
            if (mode == RoleSelectionMode.Random)
            {
                ClearRoles();
            }

            return true;
        }

        /// <summary>Claim <paramref name="role"/> for <paramref name="player"/>, giving up any role they held.</summary>
        public bool TryClaim(ulong player, Role role)
        {
            if (IsLocked || Mode != RoleSelectionMode.Pick || !_roles.ContainsKey(player) ||
                role == Role.None || !Available.Contains(role))
            {
                return false;
            }

            foreach (var entry in _roles)
            {
                if (entry.Key != player && entry.Value == role)
                {
                    return false;
                }
            }

            _roles[player] = role;
            return true;
        }

        public bool Release(ulong player)
        {
            if (IsLocked || !_roles.ContainsKey(player))
            {
                return false;
            }

            _roles[player] = Role.None;
            return true;
        }

        /// <summary>
        /// Hand every player without a role a free one at random — everyone, in
        /// <see cref="RoleSelectionMode.Random"/> — then lock. The same seed and players give the same
        /// roles. Fails, and changes nothing, unless the result covers the core loop.
        /// </summary>
        public bool Lock(int seed)
        {
            if (IsLocked)
            {
                return false;
            }

            var before = new Dictionary<ulong, Role>(_roles);
            if (Mode == RoleSelectionMode.Random)
            {
                ClearRoles();
            }

            // Core roles first, so a lobby too small to fill every role still gets the core loop.
            var free = new List<Role>();
            foreach (var role in Available)
            {
                if (!_roles.ContainsValue(role))
                {
                    free.Add(role);
                }
            }

            var random = new SeededRandom(seed);
            Shuffle(free, random);
            free = free.OrderByDescending(IsCore).ToList(); // stable: keeps the shuffle within each group

            var waiting = _roles.Where(e => e.Value == Role.None).Select(e => e.Key).ToList();
            Shuffle(waiting, random);
            for (var i = 0; i < waiting.Count && i < free.Count; i++)
            {
                _roles[waiting[i]] = free[i];
            }

            if (!IsComplete)
            {
                _roles.Clear();
                foreach (var entry in before)
                {
                    _roles[entry.Key] = entry.Value;
                }

                return false;
            }

            IsLocked = true;
            return true;
        }

        /// <summary>Back to choosing: claims in Pick mode are kept, Random mode clears them.</summary>
        public void Unlock()
        {
            IsLocked = false;
            if (Mode == RoleSelectionMode.Random)
            {
                ClearRoles();
            }
        }

        private void ClearRoles()
        {
            foreach (var player in _roles.Keys.ToArray())
            {
                _roles[player] = Role.None;
            }
        }

        private static bool IsCore(Role role)
        {
            return RoleRules.CoreRoles.Contains(role);
        }

        // Fisher–Yates on our own PRNG, so every machine shuffles alike.
        private static void Shuffle<T>(IList<T> items, SeededRandom random)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var j = random.Range(0, i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
