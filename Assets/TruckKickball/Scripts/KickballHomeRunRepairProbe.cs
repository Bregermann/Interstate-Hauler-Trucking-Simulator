#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckKickball
{
    public sealed class KickballHomeRunRepairProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsKickballHomeRunRepair")<0)return;var go=new GameObject("Explicit legal-fence Home Run regression");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<KickballHomeRunRepairProbe>();}
        KickballMatch match;string folder;int checks,errors;bool done;float deadline;IDisposable preferences;readonly StringBuilder log=new StringBuilder();
        void Check(bool value,string message){checks++;if(!value)errors++;log.AppendLine((value?"PASS ":"FAIL ")+message);File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Error(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;log.AppendLine("ERROR "+message+"\n"+stack);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsKickballOutput");folder=output>=0?args[output+1]:SportsEvidencePaths.Folder("PlaytestRepair/KickballHomeRuns");Directory.CreateDirectory(folder);
            preferences=SportsPrefs.IsolateValidation("KickballHomeRunRepair");Application.logMessageReceived+=Error;deadline=Time.realtimeSinceStartup+120;
            yield return SportsProbeScene.Ensure<KickballMatch>(SportsNavigation.Kickball);yield return null;match=FindAnyObjectByType<KickballMatch>();match.AllBots();match.option=7;
            while(match.ViewMode!=0)match.ExtraAction("view");Check(match.StartMatch(),"saved twelve-truck match starts with actual existing cameras and rules");
            for(int attempt=0;attempt<2;attempt++)
            {
                while(match.rules.Phase!=KickballPhase.Pitch)yield return null;
                int batter=match.rules.Batter,team=match.rules.BattingTeam,score=match.rules.Scores[team],outs=match.rules.Outs;
                // Seed a legal kicked-flight state after the actual pitcher
                // releases the ball. The moving fence clearance below is real
                // physics; this is not a human bumper-kick acceptance claim.
                Check(match.Carrier<0&&match.rules.Kick(),"explicit legal-flight fixture follows a real released pitch");
                match.ball.body.position=new Vector3(0,6,104);match.ball.body.linearVelocity=Vector3.forward*16;match.ball.body.angularVelocity=Vector3.zero;match.ball.Observe();Physics.SyncTransforms();
                float until=Time.realtimeSinceStartup+2;while(!match.HomeRunCelebrating&&Time.realtimeSinceStartup<until)yield return null;
                Check(match.HomeRunCelebrating&&match.rules.Phase==KickballPhase.HomeRun,"actual fair airborne fence clearance enters sealed celebration");
                Check(match.rules.Scores[team]==score+1&&!match.rules.Runner(batter),"run is credited immediately without driving the bases");
                match.QueueTag(batter);yield return new WaitForFixedUpdate();
                Check(match.rules.Scores[team]==score+1&&match.rules.Outs==outs&&match.Carrier<0,"late tag cannot revoke credit or create an out");
                Check(match.homeRunCard!=null&&match.homeRunCard.gameObject.activeInHierarchy&&match.homeRunBanner.text.Contains("HOME RUN"),"saved scene displays its animated home-run banner");
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"home-run-"+attempt+"-"+size.x+".png"));yield return new WaitForSecondsRealtime(.15f);}
                until=Time.realtimeSinceStartup+8;while(match.HomeRunCelebrating&&Time.realtimeSinceStartup<until)yield return null;
                if(match.ReplayControls)match.ExtraAction("replayskip");
                Check(!match.HomeRunCelebrating&&match.rules.Phase==KickballPhase.PitchReady&&match.rules.CompletedPlays==attempt+1,"celebration/review returns to exactly one next at-bat");
                Check(match.rules.Scores[team]==score+1&&!match.homeRunCard.gameObject.activeSelf&&match.cameras.Length>0,"credited score and camera ownership survive next-play reset");
            }
            match.SetupScreen();Check(match.StartMatch()&&match.rules.Scores[0]==0&&match.HomeRunCelebrations==0&&!match.HomeRunCelebrating,"rematch clears result, celebration and pending physics state");match.SetupScreen();Finish();
        }
        void Update(){if(!done&&deadline>0&&Time.realtimeSinceStartup>deadline){Check(false,"home-run reproduction watchdog expired");Finish();}}
        void Finish(){if(done)return;done=true;log.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; explicit physical fence-crossing fixtures, not human bumper-kick/device/listening acceptance");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());Application.Quit(errors==0?0:1);}
        void OnDestroy(){Application.logMessageReceived-=Error;preferences?.Dispose();}
    }
}
#endif
