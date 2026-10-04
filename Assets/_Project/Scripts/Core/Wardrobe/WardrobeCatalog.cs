using System;
using UnityEngine;

namespace LastSeenWearing.Core.Wardrobe
{
    public enum Sex
    {
        Male,
        Female,
    }

    /// <summary>The body's build shape keys (art: <c>Build_Slim</c>, <c>Build_Heavy</c>). Only heavy reads on camera (PL.16).</summary>
    public enum Build
    {
        Average,
        Slim,
        Heavy,
    }

    /// <summary>A character's height bucket (GDD §05: a permanent trait, the composite's first clue with build).</summary>
    public enum Height
    {
        Average,
        Short,
        Tall,
    }

    /// <summary>A cloth colour's value: what the black-and-white camera reads of it (palette: light ≥ 0.60, dark ≤ 0.25 luma).</summary>
    public enum Tone
    {
        Light,
        Dark,
    }

    /// <summary>
    /// Everything a character can wear (GDD §05, docs/DATA.md §2): the garments by slot, the hair styles, the body
    /// parts and the palette — generated from the art's export (body JSON, hair options, palette) by
    /// <c>Editor/Import/WardrobeImporter</c>, never typed (D-037). Meshes are named as in the body FBX.
    /// </summary>
    [CreateAssetMenu(fileName = "WardrobeCatalog", menuName = "Last Seen Wearing/Wardrobe/Catalog")]
    public sealed class WardrobeCatalog : ScriptableObject
    {
        /// <summary>A body region a garment can cover (art JSON <c>region</c> / <c>hides</c>).</summary>
        public const string HairRegion = "Hair";

        [Serializable]
        public struct Garment
        {
            public string Id;
            [Tooltip("Localization key of the word a player would use (Festival table).")]
            public string NameKey;
            public string MaleMesh;
            public string FemaleMesh;
            [Tooltip("Body regions it covers; \"Hair\" means a hood.")]
            public string[] Hides;

            public string Mesh(Sex sex) => sex == Sex.Male ? MaleMesh : FemaleMesh;

            public bool Covers(string region) => Hides != null && Array.IndexOf(Hides, region) >= 0;
        }

        [Serializable]
        public struct HairStyle
        {
            public string Id;
            public string NameKey;
            public string[] MaleMeshes;
            [Tooltip("Empty for a style only men wear (the facial-hair styles) — and for bald, see Bald.")]
            public string[] FemaleMeshes;
            public bool Bald;

            public string[] Meshes(Sex sex) => sex == Sex.Male ? MaleMeshes : FemaleMeshes;

            public bool Fits(Sex sex) => Bald || Meshes(sex) is { Length: > 0 };
        }

        [Serializable]
        public struct BodyMesh
        {
            public string Mesh;
            public string Region;
            public Sex Sex;
        }

        [Serializable]
        public struct Swatch
        {
            public string Name;
            [Tooltip("sRGB, as the palette's hex.")]
            public Color Colour;
            public float Luma;
        }

        [Serializable]
        public struct ClothHue
        {
            public string Name;
            public Swatch Light;
            public Swatch Dark;

            public Swatch Of(Tone tone) => tone == Tone.Light ? Light : Dark;
        }

        [SerializeField] private Garment[] _tops = Array.Empty<Garment>();
        [SerializeField] private Garment[] _bottoms = Array.Empty<Garment>();
        [SerializeField] private Garment[] _hats = Array.Empty<Garment>();
        [SerializeField] private HairStyle[] _hairStyles = Array.Empty<HairStyle>();
        [SerializeField] private BodyMesh[] _body = Array.Empty<BodyMesh>();
        [SerializeField] private Swatch[] _skins = Array.Empty<Swatch>();
        [SerializeField] private Swatch[] _hairColours = Array.Empty<Swatch>();
        [SerializeField] private ClothHue[] _cloth = Array.Empty<ClothHue>();

        public Garment[] Tops => _tops;
        public Garment[] Bottoms => _bottoms;
        public Garment[] Hats => _hats;
        public HairStyle[] HairStyles => _hairStyles;
        public BodyMesh[] Body => _body;
        public Swatch[] Skins => _skins;
        public Swatch[] HairColours => _hairColours;
        public ClothHue[] Cloth => _cloth;

        /// <summary>Editor import only: replaces everything from the art's export.</summary>
        public void Set(Garment[] tops, Garment[] bottoms, Garment[] hats, HairStyle[] hairStyles, BodyMesh[] body,
            Swatch[] skins, Swatch[] hairColours, ClothHue[] cloth)
        {
            _tops = tops;
            _bottoms = bottoms;
            _hats = hats;
            _hairStyles = hairStyles;
            _body = body;
            _skins = skins;
            _hairColours = hairColours;
            _cloth = cloth;
        }
    }
}
