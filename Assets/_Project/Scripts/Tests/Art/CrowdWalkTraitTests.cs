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

        /// <summary>
        /// PL.12a: the art once shipped every spine pitch inverted (Brisk leaned back, Hunch bent back). The
        /// leans must order as LSW_WalkSystem's styles do — Brisk +9°, Heavy +5°, Normal +3°, Stroll −3° — and
        /// the hunch must put the head forward. Measured as head ahead of hips (+Z is the body's front).
        /// </summary>
        [Test]
        public void TheBodyLeansTheWayTheArtMeans()
        {
            var npc = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath));
            try
            {
                var animator = npc.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var bones = npc.GetComponentsInChildren<Transform>(true);
                var hips = bones.Single(t => t.name == "Hips");
                var head = bones.Single(t => t.name == "Head");

                float Lean(BaseWalk walk, WalkTrait? trait)
                {
                    animator.Rebind();
                    animator.SetFloat(CrowdAnimatorBuilder.StyleParameter, (float)walk);
                    if (trait is WalkTrait t)
                    {
                        animator.SetLayerWeight(animator.GetLayerIndex(t.ToString()), 1f);
                    }

                    var sum = 0f;
                    foreach (var phase in new[] { 0f, 0.25f, 0.5f, 0.75f })
                    {
                        animator.SetFloat(CrowdAnimatorBuilder.PhaseParameter, phase);
                        animator.Update(1f / 60f);
                        sum += head.position.z - hips.position.z;
                    }

                    return sum / 4f;
                }

                var brisk = Lean(BaseWalk.Brisk, null);
                var heavy = Lean(BaseWalk.Heavy, null);
                var normal = Lean(BaseWalk.Normal, null);
                var stroll = Lean(BaseWalk.Stroll, null);
                Assert.That(brisk, Is.GreaterThan(heavy), "Brisk leans forward of Heavy");
                Assert.That(heavy, Is.GreaterThan(normal), "Heavy leans forward of Normal");
                Assert.That(normal, Is.GreaterThan(stroll), "Normal leans forward of Stroll");
                Assert.That(brisk, Is.GreaterThan(0f), "Brisk leans forward");
                Assert.That(Lean(BaseWalk.Normal, WalkTrait.Hunch), Is.GreaterThan(normal + 0.1f), "the hunch brings the head well forward");
            }
            finally
            {
                Object.DestroyImmediate(npc);
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

        /// <summary>
        /// P1.08 (WalkSystem §7 "no foot sliding when layers combine"): every pair of traits a signature can
        /// carry, at half and full strength, on the side that moves the body most (limp left, stiff arms).
        /// </summary>
        [Test]
        public void NoPairOfTraitsMakesAPlantedFootSlide()
        {
            var npc = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath));
            try
            {
                var animator = npc.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var bones = npc.GetComponentsInChildren<Transform>(true);
                var left = bones.Single(t => t.name == "Foot.L");
                var right = bones.Single(t => t.name == "Foot.R");
                for (var a = 0; a < Traits.Length; a++)
                {
                    for (var b = a + 1; b < Traits.Length; b++)
                    {
                        foreach (var weight in new[] { 0.5f, 1f })
                        {
                            animator.Rebind();
                            foreach (var trait in new[] { Traits[a], Traits[b] })
                            {
                                animator.SetLayerWeight(animator.GetLayerIndex(trait.ToString()), weight);
                                if (trait == WalkTrait.Limp || trait == WalkTrait.ArmSwing)
                                {
                                    animator.SetFloat(CrowdAgent.SideParameter(trait), -1f);
                                }
                            }

                            Assert.That(WorstSlip(npc.transform, animator, left, right), Is.LessThan(MaxSlipMetres),
                                $"{Traits[a]} + {Traits[b]} at {weight}");
                        }
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
