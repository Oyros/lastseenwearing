using LastSeenWearing.Core.Composite;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Movement;
using LastSeenWearing.Core.Wardrobe;
using LastSeenWearing.Gameplay.Crowd;
using LastSeenWearing.Gameplay.Interaction;
using LastSeenWearing.Gameplay.Round;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Player
{
    /// <summary>
    /// The fugitive (GDD §03, P1.11): a crowd body that walks like the crowd — the crowd's speed × its own
    /// pace, the crowd's animator through the same <see cref="WalkCycle"/>, and a walk of its own that no NPC
    /// shares. Sprint is faster, and the crowd will notice (GDD §04.3). The owner steers relative to an
    /// over-the-shoulder camera; the owner-authority <c>NetworkTransform</c> carries the body, and every client
    /// animates it from the distance it covers. Who the fugitive is — body and walk — is the case's
    /// (<see cref="CompositeBuilder.SuspectFor"/>, P1.19), the same every round; the clothes are the round's.
    /// Both come from the seeds the server hands in at spawn — never sent as a walk or an outfit.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FugitiveController : NetworkBehaviour
    {
        // The fugitive's slot among the characters outside the crowd (OutfitPlanner.CharacterOutfit).
        private const int OutfitSlot = 0;

        // The local fugitive's root carries this tag so its own collider never blocks its camera.
        private const string SelfTag = "Player";

        [SerializeField] private MovementConfig _movement;
        [SerializeField] private CameraConfig _camera;
        [SerializeField] private CrowdConfig _crowd;
        [SerializeField] private WardrobeConfig _wardrobeOdds;
        [SerializeField] private Transform _cameraPivot;

        // x: the case seed (who the fugitive is), y: the round's crowd seed (what they wear). One variable, so a
        // client never dresses the case's body in another round's clothes.
        private readonly NetworkVariable<Vector2Int> _seeds = new();

        // A tent change (P1.21): the clothes the fugitive came out in, and until when they are inside. Cleared with
        // every new round's seeds.
        private readonly NetworkVariable<Change> _change = new();

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
        private bool _hidden;
        private TentStage _tentStage;
        private float _heldUntil;

        public GaitSignature Gait { get; private set; }

        /// <summary>The outfit the fugitive starts the round in (P1.18); tents change it later (P1.21).</summary>
        public Outfit Outfit { get; private set; }

        /// <summary>At or inside a tent, changing (P1.21, P1.25): not moving, and no job, no escape, no arrest.</summary>
        public bool IsChanging => IsSpawned && NetworkManager.ServerTime.Time < _change.Value.InsideUntil;

        // The change on every client's clock (P1.25): Tent_Enter at the door, unseen inside — when the clothes swap —
        // then Tent_Exit in the new ones.
        private enum TentStage
        {
            Out,
            Entering,
            Inside,
            Exiting,
        }

        private TentStage StageAt(double now)
        {
            var change = _change.Value;
            if (!change.Changed || now >= change.InsideUntil || now < change.From)
            {
                return TentStage.Out;
            }

            var enter = _walk.ActionLength(BodyAction.TentEnter);
            var exit = _walk.ActionLength(BodyAction.TentExit);
            return now < change.From + enter ? TentStage.Entering
                : now < change.InsideUntil - exit ? TentStage.Inside
                : TentStage.Exiting;
        }

        /// <summary>Server: a one-shot every client sees this body play (P1.25) — a target job, being cuffed.</summary>
        public void Act(BodyAction action) => ActRpc(action);

        [Rpc(SendTo.Everyone)]
        private void ActRpc(BodyAction action)
        {
            _walk.PlayAction(action);
            if (action == BodyAction.ArrestSuspect)
            {
                _heldUntil = Time.time + _walk.ActionLength(action); // cuffed: no walking off mid-clip
            }
        }

        /// <summary>The local fugitive's tent panel is open: the body stands still and the camera holds.</summary>
        public bool Paused { get; set; }

        /// <summary>Server: tents this fugitive changed in this round (for the result, P1.26).</summary>
        public int TentsUsed { get; private set; }

        /// <summary>Server, right after spawning: the case, and the crowd this fugitive hides in this round.</summary>
        public void SetSeeds(int caseSeed, int crowdSeed)
        {
            _change.Value = default;
            TentsUsed = 0;
            _seeds.Value = new Vector2Int(caseSeed, crowdSeed);
        }

        /// <summary>Server, from a tent: in for <paramref name="seconds"/>, out in <paramref name="outfit"/>'s clothes.</summary>
        public void ChangeInto(Outfit outfit, float seconds)
        {
            TentsUsed++;
            _change.Value = new Change
            {
                Changed = true,
                Before = CompositeSync.Clothes.Of(Outfit),
                Clothes = CompositeSync.Clothes.Of(outfit),
                From = NetworkManager.ServerTime.Time,
                InsideUntil = NetworkManager.ServerTime.Time + seconds,
            };
        }

        public override void OnNetworkSpawn()
        {
            _body = GetComponent<CharacterController>();
            _walk = new WalkCycle(GetComponentInChildren<Animator>(), _crowd.StrideLength, _crowd.RunStrideLength);
            _lastPosition = transform.position;
            _seeds.OnValueChanged += OnSeedsChanged;
            _change.OnValueChanged += OnChangeChanged;
            ApplyGait(_seeds.Value);

            if (IsOwner)
            {
                // A client's body is created at the origin and then moved to its spawn point; the controller still
                // holds the origin and its first Move would snap back there. Re-enabling it takes the real position.
                _body.enabled = false;
                _body.enabled = true;
                _controls = new LastSeenWearingControls();
                _controls.Field.Enable();
                _yaw = transform.eulerAngles.y;
                gameObject.tag = SelfTag;
                CreateCamera();
            }
            else
            {
                _body.enabled = false; // remote bodies follow the network transform, not physics
            }
        }

        public override void OnNetworkDespawn()
        {
            _seeds.OnValueChanged -= OnSeedsChanged;
            _change.OnValueChanged -= OnChangeChanged;
            _controls?.Dispose();
            _controls = null;
            if (_view != null)
            {
                Destroy(_view.gameObject);
            }
        }

        private void OnSeedsChanged(Vector2Int previous, Vector2Int current) => ApplyGait(current);

        private void OnChangeChanged(Change previous, Change current) => ApplyGait(_seeds.Value);

        private void ApplyGait(Vector2Int seeds)
        {
            var view = GetComponent<OutfitView>();
            var suspect = CompositeBuilder.SuspectFor(seeds.x, view.Catalog, _wardrobeOdds, _crowd.Gait);
            Gait = suspect.Walk; // every round's crowd reserves it (CrowdSpawner)
            _walk.Apply(Gait);
            _walkSpeed = _crowd.WalkSpeed * _crowd.TempoMultiplier(Gait.Tempo);
            _walk.SetRunSpeeds(_walkSpeed, _movement.FugitiveRunSpeed); // the sprint breaks into the Run clip (PL.11b)

            // The case's body in clothes drawn after the round's crowd, unlike any NPC (GDD §05).
            Outfit = OutfitPlanner.CharacterOutfit(seeds.y, _crowd.NpcCount, OutfitSlot, view.Catalog, _wardrobeOdds, suspect.Body);
            var change = _change.Value;
            if (change.Changed)
            {
                // Until the fugitive is out of sight the old clothes are what everyone sees.
                var stage = StageAt(NetworkManager.ServerTime.Time);
                var clothes = stage == TentStage.Entering ? change.Before : change.Clothes;
                Outfit = Outfit.WithClothesOf(new Outfit(Outfit.Sex, Outfit.Height, Outfit.Build, Outfit.Skin, Outfit.Hair, Outfit.HairColour,
                    clothes.Top.ToWorn(), clothes.Bottom.ToWorn(), clothes.Hat.ToWorn()));
            }

            view.Apply(Outfit);
            _walk.SetScale(view.Scale);
            if (IsOwner)
            {
                Debug.Log($"[Fugitive] Your walk: {Gait.Describe()}. You wear: {Outfit.Describe(view.Catalog)}.");
            }
        }

        // Latch input in Update, use it in FixedUpdate (CONVENTIONS.md §6).
        private void Update()
        {
            StepTent();
            if (IsOwner && _controls != null && !Paused)
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
            if (Paused || IsChanging || Time.time < _heldUntil)
            {
                _move = Vector2.zero;
                _interactQueued = false;
            }

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

        // Every client walks the change's stages on the server clock: into the tent, out of sight (and into the new
        // clothes), out of the tent.
        private void StepTent()
        {
            var stage = IsSpawned ? StageAt(NetworkManager.ServerTime.Time) : TentStage.Out;
            if (stage == _tentStage)
            {
                return;
            }

            var from = _tentStage;
            _tentStage = stage;
            switch (stage)
            {
                case TentStage.Entering:
                    _walk.PlayAction(BodyAction.TentEnter);
                    break;
                case TentStage.Inside:
                    ApplyGait(_seeds.Value); // the swap, unseen
                    break;
                case TentStage.Exiting:
                    if (from == TentStage.Entering)
                    {
                        ApplyGait(_seeds.Value);
                    }

                    _walk.PlayAction(BodyAction.TentExit);
                    break;
            }

            Hide(stage == TentStage.Inside);
        }

        // Inside the tent nobody sees the fugitive or can aim at them; the body waits at the door.
        private void Hide(bool inside)
        {
            if (inside == _hidden)
            {
                return;
            }

            _hidden = inside;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.forceRenderingOff = inside;
            }

            foreach (var hitbox in GetComponentsInChildren<CharacterHitbox>(true))
            {
                hitbox.GetComponent<Collider>().enabled = !inside;
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
            // Layout A's stalls and walls (P1.17): pull the camera in rather than clip through them. Characters
            // are triggers on their own layer and never block it; the body's own controller is ignored by tag,
            // or the camera would pull into the fugitive's head.
            var obstacles = follow.AvoidObstacles;
            obstacles.Enabled = true;
            obstacles.CollisionFilter = LayerMask.GetMask("Default");
            obstacles.IgnoreTag = SelfTag;
            obstacles.CameraRadius = _camera.ThirdPersonCollisionRadius;
            follow.AvoidObstacles = obstacles;
        }

        private struct Change : INetworkSerializable
        {
            public bool Changed;
            public CompositeSync.Clothes Before;
            public CompositeSync.Clothes Clothes;
            public double From;
            public double InsideUntil;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Changed);
                serializer.SerializeValue(ref Before);
                serializer.SerializeValue(ref Clothes);
                serializer.SerializeValue(ref From);
                serializer.SerializeValue(ref InsideUntil);
            }
        }
    }
}
