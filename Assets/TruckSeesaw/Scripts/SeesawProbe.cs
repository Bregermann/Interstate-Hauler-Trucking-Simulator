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
namespace LWS.TruckSeesaw
{
    public sealed class SeesawProbe : MonoBehaviour
    {
        const string Folder="Logs/TruckSports/EighteenSports/Seesaw";
        IDisposable preferences;int checks,errors;readonly StringBuilder report=new StringBuilder();Gamepad fixturePad;InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        [RuntimeInitializeOnLoadMethod]static void Boot()
        {
            bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsSeesaw")>=0;
#if UNITY_EDITOR
            run|=UnityEditor.SessionState.GetBool("Sports.Seesaw.Probe",false);UnityEditor.SessionState.SetBool("Sports.Seesaw.Probe",false);
#endif
            if(!run)return;var root=new GameObject("Explicit physical quarry wobble acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<SeesawProbe>();
        }
        void Awake()
        {
            preferences=SportsPrefs.IsolateValidation("Seesaw");SportsParticipantRegistry.ForgetCache();priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }
        void Check(bool ok,string message){checks++;if(!ok)errors++;report.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
        void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message+"\n"+stack);}}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");Application.logMessageReceived+=Error;
            yield return SportsProbeScene.Ensure<SeesawMatch>("TruckSeesaw");yield return null;var m=FindAnyObjectByType<SeesawMatch>();Check(m!=null&&m.trucks.Length==4&&m.pivot.angularYMotion==ConfigurableJointMotion.Locked&&m.pivot.angularXMotion==ConfigurableJointMotion.Limited&&m.pivot.angularZMotion==ConfigurableJointMotion.Limited,"saved quarry scene has four full NWH tractors and a real constrained two-axis pivot");
            if(m==null){Complete();yield break;}
            SportsCameraOptions.Set(false);m.drivingViews.Select(0);m.setup.count=4;m.option=1;m.AllBots();for(int i=0;i<4;i++)m.setup.players[i].difficulty=i==0?BowlingDifficulty.Easy:i==1?BowlingDifficulty.Normal:BowlingDifficulty.Hard;
            Check(m.StartMatch(),"default three-heat physical four-truck AI match starts");var starts=Array.ConvertAll(m.trucks,t=>t.body.position);yield return new WaitForSeconds(1.5f);bool settled=m.platform.isKinematic&&m.Tilt<.1f;float drift=0;for(int i=0;i<4;i++){settled&=m.OnBoard(i)&&Vector3.Dot(m.trucks[i].transform.up,Vector3.up)>.95f;drift=Mathf.Max(drift,Vector3.ProjectOnPlane(m.trucks[i].body.position-starts[i],Vector3.up).magnitude);}Check(settled&&drift<.3f,"all four trucks settle grounded and upright on the held board before clean release; drift="+drift.ToString("F3"));
            Time.timeScale=4;float deadline=Time.realtimeSinceStartup+170;bool paused=false,captured=false;float peak=0,nextTrace=0;
            while(!m.Finished&&Time.realtimeSinceStartup<deadline)
            {
                peak=Mathf.Max(peak,m.Tilt);
                if(!paused&&m.rules.Clock>2){paused=true;float clock=m.rules.Clock;var p=m.platform.position;var q=m.platform.rotation;m.Pause();yield return new WaitForSecondsRealtime(.15f);Check(clock==m.rules.Clock&&p==m.platform.position&&q==m.platform.rotation,"pause freezes the actual pivot, tractors and scoring clock");m.Resume();Time.timeScale=4;}
                if(!captured&&m.rules.Clock>12){captured=true;bool visible=true;for(int corner=0;corner<8;corner++){var p=m.platform.transform.TransformPoint(new Vector3((corner&1)==0?-m.halfWidth:m.halfWidth,(corner&2)==0?-m.halfThickness:m.halfThickness,(corner&4)==0?-m.halfWidth:m.halfWidth));var v=m.broadcast.view.WorldToViewportPoint(p);visible&=v.z>0&&v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1;}Check(visible,"shared camera keeps all eight actual tilted deck corners inside its viewport");yield return SportsWindowCapture.Save(1280,720,Folder+"/gameplay-720.png");yield return SportsWindowCapture.Save(1920,1080,Folder+"/gameplay-1080.png");}
                if(m.rules.Heat*100+m.rules.Clock>=nextTrace){nextTrace=m.rules.Heat*100+m.rules.Clock+15;report.AppendLine("ACTUAL_SEESAW heat="+m.rules.Heat+" clock="+m.rules.Clock.ToString("F1")+" tilt="+m.Tilt.ToString("F2")+" impacts="+m.QuarryImpacts+" scores="+string.Join(",",m.rules.scores));File.WriteAllText(Folder+"/progress.txt",report.ToString());}yield return null;
            }
            Time.timeScale=1;Check(m.Finished&&m.rules.Complete&&m.rules.Heat==2,"actual AI balance/ramming completes all three heats and final results");Check(peak>3&&captured,"actual vehicle weights and movement produce visible physical platform tilt; peak="+peak.ToString("F2"));report.AppendLine("ACTUAL_AI_RESULTS\n"+m.Standings);
            if(m.Finished){string result=m.Results;m.ExtraAction("watchseesaw");yield return new WaitForSecondsRealtime(.4f);Check(m.review.Playing,"actual board and tractor pose replay starts");yield return SportsWindowCapture.Save(1280,720,Folder+"/replay-720.png");m.ExtraAction("replayskip");Check(!m.review.Playing&&result==m.Results,"replay preserves authoritative heat scores and final placing");}
            fixturePad=InputSystem.AddDevice<Gamepad>("Controlled real platform weight and driving fixture");
            float centered=0;for(int axis=0;axis<3;axis++)
            {
                m.SetupScreen();m.setup.count=1;m.option=0;m.setup.players[0].source=BowlingSource.WholeController;m.setup.players[0].pad=fixturePad;InputSystem.QueueStateEvent(fixturePad,new GamepadState());yield return null;
                bool started=m.StartMatch();var target=m.platformOrigin+new Vector3(axis==1?26:0,m.halfThickness+.65f,axis==2?26:0);m.trucks[0].Prepare(target,Quaternion.identity);Check(started,"physical mass-distribution fixture starts at axis "+axis);yield return new WaitForSeconds(8);
                if(axis==0){centered=m.Tilt;Check(centered<3,"centered physical tractor leaves the two-axis platform nearly level; tilt="+centered.ToString("F2"));}
                else{float lowered=axis==1?m.platform.transform.right.y:m.platform.transform.forward.y;Check(m.Tilt>centered+1&&lowered<-.015f,"actual NWH wheel weight lowers the occupied "+(axis==1?"right":"forward")+" edge; tilt="+m.Tilt.ToString("F2")+" lowered="+lowered.ToString("F3"));}
            }
            m.SetupScreen();m.setup.count=1;m.setup.players[0].source=BowlingSource.WholeController;m.setup.players[0].pad=fixturePad;InputSystem.QueueStateEvent(fixturePad,new GamepadState());yield return null;Check(m.StartMatch(),"driving fall/rematch restores a real unbroken tractor and level countdown");m.trucks[0].Prepare(m.platformOrigin+new Vector3(26,m.halfThickness+.65f,0),Quaternion.Euler(0,90,0));
            while(m.Countdown>0)yield return null;InputSystem.QueueStateEvent(fixturePad,new GamepadState{rightTrigger=1});deadline=Time.realtimeSinceStartup+24;while(m.rules.alive[0]&&Time.realtimeSinceStartup<deadline)yield return null;Check(!m.rules.alive[0],"software accelerator physically drives the tractor off the actual deck into a legitimate fall");InputSystem.QueueStateEvent(fixturePad,new GamepadState());while(m.QuarryImpacts==0&&Time.realtimeSinceStartup<deadline)yield return null;Check(m.QuarryImpacts>0&&m.fragments[0].Bursting,"the fallen physical truck hits the quarry and activates actual mesh destruction");yield return SportsWindowCapture.Save(1280,720,Folder+"/quarry-impact-720.png");
            m.SetupScreen();yield return SportsExpansionCameraAcceptance.Run(m,m.drivingViews,Check,Folder);Complete();
        }
        void Complete()
        {
            report.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; platform="+Application.platform+"; capture="+SportsWindowCapture.EvidenceKind+"; real NWH wheel reactions, joint physics and software input; no human/device/listening certification.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
            if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
            Application.Quit(errors==0?0:2);
        }
        void OnDestroy(){Application.logMessageReceived-=Error;if(fixturePad!=null&&fixturePad.added)InputSystem.RemoveDevice(fixturePad);InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            preferences?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
    }
}
#endif
