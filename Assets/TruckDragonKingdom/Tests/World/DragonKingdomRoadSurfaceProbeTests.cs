#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonKingdomRoadSurfaceProbeTests
{
    [UnityTest,Timeout(600000)]
    public IEnumerator ActualSaved23UrbanBankProjectionPreservesEverySourceAndColliderAndMeasures2146Locations()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"BankProjectionRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False);}var setup=EditorSceneManager.GetSceneManagerSetup();var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolated,generated+"BankProjectionIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            yield return null;var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);type.GetMethod("CaptureActualSavedUrbanBankProjection").Invoke(null,null);var proof=type.GetProperty("LastUrbanBankProjection").GetValue(null);object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);Assert.That((string)Field(proof,"status"),Is.EqualTo("NATIVE_PROJECTED_12M_BANK_PASSED_REQUIRES_COMPOSITION_AND_FRESH_FULL_WORLD_GATES"));foreach(string n in new[]{"sourcesExact","originalDataBindingsRestored","colliderDataBindingsUnchanged","heightsAndPhysicsExact"})Assert.That((bool)Field(proof,n),Is.True,n);Assert.That((int)Field(proof,"coreTiles"),Is.EqualTo(4));Assert.That((int)Field(proof,"originalColliders"),Is.EqualTo(16));var bank=Field(proof,"projectedBank");Assert.That((bool)Field(bank,"passed"),Is.True);Assert.That(((Array)Field(bank,"districts")).Cast<object>().Sum(d=>((Array)Field(d,"points")).Length),Is.EqualTo(2146));
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    [UnityTest,Timeout(600000)]
    public IEnumerator ActualExistingDiggerPublicNormalOptionAbaPreservesSourceAndTerrainBindings()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"TerrainProbeRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False);}
        var setup=EditorSceneManager.GetSceneManagerSetup();var before=setup.ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolated,generated+"TerrainProbeIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            yield return null;var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureActualTerrainPublicNormalComparison").Invoke(null,null);var proof=type.GetProperty("LastTerrainSurfaceReceipt").GetValue(null);object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
            Assert.That((string)Field(proof,"status"),Is.EqualTo("CAPTURED_PUBLIC_NORMAL_OPTION_REQUIRES_ACTUAL_CAUSAL_IMAGE_REVIEW_NOT_A_FIX"));foreach(string n in new[]{"sourceBytesPreserved","physicsAndHeightsExact","originalTerrainDataRestored","colliderBindingsUnchanged","baselinePixelsRestored","nonTerrainRendererStatesRestored","materialBindingsRestored","publicNormalBindingVerified","textureSamplerStatesRestored"})Assert.That((bool)Field(proof,n),Is.True,n);Assert.That((int)Field(proof,"maskedNonTerrainRenderers"),Is.GreaterThan(0));Assert.That((int)Field(proof,"terrains"),Is.EqualTo(16));Assert.That((int)Field(proof,"colliderTerrainBindings"),Is.EqualTo(16));Assert.That(((Array)Field(proof,"frames")).Length,Is.EqualTo(12));foreach(var frame in (Array)Field(proof,"frames")){Assert.That((int)Field(frame,"terrainSamples"),Is.GreaterThanOrEqualTo(100));Assert.That(((string[])Field(frame,"shaderErrors")),Is.Empty);Assert.That(File.Exists((string)Field(frame,"png")),Is.True);}foreach(var source in before)Assert.That(File.ReadAllBytes(source.Key),Is.EqualTo(source.Value));
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    [UnityTest,Timeout(600000)]
    public IEnumerator ActualSourceAuthoredTerrainPalettesPreserveHeightAlphaAndShaderExceptExplicitUrpReference()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"TerrainProbeRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False);}
        var setup=EditorSceneManager.GetSceneManagerSetup();var before=setup.ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolated,generated+"TerrainProbeIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            yield return null;var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureActualAuthorTerrainPaletteComparison").Invoke(null,null);var proof=type.GetProperty("LastTerrainSurfaceReceipt").GetValue(null);object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
            Assert.That((string)Field(proof,"status"),Is.EqualTo("CAPTURED_SOURCE_AUTHORED_PALETTES_REQUIRES_IMAGE_CHOICE_NOT_WORLD_ACCEPTANCE"));foreach(string n in new[]{"sourceBytesPreserved","physicsAndHeightsExact","originalTerrainDataRestored","colliderBindingsUnchanged","baselinePixelsRestored","nonTerrainRendererStatesRestored","materialBindingsRestored","textureSamplerStatesRestored"})Assert.That((bool)Field(proof,n),Is.True,n);Assert.That((int)Field(proof,"maskedNonTerrainRenderers"),Is.GreaterThan(0));Assert.That((int)Field(proof,"terrains"),Is.EqualTo(16));Assert.That((int)Field(proof,"colliderTerrainBindings"),Is.EqualTo(16));Assert.That(((Array)Field(proof,"frames")).Length,Is.EqualTo(15));foreach(var frame in (Array)Field(proof,"frames")){Assert.That((bool)Field(frame,"alphaMapsExact"),Is.True);Assert.That((int)Field(frame,"terrainSamples"),Is.GreaterThanOrEqualTo(100));Assert.That(((string[])Field(frame,"shaderErrors")),Is.Empty);Assert.That(File.Exists((string)Field(frame,"png")),Is.True);}foreach(var source in before)Assert.That(File.ReadAllBytes(source.Key),Is.EqualTo(source.Value));
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    [UnityTest,Timeout(600000)]
    public IEnumerator ActualSavedTerrainUvAndNormalAbaPreservesSourcesCollidersAndEveryHeightmap()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"TerrainProbeRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False);}
        var setup=EditorSceneManager.GetSceneManagerSetup();var before=setup.ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolated,generated+"TerrainProbeIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            yield return null;var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureActualTerrainSurfaceComparison").Invoke(null,null);var proof=type.GetProperty("LastTerrainSurfaceReceipt").GetValue(null);object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
            Assert.That((string)Field(proof,"status"),Is.EqualTo("CAPTURED_REQUIRES_TEXTURE_NORMAL_IMAGE_REVIEW_NOT_A_FIX"));foreach(string n in new[]{"sourceBytesPreserved","physicsAndHeightsExact","originalTerrainDataRestored","colliderBindingsUnchanged","baselinePixelsRestored","nonTerrainRendererStatesRestored"})Assert.That((bool)Field(proof,n),Is.True,n);Assert.That((int)Field(proof,"maskedNonTerrainRenderers"),Is.GreaterThan(0));Assert.That((int)Field(proof,"terrains"),Is.EqualTo(16));Assert.That((int)Field(proof,"colliderTerrainBindings"),Is.EqualTo(16));Assert.That(((Array)Field(proof,"frames")).Length,Is.EqualTo(15));foreach(var frame in (Array)Field(proof,"frames")){Assert.That((int)Field(frame,"terrainSamples"),Is.GreaterThanOrEqualTo(100));Assert.That(((string[])Field(frame,"shaderErrors")),Is.Empty);Assert.That(File.Exists((string)Field(frame,"png")),Is.True);}foreach(var source in before)Assert.That(File.ReadAllBytes(source.Key),Is.EqualTo(source.Value));
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    [UnityTest,Timeout(600000)]
    public IEnumerator ActualCavernLightRangeMaterialAbaAndSettlementWaterAreMeasuredWithoutSourceWrites()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"CavernProbeRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False);
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();var before=setup.ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));
        var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolated,generated+"CavernProbeIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            yield return null;var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);
            type.GetMethod("CaptureActualCavernRoadLightingComparison").Invoke(null,null);var receipt=type.GetProperty("LastCavernReceipt").GetValue(null);object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
            Assert.That((string)Field(receipt,"status"),Is.EqualTo("CAPTURED_MEASURED_REQUIRES_CAUSAL_IMAGE_REVIEW_NOT_A_FIX"));
            foreach(string n in new[]{"sourcesExact","physicsAndHeightmapsExact","originalBindingsRestored","originalLightsRestored","diggerRenderersRestored"})Assert.That((bool)Field(receipt,n),Is.True,n);
            Assert.That((int)Field(receipt,"actualDiggerRenderers"),Is.GreaterThan(0));
            var frames=(Array)Field(receipt,"frames");Assert.That(frames.Length,Is.EqualTo(9));
            foreach(var frame in frames){Assert.That(((string[])Field(frame,"shaderErrors")),Is.Empty);Assert.That((int)Field(frame,"roadSamples"),Is.GreaterThanOrEqualTo(100));Assert.That(File.Exists((string)Field(frame,"png")),Is.True);}
            Assert.That((int)Field(receipt,"sourceLampRoots"),Is.EqualTo(12));Assert.That((bool)Field(receipt,"lampMeshSourcesExact"),Is.True);Assert.That((int)Field(receipt,"expectedSourcePointLights"),Is.EqualTo((bool)Field(receipt,"singleSourceLightPerLantern")?12:24));Assert.That((int)Field(receipt,"cavernPointLights"),Is.EqualTo((int)Field(receipt,"expectedSourcePointLights")));Assert.That(((Array)Field(receipt,"settlements")).Length,Is.EqualTo(2));
            foreach(var source in before)Assert.That(File.ReadAllBytes(source.Key),Is.EqualTo(source.Value));
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    [UnityTest,Timeout(600000)]
    public IEnumerator ActualSavedRoadVendorShaderComparisonPreservesAllSourceAndPhysics()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"RoadShaderRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve user edits before isolated read-only road comparison.");
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();var before=setup.ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));
        var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolated,generated+"RoadShaderIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            yield return null;
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);
            type.GetMethod("CaptureActualRoadSurfaceComparison").Invoke(null,null);
            var receipt=type.GetProperty("LastReceipt").GetValue(null);object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
            Assert.That((string)Field(receipt,"status"),Is.EqualTo("CAPTURED_REQUIRES_ACTUAL_IMAGE_AND_ROAD_PIXEL_REVIEW"));
            foreach(string n in new[]{"shaderSupported","sourceBytesPreserved","physicsAndHeightsExact","asynchronousCompilationDisabled"})Assert.That((bool)Field(receipt,n),Is.True,n);
            Assert.That((int)Field(receipt,"actualRoadRenderers"),Is.GreaterThanOrEqualTo(17));
            var frames=(Array)Field(receipt,"frames");Assert.That(frames.Length,Is.EqualTo(6));
            foreach(var frame in frames){Assert.That(((string[])Field(frame,"shaderErrors")),Is.Empty);Assert.That((int)Field(frame,"roadSamples"),Is.GreaterThanOrEqualTo(100));Assert.That(File.Exists((string)Field(frame,"png")),Is.True);}
            foreach(var source in before)Assert.That(File.ReadAllBytes(source.Key),Is.EqualTo(source.Value));
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
}
#endif
