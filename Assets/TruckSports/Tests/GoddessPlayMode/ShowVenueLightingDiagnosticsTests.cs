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

// Inspect the already-authored show/quarry fixtures before considering any asset edits.
public sealed class ShowVenueLightingDiagnosticsTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner,string name) => owner.GetType().GetField(name,Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name,Any).GetValue(owner);
    static object Call(object owner,string name,params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name==name && method.GetParameters().Length==args.Length).Invoke(owner,args);
    [Explicit("Unsaved lighting diagnosis; pixel changes are not full venue acceptance.")]
    [UnityTest,Timeout(180000)]
    public IEnumerator CompareExistingNinjaAndQuarryFixturePower()
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ShowVenueLightAudit"});
        var isolation=new GameObject("Isolated show-venue lighting audit");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match=null;var report=new StringBuilder();
        try
        {
            foreach(string name in new[]{"TruckNinjaWarrior","TruckTrailerPlug"})
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/"+name+"/Scenes/"+name+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession")).Cast<Component>().Single();Call(match,"AllBots");
                var catalog=Get(match.GetComponent(Runtime("SportsChaosWeather")),"presets");var clear=((Array)Get(catalog,"entries")).Cast<object>().Single(entry=>(string)Get(entry,"displayName")=="Clear");Call(catalog,"Select",Get(clear,"id"));
                Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null,new object[]{Enum.Parse(Runtime("SportsTimeChoice"),"Night")});Assert.That(Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(3);
                var lamps=UnityEngine.Object.FindObjectsByType<Light>().Where(light=>light.gameObject.scene==match.gameObject.scene && light.name==(name=="TruckNinjaWarrior"?"Actual broad show floodlight":"Quarry event floodlight")).ToArray();Assert.That(lamps.Length,Is.EqualTo(name=="TruckNinjaWarrior"?24:6));
                var powers=lamps.Select(light=>light.intensity).ToArray();var enabled=lamps.Select(light=>light.enabled).ToArray();var camera=((Camera[])Get(match,"cameras")).First(item=>item.enabled);Time.timeScale=0;
                try
                {
                    var current=Read(camera);foreach(var lamp in lamps)lamp.enabled=false;var dark=Read(camera);for(int i=0;i<lamps.Length;i++)lamps[i].enabled=enabled[i];
                    int originalPixels=current.Zip(dark,(a,b)=>a.maxColorComponent-b.maxColorComponent>.025f).Count(value=>value);Capture("original");
                    float gain=name=="TruckNinjaWarrior"?100:700;for(int i=0;i<lamps.Length;i++)lamps[i].intensity=powers[i]*gain;var boosted=Read(camera);Capture("unsaved-power-diagnostic");int boostedPixels=boosted.Zip(dark,(a,b)=>a.maxColorComponent-b.maxColorComponent>.025f).Count(value=>value);
                    var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;report.AppendLine(name+" powers="+string.Join(",",powers)+" enabled="+string.Join(",",enabled)+" originalLitPixels="+originalPixels+" transientGain="+gain+" diagnosticLitPixels="+boostedPixels+" maxAdditionalLights="+Get(pipeline,"maxAdditionalLightsCount"));
                }
                finally{for(int i=0;i<lamps.Length;i++){lamps[i].intensity=powers[i];lamps[i].enabled=enabled[i];}Time.timeScale=1;}
                void Capture(string label)
                {
                    Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,"Logs/TruckSports/PlaytestRepair/ShowVenueLightAudit/"+name+"-"+label+".png",null});
                }
                Call(match,"SetupScreen");match=null;
            }
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair/ShowVenueLightAudit");File.WriteAllText("Logs/TruckSports/PlaytestRepair/ShowVenueLightAudit/observations.txt",report.ToString());TestContext.WriteLine(report.ToString());
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
