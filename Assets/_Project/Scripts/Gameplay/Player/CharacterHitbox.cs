using UnityEngine;

namespace LastSeenWearing.Gameplay.Player
{
    /// <summary>
    /// A character's body for aiming (P1.12): a trigger capsule on the <see cref="Layer"/> layer, on every crowd
    /// NPC and every field body. <see cref="Character"/> is the object a stop or arrest acts on (P1.24).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class CharacterHitbox : MonoBehaviour
    {
        public const string Layer = "Character";

        [SerializeField] private GameObject _character;

        public GameObject Character => _character != null ? _character : transform.root.gameObject;
    }
}
