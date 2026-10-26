#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SnowStartupExposureDiagnosticsTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any)?.GetValue(owner)??owner.GetType().GetProperty(name,Any).GetValue(owner);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    [Explicit("Unsaved direct-snow startup/depth-refresh diagnosis, not acceptance.")]
    [UnityTest,Timeout(180000)] public IEnumerator CompareOriginalCameraBeforeAndAfterVendorDepthRefresh()
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"SnowStartupDepthAudit"});var isolation=new GameObject("Isolated snow startup audit");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;var report=new StringBuilder();
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSnake/Scenes/TruckSnake.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession")).Cast<Component>().Single();Call(match,"AllBots");var catalog=Get(match.GetComponent(Runtime("SportsChaosWeather")),"presets");var entries=((Array)Get(catalog,"entries")).Cast<object>().ToArray();Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null,new object[]{Enum.Parse(Runtime("SportsTimeChoice"),"Day")});
            var bridge=match.GetComponent(Runtime("SportsSnowSurface"));var adapter=Get(bridge,"adapter");
            foreach(string forecast in new[]{"Snow","Clear","Snow"})
            {
                Call(match,"SetupScreen");Call(catalog,"Select",Get(entries.Single(entry=>(string)Get(entry,"displayName")==forecast),"id"));Assert.That(Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(8);
                if(forecast=="Clear")continue;var coverage=(Component)Get(adapter,"_coverageComponent");var camera=((Camera[])Get(match,"cameras")).First(item=>item.enabled);Time.timeScale=0;
                Snapshot("before-depth-refresh");Call(coverage,"UpdateDepth");Snapshot("after-depth-refresh");Time.timeScale=1;
                void Snapshot(string label)
                {
                    var materials=((IDictionary)Get(bridge,"runtimeRoads")).Values.Cast<Material>().ToArray();var values=materials.Select(material=>material.GetFloat("_CoverageAmount")).ToArray();var applied=Read(camera);Color[] bare;
                    try{foreach(var material in materials)material.SetFloat("_CoverageAmount",0);bare=Read(camera);}
                    finally{for(int i=0;i<materials.Length;i++)materials[i].SetFloat("_CoverageAmount",values[i]);}
                    int pixels=applied.Zip(bare,(a,b)=>a.grayscale-b.grayscale>.04f).Count(value=>value);var depth=(Camera)Get(coverage,"sceneDepthCam");report.AppendLine("match="+Get(match,"MatchId")+" phase="+label+" coverage="+Get(bridge,"Coverage")+" roadPixels="+pixels+" camera="+camera.transform.position+" depth="+depth.transform.position+" depthSize="+depth.orthographicSize*2+" follow="+((Transform)Get(coverage,"followTarget")).position);
                    Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,"Logs/TruckSports/PlaytestRepair/SnowStartupDepthAudit/"+Get(match,"MatchId")+"-"+label+".png",null});
                }
            }
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair/SnowStartupDepthAudit");File.WriteAllText("Logs/TruckSports/PlaytestRepair/SnowStartupDepthAudit/observations.txt",report.ToString());TestContext.WriteLine(report.ToString());
        }
        finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
    }
    static Color[] Read(Camera camera)
    {
        var target=RenderTexture.GetTemporary(320,180,24,RenderTextureFormat.ARGB32);var prior=camera.targetTexture;var active=RenderTexture.active;Texture2D image=null;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image=new Texture2D(320,180,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,320,180),0,0);image.Apply();return image.GetPixels();}
        finally{camera.targetTexture=prior;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
    }
}
#endif
