using System;using NUnit.Framework;
public sealed class HerdRulesTests
{
 Rules r;bool[] hazards;[SetUp]public void Setup(){hazards=new bool[40];hazards[39]=true;r=new Rules();r.Reset(4,hazards);r.Begin();}
 [Test]public void FiniteNormalSheepExcludeHazards(){Assert.AreEqual(39,r.Remaining);}
 [Test]public void PenEntryScoresOne(){Assert.True(r.Secure(0,0));Assert.AreEqual(1,r.points[0]);}
 [Test]public void RepeatedPenContactNeverRepeatsPoints(){r.Secure(0,0);Assert.False(r.Secure(0,0));Assert.AreEqual(1,r.points[0]);}
 [Test]public void FirstValidPenWinsAtomicOwnership(){r.Secure(0,1);Assert.False(r.Secure(0,2));Assert.AreEqual(1,r.owners[0]);Assert.AreEqual(0,r.points[2]);}
 [Test]public void LeavingAndReenteringCannotFarmTheAnimal(){r.Secure(0,0);r.Tick(10);Assert.False(r.Secure(0,1));Assert.AreEqual(38,r.Remaining);}
 [Test]public void ExplosiveSheepNeverAwardOrdinaryHerdPoints(){Assert.False(r.Secure(39,0));Assert.AreEqual(0,r.points[0]);}
 [Test]public void OrdinarySheepNeverDetonateThroughThisRule(){Assert.False(r.Detonate(0));Assert.False(r.detonated[0]);}
 [Test]public void HazardExplodesExactlyOnce(){Assert.True(r.Detonate(39));Assert.False(r.Detonate(39));Assert.True(r.detonated[39]);}
 [Test]public void ExplosionDoesNotAwardAnyPoints(){r.Detonate(39);Assert.AreEqual(0,r.points[0]);Assert.AreEqual(39,r.Remaining);}
 [Test]public void CountdownRejectsPenClaims(){r.Reset(4,hazards);Assert.False(r.Secure(0,0));Assert.False(r.Detonate(39));}
 [Test]public void TimerLocksLatePenClaims(){r.Tick(180);Assert.True(r.Complete);Assert.False(r.Secure(0,0));Assert.False(r.Detonate(39));}
 [Test]public void ScoresOutrankCompletionTimes(){r.Secure(0,0);r.Tick(5);r.Secure(1,1);r.Secure(2,1);Assert.AreEqual(1,r.Place(1));Assert.AreEqual(2,r.Place(0));}
 [Test]public void EqualCountUsesEarliestValidCompletionTime(){r.Secure(0,0);r.Tick(10);r.Secure(1,1);Assert.AreEqual(1,r.Place(0));Assert.AreEqual(2,r.Place(1));}
 [Test]public void ExactTimeTiesSharePlacing(){r.Secure(0,0);r.Secure(1,1);Assert.AreEqual(1,r.Place(0));Assert.AreEqual(1,r.Place(1));Assert.AreEqual(3,r.Place(2));}
 [Test]public void ZeroScoresSharePlacingRatherThanInventingWinner(){for(int i=0;i<4;i++)Assert.AreEqual(1,r.Place(i));}
 [Test]public void LastNormalCaptureEndsWithoutWaitingForBomb(){r.Reset(1,new[]{false,false,false,true});r.Begin();for(int i=0;i<3;i++)r.Secure(i,0);Assert.True(r.Complete);Assert.False(r.Live);Assert.AreEqual(0,r.Remaining);}
 [Test]public void InvalidReferencesNeverScore(){Assert.False(r.Secure(-1,0));Assert.False(r.Secure(40,0));Assert.False(r.Secure(0,4));}
 [Test]public void RematchRestoresEveryAnimalAndTimestamp(){r.Secure(0,0);r.Detonate(39);r.Reset(4,hazards);Assert.AreEqual(-1,r.owners[0]);Assert.False(r.detonated[39]);Assert.AreEqual(0,r.points[0]);Assert.True(float.IsPositiveInfinity(r.lastCompletion[0]));}
 [Test]public void AuthoringHazardArrayIsCloned(){hazards[0]=true;Assert.True(r.Secure(0,0));}
 sealed class Rules
 {
  readonly Type type=Type.GetType("LWS.TruckSheepHerding.HerdRules, Assembly-CSharp",true);readonly object value;public Rules(){value=Activator.CreateInstance(type);}object Call(string n,params object[] a)=>type.GetMethod(n).Invoke(value,a);T Read<T>(string n)=>(T)type.GetProperty(n).GetValue(value);T Field<T>(string n)=>(T)type.GetField(n).GetValue(value);
  public int Remaining=>Read<int>("Remaining");public bool Complete=>Read<bool>("Complete");public bool Live=>Read<bool>("Live");public int[] points=>Field<int[]>("points");public int[] owners=>Field<int[]>("owners");public bool[] detonated=>Field<bool[]>("detonated");public float[] lastCompletion=>Field<float[]>("lastCompletion");public void Reset(int p,bool[] h)=>Call("Reset",p,h,180f);public void Begin()=>Call("Begin");public void Tick(float dt)=>Call("Tick",dt);public bool Secure(int a,int b)=>(bool)Call("Secure",a,b);public bool Detonate(int i)=>(bool)Call("Detonate",i);public int Place(int i)=>(int)Call("Place",i);
 }
}
