#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class DragonKingdomRetainedRoofThermalTests
{
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static Type Nested(string name)=>Integration.GetNestedType(name,BindingFlags.Public|BindingFlags.NonPublic);
    static object Read(object value,string field)=>value.GetType().GetField(field).GetValue(value);
    static void Set(object value,string field,object replacement)=>value.GetType().GetField(field).SetValue(value,replacement);
    static string HeightHash(float[,] values)=>(string)Integration.GetMethod("FinishFloatHash",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{values});
    sealed class NativeRoofFixture:IDisposable
    {
        readonly Scene incoming,preview;readonly bool incomingDirty;bool disposed;public GameObject go;public TerrainData data,foundationData;
        public NativeRoofFixture(bool withFoundation=false)
        {
            incoming=SceneManager.GetActiveScene();incomingDirty=incoming.isDirty;preview=EditorSceneManager.NewPreviewScene();
            try
            {
                go=new GameObject("Inactive isolated native roof authority test");go.hideFlags=HideFlags.HideAndDontSave;go.SetActive(false);SceneManager.MoveGameObjectToScene(go,preview);data=new TerrainData{heightmapResolution=513,size=new Vector3(1600,600,1600)};data.hideFlags=HideFlags.HideAndDontSave;var terrain=go.AddComponent<Terrain>();terrain.terrainData=data;var collider=go.AddComponent<TerrainCollider>();collider.terrainData=data;go.transform.position=new Vector3(0,-20,800);if(withFoundation)foundationData=new TerrainData{heightmapResolution=513,size=new Vector3(1600,600,1600),hideFlags=HideFlags.HideAndDontSave};
            }
            catch{Dispose();throw;}
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;try{if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(data!=null)UnityEngine.Object.DestroyImmediate(data);if(foundationData!=null)UnityEngine.Object.DestroyImmediate(foundationData);}finally{if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);}Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(incoming),"Native fixture must not change incoming active scene");Assert.That(incoming.isDirty,Is.EqualTo(incomingDirty),"Native fixture must not dirty or clear incoming user scene");
        }
    }
    [Test]
    public void OriginalRoofThermalBranchRequiresAnExplicitSeparateProfile()
    {var failure=Assert.Throws<TargetInvocationException>(()=>Integration.GetMethod("RequireRetainedRoofThermalProfile",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{""}));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(failure.InnerException.Message,Does.Contain("never enable original-roof restoration implicitly"));}
    [TestCase("start")][TestCase("seam")][TestCase("unconstrained")]
    public void ContradictoryOriginalRoofCannotOverrideFinalPhysicalAuthority(string kind)
    {
        using(var fixture=new NativeRoofFixture())
        {
            var data=fixture.data;var go=fixture.go;
            var before=new float[513,513];var foundation=(float[,])before.Clone();int x=240,z=kind=="seam"?0:35;foundation[z,x]=.3f;var state=Activator.CreateInstance(Nested("TileState"),true);Set(state,"terrain",go.GetComponent<Terrain>());Set(state,"before",before);Set(state,"foundation",foundation);var record=Activator.CreateInstance(Nested("TileRecord"));Set(record,"candidate","CONTROLLED_UNSAVED_ROOF_AUTHORITY_TEST");Set(state,"record",record);var states=Array.CreateInstance(state.GetType(),1);states.SetValue(state,0);
            var mask=Activator.CreateInstance(Nested("NativeVistaPhysicalMaskContext"),true);Set(mask,"bounds",kind=="start"?new List<Bounds>{new Bounds(new Vector3(750,0,909.375f),new Vector3(1,0,1))}:new List<Bounds>());Set(mask,"boundOwners",kind=="start"?new List<string>{"ORIGINAL_NWH_START:CONTROLLED"}:new List<string>());Set(mask,"physicalBoundCount",kind=="start"?1:0);Set(mask,"roadBins",Activator.CreateInstance(mask.GetType().GetField("roadBins").FieldType));var footings=Array.CreateInstance(Nested("NativeArchitectureFooting"),0);Set(mask,"footings",Activator.CreateInstance(Nested("NativeFootingIndex"),new object[]{footings,12.5f}));var conflicts=((IEnumerable)Integration.GetMethod("AuditOriginalRoofPhysicalContradictions",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{states,mask})).Cast<object>().ToArray();
            if(kind=="unconstrained")Assert.That(conflicts,Is.Empty);else{Assert.That(conflicts.Length,Is.EqualTo(1));Assert.That((float)Read(conflicts[0],"finalPhysicalHeight"),Is.Not.EqualTo((float)Read(conflicts[0],"originalRoofHeight")));Assert.That(((string[])Read(conflicts[0],"owners")).Any(o=>o.Contains(kind=="start"?"ORIGINAL_NWH_START":"ORIGINAL_TILE_SEAM_FINAL_BASELINE")),Is.True);}
        }
    }
    [Test]
    public void NativePartialRoofCopyPreservesExactOutsideBitsAndTerrainColliderBinding()
    {
        using(var fixture=new NativeRoofFixture(true))
        {
            var data=fixture.data;var foundationData=fixture.foundationData;var go=fixture.go;
            var values=new float[513,513];for(int z=0;z<513;z++)for(int x=0;x<513;x++)values[z,x]=.31f;data.SetHeights(0,0,values);values[35,240]=.32f;foundationData.SetHeights(0,0,values);var observed=data.GetHeights(0,0,513,513);var foundation=foundationData.GetHeights(0,0,513,513);string before=HeightHash(observed),original=HeightHash(foundation);var args=new object[]{observed,foundation,go.transform.position,data.size,new Bounds(new Vector3(560,135,780),new Vector3(400,140,260)),before,before,original,"ACTUAL_SDK_RAW_BEFORE_NATIVE_COORDINATOR",null};var proof=Integration.GetMethod("CopyOriginalUndergroundRoof",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);var output=(float[,])args[9];var rect=(RectInt)Read(proof,"changedRectangle");Assert.That(rect.width*rect.height,Is.EqualTo(1));data.SetHeights(rect.x,rect.y,new float[,]{{output[rect.y,rect.x]}});Assert.That(HeightHash(data.GetHeights(0,0,513,513)),Is.EqualTo((string)Read(proof,"protectedCompositionHeightSha256")));Assert.That((bool)Read(proof,"outsideMaskBitExact"),Is.True);Assert.That(go.GetComponent<TerrainCollider>().terrainData,Is.SameAs(data));Assert.That(HeightHash(foundationData.GetHeights(0,0,513,513)),Is.EqualTo(original));Assert.That(HeightHash(observed),Is.EqualTo(before));
        }
    }
    [UnityTest,Timeout(900000)]
    public IEnumerator ActualFinalFieldsMeasureRoofContradictionsBeforeGpuAndSameVendorThermalWithoutExemptions()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"RoofThermalRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve incoming user scene before explicit roof thermal probe");}
        yield return (IEnumerator)Integration.GetMethod("CaptureRetainedOriginalRoofThermalRepair").Invoke(null,null);var proof=Integration.GetProperty("LastRetainedRoofThermalReport").GetValue(null);foreach(string field in new[]{"originalSourcesExact","sourceTerrainMemoryExact","contextRestored"})Assert.That((bool)Read(proof,field),Is.True,field);var stages=((IEnumerable)Read(proof,"stages")).Cast<object>().ToArray();Assert.That(stages.Any(s=>(string)Read(s,"stage")=="BASELINE_20464_4492_CORE0_SEAM0_QUALIFIED_BEFORE_COPY"),Is.True);Assert.That((string)Read(stages.Last(),"stage"),Is.EqualTo("SOURCE_RESTORED"));double elapsed=-1;foreach(var stage in stages){Assert.That((double)Read(stage,"elapsedSeconds"),Is.GreaterThanOrEqualTo(elapsed));elapsed=(double)Read(stage,"elapsedSeconds");}var trials=((IEnumerable)Read(proof,"trials")).Cast<object>().ToArray();Assert.That(trials.Length,Is.GreaterThan(0));
        foreach(var trial in trials)
        {
            Assert.That((bool)Read(trial,"baselineExact"),Is.True);Assert.That((bool)Read(trial,"maskMatchesFrozenOriginal"),Is.True);var conflicts=(Array)Read(trial,"contradictions");if(conflicts.Length>0){Assert.That((bool)Read(trial,"rejectedBeforeVendorStage"),Is.True);Assert.That(Read(trial,"vendor"),Is.Null);Assert.That((string)Read(trial,"status"),Does.Contain("BEFORE_NATIVE_COPY_OR_GPU"));foreach(var conflict in conflicts){Assert.That(((string[])Read(conflict,"owners")).Length,Is.GreaterThan(0));Assert.That((float)Read(conflict,"originalRoofHeight"),Is.Not.EqualTo((float)Read(conflict,"finalPhysicalHeight")));}TestContext.WriteLine("ACTUAL_ROOF_CONTRADICTION_REJECTED_BEFORE_COPY_OR_GPU "+JsonUtility.ToJson(trial));continue;}
            Assert.That((bool)Read(trial,"maskBeforeAfterExact"),Is.True);Assert.That((bool)Read(trial,"sourceGraphAndCriticalPosesExact"),Is.True);foreach(var field in (Array)Read(trial,"fields")){Assert.That((bool)Read(field,"nativeReadbackExact")&&(bool)Read(field,"holesLayersExact"),Is.True);}var vendor=Read(trial,"vendor");Assert.That((string)Read(vendor,"observedInputRole"),Is.EqualTo("ACTUAL_FINAL_V417_THEN_ORIGINAL_ROOF_SOURCE_PROTECTION_NOT_RAW_SDK"));Assert.That((bool)Read(vendor,"publicThermalNodeConfiguredExact"),Is.True);Assert.That((bool)Read(vendor,"actualSdkRawMosaicKnown"),Is.False);Assert.That((bool)Read(trial,"originalQualityLimitsPreserved"),Is.True);TestContext.WriteLine("ACTUAL_BOUNDED_ROOF_THERMAL_RESULT_NOT_WORLD_ACCEPTANCE "+JsonUtility.ToJson(trial));
        }
        Assert.That((string)Read(proof,"scope"),Does.Contain("NOT_WORLDHOOK_COLLIDER_DIGGER_DRIVING_OR_ADOPTION_ACCEPTANCE"));
    }
}
#endif
