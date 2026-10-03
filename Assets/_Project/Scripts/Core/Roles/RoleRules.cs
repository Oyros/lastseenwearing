using System.Collections.Generic;

namespace LastSeenWearing.Core.Roles
{
    /// <summary>
    /// Which roles a lobby of a given size plays (GDD §03). Watcher, Patrol and Fugitive are the core
    /// loop and always in; Plainclothes joins at four and the Dog at five (D-007). Three players is the
    /// P1 prototype's lobby.
    /// </summary>
    public static class RoleRules
    {
        private static readonly Role[] Core = { Role.Watcher, Role.Patrol, Role.Fugitive };

        public static IReadOnlyList<Role> CoreRoles => Core;

        public static IReadOnlyList<Role> RolesFor(int playerCount)
        {
            var roles = new List<Role>(Core);
            if (playerCount >= 4)
            {
                roles.Add(Role.Plainclothes);
            }

            if (playerCount >= 5)
            {
                roles.Add(Role.Dog);
            }

            return roles;
        }
    }
}
