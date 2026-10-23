#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;using System.Collections;using System.IO;using System.Text;
using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSports;using LWS.TruckBowling;
namespace LWS.TruckJousting
{
 public sealed class JoustProbe:MonoBehaviour
 {
  const string Folder="Logs/TruckSports/EighteenSports/Joust";IDisposable preferences;int checks,errors;readonly StringBuilder report=new StringBuilder();Gamepad[] pads=new Gamepad[2];InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
  InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
  [RuntimeInitializeOnLoadMethod]static void Boot()
  {
   bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsJoust")>=0;
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("Sports.Joust.Probe",false);UnityEditor.SessionState.SetBool("Sports.Joust.Probe",false);
#endif
   if(!run)return;var go=new GameObject("Explicit actual lance tournament acceptance");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<JoustProbe>();
  }
  void Awake(){preferences=SportsPrefs.IsolateValidation("Joust");SportsParticipantRegistry.ForgetCache();priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
   priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
  }
  void Check(bool ok,string message){checks++;if(!ok)errors++;report.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
  void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message+"\n"+stack);}}
  IEnumerator Start()
  {
   Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");Application.logMessageReceived+=Error;
   yield return SportsProbeScene.Ensure<JoustMatch>("TruckJousting");yield return null;var m=FindAnyObjectByType<JoustMatch>();
   Check(m!=null&&m.trucks.Length==2&&m.shields.Length==2&&m.lances.Length==2,"saved tournament scene has two NWH tractors, physical compound tips and shield zones");if(m==null){Complete();yield break;}
   using var scoreTally=new SportsScoreAcceptance.Tally(m);SportsCameraOptions.Set(false);m.drivingViews.Select(0);m.AllBots();m.option=0;m.setup.players[0].difficulty=BowlingDifficulty.Hard;m.setup.players[1].difficulty=BowlingDifficulty.Normal;Check(m.StartMatch(),"actual AI three-pass tournament starts through normal setup");
   var start=Array.ConvertAll(m.trucks,t=>t.body.position);yield return new WaitForSeconds(1.5f);bool stable=!m.rules.Live&&m.ActualStrikes==0;
   for(int i=0;i<2;i++)stable&=m.trucks[i].tractorMotor.Grounded&&Vector3.Dot(m.trucks[i].transform.up,Vector3.up)>.95f&&Vector3.ProjectOnPlane(m.trucks[i].body.position-start[i],Vector3.up).magnitude<.3f;Check(stable,"trucks settle upright and stationary with real lances held through countdown");
   Time.timeScale=4;float deadline=Time.realtimeSinceStartup+150,nextTrace=0;bool paused=false,captured=false,broken=false,scoreCaptured=false;
   while(!m.Finished&&Time.realtimeSinceStartup<deadline)
   {
    if(!scoreCaptured&&m.ScoreJournal.Count>0){scoreCaptured=true;yield return SportsScoreAcceptance.CaptureAward(m,Check,Folder);}
    for(int i=0;i<2;i++)if(m.lances[i].Broken)foreach(var piece in m.lances[i].splinters)broken|=piece.gameObject.activeInHierarchy&&!piece.isKinematic;
    if(!paused&&m.rules.Clock>2){paused=true;float clock=m.rules.Clock;var p=m.trucks[0].body.position;m.Pause();yield return new WaitForSecondsRealtime(.15f);Check(clock==m.rules.Clock&&p==m.trucks[0].body.position,"pause freezes tournament clock and actual trucks");m.Resume();Time.timeScale=4;}
    if(!captured&&m.ActualStrikes>0){captured=true;yield return SportsWindowCapture.Save(1280,720,Folder+"/gameplay-720.png");yield return SportsWindowCapture.Save(1920,1080,Folder+"/gameplay-1080.png");}
    float now=m.rules.Pass*1000+m.rules.Clock;if(now>=nextTrace){nextTrace=now+10;report.AppendLine("ACTUAL_JOUST pass="+m.rules.Pass+" t="+m.rules.Clock+" strikes="+m.ActualStrikes+" points="+string.Join(",",m.rules.points)+" positions="+m.trucks[0].body.position+" / "+m.trucks[1].body.position);File.WriteAllText(Folder+"/progress.txt",report.ToString());}yield return null;
   }
   Time.timeScale=1;scoreTally.Verify(slot=>m.rules.points[slot],Check);Check(scoreCaptured,"actual lance award captured at720p/1080p");Check(m.Finished&&m.rules.Complete&&m.rules.Pass==3,"actual AI driving and lance aiming complete all three tournament passes");Check(m.ActualStrikes>=2&&m.ActualStrikes==m.rules.hits[0]+m.rules.hits[1],"real physical lance-tip contacts earn all hit points exactly once");Check(broken&&captured,"Core Collapse lances release actual pooled physical oak splinters and readable score feedback");report.AppendLine("ACTUAL_RESULTS\n"+m.Standings);
   if(m.Finished){string result=m.Results;m.ExtraAction("watchjoust");yield return new WaitForSecondsRealtime(.3f);Check(m.review.Playing,"actual truck/lance/splinter replay starts");yield return SportsWindowCapture.Save(1280,720,Folder+"/replay-720.png");m.ExtraAction("replayskip");Check(result==m.Results&&!m.review.Playing,"replay preserves tournament points and shared/winning placement");}
   for(int i=0;i<2;i++)pads[i]=InputSystem.AddDevice<Gamepad>("Injected joust owner "+i);
   for(int humans=1;humans<=2;humans++)
   {
    m.SetupScreen();m.AllBots();for(int i=0;i<humans;i++){m.setup.players[i].source=BowlingSource.WholeController;m.setup.players[i].pad=pads[i];InputSystem.QueueStateEvent(pads[i],new GamepadState());}SportsCameraOptions.Set(false);yield return null;Check(m.StartMatch(),humans+" human-controller rematch resets all scores, lances and splinters");m.drivingViews.Select(2);yield return new WaitForSecondsRealtime(.2f);
    bool owned=m.cameras.Length==humans&&m.rules.points[0]==0&&m.rules.points[1]==0&&!m.lances[0].Broken;for(int i=0;i<humans;i++)owned&=m.drivingViews.rally.owners[i]==i;Check(owned,humans+" human-only cab cameras have correct ownership and clean pass state");
    float limit=Time.realtimeSinceStartup+7;while(m.Countdown>0&&Time.realtimeSinceStartup<limit)yield return null;
    var initial=m.mounts[0].localPosition;var angle=m.mounts[0].localRotation;
    InputSystem.QueueStateEvent(pads[0],new GamepadState().WithButton(GamepadButton.South));yield return null;InputSystem.QueueStateEvent(pads[0],new GamepadState());yield return null;
    InputSystem.QueueStateEvent(pads[0],new GamepadState{leftStick=new Vector2(.75f,0),rightTrigger=.75f});yield return new WaitForSecondsRealtime(.45f);
    Check(m.mounts[0].localPosition.y>initial.y+.1f&&Quaternion.Angle(angle,m.mounts[0].localRotation)>.3f&&(humans==1||m.commands[1]==Vector2.zero),"real assigned controller adjusts physical lance height/angle without changing the other player's input");
    InputSystem.QueueStateEvent(pads[0],new GamepadState());yield return null;InputSystem.QueueStateEvent(pads[0],new GamepadState().WithButton(GamepadButton.South));yield return null;InputSystem.QueueStateEvent(pads[0],new GamepadState());yield return null;
    var position=m.trucks[0].body.position;InputSystem.QueueStateEvent(pads[0],new GamepadState{rightTrigger=1});yield return new WaitForSecondsRealtime(1.5f);report.AppendLine("AIM_DRIVE_DIAG humans="+humans+" moved="+Vector3.Distance(m.trucks[0].body.position,position)+" command="+m.commands[0]+" applied="+m.trucks[0].command+" caption="+m.ControlCaption(0)+" paused="+m.Paused+" speed="+m.trucks[0].body.linearVelocity+" throttle="+m.trucks[0].tractorMotor.vehicle.input.states.throttle+" handbrake="+m.trucks[0].tractorMotor.vehicle.input.states.handbrake);Check(Vector3.Distance(m.trucks[0].body.position,position)>1,"leaving aim mode restores responsive actual NWH acceleration");InputSystem.QueueStateEvent(pads[0],new GamepadState());
    if(humans==2)yield return SportsWindowCapture.Save(1280,720,Folder+"/two-cabs-720.png");SportsCameraOptions.Set(true);m.drivingViews.Layout();yield return null;Check(m.cameras.Length==2&&m.trucks[0].gameObject.activeSelf&&m.trucks[1].gameObject.activeSelf,"AI camera preference changes only presentation, leaving both competitors active");
   }
   m.SetupScreen();m.AllBots();SportsCameraOptions.Set(false);Check(m.StartMatch(),"AI-only spectator rematch starts");m.drivingViews.Select(1);yield return null;Check(m.drivingViews.NeedsArena&&m.broadcast.view.enabled,"AI-only cameras OFF retains a functional broadcast");m.SetupScreen();Complete();
  }
  void Complete(){report.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; platform="+Application.platform+"; capture="+SportsWindowCapture.EvidenceKind+"; actual NWH/PhysX lance contacts and software input; no human/device/listening certification.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
   if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
   Application.Quit(errors==0?0:2);}
  void OnDestroy(){Application.logMessageReceived-=Error;foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
   InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
   preferences?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
 }
}
#endif
