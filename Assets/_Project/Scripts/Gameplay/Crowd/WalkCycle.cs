using System;
using LastSeenWearing.Core.Crowd;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Crowd
{
    /// <summary>
    /// A crowd body's walk on the crowd animator — the one piece every walking character shares, NPC or
    /// player, so the fugitive cannot walk differently from the crowd it hides in (P1.11). Base walk and
    /// traits from a <see cref="GaitSignature"/> (D-024, D-025); the cycle runs on metres walked (D-022);
    /// standing still goes to idle and fades the traits with it. A character that can run (<see cref="SetRunSpeeds"/>)
    /// blends into the Run clip as its speed rises from walking to running (PL.11b, D-034): one phase for both,
    /// the stride stretching from the walk's to the run's, so the feet stay planted through the change.
    /// </summary>
    public sealed class WalkCycle
    {
        /// <summary>Animator parameters of the crowd controller (built by CrowdAnimatorBuilder).</summary>
        public const string StyleParameter = "Style";
        public const string PhaseParameter = "Phase";
        public const string WalkingParameter = "Walking";
        public const string RunParameter = "Run";

        /// <summary>Walk ↔ idle, and the walk traits fading with it: long enough to hide the change of pose.</summary>
        public const float IdleBlendSeconds = 0.25f;

        // A body moved by physics advances only on fixed ticks, so most rendered frames at a high frame rate
        // see no movement at all: it counts as walking while it moved within this long, and its speed is
        // averaged over the same time constant rather than read off one frame.
        public const float SmoothingSeconds = 0.1f;

        // Below this per-tick distance a body has not moved: a controller's ground contact jitters.
        private const double StandingEpsilon = 1e-4;

        // One tick's speed sample is capped here: a jump (a schedule skipped ahead, a teleport) is not a sprint.
        private const float MaxSpeedSample = 10f;

        private static readonly int StyleId = Animator.StringToHash(StyleParameter);
        private static readonly int PhaseId = Animator.StringToHash(PhaseParameter);
        private static readonly int WalkingId = Animator.StringToHash(WalkingParameter);
        private static readonly int RunId = Animator.StringToHash(RunParameter);
        private static readonly WalkTrait[] Traits = (WalkTrait[])Enum.GetValues(typeof(WalkTrait));

        private readonly Animator _animator;
        private readonly float _strideLength;
        private readonly float _runStrideLength;
        private readonly float[] _traits = new float[Traits.Length];
        private readonly int[] _traitLayers = new int[Traits.Length];
        private double _lastWalked = -1d;
        private float _walkPresence = 1f;
        private float _walkSpeed;
        private float _runSpeed;
        private float _stride;
        private double _phaseOffset;
        private float _scale = 1f;
        private float _speed;
        private float _sinceMoved = float.PositiveInfinity;

        /// <summary>The parameter that picks a two-sided trait's side: −1 first clip, +1 second.</summary>
        public static string SideParameter(WalkTrait trait) => trait + "Side";

        /// <param name="runStrideLength">Metres per run cycle; 0 for a character that never runs (the crowd).</param>
        public WalkCycle(Animator animator, float strideLength, float runStrideLength = 0f)
        {
            _animator = animator;
            _strideLength = strideLength;
            _runStrideLength = runStrideLength > 0f ? runStrideLength : strideLength;
            _stride = strideLength;
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

        /// <summary>Metres per second, averaged over a tenth of a second.</summary>
        public float Speed => _speed;

        /// <summary>0 walking, 1 running.</summary>
        public float Run { get; private set; }

        /// <summary>The cycle's phase, 0–1 (contact_L at 0, contact_R at 0.5).</summary>
        public float Phase { get; private set; }

        /// <summary>
        /// Speeds between which the body blends from its walk into the run. Without them it only walks. Every
        /// client derives the blend from the speed it sees, so nothing about running is sent.
        /// </summary>
        /// <summary>A taller or shorter body (P1.19): both strides scale with it, so the feet stay planted.</summary>
        public void SetScale(float scale)
        {
            _scale = scale;
        }

        public void SetRunSpeeds(float walkSpeed, float runSpeed)
        {
            _walkSpeed = walkSpeed;
            _runSpeed = runSpeed;
        }

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
        /// the sign picks the side (see <see cref="WalkTrait"/>). Applied while walking, faded out while idle or running.
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
            var step = _lastWalked < 0d ? 0d : walked - _lastWalked;
            _lastWalked = walked;
            _sinceMoved = step > StandingEpsilon ? 0f : _sinceMoved + deltaTime;
            IsWalking = _sinceMoved < SmoothingSeconds;
            if (deltaTime > 0f)
            {
                var sample = Mathf.Min((float)(step / deltaTime), MaxSpeedSample);
                _speed += (sample - _speed) * (1f - Mathf.Exp(-deltaTime / SmoothingSeconds));
            }

            var targetRun = 0f;
            if (IsWalking && _runSpeed > _walkSpeed)
            {
                targetRun = Mathf.InverseLerp(_walkSpeed, _runSpeed, _speed);
            }

            Run = Mathf.MoveTowards(Run, targetRun, deltaTime / IdleBlendSeconds);

            // The stride follows the blend; the phase stays continuous when it changes (and is the plain
            // walked / stride of D-022 for a body that never runs).
            var stride = Mathf.Lerp(_strideLength, _runStrideLength, Run) * _scale;
            if (!Mathf.Approximately(stride, _stride))
            {
                _phaseOffset += walked / _stride - walked / stride;
                _stride = stride;
            }

            Phase = (float)(((walked / _stride + _phaseOffset) % 1d + 1d) % 1d);
            if (_animator == null)
            {
                return;
            }

            _animator.SetFloat(PhaseId, Phase);
            _animator.SetFloat(RunId, Run);
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
                    // A walk trait is a walk's: it fades out as the body breaks into a run.
                    _animator.SetLayerWeight(layer, Mathf.Abs(_traits[(int)trait]) * _walkPresence * (1f - Run));
                }
            }
        }
    }
}
