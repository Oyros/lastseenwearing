using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Watcher;
using NUnit.Framework;
using UnityEditor;

namespace LastSeenWearing.Tests.Watcher
{
    /// <summary>P1.13: switching a monitor costs the configured delay (GDD §04.1).</summary>
    public sealed class FeedSwitcherTests
    {
        private const float Delay = 1.5f;

        [Test]
        public void TheWallStartsLiveOnTheFirstCameras()
        {
            var switcher = new FeedSwitcher(Delay, 4);
            Assert.That(switcher.Showing(0, 0d), Is.EqualTo(0));
            Assert.That(switcher.Showing(1, 0d), Is.EqualTo(1));
            var single = new FeedSwitcher(Delay, 1);
            Assert.That(single.Showing(1, 0d), Is.EqualTo(FeedSwitcher.NoCamera));
        }

        [Test]
        public void SwitchingCostsTheDelay()
        {
            var switcher = new FeedSwitcher(Delay, 4);
            Assert.That(switcher.Select(0, 3, 10d), Is.True);
            Assert.That(switcher.Showing(0, 10d), Is.EqualTo(FeedSwitcher.NoCamera));
            Assert.That(switcher.Showing(0, 10d + Delay - 0.01), Is.EqualTo(FeedSwitcher.NoCamera));
            Assert.That(switcher.Showing(0, 10d + Delay), Is.EqualTo(3));
            Assert.That(switcher.Showing(1, 10.5d), Is.EqualTo(1), "the other monitor is untouched");
        }

        [Test]
        public void TheSameCameraIsFreeAndANewChoiceRestartsTheWait()
        {
            var switcher = new FeedSwitcher(Delay, 4);
            Assert.That(switcher.Select(0, 0, 5d), Is.False, "already showing");
            Assert.That(switcher.Showing(0, 5d), Is.EqualTo(0));

            switcher.Select(0, 2, 5d);
            Assert.That(switcher.Select(0, 2, 6d), Is.False, "already switching to it: no extra wait");
            Assert.That(switcher.Showing(0, 5d + Delay), Is.EqualTo(2));

            switcher.Select(0, 3, 7d);
            switcher.Select(0, 1, 8d);
            Assert.That(switcher.Showing(0, 7d + Delay), Is.EqualTo(FeedSwitcher.NoCamera), "restarted at 8 s");
            Assert.That(switcher.Showing(0, 8d + Delay), Is.EqualTo(1));
        }

        [Test]
        public void ACameraThatDoesNotExistIsIgnored()
        {
            var switcher = new FeedSwitcher(Delay, 2);
            Assert.That(switcher.Select(0, 5, 0d), Is.False);
            Assert.That(switcher.Select(0, -1, 0d), Is.False);
        }

        [Test]
        public void TheDelayComesFromTheWatcherConfig()
        {
            var game = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/Config/GameConfig.asset");
            Assert.That(game.Watcher, Is.Not.Null, "WatcherConfig reachable from GameConfig");
            Assert.That(game.Watcher.FeedSwitchSeconds, Is.GreaterThan(0f), "switching costs time");
        }
    }
}
