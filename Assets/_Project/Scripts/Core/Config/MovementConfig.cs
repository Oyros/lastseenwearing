using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// How field characters move (docs/DATA.md §3, GDD §03). The fugitive walks at the crowd's own pace —
    /// <c>CrowdConfig</c>'s walk speed × its pace bucket — so it cannot be told apart by speed; only what is
    /// the player's own (running, turning, looking) lives here. Patrol and dog speeds arrive with P1.12.
    /// </summary>
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "Last Seen Wearing/Config/Movement")]
    public sealed class MovementConfig : ScriptableObject
    {
        [Header("Placeholder body (P0.10 capsule)")]
        [Tooltip("Metres per second. [PROVISIONAL] — until each role has its controller.")]
        [SerializeField, Min(0f)] private float _walkSpeed = 1.4f;

        [Header("Fugitive")]
        [Tooltip("Metres per second while sprinting. Running is the one thing the crowd notices (GDD §04.3). [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _fugitiveRunSpeed = 3.4f;
        [Tooltip("Metres per second², speeding up and slowing down. [PROVISIONAL]")]
        [SerializeField, Min(0.1f)] private float _acceleration = 8f;
        [Tooltip("Degrees per second the body turns to face where it walks. [PROVISIONAL]")]
        [SerializeField, Min(1f)] private float _turnSpeed = 540f;

        [Header("Look")]
        [Tooltip("Degrees per mouse count.")]
        [SerializeField, Min(0.001f)] private float _mouseSensitivity = 0.12f;
        [Tooltip("Degrees per second at full stick.")]
        [SerializeField, Min(1f)] private float _stickSensitivity = 160f;

        [Header("Interaction")]
        [Tooltip("Metres within which a character can use something (tent, target).")]
        [SerializeField, Min(0.1f)] private float _interactRange = 1.6f;

        public float WalkSpeed => _walkSpeed;
        public float FugitiveRunSpeed => _fugitiveRunSpeed;
        public float Acceleration => _acceleration;
        public float TurnSpeed => _turnSpeed;
        public float MouseSensitivity => _mouseSensitivity;
        public float StickSensitivity => _stickSensitivity;
        public float InteractRange => _interactRange;
    }
}
