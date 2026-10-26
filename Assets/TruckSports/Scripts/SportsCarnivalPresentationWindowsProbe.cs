#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using LWS.TruckBowling;
using LWS.TruckCornhole;
using LWS.TruckDockDiving;
using LWS.TruckGiantSwing;
using LWS.TruckTargetLaunch;
using LWS.TruckSkyTow;
using LWS.TruckFieldGoal;
using LWS.TruckBungeeDemolition;
using LWS.TruckWhackAMole;
using LWS.TruckFloorIsLava;
using LWS.TruckVolleyball;
using LWS.TruckAvalancheEscape;
using LWS.TruckRedLightGreenLight;
using LWS.TruckNinjaWarrior;
namespace LWS.TruckSports
{
 // A real Windows backbuffer tour. Only ordinary setup/profile/view preferences
 // are configured. No supplied truck pose, velocity, command, score or result.
 public sealed class SportsCarnivalPresentationWindowsProbe:MonoBehaviour
 {
  const string Flag="-tsCarnivalPresentation",Output="-tsCarnivalPresentationOutput";
  [RuntimeInitializeOnLoadMethod]static void Boot()
  {
   var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,Flag)<0)return;
   if(Application.isEditor||Application.platform!=RuntimePlatform.WindowsPlayer){Debug.LogError("Carnival presentation acceptance requires the actual Windows player.");return;}
   var root=new GameObject("Explicit Carnival presentation Windows tour");DontDestroyOnLoad(root);if(!SportsProbeInputIsolation.Active)root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<SportsCarnivalPresentationWindowsProbe>();
  }
  sealed class Venue
  {
   public readonly string scene;public readonly int roster;public readonly bool split,matrix;
   public Venue(string name,int count,bool views=false,bool allCounts=false){scene=name;roster=count;split=views;matrix=allCounts;}
  }
  static readonly Venue[] Venues={
   new Venue("TruckCornhole",4,true,true),new Venue("TruckTargetLaunch",4,true,true),new Venue("TruckSkyTow",4,true,true),
   new Venue("TruckDockDiving",4),new Venue("TruckGiantSwing",4),new Venue("TruckFieldGoal",4),new Venue("TruckBungeeDemolition",4,true),
   new Venue("TruckWhackAMole",8,true),new Venue("TruckFloorIsLava",8,true),new Venue("TruckVolleyball",8,true),
   new Venue("TruckAvalancheEscape",12,true),new Venue("TruckRedLightGreenLight",12,true)};
  readonly StringBuilder log=new StringBuilder(),frames=new StringBuilder("scene\tcase\tresolution\trequestFrame\tinspectionFrame\trequestPhase\tinspectionPhase\tcameras\towner\tvisibleSubjectMeshesAtInspection\tvisibleObjectiveAtInspection\tworldRange\tnonblack\n"),performance=new StringBuilder("scene\tresolution\telapsedSeconds\tframes\tmeanWallMs\tp95WallMs\tmaxWallMs\tallocatedBefore\tallocatedAfter\tmonoBefore\tmonoAfter\tfocusedFrames\tgpu\n");
  readonly HashSet<string> effectiveBodies=new HashSet<string>();readonly HashSet<int> rendered=new HashSet<int>();readonly List<SportsRaceFinishReceipt> finishes=new List<SportsRaceFinishReceipt>(12);
  string folder,caseLabel;IDisposable preferences;SportsTruckAppearanceCatalog catalog;SportsEventSession sport;CornholeMatch corn;Camera[] capturing;
  int checks,errors,runtimeErrors,visited,cases,screenshots,activeAppearanceHosts,requestFrame;string requestState;bool finished,evidenceOwned;float began;int oldWidth,oldHeight;FullScreenMode oldScreenMode;
  void Check(bool value,string reason){checks++;if(!value)errors++;log.AppendLine((value?"PASS ":"FAIL ")+reason);Flush();}
  void Note(string value){log.AppendLine(value);Flush();}
  void Flush(){if(folder!=null)File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
  void Error(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;runtimeErrors++;Note("ERROR "+message+"\n"+stack);}
  IEnumerator Start()
  {
   IEnumerator routine;try{Initialize();routine=Run();}catch(Exception error){if(evidenceOwned){errors++;Note("INITIALIZATION_EXCEPTION "+error);Finish();}else{Debug.LogException(error);Application.Quit(2);}yield break;}
   // Flatten nested coroutines so an exception still produces owned terminal
   // failure evidence and restores this probe's preference/input lease.
   var work=new Stack<IEnumerator>();work.Push(routine);
   while(work.Count>0)
   {
    bool next=false;object value=null;Exception failure=null;try{next=work.Peek().MoveNext();if(next)value=work.Peek().Current;}catch(Exception error){failure=error;}
    if(failure!=null){errors++;Note("PROBE_EXCEPTION "+failure);break;}if(!next){work.Pop();continue;}if(value is IEnumerator nested){work.Push(nested);continue;}yield return value;
   }
   Finish();
  }
  void Initialize()
  {
   var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,Output);if(at<0||at+1>=args.Length)throw new ArgumentException("Provide "+Output+" under Logs/TruckSports/PlaytestRepair.");
   string allowedRoot=Path.GetFullPath("Logs/TruckSports/PlaytestRepair").TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),allowed=allowedRoot+Path.DirectorySeparatorChar,requested=Path.GetFullPath(args[at+1]);if(!requested.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Evidence output must stay inside canonical PlaytestRepair.");
   for(var directory=new DirectoryInfo(requested);directory!=null&&(directory.FullName.Equals(allowedRoot,StringComparison.OrdinalIgnoreCase)||directory.FullName.StartsWith(allowed,StringComparison.OrdinalIgnoreCase));directory=directory.Parent)if(directory.Exists&&(directory.Attributes&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Evidence directories cannot redirect through a junction/symlink.");
   Directory.CreateDirectory(requested);if(File.Exists(Path.Combine(requested,"result.txt")))throw new InvalidOperationException("Choose a new evidence directory; prior terminal evidence is retained.");folder=requested;
   using(var stream=new FileStream(Path.Combine(folder,"result.txt"),FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var writer=new StreamWriter(stream))writer.Write("RUNNING "+DateTime.UtcNow.ToString("O"));evidenceOwned=true;
   oldWidth=Screen.width;oldHeight=Screen.height;oldScreenMode=Screen.fullScreenMode;began=Time.realtimeSinceStartup;preferences=SportsPrefs.IsolateValidation("CarnivalPresentation"+System.Diagnostics.Process.GetCurrentProcess().Id);SportsParticipantRegistry.ForgetCache();Application.logMessageReceived+=Error;
   SportsTimeOfDay.Select(SportsTimeChoice.Day);
   catalog=Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");Check(catalog!=null&&catalog.Validate(out _)&&catalog.bodies.Length>=7,"built original body/cosmetic catalog validates");if(catalog==null)throw new InvalidOperationException("Built appearance catalog unavailable.");
   Note("ACTUAL_WINDOWS_PRESENTATION all sources remain AI; profile/view/setup configuration only. LIVE1x wall-frame/memory diagnostics are not FPS acceptance. GPU timing unavailable/not sampled. Existing full Pong/Hauler/Hot/Skee workload probe is separate. Backend="+SystemInfo.graphicsDeviceType+" device="+SystemInfo.graphicsDeviceName+" reportedGraphicsMemoryMB="+SystemInfo.graphicsMemorySize);
  }
  IEnumerator Run()
  {
   bool includeNinja=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsCarnivalPresentationNinja")>=0;int expected=Venues.Length+(includeNinja?1:0);foreach(var venue in Venues){yield return Visit(venue);if(AbortForRuntimeFlood(runtimeErrors)){Note("BOUNDED_ABORT after more than12 runtime errors/exceptions; all failed checks retained; unvisited venues are not accepted.");break;}}
   if(!AbortForRuntimeFlood(runtimeErrors)&&includeNinja)yield return Visit(new Venue("TruckNinjaWarrior",4));
   Check(visited==expected,"all explicitly named venues visited "+visited+"/"+expected);Check(activeAppearanceHosts>0&&effectiveBodies.Count>=2,"actual applied appearance hosts and at least two effective composed body IDs observed; hosts="+activeAppearanceHosts+" bodies="+string.Join(",",effectiveBodies));
   yield return ReturnFront("final");
  }
  IEnumerator Visit(Venue venue)
  {
   SportsNavigation.LoadSport(venue.scene);yield return null;yield return null;yield return new WaitForSecondsRealtime(.2f);sport=FindAnyObjectByType<SportsEventSession>();corn=FindAnyObjectByType<CornholeMatch>();
   Check(SceneManager.GetActiveScene().name==venue.scene&&(sport!=null||corn!=null),venue.scene+" actual saved scene loaded");if(sport==null&&corn==null)yield break;visited++;
   yield return RunCase(venue,venue.roster,false,true);
   if(venue.matrix)for(int count=1;count<=4;count++)yield return RunCase(venue,count,true,false);
   else if(venue.split)yield return RunCase(venue,venue.roster,true,false);
   StopMatch();yield return null;yield return ReturnFront(venue.scene);
  }
  BowlingSetup Setup=>corn!=null?corn.setup:sport.setup;
  string MatchId=>corn!=null?corn.MatchId:sport.MatchId;
  Camera[] Cameras=>corn!=null?corn.courses.Where(course=>course.chase.view.enabled).Select(course=>course.chase.view).ToArray():sport.cameras.Where(camera=>camera!=null&&camera.enabled&&camera.gameObject.activeInHierarchy).ToArray();
  bool Playing=>corn!=null?corn.InMatch&&!corn.Paused&&!corn.Finished&&corn.menus.Screen==CornholeScreen.Play:sport!=null&&sport.Live&&!sport.ReplayControls&&sport.menus.Screen==SportsEventScreen.Play;
  void StopMatch(){Time.timeScale=1;if(corn!=null)corn.ChangeSetup();else if(sport!=null)sport.SetupScreen();}
  void Profiles(int sceneIndex)
  {
   for(int slot=0;slot<Setup.count;slot++)
   {
    var player=Setup.players[slot];var body=catalog.bodies[(sceneIndex+slot)%catalog.bodies.Length];var draft=SportsParticipantRegistry.GetAppearance(player.participantId)??new SportsTruckAppearanceData();draft.bodyId=body.id;
    var hat=catalog.cosmetics.FirstOrDefault(item=>item!=null&&item.slotId==SportsTruckAppearanceSlots.Hat&&item.CompatibleWith(body.id));if(hat!=null){draft.selections.RemoveAll(item=>item.slotId==SportsTruckAppearanceSlots.Hat);draft.selections.Add(new SportsTruckCosmeticSelection{slotId=hat.slotId,itemId=hat.id});}
    SportsParticipantRegistry.TrySetAppearance(player.participantId,draft);Check(SportsParticipantRegistry.GetAppearance(player.participantId).bodyId==body.id,"isolated real profile retains requested body "+body.id+" slot="+slot);
   }
  }
  IEnumerator RunCase(Venue venue,int roster,bool split,bool sample)
  {
   StopMatch();Setup.count=roster;if(sport!=null){if(sport.TeamSport){sport.teamA=Mathf.Max(1,roster/2);sport.teamB=Mathf.Max(1,roster-sport.teamA);}sport.option=0;sport.NormalizeSetup();sport.AllBots();}else corn.AllAI();SportsCameraOptions.Set(split);
   Profiles(Array.IndexOf(Venues,venue)>=0?Array.IndexOf(Venues,venue):12);
   foreach(var weather in FindObjectsByType<SportsChaosWeather>(FindObjectsSortMode.None))if(weather.presets!=null){var clear=weather.presets.entries.FirstOrDefault(entry=>entry!=null&&entry.displayName=="Clear");if(clear!=null)weather.presets.Select(clear.id);}
   if(corn!=null)corn.RefreshParticipants(false);else sport.RefreshParticipants(false);
   foreach(var driving in FindObjectsByType<SportsDrivingViews>(FindObjectsSortMode.None))driving.Select(split?1:0);
   caseLabel=(split?"actual-AI-views-":"shared-AI-roster-")+roster;bool start=corn!=null?corn.StartMatch():sport.StartMatch();Check(start,venue.scene+" "+caseLabel+" original start");if(!start)yield break;cases++;Time.timeScale=1;
   Check(Setup.players.Take(Setup.count).All(player=>player.source==BowlingSource.AI),venue.scene+" every participant remains actual AI; no injected keyboard/controller owner");
   float deadline=Time.realtimeSinceStartup+NaturalLimit();while(!ReadyForView()&&Time.realtimeSinceStartup<deadline)yield return null;Check(ReadyForView(),venue.scene+" natural original active phase observed "+State());
   if(sample)yield return SampleLive(venue.scene);
   int expectedViews=split?Mathf.Min(4,Setup.count):1;Check(Cameras.Length==expectedViews,venue.scene+" original selector produces "+expectedViews+" actual AI view(s), not fabricated human owners");
   yield return Capture(venue.scene);CheckAppearances(venue.scene);CheckFinish(venue.scene);if(sport is NinjaMatch ninja)Note("NINJA_ORIGINAL_OBSERVED charging="+ninja.athletes.Count(athlete=>athlete.Charging)+" jumps="+ninja.athletes.Sum(athlete=>athlete.Jumps)+" falls="+ninja.rules.falls.Sum()+"; brief current-state evidence, no forced jump/fall/finish");
   string prior=MatchId;StopMatch();bool again=corn!=null?corn.StartMatch():sport.StartMatch();Check(again&&MatchId!=prior,venue.scene+" original rematch creates fresh identity");StopMatch();
  }
  float NaturalLimit()=>sport is SwingMatch?35:sport is DockMatch||sport is FieldGoalMatch?35:18;
  bool ReadyForView()
  {
   if(corn!=null)return corn.courses.Take(corn.setup.count).Any(course=>course.Current!=null&&course.Current.Launched);
   if(sport is TargetMatch target)return target.racers.Take(target.setup.count).Any(racer=>racer.Phase==TargetFlight.Airborne);
   if(sport is SkyTowMatch sky)return sky.Phase==SkyTowPhase.Run&&sky.Age>=5;
   if(sport is DockMatch dock)return dock.Phase==DockPhase.Flight||dock.Phase==DockPhase.Replay;
   if(sport is SwingMatch swing)return swing.Phase==SwingPhase.Pumping&&swing.Age>=2||swing.Phase==SwingPhase.Flight;
   if(sport is FieldGoalMatch field)return field.Phase==FieldGoalPhase.Flight||field.Phase==FieldGoalPhase.Replay;
   if(sport is BungeeMatch bungee)return bungee.Phase==BungeePhase.Demolition&&bungee.Clock<58;
   if(sport is WhackMatch whack)return whack.Countdown<=0&&whack.rules.Clock>=2;
   if(sport is LavaMatch lava)return lava.Countdown<=0&&lava.rules.Clock>=2;
   if(sport is VolleyMatch volley)return volley.Countdown<=0&&volley.rules.Live;
   if(sport is AvalancheMatch avalanche)return avalanche.rules.Phase==AvalanchePhase.Racing&&avalanche.rules.Clock>=2;
   if(sport is RedLightMatch red)return red.rules.Phase!=RedLightPhase.Countdown&&red.rules.PhaseTime>=1;
   if(sport is NinjaMatch ninja)return ninja.athletes.Any(athlete=>athlete.Charging)||ninja.rules.falls.Sum()>0||ninja.rules.Clock>3;
   return Playing;
  }
  string State()
  {
   if(corn!=null)return "round="+corn.Round+" phases="+string.Join(",",corn.courses.Take(corn.setup.count).Select(course=>course.Phase.ToString()));
   return sport.Header+" | "+sport.Status;
  }
  bool LiveSample()
  {
   if(!Playing||Time.timeScale!=1)return false;if(sport is DockMatch dock)return dock.Phase==DockPhase.Approach||dock.Phase==DockPhase.Flight;if(sport is FieldGoalMatch field)return field.Phase==FieldGoalPhase.Approach||field.Phase==FieldGoalPhase.Flight;return true;
  }
  IEnumerator SampleLive(string scene)
  {
   Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.6f);var samples=new List<float>(1024);long allocated=Profiler.GetTotalAllocatedMemoryLong(),mono=Profiler.GetMonoUsedSizeLong();float start=Time.realtimeSinceStartup,deadline=start+3;int focused=0;
   while(Time.realtimeSinceStartup<deadline){yield return null;if(LiveSample()&&Screen.width==1280&&Screen.height==720){samples.Add(Time.unscaledDeltaTime*1000);if(Application.isFocused)focused++;}}
   long after=Profiler.GetTotalAllocatedMemoryLong(),monoAfter=Profiler.GetMonoUsedSizeLong();float elapsed=Time.realtimeSinceStartup-start;
   if(samples.Count==0){Note("LIVE_DIAGNOSTIC_UNAVAILABLE "+scene+" no eligible live1x frames; no frozen/resize samples substituted");yield break;}samples.Sort();float mean=samples.Average(),p95=samples[Mathf.Clamp(Mathf.CeilToInt(samples.Count*.95f)-1,0,samples.Count-1)];performance.AppendLine(scene+"\t1280x720\t"+elapsed.ToString("F3")+"\t"+samples.Count+"\t"+mean.ToString("F3")+"\t"+p95.ToString("F3")+"\t"+samples[samples.Count-1].ToString("F3")+"\t"+allocated+"\t"+after+"\t"+mono+"\t"+monoAfter+"\t"+focused+"\tunavailable-not-sampled");File.WriteAllText(Path.Combine(folder,"live-diagnostics.tsv"),performance.ToString());Note("LIVE_DIAGNOSTIC "+scene+" frames="+samples.Count+" meanWallMs="+mean.ToString("F2")+" p95WallMs="+p95.ToString("F2")+" GPU unavailable; diagnostic only, no FPS acceptance");
  }
  void Rendered(ScriptableRenderContext context,Camera camera){if(capturing!=null&&capturing.Contains(camera))rendered.Add(camera.GetInstanceID());}
  IEnumerator Capture(string scene)
  {
   foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
   {
    Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.45f);float until=Time.realtimeSinceStartup+4;while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<until)yield return null;
    Check(Screen.width==size.x&&Screen.height==size.y,scene+" actual player backbuffer resolution "+size);capturing=Cameras;rendered.Clear();RenderPipelineManager.endCameraRendering+=Rendered;
    string path=Path.Combine(folder,scene+"-"+caseLabel+"-"+size.x+"x"+size.y+".png");
    try{yield return new WaitForEndOfFrame();requestFrame=Time.frameCount;requestState=State();ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.25f);for(int retry=0;retry<8&&!File.Exists(path);retry++)yield return new WaitForSecondsRealtime(.1f);Check(File.Exists(path),scene+" actual ScreenCapture file completed "+caseLabel);if(File.Exists(path))Inspect(path,scene,size);Check(capturing.All(camera=>rendered.Contains(camera.GetInstanceID())),scene+" every registered gameplay viewport rendered during the real capture interval");}
    finally{RenderPipelineManager.endCameraRendering-=Rendered;capturing=null;}
   }
  }
  Renderer[] Subjects()
  {
   var renderers=new List<Renderer>();foreach(var truck in FindObjectsByType<SportsEventTruck>(FindObjectsSortMode.None))renderers.AddRange(truck.GetComponentsInChildren<Renderer>(true));if(corn!=null)foreach(var course in corn.courses)foreach(var truck in course.trucks)if(truck.gameObject.activeInHierarchy)renderers.AddRange(truck.Visuals);
   return renderers.Where(renderer=>renderer!=null&&renderer.enabled&&renderer.gameObject.activeInHierarchy&&renderer.GetComponent<TMP_Text>()==null&&renderer.bounds.size.sqrMagnitude>.000001f&&(renderer is MeshRenderer||renderer is SkinnedMeshRenderer)).Distinct().ToArray();
  }
  bool ObjectiveVisible(Camera camera,Plane[] planes)
  {
   Transform target=corn!=null?corn.courses[0].board:sport is TargetMatch launch?launch.target:sport is FieldGoalMatch field?field.posts:sport is SwingMatch swing?swing.rigVisual:null;if(target==null)return false;return target.GetComponentsInChildren<Renderer>(true).Any(renderer=>renderer.enabled&&renderer.gameObject.activeInHierarchy&&GeometryUtility.TestPlanesAABB(planes,renderer.bounds)&&(camera.cullingMask&(1<<renderer.gameObject.layer))!=0);
  }
  void Inspect(string path,string scene,Vector2Int size)
  {
   var texture=new Texture2D(2,2,TextureFormat.RGB24,false);try
   {
    var bytes=File.ReadAllBytes(path);bool valid=texture.LoadImage(bytes);Check(valid&&texture.width==size.x&&texture.height==size.y,scene+" PNG is original backbuffer dimensions, no resized substitute");if(!valid)return;screenshots++;var pixels=texture.GetPixels32();var subjects=Subjects();var cameras=Cameras;float area=cameras.Sum(camera=>camera.rect.width*camera.rect.height);Check(cameras.Length>0&&Mathf.Abs(area-1)<.002f,scene+" actual viewports cover the screen once");var owners=new List<int>();
    for(int view=0;view<cameras.Length;view++)
    {
     var camera=cameras[view];for(int other=view+1;other<cameras.Length;other++)Check(!camera.rect.Overlaps(cameras[other].rect),scene+" viewport rectangles do not overlap "+view+"/"+other);var planes=GeometryUtility.CalculateFrustumPlanes(camera);int visible=subjects.Count(renderer=>(camera.cullingMask&(1<<renderer.gameObject.layer))!=0&&GeometryUtility.TestPlanesAABB(planes,renderer.bounds));bool objective=ObjectiveVisible(camera,planes);Check(visible>0||objective,scene+" actual truck mesh or original gameplay objective intersects viewport "+view);
     int owner=Owner(view,camera);owners.Add(owner);if(owner>=0)Check(owner<Setup.count&&Setup.players[owner].source==BowlingSource.AI,scene+" viewport "+view+" mapped to actual AI slot "+owner);else Note("SHARED_CAMERA_OWNER "+scene+" automatic arena/turn broadcast; no fake participant index");
     float low=1,high=0;int nonblack=0,total=0;for(int y=1;y<=7;y++)for(int x=1;x<=11;x++){int px=Mathf.Clamp(Mathf.RoundToInt((camera.rect.x+camera.rect.width*(.16f+x*.055f))*texture.width),0,texture.width-1),py=Mathf.Clamp(Mathf.RoundToInt((camera.rect.y+camera.rect.height*(.16f+y*.085f))*texture.height),0,texture.height-1);var color=pixels[py*texture.width+px];float luma=(color.r+color.g+color.b)/765f;low=Mathf.Min(low,luma);high=Mathf.Max(high,luma);if(color.r+color.g+color.b>18)nonblack++;total++;}
     float fraction=nonblack/(float)total;Check(fraction>.15f&&high-low>.012f,scene+" actual central world pixels are nonblank/variable viewport "+view+" range="+(high-low).ToString("F3")+" nonblack="+fraction.ToString("F3"));frames.AppendLine(scene+"\t"+caseLabel+"\t"+size.x+"x"+size.y+"\t"+requestFrame+"\t"+Time.frameCount+"\t"+requestState.Replace('\t',' ').Replace('\n',' ')+"\t"+State().Replace('\t',' ').Replace('\n',' ')+"\t"+cameras.Length+"\t"+owner+"\t"+visible+"\t"+objective+"\t"+(high-low).ToString("F4")+"\t"+fraction.ToString("F4"));
    }
    if(cameras.Length>1)Check(owners.All(owner=>owner>=0)&&owners.Distinct().Count()==owners.Count,scene+" split viewports retain distinct actual owners");File.WriteAllText(Path.Combine(folder,"frames.tsv"),frames.ToString());
   }finally{Destroy(texture);}
  }
  int Owner(int view,Camera camera)
  {
   if(corn!=null){int slot=Array.FindIndex(corn.courses,course=>course.chase.view==camera);if(slot>=0){var hud=corn.menus.GetComponent<CornholeCompactHud>();Check(hud!=null&&hud.cards[slot].root.gameObject.activeInHierarchy&&hud.cards[slot].ownerId==corn.setup.players[slot].participantId,"Cornhole actual viewport card/profile identity "+slot);}return slot;}
   if(sport.TryReadViewportCard(view,out int owner,out _,out _)){if(sport.CompactViewportHud&&sport.MultiViewHUD){var root=sport.menus.viewPanels[view].Find("Compact owned viewport score");var name=root!=null?root.Find("Participant name"):null;var text=name!=null?name.GetComponent<TMP_Text>():null;Check(root!=null&&root.gameObject.activeInHierarchy&&text!=null&&text.text==sport.ViewportCardLabel(view,owner),sport.Title+" typed viewport HUD agrees with actual owner "+owner);}return owner;}
   var driving=sport.GetComponent<SportsDrivingViews>();if(driving!=null&&!driving.NeedsArena&&view<driving.rally.Humans)return driving.rally.owners[view];var rally=sport.GetComponent<SportsRallyViews>();if(rally!=null&&rally.Humans>0&&view<rally.Humans)return rally.owners[view];return -1;
  }
  static bool AbortForRuntimeFlood(int runtimeErrorCount)=>runtimeErrorCount>12;
  static Mesh SharedMesh(Renderer renderer)=>renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer is MeshRenderer?renderer.GetComponent<MeshFilter>()?.sharedMesh:null;
  static bool ActualRenderedMesh(Renderer renderer)=>renderer!=null&&renderer.enabled&&renderer.gameObject.activeInHierarchy&&SharedMesh(renderer)!=null&&renderer.transform.lossyScale.sqrMagnitude>1e-8f&&renderer.bounds.size.sqrMagnitude>1e-8f;
  static Transform RecordedCounterpart(Transform source,Transform ghost,Transform live)
  {
   if(source==null||ghost==null||live==null||!(live==source||live.IsChildOf(source)))return null;
   var indices=new Stack<int>();for(var part=live;part!=source;part=part.parent)indices.Push(part.GetSiblingIndex());
   var original=source;var copy=ghost;while(indices.Count>0){int index=indices.Pop();if(index>=original.childCount||index>=copy.childCount)return null;original=original.GetChild(index);copy=copy.GetChild(index);if(original.name!=copy.name)return null;}return copy;
  }
  static bool SamePaint(Renderer source,Renderer ghost,int slot)
  {
   var a=new MaterialPropertyBlock();var b=new MaterialPropertyBlock();if(slot<0){source.GetPropertyBlock(a);ghost.GetPropertyBlock(b);}else{source.GetPropertyBlock(a,slot);ghost.GetPropertyBlock(b,slot);}
   if(a.isEmpty!=b.isEmpty)return false;foreach(string property in new[]{"_BaseColor","_Color","_EmissionColor"})if(!a.GetColor(property).Equals(b.GetColor(property)))return false;return true;
  }
  static bool TryRecordedAppearance(SportsMotionReplay replay,Transform model,Renderer[] originals,out Renderer[] rendered,out string detail)
  {
   rendered=Array.Empty<Renderer>();detail="recorded selected model unavailable";
   if(replay==null||!replay.Playing||model==null||replay.sources==null||replay.ghosts==null||replay.sources.Length==0||replay.ghosts.Length==0)return false;
   // Field Goal's authored truck is source0; never accept another source/owner
   // or a global ghost renderer simply because it happens to be visible.
   var root=RecordedCounterpart(replay.sources[0],replay.ghosts[0],model);if(root==null||!root.gameObject.activeInHierarchy){detail="selected model missing from original source0/ghost0 hierarchy";return false;}
   var copies=new List<Renderer>();foreach(var source in originals)
   {
    if(source==null||!source.enabled||SharedMesh(source)==null||source.transform.lossyScale.sqrMagnitude<=1e-8f)continue;
    var part=RecordedCounterpart(replay.sources[0],replay.ghosts[0],source.transform);var copy=part!=null?part.GetComponent(source.GetType()) as Renderer:null;
    if(copy==null||!ActualRenderedMesh(copy)||SharedMesh(source)!=SharedMesh(copy)||!source.sharedMaterials.SequenceEqual(copy.sharedMaterials)||!SamePaint(source,copy,-1)){detail="recorded geometry/material/renderer paint mismatch: "+source.name;return false;}
    for(int slot=0;slot<source.sharedMaterials.Length;slot++)if(!SamePaint(source,copy,slot)){detail="recorded material-slot paint mismatch: "+source.name+"/"+slot;return false;}copies.Add(copy);
   }
   rendered=copies.ToArray();detail="source0 selectedRoot="+model.name+" recordedRoot="+root.name+" matchingVisibleMeshes="+rendered.Length;return rendered.Length>0;
  }
  void CheckAppearances(string scene)
  {
   var sceneHandle=SceneManager.GetActiveScene();var hosts=FindObjectsByType<SportsTruckAppearanceHost>(FindObjectsSortMode.None).Where(host=>host.gameObject.scene==sceneHandle).ToArray();int applied=0;foreach(var host in hosts)
   {
    if(!host.Applied||string.IsNullOrEmpty(host.ProfileId))continue;applied++;activeAppearanceHosts++;effectiveBodies.Add(host.EffectiveBodyId);var requested=SportsParticipantRegistry.GetAppearance(host.ProfileId);Check(requested!=null&&!string.IsNullOrEmpty(requested.bodyId),scene+" applied host retains actual profile ID and requested body");Check(catalog.FindBody(host.EffectiveBodyId)!=null,scene+" effective composed body is installed catalog content "+host.EffectiveBodyId);
    var exterior=host.ExteriorRenderers.Where(renderer=>renderer!=null&&!(host.auxiliaryExteriorRenderers??Array.Empty<Renderer>()).Contains(renderer)).ToArray();
    var rendered=exterior.Where(ActualRenderedMesh).ToArray();bool recorded=false;
    if(sport is FieldGoalMatch field&&field.Phase==FieldGoalPhase.Replay)
    {
     recorded=true;bool owner=field.rules.Current>=0&&field.rules.Current<field.setup.count&&field.setup.players[field.rules.Current].participantId==host.ProfileId;
     bool matched=TryRecordedAppearance(field.replay,host.AppearanceRoot,exterior,out rendered,out var detail);Check(owner&&matched,scene+" actual recorded selected model/profile/mesh/material/paint agree with hidden live source; "+detail);Note("RECORDED_APPEARANCE "+scene+" phase="+field.Phase+" current="+field.rules.Current+" owner="+owner+" "+detail);
    }
    Check(rendered.Length>0,scene+(recorded?" actual recorded composed exterior renderers exist":" actual composed exterior renderers exist"));Check(rendered.Length>0&&rendered.SelectMany(renderer=>renderer.sharedMaterials).All(material=>material!=null&&material.shader!=null&&material.shader.isSupported&&material.shader.name!="Hidden/InternalErrorShader"),scene+" actual composed exterior material shaders supported");
    var truck=host.GetComponentInParent<SportsEventTruck>();if(!recorded&&truck!=null&&rendered.Length>0)foreach(var framing in FindObjectsByType<SportsBroadcastFraming>(FindObjectsSortMode.None))for(int slot=0;slot<framing.SubjectCount;slot++)if(framing.Valid(slot)&&framing.Body(slot)==truck.body){var envelope=framing.TruckBounds(slot);envelope.Expand(.1f);Check(rendered.All(renderer=>envelope.Contains(renderer.bounds.min)&&envelope.Contains(renderer.bounds.max)),scene+" existing framing envelope contains actual composed body geometry slot="+slot);}
    Note("APPEARANCE "+scene+" profile="+host.ProfileId+" requested="+requested?.bodyId+" effective="+host.EffectiveBodyId+" notice="+host.CompatibilityNotice+" renderers="+rendered.Length);
   }
   Note("APPEARANCE_SCOPE "+scene+" appliedHosts="+applied+"; modes without authored hosts are venue/HUD captures only, never claimed customized-body acceptance.");
  }
  void CheckFinish(string scene)
  {
   var source=FindAnyObjectByType<SportsRaceFinishSource>();if(source==null){Note("FINISH_SCOPE "+scene+" no racing finish presenter applies; no invented race finish");return;}Check(source.gates!=null&&source.gates.Length>0&&source.gates.All(gate=>gate!=null&&gate.root!=null&&gate.checks!=null&&gate.checks.sharedMesh!=null&&gate.checks.sharedMesh.vertexCount>0),scene+" original actual-race checkered finish geometry bound");int count=source.Read(finishes);var presentation=FindAnyObjectByType<SportsRaceFinishPresentation>();Note("FINISH_OBSERVED "+scene+" actualConfirmed="+count+" victoryBursts="+(presentation!=null?presentation.VictoryBursts:0)+"; short natural tour, zero does not claim finish-celebration execution");foreach(var receipt in finishes)Note("ACTUAL_FINISH "+receipt.id+" place="+receipt.place+" seconds="+receipt.seconds.ToString("F3")+" winner="+receipt.winner);
  }
  IEnumerator ReturnFront(string scene)
  {
   var oldScene=SceneManager.GetActiveScene();var oldHosts=FindObjectsByType<SportsTruckAppearanceHost>(FindObjectsSortMode.None).Where(host=>host.gameObject.scene==oldScene).ToArray();StopMatch();SportsNavigation.FrontEnd();yield return null;yield return null;yield return new WaitForSecondsRealtime(.1f);Check(FindAnyObjectByType<SportsFrontEnd>()!=null&&FindAnyObjectByType<SportsEventSession>()==null&&FindAnyObjectByType<CornholeMatch>()==null,scene+" original exit returns to collection and clears scene match ownership");Check(oldHosts.All(host=>host==null),scene+" prior scene's composed appearance hosts were destroyed by original unload");sport=null;corn=null;
  }
  void Finish()
  {
   if(finished)return;finished=true;RenderPipelineManager.endCameraRendering-=Rendered;Application.logMessageReceived-=Error;Time.timeScale=1;preferences?.Dispose();preferences=null;SportsParticipantRegistry.ForgetCache();log.AppendLine("RUNTIME_ERRORS="+runtimeErrors);log.AppendLine("CHECKS="+checks+" ERRORS="+errors+" SCENES="+visited+" CASES="+cases+" SCREENSHOTS="+screenshots+" PID="+System.Diagnostics.Process.GetCurrentProcess().Id+" SECONDS="+(Time.realtimeSinceStartup-began).ToString("F2"));log.AppendLine("Actual Windows ScreenCapture and original natural AI/current states. AI views are not physical humans. No supplied poses/forces/scores/results; no full-match, controller, listening, human visual approval or performance/FPS/GPU acceptance. Imported art is existing runtime content; no raw FBX/texture/package/license-source copies in this probe handoff.");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());File.WriteAllText(Path.Combine(folder,"frames.tsv"),frames.ToString());File.WriteAllText(Path.Combine(folder,"live-diagnostics.tsv"),performance.ToString());Debug.Log("TS_CARNIVAL_PRESENTATION_FINISHED code="+(errors==0?0:2));Application.Quit(errors==0?0:2);
  }
  void OnDestroy(){RenderPipelineManager.endCameraRendering-=Rendered;Application.logMessageReceived-=Error;Time.timeScale=1;if(!finished&&evidenceOwned&&folder!=null&&Directory.Exists(folder))File.WriteAllText(Path.Combine(folder,"result.txt"),log+"\nINTERRUPTED; no terminal acceptance footer.");preferences?.Dispose();SportsParticipantRegistry.ForgetCache();if(!finished&&oldWidth>0&&oldHeight>0)Screen.SetResolution(oldWidth,oldHeight,oldScreenMode);}
 }
}
#endif
