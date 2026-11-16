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
        [Serializable]public sealed class FinishedWaterfrontHull
        {public string instance,source;public Bounds bounds;public float keel,draft,minimumKeelClearance;public int waterlineIntersections,depthSamples;public Vector3[] waterline;public bool entirelyInOriginalSeaCore,actualWaterlinePassed,actualBedContactsPassed;}
        [Serializable]public sealed class FinishedWaterfront
        {
            public string scope="INTACT_30_SOURCE_INSTANCE_FISHING_WATERFRONT_WITH_SEPARATE_NATIVE_HULL_BED_PILE_AND_GANGWAY_ACCEPTANCE",status="SOURCE_GEOMETRY_ONLY_REQUIRES_ACTUAL_COORDINATED_BED_AND_PHYSICAL_SUPPORTS",error;
            public float seaLevel=-2,originalSeaCoreBoundary=-650,boatNorthMargin=2,requiredDraft=.22f,requiredKeelClearance=3;public Vector3 wholeFramePosition;public Bounds wholeFrameBounds;
            public bool actualSourceHullGeometryPassed,all30SourcePosesPreserved,noWaterfrontTerrainPads,actualBedAndPhysicalSupportsPassed,sourceGeometryPreserved,originalRoadsAndFerryPreserved;
            public int supportedStructures,gangwayPlanks,gangwayPierStations,ferryDepartureQueries;public bool ferryDepartureClear,sourceAndTerrainBytesPreserved;public FinishedWaterfrontHull[] boats;public HarborPart[] addedParts;public HarborPierContact[] pierContacts;public HarborSourceBearingContact[] sourceBearings;public HarborDeckContact[] gangwayContacts;
        }
        static string ResolveFinishedWaterfrontTimberSource()
        {
            // Match WorldAuthoring.Place's existing Knights prefab-or-model
            // authority. The purchased beam is currently a real FBX model;
            // an absent optional prefab is not an invented required asset.
            string requested=DragonKingdomWorldAuthoring.Knights+"SM_Prop_Beam_01.prefab";var source=AssetDatabase.LoadAssetAtPath<GameObject>(requested);
            if(source==null&&requested.StartsWith(DragonKingdomWorldAuthoring.Knights,StringComparison.Ordinal))source=AssetDatabase.LoadAssetAtPath<GameObject>(requested.Replace("/Prefabs/Buildings/","/Models/").Replace(".prefab",".fbx"));
            if(source==null)throw new InvalidOperationException("Dependency-closed actual owned beam prefab or its existing model authority required.");return AssetDatabase.GetAssetPath(source);
        }
        static string[] FinishedWaterfrontTimberDependencies()
        {
            string source=ResolveFinishedWaterfrontTimberSource();var assets=AssetDatabase.GetDependencies(source,true).Concat(new[]{source}).Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)).Distinct().ToArray();
            foreach(string path in assets)if(!File.Exists(Io(path))||!File.Exists(Io(path+".meta")))throw new InvalidOperationException("Actual resolved waterfront timber dependency or metadata missing: "+path);
            return assets.SelectMany(p=>new[]{p,p+".meta"}).ToArray();
        }
        [Serializable]sealed class FinishedWaterfrontTimberSourceProbe
        {public string scope="ACTUAL_EXISTING_BEAM_PREFAB_OR_FBX_RESOLUTION_AND_FIT_OWNED_BEAM_INSTANCE_NOT_WATERFRONT_SUPPORT_ACCEPTANCE",requestedPrefab,resolvedSource,actualInstantiatedSource;public int dependencyFiles,primaryMeshes,primaryVertices,colliders;public bool requestedPrefabPresent,resolverMatchesProductionPlacement,sourceAndMetadataBytesExact,temporarySceneClosed;public ArtistDependency[] dependencies;}
        public static string CaptureFinishedWaterfrontTimberSourceProbe()
        {
            string requested=DragonKingdomWorldAuthoring.Knights+"SM_Prop_Beam_01.prefab",resolved=ResolveFinishedWaterfrontTimberSource();var paths=FinishedWaterfrontTimberDependencies();var watched=paths.ToDictionary(p=>p,AssetSha);var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();GameObject parent=null;var proof=new FinishedWaterfrontTimberSourceProbe{requestedPrefab=requested,resolvedSource=resolved,requestedPrefabPresent=AssetDatabase.LoadAssetAtPath<GameObject>(requested)!=null,dependencyFiles=paths.Length};
            try
            {
                parent=new GameObject("Temporary actual waterfront beam source proof");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(parent,scene);var beam=FitOwnedBeam(parent.transform,Vector3.zero,Vector3.forward,3,4,.18f,"Actual existing source beam instance",Root+"VisualCandidates/NotCreatedByBeamSourceProbe",Vector3.up,false);
                proof.actualInstantiatedSource=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(beam.gameObject);proof.resolverMatchesProductionPlacement=proof.actualInstantiatedSource==resolved;var filters=RidgePrimaryMeshFilters(beam).ToArray();proof.primaryMeshes=filters.Length;proof.primaryVertices=filters.Sum(f=>f.sharedMesh.vertexCount);var colliders=beam.GetComponentsInChildren<Collider>(true);proof.colliders=colliders.Count(c=>c.enabled&&!c.isTrigger);if(!proof.resolverMatchesProductionPlacement||proof.primaryVertices==0||proof.colliders==0||beam.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null)))throw new InvalidOperationException("Actual existing production beam placement/source geometry/material/collision does not match resolved authority.");
                proof.dependencies=paths.Where(p=>!p.EndsWith(".meta",StringComparison.Ordinal)).Select(p=>new ArtistDependency{path=p,guid=AssetDatabase.AssetPathToGUID(p),sha256=AssetSha(p),metaSha256=AssetSha(p+".meta")}).ToArray();
            }
            finally
            {
                if(parent!=null)UnityEngine.Object.DestroyImmediate(parent);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);proof.temporarySceneClosed=!scene.IsValid();proof.sourceAndMetadataBytesExact=watched.All(p=>AssetSha(p.Key)==p.Value);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"finished-waterfront-actual-timber-source.json"),JsonUtility.ToJson(proof,true));if(!proof.sourceAndMetadataBytesExact)throw new InvalidOperationException("Read-only actual beam-source proof changed original dependency bytes.");
            }
            return JsonUtility.ToJson(proof,true);
        }
        static Bounds ArchitecturePrimaryEnvelope(Transform root,out int meshes,out int vertexCount)
        {
            var bounds=new Bounds();bool first=true;meshes=vertexCount=0;
            foreach(var filter in RidgePrimaryMeshFilters(root))using(var data=Mesh.AcquireReadOnlyMeshData(filter.sharedMesh))using(var vertices=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp))
            {data[0].GetVertices(vertices);meshes++;vertexCount+=vertices.Length;foreach(var vertex in vertices){var point=filter.transform.TransformPoint(vertex);if(!ArchitectureFinite(point))throw new InvalidOperationException("Nonfinite actual architecture mesh vertex.");if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}}
            // Architectural sheets/ground tiles can be genuinely flat or have
            // fewer than the eight samples required by the natural-rock helper.
            // Keep their REAL vertices/collision; never manufacture thickness.
            if(first||vertexCount<3||bounds.size.sqrMagnitude<.000001f)throw new InvalidOperationException("Actual nonempty primary architecture mesh required: "+root.name);return bounds;
        }
        static void PositionFinishedFishingFrame(Transform frame,ArchitectureGeometryLayout layout,List<Transform> roots,FinishedArchitectureGroup group)
        {
            var boats=layout.instances.Select((row,i)=>new{row,root=roots[i]}).Where(p=>p.row.prefab.EndsWith("/TB_Prop_Ship.prefab",StringComparison.Ordinal)).ToArray();if(boats.Length!=2)throw new InvalidOperationException("Both original source fishing boats are required.");
            var before=boats.Select(b=>ArchitecturePrimaryEnvelope(b.root,out _,out _)).ToArray();float z=-652-before.Max(b=>b.max.z),y=-2-.22f-before.Min(b=>b.min.y);frame.position+=new Vector3(0,y,z);group.placementAuthority="INTACT_SOURCE_FRAME_SHALLOWER_HULL_MAX_Z_AT_ORIGINAL_SEA_CORE_MINUS_2M_AND_22CM_ACTUAL_SOURCE_KEEL_DRAFT";
            group.declaredCompositionParcel=new Bounds(new Vector3(570,10,-646.6873f),new Vector3(50,80,65));
        }
        static Vector3[] ActualSourceHullWaterline(Transform boat,float sea)
        {
            var points=new List<Vector3>();foreach(var filter in RidgePrimaryMeshFilters(boat))
            {var vertices=filter.sharedMesh.vertices;var triangles=filter.sharedMesh.triangles;for(int i=0;i<triangles.Length;i+=3)for(int edge=0;edge<3;edge++){var a=filter.transform.TransformPoint(vertices[triangles[i+edge]]);var b=filter.transform.TransformPoint(vertices[triangles[i+(edge+1)%3]]);if((a.y-sea)*(b.y-sea)>=0||Mathf.Abs(a.y-b.y)<.00001f)continue;points.Add(Vector3.Lerp(a,b,(sea-a.y)/(b.y-a.y)));}}return points.ToArray();
        }
        static FinishedWaterfront MeasureFinishedWaterfrontSource(Transform frame,FinishedArchitectureGroup group)
        {
            var result=new FinishedWaterfront{wholeFramePosition=frame.position,wholeFrameBounds=group.bounds,noWaterfrontTerrainPads=group.parts.All(p=>string.IsNullOrEmpty(p.footingId)),all30SourcePosesPreserved=group.parts.Length==30&&group.parts.All(p=>p.sourcePoseExact&&p.sourceGeometryExact)};var boats=new List<FinishedWaterfrontHull>();
            foreach(var part in group.parts.Where(p=>p.source.EndsWith("/TB_Prop_Ship.prefab",StringComparison.Ordinal)))
            {var root=frame.GetComponentsInChildren<Transform>(true).Single(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)==part.source&&t.position==part.composedRootPosition);var bounds=ArchitecturePrimaryEnvelope(root,out _,out _);var line=ActualSourceHullWaterline(root,result.seaLevel);float draft=result.seaLevel-bounds.min.y;boats.Add(new FinishedWaterfrontHull{instance=part.instance,source=part.source,bounds=bounds,keel=bounds.min.y,draft=draft,waterline=line,waterlineIntersections=line.Length,entirelyInOriginalSeaCore=bounds.max.z<=result.originalSeaCoreBoundary-result.boatNorthMargin+.001f,actualWaterlinePassed=line.Length>=6&&draft>.1f&&draft<1});}
            result.boats=boats.ToArray();result.actualSourceHullGeometryPassed=boats.Count==2&&boats.All(b=>b.entirelyInOriginalSeaCore&&b.actualWaterlinePassed)&&result.noWaterfrontTerrainPads&&result.all30SourcePosesPreserved;if(!result.actualSourceHullGeometryPassed)throw new InvalidOperationException("Actual source fishing hull waterline/sea-core/pose qualification rejected: "+JsonUtility.ToJson(result));return result;
        }
        // After real candidate terrain coordination ONLY. No sea fill, invented
        // depth, boat teleport, source reparenting or dock-floor terrain pads.
        public static FinishedWaterfront CompleteFinishedWaterfrontPhysicalSupport(DragonKingdomWorld world,FinishedArchitectureComposition composition,string folder,bool configureMaterials=true)
        {
            if(world==null||EditorUtility.IsPersistent(world)||!world.gameObject.scene.IsValid()||composition?.waterfront==null||!composition.waterfront.actualSourceHullGeometryPassed||world.GetComponentsInChildren<Terrain>(true).Length<4||EditorApplication.isPlayingOrWillChangePlaymode||!folder.StartsWith(Root+"VisualCandidates/",StringComparison.Ordinal)||folder.Substring((Root+"VisualCandidates/").Length).Contains("/"))throw new InvalidOperationException("Actual four coordinated fields and original source waterfront geometry required.");
            var proof=composition.waterfront;var group=composition.groups.Single(g=>g.key=="fishing-warehouse");var frame=world.Region("harbor").root.Cast<Transform>().Single(t=>t.name==group.name);if(world.Region("harbor").root.Cast<Transform>().Any(t=>t.name=="Finished source fishing supports and gangway"))throw new InvalidOperationException("Fresh candidate waterfront support only.");var original=RidgeGeometryBindings(frame,new Dictionary<string,string>());string originalJson=JsonUtility.ToJson(new RidgeGeometryContainer{bindings=original});var critical=CriticalTransforms(world).Distinct().ToDictionary(t=>t,Pose);string graph=JsonUtility.ToJson(world.definition.graph);var root=new GameObject("Finished source fishing supports and gangway").transform;root.SetParent(world.Region("harbor").root,false);root.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var added=new List<HarborPart>();var piers=new List<HarborPierContact>();var bearings=new List<HarborSourceBearingContact>();var walk=new List<HarborDeckContact>();
            var watched=group.parts.SelectMany(p=>new[]{p.source,p.source+".meta"}).Concat(original.Where(p=>!string.IsNullOrEmpty(p.mesh)).SelectMany(p=>new[]{p.mesh,p.mesh+".meta"})).Concat(world.GetComponentsInChildren<Terrain>(true).SelectMany(t=>new[]{AssetDatabase.GetAssetPath(t.terrainData),AssetDatabase.GetAssetPath(t.terrainData)+".meta"})).Concat(FinishedWaterfrontTimberDependencies()).Where(p=>!string.IsNullOrEmpty(p)).Distinct().ToDictionary(p=>p,AssetSha);
            proof.error=null;proof.status="MEASURING_ACTUAL_NATIVE_WATERFRONT_SUPPORT";proof.supportedStructures=proof.gangwayPlanks=proof.gangwayPierStations=proof.ferryDepartureQueries=0;proof.actualBedAndPhysicalSupportsPassed=false;
            try
            {
                foreach(var boat in proof.boats)
                {
                    boat.depthSamples=0;
                    float minimum=float.PositiveInfinity;foreach(float x in new[]{boat.bounds.min.x,boat.bounds.center.x,boat.bounds.max.x})foreach(float z in new[]{boat.bounds.min.z,boat.bounds.center.z,boat.bounds.max.z}){float bed=ActualWaterfrontTerrainBed(world,new Vector3(x,0,z));minimum=Mathf.Min(minimum,boat.keel-bed);boat.depthSamples++;}boat.minimumKeelClearance=minimum;boat.actualBedContactsPassed=boat.depthSamples==9&&minimum>=proof.requiredKeelClearance;if(!boat.actualBedContactsPassed)throw new InvalidOperationException("Actual unchanged native bed cannot float source boat with required keel clearance: "+boat.instance);
                }
                foreach(var part in group.parts.Where(p=>p.source.EndsWith("/TB_Bd_Fishing.prefab",StringComparison.Ordinal)||p.source.EndsWith("/TB_Bd_Cargo_B.prefab",StringComparison.Ordinal)))
                {var structure=frame.GetComponentsInChildren<Transform>(true).Single(t=>t.position==part.composedRootPosition&&PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)&&PrefabUtility.GetCorrespondingObjectFromSourceAtPath(t.gameObject,part.source)!=null);var stations=ActualLowestStructuralStations(structure);foreach(var station in stations)InstallWaterfrontPier(world,root,structure.GetComponentsInChildren<Collider>(true),station,folder,added,piers,bearings,configureMaterials);proof.supportedStructures++;}
                var cargo=group.parts.Single(p=>p.source.EndsWith("/TB_Bd_Cargo_B.prefab",StringComparison.Ordinal));var pad=group.parts.Single(p=>p.source.EndsWith("/TB_Prop_WoodenPad.prefab",StringComparison.Ordinal));var padRoot=frame.GetComponentsInChildren<Transform>(true).Single(t=>t.position==pad.composedRootPosition&&PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)&&PrefabUtility.GetCorrespondingObjectFromSourceAtPath(t.gameObject,pad.source)!=null);var padColliders=padRoot.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger).ToArray();var end=ActualWalkableWaterfrontPad(padRoot,pad.primaryBounds);end.y-=.015f;
                float xEast=cargo.primaryBounds.max.x+2.2f;var land=new Vector3(xEast,0,cargo.primaryBounds.max.z+6);land.y=ActualWaterfrontTerrainBed(world,land)+.06f;var corner=new Vector3(xEast,cargo.primaryBounds.min.y+.12f,cargo.primaryBounds.center.z);var route=new[]{land,corner,end};
                for(int segment=1;segment<route.Length;segment++)
                {var a=route[segment-1];var b=route[segment];var forward=(b-a).normalized;var right=Vector3.Cross(Vector3.up,forward).normalized;var up=Vector3.Cross(forward,right).normalized;int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/3));for(int i=0;i<count;i++){var point=Vector3.Lerp(a,b,(i+.5f)/count);var plank=FitOwnedBeam(root,point-up*.09f,forward,3,Vector3.Distance(a,b)/count*1.02f,.18f,"Owned fishing gangway plank "+proof.gangwayPlanks,folder,up,configureMaterials);added.Add(DescribeHarborPart(plank,DragonKingdomWorldAuthoring.Art+"Synty Studios/PolygonKnights/Models/SM_Prop_Beam_01.fbx"));proof.gangwayPlanks++;Physics.SyncTransforms();var contact=new HarborDeckContact{requested=point};foreach(var collider in plank.GetComponentsInChildren<Collider>(true))if(collider.Raycast(new Ray(point+Vector3.up,Vector3.down),out var observed,2)&&observed.normal.y>.5f&&Mathf.Abs(observed.point.y-point.y)<.04f){contact.actual=observed.point;contact.collider=PathOf(collider.transform,world.transform);contact.passed=true;break;}walk.Add(contact);if(!contact.passed)throw new InvalidOperationException("Actual source timber gangway has no native walkable surface.");if(i%2==0){InstallWaterfrontPier(world,root,plank.GetComponentsInChildren<Collider>(true),point,folder,added,piers,bearings,configureMaterials);proof.gangwayPierStations++;}}}
                Physics.SyncTransforms();var padJoint=new HarborDeckContact{requested=end};var sourceJointHits=new List<RaycastHit>();foreach(var collider in padColliders)if(collider.Raycast(new Ray(end+Vector3.up,Vector3.down),out var hit,2)&&hit.normal.y>.5f)sourceJointHits.Add(hit);var gangwayJointHits=new List<RaycastHit>();foreach(var collider in root.Cast<Transform>().Where(t=>t.name.StartsWith("Owned fishing gangway plank ",StringComparison.Ordinal)).SelectMany(t=>t.GetComponentsInChildren<Collider>(true)))if(collider.Raycast(new Ray(end+Vector3.up,Vector3.down),out var hit,2)&&hit.normal.y>.5f)gangwayJointHits.Add(hit);if(sourceJointHits.Count>0&&gangwayJointHits.Count>0){var nativePad=sourceJointHits.OrderByDescending(h=>h.point.y).First();var nativePlank=gangwayJointHits.OrderByDescending(h=>h.point.y).First();float step=nativePad.point.y-nativePlank.point.y;padJoint.actual=nativePad.point;padJoint.collider=PathOf(nativePad.collider.transform,world.transform);padJoint.passed=step>=.005f&&step<=.04f;}walk.Add(padJoint);if(!padJoint.passed)throw new InvalidOperationException("Actual gangway-to-original-pad joint lacks native overlapping walkable contacts and a non-coplanar safe step.");
                var boats=group.parts.Where(p=>p.source.EndsWith("/TB_Prop_Ship.prefab",StringComparison.Ordinal)).ToArray();foreach(var part in added)if(boats.Any(b=>b.primaryBounds.Intersects(part.bounds)))throw new InvalidOperationException("New dock support/gangway intersects an original source boat envelope.");if(!FinishBoundsClearOfRoads(world,BoundsOf(root))||!ClearRoadEnvelope(world,root,false))throw new InvalidOperationException("Actual waterfront support blocks an original road/start/objective corridor.");
                var ferry=world.extractionSites.Single(site=>site.vessel!=null).vessel;var own=new HashSet<Collider>(root.GetComponentsInChildren<Collider>(true));proof.ferryDepartureClear=true;for(int i=0;i<=52;i++){var center=Vector3.Lerp(ferry.transform.position,ferry.departureWaypoints.Last().position,i/52f);if(Physics.OverlapBox(center+Vector3.up*8,new Vector3(12,12,26),ferry.transform.rotation,~0,QueryTriggerInteraction.Ignore).Any(own.Contains))proof.ferryDepartureClear=false;proof.ferryDepartureQueries++;}if(!proof.ferryDepartureClear||proof.ferryDepartureQueries!=53)throw new InvalidOperationException("Actual source fishing dock obstructs the original ferry departure sweep.");
                proof.addedParts=added.ToArray();proof.pierContacts=piers.ToArray();proof.sourceBearings=bearings.ToArray();proof.gangwayContacts=walk.ToArray();proof.sourceGeometryPreserved=originalJson==JsonUtility.ToJson(new RidgeGeometryContainer{bindings=RidgeGeometryBindings(frame,new Dictionary<string,string>())});proof.originalRoadsAndFerryPreserved=graph==JsonUtility.ToJson(world.definition.graph)&&critical.All(p=>p.Key!=null&&Pose(p.Key)==p.Value);proof.sourceAndTerrainBytesPreserved=watched.All(p=>AssetSha(p.Key)==p.Value);proof.actualBedAndPhysicalSupportsPassed=proof.sourceAndTerrainBytesPreserved&&proof.ferryDepartureClear&&proof.sourceGeometryPreserved&&proof.originalRoadsAndFerryPreserved&&proof.supportedStructures==3&&proof.boats.All(b=>b.actualBedContactsPassed)&&walk.Count>0&&walk.All(c=>c.passed)&&piers.All(c=>c.baseContact&&c.deckContact)&&bearings.All(c=>c.passed);if(!proof.actualBedAndPhysicalSupportsPassed)throw new InvalidOperationException("Complete original fishing waterfront physical qualification rejected.");proof.status="ACTUAL_SOURCE_HULL_WATERLINE_BED_PILE_GANGWAY_AND_ORIGINAL_POSES_VERIFIED_REQUIRES_RENDER_REVIEW";return proof;
            }
            catch(Exception e){proof.error=e.ToString();proof.status="REJECTED_NATIVE_FISHING_WATERFRONT_PHYSICAL_SUPPORT";if(root!=null)UnityEngine.Object.DestroyImmediate(root.gameObject);throw;}
            finally{proof.addedParts=added.ToArray();proof.pierContacts=piers.ToArray();proof.sourceBearings=bearings.ToArray();proof.gangwayContacts=walk.ToArray();proof.sourceAndTerrainBytesPreserved=watched.All(p=>AssetSha(p.Key)==p.Value);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"finished-fishing-waterfront-actual-physical-support.json"),JsonUtility.ToJson(proof,true));if(!proof.sourceAndTerrainBytesPreserved)throw new InvalidOperationException("Actual waterfront support changed protected source or coordinated terrain bytes.");}
        }
        static float ActualWaterfrontTerrainBed(DragonKingdomWorld world,Vector3 point)=>ActualWaterfrontTerrainBed(world,point,out _);
        static float ActualWaterfrontTerrainBed(DragonKingdomWorld world,Vector3 point,out TerrainCollider actualCollider)
        {
            float height=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,point);actualCollider=null;foreach(var terrain in world.GetComponentsInChildren<Terrain>(true)){var collider=terrain.GetComponent<TerrainCollider>();if(collider!=null&&collider.Raycast(new Ray(new Vector3(point.x,height+2,point.z),Vector3.down),out var hit,4)&&Mathf.Abs(hit.point.y-height)<=.03f){actualCollider=collider;break;}}if(actualCollider==null)throw new InvalidOperationException("Actual matching native TerrainCollider bed contact required, not just a heightfield number.");return height;
        }
        static Vector3[] ActualLowestStructuralStations(Transform structure)
        {
            var bounds=ArchitecturePrimaryEnvelope(structure,out _,out _);var clusters=new List<List<Vector3>>();foreach(var filter in RidgePrimaryMeshFilters(structure))foreach(var vertex in filter.sharedMesh.vertices){var p=filter.transform.TransformPoint(vertex);if(p.y>bounds.min.y+.04f)continue;var cluster=clusters.FirstOrDefault(c=>Vector2.Distance(new Vector2(c[0].x,c[0].z),new Vector2(p.x,p.z))<.5f);if(cluster==null){cluster=new List<Vector3>();clusters.Add(cluster);}cluster.Add(p);}var points=clusters.Select(c=>c.Aggregate(Vector3.zero,(a,b)=>a+b)/c.Count).ToArray();if(points.Length<3)throw new InvalidOperationException("Actual source structure needs at least three separate lower structural bearing stations.");return points;
        }
        [Serializable]public sealed class WaterfrontBearingDiagnostic
        {public string structure;public Vector3 station,resolvedStation,resolvedUnderside,resolvedNormal;public float bed;public WaterfrontBearingRay[] colliders;public bool existingUndersideRayPassed,boundedNativeBearingPassed;}
        [Serializable]public sealed class WaterfrontBearingRay
        {public string path,type;public Bounds bounds;public Vector3 upPoint,upNormal,downPoint,downNormal;public bool upHit,downHit;}
        static WaterfrontBearingDiagnostic[] MeasureFinishedWaterfrontBearings(DragonKingdomWorld world,FinishedArchitectureComposition composition)
        {
            var group=composition.groups.Single(g=>g.key=="fishing-warehouse");var frame=world.Region("harbor").root.Cast<Transform>().Single(t=>t.name==group.name);var result=new List<WaterfrontBearingDiagnostic>();Physics.SyncTransforms();
            foreach(var part in group.parts.Where(p=>p.source.EndsWith("/TB_Bd_Fishing.prefab",StringComparison.Ordinal)||p.source.EndsWith("/TB_Bd_Cargo_B.prefab",StringComparison.Ordinal)))
            {
                var structure=frame.GetComponentsInChildren<Transform>(true).Single(t=>t.position==part.composedRootPosition&&PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)&&PrefabUtility.GetCorrespondingObjectFromSourceAtPath(t.gameObject,part.source)!=null);
                foreach(var station in ActualLowestStructuralStations(structure))
                {
                    float bed=ActualWaterfrontTerrainBed(world,station);var row=new WaterfrontBearingDiagnostic{structure=part.source,station=station,bed=bed};var queries=new List<WaterfrontBearingRay>();
                    foreach(var collider in structure.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger))
                    {
                        var query=new WaterfrontBearingRay{path=PathOf(collider.transform,world.transform),type=collider.GetType().Name,bounds=collider.bounds};
                        query.upHit=collider.Raycast(new Ray(new Vector3(station.x,bed-1,station.z),Vector3.up),out var up,Mathf.Max(4,station.y-bed+8));if(query.upHit){query.upPoint=up.point;query.upNormal=up.normal;}
                        query.downHit=collider.Raycast(new Ray(station+Vector3.up*30,Vector3.down),out var down,Mathf.Max(40,station.y-bed+40));if(query.downHit){query.downPoint=down.point;query.downNormal=down.normal;}
                        queries.Add(query);
                    }
                    row.colliders=queries.ToArray();row.existingUndersideRayPassed=queries.Any(q=>q.upHit&&q.upNormal.y<-.35f);row.boundedNativeBearingPassed=TryWaterfrontNativeBearing(structure.GetComponentsInChildren<Collider>(true),station,bed,out var resolved,out var contact);row.resolvedStation=resolved;if(row.boundedNativeBearingPassed){row.resolvedUnderside=contact.point;row.resolvedNormal=contact.normal;}result.Add(row);
                }
            }
            return result.ToArray();
        }
        static Vector3 ActualWalkableWaterfrontPad(Transform pad,Bounds bounds)
        {
            var colliders=pad.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger).ToArray();var desired=new Vector3(bounds.center.x,bounds.max.y+1,bounds.max.z-.05f);
            bool Query(Vector3 sample,bool exactSurface,out Vector3 point)
            {
                point=default;bool found=false;foreach(var collider in colliders)if(collider.Raycast(new Ray(new Vector3(sample.x,bounds.max.y+1,sample.z),Vector3.down),out var hit,bounds.size.y+2)&&hit.normal.y>.5f&&(!exactSurface||Mathf.Abs(hit.point.y-sample.y)<.05f)&&(!found||hit.point.y>point.y)){point=hit.point;found=true;}return found;
            }
            if(Query(desired,false,out var original))return original;
            var samples=new List<Vector3>();foreach(var filter in RidgePrimaryMeshFilters(pad))
            {
                var vertices=filter.sharedMesh.vertices;var triangles=filter.sharedMesh.triangles;for(int i=0;i<triangles.Length;i+=3)
                {
                    var a=filter.transform.TransformPoint(vertices[triangles[i]]);var b=filter.transform.TransformPoint(vertices[triangles[i+1]]);var c=filter.transform.TransformPoint(vertices[triangles[i+2]]);var normal=Vector3.Cross(b-a,c-a).normalized;if(normal.y>.5f)samples.Add((a+b+c)/3);
                }
            }
            foreach(var sample in samples.OrderBy(p=>(new Vector2(p.x-desired.x,p.z-desired.z)).sqrMagnitude))if(Query(sample,true,out var actual))return actual;
            throw new InvalidOperationException("Actual source wooden-pad mesh has no native matching walkable surface for the gangway joint.");
        }
        // Lowest authored vertex-cluster centroids can fall just outside an
        // open source mesh at its edge. Keep the source untouched, and keep
        // the original station inside the .6m pile footprint: at most .25m
        // horizontal adjustment, always backed by an actual underside ray.
        static bool TryWaterfrontNativeBearing(Collider[] source,Vector3 station,float bed,out Vector3 resolved,out RaycastHit contact)
        {
            var colliders=source.Where(c=>c!=null&&c.enabled&&!c.isTrigger).ToArray();resolved=station;contact=default;
            bool Query(Vector3 point,out RaycastHit best)
            {
                best=default;bool found=false;foreach(var collider in colliders)if(collider.Raycast(new Ray(new Vector3(point.x,bed-1,point.z),Vector3.up),out var hit,Mathf.Max(4,station.y-bed+8))&&hit.normal.y<-.35f&&(!found||hit.point.y<best.point.y)){best=hit;found=true;}return found;
            }
            if(Query(station,out contact))return true;
            foreach(float radius in new[]{.025f,.05f,.1f,.15f,.2f,.25f})for(int i=0;i<32;i++)
            {
                float angle=i*Mathf.PI*2/32;var point=station+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;if(!Query(point,out var hit))continue;resolved=point;contact=hit;return true;
            }
            return false;
        }
        static void InstallWaterfrontPier(DragonKingdomWorld world,Transform parent,Collider[] source,Vector3 station,string folder,List<HarborPart> parts,List<HarborPierContact> piers,List<HarborSourceBearingContact> bearings,bool configureMaterials=true)
        {
            float bed=ActualWaterfrontTerrainBed(world,station,out var terrainCollider);if(!TryWaterfrontNativeBearing(source,station,bed,out var resolved,out var underside))throw new InvalidOperationException("Actual source dock/gangway underside required within the bounded pile footprint at measured bearing station.");station=resolved;bed=ActualWaterfrontTerrainBed(world,station,out terrainCollider);if(underside.point.y<=bed+.02f){if(bed-underside.point.y>.6f)throw new InvalidOperationException("Source dock base is excessively buried.");bearings.Add(new HarborSourceBearingContact{position=station,nativeBed=bed,sourceUnderside=underside.point.y,sourceCollider=PathOf(underside.collider.transform,world.transform),passed=true});return;}
            float bottom=bed-.12f,top=underside.point.y+.015f;var beam=FitOwnedBeam(parent,new Vector3(station.x,(bottom+top)*.5f,station.z),Vector3.up,.6f,top-bottom,.6f,"Owned grounded fishing pier "+piers.Count,folder,Vector3.forward,configureMaterials);Physics.SyncTransforms();var bounds=BoundsOf(beam);bool baseContact=bounds.min.y<=bed+.02f&&bed-bounds.min.y<=.2f;bool joint=NativeBearingJoint(beam,underside.collider.transform,new Vector3(station.x,top,station.z));piers.Add(new HarborPierContact{position=station,nativeBed=bed,postBottom=bounds.min.y,postTop=bounds.max.y,deckUnderside=underside.point.y,baseCollider=PathOf(terrainCollider.transform,world.transform),baseContact=baseContact,deckContact=joint});parts.Add(DescribeHarborPart(beam,DragonKingdomWorldAuthoring.Art+"Synty Studios/PolygonKnights/Models/SM_Prop_Beam_01.fbx"));if(!baseContact||!joint)throw new InvalidOperationException("Real owned fishing pier failed native bed/source support joint.");
        }
    }
}
#endif
