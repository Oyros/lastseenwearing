using LastSeenWearing.Gameplay.Cameras;
using UnityEngine;
using UnityEngine.UI;

namespace LastSeenWearing.UI.Watcher
{
    /// <summary>
    /// Shows a <see cref="CctvCamera"/>'s feed in UI with the CCTV look (<c>LSW/UI/CctvFeed</c>), set from the
    /// camera's profile. <see cref="Filtered"/> off shows the raw feed — for the walk test's answer key. The
    /// Watcher's monitor wall (P1.13) is made of these.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class CctvFeedView : MonoBehaviour
    {
        private static readonly int FeedSizeId = Shader.PropertyToID("_FeedSize");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int GrainId = Shader.PropertyToID("_Grain");
        private static readonly int ScanLinesId = Shader.PropertyToID("_ScanLines");
        private static readonly int VignetteId = Shader.PropertyToID("_Vignette");

        [SerializeField] private CctvCamera _camera;
        [SerializeField] private Material _feedMaterial;
        [SerializeField] private bool _filtered = true;

        private RawImage _image;
        private Material _material;

        public bool Filtered
        {
            get => _filtered;
            set
            {
                _filtered = value;
                Apply();
            }
        }

        /// <summary>The camera this view shows, or none.</summary>
        public CctvCamera Showing => _camera;

        public void Show(CctvCamera feedCamera)
        {
            _camera = feedCamera;
            Apply();
        }

        /// <summary>Draws what this view shows — filtered or raw — into <paramref name="target"/> (recording).</summary>
        public void RenderTo(RenderTexture target)
        {
            if (_camera == null)
            {
                return;
            }

            if (_filtered)
            {
                Graphics.Blit(_camera.Feed, target, _material);
            }
            else
            {
                Graphics.Blit(_camera.Feed, target);
            }
        }

        /// <summary>The CCTV look's material, for views built in code (the Watcher's wall).</summary>
        public Material FeedMaterial
        {
            set
            {
                _feedMaterial = value;
                Apply();
            }
        }

        private void Awake()
        {
            _image = GetComponent<RawImage>();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }

        private void Apply()
        {
            if (_image == null || _camera == null || _feedMaterial == null)
            {
                return;
            }

            if (_material == null)
            {
                _material = new Material(_feedMaterial) { name = $"{_feedMaterial.name} ({name})" };
            }

            var profile = _camera.Profile;
            _material.SetVector(FeedSizeId, new Vector4(profile.Width, profile.Height, 0f, 0f));
            _material.SetFloat(ContrastId, profile.Contrast);
            _material.SetFloat(BrightnessId, profile.Brightness);
            _material.SetFloat(GrainId, profile.Grain);
            _material.SetFloat(ScanLinesId, profile.ScanLines);
            _material.SetFloat(VignetteId, profile.Vignette);

            _image.texture = _camera.Feed;
            _image.material = _filtered ? _material : null;
        }
    }
}
