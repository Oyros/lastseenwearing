using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Movement;
using LastSeenWearing.Gameplay.Crowd;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSeenWearing.Gameplay.Player
{
    /// <summary>
    /// The patrol (GDD §03, P1.12): first person, fast, in uniform. The owner looks with mouse or stick, walks and
    /// sprints relative to the view, sees first-person arms (PL.19) instead of their body, and aims — the
    /// character under the crosshair is <see cref="AimTarget"/>, the input of stop and arrest (P1.24). Everyone
    /// else sees the uniformed body walk on the crowd's <see cref="WalkCycle"/>; an owner-authority
    /// <c>NetworkTransform</c> carries it.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PatrolController : NetworkBehaviour
    {
        private static readonly int PhaseId = Animator.StringToHash(WalkCycle.PhaseParameter);
        private static readonly int WalkingId = Animator.StringToHash(WalkCycle.WalkingParameter);

        // PL.19's arms rig carries a non-deforming bone at the eye the arms were framed from.
        private const string ArmsEyeBone = "Camera";

        [SerializeField] private MovementConfig _movement;
        [SerializeField] private CameraConfig _camera;
        [SerializeField] private CrowdConfig _crowd;
        [SerializeField] private Animator _fpArmsPrefab;

        private CharacterController _body;
        private WalkCycle _walk;
        private double _walked;
        private Vector3 _lastPosition;

        // Owner only.
        private LastSeenWearingControls _controls;
        private Camera _eye;
        private Animator _arms;
        private Vector2 _move;
        private bool _sprint;
        private float _yaw;
        private float _pitch;
        private GroundPoint _velocity;
        private float _fall;

        /// <summary>The patrol this client plays, or none.</summary>
        public static PatrolController Local { get; private set; }

        /// <summary>Owner only: the character under the crosshair, or none.</summary>
        public CharacterHitbox AimTarget { get; private set; }

        public override void OnNetworkSpawn()
        {
            _body = GetComponent<CharacterController>();
            _walk = new WalkCycle(GetComponentInChildren<Animator>(), _crowd.StrideLength);
            _walk.SetBase(BaseWalk.Normal); // a uniform walks plainly; the patrol is not hiding
            _lastPosition = transform.position;

            if (!IsOwner)
            {
                _body.enabled = false; // remote bodies follow the network transform, not physics
                return;
            }

            // A client's body is created at the origin and then moved to its spawn point; the controller still
            // holds the origin and its first Move would snap back there. Re-enabling it takes the real position.
            _body.enabled = false;
            _body.enabled = true;
            _controls = new LastSeenWearingControls();
            _controls.Field.Enable();
            _yaw = transform.eulerAngles.y;
            Local = this;
            TakeTheCamera();
        }

        public override void OnNetworkDespawn()
        {
            _controls?.Dispose();
            _controls = null;
            if (Local == this)
            {
                Local = null;
            }

            if (_arms != null)
            {
                Destroy(_arms.gameObject);
            }
        }

        // Latch input in Update, use it in FixedUpdate (CONVENTIONS.md §6).
        private void Update()
        {
            if (IsOwner && _controls != null)
            {
                var field = _controls.Field;
                _move = field.Move.ReadValue<Vector2>();
                _sprint = field.Sprint.IsPressed();

                var look = field.Look.ReadValue<Vector2>();
                var isMouse = field.Look.activeControl?.device is UnityEngine.InputSystem.Mouse;
                var scale = isMouse ? _movement.MouseSensitivity : _movement.StickSensitivity * Time.deltaTime;
                _yaw += look.x * scale;
                _pitch = Mathf.Clamp(_pitch - look.y * scale, _camera.FirstPersonPitchMin, _camera.FirstPersonPitchMax);
            }

            // Every client: the walk runs on the ground this body covered (D-022); the owner's arms walk with it.
            var step = transform.position - _lastPosition;
            step.y = 0f;
            _walked += step.magnitude;
            _lastPosition = transform.position;
            _walk.Advance(_walked, Time.deltaTime);
            if (_arms != null)
            {
                _arms.SetFloat(PhaseId, (float)(_walked / _crowd.StrideLength % 1d));
                _arms.SetBool(WalkingId, _walk.IsWalking);
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner || _controls == null)
            {
                return;
            }

            var dt = Time.fixedDeltaTime;
            var speed = _sprint ? _movement.PatrolRunSpeed : _movement.PatrolWalkSpeed;
            var desired = GroundMotion.DesiredVelocity(_move.x, _move.y, _yaw, speed);
            _velocity = GroundMotion.StepToward(_velocity, desired, _movement.Acceleration * dt);

            _fall = _body.isGrounded ? 0f : _fall + Physics.gravity.y * dt;
            _body.Move(new Vector3(_velocity.X, _fall, _velocity.Z) * dt);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f); // first person: the body faces where you look
        }

        private void LateUpdate()
        {
            if (!IsOwner || _eye == null)
            {
                return;
            }

            _eye.transform.SetPositionAndRotation(
                transform.position + Vector3.up * _camera.FirstPersonEyeHeight,
                Quaternion.Euler(_pitch, _yaw, 0f));
            _eye.fieldOfView = Camera.HorizontalToVerticalFieldOfView(_camera.FirstPersonFieldOfView, _eye.aspect);
            AimTarget = AimProbe.Find(_eye.transform.position, _eye.transform.forward, _camera.AimRange, gameObject);
        }

        private void TakeTheCamera()
        {
            _eye = Camera.main;
            if (_eye == null)
            {
                return;
            }

            // A third-person brain left on the camera would fight the first-person pose.
            if (_eye.TryGetComponent<CinemachineBrain>(out var brain))
            {
                brain.enabled = false;
            }

            _eye.nearClipPlane = _camera.FirstPersonNearClip;

            // The owner sees arms, not their own body; the body still casts its shadow.
            foreach (var part in GetComponentsInChildren<Renderer>())
            {
                part.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }

            _arms = Instantiate(_fpArmsPrefab, _eye.transform, false);
            SeatArmsAtTheEye(_arms.transform);
        }

        /// <summary>Moves the arms so the eye they were framed from is the camera.</summary>
        public static void SeatArmsAtTheEye(Transform arms)
        {
            foreach (var bone in arms.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == ArmsEyeBone)
                {
                    arms.localPosition = -arms.InverseTransformPoint(bone.position);
                    return;
                }
            }

            Debug.LogWarning($"[Patrol] {arms.name} has no '{ArmsEyeBone}' bone; the arms sit at the camera's origin.");
        }
    }
}
