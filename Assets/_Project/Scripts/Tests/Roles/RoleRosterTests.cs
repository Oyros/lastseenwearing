using System.Linq;
using LastSeenWearing.Core.Roles;
using NUnit.Framework;

namespace LastSeenWearing.Tests.Roles
{
    /// <summary>P1.09: handing out roles (GDD §03, D-007, D-021).</summary>
    public sealed class RoleRosterTests
    {
        private static RoleRoster Lobby(RoleSelectionMode mode, int players)
        {
            var roster = new RoleRoster(mode);
            for (ulong i = 0; i < (ulong)players; i++)
            {
                roster.AddPlayer(i);
            }

            return roster;
        }

        [TestCase(3, new[] { Role.Watcher, Role.Patrol, Role.Fugitive })]
        [TestCase(4, new[] { Role.Watcher, Role.Patrol, Role.Fugitive, Role.Plainclothes })]
        [TestCase(5, new[] { Role.Watcher, Role.Patrol, Role.Fugitive, Role.Plainclothes, Role.Dog })]
        public void TheLobbySizeDecidesTheRoles(int players, Role[] expected)
        {
            Assert.That(RoleRules.RolesFor(players), Is.EquivalentTo(expected));
        }

        [Test]
        public void ARoleGoesToOnePlayer()
        {
            var roster = Lobby(RoleSelectionMode.Pick, 3);
            Assert.That(roster.TryClaim(0, Role.Fugitive), Is.True);
            Assert.That(roster.TryClaim(1, Role.Fugitive), Is.False);
            Assert.That(roster.Roles[1], Is.EqualTo(Role.None));
        }

        [Test]
        public void ClaimingAnotherRoleGivesUpTheFirst()
        {
            var roster = Lobby(RoleSelectionMode.Pick, 3);
            roster.TryClaim(0, Role.Fugitive);
            roster.TryClaim(0, Role.Watcher);
            Assert.That(roster.TryClaim(1, Role.Fugitive), Is.True);
        }

        [Test]
        public void ARoleTheLobbySizeDoesNotPlayCannotBeClaimed()
        {
            var roster = Lobby(RoleSelectionMode.Pick, 3);
            Assert.That(roster.TryClaim(0, Role.Dog), Is.False);
            Assert.That(roster.TryClaim(0, Role.Plainclothes), Is.False);
        }

        [Test]
        public void NobodyClaimsInRandomMode()
        {
            var roster = Lobby(RoleSelectionMode.Random, 3);
            Assert.That(roster.TryClaim(0, Role.Watcher), Is.False);
        }

        [Test]
        public void SwitchingToRandomClearsClaims()
        {
            var roster = Lobby(RoleSelectionMode.Pick, 3);
            roster.TryClaim(0, Role.Watcher);
            roster.SetMode(RoleSelectionMode.Random);
            Assert.That(roster.Roles.Values, Is.All.EqualTo(Role.None));
        }

        [Test]
        public void LockingInPickModeKeepsClaimsAndFillsTheRest()
        {
            var roster = Lobby(RoleSelectionMode.Pick, 3);
            roster.TryClaim(2, Role.Watcher);
            Assert.That(roster.Lock(7), Is.True);
            Assert.That(roster.Roles[2], Is.EqualTo(Role.Watcher));
            Assert.That(roster.Roles.Values, Is.EquivalentTo(new[] { Role.Watcher, Role.Patrol, Role.Fugitive }));
        }

        [Test]
        public void LockingInRandomModeHandsOutEveryRole()
        {
            var roster = Lobby(RoleSelectionMode.Random, 5);
            Assert.That(roster.Lock(11), Is.True);
            Assert.That(roster.Roles.Values, Is.EquivalentTo(RoleRules.RolesFor(5)));
        }

        [Test]
        public void TheSameSeedDrawsTheSameRoles()
        {
            var a = Lobby(RoleSelectionMode.Random, 5);
            var b = Lobby(RoleSelectionMode.Random, 5);
            a.Lock(42);
            b.Lock(42);
            Assert.That(a.Roles.ToArray(), Is.EqualTo(b.Roles.ToArray()));
        }

        [Test]
        public void EverySeatGetsEveryRoleSometimes()
        {
            for (ulong seat = 0; seat < 3; seat++)
            {
                var seen = Enumerable.Range(0, 200).Select(seed =>
                {
                    var roster = Lobby(RoleSelectionMode.Random, 3);
                    roster.Lock(seed);
                    return roster.Roles[seat];
                }).Distinct().Count();
                Assert.That(seen, Is.EqualTo(3), $"seat {seat} is not drawn fairly");
            }
        }

        [Test]
        public void ALobbyTooSmallForTheCoreLoopCannotLock()
        {
            var roster = Lobby(RoleSelectionMode.Pick, 2);
            roster.TryClaim(0, Role.Watcher);
            Assert.That(roster.Lock(1), Is.False);
            Assert.That(roster.IsLocked, Is.False);
            Assert.That(roster.Roles[0], Is.EqualTo(Role.Watcher), "a failed lock changes nothing");
            Assert.That(roster.Roles[1], Is.EqualTo(Role.None));
        }

        [Test]
        public void NothingChangesOnceLocked()
        {
            var roster = Lobby(RoleSelectionMode.Pick, 3);
            roster.Lock(3);
            var before = roster.Roles.ToArray();
            Assert.That(roster.TryClaim(0, Role.Watcher), Is.False);
            Assert.That(roster.Release(0), Is.False);
            Assert.That(roster.SetMode(RoleSelectionMode.Random), Is.False);
            Assert.That(roster.Roles.ToArray(), Is.EqualTo(before));
        }

        [Test]
        public void ALeavingPlayerFreesTheirRole()
        {
            var roster = Lobby(RoleSelectionMode.Pick, 4);
            roster.TryClaim(3, Role.Plainclothes);
            roster.TryClaim(0, Role.Fugitive);
            roster.RemovePlayer(0);
            Assert.That(roster.Roles.ContainsKey(0), Is.False);
            Assert.That(roster.Roles[3], Is.EqualTo(Role.None), "three players no longer play Plainclothes");
            Assert.That(roster.TryClaim(1, Role.Fugitive), Is.True);
        }
    }
}
