using System.Linq;
using LastSeenWearing.Core.Capture;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Round;
using LastSeenWearing.Gameplay.Capture;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LastSeenWearing.Tests.Capture
{
    /// <summary>
    /// P1.24: the patrol's cuffs (GDD §04.3, §06) — the fugitive cuffed is the police's round, anyone else costs a
    /// cuff, the last one wasted starts the chase, and a chase that runs out is the fugitive's.
    /// </summary>
    public sealed class CuffTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Festival_A.unity";

        private static CaptureConfig Config => AssetDatabase.LoadAssetAtPath<CaptureConfig>("Assets/_Project/Data/Config/CaptureConfig.asset");

        [Test]
        public void ThreeCuffsARound()
        {
            Assert.That(Config.CuffsPerRound, Is.EqualTo(3), "GDD §04.3");
            Assert.That(Config.ArrestRange, Is.GreaterThan(0f));
            Assert.That(Config.WrongArrestBonusSeconds, Is.GreaterThan(0f), "a wrong arrest gives the fugitive time");
            Assert.That(AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/Config/GameConfig.asset").Capture, Is.EqualTo(Config));
        }

        [Test]
        public void CuffingTheFugitiveWinsWithoutSpendingACuff()
        {
            var cuffs = new Cuffs(3);
            Assert.That(cuffs.Arrest(true), Is.EqualTo(ArrestResult.Caught));
            Assert.That(cuffs.Left, Is.EqualTo(3));
        }

        [Test]
        public void EveryWrongArrestCostsACuffAndTheLastStartsTheChase()
        {
            var cuffs = new Cuffs(3);
            Assert.That(cuffs.Arrest(false), Is.EqualTo(ArrestResult.Wrong));
            Assert.That(cuffs.Left, Is.EqualTo(2));
            Assert.That(cuffs.Arrest(false), Is.EqualTo(ArrestResult.Wrong));
            Assert.That(cuffs.Arrest(false), Is.EqualTo(ArrestResult.LastCuffSpent));
            Assert.That(cuffs.Left, Is.EqualTo(0));
            Assert.That(cuffs.Arrest(true), Is.EqualTo(ArrestResult.Refused), "no cuff left: not even the fugitive");
            Assert.That(cuffs.Arrest(false), Is.EqualTo(ArrestResult.Refused));
        }

        [Test]
        public void TheRoundGoesFromTheLastCuffToTheChaseAndOut()
        {
            Assert.That(RoundCycle.Next(RoundPhase.Live, RoundEvent.CuffsSpent, 0, 3), Is.EqualTo(RoundPhase.LastCuff));
            Assert.That(RoundCycle.Next(RoundPhase.LastCuff, RoundEvent.ChaseOver, 0, 3), Is.EqualTo(RoundPhase.Result));
            Assert.That(RoundCycle.Next(RoundPhase.Live, RoundEvent.Outcome, 0, 3), Is.EqualTo(RoundPhase.Result), "an arrest ends play");
            Assert.That(new[] { RoundOutcome.Arrested, RoundOutcome.OutOfCuffs, RoundOutcome.Escaped, RoundOutcome.TimeUp }.Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void TheFestivalSceneCarriesTheCuffs()
        {
            var already = SceneManager.GetSceneByPath(ScenePath);
            var scene = already.isLoaded ? already : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var arrests = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Arrests>(true)).Single();
                Assert.That(arrests.Config, Is.EqualTo(Config));
                var wiring = new SerializedObject(arrests);
                foreach (var field in new[] { "_director", "_roster", "_crowd" })
                {
                    Assert.That(wiring.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                }
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
