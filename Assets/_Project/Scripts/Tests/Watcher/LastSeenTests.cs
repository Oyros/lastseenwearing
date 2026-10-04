using LastSeenWearing.Core.Wardrobe;
using LastSeenWearing.Core.Watcher;
using LastSeenWearing.Editor.Import;
using LastSeenWearing.Gameplay.Crowd;
using LastSeenWearing.Gameplay.Player;
using LastSeenWearing.Gameplay.Round;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSeenWearing.Tests.Watcher
{
    /// <summary>
    /// P1.20: the Watcher's "last seen" — the clothes and when, counting up from the last sighting (the witness at
    /// round start, or the Watcher's own mark on a feed).
    /// </summary>
    public sealed class LastSeenTests
    {
        private static readonly Outfit Someone = new(Sex.Female, Height.Tall, Build.Slim, 1, 2, 3,
            new Worn(1, 4, Tone.Dark), new Worn(2, 5, Tone.Light), new Worn(3, 6, Tone.Dark));

        [Test]
        public void TheAgeCountsUpFromTheSighting()
        {
            var sighting = Sighting.Of(Someone, 100d, SightingSource.Witness);
            Assert.That(sighting.Age(100d), Is.EqualTo(0d));
            Assert.That(sighting.Age(225.7d), Is.EqualTo(125.7d).Within(1e-9));
            Assert.That(sighting.Age(90d), Is.EqualTo(0d), "never negative");
            Assert.That(Sighting.Clock(125.7d), Is.EqualTo((2, 5)), "2:05 ago");

            var marked = Sighting.Of(Someone, 300d, SightingSource.Mark);
            Assert.That(marked.Age(301d), Is.EqualTo(1d), "a new sighting starts the count again");
        }

        [Test]
        public void ASightingIsTheClothesOnly()
        {
            var sighting = Sighting.Of(Someone, 0d, SightingSource.Mark);
            Assert.That(sighting.Top, Is.EqualTo(Someone.Top));
            Assert.That(sighting.Bottom, Is.EqualTo(Someone.Bottom));
            Assert.That(sighting.Hat, Is.EqualTo(Someone.Hat));

            var wire = CompositeSync.Clothes.Of(Someone);
            Assert.That(wire.Top.ToWorn(), Is.EqualTo(Someone.Top));
            Assert.That(wire.Hat.ToWorn(), Is.EqualTo(Someone.Hat), "the witness report crosses the network whole");
        }

        [Test]
        public void AClickOnAFeedFindsWhoStandsThere()
        {
            var npc = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/CrowdNpc.prefab"));
            var cameraObject = new GameObject("Cctv", typeof(Camera));
            try
            {
                npc.transform.position = new Vector3(0f, 0f, 12f);
                npc.GetComponent<OutfitView>().Apply(Someone);
                var camera = cameraObject.GetComponent<Camera>();
                camera.aspect = 16f / 9f;
                cameraObject.transform.position = new Vector3(3f, 5.7f, 0f);
                cameraObject.transform.LookAt(new Vector3(0f, 1f, 12f));
                Physics.SyncTransforms();

                // Where the chest sits on the feed is where the Watcher clicks.
                var viewport = camera.WorldToViewportPoint(new Vector3(0f, 1.2f, 12f));
                var ray = camera.ViewportPointToRay(viewport);
                var hit = AimProbe.Find(ray.origin, ray.direction, 80f, null);
                Assert.That(hit, Is.Not.Null);
                Assert.That(hit.Character.GetComponent<OutfitView>().Outfit, Is.EqualTo(Someone));

                var beside = camera.ViewportPointToRay(new Vector3(viewport.x + 0.2f, viewport.y, 0f));
                Assert.That(AimProbe.Find(beside.origin, beside.direction, 80f, null), Is.Null, "a click beside them marks no one");
            }
            finally
            {
                Object.DestroyImmediate(npc);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void EveryGarmentHasAWordForTheLastSeenLine()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WardrobeCatalog>(WardrobeImporter.CatalogPath);
            var words = (UnityEngine.Localization.Tables.StringTable)UnityEditor.Localization.LocalizationEditorSettings
                .GetStringTableCollection("Festival").GetTable("en");
            Assert.That(words.GetEntry("person.tone.light"), Is.Not.Null);
            Assert.That(words.GetEntry("person.tone.dark"), Is.Not.Null);
            foreach (var garment in catalog.Tops)
            {
                Assert.That(words.GetEntry(garment.NameKey), Is.Not.Null, garment.Id);
            }
        }
    }
}
