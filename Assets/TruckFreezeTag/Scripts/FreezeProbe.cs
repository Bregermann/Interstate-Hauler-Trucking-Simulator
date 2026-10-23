#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;using System.Collections;using System.IO;using System.Text;using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSports;using LWS.TruckBowling;
namespace LWS.TruckFreezeTag
{
 public sealed class FreezeProbe:MonoBehaviour
 {
  const string Folder="Logs/TruckSports/EighteenSports/Freeze";IDisposable preferences;int checks,errors;readonly StringBuilder report=new StringBuilder();Gamepad[] pads=new Gamepad[4];InputSettings.BackgroundBehavior background;FreezeMatch watched;bool intentionalPause;float requestedScale=1,watchdog;
  void Update(){if(watchdog>0&&Time.realtimeSinceStartup>watchdog){watchdog=0;Check(false,"bounded acceptance watchdog expired");watched?.SetupScreen();StopAllCoroutines();Complete();return;}if(watched!=null&&watched.Running&&!watched.Finished&&!intentionalPause&&watched.Paused&&watched.DevicesReady){report.AppendLine("HARNESS_ACKNOWLEDGED_EXTERNAL_PAUSE focused="+Application.isFocused);watched.Resume();Time.timeScale=requestedScale;}}
#if UNITY_EDITOR
  InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
  [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsFreeze")>=0;
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("Sports.Freeze.Probe",false);UnityEditor.SessionState.SetBool("Sports.Freeze.Probe",false);
#endif
   if(!run)return;var go=new GameObject("Explicit physical beach freeze-tag acceptance");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<FreezeProbe>();}
  void Awake(){preferences=SportsPrefs.IsolateValidation("Freeze");SportsParticipantRegistry.ForgetCache();background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
   editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
  }
  void Check(bool ok,string message){checks++;if(!ok)errors++;report.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
  void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message+"\n"+stack);}}
  IEnumerator Start()
  {
   watchdog=Time.realtimeSinceStartup+480;Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");Application.logMessageReceived+=Error;yield return SportsProbeScene.Ensure<FreezeMatch>("TruckFreezeTag");yield return null;var m=FindAnyObjectByType<FreezeMatch>();watched=m;
   Check(m!=null&&m.trucks.Length==8&&m.muzzles.Length==8&&m.shells.Length==8,"saved winter stadium has eight full NWH trucks, cannons, persistent ice and pooled shatter effects");if(m==null){Complete();yield break;}
   int coverCount=0;bool matchingCover=true;foreach(var shape in FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude))if(shape.name=="Visible solid snow-covered cargo cover"){coverCount++;bool first=true;Bounds visual=default;foreach(var renderer in shape.GetComponentsInChildren<Renderer>())if(renderer.enabled&&!renderer.name.StartsWith("Accumulated snow cap")){if(first){visual=renderer.bounds;first=false;}else visual.Encapsulate(renderer.bounds);}matchingCover&=!first&&shape.bounds.size.y>4&&Vector3.Distance(visual.center,shape.bounds.center)<.2f&&Vector3.Distance(visual.size,shape.bounds.size)<.3f;}Check(coverCount==6&&matchingCover,"all six genuine stacked-container cover silhouettes match their solid collision and block roof-height spray");
   using var scoreTally=new SportsScoreAcceptance.TeamTally(m);
   var clear=Array.Find(m.atmosphere.presets.entries,p=>p.legacyKind==SportsWeatherKind.Clear);if(clear!=null)m.atmosphere.presets.Select(clear.id);m.teamA=m.teamB=4;m.NormalizeSetup();m.AllBots();m.option=1;SportsCameraOptions.Set(false);m.drivingViews.Select(0);Check(m.StartMatch(),"default four-versus-four four-round asymmetric AI match starts");yield return new WaitForSeconds(1.5f);bool stable=!m.rules.Live&&m.ActualSprayHits==0;for(int i=0;i<8;i++)stable&=m.trucks[i].tractorMotor.Grounded&&Vector3.Dot(m.trucks[i].transform.up,Vector3.up)>.95f;Check(stable,"all eight settle upright with freezing disabled during countdown");
   requestedScale=Time.timeScale=4;float end=Time.realtimeSinceStartup+220,nextTrace=0;bool scoreCaptured=false,paused=false,captured=false,reversed=false;
   while(!m.Finished&&Time.realtimeSinceStartup<end)
   {
    if(!scoreCaptured&&m.ScoreJournal.Count>0){scoreCaptured=true;yield return SportsScoreAcceptance.CaptureAward(m,Check,Folder);var award=m.ScoreJournal.Latest;Check(award.IsTeam&&award.Slot==-1&&award.ParticipantId==null,"real score notification identifies the team without claiming a player award");}

    reversed|=m.rules.Freezers==1&&m.cannons[4].activeSelf&&!m.cannons[0].activeSelf;
    if(!paused&&m.rules.Clock>3){paused=true;intentionalPause=true;float clock=m.rules.Clock;var p=m.trucks[0].body.position;m.Pause();yield return new WaitForSecondsRealtime(.15f);Check(clock==m.rules.Clock&&p==m.trucks[0].body.position,"pause freezes timer, driving and freeze progression");m.Resume();intentionalPause=false;Time.timeScale=4;}
    if(!captured&&m.ActualFreezes>0){captured=true;yield return SportsWindowCapture.Save(1280,720,Folder+"/gameplay-720.png");yield return SportsWindowCapture.Save(1920,1080,Folder+"/gameplay-1080.png");}
    if(Time.realtimeSinceStartup>nextTrace){nextTrace=Time.realtimeSinceStartup+10;report.AppendLine("ACTUAL_FREEZE round="+m.rules.Round+" t="+m.rules.Clock+" sprayHits="+m.ActualSprayHits+" ice="+m.ActualFreezes+" rescues="+m.ActualRescues+" wins="+string.Join(",",m.rules.wins));File.WriteAllText(Folder+"/progress.txt",report.ToString());}yield return null;
   }
   requestedScale=Time.timeScale=1;scoreTally.Verify(m.rules.wins,Check);Check(scoreCaptured,"real team award captured at720p/1080p");Check(m.Finished&&m.rules.Complete&&m.rules.Round==3,"actual default four-round AI match reaches final team standings");Check(Array.TrueForAll(m.shatter,fx=>fx!=null&&Array.TrueForAll(fx.GetComponentsInChildren<ParticleSystem>(true),part=>part.main.stopAction==ParticleSystemStopAction.None)),"all pooled snow/shatter transforms survive actual rescues and remain replayable");Check(m.ActualSprayHits>3&&m.ActualFreezes>0,"AI genuinely aims and freezes through actual unoccluded physical casts");Check(reversed,"actual cannon ownership switches sides between rounds");Check(m.rules.wins[0]+m.rules.wins[1]==4,"each round awards exactly one team win, including timer survival");
   if(m.Finished){string result=m.Results;m.ExtraAction("watchfreeze");yield return new WaitForSecondsRealtime(.25f);Check(m.review.Playing,"recorded truck/ice highlight replay starts");yield return SportsWindowCapture.Save(1280,720,Folder+"/replay-720.png");m.ExtraAction("replayskip");Check(result==m.Results&&!m.review.Playing,"replay preserves team wins and final placing");}
   for(int i=0;i<4;i++)pads[i]=InputSystem.AddDevice<Gamepad>("Injected freeze-tag player "+i);yield return Rescue(m);
   for(int humans=1;humans<=4;humans++)
   {
    m.SetupScreen();m.teamA=m.teamB=4;m.NormalizeSetup();m.AllBots();for(int i=0;i<humans;i++){m.setup.players[i].source=BowlingSource.WholeController;m.setup.players[i].pad=pads[i];InputSystem.QueueStateEvent(pads[i],new GamepadState());}SportsCameraOptions.Set(false);yield return null;Check(m.StartMatch(),humans+" humans plus asymmetric AI start a clean rematch");m.drivingViews.Select(2);yield return new WaitForSecondsRealtime(.2f);bool owners=m.cameras.Length==humans;for(int i=0;i<humans;i++)owners&=m.drivingViews.rally.owners[i]==i;Check(owners,humans+" human-only cab viewports have distinct correct owners");InputSystem.QueueStateEvent(pads[0],new GamepadState{rightTrigger=.8f});yield return new WaitForSecondsRealtime(.12f);bool isolated=m.commands[0].y>.6f;for(int i=1;i<humans;i++)isolated&=m.commands[i]==Vector2.zero;Check(isolated,"assigned acceleration stays isolated to its participant");InputSystem.QueueStateEvent(pads[0],new GamepadState());SportsCameraOptions.Set(true);m.drivingViews.Layout();yield return null;Check(m.cameras.Length==4&&Array.FindAll(m.trucks,t=>t.gameObject.activeSelf).Length==8,"optional AI views preserve all eight active trucks");if(humans==4){yield return SportsWindowCapture.Save(1280,720,Folder+"/four-cabs-720.png");yield return SportsWindowCapture.Save(1920,1080,Folder+"/four-cabs-1080.png");}
   }
   m.SetupScreen();m.AllBots();SportsCameraOptions.Set(false);Check(m.StartMatch()&&m.ActualFreezes==0&&m.rules.wins[0]+m.rules.wins[1]==0&&Array.TrueForAll(m.shells,x=>!x.activeSelf),"AI-only rematch clears every ice shell, score and role state");m.drivingViews.Select(1);yield return null;Check(m.drivingViews.NeedsArena&&m.broadcast.view.enabled,"AI-only cameras OFF has a working broadcast");Check(m.ScoreJournal.Count==0,"final rematch has no stale team receipts");m.SetupScreen();Complete();
  }
  IEnumerator Rescue(FreezeMatch m)
  {
   m.SetupScreen();m.teamA=m.teamB=2;m.NormalizeSetup();for(int i=0;i<4;i++){m.setup.players[i].source=BowlingSource.WholeController;m.setup.players[i].pad=pads[i];InputSystem.QueueStateEvent(pads[i],new GamepadState());}Check(m.StartMatch(),"controlled real cannon and rescue fixture starts through normal setup");while(m.Countdown>0)yield return null;
   m.trucks[0].Prepare(new Vector3(0,.65f,-25),Quaternion.identity);m.trucks[1].Prepare(new Vector3(-65,.65f,-60),Quaternion.identity);m.trucks[2].Prepare(new Vector3(0,.65f,0),Quaternion.Euler(0,180,0));m.trucks[3].Prepare(new Vector3(0,.65f,25),Quaternion.Euler(0,180,0));Physics.SyncTransforms();yield return new WaitForSeconds(.5f);
   var wall=new GameObject("Explicit actual solid ice-spray blocker");wall.transform.position=new Vector3(0,3,-12);wall.AddComponent<BoxCollider>().size=new Vector3(15,6,1);Physics.SyncTransforms();m.Fire(0);yield return new WaitForSeconds(1);Check(m.rules.buildup[2]==0,"actual solid cover blocks the cannon; no through-wall freeze");Destroy(wall);yield return null;
   for(int i=0;i<3;i++){InputSystem.QueueStateEvent(pads[0],new GamepadState().WithButton(GamepadButton.South));yield return null;InputSystem.QueueStateEvent(pads[0],new GamepadState());yield return new WaitForSeconds(1);}
   report.AppendLine("PHYSICAL_ICE_FIXTURE origin="+m.muzzles[0].position+" victim="+m.trucks[2].shape.bounds+" buildup="+m.rules.buildup[2]+" castHits="+m.ActualSprayHits);
   Check(m.rules.frozen[2]&&m.shells[2].activeSelf&&m.trucks[2].body.isKinematic&&!m.trucks[2].Powered,"actual assigned fire input builds ice and freezes the visible physical truck");yield return new WaitForSeconds(5);Check(m.rules.frozen[2]&&m.shells[2].activeSelf,"frozen truck remains frozen with no automatic thaw");
   InputSystem.QueueStateEvent(pads[3],new GamepadState{rightTrigger=1});float limit=Time.realtimeSinceStartup+8;while(m.ActualRescues==0&&Time.realtimeSinceStartup<limit)yield return null;InputSystem.QueueStateEvent(pads[3],new GamepadState());Check(m.ActualRescues==1&&!m.rules.frozen[2]&&!m.shells[2].activeSelf&&m.trucks[2].Powered,"teammate physically drives into ice, shatters it once and restores actual NWH control");Check(m.rules.protectedUntil[2]>m.rules.Clock,"actual rescue grants a brief refreeze protection window");m.SetupScreen();
  }
  void Complete(){report.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; platform="+Application.platform+"; capture="+SportsWindowCapture.EvidenceKind+"; actual NWH/PhysX freezing, impact rescue and software input; no human/device/listening certification.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
   if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
   Application.Quit(errors==0?0:2);}
  void OnDestroy(){Application.logMessageReceived-=Error;foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;
#if UNITY_EDITOR
   InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
#endif
   preferences?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
 }
}
#endif
