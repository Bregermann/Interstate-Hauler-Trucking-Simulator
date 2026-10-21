using System;
using NUnit.Framework;
public sealed class AvalancheRulesTests
{
 Type type;object r;[SetUp]public void Setup(){type=Type.GetType("LWS.TruckAvalancheEscape.AvalancheRules, Assembly-CSharp",true);r=Activator.CreateInstance(type);Call("Reset",3,2000f,4.5f,300f);Call("Begin");}
 object Call(string n,params object[] a)=>type.GetMethod(n).Invoke(r,a);T Get<T>(string n)=>(T)type.GetProperty(n).GetValue(r);T Field<T>(string n)=>(T)type.GetField(n).GetValue(r);void Go()=>Call("Tick",3f);
 [Test]public void CountdownHoldsRaceAndSnow(){Assert.False((bool)Call("Progress",0,100f));Assert.False((bool)Call("SnowContact",0,5f,true));Call("Tick",2f);Assert.AreEqual("Countdown",Get<object>("Phase").ToString());}
 [Test]public void TwelveCompetitorsAreSupported(){Call("Reset",12,2000f,4.5f,300f);Assert.AreEqual(12,Get<int>("Players"));}
 [Test]public void InitialContactDoesNotInstantlyBury(){Go();Assert.False((bool)Call("SnowContact",0,.2f,true));Assert.True(Field<bool[]>("alive")[0]);}
 [Test]public void ProlongedEngulfmentEliminatesOnce(){Go();Assert.True((bool)Call("SnowContact",0,4.5f,true));Assert.False((bool)Call("SnowContact",0,1f,true));Assert.False(Field<bool[]>("alive")[0]);}
 [Test]public void EscapingSnowReducesBurialExposure(){Go();Call("SnowContact",0,3f,true);Call("SnowContact",0,1f,false);Assert.AreEqual(1,Field<float[]>("caughtFor")[0]);Assert.True(Field<bool[]>("alive")[0]);}
 [Test]public void FinishingLocksOutLaterBurial(){Go();Call("Tick",20f);Assert.True((bool)Call("Finish",0,19f));Assert.False((bool)Call("Eliminate",0,"LATE CONTACT"));Assert.False((bool)Call("SnowContact",0,10f,true));}
 [Test]public void FinishTimesCannotBeFabricatedPastActualClock(){Go();Assert.False((bool)Call("Finish",0,50f));Assert.False((bool)Call("Finish",0,float.NaN));}
 [Test]public void EarlierRealFinisherHasBetterPlacement(){Go();Call("Tick",30f);Call("Finish",1,29f);Call("Finish",0,30f);Assert.AreEqual(1,Call("Place",1));Assert.AreEqual(2,Call("Place",0));}
 [Test]public void EliminatedRankUsesDistanceSurvived(){Go();Call("Progress",0,100f);Call("Progress",1,500f);Call("Eliminate",0,"SNOW");Call("Eliminate",1,"SNOW");Assert.Less((int)Call("Place",1),(int)Call("Place",0));}
 [Test]public void FirstFinishDoesNotEndOtherCompetitors(){Go();Call("Tick",30f);Call("Finish",0,30f);Assert.False(Get<bool>("Complete"));}
 [Test]public void EveryResolvedCompetitorEndsRace(){Go();Call("Tick",30f);Call("Finish",0,30f);Call("Eliminate",1,"SNOW");Call("Eliminate",2,"SNOW");Assert.True(Get<bool>("Complete"));}
 [Test]public void TimerBoundsStalledMatches(){Go();Call("Tick",300f);Assert.True(Get<bool>("Complete"));}
 [Test]public void ProgressIsMonotonicAndCourseBounded(){Go();Call("Progress",0,100f);Call("Progress",0,50f);Assert.AreEqual(100,Field<float[]>("distance")[0]);Call("Progress",0,9999f);Assert.AreEqual(2000,Field<float[]>("distance")[0]);}
 [Test]public void RematchRestoresAllTruckLivesAndExposure(){Go();Call("SnowContact",0,4.5f,true);Call("Reset",3,2000f,4.5f,300f);Assert.True(Field<bool[]>("alive")[0]);Assert.AreEqual(0,Field<float[]>("caughtFor")[0]);}
}
