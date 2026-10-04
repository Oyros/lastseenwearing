using System.Collections.Generic;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Wardrobe;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Crowd
{
    /// <summary>
    /// Dresses a crowd body (GDD §05, P1.18, D-037): of the body FBX's every part and garment it shows the sex's
    /// body parts that the clothes leave bare, the face, the hair (not under a hood), the top, the bottom and the
    /// hat; colours them from the palette — one shared material per colour, so they batch; and sets the build's
    /// shape key. It wears LOD0 only: a LOD group culls drawing but a skinned mesh it hides is still skinned every
    /// frame — the art's <c>_LOD1</c>/<c>_LOD2</c> copies tripled the skinning and took 150 dressed NPCs from 33 to
    /// 9 fps (D-037). A crowd NPC never changes clothes, so it drops every part it does not wear
    /// (<see cref="_keepWardrobe"/> off); the fugitive keeps them for the tent (P1.21).
    /// </summary>
    public sealed class OutfitView : MonoBehaviour
    {
        private const string Lod1Suffix = "_LOD1";
        private const string Lod2Suffix = "_LOD2";
        private const string BuildSlim = "Build_Slim";
        private const string BuildHeavy = "Build_Heavy";
        private const string FaceDecalPrefix = "LSW_Crowd_FaceDecal_";
        private const float FullShapeKey = 100f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Dictionary<string, Material> Swatches = new();

        [SerializeField] private WardrobeCatalog _catalog;
        [Tooltip("Height scales (P1.19).")]
        [SerializeField] private WardrobeConfig _odds;
        [Tooltip("The face decal's material: the art's face atlas, alpha cut out (PL.32, D-010).")]
        [SerializeField] private Material _faceMaterial;
        [Tooltip("Keep every garment after dressing (the fugitive changes in tents); off, unworn parts are destroyed.")]
        [SerializeField] private bool _keepWardrobe;

        private readonly Dictionary<string, List<SkinnedMeshRenderer>> _parts = new();

        public Outfit Outfit { get; private set; }

        public WardrobeCatalog Catalog => _catalog;

        /// <summary>The body's scale for its height (P1.19): the walk's stride scales with it.</summary>
        public float Scale => _odds.ScaleOf(Outfit.Height);

        public void Apply(Outfit outfit)
        {
            Outfit = outfit;
            if (_parts.Count == 0)
            {
                Index();
            }

            // Height is the whole body, rig and all: the animator's root is scaled, the controller is not.
            GetComponentInChildren<Animator>().transform.localScale = Vector3.one * Scale;

            var dress = Dress(outfit);
            var dropped = new List<string>();
            foreach (var (part, renderers) in _parts)
            {
                var worn = dress.TryGetValue(part, out var swatch);
                foreach (var renderer in renderers)
                {
                    renderer.gameObject.SetActive(worn && IsLod0(renderer));
                    if (worn)
                    {
                        if (part.StartsWith(FaceDecalPrefix))
                        {
                            renderer.sharedMaterial = _faceMaterial;
                        }
                        else
                        {
                            Paint(renderer, swatch);
                        }

                        Shape(renderer, outfit.Build);
                    }
                }

                if (!_keepWardrobe)
                {
                    if (!worn)
                    {
                        dropped.Add(part);
                    }
                    else
                    {
                        renderers.RemoveAll(r =>
                        {
                            var copy = !IsLod0(r);
                            if (copy)
                            {
                                Drop(r);
                            }

                            return copy;
                        });
                    }
                }
            }

            foreach (var part in dropped)
            {
                foreach (var renderer in _parts[part])
                {
                    Drop(renderer);
                }

                _parts.Remove(part);
            }
        }

        private static bool IsLod0(Renderer renderer) => !renderer.name.EndsWith(Lod1Suffix) && !renderer.name.EndsWith(Lod2Suffix);

        private static void Drop(Component renderer)
        {
            if (Application.isPlaying)
            {
                Destroy(renderer.gameObject);
            }
            else
            {
                DestroyImmediate(renderer.gameObject); // edit-mode tools and tests
            }
        }

        // Every part by its LOD0 name; the LOD copies under the same key.
        private void Index()
        {
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var name = renderer.name;
                var part = name.EndsWith(Lod1Suffix) || name.EndsWith(Lod2Suffix) ? name.Substring(0, name.Length - Lod1Suffix.Length) : name;
                if (!_parts.TryGetValue(part, out var list))
                {
                    _parts[part] = list = new List<SkinnedMeshRenderer>();
                }

                list.Add(renderer);
            }
        }

        // What this outfit shows: part name → the colour of its first material (null keeps the art's).
        private Dictionary<string, WardrobeCatalog.Swatch?> Dress(Outfit outfit)
        {
            var dress = new Dictionary<string, WardrobeCatalog.Swatch?>();
            var top = _catalog.Tops[outfit.Top.Item];
            var bottom = _catalog.Bottoms[outfit.Bottom.Item];
            var skin = _catalog.Skins[outfit.Skin];
            var hair = _catalog.HairColours[outfit.HairColour];

            foreach (var part in _catalog.Body)
            {
                if (part.Sex == outfit.Sex && !top.Covers(part.Region) && !bottom.Covers(part.Region))
                {
                    dress[part.Mesh] = skin;
                }
            }

            dress[FaceDecalPrefix + (outfit.Sex == Sex.Male ? "M" : "F")] = null;
            if (!top.Covers(WardrobeCatalog.HairRegion))
            {
                foreach (var mesh in _catalog.HairStyles[outfit.Hair].Meshes(outfit.Sex) ?? System.Array.Empty<string>())
                {
                    dress[mesh] = hair;
                }
            }

            dress[top.Mesh(outfit.Sex)] = Cloth(outfit.Top);
            dress[bottom.Mesh(outfit.Sex)] = Cloth(outfit.Bottom);
            if (!outfit.Hat.IsNone)
            {
                dress[_catalog.Hats[outfit.Hat.Item].Mesh(outfit.Sex)] = Cloth(outfit.Hat);
            }

            return dress;
        }

        private WardrobeCatalog.Swatch Cloth(Worn worn) => _catalog.Cloth[worn.Hue].Of(worn.Tone);

        // The first material slot takes the colour; a second slot is the garment's accent and keeps the art's.
        private static void Paint(SkinnedMeshRenderer renderer, WardrobeCatalog.Swatch? swatch)
        {
            if (swatch is not { } colour)
            {
                return;
            }

            var materials = renderer.sharedMaterials;
            if (!Swatches.TryGetValue(colour.Name, out var material) || material == null)
            {
                material = new Material(materials[0]) { name = $"Swatch_{colour.Name}" };
                material.SetColor(BaseColorId, colour.Colour);
                Swatches[colour.Name] = material;
            }

            materials[0] = material;
            renderer.sharedMaterials = materials;
        }

        private static void Shape(SkinnedMeshRenderer renderer, Build build)
        {
            var mesh = renderer.sharedMesh;
            var slim = mesh.GetBlendShapeIndex(BuildSlim);
            var heavy = mesh.GetBlendShapeIndex(BuildHeavy);
            if (slim >= 0)
            {
                renderer.SetBlendShapeWeight(slim, build == Build.Slim ? FullShapeKey : 0f);
            }

            if (heavy >= 0)
            {
                renderer.SetBlendShapeWeight(heavy, build == Build.Heavy ? FullShapeKey : 0f);
            }
        }

    }
}
