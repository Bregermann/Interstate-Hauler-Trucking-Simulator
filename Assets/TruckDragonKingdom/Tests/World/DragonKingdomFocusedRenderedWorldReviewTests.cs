#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonKingdomFocusedRenderedWorldReviewTests
{
    static string Preview=>WorldPath("Scenes/DragonKingdomPreview.unity","DragonKingdomPreview.unity");
    static string Prefab=>WorldPath("Prefabs/Shared Dragon Kingdom.prefab","Shared Dragon Kingdom.prefab");
    static string Definition=>WorldPath("Configuration/Dragon Kingdom.asset","Dragon Kingdom.asset");
    static readonly Vector2Int RenderSize=RequiredRenderSize(Environment.GetCommandLineArgs());
    static int Width=>RenderSize.x;
    static int Height=>RenderSize.y;
    static Vector2Int RequiredRenderSize(string[] args)
    {
        const string flag="-tsDragonWorldRenderResolution";int at=Array.IndexOf(args,flag);if(at<0)return new Vector2Int(1280,720);if(args.Count(a=>a==flag)!=1||at+1>=args.Length)throw new ArgumentException("One explicit world render resolution required.");switch(args[at+1]){case "1280x720":return new Vector2Int(1280,720);case "1920x1080":return new Vector2Int(1920,1080);default:throw new ArgumentException("World acceptance views support exactly1280x720 or1920x1080; unsupported values cannot silently fall back.");}
    }
    [TestCase(null,1280,720),TestCase("1280x720",1280,720),TestCase("1920x1080",1920,1080)]
    public void ActualWorldReviewUsesExplicitRequestedRenderDimensionsWithOriginalDefault(string selection,int width,int height)
    {var args=selection==null?Array.Empty<string>():new[]{"-tsDragonWorldRenderResolution",selection};Assert.That(RequiredRenderSize(args),Is.EqualTo(new Vector2Int(width,height)));}
    [Test]
    public void InvalidOrDuplicateWorldRenderResolutionCannotSilentlyProduceWrongEvidenceDimensions()
    {
        Assert.Throws<ArgumentException>(()=>RequiredRenderSize(new[]{"-tsDragonWorldRenderResolution"}));Assert.Throws<ArgumentException>(()=>RequiredRenderSize(new[]{"-tsDragonWorldRenderResolution","3840x2160"}));Assert.Throws<ArgumentException>(()=>RequiredRenderSize(new[]{"-tsDragonWorldRenderResolution","1280x720","-tsDragonWorldRenderResolution","1920x1080"}));
    }
    static int settledFrames;static double settledRealSeconds;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckDragonKingdom."+name+", Assembly-CSharp",true);
    static object Field(object source,string name)=>source.GetType().GetField(name).GetValue(source);
    [Serializable]sealed class Review
    {
        public string scope="CONTROLLED_NATIVE_EDITMODE_CAMERA_RENDER_NOT_WINDOWS_GAMEPLAY_HUMAN_OR_PERFORMANCE_ACCEPTANCE";
        public string preview,prefab,definition,unityVersion;
        public int width,height,sceneRoots,gameObjects,renderers,meshFilters,colliders,rigidbodies,terrains,roadMeshColliders,graphNodes,graphEdges;
        public ImportRenderEvidence artistImports;public AssemblyRenderEvidence assemblyImports;public NativeCohortRenderEvidence nativeImports;public ArchitectureRenderEvidence architectureImports;public RegionEvidence[] regions;public ArchitectureFrameEvidence[] architectureFrames;public ExtractionEvidence[] extractions;public MaterialEvidence[] materials;
    }
    [Serializable]sealed class CandidateReview{public CoastReview outerBand;public FinishReview finish;public ArchitectureCompositionReview finishedArchitecture;}
    [Serializable]sealed class ArchitectureCompositionReview{public ArchitectureGroupReview[] groups;}
    [Serializable]sealed class ArchitectureGroupReview{public string key,region,name;public Bounds bounds;}
    [Serializable]sealed class ArchitectureFrameEvidence{public string key,region,path;public Bounds actualBounds,declaredBounds;public int actualRenderers;public CaptureEvidence eye,overview;}
    [Serializable]sealed class FinishReview{public int compositionSchema;}
    [Serializable]sealed class ImportRenderEvidence{public string preflightReceipt,preflightReceiptSha256,provenance,provenanceSha256;public int approvedNativeImports,protectedProperties,convertedProperties;public bool preparedBeforeSceneHashes;}
    [Serializable]sealed class AssemblyRenderEvidence{public string preflightReceipt,preflightReceiptSha256,provenance,provenanceSha256;public int approvedNativeImports,protectedProperties,convertedProperties,addedProperties,metadataChanges;public bool preparedBeforeSceneHashes;}
    [Serializable]sealed class NativeCohortRenderEvidence{public string preflightReceipt,preflightReceiptSha256;public int protectedModels,sourceFilesWatched;public bool preparedBeforeSceneHashes,passed;}
    [Serializable]sealed class ArchitectureRenderEvidence{public string preflightReceipt,preflightReceiptSha256;public int layouts,sourceInstances,sourceDependencies,rawMaterials;public bool preparedBeforeSceneHashes,passed;}
    [Serializable]sealed class CoastReview{public string status;public int outerTerrains,totalTerrains;public bool coreHeightmapsExact,coreColliderGeometryExact,sourceBytesPreserved;}
    [Serializable]sealed class RegionEvidence
    {
        public string id,path;public Bounds actualArtBounds;public int actualArtRenderers,truckStarts,perches,nests;
        public CaptureEvidence eye,overview;
    }
    [Serializable]sealed class CaptureEvidence
    {
        public string png;public Vector3 cameraPosition,cameraEuler,target;public int frustumRegionRenderers,nonBackgroundPixels,occupiedScreenTiles,settledEditorFrames;public double settledRealSeconds;
    }
    [Serializable]sealed class ExtractionEvidence
    {
        public string id,path;public Bounds secureBounds;public Vector3 approach;public int solidDeckColliders,externalDepartureWaypoints;public bool movingVessel;
    }
    [Serializable]sealed class MaterialEvidence {public string name,path,shader;public bool supported;}

    [UnityTest,Timeout(300000)]
    public IEnumerator AcceptedPreviewRendersNineRealRegionsAtEyeLevelAndOverviewWithoutRegeneration()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False,"This is a controlled native EditMode review fixture.");
        foreach(string path in new[]{Preview,Prefab,Definition})Assert.That(File.Exists(path),Is.True,"Complete the real world builder and its original physical gates first: "+path);
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");
        Assert.That(at>=0&&at+1<args.Length,Is.True,"Supply an explicit -tsDragonOutput evidence directory.");
        string output=Path.GetFullPath(args[at+1]);Directory.CreateDirectory(output);
        ImportRenderEvidence importEvidence=null;AssemblyRenderEvidence assemblyEvidence=null;NativeCohortRenderEvidence nativeEvidence=null;var importFiles=new List<string>();string integrationPath=Path.GetDirectoryName(Prefab)+"/Integration Receipt.json";
        int preflightComposition=File.Exists(integrationPath)?JsonUtility.FromJson<CandidateReview>(File.ReadAllText(integrationPath)).finish?.compositionSchema??0:0;Assert.That(preflightComposition,Is.EqualTo(0).Or.EqualTo(2).Or.EqualTo(3).Or.EqualTo(4),"Unknown composition cannot skip exact native material preflight.");
        ArchitectureRenderEvidence architectureEvidence=null;
        if(preflightComposition==4)
        {
            var integration=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);var proof=integration.GetMethod("CaptureArchitectureMaterialPreflight").Invoke(null,null);foreach(string field in new[]{"passed","sourceBytesPreserved","allRawSnapshotsExact","allSourceIdentitiesExact","noUnknownDependencyChanges","temporaryVariantsDestroyed"})Assert.That((bool)Field(proof,field),Is.True,field);string path=Path.Combine(output,"architecture-material-native-preflight.json");Assert.That(File.Exists(path),Is.True);architectureEvidence=new ArchitectureRenderEvidence{preflightReceipt=path,preflightReceiptSha256=FileSha(path),layouts=(int)Field(proof,"layouts"),sourceInstances=(int)Field(proof,"sourceInstances"),sourceDependencies=(int)Field(proof,"sourceDependencies"),rawMaterials=(int)Field(proof,"rawMaterials"),preparedBeforeSceneHashes=true,passed=true};Assert.That(architectureEvidence.layouts,Is.EqualTo(4));Assert.That(architectureEvidence.sourceInstances,Is.EqualTo(678));Assert.That(architectureEvidence.sourceDependencies,Is.EqualTo(518));Assert.That(architectureEvidence.rawMaterials,Is.EqualTo(26));importFiles.Add(path);foreach(var dependency in (Array)Field(proof,"dependencies")){string source=(string)Field(dependency,"path");importFiles.AddRange(new[]{source,source+".meta"});}foreach(string layout in (string[])integration.GetField("ArchitectureLayoutPaths").GetValue(null))importFiles.AddRange(new[]{layout,layout+".meta"});
        }
        if(preflightComposition==2||preflightComposition==3||preflightComposition==4)
        {
            // Public vendor import/save preparation is a separate explicit
            // phase BEFORE freezing any render source or scene hashes. Every
            // frame then uses the exact whole-world-approved material output.
            var integration=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);if(preflightComposition!=4)integration.GetMethod("CaptureArtistSourcePreflight").Invoke(null,null);var proof=integration.GetProperty("LastArtistSourcePreflight").GetValue(null);var imports=Field(proof,"materialImports");Assert.That((bool)Field(proof,"sourceBytesPreserved"),Is.True);Assert.That((bool)Field(imports,"passed"),Is.True);
            string preflight=Path.Combine(output,"artist-source-preflight.json");Assert.That(File.Exists(preflight),Is.True);importEvidence=new ImportRenderEvidence{preflightReceipt=preflight,preflightReceiptSha256=FileSha(preflight),provenance=(string)Field(imports,"provenance"),provenanceSha256=(string)Field(imports,"provenanceSha256"),approvedNativeImports=(int)Field(imports,"approvedNativeImports"),protectedProperties=(int)Field(imports,"protectedProperties"),convertedProperties=(int)Field(imports,"convertedProperties"),preparedBeforeSceneHashes=true};
            Assert.That(importEvidence.approvedNativeImports,Is.EqualTo(19));Assert.That(importEvidence.protectedProperties,Is.EqualTo(769));Assert.That(importEvidence.convertedProperties,Is.EqualTo(2));Assert.That(FileSha(importEvidence.provenance),Is.EqualTo(importEvidence.provenanceSha256));
            importFiles.AddRange(new[]{preflight,importEvidence.provenance,importEvidence.provenance+".meta",(string)Field(proof,"layout"),(string)Field(proof,"layout")+".meta"});
            foreach(var binding in (Array)Field(imports,"bindings")){string material=(string)Field(binding,"path"),snapshot=(string)Field(binding,"snapshot");importFiles.AddRange(new[]{material,material+".meta",snapshot,snapshot+".meta"});}
        }
        if(preflightComposition==3||preflightComposition==4)
        {
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);if(preflightComposition!=4)type.GetMethod("CaptureExact51AssemblyMaterialProof").Invoke(null,null);var proof=type.GetProperty("LastAssemblyMaterialEvidence").GetValue(null);Assert.That((bool)Field(proof,"passed"),Is.True);string path=Path.Combine(output,"exact51-assembly-material-proof.json");assemblyEvidence=new AssemblyRenderEvidence{preflightReceipt=path,preflightReceiptSha256=FileSha(path),provenance=(string)Field(proof,"provenance"),provenanceSha256=(string)Field(proof,"provenanceSha256"),approvedNativeImports=(int)Field(proof,"approvedNativeImports"),protectedProperties=(int)Field(proof,"protectedProperties"),convertedProperties=(int)Field(proof,"convertedProperties"),addedProperties=(int)Field(proof,"addedProperties"),metadataChanges=(int)Field(proof,"metadataChanges"),preparedBeforeSceneHashes=true};Assert.That(assemblyEvidence.approvedNativeImports,Is.EqualTo(51));Assert.That(assemblyEvidence.protectedProperties,Is.EqualTo(2544));Assert.That(assemblyEvidence.convertedProperties,Is.EqualTo(6));Assert.That(assemblyEvidence.addedProperties,Is.EqualTo(347));Assert.That(assemblyEvidence.metadataChanges,Is.EqualTo(358));Assert.That(FileSha(assemblyEvidence.provenance),Is.EqualTo(assemblyEvidence.provenanceSha256));importFiles.AddRange(new[]{path,assemblyEvidence.provenance,assemblyEvidence.provenance+".meta"});foreach(var binding in (Array)Field(proof,"bindings")){string material=(string)Field(binding,"path"),snapshot=(string)Field(binding,"snapshot");importFiles.AddRange(new[]{material,material+".meta",snapshot,snapshot+".meta"});}
        }
        if(preflightComposition==3||preflightComposition==4)
        {
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);var proof=type.GetMethod("PrepareValidatedQuayNativeImports").Invoke(null,null);foreach(string flag in new[]{"passed","allUnknownChangesRejected","protectedModelIdentitiesExact","nonMaterialSourceBytesExact"})Assert.That((bool)Field(proof,flag),Is.True,flag);string receiptPath=Path.Combine(output,"validated-world-source-import-preflight.json");File.WriteAllText(receiptPath,JsonUtility.ToJson(proof,true));nativeEvidence=new NativeCohortRenderEvidence{preflightReceipt=receiptPath,preflightReceiptSha256=FileSha(receiptPath),protectedModels=(int)Field(proof,"protectedModels"),sourceFilesWatched=(int)Field(proof,"sourceFilesWatched"),preparedBeforeSceneHashes=true,passed=true};Assert.That(nativeEvidence.protectedModels,Is.EqualTo(3));importFiles.Add(receiptPath);var roots=(string[])type.GetMethod("QuayNativeImportSourcePaths").Invoke(null,null);importFiles.AddRange(roots.SelectMany(p=>AssetDatabase.GetDependencies(p,true).Concat(new[]{p})).SelectMany(p=>new[]{p,p+".meta"}).Distinct().Where(p=>File.Exists(Io(p))));
        }
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))
            {
                const string generated="Assets/TruckDragonKingdom/Tests/World/Generated";
                Directory.CreateDirectory(generated);AssetDatabase.Refresh();
                Assert.That(EditorSceneManager.SaveScene(scene,generated+"/RunnerScene-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            }
            Assert.That(scene.isDirty,Is.False,"Preserve original user edits before isolated rendering: "+scene.name);
            Assert.That(!string.IsNullOrEmpty(scene.path)||scene.rootCount==0,Is.True);
        }
        var prior=EditorSceneManager.GetSceneManagerSetup();
        var hashes=prior.Where(s=>!string.IsNullOrEmpty(s.path)&&File.Exists(s.path)).Select(s=>s.path).Concat(new[]{Preview,Prefab,Definition}).Concat(importFiles).Distinct().ToDictionary(p=>p,p=>Tuple.Create(new FileInfo(Io(p)).Length,FileSha(p)));
        Camera camera=null;bool priorAsyncCompilation=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        try
        {
            var scene=EditorSceneManager.OpenScene(Preview,OpenSceneMode.Single);
            var world=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("DragonKingdomWorld"));
            Assert.That(world,Is.Not.Null);world.GetType().GetMethod("ValidateReferences").Invoke(world,null);
            var definition=Field(world,"definition");Assert.That(AssetDatabase.GetAssetPath((UnityEngine.Object)definition),Is.EqualTo(Definition));
            var graph=Field(definition,"graph");
            var author=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAuthoring, Assembly-CSharp-Editor",true);
            Physics.SyncTransforms();author.GetMethod("ValidatePhysicalRoads").Invoke(null,new[]{(object)world.transform,graph});
            var objects=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var renderers=objects.SelectMany(t=>t.GetComponents<Renderer>()).ToArray();
            var colliders=objects.SelectMany(t=>t.GetComponents<Collider>()).ToArray();
            var terrains=objects.SelectMany(t=>t.GetComponents<Terrain>()).ToArray();
            int expectedTerrainCount=4;string integration=Path.GetDirectoryName(Prefab)+"/Integration Receipt.json";
            if(File.Exists(integration))
            {
                var composition=JsonUtility.FromJson<CandidateReview>(File.ReadAllText(integration));
                // JsonUtility can materialize an absent inline-class field as
                // an empty DTO. Require the actual attached root or declared
                // nonzero tile count before applying the coastal contract.
                if(world.transform.Find("Owned Vista coastal outskirts")!=null||composition.outerBand!=null&&composition.outerBand.outerTerrains!=0)
                {var coast=composition.outerBand;Assert.That(coast,Is.Not.Null);Assert.That(coast.status,Is.EqualTo("ATTACHED_REAL_VISTA_COAST_REQUIRES_FULL_WORLD_RENDER_AND_FLIGHT_REVIEW"));Assert.That(coast.outerTerrains,Is.EqualTo(12));Assert.That(coast.totalTerrains,Is.EqualTo(16));Assert.That(coast.coreHeightmapsExact&&coast.coreColliderGeometryExact&&coast.sourceBytesPreserved,Is.True);expectedTerrainCount=16;}
            }
            Assert.That(terrains.Length,Is.EqualTo(expectedTerrainCount));Assert.That(Terrain.activeTerrains.All(t=>t.gameObject.scene==scene),Is.True,"No unrelated scene terrain may enter the evidence.");
            if(expectedTerrainCount==16)
            {
                var linking=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);linking.GetMethod("LinkActualTerrainNeighbors").Invoke(null,new object[]{terrains});
                Assert.That(terrains.All(t=>!t.allowAutoConnect),Is.True,"The actual mixed-size neighborhood must retain explicit native neighbor references across render frames.");
            }
            foreach(var terrain in terrains)
            {
                Assert.That(terrain.terrainData!=null&&terrain.terrainData.terrainLayers.Length>0&&terrain.materialTemplate!=null,Is.True);
                var data=terrain.terrainData;Assert.That(data.terrainLayers.All(l=>l!=null&&l.diffuseTexture!=null),Is.True,"No null diffuse terrain layers in a review capture.");
                var weights=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);float maximumError=0;int invalid=0;
                for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++){float sum=0;for(int i=0;i<data.alphamapLayers;i++){float w=weights[z,x,i];if(float.IsNaN(w)||float.IsInfinity(w)||w<0||w>1)invalid++;sum+=w;}maximumError=Mathf.Max(maximumError,Mathf.Abs(sum-1));}
                Assert.That(invalid,Is.Zero);Assert.That(maximumError,Is.LessThanOrEqualTo(2f/255+.00001f),"Every persisted splat texel must be normalized; a rendered image alone cannot establish this.");
            }
            foreach(var renderer in renderers)
            {
                if(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)Assert.That(renderer.sharedMaterials.Length,Is.GreaterThan(0),"Actual model has no material bindings: "+Hierarchy(renderer.transform));
                foreach(var material in renderer.sharedMaterials)Assert.That(material!=null&&material.shader!=null&&material.shader.isSupported&&material.shader.name!="Hidden/InternalErrorShader",Is.True,"Missing or unsupported actual material: "+Hierarchy(renderer.transform));
            }
            var materials=renderers.SelectMany(r=>r.sharedMaterials).Concat(terrains.Select(t=>t.materialTemplate)).Where(m=>m!=null).Distinct().Select(m=>new MaterialEvidence{name=m.name,path=AssetDatabase.GetAssetPath(m),shader=m.shader.name,supported=m.shader.isSupported}).ToArray();
            int roads=colliders.OfType<MeshCollider>().Count(c=>c.transform.parent!=null&&c.transform.parent.name.StartsWith("Kingdom ",StringComparison.Ordinal));
            Assert.That(roads,Is.GreaterThanOrEqualTo(17),"Actual baked EasyRoads collision must be present.");
            var regions=(Array)Field(world,"regions");Assert.That(regions.Length,Is.EqualTo(9));
            var cameraObject=new GameObject("Controlled native world review camera");camera=cameraObject.AddComponent<Camera>();
            camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=58;camera.aspect=(float)Width/Height;camera.cullingMask=~0;camera.clearFlags=CameraClearFlags.Skybox;camera.allowHDR=true;
            var regionEvidence=new List<RegionEvidence>();
            foreach(var region in regions)
            {
                string id=(string)Field(region,"id");var root=(Transform)Field(region,"root");
                var art=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(r is MeshRenderer||r is SkinnedMeshRenderer)&&r.bounds.size.sqrMagnitude>.01f).ToArray();
                Assert.That(art.Length,Is.GreaterThan(0),"Actual owned region art must be present: "+id);
                var bounds=(Bounds)Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAuthoring, Assembly-CSharp-Editor",true).GetMethod("MeasureOwnedModelBounds").Invoke(null,new object[]{root});
                var starts=(Transform[])Field(region,"truckStarts");Assert.That(starts.Length,Is.EqualTo(4));
                Vector3 origin=root.position;Vector3 target;
                switch(id)
                {
                    case "castle":target=origin+new Vector3(0,42,180);break;
                    case "harbor":target=origin+new Vector3(120,3,-205);break;
                    case "caverns":target=origin+new Vector3(0,15,0);break;
                    case "refuge":target=origin+new Vector3(0,5,65);break;
                    case "nests":target=origin+new Vector3(0,4,0);break;
                    case "mountains":case "volcano":target=new Vector3(bounds.center.x,Mathf.Max(origin.y+20,bounds.center.y),bounds.center.z);break;
                    default:target=Vector3.Lerp(origin+Vector3.up*10,bounds.center,.35f);break;
                }
                Vector3 eye=RoadEye(graph,origin,id)+Vector3.up*6;
                if(id=="caverns")
                {
                    // V154's unconstrained nearest-road query selected the
                    // castle-pass at(335,115,680), looking through the solid
                    // massif instead of its real western cave entrance. Use
                    // the actual connected forest-caverns road/floor corridor.
                    var entrance=((IEnumerable)Field(graph,"edges")).Cast<object>().Single(e=>(string)Field(e,"edgeId")=="forest-caverns");
                    eye=((IEnumerable)Field(entrance,"samples")).Cast<object>().Select(s=>(Vector3)Field(s,"position")).OrderBy(p=>(p-(origin+Vector3.left*80)).sqrMagnitude).First()+Vector3.up*6;
                }
                if(id=="harbor")eye=origin+new Vector3(105,8,-115);
                Assert.That(Physics.OverlapSphere(eye,.2f,~0,QueryTriggerInteraction.Ignore),Is.Empty,"Eye review camera must be outside actual physical geometry: "+id);
                var record=new RegionEvidence{id=id,path=Hierarchy(root),actualArtBounds=bounds,actualArtRenderers=art.Length,truckStarts=starts.Length,perches=((Transform[])Field(region,"perches")).Length,nests=((Transform[])Field(region,"nests")).Length};
                yield return SettleView(camera,eye,target);
                record.eye=Capture(camera,art,eye,target,Path.Combine(output,id+"-eye-"+Width+"x"+Height+".png"));
                float radius=Mathf.Max(35,bounds.extents.magnitude);
                Vector3 direction=new Vector3(.8f,.68f,-1).normalized;
                Vector3 overview=bounds.center+direction*radius*2.25f;
                camera.farClipPlane=Mathf.Max(6500,Vector3.Distance(overview,bounds.center)+radius*1.5f);
                yield return SettleView(camera,overview,bounds.center);
                record.overview=Capture(camera,art,overview,bounds.center,Path.Combine(output,id+"-overview-"+Width+"x"+Height+".png"));
                camera.farClipPlane=6500;
                regionEvidence.Add(record);
            }
            var extractions=new List<ExtractionEvidence>();
            foreach(var extraction in (Array)Field(world,"extractionSites"))
            {
                var zone=(Collider)Field(extraction,"secureZone");var approach=(Transform)Field(extraction,"approach");var vessel=(Component)Field(extraction,"vessel");
                var evidence=new ExtractionEvidence{id=(string)Field(extraction,"id"),path=Hierarchy(zone.transform),secureBounds=zone.bounds,approach=approach.position,movingVessel=vessel!=null};
                if(vessel!=null){evidence.solidDeckColliders=((Collider[])Field(vessel,"supportingDeck")).Count(c=>c!=null&&!c.isTrigger);evidence.externalDepartureWaypoints=((Transform[])Field(vessel,"departureWaypoints")).Count(t=>t!=null&&!t.IsChildOf(vessel.transform));Assert.That(evidence.solidDeckColliders,Is.GreaterThan(0));Assert.That(evidence.externalDepartureWaypoints,Is.GreaterThan(0));}
                extractions.Add(evidence);
            }
            Assert.That(extractions.Count,Is.EqualTo(3));
            var architectureFrames=new List<ArchitectureFrameEvidence>();
            if(preflightComposition==4)
            {
                // Existing regional cameras follow the original landmarks.
                // Measure and show each new complete source ensemble as well;
                // never hide terrain/old structures or force an artificial LOD
                // to make source art look visible when it is actually buried.
                var groups=JsonUtility.FromJson<CandidateReview>(File.ReadAllText(integrationPath)).finishedArchitecture.groups;
                Assert.That(groups.Length,Is.EqualTo(4));
                foreach(var group in groups)
                {
                    var region=regions.Cast<object>().Single(value=>(string)Field(value,"id")==group.region);var root=(Transform)Field(region,"root");
                    var frame=root.Cast<Transform>().Single(value=>value.name==group.name);
                    var art=frame.GetComponentsInChildren<Renderer>(true).Where(value=>value.enabled&&value.gameObject.activeInHierarchy&&(value is MeshRenderer||value is SkinnedMeshRenderer)&&value.bounds.size.sqrMagnitude>.01f).ToArray();
                    Assert.That(art.Length,Is.GreaterThan(0),"The real complete source ensemble must be active: "+group.key);
                    var bounds=art[0].bounds;foreach(var item in art.Skip(1))bounds.Encapsulate(item.bounds);
                    float radius=Mathf.Max(20,bounds.extents.magnitude);camera.farClipPlane=Mathf.Max(6500,radius*6);
                    Vector3 eye=bounds.center-frame.forward*(radius*2.5f)+Vector3.up*Mathf.Min(20,radius*.2f);
                    eye=AboveNativeTerrain(eye,scene);
                    var record=new ArchitectureFrameEvidence{key=group.key,region=group.region,path=Hierarchy(frame),actualBounds=bounds,declaredBounds=group.bounds,actualRenderers=art.Length};
                    yield return SettleView(camera,eye,bounds.center);
                    record.eye=Capture(camera,art,eye,bounds.center,Path.Combine(output,"architecture-"+group.key+"-eye-"+Width+"x"+Height+".png"));
                    Vector3 overview=bounds.center+new Vector3(1,.85f,-1).normalized*(radius*3);
                    overview=AboveNativeTerrain(overview,scene);
                    yield return SettleView(camera,overview,bounds.center);
                    record.overview=Capture(camera,art,overview,bounds.center,Path.Combine(output,"architecture-"+group.key+"-overview-"+Width+"x"+Height+".png"));
                    architectureFrames.Add(record);
                }
                camera.farClipPlane=6500;
            }
            var receipt=new Review{artistImports=importEvidence,assemblyImports=assemblyEvidence,nativeImports=nativeEvidence,architectureImports=architectureEvidence,preview=Preview,prefab=Prefab,definition=Definition,unityVersion=Application.unityVersion,width=Width,height=Height,sceneRoots=objects.Count(t=>t.parent==null),gameObjects=objects.Length,renderers=renderers.Length,meshFilters=objects.Sum(t=>t.GetComponents<MeshFilter>().Length),colliders=colliders.Length,rigidbodies=objects.Sum(t=>t.GetComponents<Rigidbody>().Length),terrains=terrains.Length,roadMeshColliders=roads,graphNodes=((ICollection)Field(graph,"nodes")).Count,graphEdges=((ICollection)Field(graph,"edges")).Count,regions=regionEvidence.ToArray(),extractions=extractions.ToArray(),materials=materials};
            receipt.architectureFrames=architectureFrames.ToArray();
            File.WriteAllText(Path.Combine(output,"world-render-review.json"),JsonUtility.ToJson(receipt,true));
            Debug.Log("DRAGON_KINGDOM_RENDER_REVIEW regions=9 images="+(18+architectureFrames.Count*2)+" nativeCameraRender="+Width+"x"+Height+" output="+output+" controlledNativeOnly=true");
        }
        finally
        {
            ShaderUtil.allowAsyncCompilation=priorAsyncCompilation;
            if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);
            EditorSceneManager.RestoreSceneManagerSetup(prior);
            foreach(var pair in hashes)Assert.That(Tuple.Create(new FileInfo(Io(pair.Key)).Length,FileSha(pair.Key)),Is.EqualTo(pair.Value),"Rendering must preserve original saved length and SHA256: "+pair.Key);
        }
    }
    static Vector3 RoadEye(object graph,Vector3 center,string id)
    {
        Vector3 desired=center+new Vector3(0,0,-90);Vector3 best=center;float distance=float.PositiveInfinity;
        foreach(var edge in (IEnumerable)Field(graph,"edges"))foreach(var sample in (IEnumerable)Field(edge,"samples"))
        {
            var point=(Vector3)Field(sample,"position");float fromCenter=Vector3.ProjectOnPlane(point-center,Vector3.up).magnitude;
            if(fromCenter<60||Mathf.Abs(point.y-center.y)>90)continue;
            float d=(point-desired).sqrMagnitude;
            if(d<distance){distance=d;best=point;}
        }
        Assert.That(distance,Is.LessThan(300*300),"Region needs actual nearby drivable roads: "+id);return best;
    }
    static Vector3 AboveNativeTerrain(Vector3 eye,Scene scene)
    {
        foreach(var hit in Physics.RaycastAll(eye+Vector3.up*3000,Vector3.down,6000,~0,QueryTriggerInteraction.Ignore))if(hit.collider is TerrainCollider&&hit.collider.gameObject.scene==scene)eye.y=Mathf.Max(eye.y,hit.point.y+8);
        return eye;
    }
    static string Io(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
    static string FileSha(string path){using(var stream=File.OpenRead(Io(path)))using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    static string WorldPath(string original,string candidate)
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonWorldCandidate");
        if(at<0||at+1>=args.Length)return "Assets/TruckDragonKingdom/"+original;
        string tag=args[at+1];Assert.That(tag.Length<=40&&tag.All(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_'),Is.True);
        return "Assets/TruckDragonKingdom/VisualCandidates/"+tag+"/"+candidate;
    }
    static CaptureEvidence Capture(Camera camera,Renderer[] art,Vector3 position,Vector3 target,string path)
    {
        camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
        var planes=GeometryUtility.CalculateFrustumPlanes(camera);int inView=art.Count(r=>GeometryUtility.TestPlanesAABB(planes,r.bounds));
        var texture=new Texture2D(Width,Height,TextureFormat.RGB24,false);var rt=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,Width,Height),0,0);texture.Apply();
            var pixels=texture.GetPixels32();var corner=pixels[0];int different=0;var tiles=new bool[64];
            for(int i=0;i<pixels.Length;i++){var p=pixels[i];if(Mathf.Abs(p.r-corner.r)+Mathf.Abs(p.g-corner.g)+Mathf.Abs(p.b-corner.b)<=30)continue;different++;int x=i%Width,y=i/Width;tiles[(y*8/Height)*8+x*8/Width]=true;}
            int occupied=tiles.Count(v=>v);File.WriteAllBytes(path,texture.EncodeToPNG());
            Assert.That(inView,Is.GreaterThan(0),"Review camera must frame actual region art, not a distant empty view: "+path);
            Assert.That(different,Is.GreaterThan(5000),"Actual native image contains no useful rendered geometry: "+path);Assert.That(occupied,Is.GreaterThanOrEqualTo(8),"Actual rendered content must occupy meaningful screen area: "+path);
            return new CaptureEvidence{png=path,cameraPosition=position,cameraEuler=camera.transform.eulerAngles,target=target,frustumRegionRenderers=inView,nonBackgroundPixels=different,occupiedScreenTiles=occupied,settledEditorFrames=settledFrames,settledRealSeconds=settledRealSeconds};
        }
        finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
    static IEnumerator SettleView(Camera camera,Vector3 position,Vector3 target)
    {
        // Camera changes start real vendor LOD crossfades. Capturing on that
        // same frame made the prior forest review transient/speckled. Advance
        // genuine Editor updates/renders without forcing any production LOD.
        camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
        var rt=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32);var previous=camera.targetTexture;
        double began=EditorApplication.timeSinceStartup,required=Math.Max(1.5,LODGroup.crossFadeAnimationDuration+.5);settledFrames=0;
        try
        {
            camera.targetTexture=rt;
            do{EditorApplication.QueuePlayerLoopUpdate();yield return null;camera.Render();settledFrames++;}
            while(settledFrames<12||EditorApplication.timeSinceStartup-began<required);
            settledRealSeconds=EditorApplication.timeSinceStartup-began;
        }
        finally{camera.targetTexture=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    static string Hierarchy(Transform t){string path=t.name;while(t.parent!=null){t=t.parent;path=t.name+"/"+path;}return path;}
}
#endif
