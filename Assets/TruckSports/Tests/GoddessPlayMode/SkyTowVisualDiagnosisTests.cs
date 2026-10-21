#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SkyTowVisualDiagnosisTests
{
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    [UnityTest]public IEnumerator FrozenPoseSeparatesGateEffectsModelsAndPostProcessing()
    {
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","Logs/TruckSports/PlaytestRepair/SkyTowNativeVisualDiagnosis"));Directory.CreateDirectory(folder);
        var preferences=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"SkyVisualDiagnosis"});
        var input=new GameObject("Explicit diagnostic input isolation");UnityEngine.Object.DontDestroyOnLoad(input);input.AddComponent(Runtime("SportsProbeInputIsolation"));
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSkyTow/Scenes/TruckSkyTow.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var type=Type.GetType("LWS.TruckSkyTow.SkyTowMatch, Assembly-CSharp",true);var match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
            var presets=type.GetField("presets").GetValue(match);foreach(var preset in (Array)presets.GetType().GetField("entries").GetValue(presets))if((string)preset.GetType().GetField("displayName").GetValue(preset)=="Clear")presets.GetType().GetMethod("Select").Invoke(presets,new[]{preset.GetType().GetField("id").GetValue(preset)});
            type.GetMethod("AllBots").Invoke(match,null);Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{true});Assert.That((bool)type.GetMethod("StartMatch").Invoke(match,null),Is.True);
            Time.timeScale=4;float deadline=Time.realtimeSinceStartup+30;while((float)type.GetProperty("Age").GetValue(match)<15&&Time.realtimeSinceStartup<deadline)yield return null;Time.timeScale=0;
            var cameras=(Camera[])type.GetField("cameras").GetValue(match);
            Action<string> capture=name=>CaptureHDR(cameras,Path.Combine(folder,name+".png"));
            capture("01-baseline");var feedback=(Component)type.GetField("gateFeedback").GetValue(match);foreach(var particle in feedback.GetComponentsInChildren<ParticleSystem>(true)){var renderer=particle.GetComponent<ParticleSystemRenderer>();TestContext.WriteLine("SNOW "+particle.name+" mesh="+(renderer.mesh!=null?renderer.mesh.bounds.ToString():"none")+" actualRendererBounds="+renderer.bounds+" size="+particle.main.startSize.constantMax+" materials="+string.Join(",",renderer.sharedMaterials.Select(m=>m.name+":"+m.shader.name)));}
            feedback.gameObject.SetActive(false);yield return null;capture("02-no-gate-effects");
            var course=type.GetField("course").GetValue(match);var rings=(Array)course.GetType().GetField("rings").GetValue(course);foreach(Component ring in rings)((Renderer)ring.GetType().GetField("art").GetValue(ring)).enabled=false;yield return null;capture("03-no-gate-models-or-effects");
            var dataType=Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime",true);foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)){var data=camera.GetComponent(dataType);if(data!=null)dataType.GetProperty("renderPostProcessing").SetValue(data,false);}yield return null;capture("04-no-post-gates-or-effects");
            type.GetMethod("SetupScreen").Invoke(match,null);
        }
        finally{Time.timeScale=1;UnityEngine.Object.Destroy(input);preferences.Dispose();}
    }
    static void CaptureHDR(Camera[] cameras,string path)
    {
        var previous=RenderTexture.active;var texture=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGBHalf);var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);
        try{foreach(var camera in cameras){if(!camera.enabled)continue;var target=camera.targetTexture;try{camera.targetTexture=texture;camera.Render();}finally{camera.targetTexture=target;}}RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(texture);UnityEngine.Object.Destroy(image);}
    }
}
#endif
