#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Digger.Modules.Core.Sources;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomDiggerCavernAuthoring
    {
        [Serializable]public sealed class CavernJoinSite{public Vector3 boundary,inward,authoredForward;public float roadWidth;public string[] edges;}
        // Reuse this author's actual lobe centers and the current graph. Shared
        // east roads produce one doorway, not three overlapping decorations.
        public static CavernJoinSite[] DescribeCurrentCavernJoinSites(DragonKingdomWorld world)
        {
            if(world==null||world.definition?.graph?.edges==null)throw new InvalidOperationException("Actual current graph required for cavern joins.");var sites=new List<CavernJoinSite>();
            bool Inside(Vector3 p)=>RoundedCenters.Any(c=>(new Vector2(p.x-c.x,p.z-c.z)).sqrMagnitude<65*65);
            foreach(var edge in world.definition.graph.edges)for(int i=1;i<edge.samples.Count;i++)
            {
                Vector3 a=edge.samples[i-1].position,b=edge.samples[i].position,d=b-a;float aa=d.x*d.x+d.z*d.z;if(aa<.000001f)continue;
                foreach(var center in RoundedCenters)
                {
                    float ax=a.x-center.x,az=a.z-center.z,bb=2*(ax*d.x+az*d.z),cc=ax*ax+az*az-65*65,disc=bb*bb-4*aa*cc;if(disc<=0)continue;
                    foreach(float t in new[]{(-bb-Mathf.Sqrt(disc))/(2*aa),(-bb+Mathf.Sqrt(disc))/(2*aa)})
                    {
                        if(t<0||t>1)continue;var point=Vector3.Lerp(a,b,t);var step=d.normalized*.05f;if(Inside(point-step)==Inside(point+step))continue;var inward=Inside(point+step)?d.normalized:-d.normalized;inward.y=0;inward.Normalize();
                        if(point.y<95||point.y>110)throw new InvalidOperationException("Current chamber join no longer has its original low road elevation.");var prior=sites.FirstOrDefault(s=>Vector3.Distance(s.boundary,point)<.25f);
                        if(prior==null)sites.Add(new CavernJoinSite{boundary=point,inward=inward,authoredForward=Vector3.Slerp(edge.samples[i-1].forward,edge.samples[i].forward,t).normalized,roadWidth=Mathf.Max(edge.samples[i-1].roadWidthMeters,edge.samples[i].roadWidthMeters),edges=new[]{edge.edgeId}});else{if(Vector3.Dot(prior.inward,inward)<.99f)throw new InvalidOperationException("Conflicting actual road directions at shared chamber join.");prior.roadWidth=Mathf.Max(prior.roadWidth,edge.samples[i].roadWidthMeters);prior.edges=prior.edges.Concat(new[]{edge.edgeId}).Distinct().OrderBy(s=>s,StringComparer.Ordinal).ToArray();}
                    }
                }
            }
            if(sites.Count!=2)throw new InvalidOperationException("The current two-lobe chamber requires exactly its two distinct real corridor joins; no invented placement fallback.");return sites.OrderBy(s=>s.boundary.x).ToArray();
        }
    }
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        // Public Unity registration owns virtual Packages paths. Project Io
        // owns Windows long paths. Both art authoring and its fixture share this
        // one narrow source-file adapter; no PackageCache probing or file drop.
        static string CavernJoinPhysicalSource(string path)
        {
            string physical=path;
            if(path.StartsWith("Packages/",StringComparison.Ordinal))
            {
                string name=path.Split('/')[1];var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/"+name);
                if(package==null||package.name!=name||package.assetPath!="Packages/"+name||string.IsNullOrEmpty(package.resolvedPath))throw new InvalidOperationException("Exact registered public package source required: "+path);
                string root=Path.GetFullPath(package.resolvedPath);physical=Path.GetFullPath(Path.Combine(root,path.Substring(package.assetPath.Length).TrimStart('/')));
                if(!physical.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Package source escapes its exact registered physical root: "+path);
            }
            return Io(physical);
        }
        const float CavernJoinClearanceHalfDepth=16,CavernJoinMaximumRockSize=8.3f,CavernJoinEmbeddingFraction=.16f;
        static float JoinCollarMinimumInset()
        {
            // Every fitted renderer AABB is at most8.3m on each axis. Include
            // its largest possible half-diagonal embedding plus .25m separation
            // from the UNCHANGED protected doorway box; never shrink that box.
            float half=CavernJoinMaximumRockSize*.5f;
            return Mathf.Ceil(CavernJoinClearanceHalfDepth+half+Mathf.Sqrt(3)*half*CavernJoinEmbeddingFraction+.25f);
        }
        [Serializable]public sealed class CavernJoinPlacementAttempt
        {
            public string name,source,nativeMesh,nativeMeshSha256;public Vector3 contact,normal;public Bounds bounds;
            public bool nativeContactContained,originalBoxesClear,roadObjectiveClear,guardianClear,passed;
            public CavernJoinNormalFit normalFit;public string[] obstructedBoxes;public AuthoredClearanceWitness roadObjectiveBlocker;public int primaryNativeMeshCrossings,adjacentPrimaryMeshCrossings;public bool adjacentMeshRequired;public float actualVertexEmbeddingQuantile,uniformSourceSizeFraction;
        }
        [Serializable]public sealed class CavernJoinRock
        {public CavernJoinNormalFit normalFit;public string source,sourceGuid,sourceSha256,name,join;public int primaryNativeMeshCrossings,adjacentPrimaryMeshCrossings;public bool adjacentMeshRequired;public float actualVertexEmbeddingQuantile,uniformSourceSizeFraction;public Vector3 contact,normal,position,scale;public Quaternion rotation;public Bounds bounds;public bool nativeRockContact,originalClearancePreserved,guardianClear;public RidgeGeometryBinding[] geometry;}
        [Serializable]public sealed class CavernJoinFinishEvidence
        {
            public string scope="SOURCE_ROCK_COLLARS_ON_ACTUAL_NATIVE_JOIN_SURFACES_NOT_VOXEL_REPAIR_OR_FINISHED_WORLD_ACCEPTANCE",status="IN_PROGRESS",error;
            public DragonKingdomDiggerCavernAuthoring.CavernJoinSite[] sites;public CavernJoinRock[] rocks;public float chamberwardInset;public List<CavernJoinPlacementAttempt> attempts=new List<CavernJoinPlacementAttempt>();
            public string profile="ACTUAL_JOIN_DENSE_PRIMARY_MESH_EMBEDDING04_MEASURED_CORNER_SIZE_AND_NORMAL_CLEARANCE";public Bounds recordedFlatFloorFootprint;public string floorReceipt,floorReceiptSha256,roadReceipt,roadReceiptSha256;public List<CavernJoinFloorMeasurement> floorMeasurements=new List<CavernJoinFloorMeasurement>();public int expectedPieces;public float maximumContactGap;public bool everyPrimaryMeshActuallyIntersectsNative,everyBandActuallyConnected;
            public bool originalTransformsMeshesPhysicsTerrainAndGraphExact,originalSourceBytesExact,allOriginalClearancesPreserved;public string sourceOccupancy,liveOccupancy;
        }
        static Bounds JoinLocalBounds(Bounds world,Vector3 origin,Quaternion rotation)
        {var inverse=Quaternion.Inverse(rotation);var points=new List<Vector3>();foreach(float x in new[]{world.min.x,world.max.x})foreach(float y in new[]{world.min.y,world.max.y})foreach(float z in new[]{world.min.z,world.max.z})points.Add(inverse*(new Vector3(x,y,z)-origin));var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points.Skip(1))bounds.Encapsulate(p);return bounds;}
        static bool JoinOriginalClearancePreserved(Bounds bounds,DragonKingdomDiggerCavernAuthoring.CavernJoinSite site)
        {
            var rotation=Quaternion.LookRotation(site.inward,Vector3.up);var local=JoinLocalBounds(bounds,site.boundary,rotation);var clearance=new Bounds(new Vector3(0,27.5f,0),new Vector3(site.roadWidth+14,55.1f,CavernJoinClearanceHalfDepth*2));return !local.Intersects(clearance);
        }
        static RaycastHit RequireJoinNativeHit(MeshCollider[] native,Ray ray,float distance)
        {
            var hits=new List<RaycastHit>();foreach(var collider in native)if(collider.Raycast(ray,out var hit,distance))hits.Add(hit);if(hits.Count==0)throw new InvalidOperationException("A real native Digger surface is required for every join rock; no floating fallback.");var nearest=hits.OrderBy(h=>h.distance).First();if(nearest.distance<1||!float.IsFinite(nearest.distance)||nearest.normal.sqrMagnitude<.9f)throw new InvalidOperationException("Actual join surface is degenerate or the measured ray starts in solid rock.");return nearest;
        }
        [Serializable]public sealed class CavernJoinFloorMeasurement
        {
            public int join;public Vector3 expected;public string region,status="IN_PROGRESS",error;
            public bool insideAuthoredFlatFloor,nativeFloorHit,passed;public CavernAuthorityHit nativeFloor;public CavernAuthorityHit[] rawNativeFloorHits;
            public string[] matchedQualifiedRoads;public CavernAuthoredRoadContact authoredPavement;
        }
        static string JoinRequiredArgument(string flag)
        {var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,flag);if(at<0||at+1>=args.Length||string.IsNullOrEmpty(args[at+1]))throw new InvalidOperationException("Explicit measured source authority required: "+flag);return args[at+1];}
        // Region selection only. The signed native floor operation remains the
        // sole105m authority; this adapter neither derives nor expands its box.
        static bool JoinInsideRecordedFlatFloor(Bounds footprint,Vector3 point)=>InsideXZ(footprint,point);
        static bool JoinRecordedFlatFloorPasses(float floorY,float normalY,float recordedY)
            =>float.IsFinite(floorY)&&float.IsFinite(normalY)&&recordedY==105&&normalY>.5f&&Mathf.Abs(floorY-recordedY)<=.35f;
        static RaycastHit[] JoinComponentHits(Collider[] colliders,Ray ray,float length)
        {var hits=new List<RaycastHit>();foreach(var c in colliders)if(c.Raycast(ray,out var hit,length))hits.Add(hit);return hits.OrderBy(h=>h.distance).ToArray();}
        static void MeasureAllJoinFloorContracts(DragonKingdomWorld world,MeshCollider[] native,CavernJoinFinishEvidence proof)
        {
            if(world.gameObject.scene.GetPhysicsScene()!=Physics.defaultPhysicsScene||EditorSceneManager.IsPreviewScene(world.gameObject.scene))throw new InvalidOperationException("Existing authored-pavement PhysX authority requires an isolated registered scene, not a prefab preview scene.");
            string receipt=JoinRequiredArgument("-tsDragonFinalOriginalRoofReceipt"),tests=JoinRequiredArgument("-tsDragonFinalOriginalRoofTests");
            var token=LoadQualifiedCavernRoadAuthority("CavernRoadColliderRoof01",receipt,tests);var qualified=JsonUtility.FromJson<CavernAuthoredRoadEvidence>(File.ReadAllText(Io(receipt)));var raw=JsonUtility.FromJson<CavernPhysicalAuthorityEvidence>(File.ReadAllText(Io(qualified.rawReceipt)));
            string floorPath=Path.Combine(raw.passedSourceEvidence,"rendered-evidence","actual-digger-cavern-CavernRoundedProbe17.json");if(!File.Exists(Io(floorPath))||!CavernRoadSha256Equal(AssetSha(floorPath),raw.geometryReceiptSha256))throw new InvalidOperationException("Exact source-qualified native Digger floor receipt required; no inferred footprint.");
            var original=JsonUtility.FromJson<DragonKingdomDiggerCavernAuthoring.Evidence>(File.ReadAllText(Io(floorPath)));var footprint=original.floorRestorationBounds;
            if(original.floorRestorationKind!="AUTHORED_SIGNED_PLANE_INTERSECTION"||original.authoredFloor==null||!original.authoredFloor.originalFloorFootprint.Equals(footprint)||original.authoredFloor.worldFloorHeight!=105||footprint.max.y!=105||!original.authoredFloor.sourceOutsideFootprintUntouchedByJob)throw new InvalidOperationException("Unchanged actual signed105m floor footprint required.");
            proof.recordedFlatFloorFootprint=footprint;proof.floorReceipt=floorPath;proof.floorReceiptSha256=AssetSha(floorPath);proof.roadReceipt=receipt;proof.roadReceiptSha256=AssetSha(receipt);var hashes=new Dictionary<Mesh,string>();
            var allRoads=world.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy&&c.sharedMesh!=null&&c.GetComponent<MeshFilter>()?.sharedMesh==c.sharedMesh).ToArray();
            Physics.SyncTransforms();
            try
            {
                for(int index=0;index<proof.sites.Length;index++)
                {
                    var site=proof.sites[index];var row=new CavernJoinFloorMeasurement{join=index,expected=site.boundary,insideAuthoredFlatFloor=JoinInsideRecordedFlatFloor(footprint,site.boundary)};proof.floorMeasurements.Add(row);row.region=row.insideAuthoredFlatFloor?"EXACT_RECORDED_SIGNED_105M_FLAT_FLOOR":"ORIGINAL_AUTHORED_PORTAL_PAVEMENT_AND_GENUINE_LOWER_DIGGER_FLOOR";
                    try
                    {
                        // Match this portal's actual qualified road mesh identity,
                        // transform and CPU geometry; other absent road witnesses
                        // confer no blanket authority on the saved art clone.
                        var records=qualified.roads.Where(r=>site.edges.Any(e=>r.owner.StartsWith("Kingdom "+e+" ·",StringComparison.Ordinal))).ToArray();var roads=new List<Collider>();var matched=new List<string>();
                        foreach(var record in records)foreach(var c in allRoads.Where(c=>AssetDatabase.GetAssetPath(c.sharedMesh)==record.mesh&&c.transform.localToWorldMatrix.Equals(record.localToWorld)))
                        {AssetDatabase.TryGetGUIDAndLocalFileIdentifier(c.sharedMesh,out string guid,out long id);if(guid!=record.guid||id!=record.localId||!CavernRoadSha256Equal(AssetSha(record.mesh),record.meshSha256)||DragonKingdomRoadSurfaceProbe.CavernPhysicalAuthorityMeshPayload(c.sharedMesh)!=record.payload)throw new InvalidOperationException("Matched portal road geometry differs from its qualified native source.");roads.Add(c);matched.Add(record.owner+":"+record.mesh);}
                        row.matchedQualifiedRoads=matched.ToArray();var ray=new Ray(site.boundary+Vector3.up*20,Vector3.down);var floors=JoinComponentHits(native,ray,30);var floor=floors.FirstOrDefault();row.rawNativeFloorHits=floors.Select(h=>CavernAuthorityRay(h,hashes)).ToArray();row.nativeFloorHit=floor.collider!=null;if(row.nativeFloorHit)row.nativeFloor=CavernAuthorityRay(floor,hashes);
                        row.authoredPavement=MeasureCavernAuthoredRoadContact(world.transform,site.boundary,site.authoredForward,roads.Distinct().ToArray(),native,raw.nwhBoxes,hashes);
                        if(!row.insideAuthoredFlatFloor&&row.authoredPavement.qualified){row.nativeFloor=row.authoredPavement.diggerDown.Where(h=>h.point.y<=row.authoredPavement.roadSupport.point.y+.3f).OrderByDescending(h=>h.point.y).FirstOrDefault();row.nativeFloorHit=row.nativeFloor!=null;}
                        row.passed=row.nativeFloorHit&&(row.insideAuthoredFlatFloor?JoinRecordedFlatFloorPasses(floor.point.y,floor.normal.y,original.authoredFloor.worldFloorHeight):row.authoredPavement.qualified);
                        row.status=row.passed?"ACTUAL_REGION_FLOOR_CONTRACT_PASSED":"REJECTED_ACTUAL_REGION_FLOOR_CONTRACT";
                        if(!row.passed)row.error=row.insideAuthoredFlatFloor?"Exact original105±.35m/normal>.5 flat-floor requirement failed.":"Original authored pavement/native-lower-floor/NWH qualification failed: "+string.Join(",",row.authoredPavement.failures);
                    }
                    catch(Exception error){row.error=error.ToString();row.status="REJECTED_FLOOR_SOURCE_OR_NATIVE_QUERY";}
                }
            }
            finally{token.VerifySourcesUnchanged();}
            if(proof.floorMeasurements.Any(r=>!r.passed))throw new InvalidOperationException("Actual join floor qualification rejected; every join's exact native floor, pavement and region is retained: "+string.Join("; ",proof.floorMeasurements.Where(r=>!r.passed).Select(r=>r.join+":"+r.error)));
        }
        const float JoinSourceBoxSeparation=.05f;
        [Serializable]public sealed class CavernJoinNormalFit
        {
            public string profile="MEASURED_NORMAL_ONLY_CLEARANCE_FIT01",error;public float separation=JoinSourceBoxSeparation,normalScale,normalRayEntryBudget;
            public bool boundedNormalRay,normalAligned,zeroNormalPlaneClear,originalTangentSpansExact,boxesClearWithSeparation,passed;public Vector2 tangentSpanBefore,tangentSpanAfter;public Bounds zeroNormalPlaneBounds,actualBounds;public Vector3 scaleBefore,scaleAfter;
        }
        static bool JoinBoxesClearWithSeparation(Bounds bounds,DragonKingdomDiggerCavernAuthoring.CavernJoinSite[] sites)
        {bounds.Expand(JoinSourceBoxSeparation*2);return sites.All(site=>JoinOriginalClearancePreserved(bounds,site));}
        static float JoinNormalRayBoxBudget(Vector3 contact,Vector3 normal,DragonKingdomDiggerCavernAuthoring.CavernJoinSite[] sites)
        {
            float result=float.PositiveInfinity;foreach(var site in sites){var inverse=Quaternion.Inverse(Quaternion.LookRotation(site.inward,Vector3.up));var point=inverse*(contact-site.boundary);var box=new Bounds(new Vector3(0,27.5f,0),new Vector3(site.roadWidth+14,55.1f,CavernJoinClearanceHalfDepth*2));if(box.Contains(point))return 0;if(box.IntersectRay(new Ray(point,inverse*normal),out float entry)&&entry>=0)result=Mathf.Min(result,entry);}return result;
        }
        static void OrientJoinSourceRock(Transform source,Vector3 contact,Vector3 normal,Vector3 tangent,int index,float targetSize)
        {
            // Roll varies the silhouette without tilting its normal-thickness
            // axis into the protected throat. Tangential size stays authored.
            source.rotation=Quaternion.LookRotation(-normal,tangent)*Quaternion.AngleAxis(index%3*7-7,Vector3.forward);source.localScale=Vector3.one;var raw=BoundsOf(source);float longest=Mathf.Max(raw.size.x,raw.size.y,raw.size.z);if(longest<=0)throw new InvalidOperationException("Actual source rock bounds required.");source.localScale=Vector3.one*(targetSize/longest);source.position+=contact-BoundsOf(source).center;
        }
        static CavernJoinNormalFit FitJoinNormalThickness(Transform source,Vector3 contact,Vector3 normal,DragonKingdomDiggerCavernAuthoring.CavernJoinSite[] sites,float quantile)
        {
            var proof=new CavernJoinNormalFit();if(source==null||sites==null||sites.Length==0||!ArchitectureFinite(contact)||!ArchitectureFinite(normal)||normal.sqrMagnitude<.99f||normal.sqrMagnitude>1.01f)throw new InvalidOperationException("Actual source, unit native normal and unchanged original boxes required.");
            var normalAxis=source.localToWorldMatrix.MultiplyVector(Vector3.forward).normalized;proof.normalAligned=Vector3.Dot(normalAxis,-normal)>.99999f;if(!proof.normalAligned)throw new InvalidOperationException("Source thickness axis must align with the measured native normal; no hidden tangential intrusion.");
            var scale=source.localScale;proof.scaleBefore=scale;var centered=source.position;float pivot=Vector3.Dot(centered-contact,normal);var vertices=JoinPrimaryVertices(source);float offset=JoinEmbeddingOffset(vertices.Select(v=>Vector3.Dot(v-contact,normal)).ToArray(),quantile);var tx=source.right.normalized;var ty=source.up.normalized;
            Vector2 Span(Vector3[] points)=>new Vector2(points.Max(v=>Vector3.Dot(v,tx))-points.Min(v=>Vector3.Dot(v,tx)),points.Max(v=>Vector3.Dot(v,ty))-points.Min(v=>Vector3.Dot(v,ty)));proof.tangentSpanBefore=Span(vertices);
            var projected=new List<Vector3>();foreach(var renderer in source.GetComponentsInChildren<MeshRenderer>(true))foreach(float x in new[]{renderer.localBounds.min.x,renderer.localBounds.max.x})foreach(float y in new[]{renderer.localBounds.min.y,renderer.localBounds.max.y})foreach(float z in new[]{renderer.localBounds.min.z,renderer.localBounds.max.z}){var point=renderer.transform.TransformPoint(new Vector3(x,y,z));projected.Add(point-normal*Vector3.Dot(point-contact,normal));}
            if(projected.Count==0)throw new InvalidOperationException("Actual source renderer bounds required.");var zero=new Bounds(projected[0],Vector3.zero);foreach(var point in projected.Skip(1))zero.Encapsulate(point);proof.zeroNormalPlaneBounds=zero;proof.zeroNormalPlaneClear=JoinBoxesClearWithSeparation(zero,sites);float budget=JoinNormalRayBoxBudget(contact,normal,sites);proof.boundedNormalRay=float.IsFinite(budget);proof.normalRayEntryBudget=proof.boundedNormalRay?budget:0;
            void Apply(float fraction){source.localScale=new Vector3(scale.x,scale.y,scale.z*fraction);source.position=centered+normal*((fraction-1)*pivot-offset*fraction);}
            if(!proof.zeroNormalPlaneClear){proof.error="Even the zero-thickness projected tangent envelope intersects the unchanged protected box; normal-only fitting is impossible.";return proof;}
            Apply(1);float selected=1;if(!JoinBoxesClearWithSeparation(BoundsOf(source),sites))
            {
                float low=0,high=1;for(int iteration=0;iteration<25;iteration++){float mid=(low+high)*.5f;Apply(mid);if(JoinBoxesClearWithSeparation(BoundsOf(source),sites))low=mid;else high=mid;}selected=low;
            }
            if(selected<=0){source.localScale=scale;source.position=centered;proof.error="No finite positive normal thickness fits; no collapsed source geometry may pass.";return proof;}
            Apply(selected);proof.normalScale=selected;proof.scaleAfter=source.localScale;proof.actualBounds=BoundsOf(source);proof.tangentSpanAfter=Span(JoinPrimaryVertices(source));proof.originalTangentSpansExact=source.localScale.x==scale.x&&source.localScale.y==scale.y&&Vector2.Distance(proof.tangentSpanBefore,proof.tangentSpanAfter)<=.0002f;proof.boxesClearWithSeparation=JoinBoxesClearWithSeparation(proof.actualBounds,sites);proof.passed=proof.normalAligned&&proof.zeroNormalPlaneClear&&proof.originalTangentSpansExact&&proof.boxesClearWithSeparation;if(!proof.passed)proof.error="Actual normal fit changed tangential size or failed unchanged original box separation.";return proof;
        }
        // Exact public mesh data, not renderer AABBs, owns visible rock fitting.
        static Vector3[] JoinPrimaryVertices(Transform root)
        {
            var result=new List<Vector3>();foreach(var filter in RidgePrimaryMeshFilters(root))
                using(var data=Mesh.AcquireReadOnlyMeshData(filter.sharedMesh))using(var points=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp))
                {data[0].GetVertices(points);foreach(var point in points)result.Add(filter.transform.TransformPoint(point));}
            if(result.Count<3)throw new InvalidOperationException("Primary visible source vertices required for actual join fitting.");return result.ToArray();
        }
        static float JoinEmbeddingOffset(float[] signed,float quantile)
        {
            if(signed==null||signed.Length<3||signed.Any(v=>!float.IsFinite(v))||quantile<.5f||quantile>.9f)throw new InvalidOperationException("Finite primary mesh projections and explicit bounded embedding required.");
            var sorted=signed.OrderBy(v=>v).ToArray();return sorted[Mathf.FloorToInt((sorted.Length-1)*quantile)];
        }
        // Every witness is a real source TRIANGLE EDGE crossing a real native
        // surface via Collider.Raycast. AABB contact alone cannot pass this.
        static int JoinPrimaryMeshCrossings(Transform root,Collider[] surfaces)
        {
            if(root==null||surfaces==null||surfaces.Length==0)throw new InvalidOperationException("Actual primary mesh and native surfaces required.");
            var witnesses=new List<Vector3>();foreach(var filter in RidgePrimaryMeshFilters(root))
                using(var data=Mesh.AcquireReadOnlyMeshData(filter.sharedMesh))using(var points=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp))
                {
                    data[0].GetVertices(points);for(int sub=0;sub<data[0].subMeshCount;sub++)
                    {
                        var descriptor=data[0].GetSubMesh(sub);if(descriptor.topology!=MeshTopology.Triangles)throw new InvalidOperationException("Source rock must retain its actual triangle topology.");
                        using(var indices=new NativeArray<int>(descriptor.indexCount,Allocator.Temp))
                        {
                            data[0].GetIndices(indices,sub,true);for(int i=0;i+2<indices.Length;i+=3)for(int edge=0;edge<3;edge++)
                            {
                                var a=filter.transform.TransformPoint(points[indices[i+edge]]);var b=filter.transform.TransformPoint(points[indices[i+(edge+1)%3]]);var delta=b-a;float length=delta.magnitude;if(length<.001f)continue;
                                foreach(var surface in surfaces)foreach(bool reverse in new[]{false,true})
                                {
                                    var ray=new Ray(reverse?b:a,reverse?-delta/length:delta/length);
                                    if(surface.Raycast(ray,out var hit,length)&&hit.distance>.001f&&hit.distance<length-.001f&&witnesses.All(p=>Vector3.Distance(p,hit.point)>.1f))
                                    {witnesses.Add(hit.point);if(witnesses.Count>=3)return witnesses.Count;}
                                }
                            }
                        }
                    }
                }
            return witnesses.Count;
        }
        public static CavernJoinFinishEvidence InstallMeasuredCavernJoinFinish(DragonKingdomWorld world,string folder,string sourceOccupancy,string liveOccupancy)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||world==null||EditorUtility.IsPersistent(world)||!world.gameObject.scene.IsValid()||!folder.StartsWith(Root+"VisualCandidates/",StringComparison.Ordinal)||folder.Substring((Root+"VisualCandidates/").Length).Contains('/')||folder.Contains(".."))throw new InvalidOperationException("Stopped isolated native cavern and direct owned candidate folder required; never edit a persistent source prefab.");
            var region=world.Region("caverns");if(region.root.Cast<Transform>().Any(t=>t.name=="Owned measured cavern join collars"))throw new InvalidOperationException("Fresh join art only; never stack duplicated collars.");
            var native=world.GetComponentsInChildren<DiggerSystem>(true).SelectMany(s=>s.GetComponentsInChildren<MeshCollider>(true)).Where(c=>c.sharedMesh!=null&&c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy).Distinct().ToArray();if(native.Length==0)throw new InvalidOperationException("Actual retained native Digger collision required; never replace it with decorative collision.");
            var proof=new CavernJoinFinishEvidence{chamberwardInset=0,sites=DragonKingdomDiggerCavernAuthoring.DescribeCurrentCavernJoinSites(world),sourceOccupancy=sourceOccupancy,liveOccupancy=liveOccupancy};var occupancy=new CavernFinishEvidence();var guardian=RequireRecordedCavernGuardianEnvelope(sourceOccupancy,liveOccupancy,occupancy);guardian.Expand(6);
            var sourcePaths=Enumerable.Range(1,4).Select(i=>DragonKingdomWorldAuthoring.Cliff+"RockFormation_"+i.ToString("00")+".prefab").ToArray();CaptureCavernFinishSourceAudit();var bytes=sourcePaths.SelectMany(p=>AssetDatabase.GetDependencies(p,true)).Concat(new[]{sourceOccupancy,liveOccupancy}).Where(p=>File.Exists(CavernJoinPhysicalSource(p))).SelectMany(p=>File.Exists(CavernJoinPhysicalSource(p+".meta"))?new[]{p,p+".meta"}:new[]{p}).Distinct().ToDictionary(p=>p,p=>AssetSha(CavernJoinPhysicalSource(p)));
            var originalPoses=world.GetComponentsInChildren<Transform>(true).ToDictionary(t=>t,t=>Pose(t)+"/"+t.gameObject.activeSelf);var filters=world.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f,f=>f.sharedMesh);var colliders=world.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));var bodies=world.GetComponentsInChildren<Rigidbody>(true).ToDictionary(b=>b,b=>EditorJsonUtility.ToJson(b));var terrains=world.GetComponentsInChildren<Terrain>(true).ToDictionary(t=>t,t=>FinishFloatHash(t.terrainData.GetHeights(0,0,t.terrainData.heightmapResolution,t.terrainData.heightmapResolution))+"/"+FinishHoleHash(t.terrainData));string graph=JsonUtility.ToJson(world.definition.graph);var parts=new List<CavernJoinRock>();var temporaryPrimaryColliders=new List<Collider>();proof.expectedPieces=proof.sites.Length*(97+12);GameObject group=null;
            try
            {
                MeasureAllJoinFloorContracts(world,native,proof);Directory.CreateDirectory(Io(folder+"/Materials/Finish"));AssetDatabase.Refresh();group=new GameObject("Owned measured cavern join collars");group.transform.SetParent(region.root,false);
                int joinIndex=0;foreach(var site in proof.sites)
                {
                    // Fit to the ACTUAL join plane; V436's22m shifted collar is
                    // preserved as rejected historical evidence, never reused.
                    Vector3 axis=site.boundary;var measuredFloor=proof.floorMeasurements.Single(r=>r.join==joinIndex);float floorY=measuredFloor.nativeFloor.point.y;axis.y=floorY+27.5f;var right=Vector3.Cross(Vector3.up,site.inward).normalized;
                    var rays=new List<Ray>();for(int i=0;i<=96;i++){float angle=Mathf.PI*i/96;rays.Add(new Ray(axis,(right*Mathf.Cos(angle)+Vector3.up*Mathf.Sin(angle)).normalized));}foreach(float height in new[]{24f,20f,16f,12f,8f,4f})foreach(float side in new[]{1f,-1f})rays.Add(new Ray(new Vector3(axis.x,floorY+height,axis.z),right*side));
                    var previousMeshes=new Dictionary<int,Collider[]>();Vector3? previousArcContact=null;int index=0;foreach(var ray in rays)
                    {
                        var hit=RequireJoinNativeHit(native,ray,70);float targetSize=7f+(index%3)*.65f;
                        if(index<97){if(previousArcContact.HasValue){float gap=Vector3.Distance(previousArcContact.Value,hit.point);proof.maximumContactGap=Mathf.Max(proof.maximumContactGap,gap);}previousArcContact=hit.point;}var source=sourcePaths[(index+joinIndex)%sourcePaths.Length];var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source),world.gameObject.scene);go.name="Measured join "+joinIndex+" rock "+index;go.transform.SetParent(group.transform,false);go.SetActive(true);foreach(var collider in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);foreach(var body in go.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
                        var tangent=Vector3.Cross(site.inward,ray.direction).normalized;
                        // A curved native corner may not fit the full-size source's
                        // tangent footprint even at zero thickness. Try a smaller
                        // uniformly scaled instance at the SAME measured contact;
                        // all native/neighbor triangle and protected-box gates stay.
                        int run=index<97?0:(index-97)%2+1;bool neighborRequired=previousMeshes.ContainsKey(run);CavernJoinPlacementAttempt attempt=null;Bounds bounds=default;
                        foreach(float sizeFraction in new[]{1f,.8f,.6f,.4f,.25f,.125f})
                        {
                          OrientJoinSourceRock(go.transform,hit.point,hit.normal,tangent,index,targetSize*sizeFraction);
                          Vector3 centered=go.transform.position,sourceBaseScale=go.transform.localScale;
                          foreach(float quantile in new[]{.65f,.75f,.85f})
                          {
                            go.transform.localScale=sourceBaseScale;go.transform.position=centered;var normalFit=FitJoinNormalThickness(go.transform,hit.point,hit.normal,proof.sites,quantile);bounds=BoundsOf(go.transform);
                            attempt=new CavernJoinPlacementAttempt{name=go.name,source=source,contact=hit.point,normal=hit.normal,bounds=bounds,nativeMesh=AssetDatabase.GetAssetPath(((MeshCollider)hit.collider).sharedMesh),nativeContactContained=bounds.Contains(hit.point),normalFit=normalFit,uniformSourceSizeFraction=sizeFraction,actualVertexEmbeddingQuantile=quantile,adjacentMeshRequired=neighborRequired,obstructedBoxes=proof.sites.Select((s,i)=>new{s,i}).Where(v=>!JoinOriginalClearancePreserved(bounds,v.s)).Select(v=>v.i.ToString()).ToArray(),guardianClear=!guardian.Intersects(bounds)};
                            attempt.nativeMeshSha256=AssetSha(attempt.nativeMesh);attempt.originalBoxesClear=attempt.obstructedBoxes.Length==0;attempt.roadObjectiveClear=FinishBoundsClearOfRoads(world,bounds,w=>attempt.roadObjectiveBlocker=w);
                            if(normalFit.passed&&attempt.originalBoxesClear&&attempt.roadObjectiveClear&&attempt.guardianClear)attempt.primaryNativeMeshCrossings=JoinPrimaryMeshCrossings(go.transform,new Collider[]{hit.collider});
                            if(attempt.primaryNativeMeshCrossings>=3&&neighborRequired)attempt.adjacentPrimaryMeshCrossings=JoinPrimaryMeshCrossings(go.transform,previousMeshes[run]);
                            attempt.passed=normalFit.passed&&attempt.originalBoxesClear&&attempt.roadObjectiveClear&&attempt.guardianClear&&attempt.primaryNativeMeshCrossings>=3&&(!neighborRequired||attempt.adjacentPrimaryMeshCrossings>=1);proof.attempts.Add(attempt);if(attempt.passed)break;
                          }
                          if(attempt!=null&&attempt.passed)break;
                        }
                        if(attempt==null||!attempt.passed)throw new InvalidOperationException("Actual join source-mesh fitting cannot satisfy original native/road/throat/guardian geometry at "+go.name+"; native Digger join geometry may require correction. No shifted collar, skipped rock, disconnected primary meshes, or clearance waiver is permitted.");
                        // Temporary public mesh-collider queries prove actual
                        // primary source geometry overlaps its prior band piece.
                        // They are removed before persistence or any gameplay.
                        var actualPrimary=new List<Collider>();foreach(var filter in RidgePrimaryMeshFilters(go.transform))
                        {var query=filter.gameObject.AddComponent<MeshCollider>();query.hideFlags=HideFlags.DontSave;query.sharedMesh=filter.sharedMesh;actualPrimary.Add(query);temporaryPrimaryColliders.Add(query);}
                        previousMeshes[run]=actualPrimary.ToArray();if(index==0)previousMeshes[1]=actualPrimary.ToArray();if(index==96)previousMeshes[2]=actualPrimary.ToArray();
                        ConfigureFinishMaterials(go.transform,folder);parts.Add(new CavernJoinRock{source=source,sourceGuid=AssetDatabase.AssetPathToGUID(source),sourceSha256=AssetSha(source),name=go.name,join=joinIndex.ToString(),normalFit=attempt.normalFit,primaryNativeMeshCrossings=attempt.primaryNativeMeshCrossings,adjacentPrimaryMeshCrossings=attempt.adjacentPrimaryMeshCrossings,adjacentMeshRequired=neighborRequired,uniformSourceSizeFraction=attempt.uniformSourceSizeFraction,actualVertexEmbeddingQuantile=attempt.actualVertexEmbeddingQuantile,contact=hit.point,normal=hit.normal,position=go.transform.position,rotation=go.transform.rotation,scale=go.transform.localScale,bounds=bounds,nativeRockContact=true,originalClearancePreserved=true,guardianClear=true});index++;
                    }
                    joinIndex++;
                }
                foreach(var temporary in temporaryPrimaryColliders)if(temporary!=null)UnityEngine.Object.DestroyImmediate(temporary);temporaryPrimaryColliders.Clear();foreach(var part in parts){var placed=group.transform.Cast<Transform>().Single(t=>t.name==part.name);part.geometry=RidgeGeometryBindings(placed,new Dictionary<string,string>());}proof.rocks=parts.ToArray();proof.everyBandActuallyConnected=parts.All(p=>!p.adjacentMeshRequired||p.adjacentPrimaryMeshCrossings>=1);proof.everyPrimaryMeshActuallyIntersectsNative=parts.All(p=>p.primaryNativeMeshCrossings>=3);proof.allOriginalClearancesPreserved=parts.Count==proof.expectedPieces&&proof.everyPrimaryMeshActuallyIntersectsNative&&proof.everyBandActuallyConnected&&parts.All(p=>p.nativeRockContact&&p.originalClearancePreserved&&p.guardianClear)&&group.GetComponentsInChildren<Collider>(true).Length==0&&group.GetComponentsInChildren<Rigidbody>(true).Length==0;if(!proof.allOriginalClearancesPreserved)throw new InvalidOperationException("Every planned actual-join source piece must retain three real primary-mesh/native crossings and original clearances; no sparse-count acceptance.");proof.status="ACTUAL_JOIN_PRIMARY_MESH_EMBEDDING_REQUIRES_DIRECT_VISUAL_REVIEW_NOT_WORLD_ACCEPTANCE";return proof;
            }
            catch(Exception error){proof.error=error.ToString();proof.status="REJECTED_MEASURED_CAVERN_JOIN_ART";if(group!=null)UnityEngine.Object.DestroyImmediate(group);throw;}
            finally
            {
                proof.originalTransformsMeshesPhysicsTerrainAndGraphExact=originalPoses.All(p=>p.Key!=null&&Pose(p.Key)+"/"+p.Key.gameObject.activeSelf==p.Value)&&filters.All(p=>p.Key!=null&&p.Key.sharedMesh==p.Value)&&colliders.All(p=>p.Key!=null&&EditorJsonUtility.ToJson(p.Key)==p.Value)&&bodies.All(p=>p.Key!=null&&EditorJsonUtility.ToJson(p.Key)==p.Value)&&terrains.All(p=>FinishFloatHash(p.Key.terrainData.GetHeights(0,0,p.Key.terrainData.heightmapResolution,p.Key.terrainData.heightmapResolution))+"/"+FinishHoleHash(p.Key.terrainData)==p.Value)&&JsonUtility.ToJson(world.definition.graph)==graph;proof.originalSourceBytesExact=bytes.All(p=>File.Exists(CavernJoinPhysicalSource(p.Key))&&AssetSha(CavernJoinPhysicalSource(p.Key))==p.Value);proof.rocks=parts.ToArray();Directory.CreateDirectory(Io(Output()));File.WriteAllText(Io(Path.Combine(Output(),"measured-cavern-join-finish.json")),JsonUtility.ToJson(proof,true));if(!proof.originalTransformsMeshesPhysicsTerrainAndGraphExact||!proof.originalSourceBytesExact)throw new InvalidOperationException("Cavern join art changed original native geometry/physics/terrain/graph or source bytes.");
            }
        }
    }
}
#endif
