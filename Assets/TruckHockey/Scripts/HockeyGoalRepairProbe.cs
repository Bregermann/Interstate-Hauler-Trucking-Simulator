#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckHockey
{
    public sealed class HockeyGoalRepairProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsHockeyGoalRepair")<0)return;var go=new GameObject("Explicit physical Hockey goal reproduction");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<HockeyGoalRepairProbe>();}
        HockeyMatch match;string folder;int checks,errors;bool done;float deadline;IDisposable preferences;readonly StringBuilder log=new StringBuilder();
        void Check(bool value,string message){checks++;if(!value)errors++;log.AppendLine((value?"PASS ":"FAIL ")+message);File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Error(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;log.AppendLine("ERROR "+message+"\n"+stack);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsHockeyOutput");folder=output>=0?args[output+1]:SportsEvidencePaths.Folder("PlaytestRepair/HockeyGoals");Directory.CreateDirectory(folder);
            deadline=Time.realtimeSinceStartup+180;preferences=SportsPrefs.IsolateValidation("HockeyGoalRepair");Application.logMessageReceived+=Error;
            yield return SportsProbeScene.Ensure<HockeyMatch>(SportsNavigation.Hockey);yield return null;match=FindAnyObjectByType<HockeyMatch>();
            match.mode=HockeyMode.AIVsAI;match.Configure();
            foreach(int sign in new[]{-1,1})foreach(float speed in new[]{2f,8f,16f})
            {
                yield return NewShot();int team=match.rules.Attacker(sign);
                match.puck.position=new Vector3(sign*(match.goalLine-1.5f),match.puck.position.y,0);match.puck.linearVelocity=Vector3.zero;Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
                match.puck.linearVelocity=Vector3.right*(sign*speed);float end=Time.realtimeSinceStartup+3;float greatest=float.NegativeInfinity;
                while(match.rules.Live&&Time.realtimeSinceStartup<end){greatest=Mathf.Max(greatest,match.puck.position.x*sign);yield return new WaitForFixedUpdate();}
                log.AppendLine("SHOT sign="+sign+" speed="+speed+" greatestCenter="+greatest+" required="+(match.goalLine+match.puckContact.radius)+" phase="+match.rules.Phase+" score="+string.Join(",",match.rules.Score));
                Check(match.rules.Goals==1&&match.rules.Score[team]==1&&match.rules.Score[1-team]==0,"physical center shot scores exactly once for correct attacker sign="+sign+" speed="+speed);
                Check(match.rules.Phase==HockeyPhase.Goal&&match.Notice.Contains("GOAL"),"goal celebration state and visible notice follow actual puck crossing");
                yield return new WaitForSeconds(.2f);Check(match.rules.Goals==1,"back-net contacts cannot duplicate the goal");
                if(sign==1&&speed==8)foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();CheckHud();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"physical-goal-hud-"+size.x+".png"));yield return new WaitForSecondsRealtime(.1f);}
                float until=Time.realtimeSinceStartup+6;while(!match.rules.Live&&Time.realtimeSinceStartup<until)yield return null;
                Check(match.rules.Live&&match.puck.position.sqrMagnitude<1,"goal resets the actual puck for the next faceoff");
            }
            yield return NewShot();match.puck.position=new Vector3(match.goalLine-1.5f,match.puck.position.y,match.goalHalfWidth+.6f);match.puck.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();match.puck.linearVelocity=Vector3.right*8;yield return new WaitForSeconds(.6f);
            Check(match.rules.Goals==0,"outside-of-net physical shot cannot score");
            match.ChangeSetup();Check(match.StartMatch(true)&&match.rules.Goals==0,"rematch resets goal authority and score");
            match.ChangeSetup();Finish();
        }
        IEnumerator NewShot()
        {
            match.ChangeSetup();Check(match.StartMatch(),"real saved Hockey match starts physical-shot fixture");
            foreach(var agent in match.agents)agent.enabled=false;
            while(!match.rules.Live)yield return null;
            for(int i=0;i<match.trucks.Length;i++){var truck=match.trucks[i];truck.ClearInput();truck.enabled=false;truck.body.isKinematic=true;truck.body.position=new Vector3(-6+i*2,truck.body.position.y,-match.halfWidth+1.5f);}
            Physics.SyncTransforms();
        }
        void CheckHud()
        {
            foreach(var label in new[]{match.menus.scoreText,match.menus.clockText,match.menus.messageText})
            {
                var corners=new Vector3[4];label.rectTransform.GetWorldCorners(corners);bool safe=label.gameObject.activeInHierarchy;
                foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(null,corner);safe&=point.x>=0&&point.y>=0&&point.x<=Screen.width&&point.y<=Screen.height;}
                Check(safe,"actual Hockey "+label.name+" remains visible and screen-safe during the action camera at"+Screen.width);
            }
        }
        void Update(){if(!done&&deadline>0&&Time.realtimeSinceStartup>deadline){Check(false,"physical-shot watchdog expired");Finish();}}
        void Finish(){if(done)return;done=true;log.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; physical saved-scene puck fixtures, not human match acceptance");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());Application.Quit(errors==0?0:1);}
        void OnDestroy(){Application.logMessageReceived-=Error;preferences?.Dispose();}
    }
}
#endif
