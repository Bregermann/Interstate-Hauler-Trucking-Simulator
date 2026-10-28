#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Sources.Polygonizers;
using Digger.Modules.Runtime.Sources;
public sealed class ArchaeologyStartupDiagnostics
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object o,string n)=>!Present(o)?null:o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static bool Present(object value)=>value is UnityEngine.Object u?u!=null:value!=null;
    static string State(object value)=>value is Array a?"count="+a.Length+",nonnull="+a.Cast<object>().Count(Present):Present(value)?value.GetType().FullName:"NULL";
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    [UnityTest,Timeout(60000)]public IEnumerator ActualOriginalStartButtonReportsEveryVenueAndVendorInitializationGateWithoutWaitOrOverride()
    {
        var prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ArchaeologyStartupActual"});var original=InputSystem.settings;var scoped=UnityEngine.Object.Instantiate(original);scoped.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;scoped.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=scoped;var isolate=new GameObject("Archaeology startup diagnostic input isolation");UnityEngine.Object.DontDestroyOnLoad(isolate);isolate.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckArchaeology/Scenes/TruckArchaeology.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckArchaeology.ArchaeologyMatch"));Assert.That(match,Is.Not.Null);var earth=Get(match,"earth");var system=(DiggerSystem)Get(earth,"system");var runtime=(DiggerMasterRuntime)Get(earth,"runtime");
            var material=system!=null&&system.Terrain!=null?system.Terrain.materialTemplate:null;var shader=material!=null?material.shader:null;
            string gates="trucks="+State(Get(match,"trucks"))+" equipment="+State(Get(match,"equipment"))+" artifacts="+State(Get(match,"artifacts"))+" receptions="+State(Get(match,"receptions"))+" earth="+State(earth)+" ready="+Get(earth,"Ready")+" systemInitialized="+(system!=null&&system.IsInitialized)+" systemEnabled="+(system!=null&&system.enabled)+" runtimeEnabled="+(runtime!=null&&runtime.enabled)+" runtimeAsync="+(runtime!=null&&runtime.IsRunningAsync)+" runtimeRoster="+State(Get(runtime,"diggerSystems"))+" terrain="+State(system!=null?system.Terrain:null)+" master="+State(Get(system,"master"))+" chunks="+State(Get(system,"chunks"))+" cutter="+State(system!=null?system.Cutter:null)+" heights="+State(system!=null?system.HeightsFeeder:null)+" normals="+State(system!=null?system.NormalsFeeder:null)+" alphamaps="+State(system!=null?system.AlphamapsFeeder:null)+" dirtyChunks="+State(system!=null?system.ChunksToPersist:null)+" provider="+State(system!=null?system.PolygonizerProvider:null)+" providersInScene="+UnityEngine.Object.FindObjectsByType<APolygonizerProvider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length+" terrainShader="+(shader==null?"NULL":shader.name)+" prefix="+(system!=null?system.PersistenceSubPath:"NULL")+" vendorVersion="+DiggerSystem.DiggerVersion;
            Debug.Log("ARCHAEOLOGY_ACTUAL_START_PRE "+gates);
            Call(match,"AllBots");Exception error=null;bool started=false;try{started=(bool)Call(match,"StartMatch");}catch(TargetInvocationException exception){error=exception.InnerException;}
            Debug.Log("ARCHAEOLOGY_ACTUAL_START_POST started="+started+" error="+(error==null?"none":error.ToString())+" "+gates);
            Assert.That(error,Is.Null,"Actual original start button must initialize the genuine venue, not bypass Ready or wait away the failure. "+gates);Assert.That(started,Is.True);Assert.That((bool)Get(earth,"Ready"),Is.True);Assert.That(system.IsInitialized,Is.True);Assert.That(system.Cutter,Is.Not.Null);Assert.That(system.PolygonizerProvider,Is.Not.Null);Assert.That(runtime.IsRunningAsync,Is.False);
        }
        finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolate);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(scoped);prefs.Dispose();}
    }
}
#endif
