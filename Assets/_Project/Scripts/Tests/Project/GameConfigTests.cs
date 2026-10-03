using System.Linq;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Gameplay.Bootstrap;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LastSeenWearing.Tests.Project
{
    /// <summary>
    /// P0.04, P0.05: one <see cref="GameConfig"/> root exists, and the bootstrap is handed it —
    /// a config the bootstrap cannot reach is a config no system can reach (docs/DATA.md §3).
    /// </summary>
    public sealed class GameConfigTests
    {
        private const string GameConfigPath = "Assets/_Project/Data/Config/GameConfig.asset";
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        [Test]
        public void TheGameConfigAssetExists()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath), Is.Not.Null,
                $"no GameConfig at {GameConfigPath}");
        }

        [Test]
        public void EveryDomainConfigIsAssigned()
        {
            var config = new SerializedObject(AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath));
            var field = config.GetIterator();
            field.NextVisible(true);
            while (field.NextVisible(false))
            {
                if (field.propertyType == SerializedPropertyType.ObjectReference)
                {
                    Assert.That(field.objectReferenceValue, Is.Not.Null, $"GameConfig.{field.name} is not assigned");
                }
            }
        }

        [Test]
        public void TheBootstrapHoldsTheGameConfig()
        {
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            try
            {
                var bootstraps = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<GameBootstrap>(true))
                    .ToArray();
                Assert.That(bootstraps, Has.Length.EqualTo(1), "Bootstrap.unity must hold exactly one GameBootstrap");
                Assert.That(bootstraps[0].GameConfig, Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath)),
                    "GameBootstrap is not handed the GameConfig asset");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
