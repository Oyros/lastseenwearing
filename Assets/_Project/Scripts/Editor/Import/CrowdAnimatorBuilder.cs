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
        public const string RunParameter = WalkCycle.RunParameter;
        public const string RunClip = "Run";
        public const string IdleState = "Idle";
        public const string IdleClip = "Idle_Stand";

        /// <summary>Picks a two-sided layer's clip: −1 the first, +1 the second (see <see cref="WalkTrait"/>).</summary>
        public static string SideParameter(WalkTrait trait) => CrowdAgent.SideParameter(trait);

        /// <summary>
        /// Each additive layer's clips (PL.12), in side order: two for a two-sided trait (−, +), one otherwise.
        /// The layer is named after the trait.
        /// </summary>
        public static string[] TraitClips(WalkTrait trait) => trait switch
        {
            WalkTrait.Limp => new[] { "Add_Limp_L", "Add_Limp_R" },
            WalkTrait.ArmSwing => new[] { "Add_ArmSwing_Stiff", "Add_ArmSwing_Big" },
            _ => new[] { "Add_" + trait },
        };

        private const float IdleBlendSeconds = CrowdAgent.IdleBlendSeconds;
        public const string WalkState = "Walk";
        private const string ClipPrefix = "Walk_";

        public static string ClipName(BaseWalk walk) => ClipPrefix + walk;

        /// <summary>The body's one-shot clips, one per <see cref="BodyAction"/> but None.</summary>
        public static string[] ActionClips => Enum.GetValues(typeof(BodyAction)).Cast<BodyAction>()
            .Where(a => a != BodyAction.None).Select(BodyActions.ClipName).ToArray();

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
            controller.AddParameter(RunParameter, AnimatorControllerParameterType.Float);
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

            // The run (PL.11b) shares the walk's phase — its contacts sit at 0 and 0.5 like every walk's — so
            // Run blends walking into running on the same step; WalkCycle stretches the stride with it.
            if (!clips.TryGetValue(RunClip, out var runClip))
            {
                throw new InvalidOperationException($"[CrowdAnimator] {BodyPath} has no clip {RunClip}.");
            }

            var gait = new BlendTree
            {
                name = "WalkToRun",
                blendType = BlendTreeType.Simple1D,
                blendParameter = RunParameter,
                useAutomaticThresholds = false,
            };
            AssetDatabase.AddObjectToAsset(gait, controller);
            gait.AddChild(tree, 0f);
            gait.AddChild(runClip, 1f);

            var machine = controller.layers[0].stateMachine;
            var state = machine.AddState(WalkState);
            state.motion = gait;
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

            foreach (WalkTrait trait in Enum.GetValues(typeof(WalkTrait)))
            {
                AddTraitLayer(controller, trait, clips);
            }

            // One-shots over everything (PL.17, P1.25): target jobs, tent, arrest.
            ActionLayerBuilder.Add(controller, BodyPath, clips, ActionClips);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        // One additive layer per trait (P1.05): its strength is the layer weight (0 until the gait signature
        // sets it, P1.06), its time the same Phase as the base walk, so the delta stays on the step it was
        // authored for. Each clip's reference pose is frame 40 of the body file (BodyClipPostprocessor).
        private static void AddTraitLayer(AnimatorController controller, WalkTrait trait, System.Collections.Generic.Dictionary<string, AnimationClip> clips)
        {
            var names = TraitClips(trait);
            var traitClips = names.Select(n => clips.TryGetValue(n, out var c)
                ? c
                : throw new InvalidOperationException($"[CrowdAnimator] {BodyPath} has no clip {n}.")).ToArray();

            Motion motion = traitClips[0];
            if (traitClips.Length == 2)
            {
                var side = SideParameter(trait);
                controller.AddParameter(side, AnimatorControllerParameterType.Float);
                var tree = new BlendTree
                {
                    name = trait + "Sides",
                    blendType = BlendTreeType.Simple1D,
                    blendParameter = side,
                    useAutomaticThresholds = false,
                };
                AssetDatabase.AddObjectToAsset(tree, controller);
                tree.AddChild(traitClips[0], -1f);
                tree.AddChild(traitClips[1], 1f);
                motion = tree;
            }

            var machine = new AnimatorStateMachine { name = trait.ToString() };
            AssetDatabase.AddObjectToAsset(machine, controller);
            var state = machine.AddState(trait.ToString());
            state.motion = motion;
            state.timeParameterActive = true;
            state.timeParameter = PhaseParameter;
            machine.defaultState = state;

            controller.AddLayer(new AnimatorControllerLayer
            {
                name = trait.ToString(),
                stateMachine = machine,
                blendingMode = AnimatorLayerBlendingMode.Additive,
                defaultWeight = 0f,
            });
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

            foreach (var tree in AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>())
            {
                AssetDatabase.RemoveObjectFromAsset(tree);
                UnityEngine.Object.DestroyImmediate(tree, true);
            }
        }
    }
}
