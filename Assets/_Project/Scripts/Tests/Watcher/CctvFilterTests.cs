using System.Linq;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Gameplay.Cameras;
using LastSeenWearing.UI.Watcher;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastSeenWearing.Tests.Watcher
{
    /// <summary>P1.14: each camera carries its own filter — two cameras, two looks (GDD §04.1, D-026).</summary>
    public sealed class CctvFilterTests
    {
        private const string DefaultPath = "Assets/_Project/Data/Cameras/CctvFilter_Default.asset";
        private const string WornPath = "Assets/_Project/Data/Cameras/CctvFilter_Worn.asset";
        private const string FeedMaterialPath = "Assets/_Project/Art/Materials/M_CctvFeed.mat";
        private const string SandboxPath = "Assets/_Project/Scenes/Sandbox_Crowd.unity";

        [Test]
        public void TheWornFilterIsAnotherLook()
        {
            var good = AssetDatabase.LoadAssetAtPath<CctvFilterProfile>(DefaultPath);
            var worn = AssetDatabase.LoadAssetAtPath<CctvFilterProfile>(WornPath);
            Assert.That(worn, Is.Not.Null);
            Assert.That(worn.Width * worn.Height, Is.LessThan(good.Width * good.Height), "fewer pixels");
            Assert.That(worn.Grain, Is.GreaterThan(good.Grain), "noisier");
        }

        [Test]
        public void TheSandboxCamerasCarryTwoFilters()
        {
            var text = System.IO.File.ReadAllText(SandboxPath);
            var guids = new[] { DefaultPath, WornPath }.Select(AssetDatabase.AssetPathToGUID).ToArray();
            Assert.That(guids.All(g => text.Contains(g)), Is.True, "Sandbox_Crowd references both profiles");
        }

        [Test]
        public void AFeedViewTakesTheLookOfTheCameraItShows()
        {
            var good = NewCamera(AssetDatabase.LoadAssetAtPath<CctvFilterProfile>(DefaultPath));
            var worn = NewCamera(AssetDatabase.LoadAssetAtPath<CctvFilterProfile>(WornPath));
            var view = new GameObject("View", typeof(RectTransform), typeof(RawImage));
            try
            {
                var feed = view.AddComponent<CctvFeedView>();
                typeof(CctvFeedView).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(feed, null);
                feed.FeedMaterial = AssetDatabase.LoadAssetAtPath<Material>(FeedMaterialPath);
                var image = view.GetComponent<RawImage>();

                foreach (var feedCamera in new[] { good, worn, good })
                {
                    feed.Show(feedCamera);
                    var profile = feedCamera.Profile;
                    Assert.That(image.texture.width, Is.EqualTo(profile.Width));
                    Assert.That(image.texture.height, Is.EqualTo(profile.Height));
                    Assert.That(image.material.GetFloat("_Grain"), Is.EqualTo(profile.Grain).Within(1e-5f));
                    Assert.That(image.material.GetFloat("_Contrast"), Is.EqualTo(profile.Contrast).Within(1e-5f));
                    Assert.That(image.material.GetVector("_FeedSize").x, Is.EqualTo(profile.Width));
                }
            }
            finally
            {
                Object.DestroyImmediate(view);
                Object.DestroyImmediate(good.gameObject);
                Object.DestroyImmediate(worn.gameObject);
            }
        }

        private static CctvCamera NewCamera(CctvFilterProfile profile)
        {
            var go = new GameObject("Cam", typeof(Camera));
            go.SetActive(false);
            var feedCamera = go.AddComponent<CctvCamera>();
            var serialized = new SerializedObject(feedCamera);
            serialized.FindProperty("_profile").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return feedCamera;
        }
    }
}
