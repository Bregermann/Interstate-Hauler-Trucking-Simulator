#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonEggDelivery;
using LWS.TruckDragonEscape;
using LWS.TruckDragonRiders;
using LWS.TruckHoardHeist;
using LWS.TruckSports;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LWS.TruckDragonKingdom.Validation
{
    // Explicit development-player observer. Ordinary navigation, setup, AI,
    // NWH/Malbers simulation, score journals and match lifecycle remain owners.
    // No score/contact/animation callback, body pose or clock is injected.
    public sealed class DragonKingdomWindowsProbe : MonoBehaviour
    {
        const string Flag="-tsDragon08",Complete="-tsDragon08Complete",Output="-tsDragon08Output",Scenes="-tsDragon08Scenes";
        [Serializable] sealed class Visit
        {
            public string scene,matchId,results,stopReason;public bool finished,rematch;public int physicalOutcomes,scoreReceipts;public float realSeconds;
        }
        [Serializable] sealed class Receipt
        {
            public string scope="ACTUAL_WINDOWS_AI_OBSERVATION_NOT_HUMAN_DEVICE_LISTENING_OR_COMPLETE_VARIANT_ACCEPTANCE",version,buildGuid,cpu,gpu;
            public int pid,checks,failures,captures;public string platform;public bool completeRequested,splitAIViews;public Visit[] visits;public PerformanceFact[] performance;public string[] observations;
        }
        [Serializable] sealed class PerformanceFact {public string scene,weather;public int width,height,views;public SportsExpansionPerformance.Receipt sample;}
        readonly List<string> observations=new List<string>();readonly List<Visit> visits=new List<Visit>();
        readonly List<PerformanceFact> performance=new List<PerformanceFact>();
        string folder,current;int checks,failures,captures;bool complete,finished,splitAIViews,measurePerformance;IDisposable preferences;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)<0)return;
            if(Application.isEditor||Application.platform!=RuntimePlatform.WindowsPlayer){Debug.LogError("Dragon0.8 observer requires the actual Windows development player.");return;}
            var owner=new GameObject("Explicit Dragon0.8 Windows observer");DontDestroyOnLoad(owner);
            if(!SportsProbeInputIsolation.Active)owner.AddComponent<SportsProbeInputIsolation>();owner.AddComponent<DragonKingdomWindowsProbe>();
        }
        static string Argument(string key,string fallback=null)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);if(at<0)return fallback;
            if(at+1>=args.Length||args[at+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException("Value required for "+key);return args[at+1];
        }
        void Check(bool pass,string text){checks++;if(!pass)failures++;observations.Add((pass?"PASS ":"FAIL ")+current+" "+text);Write();}
        void LogError(string text,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;failures++;observations.Add("ERROR "+current+" "+text+"\n"+stack);Write();}
        void Write(){if(folder!=null)File.WriteAllText(Path.Combine(folder,"progress.txt"),string.Join("\n",observations));}
        IEnumerator Start()
        {
            folder=Path.GetFullPath(Argument(Output)??throw new ArgumentException("Provide a unique -tsDragon08Output folder."));Directory.CreateDirectory(folder);
            if(File.Exists(Path.Combine(folder,"result.json")))throw new IOException("Preserve prior Windows evidence; choose a new folder.");
            complete=Array.IndexOf(Environment.GetCommandLineArgs(),Complete)>=0;
            splitAIViews=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragon08AIViews")>=0;measurePerformance=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragon08Perf")>=0;
            preferences=SportsPrefs.IsolateValidation("Dragon08Windows");SportsParticipantRegistry.ForgetCache();SportsCameraOptions.Set(splitAIViews);Application.logMessageReceived+=LogError;
            string[] names=(Argument(Scenes)??string.Join(",",SportsDragonGames.All.Select(entry=>entry.scene))).Split(',');
            if(names.Length==0||names.Distinct().Count()!=names.Length||names.Any(name=>!SportsDragonGames.TryFindScene(name,out _)))throw new ArgumentException("Use distinct registered Dragon0.8 scene names.");
            foreach(string name in names)
            {
                current=name;Check(Application.CanStreamedLevelBeLoaded(name),"saved scene included in executable");if(!Application.CanStreamedLevelBeLoaded(name))continue;
                SportsNavigation.LoadSport(name);yield return null;yield return null;yield return new WaitForSecondsRealtime(.5f);
                var match=FindAnyObjectByType<SportsEventSession>();Check(match!=null&&SceneManager.GetActiveScene().name==name,"ordinary collection navigation opens correct saved mode");if(match==null)continue;
                var world=FindAnyObjectByType<DragonKingdomWorld>();Check(world!=null&&world.regions.Length==9,"actual nine-region shared world is instantiated");
                var logos=match.menus.pages[(int)SportsEventScreen.Setup].GetComponentsInChildren<Image>(true).Where(image=>image.name=="Original fantasy sport logo").ToArray();
                Check(logos.Length==1&&logos[0].sprite!=null&&logos[0].sprite.name==name,"assigned original sport logo loads in setup");yield return Capture("setup");
                match.setup.count=4;match.option=0;match.NormalizeSetup();match.AllBots();
                bool started=match.StartMatch();Check(started,"ordinary four-AI setup starts match");if(!started)continue;
                string firstId=match.MatchId;var visit=new Visit{scene=name,matchId=firstId};visits.Add(visit);float began=Time.realtimeSinceStartup;
                yield return new WaitForSecondsRealtime(8);yield return Capture("live");
                if(measurePerformance)yield return MeasurePerformance(match);
                Check(FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Count(listener=>listener.isActiveAndEnabled)==1,"one actual active audio listener");
                Check(match.cameras.Any(camera=>camera!=null&&camera.isActiveAndEnabled&&camera.targetTexture==null&&camera.rect.width>0&&camera.rect.height>0),"live camera renders to actual Windows backbuffer");
                if(match.Live)
                {
                    match.Pause();yield return new WaitForSecondsRealtime(.2f);Check(match.Paused&&!match.Live,"ordinary pause gates live match");yield return Capture("pause");
                    match.Resume();yield return null;Check(match.Live,"ordinary resume restores live match");
                }
                if(complete)
                {
                    float deadline=began+900,nextCapture=Time.realtimeSinceStartup+35;
                    while(!match.Finished&&Time.realtimeSinceStartup<deadline)
                    {
                        if(Time.realtimeSinceStartup>=nextCapture){observations.Add("STATE "+name+" "+match.Header+" | "+match.Status+" | "+match.Standings);Write();yield return Capture("progress-"+Mathf.FloorToInt(Time.realtimeSinceStartup-began));nextCapture=Time.realtimeSinceStartup+60;}
                        yield return new WaitForSecondsRealtime(.5f);
                    }
                    Check(match.Finished,"actual rules reach ordinary results within bounded real-time observation");
                    visit.physicalOutcomes=Outcomes(match);Check(visit.physicalOutcomes>0,"completed match includes a genuine physical delivery, extraction or gate-race finish");
                }
                visit.finished=match.Finished;visit.results=match.Finished?match.Results:match.Standings;visit.scoreReceipts=match.ScoreJournal?.Count??0;visit.realSeconds=Time.realtimeSinceStartup-began;
                visit.stopReason=match.Finished?"ORDINARY_RESULTS":complete?"OBSERVATION_DEADLINE":"SHORT_OPENING_ONLY";yield return Capture(match.Finished?"results":"opening-end");
                match.SetupScreen();visit.rematch=match.StartMatch()&&match.MatchId!=firstId;Check(visit.rematch,"ordinary rematch resets and creates a distinct match identity");yield return new WaitForSecondsRealtime(5);yield return Capture("rematch");match.SetupScreen();
                var oldScene=match.gameObject.scene;SportsNavigation.FrontEnd();yield return null;yield return null;yield return new WaitForSecondsRealtime(.2f);
                Check(FindAnyObjectByType<SportsFrontEnd>()!=null&&!oldScene.isLoaded,"returns to collection and unloads original sport scene");
            }
            current="collection";yield return Capture("return");Finish();
        }
        static int Outcomes(SportsEventSession match)
        {
            if(match is HoardHeistMatch heist)return heist.PhysicalDeliveries;if(match is DragonEggDeliveryMatch eggs)return eggs.PhysicalDeliveries;if(match is DragonEscapeMatch escape)return escape.ActualExtractions;
            if(match is DragonRidersMatch riders)return Enumerable.Range(0,riders.setup.count).Count(slot=>riders.rules.Finished(slot));return 0;
        }
        IEnumerator MeasurePerformance(SportsEventSession match)
        {
            foreach(int width in new[]{1280,1920})
            {
                if(!match.Live)yield break;
                int height=width==1280?720:1080;Screen.SetResolution(width,height,FullScreenMode.Windowed);for(int frame=0;frame<5;frame++)yield return null;
                var owner=new GameObject("Explicit Dragon normal-speed profiler window");var observer=owner.AddComponent<SportsExpansionPerformance>();
                string path=Path.Combine(folder,"Performance",current+"-"+width);observer.BeginWindow(match,path);float began=Time.realtimeSinceStartup,until=began+20;
                while(match.Live&&Time.realtimeSinceStartup<until)yield return null;
                var sample=observer.EndWindow();performance.Add(new PerformanceFact{scene=current,width=Screen.width,height=Screen.height,views=match.cameras.Count(camera=>camera!=null&&camera.isActiveAndEnabled&&camera.targetTexture==null),weather=match.presentationWeather!=null?match.presentationWeather.Description:"unavailable",sample=sample});
                Check(Screen.width==width&&Screen.height==height,"profiler window uses actual requested backbuffer");
                if(Time.realtimeSinceStartup-began>6)Check(sample.samples>0,"normal-speed Unity profiler window records live samples; unavailable GPU counters stay explicitly unavailable");
                Destroy(owner);yield return null;
            }
        }
        IEnumerator Capture(string state)
        {
            foreach(int width in new[]{1280,1920})
            {
                int height=width==1280?720:1080;string path=Path.Combine(folder,current+"-"+state+"-"+width+".png");yield return SportsWindowCapture.Save(width,height,path);
                bool actual=Screen.width==width&&Screen.height==height&&File.Exists(path);if(actual){var data=File.ReadAllBytes(path);actual=data.Length>24&&ReadBigEndian(data,16)==width&&ReadBigEndian(data,20)==height;}
                Check(actual,"actual backbuffer and PNG dimensions "+width+"x"+height+" "+state);if(actual)captures++;
            }
        }
        static int ReadBigEndian(byte[] data,int at)=>(data[at]<<24)|(data[at+1]<<16)|(data[at+2]<<8)|data[at+3];
        void Finish()
        {
            if(finished)return;finished=true;Application.logMessageReceived-=LogError;Time.timeScale=1;preferences?.Dispose();preferences=null;SportsParticipantRegistry.ForgetCache();
            File.WriteAllText(Path.Combine(folder,"result.json"),JsonUtility.ToJson(new Receipt{pid=System.Diagnostics.Process.GetCurrentProcess().Id,platform=Application.platform.ToString(),version=Application.version,buildGuid=Application.buildGUID,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,checks=checks,failures=failures,captures=captures,completeRequested=complete,splitAIViews=splitAIViews,visits=visits.ToArray(),performance=performance.ToArray(),observations=observations.ToArray()},true));
            Application.Quit(failures==0?0:2);
        }
        void OnDestroy(){Application.logMessageReceived-=LogError;preferences?.Dispose();}
    }
}
#endif
