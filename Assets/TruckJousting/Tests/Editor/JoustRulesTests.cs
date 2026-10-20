using System;using NUnit.Framework;
namespace LWS.TruckSports.Tests
{
 public sealed class JoustRulesTests
 {
  Type type;object rules;object Call(string method,params object[] args)=>type.GetMethod(method).Invoke(rules,args);T Read<T>(string name)=>(T)type.GetProperty(name).GetValue(rules);int[] Points=>(int[])type.GetField("points").GetValue(rules);
  [SetUp]public void Setup(){type=Type.GetType("LWS.TruckJousting.JoustRules, Assembly-CSharp",true);rules=Activator.CreateInstance(type);Call("Reset",3);}
  [Test]public void CountdownCannotScore(){Assert.That((bool)Call("Hit",0,true,20f),Is.False);Assert.That(Points,Is.All.Zero);}
  [TestCase(false,1)][TestCase(true,2)]public void RealQualifyingContactUsesZoneValue(bool central,int expected){Call("Begin");Assert.That((bool)Call("Hit",0,central,12f),Is.True);Assert.That(Points[0],Is.EqualTo(expected));Assert.That(Points[1],Is.Zero);}
  [TestCase(0f)][TestCase(5.99f)][TestCase(float.NaN)][TestCase(float.PositiveInfinity)]public void StationaryGentleAndInvalidContactsCannotScore(float speed){Call("Begin");Assert.That((bool)Call("Hit",0,true,speed),Is.False);}
  [Test]public void SustainedContactCannotScoreTwice(){Call("Begin");Call("Hit",0,true,14f);Assert.That((bool)Call("Hit",0,true,30f),Is.False);Assert.That(Points[0],Is.EqualTo(2));}
  [Test]public void BothActualLancesMayScoreSamePass(){Call("Begin");Call("Hit",0,false,12f);Call("Hit",1,true,12f);Assert.That(Points,Is.EqualTo(new[]{1,2}));}
  [Test]public void QualifyingKnockdownUpgradesRatherThanDuplicatesPoints(){Call("Begin");Call("Hit",0,true,20f);Assert.That((bool)Call("Knockdown",1),Is.True);Assert.That(Points[0],Is.EqualTo(3));Assert.That((bool)Call("Knockdown",1),Is.False);}
  [Test]public void UncreditedRolloverAwardsNothing(){Call("Begin");Assert.That((bool)Call("Knockdown",1),Is.False);Assert.That(Points,Is.All.Zero);}
  [Test]public void PassClosesOnceAndNextPassResetsContactEligibility(){Call("Begin");Call("Hit",0,true,12f);Call("EndPass");Assert.That((bool)Call("EndPass"),Is.False);Assert.That((bool)Call("NextPass"),Is.True);Call("Begin");Assert.That((bool)Call("Hit",0,true,12f),Is.True);Assert.That(Points[0],Is.EqualTo(4));}
  [Test]public void CompleteTournamentLocksScores(){for(int i=0;i<3;i++){Call("Begin");Call("Hit",0,true,12f);Call("EndPass");if(i<2)Call("NextPass");}Assert.That(Read<bool>("Complete"),Is.True);Assert.That(Read<int>("Winner"),Is.Zero);Assert.That((bool)Call("Hit",1,true,30f),Is.False);Assert.That((bool)Call("NextPass"),Is.False);}
  [Test]public void EqualActualScoresProduceSharedPlacing(){for(int i=0;i<3;i++){Call("Begin");Call("EndPass");if(i<2)Call("NextPass");}Assert.That(Read<bool>("Complete"),Is.True);Assert.That(Read<int>("Winner"),Is.EqualTo(-1));}
  [Test]public void RematchRestoresAllContactAndTournamentState(){Call("Begin");Call("Hit",0,true,12f);Call("Reset",5);Assert.That(Points,Is.All.Zero);Assert.That(Read<int>("Pass"),Is.EqualTo(1));Assert.That(Read<bool>("Live"),Is.False);}
 }
}
