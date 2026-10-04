using LastSeenWearing.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSeenWearing.UI.Field
{
    /// <summary>
    /// The field officer's crosshair (P1.12): a dot in the middle of the screen while this client plays the
    /// patrol, brighter when it rests on a character and amber when that one is close enough to cuff, with the
    /// Arrest hold filling a bar over it (P1.24). In development builds the character's name shows under
    /// it — a debug readout until stops give aiming their own feedback (P2.06). P1 placeholder look,
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
        private RectTransform _holdBar;
        private const float HoldBarWidth = 48f;
        private const float HoldBarHeight = 4f;
        private static readonly Color InReach = new(1f, 0.85f, 0.2f, 1f);

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
            _dot.color = patrol.InArrestRange ? InReach : target != null ? OnCharacter : Idle;
            _holdBar.gameObject.SetActive(patrol.ArrestHold > 0f);
            _holdBar.sizeDelta = new Vector2(HoldBarWidth * patrol.ArrestHold, HoldBarHeight); // the Arrest hold (P1.24)
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

            var barObject = new GameObject("ArrestHold", typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(canvasObject.transform, false);
            _holdBar = (RectTransform)barObject.transform;
            _holdBar.anchorMin = _holdBar.anchorMax = new Vector2(0.5f, 0.5f);
            _holdBar.pivot = new Vector2(0.5f, 0.5f);
            _holdBar.anchoredPosition = new Vector2(0f, DotSize * 2f); // above the dot; the debug name sits below
            barObject.GetComponent<Image>().color = InReach;
            barObject.GetComponent<Image>().raycastTarget = false;
            barObject.SetActive(false);

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
