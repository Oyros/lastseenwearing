using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// How one CCTV camera's feed looks (GDD §04.1, docs/LOOKDEV.md §2): resolution and the greyscale
    /// treatment. Per camera, not global — two cameras can carry two looks (P1.14). Instance data under
    /// <c>Data/Cameras/</c>, referenced by the camera (docs/DATA.md §2).
    /// </summary>
    [CreateAssetMenu(fileName = "CctvFilter", menuName = "Last Seen Wearing/Cameras/CCTV Filter")]
    public sealed class CctvFilterProfile : ScriptableObject
    {
        [Header("Resolution")]
        [Tooltip("Feed resolution in pixels. LOOKDEV gate: 320×180.")]
        [SerializeField, Min(16)] private int _width = 320;
        [SerializeField, Min(16)] private int _height = 180;

        [Header("Treatment")]
        [Tooltip("Contrast around mid-grey; 1 = unchanged.")]
        [SerializeField, Range(0.2f, 3f)] private float _contrast = 1.25f;
        [Tooltip("Brightness offset added after contrast.")]
        [SerializeField, Range(-0.5f, 0.5f)] private float _brightness = 0f;
        [Tooltip("Strength of the per-frame grain, 0–1 of full scale.")]
        [SerializeField, Range(0f, 0.5f)] private float _grain = 0.08f;
        [Tooltip("Darkening of every other feed line (scan lines), 0–1.")]
        [SerializeField, Range(0f, 0.5f)] private float _scanLines = 0.12f;
        [Tooltip("Corner darkening, 0–1.")]
        [SerializeField, Range(0f, 1f)] private float _vignette = 0.35f;

        public int Width => _width;
        public int Height => _height;
        public float Contrast => _contrast;
        public float Brightness => _brightness;
        public float Grain => _grain;
        public float ScanLines => _scanLines;
        public float Vignette => _vignette;
    }
}
