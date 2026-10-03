using System.Collections.Generic;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Voice
{
    /// <summary>
    /// The talk light on an officer's radio (GDD §04.2, PL.18): lit while the radio is on air, for everyone —
    /// the fugitive and the cameras see it too. Off, the lamp's emission and colour are dimmed through a
    /// property block, so the material is untouched.
    /// </summary>
    public sealed class RadioLight : MonoBehaviour
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly HashSet<RadioLight> All = new();
        private static bool _onAir;

        // Placeholder look of an unlit lamp, not tuning.
        private static readonly Color OffColour = new(0.03f, 0.05f, 0.03f, 1f);

        [SerializeField] private Renderer _lamp;

        private MaterialPropertyBlock _block;

        public bool IsLit { get; private set; }

        /// <summary>Every radio light in the festival follows the radio (they all hear the same channel).</summary>
        public static void SetOnAir(bool onAir)
        {
            _onAir = onAir;
            foreach (var light in All)
            {
                light.Show(onAir);
            }
        }

        private void OnEnable()
        {
            All.Add(this);
            Show(_onAir);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        private void Show(bool lit)
        {
            IsLit = lit;
            _block ??= new MaterialPropertyBlock();
            _block.Clear();
            if (!lit)
            {
                _block.SetColor(EmissionId, Color.black);
                _block.SetColor(BaseColorId, OffColour);
            }

            _lamp.SetPropertyBlock(_block);
        }
    }
}
