using System;
using System.IO;
using System.Linq;
using LastSeenWearing.Gameplay.Crowd;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastSeenWearing.Editor.Import
{
    /// <summary>
    /// Builds the first-person arms' animator from their clips (PL.19, P1.12) — generated, like
    /// <see cref="CrowdAnimatorBuilder"/>. <c>Walk</c> runs on the same distance phase as the body (D-022), so the
    /// arms swing with the steps the patrol takes; <c>Idle</c> while standing. The stop and cuffs gestures
    /// join with P1.24.
    /// </summary>
    public static class FpArmsAnimatorBuilder
    {
        public const string ArmsPath = "Assets/_Project/Art/Models/FP/LSW_FP_Arms_M.fbx";
        public const string ControllerPath = "Assets/_Project/Art/Animations/FP/AC_FPArms.controller";
        public const string IdleClip = "FP_Idle";
        public const string WalkClip = "FP_Walk";

        /// <summary>The arms' one-shot when the patrol cuffs someone (PL.19, P1.25).</summary>
        public const string CuffsClip = "FP_Cuffs";

        [MenuItem("Last Seen Wearing/Art/Build First-Person Arms Animator")]
        public static void BuildFromMenu()
        {
            EditorGUIUtility.PingObject(Build());
        }

        public static AnimatorController Build()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ArmsPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview"))
                .ToDictionary(c => c.name);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            foreach (var parameter in controller.parameters)
            {
                controller.RemoveParameter(parameter);
            }

            while (controller.layers.Length > 1)
            {
                controller.RemoveLayer(controller.layers.Length - 1);
            }

            var machine = controller.layers[0].stateMachine;
            foreach (var orphan in AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<AnimatorStateMachine>().Where(m => m != machine))
            {
                foreach (var child in orphan.states)
                {
                    AssetDatabase.RemoveObjectFromAsset(child.state);
                    UnityEngine.Object.DestroyImmediate(child.state, true);
                }

                AssetDatabase.RemoveObjectFromAsset(orphan);
                UnityEngine.Object.DestroyImmediate(orphan, true);
            }

            foreach (var child in machine.states)
            {
                machine.RemoveState(child.state);
            }

            controller.AddParameter(WalkCycle.PhaseParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(WalkCycle.WalkingParameter, AnimatorControllerParameterType.Bool);

            var idle = machine.AddState("Idle");
            idle.motion = Clip(clips, IdleClip);
            var walk = machine.AddState("Walk");
            walk.motion = Clip(clips, WalkClip);
            walk.timeParameterActive = true;
            walk.timeParameter = WalkCycle.PhaseParameter;
            machine.defaultState = idle;

            Connect(idle, walk, AnimatorConditionMode.If);
            Connect(walk, idle, AnimatorConditionMode.IfNot);
            ActionLayerBuilder.Add(controller, ArmsPath, clips, new[] { CuffsClip });

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip Clip(System.Collections.Generic.Dictionary<string, AnimationClip> clips, string name)
        {
            return clips.TryGetValue(name, out var clip)
                ? clip
                : throw new InvalidOperationException($"[FpArmsAnimator] {ArmsPath} has no clip {name}.");
        }

        private static void Connect(AnimatorState from, AnimatorState to, AnimatorConditionMode walking)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = WalkCycle.IdleBlendSeconds;
            transition.hasFixedDuration = true;
            transition.AddCondition(walking, 0f, WalkCycle.WalkingParameter);
        }
    }
}
