#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class NativeSettlementBaseMesh
        {public string path,mesh,guid,localId,meshSha256,meshMetaSha256;public int vertices,baseVertices;public Bounds bounds;}
        [Serializable]public sealed class NativeSettlementBuildingFooting
        {
            public string region,name,source,sourceGuid,sourceSha256,sourceMetaSha256,actualRootSourceGlobalId,footingId;
            public string provenance="ACTUAL_RETAINED_SOURCE_BUILDING_AND_SAVED_ORIGINAL_WORLD_LAYOUT_NOT_A_NEW_AUTHORED_JSON_INSTANCE";
            public Vector3 before,after,scale;public Quaternion rotation;public Bounds beforePrimaryBounds,afterPrimaryBounds,measuredBaseFootprint;
            public int actualRootInstanceId,trial,baseVertexCount,excludedAnimatedMeshes,regionRejects,roadRejects,wingRejects,waterRejects,peerRejects;public float groundSurface,maximumNativeContactError;public Bounds originalRegionBounds,legacyPaddedBounds;public NativeSettlementFootingTrial[] trials;public NativeSettlementCreekParcel[] fallbackParcels;public bool usedCreekBankFallback;
            public bool sourceGeometryAndScalePreserved,originalRoadStartObjectiveAndWingClear,actualTerrainContactsVerified;
            public NativeSettlementBaseMesh[] baseMeshes;public RidgeGeometryBinding[] geometry;public AssemblyIdentityLink[] nativeRootCorrespondence;
        }
        [Serializable]public sealed class NativeSettlementFootingTrial
        {public int trial;public string reason,peer,phase;public NativeSettlementCreekParcel creekParcel;public Vector3 position;public Bounds primaryBounds,legacyBounds,measuredBase;public AuthoredClearanceWitness blocker;public AuthoredGroupFloorPoint[] primaryWaterPoints,legacyWaterPoints;}
        [Serializable]public sealed class NativeSettlementCreekParcel
        {
            public string phase="EXPLICIT_NEAREST_AUTHORED_CREEK_BANK_NORMAL_MEASURED_MODULE_PARCEL_AFTER_ALL192_LOCAL_TRIALS",region;
            public int segment,bankSide;public Vector3 segmentStart,segmentEnd,originalClosestCreekPoint,closestCreekPoint,normal,tangent,requestedRoot;
            public Bounds originalConnectedDistrict;public float creekCoreRadius=34,existingRetainingBank,projectedModuleHalfExtent,clearanceMeters=2,alongOffset,setbackMeters,centerlineDistance,sourceDistance;
        }
        [Serializable]public sealed class NativeSettlementFootingEvidence
        {
            public string scope="NATIVE_ONLY_RETAINED_PERIPHERAL_BUILDING_BASE_SEATING_AND_INDIVIDUAL_PHYSICAL_FOOTINGS_NOT_WORLD_ART_ACCEPTANCE",status="IN_PROGRESS",error;
            public string inputDefinitionRole="UNCHANGED_ORIGINAL_DEFINITION",originalDefinitionGraphSha,candidateDefinitionGraphSha;public string originalWorld,originalWorldGuid,originalWorldSha256,inputDefinition,inputDefinitionGuid,inputDefinitionSha256,inputDefinitionMetaSha256,geometryWitness,geometryWitnessSha256;
            public int buildings,baseVertices;public bool sourceBytesPreserved,originalCriticalPosesAndGraphExact,allNativeTerrainContactsVerified;public string[] changedSources;
            public NativeSettlementBuildingFooting[] parts;public NativeArchitectureFooting[] physicalFootings;
        }
        public static NativeSettlementFootingEvidence LastNativeSettlementFootings{get;private set;}
        static bool NativeSettlementAnimatedBelowRoot(Transform value,Transform root)
        {for(var t=value;t!=null&&t!=root;t=t.parent)if(t.GetComponent<Animator>()!=null)return true;return false;}
        static Bounds MeasureNativeSettlementBuildingBase(Transform root,out NativeSettlementBaseMesh[] evidence,out int excluded,out int baseVertices)
        {
            var rows=new List<NativeSettlementBaseMesh>();var points=new List<Tuple<MeshFilter,Vector3[]>>();excluded=0;baseVertices=0;float lowest=float.PositiveInfinity;
            foreach(var filter in RidgePrimaryMeshFilters(root))
            {
                if(NativeSettlementAnimatedBelowRoot(filter.transform,root)){excluded++;continue;}
                using(var data=Mesh.AcquireReadOnlyMeshData(filter.sharedMesh))using(var vertices=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp))
                {
                    data[0].GetVertices(vertices);var world=new Vector3[vertices.Length];var bounds=new Bounds();bool first=true;
                    for(int i=0;i<vertices.Length;i++){world[i]=filter.transform.TransformPoint(vertices[i]);if(first){bounds=new Bounds(world[i],Vector3.zero);first=false;}else bounds.Encapsulate(world[i]);}
                    if(first||bounds.size.y<.3f||bounds.size.x<.1f||bounds.size.z<.1f)continue;points.Add(Tuple.Create(filter,world));lowest=Mathf.Min(lowest,bounds.min.y);
                    if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter.sharedMesh,out string guid,out long id))throw new InvalidOperationException("Persistent real source base mesh required.");string path=AssetDatabase.GetAssetPath(filter.sharedMesh);rows.Add(new NativeSettlementBaseMesh{path=PathOf(filter.transform,root),mesh=path,guid=guid,localId=id.ToString(System.Globalization.CultureInfo.InvariantCulture),meshSha256=AssetSha(path),meshMetaSha256=AssetSha(path+".meta"),vertices=world.Length,bounds=bounds});
                }
            }
            if(float.IsInfinity(lowest)||points.Count==0)throw new InvalidOperationException("No actual non-animated structural source mesh base exists: "+root.name);
            var footprint=new Bounds();bool start=true;
            for(int i=0;i<points.Count;i++)foreach(var vertex in points[i].Item2)
                if(vertex.y<=lowest+.25f){var flat=new Vector3(vertex.x,lowest,vertex.z);if(start){footprint=new Bounds(flat,Vector3.zero);start=false;}else footprint.Encapsulate(flat);rows[i].baseVertices++;baseVertices++;}
            if(start||baseVertices<4||footprint.size.x<.5f||footprint.size.z<.5f||footprint.size.x>65||footprint.size.z>65)throw new InvalidOperationException("Actual lower structural vertices must define a bounded individual building base, not a sail/roof/whole-settlement envelope: "+root.name);
            evidence=rows.Where(r=>r.baseVertices>0).ToArray();return footprint;
        }
        // Invoked only in the new explicit native-relief candidate, after the
        // established source-building replacements and before terrain cores.
        // Normal/default candidates and all source assets remain unchanged.
        public static NativeSettlementFootingEvidence SeatRetainedBuildingsForNativeRelief(DragonKingdomWorld world,string geometryWitnessPath)=>SeatRetainedBuildingsForNativeRelief(world,geometryWitnessPath,null);
        static NativeSettlementFootingEvidence SeatRetainedBuildingsForNativeRelief(DragonKingdomWorld world,string geometryWitnessPath,OwnedDefinitionProvenance definitionProvenance)
        {
            if(world==null||EditorUtility.IsPersistent(world)||!world.gameObject.scene.IsValid()||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||string.IsNullOrEmpty(geometryWitnessPath))throw new InvalidOperationException("Stopped disposable native-relief candidate and explicit output witness required.");
            string witnessAbsolute=Path.GetFullPath(geometryWitnessPath),outputRoot=Path.GetFullPath(Output()).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(!witnessAbsolute.StartsWith(outputRoot,StringComparison.OrdinalIgnoreCase)||!witnessAbsolute.EndsWith(".json",StringComparison.OrdinalIgnoreCase)||File.Exists(witnessAbsolute))throw new InvalidOperationException("New explicit evidence JSON inside this unique native run output only; never overwrite source assets or previous witnesses.");
            string definition=definitionProvenance!=null?definitionProvenance.ValidateAndPath(world):AssetDatabase.GetAssetPath(world.definition);if(!RidgeDefinitionPath(definition))throw new InvalidOperationException("Actual unchanged original definition required for retained-building provenance.");
            var proof=new NativeSettlementFootingEvidence{originalWorld=Prefab,originalWorldGuid=AssetDatabase.AssetPathToGUID(Prefab),originalWorldSha256=AssetSha(Prefab),inputDefinition=definition,inputDefinitionGuid=AssetDatabase.AssetPathToGUID(definition),inputDefinitionSha256=AssetSha(definition),inputDefinitionMetaSha256=AssetSha(definition+".meta"),geometryWitness=geometryWitnessPath};LastNativeSettlementFootings=proof;if(definitionProvenance!=null){proof.inputDefinitionRole="UNCHANGED_CANONICAL_PROVENANCE_FOR_EXACT_PREQUALIFIED_IN_MEMORY_OWNED_ROAD_CANDIDATE";proof.originalDefinitionGraphSha=definitionProvenance.originalGraphSha;proof.candidateDefinitionGraphSha=definitionProvenance.candidateGraphSha;proof.scope+="_PROVIDED_CANDIDATE_GRAPH_REMAINS_EXACT_DURING_SEATING_NOT_ORIGINAL_TARGET_GRAPH_IDENTITY";}
            var graph=JsonUtility.ToJson(world.definition.graph);var critical=CriticalTransforms(world).Distinct().ToDictionary(t=>t,Pose);var sources=new Dictionary<string,string>{{Prefab,AssetSha(Prefab)},{definition,AssetSha(definition)},{definition+".meta",AssetSha(definition+".meta")}};var parts=new List<NativeSettlementBuildingFooting>();var footings=new List<NativeArchitectureFooting>();var originalPoses=new Dictionary<Transform,Vector3>();
            try
            {
                foreach(var region in world.regions.Where(r=>r.id=="village"||r.id=="harbor"||r.id=="refuge"))foreach(Transform root in region.root)
                {
                    if(!root.name.StartsWith("P_BLD_house",StringComparison.Ordinal)&&root.name!="Complete owned windmill house14")continue;
                    if(world.regions.SelectMany(r=>r.nests.Concat(r.perches).Concat(r.treasureCaches)).Any(a=>a!=null&&a.IsChildOf(root)))throw new InvalidOperationException("Do not move an original referenced actor/objective anchor by silently reseating its parent building.");
                    string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root.gameObject);var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null||string.IsNullOrEmpty(path)||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long _))throw new InvalidOperationException("Actual retained source prefab identity required.");
                    foreach(string watched in new[]{path,path+".meta"})if(!sources.ContainsKey(watched))sources.Add(watched,AssetSha(watched));originalPoses.Add(root,root.position);
                    var row=new NativeSettlementBuildingFooting{actualRootInstanceId=root.GetInstanceID(),region=region.id,name=root.name,source=path,sourceGuid=guid,sourceSha256=AssetSha(path),sourceMetaSha256=AssetSha(path+".meta"),actualRootSourceGlobalId=GlobalObjectId.GetGlobalObjectIdSlow(source).ToString(),before=root.position,rotation=root.rotation,scale=root.localScale,groundSurface=region.root.position.y,nativeRootCorrespondence=AllNativeAssemblyCorrespondences(root)};
                    row.originalRegionBounds=world.definition.FindRegion(region.id).bounds;row.legacyPaddedBounds=BoundsOf(root);var trials=new List<NativeSettlementFootingTrial>();parts.Add(row);proof.parts=parts.ToArray();RidgeMeshSamples(root,out row.beforePrimaryBounds,out _,out _);var baseBefore=MeasureNativeSettlementBuildingBase(root,out var initialMeshes,out int excluded,out int baseVertices);row.baseVertexCount=baseVertices;row.excludedAnimatedMeshes=excluded;var beforeGeometry=RidgeGeometryBindings(root,new Dictionary<string,string>());var childPoses=root.GetComponentsInChildren<Transform>(true).Where(t=>t!=root).ToDictionary(t=>t,t=>t.localPosition.ToString("R")+"/"+t.localRotation.ToString("R")+"/"+t.localScale.ToString("R"));bool placed=false;
                    NativeSettlementCreekParcel[] fallback=null;
                    for(int trial=0;;trial++)
                    {
                        if(trial==192)
                        {
                            if((region.id!="village"&&region.id!="harbor")||row.waterRejects==0)break;
                            fallback=DeriveNativeSettlementCreekBankParcels(world,row,baseBefore);row.fallbackParcels=fallback;
                        }
                        if(trial>=192+(fallback?.Length??0))break;NativeSettlementCreekParcel parcel=trial<192?null:fallback[trial-192];
                        if(parcel==null){float radius=trial==0?0:3*Mathf.Sqrt(trial),angle=trial*2.399963f;root.position=row.before+new Vector3(Mathf.Cos(angle)*radius,row.groundSurface-baseBefore.min.y,Mathf.Sin(angle)*radius);}
                        else root.position=parcel.requestedRoot;RidgeMeshSamples(root,out row.afterPrimaryBounds,out _,out _);
                        var attempt=new NativeSettlementFootingTrial{trial=trial,phase=parcel==null?"ORIGINAL_LOCAL192_0_TO_42M":"EXPLICIT_NEAREST_AUTHORED_CREEK_BANK_MODULE_PARCEL",creekParcel=parcel,position=root.position,primaryBounds=row.afterPrimaryBounds,legacyBounds=BoundsOf(root),measuredBase=new Bounds(baseBefore.center+root.position-row.before,baseBefore.size)};trials.Add(attempt);row.trials=trials.ToArray();
                        if(!RidgeInsideRegionXZ(row.originalRegionBounds,row.afterPrimaryBounds)){row.regionRejects++;attempt.reason="ACTUAL_PRIMARY_MESH_OUTSIDE_ORIGINAL_REGION";continue;}
                        if(parcel!=null&&!RidgeInsideRegionXZ(parcel.originalConnectedDistrict,row.afterPrimaryBounds)){row.regionRejects++;attempt.reason="CREEK_BANK_FALLBACK_OUTSIDE_ORIGINAL_CONNECTED_DISTRICT";continue;}
                        if(!FinishBoundsClearOfRoads(world,row.afterPrimaryBounds,w=>attempt.blocker=w)){row.roadRejects++;attempt.reason="ORIGINAL_ROAD_START_OBJECTIVE_ENVELOPE";continue;}
                        if(!RidgeWingClear(world,row.afterPrimaryBounds)){row.wingRejects++;attempt.reason="ORIGINAL_WING_CORRIDOR";continue;}
                        if((region.id=="village"||region.id=="harbor")&&!BuildingClearOfActualWatercourse(region.id,root))
                        {row.waterRejects++;attempt.reason="EXISTING_BUILDING_WATERCOURSE_GUARD";attempt.primaryWaterPoints=NativeSettlementWaterPoints(region.id,attempt.primaryBounds);attempt.legacyWaterPoints=NativeSettlementWaterPoints(region.id,attempt.legacyBounds);continue;}
                        var peer=parts.FirstOrDefault(other=>other!=row&&other.region==region.id&&RidgeInterior(other.afterPrimaryBounds).Intersects(RidgeInterior(row.afterPrimaryBounds)));if(peer!=null){row.peerRejects++;attempt.reason="RETAINED_BUILDING_PEER_PRIMARY_ENVELOPE";attempt.peer=peer.footingId;continue;}if(region.id=="village"||region.id=="harbor"){attempt.primaryWaterPoints=NativeSettlementWaterPoints(region.id,attempt.primaryBounds);attempt.legacyWaterPoints=NativeSettlementWaterPoints(region.id,attempt.legacyBounds);}attempt.reason="ACCEPTED";
                        row.trial=trial;row.usedCreekBankFallback=parcel!=null;placed=true;break;
                    }
                    if(!placed)throw new InvalidOperationException("Retained complete source building has no clear native-floor parcel in original192 local trials or explicit nearest-creek bank parcels: "+region.id+"/"+root.name);
                    row.measuredBaseFootprint=MeasureNativeSettlementBuildingBase(root,out row.baseMeshes,out excluded,out baseVertices);row.after=root.position;row.geometry=RidgeGeometryBindings(root,new Dictionary<string,string>());row.sourceGeometryAndScalePreserved=root.localScale==row.scale&&Quaternion.Angle(root.rotation,row.rotation)<.0001f&&NativeRetainedGeometryIdentityExact(beforeGeometry,row.geometry)&&childPoses.All(p=>p.Key.localPosition.ToString("R")+"/"+p.Key.localRotation.ToString("R")+"/"+p.Key.localScale.ToString("R")==p.Value);row.originalRoadStartObjectiveAndWingClear=true;row.footingId="retained-native-building:"+region.id+":"+root.GetSiblingIndex()+":"+root.name;
                    if(!row.sourceGeometryAndScalePreserved)throw new InvalidOperationException("Native re-seat changed a retained source mesh/collider/material/scale/orientation.");proof.parts=parts.ToArray();proof.buildings=parts.Count;proof.baseVertices+=row.baseVertexCount;
                    footings.Add(new NativeArchitectureFooting{id=row.footingId,layout=Prefab,layoutSha256=proof.originalWorldSha256,instance=region.id+"/"+root.GetSiblingIndex()+"/"+root.name+"/"+row.actualRootSourceGlobalId,sourcePrefab=path,sourcePrefabGuid=guid,sourcePrefabSha256=row.sourceSha256,sourcePrefabMetaSha256=row.sourceMetaSha256,sourceGeometryWitness=geometryWitnessPath,physicalFootprint=new Bounds(new Vector3(row.measuredBaseFootprint.center.x,row.groundSurface,row.measuredBaseFootprint.center.z),new Vector3(row.measuredBaseFootprint.size.x,.16f,row.measuredBaseFootprint.size.z)),surfaceY=row.groundSurface});
                }
                if(parts.Count==0)throw new InvalidOperationException("Actual retained settlement buildings are required; no empty footing adapter acceptance.");proof.originalCriticalPosesAndGraphExact=JsonUtility.ToJson(world.definition.graph)==graph&&critical.All(p=>p.Key!=null&&Pose(p.Key)==p.Value);if(!proof.originalCriticalPosesAndGraphExact)throw new InvalidOperationException("Native building seating changed an original critical pose/road graph.");
                proof.status="ACTUAL_RETAINED_BASES_SEATED_AND_MEASURED_REQUIRES_COORDINATED_NATIVE_TERRAIN_CONTACT_AND_ART_ACCEPTANCE";
            }
            catch(Exception e){proof.status="REJECTED_NATIVE_RETAINED_BUILDING_FOOTINGS";proof.error=e.ToString();foreach(var before in originalPoses)if(before.Key!=null)before.Key.position=before.Value;throw;}
            finally
            {
                proof.parts=parts.ToArray();proof.changedSources=sources.Where(p=>AssetSha(p.Key)!=p.Value).Select(p=>p.Key).ToArray();proof.sourceBytesPreserved=proof.changedSources.Length==0;Directory.CreateDirectory(Path.GetDirectoryName(Io(geometryWitnessPath)));File.WriteAllText(Io(geometryWitnessPath),JsonUtility.ToJson(proof,true));proof.geometryWitnessSha256=AssetSha(geometryWitnessPath);foreach(var footing in footings)footing.sourceGeometryWitnessSha256=proof.geometryWitnessSha256;proof.physicalFootings=footings.ToArray();if(!proof.sourceBytesPreserved)throw new InvalidOperationException("Native retained-building footing stage changed original sources.");
            }
            return proof;
        }
        static NativeSettlementCreekParcel[] DeriveNativeSettlementCreekBankParcels(DragonKingdomWorld world,NativeSettlementBuildingFooting row,Bounds measuredBase)
        {
            var district=CoherentSettlementFields(world).Single(f=>f.region==row.region);var originalCenter=row.legacyPaddedBounds.center;float nearest=float.PositiveInfinity,projection=0;int segment=-1;Vector3 closest=Vector3.zero,tangent=Vector3.zero;
            for(int i=1;i<FieldCreek.Length;i++)
            {var a=FieldCreek[i-1];var b=FieldCreek[i];a.y=b.y=0;var d=b-a;if(d.sqrMagnitude<.0001f)continue;float t=Mathf.Clamp01(Vector3.Dot(new Vector3(originalCenter.x,0,originalCenter.z)-a,d)/d.sqrMagnitude);var q=a+d*t;float distance=Vector2.Distance(new Vector2(q.x,q.z),new Vector2(originalCenter.x,originalCenter.z));if(distance<nearest){nearest=distance;segment=i;projection=t;closest=q;tangent=d.normalized;}}
            if(segment<0)throw new InvalidOperationException("Actual authored creek polyline must have a nondegenerate nearest segment.");
            var normal=new Vector3(-tangent.z,0,tangent.x);float side=Vector3.Dot(new Vector3(originalCenter.x,0,originalCenter.z)-closest,normal);int firstSide=side<0?-1:1;float projectedHalf=Mathf.Abs(normal.x)*row.legacyPaddedBounds.extents.x+Mathf.Abs(normal.z)*row.legacyPaddedBounds.extents.z;var result=new List<NativeSettlementCreekParcel>();var start=FieldCreek[segment-1];var end=FieldCreek[segment];start.y=end.y=0;float length=Vector3.Distance(start,end);
            // Two explicit banks of the nearest real creek segment, bounded
            // to48m along that segment and16m additional bank setback. This is
            // not a wider radial search or permission to fill the creek. The
            // original12m bank, hydrology/road/wing/peer guards remain unchanged.
            foreach(int bankSide in new[]{firstSide,-firstSide})foreach(float along in new[]{0f,8,-8,16,-16,24,-24,32,-32,40,-40,48,-48})foreach(float setback in new[]{0f,8,16})
            {
                float alongClamped=Mathf.Clamp(projection*length+along,0,length);var q=start+tangent*alongClamped;float offset=34+UrbanRetainingBank+projectedHalf+2+setback;var center=q+normal*(bankSide*offset);var delta=new Vector3(center.x-originalCenter.x,row.groundSurface-measuredBase.min.y,center.z-originalCenter.z);var requested=row.before+delta;
                result.Add(new NativeSettlementCreekParcel{region=row.region,segment=segment,bankSide=bankSide,segmentStart=start,segmentEnd=end,originalClosestCreekPoint=closest,closestCreekPoint=q,normal=normal*bankSide,tangent=tangent,requestedRoot=requested,originalConnectedDistrict=district.bounds,existingRetainingBank=UrbanRetainingBank,projectedModuleHalfExtent=projectedHalf,alongOffset=alongClamped-projection*length,setbackMeters=setback,centerlineDistance=offset,sourceDistance=Vector2.Distance(new Vector2(requested.x,requested.z),new Vector2(row.before.x,row.before.z))});
            }
            return result.ToArray();
        }
        static AuthoredGroupFloorPoint[] NativeSettlementWaterPoints(string region,Bounds bounds)
        {
            var points=new List<AuthoredGroupFloorPoint>();foreach(float z in new[]{bounds.min.z,bounds.center.z,bounds.max.z})foreach(float x in new[]{bounds.min.x,bounds.center.x,bounds.max.x}){var p=new Vector3(x,0,z);points.Add(new AuthoredGroupFloorPoint{point=p,classifiedDry=IntendedDrySettlementPosition(region,p),creekBankDistance=CreekBankDistance(p)});}return points.ToArray();
        }
        public static NativeSettlementFootingEvidence CaptureRetainedNativeBuildingBaseStageFromCommandLine()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);if(source==null)throw new InvalidOperationException("Actual original world prefab required for retained-base native stage.");string before=AssetSha(Prefab);var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();GameObject instance=null;NativeSettlementFootingEvidence proof=null;
            try{instance=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);proof=SeatRetainedBuildingsForNativeRelief(instance.GetComponent<DragonKingdomWorld>(),Path.Combine(Output(),"retained-building-base-geometry-witness.json"));return proof;}
            finally{if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"retained-building-native-base-stage.json"),JsonUtility.ToJson(proof??LastNativeSettlementFootings,true));if(AssetSha(Prefab)!=before)throw new InvalidOperationException("Disposable retained-base stage changed the original source world.");}
        }
        static bool NativeRetainedGeometryIdentityExact(RidgeGeometryBinding[] before,RidgeGeometryBinding[] after)
        {
            // Every child local TRS is independently checked byte-exact above.
            // A world translation can change inverse-matrix roundoff despite
            // identical local geometry; compare all authored identity/shape
            // fields without pretending that computed matrix floats are assets.
            if(before.Length!=after.Length)return false;
            for(int i=0;i<before.Length;i++){var a=JsonUtility.FromJson<RidgeGeometryBinding>(JsonUtility.ToJson(before[i]));var b=JsonUtility.FromJson<RidgeGeometryBinding>(JsonUtility.ToJson(after[i]));a.rootMatrix=null;b.rootMatrix=null;if(JsonUtility.ToJson(a)!=JsonUtility.ToJson(b))return false;}return true;
        }
        public static void ValidateRetainedNativeBuildingTerrainContacts(DragonKingdomWorld world,NativeSettlementFootingEvidence proof)
        {
            if(world==null||proof==null||!proof.sourceBytesPreserved||!proof.originalCriticalPosesAndGraphExact||proof.parts==null||proof.parts.Length==0||AssetSha(proof.geometryWitness)!=proof.geometryWitnessSha256)throw new InvalidOperationException("Actual original measured base witness required after native terrain coordination.");
            foreach(var part in proof.parts)
            {
                var root=world.Region(part.region).root.Cast<Transform>().Single(t=>t.name==part.name&&t.position==part.after);var b=MeasureNativeSettlementBuildingBase(root,out _,out _,out _);float error=0;
                foreach(float x in new[]{b.min.x,b.center.x,b.max.x})foreach(float z in new[]{b.min.z,b.center.z,b.max.z})
                {float ground=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,new Vector3(x,part.groundSurface,z));error=Mathf.Max(error,Mathf.Abs(ground-(part.groundSurface-.08f)));}
                part.maximumNativeContactError=error;part.actualTerrainContactsVerified=error<=.25f&&Mathf.Abs(b.min.y-part.groundSurface)<=.02f;if(!part.actualTerrainContactsVerified)throw new InvalidOperationException("Retained individual actual building base is floating or buried after native terrain coordination: "+part.region+"/"+part.name+" error="+error);
            }
            proof.allNativeTerrainContactsVerified=proof.parts.All(p=>p.actualTerrainContactsVerified);
        }
    }
}
#endif
