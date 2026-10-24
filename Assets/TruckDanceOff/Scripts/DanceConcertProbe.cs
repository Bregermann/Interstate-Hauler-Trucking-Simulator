#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using LWS.TruckBowling;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDanceOff
{
    [DefaultExecutionOrder(1600)]
    public sealed class DanceConcertProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDanceConcert")<0)return;var root=new GameObject("Explicit original Dance-Off concert acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<DanceConcertProbe>();}
        string folder;DanceMatch match;IDisposable prefs;int checks,errors;bool observing;float minimumHeight=1,peakStep;Vector3 previous;readonly bool[] seen=new bool[4],captured=new bool[4];readonly StringBuilder log=new StringBuilder();
        void Check(bool okay,string text){checks++;if(!okay)errors++;log.AppendLine((okay?"PASS ":"FAIL ")+text);File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Error(string text,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;log.AppendLine("ERROR "+text+"\n"+stack);}
        void LateUpdate()
        {
            if(!observing||match==null||!match.Live||match.Phase!=DancePhase.Song)return;var camera=match.stageCamera;var director=match.GetComponent<DanceConcertCamera>();
            if(director.CurrentShot>=0)seen[director.CurrentShot]=true;peakStep=Mathf.Max(peakStep,Vector3.Distance(previous,camera.transform.position));previous=camera.transform.position;
            float largest=0;foreach(var dancer in match.dancers)if(dancer.gameObject.activeInHierarchy)largest=Mathf.Max(largest,SportsCameraFit.Projected(camera,dancer.truck.shape.bounds).height);minimumHeight=Mathf.Min(minimumHeight,largest);
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDanceConcertOutput");folder=at>=0?args[at+1]:SportsEvidencePaths.Folder("DanceConcert");Directory.CreateDirectory(folder);prefs=SportsPrefs.IsolateValidation("DanceConcertWindows");Application.logMessageReceived+=Error;
            SportsNavigation.LoadSport("TruckDanceOff");yield return null;yield return null;match=FindAnyObjectByType<DanceMatch>();var director=match.GetComponent<DanceConcertCamera>();
            Check(director!=null&&director.cameraRig.rig!=null,"saved scene uses the original Cinemachine rig and owned musical shot selection");
            var sign=GameObject.Find("Original Truck Sports Party stage wordmark").GetComponent<Renderer>();Check(sign.sharedMaterial.mainTexture.name=="TruckDanceOff","actual stage uses the original dedicated Dance-Off PNG");
            Check(match.dancers.All(d=>Vector3.Dot(d.transform.forward,Vector3.back)>.999f),"all four authored trucks face the audience before choreography");
            match.SetupScreen();match.setup.count=4;match.AllBots();match.option=7;for(int i=0;i<4;i++)match.setup.players[i].difficulty=(BowlingDifficulty)(i%3);
            Check(match.StartMatch(),"original four-AI DSP-timed competition starts");Check(match.Rules.All(r=>Math.Abs(r.AcceptedGreatWindow-.12)<.00001&&Math.Abs(r.AcceptedGoodWindow-.21)<.00001),"humans and AI share configured120/210ms timing with narrow45ms PERFECT");
            previous=match.stageCamera.transform.position;float deadline=Time.realtimeSinceStartup+(float)match.Song.duration+45;bool paused=false;double lastShot=-1;int priorShot=-1;observing=true;
            while(!match.Finished&&Time.realtimeSinceStartup<deadline)
            {
                if(match.Phase==DancePhase.Song)
                {
                    int shot=director.CurrentShot;if(shot!=priorShot){priorShot=shot;lastShot=match.SongTime;log.AppendLine("SHOT "+shot+" dspSongTime="+match.SongTime.ToString("F3"));}
                    if(shot>=0&&!captured[shot]&&match.SongTime-lastShot>1.3)
                    {
                        captured[shot]=true;
                        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Check(Screen.width==size.x&&Screen.height==size.y,"actual concert window shot="+shot+" "+size);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"actual-musical-shot-"+shot+"-"+size.x+".png"));yield return new WaitForSecondsRealtime(.12f);}
                    }
                    if(!paused&&match.SongTime>10){paused=true;match.Pause();yield return null;var pose=match.stageCamera.transform.position;double clock=match.SongTime;yield return new WaitForSecondsRealtime(.25f);Check(match.Paused&&pose==match.stageCamera.transform.position&&clock==match.SongTime,"pause freezes camera and authoritative song judgment clock");match.Resume();previous=match.stageCamera.transform.position;}
                }
                yield return null;
            }
if(match.Finished)yield return SportsResultsAcceptance.Capture(match,Check,folder,slot=>SportsResultPlacings.FromFinalTotals(slot,match.setup.count,match.total),slot=>(double?)match.total[slot],slot=>SportsResultPlacings.FromFinalTotals(slot,match.setup.count,match.total)==1);
             observing=false;Check(match.Finished&&match.Rules.All(r=>r.Resolved==r.Count),"complete original song resolves every real note and reaches results");
            Check(match.total.All(v=>v>0)&&match.PhysicalRoutines>40,"actual AI timestamps produce scores and original authored truck dance moves");Check(seen.All(v=>v)&&captured.All(v=>v),"all four bounded musical shot styles render during ordinary DSP gameplay");Check(minimumHeight>.035f,"important truck stays recognizable during concert shots; minimum largest projected height="+minimumHeight.ToString("F4"));
            log.AppendLine("CAMERA peak observed frame displacement="+peakStep.ToString("F3")+"m; not a controlled performance benchmark.");
            yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"actual-concert-results.png"));yield return new WaitForSecondsRealtime(.2f);
            match.SetupScreen();match.AllBots();Check(match.StartMatch()&&match.Round==1&&match.Rules.All(r=>r.Score==0),"rematch restores original chart and clean score");yield return null;yield return null;Check(director.CurrentShot==0,"rematch resets the musical shot sequence");match.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;
            Check(FindAnyObjectByType<DanceConcertCamera>()==null&&FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"menu return removes concert ownership and retains one listener");
            log.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; actual composed Windows concert and original DSP/AI judgments. No injected scores, human playtest, listening, physical-device or isolated performance claim.");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());Application.Quit(errors==0?0:2);
        }
        void OnDestroy(){Application.logMessageReceived-=Error;prefs?.Dispose();Time.timeScale=1;}
    }
}
#endif
