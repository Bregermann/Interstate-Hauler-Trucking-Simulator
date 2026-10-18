#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using LWS.SportsPresentation;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckKeepaway {
[DefaultExecutionOrder(550)]public sealed class KeepawayPresentationProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){
  var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsKeepawayPresentation");string mode=at>=0&&at+1<args.Length?args[at+1]:null;
#if UNITY_EDITOR
  if(UnityEditor.SessionState.GetBool("Keepaway.PresentationProbe",false)){UnityEditor.SessionState.SetBool("Keepaway.PresentationProbe",false);mode=UnityEditor.SessionState.GetString("Keepaway.PresentationMode","baseline");}
#endif
  if(mode==null)return;var root=new GameObject("Explicit Keepaway presentation acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();var probe=root.AddComponent<KeepawayPresentationProbe>();probe.mode=mode;
 }
 string mode,folder;int checks,errors;bool done;IDisposable scope;Keyboard keyboard;KeepawayMatch match;readonly StringBuilder log=new StringBuilder();
 void Awake(){scope=SportsPrefs.IsolateValidation("KeepawayPresentation");SportsParticipantRegistry.ForgetCache();keyboard=InputSystem.AddDevice<Keyboard>("Keepaway presentation fixture keyboard");Application.logMessageReceived+=Error;}
 void Error(string text,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;log.AppendLine("ERROR "+text);}
 void Check(bool ok,string message){checks++;if(!ok)errors++;log.AppendLine((ok?"PASS ":"FAIL ")+message);Write();}
 void Write(){if(folder!=null)File.WriteAllText(folder+"/progress.txt",log.ToString());}
 IEnumerator Start(){folder=Path.GetFullPath(Path.Combine(Application.dataPath,Application.isEditor?"..":"../../..","Logs/TruckSports/Keepaway",Application.isEditor?"Native":"Windows",mode));Directory.CreateDirectory(folder);File.WriteAllText(folder+"/result.txt","RUNNING");log.AppendLine("KEEPAWAY_PRESENTATION mode="+mode+" PID="+System.Diagnostics.Process.GetCurrentProcess().Id+" editor="+Application.isEditor);var steps=new System.Collections.Generic.Stack<IEnumerator>();steps.Push(Run());while(steps.Count>0){object next=null;bool more=false;try{var run=steps.Peek();more=run.MoveNext();if(!more){steps.Pop();continue;}next=run.Current;if(next is IEnumerator child){steps.Push(child);continue;}}catch(Exception e){Check(false,e.ToString());Finish();steps.Clear();}if(steps.Count==0)yield break;yield return next;}}
 IEnumerator Run(){
  for(int variant=0;variant<2;variant++){
   if(match!=null)match.SetupScreen();Time.timeScale=1;
   SportsNavigation.LoadSport(variant==0?SportsNavigation.Keepaway:SportsNavigation.KeepawayCab);yield return null;yield return null;match=FindAnyObjectByType<KeepawayMatch>();match.SetupScreen();match.setup.count=4;match.option=1;match.weatherChoice=1;match.chaosLevel=1;SportsCameraOptions.Set(false);
   for(int i=0;i<4;i++){match.setup.players[i].source=variant==0?BowlingSource.AI:BowlingSource.Keyboard;match.setup.players[i].pad=null;}
   Check(match.StartMatch(),"saved variant starts "+variant);yield return new WaitForSecondsRealtime(1);
   Check(match.cabVariant==(variant==1)&&match.cameras.Length==(variant==0?1:4),"variant and human camera ownership "+variant);
   if(variant==0)yield return Sheep();
   Time.timeScale=0;match.setup.players[0].name=match.setup.players[1].name="Same Driver";match.Save();match.possession[0]=3.2f;match.possession[1]=17.4f;match.menus.Dirty();yield return new WaitForSecondsRealtime(.3f);
   var cards=match.menus.hud.transform.Find("Broadcast participant cards");string a=Value(cards,0),b=Value(cards,1);log.AppendLine("DUPLICATE_NAME_UI variant="+variant+" first="+a+" second="+b+" expected=3.2,17.4");
   if(variant==0&&mode!="baseline")Check(a=="3.2"&&b=="17.4","duplicate names keep independent authoritative card totals");
   if(variant==0){var rect=SportsCameraFit.Projected(match.broadcast.view,new Bounds(Vector3.zero,new Vector3(64,0,36)));log.AppendLine("ARENA_PROJECTED "+rect+" orthographic="+match.broadcast.view.orthographicSize+" physical="+match.arenaX+","+match.arenaZ);}
   if(mode!="baseline"){
    Check(!match.menus.header.transform.parent.gameObject.activeSelf&&!match.menus.status.gameObject.activeSelf,"obsolete broadcast header/status removed "+variant);
    var backdrop=match.menus.status.transform.parent.Find("Broadcast status backplate");Check(backdrop==null||!backdrop.gameObject.activeInHierarchy,"no empty status backplate "+variant);
    Check(FindObjectsByType<AudioListener>(FindObjectsInactive.Include).Length==1,"one AudioListener "+variant);
   }
   foreach(var size in mode=="baseline"?new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}:new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(1440,900),new Vector2Int(1024,768),new Vector2Int(2560,1080)}){
    if(Application.isEditor)SportsControlledCapture.Save(match.menus.GetComponent<Canvas>(),match.cameras,size.x,size.y,folder+"/"+(variant==0?"arena":"cab-four")+"-"+size.x+".png");
    else{Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.35f);Check(Screen.width==size.x&&Screen.height==size.y,"actual window "+size);if(variant==0)Framing(size);ScreenCapture.CaptureScreenshot(folder+"/"+(variant==0?"arena":"cab-four")+"-"+size.x+".png");yield return new WaitForSecondsRealtime(.15f);}
   }
   if(variant==1&&mode!="baseline")yield return Guidance();
   match.SetupScreen();Time.timeScale=1;
  }
  Finish();
 }
 void Framing(Vector2Int size){
  var rect=SportsCameraFit.Projected(match.broadcast.view,new Bounds(new Vector3(0,.6f,0),new Vector3(65.2f,1.2f,37.2f)));
  log.AppendLine("FRAME "+size+" projected="+rect);
  Check(rect.xMin>=0&&rect.xMax<=1&&rect.yMin>=.095f&&rect.yMax<=1,"all rounded boundaries visible above bottom cards "+size);
  if(size.x==1280||size.x==1920)Check(rect.width>=.90f,"arena fills at least 90 percent of 16:9 width "+size);
  var cards=match.menus.hud.transform.Find("Broadcast participant cards");float cardTop=0;var corners=new Vector3[4];
  for(int i=0;i<match.setup.count;i++){var card=(RectTransform)cards.Find("Participant card "+i);card.GetWorldCorners(corners);foreach(var corner in corners)cardTop=Mathf.Max(cardTop,RectTransformUtility.WorldToScreenPoint(null,corner).y);}
  Check(cardTop<rect.yMin*size.y-4,"score cards clear the near board and gameplay "+size+" cardTop="+cardTop+" boardBottom="+rect.yMin*size.y);
 }
 IEnumerator Guidance(){
  var guidance=match.GetComponent<KeepawayBallGuidance>();Check(guidance!=null&&guidance.views.Length==4,"saved four-group Compass Navigator Pro adapter");
  for(int humans=1;humans<=4;humans++)for(int show=0;show<2;show++){
   match.SetupScreen();SportsCameraOptions.Set(show!=0);for(int i=0;i<4;i++)match.setup.players[i].source=i>=4-humans?BowlingSource.Keyboard:BowlingSource.AI;
   Check(match.StartMatch(),"presentation input fixture humans="+humans+" showAI="+show);yield return new WaitForSecondsRealtime(.15f);Time.timeScale=0;yield return null;
   Check(match.HumanViews==(show!=0?4:humans),"viewport count independent of AI participation humans="+humans+" show="+show);
   Check(guidance.views.Count(v=>v.gameObject.activeInHierarchy)==humans,"only actual human viewports receive ball indicators "+humans+"/"+show);
   for(int v=0;v<match.HumanViews;v++)if(match.setup.players[match.CabSlot(v)].source!=BowlingSource.AI)Check(guidance.views[v].cameraMain==match.cameras[v],"indicator actual camera ownership view="+v+" slot="+match.CabSlot(v));
  }
  // Sparse ownership proves the sole full-screen camera belongs to slot 3, not slot 1.
  match.SetupScreen();SportsCameraOptions.Set(false);for(int i=0;i<4;i++)match.setup.players[i].source=i==2?BowlingSource.Keyboard:BowlingSource.AI;
  Check(match.StartMatch()&&match.CabSlot(0)==2,"sparse human slot owns full-screen cab");yield return new WaitForSecondsRealtime(.15f);Time.timeScale=0;yield return null;
  var compass=guidance.views[0];var camera=match.cameras[0];
  var points=new[]{new Vector3(40,0,10),new Vector3(-40,0,10),new Vector3(0,15,10),new Vector3(0,0,-20)};var bearings=new[]{0f,180f,90f,-90f};
  for(int k=0;k<points.Length;k++){
   match.ball.position=camera.transform.TransformPoint(points[k]);yield return new WaitForSecondsRealtime(.35f);float until=Time.realtimeSinceStartup+2;while(Mathf.Abs(Mathf.DeltaAngle(compass.Bearing,bearings[k]))>=8&&Time.realtimeSinceStartup<until)yield return null;
   Check(compass.BallVisible&&compass.Opacity>.95f,"offscreen ball fades in case="+k);
   Check(Vector3.Distance(guidance.ballPOI.transform.position,match.ball.position)<.001f,"read-only POI follows actual ball body case="+k);
   Check(Mathf.Abs(Mathf.DeltaAngle(compass.Bearing,bearings[k]))<8,"camera-relative bearing case="+k+" actual="+compass.Bearing);
   CheckWidget(compass,camera,"case "+k);
  }
  match.ball.position=camera.transform.TransformPoint(new Vector3(0,0,20));yield return new WaitForSecondsRealtime(.3f);
  Check(!compass.BallVisible&&compass.Opacity<.02f,"onscreen ball hides pointer with fade");
  // Rotate only the actual rendered camera after the normal cab/free-look adapter.
  // This is an injected pose fixture, not a physical controller test.
  match.ball.position=camera.transform.TransformPoint(new Vector3(0,0,20));cameraOverride=camera;overrideRotation=camera.transform.rotation*Quaternion.Euler(0,100,0);yield return new WaitForSecondsRealtime(.5f);
  var projected=compass.ballPOI.GetState(compass.compassGroup).viewportPos;
  Check(projected.z<0||projected.x<0||projected.x>1,"injected free-look turns formerly visible ball offscreen");
  Check(compass.BallVisible,"indicator follows final camera orientation after free look");cameraOverride=null;
  match.Pause();yield return new WaitForSecondsRealtime(.2f);Check(guidance.views.All(v=>!v.gameObject.activeInHierarchy),"pause hides guidance without changing ball");match.Resume();Time.timeScale=0;yield return null;
  var ball=match.ball;match.enabled=false;match.ball=null;yield return null;yield return null;Check(guidance.views.All(v=>!v.gameObject.activeInHierarchy),"invalid ball reference hides all indicators");match.ball=ball;match.enabled=true;
  // Re-enter real four-human viewports for actual window HUD containment captures.
  match.SetupScreen();for(int i=0;i<4;i++)match.setup.players[i].source=BowlingSource.Keyboard;Check(match.StartMatch(),"four-human guidance rematch");yield return new WaitForSecondsRealtime(.15f);Time.timeScale=0;
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){
   if(!Application.isEditor){Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.3f);}
   match.ball.position=match.cameras[0].transform.TransformPoint(new Vector3(0,0,-20));yield return new WaitForSecondsRealtime(.5f);
   for(int v=0;v<4;v++)if(guidance.views[v].BallVisible)CheckWidget(guidance.views[v],match.cameras[v],"four views "+size+" view="+v);
   if(!Application.isEditor){ScreenCapture.CaptureScreenshot(folder+"/guidance-four-"+size.x+".png");yield return new WaitForSecondsRealtime(.2f);}
  }
  match.SetupScreen();match.AllBots();Check(match.StartMatch(),"AI-only spectator rematch");yield return new WaitForSecondsRealtime(.2f);Time.timeScale=0;
  Check(match.broadcast.view.enabled&&guidance.views.All(v=>!v.gameObject.activeInHierarchy),"AI-only broadcast has no human arrows and no blank screen");
 }
 Camera cameraOverride;Quaternion overrideRotation;
 void LateUpdate(){if(cameraOverride!=null)cameraOverride.transform.rotation=overrideRotation;}
 void CheckWidget(KeepawayBallCompass compass,Camera camera,string label){
  var rect=compass.Widget;Check(rect!=null,"vendor indicator instance "+label);if(rect==null)return;
  var center=RectTransformUtility.WorldToScreenPoint(null,rect.position);var viewport=camera.pixelRect;float factor=compass.canvas.scaleFactor;
  Check(center.x>=viewport.xMin+59*factor&&center.x<=viewport.xMax-59*factor&&center.y>=viewport.yMin+81*factor&&center.y<=viewport.yMax-59*factor,"fixed compass surround inside own viewport "+label+" center="+center+" view="+viewport);
 }
 static string Value(Transform root,int index){var target=root!=null?root.Find("Participant card "+index+"/Score badge"):null;return target!=null?target.GetComponent<TMP_Text>().text:"MISSING";}
 IEnumerator Sheep(){
  var sheep=match.chaosField.sheep.Where(s=>s.Active).ToArray();var start=sheep.Select(s=>s.body.position).ToArray();var rotations=sheep.Select(s=>s.GetComponentInChildren<SkinnedMeshRenderer>().bones.Select(b=>b.localRotation).ToArray()).ToArray();var moved=new bool[sheep.Length];var animated=new bool[sheep.Length];float highest=float.NegativeInfinity,lowest=float.PositiveInfinity;int samples=0;var mesh=new Mesh();
  for(int frame=0;frame<20;frame++){yield return new WaitForSecondsRealtime(.15f);for(int i=0;i<sheep.Length;i++){
   var s=sheep[i];if(!s.Active)continue;var skin=s.GetComponentInChildren<SkinnedMeshRenderer>();skin.BakeMesh(mesh,true);var vertices=mesh.vertices;Vector3 foot=skin.transform.TransformPoint(vertices[0]);for(int k=1;k<vertices.Length;k++){var p=skin.transform.TransformPoint(vertices[k]);if(p.y<foot.y)foot=p;}var ground=match.chaosField.ground.surface.GetComponent<Collider>();if(ground.Raycast(new Ray(foot+Vector3.up*4,Vector3.down),out var hit,8)){float gap=foot.y-hit.point.y;highest=Mathf.Max(highest,gap);lowest=Mathf.Min(lowest,gap);samples++;}
   if(Vector3.Distance(s.body.position,start[i])>.3f)moved[i]=true;for(int k=0;k<skin.bones.Length;k++)if(Quaternion.Angle(rotations[i][k],skin.bones[k].localRotation)>5)animated[i]=true;
  }}
  Destroy(mesh);log.AppendLine("SHEEP_BASELINE active="+sheep.Length+" moved="+moved.Count(v=>v)+" animated="+animated.Count(v=>v)+" samples="+samples+" feetGapMin="+lowest+" max="+highest);
  Check(moved.Count(v=>v)>=3&&animated.Count(v=>v)>=3,"actual displacement and skeletal animation, not static sliding");if(mode!="baseline")Check(samples>0&&highest<.25f&&lowest>-.25f,"animated feet remain close to actual contoured collision surface");Write();
 }
 void Finish(){if(done)return;done=true;Time.timeScale=1;log.AppendLine("CHECKS="+checks);log.AppendLine("ERRORS="+errors);log.AppendLine(errors==0?"PASS":"FAIL");File.WriteAllText(folder+"/result.txt",log.ToString());
#if UNITY_EDITOR
  if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
  Application.Quit(errors==0?0:2);
 }
 void OnDestroy(){Application.logMessageReceived-=Error;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);scope?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
}
}
#endif
