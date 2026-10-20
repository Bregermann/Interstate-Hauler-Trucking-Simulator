using System;using NUnit.Framework;
namespace LWS.TruckSports.Tests
{
 public sealed class MagnetRulesTests
 {
  Type type;object rules;object Call(string method,params object[] args)=>type.GetMethod(method).Invoke(rules,args);T Read<T>(string name)=>(T)type.GetProperty(name).GetValue(rules);int[] Points=>(int[])type.GetField("points").GetValue(rules);
  [SetUp]public void Setup(){type=Type.GetType("LWS.TruckMagnetMayhem.MagnetRules, Assembly-CSharp",true);rules=Activator.CreateInstance(type);Call("Reset",4,new[]{1,3,5},180f,true);}
  bool Observe(int item=0,int bay=0,bool contained=true,float speed=0,float spin=0,float dt=.5f)=>(bool)Call("Observe",item,bay,contained,speed,spin,dt);
  [Test]public void CountdownCannotScore(){Assert.That(Observe(dt:2),Is.False);Assert.That(Points,Is.All.Zero);}
  [Test]public void EntryWithoutSettlingDoesNotScore(){Call("Begin");Assert.That(Observe(),Is.False);Assert.That(Points,Is.All.Zero);}
  [TestCase(.8f,0f)][TestCase(0f,.7f)]public void MovingOrSpinningObjectsCannotScore(float speed,float spin){Call("Begin");Assert.That(Observe(speed:speed,spin:spin,dt:2),Is.False);}
  [Test]public void WholeObjectMustFitInsideBay(){Call("Begin");Assert.That(Observe(contained:false,dt:2),Is.False);}
  [Test]public void SettledEntryAwardsItsActualValueOnce(){Call("Begin");Assert.That(Observe(item:2,dt:2),Is.True);Assert.That(Points[0],Is.EqualTo(5));Assert.That(Observe(item:2,dt:2),Is.False);}
  [Test]public void EnteringDifferentBayRestartsSettleClock(){Call("Begin");Observe(dt:1);Assert.That(Observe(bay:1,dt:1),Is.False);Assert.That(Observe(bay:1,dt:.5f),Is.True);Assert.That(Points[0],Is.Zero);Assert.That(Points[1],Is.EqualTo(1));}
  [Test]public void LeavingAndReturningCannotReusePartialSettle(){Call("Begin");Observe(dt:1);Observe(contained:false);Assert.That(Observe(dt:1),Is.False);}
  [Test]public void TimerExpiryRejectsLateDeposits(){Call("Begin");Call("Tick",180f);Assert.That(Read<bool>("Complete"),Is.True);Assert.That(Observe(dt:2),Is.False);}
  [Test]public void FinalFiniteDepositCompletesOnlyOnce(){Call("Begin");for(int i=0;i<3;i++)Assert.That(Observe(item:i,dt:2),Is.True);Assert.That(Read<int>("Remaining"),Is.Zero);Assert.That(Read<bool>("Complete"),Is.True);Assert.That(Points[0],Is.EqualTo(9));}
  [Test]public void IdenticalTotalsSharePlaceIndependentOfNames(){Call("Begin");Observe(bay:0,dt:2);Assert.That((int)Call("Place",0),Is.EqualTo(1));Assert.That((int)Call("Place",1),Is.EqualTo(2));Assert.That((int)Call("Place",2),Is.EqualTo(2));}
  [Test]public void RematchRestoresEveryObjectAndScore(){Call("Begin");Observe(dt:2);Call("Reset",2,new[]{5,3},180f,true);Assert.That(Points,Is.All.Zero);Assert.That(Read<int>("Remaining"),Is.EqualTo(2));Assert.That(Read<float>("Clock"),Is.Zero);}
 }
 public sealed class MagnetForceTests
 {
  float Force(int source=1,int target=-1,float distance=5,float cap=120000)=> (float)Type.GetType("LWS.TruckMagnetMayhem.MagnetForce, Assembly-CSharp",true).GetMethod("Newtons").Invoke(null,new object[]{source,target,distance,26f,8000f,500f,cap,8f});
  [Test]public void OppositePolesAttract(){Assert.That(Force(),Is.GreaterThan(0));}
  [Test]public void MatchingPolesRepel(){Assert.That(Force(target:1),Is.LessThan(0));}
  [Test]public void PassiveNeutralMetalIsAttracted(){Assert.That(Force(target:0),Is.GreaterThan(0));}
  [Test]public void OffDoesNotEmitForce(){Assert.That(Force(source:0),Is.Zero);}
  [TestCase(0f)][TestCase(26f)][TestCase(100f)][TestCase(float.NaN)]public void InvalidOrOutOfRangeSamplesProduceNoForce(float distance){Assert.That(Force(distance:distance),Is.Zero);}
  [Test]public void ForceFallsWithDistanceAndRespectsCap(){Assert.That(Force(distance:5),Is.GreaterThan(Force(distance:20)));Assert.That(Force(cap:100),Is.InRange(0,100));}
 }
}
