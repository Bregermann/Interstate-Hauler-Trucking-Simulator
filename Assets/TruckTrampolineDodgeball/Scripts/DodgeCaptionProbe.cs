#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckDodgeball
{
    // Focused actual-event display acceptance; not another full-match or hardware claim.
    public sealed class DodgeCaptionProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDodgeCaption")<0)return;var root=new GameObject("Explicit actual Dodgeball caption acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<DodgeCaptionProbe>();}
        DodgeMatch match;IDisposable preferences;string folder;int checks,errors;readonly StringBuilder log=new StringBuilder();
        void Check(bool pass,string message){checks++;if(!pass)errors++;log.AppendLine((pass?"PASS ":"FAIL ")+message);File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Error(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;log.AppendLine("ERROR "+message);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsDodgeCaptionOutput");folder=output>=0?args[output+1]:SportsEvidencePaths.Folder("DodgeCaptionEvidence");Directory.CreateDirectory(folder);
            preferences=SportsPrefs.IsolateValidation("DodgeCaptionActualEvents");SportsParticipantRegistry.ForgetCache();Application.logMessageReceived+=Error;
            yield return SportsProbeScene.Ensure<DodgeMatch>("TruckTrampolineDodgeball");match=FindAnyObjectByType<DodgeMatch>();yield return null;
            foreach(int count in new[]{2,4,8})
            {
                match.SetupScreen();match.teamA=match.teamB=count/2;match.NormalizeSetup();match.AllBots();foreach(var player in match.setup.players){player.name="Same Driver Name";player.difficulty=BowlingDifficulty.Normal;}
                Check(match.StartMatch()&&match.setup.count==count,"original autonomous "+count+"participant match starts; no score or contact injection");Time.timeScale=4;float end=Time.realtimeSinceStartup+300;
                while(match.ScoreJournal.Count==0&&!match.Finished&&Time.realtimeSinceStartup<end)yield return null;
                Check(match.ScoreJournal.Count>0,"actual physical team round win before caption capture, count="+count);
                if(match.ScoreJournal.Count>0)
                {
                    match.enabled=false;int receipts=match.ScoreJournal.Count;yield return SportsScoreAcceptance.CaptureAward(match,Check,Path.Combine(folder,"roster-"+count));
                    var root=match.menus.hud.transform.Find("Broadcast participant cards");Check(root!=null,"saved shared broadcast cards exist");
                    if(root!=null)for(int slot=0;slot<count;slot++)
                    {
                        var card=root.Find("Participant card "+slot);var value=card.Find("Score badge").GetComponent<TMP_Text>();var detail=card.Find("Status").GetComponent<TMP_Text>();detail.ForceMeshUpdate();int team=match.rules.Team(slot);
                        Check(value.text==match.rules.wins[team].ToString()&&detail.text.StartsWith(team==0?"BLUE TEAM":"ORANGE TEAM"),"duplicate-name slot keeps its own team and actual win total: "+slot);
                        Check(!detail.isTextOverflowing,"actual ready/out/cooldown caption fits: "+slot+" "+detail.text);
                    }
                    Check(match.ScoreJournal.Count==receipts,"display capture cannot create another award");match.enabled=true;
                }
                match.SetupScreen();Time.timeScale=1;
            }
            SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<DodgeMatch>()==null,"menu return releases original match and caption owner");
            log.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; actual physical first-round events and720/1080backbuffers for2/4/8AIrosters. Not complete matches, human controllers, listening or performance acceptance.");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());Application.Quit(errors==0?0:2);
        }
        void OnDestroy(){Application.logMessageReceived-=Error;if(match!=null)match.enabled=true;preferences?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
    }
}
#endif
