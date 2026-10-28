using System.Linq;
using NUnit.Framework;
using UnityEngine;
using LWS.TruckYoga;

public sealed class YogaRulesTests
{
    [Test] public void SevenTargetsAreDistinctAndMeasuredAccuracyRejectsWrongPoseOrNonfiniteValues()
    {
        var targets = Enumerable.Range(0, 7).Select(i => YogaPoses.Target((YogaPose)i)).ToArray();
        Assert.That(targets.Distinct().Count(), Is.EqualTo(7));
        for (int i = 0; i < 7; i++)
        {
            Assert.That(YogaPoses.Accuracy(targets[i], (YogaPose)i), Is.EqualTo(1));
            Assert.That(YogaPoses.Accuracy(targets[(i + 1) % 7], (YogaPose)i), Is.LessThan(.86f));
        }
        Assert.That(YogaPoses.Accuracy(new Vector4(float.NaN, 0, 0, 0), YogaPose.CabTwist), Is.Zero);
    }
    [Test] public void WrongAchievedPoseCannotEarnPointsAndMotionBreaksActualHold()
    {
        var rules = new YogaRules(); rules.Begin(1, false); var wrong = YogaPoses.Target(YogaPose.WarriorWheelie);
        for (int i = 0; i < 200; i++) rules.Observe(0, wrong, wrong, .02f);
        Assert.That(rules.score[0], Is.Zero); Assert.That(rules.completed[0], Is.Zero);
        var target = YogaPoses.Target(rules.Pose);
        for (int i = 0; i < 100; i++) rules.Observe(0, target, target, .02f);
        Assert.That(rules.hold[0], Is.GreaterThan(1.9f));
        rules.Observe(0, target, wrong, .02f); Assert.That(rules.hold[0], Is.Zero);
        Assert.That(rules.completed[0], Is.Zero);
        for (int i = 0; i < 151; i++) rules.Observe(0, target, target, .02f);
        Assert.That(rules.completed[0], Is.EqualTo(1)); Assert.That(rules.score[0], Is.GreaterThan(50));
    }
    [TestCase(1)] [TestCase(4)]
    public void EveryParticipantMustAchieveAllSevenAndRematchClearsScores(int count)
    {
        var rules = new YogaRules(); rules.Begin(count, false);
        for (int pose = 0; pose < 7; pose++)
        {
            Assert.That((int)rules.Pose, Is.EqualTo(pose)); var target = YogaPoses.Target(rules.Pose);
            for (int frame = 0; frame < 155; frame++) { for (int slot = 0; slot < count; slot++) rules.Observe(slot, target, target, .02f); rules.Tick(.02f); }
        }
        Assert.That(rules.Finished, Is.True);
        for (int slot = 0; slot < count; slot++) { Assert.That(rules.completed[slot], Is.EqualTo(7)); Assert.That(rules.score[slot], Is.GreaterThan(350)); }
        rules.Begin(count, false); Assert.That(rules.Finished, Is.False); Assert.That(rules.score.All(v => v == 0), Is.True);
    }
    [Test] public void FreeYogaHasNoCompulsoryTimerScoreRankOrWinner()
    {
        var rules = new YogaRules(); rules.Begin(4, true); var target = YogaPoses.Target(rules.Pose);
        for (int frame = 0; frame < 2000; frame++) { for (int slot = 0; slot < 4; slot++) rules.Observe(slot, target, target, .02f); rules.Tick(.02f); }
        Assert.That(rules.PoseSeconds, Is.GreaterThan(39)); Assert.That(rules.Finished, Is.False); Assert.That(rules.Pose, Is.EqualTo(YogaPose.DownwardFacingTruck));
        Assert.That(rules.score.All(v => v == 0), Is.True); Assert.That(rules.Place(0), Is.Zero);
        for (int i = 0; i < 7; i++) rules.NextPose(); Assert.That(rules.Pose, Is.EqualTo(YogaPose.DownwardFacingTruck)); Assert.That(rules.Finished, Is.False);
    }
}
