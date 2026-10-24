using System;
using NUnit.Framework;
namespace LWS.TruckLimbo.Tests {
public sealed class LimboResultRuleTests {
 [Test] public void LowerBarSharedWinnersPrecedeLargerEarlierScoreWithoutMutatingResults(){
  var r=new LimboRules();r.Start(3,LimboFormat.BestHeight,3,.2f,3);
  for(int round=0;round<3;round++){
   r.ResolveAttempt(0,round<2,true,true);r.ResolveAttempt(1,round==2,false,false);r.ResolveAttempt(2,round==2,false,false);r.NextRound();
  }
  var points=(int[])r.points.Clone();var best=(float[])r.best.Clone();var winners=(bool[])r.winners.Clone();
  Assert.That(r.Complete);Assert.That(r.points[0],Is.GreaterThan(r.points[1]));
  CollectionAssert.AreEqual(new[]{3,1,1},new[]{r.ResultPlace(0),r.ResultPlace(1),r.ResultPlace(2)});
  CollectionAssert.AreEqual(points,r.points);CollectionAssert.AreEqual(best,r.best);CollectionAssert.AreEqual(winners,r.winners);
 }
 [Test] public void NoClearanceKeepsAllOriginalSharedWinners(){
  var r=new LimboRules();r.Start(3,LimboFormat.BestHeight,3,.2f,1);
  for(int i=0;i<3;i++)r.ResolveAttempt(i,false,false,false);r.NextRound();
  for(int i=0;i<3;i++){Assert.That(float.IsPositiveInfinity(r.best[i]));Assert.That(r.winners[i]);Assert.That(r.ResultPlace(i),Is.EqualTo(1));}
 }
 [Test] public void EqualHeightsUseExistingStylePointsAndFailedAttemptsStayLast(){
  var r=new LimboRules();r.Start(3,LimboFormat.BestHeight,3,.2f,1);
  r.ResolveAttempt(0,true,true,false);r.ResolveAttempt(1,true,false,false);r.ResolveAttempt(2,false,false,false);r.NextRound();
  CollectionAssert.AreEqual(new[]{1,2,3},new[]{r.ResultPlace(0),r.ResultPlace(1),r.ResultPlace(2)});
 }
 [Test] public void InvalidResultSlotsAreRejected(){
  var r=new LimboRules();r.Start(2,LimboFormat.Elimination,3,.2f,3);
  Assert.Throws<ArgumentOutOfRangeException>(()=>r.ResultPlace(-1));Assert.Throws<ArgumentOutOfRangeException>(()=>r.ResultPlace(2));
 }
}
}
