using System.Linq;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Editor.Import;
using LastSeenWearing.Gameplay.Crowd;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastSeenWearing.Tests.Art
{
    /// <summary>
    /// PL.11b: the sprint breaks into the Run clip — on the walk's phase, its stride from the art's JSON, the
    /// blend from speed — and a body that never runs walks exactly as before (D-022, D-034).
    /// </summary>
    public sealed class RunTests
    {
        private const string CrowdConfigPath = "Assets/_Project/Data/Config/CrowdConfig.asset";

        [Test]
        public void TheRunStrideIsTheClipsStride()
        {
            var run = BodyClipPostprocessor.Load(CrowdAnimatorBuilder.BodyPath).clips.Single(e => e.name == CrowdAnimatorBuilder.RunClip);
            var clip = AssetDatabase.LoadAllAssetsAtPath(CrowdAnimatorBuilder.BodyPath).OfType<AnimationClip>().Single(c => c.name == run.name);
            Assert.That(AssetDatabase.LoadAssetAtPath<CrowdConfig>(CrowdConfigPath).RunStrideLength,
                Is.EqualTo(run.speed * clip.length).Within(0.001f), "CrowdConfig.RunStrideLength = the art's speed × cycle length");
            Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.True);
        }

        [Test]
        public void TheWalkStateBlendsIntoTheRunOnTheSamePhase()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CrowdAnimatorBuilder.ControllerPath);
            var walk = controller.layers[0].stateMachine.states.Single(s => s.state.name == CrowdAnimatorBuilder.WalkState).state;
            Assert.That(walk.timeParameter, Is.EqualTo(WalkCycle.PhaseParameter));
            var tree = (BlendTree)walk.motion;
            Assert.That(tree.blendParameter, Is.EqualTo(WalkCycle.RunParameter));
            Assert.That(tree.children.Select(c => c.threshold), Is.EqualTo(new[] { 0f, 1f }));
            Assert.That(tree.children[1].motion.name, Is.EqualTo(CrowdAnimatorBuilder.RunClip));
        }

        [Test]
        public void ABodyThatNeverRunsKeepsThePlainDistancePhase()
        {
            var cycle = new WalkCycle(null, 1f);
            foreach (var walked in new[] { 0.0, 0.25, 1.4, 3.9, 10.05 })
            {
                cycle.Advance(walked, 0.1f);
                Assert.That(cycle.Phase, Is.EqualTo((float)(walked % 1d)).Within(1e-5f));
                Assert.That(cycle.Run, Is.EqualTo(0f));
            }
        }

        [Test]
        public void SpeedBlendsIntoTheRunAndThePhaseStaysContinuous()
        {
            var cycle = new WalkCycle(null, 1f, 2.5f);
            cycle.SetRunSpeeds(1f, 3f);
            var walked = 0.0;
            var dt = 0.02f;
            cycle.Advance(walked, dt);
            var lastPhase = cycle.Phase;

            // Sprint for two seconds at 3 m/s: the blend reaches full run, the phase never jumps.
            for (var i = 0; i < 100; i++)
            {
                walked += 3.0 * dt;
                cycle.Advance(walked, dt);
                var step = Mathf.Repeat(cycle.Phase - lastPhase, 1f);
                Assert.That(step, Is.LessThanOrEqualTo(3f * dt / 1f + 1e-4f), $"tick {i}: the phase advances by distance / stride, no jump");
                lastPhase = cycle.Phase;
            }

            Assert.That(cycle.Run, Is.EqualTo(1f).Within(1e-4f));
            var before = cycle.Phase;
            walked += 0.25;
            cycle.Advance(walked, 0.25f / 3f);
            Assert.That(Mathf.Repeat(cycle.Phase - before, 1f), Is.EqualTo(0.25f / 2.5f).Within(1e-4f), "a full run strides 2.5 m per cycle");

            // Back to a walk: the blend returns to zero.
            for (var i = 0; i < 50; i++)
            {
                walked += 1.0 * dt;
                cycle.Advance(walked, dt);
            }

            Assert.That(cycle.Run, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void ABodyMovedOnFixedTicksStillRunsAtAHighFrameRate()
        {
            // The owner moves in FixedUpdate (50 Hz) and is drawn at 144 fps: two of three frames see no movement.
            var cycle = new WalkCycle(null, 1f, 2.5f);
            cycle.SetRunSpeeds(1.1f, 3.4f);
            const float frame = 1f / 144f;
            const float fixedStep = 1f / 50f;
            double walked = 0d, physicsClock = 0d;
            var standingFrames = 0;
            for (var f = 0; f < 288; f++)
            {
                var now = (f + 1) * (double)frame;
                while (physicsClock + fixedStep <= now)
                {
                    physicsClock += fixedStep;
                    walked += 3.4 * fixedStep;
                }

                cycle.Advance(walked, frame);
                if (f >= 72 && !cycle.IsWalking)
                {
                    standingFrames++;
                }
            }

            Assert.That(standingFrames, Is.EqualTo(0), "never flickers to standing while sprinting");
            Assert.That(cycle.Run, Is.GreaterThan(0.95f), "the sprint reaches the run");
        }
    }
}
