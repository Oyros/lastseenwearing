using System.Linq;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Layouts;
using LastSeenWearing.Core.Objective;
using LastSeenWearing.Editor.Import;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSeenWearing.Tests.Objective
{
    /// <summary>
    /// P1.22: the fugitive's targets (GDD §04.4) — any three of five, each once, each with its job's time; the
    /// third opens an exit, the one farthest from it.
    /// </summary>
    public sealed class TargetTests
    {
        private static readonly Vector3[] Exits = { new(0f, 0f, 14f), new(14f, 0f, -26f) };

        private static FugitiveConfig Config => AssetDatabase.LoadAssetAtPath<FugitiveConfig>("Assets/_Project/Data/Config/FugitiveConfig.asset");

        [Test]
        public void EachJobTakesItsTime()
        {
            var config = Config;
            Assert.That(config.SecondsFor(TargetKind.Open), Is.EqualTo(2f), "lift a wallet");
            Assert.That(config.SecondsFor(TargetKind.Fixed), Is.EqualTo(3f), "swap a poster");
            Assert.That(config.SecondsFor(TargetKind.Social), Is.EqualTo(4f), "talk to a vendor");
            Assert.That(config.SecondsFor(TargetKind.Hidden), Is.EqualTo(5f), "pick a safe");
            Assert.That(config.TargetsNeeded, Is.EqualTo(3));
            Assert.That(config.ExitRule, Is.EqualTo(ExitRule.Farthest), "team, P1.22");
        }

        [Test]
        public void ThreeDifferentTargetsOpenAnExitAndNotBefore()
        {
            var progress = new TargetProgress(5, 3);
            Assert.That(progress.Finish(0, Vector3.zero, Exits, ExitRule.Farthest), Is.True);
            Assert.That(progress.Finish(0, Vector3.zero, Exits, ExitRule.Farthest), Is.False, "a target counts once");
            Assert.That(progress.Finish(4, Vector3.zero, Exits, ExitRule.Farthest), Is.True);
            Assert.That(progress.Complete, Is.False);
            Assert.That(progress.OpenExit, Is.EqualTo(TargetProgress.NoExit), "no exit before the third");

            Assert.That(progress.Finish(2, new Vector3(0f, 0f, 10f), Exits, ExitRule.Farthest), Is.True);
            Assert.That(progress.Complete, Is.True);
            Assert.That(progress.OpenExit, Is.EqualTo(1), "the last target, by the south gate, opens the far alley");
            Assert.That(progress.Mask, Is.EqualTo(0b10101));

            Assert.That(progress.Finish(3, Vector3.zero, Exits, ExitRule.Farthest), Is.False, "the way out is open; no fourth");
            Assert.That(progress.IsDone(5), Is.False);
            Assert.That(progress.Finish(7, Vector3.zero, Exits, ExitRule.Farthest), Is.False, "no such target");
        }

        [Test]
        public void TheLastTargetDecidesTheExit()
        {
            var bySouthGate = new Vector3(0f, 0f, 10f);
            var byAlley = new Vector3(14f, 0f, -21f);
            Assert.That(TargetProgress.ExitFor(bySouthGate, Exits, ExitRule.Farthest), Is.EqualTo(1));
            Assert.That(TargetProgress.ExitFor(byAlley, Exits, ExitRule.Farthest), Is.EqualTo(0));
            Assert.That(TargetProgress.ExitFor(byAlley, Exits, ExitRule.Nearest), Is.EqualTo(1));
            Assert.That(TargetProgress.ExitFor(byAlley, new Vector3[0], ExitRule.Farthest), Is.EqualTo(TargetProgress.NoExit));
        }

        [Test]
        public void OnLayoutAEveryEndingOpensSomeExit()
        {
            var layout = AssetDatabase.LoadAssetAtPath<LayoutDefinition>(LayoutImporter.DefinitionPath("A"));
            var exits = layout.Exits.Select(e => e.Position).ToArray();
            var opened = layout.Targets.Select(t => TargetProgress.ExitFor(t.Position, exits, Config.ExitRule)).ToArray();
            Assert.That(opened, Has.All.GreaterThanOrEqualTo(0));
            Assert.That(opened.Distinct().Count(), Is.EqualTo(exits.Length), "both exits can end a round, depending on the last target");
        }
    }
}
