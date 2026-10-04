using System.Collections.Generic;
using System.Linq;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Wardrobe;
using LastSeenWearing.Editor.Import;
using LastSeenWearing.Gameplay.Crowd;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace LastSeenWearing.Tests.Wardrobe
{
    /// <summary>
    /// P1.18: the crowd dressed from the seed (GDD §05) — the catalog is the art's export, outfits follow the art's
    /// rules and the same seed dresses the same crowd everywhere, and a dressed body shows exactly its outfit.
    /// </summary>
    public sealed class WardrobeTests
    {
        private const string NpcPath = "Assets/_Project/Prefabs/Characters/CrowdNpc.prefab";

        private static WardrobeCatalog Catalog => AssetDatabase.LoadAssetAtPath<WardrobeCatalog>(WardrobeImporter.CatalogPath);

        private static WardrobeConfig Odds => AssetDatabase.LoadAssetAtPath<WardrobeConfig>("Assets/_Project/Data/Config/WardrobeConfig.asset");

        [Test]
        public void TheCatalogIsTheArtsExportAndEveryMeshIsInTheBody()
        {
            var catalog = Catalog;
            Assert.That(catalog.Tops, Has.Length.EqualTo(8));
            Assert.That(catalog.Bottoms, Has.Length.EqualTo(6));
            Assert.That(catalog.Hats, Has.Length.EqualTo(5));
            Assert.That(catalog.Skins, Has.Length.EqualTo(6));
            Assert.That(catalog.Cloth.All(c => c.Light.Luma >= 0.6f && c.Dark.Luma <= 0.25f), Is.True, "every colour pair reads light/dark on camera");

            var meshes = new HashSet<string>(AssetDatabase.LoadAssetAtPath<GameObject>(CrowdAnimatorBuilder.BodyPath)
                .GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => r.name));
            var wanted = catalog.Tops.Concat(catalog.Bottoms).Concat(catalog.Hats).SelectMany(g => new[] { g.MaleMesh, g.FemaleMesh })
                .Concat(catalog.HairStyles.SelectMany(h => h.MaleMeshes.Concat(h.FemaleMeshes)))
                .Concat(catalog.Body.Select(b => b.Mesh));
            Assert.That(wanted.Where(m => !meshes.Contains(m)), Is.Empty, "every catalog mesh exists in the body FBX");

            var words = (StringTable)LocalizationEditorSettings.GetStringTableCollection("Festival").GetTable("en");
            var keys = catalog.Tops.Concat(catalog.Bottoms).Concat(catalog.Hats).Select(g => g.NameKey).Concat(catalog.HairStyles.Select(h => h.NameKey));
            Assert.That(keys.Where(k => words.GetEntry(k) == null), Is.Empty, "every garment has its word");
        }

        [Test]
        public void TheSameSeedDressesTheSameCrowd()
        {
            var a = OutfitPlanner.OutfitsFor(77, 150, Catalog, Odds);
            var b = OutfitPlanner.OutfitsFor(77, 150, Catalog, Odds);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(OutfitPlanner.OutfitsFor(78, 150, Catalog, Odds), Is.Not.EqualTo(a));
            Assert.That(OutfitPlanner.OutfitsFor(77, 120, Catalog, Odds), Is.EqualTo(a.Take(120)), "a smaller crowd is a prefix");
        }

        [Test]
        public void OutfitsKeepTheArtsRulesAndUseTheWholeWardrobe()
        {
            var catalog = Catalog;
            var outfits = OutfitPlanner.OutfitsFor(2026, 1000, catalog, Odds);
            foreach (var outfit in outfits)
            {
                var top = catalog.Tops[outfit.Top.Item];
                if (top.Covers(WardrobeCatalog.HairRegion))
                {
                    Assert.That(outfit.Hat.IsNone, Is.True, "no hat over a hood");
                }

                Assert.That(OutfitPlanner.Clash(top.Id, catalog.Bottoms[outfit.Bottom.Item].Id), Is.False, "no skirt under the trench");
                Assert.That(catalog.HairStyles[outfit.Hair].Fits(outfit.Sex), Is.True, "hair the body can wear");
            }

            var women = outfits.Count(o => o.Sex == Sex.Female) / 1000f;
            Assert.That(women, Is.InRange(0.45f, 0.55f), "50/50 (team, P1.18)");
            Assert.That(outfits.Select(o => o.Top.Item).Distinct().Count(), Is.EqualTo(catalog.Tops.Length));
            Assert.That(outfits.Select(o => o.Bottom.Item).Distinct().Count(), Is.EqualTo(catalog.Bottoms.Length));
            Assert.That(outfits.Where(o => !o.Hat.IsNone).Select(o => o.Hat.Item).Distinct().Count(), Is.EqualTo(catalog.Hats.Length));
            Assert.That(outfits.Select(o => o.Hair).Distinct().Count(), Is.EqualTo(catalog.HairStyles.Length));
            Assert.That(outfits.Select(o => o.Top.Tone).Distinct().Count(), Is.EqualTo(2), "light and dark tops");
        }

        [Test]
        public void TheFugitiveWearsWhatNoNpcWears()
        {
            foreach (var seed in new[] { 1, 99, 4242 })
            {
                var crowd = OutfitPlanner.OutfitsFor(seed, 150, Catalog, Odds);
                var fugitive = OutfitPlanner.CharacterOutfit(seed, 150, 0, Catalog, Odds);
                Assert.That(crowd, Has.No.Member(fugitive), $"seed {seed}");
                Assert.That(OutfitPlanner.CharacterOutfit(seed, 150, 0, Catalog, Odds), Is.EqualTo(fugitive), "the same on every client");
            }
        }

        [Test]
        public void TheBuildSurvivesTheAnimator()
        {
            // The export keys every shape key at 0 in every clip; played, they undid the build (P1.19).
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(CrowdAnimatorBuilder.BodyPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")))
            {
                Assert.That(AnimationUtility.GetCurveBindings(clip).Where(b => b.propertyName.StartsWith("blendShape.")), Is.Empty, clip.name);
            }

            var catalog = Catalog;
            var outfit = new Outfit(Sex.Male, Height.Average, Build.Heavy, 0, 0, 0, new Worn(0, 0, Tone.Dark), new Worn(0, 0, Tone.Dark), Worn.Nothing);
            var npc = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(NpcPath));
            try
            {
                npc.GetComponent<OutfitView>().Apply(outfit);
                var animator = npc.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                for (var i = 0; i < 5; i++)
                {
                    animator.Update(0.1f);
                }

                var top = npc.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name == catalog.Tops[0].MaleMesh);
                Assert.That(top.GetBlendShapeWeight(top.sharedMesh.GetBlendShapeIndex("Build_Heavy")), Is.EqualTo(100f), "still heavy after the animator ran");
            }
            finally
            {
                Object.DestroyImmediate(npc);
            }
        }

        [Test]
        public void ADressedBodyShowsExactlyItsOutfit()
        {
            var catalog = Catalog;
            var raincoat = System.Array.FindIndex(catalog.Tops, t => t.Id == "Raincoat");
            var shorts = System.Array.FindIndex(catalog.Bottoms, b => b.Id == "Shorts");
            var crew = System.Array.FindIndex(catalog.HairStyles, h => h.Id == "Crew");
            var outfit = new Outfit(Sex.Female, Height.Tall, Build.Heavy, 2, crew, 1, new Worn(raincoat, 3, Tone.Dark), new Worn(shorts, 5, Tone.Light), Worn.Nothing);

            var npc = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(NpcPath));
            try
            {
                npc.GetComponent<OutfitView>().Apply(outfit);
                var shown = npc.GetComponentsInChildren<SkinnedMeshRenderer>(false).Select(r => r.name).Where(n => !n.Contains("_LOD")).ToList();
                Assert.That(shown, Does.Contain("LSW_Crowd_Top_Raincoat_F"));
                Assert.That(shown, Does.Contain("LSW_Crowd_Bottom_Shorts_F"));
                Assert.That(shown, Does.Contain("LSW_Crowd_Thigh_F.L"), "shorts leave the thighs bare");
                Assert.That(shown, Has.None.Contains("Torso"), "the coat covers the torso");
                Assert.That(shown, Has.None.Contains("Hair_"), "the hood covers the hair");
                Assert.That(shown, Has.None.Contains("_M"), "a woman's body");

                var coat = npc.GetComponentsInChildren<SkinnedMeshRenderer>(false).Single(r => r.name == "LSW_Crowd_Top_Raincoat_F");
                Assert.That(coat.sharedMaterial.GetColor("_BaseColor"), Is.EqualTo(catalog.Cloth[3].Dark.Colour), "the drawn colour");
                Assert.That(coat.GetBlendShapeWeight(coat.sharedMesh.GetBlendShapeIndex("Build_Heavy")), Is.EqualTo(100f), "heavy build");
                var kept = npc.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Assert.That(kept.Length, Is.EqualTo(shown.Count), "an NPC keeps only what it wears");
                Assert.That(kept.Where(r => r.name.Contains("_LOD")), Is.Empty, "LOD0 only: a hidden skinned LOD still costs its skinning (D-037)");
            }
            finally
            {
                Object.DestroyImmediate(npc);
            }
        }
    }
}
