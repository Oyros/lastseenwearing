using System.Linq;
using LastSeenWearing.Core.Capture;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Core.Round;
using LastSeenWearing.Gameplay.Crowd;
using LastSeenWearing.Gameplay.Player;
using LastSeenWearing.Gameplay.Round;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSeenWearing.Tests.Roles
{
    /// <summary>
    /// P2.01: the plainclothes (GDD §03) — a field officer in first person, dressed like a stranger from the round's
    /// seed, at the crowd's pace, who cannot arrest.
    /// </summary>
    public sealed class PlainclothesTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Characters/Plainclothes.prefab";
        private const string PatrolPath = "Assets/_Project/Prefabs/Characters/Patrol.prefab";

        [Test]
        public void OnlyThePatrolCuffsAndOnlyInOpenPlay()
        {
            Assert.That(ArrestRules.MayArrest(Role.Patrol, RoundPhase.Live), Is.True);
            Assert.That(ArrestRules.MayArrest(Role.Plainclothes, RoundPhase.Live), Is.False, "GDD §03: cannot arrest");
            Assert.That(ArrestRules.MayArrest(Role.Dog, RoundPhase.Live), Is.False);
            Assert.That(ArrestRules.MayArrest(Role.Patrol, RoundPhase.LastCuff), Is.False, "no arrests in the chase");
        }

        [Test]
        public void ThePlainclothesIsAnOwnerMovedStrangerWhoCannotArrest()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab.GetComponent<NetworkObject>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<NetworkTransform>().AuthorityMode, Is.EqualTo(NetworkTransform.AuthorityModes.Owner));
            Assert.That(prefab.GetComponent<FugitiveController>(), Is.Null);

            var officer = prefab.GetComponent<FieldOfficerController>();
            Assert.That(officer.Role, Is.EqualTo(Role.Plainclothes));
            Assert.That(officer.CanArrest, Is.False);
            var serialized = new SerializedObject(officer);
            foreach (var field in new[] { "_movement", "_camera", "_crowd", "_fpArmsPrefab", "_wardrobeOdds" })
            {
                Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            }

            Assert.That(prefab.GetComponent<OutfitView>(), Is.Not.Null, "dressed from the seed, like a stranger");
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(PatrolPath).GetComponent<FieldOfficerController>().CanArrest, Is.True);

            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
            Assert.That(list.PrefabList.Any(p => p.Prefab == prefab), Is.True, "registered network prefab");
        }

        [Test]
        public void ThePlainclothesWalksAtTheCrowdsPaceAndRunsSlowerThanThePatrol()
        {
            var movement = AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/_Project/Data/Config/MovementConfig.asset");
            Assert.That(movement.PlainclothesWalkSpeed, Is.EqualTo(movement.WalkSpeed).Within(0.15f), "passes for one of the crowd");
            Assert.That(movement.PlainclothesRunSpeed, Is.LessThan(movement.PatrolRunSpeed), "the patrol is the fast one (GDD §03)");
        }

        [Test]
        public void AFourPlayerCaseHasAPlainclothesWithABodyAndAPlace()
        {
            Assert.That(RoleRules.RolesFor(4), Does.Contain(Role.Plainclothes), "D-007");

            const string path = "Assets/_Project/Scenes/Festival_A.unity";
            var already = SceneManager.GetSceneByPath(path);
            var scene = already.isLoaded ? already : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var director = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RoundDirector>(true)).Single();
                var spawns = new SerializedObject(director).FindProperty("_spawns");
                var found = false;
                for (var i = 0; i < spawns.arraySize; i++)
                {
                    var entry = spawns.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("Role").enumValueIndex != (int)Role.Plainclothes)
                    {
                        continue;
                    }

                    found = true;
                    Assert.That(entry.FindPropertyRelative("Point").objectReferenceValue, Is.Not.Null);
                    Assert.That(AssetDatabase.GetAssetPath(entry.FindPropertyRelative("Prefab").objectReferenceValue), Is.EqualTo(PrefabPath));
                }

                Assert.That(found, Is.True);
            }
            finally
            {
                if (!already.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
