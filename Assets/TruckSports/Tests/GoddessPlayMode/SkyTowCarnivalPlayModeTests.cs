#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SkyTowCarnivalPlayModeTests
{
 const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
 static ParticleSystem[] OwnedParticles(Component effects)=>((Array)Get(effects,"upcoming")).Cast<object>().Concat(((Array)Get(effects,"success")).Cast<object>()).Select(gate=>(ParticleSystem)Get(gate,"particles")).ToArray();
 static Material[][] Materials(Component[] rings)=>rings.Select(ring=>((Renderer)Get(ring,"art")).sharedMaterials).ToArray();
 static string MaterialIdentity(Material material)
 {
  if(ReferenceEquals(material,null))return "CLR-null";
  if(material==null)return "destroyed Unity wrapper";
  return "id="+material.GetInstanceID()+" name="+material.name+" shader="+(material.shader!=null?material.shader.name:"NULL")+" asset="+AssetDatabase.GetAssetPath(material)+" flags="+material.hideFlags;
 }
 static void MaterialTelemetry(string stage,Component[] rings,Material[][] captured=null,string[][] savedIdentity=null)
 {
  var current=captured??Materials(rings);int alive=current.Sum(slots=>slots.Count(material=>material!=null));int destroyed=current.Sum(slots=>slots.Count(material=>!ReferenceEquals(material,null)&&material==null));int empty=current.Sum(slots=>slots.Count(material=>ReferenceEquals(material,null)));
  TestContext.WriteLine("SKYTOW_MATERIAL_LIFETIME "+stage+" live="+alive+" destroyedWrappers="+destroyed+" CLRnull="+empty);
  foreach(int index in new[]{0,5,rings.Length-1}.Distinct())for(int slot=0;slot<current[index].Length;slot++)TestContext.WriteLine("SKYTOW_MATERIAL_ID "+stage+" ring="+index+" slot="+slot+" "+MaterialIdentity(current[index][slot])+(savedIdentity!=null?" originally={"+savedIdentity[index][slot]+"}":""));
 }
 static void AssertLiveMaterials(string stage,Component[] rings)
 {
  for(int i=0;i<rings.Length;i++){var materials=((Renderer)Get(rings[i],"art")).sharedMaterials;Assert.That(materials.Length,Is.EqualTo(4),stage+" ring="+i);for(int slot=0;slot<materials.Length;slot++)Assert.That(materials[slot]!=null,Is.True,stage+" ring="+i+" slot="+slot+" "+MaterialIdentity(materials[slot]));}
 }
 static Material[] AuthoredMaterials(Component ring)
 {
  Material Load(string name){var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckSkyTow/Materials/"+name+".mat");Assert.That(material,Is.Not.Null,name);return material;}
  var color=Load((bool)Get(ring,"bonus")?"Gold bonus rings":"Expedition safety coral");var white=Load("Expedition warm white");return new[]{color,Load("Arctic marine navy"),white,(bool)Get(ring,"bonus")?color:white};
 }
 sealed class Fixture:IDisposable
 {
  readonly IDisposable prefs;readonly GameObject isolation;public Behaviour match;
  public Fixture(string suffix){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"SkyTowCarnival"+suffix});isolation=new GameObject("Sky Tow Carnival source isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
  public object rules=>Get(match,"rules");public object course=>Get(match,"course");public Component effects=>(Component)Get(match,"RingEffects");public object Racer(int slot)=>((Array)Get(match,"racers")).GetValue(slot);public Component Ring(int slot,int index)=>(Component)Call(course,"Ring",slot,index);
  public IEnumerator Load(int count)
  {
   EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSkyTow/Scenes/TruckSkyTow.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
   match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckSkyTow.SkyTowMatch")).Cast<Behaviour>().Single();Call(match,"AllBots");Set(Get(match,"setup"),"count",count);Call(match,"NormalizeSetup");var presets=Get(match,"presets");foreach(var preset in (Array)Get(presets,"entries"))if((string)Get(preset,"displayName")=="Clear"){Call(presets,"Select",Get(preset,"id"));break;}Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});
  }
  public void Dispose(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
 }
 static void Capture(Fixture f,string label)
 {
  string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/SkyTowCarnivalNative");Directory.CreateDirectory(folder);var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var cameras=(Camera[])Get(f.match,"cameras");var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);foreach(int width in new[]{1280,1920})save.Invoke(null,new object[]{canvas,cameras,width,width==1280?720:1080,Path.Combine(folder,label+"-"+width+"-controlled.png"),null});
 }
 [UnityTest,Timeout(120000)]
 public IEnumerator ActualTowPassesAnimateBoundedPoolsWithoutMovingFramesAndCleanlyRematch()
 {
  using(var f=new Fixture("Flight"))
  {
   yield return f.Load(2);var rings=((Array)Get(f.course,"rings")).Cast<Component>().ToArray();var positions=rings.Select(ring=>ring.transform.position).ToArray();var rotations=rings.Select(ring=>ring.transform.rotation).ToArray();var scales=rings.Select(ring=>ring.transform.localScale).ToArray();var originalMaterials=Materials(rings);var originalIdentity=originalMaterials.Select(slots=>slots.Select(MaterialIdentity).ToArray()).ToArray();MaterialTelemetry("beforeStart",rings);AssertLiveMaterials("beforeStart",rings);Assert.That((bool)Call(f.match,"StartMatch"),Is.True);MaterialTelemetry("afterStart",rings);AssertLiveMaterials("afterStart",rings);float deadline=Time.realtimeSinceStartup+40;
   while((int)Get(f.match,"PhysicalRings")==0&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((int)Get(f.match,"PhysicalRings"),Is.GreaterThan(0),"Original spline/tether/AI must produce a genuine completed physical gate passage.");Assert.That((int)Get(f.effects,"SuccessfulBursts"),Is.EqualTo((int)Get(f.match,"PhysicalRings")));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo((int)Get(f.match,"PhysicalRings")));Assert.That((int)Get(f.effects,"PoolCount"),Is.EqualTo(16));
   // The component lives on the match root: a descendant search also counts
   // the original Weatherade gate-feedback pools. Audit this owner's arrays.
   var particles=OwnedParticles(f.effects);Assert.That(particles.Length,Is.EqualTo(16));Assert.That(particles.Distinct().Count(),Is.EqualTo(16));Assert.That(particles.All(effect=>effect.main.maxParticles<=96&&effect.trails.enabled&&effect.shape.shapeType==ParticleSystemShapeType.Circle),Is.True);Assert.That(particles.Sum(effect=>effect.particleCount),Is.GreaterThan(0));Assert.That(particles.Any(effect=>effect.GetComponentInChildren<Light>(true)!=null),Is.False);TestContext.WriteLine("SKYTOW_POOL owned="+particles.Length+" capacity="+particles.Sum(effect=>effect.main.maxParticles)+" existingOtherSystems="+(f.effects.GetComponentsInChildren<ParticleSystem>(true).Length-particles.Length));
   float minimum=1,maximum=0;for(int i=0;i<12;i++){yield return new WaitForSeconds(.04f);float pulse=(float)Get(f.effects,"pulse");minimum=Mathf.Min(minimum,pulse);maximum=Mathf.Max(maximum,pulse);}Assert.That(maximum-minimum,Is.GreaterThan(.02f),"DOTween must actually pulse, rather than leave a static energy material.");Capture(f,"actual-tow-ring-pass");
   for(int i=0;i<rings.Length;i++){Assert.That(rings[i].transform.position,Is.EqualTo(positions[i]));Assert.That(Quaternion.Angle(rings[i].transform.rotation,rotations[i]),Is.LessThan(.001f));Assert.That(rings[i].transform.localScale,Is.EqualTo(scales[i]));}
   var beforePauseMaterials=Materials(rings);MaterialTelemetry("beforePause",rings);AssertLiveMaterials("beforePause",rings);float age=(float)Get(f.match,"Age"),pausedPulse=(float)Get(f.effects,"pulse");var moving=particles.First(effect=>effect.gameObject.activeInHierarchy);float requestedParticleTime=moving.time;Call(f.match,"Pause",0,false);
   // Native particles can finish work scheduled for the frame whose Update
   // requested pause. Establish their baseline after that frame completes;
   // the synchronous gameplay clock and DOTween pulse keep their exact guards.
   yield return null;yield return null;float particleTime=moving.time;Assert.That((float)Get(f.match,"Age"),Is.EqualTo(age));Assert.That((float)Get(f.effects,"pulse"),Is.EqualTo(pausedPulse));
   yield return new WaitForSecondsRealtime(.25f);TestContext.WriteLine("SKYTOW_PAUSE requestedParticle="+requestedParticleTime+" completedFrameParticle="+particleTime+" heldParticle="+moving.time+" gameplayAge="+Get(f.match,"Age")+" pulse="+Get(f.effects,"pulse"));Assert.That((float)Get(f.match,"Age"),Is.EqualTo(age));Assert.That((float)Get(f.effects,"pulse"),Is.EqualTo(pausedPulse));Assert.That(moving.time,Is.EqualTo(particleTime).Within(.001f));MaterialTelemetry("afterPause",rings);AssertLiveMaterials("afterPause",rings);for(int i=0;i<rings.Length;i++)CollectionAssert.AreEqual(beforePauseMaterials[i],((Renderer)Get(rings[i],"art")).sharedMaterials,"Pause must retain live weather material identities.");Call(f.match,"Resume");
   var beforeClear=Materials(rings);Call(f.match,"SetupScreen");Assert.That((bool)Get(f.effects,"Running"),Is.False);Assert.That(particles.All(effect=>effect.particleCount==0),Is.True);MaterialTelemetry("afterSetup",rings);MaterialTelemetry("beforeStartSnapshotNow",rings,originalMaterials,originalIdentity);AssertLiveMaterials("afterSetup",rings);for(int i=0;i<rings.Length;i++)CollectionAssert.AreEqual(beforeClear[i],((Renderer)Get(rings[i],"art")).sharedMaterials,"Ring Clear must not assign cached weather clones or replace another owner's live material.");
   // Explicit ownership fixture: stop the caller while exercising its existing
   // snow restore API synchronously. Authored assets, not expired weather
   // wrappers captured before StartMatch, are the restoration contract.
   var snow=f.match.GetComponent(Runtime("LWS.TruckSports.SportsSnowSurface")) as Behaviour;Assert.That(snow,Is.Not.Null);var surface=(Behaviour)Get(snow,"surface");bool snowEnabled=snow.enabled,surfaceEnabled=surface.enabled;
   try
   {
    snow.enabled=false;surface.enabled=false;MaterialTelemetry("snowRestoreAuthored",rings);AssertLiveMaterials("snowRestoreAuthored",rings);
    for(int i=0;i<rings.Length;i++){var expected=AuthoredMaterials(rings[i]);var actual=((Renderer)Get(rings[i],"art")).sharedMaterials;CollectionAssert.AreEqual(expected,actual,"The original snow-owner restore must recover genuine authored refs on ring "+i);Assert.That(actual.All(material=>AssetDatabase.Contains(material)&&!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material))),Is.True);}
   }
   finally{surface.enabled=surfaceEnabled;snow.enabled=snowEnabled;}
   yield return null;yield return null;MaterialTelemetry("snowReenabled",rings);AssertLiveMaterials("snowReenabled",rings);
   Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Assert.That((int)Get(f.effects,"PoolCount"),Is.EqualTo(16));CollectionAssert.AreEquivalent(particles,OwnedParticles(f.effects));Assert.That((int)Get(f.effects,"SuccessfulBursts"),Is.Zero);Assert.That((int)Get(f.match,"PhysicalRings"),Is.Zero);Assert.That(rings.All(ring=>(int)Get(ring,"Outcome")==0),Is.True);Assert.That(((int[])Get(f.rules,"score")).Sum(),Is.Zero);var block=new MaterialPropertyBlock();((Renderer)Get(rings[0],"art")).GetPropertyBlock(block);Assert.That(block.GetColor("_BaseColor"),Is.EqualTo(Color.clear),"A previous success tint cannot leak into the new ring.");
   MaterialTelemetry("rematchBegin",rings);AssertLiveMaterials("rematchBegin",rings);deadline=Time.realtimeSinceStartup+40;while((int)Get(f.match,"PhysicalRings")==0&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((int)Get(f.match,"PhysicalRings"),Is.GreaterThan(0),"Rematch must produce a second actual physical tow pass, not only rebuild metadata.");Assert.That((int)Get(f.effects,"SuccessfulBursts"),Is.EqualTo((int)Get(f.match,"PhysicalRings")));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo((int)Get(f.match,"PhysicalRings")));MaterialTelemetry("rematchActualPass",rings);AssertLiveMaterials("rematchActualPass",rings);Call(f.match,"SetupScreen");yield return null;yield return null;MaterialTelemetry("secondSetupSettled",rings);AssertLiveMaterials("secondSetupSettled",rings);Assert.That(particles.All(effect=>effect.particleCount==0),Is.True);
   TestContext.WriteLine("Actual saved Arctic spline/NWH-wheel truck/PhysX tether/AI produced ring receipts in BOTH matches with reused16 bounded energy pools. DOTween pulse, all144 unchanged frame poses, exact pause guards, live weather material lifetimes and genuine authored refs through existing snow restore/reenable checked. Controlled native captures; not Windows/human/full-course acceptance.");
  }
 }
 [UnityTest,Timeout(90000)]
 public IEnumerator ProductionRacerResolvesPartialBodyOnceButRejectsOutsideFrameReverseAndNoncrossing()
 {
  using(var f=new Fixture("GeometryIntegration"))
  {
   yield return f.Load(1);f.match.enabled=false;
   foreach(int test in new[]{0,1,2,3})
   {
    Call(f.match,"SetupScreen");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Time.timeScale=0;var racer=f.Racer(0);Call(racer,"Release");var ring=f.Ring(0,0);var truck=Get(racer,"truck");var body=(Rigidbody)Get(truck,"body");var shape=(BoxCollider)Get(truck,"shape");var scale=((Component)truck).transform.lossyScale;var center=Vector3.Scale(shape.center,scale);float radius=(float)Get(ring,"ClearRadius"),half=shape.size.x*Mathf.Abs(scale.x)*.5f;float lateral=radius+(test==1?half+.1f:half*.5f);float start=test==2?3:-3,end=test==2?-3:test==3?-3:3;
    var before=ring.transform.position+ring.transform.right*lateral+ring.transform.forward*start-ring.transform.rotation*center;var after=ring.transform.position+ring.transform.right*lateral+ring.transform.forward*end-ring.transform.rotation*center;Set(racer,"previous",before);Set(racer,"previousRotation",ring.transform.rotation);body.position=after;body.rotation=ring.transform.rotation;body.transform.SetPositionAndRotation(after,ring.transform.rotation);body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
    float at=(float)Get(ring,"at");object[] frame={at,0,Vector3.zero,Quaternion.identity};f.course.GetType().GetMethod("Frame").Invoke(f.course,frame);var boat=(Rigidbody)Get(racer,"boat");boat.position=(Vector3)frame[2];boat.rotation=(Quaternion)frame[3];boat.transform.SetPositionAndRotation((Vector3)frame[2],(Quaternion)frame[3]);((bool[])Get(racer,"touched"))[0]=true;Physics.SyncTransforms();
    Assert.That(Vector3.Distance(body.position,after),Is.LessThan(.001f),"The interpolated body must retain the controlled physics pose after transform synchronization.");Assert.That(Vector3.Distance(boat.position,(Vector3)frame[2]),Is.LessThan(.001f));Assert.That(Vector3.Distance(body.position,boat.position),Is.LessThan((float)Get(racer,"ropeLength")+5),"This ring fixture must not trigger the independent cable-failure rule.");
    TestContext.WriteLine("SKYTOW_CONTROLLED beforeStep case="+test+" before="+before+" after="+body.position+" ring="+ring.transform.position+" rotation="+body.rotation+" collider="+shape.size+" scale="+scale+" lateral="+lateral+" clear="+radius+" cable="+Vector3.Distance(body.position,boat.position));
    Call(racer,"Step",at,Vector2.zero,.02f);int[,] outcomes=(int[,])Get(f.rules,"outcomes");TestContext.WriteLine("SKYTOW_CONTROLLED afterStep case="+test+" position="+body.position+" approached="+((bool[])Get(racer,"approached"))[0]+" opened="+((bool[])Get(racer,"opened"))[0]+" failed="+Get(racer,"Failed")+" outcome="+outcomes[0,0]);int expected=test==0?1:test==1?-1:0;Assert.That(outcomes[0,0],Is.EqualTo(expected));Assert.That(((int[])Get(f.rules,"hits"))[0],Is.EqualTo(test==0?1:0));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(test==0?1:0));Assert.That((bool)Get(racer,"Failed"),Is.False);int score=((int[])Get(f.rules,"score"))[0],bursts=(int)Get(f.effects,"SuccessfulBursts");Call(racer,"Step",at+.02f,Vector2.zero,.02f);Assert.That(((int[])Get(f.rules,"score"))[0],Is.EqualTo(score));Assert.That((int)Get(f.effects,"SuccessfulBursts"),Is.EqualTo(bursts));
   }
   TestContext.WriteLine("Explicit controlled saved-body before/after fixtures called the original production Racer.Step with an aligned real course boat. Partial body enters→one score even with rim-contact flag; frame-only→miss; reverse/fully-before→no score; repeat Step cannot duplicate. This is geometry integration, not a naturally flown edge-pass claim; the separate actual tow test supplies native flight evidence.");
  }
 }
}
#endif
