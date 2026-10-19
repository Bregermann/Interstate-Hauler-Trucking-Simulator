using System;using NUnit.Framework;
namespace LWS.TruckLimbo.Tests {
public sealed class LimboRuleTests {
 LimboRules Start(int n=4,LimboFormat mode=LimboFormat.Elimination,int rounds=5){var r=new LimboRules();r.Start(n,mode,3,.2f,rounds);return r;}
 [TestCase(1)][TestCase(4)][TestCase(8)]public void SupportedRosters(int count){var r=Start(count);Assert.That(r.Count,Is.EqualTo(count));Assert.That(r.active[count-1]);}
 [TestCase(0)][TestCase(9)]public void InvalidCountRejected(int count){Assert.Throws<ArgumentException>(()=>Start(count));}
 [Test]public void OneAttemptCountsOnce(){var r=Start(1);Assert.That(r.ResolveAttempt(0,true,false,false));Assert.That(r.ResolveAttempt(0,true,true,true),Is.False);Assert.That(r.points[0],Is.EqualTo(100));}
 [Test]public void RoundWaitsForEveryone(){var r=Start(2);r.ResolveAttempt(0,true,false,false);Assert.That(r.RoundComplete,Is.False);Assert.That(r.NextRound(),Is.False);r.ResolveAttempt(1,true,false,false);Assert.That(r.NextRound());Assert.That(r.Round,Is.EqualTo(2));Assert.That(r.Height,Is.EqualTo(2.8f).Within(.0001));}
 [Test]public void FailedTruckEliminatedOnlyBetweenRounds(){var r=Start(3);r.ResolveAttempt(0,false,false,false);Assert.That(r.active[0]);r.ResolveAttempt(1,true,false,false);r.ResolveAttempt(2,true,false,false);r.NextRound();Assert.That(r.active[0],Is.False);Assert.That(r.ResolveAttempt(0,true,true,true),Is.False);}
 [Test]public void SoleSurvivorWins(){var r=Start(2);r.ResolveAttempt(0,false,false,false);r.ResolveAttempt(1,true,false,false);Assert.That(r.NextRound(),Is.False);Assert.That(r.Complete);Assert.That(r.winners[1]);Assert.That(r.winners[0],Is.False);}
 [Test]public void SoloContinuesAfterClearAndEndsAfterFailure(){var r=Start(1);r.ResolveAttempt(0,true,false,false);Assert.That(r.NextRound());r.ResolveAttempt(0,false,false,false);Assert.That(r.NextRound(),Is.False);Assert.That(r.Complete);Assert.That(r.best[0],Is.EqualTo(3));}
 [Test]public void AllFailedOpeningRoundIsTie(){var r=Start(2);r.ResolveAttempt(0,false,false,false);r.ResolveAttempt(1,false,false,false);r.NextRound();Assert.That(r.winners[0]&&r.winners[1]);}
 [Test]public void BestHeightKeepsFailedCompetitors(){var r=Start(2,LimboFormat.BestHeight);r.ResolveAttempt(0,false,false,false);r.ResolveAttempt(1,true,false,false);Assert.That(r.NextRound());Assert.That(r.active[0]&&r.active[1]);}
 [Test]public void LowerClearanceWinsOverEarlierHighScore(){var r=Start(2,LimboFormat.BestHeight,2);r.ResolveAttempt(0,true,true,true);r.ResolveAttempt(1,true,false,false);r.NextRound();r.ResolveAttempt(0,false,false,false);r.ResolveAttempt(1,true,false,false);r.NextRound();Assert.That(r.winners[1]);Assert.That(r.winners[0],Is.False);}
 [Test]public void TiedBestHeightsUseExplainedStylePoints(){var r=Start(2,LimboFormat.BestHeight,1);r.ResolveAttempt(0,true,true,false);r.ResolveAttempt(1,true,false,false);r.NextRound();Assert.That(r.points[0],Is.EqualTo(125));Assert.That(r.winners[0]);}
 [Test]public void AllDoneCannotFinalizeTwice(){var r=Start(1,LimboFormat.BestHeight,1);r.ResolveAttempt(0,true,false,false);r.NextRound();int before=r.points[0];Assert.That(r.NextRound(),Is.False);Assert.That(r.ResolveAttempt(0,true,true,true),Is.False);Assert.That(r.points[0],Is.EqualTo(before));}
 [Test]public void HighSpeedOrderedEntryDetected(){Assert.That(LimboRules.ValidEntry(-3,4,0,-1,1,-3,3,true));}
 [TestCase(false,false,0)][TestCase(true,true,0)][TestCase(true,false,.1f)]public void InvalidClearanceRejected(bool entered,bool hit,float displacement){Assert.That(LimboRules.FullClearance(4,0,entered,hit,displacement),Is.False);}
 [Test]public void EntireRearMustClear(){Assert.That(LimboRules.FullClearance(-.02f,0,true,false,0),Is.False);Assert.That(LimboRules.FullClearance(.1f,0,true,false,0));}
 [Test]public void SideBypassAndAirborneEntryRejected(){Assert.That(LimboRules.ValidEntry(-1,1,0,-5,-3,-3,3,true),Is.False);Assert.That(LimboRules.ValidEntry(-1,1,0,-1,1,-3,3,false),Is.False);}
 [Test]public void ResetClearsEveryPreviousResult(){var r=Start(2);r.ResolveAttempt(0,true,true,true);r.ResolveAttempt(1,false,false,false);r.NextRound();r.Start(1,LimboFormat.Elimination,2,.1f,5);Assert.That(r.Round,Is.EqualTo(1));Assert.That(r.points[0],Is.Zero);Assert.That(r.done[0]||r.winners[0]||r.Complete,Is.False);}
}
}
