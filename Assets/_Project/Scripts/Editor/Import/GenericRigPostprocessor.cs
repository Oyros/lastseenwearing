using System.IO;
using UnityEditor;

namespace LastSeenWearing.Editor.Import
{
    /// <summary>
    /// Imports a rigged export that is not a crowd-contract body — the first-person arms (PL.19, P1.12) — as a
    /// Generic rig with its own avatar. Its clips are set from the sidecar JSON by
    /// <see cref="BodyClipPostprocessor"/>, as for the bodies.
    /// </summary>
    public sealed class GenericRigPostprocessor : AssetPostprocessor
    {
        // Bump with every change to what this writes (see HumanoidImportPostprocessor).
        private const uint Version = 1;

        public override uint GetVersion()
        {
            return Version;
        }

        private void OnPreprocessModel()
        {
            if (!HumanoidImportPostprocessor.IsRigged(assetPath) || HumanoidImportPostprocessor.Applies(assetPath))
            {
                return;
            }

            context.DependsOnSourceAsset(Path.ChangeExtension(assetPath, ".json"));
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = BodyClipPostprocessor.HasClips(assetPath);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.importBlendShapes = true;
        }
    }
}
