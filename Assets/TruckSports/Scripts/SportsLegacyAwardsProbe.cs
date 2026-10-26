#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using LWS.TruckBaseJumping;
using LWS.TruckMusicalParking;
using LWS.TruckWallball;

namespace LWS.TruckSports
{
    // Explicit automated physical matches and real player-window captures.
    // Never awards points, moves competitors or reports human/device acceptance.
    public sealed class SportsLegacyAwardsProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsLegacyAwards") < 0) return;
            var root = new GameObject("Explicit original award Windows acceptance"); DontDestroyOnLoad(root);
            root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<SportsLegacyAwardsProbe>();
        }
        readonly StringBuilder log = new StringBuilder(); readonly List<SportsScoreEvent> receipts = new List<SportsScoreEvent>();
        string folder; IDisposable preferences; int checks, errors; bool finished;
        void Check(bool value, string reason) { checks++; if (!value) errors++; log.AppendLine((value ? "PASS " : "FAIL ") + reason); File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString()); }
        void Error(string message,string stack,LogType type) { if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;log.AppendLine("ERROR "+message); }
        void Record(SportsScoreEvent receipt) => receipts.Add(receipt);
        IEnumerator Until(Func<bool> condition, float seconds, string label)
        {
            float deadline=Time.realtimeSinceStartup+seconds;
            while(!condition()&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(condition(),label);
        }
        IEnumerator Capture(string label)
        {
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,false); yield return new WaitForSecondsRealtime(.35f);
                string path=Path.Combine(folder,label+"-"+size.x+"x"+size.y+".png");bool correct=false;
                for(int attempt=0;attempt<10&&!correct;attempt++)
                {
                    if(attempt==3||attempt==6||attempt==9){Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.35f);}
                    yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.2f);
                    if(File.Exists(path))
                    {
                        var bytes=File.ReadAllBytes(path);
                        if(bytes.Length>=24){int width=(bytes[16]<<24)|(bytes[17]<<16)|(bytes[18]<<8)|bytes[19];int height=(bytes[20]<<24)|(bytes[21]<<16)|(bytes[22]<<8)|bytes[23];correct=width==size.x&&height==size.y;}
                    }
                }
                Check(correct,label+" actual Windows backbuffer "+size.x+"x"+size.y+"; no rescale/substitute");
                foreach(var award in FindObjectsByType<SportsScorePresentation>(FindObjectsSortMode.None))
                {
                    if(award.LastPresented==null||award.visibility.alpha<.95f||!award.notification.gameObject.activeInHierarchy)continue;
                    var message=award.notification.GetComponentInChildren<TMPro.TMP_Text>(true);message.ForceMeshUpdate();
                    Check(message.gameObject.activeInHierarchy&&!message.isTextOverflowing&&message.GetComponent<CanvasRenderer>().GetInheritedAlpha()>.95f,label+" actual full award text readable within its authored rail at "+size.x+"x"+size.y);
                }
            }
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsLegacyAwardsOutput");
            if(output<0||output+1>=args.Length)throw new ArgumentException("Provide -tsLegacyAwardsOutput.");
            folder=Path.GetFullPath(args[output+1]);Directory.CreateDirectory(folder);preferences=SportsPrefs.IsolateValidation("LegacyOriginalAwardsWindows");SportsParticipantRegistry.ForgetCache();Application.logMessageReceived+=Error;
            SportsTimeOfDay.Select(SportsTimeChoice.Day);
            yield return Base(); if(errors>5){Finish();yield break;}
            yield return Parking(); if(errors>5){Finish();yield break;}
            yield return Wallball();
            SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<SportsFrontEnd>()!=null,"original collection return after three saved sports");Finish();
        }
        IEnumerator Base()
        {
            yield return SportsProbeScene.Ensure<BaseJumpMatch>("TruckBaseJumping");var match=FindAnyObjectByType<BaseJumpMatch>();match.SetupScreen();match.AllBots();SportsCameraOptions.Set(true);match.option=0;foreach(var p in match.setup.players)p.difficulty=LWS.TruckBowling.BowlingDifficulty.Hard;
            receipts.Clear();match.ScoreConfirmed+=Record;Check(match.StartMatch(),"Base original four-AI round starts");Time.timeScale=4;
            yield return Until(()=>match.RoundLocks>0,130,"Base physical landings reach one original bank");
            if(match.RoundLocks==0){match.ScoreConfirmed-=Record;match.SetupScreen();yield break;}
            match.enabled=false;Time.timeScale=0;
            Check(match.LandingLocks==4&&receipts.Count>0,"Base all four original landing locks and positive awards");
            for(int slot=0;slot<4;slot++)Check(receipts.Where(e=>e.Slot==slot).Sum(e=>e.Delta)==match.totals[slot],"Base slot"+slot+" receipt sum equals original banked total");
            Check(receipts.Select(e=>e.EventId).Distinct().Count()==receipts.Count&&receipts.All(e=>!e.IsTeam&&e.Kind==SportsScoreKind.Points&&e.Unit=="PTS"),"Base unique participant-only original points");
            var panels=match.menus.GetComponentsInChildren<SportsScorePresentation>(true);
            yield return Until(()=>panels.All(p=>match.totals[p.viewport]==0||p.LastPresented!=null),8,"Base each positive award reaches its owned notification");yield return new WaitForSecondsRealtime(1);
            Check(panels.Length==4&&panels.All(p=>p.LastPresented==null||p.LastPresented.Slot==p.ViewportOwner),"Base four viewport-local awards retain original ownership");
            yield return Capture("Base-original-four-view-awards");string prior=match.MatchId;match.enabled=true;match.SetupScreen();Check(match.StartMatch()&&match.MatchId!=prior&&match.ScoreJournal.Count==0,"Base original rematch creates empty new journal");match.ScoreConfirmed-=Record;match.SetupScreen();
        }
        IEnumerator Parking()
        {
            yield return SportsProbeScene.Ensure<ParkingMatch>("TruckMusicalParking");var match=FindAnyObjectByType<ParkingMatch>();match.SetupScreen();match.AllBots();SportsCameraOptions.Set(false);foreach(var p in match.setup.players)p.difficulty=LWS.TruckBowling.BowlingDifficulty.Hard;
            receipts.Clear();match.ScoreConfirmed+=Record;Check(match.StartMatch(),"Parking original eight-AI match starts");Time.timeScale=4;yield return Until(()=>match.Finished,220,"Parking physical captures resolve original last survivor");Time.timeScale=1;
            if(!match.Finished){match.ScoreConfirmed-=Record;match.SetupScreen();yield break;}
            Check(receipts.All(e=>e.Kind==SportsScoreKind.Outcome&&e.Delta==0&&e.Total==0)&&receipts.Select(e=>e.EventId).Distinct().Count()==receipts.Count,"Parking unique measured outcomes, no invented points");
            Check(receipts.Count(e=>e.EventId.Contains(":capture:"))==match.TotalCaptures&&receipts.Count(e=>e.MatchEnded&&e.Slot==match.Winner)==1,"Parking original capture count and sole champion reconcile");
            var panel=match.menus.GetComponentInChildren<SportsScorePresentation>(true);yield return Until(()=>panel.LastPresented!=null&&panel.LastPresented.MatchEnded,45,"Parking actual final champion reaches shared Heat panel");Time.timeScale=0;yield return new WaitForSecondsRealtime(1);yield return Capture("Parking-original-champion");
            int count=receipts.Count;yield return new WaitForSecondsRealtime(.15f);Check(receipts.Count==count,"Parking result/capture cannot create another outcome");string prior=match.MatchId;match.SetupScreen();Check(match.StartMatch()&&match.MatchId!=prior&&match.TotalCaptures==0&&match.ScoreJournal.Count==0,"Parking original rematch clears captures/journal");match.ScoreConfirmed-=Record;match.SetupScreen();
        }
        IEnumerator Wallball()
        {
            yield return SportsProbeScene.Ensure<WallballMatch>("TruckWallball");var match=FindAnyObjectByType<WallballMatch>();match.ChangeSetup();match.blueSize=match.orangeSize=4;match.Configure(WallballMode.AIVsAI);foreach(var p in match.players)p.difficulty=LWS.TruckHockey.HockeyDifficulty.Hard;
            receipts.Clear();match.ScoreConfirmed+=Record;Check(match.StartMatch(),"Wallball original4v4 physical match starts");Time.timeScale=4;yield return Until(()=>match.rules.Phase==WallballPhase.Finished,240,"Wallball real win-by-two match finishes");
            if(match.rules.Phase!=WallballPhase.Finished){match.ScoreConfirmed-=Record;match.ChangeSetup();yield break;}
            Check(Time.timeScale==0,"Wallball retains original frozen results");Check(receipts.Count==match.rules.Points&&receipts.Select(e=>e.EventId).Distinct().Count()==receipts.Count,"Wallball one receipt per original rally point");
            for(int team=0;team<2;team++)Check(receipts.Where(e=>e.TeamId==(team==0?"blue":"orange")).Sum(e=>e.Delta)==match.rules.Score[team],"Wallball original team"+team+" total reconstructed");
            Check(receipts.All(e=>e.IsTeam&&e.ParticipantId==null&&e.Slot==-1)&&receipts.Count(e=>e.MatchEnded)==1,"Wallball no invented individual scorer; one champion team");
            var panel=match.menus.GetComponentInChildren<SportsScorePresentation>(true);yield return Until(()=>panel.LastPresented!=null&&panel.LastPresented.MatchEnded,8,"Wallball final Heat receipt displays during original result pause");yield return new WaitForSecondsRealtime(1);yield return Capture("Wallball-original-team-results");
            var board=match.menus.GetComponentInChildren<SportsResultsPresentation>(true);Check(board.Displaying&&board.Snapshot.Rows.Count==8,"Wallball eight original participant result rows");int count=receipts.Count;match.ResolvePoint();Check(receipts.Count==count,"Wallball repeated resolve stays idempotent");string prior=match.MatchId;Check(match.StartMatch(true)&&match.MatchId!=prior&&match.rules.Server==1&&match.ScoreJournal.Count==0,"Wallball original rematch alternates opener and clears journal");match.ScoreConfirmed-=Record;match.ChangeSetup();
        }
        void Finish(){finished=true;Application.logMessageReceived-=Error;log.AppendLine("CHECKS="+checks+" ERRORS="+errors);log.AppendLine("Actual original NWH/PhysX AI matches and Windows backbuffers; no injected scores, human/controller/listening/full-collection performance acceptance.");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());Time.timeScale=1;preferences?.Dispose();preferences=null;SportsParticipantRegistry.ForgetCache();Application.Quit(errors==0?0:2);}
        void OnDestroy(){Application.logMessageReceived-=Error;if(!finished&&folder!=null){log.AppendLine("INTERRUPTED");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());}preferences?.Dispose();Time.timeScale=1;}
    }
}
#endif
