using LastSeenWearing.Gameplay.Cameras;
using UnityEngine;

namespace LastSeenWearing.UI.Watcher
{
    /// <summary>
    /// The walk test's full-screen feed (docs/WALKTEST.md, P1.07): <see cref="Take.Near"/> and
    /// <see cref="Take.Far"/> through the CCTV look, <see cref="Take.Key"/> raw from the key camera so the
    /// trait markers keep their colours. The recorder switches takes; keys 1–3 do it by hand. Development
    /// scene only.
    /// </summary>
    public sealed class WalkTestView : MonoBehaviour
    {
        public enum Take
        {
            Near,
            Far,
            Key,
        }

        [SerializeField] private CctvFeedView _view;
        [SerializeField] private CctvCamera _near;
        [SerializeField] private CctvCamera _far;
        [SerializeField] private CctvCamera _key;
        [SerializeField] private Take _take = Take.Near;

        // Recording output: the current take, as shown, at a size a video player shows comfortably.
        private const int OutputWidth = 1280;
        private const int OutputHeight = 720;

        public Take Current => _take;

        /// <summary>The current take drawn every frame, for the recorder — independent of the Game view.</summary>
        public RenderTexture Output { get; private set; }

        public void Show(Take take)
        {
            _take = take;
            _view.Show(take switch
            {
                Take.Far => _far,
                Take.Key => _key,
                _ => _near,
            });
            _view.Filtered = take != Take.Key;
        }

        private void Start()
        {
            Output = new RenderTexture(OutputWidth, OutputHeight, 0) { name = "WalkTestOutput", filterMode = FilterMode.Point };
            Show(_take);
        }

        private void LateUpdate()
        {
            _view.RenderTo(Output);
        }

        private void OnDestroy()
        {
            if (Output != null)
            {
                Output.Release();
                Destroy(Output);
            }
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                Show(Take.Near);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                Show(Take.Far);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                Show(Take.Key);
            }
        }
    }
}
