using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// How field characters move (docs/DATA.md §3, GDD §03). P0.10 needs only one walk speed for
    /// the sandbox capsule; per-role speeds (patrol fast, dog slow), run and look arrive in P1.
    /// </summary>
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "Last Seen Wearing/Config/Movement")]
    public sealed class MovementConfig : ScriptableObject
    {
        [Header("Walk")]
        [Tooltip("Metres per second. [PROVISIONAL] — a festival-goer's stroll.")]
        [SerializeField, Min(0f)] private float _walkSpeed = 1.4f;

        public float WalkSpeed => _walkSpeed;
    }
}
