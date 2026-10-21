using System;
using NUnit.Framework;
public sealed class FrogRulesTests
{
 Rules r;[SetUp]public void Setup(){r=new Rules();r.Reset(2,1,123,180);r.Begin();}
 [Test]public void EveryChallengeIsIndependentlySelectable(){for(int option=1;option<=5;option++){r.Reset(2,option,10,180);r.Begin();Assert.AreEqual(option-1,r.Course);}}
 [Test]public void CountDownPreventsPrematureFinish(){Assert.False(r.Finish(0,1));r.Tick(3);Assert.AreEqual("Driving",r.Phase);}
 [Test]public void OneLifeLocksTheAttempt(){r.Tick(4);Assert.True(r.Fail(0,"SOLID TRAFFIC IMPACT"));Assert.False(r.Finish(0,.5f));Assert.False(r.Fail(0,"DUPLICATE"));}
 [Test]public void OnlyActiveCompetitorCanFinish(){r.Tick(5);Assert.False(r.Finish(1,1));Assert.True(r.Finish(0,1));}
 [Test]public void TimingCannotBeInventedBeyondActualRunClock(){r.Tick(4);Assert.False(r.Finish(0,50));Assert.False(r.Finish(0,-1));Assert.False(r.Finish(0,float.NaN));}
 [Test]public void FasterSuccessfulRunGetsExactlyOnePoint(){Run(0,10);Run(1,8);Assert.AreEqual("Complete",r.Phase);Assert.AreEqual(0,r.points[0]);Assert.AreEqual(1,r.points[1]);Assert.AreEqual(1,r.Winner);}
 [Test]public void FailedAttemptNeverEarnsPoint(){r.Tick(4);r.Fail(0,"WATER");r.Tick(2.5f);Run(1,20);Assert.AreEqual(0,r.points[0]);Assert.AreEqual(1,r.points[1]);}
 [Test]public void CourseSeedIsEquivalentForAllAlternatingCompetitors(){int seed=r.CourseSeed;Run(0,10);Assert.AreEqual(seed,r.CourseSeed);}
 [Test]public void AllFailedAttemptsEnterAnnouncedPhysicalTiebreak(){for(int i=0;i<2;i++){r.Tick(4);r.Fail(r.Active,"CRASH");r.Tick(2.5f);}Assert.True(r.Tiebreak);Assert.AreEqual("Countdown",r.Phase);Assert.AreEqual(0,r.points[0]+r.points[1]);}
 [Test]public void ExactFastestTieDoesNotChooseRosterOrder(){Run(0,10);Run(1,10);Assert.True(r.Tiebreak);Assert.AreEqual(-1,r.Winner);Assert.AreEqual(0,r.points[0]+r.points[1]);}
 [Test]public void ChallengeTieIsResolvedByActualAdditionalRuns(){Run(0,10);Run(1,10);Run(0,11);Run(1,9);Assert.AreEqual("Complete",r.Phase);Assert.AreEqual(1,r.Winner);Assert.AreEqual(1,r.points[1]);}
 [Test]public void TimerExpirationEndsOneAttemptWithoutRespawn(){r.Tick(183);Assert.AreEqual("TurnResult",r.Phase);Assert.AreEqual(0,r.points[0]);r.Tick(2.5f);Assert.AreEqual(1,r.Active);}
 [Test]public void RematchClearsAllRecordedTimesAndScores(){Run(0,10);Run(1,9);r.Reset(2,1,12,180);Assert.AreEqual(0,r.points[0]+r.points[1]);Assert.AreEqual("Setup",r.Phase);Assert.AreEqual(-1,r.Winner);}
 [Test]public void EarlierWholeBodyCrossingLocksOutLaterSolidImpact(){Assert.True(Order(.02f,.03f));Assert.False(Order(.03f,.02f));Assert.False(Order(.02f,.02f));}
 [Test]public void MissingCrossingCannotWinFromProximity(){Assert.False(Order(float.PositiveInfinity,.02f));Assert.True(Order(.02f,float.PositiveInfinity));}
 [Test]public void ChampionshipAdvancesThroughFiveSeparateChallenges(){r.Reset(1,0,123,180);r.Begin();for(int c=0;c<5;c++){Assert.AreEqual(c,r.Course);Run(0,10+c);}Assert.AreEqual("Complete",r.Phase);Assert.AreEqual(5,r.points[0]);}
 [TestCase(-40,28,10,true)]
 [TestCase(-40,0,10,true)]
 [TestCase(-40,60,10,false)]
 [TestCase(-100,28,-10,false)]
 public void TrafficPredictionIncludesReturnLaneButNotUnrelatedRoads(float x,float z,float velocity,bool expected){var method=Type.GetType("LWS.TruckFrogger.FrogTraffic, Assembly-CSharp",true).GetMethod("PredictedCrossingOverlap");Assert.AreEqual(expected,(bool)method.Invoke(null,new object[]{new UnityEngine.Vector3(x,0,165+z),new UnityEngine.Vector3(velocity,0,0),0f,158f,199f,6f}));}
 void Run(int slot,float seconds){Assert.AreEqual(slot,r.Active);r.Tick(3+seconds);Assert.True(r.Finish(slot,seconds));r.Tick(2.5f);}
 static bool Order(float finish,float impact)=>(bool)Type.GetType("LWS.TruckFrogger.FrogEventOrder, Assembly-CSharp",true).GetMethod("FinishWins").Invoke(null,new object[]{finish,impact});
 sealed class Rules
 {
  readonly Type type=Type.GetType("LWS.TruckFrogger.FrogRules, Assembly-CSharp",true);readonly object data;public Rules(){data=Activator.CreateInstance(type);}object Call(string n,params object[] args)=>type.GetMethod(n).Invoke(data,args);T Get<T>(string n)=>(T)type.GetProperty(n).GetValue(data);public string Phase=>Get<object>("Phase").ToString();public int Active=>Get<int>("Active");public int Course=>Get<int>("Course");public int CourseSeed=>Get<int>("CourseSeed");public int Winner=>Get<int>("Winner");public bool Tiebreak=>Get<bool>("Tiebreak");public int[] points=>(int[])type.GetField("points").GetValue(data);public void Reset(int n,int option,int seed,float limit)=>Call("Reset",n,option,seed,limit);public void Begin()=>Call("Begin");public void Tick(float dt)=>Call("Tick",dt);public bool Finish(int actor,float t)=>(bool)Call("Finish",actor,t);public bool Fail(int actor,string reason)=>(bool)Call("Fail",actor,reason);
 }
}
