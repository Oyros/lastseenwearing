using System;
using LastSeenWearing.Core.Crowd;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Crowd
{
    /// <summary>
    /// A crowd body's walk on the crowd animator — the one piece every walking character shares, NPC or
    /// player, so the fugitive cannot walk differently from the crowd it hides in (P1.11). Base walk and
    /// traits from a <see cref="GaitSignature"/> (D-024, D-025); the cycle runs on metres walked (D-022);
    /// standing still goes to idle and fades the traits with it.
    /// </summary>
    public sealed class WalkCycle
    {
        /// <summary>Animator parameters of the crowd controller (built by CrowdAnimatorBuilder).</summary>
        public const string StyleParameter = "Style";
        public const string PhaseParameter = "Phase";
        public const string WalkingParameter = "Walking";

        /// <summary>Walk ↔ idle, and the walk traits fading with it: long enough to hide the change of pose.</summary>
        public const float IdleBlendSeconds = 0.25f;

        // Below this per-tick distance a body counts as standing: a controller's ground contact jitters.
        private const double StandingEpsilon = 1e-4;

        private static readonly int StyleId = Animator.StringToHash(StyleParameter);
        private static readonly int PhaseId = Animator.StringToHash(PhaseParameter);
        private static readonly int WalkingId = Animator.StringToHash(WalkingParameter);
        private static readonly WalkTrait[] Traits = (WalkTrait[])Enum.GetValues(typeof(WalkTrait));

        private readonly Animator _animator;
        private readonly float _strideLength;
        private readonly float[] _traits = new float[Traits.Length];
        private readonly int[] _traitLayers = new int[Traits.Length];
        private double _lastWalked = -1d;
        private float _walkPresence = 1f;

        /// <summary>The parameter that picks a two-sided trait's side: −1 first clip, +1 second.</summary>
        public static string SideParameter(WalkTrait trait) => trait + "Side";

        public WalkCycle(Animator animator, float strideLength)
        {
            _animator = animator;
            _strideLength = strideLength;
            if (_animator == null)
            {
                return;
            }

            foreach (var trait in Traits)
            {
                _traitLayers[(int)trait] = _animator.GetLayerIndex(trait.ToString());
            }
        }

        public bool IsWalking { get; private set; }

        public void Apply(GaitSignature gait)
        {
            SetBase(gait.Base);
            Array.Clear(_traits, 0, _traits.Length);
            foreach (var trait in gait.Traits)
            {
                SetTrait(trait.Trait, trait.Strength);
            }

            ApplyTraits();
        }

        public void SetBase(BaseWalk walk)
        {
            if (_animator != null)
            {
                _animator.SetFloat(StyleId, (float)walk);
            }
        }

        /// <summary>
        /// A walk trait's strength, 0 to 1; for <see cref="WalkTrait.Limp"/> and <see cref="WalkTrait.ArmSwing"/>
        /// the sign picks the side (see <see cref="WalkTrait"/>). Applied while walking, faded out while idle.
        /// </summary>
        public void SetTrait(WalkTrait trait, float signedStrength)
        {
            _traits[(int)trait] = Mathf.Clamp(signedStrength, -1f, 1f);
            if (_animator != null && (trait == WalkTrait.Limp || trait == WalkTrait.ArmSwing))
            {
                _animator.SetFloat(SideParameter(trait), signedStrength < 0f ? -1f : 1f);
            }

            ApplyTraits();
        }

        /// <summary>
        /// One tick: <paramref name="walked"/> is the body's total metres walked so far. One stride per cycle,
        /// so a planted foot moves back exactly as far as the body moves on, at any speed (D-022).
        /// </summary>
        public void Advance(double walked, float deltaTime)
        {
            IsWalking = walked > _lastWalked + StandingEpsilon;
            _lastWalked = walked;
            if (_animator == null)
            {
                return;
            }

            _animator.SetFloat(PhaseId, (float)(walked / _strideLength % 1d));
            _animator.SetBool(WalkingId, IsWalking);
            _walkPresence = Mathf.MoveTowards(_walkPresence, IsWalking ? 1f : 0f, deltaTime / IdleBlendSeconds);
            ApplyTraits();
        }

        private void ApplyTraits()
        {
            if (_animator == null)
            {
                return;
            }

            foreach (var trait in Traits)
            {
                var layer = _traitLayers[(int)trait];
                if (layer > 0)
                {
                    _animator.SetLayerWeight(layer, Mathf.Abs(_traits[(int)trait]) * _walkPresence);
                }
            }
        }
    }
}
