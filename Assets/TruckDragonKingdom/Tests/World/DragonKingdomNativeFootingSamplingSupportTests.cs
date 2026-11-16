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
public sealed class DragonKingdomNativeFootingSamplingSupportTests
{
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static MethodInfo Method(string name)=>Integration.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);
    static object Read(object value,string field)=>value.GetType().GetField(field).GetValue(value);
    static object Footing(string id,Bounds bounds,float y)
    {var row=Activator.CreateInstance(Integration.GetNestedType("NativeArchitectureFooting"));row.GetType().GetField("id").SetValue(row,id);row.GetType().GetField("physicalFootprint").SetValue(row,bounds);row.GetType().GetField("surfaceY").SetValue(row,y);return row;}
    static Array Footings(params object[] rows){var array=Array.CreateInstance(Integration.GetNestedType("NativeArchitectureFooting"),rows.Length);for(int i=0;i<rows.Length;i++)array.SetValue(rows[i],i);return array;}
    static Bounds Support(Bounds physical)=>(Bounds)Method("NativeFootingTerrainSamplingBounds").Invoke(null,new object[]{physical});
    static Bounds ActualFirstBase=>new Bounds(new Vector3(-661.3426513671875f,0,-446.7654113769531f),new Vector3(6.47265625f,.16f,6.26019287109375f));
    [Test]
    public void MeasuredFirstHouseUsesMinimalTriangleBasisWithoutChangingItsPhysicalFootprint()
    {
        var physical=ActualFirstBase;var original=physical;var support=Support(physical);Assert.That(support.min.x,Is.EqualTo(-665.625f));Assert.That(support.max.x,Is.EqualTo(-656.25f));Assert.That(support.min.z,Is.EqualTo(-450f));Assert.That(support.max.z,Is.EqualTo(-440.625f));Assert.That(physical,Is.EqualTo(original));Assert.That(support.center.y,Is.EqualTo(physical.center.y));Assert.That(support.size.y,Is.EqualTo(physical.size.y));Assert.That(support.min.x,Is.LessThan(physical.min.x));Assert.That(support.max.x,Is.GreaterThan(physical.max.x));
        var row=Footing("original-measured-source",physical,0);string before=JsonUtility.ToJson(row);var context=Method("BuildNativeFootingSamplingSupports").Invoke(null,new object[]{Footings(row)});var proof=Read(context,"evidence");var rows=((Array)Read(proof,"rows")).Cast<object>().ToArray();Assert.That(rows.Length,Is.EqualTo(1));Assert.That((int)Read(rows[0],"supportVertices"),Is.EqualTo(16));Assert.That((int)Read(rows[0],"physicalCoreVertices"),Is.EqualTo(4));Assert.That((int)Read(rows[0],"additionalSamplingVertices"),Is.EqualTo(12));Assert.That(Read(rows[0],"originalPhysicalFootprint"),Is.EqualTo(physical));Assert.That(JsonUtility.ToJson(row),Is.EqualTo(before),"Never expand or rewrite the source geometry record to hide interpolation support");
    }
    [Test]
    public void GridAlignedFootprintsGainNoUnnecessaryExtraLatticeRing()
    {var physical=new Bounds(new Vector3(-660.9375f,60,-445.3125f),new Vector3(9.375f,.16f,9.375f));Assert.That(Support(physical),Is.EqualTo(physical));}
    [TestCase(-1601,0,0)][TestCase(1601,0,0)][TestCase(0,0,-801)][TestCase(0,0,2401)]
    public void UnsupportedFieldFramesNeverSilentlyClampAFooting(float x,float y,float z)
    {var error=Assert.Throws<TargetInvocationException>(()=>Support(new Bounds(new Vector3(x,y,z),Vector3.one)));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());}
    [Test]
    public void DistinctTinyPhysicalFootingsWithConflictingSharedSamplingTargetsAreRejected()
    {
        var a=Footing("first",new Bounds(new Vector3(100.1f,0,100.1f),new Vector3(.2f,.16f,.2f)),0);var b=Footing("second",new Bounds(new Vector3(101,20,101),new Vector3(.2f,.16f,.2f)),20);Assert.That(((Bounds)Read(a,"physicalFootprint")).Intersects((Bounds)Read(b,"physicalFootprint")),Is.False);var failure=Assert.Throws<TargetInvocationException>(()=>Method("BuildNativeFootingSamplingSupports").Invoke(null,new object[]{Footings(a,b)}));Assert.That(failure.InnerException.Message,Does.Contain("Incompatible original footing"));Assert.That((float)Read(a,"surfaceY"),Is.EqualTo(0));Assert.That((float)Read(b,"surfaceY"),Is.EqualTo(20));
    }
    static string Argument(string flag)
    {var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,flag);Assert.That(at>=0&&at+1<args.Length,Is.True,"Explicit actual V428 evidence required: "+flag);Assert.That(Path.IsPathRooted(args[at+1])&&File.Exists(args[at+1]),Is.True);return args[at+1];}
    static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    [Test,Timeout(180000)]
    public void ActualV428NativeTriangleContactReproducesFailureAndRequiresOutsideMeshSupportVertices()
    {
        string receiptPath=Argument("-tsDragonFootingSamplingSourceReceipt"),floatPath=Argument("-tsDragonFootingSamplingNativeField");var receipt=JsonUtility.FromJson(File.ReadAllText(receiptPath),Integration.GetNestedType("Receipt"));var originalFootings=(Array)Read(Read(receipt,"nativeRoadRelief"),"footings");Assert.That(originalFootings.Length,Is.EqualTo(550));string sourceRowsBefore=string.Join("\n",originalFootings.Cast<object>().Select(JsonUtility.ToJson));var context=Method("BuildNativeFootingSamplingSupports").Invoke(null,new object[]{originalFootings});var envelope=Read(context,"evidence");Assert.That((int)Read(envelope,"uniqueSupportVertices"),Is.EqualTo(4160));Assert.That((int)Read(envelope,"uniqueAdditionalVertices"),Is.EqualTo(2851));Assert.That((int)Read(envelope,"differentTargetConflicts"),Is.EqualTo(0));Assert.That(string.Join("\n",originalFootings.Cast<object>().Select(JsonUtility.ToJson)),Is.EqualTo(sourceRowsBefore));
        var first=((Array)Read(Read(receipt,"retainedNativeBuildings"),"parts")).Cast<object>().Single(p=>(string)Read(p,"footingId")=="retained-native-building:village:4:P_BLD_house_01");var field=((Array)Read(Read(receipt,"currentFinalTerrainRecipe"),"fields")).Cast<object>().Single(f=>((string)Read(f,"terrain")).EndsWith("/Kingdom 0 0.asset",StringComparison.Ordinal));var bytes=File.ReadAllBytes(floatPath);Assert.That(bytes.Length,Is.EqualTo(513*513*4));Assert.That(Hash(bytes),Is.EqualTo((string)Read(field,"actualFeedbackSha256")));var values=new float[513,513];Buffer.BlockCopy(bytes,0,values,0,bytes.Length);var physical=(Bounds)Read(first,"measuredBaseFootprint");var support=Support(physical);var active=SceneManager.GetActiveScene();Scene scene=default;GameObject go=null;TerrainData data=null;
        try
        {
            scene=EditorSceneManager.NewPreviewScene();go=new GameObject("Temporary actual V428 footing interpolation proof"){hideFlags=HideFlags.HideAndDontSave};go.SetActive(false);SceneManager.MoveGameObjectToScene(go,scene);data=new TerrainData{heightmapResolution=513,size=new Vector3(1600,600,1600),hideFlags=HideFlags.HideAndDontSave};data.SetHeights(0,0,values);Assert.That(Hash(FloatBytes(data.GetHeights(0,0,513,513))),Is.EqualTo((string)Read(field,"actualFeedbackSha256")),"Actual V428 native readback must reproduce before the controlled support experiment");var terrain=go.AddComponent<Terrain>();terrain.terrainData=data;go.AddComponent<TerrainCollider>().terrainData=data;go.transform.position=new Vector3(-1600,-20,-800);go.SetActive(true);terrain.Flush();Physics.SyncTransforms();float surface=(float)Read(first,"groundSurface");float Error(){float maximum=0;foreach(float x in new[]{physical.min.x,physical.center.x,physical.max.x})foreach(float z in new[]{physical.min.z,physical.center.z,physical.max.z})maximum=Mathf.Max(maximum,Mathf.Abs(terrain.SampleHeight(new Vector3(x,surface,z))-20-(surface-.08f)));return maximum;}
            float oldError=Error();Assert.That(oldError,Is.EqualTo((float)Read(first,"maximumNativeContactError")).Within(.0001f),"Actual Unity triangle interpolation, not bilinear approximation, must reproduce the reported0.5395m failure");var current=data.GetHeights(0,0,513,513);int x0=Mathf.RoundToInt((support.min.x+1600)/3.125f),x1=Mathf.RoundToInt((support.max.x+1600)/3.125f),z0=Mathf.RoundToInt((support.min.z+800)/3.125f),z1=Mathf.RoundToInt((support.max.z+800)/3.125f);for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)current[z,x]=(surface-.08f+20)/600;data.SetHeights(0,0,current);terrain.Flush();Physics.SyncTransforms();float fixedError=Error();Assert.That(fixedError,Is.LessThan(.01f));Assert.That(go.transform.position,Is.EqualTo(new Vector3(-1600,-20,-800)));Assert.That((Bounds)Read(first,"measuredBaseFootprint"),Is.EqualTo(physical));Assert.That(string.Join("\n",originalFootings.Cast<object>().Select(JsonUtility.ToJson)),Is.EqualTo(sourceRowsBefore));Assert.That(Hash(File.ReadAllBytes(floatPath)),Is.EqualTo(Hash(bytes)));TestContext.WriteLine("ACTUAL_NATIVE_V428_TRIANGLE_REPRODUCTION "+oldError+" MINIMAL_SHARED_SAMPLING_SUPPORT_CONTROL "+fixedError+" NOT_FULL_WORLD_ACCEPTANCE");
        }
        finally{if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(data!=null)UnityEngine.Object.DestroyImmediate(data);if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(active));}
    }
    static byte[] FloatBytes(float[,] values){var bytes=new byte[values.Length*4];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);return bytes;}
}
#endif
