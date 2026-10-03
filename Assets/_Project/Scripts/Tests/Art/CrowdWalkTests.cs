using System;
using System.Linq;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Editor.Import;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSeenWearing.Tests.Art
{
    /// <summary>
    /// P1.04: the body's walk clips come in as the art's JSON says, the crowd animator blends the base
    /// walks on distance (D-022), and a planted foot stays planted — every base walk and every 50/50
    /// blend, at three speeds.
    /// </summary>
    public sealed class CrowdWalkTests
    {
        private const string CrowdConfigPath = "Assets/_Project/Data/Config/CrowdConfig.asset";
        private const string NpcPrefabPath = "Assets/_Project/Prefabs/Characters/CrowdNpc.prefab";

        // Measured 3.7 mm with Foot IK in P1.04 (8.3 mm without); the art's own blend test, 1.5 mm.
        private const float MaxSlipMetres = 0.005f;

        private static AnimationClip Clip(string name) =>
            AssetDatabase.LoadAllAssetsAtPath(CrowdAnimatorBuilder.BodyPath).OfType<AnimationClip>().SingleOrDefault(c => c.name == name);

        private static BodyClipPostprocessor.ClipEntry[] Entries => BodyClipPostprocessor.Load(CrowdAnimatorBuilder.BodyPath).clips;

        [Test]
        public void EveryClipInTheJsonIsImportedAsItSays()
        {
            foreach (var entry in Entries)
            {
                var clip = Clip(entry.name);
                Assert.That(clip, Is.Not.Null, entry.name);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                Assert.That(settings.loopTime, Is.True, $"{entry.name} loops");
                Assert.That(clip.length * clip.frameRate, Is.EqualTo(entry.loop_end - entry.start).Within(0.01f), $"{entry.name} frame range");
                Assert.That(settings.hasAdditiveReferencePose, Is.EqualTo(entry.additive), $"{entry.name} additive");
                if (entry.additive)
                {
                    Assert.That(settings.additiveReferencePoseTime * clip.frameRate, Is.EqualTo(entry.additive_reference_frame).Within(0.01f), $"{entry.name} reference frame");
                }
            }
        }

        [Test]
        public void TheConfiguredStrideIsTheClipsStride()
        {
            var walk = Entries.Single(e => e.name == CrowdAnimatorBuilder.ClipName(BaseWalk.Normal));
            var stride = walk.speed * Clip(walk.name).length;
            Assert.That(AssetDatabase.LoadAssetAtPath<CrowdConfig>(CrowdConfigPath).StrideLength, Is.EqualTo(stride).Within(0.001f),
                "CrowdConfig.StrideLength must equal the art's speed × cycle length");
        }

        [Test]
        public void TheControllerBlendsEveryBaseWalkOnDistance()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CrowdAnimatorBuilder.ControllerPath);
            Assert.That(controller, Is.Not.Null);
            var state = controller.layers[0].stateMachine.defaultState;
            Assert.That(state.timeParameterActive, Is.True);
            Assert.That(state.timeParameter, Is.EqualTo(CrowdAnimatorBuilder.PhaseParameter));
            Assert.That(state.iKOnFeet, Is.True, "Foot IK keeps the planted foot planted");

            var tree = (BlendTree)state.motion;
            var walks = (BaseWalk[])Enum.GetValues(typeof(BaseWalk));
            Assert.That(tree.children.Select(c => (c.motion.name, c.threshold)),
                Is.EqualTo(walks.Select(w => (CrowdAnimatorBuilder.ClipName(w), (float)w))));
        }

        [Test]
        public void ALingeringNpcIdlesAndWalksOnAgain()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CrowdAnimatorBuilder.ControllerPath);
            var states = controller.layers[0].stateMachine.states.Select(s => s.state).ToArray();
            var walk = states.Single(s => s.name == CrowdAnimatorBuilder.WalkState);
            var idle = states.Single(s => s.name == CrowdAnimatorBuilder.IdleState);
            Assert.That(idle.motion.name, Is.EqualTo(CrowdAnimatorBuilder.IdleClip));
            Assert.That(walk.transitions.Single().destinationState, Is.EqualTo(idle));
            Assert.That(walk.transitions.Single().conditions.Single().mode, Is.EqualTo(AnimatorConditionMode.IfNot));
            Assert.That(idle.transitions.Single().destinationState, Is.EqualTo(walk));
            Assert.That(idle.transitions.Single().conditions.Single().mode, Is.EqualTo(AnimatorConditionMode.If));
            Assert.That(controller.parameters.Single(p => p.name == CrowdAnimatorBuilder.WalkingParameter).defaultBool, Is.True,
                "a new NPC starts in the walk");
        }

        [TestCase(1.3f)]
        [TestCase(0.9375f)]
        [TestCase(0.6f)]
        public void APlantedFootDoesNotSlide(float speed)
        {
            var npc = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath));
            try
            {
                var animator = npc.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var bones = npc.GetComponentsInChildren<Transform>(true);
                var left = bones.Single(t => t.name == "Foot.L");
                var right = bones.Single(t => t.name == "Foot.R");
                var stride = AssetDatabase.LoadAssetAtPath<CrowdConfig>(CrowdConfigPath).StrideLength;

                foreach (var style in new[] { 0f, 1f, 2f, 3f, 0.5f, 1.5f, 2.5f })
                {
                    Assert.That(WorstSlip(npc.transform, animator, left, right, style, speed, stride), Is.LessThan(MaxSlipMetres),
                        $"style {style} at {speed} m/s");
                }
            }
            finally
            {
                Object.DestroyImmediate(npc);
            }
        }

        // Walk the body along +Z with the phase on distance, as CrowdAgent does; for each stance — the
        // foot clearly below the other — the farthest the planted foot drifts from where it landed.
        private static float WorstSlip(Transform root, Animator animator, Transform left, Transform right, float style, float speed, float stride)
        {
            const float step = 1f / 60f;
            const float clearance = 0.02f;
            animator.Rebind();
            animator.SetFloat(CrowdAnimatorBuilder.StyleParameter, style);

            var walked = 0d;
            Transform planted = null;
            var landed = Vector3.zero;
            var worst = 0f;
            for (var frame = 0; frame < 300; frame++)
            {
                walked += speed * step;
                root.position = new Vector3(0f, 0f, (float)walked);
                animator.SetFloat(CrowdAnimatorBuilder.PhaseParameter, (float)(walked / stride % 1d));
                animator.Update(step);
                if (frame < 30)
                {
                    continue;
                }

                var low = left.position.y < right.position.y ? left : right;
                var high = low == left ? right : left;
                if (high.position.y - low.position.y < clearance)
                {
                    planted = null;
                    continue;
                }

                if (planted != low)
                {
                    planted = low;
                    landed = low.position;
                    continue;
                }

                worst = Mathf.Max(worst, new Vector2(low.position.x - landed.x, low.position.z - landed.z).magnitude);
            }

            return worst;
        }
    }
}
