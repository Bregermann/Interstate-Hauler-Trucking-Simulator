using System;
using NUnit.Framework;
using LWS.TruckBallet;
public sealed class BalletRulesTests
{
    [Test]public void TimersAndWrongPosesNeverManufactureAnyOfEightCompletions()
    {
        var rules=new BalletRules();rules.Begin(4);int changes=0;
        while(!rules.Finished){for(int i=0;i<4;i++)rules.Observe(i,default,default,.02f);if(rules.Tick(.02f))changes++;}
        Assert.That(changes,Is.EqualTo(8));CollectionAssert.AreEqual(new[]{0,0,0,0},rules.completed);CollectionAssert.AreEqual(new[]{0f,0f,0f,0f},rules.score);
    }
    [Test]public void PirouetteNeedsMeasuredNetFullTurnAndSteadyActualPoseRatherThanOscillationOrElapsedTime()
    {
        var rules=new BalletRules();rules.Begin(1);var goal=BalletMoves.Goal(BalletMove.Pirouette);var previous=goal;
        for(int i=0;i<300;i++){var pose=goal;pose.heading=(i%40-20)*.5f;rules.Observe(0,pose,previous,.02f);rules.Tick(.02f);previous=pose;}
        Assert.That(rules.completed[0],Is.Zero);Assert.That(rules.Move,Is.EqualTo(BalletMove.Pirouette));
        for(int i=0;i<400&&rules.completed[0]==0;i++){var pose=goal;pose.heading=previous.heading+2;rules.Observe(0,pose,previous,.02f);rules.Tick(.02f);previous=pose;}
        Assert.That(rules.completed[0],Is.EqualTo(1));Assert.That(rules.score[0],Is.GreaterThan(25));Assert.That(rules.Move,Is.EqualTo(BalletMove.SuspensionPlie));
    }
    [Test]public void SmallHopRequiresObservedRiseThenControlledReturnAndStabilityChangesScore()
    {
        var rules=new BalletRules();rules.Begin(1);while(rules.Move!=BalletMove.SmallHop)rules.Tick(.2f);
        for(int i=0;i<30;i++){rules.Observe(0,default,default,.02f);rules.Tick(.02f);}Assert.That(rules.rose[0],Is.False);Assert.That(rules.completed[0],Is.Zero);
        var peak=BalletMoves.Goal(BalletMove.SmallHop);rules.Observe(0,peak,peak,.02f);Assert.That(rules.rose[0],Is.True);Assert.That(rules.Goal(0).lift,Is.Zero);
        for(int i=0;i<100;i++)rules.Observe(0,default,default,.02f);Assert.That(rules.completed[0],Is.EqualTo(1));
        Assert.That(BalletMoves.Stability(default,new BalletPose(20,0,0),.02f),Is.Zero);Assert.That(BalletMoves.Stability(default,default,.02f),Is.EqualTo(1));Assert.That(BalletMoves.Accuracy(new BalletPose(float.NaN,0,0),default),Is.Zero);Assert.That(rules.Place(0),Is.EqualTo(1));
    }
    [Test]public void IdenticalObservedPerformancesRemainTiedAndEveryMoveHasDistinctTargets()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BalletRules().Begin(0));var rules=new BalletRules();rules.Begin(2);for(int i=0;i<20;i++){var pose=BalletMoves.Goal(BalletMove.Pirouette);rules.Observe(0,pose,pose,.02f);rules.Observe(1,pose,pose,.02f);rules.Tick(.02f);}Assert.That(rules.Place(0),Is.EqualTo(1));Assert.That(rules.Place(1),Is.EqualTo(1));Assert.That(BalletMoves.Names.Length,Is.EqualTo(8));Assert.That(BalletMoves.RequiredTurn(BalletMove.Pirouette),Is.EqualTo(360));Assert.That(BalletMoves.RequiredTurn(BalletMove.ControlledSpin),Is.EqualTo(180));Assert.That(BalletMoves.RequiredTurn(BalletMove.FinalFlourish),Is.EqualTo(90));
    }
}
