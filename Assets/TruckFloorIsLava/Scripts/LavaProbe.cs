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
namespace LWS.TruckFloorIsLava
{
    public sealed class LavaProbe : MonoBehaviour
    {
        const string Folder="Logs/TruckSports/EighteenSports/Lava";
        IDisposable preferences;int checks,errors;readonly StringBuilder report=new StringBuilder();Gamepad pad,secondPad;InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        [RuntimeInitializeOnLoadMethod]static void Boot()
        {
            bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsLava")>=0;
#if UNITY_EDITOR
            run|=UnityEditor.SessionState.GetBool("Sports.Lava.Probe",false);UnityEditor.SessionState.SetBool("Sports.Lava.Probe",false);
#endif
            if(!run)return;var go=new GameObject("Explicit finite volcanic platform acceptance");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<LavaProbe>();
        }
        void Awake()
        {
            preferences=SportsPrefs.IsolateValidation("Lava");SportsParticipantRegistry.ForgetCache();priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }
        void Check(bool ok,string message){checks++;if(!ok)errors++;report.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
        void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message+"\n"+stack);}}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");Application.logMessageReceived+=Error;
            yield return SportsProbeScene.Ensure<LavaMatch>("TruckFloorIsLava");yield return null;var m=FindAnyObjectByType<LavaMatch>();Check(m!=null&&m.trucks.Length==8&&m.platforms.Length==25&&m.lavaVolume.isTrigger,"saved full volcanic scene has eight NWH trucks,25physical platforms and actual molten-lake volume");
            if(m==null){Complete();yield break;}
            bool ramOnly=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsLavaRamOnly")>=0;
#if UNITY_EDITOR
            ramOnly|=UnityEditor.SessionState.GetBool("Sports.Lava.RamOnly",false);UnityEditor.SessionState.SetBool("Sports.Lava.RamOnly",false);
#endif
            if(ramOnly){yield return Ram(m);Complete();yield break;}
            bool blizzard=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsLavaBlizzard")>=0;
#if UNITY_EDITOR
            blizzard|=UnityEditor.SessionState.GetBool("Sports.Lava.Blizzard",false);UnityEditor.SessionState.SetBool("Sports.Lava.Blizzard",false);
