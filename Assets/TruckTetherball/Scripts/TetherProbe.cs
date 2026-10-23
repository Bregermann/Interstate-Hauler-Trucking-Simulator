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
namespace LWS.TruckTetherball
{
    public sealed class TetherProbe : MonoBehaviour
    {
        const string Folder="Logs/TruckSports/EighteenSports/Tether";IDisposable preferences;int checks,errors;readonly StringBuilder report=new StringBuilder();Gamepad[] pads=new Gamepad[2];InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        [RuntimeInitializeOnLoadMethod]static void Boot()
        {
            bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsTether")>=0;
#if UNITY_EDITOR
            run|=UnityEditor.SessionState.GetBool("Sports.Tether.Probe",false);UnityEditor.SessionState.SetBool("Sports.Tether.Probe",false);
#endif
            if(!run)return;var go=new GameObject("Explicit actual tetherball acceptance");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<TetherProbe>();
        }
        void Awake()
        {
            preferences=SportsPrefs.IsolateValidation("Tether");SportsParticipantRegistry.ForgetCache();priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }
        void Check(bool ok,string message){checks++;if(!ok)errors++;report.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
        void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message+"\n"+stack);}}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");Application.logMessageReceived+=Error;yield return SportsProbeScene.Ensure<TetherMatch>("TruckTetherball");yield return null;var m=FindAnyObjectByType<TetherMatch>();
            Check(m!=null&&m.trucks.Length==2&&m.cable.segments.Length==64&&m.ball.transform.localScale==Vector3.one,"saved stadium has two NWH tractors, unit-scale physical ball and64pooled render-only rope segments");if(m==null){Complete();yield break;}
            SportsCameraOptions.Set(false);m.drivingViews.Select(0);m.option=0;m.AllBots();m.setup.players[0].difficulty=BowlingDifficulty.Hard;m.setup.players[1].difficulty=BowlingDifficulty.Easy;Check(m.StartMatch(),"actual mixed-difficulty AI best-of-three match starts");var starts=Array.ConvertAll(m.trucks,t=>t.body.position);var ballStart=m.ball.position;yield return new WaitForSeconds(1.5f);
            bool settled=m.ball.isKinematic&&Vector3.Distance(ballStart,m.ball.position)<.01f&&!m.rules.Live;for(int i=0;i<2;i++)settled&=m.trucks[i].tractorMotor.Grounded&&Vector3.Dot(m.trucks[i].transform.up,Vector3.up)>.95f&&Vector3.ProjectOnPlane(m.trucks[i].body.position-starts[i],Vector3.up).magnitude<.3f;Check(settled,"real trucks settle upright and ball/rope/scoring remain held during countdown");
            Time.timeScale=4;float deadline=Time.realtimeSinceStartup+360,nextTrace=0,peak=0,maxStretch=0;bool scoreCaptured=false,paused=false,captured=false;
            while(!m.Finished&&Time.realtimeSinceStartup<deadline)
            {
                if(!scoreCaptured&&m.ScoreJournal.Count>0){scoreCaptured=true;yield return SportsScoreAcceptance.CaptureAward(m,Check,Folder);if(SportsScoreAcceptance.CaptureOnly){SportsScoreAcceptance.Journal(m,m.rules.wins[0]+m.rules.wins[1],Check);Check(true,"focused first-award capture; full-match evidence is recorded separately");m.SetupScreen();Check(m.StartMatch()&&m.ScoreJournal.Count==0,"focused scoring rematch starts with an empty journal");yield return null;m.SetupScreen();Complete();yield break;}}

                peak=Mathf.Max(peak,Mathf.Abs(m.rules.NetDegrees));maxStretch=Mathf.Max(maxStretch,Vector3.Distance(m.ball.position,m.cable.poleTop.position)-m.cable.FreeLength);
                if(!paused&&m.ActualBallContacts>0){paused=true;var position=m.ball.position;float clock=m.rules.Clock;m.Pause();yield return new WaitForSecondsRealtime(.15f);Check(position==m.ball.position&&clock==m.rules.Clock,"pause freezes actual ball, winding and match clock");m.Resume();Time.timeScale=4;}
                if(!captured&&m.ActualBallContacts>1&&peak>30){captured=true;yield return SportsWindowCapture.Save(1280,720,Folder+"/gameplay-720.png");yield return SportsWindowCapture.Save(1920,1080,Folder+"/gameplay-1080.png");}
                float now=m.rules.Round*10000+m.rules.Clock;if(now>=nextTrace){nextTrace=now+15;report.AppendLine("ACTUAL_TETHER round="+m.rules.Round+" t="+m.rules.Clock.ToString("F1")+" net="+m.rules.NetDegrees.ToString("F1")+" contacts="+m.ActualBallContacts+" ball="+m.ball.position+" rope="+m.cable.FreeLength.ToString("F2"));File.WriteAllText(Folder+"/progress.txt",report.ToString());}yield return null;
            }
            Time.timeScale=1;SportsScoreAcceptance.Journal(m,m.rules.wins[0]+m.rules.wins[1],Check);Check(scoreCaptured,"actual award animation captured at720p/1080p");Check(m.Finished&&m.rules.Complete&&m.rules.Winner>=0,"actual AI strikes complete the full best-of-three match without forced wraps or invented timeout points");Check(m.ActualBallContacts>=2&&peak>=m.requiredTurns*360-.1f,"actual ball trajectory completes the required winding after genuine physical truck contacts");Check(maxStretch<2,"real constrained rope remains bounded through impacts, wraps and reversals; maximum extension="+maxStretch.ToString("F3"));report.AppendLine("ACTUAL_RESULTS\n"+m.Standings);
            if(m.Finished){string result=m.Results;m.ExtraAction("watchtether");yield return new WaitForSecondsRealtime(.3f);Check(m.review.Playing,"actual truck/ball/individual-cable-segment replay starts");yield return SportsWindowCapture.Save(1280,720,Folder+"/replay-720.png");m.ExtraAction("replayskip");Check(result==m.Results&&!m.review.Playing,"replay cannot change round wins or results");}
            for(int i=0;i<2;i++)pads[i]=InputSystem.AddDevice<Gamepad>("Injected tether owner "+i);
            for(int humans=1;humans<=2;humans++)
            {
                m.SetupScreen();m.AllBots();for(int i=0;i<humans;i++){m.setup.players[i].source=BowlingSource.WholeController;m.setup.players[i].pad=pads[i];InputSystem.QueueStateEvent(pads[i],new GamepadState());}
                SportsCameraOptions.Set(false);yield return null;Check(m.StartMatch(),humans+" software humans start a clean rematch with all wraps/wins reset");m.drivingViews.Select(2);yield return new WaitForSecondsRealtime(.2f);
                bool owned=m.cameras.Length==humans&&m.rules.NetDegrees==0&&m.rules.wins[0]==0&&m.rules.wins[1]==0;for(int i=0;i<humans;i++)owned&=m.drivingViews.rally.owners[i]==i&&m.cameras[i].rect.xMin>=0&&m.cameras[i].rect.xMax<=1;Check(owned,humans+" human-only cab views and reset rope state have correct owners");
                InputSystem.QueueStateEvent(pads[0],new GamepadState{rightTrigger=.75f});yield return new WaitForSecondsRealtime(.15f);Check(m.commands[0].y>.5f&&(humans==1||m.commands[1]==Vector2.zero),"actual software accelerator ownership stays isolated");InputSystem.QueueStateEvent(pads[0],new GamepadState());
                if(humans==2)yield return SportsWindowCapture.Save(1280,720,Folder+"/two-cabs-720.png");SportsCameraOptions.Set(true);m.drivingViews.Layout();yield return null;Check(m.cameras.Length==2&&m.trucks[0].gameObject.activeSelf&&m.trucks[1].gameObject.activeSelf,"optional AI view adds only a camera, never removes a competitor");
            }
            m.SetupScreen();m.AllBots();SportsCameraOptions.Set(false);Check(m.StartMatch(),"AI-only spectator rematch starts");m.drivingViews.Select(1);yield return null;Check(m.drivingViews.NeedsArena&&m.broadcast.view.enabled,"AI-only with AI cameras OFF shows actual broadcast instead of black screen");m.SetupScreen();Complete();
        }
        void Complete(){report.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; platform="+Application.platform+"; capture="+SportsWindowCapture.EvidenceKind+"; actual NWH/PhysX ball/rope and software input; no human/device/listening certification.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
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
