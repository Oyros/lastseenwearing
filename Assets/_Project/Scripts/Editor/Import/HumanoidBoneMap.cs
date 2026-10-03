using System.Collections.Generic;
using UnityEngine;

namespace LastSeenWearing.Editor.Import
{
    /// <summary>
    /// The one table that maps Unity's Humanoid bones to the crowd rig's bone names
    /// (docs/ART_PIPELINE.md §3, D-018). Explicit on purpose: Unity's auto-mapping is never used, so a
    /// rename in the rig is a visible break here, not a silent mis-mapping. <c>Root</c> and every
    /// <c>SOCKET_*</c> are extra transforms and stay unmapped.
    /// The rig's shared <c>Fingers</c> chain (D-012) drives Unity's middle finger; ring and little are
    /// left empty, so a clip's middle-finger curl is the whole hand's curl (D-020).
    /// </summary>
    public static class HumanoidBoneMap
    {
        /// <summary>The skeleton's humanoid root. A model without it has no rig.</summary>
        public const string RootBone = "Hips";

        public const string LeftSuffix = ".L";
        public const string RightSuffix = ".R";

        private static readonly (HumanBodyBones Human, string Bone)[] Centre =
        {
            (HumanBodyBones.Hips, RootBone),
            (HumanBodyBones.Spine, "Spine"),
            (HumanBodyBones.Chest, "Chest"),
            (HumanBodyBones.Neck, "Neck"),
            (HumanBodyBones.Head, "Head"),
        };

        // Written once for the left side; the right side is the same bones with .R.
        private static readonly (HumanBodyBones Left, HumanBodyBones Right, string Stem)[] Sided =
        {
            (HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder, "Shoulder"),
            (HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm, "UpperArm"),
            (HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, "LowerArm"),
            (HumanBodyBones.LeftHand, HumanBodyBones.RightHand, "Hand"),
            (HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg, "UpperLeg"),
            (HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg, "LowerLeg"),
            (HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, "Foot"),
            (HumanBodyBones.LeftToes, HumanBodyBones.RightToes, "Toes"),
            (HumanBodyBones.LeftThumbProximal, HumanBodyBones.RightThumbProximal, "Thumb1"),
            (HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.RightThumbIntermediate, "Thumb2"),
            (HumanBodyBones.LeftIndexProximal, HumanBodyBones.RightIndexProximal, "Index1"),
            (HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.RightIndexIntermediate, "Index2"),
            (HumanBodyBones.LeftMiddleProximal, HumanBodyBones.RightMiddleProximal, "Fingers1"),
            (HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.RightMiddleIntermediate, "Fingers2"),
        };

        /// <summary>Every mapped pair: Unity's Humanoid bone and our bone name. 33 rows — every deform bone.</summary>
        public static IReadOnlyList<(HumanBodyBones Human, string Bone)> Rows
        {
            get
            {
                var rows = new List<(HumanBodyBones, string)>(Centre);
                foreach (var (left, right, stem) in Sided)
                {
                    rows.Add((left, stem + LeftSuffix));
                    rows.Add((right, stem + RightSuffix));
                }

                return rows;
            }
        }

        /// <summary>The table as Unity's HumanBone list, with default muscle limits.</summary>
        public static HumanBone[] ToHumanBones()
        {
            var rows = Rows;
            var bones = new HumanBone[rows.Count];
            for (var i = 0; i < rows.Count; i++)
            {
                bones[i] = new HumanBone
                {
                    humanName = HumanTrait.BoneName[(int)rows[i].Human],
                    boneName = rows[i].Bone,
                    limit = new HumanLimit { useDefaultValues = true },
                };
            }

            return bones;
        }
    }
}
