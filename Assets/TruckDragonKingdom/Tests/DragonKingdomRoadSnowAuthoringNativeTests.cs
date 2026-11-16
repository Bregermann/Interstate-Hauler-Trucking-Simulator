#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class DragonKingdomRoadSnowAuthoringNativeTests
{
    static readonly string[] Names={"TruckHoardHeist","TruckDragonEggDelivery","TruckDragonEscape","TruckDragonRiders"};
    static string Hash(string path){string full=Path.GetFullPath(path);if(Environment.OSVersion.Platform==PlatformID.Win32NT&&!full.StartsWith(@"\\?\",StringComparison.Ordinal))full=@"\\?\"+full;using(var sha=SHA256.Create())using(var input=File.OpenRead(full))return string.Concat(sha.ComputeHash(input).Select(value=>value.ToString("x2")));}
    static Dictionary<string,string> Outputs()=>Names.SelectMany(name=>Directory.GetFiles("Assets/"+name+"/Materials/Kingdom Road Snow","*",SearchOption.AllDirectories)).Concat(Names.SelectMany(name=>new[]{"Assets/"+name+"/Scenes/"+name+".unity","Assets/"+name+"/Scenes/"+name+".unity.meta"})).ToDictionary(path=>path,Hash);
    [Test]public void ExistingUnconfiguredSnowBridgeKeeps1024VendorMaskBudget()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);var owner=new GameObject("Temporary original snow-budget contract"){hideFlags=HideFlags.HideAndDontSave};
        try{var type=AppDomain.CurrentDomain.GetAssemblies().Select(assembly=>assembly.GetType("LWS.TruckTaxi.TruckTaxiSnowSurface",false)).Single(value=>value!=null);var surface=owner.AddComponent(type);Assert.That(new SerializedObject(surface).FindProperty("traceMaskResolution").intValue,Is.EqualTo(1024),"Existing modes retain the established default; only explicit new-mode saved configuration selects4096.");}
        finally{UnityEngine.Object.DestroyImmediate(owner);}
    }
    [Test,Timeout(240000)]
    public void SelectedRoadSnowVariantsPreserveCanonicalSourcesPhysicsIdentitiesAndRepeatedBytes()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);var author=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSnowAuthoring, Assembly-CSharp-Editor",true);var upgrade=author.GetMethod("UpgradeSavedRoadSnow");
        string[] originals={"Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab","Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab.meta","Assets/TruckDragonKingdom/Materials/Kingdom gravel road.mat","Assets/TruckDragonKingdom/Materials/Kingdom gravel road.mat.meta"};var before=originals.Concat(Directory.GetFiles("Assets/TruckDragonKingdom/Models","* EasyRoads mesh *.asset").SelectMany(path=>new[]{path,path+".meta"})).ToDictionary(path=>path,Hash);var guids=Names.ToDictionary(name=>name,name=>AssetDatabase.AssetPathToGUID("Assets/"+name+"/Scenes/"+name+".unity"));
        upgrade.Invoke(null,null);int surfaces=0;
        foreach(string name in Names)
        {
            string scenePath="Assets/"+name+"/Scenes/"+name+".unity";var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);var roads=(MeshRenderer[])author.GetMethod("Roads").Invoke(null,new object[]{scene});Assert.That(roads.Length,Is.GreaterThan(0));
            var snow=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Component>(true)).Single(item=>item!=null&&item.GetType().Name=="SportsSnowSurface");var surface=(Component)snow.GetType().GetField("surface").GetValue(snow);Assert.That(new SerializedObject(surface).FindProperty("traceMaskResolution").intValue,Is.EqualTo(4096),"Explicit supported vendor high-quality mask budget, never widened tire paths.");
            var adapter=(Component)snow.GetType().GetField("adapter").GetValue(snow);Assert.That(new SerializedObject(adapter).FindProperty("areaSizeMeters").floatValue,Is.EqualTo(3200),"Preserve full-world volume for four separated cameras.");
            foreach(var renderer in roads)foreach(var material in renderer.sharedMaterials)
            {
                var markers=renderer.GetComponents<Component>().Where(item=>item!=null&&item.GetType().FullName=="LWS.InterstateHauler.LwsRoadSurface").ToArray();Assert.That(markers.Length,Is.EqualTo(1),"Existing road/contact framework marker on exact baked geometry.");Assert.That(markers[0].GetType().GetProperty("SurfaceType").GetValue(markers[0]).ToString(),Is.EqualTo("Gravel"));
                Assert.That(material,Is.Not.Null);Assert.That(material.shader.name,Is.EqualTo("NOT_Lonely/Weatherade/Snow Coverage"));Assert.That(material.shader.isSupported,Is.True);string path=AssetDatabase.GetAssetPath(material);Assert.That(path,Does.StartWith("Assets/"+name+"/Materials/Kingdom Road Snow/"));
                string sourceGuid=Path.GetFileNameWithoutExtension(path);var source=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(sourceGuid));Assert.That(source,Is.Not.Null);Assert.That(material,Is.Not.SameAs(source));
                string map=source.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";Assert.That(material.GetTexture("_BaseMap"),Is.SameAs(source.GetTexture(map)));Assert.That(material.GetTextureScale("_BaseMap"),Is.EqualTo(source.GetTextureScale(map)));Assert.That(material.GetTextureOffset("_BaseMap"),Is.EqualTo(source.GetTextureOffset(map)));Assert.That(material.GetFloat("_CoverageAmount"),Is.Zero,"Saved variant starts clear; actual runtime owns accumulation.");surfaces++;
            }
            Assert.That(AssetDatabase.AssetPathToGUID(scenePath),Is.EqualTo(guids[name]));
        }
        var installed=Outputs();upgrade.Invoke(null,null);upgrade.Invoke(null,null);var after=Outputs();CollectionAssert.AreEquivalent(installed.Keys,after.Keys);foreach(var pair in installed)Assert.That(after[pair.Key],Is.EqualTo(pair.Value),"Repeated selected upgrade must preserve complete scene/variant/meta bytes: "+pair.Key);foreach(var pair in before)Assert.That(Hash(pair.Key),Is.EqualTo(pair.Value),"Original canonical source remains byte-exact.");
        TestContext.WriteLine("ROAD_SNOW_SELECTED_UPGRADE actualSurfaces="+surfaces+" scenes=4 originalSourcesExact=true authoritativeTransformsPhysicsMeshesAndIdsCheckedByMigration=true repeatBytesExact=true nativeVisibleAccumulationAndGPUProofStillRequired=true");
    }
}
#endif
