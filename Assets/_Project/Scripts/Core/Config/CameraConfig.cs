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

        public float ThirdPersonDistance => _thirdPersonDistance;
        public float ThirdPersonPivotHeight => _thirdPersonPivotHeight;
        public float ThirdPersonShoulder => _thirdPersonShoulder;
        public float ThirdPersonDamping => _thirdPersonDamping;
        public float ThirdPersonPitchMin => _thirdPersonPitchMin;
        public float ThirdPersonPitchMax => _thirdPersonPitchMax;
        public float ThirdPersonFieldOfView => _thirdPersonFieldOfView;
    }
}
