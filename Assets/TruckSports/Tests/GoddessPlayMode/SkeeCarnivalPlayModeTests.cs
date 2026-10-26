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
using UnityEngine.UI;

public sealed class SkeeCarnivalPlayModeTests
{
 const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
 static Vector2Int FeedSize(Rect rect,int width,int height){float reduction=Mathf.Min(width/(float)Screen.width,height/(float)Screen.height);return new Vector2Int(Mathf.Max(1,Mathf.RoundToInt(Screen.width*reduction*rect.width)),Mathf.Max(1,Mathf.RoundToInt(Screen.height*reduction*rect.height)));}
 [Serializable]sealed class PixelCameraSnapshot
 {
  public Vector3 position;public Quaternion rotation;public float aspect;public Rect rect;public Matrix4x4 projection;public int mask;public CameraClearFlags clear;public Color background;
  public static PixelCameraSnapshot Read(Camera camera)=>new PixelCameraSnapshot{position=camera.transform.position,rotation=camera.transform.rotation,aspect=camera.aspect,rect=camera.rect,projection=camera.projectionMatrix,mask=camera.cullingMask,clear=camera.clearFlags,background=camera.backgroundColor};
 }
 [Serializable]sealed class PixelFeedDiagnostic
 {
  public int views,view,width,height,renderFrame;public float renderTextureAspect;public PixelCameraSnapshot placement,render,restored;public Vector3[] markerWorld,markerViewportAtPlacement,markerViewportAtRender;public Color[] samples;public string[] cameraComponents;
 }
 sealed class Fixture:IDisposable
 {
  readonly IDisposable prefs;readonly GameObject isolation;public Behaviour match;public Component venue,pixels;
  public object Rules=>Get(match,"rules");public Component Runner(int slot=0)=>(Component)((Array)Get(match,"courses")).GetValue(slot);
  public object Truck(int slot=0)=>Get(Runner(slot),"truck");public Rigidbody Body(int slot=0)=>(Rigidbody)Get(Truck(slot),"body");
  public Fixture(string suffix){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"SkeeCarnival"+suffix});isolation=new GameObject("Skee Carnival software source isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
  public IEnumerator Load()
  {
   EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSkeeBall/Scenes/TruckSkeeBall.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
   match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckSkeeBall.SkeeMatch")).Cast<Behaviour>().Single();
   var originalColliders=((Array)Get(match,"courses")).Cast<object>().SelectMany(runner=>((Transform)Get(runner,"target")).GetComponentsInChildren<MeshCollider>()).ToArray();var originalMeshes=originalColliders.Select(collider=>collider.sharedMesh).ToArray();var originalPositions=originalColliders.Select(collider=>collider.transform.position).ToArray();var originalRotations=originalColliders.Select(collider=>collider.transform.rotation).ToArray();
   var author=Type.GetType("LWS.TruckSkeeBall.Editor.SkeeCarnivalAuthoring, Assembly-CSharp-Editor",true);venue=(Component)author.GetMethod("Install").Invoke(null,new object[]{match,false});Assert.That(author.GetMethod("Install").Invoke(null,new object[]{match,false}),Is.SameAs(venue));pixels=match.GetComponent(Runtime("LWS.TruckSkeeBall.SkeeArcadePixels"));
   for(int i=0;i<originalColliders.Length;i++){Assert.That(originalColliders[i].sharedMesh,Is.SameAs(originalMeshes[i]));Assert.That(originalColliders[i].transform.position,Is.EqualTo(originalPositions[i]));Assert.That(Quaternion.Angle(originalColliders[i].transform.rotation,originalRotations[i]),Is.LessThan(.001f));}
  }
  public void Fresh(int count,bool physical=false)
  {
   Call(match,"SetupScreen");Set(match,"option",0);Set(Get(match,"setup"),"count",count);Call(match,"NormalizeSetup");Call(match,"AllBots");Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{true});Assert.That((bool)Call(match,"StartMatch"),Is.True);match.enabled=!physical;
  }
  public void Dispose(){Time.timeScale=1;if(match!=null){Call(match,"SetupScreen");Call(pixels,"Restore");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
 }
 static void Flight(Fixture f,int slot,Vector3 position,Quaternion rotation,Vector3 velocity)
 {
  var runner=f.Runner(slot);var body=f.Body(slot);Call(f.Truck(slot),"Power",false);body.isKinematic=false;body.position=position;body.rotation=rotation;body.transform.SetPositionAndRotation(position,rotation);body.linearVelocity=body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
  Set(runner,"<Phase>k__BackingField",Enum.Parse(Get(runner,"Phase").GetType(),"Flight"));Set(runner,"previous",position);Set(runner,"previousRotation",rotation);body.linearVelocity=velocity;
 }
 static IEnumerator PhysicalUntilAward(Fixture f,int slot,Action<string> observed,float seconds=8)
 {
  float deadline=Time.realtimeSinceStartup+seconds;int steps=0;bool landing=false;
  while(((int[])Get(f.Rules,"attempts"))[slot]==0&&Time.realtimeSinceStartup<deadline)
  {
   yield return new WaitForFixedUpdate();Call(f.Runner(slot),"Observe");landing|=Get(f.Runner(slot),"Phase").ToString()=="Landing";Call(f.Runner(slot),"Tick",Vector2.zero,false,Time.fixedDeltaTime);steps++;
  }
  observed("steps="+steps+" landed="+landing+" phase="+Get(f.Runner(slot),"Phase")+" position="+f.Body(slot).position+" velocity="+f.Body(slot).linearVelocity+" flightAge="+Get(f.Runner(slot),"FlightAge"));Assert.That(((int[])Get(f.Rules,"attempts"))[slot],Is.EqualTo(1));
 }
 static void Capture(Fixture f,string label)
 {
  string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/SkeeCarnivalNative");Directory.CreateDirectory(folder);var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var output=(Camera)Get(f.pixels,"OutputCamera");
  foreach(int width in new[]{1280,1920})save.Invoke(null,new object[]{canvas,new[]{output},width,width==1280?720:1080,Path.Combine(folder,label+"-"+width+"-controlled.png"),null});
 }
 [UnityTest,Timeout(90000)]
 public IEnumerator ActualOpeningKeepsOriginalOnceOnlyAwardAndIndependentLaneRematch()
 {
  using(var f=new Fixture("Opening"))
  {
   yield return f.Load();f.Fresh(4,true);var runner=f.Runner();var target=(Transform)Get(runner,"target");var ring=((Array)Get(runner,"rings")).GetValue(0);var center=(Vector2)Get(ring,"center");var shape=(BoxCollider)Get(f.Truck(),"shape");var rotation=target.rotation*Quaternion.Euler(-90,0,0);var start=target.TransformPoint(new Vector3(center.x,center.y,-6))-rotation*shape.center;
   var originalMeshes=target.GetComponentsInChildren<MeshCollider>().Select(collider=>collider.sharedMesh).ToArray();Assert.That(((Array)Get(runner,"rings")).Length,Is.EqualTo(7));foreach(var hole in ((Array)Get(runner,"rings")).Cast<Component>()){Assert.That(hole.GetComponent<MeshCollider>(),Is.Not.Null);Assert.That(hole.transform.Find("Deep physical hole collector").GetComponent<MeshCollider>().sharedMesh.bounds.size.z,Is.GreaterThan(3));Assert.That(Get(hole.GetComponentInChildren(Type.GetType("TMPro.TextMeshPro, Unity.TextMeshPro",true)),"text"),Is.EqualTo(Get(hole,"points")+" PTS"));}
   Flight(f,0,start,rotation,target.forward*80);string telemetry=null;yield return PhysicalUntilAward(f,0,text=>telemetry=text);TestContext.WriteLine("SKEE_PHYSICAL_OPENING "+telemetry);Assert.That(((int[])Get(f.Rules,"totals"))[0],Is.EqualTo(10));Assert.That((int)Get(f.Runner(),"RingPasses"),Is.EqualTo(1));Assert.That(((int[])Get(f.Rules,"attempts")).Skip(1).Sum(),Is.Zero,"A lane's physical entry cannot finish another participant's delivery.");Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(1));
   for(int i=0;i<8;i++)Call(runner,"Observe");Assert.That(((int[])Get(f.Rules,"totals"))[0],Is.EqualTo(10));Assert.That(((int[])Get(f.Rules,"attempts"))[0],Is.EqualTo(1));CollectionAssert.AreEqual(originalMeshes,target.GetComponentsInChildren<MeshCollider>().Select(collider=>collider.sharedMesh).ToArray());
   f.Fresh(4,true);Assert.That(((int[])Get(f.Rules,"attempts")).Sum(),Is.Zero);Assert.That(((int[])Get(f.Rules,"totals")).Sum(),Is.Zero);Assert.That((int)Get(f.Runner(),"RingPasses"),Is.Zero);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);
  }
 }
 [UnityTest,Timeout(90000)]
 public IEnumerator PhysicalMissRestsInZeroTrayAndRealSideNetPreventsGuardEscape()
 {
  using(var f=new Fixture("Miss"))
  {
   yield return f.Load();var trays=(BoxCollider[])Get(f.venue,"zeroReturns");var nets=(BoxCollider[])Get(f.venue,"retainingNets");Assert.That(trays.Length,Is.EqualTo(4));Assert.That(nets.Length,Is.EqualTo(16));Assert.That(trays.All(tray=>tray.bounds.max.y> -2&&tray.GetComponentInParent(Runtime("LWS.TruckSkeeBall.SkeeBoard"))!=null),Is.True);Assert.That(nets.All(net=>!net.isTrigger&&net.sharedMaterial!=null),Is.True);
   foreach(bool side in new[]{false,true})
   {
    f.Fresh(1,true);Flight(f,0,new Vector3(side?14:10,side?5:4,52),Quaternion.identity,side?new Vector3(10,0,0):Vector3.zero);string telemetry=null;float peakX=0;
    yield return PhysicalUntilAward(f,0,text=>telemetry=text);peakX=f.Body().position.x;TestContext.WriteLine("SKEE_PHYSICAL_ZERO side="+side+" "+telemetry);Assert.That(telemetry,Does.Contain("landed=True"),"The real new collider must enter original Landing before its final-rest result.");Assert.That(((int[])Get(f.Rules,"totals"))[0],Is.Zero);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);Assert.That((float)Get(f.Runner(),"FlightAge"),Is.LessThan(8));Assert.That(f.Body().position.y,Is.GreaterThan(-2));Assert.That(Mathf.Abs(peakX),Is.LessThan(17));Assert.That(f.Body().linearVelocity.sqrMagnitude,Is.LessThan(.09f));Assert.That(f.Body().angularVelocity.sqrMagnitude,Is.LessThan(.16f));Assert.That((float)Get(Get(f.Runner(),"touch"),"Spent"),Is.Zero,"A physical board/net contact cannot introduce aftertouch input.");
   }
   TestContext.WriteLine("Controlled initial miss poses only; subsequent contact, retention, settling and original zero-result timing were real NWH/PhysX. No simulated trigger awards or injected score calls.");
  }
 }
 [UnityTest,Timeout(120000)]
 public IEnumerator OriginalAiUsesRealRampAndNewReturnWithoutInjectedTrajectoryOrScores()
 {
  using(var f=new Fixture("ActualAI"))
  {
   yield return f.Load();f.Fresh(4);float deadline=Time.realtimeSinceStartup+65,upward=0,furthest=0;
   while((int)Get(f.match,"Launches")==0||((int[])Get(f.Rules,"attempts")).Sum()==0)
   {
    if(Time.realtimeSinceStartup>deadline)break;
    for(int lane=0;lane<4;lane++)if(Get(f.Runner(lane),"Phase").ToString()=="Flight"){upward=Mathf.Max(upward,f.Body(lane).linearVelocity.y);furthest=Mathf.Max(furthest,f.Body(lane).position.z);}
    yield return null;
   }
   TestContext.WriteLine("SKEE_ACTUAL_AI launches="+Get(f.match,"Launches")+" attempts="+string.Join(",",(int[])Get(f.Rules,"attempts"))+" totals="+string.Join(",",(int[])Get(f.Rules,"totals"))+" maximumUpward="+upward+" furthestFlightZ="+furthest);
   Assert.That((int)Get(f.match,"Launches"),Is.GreaterThan(0));Assert.That(upward,Is.GreaterThan(5));Assert.That(furthest,Is.GreaterThan(44));Assert.That(((int[])Get(f.Rules,"attempts")).Sum(),Is.GreaterThan(0));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo((int)Get(f.match,"GenuineEntries")));
   yield return null;yield return null;Capture(f,"actual-ai-flight-and-return");var textures=Enumerable.Range(0,(int)Get(f.pixels,"FeedCount")).Select(view=>(RenderTexture)Call(f.pixels,"TextureFor",view)).ToArray();float clock=(float)Get(f.match,"Clock");Call(f.match,"Pause",0,false);yield return new WaitForSecondsRealtime(.2f);Assert.That((float)Get(f.match,"Clock"),Is.EqualTo(clock));CollectionAssert.AreEqual(textures,Enumerable.Range(0,textures.Length).Select(view=>(RenderTexture)Call(f.pixels,"TextureFor",view)).ToArray());Call(f.match,"Resume");
   TestContext.WriteLine("Original four-AI NWH approach, curved EasyRoads launch, gravity/limited aftertouch and physical first result ran with the new venue. No body/trajectory/score fixture injection in this case. Full nine-delivery and eight-player hot-seat probes remain separate acceptance.");
  }
 }
 [UnityTest,Timeout(120000)]
 public IEnumerator PixelFeedsCoverOneToFourViewportsResizeAndRestoreCamerasCanvasAndWeather()
 {
  using(var f=new Fixture("Pixels"))
  {
   yield return f.Load();var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var canvasMode=canvas.renderMode;var canvasCamera=canvas.worldCamera;float plane=canvas.planeDistance,scale=canvas.scaleFactor;int listeners=UnityEngine.Object.FindObjectsByType<AudioListener>().Length;var cameras=((Array)Get(f.match,"courses")).Cast<object>().Select(runner=>(Camera)Get(Get(runner,"viewCamera"),"view")).ToArray();var targets=cameras.Select(camera=>camera.targetTexture).ToArray();var clear=cameras.Select(camera=>camera.clearFlags).ToArray();var backgrounds=cameras.Select(camera=>camera.backgroundColor).ToArray();var pipeline=GraphicsSettings.defaultRenderPipeline;var weatherType=Runtime("DigitalRuby.WeatherMaker.WeatherMakerScript");
   for(int count=1;count<=4;count++)
   {
    f.Fresh(count);yield return null;yield return null;yield return null;var active=(Camera[])Get(f.match,"cameras");var rects=active.Select(camera=>camera.rect).ToArray();Assert.That(active.Length,Is.EqualTo(count));Assert.That((int)Get(f.pixels,"FeedCount"),Is.EqualTo(count));Assert.That((bool)Get(f.pixels,"Active"),Is.True);var output=(Camera)Get(f.pixels,"OutputCamera");Assert.That(active.Contains(output),Is.False);Assert.That(output.cullingMask,Is.Zero);Assert.That(output.cameraType,Is.EqualTo(CameraType.Game),"The real game-window compositor must use URP game-camera rendering, not its target-texture-only Preview path.");Assert.That(output.targetTexture,Is.Null,"The output presents the backbuffer; only original participant feeds use RTs.");Assert.That(output.rect,Is.EqualTo(new Rect(0,0,1,1)));var outputData=output.GetComponent(Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime",true));Assert.That(outputData,Is.Not.Null);Assert.That(Get(outputData,"renderType").ToString(),Is.EqualTo("Base"));Assert.That(output.GetComponent<AudioListener>(),Is.Null);
    var weather=UnityEngine.Object.FindObjectsByType(weatherType).Cast<Component>().FirstOrDefault();if(weather!=null){var allowed=(System.Collections.IList)Get(weather,"AllowCameras");Assert.That(allowed.Contains(output),Is.False);Assert.That((bool)weatherType.GetMethod("ShouldIgnoreCamera").Invoke(null,new object[]{weather,output,true}),Is.True);}
    for(int view=0;view<count;view++)
    {
     var rt=(RenderTexture)Call(f.pixels,"TextureFor",view);Assert.That(rt,Is.SameAs(active[view].targetTexture));Assert.That(rt.filterMode,Is.EqualTo(FilterMode.Point));var expected=FeedSize(rects[view],640,360);Assert.That(rt.width,Is.EqualTo(expected.x));Assert.That(rt.height,Is.EqualTo(expected.y));Assert.That(Mathf.Abs(rt.width/(float)rt.height-Screen.width*rects[view].width/(Screen.height*rects[view].height)),Is.LessThan(.02f),"Pixel reduction must preserve the original viewport aspect.");Assert.That((Rect)Call(f.pixels,"ViewportFor",view),Is.EqualTo(rects[view]));
     // Distinct colors at all four camera-space corners prove that URP rendered
     // a complete feed into each small RT rather than reapplying its split rect.
     var originalPosition=active[view].transform.position;var originalRotation=active[view].transform.rotation;var originalMask=active[view].cullingMask;var originalClear=active[view].clearFlags;var originalColor=active[view].backgroundColor;var markers=new GameObject[4];var colors=new[]{Color.red,Color.green,Color.blue,Color.white};
     var diagnostic=new PixelFeedDiagnostic{views=count,view=view,width=rt.width,height=rt.height,renderTextureAspect=rt.width/(float)rt.height,placement=PixelCameraSnapshot.Read(active[view]),cameraComponents=active[view].GetComponents<Behaviour>().Select(component=>component.GetType().FullName+" enabled="+component.enabled).ToArray()};
     for(int corner=0;corner<4;corner++){markers[corner]=GameObject.CreatePrimitive(PrimitiveType.Quad);markers[corner].name="Controlled whole-RT corner "+corner;markers[corner].layer=24;markers[corner].transform.SetPositionAndRotation(active[view].ViewportToWorldPoint(new Vector3((corner&1)==0?.15f:.85f,(corner&2)==0?.15f:.85f,5)),active[view].transform.rotation);markers[corner].transform.localScale=Vector3.one*1.5f;var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",colors[corner]);markers[corner].GetComponent<Renderer>().sharedMaterial=material;}
     diagnostic.markerWorld=markers.Select(marker=>marker.transform.position).ToArray();diagnostic.markerViewportAtPlacement=markers.Select(marker=>active[view].WorldToViewportPoint(marker.transform.position)).ToArray();
     Action<ScriptableRenderContext,Camera> observeRender=(context,camera)=>{if(camera!=active[view])return;diagnostic.renderFrame=Time.frameCount;diagnostic.render=PixelCameraSnapshot.Read(camera);diagnostic.markerViewportAtRender=markers.Select(marker=>camera.WorldToViewportPoint(marker.transform.position)).ToArray();};
     active[view].cullingMask=1<<24;active[view].clearFlags=CameraClearFlags.SolidColor;active[view].backgroundColor=Color.black;f.match.enabled=false;RenderPipelineManager.beginCameraRendering+=observeRender;
     try{yield return null;yield return null;}finally{RenderPipelineManager.beginCameraRendering-=observeRender;}
     diagnostic.restored=PixelCameraSnapshot.Read(active[view]);
     var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=old;
     diagnostic.samples=Enumerable.Range(0,4).Select(corner=>image.GetPixel(Mathf.RoundToInt(rt.width*((corner&1)==0?.15f:.85f)),Mathf.RoundToInt(rt.height*((corner&2)==0?.15f:.85f)))).ToArray();
     string diagnosticFolder="Logs/TruckSports/PlaytestRepair/SkeeCarnival/PixelDiagnostics";Directory.CreateDirectory(diagnosticFolder);string diagnosticPath=diagnosticFolder+"/views-"+count+"-feed-"+view;File.WriteAllBytes(diagnosticPath+".png",image.EncodeToPNG());File.WriteAllText(diagnosticPath+".json",JsonUtility.ToJson(diagnostic,true));
     TestContext.WriteLine("SKEE_PIXEL_PROJECTION "+diagnosticPath+" placementAspect="+diagnostic.placement.aspect.ToString("R")+" renderAspect="+(diagnostic.render==null?"NO_RENDER_CALLBACK":diagnostic.render.aspect.ToString("R"))+" targetAspect="+diagnostic.renderTextureAspect.ToString("R")+" restoredAspect="+diagnostic.restored.aspect.ToString("R")+" placementPosition="+diagnostic.placement.position.ToString("F7")+" renderPosition="+(diagnostic.render==null?"NONE":diagnostic.render.position.ToString("F7"))+" renderViewportCorners="+(diagnostic.markerViewportAtRender==null?"NONE":string.Join(";",diagnostic.markerViewportAtRender.Select(point=>point.ToString("F7")))));
     for(int corner=0;corner<4;corner++){var sample=diagnostic.samples[corner];Assert.That(Vector3.Distance(new Vector3(sample.r,sample.g,sample.b),new Vector3(colors[corner].r,colors[corner].g,colors[corner].b)),Is.LessThan(.35f),"Whole RT corner "+corner+" for "+count+"-view layout, viewport "+rects[view]+" actual "+sample+"; saved "+diagnosticPath+".png/.json");}
     UnityEngine.Object.Destroy(image);foreach(var marker in markers){UnityEngine.Object.Destroy(marker.GetComponent<Renderer>().sharedMaterial);UnityEngine.Object.Destroy(marker);}active[view].cullingMask=originalMask;active[view].clearFlags=originalClear;active[view].backgroundColor=originalColor;active[view].transform.SetPositionAndRotation(originalPosition,originalRotation);Assert.That(active[view].rect,Is.EqualTo(rects[view]));
    }
    f.match.enabled=true;yield return null;yield return null;yield return null;Capture(f,"arcade-"+count+"-views");Set(f.pixels,"virtualWidth",800);Set(f.pixels,"virtualHeight",450);yield return null;yield return null;yield return null;for(int view=0;view<count;view++){var rt=(RenderTexture)Call(f.pixels,"TextureFor",view);var expected=FeedSize(rects[view],800,450);Assert.That(rt.width,Is.EqualTo(expected.x));Assert.That(rt.height,Is.EqualTo(expected.y));Assert.That(active[view].rect,Is.EqualTo(rects[view]));}Set(f.pixels,"virtualWidth",640);Set(f.pixels,"virtualHeight",360);
    Call(f.match,"SetupScreen");yield return null;Assert.That((bool)Get(f.pixels,"Active"),Is.False);Assert.That(output.enabled,Is.False);Assert.That(canvas.renderMode,Is.EqualTo(canvasMode));Assert.That(canvas.worldCamera,Is.SameAs(canvasCamera));Assert.That(canvas.planeDistance,Is.EqualTo(plane));Assert.That(canvas.scaleFactor,Is.EqualTo(scale));CollectionAssert.AreEqual(targets,cameras.Select(camera=>camera.targetTexture).ToArray());CollectionAssert.AreEqual(clear,cameras.Select(camera=>camera.clearFlags).ToArray());CollectionAssert.AreEqual(backgrounds,cameras.Select(camera=>camera.backgroundColor).ToArray());Assert.That(GraphicsSettings.defaultRenderPipeline,Is.SameAs(pipeline));Assert.That(UnityEngine.Object.FindObjectsByType<AudioListener>().Length,Is.EqualTo(listeners));Assert.That(((Component)Get(f.match,"menus")).GetComponentsInChildren<RawImage>(true).Where(image=>image.name.StartsWith("Original lane pixel feed")).All(image=>image.texture==null&&!image.gameObject.activeSelf),Is.True);
    var restoredCamera=active[0];var restoredRect=restoredCamera.rect;restoredCamera.rect=new Rect(0,0,.5f,1);
    Assert.That(restoredCamera.aspect,Is.EqualTo(restoredCamera.pixelRect.width/restoredCamera.pixelRect.height).Within(.0001f),"Automatic aspect must remain automatic after ownership is released, including a later viewport change.");restoredCamera.rect=restoredRect;
   }
   // A deliberate custom lens aspect is a different contract from automatic viewport sizing.
   // It must survive feed render, context restoration and complete compositor release exactly.
   f.Fresh(2);var customCamera=((Camera[])Get(f.match,"cameras"))[0];var customRect=customCamera.rect;const float customAspect=1.713f;customCamera.aspect=customAspect;f.match.enabled=false;float renderedCustom=-1;
   Action<ScriptableRenderContext,Camera> customRender=(context,camera)=>{if(camera==customCamera)renderedCustom=camera.aspect;};RenderPipelineManager.beginCameraRendering+=customRender;
   try{yield return null;yield return null;yield return null;}finally{RenderPipelineManager.beginCameraRendering-=customRender;}
   Assert.That(renderedCustom,Is.EqualTo(customAspect),"An explicit original custom aspect must remain the actual render projection.");Assert.That(customCamera.aspect,Is.EqualTo(customAspect));Assert.That(customCamera.rect,Is.EqualTo(customRect));Call(f.match,"SetupScreen");yield return null;
   Assert.That(customCamera.aspect,Is.EqualTo(customAspect));Assert.That(customCamera.targetTexture,Is.SameAs(targets[0]));Assert.That(customCamera.rect,Is.EqualTo(customRect));TestContext.WriteLine("SKEE_CUSTOM_ASPECT configured="+customAspect.ToString("R")+" rendered="+renderedCustom.ToString("R")+" restored="+customCamera.aspect.ToString("R")+"; automatic viewport resize and explicit custom aspect tested separately.");
   TestContext.WriteLine("Actual URP one/two/three/four split feeds tested with four physical camera-space color corners each, RT resizing, point filtering, retained viewport metadata, crisp existing HUD canvas, original listeners/weather allowlist, cap-free output camera and exact exit targets/clear/canvas/pipeline restoration. Controlled captures are not Windows performance or human art approval.");
  }
 }
}
#endif
