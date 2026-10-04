using System.Linq;
using LastSeenWearing.Core.Layouts;
using LastSeenWearing.Gameplay.Objective;
using LastSeenWearing.Gameplay.Roles;
using LastSeenWearing.Gameplay.Round;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace LastSeenWearing.Editor.Import
{
    /// <summary>
    /// Puts a <see cref="TargetSpot"/> on the walkable ground beside every target prop of the open scene's layout,
    /// and hands <see cref="Objectives"/> the targets and the exits (P1.22). Re-running keeps the spots already there
    /// (their network ids stay), moves them onto the layout and adds or removes the difference.
    /// </summary>
    public static class TargetPlacer
    {
        // A target prop is solid; its spot is the nearest baked ground within this reach. Geometry, not tuning.
        public const float GroundReach = 2.5f;
        private const float TriggerRadius = 0.5f;
        private const string GameConfigPath = "Assets/_Project/Data/Config/GameConfig.asset";

        [MenuItem("Last Seen Wearing/Layouts/Place Targets In Open Scene")]
        public static void PlaceInOpenScene()
        {
            var objectives = Object.FindFirstObjectByType<Objectives>();
            var definition = TentPlacer.FindLayout();
            if (objectives == null || definition == null)
            {
                Debug.LogError("[TargetPlacer] The open scene needs an Objectives and a layout (Festival_<id>).");
                return;
            }

            Place(definition, objectives);
            EditorSceneManager.MarkSceneDirty(objectives.gameObject.scene);
        }

        public static TargetSpot[] Place(LayoutDefinition definition, Objectives objectives)
        {
            var game = AssetDatabase.LoadAssetAtPath<Core.Config.GameConfig>(GameConfigPath);
            var roster = Object.FindFirstObjectByType<RoleRosterSync>();
            var director = Object.FindFirstObjectByType<RoundDirector>();

            var existing = Object.FindObjectsByType<TargetSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var placed = new TargetSpot[definition.Targets.Length];
            foreach (var spot in existing)
            {
                var index = new SerializedObject(spot).FindProperty("_index").intValue;
                if (index >= 0 && index < placed.Length && placed[index] == null)
                {
                    placed[index] = spot;
                }
                else
                {
                    Object.DestroyImmediate(spot.gameObject);
                }
            }

            for (var i = 0; i < placed.Length; i++)
            {
                var target = definition.Targets[i];
                if (placed[i] == null)
                {
                    var created = new GameObject(target.Tag, typeof(NetworkObject), typeof(SphereCollider), typeof(TargetSpot));
                    placed[i] = created.GetComponent<TargetSpot>();
                }

                var spot = placed[i];
                var ground = NavMesh.SamplePosition(target.Position, out var hit, GroundReach, NavMesh.AllAreas) ? hit.position : target.Position;
                spot.name = $"Target_{target.Tag}";
                spot.transform.SetPositionAndRotation(ground, Quaternion.Euler(0f, target.Yaw, 0f));
                spot.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // never in an aim or a mark
                var trigger = spot.GetComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = TriggerRadius;

                var so = new SerializedObject(spot);
                so.FindProperty("_index").intValue = i;
                so.FindProperty("_kind").enumValueIndex = (int)target.Kind;
                so.FindProperty("_objectives").objectReferenceValue = objectives;
                so.FindProperty("_roster").objectReferenceValue = roster;
                so.FindProperty("_director").objectReferenceValue = director;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var wiring = new SerializedObject(objectives);
            wiring.FindProperty("_config").objectReferenceValue = game.Fugitive;
            var targets = wiring.FindProperty("_targets");
            targets.arraySize = placed.Length;
            for (var i = 0; i < placed.Length; i++)
            {
                targets.GetArrayElementAtIndex(i).objectReferenceValue = placed[i];
            }

            var exits = wiring.FindProperty("_exits");
            var names = wiring.FindProperty("_exitNames");
            exits.arraySize = names.arraySize = definition.Exits.Length;
            for (var i = 0; i < definition.Exits.Length; i++)
            {
                exits.GetArrayElementAtIndex(i).vector3Value = definition.Exits[i].Position;
                names.GetArrayElementAtIndex(i).stringValue = definition.Exits[i].Name;
            }

            wiring.ApplyModifiedPropertiesWithoutUndo();
            return placed.ToArray();
        }
    }
}
