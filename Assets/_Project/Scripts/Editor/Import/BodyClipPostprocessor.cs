using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LastSeenWearing.Editor.Import
{
    /// <summary>
    /// Sets the clips a rigged body carries (walks, additive layers, idle and one-shot actions, PL.11–PL.17) from
    /// the body's sidecar JSON, the single source (P1.04, D-022): each clip's frame range, whether it loops, and for
    /// an additive layer its reference-pose frame. Every clip plays in place — rotation, height and XZ
    /// baked into the pose, height from the feet — because the crowd moves the body itself and drives
    /// the walk phase from distance walked.
    /// </summary>
    public sealed class BodyClipPostprocessor : AssetPostprocessor
    {
        // Bump with every change to what this writes (see HumanoidImportPostprocessor).
        private const uint Version = 3; // 2: loop from the JSON (one-shot action clips, PL.17). 3: any rigged export, Generic too

        public override uint GetVersion()
        {
            return Version;
        }

        [Serializable]
        public sealed class Sidecar
        {
            public ClipEntry[] clips;
        }

        [Serializable]
        public sealed class ClipEntry
        {
            public string name;
            public int start;
            public int end;
            public int loop_end;
            public bool additive;
            public int additive_reference_frame;
            public float speed;
            public bool loop;
        }

        public static Sidecar Load(string assetPath)
        {
            var path = Path.ChangeExtension(assetPath, ".json");
            return File.Exists(path) ? JsonUtility.FromJson<Sidecar>(File.ReadAllText(path)) : null;
        }

        public static bool HasClips(string assetPath)
        {
            var sidecar = Load(assetPath);
            return sidecar?.clips != null && sidecar.clips.Length > 0;
        }

        private void OnPreprocessAnimation()
        {
            if (!HumanoidImportPostprocessor.IsRigged(assetPath) || !HasClips(assetPath))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            var takes = importer.defaultClipAnimations;
            var sidecar = Load(assetPath);
            var clips = new ModelImporterClipAnimation[sidecar.clips.Length];
            for (var i = 0; i < clips.Length; i++)
            {
                var entry = sidecar.clips[i];
                // Blender names a take after its action, sometimes prefixed with the armature ("Rig|Walk_Normal").
                var take = takes.FirstOrDefault(t => t.takeName == entry.name || t.takeName.EndsWith("|" + entry.name));
                if (take == null)
                {
                    context.LogImportError($"[Clips] {Path.GetFileName(assetPath)}: the JSON lists {entry.name}, the FBX has no such take.");
                    return;
                }

                clips[i] = Apply(entry, take);
            }

            importer.clipAnimations = clips;
        }

        public static ModelImporterClipAnimation Apply(ClipEntry entry, ModelImporterClipAnimation clip)
        {
            clip.name = entry.name;
            clip.firstFrame = entry.start;
            clip.lastFrame = entry.loop_end > 0 ? entry.loop_end : entry.end;

            // Walks, layers and the idle are authored to close exactly on the cycle (frame loop_end == frame
            // start): they loop, without Loop Pose smoothing, which would bend a closed cycle. Actions,
            // the arrest and the tent clips play once.
            clip.loopTime = entry.loop;
            clip.loopPose = false;

            // In place: nothing goes to the root.
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = true;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = false;
            clip.heightFromFeet = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalPositionXZ = false;

            clip.hasAdditiveReferencePose = entry.additive;
            clip.additiveReferencePoseFrame = entry.additive ? entry.additive_reference_frame : 0;
            return clip;
        }
    }
}
