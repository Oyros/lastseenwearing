using LastSeenWearing.Core.Config;
using LastSeenWearing.Gameplay.Cameras;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSeenWearing.Tests.Project
{
    /// <summary>
    /// P1.07: a CCTV camera renders into a low-resolution, point-filtered feed of its profile's size, and
    /// the feed shader and material exist (GDD §04.1, LOOKDEV §2, D-026).
    /// </summary>
    public sealed class CctvFilterTests
    {
        private const string DefaultProfilePath = "Assets/_Project/Data/Cameras/CctvFilter_Default.asset";
        private const string FeedMaterialPath = "Assets/_Project/Art/Materials/M_CctvFeed.mat";

        [Test]
        public void TheDefaultProfileIsTheLookdevGate()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CctvFilterProfile>(DefaultProfilePath);
            Assert.That(profile, Is.Not.Null);
            Assert.That((profile.Width, profile.Height), Is.EqualTo((320, 180)), "LOOKDEV §2: 320×180");
        }

        [Test]
        public void ACameraRendersIntoAPointFilteredFeedOfItsProfilesSize()
        {
            var go = new GameObject("CctvProbe", typeof(Camera));
            go.SetActive(false); // Awake waits until the profile is set
            try
            {
                var cctv = go.AddComponent<CctvCamera>();
                var serialized = new SerializedObject(cctv);
                serialized.FindProperty("_profile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CctvFilterProfile>(DefaultProfilePath);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var feed = cctv.Feed;
                Assert.That((feed.width, feed.height), Is.EqualTo((cctv.Profile.Width, cctv.Profile.Height)));
                Assert.That(feed.filterMode, Is.EqualTo(FilterMode.Point), "pixels stay pixels when shown large");
                Assert.That(go.GetComponent<Camera>().targetTexture, Is.SameAs(feed));
            }
            finally
            {
                var feed = go.GetComponent<Camera>().targetTexture;
                go.GetComponent<Camera>().targetTexture = null;
                if (feed != null)
                {
                    feed.Release();
                    Object.DestroyImmediate(feed);
                }

                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TheFeedMaterialUsesTheCctvShader()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FeedMaterialPath);
            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader.name, Is.EqualTo("LSW/UI/CctvFeed"));
            Assert.That(material.shader.isSupported, Is.True);
        }
    }
}
