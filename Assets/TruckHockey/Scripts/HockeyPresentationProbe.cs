#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using LWS.TruckSports;

namespace LWS.TruckHockey
{
    public sealed class HockeyPresentationProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsHockeyPresentation")<0)return;var root=new GameObject("Explicit Hockey presentation and match acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<HockeyPresentationProbe>();}
        HockeyMatch match;string folder;int checks,errors;IDisposable preferences;float deadline;bool done;readonly StringBuilder log=new StringBuilder();
        void Check(bool value,string message){checks++;if(!value)errors++;log.AppendLine((value?"PASS ":"FAIL ")+message);File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+message+"\n"+stack);}}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsHockeyOutput");folder=output>=0?args[output+1]:SportsEvidencePaths.Folder("PlaytestRepair/HockeyPresentation");Directory.CreateDirectory(folder);deadline=Time.realtimeSinceStartup+400;
            preferences=SportsPrefs.IsolateValidation("HockeyPresentation");Application.logMessageReceived+=Error;
            yield return SportsProbeScene.Ensure<HockeyMatch>(SportsNavigation.Hockey);yield return null;match=FindAnyObjectByType<HockeyMatch>();
            var menus=match.menus;var presentation=menus.presentation;Check(presentation!=null&&presentation.participantNames.Length==6,"actual saved Hockey scene binds six portrait cards and presentation adapter");
            match.mode=HockeyMode.AIVsAI;match.Configure();var colors=match.players.Select(p=>p.color).ToArray();
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1080)})
            {
                Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.3f);menus.Show(HockeyScreen.Setup);yield return new WaitForSecondsRealtime(.2f);
                Check(EventSystem.current.currentSelectedGameObject!=null,"setup retains selectable Heat focus at"+size.x);CheckLabels(menus.names.Select(n=>n.textComponent));CheckLabels(presentation.setupSlots);
                yield return Capture("setup-"+size.x);
                Check(match.StartMatch(),"actual six-AI match enters countdown at"+size.x);yield return new WaitForSecondsRealtime(.3f);CheckHud();yield return Capture("countdown-"+size.x);
                match.Pause();yield return new WaitForSecondsRealtime(.2f);Check(presentation.pauseSummary.text.Contains("00:03")||presentation.pauseSummary.text.Contains("03:00"),"pause summary shows authoritative clock/score");Check(EventSystem.current.currentSelectedGameObject!=null,"pause preserves menu focus");yield return Capture("pause-"+size.x);match.ChangeSetup();
            }
            for(int game=0;game<2;game++)
            {
                UnityEngine.Random.InitState(163019+game);match.ChangeSetup();Check(match.StartMatch(game>0),"full physical match starts rematch="+(game>0));Time.timeScale=4;float end=Time.realtimeSinceStartup+135;int count=0;
                while(match.rules.Phase!=HockeyPhase.Finished&&Time.realtimeSinceStartup<end)
                {
                    if(match.rules.Goals>count){count=match.rules.Goals;Check(menus.scoreText.text==match.rules.Score[0]+"   :   "+match.rules.Score[1]||match.rules.Phase==HockeyPhase.Goal,"goal transition uses authoritative score state");yield return new WaitForSecondsRealtime(.15f);CheckHud();if(game==0)yield return Capture("physical-goal-"+count);}
                    yield return null;
                }
                Time.timeScale=1;Check(match.rules.Phase==HockeyPhase.Finished&&match.rules.Winner>=0&&match.rules.Goals>0,"complete six-AI physical regulation/overtime match resolves a winner");Check(match.rules.Score.Sum()==match.rules.Goals,"all physical goals conserved exactly once");
                yield return new WaitForSecondsRealtime(.3f);Check(menus.Screen==HockeyScreen.Result&&menus.resultText.text.Contains(match.TeamName(match.rules.Winner)),"results display actual winner");CheckLabels(new[]{menus.resultText}.Concat(presentation.resultTeams));yield return Capture("results-"+game);
                Check(match.players.Select(p=>p.color).SequenceEqual(colors),"presentation preserves each global crew color and team membership");
            }
            match.ChangeSetup();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<HockeyPresentation>()==null,"menu return cleans up presentation and tweens");Finish();
        }
        void CheckHud()
        {
            var p=match.menus.presentation;p.Refresh();Canvas.ForceUpdateCanvases();CheckLabels(new[]{match.menus.scoreText,match.menus.clockText,match.menus.teamText,match.menus.orangeTeamText,p.blueTitle,p.orangeTitle}.Concat(p.participantNames).Concat(p.participantSources));
            var old=match.menus.hud.GetComponent<SportsArenaOverlay>();Check(old!=null&&!old.enabled&&old.docks.All(d=>!d.plate.gameObject.activeSelf),"obsolete Hockey overlay cannot resurrect empty legacy bars");
            Check(new[]{match.menus.teamText,match.menus.orangeTeamText,match.menus.controlsText}.All(t=>t.transform.parent.GetComponent<UnityEngine.UI.Image>()!=null),"compact rink-end and control labels have contrasting plates against bright ice");
            Check(p.blueTitle.color==HockeyPresentation.Blue&&p.orangeTitle.color==HockeyPresentation.Orange,"fixed team accents do not inherit unrelated crew colors");
            for(int i=0;i<6;i++)Check(p.participantNames[i].text==match.players[i].name&&p.participantNames[i].color==match.players[i].color&&p.participantSources[i].text.StartsWith((i<3?"B":"O")+(i%3+1)),"portrait card owns correct participant slot "+i);
        }
        void CheckLabels(System.Collections.Generic.IEnumerable<TMP_Text> labels)
        {
            foreach(var label in labels){label.ForceMeshUpdate();var corners=new Vector3[4];label.rectTransform.GetWorldCorners(corners);bool safe=label.isActiveAndEnabled&&!label.isTextTruncated;foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(null,corner);safe&=point.x>=-1&&point.y>=-1&&point.x<=Screen.width+1&&point.y<=Screen.height+1;}Check(safe,"rendered label fits screen and text: "+label.name+" @"+Screen.width);}
        }
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.1f);}
        void Update(){if(!done&&deadline>0&&Time.realtimeSinceStartup>deadline){Check(false,"presentation/match watchdog expired");Finish();}}
        void Finish(){if(done)return;done=true;log.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; actual rendered layouts and physical AI matches, no human/controller/listening acceptance");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());Application.Quit(errors==0?0:1);}
        void OnDestroy(){Application.logMessageReceived-=Error;preferences?.Dispose();Time.timeScale=1;}
    }
}
#endif
