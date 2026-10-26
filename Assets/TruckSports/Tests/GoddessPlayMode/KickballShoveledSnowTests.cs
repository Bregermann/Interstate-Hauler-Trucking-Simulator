#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class KickballShoveledSnowTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static bool Ready(object surface)=>(bool)Get(surface,"EnvironmentBindingsReady")&&(bool)Get(surface,"SnowTextureReady")&&(int)Get(surface,"BoundDepthCameraCount")>0&&Shader.GetGlobalTexture("_TaxiSnowTexture")!=null;
    static int Lobes(Mesh mesh){var parent=Enumerable.Range(0,mesh.vertexCount).ToArray();int Root(int at){while(parent[at]!=at){parent[at]=parent[parent[at]];at=parent[at];}return at;}var indices=mesh.triangles;for(int i=0;i<indices.Length;i+=3){parent[Root(indices[i+1])]=Root(indices[i]);parent[Root(indices[i+2])]=Root(indices[i]);}return indices.Select(Root).Distinct().Count();}
    static bool OnClearedSurface(Vector3 point,Renderer renderer)
    {
        var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;point=renderer.transform.InverseTransformPoint(point);var bounds=mesh.bounds;if(point.x<bounds.min.x||point.x>bounds.max.x||point.z<bounds.min.z||point.z>bounds.max.z)return false;var vertices=mesh.vertices;var indices=mesh.triangles;float Side(Vector3 a,Vector3 b)=>(b.x-a.x)*(point.z-a.z)-(b.z-a.z)*(point.x-a.x);
        for(int i=0;i<indices.Length;i+=3){float a=Side(vertices[indices[i]],vertices[indices[i+1]]),b=Side(vertices[indices[i+1]],vertices[indices[i+2]]),c=Side(vertices[indices[i+2]],vertices[indices[i]]);if(a>=0&&b>=0&&c>=0||a<=0&&b<=0&&c<=0)return true;}return false;
    }
    static void NaturalBanks(Renderer[] banks,Renderer[] clear,Vector3[] centers)
    {
        for(int index=0;index<banks.Length;index++)
        {
            var renderer=banks[index];var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;int baseIndex=index/3,side=index%3==0?0:index%3==1?-1:1;Assert.That(mesh.bounds.min.y,Is.GreaterThanOrEqualTo(.0239f));Assert.That(mesh.bounds.max.y,Is.LessThanOrEqualTo(.2401f),"Retire tall pipe-like snow rims.");Assert.That(mesh.bounds.max.y,Is.GreaterThan(.075f));Assert.That(Lobes(mesh),Is.GreaterThanOrEqualTo(3),"Real open gaps must break up each snow bank.");var vertices=mesh.vertices;var forward=(centers[(baseIndex+1)%4]-centers[baseIndex]).normalized;var lateral=Vector3.Cross(Vector3.up,forward);var profiles=vertices.GroupBy(point=>side==0?Mathf.RoundToInt(Mathf.Atan2(point.z-centers[baseIndex].z,point.x-centers[baseIndex].x)*1000):Mathf.RoundToInt(Vector3.Dot(point-centers[baseIndex],forward)*1000)).Where(group=>group.Count()==9).ToArray();Assert.That(profiles.Length,Is.GreaterThanOrEqualTo(4),"A rounded nine-row profile replaces the old triangular ridge.");var widths=profiles.Select(profile=>{var distances=profile.Select(point=>side==0?new Vector2(point.x-centers[baseIndex].x,point.z-centers[baseIndex].z).magnitude:Mathf.Abs(Vector3.Dot(point-centers[baseIndex],lateral))).ToArray();return distances.Max()-distances.Min();}).ToArray();Assert.That(widths.Max(),Is.InRange(1.499f,2.001f),"Preserve broad heap bodies.");Assert.That(widths.Min(),Is.LessThan(widths.Max()*.8f),"Both outline tips taper; no repeated full-width snow tiles.");Assert.That(widths.Count(width=>width>=1.499f),Is.GreaterThanOrEqualTo(3));Assert.That(widths.All(width=>width>0&&width<=2.001f),Is.True);
            foreach(var vertex in vertices){var world=renderer.transform.TransformPoint(vertex);foreach(var path in clear)Assert.That(OnClearedSurface(world,path),Is.False,"Snow must remain outward from the actual maintained clay/base footprint.");}var triangles=mesh.triangles;for(int i=0;i<triangles.Length;i+=3){var world=renderer.transform.TransformPoint((vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3);foreach(var path in clear)Assert.That(OnClearedSurface(world,path),Is.False,"A snow face cannot close a maintained route exit.");}
        }
    }
    [UnityTest, Timeout(240000)]
    public IEnumerator RealSnowAndBlizzardKeepOriginalBasesAndRoutesClearAcrossDayNightAndRematch()
    {
        var lease = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "KickballShoveledSnow" });
        var isolation = new GameObject("Private Kickball snow presentation acceptance"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckKickball/Scenes/TruckKickball.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Sports("SportsEventSession"), FindObjectsInactive.Include).Cast<Component>().Single();
            var presentation = UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckKickball.KickballShoveledSnow, Assembly-CSharp", true)) as Component;
            Assert.That(presentation, Is.Not.Null, "Run the explicit saved-scene upgrade first.");
            var bases = (Renderer[])Get(presentation, "clearedBases"); var paths = (Renderer[])Get(presentation, "clearedPaths"); var banks = (Renderer[])Get(presentation, "snowbanks");
            Assert.That(bases.Length, Is.EqualTo(4)); Assert.That(paths.Length, Is.EqualTo(8)); Assert.That(banks.Length, Is.EqualTo(12));
            Assert.That(presentation.GetComponentsInChildren<Collider>(true), Is.Empty, "Snow presentation cannot change vehicle/base collision.");
            var centers=(Vector3[])match.GetType().GetField("Bases",BindingFlags.Public|BindingFlags.Static).GetValue(null);NaturalBanks(banks,paths,centers);
            var bankMeshes=banks.Select(item=>item.GetComponent<MeshFilter>().sharedMesh).ToArray();var bankPaths=bankMeshes.Select(AssetDatabase.GetAssetPath).ToArray();var bankGuids=bankPaths.Select(AssetDatabase.AssetPathToGUID).ToArray();var bankFiles=bankPaths.Select(File.ReadAllBytes).ToArray();
            var packed=banks[0].sharedMaterial;string packedPath=AssetDatabase.GetAssetPath(packed),packedGuid=AssetDatabase.AssetPathToGUID(packedPath);var packedBytes=File.ReadAllBytes(packedPath);Assert.That(packed.shader.name,Is.EqualTo("NOT_Lonely/Weatherade/Snow Coverage"),"Run explicit owned packed-snow detail author after the geometry refinement.");Assert.That(packed.GetTexture("_CoverageTex0"),Is.Not.Null);Assert.That(packed.GetTexture("_CoverageDetailTex"),Is.Not.Null);Assert.That(packed.GetFloat("_Displacement"),Is.Zero);Assert.That(packed.GetFloat("_Tessellation"),Is.Zero);
            var colliders = bases.Select(item => item.GetComponent<Collider>()).ToArray(); var transforms = colliders.Select(item => item.transform.localToWorldMatrix).ToArray();
            var original = colliders.Select(item => EditorJsonUtility.ToJson(item)).ToArray();
            var materials = bases.Concat(paths).SelectMany(item => item.sharedMaterials).Distinct().ToArray();
            var materialPaths = materials.Select(AssetDatabase.GetAssetPath).ToArray(); var materialFiles = materialPaths.Select(File.ReadAllBytes).ToArray();
            foreach (var material in materials)
            {
                Assert.That(material.shader.name, Does.Contain("Weatherade")); Assert.That(material.GetFloat("_CoverageAmountOverride"), Is.EqualTo(1));
                Assert.That(material.GetFloat("_CoverageOverride"), Is.EqualTo(1)); Assert.That(material.GetFloat("_CoverageAmount"), Is.Zero);
            }
            Call(match, "AllBots"); var weather = match.GetComponent(Sports("SportsChaosWeather")); var catalog = Get(weather, "presets"); var entries = ((Array)Get(catalog, "entries")).Cast<object>().ToArray();
            var snow=match.GetComponent(Sports("SportsSnowSurface"));var surface=Get(snow,"surface");var infield=match.gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<MeshRenderer>(true)).Single(renderer=>renderer.name=="Continuous dirt infield");
            foreach (string forecast in new[] { "Snow", "Heavy Snow", "Blizzard", "Clear" })
            foreach (string time in new[] { "Day", "Night" })
            {
                Call(match, "SetupScreen"); Sports("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Sports("SportsTimeChoice"), time) });
                var preset = entries.Single(item => (string)Get(item, "displayName") == forecast); Call(catalog, "Select", Get(preset, "id")); Assert.That(Call(match, "StartMatch"), Is.True);
                yield return new WaitForSecondsRealtime(1.1f);
                if(forecast!="Clear")
                {
                    float started=Time.realtimeSinceStartup,scaled=Time.time,deadline=started+30;int prior=(int)Get(surface,"nextEnvironmentTarget"),frame=Time.frameCount,frames=0,maxBatch=0;
                    while(!Ready(surface))
                    {
                        if(Time.realtimeSinceStartup>=deadline)Assert.Fail("Bounded vendor snow readiness deadline: pending="+Get(surface,"PendingEnvironmentBindingCount")+" elapsedReal="+(Time.realtimeSinceStartup-started)+" elapsedScaled="+(Time.time-scaled)+" timeScale="+Time.timeScale+" infield="+infield.sharedMaterial.shader.name+"; "+Get(surface,"Diagnostic")+"; "+Get(surface,"VisualDiagnostic"));yield return null;
                        int next=(int)Get(surface,"nextEnvironmentTarget"),elapsed=Time.frameCount-frame,delta=next-prior;Assert.That(delta,Is.InRange(0,64*Mathf.Max(1,elapsed)),"Existing environment binding stays within its64-target/frame budget.");maxBatch=Mathf.Max(maxBatch,delta);frames+=elapsed;prior=next;frame=Time.frameCount;
                    }
                    // Wait a real rendered frame after binding; do not change
                    // snowfall, global coverage or scene material authority.
                    yield return null;yield return null;
                    Assert.That(infield.sharedMaterial.shader.name,Is.EqualTo("LWS/TruckTaxi/Snow/EnvironmentLitGround"),"Inspect the real adapted infield rather than masking incomplete binding with more snowfall.");
                    Assert.That(banks.All(bank=>bank.sharedMaterial==packed),Is.True);Assert.That(packed.GetFloat("_CoverageAmount"),Is.EqualTo(1));Assert.That(packed.GetFloat("_CoverageAmountOverride"),Is.EqualTo(1));Assert.That(packed.GetTexture("_CoverageTex0"),Is.SameAs(Get(snow,"snowTexture")));Assert.That(packed.GetTexture("_CoverageDetailTex"),Is.SameAs(Get(snow,"detailTexture")));
                    string diagnostic="forecast="+forecast+" time="+time+" actualCoverage="+Get(snow,"Coverage")+" shaderGlobalCoverage="+Shader.GetGlobalFloat("_TaxiSnowCoverage")+" bound="+Get(surface,"EnvironmentBindingsReady")+" pending="+Get(surface,"PendingEnvironmentBindingCount")+" readyElapsedReal="+(Time.realtimeSinceStartup-started)+" readyElapsedScaled="+(Time.time-scaled)+" sampledFrames="+frames+" maxTargetBatch="+maxBatch+" shader="+infield.sharedMaterial.shader.name+" material="+infield.sharedMaterial.name+" texture="+Shader.GetGlobalTexture("_TaxiSnowTexture").name+" bankShader="+packed.shader.name+" detail="+packed.GetTexture("_CoverageDetailTex").name+" depthCameras="+Get(surface,"BoundDepthCameraCount")+"; "+Get(surface,"Diagnostic")+"; "+Get(surface,"VisualDiagnostic");
                    TestContext.WriteLine("KICKBALL_SNOW_CAPTURE_READY "+diagnostic);string folder="Logs/TruckSports/PlaytestRepair/KickballShoveledSnowNative";Directory.CreateDirectory(folder);File.WriteAllText(folder+"/"+forecast.Replace(" ","")+"-"+time+"-readiness.txt",diagnostic);
                }
                Assert.That((bool)Get(presentation, "Visible"), Is.EqualTo(forecast != "Clear"));
                foreach (var material in materials) Assert.That(material.GetFloat("_CoverageAmount"), Is.Zero, "Vendor refresh must honor maintained-base overrides.");
                for (int i = 0; i < bases.Length; i++) { Assert.That(colliders[i].transform.localToWorldMatrix, Is.EqualTo(transforms[i])); Assert.That(EditorJsonUtility.ToJson(colliders[i]), Is.EqualTo(original[i])); }
                for(int i=0;i<banks.Length;i++){Assert.That(banks[i].GetComponent<MeshFilter>().sharedMesh,Is.SameAs(bankMeshes[i]));Assert.That(AssetDatabase.AssetPathToGUID(bankPaths[i]),Is.EqualTo(bankGuids[i]));}
                if (forecast != "Clear")
                {
                    Time.timeScale = 0;
                    foreach (int width in new[] { 1280, 1920 }) Sports("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, width, width == 1280 ? 720 : 1080, "Logs/TruckSports/PlaytestRepair/KickballShoveledSnowNative/" + forecast.Replace(" ", "") + "-" + time + "-" + width + ".png", null });
                    Time.timeScale = 1;
                }
            }
            for (int i = 0; i < materialPaths.Length; i++) CollectionAssert.AreEqual(materialFiles[i], File.ReadAllBytes(materialPaths[i]), "Matches never save accumulation into clear authored materials.");
            for(int i=0;i<bankPaths.Length;i++)CollectionAssert.AreEqual(bankFiles[i],File.ReadAllBytes(bankPaths[i]),"Weather/play/rematch never regenerate or save authored snow meshes.");
            Assert.That(AssetDatabase.AssetPathToGUID(packedPath),Is.EqualTo(packedGuid));CollectionAssert.AreEqual(packedBytes,File.ReadAllBytes(packedPath),"Weather/play/rematch never saves global accumulation into the owned packed-bank material.");
        }
        finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); }
    }
    [UnityTest,Timeout(120000)]
    public IEnumerator PendingSnowPresentationConvergesWhileOriginalGameplayIsPausedAcrossRematch()
    {
        var lease=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"KickballSnowPausedBinding"});var isolation=new GameObject("Private paused snow binding regression");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));Component match=null;
        try
        {
            var scene=EditorSceneManager.LoadSceneInPlayMode("Assets/TruckKickball/Scenes/TruckKickball.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Sports("SportsEventSession"),true)).Cast<Component>().Single();Call(match,"AllBots");var weather=match.GetComponent(Sports("SportsChaosWeather"));var catalog=Get(weather,"presets");var preset=((Array)Get(catalog,"entries")).Cast<object>().Single(item=>(string)Get(item,"displayName")=="Heavy Snow");Call(catalog,"Select",Get(preset,"id"));var snow=match.GetComponent(Sports("SportsSnowSurface"));var surface=Get(snow,"surface");var presentation=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Type.GetType("LWS.TruckKickball.KickballShoveledSnow, Assembly-CSharp",true),true)).Cast<Component>().Single();var clear=((Renderer[])Get(presentation,"clearedBases")).Concat((Renderer[])Get(presentation,"clearedPaths")).ToArray();var clearMaterials=clear.Select(renderer=>renderer.sharedMaterial).ToArray();var materials=clearMaterials.Concat(((Renderer[])Get(presentation,"snowbanks")).Select(renderer=>renderer.sharedMaterial)).Distinct().ToArray();var paths=materials.Select(AssetDatabase.GetAssetPath).ToArray();var files=paths.Select(File.ReadAllBytes).ToArray();
            for(int rematch=0;rematch<2;rematch++)
            {
                if(rematch>0)Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));yield return null;Assert.That((bool)Get(surface,"EnvironmentBindingsReady"),Is.False,"The actual multi-thousand-renderer scene must still have genuine queued work after its first bounded batch.");Call(match,"Pause",0,false);yield return null;Assert.That(Get(match,"Paused"),Is.EqualTo(true));Assert.That(Time.timeScale,Is.Zero);int pending=(int)Get(surface,"PendingEnvironmentBindingCount");Assert.That(pending,Is.GreaterThan(64));float coverage=(float)Get(snow,"Coverage"),scaled=Time.time;int seed=(int)Get(weather,"Seed");string rules=JsonUtility.ToJson(Get(match,"rules"));var bodies=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Rigidbody>(true)).Select(body=>(body,body.position,body.rotation,body.linearVelocity,body.angularVelocity)).ToArray();float started=Time.realtimeSinceStartup,deadline=started+30;int prior=(int)Get(surface,"nextEnvironmentTarget"),frame=Time.frameCount,progressFrames=0,maxBatch=0;
                while(!Ready(surface))
                {
                    if(Time.realtimeSinceStartup>=deadline)Assert.Fail("Paused snow readiness deadline pending="+Get(surface,"PendingEnvironmentBindingCount")+"; "+Get(surface,"Diagnostic"));yield return null;int next=(int)Get(surface,"nextEnvironmentTarget"),elapsed=Time.frameCount-frame,delta=next-prior;Assert.That(delta,Is.InRange(0,64*Mathf.Max(1,elapsed)),"At most one existing64-target refresh batch per rendered frame; no catchup loop.");if(delta>0)progressFrames++;maxBatch=Mathf.Max(maxBatch,delta);prior=next;frame=Time.frameCount;
                    Assert.That((float)Get(snow,"Coverage"),Is.EqualTo(coverage));Assert.That(Time.time,Is.EqualTo(scaled));Assert.That(Get(weather,"Seed"),Is.EqualTo(seed));Assert.That(JsonUtility.ToJson(Get(match,"rules")),Is.EqualTo(rules));foreach(var entry in bodies){Assert.That(entry.body.position,Is.EqualTo(entry.position));Assert.That(entry.body.rotation,Is.EqualTo(entry.rotation));Assert.That(entry.body.linearVelocity,Is.EqualTo(entry.linearVelocity));Assert.That(entry.body.angularVelocity,Is.EqualTo(entry.angularVelocity));}for(int i=0;i<clear.Length;i++){Assert.That(clear[i].sharedMaterial,Is.SameAs(clearMaterials[i]));Assert.That(clearMaterials[i].GetFloat("_CoverageAmount"),Is.Zero);Assert.That(clearMaterials[i].GetFloat("_CoverageAmountOverride"),Is.EqualTo(1));}
                }
                Assert.That(progressFrames,Is.GreaterThan(1));Assert.That((int)Get(surface,"PendingEnvironmentBindingCount"),Is.Zero);TestContext.WriteLine("KICKBALL_PAUSED_SNOW_READY rematch="+rematch+" initialPending="+pending+" progressFrames="+progressFrames+" maxTargetBatch="+maxBatch+" elapsedReal="+(Time.realtimeSinceStartup-started)+" elapsedScaled="+(Time.time-scaled)+" coverage="+coverage+"; "+Get(surface,"Diagnostic"));Call(match,"Resume");Assert.That(Get(match,"Paused"),Is.EqualTo(false));
            }
            for(int i=0;i<paths.Length;i++)CollectionAssert.AreEqual(files[i],File.ReadAllBytes(paths[i]),"Readiness/pause/rematch never saves authored snow/clear materials.");
        }
        finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolation);lease.Dispose();}
    }
}
#endif
