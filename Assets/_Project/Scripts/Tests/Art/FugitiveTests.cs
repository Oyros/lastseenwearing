using System.Collections.Generic;
using System.Linq;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Gameplay.Crowd;
using LastSeenWearing.Gameplay.Interaction;
using LastSeenWearing.Gameplay.Player;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace LastSeenWearing.Tests.Art
{
    /// <summary>
    /// P1.11: the fugitive moves like an NPC by default — the same body, the same animator driven by the same
    /// <see cref="WalkCycle"/>, the crowd's config, and a walk no NPC in its crowd shares (GDD §05).
    /// </summary>
    public sealed class FugitiveTests
    {
        private const string FugitivePath = "Assets/_Project/Prefabs/Characters/Fugitive.prefab";
        private const string NpcPath = "Assets/_Project/Prefabs/Characters/CrowdNpc.prefab";
        private const string CrowdConfigPath = "Assets/_Project/Data/Config/CrowdConfig.asset";

        private static readonly GaitSettings Settings = new(0.25f, 0.45f, 0.30f, 0.4f);

        [Test]
        public void TheFugitiveIsAnOwnerMovedCrowdBody()
        {
            var fugitive = AssetDatabase.LoadAssetAtPath<GameObject>(FugitivePath);
            var npc = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPath);
            Assert.That(fugitive.GetComponent<NetworkObject>(), Is.Not.Null);
            Assert.That(fugitive.GetComponent<NetworkTransform>().AuthorityMode, Is.EqualTo(NetworkTransform.AuthorityModes.Owner));
            Assert.That(fugitive.GetComponent<CharacterController>(), Is.Not.Null);

            var controller = fugitive.GetComponent<FugitiveController>();
            Assert.That(controller, Is.Not.Null);
            var serialized = new SerializedObject(controller);
            Assert.That(serialized.FindProperty("_crowd").objectReferenceValue,
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<CrowdConfig>(CrowdConfigPath)), "it walks on the crowd's config");
            foreach (var field in new[] { "_movement", "_camera", "_cameraPivot" })
            {
                Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            }

            var fugitiveAnimator = fugitive.GetComponentInChildren<Animator>();
            var npcAnimator = npc.GetComponentInChildren<Animator>();
            Assert.That(fugitiveAnimator.runtimeAnimatorController, Is.SameAs(npcAnimator.runtimeAnimatorController), "same animator");
            Assert.That(fugitiveAnimator.avatar, Is.SameAs(npcAnimator.avatar), "same body");
            Assert.That(VisibleParts(fugitive), Is.EqualTo(VisibleParts(npc)), "the same visible parts as an NPC");
        }

        [Test]
        public void TheFugitiveIsARegisteredNetworkPrefab()
        {
            var fugitive = AssetDatabase.LoadAssetAtPath<GameObject>(FugitivePath);
            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
            Assert.That(list.PrefabList.Any(p => p.Prefab == fugitive), Is.True);
        }

        [TestCase(1)]
        [TestCase(2026)]
        public void TheFugitivesWalkIsNoNpcsWalk(int seed)
        {
            var crowd = GaitPlanner.SignaturesFor(seed, 150, Settings);
            var fugitive = GaitPlanner.CharacterSignature(seed, 150, 0, Settings);
            Assert.That(crowd, Has.No.Member(fugitive));
            Assert.That(GaitPlanner.SignaturesFor(seed, 151, Settings).Take(150), Is.EqualTo(crowd), "the crowd is unchanged by it");
        }

        [Test]
        public void TheFugitiveAndAnNpcAnimateAlikeOnTheSameWalk()
        {
            var fugitive = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FugitivePath));
            var npc = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(NpcPath));
            try
            {
                var gait = new GaitSignature(BaseWalk.Heavy, Tempo.Slow,
                    new[] { new TraitStrength(WalkTrait.Limp, -0.5f), new TraitStrength(WalkTrait.Bounce, 1f) });
                var a = fugitive.GetComponentInChildren<Animator>();
                var b = npc.GetComponentInChildren<Animator>();
                var walkA = new WalkCycle(a, 1f);
                var walkB = new WalkCycle(b, 1f);
                walkA.Apply(gait);
                walkB.Apply(gait);
                foreach (var walked in new[] { 0.0, 0.3, 0.9, 1.4, 1.4, 1.4, 2.2 })
                {
                    walkA.Advance(walked, 0.1f);
                    walkB.Advance(walked, 0.1f);
                    Assert.That(a.GetFloat(WalkCycle.PhaseParameter), Is.EqualTo(b.GetFloat(WalkCycle.PhaseParameter)));
                    Assert.That(a.GetBool(WalkCycle.WalkingParameter), Is.EqualTo(b.GetBool(WalkCycle.WalkingParameter)));
                    for (var layer = 0; layer < a.layerCount; layer++)
                    {
                        Assert.That(a.GetLayerWeight(layer), Is.EqualTo(b.GetLayerWeight(layer)), a.GetLayerName(layer));
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(fugitive);
                Object.DestroyImmediate(npc);
            }
        }

        [Test]
        public void InteractUsesTheNearestThingInRangeThatLetsYouIn()
        {
            var near = new Fake(new Vector3(0f, 0f, 1f), true);
            var nearer = new Fake(new Vector3(0f, 0f, 0.5f), true);
            var closedNearest = new Fake(new Vector3(0f, 0f, 0.2f), false);
            var far = new Fake(new Vector3(0f, 0f, 3f), true);
            var candidates = new List<IInteractable> { near, nearer, closedNearest, far };
            Assert.That(Interactor.Nearest(Vector3.zero, candidates, 1.6f, 1), Is.SameAs(nearer));
            Assert.That(Interactor.Nearest(Vector3.zero, new List<IInteractable> { far }, 1.6f, 1), Is.Null, "out of range");
        }

        private static string[] VisibleParts(GameObject root) =>
            root.GetComponentsInChildren<SkinnedMeshRenderer>(false).Select(r => r.name).OrderBy(n => n).ToArray();

        private sealed class Fake : IInteractable
        {
            private readonly bool _open;

            public Fake(Vector3 point, bool open)
            {
                InteractionPoint = point;
                _open = open;
            }

            public Vector3 InteractionPoint { get; }

            public bool CanInteract(ulong clientId) => _open;

            public void Interact(ulong clientId)
            {
            }
        }
    }
}
