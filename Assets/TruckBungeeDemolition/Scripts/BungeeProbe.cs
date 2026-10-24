using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckBungeeDemolition
{
    public sealed class BungeeProbe:MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod]static void Boot()
        {
            bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsBungee")>=0;
#if UNITY_EDITOR
            run|=UnityEditor.SessionState.GetBool("LWS.BungeeProbe",false);UnityEditor.SessionState.SetBool("LWS.BungeeProbe",false);
#endif
            if(!run)return;var root=new GameObject("Bungee complete four-AI physical match acceptance");DontDestroyOnLoad(root);
            if(FindAnyObjectByType<SportsProbeInputIsolation>()==null)root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<BungeeProbe>();
        }
        SportsScoreAcceptance.Tally awards;
        BungeeMatch match;string folder;int checks,errors;IDisposable preferences;readonly StringBuilder report=new StringBuilder();
        void Check(bool ok,string label){checks++;if(!ok)errors++;report.AppendLine((ok?"PASS ":"FAIL ")+label);File.WriteAllText(Path.Combine(folder,"progress.txt"),report.ToString());}
        void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message+"\n"+stack);}}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.15f);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsBungeeOutput");folder=output>=0?args[output+1]:SportsEvidencePaths.Folder("BungeeEvidence");Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"result.txt"),"RUNNING "+DateTime.UtcNow.ToString("O"));preferences=SportsPrefs.IsolateValidation("BungeeCityWindows");Application.logMessageReceived+=Error;
            yield return SportsProbeScene.Ensure<BungeeMatch>("TruckBungeeDemolition");match=FindAnyObjectByType<BungeeMatch>();yield return null;match.setup.count=4;match.NormalizeSetup();match.AllBots();SportsCameraOptions.Set(false);
            for(int i=0;i<4;i++)match.setup.players[i].difficulty=(BowlingDifficulty)(i%3);
            awards=new SportsScoreAcceptance.Tally(match);Check(match.StartMatch(),"four independent Easy/Normal/Hard AI bays start without controllers");Time.timeScale=4;
            Check(match.bays.All(b=>b.modules.Length==85&&b.modules.Any(m=>m.GetComponentsInChildren<MeshFilter>().Any(f=>f.name=="Owned city model band"))),"four bays contain actual owned model bands and all85original structural modules");
            float end=Time.realtimeSinceStartup+240,next=0;bool paused=false,captured=false,awardCaptured=false;int round=0;var physical=new int[4];
            while(!match.Finished&&Time.realtimeSinceStartup<end)
            {
                if(!awardCaptured&&match.ScoreJournal.Count>0){awardCaptured=true;yield return SportsScoreAcceptance.CaptureAward(match,Check,folder+"/first-collapse");}
                for(int i=0;i<4;i++)physical[i]=Mathf.Max(physical[i],match.bays[i].Collapses);
                if(!paused&&match.Phase==BungeePhase.Demolition&&match.Clock<56)
                {
                    paused=true;match.Pause();var p=match.bays[0].truck.body.position;float clock=match.Clock;int score=match.bays[0].Score;yield return new WaitForSecondsRealtime(.2f);
                    Check(match.Paused&&p==match.bays[0].truck.body.position&&clock==match.Clock&&score==match.bays[0].Score,"pause preserves actual flight,score,rope and active clock");match.Resume();Time.timeScale=4;
                }
                if(!captured&&!Application.isEditor&&match.Phase==BungeePhase.Demolition&&match.Clock<50)
                {
                    captured=true;Time.timeScale=0;
                    foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.3f);yield return Capture("owned-city-actual-match-"+size.x);}
                    Time.timeScale=4;
                }
                if(match.CompletedRounds!=round){round=match.CompletedRounds;report.AppendLine("ROUND "+round+" totals="+string.Join(",",match.total)+" collapses="+string.Join(",",match.bays.Select(b=>b.Collapses))+" recoveries="+string.Join(",",match.bays.Select(b=>b.Recoveries))+" contactImpulse="+string.Join(",",match.bays.Select(b=>b.TransferredImpactImpulse)));}
                if(Time.realtimeSinceStartup>=next){File.WriteAllText(Path.Combine(folder,"progress.txt"),report+"LIVE round="+match.Round+" phase="+match.Phase+" clock="+match.Clock+" scores="+string.Join(",",match.bays.Select(b=>b.Score))+" positions="+string.Join(" | ",match.bays.Select(b=>b.truck.body.position.ToString("F1")))+" errors="+errors);next=Time.realtimeSinceStartup+2;}
                yield return null;
            }
if(match.Finished)yield return SportsResultsAcceptance.Capture(match,Check,folder,slot=>SportsResultPlacings.FromFinalTotals(slot,match.setup.count,match.total,match.Winner),slot=>(double?)match.total[slot],slot=>match.Winner==slot);
             Check(match.Finished&&match.Winner>=0&&match.CompletedRounds>=3,"complete physical three-round four-AI match,including real tiebreak if needed");
            for(int i=0;i<4;i++)Check(physical[i]>0&&match.total[i]>0,"AI physically damages/collapses tower without hidden score slot="+i+" total="+match.total[i]);
            awards.Verify(slot=>match.total[slot],Check);awards.Dispose();awards=null;Check(match.FinaleScoreChanges==0,"all automatic finales preserve locked scores");Check(FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"one listener");
            match.SetupScreen();Check(match.StartMatch()&&match.Round==1&&match.total.All(x=>x==0)&&match.bays.All(b=>b.TransferredImpactImpulse==0),"rematch clears scores/contact momentum and restores all four building variants");
            match.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<BungeeMatch>()==null,"menu return cleans scene-owned building and debris lifecycle");
            report.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; actual Core-Collapse/PhysX AI match; no physical-controller,human,listening or isolated GPU benchmark acceptance");File.WriteAllText(Path.Combine(folder,"result.txt"),report.ToString());Debug.Log("BUNGEE_PROBE_COMPLETE "+report);
            Time.timeScale=1;Application.logMessageReceived-=Error;if(!Application.isEditor)Application.Quit(errors==0?0:2);
        }
        void OnDestroy(){awards?.Dispose();Application.logMessageReceived-=Error;preferences?.Dispose();Time.timeScale=1;}
#endif
    }
}
