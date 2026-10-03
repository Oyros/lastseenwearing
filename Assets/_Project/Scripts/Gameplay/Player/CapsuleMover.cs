using LastSeenWearing.Core.Config;
using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Player
{
    /// <summary>
    /// The P0.10 stand-in for a field character: the owner walks it on the ground plane with the
    /// <c>Field</c> map's Move; an owner-authority <c>NetworkTransform</c> carries it to everyone else.
    /// </summary>
    public sealed class CapsuleMover : NetworkBehaviour
    {
        [SerializeField] private MovementConfig _config;

        private LastSeenWearingControls _controls;
        private Vector2 _move;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _controls = new LastSeenWearingControls();
            _controls.Field.Enable();
        }

        public override void OnNetworkDespawn()
        {
            _controls?.Dispose();
            _controls = null;
        }

        // Latch in Update, consume in FixedUpdate (CONVENTIONS.md §6). Move is a held value, so the
        // latest reading is the latch.
        private void Update()
        {
            if (_controls != null)
            {
                _move = _controls.Field.Move.ReadValue<Vector2>();
            }
        }

        private void FixedUpdate()
        {
            if (_controls == null)
            {
                return;
            }

            var step = new Vector3(_move.x, 0f, _move.y) * (_config.WalkSpeed * Time.fixedDeltaTime);
            transform.position += step;
        }
    }
}
