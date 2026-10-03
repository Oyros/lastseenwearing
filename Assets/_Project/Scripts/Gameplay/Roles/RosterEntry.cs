using System;
using LastSeenWearing.Core.Roles;
using Unity.Netcode;

namespace LastSeenWearing.Gameplay.Roles
{
    /// <summary>One row of the synced roster: a player and their role.</summary>
    public struct RosterEntry : INetworkSerializable, IEquatable<RosterEntry>
    {
        public ulong ClientId;
        public Role Role;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Role);
        }

        public bool Equals(RosterEntry other)
        {
            return ClientId == other.ClientId && Role == other.Role;
        }
    }
}
