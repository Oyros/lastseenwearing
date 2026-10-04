using System.IO;
using System.Linq;
using LastSeenWearing.Core.Layouts;
using LastSeenWearing.Editor.Import;
using LastSeenWearing.Gameplay.Cameras;
using LastSeenWearing.Gameplay.Disguise;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LastSeenWearing.Tests.Layouts
{
    /// <summary>
    /// P1.17: layout A in Unity — the definition is the art's JSON, the scene's cameras stand where the layout
    /// puts them, and every target, tent and exit can be walked to from where the crowd walks (docs/LAYOUTS.md).
    /// </summary>
    public sealed class LayoutATests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Festival_A.unity";

        private static LayoutDefinition Definition => AssetDatabase.LoadAssetAtPath<LayoutDefinition>(LayoutImporter.DefinitionPath("A"));

        [Test]
        public void TheDefinitionIsTheArtsJson()
        {
            var json = File.ReadAllText(Path.ChangeExtension(LayoutImporter.ModelPath("A"), ".json"));
            var definition = Definition;
            Assert.That(definition.Id, Is.EqualTo("A"));
            Assert.That(definition.Cameras, Has.Length.EqualTo(4));
            Assert.That(definition.Targets, Has.Length.EqualTo(5));
            Assert.That(definition.Tents, Has.Length.EqualTo(2));
            Assert.That(definition.Exits, Has.Length.EqualTo(2));
            Assert.That(definition.Targets.Select(t => t.Kind).Distinct(), Is.EquivalentTo(new[] { TargetKind.Open, TargetKind.Fixed, TargetKind.Social, TargetKind.Hidden }),
                "every target job is on the layout (GDD §04.4)");
            Assert.That(json, Does.Contain("\"name\": \"CAM_A_1\""), "the JSON on disk is the one imported");
        }

        [Test]
        public void TheSceneIsTheFirstSceneAndItsCamerasStandWhereTheLayoutPutsThem()
        {
            var bootstrap = File.ReadAllText("Assets/_Project/Scenes/Bootstrap.unity");
            Assert.That(bootstrap, Does.Contain("_firstSceneName: Festival_A"));
            Assert.That(bootstrap, Does.Contain("_sessionSceneName: Festival_A"), "the editor session starts in it");

            WithScene(scene =>
            {
                var cameras = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CctvCamera>(true)).ToArray();
                Assert.That(cameras, Has.Length.EqualTo(Definition.Cameras.Length));
                foreach (var spot in Definition.Cameras)
                {
                    var camera = cameras.Single(c => c.name == spot.Name);
                    Assert.That(Vector3.Distance(camera.transform.position, spot.Position), Is.LessThan(0.01f), spot.Name);
                    Assert.That(Vector3.Angle(camera.transform.forward, spot.Forward), Is.LessThan(0.5f), spot.Name);
                    Assert.That(camera.GetComponent<Camera>().fieldOfView, Is.EqualTo(spot.VerticalFieldOfView).Within(0.01f), spot.Name);
                    Assert.That(camera.Profile, Is.Not.Null, spot.Name);
                }

                Assert.That(cameras.Select(c => c.Profile).Distinct().Count(), Is.GreaterThan(1), "two looks on the wall (P1.14)");
            });
        }

        [Test]
        public void EveryTargetTentAndExitCanBeWalkedTo()
        {
            WithScene(_ =>
            {
                var area = Definition.CrowdAreas[0];
                var from = Snap(new Vector3((area.X0 + area.X1) / 2f, 0f, (area.Z0 + area.Z1) / 2f));
                var places = Definition.Targets.Select(t => (t.Tag, t.Position))
                    .Concat(Definition.Tents.Select(t => (t.Name, t.Position)))
                    .Concat(Definition.Exits.Select(e => (e.Name, e.Position)));
                foreach (var (name, position) in places)
                {
                    var to = Snap(position);
                    var path = new NavMeshPath();
                    Assert.That(NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete,
                        Is.True, $"{name} at {position} is reachable on foot");
                }
            });
        }

        [Test]
        public void TheLayoutBringsNoCamerasOrLights()
        {
            var layout = AssetDatabase.LoadAssetAtPath<GameObject>(LayoutImporter.ModelPath("A"));
            Assert.That(layout.GetComponentsInChildren<Camera>(true), Is.Empty, "a Blender camera would render over the players' view");
            Assert.That(layout.GetComponentsInChildren<Light>(true), Is.Empty);
        }

        [Test]
        public void TheLayoutIsInMetres()
        {
            var layout = AssetDatabase.LoadAssetAtPath<GameObject>(LayoutImporter.ModelPath("A"));
            var bounds = layout.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Assert.That(bounds.size.x, Is.EqualTo(Definition.Bounds.X1 - Definition.Bounds.X0).Within(0.1f));
            Assert.That(bounds.size.z, Is.EqualTo(Definition.Bounds.Z1 - Definition.Bounds.Z0).Within(0.1f));

            // Each camera hangs at the top of its 5.7 m pole (docs/LAYOUTS.md).
            var poles = layout.GetComponentsInChildren<Renderer>().Where(r => r.name.Contains("CCTVPole")).ToArray();
            Assert.That(poles, Has.Length.EqualTo(Definition.Cameras.Length));
            foreach (var spot in Definition.Cameras)
            {
                var pole = poles.OrderBy(p => Flat(p.bounds.center - spot.Position)).First();
                Assert.That(Flat(pole.bounds.center - spot.Position), Is.LessThan(1f), $"{spot.Name} stands on a pole");
                Assert.That(pole.bounds.max.y, Is.EqualTo(spot.Position.y).Within(0.5f), $"{spot.Name} at the pole's top");
            }
        }

        [Test]
        public void EveryTentHasAChangingTentAtItsDoor()
        {
            WithScene(scene =>
            {
                var tents = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ChangingTent>(true)).ToArray();
                Assert.That(tents, Has.Length.EqualTo(Definition.Tents.Length), "P1.21");
                foreach (var spot in Definition.Tents)
                {
                    var tent = tents.Single(t => t.name == $"Tent_{spot.Name}");
                    var door = spot.Position + Quaternion.Euler(0f, spot.Yaw, 0f) * Vector3.forward * TentPlacer.DoorDistance;
                    Assert.That(Vector3.Distance(tent.transform.position, door), Is.LessThan(0.01f), spot.Name);
                    Assert.That(NavMesh.SamplePosition(tent.InteractionPoint, out _, 0.5f, NavMesh.AllAreas), Is.True,
                        $"{spot.Name}: the door is walkable ground");
                    Assert.That(tent.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("Ignore Raycast")), "never caught by an aim or a mark");
                }
            });
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        private static Vector3 Snap(Vector3 point)
        {
            // A target or a gate is a solid prop: the spot to reach is the walkable ground beside it.
            Assert.That(NavMesh.SamplePosition(point, out var hit, 2.5f, NavMesh.AllAreas), Is.True, $"walkable ground near {point}");
            return hit.position;
        }

        private static void WithScene(System.Action<Scene> body)
        {
            var already = SceneManager.GetSceneByPath(ScenePath);
            var scene = already.isLoaded ? already : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                body(scene);
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
