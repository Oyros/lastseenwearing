using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Movement;
using LastSeenWearing.Gameplay.Crowd;
using LastSeenWearing.Gameplay.Interaction;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Player
{
    /// <summary>
    /// The fugitive (GDD §03, P1.11): a crowd body that walks like the crowd — the crowd's speed × its own
    /// pace, the crowd's animator through the same <see cref="WalkCycle"/>, and a walk of its own that no NPC
    /// shares (<see cref="GaitPlanner.CharacterSignature"/>). Sprint is faster, and the crowd will notice
    /// (GDD §04.3). The owner steers relative to an over-the-shoulder camera; the owner-authority
    /// <c>NetworkTransform</c> carries the body, and every client animates it from the distance it covers.
    /// The walk is f(crowd seed), handed in by the server at spawn — never sent as a walk.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FugitiveController : NetworkBehaviour
    {
        // The fugitive's slot among the characters outside the crowd (GaitPlanner.CharacterSignature).
        private const int GaitSlot = 0;

        [SerializeField] private MovementConfig _movement;
        [SerializeField] private CameraConfig _camera;
        [SerializeField] private CrowdConfig _crowd;
        [SerializeField] private Transform _cameraPivot;

        private readonly NetworkVariable<int> _crowdSeed = new();

        private CharacterController _body;
        private WalkCycle _walk;
        private float _walkSpeed;
        private double _walked;
        private Vector3 _lastPosition;

        // Owner only.
        private LastSeenWearingControls _controls;
        private CinemachineCamera _view;
        private Vector2 _move;
        private bool _sprint;
        private bool _interactQueued;
        private float _yaw;
        private float _pitch;
        private GroundPoint _velocity;
        private float _fall;

        public GaitSignature Gait { get; private set; }

        /// <summary>Server, right after spawning: the crowd this fugitive hides in this round.</summary>
        public void SetCrowdSeed(int seed)
        {
            _crowdSeed.Value = seed;
        }

        public override void OnNetworkSpawn()
        {
            _body = GetComponent<CharacterController>();
            _walk = new WalkCycle(GetComponentInChildren<Animator>(), _crowd.StrideLength);
            _lastPosition = transform.position;
            _crowdSeed.OnValueChanged += OnCrowdSeedChanged;
            ApplyGait(_crowdSeed.Value);

            if (IsOwner)
            {
                // A client's body is created at the origin and then moved to its spawn point; the controller still
                // holds the origin and its first Move would snap back there. Re-enabling it takes the real position.
                _body.enabled = false;
                _body.enabled = true;
                _controls = new LastSeenWearingControls();
                _controls.Field.Enable();
                _yaw = transform.eulerAngles.y;
                CreateCamera();
            }
            else
            {
                _body.enabled = false; // remote bodies follow the network transform, not physics
            }
        }

        public override void OnNetworkDespawn()
        {
            _crowdSeed.OnValueChanged -= OnCrowdSeedChanged;
            _controls?.Dispose();
            _controls = null;
            if (_view != null)
            {
                Destroy(_view.gameObject);
            }
        }

        private void OnCrowdSeedChanged(int previous, int current) => ApplyGait(current);

        private void ApplyGait(int crowdSeed)
        {
            Gait = GaitPlanner.CharacterSignature(crowdSeed, _crowd.NpcCount, GaitSlot, _crowd.Gait);
            _walk.Apply(Gait);
            _walkSpeed = _crowd.WalkSpeed * _crowd.TempoMultiplier(Gait.Tempo);
            if (IsOwner)
            {
                Debug.Log($"[Fugitive] Your walk: {Gait.Describe()}.");
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
                _interactQueued |= field.Interact.WasPressedThisFrame();

                var look = field.Look.ReadValue<Vector2>();
                var isMouse = field.Look.activeControl?.device is UnityEngine.InputSystem.Mouse;
                var scale = isMouse ? _movement.MouseSensitivity : _movement.StickSensitivity * Time.deltaTime;
                _yaw += look.x * scale;
                _pitch = Mathf.Clamp(_pitch - look.y * scale, _camera.ThirdPersonPitchMin, _camera.ThirdPersonPitchMax);
            }

            // Every client: the walk runs on the ground this body actually covered (D-022).
            var step = transform.position - _lastPosition;
            step.y = 0f;
            _walked += step.magnitude;
            _lastPosition = transform.position;
            _walk.Advance(_walked, Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!IsOwner || _controls == null)
            {
                return;
            }

            var dt = Time.fixedDeltaTime;
            var speed = _sprint ? _movement.FugitiveRunSpeed : _walkSpeed;
            var desired = GroundMotion.DesiredVelocity(_move.x, _move.y, _yaw, speed);
            _velocity = GroundMotion.StepToward(_velocity, desired, _movement.Acceleration * dt);

            _fall = _body.isGrounded ? 0f : _fall + Physics.gravity.y * dt;
            _body.Move(new Vector3(_velocity.X, _fall, _velocity.Z) * dt);

            if (_velocity.X * _velocity.X + _velocity.Z * _velocity.Z > 0.01f)
            {
                var facing = Quaternion.LookRotation(new Vector3(_velocity.X, 0f, _velocity.Z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, _movement.TurnSpeed * dt);
            }

            if (_interactQueued)
            {
                _interactQueued = false;
                TryInteract();
            }
        }

        private void LateUpdate()
        {
            if (IsOwner && _cameraPivot != null)
            {
                _cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
        }

        private void TryInteract()
        {
            var target = Interactor.NearestAround(transform.position, _movement.InteractRange, OwnerClientId);
            if (target is Component component && component.TryGetComponent<NetworkObject>(out var networkObject))
            {
                InteractRpc(networkObject);
            }
        }

        [Rpc(SendTo.Server)]
        private void InteractRpc(NetworkObjectReference target, RpcParams rpcParams = default)
        {
            // The server checks what the client claimed: the thing exists, is in range, and lets them in.
            if (!target.TryGet(out var networkObject) || !networkObject.TryGetComponent<IInteractable>(out var interactable))
            {
                return;
            }

            var sender = rpcParams.Receive.SenderClientId;
            var inRange = (interactable.InteractionPoint - transform.position).sqrMagnitude <= _movement.InteractRange * _movement.InteractRange;
            if (sender == OwnerClientId && inRange && interactable.CanInteract(sender))
            {
                interactable.Interact(sender);
            }
        }

        private void CreateCamera()
        {
            var main = Camera.main;
            if (main == null)
            {
                return;
            }

            if (!main.TryGetComponent<CinemachineBrain>(out _))
            {
                main.gameObject.AddComponent<CinemachineBrain>();
            }

            _cameraPivot.localPosition = new Vector3(0f, _camera.ThirdPersonPivotHeight, 0f);
            _view = new GameObject("FugitiveCamera").AddComponent<CinemachineCamera>();
            _view.Follow = _cameraPivot;
            _view.Lens.FieldOfView = _camera.ThirdPersonFieldOfView;
            var follow = _view.gameObject.AddComponent<CinemachineThirdPersonFollow>();
            follow.CameraDistance = _camera.ThirdPersonDistance;
            follow.ShoulderOffset = new Vector3(_camera.ThirdPersonShoulder, 0f, 0f);
            follow.VerticalArmLength = 0f;
            follow.Damping = Vector3.one * _camera.ThirdPersonDamping;
        }
    }
}
