#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;using System.Collections;using System.IO;using System.Text;using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSports;using LWS.TruckBowling;
namespace LWS.TruckVolleyball
{
 public sealed class VolleyProbe:MonoBehaviour
 {
  const string Folder="Logs/TruckSports/EighteenSports/Volley";IDisposable preferences;int checks,errors;readonly StringBuilder report=new StringBuilder();Gamepad[] pads=new Gamepad[4];InputSettings.BackgroundBehavior background;VolleyMatch watched;bool intentionalPause;float requestedScale=1,watchdog;
  void Update(){if(watchdog>0&&Time.realtimeSinceStartup>watchdog){watchdog=0;Check(false,"bounded acceptance watchdog expired");watched?.SetupScreen();StopAllCoroutines();Complete();return;}if(watched!=null&&watched.Running&&!watched.Finished&&!intentionalPause&&watched.Paused&&watched.DevicesReady){report.AppendLine("HARNESS_ACKNOWLEDGED_EXTERNAL_PAUSE focused="+Application.isFocused);watched.Resume();Time.timeScale=requestedScale;}}
#if UNITY_EDITOR
  InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
  [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsVolley")>=0;
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("Sports.Volley.Probe",false);UnityEditor.SessionState.SetBool("Sports.Volley.Probe",false);
#endif
   if(!run)return;var go=new GameObject("Explicit physical beach volleyball acceptance");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<VolleyProbe>();}
  void Awake(){preferences=SportsPrefs.IsolateValidation("Volley");SportsParticipantRegistry.ForgetCache();background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
   editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
  }
  void Check(bool ok,string message){checks++;if(!ok)errors++;report.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
  void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message+"\n"+stack);}}
  IEnumerator Start()
  {
   watchdog=Time.realtimeSinceStartup+420;Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");Application.logMessageReceived+=Error;yield return SportsProbeScene.Ensure<VolleyMatch>("TruckVolleyball");yield return null;var m=FindAnyObjectByType<VolleyMatch>();watched=m;Check(m!=null&&m.trucks.Length==8&&m.ball!=null&&m.net!=null&&m.crowd.Spectators>100,"saved beach has full NWH trucks, striped inflatable ball, physical net and responsive spectators");if(m==null){Complete();yield break;}
   var clear=Array.Find(m.atmosphere.presets.entries,p=>p.legacyKind==SportsWeatherKind.Clear);if(clear!=null)m.atmosphere.presets.Select(clear.id);m.teamA=m.teamB=2;m.NormalizeSetup();m.AllBots();m.option=1;SportsCameraOptions.Set(false);m.drivingViews.Select(0);Check(m.StartMatch(),"normal two-versus-two first-to-fifteen AI match starts");var initial=Array.ConvertAll(m.trucks,t=>t.body.position);yield return new WaitForSeconds(1.5f);bool grounded=!m.rules.Live&&m.ActualContacts==0;for(int i=0;i<4;i++)grounded&=m.trucks[i].tractorMotor.Grounded&&Vector3.ProjectOnPlane(m.trucks[i].body.position-initial[i],Vector3.up).magnitude<.35f;Check(grounded,"service countdown holds all four actual trucks settled and ball stationary");
   requestedScale=Time.timeScale=4;float deadline=Time.realtimeSinceStartup+240,nextTrace=0;bool paused=false,captured=false;
   while(!m.Finished&&Time.realtimeSinceStartup<deadline)
   {
    if(!paused&&m.rules.Rally>1){paused=true;intentionalPause=true;var p=m.ball.position;int score=m.rules.points[0]+m.rules.points[1];m.Pause();yield return new WaitForSecondsRealtime(.15f);Check(p==m.ball.position&&score==m.rules.points[0]+m.rules.points[1],"pause freezes ball, rally and score");m.Resume();intentionalPause=false;Time.timeScale=4;}
    if(!captured&&m.ActualContacts>0){captured=true;yield return SportsWindowCapture.Save(1280,720,Folder+"/gameplay-720.png");yield return SportsWindowCapture.Save(1920,1080,Folder+"/gameplay-1080.png");}
    if(Time.realtimeSinceStartup>nextTrace){nextTrace=Time.realtimeSinceStartup+10;report.AppendLine("ACTUAL_VOLLEY rally="+m.rules.Rally+" score="+string.Join(",",m.rules.points)+" hits="+m.ActualContacts+" jumps="+m.ActualJumps+" ball="+m.ball.position+" velocity="+m.ball.linearVelocity+" call="+m.rules.Call);File.WriteAllText(Folder+"/progress.txt",report.ToString());}yield return null;
   }
   requestedScale=Time.timeScale=1;Check(m.Finished&&m.rules.Complete&&m.rules.Winner>=0,"actual default first-to-fifteen AI volleyball match completes under win-by-two/cap rules");Check(m.ActualContacts>3&&m.ActualJumps>0,"AI physically returns and jumps for the actual inflatable ball");Check(m.rules.Winner>=0&&(m.rules.points[m.rules.Winner]>=15&&m.rules.points[m.rules.Winner]>=m.rules.points[1-m.rules.Winner]+2||m.rules.points[0]>=21||m.rules.points[1]>=21),"final winner follows actual scoreboard rules");Check(m.crowd.Reactions>0,"actual point events trigger audience reactions");
   if(m.Finished){string result=m.Results;m.ExtraAction("watchvolley");yield return new WaitForSecondsRealtime(.3f);Check(m.review.Playing,"recorded physical rally replay starts");yield return SportsWindowCapture.Save(1280,720,Folder+"/replay-720.png");m.ExtraAction("replayskip");Check(result==m.Results&&!m.review.Playing,"replay cannot alter points or winner");}
   for(int i=0;i<4;i++)pads[i]=InputSystem.AddDevice<Gamepad>("Injected volleyball player "+i);
   m.SetupScreen();m.teamA=m.teamB=1;m.NormalizeSetup();for(int i=0;i<2;i++){m.setup.players[i].source=BowlingSource.WholeController;m.setup.players[i].pad=pads[i];}Check(m.StartMatch(),"one-versus-one physical fixture resets ball and scoreboard");while(m.Countdown>0)yield return null;SetBall(m,new Vector3(0,4.5f,-9),new Vector3(0,0,10));int net=m.NetContacts;yield return new WaitForSeconds(1);Check(m.NetContacts>net&&m.ball.position.z<0,"actual ball collides with the woven net rather than passing through");
   m.SetupScreen();Check(m.StartMatch(),"independent physical floor fixture restores a live service");while(m.Countdown>0)yield return null;int points=m.rules.points[0]+m.rules.points[1];SetBall(m,new Vector3(20,3.1f,25),Vector3.down);yield return new WaitForSeconds(.7f);Check(m.rules.points[0]+m.rules.points[1]==points+1,"actual court landing awards exactly one rally point");yield return new WaitForSeconds(.7f);Check(m.rules.points[0]+m.rules.points[1]==points+1,"later resting floor contacts cannot rescore that rally");
   for(int humans=1;humans<=4;humans++)
   {
    m.SetupScreen();m.teamA=m.teamB=humans>=3?4:2;m.NormalizeSetup();m.AllBots();for(int i=0;i<humans;i++){m.setup.players[i].source=BowlingSource.WholeController;m.setup.players[i].pad=pads[i];InputSystem.QueueStateEvent(pads[i],new GamepadState());}SportsCameraOptions.Set(false);yield return null;Check(m.StartMatch(),humans+" human rematch with correct two/four-truck team size starts");m.drivingViews.Select(2);yield return new WaitForSecondsRealtime(.2f);bool owners=m.cameras.Length==humans;for(int v=0;v<humans;v++){owners&=m.drivingViews.rally.owners[v]==v;for(int other=0;other<v;other++)owners&=!m.cameras[v].rect.Overlaps(m.cameras[other].rect);}Check(owners,humans+" human-only cab cameras preserve independent participant ownership");InputSystem.QueueStateEvent(pads[0],new GamepadState{rightTrigger=.8f});yield return new WaitForSecondsRealtime(.12f);bool isolated=m.commands[0].y>.6f;for(int i=1;i<humans;i++)isolated&=m.commands[i]==Vector2.zero;Check(isolated,"actual assigned accelerator input stays with its player");InputSystem.QueueStateEvent(pads[0],new GamepadState());SportsCameraOptions.Set(true);m.drivingViews.Layout();yield return null;Check(m.cameras.Length==4&&Array.FindAll(m.trucks,t=>t.gameObject.activeSelf).Length==m.setup.count,"AI cameras only alter the four supported views and leave all teammates active");if(humans==4){yield return SportsWindowCapture.Save(1280,720,Folder+"/four-cabs-720.png");yield return SportsWindowCapture.Save(1920,1080,Folder+"/four-cabs-1080.png");}
   }
   m.SetupScreen();m.teamA=m.teamB=2;m.NormalizeSetup();m.AllBots();SportsCameraOptions.Set(false);Check(m.StartMatch()&&m.rules.points[0]+m.rules.points[1]==0&&m.ActualContacts==0,"AI-only rematch cleanly restores default teams and score");m.drivingViews.Select(1);yield return null;Check(m.drivingViews.NeedsArena&&m.broadcast.view.enabled,"AI-only cameras OFF has a live spectator broadcast");m.SetupScreen();Complete();
  }
  static void SetBall(VolleyMatch m,Vector3 p,Vector3 v){m.ball.interpolation=RigidbodyInterpolation.None;m.ball.position=p;m.ball.transform.position=p;m.ball.linearVelocity=v;m.ball.angularVelocity=Vector3.zero;m.ball.interpolation=RigidbodyInterpolation.Interpolate;Physics.SyncTransforms();}
  void Complete(){report.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; platform="+Application.platform+"; capture="+SportsWindowCapture.EvidenceKind+"; actual NWH/PhysX rallies and software input; no human/device/listening certification.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
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
