using System;
using System.Linq;
using LastSeenWearing.Core.Round;
using LastSeenWearing.UI.Round;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LastSeenWearing.Tests.Round
{
    /// <summary>
    /// P1.26: the end of a round (GDD §06) — every ending goes to a side, the case keeps score, the summary counts
    /// what was spent, and every screen has the words for it.
    /// </summary>
    public sealed class ResultsTests
    {
        [TestCase(RoundOutcome.Arrested, Side.Police)]
        [TestCase(RoundOutcome.TimeUp, Side.Police)]
        [TestCase(RoundOutcome.Escaped, Side.Fugitive)]
        [TestCase(RoundOutcome.OutOfCuffs, Side.Fugitive)]
        [TestCase(RoundOutcome.None, Side.None)]
        public void EveryEndingGoesToASide(RoundOutcome outcome, Side winner)
        {
            Assert.That(RoundOutcomes.Winner(outcome), Is.EqualTo(winner));
        }

        [Test]
        public void TheCaseGoesToTheSideWithMoreRounds()
        {
            Assert.That(RoundOutcomes.Leader(2, 1), Is.EqualTo(Side.Police));
            Assert.That(RoundOutcomes.Leader(0, 3), Is.EqualTo(Side.Fugitive));
            Assert.That(RoundOutcomes.Leader(1, 1), Is.EqualTo(Side.None), "a draw");
        }

        [Test]
        public void TheSummaryCountsTheCuffsSpent()
        {
            var summary = new RoundSummary { CuffsPerRound = 3, CuffsLeft = 1 };
            Assert.That(summary.CuffsSpent, Is.EqualTo(2));
        }

        [Test]
        public void EveryEndingAndLineHasItsWords()
        {
            var words = (UnityEngine.Localization.Tables.StringTable)UnityEditor.Localization.LocalizationEditorSettings
                .GetStringTableCollection("UI").GetTable("en");
            foreach (var key in new[] { "time_up", "escaped", "arrested", "out_of_cuffs" })
            {
                Assert.That(words.GetEntry($"ui.round.result.{key}"), Is.Not.Null, key);
            }

            foreach (var key in new[] { "police_win", "fugitive_win", "targets", "tents", "cuffs", "time_left", "score", "case.police", "case.fugitive", "case.draw" })
            {
                Assert.That(words.GetEntry($"results.{key}"), Is.Not.Null, key);
            }

            Assert.That(Enum.GetValues(typeof(RoundOutcome)).Length, Is.EqualTo(5), "a new ending needs a line here and a side above");
        }

        [Test]
        public void TheFestivalSceneShowsTheResults()
        {
            const string path = "Assets/_Project/Scenes/Festival_A.unity";
            var already = SceneManager.GetSceneByPath(path);
            var scene = already.isLoaded ? already : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var screen = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ResultsScreen>(true)).Single();
                Assert.That(new SerializedObject(screen).FindProperty("_director").objectReferenceValue, Is.Not.Null);
            }
            finally
            {
                if (!already.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
