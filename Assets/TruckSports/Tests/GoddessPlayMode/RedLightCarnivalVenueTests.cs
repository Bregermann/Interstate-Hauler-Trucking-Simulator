#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RedLightCarnivalVenueTests
{
 const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
 sealed class Fixture:IDisposable
 {
  readonly IDisposable prefs;readonly InputSettings original,temporary;readonly GameObject isolation;public readonly Gamepad[] pads=new Gamepad[4];public Behaviour match;public Component venue;public string sharedProfile;
  public Fixture(string suffix)
  {
   prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"RedLightVenue"+suffix});original=InputSystem.settings;temporary=UnityEngine.Object.Instantiate(original);temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporary;
   isolation=new GameObject("Red Light venue source isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));for(int i=0;i<4;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
  }
  public object rules=>Get(match,"rules");public object groundCamera=>Get(venue,"groundCamera");public object broadcast=>Get(match,"broadcast");public Camera camera=>(Camera)Get(broadcast,"view");public object framing=>Get(broadcast,"broadcastFraming");public object Player(int slot)=>((Array)Get(Get(match,"setup"),"players")).GetValue(slot);
  public void Humans(int count){for(int i=0;i<count;i++){var player=Player(i);Set(player,"source",Enum.Parse(player.GetType().GetField("source").FieldType,"WholeController"));Set(player,"pad",pads[i]);}}
  public IEnumerator Load()
  {
   EditorSceneManager.LoadSceneInPlayMode("Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
   match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckRedLightGreenLight.RedLightMatch")).Cast<Behaviour>().Single();Call(match,"AllBots");sharedProfile=JsonUtility.ToJson((UnityEngine.Object)Get(framing,"sharedDefaults"));
   var author=Type.GetType("LWS.TruckRedLightGreenLight.Editor.RedLightCarnivalAuthoring, Assembly-CSharp-Editor",true);venue=(Component)author.GetMethod("Install").Invoke(null,new object[]{match,false});Assert.That(author.GetMethod("Install").Invoke(null,new object[]{match,false}),Is.SameAs(venue),"Explicit authoring is idempotent.");
   var presets=Get(Get(match,"atmosphere"),"presets");foreach(var preset in (Array)Get(presets,"entries"))if((string)Get(preset,"displayName")=="Clear"){Call(presets,"Select",Get(preset,"id"));break;}Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});Call(Get(match,"drivingViews"),"Select",0);
  }
  public IEnumerator Start(int count)
  {
   Set(Get(match,"setup"),"count",count);foreach(var pad in pads)InputSystem.QueueStateEvent(pad,new GamepadState());Assert.That((bool)Call(match,"StartMatch"),Is.True);float deadline=Time.realtimeSinceStartup+8;while(Get(rules,"Phase").ToString()=="Countdown"&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(Get(rules,"Phase").ToString(),Is.EqualTo("Green"));yield return null; yield return null;
  }
  public void Dispose(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");InputSystem.settings=original;foreach(var pad in pads)if(pad.added)InputSystem.RemoveDevice(pad);UnityEngine.Object.DestroyImmediate(temporary);UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Time.timeScale=1;}
 }
 static void Capture(Fixture f,string label)
 {
  string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/RedLightCarnivalNative");Directory.CreateDirectory(folder);var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var cameras=(Camera[])Get(f.match,"cameras");var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);foreach(int width in new[]{1280,1920})save.Invoke(null,new object[]{canvas,cameras,width,width==1280?720:1080,Path.Combine(folder,label+"-"+width+"-controlled.png"),null});
 }
 static void GroundFrame(Fixture f,int count)
 {
  Assert.That((bool)Get(f.groundCamera,"OwnsGroundShot"),Is.True);Assert.That((int)Get(f.groundCamera,"AppliedFrame"),Is.InRange(Time.frameCount-1,Time.frameCount),"UnityTest resumes before LateUpdate: require the completed preceding frame or same-frame manual render, never an older shot.");Assert.That((int)Get(f.broadcast,"FrameEvaluations"),Is.EqualTo(1));Assert.That(f.camera.orthographic,Is.False);Assert.That(f.camera.fieldOfView,Is.InRange(60,75));Assert.That(f.camera.transform.position.y-(float)Get(f.groundCamera,"GroundHeight"),Is.EqualTo(.95f).Within(.04f));Assert.That((int)Get(f.groundCamera,"FramedRacers"),Is.EqualTo(count));Assert.That((bool)Get(f.groundCamera,"AllBodiesVisible"),Is.True);
  var trucks=(Array)Get(f.match,"trucks");for(int i=0;i<count;i++){var bounds=((Collider)Get(trucks.GetValue(i),"shape")).bounds;for(int corner=0;corner<8;corner++){var point=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));var p=f.camera.WorldToViewportPoint(point);Assert.That(p.z,Is.GreaterThan(0));Assert.That(p.x,Is.InRange(.015f,.985f));Assert.That(p.y,Is.InRange(.015f,.985f));}}
  var master=(Transform)Get(f.match,"master");var renderers=master.GetComponentsInChildren<Renderer>(true);Assert.That(renderers.Length,Is.GreaterThan(0));var masterBounds=renderers[0].bounds;foreach(var renderer in renderers)masterBounds.Encapsulate(renderer.bounds);var points=new Vector3[9];points[0]=new Vector3(0,0,(float)Get(f.match,"finishZ"));for(int corner=0;corner<8;corner++)points[corner+1]=masterBounds.center+Vector3.Scale(masterBounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));foreach(var point in points){var p=f.camera.WorldToViewportPoint(point);Assert.That(p.z,Is.GreaterThan(0));Assert.That(p.x,Is.InRange(.01f,.99f));Assert.That(p.y,Is.InRange(.01f,.99f));}
 }
 static Color Emission(Renderer renderer){var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);return block.GetColor("_EmissionColor");}
 static void TwoAspects(Fixture f,bool red)
 {
  var reds=(Renderer[])Get(f.venue,"redLenses");var greens=(Renderer[])Get(f.venue,"greenLenses");Assert.That(reds.Length,Is.EqualTo(8));Assert.That(greens.Length,Is.EqualTo(8));foreach(var color in reds.Select(Emission)){Assert.That(color.g,Is.Zero);Assert.That(color.b,Is.Zero);if(red)Assert.That(color.r,Is.GreaterThan(3));else Assert.That(color.r,Is.LessThan(.2f));}foreach(var color in greens.Select(Emission)){Assert.That(color.r,Is.Zero);Assert.That(color.b,Is.Zero);if(red)Assert.That(color.g,Is.LessThan(.2f));else Assert.That(color.g,Is.GreaterThan(.2f));}
 }
 [UnityTest,Timeout(120000)]
 public IEnumerator SingleCinemachineGroundShotKeepsAllTwelveBodiesFinishAndMasterVisible()
 {
  using(var f=new Fixture("Camera"))
  {
   yield return f.Load();yield return f.Start(12);for(int i=0;i<6;i++){yield return null; yield return null;GroundFrame(f,12);}Capture(f,"twelve-racer-ground-shot");
   var dynamicProp=GameObject.CreatePrimitive(PrimitiveType.Cube);var staticProp=GameObject.CreatePrimitive(PrimitiveType.Cube);try{dynamicProp.name="Ground-query truck stand-in";dynamicProp.transform.position=new Vector3(f.camera.transform.position.x,4,f.camera.transform.position.z);dynamicProp.transform.localScale=new Vector3(8,2,8);dynamicProp.AddComponent<Rigidbody>().isKinematic=true;staticProp.name="Ground-query static prop stand-in";staticProp.transform.position=dynamicProp.transform.position+Vector3.up*5;staticProp.transform.localScale=new Vector3(8,2,8);Physics.SyncTransforms();yield return null; yield return null;GroundFrame(f,12);}finally{UnityEngine.Object.DestroyImmediate(dynamicProp);UnityEngine.Object.DestroyImmediate(staticProp);}
   Assert.That(JsonUtility.ToJson((UnityEngine.Object)Get(f.framing,"sharedDefaults")),Is.EqualTo(f.sharedProfile));var menus=Get(f.match,"menus");Assert.That(((Component)Get(menus,"status")).gameObject.activeSelf,Is.False);var plate=((Component)Get(menus,"status")).transform.parent.Find("Broadcast status backplate");if(plate!=null)Assert.That(plate.gameObject.activeSelf,Is.False);
   Call(f.match,"SetupScreen");f.Humans(4);yield return f.Start(4);Call(Get(f.match,"drivingViews"),"Select",2);yield return null;yield return null; yield return null;Assert.That((bool)Get(f.groundCamera,"OwnsGroundShot"),Is.False);Assert.That(((Camera[])Get(f.match,"cameras")).Length,Is.EqualTo(4));var rally=Get(Get(f.match,"drivingViews"),"rally");foreach(var view in (Array)Get(rally,"views"))Assert.That(((Component)Get(view,"rig")).GetComponent(f.groundCamera.GetType()),Is.Null,"Cab/chase rigs cannot inherit the ground-shot extension.");Call(Get(f.match,"drivingViews"),"Select",0);yield return null; yield return null;GroundFrame(f,4);
   TestContext.WriteLine("Original Present+shared LateUpdate each frame produced exactly one adapter evaluation with mode Cinemachine Finalize ground constraint. All12 physical body corners, finish/master, explicit-ground-only ray test, boundedFOV, shared profile immutability, hidden superseded status strip and four independent cabs checked. Controlled720/1080 native captures; no hardware/subjective-readability claim.");
  }
 }
 [UnityTest,Timeout(150000)]
 public IEnumerator ActualLaughWarnsOneSecondThenRedAndPreservesMassiveViolationAndReplay()
 {
  using(var f=new Fixture("Signals"))
  {
   yield return f.Load();f.Humans(1);yield return f.Start(1);InputSystem.QueueStateEvent(f.pads[0],new GamepadState().WithButton(GamepadButton.South));float deadline=Time.realtimeSinceStartup+14;while(Get(f.rules,"Phase").ToString()!="LaughWarning"&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(Get(f.rules,"Phase").ToString(),Is.EqualTo("LaughWarning"));yield return null; yield return null;
   float warningStart=(float)Get(f.rules,"Elapsed")-(float)Get(f.rules,"PhaseTime");Assert.That((bool)Get(f.venue,"WarningVisible"),Is.True);Assert.That((int)Get(f.venue,"WarningPresentations"),Is.EqualTo(1));Assert.That(((Array)Get(f.venue,"warnings")).Cast<Component>().All(label=>label.gameObject.activeInHierarchy),Is.True);TwoAspects(f,false);var laugh=(AudioSource)Get(f.match,"laugh");Assert.That(laugh.isPlaying,Is.True);Assert.That(laugh.clip,Is.Not.Null);Assert.That(laugh.pitch,Is.EqualTo(.7f).Within(.001f));Capture(f,"actual-one-second-laugh-warning");
   float phaseTime=(float)Get(f.rules,"PhaseTime");Call(f.match,"Pause",0,false);yield return new WaitForSecondsRealtime(.2f);Assert.That((float)Get(f.rules,"PhaseTime"),Is.EqualTo(phaseTime));Assert.That((bool)Get(f.venue,"WarningVisible"),Is.True);Call(f.match,"Resume");InputSystem.QueueStateEvent(f.pads[0],new GamepadState());yield return null;yield return null;InputSystem.QueueStateEvent(f.pads[0],new GamepadState().WithButton(GamepadButton.South));
   deadline=Time.realtimeSinceStartup+3;while(Get(f.rules,"Phase").ToString()!="Red"&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(Get(f.rules,"Phase").ToString(),Is.EqualTo("Red"));yield return null; yield return null;Assert.That((float)Get(f.rules,"Elapsed")-(float)Get(f.rules,"PhaseTime")-warningStart,Is.EqualTo(1).Within(.001f));Assert.That((int)Get(f.venue,"WarningPresentations"),Is.EqualTo(1),"Pause/resume cannot restart the laugh-warning presentation.");Assert.That((bool)Get(f.venue,"WarningVisible"),Is.False);TwoAspects(f,true);Capture(f,"actual-red-world-signals");
   Assert.That((float)Get(f.match,"explosionRadius"),Is.EqualTo(20));Assert.That((float)Get(f.match,"explosionVelocity"),Is.EqualTo(12));InputSystem.QueueStateEvent(f.pads[0],new GamepadState{rightTrigger=1}.WithButton(GamepadButton.South));deadline=Time.realtimeSinceStartup+3;while((int)Get(f.match,"Explosions")==0&&Time.realtimeSinceStartup<deadline)yield return null;yield return null; yield return null;Assert.That((int)Get(f.match,"Explosions"),Is.EqualTo(1));var fragment=((Array)Get(f.match,"fragments")).GetValue(0);Assert.That((bool)Get(fragment,"Bursting"),Is.True);Assert.That(((Array)Get(fragment,"pieces")).Cast<object>().Any(piece=>!((Rigidbody)Get(piece,"body")).isKinematic&&((Rigidbody)Get(piece,"body")).linearVelocity.magnitude>3),Is.True);var blast=(ParticleSystem)((Array)Get(f.match,"explosions")).GetValue(0);Assert.That(blast.main.startSize.constantMax,Is.GreaterThanOrEqualTo(15));Assert.That(blast.main.startSpeed.constantMax,Is.GreaterThanOrEqualTo(25));Assert.That(blast.main.maxParticles,Is.GreaterThanOrEqualTo(160));Assert.That(blast.particleCount,Is.GreaterThan(0));Capture(f,"actual-massive-violation");InputSystem.QueueStateEvent(f.pads[0],new GamepadState());
   deadline=Time.realtimeSinceStartup+7;while(!(bool)Get(f.match,"Finished")&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((bool)Get(f.match,"Finished"),Is.True);Assert.That((int)Get(f.match,"Explosions"),Is.EqualTo(1));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);Call(f.match,"ExtraAction","watchredlight");yield return null;yield return null; yield return null;Assert.That((bool)Get(Get(f.match,"review"),"Playing"),Is.True,"The real eliminated run must offer its retained physical recording.");Assert.That((bool)Get(f.groundCamera,"OwnsGroundShot"),Is.False);Assert.That(f.camera.transform.position.y,Is.GreaterThan(3));Capture(f,"original-replay-camera");Call(f.match,"ExtraAction","replayskip");
   Call(f.match,"SetupScreen");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);yield return null; yield return null;Assert.That((int)Get(f.match,"Explosions"),Is.Zero);Assert.That((int)Get(f.venue,"WarningPresentations"),Is.Zero);Assert.That((bool)Get(f.venue,"WarningVisible"),Is.False);Assert.That((bool)Get(fragment,"Bursting"),Is.False);
   TestContext.WriteLine("Actual unchanged rule transition/laugh audio/UTS master warning lasted1simulated second (pause excluded); new world-only white warning and green pulse became RED without anyyellow state. Assigned accelerator+handbrake caused one real original massive fragmentation/fireball, zero invented finish score, actual elimination result/replay camera and rematch. Native software input/captures only.");
  }
 }
}
#endif
