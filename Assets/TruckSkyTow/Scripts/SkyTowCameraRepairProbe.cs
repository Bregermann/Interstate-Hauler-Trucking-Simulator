#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
using UnityEngine.Rendering.Universal;

namespace LWS.TruckSkyTow
{
    public sealed class SkyTowCameraRepairProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsSkyTowCameraRepair")<0)return;var go=new GameObject("Explicit Sky Tow camera/flight regression");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<SkyTowCameraRepairProbe>();}
        SkyTowMatch match;string folder;IDisposable preferences;int checks,errors,samples,duplicates,outside;bool done;float deadline,peakAngularRate;readonly StringBuilder log=new StringBuilder();
        readonly Quaternion[] previous=new Quaternion[4];readonly bool[] have=new bool[4];
        void Check(bool value,string message){checks++;if(!value)errors++;log.AppendLine((value?"PASS ":"FAIL ")+message);File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Error(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;log.AppendLine("ERROR "+message+"\n"+stack);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsSkyTowOutput");folder=output>=0?args[output+1]:SportsEvidencePaths.Folder("PlaytestRepair/SkyTowCamera");Directory.CreateDirectory(folder);
            preferences=SportsPrefs.IsolateValidation("SkyTowCameraRepair");Application.logMessageReceived+=Error;deadline=Time.realtimeSinceStartup+180;
            yield return SportsProbeScene.Ensure<SkyTowMatch>(SportsNavigation.SkyTow);yield return null;match=FindAnyObjectByType<SkyTowMatch>();match.setup.count=4;match.AllBots();
            foreach(var player in match.setup.players)player.difficulty=BowlingDifficulty.Normal;
            foreach(var preset in match.presets.entries)if(preset.displayName=="Clear"){match.presets.Select(preset.id);break;}
            SportsCameraOptions.Set(true);Check(match.StartMatch(),"real four-tow scene starts with four optional AI cameras");Time.timeScale=2;
            bool early=false,turn=false;
            while(!match.Finished)
            {
                if(!early&&match.Age>15){early=true;yield return Capture("early-turn");if(Array.IndexOf(args,"-tsSkyTowVisualDiagnosis")>=0){yield return VisualDiagnosis();match.SetupScreen();Finish();yield break;}}
                if(!turn&&match.Age>70){turn=true;yield return Capture("later-turn");}
                yield return null;
            }
            Time.timeScale=1;
            Check(match.rules.Complete,"complete physical four-truck Arctic run reaches results");
            Check(samples>200&&duplicates==0,"each rendered Sky Tow camera has exactly one pose evaluation per frame samples="+samples+" duplicates="+duplicates);
            Check(outside<samples*.05f,"actual truck centers remain inside their own camera during flight outside="+outside);
            Check(peakAngularRate<240,"no abrupt camera orientation flips; observed peak degrees/real-second="+peakAngularRate);
            for(int slot=0;slot<4;slot++)log.AppendLine("FLIGHT slot="+slot+" hits="+match.rules.hits[slot]+" score="+match.rules.score[slot]+" failed="+match.racers[slot].Failed+" maximumCable="+match.racers[slot].FurthestCable);
            if(match.course.playtestRepairVersion>=1)for(int slot=0;slot<4;slot++){int ordinary=0,hit=0;for(int i=0;i<36;i++)if(!match.course.Ring(slot,i).bonus){ordinary++;if(match.rules.outcomes[slot,i]>0)hit++;}Check(!match.racers[slot].Failed&&hit>=ordinary*.7f,"actual ordinary gates are regularly reachable on the full turning tow route slot="+slot+" hit="+hit+"/"+ordinary);}
            match.SetupScreen();SportsCameraOptions.Set(false);Check(match.StartMatch()&&match.cameras.Length==1&&match.cameras[0].enabled,"AI-only rematch restores one functional spectator view");match.SetupScreen();Finish();
        }
        IEnumerator Capture(string stage){float scale=Time.timeScale;Time.timeScale=0;foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,stage+"-"+size.x+".png"));yield return new WaitForSecondsRealtime(.1f);}Time.timeScale=scale;}
        IEnumerator VisualDiagnosis(){Time.timeScale=0;match.gateFeedback.gameObject.SetActive(false);yield return Capture("diagnostic-no-gate-fx");foreach(var ring in match.course.rings)ring.art.enabled=false;yield return Capture("diagnostic-no-gate-fx-or-models");foreach(var camera in match.cameras)camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;yield return Capture("diagnostic-no-gates-or-post");Check(true,"explicit frozen Windows visual-isolation captures only; no full-flight acceptance claimed");}
        void LateUpdate()
        {
            if(match==null||!match.Live||match.Phase!=SkyTowPhase.Run||match.Age<2)return;
            for(int view=0;view<match.views.Humans;view++)
            {
                var rig=match.views.views[view];if(!rig.view.enabled)continue;samples++;if(rig.FrameEvaluations!=1)duplicates++;
                int slot=match.views.owners[view];var point=rig.view.WorldToViewportPoint(match.racers[slot].truck.shape.bounds.center);if(point.z<=0||point.x<.02f||point.x>.98f||point.y<.02f||point.y>.98f)outside++;
                if(have[view]&&Time.unscaledDeltaTime>.0001f)peakAngularRate=Mathf.Max(peakAngularRate,Quaternion.Angle(previous[view],rig.transform.rotation)/Time.unscaledDeltaTime);previous[view]=rig.transform.rotation;have[view]=true;
            }
        }
        void Update(){if(!done&&deadline>0&&Time.realtimeSinceStartup>deadline){Check(false,"Sky Tow camera/flight watchdog expired");Finish();}}
        void Finish(){if(done)return;done=true;log.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; actual four-tow flight at stated accelerated simulation; no human/controller/listening or GPU benchmark acceptance");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());Application.Quit(errors==0?0:1);}
        void OnDestroy(){Time.timeScale=1;Application.logMessageReceived-=Error;preferences?.Dispose();}
    }
}
#endif
