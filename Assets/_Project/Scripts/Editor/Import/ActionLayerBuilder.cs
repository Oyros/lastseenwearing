using System;
using System.Collections.Generic;
using LastSeenWearing.Gameplay.Crowd;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastSeenWearing.Editor.Import
{
    /// <summary>
    /// The one-shot layer of a generated animator (P1.25): a full-body override on top, resting in an empty
    /// <see cref="WalkCycle.NoActionState"/> that leaves the walk below untouched, with one state per clip named after
    /// the clip. <see cref="WalkCycle.PlayAction"/> cross-fades into a state; at its end it blends back to rest.
    /// </summary>
    public static class ActionLayerBuilder
    {
        public static void Add(AnimatorController controller, string assetPath, IReadOnlyDictionary<string, AnimationClip> clips,
            IEnumerable<string> clipNames)
        {
            var machine = new AnimatorStateMachine { name = WalkCycle.ActionLayer };
            AssetDatabase.AddObjectToAsset(machine, controller);
            var rest = machine.AddState(WalkCycle.NoActionState);
            machine.defaultState = rest;

            foreach (var name in clipNames)
            {
                if (!clips.TryGetValue(name, out var clip))
                {
                    throw new InvalidOperationException($"[ActionLayer] {assetPath} has no clip {name}.");
                }

                var state = machine.AddState(name);
                state.motion = clip;
                state.iKOnFeet = true;
                var back = state.AddTransition(rest);
                back.hasExitTime = true;
                back.exitTime = 1f - WalkCycle.ActionBlendSeconds / Mathf.Max(clip.length, WalkCycle.ActionBlendSeconds * 2f);
                back.duration = WalkCycle.ActionBlendSeconds;
                back.hasFixedDuration = true;
            }

            controller.AddLayer(new AnimatorControllerLayer
            {
                name = WalkCycle.ActionLayer,
                stateMachine = machine,
                blendingMode = AnimatorLayerBlendingMode.Override,
                defaultWeight = 1f,
            });
        }
    }
}
