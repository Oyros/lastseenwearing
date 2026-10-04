using LastSeenWearing.Core.Layouts;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Core.Round;
using LastSeenWearing.Gameplay.Interaction;
using LastSeenWearing.Gameplay.Roles;
using LastSeenWearing.Gameplay.Round;
using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Objective
{
    /// <summary>
    /// One of the layout's five targets (GDD §04.4, P1.22), standing on the walkable ground beside its prop. Everyone
    /// can see where it is; only the fugitive can work it, and <see cref="Objectives"/> keeps the score.
    /// </summary>
    public sealed class TargetSpot : NetworkBehaviour, IInteractable
    {
        [Tooltip("Which of the layout's targets this is.")]
        [SerializeField] private int _index;
        [SerializeField] private TargetKind _kind;
        [SerializeField] private Objectives _objectives;
        [SerializeField] private RoleRosterSync _roster;
        [SerializeField] private RoundDirector _director;

        public int Index => _index;
        public TargetKind Kind => _kind;
        public Vector3 InteractionPoint => transform.position;

        // On the fugitive's client the done list is theirs (Objectives sends it); elsewhere nobody asks.
        public bool CanInteract(ulong clientId) =>
            _roster.RoleOf(clientId) == Role.Fugitive && _director.Phase is RoundPhase.Live or RoundPhase.LastCuff
            && !_objectives.IsDone(_index) && _objectives.Job < 0 && _objectives.DoneCount < _objectives.Needed;

        public void Interact(ulong clientId) => _objectives.Begin(clientId, this);
    }
}
