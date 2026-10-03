using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Crowd
{
    /// <summary>
    /// A developer panel for the crowd: crowd time, how many NPCs are taken over, crowd bytes in the
    /// last logged minute, and a button that bumps the NPC nearest to your player — the stand-in for
    /// real contact until P1's capture work. Editor and development builds only; debug text is exempt
    /// from localization (docs/LOCALIZATION.md §1).
    /// </summary>
    public sealed class CrowdDebugPanel : MonoBehaviour
    {
        // Drift probe: every instance logs on the same wall-clock boundaries, so two logs line up.
        private const double ProbeIntervalSeconds = 30d;
        private const int ProbedNpcs = 5;

        [SerializeField] private CrowdSpawner _spawner;

        private long _lastProbeSlot = -1;

        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (_spawner == null || !_spawner.IsSpawned || _spawner.Agents == null)
            {
                return;
            }

            var wall = System.DateTime.UtcNow.Ticks / (double)System.TimeSpan.TicksPerSecond;
            var slot = (long)(wall / ProbeIntervalSeconds);
            if (slot == _lastProbeSlot)
            {
                return;
            }

            _lastProbeSlot = slot;
            var line = new System.Text.StringBuilder();
            line.Append($"[CrowdProbe] slot={slot} wall={wall % 1000d:F3} t={_spawner.CrowdTime:F3}");
            for (var i = 0; i < ProbedNpcs && i < _spawner.Agents.Length; i++)
            {
                var position = _spawner.Agents[i].transform.position;
                line.Append($" {i}:{position.x:F3},{position.z:F3}");
            }

            Debug.Log(line.ToString());
        }

        private void OnGUI()
        {
            if (_spawner == null || !_spawner.IsSpawned)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, Screen.height - 120, 260, 110), GUI.skin.box);
            GUILayout.Label($"Crowd seed {_spawner.Seed}, t = {_spawner.CrowdTime:F1} s");
            GUILayout.Label($"Taken over: {_spawner.TakenOverCount}. Last minute: {_spawner.BytesLastMinute} B");

            var player = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (player != null && GUILayout.Button("Bump nearest NPC"))
            {
                _spawner.BumpNearest(player.transform.position);
            }

            GUILayout.EndArea();
        }
    }
}
