using UnityEngine;

namespace LastSeenWearing.Gameplay.Interaction
{
    /// <summary>
    /// Something a field character can use with Interact (DATA §7): a tent, a target, a witness. Lives on a
    /// network object with a collider. The server decides: <see cref="Interact"/> runs there only.
    /// </summary>
    public interface IInteractable
    {
        Vector3 InteractionPoint { get; }

        bool CanInteract(ulong clientId);

        /// <summary>Server only.</summary>
        void Interact(ulong clientId);
    }
}
