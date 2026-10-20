using NUnit.Framework;using System;
public sealed class FreezeRulesTests
{
 Rules r;[SetUp]public void Setup(){r=new Rules();r.Reset(4,4);r.Begin();}
 void Freeze(int victim){for(int i=0;i<3;i++)r.Hit(0,victim);}
 [Test]public void AsymmetricFourVersusFour(){Assert.AreEqual(8,r.Players);Assert.AreEqual(0,r.Freezers);Assert.AreEqual(1,r.Team(7));}
 [Test]public void RunnersCannotShoot(){Assert.False(r.Hit(4,0));Assert.AreEqual(0,r.buildup[0]);}
 [Test]public void FriendlyShotsDoNothing(){Assert.False(r.Hit(0,1));}
 [Test]public void FreezeBuildsOverThreeRealHits(){r.Hit(0,4);Assert.False(r.frozen[4]);r.Hit(0,4);Assert.False(r.frozen[4]);r.Hit(0,4);Assert.True(r.frozen[4]);Assert.AreEqual(1,r.tags[0]);}
 [Test]public void NoAutomaticThaw(){Freeze(4);r.Tick(60);Assert.True(r.frozen[4]);Assert.AreEqual(100,r.buildup[4]);}
 [Test]public void RepeatedFrozenHitCannotDuplicateCredit(){Freeze(4);Assert.False(r.Hit(0,4));Assert.AreEqual(1,r.tags[0]);}
 [Test]public void PhysicalTeammateRescueRestoresControl(){Freeze(4);Assert.True(r.Rescue(5,4,3));Assert.False(r.frozen[4]);Assert.AreEqual(0,r.buildup[4]);Assert.AreEqual(1,r.rescues[5]);}
 [Test]public void RestingOverlapCannotRescue(){Freeze(4);Assert.False(r.Rescue(5,4,.5f));Assert.True(r.frozen[4]);}
 [Test]public void OpponentCannotRescue(){Freeze(4);Assert.False(r.Rescue(0,4,10));}
 [Test]public void FrozenHelperCannotRescue(){Freeze(4);Freeze(5);Assert.False(r.Rescue(5,4,10));}
 [Test]public void RescueProtectionPreventsInstantRefreeze(){Freeze(4);r.Rescue(5,4,3);Assert.False(r.Hit(0,4));r.Tick(2.1f);Assert.True(r.Hit(0,4));}
 [Test]public void AllRunnersFrozenEndsHeat(){for(int v=4;v<8;v++)Freeze(v);Assert.False(r.Live);Assert.AreEqual(0,r.RoundWinner);Assert.AreEqual(1,r.wins[0]);}
 [Test]public void SurvivingRunnerWinsAtTimer(){for(int v=4;v<7;v++)Freeze(v);r.Tick(90);Assert.AreEqual(1,r.RoundWinner);}
 [Test]public void RolesReverseOnNextRound(){r.Tick(90);Assert.True(r.NextRound());r.Begin();Assert.AreEqual(1,r.Freezers);Assert.False(r.Hit(0,4));Assert.True(r.Hit(4,0));}
 [Test]public void CompleteFourRoundsCannotAwardLatePoints(){for(int i=0;i<4;i++){r.Tick(90);if(i<3){r.NextRound();r.Begin();}}Assert.True(r.Complete);Assert.False(r.NextRound());Assert.False(r.Hit(0,4));Assert.AreEqual(2,r.wins[0]);Assert.AreEqual(2,r.wins[1]);Assert.AreEqual(-1,r.Winner);}
 [Test]public void RematchClearsFrozenAndStatistics(){Freeze(4);r.Reset(4,4);Assert.False(r.frozen[4]);Assert.AreEqual(0,r.tags[0]);Assert.AreEqual(0,r.Round);Assert.False(r.Live);}
 [Test]public void CountdownDoesNotAcceptHits(){r.Reset(4,4);Assert.False(r.Hit(0,4));Assert.False(r.Rescue(5,4,5));}

 sealed class Rules
 {
  readonly Type type=Type.GetType("LWS.TruckFreezeTag.FreezeRules, Assembly-CSharp",true);readonly object value;public Rules(){value=Activator.CreateInstance(type);}object Call(string name,params object[] args)=>type.GetMethod(name).Invoke(value,args);T Read<T>(string name)=>(T)type.GetProperty(name).GetValue(value);T Field<T>(string name)=>(T)type.GetField(name).GetValue(value);
  public int Players=>Read<int>("Players");public int Freezers=>Read<int>("Freezers");public int Round=>Read<int>("Round");public int RoundWinner=>Read<int>("RoundWinner");public int Winner=>Read<int>("Winner");public bool Live=>Read<bool>("Live");public bool Complete=>Read<bool>("Complete");public bool[] frozen=>Field<bool[]>("frozen");public float[] buildup=>Field<float[]>("buildup");public int[] tags=>Field<int[]>("tags");public int[] rescues=>Field<int[]>("rescues");public int[] wins=>Field<int[]>("wins");
  public void Reset(int a,int b)=>Call("Reset",a,b,4,90f);public void Begin()=>Call("Begin");public int Team(int player)=>(int)Call("Team",player);public void Tick(float dt)=>Call("Tick",dt);public bool Hit(int attacker,int victim)=>(bool)Call("Hit",attacker,victim,35f);public bool Rescue(int helper,int victim,float speed)=>(bool)Call("Rescue",helper,victim,speed);public bool NextRound()=>(bool)Call("NextRound");
 }
}
