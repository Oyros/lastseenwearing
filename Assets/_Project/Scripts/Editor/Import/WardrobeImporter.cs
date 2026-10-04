using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastSeenWearing.Core.Wardrobe;
using UnityEditor;
using UnityEngine;

namespace LastSeenWearing.Editor.Import
{
    /// <summary>
    /// The wardrobe from the art's export (P1.18, D-037): the crowd body JSON (garments by slot, body parts, what
    /// each covers — LOD0 names; a dressed body finds the <c>_LOD1</c>/<c>_LOD2</c> copies by name), the hair options
    /// and the palette become <c>Data/Wardrobe/WardrobeCatalog.asset</c>. Rebuild
    /// after an art re-export: menu, or <see cref="Import"/>.
    /// </summary>
    public static class WardrobeImporter
    {
        public const string CatalogPath = "Assets/_Project/Data/Wardrobe/WardrobeCatalog.asset";
        public const string HairOptionsPath = "Assets/_Project/Art/Models/Crowd/LSW_Crowd_HairOptions.json";
        public const string PalettePath = "Assets/_Project/Art/Palette/LSW_Palette.json";

        private static string BodyJsonPath => Path.ChangeExtension(CrowdAnimatorBuilder.BodyPath, ".json");

        [MenuItem("Last Seen Wearing/Art/Import Wardrobe")]
        public static void ImportFromMenu()
        {
            EditorGUIUtility.PingObject(Import());
        }

