using NUnit.Framework;using System;
public sealed class HaulerRulesTests
{
 Rules r;int[] values;[SetUp]public void Setup(){values=new int[256];for(int i=0;i<256;i++)values[i]=i==254?5:i==255?0:1;r=new Rules();r.Reset(4,values);r.Begin();}
 [Test]public void HundredsOfFiniteBallsAreSupported(){Assert.AreEqual(256,r.Count);Assert.AreEqual(255,r.PositiveRemaining);}
 [Test]public void ScoopContactDoesNotScore(){Assert.AreEqual("Carried",r.Collect(0,0));Assert.AreEqual(0,r.points[0]);Assert.AreEqual(1,r.carried[0]);}
 [Test]public void CompetingScoopsClaimAtomically(){r.Collect(0,0);Assert.AreEqual("Ignored",r.Collect(0,1));Assert.AreEqual(0,r.carried[1]);}
 [Test]public void RepeatedColliderCannotCollectAgain(){r.Collect(0,0);Assert.AreEqual("Ignored",r.Collect(0,0));Assert.AreEqual(1,r.carried[0]);}
 [Test]public void CapacityTwelveIsEnforced(){for(int i=0;i<12;i++)r.Collect(i,0);Assert.AreEqual("Ignored",r.Collect(12,0));Assert.AreEqual(12,r.carried[0]);}
 [Test]public void ActualDepositAwardsCargoValue(){r.Collect(0,0);r.Collect(254,0);Assert.AreEqual(2,r.Deposit(0));Assert.AreEqual(6,r.points[0]);Assert.AreEqual(1,r.gold[0]);Assert.AreEqual(0,r.carried[0]);}
 [Test]public void DepositCannotScoreTwice(){r.Collect(0,0);r.Deposit(0);Assert.AreEqual(0,r.Deposit(0));Assert.AreEqual(1,r.points[0]);}
 [Test]public void OtherPlayerCannotDepositYourCargo(){r.Collect(0,0);Assert.AreEqual(0,r.Deposit(1));Assert.AreEqual(0,r.points[1]);}
 [Test]public void HazardNeverAwardsCollectionPoints(){Assert.AreEqual("Explosive",r.Collect(255,0));Assert.AreEqual(1,r.bombs[0]);Assert.AreEqual(0,r.points[0]);Assert.AreEqual(0,r.carried[0]);}
 [Test]public void HazardTriggersEvenWithFullCargo(){for(int i=0;i<12;i++)r.Collect(i,0);Assert.AreEqual("Explosive",r.Collect(255,0));}
 [Test]public void SpentBombCannotExplodeEveryFrame(){r.Collect(255,0);Assert.AreEqual("Ignored",r.Collect(255,1));Assert.AreEqual(0,r.bombs[1]);}
 [Test]public void SpilledCargoReturnsToRealLoosePool(){r.Collect(0,0);var ids=new int[12];Assert.AreEqual(1,r.Drop(0,6,ids));Assert.AreEqual(0,ids[0]);Assert.AreEqual("Loose",r.State(0));Assert.AreEqual(-1,r.owners[0]);Assert.AreEqual("Carried",r.Collect(0,1));}
 [Test]public void SafeDepositsCannotBeScattered(){r.Collect(0,0);r.Deposit(0);Assert.AreEqual(0,r.Drop(0,12,new int[12]));Assert.AreEqual(1,r.points[0]);}
 [Test]public void DropUsesBoundedCallerBuffer(){for(int i=0;i<6;i++)r.Collect(i,0);Assert.AreEqual(2,r.Drop(0,6,new int[2]));Assert.AreEqual(4,r.carried[0]);}
 [Test]public void TimerPreventsLateCollectionOrDeposit(){r.Collect(0,0);r.Tick(180);Assert.True(r.Complete);Assert.AreEqual(0,r.Deposit(0));Assert.AreEqual("Ignored",r.Collect(1,0));Assert.AreEqual(0,r.points[0]);}
 [Test]public void SharedScoreProducesSharedPlacing(){r.Collect(0,0);r.Collect(1,1);r.Deposit(0);r.Deposit(1);Assert.AreEqual(1,r.Place(0));Assert.AreEqual(1,r.Place(1));Assert.AreEqual(3,r.Place(2));}
 [Test]public void RematchRestoresAllBallOwnership(){r.Collect(0,0);r.Deposit(0);r.Reset(4,values);Assert.AreEqual(0,r.points[0]);Assert.AreEqual("Loose",r.State(0));Assert.AreEqual(-1,r.owners[0]);Assert.False(r.Live);}
 [Test]public void CountdownRejectsCargoClaims(){r.Reset(4,values);Assert.AreEqual("Ignored",r.Collect(0,0));}

 [Test]public void SecuringEveryValuableBallEndsWithoutWaitingForBombs(){var few=new int[16];for(int i=0;i<15;i++)few[i]=1;r.Reset(4,few);r.Begin();for(int i=0;i<12;i++)r.Collect(i,0);r.Deposit(0);for(int i=12;i<15;i++)r.Collect(i,0);r.Deposit(0);Assert.True(r.Complete);Assert.AreEqual(15,r.points[0]);Assert.AreEqual(0,r.PositiveRemaining);}
 [Test]public void IntakeHandlerHasItsOwnBuildableMonoScriptAsset(){const string path="Assets/TruckHungryHaulers/Scripts/HaulerMouth.cs";var script=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(path);Assert.NotNull(script);Assert.AreEqual("LWS.TruckHungryHaulers.HaulerMouth",script.GetClass().FullName);var scene=System.IO.File.ReadAllText("Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity");StringAssert.Contains("guid: "+UnityEditor.AssetDatabase.AssetPathToGUID(path),scene);Assert.False(scene.Contains("m_ClassName: HaulerMouth"),"No embedded unbacked MonoScript may remain in the saved player scene.");}
 sealed class Rules
 {
  readonly Type type=Type.GetType("LWS.TruckHungryHaulers.HaulerRules, Assembly-CSharp",true);readonly object value;public Rules(){value=Activator.CreateInstance(type);}object Call(string name,params object[] args)=>type.GetMethod(name).Invoke(value,args);T Read<T>(string name)=>(T)type.GetProperty(name).GetValue(value);T Field<T>(string name)=>(T)type.GetField(name).GetValue(value);
  public int Count=>Field<Array>("state").Length;public string State(int i)=>Field<Array>("state").GetValue(i).ToString();public int PositiveRemaining=>Read<int>("PositiveRemaining");public bool Live=>Read<bool>("Live");public bool Complete=>Read<bool>("Complete");public int[] points=>Field<int[]>("points");public int[] carried=>Field<int[]>("carried");public int[] gold=>Field<int[]>("gold");public int[] bombs=>Field<int[]>("bombs");public int[] owners=>Field<int[]>("owners");
  public void Reset(int players,int[] values)=>Call("Reset",players,values,180f,12);public void Begin()=>Call("Begin");public void Tick(float dt)=>Call("Tick",dt);public string Collect(int ball,int player)=>Call("Collect",ball,player).ToString();public int Deposit(int player)=>(int)Call("Deposit",player);public int Drop(int player,int max,int[] ids)=>(int)Call("Drop",player,max,ids);public int Place(int player)=>(int)Call("Place",player);
 }
}
