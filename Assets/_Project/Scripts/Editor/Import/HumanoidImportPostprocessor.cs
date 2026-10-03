using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LastSeenWearing.Editor.Import
{
    /// <summary>
    /// Imports every rigged <c>LSW_*</c> model under <c>Art/Models/</c> as a Humanoid with an explicit
    /// avatar: <see cref="HumanoidBoneMap"/> for the bones, never Unity's auto-mapping (P1.03, D-020).
    /// "Rigged" is the art export's own word: the model's sidecar JSON (lsw_export.py) says
    /// <c>"kind": "rigged"</c>, so festival props under the same folder are left alone. No animation —
    /// clips come from their own files. Shape keys (builds, PL.09) come in as blend shapes.
    ///
    /// Two passes, because <see cref="HumanDescription.skeleton"/> needs the rest pose and the rest
    /// pose only exists after an import: the first pass imports Generic and records the hierarchy,
    /// then a reimport builds the Humanoid avatar from it. Once the skeleton is stored in the .meta,
    /// every later import is a single pass. The reimport is issued from
    /// <c>OnPostprocessAllAssets</c>, so it runs inside the same import — never on a later editor
    /// tick, which an unfocused editor, a batch build or CI might not reach.
    ///
    /// A-pose art, T-pose avatar (D-020): the mesh and bind pose stay in the file's A-pose; the
    /// skeleton written to the avatar is <see cref="TPoseBuilder"/>'s T-pose built from it, so
    /// characters and clips share one T-pose definition.
    ///
    /// Animation files (<c>LSW_A_*</c> under <c>Art/Animations/</c>, P1.04) get the same avatar the
    /// same way — the same bone table, the same T-pose from the rigged body they were made on — and
    /// keep their animation.
    /// Ported from Borrowed Crown's importer (PL.16 / D-030, D-031 there).
    /// </summary>
    public sealed class HumanoidImportPostprocessor : AssetPostprocessor
    {
        // Unity reuses a cached import result unless this changes: bump it with every change to what
        // this postprocessor writes, or a model can come back from the cache imported by the old rules.
        private const uint Version = 1;

        public override uint GetVersion()
        {
            return Version;
        }

        public const string ModelsFolder = "Assets/_Project/Art/Models/";
        public const string AnimationsFolder = "Assets/_Project/Art/Animations/";
        private const string ModelPrefix = "LSW_";
        private const string ClipPrefix = "LSW_A_";
        private const string RiggedKind = "rigged";

        // Unity's own defaults for a new Humanoid avatar — kept, not tuned.
        private const float DefaultTwist = 0.5f;
        private const float DefaultStretch = 0.05f;
        private const float DefaultFeetSpacing = 0f;

        // Skeletons to store once the current import has finished, by asset path.
        private static readonly Dictionary<string, SkeletonBone[]> Pending = new Dictionary<string, SkeletonBone[]>();

        [System.Serializable]
        private sealed class ExportSidecar
        {
            public string kind;
        }

        /// <summary>True for a rigged character body: <c>LSW_*.fbx</c> under Models whose sidecar JSON says rigged.</summary>
        public static bool Applies(string assetPath)
        {
            if (!assetPath.StartsWith(ModelsFolder)
                || !Path.GetFileName(assetPath).StartsWith(ModelPrefix)
                || Path.GetExtension(assetPath).ToLowerInvariant() != ".fbx")
            {
                return false;
            }

            var sidecar = Path.ChangeExtension(assetPath, ".json");
            if (!File.Exists(sidecar))
            {
                return false;
            }

            var data = JsonUtility.FromJson<ExportSidecar>(File.ReadAllText(sidecar));
            return data != null && data.kind == RiggedKind;
        }

        /// <summary>True for an animation file: <c>LSW_A_*.fbx</c> under <see cref="AnimationsFolder"/>.</summary>
        public static bool IsClip(string assetPath)
        {
            return assetPath.StartsWith(AnimationsFolder)
                && Path.GetFileName(assetPath).StartsWith(ClipPrefix)
                && Path.GetExtension(assetPath).ToLowerInvariant() == ".fbx";
        }

        private static bool Owns(string assetPath)
        {
            return IsClip(assetPath) || Applies(assetPath);
        }

        private void OnPreprocessModel()
        {
            if (!Owns(assetPath))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            if (IsClip(assetPath))
            {
                // A clip file: the animation is the point; any mesh in it is not used.
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importBlendShapes = false;
            }
            else
            {
                context.DependsOnSourceAsset(Path.ChangeExtension(assetPath, ".json"));
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.importBlendShapes = true;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importBlendShapeNormals = ModelImporterNormals.Calculate;
            }

            var description = importer.humanDescription;
            if (description.skeleton == null || description.skeleton.Length == 0)
            {
                // First pass: no rest pose recorded yet. Import Generic; OnPostprocessModel records it.
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            description.human = HumanoidBoneMap.ToHumanBones();
            description.upperArmTwist = DefaultTwist;
            description.lowerArmTwist = DefaultTwist;
            description.upperLegTwist = DefaultTwist;
            description.lowerLegTwist = DefaultTwist;
            description.armStretch = DefaultStretch;
            description.legStretch = DefaultStretch;
            description.feetSpacing = DefaultFeetSpacing;
            description.hasTranslationDoF = false;
            importer.humanDescription = description;
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!Owns(assetPath))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            if (!HasSkeleton(root))
            {
                // A mesh-only export (the armature was left out). It stays Generic; clear any stored
                // T-pose so the next import does not try to build a Humanoid from bones that are gone.
                var file = Path.GetFileName(assetPath);
                Debug.LogError($"[Humanoid] {file}: no skeleton in file — re-export from Blender.");
                if (importer.humanDescription.skeleton != null && importer.humanDescription.skeleton.Length > 0)
                {
                    Pending[assetPath] = new SkeletonBone[0];
                }

                return;
            }

            var rest = RestPose(root, out var parents);

            // A clip file has no bind pose to read: its transforms hold the first frame. Its rest is
            // the rigged body whose bones it was exported on (Borrowed Crown D-059).
            if (IsClip(assetPath))
            {
                rest = ClipRest(rest, root);
            }

            // Deterministic: the same rest pose always gives the same T-pose, so this comparison
            // settles after one reimport instead of looping.
            var skeleton = TPoseBuilder.Build(rest, parents);
            if (SameSkeleton(importer.humanDescription.skeleton, skeleton))
            {
                return;
            }

            Debug.Log($"[Humanoid] {Path.GetFileName(assetPath)}: T-pose built from the A-pose rest ({skeleton.Length} transforms); reimporting as Humanoid.");
            Pending[assetPath] = skeleton;
        }

        // Runs at the end of every import batch, still inside it: the reimports requested here are
        // processed before the import call that triggered them returns.
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Pending.Count == 0)
            {
                return;
            }

            var work = Pending.ToArray();
            Pending.Clear();
            foreach (var entry in work)
            {
                StoreTPoseAndReimport(entry.Key, entry.Value);
            }
        }

        /// <summary>True when the model carries the humanoid root bone.</summary>
        public static bool HasSkeleton(GameObject root)
        {
            return root.GetComponentsInChildren<Transform>(true).Any(t => t.name == HumanoidBoneMap.RootBone);
        }

        private static void StoreTPoseAndReimport(string path, SkeletonBone[] skeleton)
        {
            var model = (ModelImporter)AssetImporter.GetAtPath(path);
            if (model == null)
            {
                return;
            }

            var description = model.humanDescription;
            description.skeleton = skeleton;
            model.humanDescription = description;
            model.SaveAndReimport();
        }

        // Every transform of the imported model at its rest pose — the file's own A-pose — and
        // each one's parent name, which the T-pose needs to work in world space.
        private static SkeletonBone[] RestPose(GameObject root, out string[] parents)
        {
            var bones = new List<SkeletonBone>();
            var parentNames = new List<string>();
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                parentNames.Add(transform == root.transform || transform.parent == null ? null : transform.parent.name);
                bones.Add(new SkeletonBone
                {
                    name = transform.name,
                    position = transform.localPosition,
                    rotation = transform.localRotation,
                    scale = transform.localScale,
                });
            }

            parents = parentNames.ToArray();
            return bones.ToArray();
        }

        // The clip's own bone lengths with the rest rotations — and the hips' rest position — of the
        // rigged body whose bone lengths match it best.
        private SkeletonBone[] ClipRest(SkeletonBone[] clip, GameObject root)
        {
            var reference = ReferenceBody(clip, out var referencePath);
            if (reference == null)
            {
                Debug.LogWarning($"[Humanoid] {Path.GetFileName(assetPath)}: no rigged body matches its skeleton — its first frame is taken as its rest.");
                return clip;
            }

            context.DependsOnSourceAsset(referencePath);
            var rest = new SkeletonBone[clip.Length];
            for (var i = 0; i < clip.Length; i++)
            {
                rest[i] = clip[i];
                if (reference.TryGetValue(clip[i].name, out var bone))
                {
                    rest[i].rotation = bone.localRotation;
                    if (clip[i].name == HumanoidBoneMap.RootBone)
                    {
                        // Where the body's hips stand in the body, in the clip's hips' parent — not the
                        // body's local offset, which carries the armature's place in the Blender scene.
                        var clipHips = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == HumanoidBoneMap.RootBone);
                        var inBody = bone.root.InverseTransformPoint(bone.position);
                        rest[i].position = clipHips != null && clipHips.parent != null
                            ? clipHips.parent.InverseTransformPoint(inBody)
                            : inBody;
                    }
                }
            }

            return rest;
        }

        private static Dictionary<string, Transform> ReferenceBody(SkeletonBone[] clip, out string path)
        {
            path = null;
            Dictionary<string, Transform> best = null;
            var bestError = float.MaxValue;
            var candidates = Directory.Exists(ModelsFolder)
                ? Directory.GetFiles(ModelsFolder, ModelPrefix + "*.fbx", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')).Where(Applies)
                : Enumerable.Empty<string>();
            foreach (var candidate in candidates)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(candidate);
                if (model == null)
                {
                    continue;
                }

                var bones = new Dictionary<string, Transform>();
                foreach (var transform in model.GetComponentsInChildren<Transform>(true))
                {
                    bones[transform.name] = transform;
                }

                if (!bones.ContainsKey(HumanoidBoneMap.RootBone))
                {
                    continue;
                }

                var error = 0f;
                var shared = 0;
                foreach (var bone in clip)
                {
                    if (bone.name != HumanoidBoneMap.RootBone && bones.TryGetValue(bone.name, out var match))
                    {
                        error += (match.localPosition - bone.position).magnitude;
                        shared++;
                    }
                }

                if (shared > 20 && error < bestError)
                {
                    bestError = error;
                    best = bones;
                    path = candidate;
                }
            }

            return best;
        }

        /// <summary>True when both skeletons hold the same bones at the same rest, in any order.</summary>
        public static bool SameSkeleton(SkeletonBone[] stored, SkeletonBone[] current)
        {
            if (stored == null || stored.Length != current.Length)
            {
                return false;
            }

            // By name, not by index (Unity matches the skeleton by name; sibling order is not
            // guaranteed), and on normalised rotations: an off-unit quaternion reads as a turn
            // against itself, which can keep a model reimporting forever.
            var byName = new Dictionary<string, SkeletonBone>();
            foreach (var bone in stored)
            {
                byName[bone.name] = bone;
            }

            foreach (var bone in current)
            {
                if (!byName.TryGetValue(bone.name, out var before)
                    || (before.position - bone.position).sqrMagnitude > 1e-10f
                    || Quaternion.Angle(Quaternion.Normalize(before.rotation), Quaternion.Normalize(bone.rotation)) > 1e-3f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
