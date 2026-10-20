using NUnit.Framework;using System;
public sealed class VolleyRulesTests
{
 Rules r;[SetUp]public void Setup(){r=new Rules();r.Reset();r.Serve();}
 [Test]public void DefaultIsFifteenWinByTwo(){Assert.AreEqual(15,r.Target);Assert.AreEqual(21,r.Cap);}
 [Test]public void LegalLandingScoresOppositeCourt(){Assert.True(r.Land(0));Assert.AreEqual(1,r.points[1]);Assert.AreEqual(1,r.Server);}
 [Test]public void RepeatedLandingCannotScoreAgain(){r.Land(0);Assert.False(r.Land(0));Assert.AreEqual(1,r.points[1]);}
 [Test]public void OutPenalizesLastActualTouch(){r.Touch(1,3,0,false,false);r.Out();Assert.AreEqual(1,r.points[0]);}
 [Test]public void ServeOutPenalizesServer(){r.Out();Assert.AreEqual(1,r.points[1]);}
 [Test]public void ThreeTouchesAllowedFourthFaults(){for(int i=0;i<3;i++)Assert.True(r.Touch(0,i,i,false,false));Assert.False(r.Touch(0,3,4,false,false));Assert.AreEqual(1,r.points[1]);}
 [Test]public void ConsecutivePlayerCannotJuggle(){r.Touch(0,0,0,false,false);Assert.False(r.Touch(0,0,1,false,false));Assert.AreEqual(1,r.points[1]);}
 [Test]public void CompoundContactDoesNotBecomeDoubleTouch(){r.Touch(0,0,0,false,false);Assert.False(r.Touch(0,0,.1f,false,false));Assert.True(r.Live);Assert.AreEqual(1,r.Touches);}
 [Test]public void CrossingResetsTouchLimit(){r.Touch(0,0,0,false,false);r.Cross(0,1);Assert.AreEqual(0,r.Touches);Assert.True(r.Touch(1,4,1,false,false));}
 [Test]public void ThreeTouchCrossRecordsTeamPlay(){for(int i=0;i<3;i++)r.Touch(0,i,i,false,false);r.Cross(0,1);Assert.AreEqual(1,r.teamPlays[0]);}
 [Test]public void ContactNeverGivesPoint(){r.Touch(0,0,0,true,true);Assert.AreEqual(0,r.points[0]+r.points[1]);Assert.AreEqual(1,r.spikes[0]);Assert.AreEqual(1,r.saves[0]);}
 [Test]public void CompleteMatchLocksWinner(){for(int i=0;i<15;i++){r.Land(1);if(i<14)r.Serve();}Assert.True(r.Complete);Assert.AreEqual(0,r.Winner);Assert.False(r.Serve());Assert.False(r.Land(0));}
 [Test]public void DeuceRequiresTwoPointMargin(){for(int i=0;i<14;i++){r.Land(1);r.Serve();r.Land(0);r.Serve();}r.Land(1);Assert.False(r.Complete);r.Serve();r.Land(1);Assert.True(r.Complete);Assert.AreEqual(16,r.points[0]);}
 [Test]public void PracticalCapEndsAlternatingDeuce(){for(int i=0;i<20;i++){r.Land(1);r.Serve();r.Land(0);r.Serve();}r.Land(1);Assert.True(r.Complete);Assert.AreEqual(21,r.points[0]);}
 [Test]public void ResetRestoresServeAndStatistics(){r.Touch(0,0,0,true,true);r.Land(1);r.Reset();Assert.AreEqual(0,r.points[0]);Assert.AreEqual(0,r.spikes[0]);Assert.False(r.Live);Assert.AreEqual(-1,r.Winner);}

 // Test the actual predefined runtime assembly without changing production assembly boundaries.
 sealed class Rules
 {
  readonly Type type=Type.GetType("LWS.TruckVolleyball.VolleyRules, Assembly-CSharp",true);readonly object value;
  public Rules(){value=Activator.CreateInstance(type);}object Call(string name,params object[] args)=>type.GetMethod(name).Invoke(value,args);T Read<T>(string name)=>(T)type.GetProperty(name).GetValue(value);int[] Field(string name)=>(int[])type.GetField(name).GetValue(value);
  public int Target=>Read<int>("Target");public int Cap=>Read<int>("Cap");public int Server=>Read<int>("Server");public int Winner=>Read<int>("Winner");public int Touches=>Read<int>("Touches");public bool Live=>Read<bool>("Live");public bool Complete=>Read<bool>("Complete");public int[] points=>Field("points");public int[] spikes=>Field("spikes");public int[] saves=>Field("saves");public int[] teamPlays=>Field("teamPlays");
  public void Reset(int target=15,int cap=21)=>Call("Reset",target,cap);public bool Serve()=>(bool)Call("Serve");public bool Touch(int team,int player,float time,bool air,bool save)=>(bool)Call("Touch",team,player,time,air,save);public void Cross(int from,int to)=>Call("Cross",from,to);public bool Land(int court)=>(bool)Call("Land",court);public bool Out()=>(bool)Call("Out");
 }
}
