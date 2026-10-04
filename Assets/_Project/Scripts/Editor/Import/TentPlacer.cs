using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Layouts;
using LastSeenWearing.Core.Wardrobe;
using LastSeenWearing.Gameplay.Crowd;
using LastSeenWearing.Gameplay.Disguise;
using LastSeenWearing.Gameplay.Roles;
using LastSeenWearing.Gameplay.Round;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastSeenWearing.Editor.Import
{
    /// <summary>
    /// Puts a <see cref="ChangingTent"/> at the door of every tent in the open scene's layout (P1.21). Re-running
    /// keeps the tents already there (their network ids stay), moves them onto the layout and adds or removes the
    /// difference.
    /// </summary>
    public static class TentPlacer
    {
        // The greybox kit's changing tent is 3 m square with its door on the spot's forward side: the tent's
        // interaction point stands just outside it. Geometry of the art, not tuning.
        public const float DoorDistance = 1.6f;
        private const float TriggerRadius = 0.5f;
        private const string GameConfigPath = "Assets/_Project/Data/Config/GameConfig.asset";

        [MenuItem("Last Seen Wearing/Layouts/Place Tents In Open Scene")]
        public static void PlaceInOpenScene()
        {
            var crowd = Object.FindFirstObjectByType<CrowdSpawner>();
            var definition = FindLayout();
            if (crowd == null || Object.FindFirstObjectByType<RoundDirector>() == null || definition == null)
            {
                Debug.LogError("[TentPlacer] The open scene needs a CrowdSpawner, a RoundDirector and a layout.");
                return;
            }

            Place(definition);
            EditorSceneManager.MarkSceneDirty(crowd.gameObject.scene);
        }

        public static ChangingTent[] Place(LayoutDefinition definition)
        {
            var game = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            var catalog = AssetDatabase.LoadAssetAtPath<WardrobeCatalog>(WardrobeImporter.CatalogPath);
            var crowd = Object.FindFirstObjectByType<CrowdSpawner>();
            var roster = Object.FindFirstObjectByType<RoleRosterSync>();
            var director = Object.FindFirstObjectByType<RoundDirector>();

            var existing = Object.FindObjectsByType<ChangingTent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var placed = new ChangingTent[definition.Tents.Length];
            foreach (var tent in existing)
            {
                var index = new SerializedObject(tent).FindProperty("_index").intValue;
                if (index >= 0 && index < placed.Length && placed[index] == null)
                {
                    placed[index] = tent;
                }
                else
                {
                    Object.DestroyImmediate(tent.gameObject);
                }
            }

            for (var i = 0; i < placed.Length; i++)
            {
                var spot = definition.Tents[i];
                if (placed[i] == null)
                {
                    var created = new GameObject(spot.Name, typeof(NetworkObject), typeof(SphereCollider), typeof(ChangingTent));
                    placed[i] = created.GetComponent<ChangingTent>();
                }

                var tent = placed[i];
                var facing = Quaternion.Euler(0f, spot.Yaw, 0f);
                tent.name = $"Tent_{spot.Name}";
                tent.transform.SetPositionAndRotation(spot.Position + facing * Vector3.forward * DoorDistance, facing);
                tent.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // never in an aim or a mark
                var trigger = tent.GetComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = TriggerRadius;

                var so = new SerializedObject(tent);
                so.FindProperty("_index").intValue = i;
                so.FindProperty("_config").objectReferenceValue = game.Disguise;
                so.FindProperty("_movement").objectReferenceValue = game.Movement;
                so.FindProperty("_crowdConfig").objectReferenceValue = game.Crowd;
                so.FindProperty("_wardrobeOdds").objectReferenceValue = game.Wardrobe;
                so.FindProperty("_catalog").objectReferenceValue = catalog;
                so.FindProperty("_crowd").objectReferenceValue = crowd;
                so.FindProperty("_roster").objectReferenceValue = roster;
                so.FindProperty("_director").objectReferenceValue = director;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return placed;
        }

        // Festival_<id> plays Layout_<id> (D-035).
        public static LayoutDefinition FindLayout()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            var id = scene.StartsWith("Festival_") ? scene.Substring("Festival_".Length) : null;
            return id == null ? null : AssetDatabase.LoadAssetAtPath<LayoutDefinition>($"Assets/_Project/Data/Layouts/Layout_{id}.asset");
        }
    }
}
