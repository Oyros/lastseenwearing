using System;
using System.Linq;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Editor.Import;
using LastSeenWearing.Gameplay.Crowd;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSeenWearing.Tests.Art
{
    /// <summary>
    /// P1.05: one additive layer per walk trait, on the same Phase as the walk; a trait's signed strength
    /// sets its layer's weight and side; traits fade out while the NPC lingers; and no trait, even at full
    /// strength, makes a planted foot slide.
    /// </summary>
    public sealed class CrowdWalkTraitTests
    {
        private const string NpcPrefabPath = "Assets/_Project/Prefabs/Characters/CrowdNpc.prefab";
        private const float MaxSlipMetres = 0.005f;

        private static readonly WalkTrait[] Traits = (WalkTrait[])Enum.GetValues(typeof(WalkTrait));

        [Test]
        public void EveryTraitIsAnAdditiveLayerOnThePhase()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CrowdAnimatorBuilder.ControllerPath);
            foreach (var trait in Traits)
            {
                var layer = controller.layers.SingleOrDefault(l => l.name == trait.ToString());
                Assert.That(layer, Is.Not.Null, $"no layer {trait}");
                Assert.That(layer.blendingMode, Is.EqualTo(AnimatorLayerBlendingMode.Additive), $"{trait}");
                Assert.That(layer.defaultWeight, Is.Zero, $"{trait} is off until a signature sets it");

                var state = layer.stateMachine.defaultState;
                Assert.That(state.timeParameterActive && state.timeParameter == CrowdAnimatorBuilder.PhaseParameter, Is.True,
                    $"{trait} runs on the walk's phase");

                var clips = state.motion is BlendTree tree
                    ? tree.children.OrderBy(c => c.threshold).Select(c => c.motion.name).ToArray()
                    : new[] { state.motion.name };
                Assert.That(clips, Is.EqualTo(CrowdAnimatorBuilder.TraitClips(trait)), $"{trait} clips, in side order");
            }
        }

        [Test]
        public void ASignedStrengthSetsWeightAndSide()
        {
            WithAgent((agent, animator) =>
            {
                agent.SetTrait(WalkTrait.Hunch, 0.5f);
                agent.SetTrait(WalkTrait.Limp, -1f);
                agent.SetTrait(WalkTrait.ArmSwing, 0.5f);

                Assert.That(animator.GetLayerWeight(animator.GetLayerIndex("Hunch")), Is.EqualTo(0.5f).Within(1e-5f));
                Assert.That(animator.GetLayerWeight(animator.GetLayerIndex("Limp")), Is.EqualTo(1f).Within(1e-5f));
                Assert.That(animator.GetFloat(CrowdAgent.SideParameter(WalkTrait.Limp)), Is.EqualTo(-1f), "limp left");
                Assert.That(animator.GetFloat(CrowdAgent.SideParameter(WalkTrait.ArmSwing)), Is.EqualTo(1f), "big arms");
                Assert.That(animator.GetLayerWeight(animator.GetLayerIndex("Sway")), Is.Zero, "untouched traits stay off");
            });
        }

        [Test]
        public void TraitsFadeOutWhileLingeringAndBackWhileWalking()
        {
            WithAgent((agent, animator) =>
            {
                agent.SetTrait(WalkTrait.Hunch, 1f);
                var hunch = animator.GetLayerIndex("Hunch");

                // The schedule walks 10 m (5 s), then lingers 3 s.
                agent.FollowSchedule(1d, 0.02f);
                agent.FollowSchedule(1.02d, 0.02f);
                Assert.That(animator.GetLayerWeight(hunch), Is.EqualTo(1f).Within(1e-4f), "walking");

                for (var t = 5.5d; t < 5.5d + CrowdAgent.IdleBlendSeconds + 0.1d; t += 0.02d)
                {
                    agent.FollowSchedule(t, 0.02f);
                }

                Assert.That(animator.GetLayerWeight(hunch), Is.Zero.Within(1e-4f), "lingering");

                for (var t = 8.2d; t < 8.2d + CrowdAgent.IdleBlendSeconds + 0.1d; t += 0.02d)
                {
                    agent.FollowSchedule(t, 0.02f);
                }

                Assert.That(animator.GetLayerWeight(hunch), Is.EqualTo(1f).Within(1e-4f), "walking again");
            });
        }

        [Test]
        public void NoTraitMakesAPlantedFootSlide()
        {
            var npc = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath));
            try
            {
                var animator = npc.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var bones = npc.GetComponentsInChildren<Transform>(true);
                var left = bones.Single(t => t.name == "Foot.L");
                var right = bones.Single(t => t.name == "Foot.R");
                foreach (var trait in Traits)
                {
                    foreach (var side in new[] { -1f, 1f })
                    {
                        animator.Rebind();
                        animator.SetLayerWeight(animator.GetLayerIndex(trait.ToString()), 1f);
                        if (trait == WalkTrait.Limp || trait == WalkTrait.ArmSwing)
                        {
                            animator.SetFloat(CrowdAgent.SideParameter(trait), side);
                        }

                        Assert.That(WorstSlip(npc.transform, animator, left, right), Is.LessThan(MaxSlipMetres), $"{trait} side {side}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(npc);
            }
        }

        // An agent on the P1.04 test schedule: 10 m legs at 2 m/s, 3 s dwell; stride 1 m.
        private static void WithAgent(Action<CrowdAgent, Animator> test)
        {
            var npc = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath));
            try
            {
                var waypoints = new[] { new GroundPoint(0f, 0f), new GroundPoint(10f, 0f) };
                var plan = new NpcPlan(0, 0f, 0f, new[] { new NpcLeg(1, 0f, 0f, 3f), new NpcLeg(0, 0f, 0f, 3f) });
                var schedule = new NpcSchedule(plan, waypoints, 2f, (from, to) => new[] { from, to });
                var agent = npc.GetComponent<CrowdAgent>();
                agent.Begin(0, schedule, 0f, BaseWalk.Normal, 1f);
                test(agent, npc.GetComponentInChildren<Animator>());
            }
            finally
            {
                Object.DestroyImmediate(npc);
            }
        }

        private static float WorstSlip(Transform root, Animator animator, Transform left, Transform right)
        {
            const float step = 1f / 60f;
            var walked = 0d;
            Transform planted = null;
            var landed = Vector3.zero;
            var worst = 0f;
            for (var frame = 0; frame < 300; frame++)
            {
                walked += 1.3 * step;
                root.position = new Vector3(0f, 0f, (float)walked);
                animator.SetFloat(CrowdAnimatorBuilder.PhaseParameter, (float)(walked % 1d));
                animator.Update(step);
                if (frame < 30)
                {
                    continue;
                }

                var low = left.position.y < right.position.y ? left : right;
                var high = low == left ? right : left;
                if (high.position.y - low.position.y < 0.02f)
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
