using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Movement;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Core.Wardrobe;
using LastSeenWearing.Gameplay.Capture;
using LastSeenWearing.Gameplay.Crowd;
using LastSeenWearing.Gameplay.Interaction;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSeenWearing.Gameplay.Player
{
    /// <summary>
    /// A field officer in first person (GDD §03): the patrol (P1.12) — fast, in uniform, holding the cuffs — or the
    /// plainclothes (P2.01) — at the crowd's pace, in clothes drawn from the round's seed like a stranger's, never
    /// arresting. The owner looks with mouse or stick, walks and sprints relative to the view, sees first-person arms
    /// (PL.19) instead of their body, and aims — the character under the crosshair is <see cref="AimTarget"/>, the
    /// input of stop and arrest (P1.24). Everyone else sees the body walk on the crowd's <see cref="WalkCycle"/>; an
    /// owner-authority <c>NetworkTransform</c> carries it.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FieldOfficerController : NetworkBehaviour
    {
        private static readonly int PhaseId = Animator.StringToHash(WalkCycle.PhaseParameter);
        private static readonly int WalkingId = Animator.StringToHash(WalkCycle.WalkingParameter);

        // PL.19's arms rig carries a non-deforming bone at the eye the arms were framed from.
        private const string ArmsEyeBone = "Camera";

        // The arms' cuffing one-shot (PL.19): its state in the arms animator's Action layer is named after the clip.
        private const string ArmsCuffsState = "FP_Cuffs";

        [Tooltip("Patrol or Plainclothes: speed, cuffs, and whether the body is dressed from the seed.")]
        [SerializeField] private Role _role = Role.Patrol;
        [SerializeField] private MovementConfig _movement;
        [Tooltip("Plainclothes only: the odds the stranger's clothes are drawn with.")]
        [SerializeField] private WardrobeConfig _wardrobeOdds;
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

        // The plainclothes' slot among the characters outside the crowd (the fugitive is 0; OutfitPlanner.CharacterOutfit).
        private const int PlainclothesOutfitSlot = 1;

        // Plainclothes: the round's crowd seed, which their clothes are drawn from.
        private readonly NetworkVariable<int> _crowdSeed = new();

        public Role Role => _role;

        /// <summary>Only the patrol holds the cuffs (GDD §03).</summary>
        public bool CanArrest => _role == Role.Patrol;

        /// <summary>Server, right after spawning: the crowd this officer walks in, for a plainclothes' clothes.</summary>
        public void SetCrowdSeed(int crowdSeed) => _crowdSeed.Value = crowdSeed;

        /// <summary>The field officer this client plays, or none.</summary>
        public static FieldOfficerController Local { get; private set; }

        /// <summary>Owner only: the character under the crosshair, or none.</summary>
        public CharacterHitbox AimTarget { get; private set; }

        /// <summary>Owner only: how far the Arrest hold has got, 0–1 (P1.24); 0 when not held.</summary>
        public float ArrestHold { get; private set; }

        /// <summary>Owner only: the one under the crosshair is close enough to cuff.</summary>
        public bool InArrestRange { get; private set; }

        private Arrests _arrests;
        private float _heldUntil;

        public override void OnNetworkSpawn()
        {
            _body = GetComponent<CharacterController>();
            _walk = new WalkCycle(GetComponentInChildren<Animator>(), _crowd.StrideLength, _crowd.RunStrideLength);
            _walk.SetBase(BaseWalk.Normal); // a uniform walks plainly; the patrol is not hiding
            _walk.SetRunSpeeds(WalkSpeed, RunSpeed);
            _crowdSeed.OnValueChanged += OnCrowdSeedChanged;
            Dress(_crowdSeed.Value);
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
            _arrests = FindFirstObjectByType<Arrests>();
            Local = this;
            TakeTheCamera();
        }

        private float WalkSpeed => _role == Role.Plainclothes ? _movement.PlainclothesWalkSpeed : _movement.PatrolWalkSpeed;
        private float RunSpeed => _role == Role.Plainclothes ? _movement.PlainclothesRunSpeed : _movement.PatrolRunSpeed;

        private void OnCrowdSeedChanged(int previous, int current) => Dress(current);

        // A plainclothes wears clothes drawn after the round's crowd, unlike any NPC (GDD §03: they pass for one of
        // it). The patrol's uniform is its model; it has no wardrobe.
        private void Dress(int crowdSeed)
        {
            if (_role != Role.Plainclothes || crowdSeed == 0 || !TryGetComponent<OutfitView>(out var view))
            {
                return;
            }

            var outfit = OutfitPlanner.CharacterOutfit(crowdSeed, _crowd.NpcCount, PlainclothesOutfitSlot, view.Catalog, _wardrobeOdds);
            view.Apply(outfit);
            _walk.SetScale(view.Scale);
        }

        public override void OnNetworkDespawn()
        {
            _crowdSeed.OnValueChanged -= OnCrowdSeedChanged;
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
                ReadArrest(field);
                if (field.Interact.WasPressedThisFrame())
                {
                    TryInteract();
                }
            }

            // Every client: the walk runs on the ground this body covered (D-022); the owner's arms walk with it.
            var step = transform.position - _lastPosition;
            step.y = 0f;
            _walked += step.magnitude;
            _lastPosition = transform.position;
            _walk.Advance(_walked, Time.deltaTime);
            if (_arms != null)
            {
                _arms.SetFloat(PhaseId, _walk.Phase);
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
            var speed = _sprint ? RunSpeed : WalkSpeed;
            var move = Time.time < _heldUntil ? Vector2.zero : _move;
            var desired = GroundMotion.DesiredVelocity(move.x, move.y, _yaw, speed);
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

        /// <summary>Server: a one-shot every client sees this patrol play (P1.25) — cuffing someone.</summary>
        public void Act(BodyAction action) => ActRpc(action);

        [Rpc(SendTo.Everyone)]
        private void ActRpc(BodyAction action)
        {
            _walk.PlayAction(action);
            if (IsOwner && action == BodyAction.ArrestOfficer)
            {
                _heldUntil = Time.time + _walk.ActionLength(action); // the officer stands for the cuffing
                if (_arms != null)
                {
                    _arms.CrossFadeInFixedTime(ArmsCuffsState, WalkCycle.ActionBlendSeconds, _arms.GetLayerIndex(WalkCycle.ActionLayer), 0f);
                }
            }
        }

        // Interact (DATA §7): a plainclothes at a tent goes in to look (P2.02). The server decides what it shows.
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

        // Hold Arrest on someone close (DATA §7: F, 0.5 s). The server decides who it was and what it cost.
        private void ReadArrest(LastSeenWearingControls.FieldActions field)
        {
            var arrest = field.Arrest;
            if (!CanArrest)
            {
                ArrestHold = 0f;
                InArrestRange = false;
                return;
            }

            ArrestHold = arrest.IsInProgress() ? arrest.GetTimeoutCompletionPercentage() : 0f;
            var range = _arrests != null ? _arrests.Config.ArrestRange : 0f;
            InArrestRange = AimTarget != null && Vector3.Distance(AimTarget.Character.transform.position, transform.position) <= range;
            if (arrest.WasPerformedThisFrame() && InArrestRange)
            {
                _arrests.Request(AimTarget);
            }
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
