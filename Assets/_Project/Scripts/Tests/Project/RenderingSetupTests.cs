using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastSeenWearing.Tests.Project
{
    /// <summary>
    /// The project renders with URP and skins on the GPU (D-023). Both were missing until P1.04
    /// measured a 150-body crowd at ~10 fps; nothing caught it because Built-in still draws.
    /// </summary>
    public sealed class RenderingSetupTests
    {
        private const string PipelinePath = "Assets/_Project/Settings/Rendering/LSW_URP.asset";

        [Test]
        public void UrpIsTheDefaultPipeline()
        {
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline), Is.EqualTo(PipelinePath));
        }

        [Test]
        public void EveryQualityLevelUsesUrp()
        {
            var current = QualitySettings.GetQualityLevel();
            try
            {
                for (var level = 0; level < QualitySettings.names.Length; level++)
                {
                    QualitySettings.SetQualityLevel(level, false);
                    Assert.That(AssetDatabase.GetAssetPath(QualitySettings.renderPipeline), Is.EqualTo(PipelinePath),
                        $"quality level {QualitySettings.names[level]}");
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(current, false);
            }
        }

        [Test]
        public void SkinningRunsOnTheGpu()
        {
            Assert.That(PlayerSettings.meshDeformation, Is.Not.EqualTo(MeshDeformation.CPU));
        }

        [Test]
        public void NoBuildSceneOrPrefabUsesABuiltInShader()
        {
            var offenders = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(path => BuiltIn(AssetDatabase.LoadAssetAtPath<GameObject>(path), path))
                .ToList();

            foreach (var entry in EditorBuildSettings.scenes)
            {
                var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Additive);
                try
                {
                    offenders.AddRange(scene.GetRootGameObjects().SelectMany(root => BuiltIn(root, entry.path)));
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            Assert.That(offenders, Is.Empty, "Built-in shaders render pink under URP");
        }

        private static System.Collections.Generic.IEnumerable<string> BuiltIn(GameObject root, string where)
        {
            return root.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials.Where(m => m != null && m.shader.name == "Standard")
                    .Select(m => $"{where}: {renderer.name} [{m.name}]"));
        }
    }
}
