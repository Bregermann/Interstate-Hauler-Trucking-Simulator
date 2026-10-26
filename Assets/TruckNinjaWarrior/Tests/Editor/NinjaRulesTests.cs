using System;
using NUnit.Framework;
public sealed class NinjaRulesTests
{
 Type type;object r;[SetUp]public void Setup(){type=Type.GetType("LWS.TruckNinjaWarrior.NinjaRules, Assembly-CSharp",true);r=Activator.CreateInstance(type);Call("Reset",2,6,180f);Call("Begin");}
 object Call(string n,params object[] a)=>type.GetMethod(n).Invoke(r,a);T Get<T>(string n)=>(T)type.GetProperty(n).GetValue(r);T Field<T>(string n)=>(T)type.GetField(n).GetValue(r);void Go()=>Call("Tick",3f);void Clear(int actor){for(int i=1;i<=6;i++)Assert.True((bool)Call("Clear",actor,i));}
 [Test]public void CountdownPreventsEarlyCheckpoint(){Assert.False((bool)Call("Clear",0,1));Assert.False((bool)Call("Bell",0));}
 [Test]public void OnlyActiveParticipantCanClearObstacle(){Go();Assert.False((bool)Call("Clear",1,1));Assert.True((bool)Call("Clear",0,1));}
 [Test]public void CheckpointsCannotBeSkipped(){Go();Assert.False((bool)Call("Clear",0,2));Assert.AreEqual(0,Get<int>("Checkpoint"));}
 [Test]public void DuplicateCheckpointDoesNotAdvance(){Go();Call("Clear",0,1);Assert.False((bool)Call("Clear",0,1));Assert.AreEqual(1,Get<int>("Checkpoint"));}
 [Test]public void FallRestartsActualAttemptWithoutTimePenalty(){Go();Call("Tick",30f);Call("Fall",0);Assert.AreEqual(0,Get<float>("Penalty"));Assert.AreEqual(0,Get<float>("Clock"));Assert.AreEqual(0,Get<float>("StateClock"));Assert.AreEqual("Countdown",type.GetProperty("Phase").GetValue(r).ToString());Assert.AreEqual(1,Field<int[]>("falls")[0]);}
 [Test]public void FallClearsLatestCheckpointAndRestartsSameAthlete(){Go();Call("Clear",0,1);Call("Clear",0,2);Call("Fall",0);Assert.AreEqual(0,Get<int>("Checkpoint"));Assert.AreEqual(0,Field<int[]>("cleared")[0]);Assert.AreEqual(0,Get<int>("Active"));}
 [Test]public void InactiveTruckCannotReceiveFallPenalty(){Go();Assert.False((bool)Call("Fall",1));Assert.AreEqual(0,Field<int[]>("falls")[1]);}
 [Test]public void BellRequiresEveryObstacle(){Go();Assert.False((bool)Call("Bell",0));Clear(0);Assert.True((bool)Call("Bell",0));}
 [Test]public void BellRecordsOnlyTheActualCompletedAttemptAfterARestart(){Go();Call("Tick",30f);Clear(0);Call("Fall",0);Assert.False((bool)Call("Bell",0));Go();Call("Tick",12f);Clear(0);Call("Bell",0);Assert.AreEqual(12,Field<float[]>("adjustedTime")[0]);Assert.AreEqual(0,Get<float>("Penalty"));}
 [Test]public void CompletedAttemptCannotBeScoredAgain(){Go();Clear(0);Call("Bell",0);Assert.False((bool)Call("Bell",0));Assert.False((bool)Call("Fall",0));}
 [Test]public void NextDriverGetsFreshCourseState(){Go();Clear(0);Call("Bell",0);Call("Tick",3f);Assert.AreEqual(1,Get<int>("Active"));Assert.AreEqual(0,Get<int>("Checkpoint"));Assert.AreEqual(0,Get<float>("Penalty"));}
 [Test]public void TimeoutEndsAttemptWithoutInventedFinish(){Go();Call("Tick",180f);Assert.False(Field<bool[]>("finished")[0]);Assert.True(float.IsPositiveInfinity(Field<float[]>("adjustedTime")[0]));}
 [Test]public void AllDriversReceiveActualAttempts(){Go();Clear(0);Call("Bell",0);Call("Tick",3f);Go();Clear(1);Call("Bell",1);Call("Tick",3f);Assert.True(Get<bool>("Complete"));}
 [Test]public void EqualTimeAndFallsSharePlacing(){Go();Call("Tick",30f);Clear(0);Call("Bell",0);Call("Tick",3f);Go();Call("Tick",30f);Clear(1);Call("Bell",1);Assert.AreEqual(Call("Place",0),Call("Place",1));}
 [Test]public void RematchClearsRecordsAndFalls(){Go();Call("Fall",0);Call("Reset",2,6,180f);Assert.AreEqual(0,Field<int[]>("falls")[0]);Assert.False(Field<bool[]>("finished")[0]);}
}
