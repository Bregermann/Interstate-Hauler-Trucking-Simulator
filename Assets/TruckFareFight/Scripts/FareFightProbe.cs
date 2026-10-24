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
  string ChassisState(SportsEventTruck truck){
   int grounded=0;var wheelState=new StringBuilder();foreach(var wheel in truck.GetComponentsInChildren<NWH.WheelController3D.WheelController>()){if(wheel.enabled&&wheel.IsGrounded)grounded++;wheelState.Append(wheel.name).Append(':').Append(wheel.enabled).Append('/').Append(wheel.MotorTorque.ToString("F0")).Append('/').Append(wheel.BrakeTorque.ToString("F0")).Append('/').Append(wheel.SteerAngle.ToString("F0")).Append(';');}
   var bounds=truck.shape.bounds;var hits=Physics.OverlapBox(bounds.center,bounds.extents+Vector3.one*.15f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);var contacts=new StringBuilder();foreach(var hit in hits)if(!hit.transform.IsChildOf(truck.transform))contacts.Append(hit.name).Append('@').Append(hit.bounds.max.y.ToString("F2")).Append(';');
   var vehicle=truck.tractorMotor.vehicle;
   return " rotation="+truck.body.rotation.eulerAngles+" groundedWheels="+grounded+" chassisNeighbors="+contacts+" sleep="+truck.body.IsSleeping()+" kinematic="+truck.body.isKinematic+" constraints="+truck.body.constraints+" powered="+truck.Powered+" nativeEnabled="+vehicle.enabled+" powertrainActive="+vehicle.powertrain.IsActive+" engineActive="+vehicle.powertrain.engine.IsActive+" transmissionActive="+vehicle.powertrain.transmission.IsActive+" engineConnection="+vehicle.powertrain.engine.outputNameHash+" ignition="+vehicle.powertrain.engine.ignition+" stalledEngine="+vehicle.powertrain.engine.IsStalled+" throttlePosition="+vehicle.powertrain.engine.ThrottlePosition+" rpm="+vehicle.powertrain.engine.OutputRPM+" generatedPower="+vehicle.powertrain.engine.generatedPower+" actualInputs="+vehicle.input.Throttle+"/"+vehicle.input.Brakes+"/"+vehicle.input.Handbrake+" wheelEnabledMotorBrakeSteer="+wheelState;
  }
  void Error(string message,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception){errors++;report.AppendLine("ERROR "+message);Flush();}}
  IEnumerator Start(){
   folder="Logs/TruckSports/EssentialRelease/FareFight/"+(Application.isEditor?"Native/":"Windows/")+mode;Directory.CreateDirectory(folder);File.WriteAllText(folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("o"));Application.logMessageReceived+=Error;
   yield return SportsProbeScene.Ensure<FareFightMatch>("TruckFareFight");yield return null;var m=FindAnyObjectByType<FareFightMatch>();
   Check(m!=null&&m.trucks.Length==4&&m.district.stops.Length==20,"saved original-town scene contains four full NWH tractors and twenty authored stops");
   var launch=Environment.GetCommandLineArgs();int seedArgument=Array.IndexOf(launch,"-tsFareSeed");if(seedArgument>=0){if(seedArgument+1>=launch.Length||!int.TryParse(launch[seedArgument+1],out var requestedSeed)||requestedSeed<=0)throw new ArgumentException("-tsFareSeed requires the positive recorded match seed.");m.validationSeed=requestedSeed;}
   bool defaultMatch=mode.StartsWith("default",StringComparison.Ordinal),fullMatch=mode=="full"||defaultMatch;m.option=defaultMatch?1:0;m.AllBots();if(defaultMatch){var level=mode.EndsWith("-easy",StringComparison.Ordinal)?BowlingDifficulty.Easy:mode.EndsWith("-hard",StringComparison.Ordinal)?BowlingDifficulty.Hard:BowlingDifficulty.Normal;for(int i=0;i<m.setup.count;i++)m.setup.players[i].difficulty=level;}foreach(var preset in m.presets.entries)if(preset.displayName=="Clear"){m.presets.Select(preset.id);break;}
   Check(m.StartMatch(),"four-AI match starts without controllers");report.AppendLine("MATCH SEED="+m.matchSeed);Time.timeScale=3;
   Physics.SyncTransforms();float shortest=float.PositiveInfinity,longest=0;bool overlap=false;for(int i=0;i<m.setup.count;i++){shortest=Mathf.Min(shortest,m.InitialRouteDistances[i]);longest=Mathf.Max(longest,m.InitialRouteDistances[i]);for(int j=0;j<i;j++)overlap|=m.trucks[i].shape.bounds.Intersects(m.trucks[j].shape.bounds);}
   Check(shortest>200&&longest/shortest<1.1f,"initial driving distances are fair, without immediate pickup advantage: "+string.Join(",",m.InitialRouteDistances));
   Check(!overlap,"actual initialized full-truck colliders do not overlap");
   var start=new Vector3[4];var progressAt=new float[4];var longestStall=new float[4];for(int i=0;i<4;i++){start[i]=m.trucks[i].body.position;progressAt[i]=m.rules.Clock;}
   float end=Time.realtimeSinceStartup+(defaultMatch?900:fullMatch?600:110),next=0;bool captured=false,paused=false;float maxSpeed=0;
   while(!m.Finished&&Time.realtimeSinceStartup<end){
    for(int i=0;i<4;i++){maxSpeed=Mathf.Max(maxSpeed,m.trucks[i].body.linearVelocity.magnitude);if(Vector3.Distance(start[i],m.trucks[i].body.position)>2){start[i]=m.trucks[i].body.position;progressAt[i]=m.rules.Clock;}longestStall[i]=Mathf.Max(longestStall[i],m.rules.Clock-progressAt[i]);}
    if(!paused&&m.rules.Carrier>=0){paused=true;Time.timeScale=1;var position=m.trucks[m.rules.Carrier].body.position;float clock=m.rules.Clock;int carrier=m.rules.Carrier;m.Pause();yield return new WaitForSecondsRealtime(.25f);Check(m.rules.Clock==clock&&m.rules.Carrier==carrier&&m.trucks[carrier].body.position==position,"pause preserves carrier, real truck pose and match clock");m.Resume();Time.timeScale=3;}
    if(!captured&&m.rules.Clock>10){captured=true;foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){SportsControlledCapture.Save(m,size.x,size.y,folder+"/town-"+size.x+"x"+size.y+".png");yield return null;}}
    if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+3;report.AppendLine("LIVE t="+m.rules.Clock.ToString("F1")+" phase="+m.rules.Phase+" carrier="+m.rules.Carrier+" points="+string.Join(",",m.rules.score)+" contacts="+m.PhysicalContacts+" traffic="+m.traffic.ActiveCount+" peds="+m.pedestrians.ActiveCount);for(int i=0;i<4;i++)report.AppendLine("  slot="+i+" p="+m.trucks[i].body.position+" v="+m.trucks[i].body.linearVelocity.magnitude.ToString("F1")+" route="+m.routes[i].Cursor+"/"+m.routes[i].points.Count+" searches="+m.routes[i].Searches+" target="+(m.routes[i].points.Count>0?m.routes[i].points[m.routes[i].Cursor].ToString():"none")+" state="+m.ObjectiveState(i)+" blocked="+m.routes[i].BlockingObstacle+" stalled="+m.routes[i].SecondsWithoutProgress.ToString("F1")+" reversals="+m.routes[i].RecoveryStarts+" input="+m.trucks[i].command+" engineDamage="+m.trucks[i].tractorMotor.vehicle.powertrain.engine.Damage+" transmissionDamage="+m.trucks[i].tractorMotor.vehicle.powertrain.transmission.Damage+" running="+m.trucks[i].tractorMotor.vehicle.powertrain.engine.IsRunning+" gear="+m.trucks[i].tractorMotor.vehicle.powertrain.transmission.Gear+" noDisplacement="+(m.rules.Clock-progressAt[i]).ToString("F1")+(m.rules.Clock-progressAt[i]>10?ChassisState(m.trucks[i]):""));Flush();}
    if(!fullMatch&&m.rules.Deliveries>=1&&m.rules.Clock>20)break;yield return null;
   }
   Time.timeScale=1;Check(maxSpeed>3,"actual NWH propulsion moves competing trucks");Check(m.rules.pickups[0]+m.rules.pickups[1]+m.rules.pickups[2]+m.rules.pickups[3]>0,"physical roadside crossing earns a passenger pickup");Check(m.rules.Deliveries>0,"actual AI navigation completes a real passenger delivery");
   if(fullMatch){Check(System.Array.TrueForAll(longestStall,seconds=>seconds<60),"no competitor remains within a two-metre pocket for sixty simulation seconds: "+string.Join(",",longestStall)+"; actual announced recoveries="+m.Recoveries);Check(m.Finished&&m.rules.Phase==FareFightPhase.Complete,"complete first-to-"+m.rules.Target+" four-AI match through real driving, pickups and deliveries");if(defaultMatch)Check(m.rules.Target==5&&System.Array.Exists(m.rules.score,value=>value==5),"default target is five actual physical deliveries, without a free fifth point");}
if(m.Finished)yield return SportsResultsAcceptance.Capture(m,Check,folder,slot=>SportsResultPlacings.FromFinalTotals(slot,m.setup.count,m.rules.score),slot=>(double?)m.rules.score[slot],slot=>m.rules.winners[slot]);
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
