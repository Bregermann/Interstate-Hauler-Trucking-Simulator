#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckMinefield
{
    public sealed class MinefieldSideRangeProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsMineRanges")<0)return;var root=new GameObject("Explicit Minefield landscape Windows acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<MinefieldSideRangeProbe>();}
        MinefieldMatch match;string folder;int checks,errors;IDisposable preferences;readonly StringBuilder log=new StringBuilder();
        void Check(bool value,string label){checks++;if(!value)errors++;log.AppendLine((value?"PASS ":"FAIL ")+label);File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+message+"\n"+stack);}}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.15f);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsMineRangesOutput");folder=output>=0?args[output+1]:SportsEvidencePaths.Folder("MinefieldSideRanges");Directory.CreateDirectory(folder);preferences=SportsPrefs.IsolateValidation("MinefieldRangesWindows");Application.logMessageReceived+=Error;
            yield return SportsProbeScene.Ensure<MinefieldMatch>("TruckMinefield");yield return null;match=FindAnyObjectByType<MinefieldMatch>();match.AllBots();match.weather=1;SportsCameraOptions.Set(false);var driving=match.GetComponent<SportsDrivingViews>();if(driving!=null)driving.Select(0);Check(match.StartMatch(),"saved scene starts ordinary eight-AI match");Time.timeScale=0;
            var sides=match.grounds.Where(t=>t.name=="West meadow"||t.name=="East meadow").ToArray();Check(sides.Length==2,"both authored terrain ranges exist");
            foreach(var side in sides)
            {
                Check(side.terrainData.treeInstanceCount==2500&&side.GetComponent<TerrainCollider>().terrainData==side.terrainData,"native2500-tree forest and matching real terrain collision "+side.name);
                Check(side.terrainData.GetHeights(0,0,side.terrainData.heightmapResolution,side.terrainData.heightmapResolution).Cast<float>().Max()*side.terrainData.size.y>110,"mountain peaks exceed110metres "+side.name);
            }
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.3f);yield return Capture("normal-shared-start-"+size.x);}
            var rig=match.views.broadcast;var frame=rig.broadcastFraming;bool framed=frame!=null&&frame.enabled;float far=rig.view.farClipPlane;bool enabled=match.enabled;
            try
            {
                match.enabled=false;if(frame!=null)frame.enabled=false;rig.view.farClipPlane=2500;
                rig.PoseRaw(new Vector3(0,35,600),new Vector3(0,180,-400),65,true);yield return new WaitForSecondsRealtime(.3f);yield return Capture("valley-landscape-camera-fixture-1920");
                var west=sides.First(t=>t.name=="West meadow");var point=new Vector3(-260,0,600);point.y=west.SampleHeight(point)+west.transform.position.y+9;rig.PoseRaw(point,new Vector3(70,35,-100),65,true);yield return new WaitForSecondsRealtime(.3f);yield return Capture("forest-camera-fixture-1920");
                Check(sides.All(t=>t.terrainData.treePrototypes[0].prefab!=null),"native forest source models remain assigned in the Windows player");
            }
            finally{rig.view.farClipPlane=far;if(frame!=null)frame.enabled=framed;match.enabled=enabled;}
            match.SetupScreen();Check(match.StartMatch(),"fresh normal AI round starts after camera-only fixtures");Time.timeScale=4;float until=Time.realtimeSinceStartup+180;bool airborne=false;
            while(!match.Finished&&match.HeatLocks==0&&Time.realtimeSinceStartup<until)
            {
                if(!airborne&&match.racers.Any(r=>r.Phase==MineAttempt.Launched)){airborne=true;yield return Capture("ordinary-ai-flight-1920");}yield return null;
            }
            Check(airborne,"ordinary AI physically triggers mines and flies without initialized launch fixtures");Check(match.Finished||match.HeatLocks>0,"ordinary physical AI round resolves");Check(match.racers.All(r=>r.LandingLocks<=1),"all attempts retain once-only rest locks");
            match.SetupScreen();Check(match.StartMatch(),"rematch starts");Check(sides.All(t=>t.terrainData.treeInstanceCount==2500),"rematch preserves both forests and authored terrain");match.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<MinefieldMatch>()==null,"menu return cleans landscape scene");
            log.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; actual Windows rendering/AI round plus explicitly labelled landscape cameras; no human/hardware/listening acceptance");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());Application.Quit(errors==0?0:1);
        }
        void OnDestroy(){Application.logMessageReceived-=Error;preferences?.Dispose();Time.timeScale=1;}
    }
}
#endif
