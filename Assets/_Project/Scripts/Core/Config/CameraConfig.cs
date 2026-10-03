using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// The players' own cameras (docs/DATA.md §3, GDD §02): the fugitive's over-the-shoulder third person
    /// now; the field team's first-person cameras with P1.12. The CCTV feeds are per camera
    /// (<see cref="CctvFilterProfile"/>), not here.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Last Seen Wearing/Config/Camera")]
    public sealed class CameraConfig : ScriptableObject
    {
        [Header("Fugitive — third person")]
        [Tooltip("Metres behind the pivot.")]
        [SerializeField, Min(0.5f)] private float _thirdPersonDistance = 3.2f;
        [Tooltip("Pivot height above the feet, metres.")]
        [SerializeField, Min(0f)] private float _thirdPersonPivotHeight = 1.55f;
        [Tooltip("Metres to the side of the pivot (right shoulder).")]
        [SerializeField] private float _thirdPersonShoulder = 0.45f;
        [Tooltip("Seconds the camera takes to catch up with the body.")]
        [SerializeField, Min(0f)] private float _thirdPersonDamping = 0.12f;
        [SerializeField, Range(-89f, 0f)] private float _thirdPersonPitchMin = -35f;
        [SerializeField, Range(0f, 89f)] private float _thirdPersonPitchMax = 60f;
        [SerializeField, Range(30f, 100f)] private float _thirdPersonFieldOfView = 60f;
        [Tooltip("Metres the third-person camera keeps from walls and props (it pulls in rather than clip through).")]
        [SerializeField, Range(0.05f, 1f)] private float _thirdPersonCollisionRadius = 0.2f;

        [Header("Field team — first person (P1.12)")]
        [Tooltip("Eye height above the feet, metres — the uniform body's SOCKET_Eye.")]
        [SerializeField, Min(0.5f)] private float _firstPersonEyeHeight = 1.68f;
        [Tooltip("HORIZONTAL degrees, as Blender frames PL.19's arms (90°); turned into Unity's vertical field of view for the screen's aspect.")]
        [SerializeField, Range(50f, 110f)] private float _firstPersonFieldOfView = 90f;
        [Tooltip("Near clip, metres: the arms sit a hand's breadth from the eye.")]
        [SerializeField, Range(0.01f, 0.3f)] private float _firstPersonNearClip = 0.05f;
        [SerializeField, Range(-89f, 0f)] private float _firstPersonPitchMin = -80f;
        [SerializeField, Range(0f, 89f)] private float _firstPersonPitchMax = 80f;

        [Header("Aim")]
        [Tooltip("Metres the field team's aim reaches: who you are looking at, for stops and arrests.")]
        [SerializeField, Min(1f)] private float _aimRange = 30f;

        public float FirstPersonEyeHeight => _firstPersonEyeHeight;
        public float FirstPersonFieldOfView => _firstPersonFieldOfView;
        public float FirstPersonNearClip => _firstPersonNearClip;
        public float FirstPersonPitchMin => _firstPersonPitchMin;
        public float FirstPersonPitchMax => _firstPersonPitchMax;
        public float AimRange => _aimRange;

        public float ThirdPersonDistance => _thirdPersonDistance;
        public float ThirdPersonPivotHeight => _thirdPersonPivotHeight;
        public float ThirdPersonShoulder => _thirdPersonShoulder;
        public float ThirdPersonDamping => _thirdPersonDamping;
        public float ThirdPersonPitchMin => _thirdPersonPitchMin;
        public float ThirdPersonPitchMax => _thirdPersonPitchMax;
        public float ThirdPersonFieldOfView => _thirdPersonFieldOfView;
        public float ThirdPersonCollisionRadius => _thirdPersonCollisionRadius;
    }
}
