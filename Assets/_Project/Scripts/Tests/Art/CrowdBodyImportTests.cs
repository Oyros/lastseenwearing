using System;
using System.IO;
using System.Linq;
using LastSeenWearing.Editor.Import;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSeenWearing.Tests.Art
{
    /// <summary>
    /// P1.03 (P1.04: the rest is the bind pose, since the body now carries its clips): the crowd body
    /// from the art track imports as a Humanoid that keeps the locked bone
    /// contract (ART_PIPELINE §3, D-018) and whose avatar reads the A-pose art correctly (D-020).
    /// </summary>
    public sealed class CrowdBodyImportTests
    {
        private const string BodyPath = "Assets/_Project/Art/Models/Crowd/LSW_Crowd_Body_M.fbx";

        [Serializable]
        private sealed class Sidecar
        {
            public string[] bones;
            public Socket[] sockets;
        }

        [Serializable]
        private sealed class Socket
        {
            public string name;
            public string bone;
        }

        private static GameObject Body => AssetDatabase.LoadAssetAtPath<GameObject>(BodyPath);

        private static Avatar Avatar => AssetDatabase.LoadAllAssetsAtPath(BodyPath).OfType<Avatar>().SingleOrDefault();

        private static Sidecar Contract => JsonUtility.FromJson<Sidecar>(File.ReadAllText(Path.ChangeExtension(BodyPath, ".json")));

        private static Transform Find(GameObject root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        [Test]
        public void TheImporterOwnsTheBody()
        {
            Assert.That(HumanoidImportPostprocessor.Applies(BodyPath), Is.True, "the sidecar JSON should mark the body rigged");
        }

        [Test]
        public void TheBodyHasAValidHumanoidAvatar()
        {
            Assert.That(Avatar, Is.Not.Null, "no avatar");
            Assert.That(Avatar.isValid && Avatar.isHuman, Is.True, "not a valid Humanoid avatar");
        }

        [Test]
        public void TheBoneMapIsExactlyTheContract()
        {
            var mapped = HumanoidBoneMap.Rows.Select(r => r.Bone).OrderBy(b => b).ToArray();
            var contract = Contract.bones.OrderBy(b => b).ToArray();
            Assert.That(mapped, Is.EqualTo(contract), "every deform bone in the export is mapped, and nothing else");
        }

        [Test]
        public void EveryContractBoneAndSocketIsInTheModel()
        {
            var missing = Contract.bones.Where(b => Find(Body, b) == null)
                .Concat(Contract.sockets.Where(s => Find(Body, s.name)?.parent?.name != s.bone).Select(s => s.name))
                .ToArray();
            Assert.That(missing, Is.Empty, "bones missing, or sockets missing / under the wrong bone");
        }

        [Test]
        public void TheAvatarTPoseHasLevelArmsAndStraightLegs()
        {
            var skeleton = ((ModelImporter)AssetImporter.GetAtPath(BodyPath)).humanDescription.skeleton;
            var body = Object.Instantiate(Body);
            try
            {
                foreach (var bone in skeleton)
                {
                    var transform = Find(body, bone.name);
                    if (transform != null && transform != body.transform)
                    {
                        transform.localPosition = bone.position;
                        transform.localRotation = bone.rotation;
                    }
                }

                foreach (var side in new[] { HumanoidBoneMap.LeftSuffix, HumanoidBoneMap.RightSuffix })
                {
                    var arm = Find(body, "Hand" + side).position - Find(body, "UpperArm" + side).position;
                    var leg = Find(body, "Foot" + side).position - Find(body, "UpperLeg" + side).position;
                    Assert.That(Mathf.Abs(arm.normalized.y), Is.LessThan(0.01f), $"arm{side} is not level in the T-pose");
                    Assert.That(leg.normalized.y, Is.LessThan(-0.999f), $"leg{side} is not straight down in the T-pose");
                }
            }
            finally
            {
                Object.DestroyImmediate(body);
            }
        }

        [Test]
        public void TheRestPoseReadsInsideEveryMuscleRangeAndSymmetrically()
        {
            var body = Object.Instantiate(Body);
            HumanoidImportPostprocessor.ApplyRestPose(body);
            var handler = new HumanPoseHandler(Avatar, body.transform);
            try
            {
                var pose = new HumanPose();
                handler.GetHumanPose(ref pose);
                for (var i = 0; i < pose.muscles.Length; i++)
                {
                    Assert.That(pose.muscles[i], Is.InRange(-1f, 1f), HumanTrait.MuscleName[i]);
                }

                for (var i = 0; i < pose.muscles.Length; i++)
                {
                    var name = HumanTrait.MuscleName[i];
                    if (!name.StartsWith("Left "))
                    {
                        continue;
                    }

                    var mirror = Array.IndexOf(HumanTrait.MuscleName, "Right " + name.Substring(5));
                    Assert.That(pose.muscles[i], Is.EqualTo(pose.muscles[mirror]).Within(0.02f), $"{name} vs its right side");
                }
            }
            finally
            {
                handler.Dispose();
                Object.DestroyImmediate(body);
            }
        }

        [Test]
        public void APoseSurvivesAHumanoidRoundTrip()
        {
            var body = Object.Instantiate(Body);
            HumanoidImportPostprocessor.ApplyRestPose(body);
            var handler = new HumanPoseHandler(Avatar, body.transform);
            try
            {
                var tips = new[] { "Hand.L", "Hand.R", "Foot.L", "Foot.R", "Head", "Index2.L", "Fingers2.R", "Thumb2.L" };
                var before = tips.Select(t => Find(body, t).position).ToArray();
                var pose = new HumanPose();
                handler.GetHumanPose(ref pose);
                handler.SetHumanPose(ref pose);
                for (var i = 0; i < tips.Length; i++)
                {
                    Assert.That((Find(body, tips[i]).position - before[i]).magnitude, Is.LessThan(0.001f), tips[i]);
                }
            }
            finally
            {
                handler.Dispose();
                Object.DestroyImmediate(body);
            }
        }

        [Test]
        public void TheAvatarIsBuiltFromTheBindPoseNotTheNodes()
        {
            // The nodes may hold a clip's frame; the avatar's skeleton must hold the bind pose's rotations
            // for every bone the T-pose leaves alone (spine, neck, head, shoulders).
            var skeleton = ((ModelImporter)AssetImporter.GetAtPath(BodyPath)).humanDescription.skeleton.ToDictionary(b => b.name);
            var rest = HumanoidImportPostprocessor.RestPose(Body, out _).ToDictionary(b => b.name);
            foreach (var name in new[] { "Hips", "Spine", "Chest", "Neck", "Head", "Shoulder.L", "Shoulder.R" })
            {
                Assert.That(Quaternion.Angle(skeleton[name].rotation, rest[name].rotation), Is.LessThan(0.01f), name);
            }
        }

        [Test]
        public void TheBuildsComeInAsBlendShapes()
        {
            var shapes = Body.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SelectMany(r => Enumerable.Range(0, r.sharedMesh.blendShapeCount).Select(r.sharedMesh.GetBlendShapeName))
                .Distinct()
                .ToArray();
            Assert.That(shapes, Is.SupersetOf(new[] { "Build_Slim", "Build_Heavy" }));
        }
    }
}
