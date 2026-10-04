using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// How field characters move (docs/DATA.md §3, GDD §03). The fugitive walks at the crowd's own pace —
    /// <c>CrowdConfig</c>'s walk speed × its pace bucket — so it cannot be told apart by speed; only what is
    /// the player's own (running, turning, looking) lives here, and the patrol's speeds (P1.12). The dog's arrive later.
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

        [Header("Patrol")]
        [Tooltip("Metres per second walking. GDD §03: the patrol is fast. [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _patrolWalkSpeed = 1.7f;
        [Tooltip("Metres per second sprinting — faster than the fugitive's sprint. [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _patrolRunSpeed = 4.2f;

        [Header("Plainclothes")]
        [Tooltip("Metres per second walking — the crowd's pace, so they pass for one of it (GDD §03). [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _plainclothesWalkSpeed = 1.4f;
        [Tooltip("Metres per second sprinting — the fugitive's sprint, slower than the patrol's. [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _plainclothesRunSpeed = 3.6f;

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
        public float PatrolWalkSpeed => _patrolWalkSpeed;
        public float PatrolRunSpeed => _patrolRunSpeed;
        public float PlainclothesWalkSpeed => _plainclothesWalkSpeed;
        public float PlainclothesRunSpeed => _plainclothesRunSpeed;
        public float Acceleration => _acceleration;
        public float TurnSpeed => _turnSpeed;
        public float MouseSensitivity => _mouseSensitivity;
        public float StickSensitivity => _stickSensitivity;
        public float InteractRange => _interactRange;
    }
}
