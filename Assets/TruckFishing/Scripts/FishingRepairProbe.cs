#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckFishing
{
    // Explicit real-stream regression fixtures, never enabled in ordinary play.
    public sealed class FishingRepairProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsFishingRepair")<0)return;
            var go=new GameObject("Explicit fish/net crossing regression");DontDestroyOnLoad(go);
            go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<FishingRepairProbe>();
        }
        FishingMatch match;Transform spawned;string folder;int checks,errors;bool done;
        IDisposable preferences;Keyboard keyboard;readonly StringBuilder log=new StringBuilder();float deadline;
        void Check(bool pass,string message){checks++;if(!pass)errors++;log.AppendLine((pass?"PASS ":"FAIL ")+message);File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Error(string message,string stack,LogType type){if(type!=LogType.Exception&&type!=LogType.Error)return;errors++;log.AppendLine("ERROR "+message+"\n"+stack);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsFishingOutput");
            folder=output>=0?args[output+1]:SportsEvidencePaths.Folder("PlaytestRepair/Fishing");Directory.CreateDirectory(folder);
            preferences=SportsPrefs.IsolateValidation("FishingRepair");Application.logMessageReceived+=Error;deadline=Time.realtimeSinceStartup+180;
            keyboard=InputSystem.AddDevice<Keyboard>("Fishing regression keyboard");
            yield return SportsProbeScene.Ensure<FishingMatch>(SportsNavigation.Fishing);yield return null;
            match=FindAnyObjectByType<FishingMatch>();match.setup.count=1;match.AllBots();match.setup.players[0].source=BowlingSource.Keyboard;
            bool started=match.StartMatch();Check(started,"real saved Fishing scene starts");if(!started){Finish();yield break;}while(!match.Playing)yield return null;
            var stream=match.streams[0];stream.Spawned+=(s,t)=>spawned=t;
            int hits=0;
            for(int attempt=0;attempt<10;attempt++)
            {
                stream.HideFish();int before=match.rules.caught[0];
                var plan=new FishPlan{id=10000+attempt,kind=attempt%2==0?FishKind.Small:FishKind.Large,x=stream.NetX,travel=.25f,entryLead=.35f,arrival=match.Clock+.60};
                stream.Spawn(plan);var fish=spawned;float previous=fish.localPosition.z;double previousClock=match.Clock;bool crossed=false;
                while(fish.gameObject.activeSelf&&match.Clock<plan.arrival+.1)
                {
                    yield return new WaitForFixedUpdate();yield return null;
                    float current=fish.localPosition.z;
                    if(previous>0&&current<=0&&!crossed)
                    {
                        crossed=true;
                        log.AppendLine("CROSS fixture="+attempt+" clock="+match.Clock.ToString("F4")+" due="+plan.arrival.ToString("F4")+" x="+fish.localPosition.x+" net="+stream.NetX+" z="+current);
                        Check(match.rules.caught[0]==before+(plan.kind==FishKind.Large?5:1),"visible net passage awards correct weight in the same simulation step fixture="+attempt);
                    }
                    if(match.Clock>previousClock){Check(Mathf.Abs(current-previous)<=20f/plan.travel*(float)(match.Clock-previousClock)+.05f,"upstream entry has no lead-time teleport fixture="+attempt);previousClock=match.Clock;previous=current;}
                }
                Check(crossed,"actual pooled fish crosses the visible hoop plane fixture="+attempt);
                int expected=before+(plan.kind==FishKind.Large?5:1);if(match.rules.caught[0]==expected)hits++;
                Check(match.rules.caught[0]==expected,"exactly one score for the resolved pooled fish fixture="+attempt);
            }
            Check(hits==10,"ten consecutive real-stream catches at the actual net");
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"fishing-net-"+size.x+".png"));yield return new WaitForSecondsRealtime(.2f);
            }
            match.SetupScreen();Check(match.StartMatch()&&match.Clock==0&&match.rules.caught[0]==0,"real rematch resets clock, score and pooled models");
            match.SetupScreen();if(Array.IndexOf(args,"-tsFishingFull")>=0)yield return CompleteAIMatches();Finish();
        }
        sealed class MotionTrace{public Transform model;public FishingStream stream;public Vector3 position;public double clock;public bool sampled;}
        readonly Dictionary<Transform,MotionTrace> motion=new Dictionary<Transform,MotionTrace>();
        bool measure;int movementSamples;float peakDownstream;
        void ObserveSpawn(FishingStream stream,Transform model)
        {
            if(!measure)return;
            if(!motion.TryGetValue(model,out var trace)){trace=new MotionTrace();motion.Add(model,trace);}
            trace.model=model;trace.stream=stream;trace.position=model.localPosition;trace.clock=match.Clock;trace.sampled=false;
        }
        void SampleMovement()
        {
            foreach(var trace in motion.Values)
            {
                if(!trace.model.gameObject.activeInHierarchy||match.Clock<=trace.clock)continue;
                if(trace.sampled){float speed=(trace.position.z-trace.model.localPosition.z)/(float)(match.Clock-trace.clock);if(speed>0){peakDownstream=Mathf.Max(peakDownstream,speed);movementSamples++;}}
                trace.position=trace.model.localPosition;trace.clock=match.Clock;trace.sampled=true;
            }
        }
        IEnumerator CompleteAIMatches()
        {
            foreach(var stream in match.streams)stream.Spawned+=ObserveSpawn;
            deadline=Time.realtimeSinceStartup+480;
            int first=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsFishingHardOnly")>=0?2:0;
            for(int difficulty=first;difficulty<3;difficulty++)
            {
                match.SetupScreen();match.setup.count=4;match.AllBots();match.seed=17381+difficulty*8011;
                foreach(var player in match.setup.players)player.difficulty=(BowlingDifficulty)difficulty;
                SportsCameraOptions.Set(difficulty==1);motion.Clear();movementSamples=0;peakDownstream=0;measure=true;
                Check(match.StartMatch(),"complete four-AI survival starts difficulty="+difficulty);Time.timeScale=4;float until=Time.realtimeSinceStartup+(difficulty==2?200:90);bool captured=false;
                while(!match.Finished&&Time.realtimeSinceStartup<until)
                {
                    SampleMovement();
                    if(!captured&&match.Clock>14)
                    {
                        captured=true;Time.timeScale=1;
                        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"natural-fish-difficulty-"+difficulty+"-"+size.x+".png"));yield return new WaitForSecondsRealtime(.1f);}
                        Time.timeScale=4;
                    }
                    yield return null;
                }
                Time.timeScale=1;measure=false;
                Check(match.Finished&&match.rules.Complete&&match.rules.Remaining<=1,"actual four-AI survival resolves a final result difficulty="+difficulty);
                for(int slot=0;slot<4;slot++)Check(match.rules.caught[slot]>0&&!match.streams[slot].Overflowed,"same actual net catches generated fish without pool overflow slot="+slot+" difficulty="+difficulty);
                Check(movementSamples>20&&peakDownstream>match.originalInitialSpeed*1.8f,"measured model translation exceeds original speed by approximately2x or more");
                log.AppendLine("NATURAL MATCH difficulty="+difficulty+" clock="+match.Clock+" caught="+string.Join(",",match.rules.caught)+" missed="+string.Join(",",match.rules.missed)+" winner="+match.rules.Winner+" measuredSamples="+movementSamples+" peakDownstreamMetersPerSecond="+peakDownstream+" acceleratedSimulation=4x; not a GPU benchmark");
            }
            foreach(var stream in match.streams)stream.Spawned-=ObserveSpawn;
            match.SetupScreen();SportsCameraOptions.Set(false);Check(match.StartMatch()&&match.Clock==0&&Array.TrueForAll(match.rules.caught,s=>s==0),"repeated complete-match rematch restores all stream state");match.SetupScreen();
        }
        void Update(){if(!done&&deadline>0&&Time.realtimeSinceStartup>deadline){Check(false,"watchdog: no softlock permitted");Finish();}}
        void Finish(){if(done)return;done=true;log.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; explicit real-stream software fixtures; no human/controller/listening acceptance");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());Application.Quit(errors==0?0:1);}
        void OnDestroy(){Time.timeScale=1;Application.logMessageReceived-=Error;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);preferences?.Dispose();}
    }
}
#endif
