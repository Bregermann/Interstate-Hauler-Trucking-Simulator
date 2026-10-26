#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class BaseGustPlayModeTests
{
 const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
 sealed class Fixture:IDisposable
 {
  readonly IDisposable prefs;readonly GameObject isolation;public Behaviour match;
  public Fixture(string suffix){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"BaseGust"+suffix});isolation=new GameObject("Base gust native source isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
  public object Weather=>Get(match,"weatherField");public object Settings=>Get(Weather,"gustSettings");public object Gust=>Get(Weather,"Gusts");public object Sweep=>Get(Weather,"GustSweep");
  public object Jumper(int slot)=>((Array)Get(match,"jumpers")).GetValue(slot);public Rigidbody Body(int slot)=>(Rigidbody)Get(Get(Jumper(slot),"truck"),"body");
  public IEnumerator Load()
  {
   EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckBaseJumping.BaseJumpMatch")).Cast<Behaviour>().Single();Call(match,"AllBots");Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{true});Set(match,"option",0);Assert.That((bool)Call(match,"StartMatch"),Is.True);
  }
  public void Dispose(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Time.timeScale=1;}
 }
 static ParticleSystem[] Emitters(Fixture f)=>Enumerable.Range(0,(int)Get(f.Sweep,"PoolCount")).Select(index=>(ParticleSystem)Call(f.Sweep,"Emitter",index)).ToArray();
 static void Capture(Fixture f,string label)
 {
  string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/BaseGustNative");Directory.CreateDirectory(folder);var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var cameras=(Camera[])Get(f.match,"cameras");var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);foreach(int width in new[]{1280,1920})save.Invoke(null,new object[]{canvas,cameras,width,width==1280?720:1080,Path.Combine(folder,label+"-"+width+"-controlled.png"),null});
 }
 [UnityTest,Timeout(150000)]
 public IEnumerator ActualSharedGustSweepsNativeSmokePausesAndKeepsFourViewsAndNaturalLandingScores()
 {
  using(var f=new Fixture("Natural"))
  {
   yield return f.Load();Set(f.match,"seed",4757);Call(f.match,"NewRound");var cameras=(Camera[])Get(f.match,"cameras");var rects=cameras.Select(camera=>camera.rect).ToArray();var targets=cameras.Select(camera=>camera.targetTexture).ToArray();var pipeline=GraphicsSettings.defaultRenderPipeline;var particles=Emitters(f);Assert.That(particles.Length,Is.EqualTo(6));var ids=particles.Select(effect=>effect.GetInstanceID()).ToArray();Assert.That((int)Get(f.match,"ViewCount"),Is.EqualTo(4));
   float deadline=Time.realtimeSinceStartup+35;while((!(bool)Get(f.Gust,"Active")||(float)Get(f.Gust,"Envelope")<.7f)&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((bool)Get(f.Gust,"Active"),Is.True);Assert.That(particles.Sum(effect=>effect.particleCount),Is.GreaterThan(0));
   foreach(var effect in particles){Assert.That(effect.main.maxParticles,Is.EqualTo(48));Assert.That(effect.main.simulationSpace,Is.EqualTo(ParticleSystemSimulationSpace.World));Assert.That(effect.collision.enabled,Is.False);Assert.That(effect.GetComponentsInChildren<Collider>(true).Length,Is.Zero);Assert.That(effect.GetComponentsInChildren<Rigidbody>(true).Length,Is.Zero);Assert.That(effect.textureSheetAnimation.numTilesX,Is.EqualTo(6));Assert.That(effect.textureSheetAnimation.numTilesY,Is.EqualTo(5));Assert.That(effect.GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture,Is.Not.Null);}
   var center=(Vector3)Get(f.Gust,"Center");var direction=(Vector3)Get(f.Gust,"Direction");float start=(float)Get(f.Gust,"Age");yield return new WaitForSeconds(.3f);float elapsed=(float)Get(f.Gust,"Age")-start;var traveled=(Vector3)Get(f.Gust,"Center")-center;Assert.That(Vector3.Dot(traveled,direction),Is.EqualTo((float)Get(f.Settings,"sweepSpeed")*elapsed).Within(.05f));Assert.That(traveled.magnitude,Is.GreaterThan(5));
   var sample=(Vector3)Get(f.Gust,"Center")+Vector3.up*100;var wind=(Vector3)Call(f.Weather,"SampleWind",sample);Assert.That(wind.magnitude,Is.LessThanOrEqualTo(13.001f));Assert.That((Vector3)Call(f.Weather,"SampleWind",new Vector3(sample.x,20,sample.z)),Is.EqualTo((Vector3)Get(f.Weather,"Flow")),"Gust fades out before the landing zone.");Capture(f,"actual-shared-sweeping-gust");
   float air=(float)Get(f.match,"AirTime"),age=(float)Get(f.Gust,"Age");Call(f.match,"Pause",0,false);yield return null;yield return null;var times=particles.Select(effect=>effect.time).ToArray();yield return new WaitForSecondsRealtime(.25f);Assert.That((float)Get(f.match,"AirTime"),Is.EqualTo(air));Assert.That((float)Get(f.Gust,"Age"),Is.EqualTo(age));for(int i=0;i<particles.Length;i++)Assert.That(particles[i].time,Is.EqualTo(times[i]).Within(.001f));Call(f.match,"Resume");
   deadline=Time.realtimeSinceStartup+100;while((int)Get(f.match,"RoundLocks")==0&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((int)Get(f.match,"RoundLocks"),Is.EqualTo(1));var results=((Array)Get(f.match,"jumpers")).Cast<object>().Select(jumper=>Get(jumper,"Landing")).ToArray();Assert.That(results.Any(result=>(bool)Get(result,"valid")),Is.True,"Default bounded gusts must leave ordinary AI attempts achievable.");var points=results.Select(result=>(int)Get(result,"points")).ToArray();CollectionAssert.AreEqual(points,(int[])Get(f.match,"totals"));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(points.Count(value=>value>0)));CollectionAssert.AreEqual(rects,cameras.Select(camera=>camera.rect).ToArray());CollectionAssert.AreEqual(targets,cameras.Select(camera=>camera.targetTexture).ToArray());Assert.That(GraphicsSettings.defaultRenderPipeline,Is.SameAs(pipeline));
   Call(f.match,"SetupScreen");Assert.That(particles.All(effect=>effect.particleCount==0&&!effect.gameObject.activeSelf),Is.True);Assert.That((bool)Get(f.Gust,"Active"),Is.False);Assert.That((bool)Call(f.match,"StartMatch"),Is.True);CollectionAssert.AreEqual(ids,Emitters(f).Select(effect=>effect.GetInstanceID()).ToArray());Assert.That((int)Get(f.Gust,"Serial"),Is.Zero);Assert.That((float)Get(f.Gust,"Age"),Is.Zero);Assert.That(((int[])Get(f.match,"totals")),Is.All.Zero);
   TestContext.WriteLine("BASE_GUST_NATURAL one original four-AI round, accepted landing/ranking receipts, shared spatial sweep,6owned NWH-atlas emitters/288particle capacity, actual movement, pause and rematch identity cleanup. Four original viewport rectangles/targets and pipeline retained. Controlled captures; not human art/performance approval. Birds remain absent: no owned flying-bird rig/clip package was located.");
  }
 }
 sealed class FlightResult{public Vector3 position,velocity;public float maximumWind;}
 static FlightResult SimulateWindow(Fixture f,bool enabled,int seed,float firstAge)
 {
  Set(f.Settings,"enabled",enabled);Call(f.Weather,"Select",0,seed);var jumper=f.Jumper(0);Call(jumper,"Prepare",new Vector3(0,150,0));Call(jumper,"Release");Physics.SyncTransforms();var result=new FlightResult();
  for(int step=0;step<200;step++)
  {
   Call(f.Weather,"Tick",firstAge+step*Time.fixedDeltaTime);var wind=(Vector3)Call(f.Weather,"SampleWind",f.Body(0).worldCenterOfMass);result.maximumWind=Mathf.Max(result.maximumWind,wind.magnitude);Call(jumper,"Step",Vector2.zero,step==0,false,Time.fixedDeltaTime,wind);Physics.Simulate(Time.fixedDeltaTime);
  }
  result.position=f.Body(0).position;result.velocity=f.Body(0).linearVelocity;Assert.That((bool)Get(jumper,"Landed"),Is.False);Assert.That((float)Get(jumper,"Opening"),Is.EqualTo(1));Assert.That((int)Get(f.match,"LandingLocks"),Is.Zero);return result;
 }
 [UnityTest,Timeout(90000)]
 public IEnumerator OriginalJumperForcesProduceBoundedSeedReproducibleRealBodyDeviation()
 {
  using(var f=new Fixture("PhysicalAB"))
  {
   yield return f.Load();f.match.enabled=false;Call(Get(f.match,"skyField"),"StopField");for(int i=1;i<4;i++)f.Body(i).isKinematic=true;
   const int seed=123;Call(f.Weather,"Select",0,seed);float midpoint=-1;for(float age=0;age<35;age+=.02f){Call(f.Gust,"Tick",age);if((bool)Get(f.Gust,"Active")&&(float)Get(f.Gust,"Envelope")>.99f&&Vector3.Dot((Vector3)Get(f.Gust,"Center"),(Vector3)Get(f.Gust,"Direction"))>=0){midpoint=age;break;}}Assert.That(midpoint,Is.GreaterThan(8));
   var simulation=Physics.simulationMode;FlightResult baseline=null,first=null,repeat=null;
   try{Physics.simulationMode=SimulationMode.Script;baseline=SimulateWindow(f,false,seed,midpoint-2);first=SimulateWindow(f,true,seed,midpoint-2);repeat=SimulateWindow(f,true,seed,midpoint-2);}
   finally{Physics.simulationMode=simulation;}
   float difference=Vector3.ProjectOnPlane(first.position-baseline.position,Vector3.up).magnitude;TestContext.WriteLine("BASE_GUST_PHYSX seed="+seed+" startAge="+(midpoint-2)+" baselinePosition="+baseline.position+" gustPosition="+first.position+" repeatPosition="+repeat.position+" planarDeviation="+difference+" velocityDelta="+(first.velocity-baseline.velocity).magnitude+" maxWind="+first.maximumWind);
   Assert.That(difference,Is.InRange(.25f,8f),"Actual original canopy/drag/steer force integration must create a meaningful but manageable4-second deviation.");Assert.That((first.velocity-baseline.velocity).magnitude,Is.LessThan(4));Assert.That(Vector3.Distance(first.position,repeat.position),Is.LessThan(.01f));Assert.That(Vector3.Distance(first.velocity,repeat.velocity),Is.LessThan(.01f));Assert.That(first.maximumWind,Is.LessThanOrEqualTo(13.001f));Assert.That(((int[])Get(f.match,"totals")),Is.All.Zero);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);
   // Same field at the same place/time, no participant-specific randomness.
   var position=(Vector3)Get(f.Gust,"Center")+Vector3.up*100;Assert.That((Vector3)Call(f.Weather,"SampleWind",position),Is.EqualTo((Vector3)Call(f.Weather,"SampleWind",position)));Assert.That((Vector3)Call(f.Gust,"Delta",position+(Vector3)Get(f.Gust,"Direction")*1000),Is.EqualTo(Vector3.zero));
   TestContext.WriteLine("Controlled identical initial release/deploy commands and Script PhysX steps only; actual BaseJumper.Step applied every force/torque. No assigned trajectory, score, fake landing or world velocity correction. Original eight-second/multiround input/camera tests remain separate.");
  }
 }
}
#endif