#endif
            if(blizzard){var preset=System.Array.Find(m.atmosphere.presets.entries,p=>p.displayName.IndexOf("blizzard",StringComparison.OrdinalIgnoreCase)>=0);if(preset==null)throw new InvalidOperationException("Installed Blizzard weather preset required for HDR verification.");m.atmosphere.presets.Select(preset.id);}
            using var scoreTally=new SportsScoreAcceptance.Tally(m);SportsCameraOptions.Set(false);m.drivingViews.Select(0);m.setup.count=8;m.option=1;m.AllBots();for(int i=0;i<8;i++)m.setup.players[i].difficulty=(BowlingDifficulty)(i%3);
            Check(m.StartMatch(),"eight actual AI trucks start standard collapse survival");var start=Array.ConvertAll(m.trucks,t=>t.body.position);yield return new WaitForSeconds(1.5f);
            bool grounded=true;float drift=0;for(int i=0;i<8;i++){grounded&=m.trucks[i].tractorMotor.Grounded&&Vector3.Dot(m.trucks[i].transform.up,Vector3.up)>.95f;drift=Mathf.Max(drift,Vector3.ProjectOnPlane(m.trucks[i].body.position-start[i],Vector3.up).magnitude);}
            Check(grounded&&drift<.35f&&m.rules.Collapses==0&&!m.rules.Started,"all eight settle upright and separate; no platform activates during countdown; planar drift="+drift.ToString("F3"));
            Time.timeScale=4;float deadline=Time.realtimeSinceStartup+200;bool paused=false,captured=false,scoreCaptured=false;float nextTrace=0;
            while(!m.Finished&&Time.realtimeSinceStartup<deadline)
            {
                if(!scoreCaptured&&m.ScoreJournal.Count>0){scoreCaptured=true;yield return SportsScoreAcceptance.CaptureAward(m,Check,Folder);}

                if(!paused&&m.rules.Clock>2){paused=true;float clock=m.rules.Clock;var position=m.platforms[12].body.position;m.Pause();yield return new WaitForSecondsRealtime(.15f);Check(clock==m.rules.Clock&&position==m.platforms[12].body.position,"pause freezes collapse time, platform motion and survival scores");m.Resume();Time.timeScale=4;}
                if(!captured&&m.rules.Clock>9){captured=true;yield return SportsWindowCapture.Save(1280,720,Folder+"/gameplay-720.png");yield return SportsWindowCapture.Save(1920,1080,Folder+"/gameplay-1080.png");}
                if(m.rules.Clock>=nextTrace){nextTrace=m.rules.Clock+10;report.AppendLine("ACTUAL_LAVA t="+m.rules.Clock.ToString("F1")+" platforms="+m.rules.Available()+" collapses="+m.rules.Collapses+" impacts="+m.LavaImpacts+" jumps="+m.ActualJumps);File.WriteAllText(Folder+"/progress.txt",report.ToString());}yield return null;
            }
            Time.timeScale=1;scoreTally.Verify(slot=>m.rules.scores[slot],Check);Check(scoreCaptured,"actual confirmed award captured at720p/1080p");Check(m.Finished&&m.rules.Complete,"physical eight-truck AI survival reaches complete results without an invented score or timeout");Check(m.rules.Collapses>=1&&m.LavaImpacts>=7,"genuine platform collapses lead to actual lake contacts and mesh destruction");Check(m.ActualJumps>0&&System.Array.FindAll(m.rules.platforms,state=>state!=LavaPlatformState.Untouched).Length>=4,"AI physically escapes across multiple actual platforms using bounded jumps");report.AppendLine("ACTUAL_RESULTS\n"+m.Standings);
            if(m.Finished){yield return SportsResultsAcceptance.Capture(m,Check,Folder,m.rules.Place,slot=>(double?)m.rules.scores[slot],slot=>m.rules.Winner==slot);string result=m.Results;m.ExtraAction("watchlava");yield return new WaitForSecondsRealtime(.3f);Check(m.review.Playing,"recorded truck/platform/debris replay starts");yield return SportsWindowCapture.Save(1280,720,Folder+"/replay-720.png");m.ExtraAction("replayskip");Check(result==m.Results&&!m.review.Playing,"replay cannot change survival placing or scores");}
            pad=InputSystem.AddDevice<Gamepad>("Controlled lava jump and collapse fixture");m.SetupScreen();m.setup.count=1;m.option=1;m.setup.players[0].source=BowlingSource.WholeController;m.setup.players[0].pad=pad;InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;Check(m.StartMatch(),"solo rematch restores all25physical platforms and a full tractor");
            while(m.Countdown>0)yield return null;yield return new WaitForSeconds(.3f);var centre=m.platforms[12];Check(m.rules.platforms[12]==LavaPlatformState.Warning&&m.rules.Available()==25,"actual NWH tire contact activates only the touched centre warning");
            report.AppendLine("SOLO_DEBUG live="+m.Live+" started="+m.rules.Started+" clock="+m.rules.Clock+" pos="+m.trucks[0].body.position+" centre="+centre.body.position+" state="+m.rules.platforms[12]+" kin="+centre.body.isKinematic+" enabled="+centre.surface.enabled+" width="+centre.halfSize+" transform="+centre.transform.position+" collider="+centre.surface.bounds);
            foreach(var wheel in m.trucks[0].wheels)report.AppendLine("WHEEL "+wheel.IsGrounded+" hit="+(wheel.HitCollider==null?"none":wheel.HitCollider.name));
            float remaining=m.rules.Remaining(12);yield return new WaitForSeconds(.3f);Check(m.rules.Remaining(12)<remaining&&m.rules.Available()==25,"resting repeated wheel contacts cannot postpone collapse or consume other tiles");
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;InputSystem.QueueStateEvent(pad,new GamepadState());float baseY=m.trucks[0].body.position.y,peak=baseY;deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline){peak=Mathf.Max(peak,m.trucks[0].body.position.y);yield return null;}
            Check(m.ActualJumps==1&&peak-baseY>3.5f,"actual controller action produces one physical jump with meaningful clearance; height="+(peak-baseY).ToString("F2"));
            Time.timeScale=4;deadline=Time.realtimeSinceStartup+20;while(!m.Finished&&Time.realtimeSinceStartup<deadline)yield return null;Time.timeScale=1;
            Check(m.Finished&&m.rules.Collapses==1&&centre.body.position.y<30,"the touched slab physically falls after its finite warning; untouched tiles remain intact");Check(m.LavaImpacts==1&&m.fragments[0].Bursting,"actual unsupported solo truck reaches molten lake and bursts once");yield return SportsWindowCapture.Save(1280,720,Folder+"/lava-impact-720.png");
            yield return Ram(m);
            m.SetupScreen();yield return SportsExpansionCameraAcceptance.Run(m,m.drivingViews,Check,Folder);Complete();
        }
        IEnumerator Ram(LavaMatch m)
        {
            var clear=Array.Find(m.atmosphere.presets.entries,p=>p.legacyKind==SportsWeatherKind.Clear);if(clear==null)throw new InvalidOperationException("Installed clear preset required for deterministic contact attribution.");m.atmosphere.presets.Select(clear.id);report.AppendLine("CONTROLLED_RAM_WEATHER "+clear.displayName+"; preceding gameplay retains its selected weather.");
            if(pad==null)pad=InputSystem.AddDevice<Gamepad>("Actual ram driver");secondPad=InputSystem.AddDevice<Gamepad>("Stationary actual ram victim");m.SetupScreen();m.setup.count=2;
            for(int i=0;i<2;i++){m.setup.players[i].source=BowlingSource.WholeController;m.setup.players[i].pad=i==0?pad:secondPad;InputSystem.QueueStateEvent(m.setup.players[i].pad,new GamepadState());}
            foreach(var truck in new[]{m.trucks[0],m.trucks[1]}){truck.tractorMotor.vehicle.powertrain.engine.Damage=1;truck.tractorMotor.vehicle.powertrain.transmission.Damage=.9f;truck.wheels[0].Damage=.8f;}
            yield return null;Check(m.StartMatch(),"two independent physical trucks start controlled genuine knockoff fixture");
            Check(Array.TrueForAll(new[]{m.trucks[0],m.trucks[1]},truck=>truck.tractorMotor.vehicle.powertrain.engine.Damage==0&&truck.tractorMotor.vehicle.powertrain.transmission.Damage==0&&Array.TrueForAll(truck.wheels,wheel=>wheel.Damage==0)),"fresh rematch repairs actual NWH engines, transmissions and tyres before contact attribution");
            // Controlled vendor-state regression: NWH actually enters its limiter;
            // normal reset interrupts the coroutine, then the original ram is driven.
            yield return new WaitForFixedUpdate();var engines=new[]{m.trucks[0].tractorMotor.vehicle.powertrain.engine,m.trucks[1].tractorMotor.vehicle.powertrain.engine};var cutoffs=Array.ConvertAll(engines,engine=>engine.revLimiterCutoffDuration);
            foreach(var engine in engines){engine.revLimiterCutoffDuration=2;engine.outputAngularVelocity=engine.revLimiterRPM*Mathf.PI/30*1.02f;}
            float limiterDeadline=Time.realtimeSinceStartup+2;while(!Array.TrueForAll(engines,engine=>engine.revLimiterActive)&&Time.realtimeSinceStartup<limiterDeadline)yield return new WaitForFixedUpdate();
            Check(Array.TrueForAll(engines,engine=>engine.revLimiterActive),"controlled actual NWH limiter activates on both engines before ordinary rematch interruption");
            m.SetupScreen();for(int i=0;i<engines.Length;i++)engines[i].revLimiterCutoffDuration=cutoffs[i];Check(m.StartMatch(),"fresh rematch starts after actual vendor limiter interruption");Check(Array.TrueForAll(engines,engine=>!engine.revLimiterActive),"fresh attempt clears interrupted limiter power restriction before actual acceleration");
            m.trucks[0].Prepare(new Vector3(84,42.65f,0),Quaternion.Euler(0,90,0));m.trucks[1].Prepare(new Vector3(112,42.65f,0),Quaternion.Euler(0,90,0));
            while(m.Countdown>0)yield return null;InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=1});float deadline=Time.realtimeSinceStartup+15,nextTrace=0;
            while(m.ActualBodyContacts==0&&!m.rules.Complete&&Time.realtimeSinceStartup<deadline)
            {
                if(m.rules.Clock>=nextTrace){nextTrace=m.rules.Clock+.5f;var vehicle=m.trucks[0].tractorMotor.vehicle;report.AppendLine("RAM_DRIVE_TRACE clock="+m.rules.Clock+" rawRT="+pad.rightTrigger.ReadValue()+" source="+m.setup.players[0].source+" released="+m.setup.players[0].released+" read="+m.ReadDrive(0)+" command="+m.commands[0]+" throttle="+vehicle.input.states.throttle+" brake="+vehicle.input.states.brakes+" handbrake="+vehicle.input.states.handbrake+" engine="+vehicle.powertrain.engine.IsRunning+" damage="+vehicle.powertrain.engine.Damage+" limiter="+vehicle.powertrain.engine.revLimiterActive+" stalled="+vehicle.powertrain.engine.IsStalled+" gear="+vehicle.powertrain.transmission.Gear+" shifting="+vehicle.powertrain.transmission.isShifting+" clutch="+vehicle.powertrain.clutch.Engagement+" rpm="+vehicle.powertrain.engine.OutputRPM+" wheelOmega="+m.trucks[0].wheels[2].wheel.angularVelocity+" wheelTorque="+m.trucks[0].wheels[2].MotorTorque+" wheelBrake="+m.trucks[0].wheels[2].BrakeTorque+" rpmPercent="+vehicle.powertrain.engine.RPMPercent+" grounded="+m.trucks[0].tractorMotor.Grounded+" position="+m.trucks[0].body.position+" velocity="+m.trucks[0].body.linearVelocity);File.WriteAllText(Folder+"/progress.txt",report.ToString());}yield return null;
            }
            InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=1}.WithButton(GamepadButton.South));yield return null;InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=1});while(!m.rules.Complete&&Time.realtimeSinceStartup<deadline)yield return null;InputSystem.QueueStateEvent(pad,new GamepadState());
            Check(m.rules.knockoffs[0]==1&&!m.rules.alive[1],"actual accelerator-driven solid-truck impact earns one recent lava knockoff, never proximity credit; contacts="+m.ActualBodyContacts+" speed="+m.LastBodyNormalSpeed+" at="+m.LastBodyContactAt+" clock="+m.rules.Clock+" impacts="+m.LavaImpacts+" alive="+string.Join(",",m.rules.alive)+" credits="+string.Join(",",m.rules.knockoffs)+" positions="+m.trucks[0].body.position+" / "+m.trucks[1].body.position);
        }
        void Complete()
        {
            report.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; platform="+Application.platform+"; capture="+SportsWindowCapture.EvidenceKind+"; genuine NWH/PhysX platforms, contacts and software input; no human/device/listening certification.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
            if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
            Application.Quit(errors==0?0:2);
        }
        void OnDestroy(){Application.logMessageReceived-=Error;if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(secondPad!=null&&secondPad.added)InputSystem.RemoveDevice(secondPad);InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            preferences?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
    }
}
#endif
