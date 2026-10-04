using System.IO;
using System.Reflection;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;

namespace LastSeenWearing.Tests.Project
{
    /// <summary>
    /// A network prefab saved by a tool can keep a stale <c>GlobalObjectIdHash</c> on disk: the editor that saved it
    /// recomputes the right one in memory, a Multiplayer Play Mode clone reads the stale one, and the clone is
    /// refused with "NetworkConfig mismatch" (hit in P1.11 and P1.18). The hash on disk must be the one NGO computes.
    /// </summary>
    public sealed class NetworkPrefabHashTests
    {
        [Test]
        public void EveryNetworkPrefabStoresTheHashNgoComputes()
        {
            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
            var validate = typeof(NetworkObject).GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var entry in list.PrefabList)
            {
                var path = AssetDatabase.GetAssetPath(entry.Prefab);
                var onDisk = File.ReadAllText(path);
                var networkObject = entry.Prefab.GetComponent<NetworkObject>();
                validate.Invoke(networkObject, null);
                Assert.That(onDisk, Does.Contain($"GlobalObjectIdHash: {networkObject.PrefabIdHash}\n").Or.Contain($"GlobalObjectIdHash: {networkObject.PrefabIdHash}\r\n"),
                    $"{path}: re-save it after NetworkObject.OnValidate");
            }
        }
    }
}
