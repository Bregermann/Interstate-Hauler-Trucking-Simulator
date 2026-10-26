#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class HotPotatoTransferPlayModeTests
{
 const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
 sealed class Fixture:IDisposable
 {
  readonly IDisposable prefs;readonly GameObject isolation;readonly InputSettings original,temporary;
  public readonly Gamepad[] pads=new Gamepad[2];public Behaviour match;
  public Fixture(string suffix)
  {
   prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"HotPotatoTransfer"+suffix});original=InputSystem.settings;temporary=UnityEngine.Object.Instantiate(original);temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporary;
   isolation=new GameObject("Potato transfer native neutral human source isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));for(int i=0;i<2;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
  }
  public object Rules=>Get(match,"rules");public object Truck(int slot)=>((Array)Get(match,"trucks")).GetValue(slot);public Rigidbody Body(int slot)=>(Rigidbody)Get(Truck(slot),"body");public object Journal=>Get(match,"ScoreJournal");
  public IEnumerator Load()
  {
   EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHotPotato/Scenes/TruckHotPotato.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckHotPotato.HotPotatoMatch")).Cast<Behaviour>().Single();Set(Get(match,"setup"),"count",2);Call(match,"NormalizeSetup");Call(match,"AllBots");
   for(int i=0;i<2;i++){var player=((Array)Get(Get(match,"setup"),"players")).GetValue(i);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),"WholeController"));Set(player,"pad",pads[i]);}
  }
  public IEnumerator Fresh()
  {
   Call(match,"SetupScreen");foreach(var pad in pads)InputSystem.QueueStateEvent(pad,new GamepadState());Assert.That((bool)Call(match,"StartMatch"),Is.True);float deadline=Time.realtimeSinceStartup+8;while(((float)Get(match,"Countdown")>0||(float)Get(Rules,"Clock")<1)&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((float)Get(match,"Countdown"),Is.Zero);Assert.That((float)Get(Rules,"Clock"),Is.GreaterThanOrEqualTo(1));
  }
  public void Pose(int slot,Vector3 position,Quaternion rotation){Call(Truck(slot),"Prepare",position,rotation,true);Body(slot).transform.SetPositionAndRotation(position,rotation);}
  public void Separate(){Pose(0,new Vector3(-20,.65f,-18),Quaternion.identity);Pose(1,new Vector3(20,.65f,18),Quaternion.Euler(0,180,0));Physics.SyncTransforms();}
  public IEnumerator GentleContact()
  {
   Pose(0,new Vector3(0,.65f,-6),Quaternion.identity);Pose(1,new Vector3(0,.65f,6),Quaternion.Euler(0,180,0));Physics.SyncTransforms();var shape0=(BoxCollider)Get(Truck(0),"shape");var shape1=(BoxCollider)Get(Truck(1),"shape");float gap=shape1.bounds.min.z-shape0.bounds.max.z;var location=Body(1).position-Vector3.forward*(gap-.015f);Pose(1,location,Quaternion.Euler(0,180,0));Physics.SyncTransforms();
   int transfers=(int)Get(Rules,"Transfers");int carrier=(int)Get(Rules,"Carrier");Call(Get(Truck(0),"tractorMotor"),"AddAssemblyVelocity",Vector3.forward*.65f);float deadline=Time.realtimeSinceStartup+4;while((int)Get(Rules,"Transfers")==transfers&&Time.realtimeSinceStartup<deadline)yield return null;
   TestContext.WriteLine("POTATO_REAL_CONTACT previousCarrier="+carrier+" newCarrier="+Get(Rules,"Carrier")+" count="+Get(Rules,"Transfers")+" fixedTime="+Time.fixedTime+" clock="+Get(Rules,"Clock")+" lastPhysicalContact="+((float[,])Get(Rules,"lastContact"))[0,1]+" scores="+string.Join(",",(int[])Get(Rules,"scores")));
   Assert.That((int)Get(Rules,"Transfers"),Is.EqualTo(transfers+1),"A genuine gentle compound NWH/PhysX contact must transfer through the original callback.");Assert.That((int)Get(Rules,"Carrier"),Is.EqualTo(1-carrier));
  }
  public void Dispose()
  {
   Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");InputSystem.settings=original;foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);UnityEngine.Object.DestroyImmediate(temporary);UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Time.timeScale=1;
  }
 }
 [UnityTest,Timeout(90000)]
 public IEnumerator GentleRealContactAwardsEveryValidPassButRestingCompoundContactCannotFarm()
 {
  using(var f=new Fixture("Edges"))
  {
   yield return f.Load();Assert.That((int)Get(f.Rules,"transferPoints"),Is.EqualTo(3));yield return f.Fresh();int first=(int)Get(f.Rules,"Carrier");var ids=new HashSet<string>();yield return f.GentleContact();var receipt=Get(f.Journal,"Latest");Assert.That((double)Get(receipt,"Delta"),Is.EqualTo(3));Assert.That((int)Get(receipt,"Slot"),Is.EqualTo(first));Assert.That(ids.Add((string)Get(receipt,"EventId")),Is.True);
   // Explicit resting-pose isolation after real contact: constrain touching
   // bodies, keep them awake and verify real CollisionStay receipts span longer
   // than the cooldown. No Contact/Step/score callback is invoked by the test.
   var constraints=new[]{f.Body(0).constraints,f.Body(1).constraints};var sleeping=new[]{f.Body(0).sleepThreshold,f.Body(1).sleepThreshold};for(int i=0;i<2;i++){f.Body(i).constraints=RigidbodyConstraints.FreezeAll;f.Body(i).sleepThreshold=0;}
   var firstShape=(BoxCollider)Get(f.Truck(0),"shape");var secondShape=(BoxCollider)Get(f.Truck(1),"shape");float restingGap=secondShape.bounds.min.z-firstShape.bounds.max.z;f.Pose(1,f.Body(1).position-Vector3.forward*(restingGap+.005f),f.Body(1).rotation);Physics.SyncTransforms();
   float contactBefore=((float[,])Get(f.Rules,"lastContact"))[0,1],clockBefore=(float)Get(f.Rules,"Clock");for(int i=0;i<65;i++){f.Body(0).WakeUp();f.Body(1).WakeUp();yield return new WaitForFixedUpdate();}
   float contactAfter=((float[,])Get(f.Rules,"lastContact"))[0,1];TestContext.WriteLine("POTATO_ACTUAL_REST clockSpan="+((float)Get(f.Rules,"Clock")-clockBefore)+" collisionStaySpan="+(contactAfter-contactBefore)+" transfers="+Get(f.Rules,"Transfers"));Assert.That(contactAfter-contactBefore,Is.GreaterThan(.8f),"Real continuous CollisionStay, rather than absent contact, must exercise the original edge filter across cooldown.");Assert.That((int)Get(f.Rules,"Transfers"),Is.EqualTo(1));Assert.That(((int[])Get(f.Rules,"scores")).Sum(),Is.EqualTo(3));Assert.That((int)Get(f.Journal,"Count"),Is.EqualTo(1));for(int i=0;i<2;i++){f.Body(i).constraints=constraints[i];f.Body(i).sleepThreshold=sleeping[i];}
   for(int pass=2;pass<=3;pass++){f.Separate();yield return new WaitForSeconds(.9f);yield return f.GentleContact();receipt=Get(f.Journal,"Latest");Assert.That((double)Get(receipt,"Delta"),Is.EqualTo(3));Assert.That(ids.Add((string)Get(receipt,"EventId")),Is.True);}
   Assert.That((int)Get(f.Rules,"Cycle"),Is.EqualTo(1));Assert.That(((int[])Get(f.Rules,"scores"))[first],Is.EqualTo(6));Assert.That(((int[])Get(f.Rules,"scores"))[1-first],Is.EqualTo(3));Assert.That((int)Get(f.Journal,"Count"),Is.EqualTo(3));Assert.That((float)Get(f.Rules,"transferCooldown"),Is.EqualTo(.7f));Assert.That((float)Get(f.match,"carrierPowerBoost"),Is.EqualTo(1.15f));
   TestContext.WriteLine("Controlled initial poses/0.65m/s assembly velocity only; three transfers used original collision callbacks and scoring. Same carrier earned twice in one cycle only after actual separation/cooldown. Continuous awake physical contact earned exactly one3-point receipt. No synthetic Contact/Step calls or scoring injection.");
  }
 }
 [UnityTest,Timeout(90000)]
 public IEnumerator ConfiguredRealPassKeepsNaturalFuseExplosionSurvivalWinnerAndRematch()
 {
  using(var f=new Fixture("Tuning"))
  {
   yield return f.Load();Set(f.Rules,"transferPoints",7);yield return f.Fresh();Assert.That((string)Get(f.match,"Help"),Does.Contain("+7 per successful bomb transfer"));int passer=(int)Get(f.Rules,"Carrier");yield return f.GentleContact();Assert.That(((int[])Get(f.Rules,"scores"))[passer],Is.EqualTo(7));Assert.That((double)Get(Get(f.Journal,"Latest"),"Delta"),Is.EqualTo(7));f.Separate();Assert.That((float)Get(f.Rules,"Fuse")+(float)Get(f.Rules,"Clock"),Is.InRange(12f,20f),"The original natural fuse distribution is unchanged.");float deadline=Time.realtimeSinceStartup+23;while(!(bool)Get(f.Rules,"Complete")&&Time.realtimeSinceStartup<deadline)yield return null;
   Assert.That((bool)Get(f.Rules,"Complete"),Is.True);Assert.That((int)Get(f.Rules,"Winner"),Is.EqualTo(passer));Assert.That((int)Get(f.match,"Explosions"),Is.EqualTo(1));Assert.That(((int[])Get(f.Rules,"scores"))[passer],Is.EqualTo(127),"Configured7-point pass plus unchanged20survival and100last-standing.");Assert.That(((int[])Get(f.Rules,"scores"))[1-passer],Is.Zero);Assert.That((int)Get(f.Journal,"Count"),Is.EqualTo(3));Assert.That((double)Get(Get(f.Journal,"Latest"),"Delta"),Is.EqualTo(100));
   string old=(string)Get(f.match,"MatchId");yield return f.Fresh();Assert.That((string)Get(f.match,"MatchId"),Is.Not.EqualTo(old));Assert.That((int)Get(f.Rules,"transferPoints"),Is.EqualTo(7));Assert.That((int)Get(f.Rules,"Transfers"),Is.Zero);Assert.That(((int[])Get(f.Rules,"scores")),Is.All.Zero);Assert.That((int)Get(f.Journal,"Count"),Is.Zero);Assert.That((int)Get(f.match,"Explosions"),Is.Zero);Assert.That((int)Call(f.Rules,"AliveCount"),Is.EqualTo(2));
   TestContext.WriteLine("Natural original12–20s fuse elapsed with actual PhysX trucks and explosion; carrier elimination, unchanged survivor/winner awards and exact configured transfer delta retained. Rematch clears receipts/population/scores and keeps requested tuning. Full eight-truck AI probe remains separate acceptance.");
  }
 }
}
#endif
