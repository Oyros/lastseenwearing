using UnityEngine;

namespace LastSeenWearing.Gameplay.Player
{
    /// <summary>
    /// Who a field officer is looking at (P1.12): the first thing along the view ray within range. A wall in front
    /// hides whoever is behind it; the looker's own body never counts.
    /// </summary>
    public static class AimProbe
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        public static CharacterHitbox Find(Vector3 origin, Vector3 direction, float range, GameObject self)
        {
            var mask = LayerMask.GetMask("Default", CharacterHitbox.Layer);
            var count = Physics.RaycastNonAlloc(origin, direction, Hits, range, mask, QueryTriggerInteraction.Collide);
            System.Array.Sort(Hits, 0, count, HitDistance.Instance);
            for (var i = 0; i < count; i++)
            {
                var hit = Hits[i];
                if (self != null && hit.collider.transform.IsChildOf(self.transform))
                {
                    continue;
                }

                // The nearest thing that is not us decides: a character, or something in the way.
                return hit.collider.GetComponent<CharacterHitbox>();
            }

            return null;
        }

        private sealed class HitDistance : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly HitDistance Instance = new();

            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
