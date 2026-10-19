using System;
using NUnit.Framework;

namespace LWS.TruckDanceOff.Tests
{
    public sealed class DanceRuleTests
    {
        DanceRules One(double duration = 0, int mask = 1) => new DanceRules(new[] { new DanceNote(1, mask, duration) });
        [TestCase(0, DanceGrade.Perfect)] [TestCase(.04, DanceGrade.Perfect)]
        [TestCase(-.04, DanceGrade.Perfect)] [TestCase(.08, DanceGrade.Great)]
        [TestCase(-.08, DanceGrade.Great)] [TestCase(.14, DanceGrade.Good)]
        [TestCase(-.14, DanceGrade.Good)] [TestCase(.045, DanceGrade.Perfect)]
        [TestCase(.09, DanceGrade.Great)] [TestCase(.16, DanceGrade.Good)]
        public void TimestampDeterminesGrade(double offset, DanceGrade grade)
        { var r = One(); r.Step(1 + offset, 1, 1); Assert.That(r.LastGrade, Is.EqualTo(grade)); Assert.That(r.Resolved, Is.EqualTo(1)); }
        [Test] public void MissingNoteExpiresExactlyOnce()
        { var r = One(); r.Step(1.17, 0, 0); r.Step(4, 0, 0); Assert.That(r.Miss, Is.EqualTo(1)); Assert.That(r.Score, Is.Zero); }
        [Test] public void DuplicatePressCannotScoreTwice()
        { var r = One(); r.Step(1, 1, 1); int score = r.Score; r.Step(1, 1, 1); Assert.That(r.Perfect, Is.EqualTo(1)); Assert.That(r.Resolved, Is.EqualTo(1)); Assert.That(r.Score, Is.LessThanOrEqualTo(score)); }
        [Test] public void SimultaneousChordResolvesOnceWithTwoLaneValue()
        { var r = One(0, 5); r.Step(1, 5, 5); Assert.That(r.Perfect, Is.EqualTo(1)); Assert.That(r.Combo, Is.EqualTo(1)); Assert.That(r.Score, Is.EqualTo(2000)); }
        [Test] public void ChordAllowsSmallInputSpread()
        { var r = One(0, 5); r.Step(.98, 1, 1); Assert.That(r.Resolved, Is.Zero); r.Step(1.02, 4, 5); Assert.That(r.Perfect, Is.EqualTo(1)); }
        [Test] public void WidelySeparatedChordIsMissed()
        { var r = One(0, 5); r.Step(.9, 1, 1); r.Step(1.02, 4, 5); Assert.That(r.Miss, Is.EqualTo(1)); }
        [Test] public void PartialChordDoesNotReceiveFullPoints()
        { var r = One(0, 5); r.Step(1, 1, 1); r.Step(1.17, 0, 1); Assert.That(r.Score, Is.Zero); Assert.That(r.Miss, Is.EqualTo(1)); }
        [Test] public void HoldScoresOnlyAtItsTail()
        { var r = One(1); r.Step(1, 1, 1); Assert.That(r.Score, Is.Zero); r.Step(1.5, 0, 1); r.Step(2, 0, 1); Assert.That(r.Perfect, Is.EqualTo(1)); Assert.That(r.Score, Is.EqualTo(1100)); }
        [Test] public void BrokenHoldCannotScoreOnLaterTail()
        { var r = One(1); r.Step(1, 1, 1); r.Step(1.2, 0, 0); r.Step(1.3, 0, 0); r.Step(2.2, 0, 0); Assert.That(r.Miss, Is.EqualTo(1)); Assert.That(r.Score, Is.Zero); }
        [Test] public void BriefReleaseWithinGraceCanRecover()
        { var r = One(1); r.Step(1, 1, 1); r.Step(1.2, 0, 0); r.Step(1.24, 1, 1); r.Step(2, 0, 1); Assert.That(r.Perfect, Is.EqualTo(1)); Assert.That(r.EmptyPresses, Is.Zero); }
        [Test] public void WrongLaneDoesNotClaimNote()
        { var r = One(); r.Step(1, 2, 2); Assert.That(r.EmptyPresses, Is.EqualTo(1)); Assert.That(r.Resolved, Is.Zero); r.Step(1.01, 1, 3); Assert.That(r.Perfect, Is.EqualTo(1)); }
        [Test] public void ClosestSameLaneNoteOwnsThePress()
        { var r = new DanceRules(new[] { new DanceNote(1, 1), new DanceNote(1.2, 1) }); r.Step(1.18, 1, 1); Assert.That(r.Judgment(0), Is.EqualTo(DanceGrade.Miss)); Assert.That(r.Judgment(1), Is.EqualTo(DanceGrade.Perfect)); }
        [Test] public void ClockCannotRewindScoring()
        { var r = One(); r.Step(1, 1, 1); Assert.Throws<InvalidOperationException>(() => r.Step(.8, 1, 1)); }
        [Test] public void DenseAndSparseFrameUpdatesGiveSameTimedHits()
        {
            var chart = new[] { new DanceNote(1, 1), new DanceNote(2, 4), new DanceNote(3, 8) };
            var dense = new DanceRules(chart); var sparse = new DanceRules(chart);
            for (int i = 0; i <= 300; i++) { double time = i / 100.0; int mask = i == 100 ? 1 : i == 200 ? 4 : i == 300 ? 8 : 0; dense.Step(time, mask, mask); }
            sparse.Step(1, 1, 1); sparse.Step(2, 4, 4); sparse.Step(3, 8, 8);
            Assert.That(dense.Score, Is.EqualTo(sparse.Score)); Assert.That(dense.Combo, Is.EqualTo(sparse.Combo));
        }
        [Test] public void FreestyleRejectsOutOfSectionAndRepeatedTick()
        { var r = One(); Assert.That(r.Freestyle(1, .5, 1, 2, 4), Is.False); Assert.That(r.Freestyle(2, .5, 1, 2, 4)); Assert.That(r.Freestyle(2.01, .5, 4, 2, 4), Is.False); Assert.That(r.FreestyleHits, Is.EqualTo(1)); }
        [Test] public void FreestyleRewardsVarietyInsteadOfOneLaneMashing()
        { var r = One(); Assert.That(r.Freestyle(2, .5, 1, 2, 4)); Assert.That(r.Freestyle(2.25, .5, 1, 2, 4)); Assert.That(r.Freestyle(2.5, .5, 1, 2, 4), Is.False); Assert.That(r.Freestyle(2.75, .5, 4, 2, 4)); }
        [Test] public void InvalidChartsFailClearly()
        { Assert.Throws<ArgumentException>(() => new DanceRules(new[] { new DanceNote(1, 0) })); Assert.Throws<ArgumentException>(() => new DanceRules(new[] { new DanceNote(double.NaN, 1) })); Assert.Throws<ArgumentException>(() => new DanceRules(new[] { new DanceNote(2, 1), new DanceNote(1, 4) })); }
        [Test] public void OriginalChartCannotMutateRunningRound()
        { var chart = new[] { new DanceNote(1, 1) }; var r = new DanceRules(chart); chart[0] = new DanceNote(1, 8); r.Step(1, 1, 1); Assert.That(r.Perfect, Is.EqualTo(1)); }
        [Test] public void SeparateCompetitorsNeverShareScoreOrCombo()
        { var a = One(); var b = One(); a.Step(1, 1, 1); b.Step(1.2, 0, 0); Assert.That(a.Score, Is.EqualTo(1000)); Assert.That(b.Score, Is.Zero); Assert.That(b.Miss, Is.EqualTo(1)); }
    }
}
