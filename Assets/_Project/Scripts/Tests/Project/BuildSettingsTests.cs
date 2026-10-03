using NUnit.Framework;
using UnityEditor;

namespace LastSeenWearing.Tests.Project
{
    /// <summary>
    /// P0.05: the build opens on the bootstrap, and every scene in the list exists.
    /// </summary>
    public sealed class BuildSettingsTests
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        [Test]
        public void BootstrapIsTheFirstScene()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.That(scenes, Is.Not.Empty, "the build list is empty");
            Assert.That(scenes[0].path, Is.EqualTo(BootstrapScenePath));
            Assert.That(scenes[0].enabled, Is.True, "Bootstrap is in the list but disabled");
        }

        [Test]
        public void EveryListedSceneExists()
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path), Is.Not.Null,
                    $"build list names a missing scene: {scene.path}");
            }
        }
    }
}
