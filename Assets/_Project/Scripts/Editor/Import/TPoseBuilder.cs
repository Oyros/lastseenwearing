using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSeenWearing.Editor.Import
{
    /// <summary>
    /// Turns a character's A-pose rest skeleton into the T-pose <em>definition</em> Unity's Humanoid
    /// avatar needs (D-020, after Borrowed Crown's D-031). The art stays in A-pose; only the avatar's
    /// <see cref="HumanDescription.skeleton"/> carries this. Changes local rotations of UpperArm,
    /// LowerArm, Hand (arms along the side axis, palms down), the index, shared-fingers and thumb
    /// chains (straight, in the palm plane — Unity reads straight fingers as 0) and UpperLeg, LowerLeg
    /// (straight down). Every other bone keeps its rest local rotation.
    /// The crowd rig's chains are two bones long (D-012): the second bone has no child to aim at, so it
    /// is turned to lie along the first.
    /// </summary>
    public static class TPoseBuilder
    {
        private static readonly string[] Sides = { HumanoidBoneMap.LeftSuffix, HumanoidBoneMap.RightSuffix };
        private static readonly string[] Fingers = { "Index", "Fingers" };

        /// <summary>
        /// The thumb in the T-pose definition: straight, in the palm plane, this far forward of the
        /// hand axis. Borrowed Crown's value (PL.16 there); revisit if LSW's thumb muscles read off.
        /// </summary>
        public const float ThumbForwardDegrees = 20f;

        /// <summary>
        /// <paramref name="parents"/> holds each bone's parent name, parallel to
        /// <paramref name="rest"/>; the root's is null or empty.
        /// </summary>
        public static SkeletonBone[] Build(SkeletonBone[] rest, string[] parents, float thumbForwardDegrees = ThumbForwardDegrees)
        {
            var tree = new PoseTree(rest, parents);

            // The character's left, on the ground plane: from the right shoulder to the left one.
            var left = Horizontal(tree.WorldPosition("UpperArm" + HumanoidBoneMap.LeftSuffix) -
                                  tree.WorldPosition("UpperArm" + HumanoidBoneMap.RightSuffix));
            var forward = Vector3.Cross(Vector3.up, left);
            foreach (var side in Sides)
            {
                var outward = side == HumanoidBoneMap.LeftSuffix ? left : -left;
                var sign = PalmSign(PalmNormal(tree, side), outward);
                var spread = RestSpread(tree, side, sign);

                ArmAlong(tree, side, outward, sign);
                FingersStraight(tree, side, sign, spread, forward, thumbForwardDegrees);
                LegDown(tree, side);
            }

            return tree.ToSkeleton();
        }

        /// <summary>
        /// Palm normal: cross(Index1 − Fingers1, Hand → Fingers1). Its sign differs between hands
        /// (mirror); <see cref="PalmSign"/> orients it out of the palm.
        /// </summary>
        public static Vector3 RawPalmNormal(Vector3 hand, Vector3 fingers1, Vector3 index1)
        {
            return Vector3.Cross(index1 - fingers1, fingers1 - hand).normalized;
        }

        /// <summary>
        /// +1 or −1 so that sign × raw normal points out of the palm, judged at the A-pose rest,
        /// where the palm faces the body: out of the palm is towards the midline.
        /// </summary>
        public static float PalmSign(Vector3 restRawNormal, Vector3 outward)
        {
            return Vector3.Dot(restRawNormal, -outward) >= 0f ? 1f : -1f;
        }

        public static Vector3 Horizontal(Vector3 v)
        {
            v.y = 0f;
            return v.normalized;
        }

        private static void ArmAlong(PoseTree tree, string side, Vector3 outward, float sign)
        {
            Aim(tree, "UpperArm" + side, "LowerArm" + side, outward);
            Aim(tree, "LowerArm" + side, "Hand" + side, outward);
            Aim(tree, "Hand" + side, "Fingers1" + side, outward);

            // Roll the straight chain about its own axis until the palm faces the ground.
            var palm = Vector3.ProjectOnPlane(sign * PalmNormal(tree, side), outward);
            var down = Vector3.ProjectOnPlane(Vector3.down, outward);
            var roll = Vector3.SignedAngle(palm, down, outward);
            tree.RotateWorld("UpperArm" + side, Quaternion.AngleAxis(roll, outward));
        }

        // Each finger's angle from the hand axis within the palm plane at rest — the natural spread,
        // kept in the T-pose. Measured about the out-of-palm normal.
        private static Dictionary<string, float> RestSpread(PoseTree tree, string side, float sign)
        {
            var hand = tree.WorldPosition("Hand" + side);
            var axis = tree.WorldPosition("Fingers1" + side) - hand;
            var normal = sign * PalmNormal(tree, side);
            var spread = new Dictionary<string, float>();
            foreach (var finger in Fingers)
            {
                var direction = tree.WorldPosition(finger + "2" + side) - tree.WorldPosition(finger + "1" + side);
                spread[finger] = Vector3.SignedAngle(Vector3.ProjectOnPlane(axis, normal), Vector3.ProjectOnPlane(direction, normal), normal);
            }

            return spread;
        }

        private static void FingersStraight(PoseTree tree, string side, float sign, Dictionary<string, float> spread, Vector3 forward, float thumbForwardDegrees)
        {
            var axis = (tree.WorldPosition("Fingers1" + side) - tree.WorldPosition("Hand" + side)).normalized;
            var normal = sign * PalmNormal(tree, side);

            foreach (var finger in Fingers)
            {
                StraightChain(tree, finger, side, Quaternion.AngleAxis(spread[finger], normal) * axis);
            }

            // The thumb swings forward of the hand axis, in the palm plane.
            var thumb = Quaternion.AngleAxis(thumbForwardDegrees, normal) * axis;
            if (Vector3.Dot(thumb, forward) < 0f)
            {
                thumb = Quaternion.AngleAxis(-thumbForwardDegrees, normal) * axis;
            }

            StraightChain(tree, "Thumb", side, thumb);
        }

        // The proximal aims at its child; the second bone has no child, so it is turned until its own
        // bone axis — taken to be the same local axis the proximal's child lies along — points the
        // same way.
        private static void StraightChain(PoseTree tree, string finger, string side, Vector3 direction)
        {
            var one = finger + "1" + side;
            var two = finger + "2" + side;
            Aim(tree, one, two, direction);

            var localAxis = Quaternion.Inverse(tree.WorldRotation(one)) * (tree.WorldPosition(two) - tree.WorldPosition(one)).normalized;
            var distalAxis = tree.WorldRotation(two) * localAxis;
            tree.RotateWorld(two, Quaternion.FromToRotation(distalAxis, direction.normalized));
        }

        private static void LegDown(PoseTree tree, string side)
        {
            Aim(tree, "UpperLeg" + side, "LowerLeg" + side, Vector3.down);
            Aim(tree, "LowerLeg" + side, "Foot" + side, Vector3.down);
        }

        private static Vector3 PalmNormal(PoseTree tree, string side)
        {
            return RawPalmNormal(tree.WorldPosition("Hand" + side), tree.WorldPosition("Fingers1" + side),
                tree.WorldPosition("Index1" + side));
        }

        // Rotate bone so the direction to child points along target, by the smallest rotation.
        private static void Aim(PoseTree tree, string bone, string child, Vector3 target)
        {
            var current = (tree.WorldPosition(child) - tree.WorldPosition(bone)).normalized;
            tree.RotateWorld(bone, Quaternion.FromToRotation(current, target.normalized));
        }

        /// <summary>A skeleton as local TRS plus parents, with world queries and world-space rotation.</summary>
        private sealed class PoseTree
        {
            private readonly SkeletonBone[] _bones;
            private readonly int[] _parent;
            private readonly Dictionary<string, int> _index = new Dictionary<string, int>();

            public PoseTree(SkeletonBone[] rest, string[] parents)
            {
                if (rest == null || parents == null || rest.Length != parents.Length)
                {
                    throw new ArgumentException("rest and parents must be parallel arrays");
                }

                _bones = (SkeletonBone[])rest.Clone();
                _parent = new int[_bones.Length];
                for (var i = 0; i < _bones.Length; i++)
                {
                    _index[_bones[i].name] = i;
                }

                for (var i = 0; i < _bones.Length; i++)
                {
                    _parent[i] = string.IsNullOrEmpty(parents[i]) || !_index.TryGetValue(parents[i], out var p) ? -1 : p;
                }
            }

            public Vector3 WorldPosition(string bone)
            {
                return WorldPosition(Index(bone));
            }

            public void RotateWorld(string bone, Quaternion delta)
            {
                var i = Index(bone);
                var parentRotation = _parent[i] < 0 ? Quaternion.identity : WorldRotation(_parent[i]);
                var bonePose = _bones[i];
                // Normalised: products of rotations drift off unit length, and an off-unit quaternion
                // reads as a small turn against itself (Quaternion.Angle) — enough to make the
                // importer's "same skeleton?" check fail forever (Borrowed Crown, BC_Merrow_Test).
                bonePose.rotation = Normalized(Quaternion.Inverse(parentRotation) * delta * WorldRotation(i));
                _bones[i] = bonePose;
            }

            private static Quaternion Normalized(Quaternion q)
            {
                var length = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
                return new Quaternion(q.x / length, q.y / length, q.z / length, q.w / length);
            }

            public SkeletonBone[] ToSkeleton()
            {
                return (SkeletonBone[])_bones.Clone();
            }

            private int Index(string bone)
            {
                if (!_index.TryGetValue(bone, out var i))
                {
                    throw new KeyNotFoundException($"T-pose: no bone named {bone}");
                }

                return i;
            }

            public Quaternion WorldRotation(string bone)
            {
                return WorldRotation(Index(bone));
            }

            private Quaternion WorldRotation(int i)
            {
                var rotation = _bones[i].rotation;
                for (var p = _parent[i]; p >= 0; p = _parent[p])
                {
                    rotation = _bones[p].rotation * rotation;
                }

                return rotation;
            }

            private Vector3 WorldScale(int i)
            {
                var scale = _bones[i].scale;
                for (var p = _parent[i]; p >= 0; p = _parent[p])
                {
                    scale = Vector3.Scale(_bones[p].scale, scale);
                }

                return scale;
            }

            private Vector3 WorldPosition(int i)
            {
                var p = _parent[i];
                return p < 0
                    ? _bones[i].position
                    : WorldPosition(p) + WorldRotation(p) * Vector3.Scale(WorldScale(p), _bones[i].position);
            }
        }
    }
}
