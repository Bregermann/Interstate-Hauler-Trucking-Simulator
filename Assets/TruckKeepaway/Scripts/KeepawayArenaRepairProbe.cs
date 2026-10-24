#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckKeepaway
{
    public sealed class KeepawayArenaRepairProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsKeepawayArenaRepair")<0)return;var root=new GameObject("Explicit expanded Keepaway match acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<KeepawayArenaRepairProbe>();}
        int checks,errors;string folder;IDisposable prefs;readonly StringBuilder log=new StringBuilder();
        void Check(bool value,string message){checks++;if(!value)errors++;log.AppendLine((value?"PASS ":"FAIL ")+message);Flush();}
        void Flush()=>File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());
        void Error(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;log.AppendLine("ERROR "+message+"\n"+stack);Flush();}
        static float Footprint(Mesh mesh){var p=mesh.vertices;var t=mesh.triangles;float result=0;for(int i=0;i<t.Length;i+=3)result+=Mathf.Abs(Vector3.Cross(p[t[i+1]]-p[t[i]],p[t[i+2]]-p[t[i]]).y)*.5f;return result;}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsKeepawayArenaOutput");folder=output>=0?args[output+1]:SportsEvidencePaths.Folder("PlaytestRepair/KeepawayArena");Directory.CreateDirectory(folder);
            prefs=SportsPrefs.IsolateValidation("KeepawayExpandedArena");SportsParticipantRegistry.ForgetCache();Application.logMessageReceived+=Error;
            foreach(bool cab in new[]{false,true})
            {
                SportsNavigation.LoadSport(cab?SportsNavigation.KeepawayCab:SportsNavigation.Keepaway);yield return null;yield return null;var match=FindAnyObjectByType<KeepawayMatch>();var field=match.chaosField;
                Check(field.arenaRepairVersion==1,"saved expanded scene is integrated: "+(cab?"Cab":"Arena"));
                var surface=field.ground.surface;var mesh=surface.GetComponent<MeshFilter>().sharedMesh;float area=Footprint(mesh),ratio=area/field.baselinePlayableArea;
                Check(Mathf.Abs(ratio-3)<.002f&&surface.GetComponent<MeshCollider>().sharedMesh==mesh,"actual rendered/collision surface area is three times recorded baseline: "+ratio);
                Check(field.breakableProps.Length==field.baselineObstacleCount&&field.breakableProps.All(p=>p!=null&&p.breakable.fragments.Length==2),"every existing physical obstacle has actual-model pooled fracture; count retained for one-third density");
                match.AllBots();match.option=0;match.chaosLevel=1;match.weatherChoice=1;
                foreach(var player in match.setup.players)player.difficulty=BowlingDifficulty.Normal;
                var scoreTally=new SportsScoreAcceptance.Tally(match,false);bool awardCaptured=false;
                Check(match.StartMatch(),"four-AI first-to-thirty starts in the actual saved "+(cab?"cab":"arena")+" scene");
                Check(match.chaosField.ConnectedCells>1200,"expanded navigation covers the enlarged surface: "+match.chaosField.ConnectedCells);
                Check(match.trucks.Take(match.setup.count).All(t=>field.Reachable(t.body.position)),"all actual initial truck poses connect to the ball");
                int active=field.breakableProps.Count(p=>p.gameObject.activeInHierarchy);Check(active==18,"Lively retains eighteen props across three times the area: "+active);
                Time.timeScale=4;float deadline=Time.realtimeSinceStartup+300,next=0;bool captured=false;
                while(!match.Finished&&Time.realtimeSinceStartup<deadline)
                {
                    if(!awardCaptured&&match.ScoreJournal.Count>0){awardCaptured=true;yield return SportsScoreAcceptance.CaptureAward(match,Check,Path.Combine(folder,cab?"cab-award":"arena-award"));}
                    if(!captured&&match.Pickups>0)
                    {
                        captured=true;
                        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){SportsControlledCapture.Save(match,size.x,size.y,Path.Combine(folder,(cab?"cab-ai-broadcast":"arena")+"-"+size.x+".png"));yield return null;}
                    }
                    if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+3;File.WriteAllText(Path.Combine(folder,"progress.txt"),log+"LIVE variant="+cab+" score="+string.Join(",",match.possession)+" steals="+match.Steals+" broken="+field.breakableProps.Count(p=>p.breakable.HasCollapsed));}
                    yield return null;
                }
                Time.timeScale=1;
                scoreTally.Verify(slot=>Mathf.FloorToInt(match.possession[slot]/10)*10,Check);scoreTally.Dispose();
                Check(match.Finished&&match.Winner>=0&&match.possession[match.Winner]>=30,"actual complete possession match: "+string.Join(",",match.possession));
                Check(match.Pickups>0&&match.Steals>0&&match.Blasts>=match.Steals,"physical pickup, theft and former-carrier explosion rules remain active");
                Check(field.breakableProps.Any(p=>p.breakable.HasCollapsed),"ordinary physical match contact actually destroys at least one arena obstacle");
                match.SetupScreen();Check(match.StartMatch()&&match.possession.All(v=>v==0)&&match.Carrier<0,"rematch resets possession while keeping expanded match settings");yield return null;
                Check(field.breakableProps.All(p=>!p.breakable.HasCollapsed&&p.intactShape.enabled&&p.breakable.fragments.All(f=>!f.gameObject.activeSelf)),"rematch restores every obstacle and retires detached fragments");
                match.SetupScreen();
            }
            SportsNavigation.FrontEnd();yield return null;yield return null;
            Check(FindAnyObjectByType<KeepawayMatch>()==null,"menu return removes expanded match and debris ownership");
            log.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; actual NWH/PhysX AI matches and saved-scene rendering; no physical-controller or human-playtest acceptance.");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());Application.Quit(errors==0?0:2);
        }
        void OnDestroy(){Application.logMessageReceived-=Error;prefs?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
    }
}
#endif
