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
using LWS.TruckArchaeology;
public sealed class ArchaeologyVendorProviderTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string n)=>Type.GetType(n+", Assembly-CSharp",true);
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    static RaycastHit Surface(object earth,Vector3 p){var a=new object[]{p,default(RaycastHit)};Assert.That((bool)earth.GetType().GetMethod("Surface").Invoke(earth,a),Is.True,"Actual vendor/terrain collider ray required");return (RaycastHit)a[1];}
    [UnityTest,Timeout(150000)]public IEnumerator SupportedDefaultFactoryKeepsRealReadyUrpExcavationColliderAndRematchWithoutBypass()
    {
        var prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ArchaeologyActualDefaultFactory"});var original=InputSystem.settings;var scoped=UnityEngine.Object.Instantiate(original);scoped.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;scoped.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=scoped;var isolate=new GameObject("Archaeology default provider input isolation");UnityEngine.Object.DontDestroyOnLoad(isolate);isolate.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));Behaviour match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckArchaeology/Scenes/TruckArchaeology.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Behaviour)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckArchaeology.ArchaeologyMatch"));var earth=Get(match,"earth");var system=(DiggerSystem)Get(earth,"system");var terrain=(Terrain)Get(earth,"terrain");var provider=system.PolygonizerProvider;
            Assert.That((bool)Get(earth,"Ready"),Is.True,"Actual vendor Awake must complete before normal original start, without a fixture Ready wait");Assert.That(system.IsInitialized,Is.True);Assert.That(provider,Is.Not.Null);Assert.That(provider.GetType().FullName,Is.EqualTo("LWS.TruckArchaeology.ArchaeologyDefaultPolygonizerProvider"));Assert.That(provider.NewPolygonizer(system).GetType(),Is.EqualTo(typeof(MarchingCubesPolygonizer)),"The exact vendor default algorithm, no custom polygonizer");Assert.That(provider.GetMaterials(),Is.Null,"Retain original normal URP material route rather than block materials");
            var material=terrain.materialTemplate;var meshMaterials=(Material[])system.Materials.Clone();Assert.That(material.shader.name,Is.EqualTo("Digger/Terrain/URP/Lit"));Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True,"Original real start button remains authoritative");match.enabled=false;
            var finds=((Array)Get(match,"artifacts")).Cast<Component>().ToArray();var roots=finds.Select(a=>(Rigidbody)Get(a,"body")).ToArray();Vector3 point=(Vector3)Get(finds[0],"seedPosition");var before=Surface(earth,point);
            for(int i=0;i<8;i++){var hit=Surface(earth,point);Assert.That((bool)Call(earth,"Cut",hit.point-Vector3.up*.35f,1.7f,1f),Is.True,"Real supported vendor API dig must execute");yield return null;yield return null;}
            Physics.SyncTransforms();var after=Surface(earth,point);Assert.That(after.point.y,Is.LessThan(before.point.y-1));Assert.That(after.collider.GetComponentInParent<DiggerSystem>(),Is.SameAs(system),"Actual rebuilt voxel collider must replace original surface at cut");Assert.That(system.GetComponentsInChildren<VoxelChunk>(true).Length,Is.GreaterThan(0));Assert.That((int)Get(earth,"ActualEdits"),Is.EqualTo(8));Assert.That(((ArchaeologyRules)Get(match,"Rules")).Delivered,Is.Zero);Assert.That(((ArchaeologyRules)Get(match,"Rules")).Scores.Sum(),Is.Zero);Assert.That(terrain.materialTemplate,Is.SameAs(material));CollectionAssert.AreEqual(meshMaterials,system.Materials);
            match.enabled=true;Call(match,"SetupScreen");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;yield return null;var reset=Surface(earth,point);Assert.That(reset.point.y,Is.EqualTo(before.point.y).Within(.02f));Assert.That((bool)Get(earth,"Ready"),Is.True);Assert.That(system.IsInitialized,Is.True);Assert.That(system.PolygonizerProvider,Is.SameAs(provider));Assert.That(terrain.materialTemplate,Is.SameAs(material));CollectionAssert.AreEqual(meshMaterials,system.Materials);CollectionAssert.AreEqual(roots,finds.Select(a=>(Rigidbody)Get(a,"body")).ToArray());Assert.That((int)Get(earth,"ActualEdits"),Is.Zero);Assert.That(((ArchaeologyRules)Get(match,"Rules")).Scores.Sum(),Is.Zero);
            TestContext.WriteLine("ARCHAEOLOGY_VENDOR_DEFAULT_FACTORY exact provider/normalURP/readiness; actual collider before="+before.point+" after="+after.point+" reset="+reset.point+"; no Ready/input/score override; disk persistence remains separate Windows-only acceptance");
        }
        finally{Time.timeScale=1;if(match!=null){match.enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolate);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(scoped);prefs.Dispose();}
    }
}
#endif
