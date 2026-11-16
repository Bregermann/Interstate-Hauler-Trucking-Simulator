#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
public sealed class DragonKingdomCavernJoinFinishTests
{
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static Type Cavern=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomDiggerCavernAuthoring, Assembly-CSharp-Editor",true);
    static Type World=>Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true);
    static object Read(object owner,string field)=>owner.GetType().GetField(field).GetValue(owner);
    static string Io(string path)=>(string)Integration.GetMethod("CavernJoinPhysicalSource",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{path});
    static bool Exists(string path)=>File.Exists(Io(path));
    static bool DirExists(string path)=>Directory.Exists(Io(path));
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Io(path)))).Replace("-","").ToLowerInvariant();}
    static string Argument(string flag){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,flag);Assert.That(at>=0&&at+1<args.Length,Is.True,"Explicit native visual test argument required: "+flag);return args[at+1];}
    [Test]
    public void ExistingProjectLongPathIoReadsTheExactRetainedTreeTextureMetadata()
    {
        const string path="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Plugins/Polyart Studio/HandpaintedEnvironmentVol1/Textures/Trees/Tree Large/Old/T_Tree_Large_Bark_R.png.meta";
        Assert.That(Exists(path),Is.True,"This retained original source is required; no missing-source fallback");Assert.That(Hash(path),Is.EqualTo("68bf70e3b32c7cc99e09e0f259eb5fe8abb5a0929052dd7b61cef789da45b051"));if(Application.platform==RuntimePlatform.WindowsEditor&&Path.GetFullPath(path).Length>=248)Assert.That(Io(path),Does.StartWith(@"\\?\"));
    }
    [Test]
    public void VirtualPackageSourceResolvesOnlyThroughItsExactPublicRegistration()
    {
        const string path="Packages/com.unity.splines/Runtime/SplineContainer.cs";var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.splines");Assert.That(package,Is.Not.Null);Assert.That(package.name,Is.EqualTo("com.unity.splines"));Assert.That(package.assetPath,Is.EqualTo("Packages/com.unity.splines"));Assert.That(Exists(path),Is.True);Assert.That(Io(path),Is.EqualTo((string)Integration.GetMethod("Io",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{Path.Combine(package.resolvedPath,"Runtime/SplineContainer.cs")})));Assert.That(Hash(path).Length,Is.EqualTo(64));
        var failure=Assert.Throws<TargetInvocationException>(()=>Io("Packages/com.unity.splines/../../outside-registered-package.cs"));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(failure.InnerException.Message,Does.Contain("escapes"));
    }
    [Test]
    public void ActualGraphProducesTwoSharedDoorwaysWithNoDuplicateEastCollars()
    {
        const string prefab="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";string before=Hash(prefab);var source=AssetDatabase.LoadAssetAtPath<GameObject>(prefab);Assert.That(source,Is.Not.Null);var sites=((Array)Cavern.GetMethod("DescribeCurrentCavernJoinSites").Invoke(null,new[]{source.GetComponent(World)})).Cast<object>().ToArray();Assert.That(sites.Length,Is.EqualTo(2));Assert.That(((Vector3)Read(sites[0],"authoredForward")).y,Is.GreaterThan(.02f),"The real western pavement grade is preserved for native NWH qualification");Assert.That(((Vector3)Read(sites[0],"boundary")).x,Is.InRange(424f,427f));Assert.That(((Vector3)Read(sites[1],"boundary")).x,Is.InRange(613f,616f));Assert.That(((string[])Read(sites[1],"edges")).Length,Is.EqualTo(3),"Castle, harbor and volcano share the same physical east passage");Assert.That(Vector3.Dot((Vector3)Read(sites[0],"inward"),(Vector3)Read(sites[1],"inward")),Is.LessThan(-.99f));Assert.That(Hash(prefab),Is.EqualTo(before));
    }
    [TestCase(0,130,0,false)][TestCase(0,160,0,false)][TestCase(0,168,0,true)][TestCase(25,130,0,true)][TestCase(0,130,40,true)]
    public void RockBoundsCannotNarrowTheOriginalRoadBoxOrOverheadClearance(float x,float y,float z,bool expected)
    {
        var site=Activator.CreateInstance(Cavern.GetNestedType("CavernJoinSite"));site.GetType().GetField("boundary").SetValue(site,new Vector3(0,105,0));site.GetType().GetField("inward").SetValue(site,Vector3.forward);site.GetType().GetField("roadWidth").SetValue(site,20f);var method=Integration.GetMethod("JoinOriginalClearancePreserved",BindingFlags.Static|BindingFlags.NonPublic);Assert.That((bool)method.Invoke(null,new[]{(object)new Bounds(new Vector3(x,y,z),Vector3.one*4),site}),Is.EqualTo(expected));
    }
    [Test]
    public void SourceMeasuredRejectedBoundsRequireGeometricSeparationNotAWeakerDoorwayBox()
    {
        var site=Activator.CreateInstance(Cavern.GetNestedType("CavernJoinSite"));site.GetType().GetField("boundary").SetValue(site,new Vector3(425.773834f,104.170143f,780));site.GetType().GetField("inward").SetValue(site,Vector3.right);site.GetType().GetField("roadWidth").SetValue(site,18f);var method=Integration.GetMethod("JoinOriginalClearancePreserved",BindingFlags.Static|BindingFlags.NonPublic);
        // Read-only reconstruction from the qualified native triangle and source
        // FBX LOD bounds reproduces V432 rock4's original-box intersection.
        var rejected=new Bounds(new Vector3(441.236115f,146.262325f,761.838409f),new Vector3(7.256602f,7.65f,6.928779f));Assert.That((bool)method.Invoke(null,new object[]{rejected,site}),Is.False);float inset=(float)Integration.GetMethod("JoinCollarMinimumInset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);Assert.That(inset,Is.EqualTo(22f));var translated=rejected;translated.center+=Vector3.right*(inset-16);Assert.That((bool)method.Invoke(null,new object[]{translated,site}),Is.True);Assert.That(rejected.size,Is.EqualTo(translated.size));Assert.That((float)Read(site,"roadWidth"),Is.EqualTo(18f));
    }
    static int GeometryPixelDifference(Color32[] world,Color32[] sky)
    {
        if(world==null||sky==null||world.Length!=sky.Length||world.Length==0)throw new InvalidOperationException("Paired actual world/sky GPU readbacks required.");int count=0;for(int i=0;i<world.Length;i++)if(Math.Max(Math.Max(Math.Abs(world[i].r-sky[i].r),Math.Abs(world[i].g-sky[i].g)),Math.Abs(world[i].b-sky[i].b))>=16)count++;return count;
    }
    [TestCase(false,0)][TestCase(true,1)]
    public void IdenticalSkyboxImagesCannotCountAsRenderedWorldEvidence(bool geometry,int expected)
    {var sky=new[]{new Color32(110,140,180,255),new Color32(110,140,180,255)};var world=(Color32[])sky.Clone();if(geometry)world[1]=new Color32(65,60,55,255);Assert.That(GeometryPixelDifference(world,sky),Is.EqualTo(expected));}
    [Test]
    public void PersistentCanonicalWorldCannotReceiveJoinDecorations()
    {
        const string prefab="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";string before=Hash(prefab);var source=AssetDatabase.LoadAssetAtPath<GameObject>(prefab);int objects=source.GetComponentsInChildren<Transform>(true).Length;var failure=Assert.Throws<TargetInvocationException>(()=>Integration.GetMethod("InstallMeasuredCavernJoinFinish").Invoke(null,new object[]{source.GetComponent(World),"Assets/TruckDragonKingdom/VisualCandidates/NeverCreatedJoinTest",null,null}));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(source.GetComponentsInChildren<Transform>(true).Length,Is.EqualTo(objects));Assert.That(Hash(prefab),Is.EqualTo(before));Assert.That(DirExists("Assets/TruckDragonKingdom/VisualCandidates/NeverCreatedJoinTest"),Is.False);
    }
    [TestCase(18f,true)][TestCase(19f,false)]
    [Timeout(300000)]
    public void ActualV444WallBudgetFitsNormalThicknessOrRejectsImpossibleClearance(float reservedRoadWidth,bool expected)
    {
        string rejected=Argument("-tsDragonJoinRejectedArtReceipt");Assert.That(Path.IsPathRooted(rejected)&&Exists(rejected),Is.True);var recorded=JsonUtility.FromJson(File.ReadAllText(Io(rejected)),Integration.GetNestedType("CavernJoinFinishEvidence"));var attempt=((IEnumerable)Read(recorded,"attempts")).Cast<object>().First();var contact=(Vector3)Read(attempt,"contact");var normal=(Vector3)Read(attempt,"normal");Assert.That(contact.z,Is.EqualTo(763.6875f));Assert.That(normal,Is.EqualTo(Vector3.forward));var sites=(Array)Read(recorded,"sites");Assert.That((float)Read(sites.GetValue(0),"roadWidth"),Is.EqualTo(18f));float actualBudget=(float)Integration.GetMethod("JoinNormalRayBoxBudget",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{contact,normal,sites});Assert.That(actualBudget,Is.EqualTo(.3125f).Within(.00001f),"Actual original native wall-to-unchanged-throat budget");
        string originalSites=JsonUtility.ToJson(recorded),source=(string)Read(attempt,"source"),meshPath=(string)Read(attempt,"nativeMesh"),sourceHash=Hash(source);Assert.That(Hash(meshPath),Is.EqualTo((string)Read(attempt,"nativeMeshSha256")));string qualification=Argument("-tsDragonFinalOriginalRoofReceipt");var proof=JsonUtility.FromJson(File.ReadAllText(Io(qualification)),Integration.GetNestedType("CavernAuthoredRoadEvidence"));var raw=JsonUtility.FromJson(File.ReadAllText(Io((string)Read(proof,"rawReceipt"))),Integration.GetNestedType("CavernPhysicalAuthorityEvidence"));var native=((Array)Read(raw,"diggerSources")).Cast<object>().Single(c=>(string)Read(c,"mesh")==meshPath);Assert.That(Hash(meshPath),Is.EqualTo((string)Read(native,"meshSha256")));var matrix=(Matrix4x4)Read(native,"localToWorld");
        // Mutate only this fresh test-local descriptor for the impossible case;
        // source graph, receipt and authored real18m box remain byte-exact.
        var trial=JsonUtility.FromJson(originalSites,Integration.GetNestedType("CavernJoinFinishEvidence"));var trialSites=(Array)Read(trial,"sites");trialSites.GetValue(0).GetType().GetField("roadWidth").SetValue(trialSites.GetValue(0),reservedRoadWidth);
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();GameObject rock=null,wall=null;
        try
        {
            rock=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source),scene);foreach(var c in rock.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);foreach(var body in rock.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);wall=new GameObject("Read-only actual V444 native Digger triangle fragment"){hideFlags=HideFlags.HideAndDontSave};UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall,scene);wall.transform.SetPositionAndRotation(matrix.GetColumn(3),matrix.rotation);wall.transform.localScale=matrix.lossyScale;Assert.That(wall.transform.localToWorldMatrix,Is.EqualTo(matrix));var collider=wall.AddComponent<MeshCollider>();collider.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);Assert.That(collider.sharedMesh,Is.Not.Null);UnityEngine.Physics.SyncTransforms();Assert.That(collider.Raycast(new Ray(contact+normal*5,-normal),out var nativeHit,10),Is.True);Assert.That(Vector3.Distance(nativeHit.point,contact),Is.LessThan(.0002f),"The frozen real native mesh must reproduce the actual contact before a fitting control counts");
            Integration.GetMethod("OrientJoinSourceRock",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{rock.transform,contact,normal,Vector3.up,0,7f});var fit=Integration.GetMethod("FitJoinNormalThickness",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{rock.transform,contact,normal,trialSites,.85f});Assert.That((bool)Read(fit,"passed"),Is.EqualTo(expected));
            if(expected)
            {
                var bounds=(Bounds)Read(fit,"actualBounds");Assert.That(bounds.max.z,Is.LessThanOrEqualTo(763.95f+.00002f));Assert.That((float)Read(fit,"normalScale"),Is.GreaterThan(0).And.LessThan(1));Assert.That((bool)Read(fit,"originalTangentSpansExact"),Is.True);Assert.That((bool)Read(fit,"boxesClearWithSeparation"),Is.True);int intersections=(int)Integration.GetMethod("JoinPrimaryMeshCrossings",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{rock.transform,new Collider[]{collider}});Assert.That(intersections,Is.GreaterThanOrEqualTo(3),"Actual authored primary triangles must meaningfully cross the frozen native surface; fitting an empty AABB is insufficient");
            }
            else{Assert.That((bool)Read(fit,"zeroNormalPlaneClear"),Is.False);Assert.That((string)Read(fit,"error"),Does.Contain("impossible"));}
            Assert.That(JsonUtility.ToJson(recorded),Is.EqualTo(originalSites));Assert.That(Hash(source),Is.EqualTo(sourceHash));Assert.That(Hash(meshPath),Is.EqualTo((string)Read(attempt,"nativeMeshSha256")));TestContext.WriteLine("ACTUAL_NATIVE_WALL_BUDGET_FIT_NOT_VISUAL_ACCEPTANCE "+JsonUtility.ToJson(fit));
        }
        finally{if(rock!=null)UnityEngine.Object.DestroyImmediate(rock);if(wall!=null)UnityEngine.Object.DestroyImmediate(wall);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Is.EqualTo(active));}
    }
    [TestCase(1f,false)][TestCase(.25f,true)]
    [Timeout(300000)]
    public void ActualV448CurvedCornerRetainsItsNativeContactAndOriginalBoxWhenSourceSizeChanges(float sizeFraction,bool expected)
    {
        string path=Argument("-tsDragonJoinCornerRejectedArtReceipt");Assert.That(Path.IsPathRooted(path)&&Exists(path),Is.True);
        var recorded=JsonUtility.FromJson(File.ReadAllText(Io(path)),Integration.GetNestedType("CavernJoinFinishEvidence"));
        var attempt=((IEnumerable)Read(recorded,"attempts")).Cast<object>().Last();Assert.That((string)Read(attempt,"name"),Is.EqualTo("Measured join 0 rock 32"));
        var contact=(Vector3)Read(attempt,"contact");var normal=(Vector3)Read(attempt,"normal");var sites=(Array)Read(recorded,"sites");var site=sites.GetValue(0);var boundary=(Vector3)Read(site,"boundary");var inward=(Vector3)Read(site,"inward");
        var floor=((IEnumerable)Read(recorded,"floorMeasurements")).Cast<object>().First();var axis=boundary;axis.y=((Vector3)Read(Read(floor,"nativeFloor"),"point")).y+27.5f;
        string source=(string)Read(attempt,"source"),before=Hash(source),original=JsonUtility.ToJson(recorded);var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();GameObject rock=null;
        try
        {
            rock=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source),scene);
            var tangent=Vector3.Cross(inward,(contact-axis).normalized).normalized;
            Integration.GetMethod("OrientJoinSourceRock",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{rock.transform,contact,normal,tangent,32,8.3f*sizeFraction});
            var fit=Integration.GetMethod("FitJoinNormalThickness",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{rock.transform,contact,normal,sites,.85f});
            Assert.That((bool)Read(fit,"passed"),Is.EqualTo(expected));
            if(expected){Assert.That((bool)Read(fit,"originalTangentSpansExact"),Is.True);Assert.That((bool)Read(fit,"boxesClearWithSeparation"),Is.True);}
            else Assert.That((bool)Read(fit,"zeroNormalPlaneClear"),Is.False);
            Assert.That(Hash(source),Is.EqualTo(before));Assert.That(JsonUtility.ToJson(recorded),Is.EqualTo(original));TestContext.WriteLine("ACTUAL_RETAINED_CORNER_CONTACT_SIZE_CONTROL_NOT_FULL_ART_OR_VISUAL_ACCEPTANCE "+JsonUtility.ToJson(fit));
        }
        finally{if(rock!=null)UnityEngine.Object.DestroyImmediate(rock);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Is.EqualTo(active));}
    }
    [TestCase(425.773834f,false)][TestCase(439.0625f,true)][TestCase(614.226135f,true)][TestCase(615.626f,false)]
    public void ExactRecordedNativeFlatFootprintDoesNotSwallowTheRisingWestPortal(float x,bool expected)
    {
        // Exact V385 signed floor receipt, linked by V432 -> raw V428
        // geometryReceiptSha256; this is region-selection regression only.
        var footprint=new Bounds(new Vector3(527.34375f,94.5f,789.84375f),new Vector3(176.5625f,21,129.6875f));var method=Integration.GetMethod("JoinInsideRecordedFlatFloor",BindingFlags.Static|BindingFlags.NonPublic);Assert.That((bool)method.Invoke(null,new object[]{footprint,new Vector3(x,104.170143f,780)}),Is.EqualTo(expected));
    }
    [TestCase(105.001f,1f,true)][TestCase(104.6f,1f,false)][TestCase(105f,.5f,false)]
    public void InteriorNative105FloorToleranceAndNormalRemainUnchanged(float height,float normal,bool expected)
    {Assert.That((bool)Integration.GetMethod("JoinRecordedFlatFloorPasses",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{height,normal,105f}),Is.EqualTo(expected));}
    [TestCase(.65f,3f)][TestCase(.75f,3f)][TestCase(.85f,4f)]
    public void EmbeddingUsesActualPrimaryVerticesInsteadOfTheRendererBox(float quantile,float expected)
    {
        var method=Integration.GetMethod("JoinEmbeddingOffset",BindingFlags.Static|BindingFlags.NonPublic);var projections=new[]{5f,0f,1f,2f,3f,4f};Assert.That((float)method.Invoke(null,new object[]{projections,quantile}),Is.EqualTo(expected));Assert.That(projections,Is.EqualTo(new[]{5f,0f,1f,2f,3f,4f}),"Authored source vertex ordering is not mutated");
        Assert.That(Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{new[]{0f,float.NaN,2f},quantile})).InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [TestCase(false)][TestCase(true)]
    public void RealPrimaryTriangleCrossingsRejectAContactInsideAnEmptyRendererBounds(bool actualIntersection)
    {
        // Collider.Raycast targets the explicit native component, not a global
        // Physics query, so the isolated preview scene is the intended scope.
        var incoming=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=incoming.isDirty;var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();GameObject rock=null,wall=null;Mesh mesh=null;
        try
        {
            rock=new GameObject("Temporary primary source mesh witness"){hideFlags=HideFlags.HideAndDontSave};wall=new GameObject("Temporary native wall witness"){hideFlags=HideFlags.HideAndDontSave};UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rock,scene);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall,scene);var surface=wall.AddComponent<BoxCollider>();surface.size=new Vector3(10,10,.5f);mesh=new Mesh();
            mesh.vertices=actualIntersection?new[]{new Vector3(-2,-2,-2),new Vector3(2,-2,2),new Vector3(0,2,2)}:new[]{new Vector3(-2,-2,-2),new Vector3(2,-2,-2),new Vector3(0,2,-2),new Vector3(-2,-2,2),new Vector3(2,-2,2),new Vector3(0,2,2)};mesh.triangles=actualIntersection?new[]{0,1,2}:new[]{0,1,2,3,4,5};mesh.RecalculateBounds();rock.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=rock.AddComponent<MeshRenderer>();UnityEngine.Physics.SyncTransforms();Assert.That(renderer.bounds.Contains(Vector3.zero),Is.True,"Both native controls have an AABB containing the contact");
            var ray=new Ray(new Vector3(0,0,-3),Vector3.forward);Assert.That(surface.Raycast(ray,out _,6),Is.True,"Positive native wall query must work before testing source geometry");int count=(int)Integration.GetMethod("JoinPrimaryMeshCrossings",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{rock.transform,new Collider[]{surface}});Assert.That(count>=3,Is.EqualTo(actualIntersection));
        }
        finally{if(rock!=null)UnityEngine.Object.DestroyImmediate(rock);if(wall!=null)UnityEngine.Object.DestroyImmediate(wall);if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Is.EqualTo(incoming));Assert.That(incoming.isDirty,Is.EqualTo(dirty));}
    }
    [Serializable]sealed class DeclaredSourceCheck
    {public string logicalPath,physicalPath,expectedSha256,actualSha256,packageName,packageVersion,error;public bool exists,exact;}
    [Serializable]sealed class DeclaredSourceInventory
    {public string scope="EVERY_RECORDED_V410_LOGICAL_SOURCE_RESOLVED_THROUGH_PROJECT_OR_REGISTERED_PACKAGE_AND_CHECKED_BYTE_EXACT_NO_SOURCE_DROPS",status="IN_PROGRESS",sourceReceipt,sourceReceiptSha256;public int requiredSources,assetSources,packageSources,qualificationEvidenceSources;public bool allPresentAndExact;public DeclaredSourceCheck[] checks;}
    static string[] RequireCurrentRasterDeclaredSources(object source,string[] declared,out string[] qualification)
    {
        string preview=(string)Read(source,"sourcePreview"),data=(string)Read(source,"sourceData");const string prefix="Assets/TruckDragonKingdom/VisualCandidates/";Assert.That(preview,Does.StartWith(prefix));string tag=preview.Substring(prefix.Length).Split('/')[0];Assert.That(preview,Is.EqualTo(prefix+tag+"/Cavern Input Probe.unity"));Assert.That(data,Is.EqualTo("Assets/DiggerData/Scenes/DK-"+tag));Assert.That(Read(source,"sourceRasterClearanceVerified"),Is.True);
        var probe=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);qualification=(string[])probe.GetMethod("RequirePassedRasterCaveCopySource",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{tag});Assert.That(qualification.Length,Is.EqualTo(3));
        string Rock(string name)=>(string)probe.GetField(name,BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        var expected=AssetDatabase.GetDependencies(preview,true).Concat(new[]{preview,Rock("CavernRoughRockDiffuse"),Rock("CavernRoughRockNormal")}).Concat(Directory.GetFiles(data,"*",SearchOption.AllDirectories)).Concat(qualification).SelectMany(path=>new[]{path,path+".meta"}).Where(Exists).Distinct().OrderBy(path=>path,StringComparer.Ordinal).ToArray();
        Assert.That(declared,Is.EqualTo(expected),"Every actual current source dependency, voxel member, metadata and all three passed raster qualification files must be declared; no fixed old count or omitted files");return expected;
    }
    static void RequireEveryDeclaredRetainedSource(object source,string receipt,string output)
    {
        var paths=(string[])Read(source,"sourcePaths");var hashes=(string[])Read(source,"sourceSha256");Assert.That(paths,Is.Not.Null);Assert.That(hashes,Is.Not.Null);bool raster=(bool)Read(source,"sourceRasterClearanceVerified");string[] qualification=Array.Empty<string>(),expected=null;if(raster)expected=RequireCurrentRasterDeclaredSources(source,paths,out qualification);else Assert.That(paths.Length,Is.EqualTo(2948),"Exact retained V410 source cohort required");Assert.That(paths.Length,Is.EqualTo(hashes.Length));var inventory=new DeclaredSourceInventory{sourceReceipt=receipt,sourceReceiptSha256=Hash(receipt),requiredSources=paths.Length};if(raster)inventory.scope="EVERY_ACTUAL_SOURCE_QUALIFIED_RASTER_DEPENDENCY_VOXEL_METADATA_AND_PASSED_NATIVE_EVIDENCE_FILE_BYTE_EXACT_NO_SOURCE_DROPS";var checks=new List<DeclaredSourceCheck>();
        try
        {
            for(int i=0;i<paths.Length;i++)
            {
                string path=paths[i];var row=new DeclaredSourceCheck{logicalPath=path,expectedSha256=hashes[i]};checks.Add(row);
                try
                {
                    Assert.That(path.StartsWith("Assets/",StringComparison.Ordinal)||path.StartsWith("Packages/",StringComparison.Ordinal)||qualification.Contains(path,StringComparer.Ordinal),Is.True,"Original logical source identity or exact passed raster qualification file required");string digest=hashes[i].Replace("-","").ToLowerInvariant();Assert.That(digest.Length==64&&digest.All(Uri.IsHexDigit),Is.True,"Exact64hex SHA256 with the retained optional hyphen representation required");row.physicalPath=Io(path);row.exists=File.Exists(row.physicalPath);Assert.That(row.exists,Is.True,"Declared source missing, no substitution: "+path);row.actualSha256=Hash(path);row.exact=row.actualSha256==digest;
                    if(path.StartsWith("Packages/",StringComparison.Ordinal)){var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/"+path.Split('/')[1]);row.packageName=package.name;row.packageVersion=package.version;inventory.packageSources++;}else if(qualification.Contains(path,StringComparer.Ordinal))inventory.qualificationEvidenceSources++;else inventory.assetSources++;Assert.That(row.exact,Is.True,"Declared original source bytes changed: "+path);
                }
                catch(Exception error){row.error=error.ToString();throw;}
            }
            Assert.That(inventory.assetSources,Is.EqualTo(raster?expected.Count(p=>p.StartsWith("Assets/",StringComparison.Ordinal)):2916));Assert.That(inventory.qualificationEvidenceSources,Is.EqualTo(raster?3:0));Assert.That(inventory.packageSources,Is.EqualTo(32));inventory.allPresentAndExact=true;
        }
        finally{inventory.checks=checks.ToArray();inventory.status=inventory.allPresentAndExact?(raster?"ALL_ACTUAL_RASTER_CAVE_SOURCES_AND_QUALIFICATION_FILES_BYTE_EXACT_WITH32_REGISTERED_PACKAGE_FILES":"ALL2948_DECLARED_SOURCES_BYTE_EXACT_WITH32_REGISTERED_PACKAGE_FILES"):"REJECTED_DECLARED_SOURCE_RESOLUTION_OR_BYTES_NO_RENDER_ACCEPTANCE";File.WriteAllText(Io(Path.Combine(output,"actual-retained-source-inventory.json")),JsonUtility.ToJson(inventory,true));}
    }
    [Serializable]sealed class RenderedFrame
    {public string image,skyControl,sceneCullingMask;public int width,height,geometryDifferencePixels,totalPixels;public float visibleGeometryFraction;}
    [Serializable]sealed class VisualEvidence
    {public string lightingAuthority="EXISTING_WORLD_PREVIEW_PRESENTATION_42_MINUS32_WARM1_POINT4_SAME_BOTH_PHASES_NOT_SAVED_LIGHTING_ACCEPTANCE";public string status="IN_PROGRESS",sourceReceipt,sourceReceiptSha256,sourcePrefab,sourcePrefabSha256,candidatePrefab,error;public bool originalSourcesExact,originalSourceMembershipExact,originalSceneContextRestored;public string[] images;public List<RenderedFrame> frames=new List<RenderedFrame>();}
    static void Capture(GameObject world,string folder,string phase,List<string> images,List<RenderedFrame> frames)
    {
        ulong sceneMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(world.scene);if(sceneMask==0)throw new InvalidOperationException("Actual prefab preview scene must have a nonempty camera culling authority.");
        var go=new GameObject("Temporary actual cavern join comparison camera"){hideFlags=HideFlags.HideAndDontSave};UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,world.scene);var camera=go.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.15f;camera.farClipPlane=6500;camera.fieldOfView=67;camera.clearFlags=CameraClearFlags.Skybox;camera.overrideSceneCullingMask=sceneMask;
        var lightObject=new GameObject("Temporary existing kingdom preview lighting"){hideFlags=HideFlags.HideAndDontSave};UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,world.scene);var sun=lightObject.AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.89f,.72f);sun.intensity=1.4f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(42,-32,0);
        var eyes=new[]{new Vector3(448,113,780),new Vector3(556,120,829),new Vector3(525,146,805)};var targets=new[]{new Vector3(530,134,799),new Vector3(483,128,775),new Vector3(510,125,774)};
        try{foreach(int width in new[]{1280,1920}){int height=width*9/16;for(int i=0;i<eyes.Length;i++){camera.transform.SetPositionAndRotation(eyes[i],Quaternion.LookRotation(targets[i]-eyes[i],Vector3.up));camera.aspect=(float)width/height;var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);var sky=new Texture2D(width,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;int originalMask=camera.cullingMask;try{target.Create();camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();string path=Path.Combine(folder,phase+"-view"+i+"-"+width+".png");File.WriteAllBytes(Io(path),texture.EncodeToPNG());camera.cullingMask=0;camera.Render();camera.Render();RenderTexture.active=target;sky.ReadPixels(new Rect(0,0,width,height),0,0);sky.Apply();string control=Path.Combine(folder,phase+"-view"+i+"-"+width+"-sky-control.png");File.WriteAllBytes(Io(control),sky.EncodeToPNG());int pixels=GeometryPixelDifference(texture.GetPixels32(),sky.GetPixels32());var frame=new RenderedFrame{image=path,skyControl=control,sceneCullingMask=camera.overrideSceneCullingMask.ToString("X16"),width=width,height=height,geometryDifferencePixels=pixels,totalPixels=width*height,visibleGeometryFraction=(float)pixels/(width*height)};frames.Add(frame);File.WriteAllText(Io(Path.Combine(folder,"actual-frame-"+phase+"-"+i+"-"+width+".json")),JsonUtility.ToJson(frame,true));if(frame.visibleGeometryFraction<.01f)throw new InvalidOperationException("Actual cavern rendering is skybox-only or lacks1percent visible geometry; PNG existence is not visual acceptance: "+path);images.Add(path);}finally{camera.cullingMask=originalMask;camera.targetTexture=null;RenderTexture.active=prior;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(sky);}}}}finally{UnityEngine.Object.DestroyImmediate(lightObject);UnityEngine.Object.DestroyImmediate(go);}
    }
    [Test,Timeout(600000)]
    public void ActualRetainedRoughRockCaveReceivesMeasuredJoinArtAndBeforeAfterImages()
    {
        string receiptPath=Argument("-tsDragonCavernJoinSourceReceipt"),tag=Argument("-tsDragonCavernJoinCandidate");Assert.That(Path.IsPathRooted(receiptPath)&&Exists(receiptPath),Is.True);Assert.That(tag.Length>0&&tag.Length<=40&&tag.All(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_'),Is.True);var probe=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);var source=JsonUtility.FromJson(File.ReadAllText(Io(receiptPath)),probe.GetNestedType("SavedRockCopyReceipt"));foreach(string gate in new[]{"originalSourcesExact","sourceVoxelMembershipExact","terrainCopiesExact","providerCopiesExact","savedReloaded","stableGeometryExact","stableProviderExact","sourceMeshesReferencedExact","copiedTerrainFilesExact"})Assert.That((bool)Read(source,gate),Is.True,gate);string prefab=(string)Read(source,"prefab");Assert.That(prefab,Does.StartWith("Assets/TruckDragonKingdom/VisualCandidates/"));Assert.That(Exists(prefab),Is.True);string folder="Assets/TruckDragonKingdom/VisualCandidates/"+tag;Assert.That(DirExists(folder)||Exists(folder+".meta"),Is.False);string output=Path.Combine(Argument("-tsDragonOutput"),"CavernJoinFinish");Assert.That(DirExists(output),Is.False);Directory.CreateDirectory(Io(output));
        RequireEveryDeclaredRetainedSource(source,receiptPath,output);var declaredPaths=(string[])Read(source,"sourcePaths");
        var paths=AssetDatabase.GetDependencies(prefab,true).Concat(declaredPaths).Concat(new[]{prefab,receiptPath}).Where(Exists).SelectMany(p=>Exists(p+".meta")?new[]{p,p+".meta"}:new[]{p}).Distinct().ToArray();var bytes=paths.ToDictionary(p=>p,Hash);string data=(string)Read(source,"sourceData");var membership=Directory.GetFiles(Io(data),"*",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal).ToArray();var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        // The existing authoritative pavement helper owns GLOBAL PhysX queries.
        // Reuse the proven registered/additive native harness, preserving any
        // interactive/dirty user scene rather than saving it automatically.
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);if(!string.IsNullOrEmpty(scene.path))continue;
            if(!Application.isBatchMode||scene.isDirty)Assert.Ignore("Preserving interactive or dirty untitled scene; actual portal PhysX requires the isolated native runner.");
            const string harness="Assets/TruckDragonKingdom/Tests/Generated/";Directory.CreateDirectory(Io(harness));AssetDatabase.Refresh();Assert.That(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,harness+"JoinArtRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        }
        string Context()=>string.Join("|",Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>{var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);return scene.handle+":"+scene.path+":"+scene.isLoaded+":"+scene.isDirty;}));
        string initialContext=Context();bool initialDirty=active.isDirty;UnityEngine.SceneManagement.Scene registered=default;GameObject loaded=null;var evidence=new VisualEvidence{sourceReceipt=receiptPath,sourceReceiptSha256=Hash(receiptPath),sourcePrefab=prefab,sourcePrefabSha256=Hash(prefab),candidatePrefab=folder+"/Measured Cavern Join Art TEST ONLY.prefab"};var images=new List<string>();
        try
        {
            registered=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);loaded=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab),registered);UnityEngine.Physics.SyncTransforms();Assert.That(loaded.GetComponentsInChildren<MeshFilter>(true).Count(f=>f.sharedMesh!=null),Is.EqualTo((int)Read(source,"nativeMeshReferences")));probe.GetMethod("ValidateSavedOwnedCavernRockMaterials").Invoke(null,new[]{loaded.GetComponent(World),Read(source,"materials")});Capture(loaded,output,"before",images,evidence.frames);var art=Integration.GetMethod("InstallMeasuredCavernJoinFinish").Invoke(null,new object[]{loaded.GetComponent(World),folder,Argument("-tsDragonCavernOccupancyEvidence"),Argument("-tsDragonCavernLiveEvidence")});Assert.That((bool)Read(art,"originalTransformsMeshesPhysicsTerrainAndGraphExact"),Is.True);Assert.That((bool)Read(art,"originalSourceBytesExact"),Is.True);Assert.That((bool)Read(art,"allOriginalClearancesPreserved"),Is.True);Assert.That(((IEnumerable)Read(art,"floorMeasurements")).Cast<object>().Count(r=>(bool)Read(r,"passed")),Is.EqualTo(2));Assert.That((float)Read(art,"chamberwardInset"),Is.EqualTo(0));Assert.That((bool)Read(art,"everyPrimaryMeshActuallyIntersectsNative"),Is.True);Assert.That((bool)Read(art,"everyBandActuallyConnected"),Is.True);Assert.That(((Array)Read(art,"rocks")).Length,Is.EqualTo((int)Read(art,"expectedPieces")));Assert.That(((IEnumerable)Read(art,"attempts")).Cast<object>().Count(a=>(bool)Read(a,"passed")),Is.EqualTo((int)Read(art,"expectedPieces")));Capture(loaded,output,"after",images,evidence.frames);Assert.That(PrefabUtility.SaveAsPrefabAsset(loaded,evidence.candidatePrefab),Is.Not.Null);evidence.status="ACTUAL_MEASURED_CAVERN_JOIN_ART_AND_IMAGES_RECORDED_REQUIRES_DIRECT_VISUAL_REVIEW_AND_FULL_WORLD_ACCEPTANCE";
        }
        catch(Exception error){evidence.error=error.ToString();evidence.status="REJECTED_MEASURED_CAVERN_JOIN_ART_OR_RENDER";throw;}
        finally
        {
            if(loaded!=null)UnityEngine.Object.DestroyImmediate(loaded);if(registered.IsValid()&&registered.isLoaded)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(registered,true);if(active.IsValid()&&active.isLoaded)UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(active);evidence.originalSourcesExact=bytes.All(p=>Exists(p.Key)&&Hash(p.Key)==p.Value);evidence.originalSourceMembershipExact=membership.SequenceEqual(Directory.GetFiles(Io(data),"*",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal));evidence.originalSceneContextRestored=UnityEngine.SceneManagement.SceneManager.GetActiveScene()==active&&active.isDirty==initialDirty&&Context()==initialContext;evidence.images=images.ToArray();File.WriteAllText(Io(Path.Combine(output,"actual-cavern-join-art-images.json")),JsonUtility.ToJson(evidence,true));Assert.That(evidence.originalSourcesExact&&evidence.originalSourceMembershipExact&&evidence.originalSceneContextRestored,Is.True,"Original retained Cave17 source/voxel/scene context must remain exact");
        }
        Assert.That(images.Count,Is.EqualTo(12));TestContext.WriteLine(JsonUtility.ToJson(evidence));
    }
}
#endif
