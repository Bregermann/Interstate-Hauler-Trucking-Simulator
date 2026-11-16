#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public sealed class DragonKingdomConnectedDryApronMaskTests
{
    const string Prefab="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static Type World=>Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true);
    static MethodInfo Method(string name)=>Integration.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);
    static object Read(object value,string field)=>value.GetType().GetField(field).GetValue(value);
    static string Io(string p)=>(string)Method("Io").Invoke(null,new object[]{p});
    static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    static string FileHash(string p)=>Hash(File.ReadAllBytes(Io(p)));
    static byte[] FloatBytes(float[,] values){var bytes=new byte[values.Length*4];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);return bytes;}
    static string Argument(string flag){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,flag);Assert.That(at>=0&&at+1<args.Length,Is.True,"Explicit actual V439 regression receipt required: "+flag);return args[at+1];}
    static object Receipt(){string path=Argument("-tsDragonDryApronSourceReceipt");Assert.That(Path.IsPathRooted(path)&&File.Exists(Io(path)),Is.True);var receipt=JsonUtility.FromJson(File.ReadAllText(Io(path)),Integration.GetNestedType("Receipt"));Assert.That((string)Read(receipt,"tag"),Is.EqualTo("OwnedRoadRockV439"));return receipt;}
    static object SourceWorld()=>AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponent(World);
    static object Context(object world)=>Method("BuildNativeConnectedDryApronSupports").Invoke(null,new[]{world});
    static bool Dry(string region,Vector3 p)=>(bool)Method("IntendedDrySettlementPosition").Invoke(null,new object[]{region,p});
    static float[,] Floats(string path,string expected,int n){var bytes=File.ReadAllBytes(Io(path));Assert.That(Hash(bytes),Is.EqualTo(expected));Assert.That(bytes.Length,Is.EqualTo(n*n*4));var values=new float[n,n];Buffer.BlockCopy(bytes,0,values,0,bytes.Length);return values;}
    [Test]
    public void ConnectedApronsRetainExistingSemanticDomainsAndBoundedInterpolationMetadata()
    {
        string original=FileHash(Prefab);var context=Context(SourceWorld());var proof=Read(context,"evidence");var rows=((Array)Read(proof,"districts")).Cast<object>().ToArray();Assert.That(rows.Length,Is.EqualTo(2));Assert.That(rows.Select(r=>(string)Read(r,"region")),Is.EquivalentTo(new[]{"village","harbor"}));Assert.That((float)Read(proof,"gridStep"),Is.EqualTo(3.125f));Assert.That((float)Read(proof,"featherMeters"),Is.EqualTo(12.5f));Assert.That((float)Read(proof,"retainingBank"),Is.EqualTo(12));
        foreach(var row in rows){Assert.That((int)Read(row,"semanticDryVertices"),Is.GreaterThan(0));Assert.That((int)Read(row,"basisVertices"),Is.GreaterThanOrEqualTo((int)Read(row,"semanticDryVertices")));Assert.That((int)Read(row,"hydrologySupportVertices"),Is.EqualTo(0),"Original water core is never relabelled dry");}
        Assert.That(((float[,])Read(context,"distance"))[1000,512],Is.EqualTo(float.PositiveInfinity),"This is a bounded district authority, not whole-world preservation");Assert.That(FileHash(Prefab),Is.EqualTo(original));TestContext.WriteLine("SEMANTIC_NATIVE_DRY_APRON_METADATA_NOT_VENDOR_ACCEPTANCE "+JsonUtility.ToJson(proof));
    }
    [Test]
    public void EveryOriginalNativeDryDistrictPointHasItsFourVertexBasisWithoutChangingWetClassification()
    {
        string original=FileHash(Prefab);var receipt=Receipt();var context=Context(SourceWorld());var basis=(byte[,])Read(context,"basis");var districts=((Array)Read(Read(receipt,"urbanBanks"),"districts")).Cast<object>().ToArray();int wet=0,dry=0;
        foreach(var district in districts)foreach(var sample in ((Array)Read(district,"points")).Cast<object>())
        {
            string region=(string)Read(district,"region");var p=(Vector3)Read(sample,"position");bool expected=(bool)Read(sample,"retainingDry");Assert.That(Dry(region,p),Is.EqualTo(expected),"Existing bank12/water/shoreline meaning must not change");if(!expected){wet++;continue;}dry++;int x=Mathf.FloorToInt((p.x+1600)/3.125f),z=Mathf.FloorToInt((p.z+800)/3.125f);for(int dz=0;dz<=1;dz++)for(int dx=0;dx<=1;dx++)Assert.That(basis[z+dz,x+dx],Is.Not.EqualTo(0),"Every actual sample's original interpolation basis must be exact: "+p.ToString("R"));
        }
        Assert.That(dry,Is.EqualTo(1816));Assert.That(wet,Is.EqualTo(330));Assert.That(FileHash(Prefab),Is.EqualTo(original));
    }
    [Test,Timeout(300000)]
    public void SharedMaskKeeps550MeasuredFootingsAndUsesDistinctApronAuthorityRoles()
    {
        string original=FileHash(Prefab);var receipt=Receipt();var relief=Read(receipt,"nativeRoadRelief");var footings=(Array)Read(relief,"footings");Assert.That(footings.Length,Is.EqualTo(550));string before=string.Join("\n",footings.Cast<object>().Select(JsonUtility.ToJson));var proof=Activator.CreateInstance(Integration.GetNestedType("NativeVistaPostGradeEvidence"));proof.GetType().GetField("individualArchitectureFootings").SetValue(proof,550);var mask=Method("BuildNativeVistaPhysicalMask").Invoke(null,new object[]{SourceWorld(),null,footings,proof});var roles=((Array)Read(proof,"physicalMaskAuthorities")).Cast<object>().ToArray();
        Assert.That(roles.Count(r=>(string)Read(r,"kind")=="EXPLICIT_SOURCE_PHYSICAL_ARCHITECTURE_FOOTING"),Is.EqualTo(550));Assert.That(roles.Count(r=>(string)Read(r,"kind")=="ORIGINAL_CONNECTED_DRY_STREETSCAPE_APRON"),Is.EqualTo(2));Assert.That(roles.Count(r=>(string)Read(r,"kind")=="ORIGINAL_CONNECTED_APRON_INTERPOLATION_BASIS"),Is.EqualTo(2));Assert.That((int)Read(proof,"individualArchitectureFootings"),Is.EqualTo(550));var aprons=Read(proof,"connectedDryApronSupport");Assert.That((int)Read(aprons,"nonzeroBasisWeights"),Is.EqualTo(0));Assert.That((int)Read(aprons,"newProtectedVertices"),Is.GreaterThan(0));Assert.That(string.Join("\n",footings.Cast<object>().Select(JsonUtility.ToJson)),Is.EqualTo(before));Assert.That(FileHash(Prefab),Is.EqualTo(original));Assert.That(Read(mask,"connectedAprons"),Is.Not.Null);TestContext.WriteLine("ONE_SHARED_MASK_EXACT_SOURCE_FOOTINGS550_PLUS_SEPARATE_DRY_APRON_ROLE "+JsonUtility.ToJson(aprons));
    }
    [TestCase("village",0,144)][TestCase("harbor",1,139)]
    [Timeout(300000)]
    public void ActualNativeTriangleInterpolationReproducesFeedbackFloodingAndProvesInputPreservingSupport(string region,int tileX,int expectedFailures)
    {
        // This is an ACTUAL Terrain.SampleHeight regression over source-closed
        // V439 buffers and a clearly labelled mask-support counterfactual.
        // No fresh vendor/GPU/world-repair success is claimed by recomposition.
        string sourceBefore=FileHash(Prefab);var receipt=Receipt();var recipe=Read(receipt,"currentFinalTerrainRecipe");Assert.That((bool)Read(recipe,"exactNativePhysicalCores"),Is.True);Assert.That((bool)Read(recipe,"originalSourcesExact"),Is.True);Assert.That((int)Read(recipe,"physicalFootings"),Is.EqualTo(550));var thermal=Read(recipe,"thermal");string inputPath=(string)Read(thermal,"observedInputFloatPath"),inputSha=(string)Read(thermal,"observedInputMosaicSha256");Assert.That(inputSha,Is.EqualTo("fded1f63ca3c9467b809604384c88c70f5a01805028b55f8705dbe53db6f5b0d"));var input=Floats(inputPath,inputSha,1025);var field=((Array)Read(recipe,"fields")).Cast<object>().Single(f=>Path.GetFileNameWithoutExtension((string)Read(f,"terrain"))=="Kingdom "+tileX+" 0");string finalPath=Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName((string)Read(recipe,"maskPath"))),"Kingdom "+tileX+" 0.actual-native.float32"),finalSha=(string)Read(field,"actualFeedbackSha256");var final=Floats(finalPath,finalSha,513);var context=Context(SourceWorld());var basis=(byte[,])Read(context,"basis");var district=((Array)Read(Read(receipt,"urbanBanks"),"districts")).Cast<object>().Single(d=>(string)Read(d,"region")==region);var samples=((Array)Read(district,"points")).Cast<object>().Where(p=>(bool)Read(p,"retainingDry")).Select(p=>(Vector3)Read(p,"position")).ToArray();
        var active=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewPreviewScene();TerrainData data=null;GameObject go=null;
        try
        {
            data=new TerrainData{heightmapResolution=513,size=new Vector3(1600,600,1600)};go=Terrain.CreateTerrainGameObject(data);SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=new Vector3(-1600+tileX*1600,-20,-800);var terrain=go.GetComponent<Terrain>();data.SetHeights(0,0,final);terrain.Flush();Assert.That(Hash(FloatBytes(data.GetHeights(0,0,513,513))),Is.EqualTo(finalSha),"Actual native archived final input must reproduce byte-exactly before measuring its failure");float Sample(Vector3 p)=>terrain.SampleHeight(p)+terrain.transform.position.y;int actualFailures=samples.Count(p=>Sample(p)<-.5f);Assert.That(actualFailures,Is.EqualTo(expectedFailures),"Reproduce actual V439 feedback failures using Unity's real triangle interpolation");
            var recomposed=(float[,])final.Clone();for(int z=0;z<513;z++)for(int x=0;x<513;x++)if(basis[z,x+tileX*512]!=0)recomposed[z,x]=input[z,x+tileX*512];data.SetHeights(0,0,recomposed);terrain.Flush();var actual=data.GetHeights(0,0,513,513);int changedProtected=0;for(int z=0;z<513;z++)for(int x=0;x<513;x++)if(basis[z,x+tileX*512]!=0&&BitConverter.SingleToInt32Bits(actual[z,x])!=BitConverter.SingleToInt32Bits(input[z,x+tileX*512]))changedProtected++;Assert.That(changedProtected,Is.EqualTo(0));Assert.That(samples.Count(p=>Sample(p)<-.5f),Is.EqualTo(0));Assert.That(FileHash(inputPath),Is.EqualTo(inputSha));Assert.That(FileHash(finalPath),Is.EqualTo(finalSha));Assert.That(FileHash(Prefab),Is.EqualTo(sourceBefore));TestContext.WriteLine("ACTUAL_NATIVE_V439_"+region+"_FAILURES_"+actualFailures+"_THEN_INPUT_PRESERVING_BASIS_COUNTERFACTUAL_ZERO_FAILURES_NOT_VENDOR_OR_FULL_WORLD_ACCEPTANCE");
        }
        finally{if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(data!=null)UnityEngine.Object.DestroyImmediate(data);if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(active));}
    }
}
#endif