        public static WardrobeCatalog Import()
        {
            var body = JsonUtility.FromJson<BodyJson>(File.ReadAllText(BodyJsonPath));
            var palette = JsonUtility.FromJson<PaletteJson>(File.ReadAllText(PalettePath));

            var catalog = AssetDatabase.LoadAssetAtPath<WardrobeCatalog>(CatalogPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
                catalog = ScriptableObject.CreateInstance<WardrobeCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Set(
                Garments(body, "top"),
                Garments(body, "bottom"),
                Garments(body, "hat"),
                HairStyles(),
                body.meshes.Where(m => m.slot == "body" && m.lod == 0).Select(m => new WardrobeCatalog.BodyMesh
                {
                    Mesh = m.name,
                    Region = m.region,
                    Sex = SexOf(m),
                }).ToArray(),
                Swatches(palette, "Skin"),
                Swatches(palette, "Hair"),
                ClothHues(palette));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        /// <summary>The localization key of a garment's or hair style's word (docs/LOCALIZATION.md).</summary>
        public static string NameKey(string id) => $"wardrobe.{id.ToLowerInvariant()}.name";

        private static WardrobeCatalog.Garment[] Garments(BodyJson body, string slot)
        {
            return body.meshes.Where(m => m.slot == slot && m.lod == 0)
                .GroupBy(m => m.variant)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new WardrobeCatalog.Garment
                {
                    Id = g.Key,
                    NameKey = NameKey(g.Key),
                    MaleMesh = g.Single(m => SexOf(m) == Sex.Male).name,
                    FemaleMesh = g.Single(m => SexOf(m) == Sex.Female).name,
                    Hides = g.First().hides ?? Array.Empty<string>(),
                }).ToArray();
        }

        private static WardrobeCatalog.HairStyle[] HairStyles()
        {
            // JsonUtility cannot read a dictionary: the options are read by hand, one object per style.
            var text = File.ReadAllText(HairOptionsPath);
            var options = MiniJson.Object(MiniJson.Object(MiniJson.Parse(text))["options"]);
            return options.OrderBy(o => o.Key, StringComparer.Ordinal).Select(o =>
            {
                var option = MiniJson.Object(o.Value);
                var meshes = MiniJson.Object(option["meshes"]);
                string[] Of(string sex) => meshes.TryGetValue(sex, out var list) ? MiniJson.Strings(list) : Array.Empty<string>();
                return new WardrobeCatalog.HairStyle
                {
                    Id = o.Key,
                    NameKey = NameKey(o.Key),
                    MaleMeshes = Of("M"),
                    FemaleMeshes = Of("F"),
                    Bald = option["hair"] == null,
                };
            }).ToArray();
        }

        private static WardrobeCatalog.Swatch[] Swatches(PaletteJson palette, string group)
        {
            return palette.materials.Where(m => m.group == group).Select(Swatch).ToArray();
        }

        private static WardrobeCatalog.ClothHue[] ClothHues(PaletteJson palette)
        {
            var cloth = palette.materials.Where(m => m.group == "Cloth").ToArray();
            return cloth.Where(m => m.value == "Light").Select(light =>
            {
                var hue = light.@short.Substring(0, light.@short.Length - "Light".Length);
                var dark = cloth.Single(m => m.@short == hue + "Dark");
                return new WardrobeCatalog.ClothHue { Name = hue, Light = Swatch(light), Dark = Swatch(dark) };
            }).ToArray();
        }

        private static WardrobeCatalog.Swatch Swatch(PaletteMaterial m)
        {
            if (!ColorUtility.TryParseHtmlString(m.hex, out var colour))
            {
                throw new InvalidOperationException($"[WardrobeImporter] {m.name}: bad hex '{m.hex}'.");
            }

            return new WardrobeCatalog.Swatch { Name = m.@short, Colour = colour, Luma = m.luma };
        }

        private static Sex SexOf(MeshJson mesh) => mesh.sex == "F" ? Sex.Female : Sex.Male;

#pragma warning disable CS0649 // JsonUtility fills these
        [Serializable]
        private sealed class BodyJson
        {
            public MeshJson[] meshes;
        }

        [Serializable]
        private sealed class MeshJson
        {
            public string name;
            public string slot;
            public string variant;
            public string sex;
            public string region;
            public string[] hides;
            public int lod;
        }

        [Serializable]
        private sealed class PaletteJson
        {
            public PaletteMaterial[] materials;
        }

        [Serializable]
        private sealed class PaletteMaterial
        {
            public string name;
            public string @short;
            public string hex;
            public string group;
            public string value;
            public float luma;
        }
#pragma warning restore CS0649

        /// <summary>Just enough JSON for the hair options' nested objects.</summary>
        private static class MiniJson
        {
            public static object Parse(string text)
            {
                var i = 0;
                return Value(text, ref i);
            }

            public static Dictionary<string, object> Object(object value) => (Dictionary<string, object>)value;

            public static string[] Strings(object value) => ((List<object>)value).Cast<string>().ToArray();

            private static object Value(string s, ref int i)
            {
                Skip(s, ref i);
                switch (s[i])
                {
                    case '{':
                        var obj = new Dictionary<string, object>();
                        i++;
                        while (true)
                        {
                            Skip(s, ref i);
                            if (s[i] == '}')
                            {
                                i++;
                                return obj;
                            }

                            var key = (string)Value(s, ref i);
                            Skip(s, ref i);
                            i++; // ':'
                            obj[key] = Value(s, ref i);
                            Skip(s, ref i);
                            if (s[i] == ',')
                            {
                                i++;
                            }
                        }

                    case '[':
                        var list = new List<object>();
                        i++;
                        while (true)
                        {
                            Skip(s, ref i);
                            if (s[i] == ']')
                            {
                                i++;
                                return list;
                            }

                            list.Add(Value(s, ref i));
                            Skip(s, ref i);
                            if (s[i] == ',')
                            {
                                i++;
                            }
                        }

                    case '"':
                        var end = s.IndexOf('"', i + 1);
                        var str = s.Substring(i + 1, end - i - 1);
                        i = end + 1;
                        return str;
                    default:
                        var start = i;
                        while (i < s.Length && ",}] \n\r\t".IndexOf(s[i]) < 0)
                        {
                            i++;
                        }

                        var word = s.Substring(start, i - start);
                        return word == "null" ? null : word;
                }
            }

            private static void Skip(string s, ref int i)
            {
                while (char.IsWhiteSpace(s[i]))
                {
                    i++;
                }
            }
        }
    }
}
