using System;
using LastSeenWearing.Core.Composite;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Wardrobe;
using Unity.Netcode;

namespace LastSeenWearing.Gameplay.Crowd
{
    /// <summary>
    /// A round's planted lookalikes as they cross the network (P1.19): only the server can plan them — it alone
    /// holds the composite — so it sends each changed NPC's body (and base walk and pace), never the composite.
    /// Tagged with the crowd seed it belongs to.
    /// </summary>
    public struct LookalikeSet : INetworkSerializable, IEquatable<LookalikeSet>
    {
        public int Seed;
        public Entry[] Entries;

        public struct Entry : INetworkSerializable, IEquatable<Entry>
        {
            public short Npc;
            public byte Sex;
            public byte Height;
            public byte Build;
            public byte Skin;
            public byte Hair;
            public byte HairColour;
            public bool HasWalk;
            public byte Base;
            public byte Tempo;

            public static Entry Of(Lookalike lookalike) => new()
            {
                Npc = (short)lookalike.Npc,
                Sex = (byte)lookalike.Outfit.Sex,
                Height = (byte)lookalike.Outfit.Height,
                Build = (byte)lookalike.Outfit.Build,
                Skin = (byte)lookalike.Outfit.Skin,
                Hair = (byte)lookalike.Outfit.Hair,
                HairColour = (byte)lookalike.Outfit.HairColour,
                HasWalk = lookalike.Walk != null,
                Base = lookalike.Walk != null ? (byte)lookalike.Walk.Base : (byte)0,
                Tempo = lookalike.Walk != null ? (byte)lookalike.Walk.Tempo : (byte)0,
            };

            /// <summary>The NPC as drawn, with this body.</summary>
            public Outfit Apply(Outfit drawn) =>
                drawn.WithBody((Sex)Sex, (Height)Height, (Build)Build, Skin, Hair, HairColour);

            /// <summary>The NPC's walk with this base and pace, or the drawn one.</summary>
            public GaitSignature Apply(GaitSignature drawn) =>
                HasWalk ? new GaitSignature((BaseWalk)Base, (Tempo)Tempo, drawn.Traits) : drawn;

            public bool Equals(Entry o) =>
                Npc == o.Npc && Sex == o.Sex && Height == o.Height && Build == o.Build && Skin == o.Skin && Hair == o.Hair
                && HairColour == o.HairColour && HasWalk == o.HasWalk && Base == o.Base && Tempo == o.Tempo;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Npc);
                serializer.SerializeValue(ref Sex);
                serializer.SerializeValue(ref Height);
                serializer.SerializeValue(ref Build);
                serializer.SerializeValue(ref Skin);
                serializer.SerializeValue(ref Hair);
                serializer.SerializeValue(ref HairColour);
                serializer.SerializeValue(ref HasWalk);
                serializer.SerializeValue(ref Base);
                serializer.SerializeValue(ref Tempo);
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Seed);
            var count = Entries?.Length ?? 0;
            serializer.SerializeValue(ref count);
            if (serializer.IsReader)
            {
                Entries = new Entry[count];
            }

            for (var i = 0; i < count; i++)
            {
                serializer.SerializeValue(ref Entries[i]);
            }
        }

        public bool Equals(LookalikeSet other)
        {
            if (Seed != other.Seed || (Entries?.Length ?? 0) != (other.Entries?.Length ?? 0))
            {
                return false;
            }

            for (var i = 0; i < (Entries?.Length ?? 0); i++)
            {
                if (!Entries[i].Equals(other.Entries[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj) => obj is LookalikeSet other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Seed, Entries?.Length ?? 0);
    }
}
