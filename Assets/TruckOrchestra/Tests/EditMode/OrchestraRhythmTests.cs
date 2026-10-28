using LWS.TruckDanceOff;
using NUnit.Framework;

namespace LWS.TruckOrchestra.Tests
{
    public sealed class OrchestraRhythmTests
    {
        [Test] public void RealHoldSoundsAtItsHeadButOnlyScoresAtItsTailOnce()
        {
            var rules = new DanceRules(new[] { new DanceNote(1, 1, 1) }); int heads = 0, tails = 0;
            rules.Started += (index, grade) => { Assert.That(index, Is.Zero); Assert.That(grade, Is.EqualTo(DanceGrade.Perfect)); heads++; };
            rules.Judged += (index, grade) => tails++;
            rules.Step(1, 1, 1); Assert.That(heads, Is.EqualTo(1)); Assert.That(tails, Is.Zero); Assert.That(rules.Score, Is.Zero);
            rules.Step(1.3, 0, 1); Assert.That(heads, Is.EqualTo(1)); Assert.That(tails, Is.Zero);
            rules.Step(2, 0, 1); Assert.That(heads, Is.EqualTo(1)); Assert.That(tails, Is.EqualTo(1)); Assert.That(rules.Score, Is.EqualTo(1100));
            rules.Step(2.5, 0, 0); Assert.That(tails, Is.EqualTo(1)); Assert.That(rules.Score, Is.EqualTo(1100));
        }
        [Test] public void ChordSoundsOnlyAfterAllAssignedLanesActuallyArrive()
        {
            var rules = new DanceRules(new[] { new DanceNote(1, 5) }); int heads = 0; rules.Started += (index, grade) => heads++;
            rules.Step(1, 1, 1); Assert.That(heads, Is.Zero); Assert.That(rules.Score, Is.Zero);
            rules.Step(1.02, 4, 5); Assert.That(heads, Is.EqualTo(1)); Assert.That(rules.Score, Is.EqualTo(2000));
            rules.Step(1.1, 0, 0); Assert.That(heads, Is.EqualTo(1));
        }
        [Test] public void MissedNoteCannotSoundOrEarnAnInstrumentPart()
        {
            var rules = new DanceRules(new[] { new DanceNote(1, 2) }); int heads = 0; rules.Started += (index, grade) => heads++;
            rules.Step(2, 0, 0); Assert.That(heads, Is.Zero); Assert.That(rules.Score, Is.Zero); Assert.That(rules.Miss, Is.EqualTo(1));
        }
        [Test] public void ReleasedHoldRetainsOneHeadAndProducesOneRealMissWithoutTailPoints()
        {
            var rules = new DanceRules(new[] { new DanceNote(1, 8, 1) }); int heads = 0; rules.Started += (index, grade) => heads++;
            rules.Step(1, 8, 8); rules.Step(1.2, 0, 0); rules.Step(1.3, 0, 0); rules.Step(2.1, 0, 0);
            Assert.That(heads, Is.EqualTo(1)); Assert.That(rules.Miss, Is.EqualTo(1)); Assert.That(rules.Score, Is.Zero); Assert.That(rules.Resolved, Is.EqualTo(1));
        }
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void EveryParticipantKeepsIndependentRhythmJudgment(int count)
        {
            var all = new DanceRules[count]; for (int i = 0; i < count; i++) all[i] = new DanceRules(new[] { new DanceNote(1, 1 << i) });
            for (int i = 0; i < count; i++) all[i].Step(1, 1 << i, 1 << i);
            for (int i = 0; i < count; i++) { Assert.That(all[i].Score, Is.EqualTo(1000)); Assert.That(all[i].Perfect, Is.EqualTo(1)); }
        }
    }
}
