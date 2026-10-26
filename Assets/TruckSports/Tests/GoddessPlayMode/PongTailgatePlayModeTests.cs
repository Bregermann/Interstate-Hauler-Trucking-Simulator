#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class PongTailgatePlayModeTests
{
 const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
 static string AnimationState(Animator animator){var controller=animator.runtimeAnimatorController;return "frame="+Time.frameCount+",scale="+Time.timeScale+",dt="+Time.deltaTime+",active="+animator.gameObject.activeInHierarchy+",enabled="+animator.enabled+",initialized="+animator.isInitialized+",human="+animator.isHuman+",layers="+animator.layerCount+",controller="+(controller!=null?controller.name:"NONE")+",clips="+(controller!=null?string.Join("|",controller.animationClips.Select(clip=>clip.name)):"NONE")+(animator.layerCount>0?",state="+animator.GetCurrentAnimatorStateInfo(0).shortNameHash+",next="+animator.GetNextAnimatorStateInfo(0).shortNameHash+",time="+animator.GetCurrentAnimatorStateInfo(0).normalizedTime:",NO_STATE_LAYER");}
 sealed class Fixture:IDisposable
 {
  readonly IDisposable prefs;readonly GameObject isolation;public Behaviour match;public Component venue,crowd;
  public object Rules=>Get(match,"rules");public Transform[] Jugs=>(Transform[])Get(venue,"jugs");
  public Fixture(string suffix){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"PongTailgate"+suffix});isolation=new GameObject("Beer tailgate native source isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
  public IEnumerator Load()
  {
   EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBeerPong/Scenes/TruckBeerPong.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckBeerPong.PongMatch")).Cast<Behaviour>().Single();
   var table=(Collider)Get(match,"table");var tablePose=table.transform.localToWorldMatrix;var cups=((Array)Get(match,"cups")).Cast<Component>().ToArray();var poses=cups.Select(cup=>cup.transform.localToWorldMatrix).ToArray();var shells=cups.Select(cup=>Get(cup,"shell")).ToArray();var replay=Get(match,"replay");var sources=(Transform[])Get(replay,"sources");var cameras=((Camera[])Get(match,"cameras")).ToArray();var cameraPose=cameras[0].transform.localToWorldMatrix;
   var author=Type.GetType("LWS.TruckBeerPong.Editor.PongTailgateAuthoring, Assembly-CSharp-Editor",true);venue=(Component)author.GetMethod("Install").Invoke(null,new object[]{match,false});Assert.That(author.GetMethod("Install").Invoke(null,new object[]{match,false}),Is.SameAs(venue));crowd=(Component)Get(venue,"crowd");
   Assert.That(table.transform.localToWorldMatrix,Is.EqualTo(tablePose));for(int i=0;i<cups.Length;i++){Assert.That(cups[i].transform.localToWorldMatrix,Is.EqualTo(poses[i]));Assert.That(Get(cups[i],"shell"),Is.SameAs(shells[i]));}CollectionAssert.AreEqual(sources,(Transform[])Get(replay,"sources"));CollectionAssert.AreEqual(cameras,(Camera[])Get(match,"cameras"));Assert.That(cameras[0].transform.localToWorldMatrix,Is.EqualTo(cameraPose));
   Runtime("LWS.TruckSports.SportsPrefs").GetMethod("SetInt").Invoke(null,new object[]{"TruckBeerPong.AIPacing.v1",0});yield return null;
  }
  public void Fresh(){Call(match,"SetupScreen");Set(match,"option",0);Set(match,"teamA",1);Set(match,"teamB",1);Call(match,"NormalizeSetup");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);}
  public void Dispose(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
 }
 static void Capture(Fixture f,string label)
 {
  string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/PongTailgateNative");Directory.CreateDirectory(folder);var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var cameras=(Camera[])Get(f.match,"cameras");var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);foreach(int width in new[]{1280,1920})save.Invoke(null,new object[]{canvas,cameras,width,width==1280?720:1080,Path.Combine(folder,label+"-"+width+"-controlled.png"),null});
 }
 [UnityTest,Timeout(120000)]
 public IEnumerator OriginalAiShotNotifiesOneLockedResultAndActualHumanoidCrowdResponds()
 {
  using(var f=new Fixture("ActualAI"))
  {
   yield return f.Load();int events=0;bool lockedAtEvent=false;int shooter=-1;bool hit=false;Action<int,int,bool,Vector3> receipt=(participant,shot,success,point)=>{events++;lockedAtEvent=(bool)Get(f.Rules,"ShotLocked");shooter=participant;hit=success;Assert.That(Get(f.match,"Phase").ToString(),Is.EqualTo("Aftermath"));};var notification=f.match.GetType().GetEvent("ShotResolved");notification.AddEventHandler(f.match,receipt);
   try
   {
    f.Fresh();yield return null;float deadline=Time.realtimeSinceStartup+65;while(events==0&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(events,Is.EqualTo(1));Assert.That(lockedAtEvent,Is.True);Assert.That((int)Get(f.match,"TotalLaunches"),Is.GreaterThan(0));Assert.That((int)Get(f.venue,"Reactions"),Is.EqualTo(1));Assert.That((int)Get(f.venue,"LastShooter"),Is.EqualTo(shooter));Assert.That((int)Get(f.venue,"Cheers"),Is.EqualTo(hit?1:0));Assert.That((int)Get(f.venue,"Boos"),Is.EqualTo(hit?0:1));Assert.That(Get(f.crowd,"Mood"),Is.EqualTo(hit?"CHEER":"BOO"));
    var shots=((int[])Get(f.Rules,"shots")).ToArray();var remaining=((int[])Get(f.Rules,"remaining")).ToArray();int receipts=(int)Get(Get(f.match,"ScoreJournal"),"Count");Call(f.match,"Resolve",-1,"Duplicate notification rejection fixture");Assert.That(events,Is.EqualTo(1));CollectionAssert.AreEqual(shots,(int[])Get(f.Rules,"shots"));CollectionAssert.AreEqual(remaining,(int[])Get(f.Rules,"remaining"));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));
    var fans=(Animator[])Get(f.crowd,"nearFans");TestContext.WriteLine("PONG_HUMAN_REACTION_BEFORE "+AnimationState(fans[0]));yield return new WaitForSeconds(.3f);TestContext.WriteLine("PONG_HUMAN_REACTION_AFTER "+AnimationState(fans[0]));Assert.That(fans.Length,Is.EqualTo(64));Assert.That(fans.All(fan=>fan.avatar!=null&&fan.avatar.isHuman&&fan.runtimeAnimatorController!=null),Is.True);Assert.That(fans.All(fan=>fan.isActiveAndEnabled&&fan.isInitialized&&fan.layerCount>0),Is.True,"Every actual human rig needs an initialized nonempty controller.");int state=Animator.StringToHash(hit?"Cheer":"Disappointed");Assert.That(fans.Count(fan=>fan.GetCurrentAnimatorStateInfo(0).shortNameHash==state||fan.GetNextAnimatorStateInfo(0).shortNameHash==state),Is.GreaterThan(48),"Actual humanoid reaction clips must be active, not only counters.");Assert.That(((AudioSource)Get(f.crowd,"response")).isPlaying,Is.True);Capture(f,"actual-ai-"+(hit?"hit":"miss"));
    TestContext.WriteLine("PONG_TAILGATE_ACTUAL_AI shooter="+shooter+" hit="+hit+" phase="+Get(f.match,"Phase")+" launches="+Get(f.match,"TotalLaunches")+" shots="+string.Join(",",shots)+" eventCount="+events+" crowd="+Get(f.crowd,"Mood")+" journal="+receipts+" animated="+fans.Length+" totalSpectators="+Get(f.crowd,"Spectators"));
   }
   finally{notification.RemoveEventHandler(f.match,receipt);}
  }
 }
 [UnityTest,Timeout(90000)]
 public IEnumerator PostLockJugIsPooledNoninteractivePauseSafeAndClearsBeforeNextTurn()
 {
  using(var f=new Fixture("Jug"))
  {
   yield return f.Load();f.Fresh();yield return null;Assert.That((bool)Call(f.venue,"TryJugToss"),Is.False,"No audience projectile can start before authoritative result lock.");Call(f.match,"Resolve",-1,"Explicit presentation-only miss fixture");int current=(int)Get(f.match,"Current"),shot=(int)Get(f.venue,"LastShot");var shots=((int[])Get(f.Rules,"shots")).ToArray();var cups=((bool[,])Get(f.Rules,"cups")).Clone();int receipts=(int)Get(Get(f.match,"ScoreJournal"),"Count");var replay=Get(f.match,"replay");float cursor=(float)Get(replay,"Cursor");
   if((int)Get(f.venue,"Tosses")==0)Assert.That((bool)Call(f.venue,"TryJugToss"),Is.True,"Explicit post-lock choreography fixture bypasses chance only, not its live/locked/miss guards.");Assert.That((bool)Call(f.venue,"TryJugToss"),Is.False);Assert.That(f.Jugs.Length,Is.EqualTo(4));Assert.That(f.Jugs.All(jug=>jug.GetComponentsInChildren<Collider>(true).Length==0&&jug.GetComponentsInChildren<Rigidbody>(true).Length==0),Is.True);yield return new WaitForSeconds(.15f);var active=f.Jugs.Single(jug=>jug.gameObject.activeSelf);var airborne=active.position;yield return new WaitForSeconds(.1f);Assert.That(Vector3.Distance(active.position,airborne),Is.GreaterThan(.05f));
   CollectionAssert.AreEqual(shots,(int[])Get(f.Rules,"shots"));CollectionAssert.AreEqual((Array)cups,(Array)Get(f.Rules,"cups"));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));Assert.That((int)Get(f.match,"Current"),Is.EqualTo(current));Assert.That((float)Get(replay,"Cursor"),Is.EqualTo(cursor));
   Call(f.match,"Pause",current,false);yield return null;var paused=active.position;yield return new WaitForSecondsRealtime(.2f);Assert.That(active.position,Is.EqualTo(paused));Assert.That(((AudioSource)Get(f.venue,"atmosphere")).isPlaying,Is.False);Call(f.match,"Resume");yield return new WaitForSeconds(1.5f);Assert.That(f.Jugs.All(jug=>!jug.gameObject.activeSelf),Is.True);Assert.That((int)Get(f.venue,"Tosses"),Is.EqualTo(1));Assert.That(Get(f.match,"Phase").ToString(),Is.EqualTo("Replay"));Assert.That((bool)Call(f.venue,"TryJugToss"),Is.False);
   Call(f.match,"ExtraAction","replayskip");float deadline=Time.realtimeSinceStartup+5;while((int)Get(f.match,"Current")==current&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((int)Get(f.match,"Current"),Is.Not.EqualTo(current));Assert.That((bool)Get(f.Rules,"ShotLocked"),Is.False);Assert.That((bool)Call(f.venue,"TryJugToss"),Is.False);CollectionAssert.AreEqual(shots,(int[])Get(f.Rules,"shots"));
   f.Fresh();yield return null;Assert.That((int)Get(f.venue,"Reactions"),Is.Zero);Assert.That((int)Get(f.venue,"Tosses"),Is.Zero);Assert.That((int)Get(f.venue,"LastShooter"),Is.EqualTo(-1));Assert.That(f.Jugs.All(jug=>!jug.gameObject.activeSelf),Is.True);Call(f.match,"SetupScreen");yield return null;Assert.That(((AudioSource)Get(f.venue,"atmosphere")).isPlaying,Is.False);
   TestContext.WriteLine("Explicit authoritative miss fixture only: real original Resolve lock then existing collider-free jug art/DOTween motion. No cup state, result, shooter, replay cursor or next-turn authority changed by the gag. Four pooled props, one throw per locked miss, pause/replay/next turn/rematch cleanup checked.");
  }
 }
 [UnityTest,Timeout(90000)]
 public IEnumerator TailgateUsesActualStadiumVehicleFoodAndHumanoidAssetsWithoutExtraPhysicsOrListeners()
 {
  using(var f=new Fixture("Venue"))
  {
   yield return f.Load();Assert.That((int)Get(f.crowd,"Spectators"),Is.EqualTo(384));Assert.That(((Transform[])Get(f.venue,"parkedVehicles")).Length,Is.EqualTo(20));Assert.That(((Transform[])Get(f.venue,"partyAreas")).Length,Is.EqualTo(6));Assert.That(f.venue.GetComponentsInChildren<Collider>(true).Length,Is.Zero);Assert.That(f.venue.GetComponentsInChildren<Rigidbody>(true).Length,Is.Zero);Assert.That(f.venue.GetComponentsInChildren<AudioListener>(true).Length,Is.Zero);Assert.That(f.venue.GetComponentsInChildren<Camera>(true).Length,Is.Zero);Assert.That(f.venue.GetComponentsInChildren<MeshRenderer>(true).Count(renderer=>renderer.name.StartsWith("Union Field")),Is.GreaterThanOrEqualTo(10));Assert.That(((Transform[])Get(f.venue,"parkedVehicles")).All(vehicle=>vehicle.GetComponentsInChildren<MeshFilter>().Any(filter=>filter.sharedMesh!=null&&filter.sharedMesh.vertexCount>100)),Is.True,"Parked vehicles must be actual installed models rather than stand-in boxes.");
   f.Fresh();yield return new WaitForSeconds(.2f);var fans=(Animator[])Get(f.crowd,"nearFans");TestContext.WriteLine("PONG_HUMAN_IDLE_BEFORE "+AnimationState(fans[0]));Assert.That(fans[0].layerCount,Is.GreaterThan(0));float phase=fans[0].GetCurrentAnimatorStateInfo(0).normalizedTime;yield return new WaitForSeconds(.2f);TestContext.WriteLine("PONG_HUMAN_IDLE_AFTER "+AnimationState(fans[0]));Assert.That(fans[0].GetCurrentAnimatorStateInfo(0).normalizedTime,Is.GreaterThan(phase));Capture(f,"football-party-environment");
   TestContext.WriteLine("Reused UnionField stadium mesh/material foundation,20real vehicle render assemblies including4pickup-bodied trucks/4semis,6catered party areas,64actual humanoid clip actors+320existing baked stadium fans. Decorative library has no physics/listener/camera. Visual density/readability and atmospheric audio mixing still need human approval.");
  }
 }
}
#endif
