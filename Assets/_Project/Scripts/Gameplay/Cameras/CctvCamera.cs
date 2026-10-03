using LastSeenWearing.Core.Config;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Cameras
{
    /// <summary>
    /// A festival CCTV camera (GDD §04.1): it renders into a low-resolution feed — its profile's size,
    /// point-filtered, so pixels stay pixels when the Watcher's wall shows it large. The greyscale look is
    /// applied where the feed is shown (UI <c>CctvFeedView</c>), so one feed can also be seen raw (the walk
    /// test's answer key). Each client renders the cameras it may see; nothing is streamed (ARCHITECTURE).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CctvCamera : MonoBehaviour
    {
        [SerializeField] private CctvFilterProfile _profile;
        [Tooltip("The layout's one camera that pans and zooms (P1.17a).")]
        [SerializeField] private bool _zoomable;

        private RenderTexture _feed;

        public CctvFilterProfile Profile => _profile;
        public bool Zoomable => _zoomable;

        /// <summary>The camera's picture. Created on first use.</summary>
        public RenderTexture Feed
        {
            get
            {
                if (_feed == null)
                {
                    CreateFeed();
                }

                return _feed;
            }
        }

        private void Awake()
        {
            CreateFeed();
        }

        private void OnDestroy()
        {
            if (_feed != null)
            {
                GetComponent<Camera>().targetTexture = null;
                _feed.Release();
                Destroy(_feed);
            }
        }

        private void CreateFeed()
        {
            if (_feed != null)
            {
                return;
            }

            _feed = new RenderTexture(_profile.Width, _profile.Height, 24)
            {
                name = $"Feed_{name}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            GetComponent<Camera>().targetTexture = _feed;
        }
    }
}
