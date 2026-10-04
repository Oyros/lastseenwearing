using System;
using System.Collections.Generic;
using LastSeenWearing.Core.Composite;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Wardrobe;
using LastSeenWearing.Core.Watcher;
using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Round
{
    /// <summary>
    /// The composite on the network (GDD §04.1, ARCHITECTURE: role-filtered, P1.19). The server builds it when a
    /// case starts and sends each role its copy: the Watcher the claims and their confidence, the fugitive the same
    /// plus which claims are wrong and what is true; nobody else gets anything — the field team hears it over the
    /// radio. Each client keeps only what it was sent. It also holds the Watcher's "last seen" (P1.20): the
    /// witness's report at the start of each round — sent to the Watcher alone — and the Watcher's own marks, which
    /// stay on their screen and are never checked.
    /// </summary>
    public sealed class CompositeSync : NetworkBehaviour
    {
        [SerializeField] private WardrobeCatalog _catalog;
        [SerializeField] private WardrobeConfig _wardrobeOdds;
        [SerializeField] private CrowdConfig _crowd;
        [SerializeField] private CompositeConfig _config;
        [SerializeField] private WatcherConfig _watcher;

        private CompositeSketch _full; // server only

        /// <summary>This client's copy, or none: the Watcher's or the fugitive's.</summary>
        public CompositeSketch Sketch { get; private set; }

        /// <summary>True when this client's copy shows the witness's mistakes (the fugitive's).</summary>
        public bool SeesErrors { get; private set; }

        public CompositeConfig Config => _config;
        public WardrobeCatalog Catalog => _catalog;

        /// <summary>The Watcher's latest sighting of the fugitive's clothes, or none.</summary>
        public Sighting? LastSeen { get; private set; }

        /// <summary>Every client, when its copy or its last seen arrives or is cleared.</summary>
        public event Action Changed;

        /// <summary>
        /// Server, when a round goes live: the witness tells the Watcher what the fugitive wears now — true, and
        /// <see cref="WatcherConfig.WitnessReportAge"/> old already. The fugitive's clothes are the round's.
        /// </summary>
        public void ReportWitness(int caseSeed, int crowdSeed, ulong? watcher)
        {
            if (!IsServer || watcher is not { } w)
            {
                return;
            }

            var suspect = CompositeBuilder.SuspectFor(caseSeed, _catalog, _wardrobeOdds, _crowd.Gait);
            var outfit = OutfitPlanner.CharacterOutfit(crowdSeed, _crowd.NpcCount, 0, _catalog, _wardrobeOdds, suspect.Body);
            var at = NetworkManager.ServerTime.Time - _watcher.WitnessReportAge;
            WitnessRpc(Clothes.Of(outfit), at, RpcTarget.Single(w, RpcTargetUse.Temp));
        }

        /// <summary>The Watcher's own mark: this person's clothes, now. Local — the server never hears of it.</summary>
        public void Mark(Outfit outfit)
        {
            LastSeen = Sighting.Of(outfit, NetworkManager.ServerTime.Time, SightingSource.Mark);
            Changed?.Invoke();
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void WitnessRpc(Clothes clothes, double at, RpcParams rpcParams)
        {
            LastSeen = new Sighting(clothes.Top.ToWorn(), clothes.Bottom.ToWorn(), clothes.Hat.ToWorn(), at, SightingSource.Witness);
            Changed?.Invoke();
        }

        /// <summary>Server: build the case's composite and hand each role its copy.</summary>
        public void BeginCase(int caseSeed, int rounds, ulong? watcher, ulong? fugitive)
        {
            if (!IsServer)
            {
                return;
            }

            var suspect = CompositeBuilder.SuspectFor(caseSeed, _catalog, _wardrobeOdds, _crowd.Gait);
            _full = CompositeBuilder.Build(caseSeed, suspect, _catalog, _crowd.Gait, _config, rounds);
            ClearRpc();
            if (watcher is { } w)
            {
                ReceiveRpc(Message.Of(_full.ForWatcher()), false, RpcTarget.Single(w, RpcTargetUse.Temp));
            }

            if (fugitive is { } f)
            {
                ReceiveRpc(Message.Of(_full), true, RpcTarget.Single(f, RpcTargetUse.Temp));
            }

            Debug.Log($"[Composite] Case {caseSeed}: {_full.Claims.Count} traits, " +
                      $"{CountWrong(_full)} wrong; sent to Watcher {watcher?.ToString() ?? "-"}, fugitive {fugitive?.ToString() ?? "-"}.");
        }

        /// <summary>Server: what the composite has revealed by this round (D-008) — the lookalikes copy it.</summary>
        public List<CompositeClaim> Revealed(int round) => _full?.Revealed(round, _config.Schedule) ?? new List<CompositeClaim>();

        [Rpc(SendTo.Everyone)]
        private void ClearRpc()
        {
            Sketch = null;
            LastSeen = null;
            SeesErrors = false;
            Changed?.Invoke();
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void ReceiveRpc(Message message, bool seesErrors, RpcParams rpcParams)
        {
            Sketch = message.ToSketch();
            SeesErrors = seesErrors;
            Changed?.Invoke();
        }

        private static int CountWrong(CompositeSketch sketch)
        {
            var wrong = 0;
            foreach (var claim in sketch.Claims)
            {
                wrong += claim.Wrong ? 1 : 0;
            }

            return wrong;
        }

        /// <summary>A sketch on the wire: per claim the trait, values, confidence and walks.</summary>
        public struct Message : INetworkSerializable
        {
            private Claim[] _claims;

            public static Message Of(CompositeSketch sketch)
            {
                var claims = new Claim[sketch.Claims.Count];
                for (var i = 0; i < claims.Length; i++)
                {
                    var c = sketch.Claims[i];
                    claims[i] = new Claim
                    {
                        Trait = (byte)c.Trait,
                        Value = (short)c.Value,
                        Confidence = (byte)c.Confidence,
                        Wrong = c.Wrong,
                        True = (short)c.True,
                        Walk = Walk.Of(c.Walk),
                        TrueWalk = Walk.Of(c.TrueWalk),
                    };
                }

                return new Message { _claims = claims };
            }

            public CompositeSketch ToSketch()
            {
                var claims = new CompositeClaim[_claims.Length];
                for (var i = 0; i < claims.Length; i++)
                {
                    var c = _claims[i];
                    claims[i] = new CompositeClaim((CompositeTrait)c.Trait, c.Value, c.Walk.ToSignature(), (Confidence)c.Confidence,
                        c.Wrong, c.True, c.TrueWalk.ToSignature());
                }

                return new CompositeSketch(claims);
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                var count = _claims?.Length ?? 0;
                serializer.SerializeValue(ref count);
                if (serializer.IsReader)
                {
                    _claims = new Claim[count];
                }

                for (var i = 0; i < count; i++)
                {
                    serializer.SerializeValue(ref _claims[i]);
                }
            }
        }

        /// <summary>A top, bottom and hat on the wire.</summary>
        public struct Clothes : INetworkSerializable
        {
            public Garment Top;
            public Garment Bottom;
            public Garment Hat;

            public static Clothes Of(Outfit outfit) =>
                new() { Top = Garment.Of(outfit.Top), Bottom = Garment.Of(outfit.Bottom), Hat = Garment.Of(outfit.Hat) };

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Top);
                serializer.SerializeValue(ref Bottom);
                serializer.SerializeValue(ref Hat);
            }
        }

        public struct Garment : INetworkSerializable
        {
            public short Item;
            public byte Hue;
            public byte Tone;

            public static Garment Of(Worn worn) => new() { Item = (short)worn.Item, Hue = (byte)worn.Hue, Tone = (byte)worn.Tone };

            public Worn ToWorn() => new(Item, Hue, (Tone)Tone);

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Item);
                serializer.SerializeValue(ref Hue);
                serializer.SerializeValue(ref Tone);
            }
        }

        private struct Claim : INetworkSerializable
        {
            public byte Trait;
            public short Value;
            public byte Confidence;
            public bool Wrong;
            public short True;
            public Walk Walk;
            public Walk TrueWalk;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Trait);
                serializer.SerializeValue(ref Value);
                serializer.SerializeValue(ref Confidence);
                serializer.SerializeValue(ref Wrong);
                serializer.SerializeValue(ref True);
                serializer.SerializeValue(ref Walk);
                serializer.SerializeValue(ref TrueWalk);
            }
        }

        private struct Walk : INetworkSerializable
        {
            public bool Present;
            public byte Base;
            public byte Tempo;
            public byte Count;
            public byte TraitA;
            public float StrengthA;
            public byte TraitB;
            public float StrengthB;

            public static Walk Of(GaitSignature walk)
            {
                if (walk == null)
                {
                    return default;
                }

                var result = new Walk { Present = true, Base = (byte)walk.Base, Tempo = (byte)walk.Tempo, Count = (byte)walk.Traits.Count };
                if (walk.Traits.Count > 0)
                {
                    result.TraitA = (byte)walk.Traits[0].Trait;
                    result.StrengthA = walk.Traits[0].Strength;
                }

                if (walk.Traits.Count > 1)
                {
                    result.TraitB = (byte)walk.Traits[1].Trait;
                    result.StrengthB = walk.Traits[1].Strength;
                }

                return result;
            }

            public GaitSignature ToSignature()
            {
                if (!Present)
                {
                    return null;
                }

                var traits = new List<TraitStrength>();
                if (Count > 0)
                {
                    traits.Add(new TraitStrength((WalkTrait)TraitA, StrengthA));
                }

                if (Count > 1)
                {
                    traits.Add(new TraitStrength((WalkTrait)TraitB, StrengthB));
                }

                return new GaitSignature((BaseWalk)Base, (Tempo)Tempo, traits);
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Present);
                serializer.SerializeValue(ref Base);
                serializer.SerializeValue(ref Tempo);
                serializer.SerializeValue(ref Count);
                serializer.SerializeValue(ref TraitA);
                serializer.SerializeValue(ref StrengthA);
                serializer.SerializeValue(ref TraitB);
                serializer.SerializeValue(ref StrengthB);
            }
        }
    }
}
