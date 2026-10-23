#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckFrogger
{
 public sealed class FrogProbe:MonoBehaviour
 {
  const string Folder="Logs/TruckSports/EighteenSports/Frog";IDisposable preferences;readonly StringBuilder report=new StringBuilder();FrogMatch match;int checks,errors;float deadline,scale=1;bool intentionalPause;Gamepad[] pads;Keyboard stationaryKeyboard;InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
  InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
  [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsFrog")>=0;
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("Sports.Frog.Probe",false);UnityEditor.SessionState.SetBool("Sports.Frog.Probe",false);
#endif
   if(run){var go=new GameObject("Explicit five-crossing physical acceptance");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<FrogProbe>();}}
  void Awake(){preferences=SportsPrefs.IsolateValidation("Frog");SportsParticipantRegistry.ForgetCache();background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
   editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
  }
  void Check(bool yes,string message){checks++;if(!yes)errors++;report.AppendLine((yes?"PASS ":"FAIL ")+message);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
  void Error(string m,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+m+"\n"+stack);}}
  void Update(){if(deadline>0&&Time.realtimeSinceStartup>deadline){deadline=0;Check(false,"bounded Frogger acceptance deadline expired");match?.SetupScreen();StopAllCoroutines();Complete();return;}if(match!=null&&match.Live==false&&match.Running&&!match.Finished&&!intentionalPause&&match.Paused&&match.DevicesReady){match.Resume();Time.timeScale=scale;}}
  IEnumerator Start()
  {
   Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");Application.logMessageReceived+=Error;deadline=Time.realtimeSinceStartup+850;yield return SportsProbeScene.Ensure<FrogMatch>("TruckFrogger");yield return null;match=FindAnyObjectByType<FrogMatch>();var m=match;Check(m!=null&&m.world.courses.Length==5&&m.trucks.Length==8,"saved scene contains five independently selectable complete challenges and eight NWH drivers");if(m==null){Complete();yield break;}var clear=Array.Find(m.atmosphere.presets.entries,p=>p.legacyKind==SportsWeatherKind.Clear);m.atmosphere.presets.Select(clear.id);SportsCameraOptions.Set(false);
   int first=0,last=5;
#if UNITY_EDITOR
   if(UnityEditor.SessionState.GetBool("Sports.Frog.RailRiver",false)){first=1;last=3;UnityEditor.SessionState.SetBool("Sports.Frog.RailRiver",false);}
#endif
   for(int course=first;course<last;course++)
   {
    using var scoreTally=new SportsScoreAcceptance.Tally(m);m.SetupScreen();m.setup.count=1;m.option=course+1;m.AllBots();m.setup.players[0].difficulty=BowlingDifficulty.Hard;Check(m.StartMatch(),"independent challenge "+(course+1)+" starts through setup");scale=Time.timeScale=1;float until=Time.realtimeSinceStartup+210,next=0;bool captured=false,paused=false;
    while(m.rules.History.Count==0&&Time.realtimeSinceStartup<until){if(!paused&&m.rules.Clock>3){paused=true;intentionalPause=true;var age=m.rules.Clock;m.Pause();yield return new WaitForSecondsRealtime(.12f);Check(m.rules.Clock==age,"challenge "+(course+1)+" pause preserves authoritative attempt clock");m.Resume();intentionalPause=false;Time.timeScale=scale;}if(!captured&&m.rules.Phase==FrogPhase.Driving&&m.rules.Clock>5){captured=true;yield return SportsWindowCapture.Save(1280,720,Folder+"/challenge-"+(course+1)+"-720.png");yield return SportsWindowCapture.Save(1920,1080,Folder+"/challenge-"+(course+1)+"-1080.png");}if(Time.realtimeSinceStartup>next){next=Time.realtimeSinceStartup+5;report.AppendLine("ACTUAL_CROSSING course="+course+" phase="+m.rules.Phase+" time="+m.rules.Clock+" position="+m.trucks[0].body.position+" speed="+m.trucks[0].body.linearVelocity.magnitude+" gates="+m.CompletedCrossings+" waits="+m.WaitDecisions+" "+m.Diagnostics());File.WriteAllText(Folder+"/progress.txt",report.ToString());}yield return null;}
    scale=Time.timeScale=1;bool recorded=m.rules.History.Count>0;Check(recorded&&m.rules.History[0].success,"real AI driver physically completes "+m.world.courses[course].title);if(recorded){report.AppendLine("ACTUAL_RESULT "+m.rules.History[0].reason+" seconds="+m.rules.History[0].seconds+" position="+m.trucks[0].body.position+" "+m.Diagnostics());if(!m.rules.History[0].success)yield return SportsWindowCapture.Save(1280,720,Folder+"/failed-challenge-"+(course+1)+"-720.png");}Check(recorded&&m.CompletedCrossings==m.world.courses[course].gates.Length,"every ordered whole-truck crossing is required in challenge "+(course+1));until=Time.realtimeSinceStartup+8;while(!m.Finished&&Time.realtimeSinceStartup<until)yield return null;Check(m.Finished&&m.rules.points[0]==1,"successful independent challenge awards exactly one win point");scoreTally.Verify(slot=>m.rules.points[slot],Check);if(course==0&&m.ScoreJournal.Count>0)yield return SportsScoreAcceptance.CaptureAward(m,Check,Folder);if(m.Finished&&course==0){yield return SportsResultsAcceptance.Capture(m,Check,Folder,m.rules.Place,slot=>(double?)m.rules.points[slot],slot=>m.rules.Winner==slot);string result=m.Results;m.ExtraAction("watchfrog");yield return new WaitForSecondsRealtime(.2f);Check(m.review.Playing,"actual crossing highlight replays");yield return SportsWindowCapture.Save(1280,720,Folder+"/replay-720.png");m.ExtraAction("replayskip");Check(m.Results==result,"replay cannot alter authoritative times or points");}m.SetupScreen();
   }
   m.SetupScreen();m.setup.count=1;m.option=2;m.AllBots();stationaryKeyboard=InputSystem.AddDevice<Keyboard>("Neutral stationary train-contact driver");InputSystem.QueueStateEvent(stationaryKeyboard,new KeyboardState());m.setup.players[0].source=BowlingSource.Keyboard;Check(m.StartMatch(),"genuine train-impact fixture starts");scale=Time.timeScale=1;while(m.rules.Phase==FrogPhase.Countdown)yield return null;var rail=m.world.courses[1];var train=rail.movers[0];float wait=Time.realtimeSinceStartup+15;while(Mathf.Abs(train.body.position.x-rail.start.x)<90&&Time.realtimeSinceStartup<wait)yield return null;var victim=m.trucks[0];victim.Prepare(new Vector3(rail.start.x,.3f,rail.gates[0].z),Quaternion.identity);Physics.SyncTransforms();wait=Time.realtimeSinceStartup+25;while(m.rules.History.Count==0&&Time.realtimeSinceStartup<wait)yield return null;report.AppendLine("TRAIN_FIXTURE impacts="+m.ActualImpacts+" position="+victim.body.position+" history="+m.rules.History.Count+" reason="+(m.rules.History.Count>0?m.rules.History[0].reason:"NONE"));Check(m.ActualImpacts==1&&m.rules.History.Count==1&&!m.rules.History[0].success,"actual moving freight train solid contact consumes exactly one life");wait=Time.realtimeSinceStartup+5;while(!m.Finished&&Time.realtimeSinceStartup<wait)yield return null;Check(m.rules.points[0]==0,"genuine collision failure never awards a challenge win");m.SetupScreen();
   m.option=3;m.AllBots();Check(m.StartMatch(),"physical drowning fixture starts");scale=Time.timeScale=1;while(m.rules.Phase==FrogPhase.Countdown)yield return null;victim.Prepare(new Vector3(m.world.courses[2].start.x,-3,85),Quaternion.identity);Physics.SyncTransforms();wait=Time.realtimeSinceStartup+4;while(m.rules.History.Count==0&&Time.realtimeSinceStartup<wait)yield return null;Check(m.rules.History.Count==1&&!m.rules.History[0].success&&m.rules.History[0].reason.Contains("SUNK"),"actual truck below river surface loses its one life without an invented finish");m.SetupScreen();
   m.option=1;m.AllBots();Check(m.StartMatch(),"out-of-course bypass fixture starts");while(m.rules.Phase==FrogPhase.Countdown)yield return null;victim.Prepare(new Vector3(40,.3f,300),Quaternion.identity);Physics.SyncTransforms();wait=Time.realtimeSinceStartup+4;while(m.rules.History.Count==0&&Time.realtimeSinceStartup<wait)yield return null;Check(m.rules.History.Count==1&&!m.rules.History[0].success&&m.rules.points[0]==0,"leaving the crossing corridor cannot bypass obstacles for a win");m.SetupScreen();scale=Time.timeScale=1;
   pads=new Gamepad[4];for(int i=0;i<4;i++)pads[i]=InputSystem.AddDevice<Gamepad>("Frog alternating ownership "+i);
   for(int humans=1;humans<=4;humans++){m.setup.count=4;m.option=1;m.AllBots();for(int i=0;i<humans;i++){m.setup.players[i].source=BowlingSource.WholeController;m.setup.players[i].pad=pads[i];InputSystem.QueueStateEvent(pads[i],new GamepadState());}Check(m.StartMatch(),humans+" human alternating match starts");m.drivingViews.Select(2);yield return new WaitForSeconds(.2f);Check(m.cameras.Length==1&&m.cameras[0].rect==new Rect(0,0,1,1)&&m.drivingViews.rally.owners[0]==m.rules.Active,"all spectators share the active driver full-screen camera");InputSystem.QueueStateEvent(pads[0],new GamepadState{rightTrigger=.8f});yield return new WaitForSeconds(.12f);bool isolated=m.commands[0].y>.5f;for(int i=1;i<humans;i++)isolated&=m.commands[i]==Vector2.zero;Check(isolated,"alternating human input retains correct source ownership");InputSystem.QueueStateEvent(pads[0],new GamepadState());if(humans==4)yield return SportsWindowCapture.Save(1280,720,Folder+"/cab-720.png");m.SetupScreen();}
   m.setup.count=1;m.option=3;m.AllBots();Check(m.StartMatch()&&m.rules.History.Count==0&&m.rules.points[0]==0,"rematch resets one life, history, seeded machinery and scores");m.SetupScreen();Complete();
  }
  void Complete(){report.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; platform="+Application.platform+"; capture="+SportsWindowCapture.EvidenceKind+"; actual NWH/UTS driving and physical obstacle motion. No hardware, listening or human-playtest certification.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
   if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
   Application.Quit(errors==0?0:2);}
  void OnDestroy(){Application.logMessageReceived-=Error;if(stationaryKeyboard!=null&&stationaryKeyboard.added)InputSystem.RemoveDevice(stationaryKeyboard);if(pads!=null)foreach(var p in pads)if(p!=null&&p.added)InputSystem.RemoveDevice(p);InputSystem.settings.backgroundBehavior=background;
#if UNITY_EDITOR
   InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
#endif
   preferences?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
 }
}
#endif
