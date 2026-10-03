using System.Collections.Generic;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Core.Randomness;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Crowd
{
    /// <summary>
    /// The walk readability test's crowd (docs/WALKTEST.md, P1.07) — offline, no network. Thirty crowd
    /// bodies walk a small plaza; six carry one trait each at the LOOKDEV gate weight, everyone else walks
    /// plain at one of the base walks. Uniqueness (D-025) is set aside on purpose: "find the one limping
    /// left" must have exactly one answer. Each trait carrier wears a coloured marker on the
    /// <see cref="KeyLayer"/> — seen only by the answer-key camera.
    /// Development scene only.
    /// </summary>
    public sealed class WalkTestCrowd : MonoBehaviour
    {
        public const string KeyLayer = "WalkTestKey";

        // The six asked-for traits, the WALKTEST.md key colours, and the strength each is shown at.
        private static readonly (WalkTrait Trait, float Strength, Color Key, string Label)[] Carriers =
        {
            (WalkTrait.Limp, -GateWeight, Color.red, "limps left"),
            (WalkTrait.Limp, GateWeight, new Color(1f, 0.55f, 0f), "limps right"),
            (WalkTrait.Hunch, GateWeight, Color.blue, "hunched"),
            (WalkTrait.Bounce, GateWeight, Color.green, "bouncy"),
            (WalkTrait.ArmSwing, -GateWeight, new Color(0.6f, 0.1f, 0.8f), "stiff arms"),
            (WalkTrait.Sway, GateWeight, Color.yellow, "sways"),
        };

        // docs/LOOKDEV.md §2: a gait trait must read at half weight.
        private const float GateWeight = 0.5f;
        private const float MarkerHeight = 2.15f;
        private const float MarkerSize = 0.3f;

        [SerializeField] private CrowdConfig _config;
        [SerializeField] private CrowdAgent _agentPrefab;
        [SerializeField] private Transform _waypointRoot;
        [SerializeField, Min(1)] private int _npcCount = 30;
        [SerializeField] private int _seed = 2026;

        private readonly List<CrowdAgent> _agents = new();
        private float _startTime;

        public IReadOnlyList<CrowdAgent> Agents => _agents;

        /// <summary>For each asked-for trait, the NPC index that carries it.</summary>
        public Dictionary<string, int> AnswerKey { get; } = new();

        private void Start()
        {
            var waypoints = new GroundPoint[_waypointRoot.childCount];
            for (var i = 0; i < waypoints.Length; i++)
            {
                var position = _waypointRoot.GetChild(i).position;
                waypoints[i] = new GroundPoint(position.x, position.z);
            }

            var plans = CrowdPlanner.Build(_seed, new CrowdPlanSettings(_npcCount, waypoints.Length, _config.RouteLength,
                _config.WaypointSpread, _config.DwellMin, _config.DwellMax));

            // Which six NPCs carry a trait: a seeded shuffle of the indices.
            var order = new List<int>();
            for (var i = 0; i < _npcCount; i++)
            {
                order.Add(i);
            }

            var random = new SeededRandom(_seed);
            for (var i = order.Count - 1; i > 0; i--)
            {
                var j = random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            var keyLayer = LayerMask.NameToLayer(KeyLayer);
            var groundY = transform.position.y;
            for (var i = 0; i < _npcCount; i++)
            {
                var schedule = new NpcSchedule(plans[i], waypoints, _config.WalkSpeed, (from, to) => new[] { from, to });
                var agent = Instantiate(_agentPrefab, transform);
                agent.name = $"Walker_{i:00}";
                agent.Begin(i, schedule, groundY, (BaseWalk)(i % 4), _config.StrideLength);
                _agents.Add(agent);
            }

            for (var c = 0; c < Carriers.Length && c < _npcCount; c++)
            {
                var index = order[c];
                var carrier = Carriers[c];
                _agents[index].SetTrait(carrier.Trait, carrier.Strength);
                AnswerKey[carrier.Label] = index;
                AddMarker(_agents[index].transform, carrier.Key, keyLayer);
            }

            Debug.Log($"[WalkTest] Seed {_seed}. Answer key: {string.Join(", ", KeyLines())}.");
            _startTime = Time.time;
        }

        /// <summary>Back to the start of the crowd's schedule, so every recorded take shows the same seconds.</summary>
        public void Restart()
        {
            _startTime = Time.time;
        }

        private void Update()
        {
            var time = Time.time - _startTime;
            foreach (var agent in _agents)
            {
                agent.FollowSchedule(time, Time.deltaTime);
            }
        }

        private IEnumerable<string> KeyLines()
        {
            foreach (var entry in AnswerKey)
            {
                yield return $"{entry.Key} = Walker_{entry.Value:00}";
            }
        }

        private static void AddMarker(Transform parent, Color colour, int layer)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(marker.GetComponent<Collider>());
            marker.name = "KeyMarker";
            marker.layer = layer;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(0f, MarkerHeight, 0f);
            marker.transform.localScale = Vector3.one * MarkerSize;
            var renderer = marker.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = colour };
        }
    }
}
