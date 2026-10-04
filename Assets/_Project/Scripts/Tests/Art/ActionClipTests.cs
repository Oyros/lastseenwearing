using System;
using System.Linq;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Layouts;
using LastSeenWearing.Editor.Import;
using LastSeenWearing.Gameplay.Crowd;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastSeenWearing.Tests.Art
{
    /// <summary>
    /// P1.25: the greybox one-shots (PL.17) — an Action layer over the walk on the crowd's and the arms' animators,
    /// one state per clip, back to the walk at the end; the clips fit the jobs and the tent they show.
    /// </summary>
    public sealed class ActionClipTests
    {
        private static AnimationClip Clip(string path, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => c.name == name);

        private static AnimatorControllerLayer ActionLayer(string controllerPath)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            var layer = controller.layers.Single(l => l.name == WalkCycle.ActionLayer);
            Assert.That(controller.layers.Last().name, Is.EqualTo(WalkCycle.ActionLayer), "on top of the walk and its traits");
            Assert.That(layer.blendingMode, Is.EqualTo(AnimatorLayerBlendingMode.Override));
            Assert.That(layer.defaultWeight, Is.EqualTo(1f));
            Assert.That(layer.stateMachine.defaultState.name, Is.EqualTo(WalkCycle.NoActionState));
            Assert.That(layer.stateMachine.defaultState.motion, Is.Null, "at rest the layer leaves the walk alone");
            return layer;
        }

        [Test]
        public void TheCrowdBodyHasAStateForEveryActionThatReturnsToTheWalk()
        {
            var layer = ActionLayer(CrowdAnimatorBuilder.ControllerPath);
            foreach (BodyAction action in Enum.GetValues(typeof(BodyAction)))
            {
                if (action == BodyAction.None)
                {
                    continue;
                }

                var name = BodyActions.ClipName(action);
                var state = layer.stateMachine.states.Select(s => s.state).Single(s => s.name == name);
                Assert.That(state.motion, Is.EqualTo(Clip(CrowdAnimatorBuilder.BodyPath, name)), name);
                Assert.That(state.transitions.Single().destinationState.name, Is.EqualTo(WalkCycle.NoActionState), name);
                Assert.That(state.transitions.Single().hasExitTime, Is.True, $"{name} plays out, then the walk comes back");
            }
        }

        [Test]
        public void TheArmsCuff()
        {
            var layer = ActionLayer(FpArmsAnimatorBuilder.ControllerPath);
            var cuffs = layer.stateMachine.states.Select(s => s.state).Single(s => s.name == FpArmsAnimatorBuilder.CuffsClip);
            Assert.That(cuffs.motion, Is.EqualTo(Clip(FpArmsAnimatorBuilder.ArmsPath, FpArmsAnimatorBuilder.CuffsClip)));
        }

        [Test]
        public void AJobLastsAsLongAsItsClip()
        {
            var fugitive = AssetDatabase.LoadAssetAtPath<FugitiveConfig>("Assets/_Project/Data/Config/FugitiveConfig.asset");
            foreach (TargetKind kind in Enum.GetValues(typeof(TargetKind)))
            {
                var action = BodyActions.ForTarget(kind);
                if (action == BodyAction.None)
                {
                    continue; // social and hidden have no clip yet (PL.38): the body stands
                }

                var clip = Clip(CrowdAnimatorBuilder.BodyPath, BodyActions.ClipName(action));
                Assert.That(clip.length, Is.EqualTo(fugitive.SecondsFor(kind)).Within(0.05f), $"{kind}: {clip.name}");
            }

            Assert.That(BodyActions.ForTarget(TargetKind.Open), Is.EqualTo(BodyAction.WalletLift));
            Assert.That(BodyActions.ForTarget(TargetKind.Fixed), Is.EqualTo(BodyAction.PosterSwap));
        }

        [Test]
        public void TheTentChangeHasRoomForGoingInAndComingOut()
        {
            var disguise = AssetDatabase.LoadAssetAtPath<DisguiseConfig>("Assets/_Project/Data/Config/DisguiseConfig.asset");
            var enter = Clip(CrowdAnimatorBuilder.BodyPath, BodyActions.ClipName(BodyAction.TentEnter)).length;
            var exit = Clip(CrowdAnimatorBuilder.BodyPath, BodyActions.ClipName(BodyAction.TentExit)).length;
            Assert.That(disguise.ChangeSeconds, Is.GreaterThan(enter + exit), "some time unseen inside, where the clothes swap");
        }
    }
}
