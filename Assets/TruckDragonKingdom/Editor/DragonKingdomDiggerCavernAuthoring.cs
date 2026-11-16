#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Digger.Modules.Core.Editor;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Sources.Operations;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckDragonKingdom.Editor
{
    // Project-owned clearance semantics over the installed Digger engine. The
    // cube meshes below are temporary hidden custom BRUSH inputs, never final
    // architecture/terrain/rendered placeholders. Digger owns final voxel mesh,
    // holes, materials, collision, LOD and persistence.
    public static partial class DragonKingdomDiggerCavernAuthoring
    {
        [Serializable]public sealed class RayEvidence{public string collider,mesh;public float height;public Vector3 normal;}
        [Serializable]public sealed class RoofSample{public Vector3 position;public float floorY,roofY,clearance;public bool floorPresent,roofPresent;public RayEvidence[] down,up;}
        [Serializable]public sealed class Evidence
        {
            public string scope="ACTUAL_VENDOR_DIGGER_CAVERN_REQUIRES_WORLD_PHYSICAL_RENDER_AND_DRIVING_ACCEPTANCE",dataFolder,status="IN_PROGRESS",error;
            public int systems,customBrushes,nativePrimitiveBrushes,digOperations,brushPasses=20,generatedMeshColliders,removedRejectedRoofRocks;
            public string[] tileDataFolders,streamingAssetsFolders;
            public int chunksBeforeCleanup,chunksAfterCleanup,savedMeshAssets,persistenceBatches;public double persistenceSeconds;
            public int floorRestorationOperations;public Bounds floorRestorationBounds;public string floorRestorationKind;
            public int preCutRoofColumns,roofFailures;public float minimumPreCutSurfaceY=float.PositiveInfinity;public bool queriesHitBackfaces;
            public float requiredPreCutRoofSurfaceY=170;
            public string currentPipeline,defaultPipeline,qualityPipeline,currentPipelineType,currentTerrainShader;
            public bool installedUrpDetectorSatisfied,pipelineSettingsPreserved;
            public bool exactHeightmaps,outsideHolesExact,streamingAssetsUntouched;public float minimumClearance=float.PositiveInfinity;public RoofSample[] roof;
            public RoundedProfileEvidence roundedProfile;
            public VaultSmoothingEvidence vaultSmoothing;
            public AuthoredUnionEvidence authoredUnion;
            public AuthoredFloorEvidence authoredFloor;
            public bool cubicGridRequested;public int configuredResolutionMultiplier;public bool configuredAutomaticVoxelHeight;public Vector3[] actualVoxelScales;public RoundedVoxelProbe cubicInputProbe;
        }
        public static Evidence LastEvidence{get;private set;}
        static Task activeTask;
        public static bool IsBusy=>activeTask!=null&&!activeTask.IsCompleted;
        sealed class Segment{public Vector3 a,b;public float width;}
        public static IEnumerator ExcavateCandidate(DragonKingdomWorld world,string tag)
        {
            if(IsBusy)throw new InvalidOperationException("Preserve the pending Digger candidate until its actual vendor jobs complete.");
            var task=activeTask=Build(world,tag);while(!task.IsCompleted)yield return null;
            if(task.IsFaulted)throw task.Exception.InnerException;if(task.IsCanceled)throw new OperationCanceledException("Explicit Digger candidate cancelled.");
        }
        static async Task Build(DragonKingdomWorld world,string tag)
        {
            var evidence=new Evidence{dataFolder="Assets/DiggerData/Scenes/DK-"+tag};LastEvidence=evidence;
            var priorDefault=GraphicsSettings.defaultRenderPipeline;var priorQuality=QualitySettings.renderPipeline;int priorLevel=QualitySettings.GetQualityLevel();
            var systems=new List<DiggerSystem>();var brushes=new List<GameObject>();var roundedMeshes=new List<Mesh>();
            try
            {
            if(world==null||world.definition==null||Directory.Exists(evidence.dataFolder))throw new InvalidOperationException("Actual candidate world and fresh isolated Digger persistence folder required.");
            var terrains=world.GetComponentsInChildren<Terrain>(true);
            if(terrains.Length!=4||terrains.Any(t=>!AssetDatabase.GetAssetPath(t.terrainData).StartsWith("Assets/TruckDragonKingdom/VisualCandidates/"+tag+"/Terrain/",StringComparison.Ordinal)))throw new InvalidOperationException("Digger may only edit the four fresh owned candidate TerrainData, never accepted/foundation assets.");
            if(UnityEngine.Object.FindAnyObjectByType<DiggerMaster>()!=null)throw new InvalidOperationException("Isolate candidate excavation from every existing Digger master.");
            foreach(string shaderName in new[]{"Digger/Terrain/URP/Lit","Digger/Mesh/URP/Lit-Pass0"})
            {
                var shader=Shader.Find(shaderName);if(shader==null||!shader.isSupported||ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new InvalidOperationException("Installed supported Digger URP shaders required; automatic vendor package overwrite is forbidden: "+shaderName);
            }
            var current=GraphicsSettings.currentRenderPipeline;
            evidence.currentPipeline=AssetDatabase.GetAssetPath(current);evidence.currentPipelineType=current==null?"null":current.GetType().FullName;
            evidence.defaultPipeline=AssetDatabase.GetAssetPath(priorDefault);evidence.qualityPipeline=AssetDatabase.GetAssetPath(priorQuality);
            var currentTerrain=current==null?null:current.defaultTerrainMaterial;
            evidence.currentTerrainShader=currentTerrain==null||currentTerrain.shader==null?"null":currentTerrain.shader.name;
            // Match the complete installed Digger IsBuiltInURP predicate.
            // V146's narrower name-only precondition wrongly rejected the
            // actual PC_RPAsset/IH_High_RPAsset Universal pipeline names.
            // The active quality pipeline's real Terrain/Lit shader is the
            // vendor's primary public detection branch; no override is needed.
            evidence.installedUrpDetectorSatisfied=evidence.currentTerrainShader=="Universal Render Pipeline/Terrain/Lit"||(priorDefault!=null&&priorDefault.name.Contains("URP"));
            if(!evidence.installedUrpDetectorSatisfied)throw new InvalidOperationException("Actual installed Digger URP detector rejected current="+evidence.currentPipeline+" type="+evidence.currentPipelineType+" terrainShader="+evidence.currentTerrainShader+" default="+evidence.defaultPipeline+" quality="+evidence.qualityPipeline+". Preserve actual pipeline configuration; no fallback shader/import.");
            var heightHashes=terrains.ToDictionary(t=>t,t=>DragonKingdomWorldVisualMigration.HeightHash(t.terrainData));
            var holes=terrains.ToDictionary(t=>t,t=>t.terrainData.GetHoles(0,0,t.terrainData.holesResolution,t.terrainData.holesResolution));
            var volume=DragonKingdomWorldAuthoring.CavernUndergroundBounds();
            bool rounded=RoundedProfileRequested();
            bool authoredUnion=Environment.GetCommandLineArgs().Contains("-tsDragonCavernAuthoredBooleanUnion");
            if(authoredUnion&&(!rounded||!RoundedArchedCorridorsRequested()||!NativeAnalyticLobesRequested()||Environment.GetCommandLineArgs().Any(arg=>arg=="-tsDragonCavernCubicVoxelGrid"||arg=="-tsDragonCavernDigPasses"||arg=="-tsDragonCavernSmoothUpperVault"||arg=="-tsDragonCavernPaintSmoothedRock")))
                throw new InvalidOperationException("Authored union requires a fresh original-grid analytic RoundedLobes02 candidate without additive-pressure or smoothing trial flags.");
            if(authoredUnion)evidence.brushPasses=1;
            string explicitDigPasses=ProfileArgument("-tsDragonCavernDigPasses");
            if(explicitDigPasses!=null)
            {
                if(!NativeAnalyticLobesRequested()||(explicitDigPasses!="4"&&explicitDigPasses!="2"))throw new InvalidOperationException("Only the explicit original-grid native analytic two/four-pass pressure comparison is qualified for this diagnostic.");
                evidence.brushPasses=int.Parse(explicitDigPasses,System.Globalization.CultureInfo.InvariantCulture);
            }
                bool smoothVault=Environment.GetCommandLineArgs().Contains("-tsDragonCavernSmoothUpperVault");
                if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernPaintSmoothedRock")&&!smoothVault)throw new InvalidOperationException("Rock repair is limited to the explicit measured native smoothing spheres.");
                if(smoothVault&&!RoundedArchedCorridorsRequested())throw new InvalidOperationException("Bounded native smoothing requires an explicit fresh RoundedLobes02 candidate.");
            bool measuredRasterClearance=MeasuredRasterClearanceRequested();
            if(measuredRasterClearance&&(!RoundedArchedCorridorsRequested()||!authoredUnion||Environment.GetCommandLineArgs().Contains("-tsDragonCavernCubicVoxelGrid")))throw new InvalidOperationException("Explicit measured raster clearance requires a fresh original-grid RoundedLobes02 authored Boolean candidate.");
            bool archedCorridors=RoundedArchedCorridorsRequested();evidence.requiredPreCutRoofSurfaceY=archedCorridors?178.05f:170;
            if(NativeAnalyticLobesRequested()&&(!archedCorridors||Environment.GetCommandLineArgs().Contains("-tsDragonCavernCubicVoxelGrid")))throw new InvalidOperationException("Analytic lobe comparison requires a fresh RoundedLobes02 candidate on the qualified original grid, never a combined grid experiment.");
            var roundedColumns=rounded?PrepareRoundedProfile(evidence):null;
            var preCutColumns=rounded?RoundedRoofMassColumns():LegacyRoofMassColumns();
            foreach(var column in preCutColumns)
            {
                Vector3 p=column;var terrain=terrains.First(t=>p.x>=t.transform.position.x&&p.x<=t.transform.position.x+1600&&p.z>=t.transform.position.z&&p.z<=t.transform.position.z+1600);
                Vector3 local=p-terrain.transform.position;float y=terrain.transform.position.y+terrain.terrainData.GetInterpolatedHeight(local.x/1600,local.z/1600);
                evidence.preCutRoofColumns++;evidence.minimumPreCutSurfaceY=Mathf.Min(evidence.minimumPreCutSurfaceY,y);
            }
            if(evidence.minimumPreCutSurfaceY<evidence.requiredPreCutRoofSurfaceY)throw new InvalidOperationException("Actual pre-cut Vista overhead massif falls below"+evidence.requiredPreCutRoofSurfaceY+"m; retain10m roof mass over the declared chamber/vault top. Minimum="+evidence.minimumPreCutSurfaceY+". Restore the measured vendor foundation before excavation.");
            var masterObject=new GameObject("Actual vendor Digger cavern persistence");masterObject.transform.SetParent(world.transform,false);
            var master=masterObject.AddComponent<DiggerMaster>();master.SceneDataFolder="DK-"+tag;evidence.cubicGridRequested=Environment.GetCommandLineArgs().Contains("-tsDragonCavernCubicVoxelGrid");
            if(evidence.cubicGridRequested&&!archedCorridors)throw new InvalidOperationException("Cubic-grid diagnosis requires an explicit fresh RoundedLobes02 candidate.");
            master.ResolutionMult=evidence.cubicGridRequested?4:2;master.VoxelHeight=.5f;master.AutoVoxelHeight=evidence.cubicGridRequested;
            evidence.configuredResolutionMultiplier=master.ResolutionMult;evidence.configuredAutomaticVoxelHeight=master.AutoVoxelHeight;master.ColliderLodIndex=0;master.CreateLODs=true;master.AutoSaveMeshesAsAssets=false;master.CreateDirs();
                foreach(var terrain in terrains)
                {
                    var p=terrain.transform.position;
                    if(p.x>volume.max.x||p.x+1600<volume.min.x||p.z>volume.max.z||p.z+1600<volume.min.z)continue;
                    var go=new GameObject("Actual vendor cavern Digger");go.transform.SetParent(terrain.transform,false);var system=go.AddComponent<DiggerSystem>();
                    // false avoids forced shader import. The guard above makes
                    // the vendor's ordinary missing-shader import path unused.
                    DiggerSystemEditor.Init(system,false);system.PrepareModification();systems.Add(system);
                }
                evidence.actualVoxelScales=systems.Select(s=>(Vector3)s.HeightmapScale).ToArray();
                if(evidence.cubicGridRequested)
                {
                    if(evidence.actualVoxelScales.Any(s=>s!=new Vector3(.78125f,.78125f,.78125f)))throw new InvalidOperationException("Actual native automatic cubic voxel grid differs from the declared supported4x configuration.");
                    evidence.cubicInputProbe=ProbeInstalledRoundedCubicInputVoxelizer();var input=evidence.cubicInputProbe;
                    if(!input.finite||input.center<=0||input.aboveApex>=0||input.belowFloor>=0||input.outsideWall>=0)throw new InvalidOperationException("Installed actual rounded custom-input voxelizer failed finite/interior/exterior signs at the proposed cubic grid before excavation.");
                }
                evidence.systems=systems.Count;if(systems.Count!=2)throw new InvalidOperationException("The actual cavern crosses the z800 seam and requires both adjacent owned Digger tiles.");
                evidence.tileDataFolders=systems.Select(s=>s.BasePathData.Replace('\\','/')).ToArray();
                evidence.streamingAssetsFolders=systems.Select(s=>FileUtil.GetProjectRelativePath(s.StreamingAssetsPathData).Replace('\\','/')).ToArray();
                if(systems.Any(s=>Directory.Exists(s.StreamingAssetsPathData)))throw new InvalidOperationException("Fresh candidate must not reuse any existing Digger StreamingAssets GUID folder.");
                float roadSideGuard=measuredRasterClearance?MeasuredHorizontalVoxelGuardBand(evidence.actualVoxelScales):0;
                if(evidence.roundedProfile!=null){evidence.roundedProfile.rasterClearancePolicy=measuredRasterClearance?RasterClearancePolicy:"OriginalNominalWidth";evidence.roundedProfile.measuredRoadSideGuardBand=roadSideGuard;}
                var operations=new List<BasicOperation>();
                if(rounded)operations.AddRange(RoundedVolumes(brushes,roundedMeshes,evidence));
                else operations.Add(CustomVolume(systems[0],brushes,new Vector3(520,132.5f,780),Quaternion.identity,new Vector3(188.2f,55.1f,136.2f),"Measured dragon hoard chamber"));
                foreach(var segment in RoadSegments(world,volume))
                {
                    if(archedCorridors){operations.Add(RoundedRoadVolume(brushes,roundedMeshes,segment,roadSideGuard));continue;}
                    var forward=(segment.b-segment.a).normalized;var rotation=Quaternion.LookRotation(forward,Vector3.up);
                    Vector3 center=(segment.a+segment.b)*.5f+rotation*Vector3.up*27.5f;
                    operations.Add(CustomVolume(systems[0],brushes,center,rotation,new Vector3(segment.width+14,55.1f,Vector3.Distance(segment.a,segment.b)+24),"Actual connected road clearance"));
                }
                evidence.customBrushes=operations.Count(o=>o.Params.Brush==BrushType.Custom);evidence.nativePrimitiveBrushes=operations.Count-evidence.customBrushes;evidence.digOperations=operations.Count;
                // Digger's Dig is incremental, not a Boolean subtraction. With
                // its original-terrain-distance weighting,20 actual passes put
                // the .1voxel isosurface near the measured clearance boundary.
                // Native floor/body/roof tests below remain the authority.
                if(authoredUnion)evidence.authoredUnion=await ApplyAuthoredCavernUnion(systems,operations);
                else for(int pass=0;pass<evidence.brushPasses;pass++)
                {
                    foreach(var operation in operations)foreach(var system in systems)await system.ModifyWithoutMeshes(operation);
                    if(pass%4==0)Debug.Log("DRAGON_DIGGER_CAVERN actualPass="+(pass+1)+" brushes="+operations.Count+" systems="+systems.Count);
                }
                // Genuine vendor ellipsoid alcoves give a vaulted ceiling while
                // leaving the flat, measured105m truck floor intact.
                if(!rounded)foreach(float x in new[]{485f,555f})
                {
                    var dome=new BasicOperation{Params=new ModificationParameters{Action=ActionType.Dig,Brush=BrushType.Sphere,Position=new float3(x,148,780),Size=new float3(50,26,50),Opacity=1,TextureIndex=3}};
                    for(int pass=0;pass<20;pass++)foreach(var system in systems)await system.ModifyWithoutMeshes(dome);
                    evidence.nativePrimitiveBrushes++;evidence.digOperations++;
                }
                // V150's12 failures were real overlapping-Dig isosurfaces at
                //104.648..104.668m, not missing faces or the z800 tile seam.
                // Restore the original solid BELOW the authored105m flat
                // chamber floor with the installed Reset operation. Unlike
                // another additive Dig/Add pass, Reset is not cumulative.
                // Keep the west road's genuine rising approach outside this
                // flat floor footprint; its flat end begins at x440.
                float sx=systems[0].HeightmapScale.x,sz=systems[0].HeightmapScale.z;
                float minX=Mathf.Floor(440/sx)*sx,maxX=Mathf.Floor((rounded?616:614.1f)/sx)*sx;
                float minZ=Mathf.Ceil((rounded?724:711.9f)/sz)*sz,maxZ=Mathf.Floor((rounded?856:848.1f)/sz)*sz;
                var floorCenter=new Vector3((minX+maxX)*.5f,94.5f,(minZ+maxZ)*.5f);
                var floorSize=new Vector3(maxX-minX,21,maxZ-minZ);
                evidence.floorRestorationBounds=new Bounds(floorCenter,floorSize);
                if(authoredUnion)
                {
                    evidence.authoredFloor=await ApplyAuthoredCavernFloor(systems,evidence.floorRestorationBounds);
                    evidence.floorRestorationKind="AUTHORED_SIGNED_PLANE_INTERSECTION";
                }
                else
                {
                    var restoreFloor=CustomVolume(systems[0],brushes,floorCenter,Quaternion.identity,floorSize,"Restore actual original solid below105m chamber floor");
                    restoreFloor.Params.Action=ActionType.Reset;
                    foreach(var system in systems)await system.ModifyWithoutMeshes(restoreFloor);
                    evidence.customBrushes++;evidence.floorRestorationKind="NATIVE_RESET_ORIGINAL_SOLID";
                }
                evidence.floorRestorationOperations=1;
                foreach(var system in systems)await system.BuildPendingMeshesAsync(false);
                if(smoothVault)evidence.vaultSmoothing=await SmoothMeasuredUpperVault(systems,evidence);
                evidence.chunksBeforeCleanup=systems.Sum(s=>s.GetComponentsInChildren<Chunk>(true).Length);
                // V148 completed every brush pass promptly, then paid one
                // WeatherMaker asset-postprocessor refresh per saved mesh.
                // Batch only synchronous final vendor persistence; never hold
                // asset editing across an await or any active voxel/mesh job.
                double persistenceStart=EditorApplication.timeSinceStartup;
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach(var system in systems)
                    {
                        system.PersistDiggerVersion();
                        // Installed public cleanup retains every visual chunk
                        // or altered voxel and drops only unused unaltered ones.
                        system.PersistAndRecordUndo(true,true);system.SaveMeshesAsAssets();
                    }
                    evidence.persistenceBatches++;
                }
                finally{AssetDatabase.StopAssetEditing();}
                evidence.persistenceSeconds=EditorApplication.timeSinceStartup-persistenceStart;
                evidence.chunksAfterCleanup=systems.Sum(s=>s.GetComponentsInChildren<Chunk>(true).Length);
                evidence.savedMeshAssets=systems.Sum(s=>Directory.GetFiles(s.MeshesPathData,"*.asset",SearchOption.TopDirectoryOnly).Length);
                foreach(var brush in brushes)if(brush!=null)UnityEngine.Object.DestroyImmediate(brush);brushes.Clear();
                var rejected=world.Region("caverns").root.Cast<Transform>().Where(t=>t.name.StartsWith("RockFormation_01",StringComparison.Ordinal)&&Mathf.Abs(Mathf.DeltaAngle(t.localEulerAngles.x,90))<1).ToArray();
                if(rejected.Length!=4)throw new InvalidOperationException("Expected only the four audited rejected sideways roof clusters.");
                foreach(var t in rejected)UnityEngine.Object.DestroyImmediate(t.gameObject);evidence.removedRejectedRoofRocks=rejected.Length;
                Physics.SyncTransforms();
                evidence.queriesHitBackfaces=Physics.queriesHitBackfaces;
                var colliders=new HashSet<Collider>(systems.SelectMany(s=>s.GetComponentsInChildren<MeshCollider>(true)).Where(c=>c.enabled&&!c.isTrigger&&c.sharedMesh!=null));evidence.generatedMeshColliders=colliders.Count;
                if(colliders.Count==0)throw new InvalidOperationException("Digger produced no real saved mesh collision.");
                var roof=new List<RoofSample>();
                var columns=rounded?roundedColumns:LegacyRoofMassColumns().Select(p=>new RoundedColumn{position=p,requiredTop=147,floor=105}).ToArray();
                foreach(var column in columns)
                {
                    var p=new Vector3(column.position.x,column.floor+1.5f,column.position.z);
                    var down=Physics.RaycastAll(p,Vector3.down,5,~0,QueryTriggerInteraction.Ignore).Where(h=>colliders.Contains(h.collider)).OrderBy(h=>h.distance).ToArray();
                    var up=Physics.RaycastAll(p,Vector3.up,110,~0,QueryTriggerInteraction.Ignore).Where(h=>colliders.Contains(h.collider)).OrderBy(h=>h.distance).ToArray();
                    bool floorPresent=down.Length>0,roofPresent=up.Length>0;float floorY=floorPresent?down[0].point.y:0,roofY=roofPresent?up[0].point.y:0,clearance=floorPresent&&roofPresent?roofY-floorY:0;
                    roof.Add(new RoofSample{position=p,floorPresent=floorPresent,roofPresent=roofPresent,floorY=floorY,roofY=roofY,clearance=clearance,down=down.Take(8).Select(Ray).ToArray(),up=up.Take(8).Select(Ray).ToArray()});
                    if(floorPresent&&roofPresent)evidence.minimumClearance=Mathf.Min(evidence.minimumClearance,clearance);
                    if(!floorPresent||!roofPresent||Mathf.Abs(floorY-column.floor)>.3f||(rounded?roofY<column.requiredTop:clearance<42))evidence.roofFailures++;
                }
                evidence.roof=roof.ToArray();evidence.exactHeightmaps=terrains.All(t=>DragonKingdomWorldVisualMigration.HeightHash(t.terrainData)==heightHashes[t]);
                evidence.outsideHolesExact=true;var permitted=volume;permitted.Expand(80);
                foreach(var terrain in terrains)
                {
                    int n=terrain.terrainData.holesResolution;var after=terrain.terrainData.GetHoles(0,0,n,n);var before=holes[terrain];
                    for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                    {
                        var p=terrain.transform.position+new Vector3((x+.5f)*1600/n,0,(z+.5f)*1600/n);
                        if((p.x<permitted.min.x||p.x>permitted.max.x||p.z<permitted.min.z||p.z>permitted.max.z)&&after[z,x]!=before[z,x])evidence.outsideHolesExact=false;
                    }
                    EditorUtility.SetDirty(terrain.terrainData);AssetDatabase.SaveAssetIfDirty(terrain.terrainData);
                }
                evidence.streamingAssetsUntouched=systems.All(s=>!Directory.Exists(s.StreamingAssetsPathData));
                if(!evidence.exactHeightmaps||!evidence.outsideHolesExact||!evidence.streamingAssetsUntouched)throw new InvalidOperationException("Digger changed an actual heightmap, unrelated terrain holes or unexpected StreamingAssets output.");
                if(evidence.roofFailures!=0)throw new InvalidOperationException("Measured Digger chamber failed "+evidence.roofFailures+" of"+columns.Length+" actual floor/roof contacts. Complete collider paths, mesh assets and ray normals are retained in the cavern receipt; no failed chamber is accepted.");
                if(rounded){evidence.roundedProfile.nativeRequiredColumnsPassed=true;evidence.roundedProfile.requiredColumns=columns.Length;}
                evidence.status="MEASURED_CAVERN_REQUIRES_FULL_WORLD_GATES_AND_RENDER_REVIEW";
            }
            catch(Exception e){evidence.status="REJECTED_DIGGER_CAVERN";evidence.error=e.ToString();throw;}
            finally
            {
                foreach(var brush in brushes)if(brush!=null)UnityEngine.Object.DestroyImmediate(brush);
                foreach(var mesh in roundedMeshes)if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);
                evidence.pipelineSettingsPreserved=GraphicsSettings.defaultRenderPipeline==priorDefault&&QualitySettings.renderPipeline==priorQuality&&QualitySettings.GetQualityLevel()==priorLevel;
                if(!evidence.pipelineSettingsPreserved){evidence.status="REJECTED_DIGGER_CAVERN";evidence.error+=" Actual Graphics/Quality pipeline settings changed during scoped excavation.";}
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");string output=at>=0&&at+1<args.Length?args[at+1]:"Logs/TruckSports/DragonKingdom";Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"actual-digger-cavern-"+tag+".json"),JsonUtility.ToJson(evidence,true));
                if(!evidence.pipelineSettingsPreserved)throw new InvalidOperationException("Scoped Digger excavation changed actual Graphics/Quality pipeline settings.");
            }
        }
        static RayEvidence Ray(RaycastHit hit)
        {
            string path=hit.collider.name;for(var t=hit.collider.transform.parent;t!=null;t=t.parent)path=t.name+"/"+path;
            return new RayEvidence{collider=path,mesh=hit.collider is MeshCollider mesh?AssetDatabase.GetAssetPath(mesh.sharedMesh):"",height=hit.point.y,normal=hit.normal};
        }
        static BasicOperation CustomVolume(DiggerSystem measuringSystem,List<GameObject> temporary,Vector3 center,Quaternion rotation,Vector3 size,string label)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);temporary.Add(go);go.name="Temporary Digger custom clearance brush - "+label;go.hideFlags=HideFlags.HideAndDontSave;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().enabled=false;go.transform.SetPositionAndRotation(center,rotation);go.transform.localScale=size;
            var brush=go.AddComponent<CustomBrush>();brush.enabled=false;brush.autoRefresh=false;brush.ComputeVoxels();
            if(brush.InputVoxels==null||brush.InputVoxels.Length==0)throw new InvalidOperationException("Vendor custom-brush voxelization produced no actual input.");
            return new BasicOperation{Params=new ModificationParameters{Action=ActionType.Dig,Brush=BrushType.Custom,Position=center,Size=new float3(1),Opacity=1,TextureIndex=3,CustomBrush=brush}};
        }
        static IEnumerable<Segment> RoadSegments(DragonKingdomWorld world,Bounds bounds)
        {
            var segments=new List<Segment>();var unique=new HashSet<string>();
            foreach(var edge in world.definition.graph.edges)
            {
                var merged=new List<Segment>();for(int i=1;i<edge.samples.Count;i++)
                {var a=edge.samples[i-1].position;var b=edge.samples[i].position;var last=merged.LastOrDefault();if(last!=null&&Vector3.Dot((last.b-last.a).normalized,(b-a).normalized)>.999999f)last.b=b;else merged.Add(new Segment{a=a,b=b,width=edge.samples[i].roadWidthMeters});}
                foreach(var segment in merged)
                {
                    if(!Clip(bounds,segment.a,segment.b,out var a,out var b)||Vector3.Distance(a,b)<1)continue;
                    string ka=a.ToString("F2"),kb=b.ToString("F2"),key=string.CompareOrdinal(ka,kb)<0?ka+kb:kb+ka;
                    if(unique.Add(key))segments.Add(new Segment{a=a,b=b,width=segment.width});
                }
            }
            return segments;
        }
        public static bool Clip(Bounds bounds,Vector3 a,Vector3 b,out Vector3 start,out Vector3 end)
        {
            float lo=0,hi=1;Vector3 d=b-a;
            bool Axis(float p,float q){if(Mathf.Abs(p)<.000001f)return q>=0;float r=q/p;if(p<0){if(r>hi)return false;lo=Mathf.Max(lo,r);}else{if(r<lo)return false;hi=Mathf.Min(hi,r);}return true;}
            bool result=Axis(-d.x,a.x-bounds.min.x)&&Axis(d.x,bounds.max.x-a.x)&&Axis(-d.z,a.z-bounds.min.z)&&Axis(d.z,bounds.max.z-a.z);
            start=Vector3.Lerp(a,b,lo);end=Vector3.Lerp(a,b,hi);return result&&lo<=hi;
        }
    }
}
#endif
