using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// Changing in tents (docs/DATA.md §3, GDD §05, P1.21): what a tent stocks each round, how often it can be
    /// used and how long a change takes. The mask-stall cluster radius joins with masks.
    /// </summary>
    [CreateAssetMenu(fileName = "DisguiseConfig", menuName = "Last Seen Wearing/Config/Disguise")]
    public sealed class DisguiseConfig : ScriptableObject
    {
        [Header("Tents (GDD §05)")]
        [Tooltip("Uses per tent per round. GDD §05: one.")]
        [SerializeField, Min(1)] private int _usesPerTent = 1;
        [Tooltip("Garments of each kind a tent stocks per round, copied from what the round's crowd wears. [PROVISIONAL]")]
        [SerializeField, Range(0, 6)] private int _stockTops = 2;
        [SerializeField, Range(0, 6)] private int _stockBottoms = 2;
        [SerializeField, Range(0, 6)] private int _stockHats = 2;
        [Tooltip("Seconds the fugitive spends inside, unseen, while changing. [PROVISIONAL]")]
        [SerializeField, Min(0f)] private float _changeSeconds = 4f;

        public int UsesPerTent => _usesPerTent;
        public int StockTops => _stockTops;
        public int StockBottoms => _stockBottoms;
        public int StockHats => _stockHats;
        public float ChangeSeconds => _changeSeconds;
    }
}
