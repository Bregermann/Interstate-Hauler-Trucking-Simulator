using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckPedestrianPlowing {
 public sealed partial class PlowValidationProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsPlowing")>=0;
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("LWS.PlowProbe",false);UnityEditor.SessionState.SetBool("LWS.PlowProbe",false);
#endif
   if(run){var g=new GameObject("Plowing explicit validation");DontDestroyOnLoad(g);if(FindAnyObjectByType<SportsProbeInputIsolation>()==null)g.AddComponent<SportsProbeInputIsolation>();g.AddComponent<PlowValidationProbe>();}}
  readonly StringBuilder log=new StringBuilder();int errors;string folder;PlowMatch m;bool done;PlowSettings testSettings;InputSettings originalInput,isolatedInput;
  void Check(bool value,string label){log.AppendLine((value?"PASS ":"FAIL ")+label);if(!value)errors++;Debug.Log("PLOW_CHECK "+(value?"PASS ":"FAIL ")+label);}
  void Error(string text,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error){errors++;log.AppendLine("RUNTIME ERROR "+text+"\n"+stack);}}
  IEnumerator Start(){originalInput=InputSystem.settings;isolatedInput=Instantiate(originalInput);isolatedInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings=isolatedInput;folder=Path.GetFullPath(Path.Combine(Application.dataPath,Application.isEditor?"..":"../../..","Logs","TruckSports","Plowing",Application.isEditor?"NativeRuntime":"Runtime"));Directory.CreateDirectory(folder);File.WriteAllText(folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("O"));Application.logMessageReceived+=Error;
   string mode=Mode();log.AppendLine("PLOW_RUN mode="+mode+" pid="+System.Diagnostics.Process.GetCurrentProcess().Id+" editor="+Application.isEditor+" buildGuid="+Application.buildGUID);if(mode.StartsWith("cab"))yield return SportsProbeScene.Ensure<PlowCabMatch>(SportsNavigation.PedestrianPlowingCab);else yield return SportsProbeScene.Ensure<PlowMatch>(SportsNavigation.PedestrianPlowing);yield return null;m=FindAnyObjectByType<PlowMatch>();
   testSettings=Instantiate(m.crowdSettings);m.crowdSettings=testSettings;testSettings.varySeedEachMatch=false;if(mode=="dense")testSettings.crowdSize=384;m.setup.count=8;m.NormalizeSetup();m.AllBots();for(int i=0;i<8;i++)m.setup.players[i].difficulty=BowlingDifficulty.Easy;Check(m.StartMatch(),"eight AI setup starts");yield return new WaitForSecondsRealtime(2);
   Check(m.Crowd.Length==m.crowdSettings.crowdSize,"full authored crowd spawned");int reachable=0,jointed=0;foreach(var p in m.Crowd){if(m.TryRoute(m.truckStarts[0].position,p.transform.position,out _))reachable++;if(p.ragdoll.HasJointedRagdoll)jointed++;}Check(reachable==m.Crowd.Length,"all targets truck-clear connected navigation "+reachable);Check(jointed==m.Crowd.Length,"Taxi jointed rigs "+jointed);
   Check(System.Array.TrueForAll(m.Crowd,p=>p.presentationLOD!=null&&p.distanceModel!=null&&p.detailedRenderers.Length>10),"native LOD retains original detailed and jointed models");Check(System.Array.TrueForAll(m.Crowd,p=>p.costumeNose!=null&&p.costumeNose.gameObject.activeSelf==(p.role==PlowTargetKind.Standard)&&p.costumeNose.GetComponent<Collider>()==null),"standard-only noncolliding cartoon costume noses");yield return Capture("arena-initial");
   if(mode=="smoke"||mode=="dense"){yield return new WaitForSecondsRealtime(16);Check(m.PhysicalHits>0,"actual physical AI hits="+m.PhysicalHits);yield return Capture("arena-physical");m.Pause();Check(m.Paused&&Time.timeScale==0,"pause preserves scene");float elapsed=m.Round.Elapsed;yield return new WaitForSecondsRealtime(.3f);Check(m.Round.Elapsed==elapsed,"clock frozen on pause");m.Resume();}
   else if(mode=="lod"){m.ProbeHandsOff=true;m.views.enabled=false;m.menus.hud.SetActive(false);var seen=new System.Collections.Generic.HashSet<string>();foreach(var p in m.Crowd){if(p.role!=PlowTargetKind.Standard||!seen.Add(p.characterName))continue;m.views.ReplayView(p.DisplayPosition,0);var camera=m.cameras[0];camera.orthographic=false;camera.transform.SetPositionAndRotation(p.DisplayPosition+new Vector3(3,1,-4),Quaternion.LookRotation(new Vector3(-3,-1,4)));p.agent.isStopped=true;for(int level=0;level<2;level++){p.presentationLOD.ForceLOD(level);yield return new WaitForSecondsRealtime(.1f);yield return Capture("lod-"+seen.Count+"-"+level);}p.presentationLOD.ForceLOD(-1);if(seen.Count==m.crowdSettings.characters.Length)break;}Check(seen.Count==m.crowdSettings.characters.Length,"all original character distance models rendered");m.views.enabled=true;m.ProbeHandsOff=false;}
   else if(mode=="costumes"||mode=="cabcostumes")yield return CostumeCases();
   else if(mode=="recoverybaseline"||mode=="recovery")yield return RecoveryCases(mode=="recoverybaseline");
   else if(mode=="physical")yield return PhysicalCases();
   else if(mode=="impacts"||mode=="cabimpacts")yield return ImpactCases();
   else if(mode=="cab")yield return CabCases();
   else if(mode=="matches"||mode=="cabmatches"){for(int game=0;game<3;game++){if(game>0){m.SetupScreen();for(int i=0;i<8;i++)m.setup.players[i].difficulty=(BowlingDifficulty)game;m.StartMatch();}Time.timeScale=3;float deadline=Time.realtimeSinceStartup+180;while(!m.Finished&&Time.realtimeSinceStartup<deadline)yield return null;Time.timeScale=1;Check(m.Finished,"full physical eight-AI match "+game);Check(m.PhysicalHits>0,"physical hits difficulty "+game+" hits="+m.PhysicalHits);for(int slot=0;slot<m.setup.count;slot++)Check(m.Round.StandardHits(slot)+m.Round.BonusHits(slot)+m.Round.Penalties(slot)>0,"individual AI contributes difficulty="+game+" slot="+slot);int jumps=0,climbs=0;foreach(var ped in m.Crowd){var evasion=ped.GetComponent<PlowBonusEvasion>();if(evasion!=null){jumps+=evasion.JumpCount;climbs+=evasion.ClimbCount;}}log.AppendLine("EVASION actual threat responses jumps="+jumps+" climbs="+climbs);log.AppendLine(m.Results);yield return Capture("results-"+game);}}
   else if(mode=="views"){var pads=new Gamepad[4];for(int i=0;i<4;i++)pads[i]=InputSystem.AddDevice<Gamepad>();for(int count=1;count<=4;count++){m.SetupScreen();m.setup.count=8;for(int i=0;i<8;i++){m.setup.players[i].source=i<count?BowlingSource.WholeController:BowlingSource.AI;m.setup.players[i].pad=i<count?pads[i]:null;m.setup.players[i].name="Long Participant "+(i+1);}for(int cameraMode=0;cameraMode<3;cameraMode++){m.option=cameraMode;Check(m.StartMatch(),"injected "+count+" human views mode="+cameraMode);yield return new WaitForSecondsRealtime(.3f);Check(m.cameras.Length==(cameraMode==0?1:count),"camera ownership "+count+" / "+cameraMode);Check(FindObjectsByType<AudioListener>().Length==1,"one listener");foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(1280,800),new Vector2Int(1024,768),new Vector2Int(1920,800)}){Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.2f);Check(Screen.width==size.x&&Screen.height==size.y,"actual requested viewport resolution "+size);for(int slot=0;slot<count;slot++){int view=cameraMode==0?0:slot;var lookPoint=cameraMode==2?m.trucks[slot].body.position+Vector3.up*1.3f+m.trucks[slot].transform.forward*8:m.trucks[slot].body.position+Vector3.up;var position=m.cameras[view].WorldToViewportPoint(lookPoint);Check(position.z>0&&position.x>0&&position.x<1&&position.y>(cameraMode==0?.2f:0)&&position.y<(cameraMode==0?.88f:1),"owner driving focal point remains in its actual rendered viewport outside shared HUD "+count+" / "+cameraMode+" / "+slot);}yield return Capture("views-"+count+"-"+cameraMode+"-"+size.x+"x"+size.y);}m.SetupScreen();}}
    m.setup.count=2;m.setup.players[0].source=BowlingSource.LeftStick;m.setup.players[0].pad=pads[0];m.setup.players[1].source=BowlingSource.RightStick;m.setup.players[1].pad=pads[0];Check(m.ValidateSources(out _),"shared opposite sticks allowed");m.setup.players[1].source=BowlingSource.WholeController;Check(!m.ValidateSources(out _),"whole/shared conflict rejected");foreach(var p in pads)InputSystem.RemoveDevice(p);
   }
   else if(mode.StartsWith("performance")||mode.StartsWith("cabperformance"))yield return PerformanceCases(mode);
   Check(m.Round.AwardCount==m.PhysicalHits,"one authoritative award per actual accepted impact");m.SetupScreen();Check(m.StartMatch()||m.menus.Screen==SportsEventScreen.Join,"rematch restores finite roster or correctly awaits removed injected devices");m.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindObjectsByType<PlowPedestrian>(FindObjectsInactive.Include).Length==0&&FindObjectsByType<PlowMatch>().Length==0,"scene-owned crowd and authority cleaned");Check(PlowRoleCostume.SharedMaterialCount==0,"scene-owned role material palette released");Check(FindObjectsByType<AudioListener>().Length==1,"front-end listener restored");Finish();
  }
  string Mode(){string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-tsPlowing");string mode=index>=0&&index+1<args.Length?args[index+1]:"smoke";
#if UNITY_EDITOR
   mode=UnityEditor.SessionState.GetString("LWS.PlowMode","smoke");
#endif
   return mode;}
  IEnumerator Capture(string name){if(Application.isEditor){log.AppendLine("NOT_TESTED hidden Editor capture "+name+"; actual Windows capture required");yield break;}ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return new WaitForSecondsRealtime(.15f);}
  void Finish(){done=true;Time.timeScale=1;log.AppendLine("ERRORS="+errors+"; injected input / real PhysX / no human or hardware playtest.");File.WriteAllText(folder+"/result.txt",log.ToString());Debug.Log("PLOW_PROBE_COMPLETE "+errors);if(!Application.isEditor)Application.Quit(errors==0?0:2);}
  void Update(){if(errors>0&&!done){StopAllCoroutines();Finish();}}
  void OnDestroy(){if(originalInput!=null)InputSystem.settings=originalInput;if(isolatedInput!=null)Destroy(isolatedInput);Application.logMessageReceived-=Error;if(testSettings!=null)Destroy(testSettings);if(!done&&folder!=null)File.WriteAllText(folder+"/result.txt",log+"\nINCOMPLETE");}
#endif
 }
}
