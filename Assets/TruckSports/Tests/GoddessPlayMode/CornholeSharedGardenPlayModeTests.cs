#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class CornholeSharedGardenPlayModeTests
{
 const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static Type TypeOf(string name)=>Type.GetType("LWS.TruckCornhole."+name+", Assembly-CSharp",true);
 static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value){if(owner.GetType().GetField(name,Any) is FieldInfo field)field.SetValue(owner,value);else owner.GetType().GetProperty(name,Any).GetSetMethod(true).Invoke(owner,new[]{value});}
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length&&method.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(owner,args);
 static object[] Courses(object match)=>((Array)Get(match,"courses")).Cast<object>().ToArray();
 static object[] Trucks(object match)=>Courses(match).SelectMany(course=>((Array)Get(course,"trucks")).Cast<object>()).ToArray();
 static Rigidbody Body(object truck)=>(Rigidbody)Get(truck,"body");
 static IDisposable Preferences(string key){var result=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{key});Sports("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);return result;}
 static GameObject Isolation(){var go=new GameObject("Explicit Cornhole shared garden native acceptance");UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent(Sports("SportsProbeInputIsolation"));return go;}
 static IEnumerator Load(){EditorSceneManager.LoadSceneInPlayMode("Assets/TruckCornhole/Scenes/TruckCornhole.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;}
 static Component Match()=>(Component)UnityEngine.Object.FindAnyObjectByType(TypeOf("CornholeMatch"));
 static void Start(object match,int count){Set(Get(match,"setup"),"count",count);Call(match,"AllAI");Sports("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{true});foreach(var player in ((Array)Get(Get(match,"setup"),"players")).Cast<object>())Set(player,"difficulty",Enum.Parse(Get(player,"difficulty").GetType(),"Hard"));var presets=Get(match,"weatherPresets");var clear=((Array)Get(presets,"entries")).Cast<object>().FirstOrDefault(entry=>(string)Get(entry,"displayName")=="Clear");if(clear!=null)Call(presets,"Select",Get(clear,"id"));Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=1;}
 static void Capture(Component match,string name){var save=Sports("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);var cameras=Courses(match).Select(course=>(Camera)Get(Get(course,"chase"),"view")).Where(camera=>camera.enabled).ToArray();foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{((Component)Get(match,"menus")).GetComponent<Canvas>(),cameras,size.x,size.y,"Logs/TruckSports/PlaytestRepair/CornholeSharedGarden/"+name+"-"+size.y+".png",null});}
 [UnityTest,Timeout(240000)]
 public IEnumerator NaturalFourOriginalAiApproachesShareTheActualBoardMakeCrossOwnerContactsAndBankOnce()
 {
  var prefs=Preferences("CornholeSharedNatural");var isolation=Isolation();Component match=null;var random=UnityEngine.Random.state;
  try
  {
   yield return Load();match=Match();UnityEngine.Random.InitState(174207);Start(match,4);var courses=Courses(match);var targets=courses.Select(course=>Get(course,"sharedTarget")).ToArray();Assert.That(targets[0],Is.Not.Null);Assert.That(targets.All(target=>ReferenceEquals(target,targets[0])),Is.True);var board=(Transform)Get(courses[0],"board");Assert.That(courses.All(course=>Get(course,"board")==board),Is.True);Assert.That(board.GetComponent<MeshCollider>().convex,Is.False);
   var surfaces=UnityEngine.Object.FindObjectsByType(TypeOf("CornholeSurface"),FindObjectsInactive.Exclude,FindObjectsSortMode.None).Cast<Component>().ToArray();Assert.That(surfaces.Count(surface=>Get(surface,"kind").ToString()=="Board"),Is.EqualTo(1));Assert.That(surfaces.Count(surface=>Get(surface,"kind").ToString()=="Ground"),Is.EqualTo(1));
   var pads=surfaces.Where(surface=>Get(surface,"kind").ToString()=="Trampoline").Select(surface=>surface.GetComponent<Collider>()).ToArray();Assert.That(pads.Length,Is.EqualTo(4));for(int i=0;i<4;i++)for(int j=i+1;j<4;j++)Assert.That(Physics.ComputePenetration(pads[i],pads[i].transform.position,pads[i].transform.rotation,pads[j],pads[j].transform.position,pads[j].transform.rotation,out _,out _),Is.False,"Original launch platforms must not overlap.");
   var first=courses.Select(course=>Get(course,"Current")).ToArray();float deadline=Time.realtimeSinceStartup+25;while(first.Any(truck=>!(bool)Get(truck,"Launched"))&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That(first.All(truck=>(bool)Get(truck,"Launched")&&(int)Get(truck,"LaunchCount")==1),Is.True,"Each original AI must drive its own real NWH ramp and trampoline. No supplied body poses/velocities/flags in this test.");
   deadline=Time.realtimeSinceStartup+150;while((int)Get(match,"RoundFinalizations")==0&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That(Get(match,"RoundFinalizations"),Is.EqualTo(1));Call(match,"Pause",0,false);
   var trucks=Trucks(match);int contacts=trucks.Sum(truck=>(int)Get(truck,"CrossParticipantImpacts"));int boarded=trucks.Count(truck=>(float)Get(truck,"BoardContactTime")>=0);int holed=trucks.Count(truck=>(bool)Get(truck,"Holed"));TestContext.WriteLine("NATURAL_SHARED_CORNHOLE contacts="+contacts+" actualBoardContacts="+boarded+" wholeBodyHoles="+holed+" retained="+trucks.Count(truck=>((Component)truck).gameObject.activeSelf));Assert.That(contacts,Is.GreaterThan(0),"Natural original AI trajectories must create real opposing-owner native contacts at this single board.");Assert.That(boarded,Is.GreaterThanOrEqualTo(4));Assert.That(trucks.All(truck=>((Component)truck).gameObject.activeSelf),Is.True,"All sixteen original attempts remain until the whole round clears.");
   var totals=(int[])Get(match,"totals");var final=(int[])Get(match,"lastRound");Assert.That(totals.Sum(),Is.GreaterThan(0));for(int i=0;i<4;i++){Assert.That(totals[i],Is.EqualTo(final[i]));Assert.That(final[i],Is.EqualTo((int)Get(courses[i],"Provisional")));}int receipts=(int)Get(Get(match,"ScoreJournal"),"Count");Assert.That(receipts,Is.EqualTo(totals.Count(total=>total>0)));var saved=(int[])totals.Clone();yield return new WaitForSecondsRealtime(.3f);Assert.That(Get(match,"RoundFinalizations"),Is.EqualTo(1));Assert.That(totals,Is.EqualTo(saved));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(receipts));Capture(match,"natural-four-first-round");
   Call(match,"ChangeSetup");Start(match,4);Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Assert.That(((int[])Get(match,"totals")).Sum(),Is.Zero);Assert.That(Trucks(match).Sum(truck=>(int)Get(truck,"CrossParticipantImpacts")),Is.Zero);
  }
  finally{UnityEngine.Random.state=random;Time.timeScale=1;if(match!=null)Call(match,"ChangeSetup");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
 }
 [UnityTest,Timeout(240000)]
 public IEnumerator OriginalOneToFourViewportsAndBoundedSixteenTruckReplayHideLiveOpponentsAndKeepPrivateCabs()
 {
  var prefs=Preferences("CornholeSharedReplay");var isolation=Isolation();Component match=null;
  try
  {
   yield return Load();match=Match();var courses=Courses(match);
   for(int count=1;count<=4;count++)
   {
    Call(match,"ChangeSetup");Start(match,count);Sports("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{true});Call(match,"Layout");
    for(int i=0;i<4;i++){var camera=(Camera)Get(Get(courses[i],"chase"),"view");Assert.That(camera.enabled,Is.EqualTo(i<count));if(i>=count)continue;var rect=(Rect)TypeOf("CornholeMatch").GetMethod("ViewRect").Invoke(null,new object[]{count,i});Assert.That(camera.rect,Is.EqualTo(rect));Assert.That(camera.cullingMask&(1<<29),Is.Not.Zero);Assert.That(camera.cullingMask&(1<<28),Is.Not.Zero);for(int j=0;j<4;j++)Assert.That((camera.cullingMask&(1<<(24+j)))!=0,Is.EqualTo(i==j));}
    var first=Get(courses[0],"Current");float deadline=Time.realtimeSinceStartup+25;while(!(bool)Get(first,"Launched")&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That(Get(first,"Launched"),Is.True);Assert.That(Get(Get(courses[0],"replay"),"RecordedTruckCount"),Is.EqualTo(count*4));Capture(match,"roster-"+count);
   }
   var replay=Get(courses[0],"replay");float until=Time.realtimeSinceStartup+45;while(!(bool)Get(replay,"Available")&&Time.realtimeSinceStartup<until)yield return new WaitForFixedUpdate();Assert.That(Get(replay,"Available"),Is.True);Assert.That(Get(replay,"FrameCount"),Is.GreaterThan(80));Assert.That(Call(replay,"Play"),Is.True);Time.timeScale=0;
   Assert.That((int)TypeOf("CornholeReplay").GetField("MaximumSamplePayloadBytes").GetRawConstantValue(),Is.EqualTo(722400));var sources=Trucks(match);var positions=sources.Select(truck=>Body(truck).position).ToArray();var velocities=sources.Select(truck=>Body(truck).linearVelocity).ToArray();var ghosts=(Transform[])Get(replay,"ghosts");Assert.That(ghosts.Count(ghost=>ghost!=null),Is.EqualTo(16));Assert.That(ghosts.All(ghost=>ghost.GetComponentsInChildren<Collider>(true).Length==0&&ghost.GetComponentsInChildren<Rigidbody>(true).Length==0),Is.True);
   var cameraOwner=Get(courses[0],"chase");var cameraView=(Camera)Get(cameraOwner,"view");
   for(int angle=0;angle<4;angle++)
   {
    Set(replay,"Angle",angle);foreach(float fraction in new[]{0,.35f,.9f}){Set(replay,"Clock",(float)Get(replay,"Duration")*fraction);Call(replay,"Apply");Call(replay,"PositionCamera",cameraOwner);Call(cameraOwner,"Begin",default(ScriptableRenderContext),cameraView);Assert.That(sources.SelectMany(truck=>(Renderer[])Get(truck,"Visuals")).All(renderer=>renderer.forceRenderingOff),Is.True,"Replay draws recorded opponents only; every live truck is hidden for this viewport.");for(int i=4;i<16;i++)Assert.That(ghosts[i].GetComponentsInChildren<Renderer>(true).All(renderer=>!renderer.forceRenderingOff),Is.True);Call(cameraOwner,"End",default(ScriptableRenderContext),cameraView);Assert.That(sources.SelectMany(truck=>(Renderer[])Get(truck,"Visuals")).All(renderer=>!renderer.forceRenderingOff),Is.True);for(int i=0;i<16;i++){Assert.That(Body(sources[i]).position,Is.EqualTo(positions[i]));Assert.That(Body(sources[i]).linearVelocity,Is.EqualTo(velocities[i]));}}
   }
   Call(replay,"Rewind");var sample=((Array)Get(replay,"samples")).GetValue(0,4);var recorded=(Vector3)Get(sample,"position");var parent=ghosts[4].parent;var roundtrip=parent!=null?parent.TransformPoint(parent.InverseTransformPoint(recorded)):recorded;float renderError=Vector3.Distance(ghosts[4].position,recorded),conversionError=Vector3.Distance(roundtrip,recorded);TestContext.WriteLine("CORN_REPLAY_RENDER_ROUNDTRIP recorded="+recorded.ToString("R")+" ghost="+ghosts[4].position.ToString("R")+" expectedParentRoundtrip="+roundtrip.ToString("R")+" renderErrorMetres="+renderError.ToString("R")+" parentConversionMetres="+conversionError.ToString("R")+" parentMatrix="+(parent!=null?parent.localToWorldMatrix.ToString("R"):"identity"));Assert.That(conversionError,Is.LessThanOrEqualTo(1e-5f));Assert.That(renderError,Is.LessThanOrEqualTo(1e-5f),"Ghost render pose only: original course parent has nonidentity translated/yaw fan frame. Live bodies/velocities above retain exact assertions.");Call(match,"Pause",0,false);float clock=(float)Get(replay,"Clock");yield return new WaitForSecondsRealtime(.2f);Assert.That(Get(replay,"Clock"),Is.EqualTo(clock));Capture(match,"replay-recorded-opponents");Call(match,"ChangeSetup");Assert.That(ghosts.All(ghost=>!ghost.gameObject.activeSelf),Is.True);
  }
  finally{Time.timeScale=1;if(match!=null)Call(match,"ChangeSetup");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
 }
 [UnityTest,Timeout(120000)]
 public IEnumerator ControlledCrossOwnerNativePushCanRevokeARealProvisionalBoardPointWithoutInventedPoints()
 {
  var prefs=Preferences("CornholeSharedPush");var isolation=Isolation();Component match=null;
  try
  {
   yield return Load();match=Match();Start(match,2);var courses=Courses(match);var trucks=courses.Take(2).Select(course=>Get(course,"Current")).ToArray();float deadline=Time.realtimeSinceStartup+25;while(trucks.Any(truck=>!(bool)Get(truck,"Launched"))&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That(trucks.All(truck=>(bool)Get(truck,"Launched")),Is.True);((Behaviour)match).enabled=false;foreach(var course in courses)((Behaviour)course).enabled=false;foreach(var truck in trucks)Set(truck,"command",Vector2.zero);var board=(Transform)Get(courses[0],"board");var policy=Get(match,"landingCollisions");
   // Supplied encounter geometry is deliberately separate from natural-AI acceptance.
   void Seed(object truck,Vector3 local,Vector3 velocity){var body=Body(truck);body.position=board.TransformPoint(local);body.rotation=board.rotation;((Component)truck).transform.SetPositionAndRotation(body.position,body.rotation);body.linearVelocity=board.TransformDirection(velocity);body.angularVelocity=Vector3.zero;body.WakeUp();Physics.SyncTransforms();}
   Seed(trucks[0],new Vector3(2.3f,.45f,6),Vector3.zero);Seed(trucks[1],new Vector3(5,3,0),Vector3.zero);deadline=Time.realtimeSinceStartup+7;while((int)Get(trucks[0],"Points")!=1&&Time.realtimeSinceStartup<deadline){Call(policy,"Tick");yield return new WaitForFixedUpdate();}Assert.That(Get(trucks[0],"Points"),Is.EqualTo(1),"Point must come from original native top support and stable time.");
   Seed(trucks[1],new Vector3(3.75f,.22f,6),new Vector3(-10,-.1f,0));Call(policy,"Tick");deadline=Time.realtimeSinceStartup+10;while(((int)Get(trucks[0],"CrossParticipantImpacts")==0||!(bool)Get(trucks[0],"Holed")&&!(bool)Get(trucks[0],"Grounded"))&&Time.realtimeSinceStartup<deadline){Call(policy,"Tick");yield return new WaitForFixedUpdate();}Assert.That(Get(trucks[0],"CrossParticipantImpacts"),Is.GreaterThan(0));Assert.That((bool)Get(trucks[0],"Holed")||(bool)Get(trucks[0],"Grounded"),Is.True,"Push must actually send the target through the original hole or off the physical board; temporary instability is insufficient.");Assert.That((int)Get(trucks[0],"Points"),Is.EqualTo(0).Or.EqualTo(3));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero,"Controlled physical provisional changes cannot bank a round.");Assert.That(trucks.All(truck=>((Component)truck).gameObject.activeSelf),Is.True);TestContext.WriteLine("CONTROLLED_SHARED_PUSH outcome="+Get(trucks[0],"Points")+" holed="+Get(trucks[0],"Holed")+" ground="+Get(trucks[0],"Grounded")+" nativeCrossContacts="+Get(trucks[0],"CrossParticipantImpacts")+"; supplied encounter, original support/hole authority, no supplied points.");
   foreach(var heading in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right}){var atmosphere=Get(match,"atmosphere");Set(atmosphere,"Flow",-heading*30);var wind=(Vector3)Call(match,"EffectiveWind",heading);Assert.That(Vector3.Dot(wind,heading),Is.GreaterThanOrEqualTo(-4.4705f));}
  }
  finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;foreach(var course in Courses(match))((Behaviour)course).enabled=true;Call(match,"ChangeSetup");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
 }
 [UnityTest,Timeout(90000)]
 public IEnumerator GardenUsesActualHumanWalkIdleAndReactionClipsAtDocumentedMiniatureArtScaleWithSafeCleanup()
 {
  var prefs=Preferences("CornholeSharedArt");var isolation=Isolation();Component match=null;
  try
  {
   yield return Load();match=Match();Start(match,1);var venue=(Component)UnityEngine.Object.FindAnyObjectByType(TypeOf("CornholeYardPresentation"));Assert.That(venue,Is.Not.Null);Assert.That(Get(venue,"artUnitsPerMetre"),Is.EqualTo(20));var actors=(Animator[])Get(venue,"spectators");Assert.That(actors.Length,Is.EqualTo(32));Assert.That(actors.All(actor=>actor.isHuman&&actor.avatar.isValid&&actor.runtimeAnimatorController.animationClips.All(clip=>clip.humanMotion)),Is.True);Assert.That(actors.All(actor=>actor.runtimeAnimatorController.animationClips.Select(clip=>clip.name).Any(name=>name.Contains("walk"))),Is.True);Assert.That(venue.GetComponentsInChildren<Collider>(true).Length,Is.Zero);Assert.That(venue.GetComponentsInChildren<Rigidbody>(true).Length,Is.Zero);Assert.That(venue.GetComponentsInChildren<Camera>(true).Length,Is.Zero);Assert.That(venue.GetComponentsInChildren<AudioListener>(true).Length,Is.Zero);Assert.That(((Array)Get(venue,"yardProps")).Length,Is.GreaterThan(35));
   yield return null;yield return null;var first=actors[0];var bone=first.GetBoneTransform(HumanBodyBones.LeftLowerLeg);var rotation=bone.localRotation;var position=first.transform.position;float normalized=first.GetCurrentAnimatorStateInfo(0).normalizedTime;yield return new WaitForSeconds(.65f);Assert.That(first.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.GreaterThan(normalized));Assert.That(Quaternion.Angle(rotation,bone.localRotation),Is.GreaterThan(.5f));Assert.That(Vector3.Distance(position,first.transform.position),Is.GreaterThan(.05f));Assert.That(actors.Take(8).All(actor=>Mathf.Abs(actor.transform.position.x)>60),Is.True);
   var skin=first.GetComponentInChildren<SkinnedMeshRenderer>();Assert.That(skin.bounds.size.y,Is.InRange(25,44));Call(match,"Pause",0,false);yield return null;yield return null;position=first.transform.position;rotation=bone.localRotation;yield return new WaitForSecondsRealtime(.25f);Assert.That(first.transform.position,Is.EqualTo(position));Assert.That(Quaternion.Angle(rotation,bone.localRotation),Is.LessThan(.01f));Call(match,"Resume");float deadline=Time.realtimeSinceStartup+25;while((int)Get(venue,"Reactions")==0&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That(Get(venue,"Reactions"),Is.GreaterThan(0),"Actual landing/miss/hole state must trigger the owned reaction presentation.");Capture(match,"normal-proportion-giant-spectators");var ids=actors.Select(actor=>actor.GetInstanceID()).ToArray();Call(match,"ChangeSetup");Start(match,1);Assert.That(((Animator[])Get(venue,"spectators")).Select(actor=>actor.GetInstanceID()).ToArray(),Is.EqualTo(ids));
  }
  finally{Time.timeScale=1;if(match!=null)Call(match,"ChangeSetup");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
 }
}
#endif
