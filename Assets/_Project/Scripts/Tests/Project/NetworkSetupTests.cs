using System.Linq;
using LastSeenWearing.Gameplay.Network;
using LastSeenWearing.Gameplay.Player;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastSeenWearing.Tests.Project
{
    /// <summary>
    /// P0.10: the bootstrap carries the session — a NetworkManager that spawns the player capsule,
    /// both transports and a wired <see cref="NetworkSession"/> (D-002, D-016).
    /// </summary>
    public sealed class NetworkSetupTests
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/PlayerCapsule.prefab";

        [Test]
        public void ThePlayerPrefabIsAnOwnerMovedNetworkObject()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"no player prefab at {PlayerPrefabPath}");
            Assert.That(prefab.GetComponent<NetworkObject>(), Is.Not.Null, "no NetworkObject");

            var networkTransform = prefab.GetComponent<NetworkTransform>();
            Assert.That(networkTransform, Is.Not.Null, "no NetworkTransform");
            Assert.That(networkTransform.AuthorityMode, Is.EqualTo(NetworkTransform.AuthorityModes.Owner));

            var mover = prefab.GetComponent<CapsuleMover>();
            Assert.That(mover, Is.Not.Null, "no CapsuleMover");
            Assert.That(new SerializedObject(mover).FindProperty("_config").objectReferenceValue, Is.Not.Null,
                "CapsuleMover has no MovementConfig");
        }

        [Test]
        public void TheBootstrapCarriesAWiredSession()
        {
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            try
            {
                var sessions = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<NetworkSession>(true))
                    .ToArray();
                Assert.That(sessions, Has.Length.EqualTo(1), "Bootstrap.unity must hold exactly one NetworkSession");

                var session = new SerializedObject(sessions[0]);
                foreach (var field in new[] { "_networkManager", "_localTransport", "_steamTransport", "_lobbyConfig" })
                {
                    Assert.That(session.FindProperty(field).objectReferenceValue, Is.Not.Null, $"NetworkSession.{field} is not assigned");
                }

                Assert.That(session.FindProperty("_sessionSceneName").stringValue, Is.Not.Empty, "no session scene");

                var networkManager = sessions[0].GetComponent<NetworkManager>();
                Assert.That(networkManager, Is.Not.Null, "NetworkSession is not next to the NetworkManager");
                // P1.10: bodies are the round's — RoundDirector spawns them by role; nothing spawns on connect.
                Assert.That(networkManager.NetworkConfig.PlayerPrefab, Is.Null, "the NetworkManager must not auto-spawn players");
                var capsule = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                Assert.That(networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Any(list => list.PrefabList.Any(p => p.Prefab == capsule)), Is.True,
                    "the player capsule must be a registered network prefab");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
