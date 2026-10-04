using LastSeenWearing.Gameplay.Disguise;
using TMPro;
using UnityEngine;

namespace LastSeenWearing.UI.Round
{
    /// <summary>
    /// The number painted over each tent's door (P2.02), so "tent 2" on the rails, in the plainclothes' view and on
    /// the radio is one place everyone can find — the field in person, the Watcher on a feed. P1-style placeholder.
    /// </summary>
    public sealed class TentNumbers : MonoBehaviour
    {
        // Placeholder look, not tuning: a sign above the door, read from the front.
        private const float Height = 2.6f;
        private const float FontSize = 6f;

        private bool _placed;

        private void Update()
        {
            if (_placed)
            {
                return;
            }

            var tents = FindObjectsByType<ChangingTent>(FindObjectsSortMode.None);
            if (tents.Length == 0)
            {
                return;
            }

            _placed = true;
            foreach (var tent in tents)
            {
                var sign = new GameObject($"TentNumber_{tent.Index + 1}", typeof(TextMeshPro));
                sign.transform.SetParent(transform, false);
                // A TextMeshPro reads from its back: facing the door's way out, it reads to someone walking up to it.
                sign.transform.SetPositionAndRotation(tent.InteractionPoint + Vector3.up * Height, Quaternion.LookRotation(-tent.transform.forward));
                var text = sign.GetComponent<TextMeshPro>();
                text.text = (tent.Index + 1).ToString();
                text.fontSize = FontSize;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                text.fontStyle = FontStyles.Bold;
            }
        }
    }
}
