#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyRoads3Dv3;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    // Uses the installed public post-level terrain tools. There is no LWS
    // smoothing kernel or reconstructed heightfield in this adapter.
    public static partial class DragonKingdomVendorFalloffAuthoring
    {
        public const string Flag="-tsDragonVendorSmoothNE";
        public const string ProfileFlag="-tsDragonVendorFalloffProfile",PairProfile="MeasuredNePair",ApronProfile="MeasuredNeApronOnly",FragmentProfile="MeasuredNeSourceFragment";
        [Serializable]public sealed class TileChange
        {public string terrain,data,beforeHeightSha256,afterHeightSha256,changedMaskSha256,failedBeforeFloat32,failedAfterFloat32;public int resolution,changedSamples,outsideDeclaredFootprintChanges,outsideContinuousToolFootprintChanges,protectedRoofChangedSamples;public Vector3 terrainOrigin,terrainSize;public float maximumDelta,maximumOutsideDelta,maximumOutsideDistance,maximumContinuousOutsideDistance;public bool outsideDeclaredFootprintExact;public Bounds changedBounds,outsideChangedBounds;public ChangedPoint[] outsidePoints;}
        [Serializable]public sealed class ChangedPoint{public Vector3 position;public float beforeY,afterY,delta,outsideDistance;}
        [Serializable]public sealed class NeHeight{public string terrain,pointKind="ACTUAL_TERRAIN_HEIGHTMAP_VERTEX";public Vector3 point;public int column,row,resolution;public float beforeY,afterY,delta;}
        [Serializable]public sealed class RoofComparison
        {public string terrain,beforePreSdkRoofSha256,afterSdkRoofSha256;public int samples,changedSamples;public float maximumDelta;public bool exact;}
        [Serializable]public sealed class NativeRoadDimensions
        {public string name,typeName;public int actualComponentId;public float roadWidth,typeWidth,publicWrapperWidth,laneWidth,leftShoulderWidth,rightShoulderWidth;public bool publicWrapperResolved;}
        [Serializable]public sealed class Operation
        {public string road,tool;public int actualRoadComponentId,indentStepBefore,indentStepAfter,surroundingStepBefore,surroundingStepAfter;public string terrain,maskAuthority="OUTWARD_ACTUAL_TERRAIN_GRID_CELL_COVERAGE_OF_PUBLIC_CONTOURS_PLUS_DECLARED_METER_TOOL_DISTANCE";public Bounds actualGeneratedContourBounds,declaredToolFootprint,declaredRasterToolFootprint;public float distance;public double seconds;public TileChange[] fields;public NeHeight[] neHeights;public NativeSdkStencil installedSdkStencil;}
        [Serializable]public sealed class Evidence
        {
            public string scope="EXPLICIT_MEASURED_NE_ROAD_PROFILE_INSTALLED_VENDOR_SMOOTHEN_FALLOFF_CAUSAL_TRIAL_NOT_WHOLE_WORLD_ACCEPTANCE",status="IN_PROGRESS",error,profile;
            public float authoredSurroundingDistance,previousIndentDistance,previousSurroundingDistance;public int previousIndentStep,previousSurroundingStep;
            public int actualReturnedRoads,selectedRoads,operations,changedSamples;public bool exactOwnedNetworkAndFourTiles,settingsRestored,sourceBytesExact;
            public List<Operation> results=new List<Operation>();public string[] watchedSources;
            public bool originalPreSdkRoofDeclared,originalPreSdkRoofExact;public Bounds originalPreSdkRoof;public RoofComparison[] roof;
            public string neReferenceKind="RECORDED_QUALITY_CELL_MIDPOINT_NOT_A_HEIGHTMAP_VERTEX";public Vector3 neFailureCellReference=NeFailureCellReference;public int neVertexStations;
            public NativeRoadDimensions[] actualRoadDimensions;
            public FragmentEvidence fragment;
        }
        public static Evidence LastEvidence{get;private set;}
        public static readonly Vector3 NeFailureCellReference=new Vector3(1254.6875f,0,1167.1875f);
        public static Vector3[] ActualNeVertexStations(Vector3 origin,Vector3 size,int resolution)
        {
            if(resolution<4||!float.IsFinite(size.x)||!float.IsFinite(size.z)||size.x<=0||size.z<=0)throw new ArgumentException("Actual TerrainData dimensions and resolution required for vertex witnesses.");
            float sx=size.x/(resolution-1),sz=size.z/(resolution-1);int x0=Mathf.FloorToInt((NeFailureCellReference.x-origin.x)/sx),z0=Mathf.FloorToInt((NeFailureCellReference.z-origin.z)/sz);var result=new List<Vector3>();
            for(int dz=-1;dz<=2;dz++)for(int dx=-1;dx<=2;dx++)
            {
                int x=x0+dx,z=z0+dz;if(x<0||x>=resolution||z<0||z>=resolution)throw new InvalidOperationException("The recorded NE cell needs a complete actual four-by-four vertex neighborhood.");
                var point=new Vector3(origin.x+x*sx,0,origin.z+z*sz);float fx=(point.x-origin.x)/size.x*(resolution-1),fz=(point.z-origin.z)/size.z*(resolution-1);
                if(Mathf.Abs(fx-x)>.0001f||Mathf.Abs(fz-z)>.0001f)throw new InvalidOperationException("Actual generated NE station failed strict vertex/index correspondence.");result.Add(point);
            }
            return result.ToArray();
        }
        public static string RequestedProfile()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,ProfileFlag);string profile=at<0?PairProfile:at+1<args.Length?args[at+1]:null;
            if(profile!=PairProfile&&profile!=ApronProfile&&profile!=FragmentProfile)throw new InvalidOperationException("Explicit known vendor falloff profile required; retain rejected pair/apron profiles separately from source-fragment scope.");return profile;
        }
        static string Sha(byte[] bytes){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static string HeightSha(float[,] heights){var bytes=new byte[heights.Length*4];Buffer.BlockCopy(heights,0,bytes,0,bytes.Length);return Sha(bytes);}
        static string FileSha(string path){using(var stream=File.OpenRead(path))using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
        static bool IntersectsXZ(Bounds a,Bounds b)=>a.min.x<=b.max.x&&a.max.x>=b.min.x&&a.min.z<=b.max.z&&a.max.z>=b.min.z;
        static bool InsideXZ(Bounds b,Vector3 p)=>p.x>=b.min.x&&p.x<=b.max.x&&p.z>=b.min.z&&p.z<=b.max.z;
        static float OutsideXZ(Bounds b,Vector3 p){float x=Mathf.Max(0,Mathf.Max(b.min.x-p.x,p.x-b.max.x)),z=Mathf.Max(0,Mathf.Max(b.min.z-p.z,p.z-b.max.z));return Mathf.Sqrt(x*x+z*z);}
        public static Bounds CoverTerrainGridCells(Bounds continuous,Vector3 origin,Vector3 spacing)
        {
            if(!float.IsFinite(spacing.x)||!float.IsFinite(spacing.z)||spacing.x<=0||spacing.z<=0||!float.IsFinite(origin.x)||!float.IsFinite(origin.z)||!float.IsFinite(continuous.min.x)||!float.IsFinite(continuous.min.z)||!float.IsFinite(continuous.max.x)||!float.IsFinite(continuous.max.z)||continuous.size.x<=0||continuous.size.z<=0)throw new ArgumentException("Actual finite footprint and TerrainData XZ grid required.");
            float x0=origin.x+Mathf.Floor((continuous.min.x-origin.x)/spacing.x)*spacing.x,x1=origin.x+Mathf.Ceil((continuous.max.x-origin.x)/spacing.x)*spacing.x;
            float z0=origin.z+Mathf.Floor((continuous.min.z-origin.z)/spacing.z)*spacing.z,z1=origin.z+Mathf.Ceil((continuous.max.z-origin.z)/spacing.z)*spacing.z;
            return new Bounds(new Vector3((x0+x1)*.5f,continuous.center.y,(z0+z1)*.5f),new Vector3(x1-x0,continuous.size.y,z1-z0));
        }
        static void PreserveFailedField(string path,float[,] field){if(File.Exists(path))throw new InvalidOperationException("Preserve prior failed SDK field evidence.");var bytes=new byte[field.Length*4];Buffer.BlockCopy(field,0,bytes,0,bytes.Length);File.WriteAllBytes(path,bytes);}
        static Bounds Contours(ERModularRoad road)
        {
            var arrays=new[]{road.splinePoints,road.leftIndentVecs,road.rightIndentVecs,road.leftSurroundingVecs,road.rightSurroundingVecs};bool first=true;var bounds=new Bounds();
            foreach(var array in arrays)if(array!=null)foreach(var p in array)
            {if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z))throw new InvalidOperationException("Actual finite vendor-generated contour coordinates required.");if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
            if(first||bounds.size.x<=0||bounds.size.z<=0)throw new InvalidOperationException("Real generated vendor road/indent/surrounding contours required, not renderer bounds.");return bounds;
        }
        public static void ApplyMeasuredNePair(ERRoadNetwork network,ERModularBase owner,Terrain[] terrains,IReadOnlyList<ERRoad> actualReturnedRoads,float authoredSurrounding,Bounds? originalProtectedUndergroundRoof)
        {
            if(network==null||owner==null||EditorUtility.IsPersistent(owner)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||terrains==null||terrains.Length!=4||actualReturnedRoads==null||authoredSurrounding!=110)throw new InvalidOperationException("Stopped exact owned network/four fields/returned roads and existing authored110m policy required.");
            var active=UnityEngine.Object.FindObjectsByType<ERModularBase>(FindObjectsSortMode.None);var activeTerrains=Terrain.activeTerrains;
            if(active.Length!=1||active[0]!=owner||activeTerrains.Length!=4||!activeTerrains.ToHashSet().SetEquals(terrains)||terrains.Any(t=>t==null||t.gameObject.scene!=owner.gameObject.scene||!AssetDatabase.GetAssetPath(t.terrainData).StartsWith("Assets/TruckDragonKingdom/VisualCandidates/",StringComparison.Ordinal)))throw new InvalidOperationException("Never smooth an unrelated active terrain/network or accepted/source field.");
            string profile=RequestedProfile();int expected=profile==PairProfile?2:1;
            var selected=actualReturnedRoads.Where(r=>r!=null&&r.gameObject!=null&&(r.gameObject.name=="caverns-volcano hairpin grading 9"||profile==PairProfile&&r.gameObject.name=="caverns-volcano")).ToArray();
            if(selected.Length!=expected||selected.Select(r=>r.gameObject.name).Distinct().Count()!=expected||selected.Any(r=>r.roadScript==null||r.roadScript.gameObject!=r.gameObject||!r.roadScript.transform.IsChildOf(owner.transform)||r.roadScript.gameObject.scene!=owner.gameObject.scene||!r.roadScript.terrainDeformation))throw new InvalidOperationException("Exactly the profile's measured actual returned NE road component(s) must belong to this disposable network.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");string output=at>=0&&at+1<args.Length?args[at+1]:"Logs/TruckSports/DragonKingdom";Directory.CreateDirectory(output);string receipt=Path.Combine(output,"actual-native-sdk-ne-falloff.json");if(File.Exists(receipt))throw new InvalidOperationException("Fresh vendor falloff receipt required.");
            var proof=new Evidence{profile=profile,authoredSurroundingDistance=authoredSurrounding,actualReturnedRoads=actualReturnedRoads.Count,selectedRoads=selected.Length,exactOwnedNetworkAndFourTiles=true,previousIndentDistance=owner.terrainSmoothIndentDistance,previousSurroundingDistance=owner.terrainSmoothSurroundingDistance,previousIndentStep=owner.indentSmoothStep,previousSurroundingStep=owner.surroundingSmoothStep,
                originalPreSdkRoofDeclared=originalProtectedUndergroundRoof.HasValue,originalPreSdkRoof=originalProtectedUndergroundRoof.GetValueOrDefault(),watchedSources=new[]{"Assets/EasyRoads3D/lib/EasyRoads3Dv3.dll","Assets/EasyRoads3D/lib/EasyRoads3Dv3.dll.meta","Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab","Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab.meta"}};LastEvidence=proof;var sources=proof.watchedSources.ToDictionary(p=>p,FileSha);
            proof.actualRoadDimensions=actualReturnedRoads.Where(r=>r!=null&&r.roadScript!=null).Select(r=>{var actual=r.roadScript;var type=r.GetRoadType();return new NativeRoadDimensions{name=actual.name,typeName=actual.rt?.roadTypeName,actualComponentId=actual.GetInstanceID(),roadWidth=actual.roadWidth,typeWidth=actual.rt==null?0:actual.rt.roadWidth,publicWrapperResolved=type!=null,publicWrapperWidth=type==null?0:type.roadWidth,laneWidth=actual.laneWidth,leftShoulderWidth=actual.leftShoulderWidth,rightShoulderWidth=actual.rightShoulderWidth};}).ToArray();
            var preSdk=originalProtectedUndergroundRoof.HasValue?terrains.ToDictionary(t=>t,t=>t.terrainData.GetHeights(0,0,t.terrainData.heightmapResolution,t.terrainData.heightmapResolution)):null;
            FragmentState fragmentState=null;
            try
            {
                if(profile==FragmentProfile){fragmentState=CreateMeasuredNeFragment(network,owner,terrains,actualReturnedRoads,authoredSurrounding,originalProtectedUndergroundRoof,proof);selected=selected.Concat(new[]{fragmentState.fragment}).ToArray();proof.selectedRoads=selected.Length;File.WriteAllText(receipt,JsonUtility.ToJson(proof,true));}
                var neStations=terrains.Where(t=>NeFailureCellReference.x>=t.transform.position.x&&NeFailureCellReference.x<t.transform.position.x+t.terrainData.size.x&&NeFailureCellReference.z>=t.transform.position.z&&NeFailureCellReference.z<t.transform.position.z+t.terrainData.size.z).ToDictionary(t=>t,t=>ActualNeVertexStations(t.transform.position,t.terrainData.size,t.terrainData.heightmapResolution));
                if(neStations.Count!=1||neStations.Values.Single().Length!=16)throw new InvalidOperationException("Exactly one actual NE field and16 prevalidated heightmap vertices required before any SDK operation.");proof.neVertexStations=16;
                owner.terrainSmoothIndentDistance=authoredSurrounding;owner.terrainSmoothSurroundingDistance=authoredSurrounding;
                foreach(var returned in selected)
                {
                    var road=returned.roadScript;var contour=Contours(road);var footprint=contour;footprint.Expand(new Vector3(authoredSurrounding*2,0,authoredSurrounding*2));
                    foreach(var terrain in terrains.Where(t=>IntersectsXZ(footprint,new Bounds(t.transform.position+t.terrainData.size*.5f,t.terrainData.size))))
                    foreach(string tool in new[]{"SmoothenIndentFalloff","SmoothenSurroundingFalloff"})
                    {
                        var rasterFootprint=CoverTerrainGridCells(footprint,terrain.transform.position,new Vector3(terrain.terrainData.size.x/(terrain.terrainData.heightmapResolution-1),0,terrain.terrainData.size.z/(terrain.terrainData.heightmapResolution-1)));
                        var before=terrains.ToDictionary(t=>t,t=>t.terrainData.GetHeights(0,0,t.terrainData.heightmapResolution,t.terrainData.heightmapResolution));var row=new Operation{road=returned.gameObject.name,actualRoadComponentId=road.GetInstanceID(),terrain=terrain.name,tool=tool,distance=authoredSurrounding,actualGeneratedContourBounds=contour,declaredToolFootprint=footprint,declaredRasterToolFootprint=rasterFootprint,indentStepBefore=owner.indentSmoothStep,surroundingStepBefore=owner.surroundingSmoothStep};proof.results.Add(row);
                        if(profile==FragmentProfile)
                        {
                            bool indent=tool=="SmoothenIndentFalloff";row.installedSdkStencil=MeasureInstalledSdkStencil((indent?road.leftIndentVecs:road.leftSurroundingVecs).ToArray(),(indent?road.rightIndentVecs:road.rightSurroundingVecs).ToArray(),road.bridgeElement.ToArray(),terrain.transform.position,terrain.terrainData.size,terrain.terrainData.heightmapScale,terrain.terrainData.heightmapResolution,authoredSurrounding,indent?owner.indentSmoothStep:owner.surroundingSmoothStep);row.maskAuthority="EXACT_INSTALLED_SDK_FINITE_INDEX_STENCIL_UNION_NO_HEIGHT_KERNEL";
                            if(originalProtectedUndergroundRoof.HasValue&&IntersectsXZ(row.installedSdkStencil.actualGridBounds,originalProtectedUndergroundRoof.Value))throw new InvalidOperationException("Exact installed SDK stencil reaches original protected cavern roof before tool invocation.");
                        }
                        var clock=System.Diagnostics.Stopwatch.StartNew();if(tool=="SmoothenIndentFalloff")network.SmoothenIndentFalloff(road,terrain);else network.SmoothenSurroundingFalloff(road,terrain);row.seconds=clock.Elapsed.TotalSeconds;row.indentStepAfter=owner.indentSmoothStep;row.surroundingStepAfter=owner.surroundingSmoothStep;var changes=new List<TileChange>();row.fields=changes.ToArray();var neHeights=new List<NeHeight>();
                        foreach(var field in terrains)
                        {
                            int n=field.terrainData.heightmapResolution;var after=field.terrainData.GetHeights(0,0,n,n);var change=new TileChange{terrain=field.name,data=AssetDatabase.GetAssetPath(field.terrainData),resolution=n,terrainOrigin=field.transform.position,terrainSize=field.terrainData.size,beforeHeightSha256=HeightSha(before[field]),afterHeightSha256=HeightSha(after)};var mask=new byte[n*n];var outsidePoints=new List<ChangedPoint>();
                            for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(after[z,x]!=before[field][z,x])
                            {
                                mask[z*n+x]=1;change.changedSamples++;proof.changedSamples++;float delta=(after[z,x]-before[field][z,x])*field.terrainData.size.y;change.maximumDelta=Mathf.Max(change.maximumDelta,Mathf.Abs(delta));var p=field.transform.position+new Vector3(x*field.terrainData.size.x/(n-1),0,z*field.terrainData.size.z/(n-1));p.y=field.transform.position.y+after[z,x]*field.terrainData.size.y;if(change.changedSamples==1)change.changedBounds=new Bounds(p,Vector3.zero);else change.changedBounds.Encapsulate(p);
                                if(!InsideXZ(footprint,p)){change.outsideContinuousToolFootprintChanges++;change.maximumContinuousOutsideDistance=Mathf.Max(change.maximumContinuousOutsideDistance,OutsideXZ(footprint,p));}
                                if(field!=terrain||(row.installedSdkStencil==null?!InsideXZ(rasterFootprint,p):!row.installedSdkStencil.allowed[z*n+x]))
                                {change.outsideDeclaredFootprintChanges++;float distance=OutsideXZ(rasterFootprint,p);change.maximumOutsideDelta=Mathf.Max(change.maximumOutsideDelta,Mathf.Abs(delta));change.maximumOutsideDistance=Mathf.Max(change.maximumOutsideDistance,distance);if(change.outsideDeclaredFootprintChanges==1)change.outsideChangedBounds=new Bounds(p,Vector3.zero);else change.outsideChangedBounds.Encapsulate(p);if(outsidePoints.Count<64)outsidePoints.Add(new ChangedPoint{position=p,beforeY=p.y-delta,afterY=p.y,delta=delta,outsideDistance=distance});}
                                if(originalProtectedUndergroundRoof.HasValue&&InsideXZ(originalProtectedUndergroundRoof.Value,p))change.protectedRoofChangedSamples++;
                            }
                            change.outsidePoints=outsidePoints.ToArray();
                            if(neStations.TryGetValue(field,out var stations))foreach(var point in stations)
                            {
                                float fx=(point.x-field.transform.position.x)/field.terrainData.size.x*(n-1),fz=(point.z-field.transform.position.z)/field.terrainData.size.z*(n-1);int ix=Mathf.RoundToInt(fx),iz=Mathf.RoundToInt(fz);float old=field.transform.position.y+before[field][iz,ix]*field.terrainData.size.y,next=field.transform.position.y+after[iz,ix]*field.terrainData.size.y;neHeights.Add(new NeHeight{terrain=field.name,point=point,column=ix,row=iz,resolution=n,beforeY=old,afterY=next,delta=next-old});
                            }
                            if(change.outsideDeclaredFootprintChanges>0){string stem=Path.Combine(output,"sdk-falloff-failed-field-"+proof.results.Count+"-"+field.name.Replace(' ','-'));change.failedBeforeFloat32=stem+".before.f32";change.failedAfterFloat32=stem+".after.f32";PreserveFailedField(change.failedBeforeFloat32,before[field]);PreserveFailedField(change.failedAfterFloat32,after);}
                            change.changedMaskSha256=Sha(mask);change.outsideDeclaredFootprintExact=change.outsideDeclaredFootprintChanges==0;changes.Add(change);
                        }
                        row.fields=changes.ToArray();row.neHeights=neHeights.ToArray();proof.operations++;File.WriteAllText(receipt,JsonUtility.ToJson(proof,true));if(row.installedSdkStencil!=null&&(tool=="SmoothenIndentFalloff"?owner.indentSmoothStep:owner.surroundingSmoothStep)!=row.installedSdkStencil.expectedFinalPhase)throw new InvalidOperationException("Actual installed SDK stencil phase disagrees with measured operation counter.");if(changes.Any(c=>!c.outsideDeclaredFootprintExact))throw new InvalidOperationException("Vendor operation changed outside its exact supplied owned tile/actual contour or installed discrete support.");
                    }
                }
                if(preSdk!=null)
                {
                    var roofs=new List<RoofComparison>();foreach(var terrain in terrains)
                    {
                        int n=terrain.terrainData.heightmapResolution;var after=terrain.terrainData.GetHeights(0,0,n,n);var beforeValues=new List<float>();var afterValues=new List<float>();var row=new RoofComparison{terrain=terrain.name};
                        for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                        {var p=terrain.transform.position+new Vector3(x*terrain.terrainData.size.x/(n-1),0,z*terrain.terrainData.size.z/(n-1));if(!InsideXZ(originalProtectedUndergroundRoof.Value,p))continue;float old=preSdk[terrain][z,x],next=after[z,x];beforeValues.Add(old);afterValues.Add(next);row.samples++;if(old!=next){row.changedSamples++;row.maximumDelta=Mathf.Max(row.maximumDelta,Mathf.Abs(old-next)*terrain.terrainData.size.y);}}
                        string Digest(List<float> values){var bytes=new byte[values.Count*4];Buffer.BlockCopy(values.ToArray(),0,bytes,0,bytes.Length);return Sha(bytes);}row.beforePreSdkRoofSha256=Digest(beforeValues);row.afterSdkRoofSha256=Digest(afterValues);row.exact=row.changedSamples==0;roofs.Add(row);
                    }
                    proof.roof=roofs.ToArray();proof.originalPreSdkRoofExact=roofs.All(r=>r.exact);if(!proof.originalPreSdkRoofExact)throw new InvalidOperationException("Vendor smoothing changed the original pre-SDK protected underground roof; never redefine its post-tool values as an accepted native baseline.");
                }
                if(proof.changedSamples==0)throw new InvalidOperationException("Actual installed vendor tools changed no terrain samples; no repair established.");
                proof.status="ACTUAL_VENDOR_NE_FALLOFF_OPERATIONS_RECORDED_REQUIRES_ORIGINAL_WORLD_QUALITY_AND_PHYSICAL_ACCEPTANCE";
            }
            catch(Exception error){proof.error=error.ToString();proof.status="REJECTED_NATIVE_VENDOR_FALLOFF_TRIAL";throw;}
            finally
            {
                if(fragmentState!=null)CloseMeasuredNeFragment(fragmentState,owner,proof);
                owner.terrainSmoothIndentDistance=proof.previousIndentDistance;owner.terrainSmoothSurroundingDistance=proof.previousSurroundingDistance;owner.indentSmoothStep=proof.previousIndentStep;owner.surroundingSmoothStep=proof.previousSurroundingStep;
                proof.settingsRestored=owner.terrainSmoothIndentDistance==proof.previousIndentDistance&&owner.terrainSmoothSurroundingDistance==proof.previousSurroundingDistance&&owner.indentSmoothStep==proof.previousIndentStep&&owner.surroundingSmoothStep==proof.previousSurroundingStep;proof.sourceBytesExact=sources.All(p=>File.Exists(p.Key)&&FileSha(p.Key)==p.Value);bool fragmentPreserved=fragmentState==null||proof.fragment.originalRoadAuthoritiesExact&&proof.fragment.originalTypeRegistryExact&&proof.fragment.temporaryRoadRemoved;File.WriteAllText(receipt,JsonUtility.ToJson(proof,true));if(!proof.settingsRestored||!proof.sourceBytesExact||!fragmentPreserved)throw new InvalidOperationException("Vendor falloff trial changed original source bytes/roads/types, failed temporary fragment cleanup or owned-setting restoration.");
            }
        }
    }
}
#endif
