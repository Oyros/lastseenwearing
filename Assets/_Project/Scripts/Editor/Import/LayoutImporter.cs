using System;
using System.IO;
using System.Linq;
using LastSeenWearing.Core.Layouts;
using UnityEditor;
using UnityEngine;

namespace LastSeenWearing.Editor.Import
{
    /// <summary>
    /// A festival layout from the art export (PL.22, P1.17): the layout FBX imports with mesh colliders (every
    /// greybox piece is something to walk on or bump into) and without its cameras and lights, and its sidecar
    /// JSON becomes the layout's
    /// <see cref="LayoutDefinition"/> under <c>Data/Layouts/</c>. Rebuild after an art re-export: menu, or
    /// <see cref="Import"/>.
    /// </summary>
    public sealed class LayoutImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/_Project/Art/Models/Festival/";
        public const string DefinitionFolder = "Assets/_Project/Data/Layouts/";
        private const string FilePrefix = "LSW_Festival_Layout_";

        // Bump with every change to what this writes (see HumanoidImportPostprocessor).
        private const uint Version = 2; // 2: no cameras or lights from the FBX

        public override uint GetVersion() => Version;

        public static string ModelPath(string id) => $"{Folder}{FilePrefix}{id}.fbx";

        public static string DefinitionPath(string id) => $"{DefinitionFolder}Layout_{id}.asset";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder) || !Path.GetFileName(assetPath).StartsWith(FilePrefix))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.addCollider = true;
            // The art's layout cameras come as data (the JSON); imported as Unity cameras they would render over
            // every player's view. Lights are the scene's.
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        [MenuItem("Last Seen Wearing/Layouts/Import Layout A")]
        public static void ImportA()
        {
            EditorGUIUtility.PingObject(Import("A"));
        }

        public static LayoutDefinition Import(string id)
        {
            var json = JsonUtility.FromJson<LayoutJson>(File.ReadAllText(Path.ChangeExtension(ModelPath(id), ".json")));
            var zoom = Array.FindIndex(json.cameras, c => c.zoom);
            if (zoom < 0)
            {
                // Until the art marks one ("zoom": true on a camera, PL.30), the first camera zooms.
                Debug.LogWarning($"[LayoutImporter] Layout {id}: no camera is marked \"zoom\" in the JSON; {json.cameras[0].name} zooms.");
                zoom = 0;
            }

            var path = DefinitionPath(id);
            var definition = AssetDatabase.LoadAssetAtPath<LayoutDefinition>(path);
            if (definition == null)
            {
                Directory.CreateDirectory(DefinitionFolder);
                definition = ScriptableObject.CreateInstance<LayoutDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Set(
                json.layout,
                json.title,
                json.cameras.Select(c => new LayoutDefinition.CameraSpot
                {
                    Name = c.name,
                    Position = Vec(c.pos),
                    Forward = Vec(c.forward).normalized,
                    VerticalFieldOfView = c.fov_vertical_16x9,
                }).ToArray(),
                zoom,
                json.targets.Select(t => new LayoutDefinition.TargetSpot
                {
                    Kind = Kind(t.type),
                    Tag = t.tag,
                    Position = Vec(t.pos),
                    Yaw = t.yaw,
                }).ToArray(),
                json.tents.Select(t => new LayoutDefinition.Spot { Name = t.name, Position = Vec(t.pos), Yaw = t.yaw }).ToArray(),
                json.exits.Select(e => new LayoutDefinition.Spot { Name = e.tag, Position = Vec(e.pos), Yaw = e.yaw }).ToArray(),
                json.spawn.Select(Area).ToArray(),
                Area(json.bounds));
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            return definition;
        }

        private static Vector3 Vec(float[] v) => new(v[0], v[1], v[2]);

        private static LayoutDefinition.Area Area(RectJson r) => new() { X0 = r.x0, Z0 = r.z0, X1 = r.x1, Z1 = r.z1 };

        private static TargetKind Kind(string type) => type switch
        {
            "open" => TargetKind.Open,
            "fixed" => TargetKind.Fixed,
            "social" => TargetKind.Social,
            "hidden" => TargetKind.Hidden,
            _ => throw new InvalidOperationException($"[LayoutImporter] Unknown target type '{type}'."),
        };

#pragma warning disable CS0649 // JsonUtility fills these
        [Serializable]
        private sealed class LayoutJson
        {
            public string layout;
            public string title;
            public CameraJson[] cameras;
            public TargetJson[] targets;
            public SpotJson[] tents;
            public SpotJson[] exits;
            public RectJson[] spawn;
            public RectJson bounds;
        }

        [Serializable]
        private sealed class CameraJson
        {
            public string name;
            public float[] pos;
            public float[] forward;
            public float fov_vertical_16x9;
            public bool zoom;
        }

        [Serializable]
        private sealed class TargetJson
        {
            public string type;
            public string tag;
            public float[] pos;
            public float yaw;
        }

        [Serializable]
        private sealed class SpotJson
        {
            public string name;
            public string tag;
            public float[] pos;
            public float yaw;
        }

        [Serializable]
        private sealed class RectJson
        {
            public float x0;
            public float z0;
            public float x1;
            public float z1;
        }
#pragma warning restore CS0649
    }
}
