using System;
using UnityEngine;

namespace LastSeenWearing.Core.Layouts
{
    /// <summary>The four target jobs (GDD §04.4): where a fugitive's risk comes from.</summary>
    public enum TargetKind
    {
        Open,
        Fixed,
        Social,
        Hidden,
    }

    /// <summary>
    /// One festival layout (docs/LAYOUTS.md, docs/DATA.md §2): its CCTV cameras, target spots, changing tents,
    /// exits, crowd areas and bounds — instance data generated from the art's layout JSON
    /// (<c>Editor/Import/LayoutImporter</c>), never typed by hand. Unity axes, metres, yaw in degrees about +Y.
    /// </summary>
    [CreateAssetMenu(fileName = "Layout", menuName = "Last Seen Wearing/Layouts/Layout Definition")]
    public sealed class LayoutDefinition : ScriptableObject
    {
        [Serializable]
        public struct CameraSpot
        {
            public string Name;
            public Vector3 Position;
            public Vector3 Forward;
            [Tooltip("Vertical field of view at 16:9, degrees.")]
            public float VerticalFieldOfView;
        }

        [Serializable]
        public struct TargetSpot
        {
            public TargetKind Kind;
            public string Tag;
            public Vector3 Position;
            public float Yaw;
        }

        [Serializable]
        public struct Spot
        {
            public string Name;
            public Vector3 Position;
            public float Yaw;
        }

        [Serializable]
        public struct Area
        {
            public float X0;
            public float Z0;
            public float X1;
            public float Z1;

            public bool Contains(Vector3 point) => point.x >= X0 && point.x <= X1 && point.z >= Z0 && point.z <= Z1;
        }

        [SerializeField] private string _id;
        [SerializeField] private string _title;
        [SerializeField] private CameraSpot[] _cameras = Array.Empty<CameraSpot>();
        [SerializeField] private TargetSpot[] _targets = Array.Empty<TargetSpot>();
        [SerializeField] private Spot[] _tents = Array.Empty<Spot>();
        [SerializeField] private Spot[] _exits = Array.Empty<Spot>();
        [Tooltip("Where the crowd walks.")]
        [SerializeField] private Area[] _crowdAreas = Array.Empty<Area>();
        [SerializeField] private Area _bounds;

        public string Id => _id;
        public string Title => _title;
        public CameraSpot[] Cameras => _cameras;
        public TargetSpot[] Targets => _targets;
        public Spot[] Tents => _tents;
        public Spot[] Exits => _exits;
        public Area[] CrowdAreas => _crowdAreas;
        public Area Bounds => _bounds;

        /// <summary>Editor import only: replaces everything from the art's JSON.</summary>
        public void Set(string id, string title, CameraSpot[] cameras, TargetSpot[] targets, Spot[] tents, Spot[] exits,
            Area[] crowdAreas, Area bounds)
        {
            _id = id;
            _title = title;
            _cameras = cameras;
            _targets = targets;
            _tents = tents;
            _exits = exits;
            _crowdAreas = crowdAreas;
            _bounds = bounds;
        }
    }
}
