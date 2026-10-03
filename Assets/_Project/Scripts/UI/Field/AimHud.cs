using LastSeenWearing.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSeenWearing.UI.Field
{
    /// <summary>
    /// The field officer's crosshair (P1.12): a dot in the middle of the screen while this client plays the
    /// patrol, brighter when it rests on a character. In development builds the character's name shows under
    /// it — a debug readout until stop and arrest give aiming its real feedback (P1.24). P1 placeholder look,
    /// built in code like <c>RoundHud</c>.
    /// </summary>
    public sealed class AimHud : MonoBehaviour
    {
        // Placeholder look, not tuning.
        private const float DotSize = 6f;
        private const float FontSize = 16f;
        private static readonly Color Idle = new(1f, 1f, 1f, 0.5f);
        private static readonly Color OnCharacter = new(1f, 1f, 1f, 1f);

        private GameObject _frame;
        private Image _dot;
        private TextMeshProUGUI _debugName;

        private void Awake()
        {
            BuildFrame();
        }

        private void Update()
        {
            var patrol = PatrolController.Local;
            _frame.SetActive(patrol != null);
            if (patrol == null)
            {
                return;
            }

            var target = patrol.AimTarget;
            _dot.color = target != null ? OnCharacter : Idle;
            _debugName.text = Debug.isDebugBuild && target != null ? target.Character.name : string.Empty;
        }

        private void BuildFrame()
        {
            var canvasObject = new GameObject("AimCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _frame = canvasObject;

            var dotObject = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            dotObject.transform.SetParent(canvasObject.transform, false);
            var dotRect = (RectTransform)dotObject.transform;
            dotRect.anchorMin = dotRect.anchorMax = dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(DotSize, DotSize);
            _dot = dotObject.GetComponent<Image>();
            _dot.raycastTarget = false;

            var nameObject = new GameObject("DebugName", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObject.transform.SetParent(canvasObject.transform, false);
            var nameRect = (RectTransform)nameObject.transform;
            nameRect.anchorMin = nameRect.anchorMax = nameRect.pivot = new Vector2(0.5f, 0.5f);
            nameRect.anchoredPosition = new Vector2(0f, -DotSize * 4f);
            nameRect.sizeDelta = new Vector2(400f, FontSize * 2f);
            _debugName = nameObject.GetComponent<TextMeshProUGUI>();
            _debugName.fontSize = FontSize;
            _debugName.alignment = TextAlignmentOptions.Center;
            _debugName.raycastTarget = false;
        }
    }
}
