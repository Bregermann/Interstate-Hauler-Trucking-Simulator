using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckFareFight {
 public sealed class FareFightProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsFareFight");bool run=at>=0;string mode=at>=0&&at+1<args.Length?args[at+1]:"smoke";
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("Sports.FareFight.Probe",false);UnityEditor.SessionState.SetBool("Sports.FareFight.Probe",false);if(Application.isEditor)mode=UnityEditor.SessionState.GetString("Sports.FareFight.Mode","smoke");
#endif
   if(!run)return;var root=new GameObject("Explicit Fare Fight physical acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<FareFightProbe>().mode=mode;
  }
  string mode,folder;IDisposable prefs;int checks,errors;readonly StringBuilder report=new StringBuilder();
  void Awake(){prefs=SportsPrefs.IsolateValidation("FareFight");SportsParticipantRegistry.ForgetCache();}
  void Check(bool value,string label){checks++;if(!value)errors++;report.AppendLine((value?"PASS ":"FAIL ")+label);Flush();}
  void Flush(){File.WriteAllText(folder+"/progress.txt",report.ToString());}
  void Error(string message,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception){errors++;report.AppendLine("ERROR "+message);Flush();}}
  IEnumerator Start(){
   folder="Logs/TruckSports/EssentialRelease/FareFight/"+(Application.isEditor?"Native/":"Windows/")+mode;Directory.CreateDirectory(folder);File.WriteAllText(folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("o"));Application.logMessageReceived+=Error;
   yield return SportsProbeScene.Ensure<FareFightMatch>("TruckFareFight");yield return null;var m=FindAnyObjectByType<FareFightMatch>();
   Check(m!=null&&m.trucks.Length==4&&m.district.stops.Length==20,"saved original-town scene contains four full NWH tractors and twenty authored stops");
   bool defaultMatch=mode.StartsWith("default",StringComparison.Ordinal),fullMatch=mode=="full"||defaultMatch;m.option=defaultMatch?1:0;m.AllBots();if(defaultMatch){var level=mode.EndsWith("-easy",StringComparison.Ordinal)?BowlingDifficulty.Easy:mode.EndsWith("-hard",StringComparison.Ordinal)?BowlingDifficulty.Hard:BowlingDifficulty.Normal;for(int i=0;i<m.setup.count;i++)m.setup.players[i].difficulty=level;}foreach(var preset in m.presets.entries)if(preset.displayName=="Clear"){m.presets.Select(preset.id);break;}
   Check(m.StartMatch(),"four-AI match starts without controllers");Time.timeScale=3;
   Physics.SyncTransforms();float shortest=float.PositiveInfinity,longest=0;bool overlap=false;for(int i=0;i<m.setup.count;i++){shortest=Mathf.Min(shortest,m.InitialRouteDistances[i]);longest=Mathf.Max(longest,m.InitialRouteDistances[i]);for(int j=0;j<i;j++)overlap|=m.trucks[i].shape.bounds.Intersects(m.trucks[j].shape.bounds);}
   Check(shortest>200&&longest/shortest<1.1f,"initial driving distances are fair, without immediate pickup advantage: "+string.Join(",",m.InitialRouteDistances));
   Check(!overlap,"actual initialized full-truck colliders do not overlap");
   var start=new Vector3[4];for(int i=0;i<4;i++)start[i]=m.trucks[i].body.position;
   float end=Time.realtimeSinceStartup+(defaultMatch?900:fullMatch?600:110),next=0;bool captured=false,paused=false;float maxSpeed=0;
   while(!m.Finished&&Time.realtimeSinceStartup<end){
    for(int i=0;i<4;i++)maxSpeed=Mathf.Max(maxSpeed,m.trucks[i].body.linearVelocity.magnitude);
    if(!paused&&m.rules.Carrier>=0){paused=true;Time.timeScale=1;var position=m.trucks[m.rules.Carrier].body.position;float clock=m.rules.Clock;int carrier=m.rules.Carrier;m.Pause();yield return new WaitForSecondsRealtime(.25f);Check(m.rules.Clock==clock&&m.rules.Carrier==carrier&&m.trucks[carrier].body.position==position,"pause preserves carrier, real truck pose and match clock");m.Resume();Time.timeScale=3;}
    if(!captured&&m.rules.Clock>10){captured=true;foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){SportsControlledCapture.Save(m,size.x,size.y,folder+"/town-"+size.x+"x"+size.y+".png");yield return null;}}
    if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+3;report.AppendLine("LIVE t="+m.rules.Clock.ToString("F1")+" phase="+m.rules.Phase+" carrier="+m.rules.Carrier+" points="+string.Join(",",m.rules.score)+" contacts="+m.PhysicalContacts+" traffic="+m.traffic.ActiveCount+" peds="+m.pedestrians.ActiveCount);for(int i=0;i<4;i++)report.AppendLine("  slot="+i+" p="+m.trucks[i].body.position+" v="+m.trucks[i].body.linearVelocity.magnitude.ToString("F1")+" route="+m.routes[i].Cursor+"/"+m.routes[i].points.Count+" searches="+m.routes[i].Searches+" target="+(m.routes[i].points.Count>0?m.routes[i].points[m.routes[i].Cursor].ToString():"none")+" state="+m.ObjectiveState(i)+" blocked="+m.routes[i].BlockingObstacle+" stalled="+m.routes[i].SecondsWithoutProgress.ToString("F1")+" reversals="+m.routes[i].RecoveryStarts+" input="+m.trucks[i].command);Flush();}
    if(!fullMatch&&m.rules.Deliveries>=1&&m.rules.Clock>20)break;yield return null;
   }
   Time.timeScale=1;Check(maxSpeed>3,"actual NWH propulsion moves competing trucks");Check(m.rules.pickups[0]+m.rules.pickups[1]+m.rules.pickups[2]+m.rules.pickups[3]>0,"physical roadside crossing earns a passenger pickup");Check(m.rules.Deliveries>0,"actual AI navigation completes a real passenger delivery");
   if(fullMatch){Check(m.Finished&&m.rules.Phase==FareFightPhase.Complete,"complete first-to-"+m.rules.Target+" four-AI match through real driving, pickups and deliveries");if(defaultMatch)Check(m.rules.Target==5&&System.Array.Exists(m.rules.score,value=>value==5),"default target is five actual physical deliveries, without a free fifth point");}
   Check(m.traffic.Ready&&m.traffic.ActiveCount>0,"reused UTS ordinary traffic is active without rival taxis");Check(m.pedestrians.Ready&&m.pedestrians.ActiveCount>0,"original UTS/Taxi pedestrian ecosystem is active");
   Check(FindObjectsByType<AudioListener>(FindObjectsInactive.Include).Length==1,"one shared AudioListener despite four full Taxi vehicles");
   m.SetupScreen();Check(m.StartMatch()&&m.rules.Deliveries==0&&m.rules.Carrier==-1,"rematch clears possession, scores and participant route caches");yield return null;m.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<FareFightMatch>()==null&&FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"menu return cleans match, four vehicles, GPS and population");
   report.AppendLine("CHECKS="+checks+" ERRORS="+errors+" · actual NWH/PhysX and automated AI; no human or physical-device claim.");File.WriteAllText(folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
   if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
   Application.Quit(errors==0?0:2);
  }
  void OnDestroy(){Application.logMessageReceived-=Error;prefs?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
#endif
 }
}
