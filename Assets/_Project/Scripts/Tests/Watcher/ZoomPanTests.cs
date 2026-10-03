using System.Linq;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Layouts;
using LastSeenWearing.Core.Watcher;
using LastSeenWearing.Editor.Import;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSeenWearing.Tests.Watcher
{
    /// <summary>
    /// P1.17a: one camera per layout pans and zooms, slowly (GDD §04.1) — far enough in that a figure in a
    /// "far only" zone of layout A (docs/LAYOUTS.md) becomes big enough to tell apart.
    /// </summary>
    public sealed class ZoomPanTests
    {
        private const float BaseFov = 58.72f;

        private static ZoomPan NewZoomPan() => new(BaseFov, 15f, 35f, 15f, 20f, 1f);

        [Test]
        public void ItZoomsSlowlyUpToTheNarrowestView()
        {
            var aim = NewZoomPan();
            Assert.That(aim.FieldOfView, Is.EqualTo(BaseFov).Within(0.01f), "starts unzoomed");
            aim.ZoomBy(10f);
            aim.Advance(0.5f);
            Assert.That(aim.Octaves, Is.EqualTo(0.5f).Within(1e-4f), "one doubling per second");
            for (var i = 0; i < 100; i++)
            {
                aim.Advance(0.1f);
            }

            Assert.That(aim.FieldOfView, Is.EqualTo(15f).Within(0.01f), "stops at the narrowest view");
            Assert.That(aim.Magnification, Is.EqualTo(ZoomPan.MagnificationOf(BaseFov, 15f)).Within(1e-3f));

            aim.ZoomBy(-100f);
            for (var i = 0; i < 100; i++)
            {
                aim.Advance(0.1f);
            }

            Assert.That(aim.FieldOfView, Is.EqualTo(BaseFov).Within(0.01f), "and back out, no wider than the mount");
        }

        [Test]
        public void ItPansSlowlyWithinItsRangeAndFinerWhenZoomedIn()
        {
            var aim = NewZoomPan();
            aim.PanBy(100f, -100f);
            aim.Advance(1f);
            Assert.That(aim.Yaw, Is.EqualTo(20f).Within(1e-4f), "20° per second");
            Assert.That(aim.Pitch, Is.EqualTo(-15f).Within(1e-4f), "pitch reached its limit");
            aim.Advance(10f);
            Assert.That(aim.Yaw, Is.EqualTo(35f).Within(1e-4f), "yaw stops at its range");

            var zoomed = NewZoomPan();
            zoomed.ZoomBy(1f);
            zoomed.Advance(1f); // 2×
            zoomed.PanBy(10f, 0f);
            zoomed.Advance(10f);
            Assert.That(zoomed.Yaw, Is.EqualTo(5f).Within(1e-4f), "a degree of input is half a degree at 2×");
        }

        [Test]
        public void LayoutAHasOneZoomCameraOnTheGoodFilter()
        {
            var definition = AssetDatabase.LoadAssetAtPath<LayoutDefinition>(LayoutImporter.DefinitionPath("A"));
            Assert.That(definition.ZoomCamera, Is.InRange(0, definition.Cameras.Length - 1));

            var scene = System.IO.File.ReadAllText("Assets/_Project/Scenes/Festival_A.unity");
            Assert.That(scene.Split('\n').Count(l => l.Trim() == "_zoomable: 1"), Is.EqualTo(1), "exactly one camera zooms");
        }

        [Test]
        public void ZoomedInAFarFigureIsBigEnoughToTellApart()
        {
            // The centre of layout A's square is "far only" from every camera (docs/LAYOUTS.md coverage map).
            var definition = AssetDatabase.LoadAssetAtPath<LayoutDefinition>(LayoutImporter.DefinitionPath("A"));
            var watcher = AssetDatabase.LoadAssetAtPath<WatcherConfig>("Assets/_Project/Data/Config/WatcherConfig.asset");
            var profile = AssetDatabase.LoadAssetAtPath<CctvFilterProfile>("Assets/_Project/Data/Cameras/CctvFilter_Default.asset");
            var camera = definition.Cameras[definition.ZoomCamera];
            var distance = Vector3.Distance(camera.Position, new Vector3(0f, 0.9f, 0f));

            float PixelsTall(float fov) => profile.Height * 1.75f / distance / (2f * Mathf.Tan(fov * Mathf.Deg2Rad / 2f));

            Assert.That(PixelsTall(camera.VerticalFieldOfView), Is.LessThan(25f), "unzoomed: a few pixels, no face, no clothes");
            Assert.That(PixelsTall(watcher.ZoomMinFieldOfView), Is.GreaterThanOrEqualTo(60f), "zoomed in: a figure to describe");
        }
    }
}
