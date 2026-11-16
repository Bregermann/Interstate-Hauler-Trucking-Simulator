#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonOuterVistaNativeTests
{
    [TestCase(0f,11f),TestCase(80f,10.5f),TestCase(160f,10f),TestCase(400f,10f)]
    public void CurrentCoreCollarAppliesOnlyTheMeasuredHeightDeltaInsideTheExisting160Metres(float distance,float expected)
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomOuterVistaAuthoring, Assembly-CSharp-Editor",true);float actual=(float)type.GetMethod("CurrentCoreCollarHeight").Invoke(null,new object[]{10f,20f,21f,distance});Assert.That(actual,Is.EqualTo(expected).Within(.00001f));Assert.That((float)type.GetMethod("CurrentCoreCollarHeight").Invoke(null,new object[]{10f,20f,20f,distance}),Is.EqualTo(10f));
    }
    [Test]
    public void InvalidCurrentCoreCollarSamplesCannotAuthorizeTerrainChanges()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomOuterVistaAuthoring, Assembly-CSharp-Editor",true);foreach(var values in new[]{new[]{float.NaN,20f,21f,0f},new[]{10f,float.PositiveInfinity,21f,0f},new[]{10f,20f,float.NaN,0f},new[]{10f,20f,21f,-1f}}){var error=Assert.Throws<TargetInvocationException>(()=>type.GetMethod("CurrentCoreCollarHeight").Invoke(null,values.Cast<object>().ToArray()));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());}
    }
    [UnityTest,Timeout(1500000)]
    public IEnumerator TwelveRealVistaSceneryTilesPreserveInnerWorldAndMeetEveryMeasuredSeam()
    {
        string sourceTag=Arg("-tsDragonOuterSource","RoundedIntegrated14"),tag=Arg("-tsDragonOuterCandidate","OuterCoast01");
        string innerPath="Assets/TruckDragonKingdom/VisualCandidates/"+sourceTag+"/Shared Dragon Kingdom.prefab";
        string outputPath="Assets/TruckDragonKingdom/ReviewCandidates/"+tag+"/Outer Vista Band.prefab";
        Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(innerPath),Is.Not.Null,"Actual accepted inner candidate required; do not substitute a generated flat world.");
        var before=Snapshot(innerPath);var setup=EditorSceneManager.GetSceneManagerSetup();
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"OuterRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve user scenes before isolated Vista generation.");
        }
        // Capture setup after safely saving the test runner's ordinary blank scene.
        setup=EditorSceneManager.GetSceneManagerSetup();
        var isolation=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolation,generated+"OuterIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomOuterVistaAuthoring",false)).SingleOrDefault(t=>t!=null);
            Assert.That(type,Is.Not.Null,"Real installed Vista symbol/runtime/editor assembly required.");
            var create=type.GetMethod("CreateCandidate",BindingFlags.Public|BindingFlags.Static,null,new[]{typeof(string),typeof(string)},null);
            var task=(IEnumerator)create.Invoke(null,new object[]{sourceTag,tag});while(task.MoveNext())yield return task.Current;
            var receipt=type.GetProperty("LastReceipt").GetValue(null);object Field(string n)=>receipt.GetType().GetField(n).GetValue(receipt);
            Assert.That((string)Field("status"),Is.EqualTo("MEASURED_SCENERY_CANDIDATE_REQUIRES_RENDER_AND_WORLD_ATTACHMENT_REVIEW"));
            Assert.That((bool)Field("sourceBytesPreserved"),Is.True);Assert.That((bool)Field("originalInnerNotGenerated"),Is.True);Assert.That((bool)Field("temporaryObjectsRemoved"),Is.True);
            Assert.That((int)Field("heightCallbacks"),Is.EqualTo(12));Assert.That((int)Field("uniqueHeightTiles"),Is.EqualTo(12));Assert.That((int)Field("clonedTerrainGraphs"),Is.GreaterThan(1));
            Assert.That((string[])Field("validationIssues"),Is.Empty);Assert.That(((Array)Field("terrains")).Length,Is.EqualTo(12));
            var inner=AssetDatabase.LoadAssetAtPath<GameObject>(innerPath);var outer=AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);Assert.That(outer,Is.Not.Null);
            var seams=type.GetMethod("ValidateAgainstInner").Invoke(null,new object[]{inner,outer});object Seam(string n)=>seams.GetType().GetField(n).GetValue(seams);
            Assert.That((string[])Seam("issues"),Is.Empty);Assert.That((float)Seam("innerMaximumError"),Is.LessThanOrEqualTo(.04f));Assert.That((float)Seam("outerMaximumError"),Is.LessThanOrEqualTo(.04f));Assert.That((float)Seam("bedMaximumError"),Is.LessThanOrEqualTo(.04f));
            var exactCore=inner.GetComponentsInChildren<Terrain>(true);
            var explicitSeams=type.GetMethod("ValidateCoreAgainstOuter").Invoke(null,new object[]{exactCore,outer});
            Assert.That((string[])explicitSeams.GetType().GetField("issues").GetValue(explicitSeams),Is.Empty,"Explicit actual-four-core overload retains every seam gate for later16-terrain attachment.");
            Assert.That(outer.GetComponentsInChildren<Terrain>(true).Length,Is.EqualTo(12));Assert.That(outer.GetComponentsInChildren<TerrainCollider>(true).Length,Is.EqualTo(12));
            foreach(var tile in outer.GetComponentsInChildren<Terrain>(true))
            {
                Assert.That(tile.terrainData.terrainLayers.Length,Is.EqualTo(4));Assert.That(tile.GetComponent<TerrainCollider>().terrainData,Is.SameAs(tile.terrainData));
                Assert.That(tile.GetComponents<Component>().Any(c=>c!=null&&c.GetType().FullName=="Pinwheel.Vista.UnityTerrain.TerrainTile"),Is.False);
            }
            // A repeated explicit call must reject the existing candidate BEFORE any
            // graph generation or rewrite. Existing candidate bytes remain intact.
            var saved=Snapshot(outputPath);var repeat=(IEnumerator)create.Invoke(null,new object[]{sourceTag,tag});
            Assert.Throws<InvalidOperationException>(()=>repeat.MoveNext());AssertExact(saved,"Existing output/private dependency");
            AssertExact(before,"Original accepted inner world, roads, physics and vendor dependency");Assert.That(Terrain.activeTerrains,Is.Empty);
            string evidence=Arg("-tsDragonOutput","Logs/TruckSports/DragonKingdom");Directory.CreateDirectory(evidence);
            File.WriteAllText(Path.Combine(evidence,"outer-vista-native-verification.txt"),"Actual Vista height callbacks=12; original generation callbacks=0; source dependencies/meta exact="+before.Count+"; new Terrain+TerrainCollider=12; inner/shared/bed seams<=0.04m; repeat rejects existing candidate and preserves bytes. Scenery-only native geometry proof; final rendered/world integration acceptance remains open.\n");
        }
        finally
        {
            try{AssertExact(before,"Original inner world after test cleanup");}
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
    static Dictionary<string,string> Snapshot(string path)=>AssetDatabase.GetDependencies(path,true).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(Io(p))).Distinct().ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(Io(p))));
    static void AssertExact(Dictionary<string,string> snapshot,string label){foreach(var pair in snapshot)Assert.That(File.Exists(Io(pair.Key))&&Hash(File.ReadAllBytes(Io(pair.Key)))==pair.Value,Is.True,label+": "+pair.Key);}
    static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes));}
    static string Io(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
    static string Arg(string flag,string fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,flag);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
}
#endif
