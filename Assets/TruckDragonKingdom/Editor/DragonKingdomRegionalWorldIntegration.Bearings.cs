#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class BearingEvidence
        {
            public string scope="REAL_OWNED_MODULAR_GIRDERS_CROSSBEAMS_AND_GROUNDED_PIERS_NOT_INVISIBLE_SUPPORT_COLLIDERS";
            public string tag,status="IN_PROGRESS";public List<BearingFailure> failures=new List<BearingFailure>();public List<BearingBaseTrial> baseTrials=new List<BearingBaseTrial>();
            public bool nativeMeasuredGroundContactPolicy;public int nativePavementCoincidentSupports;
            public int patches,girders,deckPlanks,crossbeams,piers,nativeBaseTrials,missingNativeBaseTrials,pierRoadRejections,pierJointRejections,groundEmbeddedBearings;public BearingPatch[] geometry;public bool allLowerRoadBuffersClear;
            public string beamModel=DragonKingdomWorldAuthoring.Art+"Synty Studios/PolygonKnights/Models/SM_Prop_Beam_01.fbx",pillarModel=DragonKingdomWorldAuthoring.Art+"Synty Studios/PolygonKnights/Models/SM_Bld_Castle_Pillar_01.fbx";
        }
        [Serializable]public sealed class BearingFailure{public string owner,reason;public Vector3 center;public int side;}
        [Serializable]public sealed class BearingBaseHit{public string collider,type;public Vector3 point,normal;public float distance;}
        [Serializable]public sealed class BearingBaseTrial
        {
            public string owner,outcome;public Vector3 patchCenter,position,rayOrigin,physicalPavementTop,physicalPavementBottom;public int side;
            public float along,pavementY,structuralUndersideY,terrainInterpolatedY;public bool physicalPavementTopPresent,physicalPavementBottomPresent;
            public string nativeHeightmapFormat,nativeMeasuredContactDecision;public float nativeHeightRange,nativeNormalizedHeightScale,nativeCoincidenceBound;public bool nativeMeasuredContactAccepted;public Vector3 physicalPavementTopNormal;
            public BearingBaseHit[] actualNativeBaseHits,eligibleNativeBaseHits;
        }
        public static BearingEvidence LastBearingEvidence{get;private set;}
        [Serializable]public sealed class BearingPatch
        {public string owner;public Vector3 center;public float width,length,maximumPierHeight,fittingDrop;public int girders,deckPlanks,piers;public string[] originalBaseColliders;public bool originalRoadsClear;}
        sealed class BearingPlan{public string key,owner;public Vector3 center,forward;public float width,length;public Collider pavement;}
        static BearingEvidence CreateActualBearingFoundations(DragonKingdomWorld world,string folder,GroundingReview review,bool nativeMeasuredGroundContactPolicy=false)
        {
            Directory.CreateDirectory(folder+"/Materials/Finish");AssetDatabase.Refresh();
            var root=new GameObject("Owned measured bridge bearing foundations").transform;root.SetParent(world.transform,false);
            var evidence=new BearingEvidence{tag=Path.GetFileName(folder),nativeMeasuredGroundContactPolicy=nativeMeasuredGroundContactPolicy};LastBearingEvidence=evidence;var plans=new Dictionary<string,BearingPlan>();
            foreach(var contact in review.contacts.Where(c=>c.classification=="ACTUAL_PAVEMENT_GAP_REQUIRES_TERRAIN_OR_PHYSICAL_SUPPORT_REPAIR"))
            {
                bool pad=contact.requiredOwner.StartsWith("Kingdom ",StringComparison.Ordinal);BearingPlan plan;
                var pavement=world.transform.Find(contact.pavement.Substring(world.transform.name.Length+1)).GetComponent<Collider>();if(pavement==null)throw new InvalidOperationException("Actual recorded pavement collider no longer exists.");
                if(pad)
                {
                    int x=Mathf.FloorToInt(contact.position.x/12),z=Mathf.FloorToInt(contact.position.z/12);string key=contact.requiredOwner+":"+x+":"+z;
                    var bounds=pavement.bounds;float minX=Mathf.Max(x*12-1,bounds.min.x),maxX=Mathf.Min((x+1)*12+1,bounds.max.x),minZ=Mathf.Max(z*12-1,bounds.min.z),maxZ=Mathf.Min((z+1)*12+1,bounds.max.z);
                    plan=new BearingPlan{key=key,owner=contact.requiredOwner,center=new Vector3((minX+maxX)*.5f,contact.pavementHeight,(minZ+maxZ)*.5f),forward=Vector3.forward,width=maxX-minX,length=maxZ-minZ,pavement=pavement};
                }
                else
                {
                    var edge=world.definition.graph.edges.Single(e=>e.edgeId==contact.requiredOwner);float nearest=float.PositiveInfinity,along=0;
                    for(int i=1;i<edge.samples.Count;i++)
                    {var a=edge.samples[i-1];var b=edge.samples[i];var d=Vector3.ProjectOnPlane(b.position-a.position,Vector3.up);float t=d.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(contact.position-a.position,Vector3.up),d)/d.sqrMagnitude):0;var q=Vector3.Lerp(a.position,b.position,t);float distance=Vector3.ProjectOnPlane(q-contact.position,Vector3.up).sqrMagnitude;if(distance<nearest){nearest=distance;along=Mathf.Lerp(a.distanceFromStartMeters,b.distanceFromStartMeters,t);}}
                    int bucket=Mathf.FloorToInt(along/10);float center=Mathf.Clamp(bucket*10+5,0,edge.distanceMeters);Vector3 q0=PointOnActualEdge(edge,Mathf.Max(0,center-1)),q1=PointOnActualEdge(edge,Mathf.Min(edge.distanceMeters,center+1));
                    plan=new BearingPlan{key=edge.edgeId+":"+bucket,owner=edge.edgeId,center=PointOnActualEdge(edge,center),forward=(q1-q0).normalized,width=edge.samples[0].roadWidthMeters+.4f,length=14,pavement=pavement};
                }
                if(plans.TryGetValue(plan.key,out var existing))plan=existing;else plans.Add(plan.key,plan);
                if(!pad)
                {
                    // A real miter can extend past the centerline bucket at a
                    // sharp turn. V174's last unsupported point was1.1m beyond
                    // its14m bearing. Cover every actual native contact rather
                    // than assuming the bucket's nominal rectangle contains it.
                    var flatForward=Vector3.ProjectOnPlane(plan.forward,Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,flatForward);var delta=Vector3.ProjectOnPlane(contact.position-plan.center,Vector3.up);
                    plan.length=Mathf.Max(plan.length,2*(Mathf.Abs(Vector3.Dot(delta,flatForward))+1));plan.width=Mathf.Max(plan.width,2*(Mathf.Abs(Vector3.Dot(delta,right))+1));
                }
            }
            var rows=new List<BearingPatch>();int index=0;
            foreach(var plan in plans.Values)
            {
                FitBearingToActualPavement(plan);
                var group=new GameObject("Measured elevated pavement bearing "+index).transform;group.SetParent(root,false);var row=new BearingPatch{owner=plan.owner,center=plan.center,width=plan.width,length=plan.length};
                Vector3 forward=plan.forward.sqrMagnitude>.01f?plan.forward.normalized:Vector3.forward,right=Vector3.Cross(Vector3.up,forward).normalized;var up=Vector3.Cross(forward,right).normalized;
                const float girderDepth=1.8f,crossDepth=2,deckDepth=.45f;
                int plankCount=Mathf.Max(2,Mathf.CeilToInt(plan.length/1.8f));
                for(int plank=0;plank<plankCount;plank++)
                {float along=((plank+.5f)/plankCount-.5f)*plan.length;var center=plan.center+forward*along-up*(.08f+deckDepth*.5f);FitOwnedBeam(group,center,right,plan.length/plankCount*1.65f,plan.width+2,deckDepth,"Owned bridge bearing deck plank "+index+"-"+plank,folder,up);row.deckPlanks++;evidence.deckPlanks++;}
                for(int i=0;i<3;i++)
                {var center=plan.center+right*(i-1)*(plan.width/3)-up*(.08f+deckDepth+girderDepth*.5f);var beam=FitOwnedBeam(group,center,forward,plan.width/3+.35f,plan.length,girderDepth,"Owned bridge bearing girder "+index+"-"+i,folder);row.girders++;evidence.girders++;}
                var crossCenter=plan.center-Vector3.up*(.08f+deckDepth+girderDepth+crossDepth*.5f);
                var cross=FitOwnedBeam(group,crossCenter,right,12,plan.width+5,crossDepth,"Owned bridge structural crossbeam "+index,folder);evidence.crossbeams++;
                float underside=plan.center.y-.08f-deckDepth-girderDepth-crossDepth;var bases=new List<string>();
                for(int side=-1;side<=1;side+=2)
                {
                    bool seated=false;
                    foreach(float along in new[]{0f,-3f,3f,-4.5f,4.5f})
                    {
                        Vector3 at=plan.center+right*side*(plan.width*.5f-1)+Vector3.ProjectOnPlane(forward,Vector3.up).normalized*along;
                        evidence.nativeBaseTrials++;float terrainY=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,at);
                        var origin=new Vector3(at.x,Mathf.Max(underside+1,terrainY+1),at.z);
                        var rawHits=Physics.RaycastAll(origin,Vector3.down,1400,~0,QueryTriggerInteraction.Ignore)
                            .Where(h=>!h.collider.transform.IsChildOf(root)&&(h.collider is TerrainCollider||AncestorName(h.collider.transform,world.transform,"Owned stone viaduct "))).OrderByDescending(h=>h.point.y).ToArray();
                        // Preserve original eligibility exactly. Record every
                        // real candidate hit first so a rejection can be traced
                        // to an actual missed ray, slope or pavement-height test.
                        var hits=rawHits.Where(h=>h.normal.y>.25f&&h.point.y<=plan.center.y-.02f).ToArray();
                        BearingBaseHit Describe(RaycastHit hit)=>new BearingBaseHit{collider=PathOf(hit.collider.transform,world.transform),type=hit.collider.GetType().FullName,point=hit.point,normal=hit.normal,distance=hit.distance};
                        var trial=new BearingBaseTrial{owner=plan.owner,patchCenter=plan.center,position=at,rayOrigin=origin,side=side,along=along,pavementY=plan.center.y,structuralUndersideY=underside,terrainInterpolatedY=terrainY,actualNativeBaseHits=rawHits.Select(Describe).ToArray(),eligibleNativeBaseHits=hits.Select(Describe).ToArray()};evidence.baseTrials.Add(trial);
                        trial.physicalPavementTopPresent=plan.pavement.Raycast(new Ray(new Vector3(at.x,1200,at.z),Vector3.down),out var top,1800);if(trial.physicalPavementTopPresent){trial.physicalPavementTop=top.point;trial.physicalPavementTopNormal=top.normal;}
                        trial.physicalPavementBottomPresent=plan.pavement.Raycast(new Ray(new Vector3(at.x,underside-100,at.z),Vector3.up),out var bottom,200);if(trial.physicalPavementBottomPresent)trial.physicalPavementBottom=bottom.point;
                        if(hits.Length==0)
                        {
                            // A preserved original ground sample may sit a few
                            // millimetres above its real road top after native
                            // heightmap quantization. This is direct existing
                            // terrain support, not an ungrounded new pillar.
                            var coincident=nativeMeasuredGroundContactPolicy?rawHits.FirstOrDefault(hit=>MeasuredNativePavementContact(hit,trial)):default;
                            if(coincident.collider!=null){trial.nativeMeasuredContactAccepted=true;trial.outcome="NATIVE_MEASURED_PAVEMENT_COINCIDENT_TERRAIN_SUPPORT_ACCEPTED";bases.Add(PathOf(coincident.collider.transform,world.transform));evidence.nativePavementCoincidentSupports++;evidence.groundEmbeddedBearings++;seated=true;break;}
                            trial.outcome=rawHits.Length==0?"NO_ACTUAL_NATIVE_BASE_RAY_HIT":"ORIGINAL_SLOPE_OR_PAVEMENT_HEIGHT_FILTER_REJECTED_REAL_HITS";evidence.missingNativeBaseTrials++;continue;
                        }var foot=hits[0];
                        if(underside-foot.point.y<.2f){trial.outcome="ORIGINAL_EMBEDDED_GROUND_SUPPORT_ACCEPTED";bases.Add(PathOf(foot.collider.transform,world.transform));evidence.groundEmbeddedBearings++;seated=true;break;}
                        float height=underside-foot.point.y+.24f,width=Mathf.Clamp(height*.12f,2.6f,7);
                        var pillar=FitOwnedPillar(group,new Vector3(at.x,foot.point.y-.12f,at.z),height,width,forward,"Owned grounded viaduct pier "+index+"-"+side,folder);Physics.SyncTransforms();
                        bool clear=ClearRoadEnvelope(world,pillar,false),joint=NativeBearingJoint(pillar,cross,new Vector3(at.x,underside+.12f,at.z));
                        if(!clear||!joint)
                        {trial.outcome=!clear?"ACTUAL_PIER_ROAD_ENVELOPE_REJECTED":"ACTUAL_PIER_TO_CROSSBEAM_JOINT_REJECTED";if(!clear)evidence.pierRoadRejections++;if(!joint)evidence.pierJointRejections++;UnityEngine.Object.DestroyImmediate(pillar.gameObject);continue;}
                        trial.outcome="ACTUAL_PIER_BASE_JOINT_AND_ROAD_CLEARANCE_ACCEPTED";row.piers++;evidence.piers++;row.maximumPierHeight=Mathf.Max(row.maximumPierHeight,height);bases.Add(PathOf(foot.collider.transform,world.transform));seated=true;break;
                    }
                    if(!seated)evidence.failures.Add(new BearingFailure{owner=plan.owner,center=plan.center,side=side,reason="No actual supported, joined and lower-road-clear bearing pier in the bounded cap footprint"});
                }
                Physics.SyncTransforms();row.originalRoadsClear=ClearRoadEnvelope(world,group,false);
                // Match the real unchanged pavement/body envelope at grade
                // transitions. Only this NEW supported assembly is fitted;
                // collider sizes, original roads and every body gate remain.
                Vector3 authored=group.position;
                for(int fit=1;!row.originalRoadsClear&&fit<=10;fit++)
                {group.position=authored-Vector3.up*(fit*.1f);Physics.SyncTransforms();row.originalRoadsClear=ClearRoadEnvelope(world,group,false);row.fittingDrop=fit*.1f;}

                if(!row.originalRoadsClear)evidence.failures.Add(new BearingFailure{owner=plan.owner,center=plan.center,reason="Actual complete bearing assembly intrudes into a road body/roof or start"});
                row.originalBaseColliders=bases.ToArray();rows.Add(row);evidence.patches++;index++;
            }
            evidence.geometry=rows.ToArray();evidence.allLowerRoadBuffersClear=rows.All(r=>r.originalRoadsClear&&r.originalBaseColliders.Length==2);evidence.status=evidence.failures.Count==0?"ACTUAL_STATIC_SUPPORT_CHAINS_REQUIRE_FULL_WORLD_GATES_AND_RENDER_REVIEW":"REJECTED_PHYSICAL_SUPPORT_CHAINS";return evidence;
        }
        public static bool NativeTerrainPavementContactWithinBound(bool actualTerrainCollider,bool actualPavementTop,float normalY,float terrainY,float pavementY,float measuredQuantizationBound)
        {
            return actualTerrainCollider&&actualPavementTop&&float.IsFinite(normalY)&&normalY>.25f&&float.IsFinite(terrainY)&&float.IsFinite(pavementY)&&float.IsFinite(measuredQuantizationBound)&&measuredQuantizationBound>0&&measuredQuantizationBound<=.02f&&Mathf.Abs(terrainY-pavementY)<=measuredQuantizationBound;
        }
        static bool MeasuredNativePavementContact(RaycastHit hit,BearingBaseTrial trial)
        {
            trial.nativeMeasuredContactDecision="REJECTED_NOT_ACTUAL_TERRAIN";
            if(!(hit.collider is TerrainCollider terrain)||terrain.terrainData==null)return false;
            var data=terrain.terrainData;var texture=data.heightmapTexture;trial.nativeHeightmapFormat=texture==null?"NULL":texture.graphicsFormat.ToString();trial.nativeHeightRange=data.size.y;
            trial.nativeNormalizedHeightScale=UnityEngine.TerrainTools.PaintContext.kNormalizedHeightScale;
            // Public Unity terrain normalized-height scale and actual R16_UNorm
            // texture determine the half-quantum bound; do not use a generic
            // gap allowance that would accept a cliff above the pavement.
            if(texture==null||texture.graphicsFormat!=UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm||data.size.y!=600||!float.IsFinite(trial.nativeNormalizedHeightScale)||trial.nativeNormalizedHeightScale<=0){trial.nativeMeasuredContactDecision="REJECTED_UNAUDITED_NATIVE_HEIGHT_ENCODING";return false;}
            trial.nativeCoincidenceBound=data.size.y/(65535f*trial.nativeNormalizedHeightScale)*.5f+.00002f;
            bool top=trial.physicalPavementTopPresent&&trial.physicalPavementTopNormal.y>.4f;
            bool accepted=NativeTerrainPavementContactWithinBound(true,top,hit.normal.y,hit.point.y,trial.physicalPavementTop.y,trial.nativeCoincidenceBound);
            trial.nativeMeasuredContactDecision=accepted?"ACTUAL_TERRAIN_WITHIN_PUBLIC_ENCODING_HALF_QUANTUM_OF_REAL_PAVEMENT_TOP":"REJECTED_NO_VALID_TOP_BAD_NORMAL_OR_REAL_HEIGHT_GAP";return accepted;
        }
        static void FitBearingToActualPavement(BearingPlan plan)
        {
            if(plan.pavement.Raycast(new Ray(new Vector3(plan.center.x,1200,plan.center.z),Vector3.down),out var center,1800)&&center.normal.y>.4f)
            {plan.center=center.point;var tangent=Vector3.ProjectOnPlane(plan.forward,center.normal);if(tangent.sqrMagnitude>.01f)plan.forward=tangent.normalized;}
            var forward=plan.forward.normalized;var right=Vector3.Cross(Vector3.up,forward).normalized;var normal=Vector3.Cross(forward,right).normalized;float drop=0;
            // The vendor road's actual triangles, rather than its interpolated
            // centerline alone, own the underside fit at flat/grade transitions.
            foreach(float x in new[]{-.48f,-.24f,0,.24f,.48f})foreach(float z in new[]{-.48f,-.24f,0,.24f,.48f})
            {
                var p=plan.center+right*plan.width*x+forward*plan.length*z;
                if(plan.pavement.Raycast(new Ray(new Vector3(p.x,1200,p.z),Vector3.down),out var hit,1800)&&hit.normal.y>.4f)drop=Mathf.Max(drop,p.y-hit.point.y);
            }
            plan.center-=normal*drop;
        }
        static Vector3 PointOnActualEdge(LwsRoadEdge edge,float along)
        {
            for(int i=1;i<edge.samples.Count;i++)if(along<=edge.samples[i].distanceFromStartMeters)
            {var a=edge.samples[i-1];var b=edge.samples[i];return Vector3.Lerp(a.position,b.position,Mathf.InverseLerp(a.distanceFromStartMeters,b.distanceFromStartMeters,along));}
            return edge.samples[edge.samples.Count-1].position;
        }
        static Transform FitOwnedBeam(Transform parent,Vector3 center,Vector3 forward,float width,float length,float depth,string name,string folder,Vector3? suppliedUp=null,bool configureMaterials=true)
        {
            var t=DragonKingdomWorldAuthoring.PlaceOwnedArtForReview(parent,DragonKingdomWorldAuthoring.Knights+"SM_Prop_Beam_01.prefab",center,0,2,true);t.name=name;t.localScale=Vector3.one;
            var b=ActualLocalMeshBounds(t);t.rotation=Quaternion.LookRotation(forward,suppliedUp??Vector3.up)*Quaternion.Euler(90,0,0);t.localScale=new Vector3(width/b.size.x,length/b.size.y,depth/b.size.z);t.position=center-t.TransformVector(b.center);if(configureMaterials)ConfigureFinishMaterials(t,folder);return t;
        }
        static Transform FitOwnedPillar(Transform parent,Vector3 bottom,float height,float width,Vector3 forward,string name,string folder)
        {
            var t=DragonKingdomWorldAuthoring.PlaceOwnedArtForReview(parent,DragonKingdomWorldAuthoring.Knights+"SM_Bld_Castle_Pillar_01.prefab",bottom,0,2,true);t.name=name;t.localScale=Vector3.one;var b=ActualLocalMeshBounds(t);
            var direction=Vector3.ProjectOnPlane(forward,Vector3.up);t.rotation=Quaternion.LookRotation(direction.sqrMagnitude>.01f?direction:Vector3.forward);t.localScale=new Vector3(width/b.size.x,height/b.size.y,width/b.size.z);
            t.position=bottom-t.TransformVector(new Vector3(b.center.x,b.min.y,b.center.z));ConfigureFinishMaterials(t,folder);return t;
        }
        static Bounds ActualLocalMeshBounds(Transform t)
        {
            bool first=true;var b=new Bounds();foreach(var filter in t.GetComponentsInChildren<MeshFilter>(true))
            {if(filter.sharedMesh==null)continue;var v=filter.sharedMesh.bounds;var matrix=t.worldToLocalMatrix*filter.transform.localToWorldMatrix;foreach(float x in new[]{v.min.x,v.max.x})foreach(float y in new[]{v.min.y,v.max.y})foreach(float z in new[]{v.min.z,v.max.z}){var p=matrix.MultiplyPoint3x4(new Vector3(x,y,z));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}
            if(first||b.size.x<.01f||b.size.y<.01f||b.size.z<.01f)throw new InvalidOperationException("Complete actual modular mesh bounds required.");return b;
        }
        static bool NativeBearingJoint(Transform pillar,Transform cross,Vector3 point)
        {
            bool top=pillar.GetComponentsInChildren<Collider>(true).Any(c=>c.Raycast(new Ray(point+Vector3.up,Vector3.down),out var h,2)&&h.normal.y>.4f&&Mathf.Abs(h.point.y-point.y)<.3f);
            bool underside=cross.GetComponentsInChildren<Collider>(true).Any(c=>c.Raycast(new Ray(point-Vector3.up,Vector3.up),out var h,2)&&h.normal.y<-.4f&&Mathf.Abs(h.point.y-point.y)<.35f);return top&&underside;
        }
    }
}
#endif
