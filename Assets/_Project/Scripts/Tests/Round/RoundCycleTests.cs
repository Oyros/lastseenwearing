using System;
using System.Linq;
using LastSeenWearing.Core.Round;
using NUnit.Framework;

namespace LastSeenWearing.Tests.Round
{
    /// <summary>P1.10: the round's one transition table, its programme and who wins on time (GDD §06, D-028).</summary>
    public sealed class RoundCycleTests
    {
        [TestCase(RoundPhase.Lobby, RoundEvent.StartCase, RoundPhase.Briefing)]
        [TestCase(RoundPhase.Briefing, RoundEvent.BriefingOver, RoundPhase.Live)]
        [TestCase(RoundPhase.Live, RoundEvent.TimeUp, RoundPhase.Result)]
        [TestCase(RoundPhase.Live, RoundEvent.Outcome, RoundPhase.Result)]
        [TestCase(RoundPhase.Live, RoundEvent.CuffsSpent, RoundPhase.LastCuff)]
        [TestCase(RoundPhase.LastCuff, RoundEvent.ChaseOver, RoundPhase.Result)]
        [TestCase(RoundPhase.LastCuff, RoundEvent.Outcome, RoundPhase.Result)]
        [TestCase(RoundPhase.CaseEnd, RoundEvent.CaseEndOver, RoundPhase.Lobby)]
        public void TheTableMovesTheRoundOn(RoundPhase from, RoundEvent roundEvent, RoundPhase to)
        {
            Assert.That(RoundCycle.Next(from, roundEvent, 0, 3), Is.EqualTo(to));
        }

        [Test]
        public void AResultLeadsToTheNextRoundUntilTheCaseIsDone()
        {
            Assert.That(RoundCycle.Next(RoundPhase.Result, RoundEvent.ResultOver, 0, 3), Is.EqualTo(RoundPhase.Briefing));
            Assert.That(RoundCycle.Next(RoundPhase.Result, RoundEvent.ResultOver, 1, 3), Is.EqualTo(RoundPhase.Briefing));
            Assert.That(RoundCycle.Next(RoundPhase.Result, RoundEvent.ResultOver, 2, 3), Is.EqualTo(RoundPhase.CaseEnd));
        }

        [Test]
        public void AnEventAPhaseDoesNotTakeChangesNothing()
        {
            foreach (RoundPhase phase in Enum.GetValues(typeof(RoundPhase)))
            {
                foreach (RoundEvent roundEvent in Enum.GetValues(typeof(RoundEvent)))
                {
                    var next = RoundCycle.Next(phase, roundEvent, 0, 3);
                    var legal = (phase, roundEvent) switch
                    {
                        (RoundPhase.Lobby, RoundEvent.StartCase) => true,
                        (RoundPhase.Briefing, RoundEvent.BriefingOver) => true,
                        (RoundPhase.Live, RoundEvent.TimeUp or RoundEvent.Outcome or RoundEvent.CuffsSpent) => true,
                        (RoundPhase.LastCuff, RoundEvent.ChaseOver or RoundEvent.Outcome) => true,
                        (RoundPhase.Result, RoundEvent.ResultOver) => true,
                        (RoundPhase.CaseEnd, RoundEvent.CaseEndOver) => true,
                        _ => false,
                    };
                    Assert.That(next != phase, Is.EqualTo(legal), $"{phase} on {roundEvent}");
                }
            }
        }

        [Test]
        public void ACaseOfThreeRoundsPlaysThreeLivesAndEnds()
        {
            var phase = RoundPhase.Lobby;
            var round = 0;
            var lives = 0;
            phase = RoundCycle.Next(phase, RoundEvent.StartCase, round, 3);
            while (phase != RoundPhase.CaseEnd)
            {
                phase = RoundCycle.Next(phase, RoundEvent.BriefingOver, round, 3);
                lives += phase == RoundPhase.Live ? 1 : 0;
                phase = RoundCycle.Next(phase, RoundEvent.TimeUp, round, 3);
                var next = RoundCycle.Next(phase, RoundEvent.ResultOver, round, 3);
                if (next == RoundPhase.Briefing)
                {
                    round++;
                }

                phase = next;
            }

            Assert.That(lives, Is.EqualTo(3));
            Assert.That(RoundCycle.Next(phase, RoundEvent.CaseEndOver, round, 3), Is.EqualTo(RoundPhase.Lobby));
        }

        [Test]
        public void TheProgrammeRaisesEachEventOnceInOrder()
        {
            var programme = new Programme(120d, 240d, 300d);
            var raised = Enumerable.Range(0, 3600).SelectMany(tick => programme.Between(tick * 0.1d - 0.1d, tick * 0.1d)).ToArray();
            Assert.That(raised, Is.EqualTo(new[] { FestivalEvent.Opening, FestivalEvent.Concert, FestivalEvent.Fireworks, FestivalEvent.Closing }));
        }

        [Test]
        public void ALongFrameStillRaisesEveryEventItSkipped()
        {
            var programme = new Programme(120d, 240d, 300d);
            Assert.That(programme.Between(-1d, 250d), Is.EqualTo(new[] { FestivalEvent.Opening, FestivalEvent.Concert, FestivalEvent.Fireworks }));
        }

        [Test]
        public void TimeUpGoesToThePolice()
        {
            Assert.That(RoundRules.WinnerOf(RoundOutcome.TimeUp), Is.EqualTo(RoundWinner.Police));
        }
    }
}
