using System;
using System.IO;
using System.Linq;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Gameplay.Crowd;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastSeenWearing.Editor.Import
{
    /// <summary>
    /// Builds the crowd's animator from the rigged body's clips (P1.04, D-022) — generated, never edited
    /// by hand, so a re-export is one menu click. One state, <c>Walk</c>: a 1D blend tree over the
    /// <see cref="BaseWalk"/> clips on <see cref="StyleParameter"/> (threshold = the enum value), its
    /// time driven by <see cref="PhaseParameter"/> instead of the clock, so the cycle follows distance
    /// walked and the feet cannot slide.
    /// </summary>
    public static class CrowdAnimatorBuilder
    {
        public const string BodyPath = "Assets/_Project/Art/Models/Crowd/LSW_Crowd_Body_M.fbx";
        public const string ControllerPath = "Assets/_Project/Art/Animations/Crowd/AC_Crowd.controller";
        public const string StyleParameter = CrowdAgent.StyleParameter;
        public const string PhaseParameter = CrowdAgent.PhaseParameter;
        public const string WalkingParameter = CrowdAgent.WalkingParameter;
        public const string IdleState = "Idle";
        public const string IdleClip = "Idle_Stand";

        // Walk ↔ idle: long enough to hide the change of pose, short enough that a stop reads as a stop.
        private const float IdleBlendSeconds = 0.25f;
        public const string WalkState = "Walk";
        private const string ClipPrefix = "Walk_";

        public static string ClipName(BaseWalk walk) => ClipPrefix + walk;

        [MenuItem("Last Seen Wearing/Art/Build Crowd Animator")]
        public static void BuildFromMenu()
        {
            var controller = Build();
            Debug.Log($"[CrowdAnimator] Built {ControllerPath} ({controller.layers[0].stateMachine.states.Length} state).");
            EditorGUIUtility.PingObject(controller);
        }

        public static AnimatorController Build()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(BodyPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview"))
                .ToDictionary(c => c.name);

            // Rebuilt in place, never deleted: the asset keeps its GUID, so prefabs keep their reference.
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            Clear(controller);
            controller.AddParameter(StyleParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(PhaseParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = WalkingParameter,
                type = AnimatorControllerParameterType.Bool,
                defaultBool = true,
            });

            var tree = new BlendTree
            {
                name = "BaseWalks",
                blendType = BlendTreeType.Simple1D,
                blendParameter = StyleParameter,
                useAutomaticThresholds = false,
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            foreach (BaseWalk walk in Enum.GetValues(typeof(BaseWalk)))
            {
                if (!clips.TryGetValue(ClipName(walk), out var clip))
                {
                    throw new InvalidOperationException($"[CrowdAnimator] {BodyPath} has no clip {ClipName(walk)}.");
                }

                tree.AddChild(clip, (float)walk);
            }

            var machine = controller.layers[0].stateMachine;
            var state = machine.AddState(WalkState);
            state.motion = tree;
            state.timeParameterActive = true;
            state.timeParameter = PhaseParameter;
            // Humanoid retargeting rebuilds the feet in muscle space and lets a planted foot creep (≤ 8.3 mm
            // per stance measured in P1.04); Foot IK pins it to the clip's own foot goal (≤ 3.7 mm).
            state.iKOnFeet = true;
            machine.defaultState = state;

            // Idle (PL.11a) plays on its own clock — a lingering NPC is cosmetic, nothing reads its phase.
            if (!clips.TryGetValue(IdleClip, out var idleClip))
            {
                throw new InvalidOperationException($"[CrowdAnimator] {BodyPath} has no clip {IdleClip}.");
            }

            var idle = machine.AddState(IdleState);
            idle.motion = idleClip;
            idle.iKOnFeet = true;
            Connect(state, idle, AnimatorConditionMode.IfNot);
            Connect(idle, state, AnimatorConditionMode.If);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void Connect(AnimatorState from, AnimatorState to, AnimatorConditionMode walking)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = IdleBlendSeconds;
            transition.hasFixedDuration = true;
            transition.AddCondition(walking, 0f, WalkingParameter);
        }

        private static void Clear(AnimatorController controller)
        {
            foreach (var parameter in controller.parameters)
            {
                controller.RemoveParameter(parameter);
            }

            while (controller.layers.Length > 1)
            {
                controller.RemoveLayer(controller.layers.Length - 1);
            }

            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states)
            {
                machine.RemoveState(child.state);
            }

            foreach (var tree in AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>())
            {
                AssetDatabase.RemoveObjectFromAsset(tree);
                UnityEngine.Object.DestroyImmediate(tree, true);
            }
        }
    }
}
