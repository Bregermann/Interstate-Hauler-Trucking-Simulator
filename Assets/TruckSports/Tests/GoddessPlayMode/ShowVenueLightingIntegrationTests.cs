#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ShowVenueLightingIntegrationTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any)?.GetValue(owner)??owner.GetType().GetProperty(name,Any).GetValue(owner);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    [UnityTest,Timeout(180000)] public IEnumerator NinjaShowFixturesIlluminateTheRealCourse()=>Verify("TruckNinjaWarrior");
    [UnityTest,Timeout(180000)] public IEnumerator QuarryFixturesIlluminateTheSwingAndLanding()=>Verify("TruckTrailerPlug");
    IEnumerator Verify(string name)
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ShowVenueAcceptance"+name});var isolation=new GameObject("Isolated show venue acceptance");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/"+name+"/Scenes/"+name+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession")).Cast<Component>().Single();Call(match,"AllBots");var clock=match.GetComponent(Runtime("SportsMatchTime"));Assert.That((Light[])Get(clock,"nightLights"),Is.Empty,"Original show lamps stay independent of ordinary night-only switching.");
            bool ninja=name=="TruckNinjaWarrior";var lamps=UnityEngine.Object.FindObjectsByType<Light>().Where(light=>light.gameObject.scene==match.gameObject.scene&&(light.name==(ninja?"Actual broad show floodlight":"Quarry event floodlight")||!ninja&&light.name=="Budgeted upper swing show floodlight")).ToArray();Assert.That(lamps.Length,Is.EqualTo(ninja?24:7));Assert.That(lamps.All(light=>light.shadows==LightShadows.None),Is.True);
            var root=clock.transform.Find("Supported original show venue lighting");Assert.That(root,Is.Not.Null);Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(root.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(root.GetComponentsInChildren<Joint>(true),Is.Empty);Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);
            if(!ninja)Assert.That(UnityEngine.Object.FindObjectsByType<Light>().Single(light=>light.name=="Sinkhole underground glow").intensity,Is.EqualTo(12),"Retain the original artistic sinkhole light.");
            if(!ninja){Assert.That(root.GetComponentsInChildren<Light>(true).Length,Is.EqualTo(1),"One upper fixture; no coincident duplicate.");Assert.That(lamps.Count(light=>Mathf.Approximately(light.intensity,18000)),Is.EqualTo(1),"Existing right-side fixture illuminates the initial suspended rig.");}
            var powers=lamps.Select(light=>light.intensity).ToArray();var ids=lamps.Select(light=>light.GetInstanceID()).ToArray();var catalog=Get(match.GetComponent(Runtime("SportsChaosWeather")),"presets");var entries=((Array)Get(catalog,"entries")).Cast<object>().ToArray();Call(catalog,"Select",Get(entries.Single(entry=>(string)Get(entry,"displayName")=="Clear"),"id"));
            foreach(string time in new[]{"Night","Day","Evening","Morning","Night"})
            {
                Call(match,"SetupScreen");Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null,new object[]{Enum.Parse(Runtime("SportsTimeChoice"),time)});Assert.That(Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(.8f);
                CollectionAssert.AreEqual(powers,lamps.Select(light=>light.intensity).ToArray());CollectionAssert.AreEqual(ids,lamps.Select(light=>light.GetInstanceID()).ToArray());Assert.That(lamps.All(light=>light.enabled),Is.True);Call(match,"Pause",0,false);yield return null;Call(match,"Resume");
            }
            var camera=((Camera[])Get(match,"cameras")).First(item=>item.enabled);
            if(!ninja)
            {
                Time.timeScale=0;var initial=Read(camera);foreach(var lamp in lamps)lamp.enabled=false;Color[] unlit;
                try{unlit=Read(camera);}finally{foreach(var lamp in lamps)lamp.enabled=true;}
                var point=camera.WorldToViewportPoint((Vector3)Get(Get(match,"rig"),"TrailerCenter"));int cx=Mathf.RoundToInt(point.x*320),cy=Mathf.RoundToInt(point.y*180),visible=0;
                for(int y=Mathf.Max(0,cy-6);y<=Mathf.Min(179,cy+6);y++)for(int x=Mathf.Max(0,cx-6);x<=Mathf.Min(319,cx+6);x++)if(initial[y*320+x].grayscale-unlit[y*320+x].grayscale>.025f)visible++;
                TestContext.WriteLine("Quarry initial trailer-center litPixels="+visible+"/169 viewport="+point);Capture("initial-clear-night");Assert.That(visible,Is.GreaterThan(30),"The original initial trailer face, not just the crane or landing ground, must receive actual light.");Time.timeScale=1;
            }
            yield return new WaitForSecondsRealtime(5);Time.timeScale=0;Read(camera);var lit=Read(camera);foreach(var lamp in lamps)lamp.enabled=false;Color[] dark;
            try{dark=Read(camera);}finally{foreach(var lamp in lamps)lamp.enabled=true;}
            int brighter=lit.Zip(dark,(a,b)=>a.maxColorComponent-b.maxColorComponent>.025f).Count(value=>value);TestContext.WriteLine(name+" savedFixtures="+lamps.Length+" actualLitPixels="+brighter+"/57600");Capture("clear-night");Assert.That(brighter,Is.GreaterThan(ninja?10000:3000),"Actual gameplay view must receive useful venue illumination.");Time.timeScale=1;
            var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;Assert.That(AssetDatabase.GetAssetPath(pipeline),Is.EqualTo("Assets/TruckSports/Rendering/"+name+" snow pipeline.asset"));Assert.That(Get(pipeline,"maxAdditionalLightsCount"),Is.EqualTo(8));var settings=new SerializedObject(pipeline);var renderers=settings.FindProperty("m_RendererDataList");Assert.That(renderers.arraySize,Is.EqualTo(2));var renderer=renderers.GetArrayElementAtIndex(0).objectReferenceValue;Assert.That(Get(renderer,"renderingMode").ToString(),Is.EqualTo("ForwardPlus"));Assert.That(new SerializedObject(renderer).FindProperty("m_RendererFeatures").arraySize,Is.EqualTo(1));Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(renderers.GetArrayElementAtIndex(1).objectReferenceValue)),Is.EqualTo("2de0078876aa96b4289a9cd6ccfc10de"));
            Call(match,"SetupScreen");Call(catalog,"Select",Get(entries.Single(entry=>(string)Get(entry,"displayName")=="Snow"),"id"));Assert.That(Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(6);Time.timeScale=0;Capture("snow-night");Time.timeScale=1;CollectionAssert.AreEqual(powers,lamps.Select(light=>light.intensity).ToArray());
            void Capture(string label){foreach(int width in new[]{1280,1920})Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,width,width==1280?720:1080,"Logs/TruckSports/PlaytestRepair/ShowVenueLightingNative/"+name+"-"+label+"-"+width+".png",null});}
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
