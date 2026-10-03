using System.Collections.Generic;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Interaction
{
    /// <summary>Which interactable a character standing somewhere would use: the nearest one in range.</summary>
    public static class Interactor
    {
        private static readonly Collider[] Hits = new Collider[32];

        /// <summary>The nearest of <paramref name="candidates"/> within <paramref name="range"/> of <paramref name="from"/> that lets <paramref name="clientId"/> in.</summary>
        public static IInteractable Nearest(Vector3 from, IEnumerable<IInteractable> candidates, float range, ulong clientId)
        {
            IInteractable best = null;
            var bestDistance = range * range;
            foreach (var candidate in candidates)
            {
                var distance = (candidate.InteractionPoint - from).sqrMagnitude;
                if (distance <= bestDistance && candidate.CanInteract(clientId))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>The nearest interactable around <paramref name="from"/>, found through the colliders in range.</summary>
        public static IInteractable NearestAround(Vector3 from, float range, ulong clientId)
        {
            var count = Physics.OverlapSphereNonAlloc(from, range, Hits, ~0, QueryTriggerInteraction.Collide);
            var found = new List<IInteractable>(count);
            for (var i = 0; i < count; i++)
            {
                if (Hits[i].GetComponentInParent<IInteractable>() is { } interactable && !found.Contains(interactable))
                {
                    found.Add(interactable);
                }
            }

            return Nearest(from, found, range, clientId);
        }
    }
}
