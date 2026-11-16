#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class LeverInput{public LeverView[] views;}
        [Serializable]sealed class LeverView{public int owner;public bool exactOwnedStatePreserved;public Vector3 cameraUp;public string actualBaseClips;public LeverSkinIdentity[] actualSkinSources;public LeverFrame[] pureConnectedNeckCandidates;public LeverRays[] sameFrameVirtualCabCandidates;}
        [Serializable]sealed class LeverSkinIdentity{public int slot;public string sourceGuid,sourceLocalId;public float trueFullMatrixRmse;}
        [Serializable]sealed class LeverFrame{public string recipe;public Vector3 modelRight;public LeverJoint[] connectedJointMatrices;}
        [Serializable]sealed class LeverJoint{public string bone;public Matrix4x4 originalWorld;}
        [Serializable]sealed class LeverRays{public string name;public LeverRay[] centralWindscreenRays;}
        [Serializable]sealed class LeverRay{public Vector2 viewport;public Vector3 origin,direction,worldPoint;public bool ownDragon;public float headNeckWeight,distance;public int triangle;}
        [Serializable]sealed class HeadLeverReport
        {
            public string scope="READ_ONLY_ANALYTIC_CONNECTED_NECK_LEVER_REDISTRIBUTION_FIXED45_TOTAL_NORMAL_PITCH_NOT_NATIVE_PROFILE_OR_RUNTIME_CAB_ACCEPTANCE",input,inputSha256;
            public float totalPitch=45,originalNeckPitch=15,originalHeadPitch=30,maximumRedistribution=45,clearanceMargin=.1f,analyticExtraDegrees,candidateExtraDegrees,candidateNeckDegrees,candidateHeadDegrees;
            public bool finiteCandidate,sourceExact;public List<HeadLeverPose> poses=new List<HeadLeverPose>();public List<HeadLeverLimit> limitingConstraints=new List<HeadLeverLimit>();
        }
        [Serializable]sealed class HeadLeverPose
        {public int owner,sourceVertices,sourceTriangles,constrainedHeadVertices,rigidHeadRayTriangles,mixedNeckTriangles,baselineOwnHeadHits,candidateOwnHeadHits;public bool baselineCenterHead,candidateCenterHead;public float baselineCenterDistanceError;public Vector3 neck,head,axis,headOriginDisplacement;public string sourceMeshGuid,sourceMeshLocalId;}
        [Serializable]sealed class HeadLeverLimit{public int owner,vertex,triangle=-1;public string constraint;public double lowDegrees,highDegrees,a,b,c;public Vector3 originalPoint,movingLever;}
        struct LeverBand{public double low,high;public HeadLeverLimit witness;}
        sealed class LeverPoseData{public LeverView view;public Vector3[] original,levers;public float[] headWeights;public int[] indices;public Vector3 neck,head,axis;public LeverRay[] rays;public HeadLeverPose row;}
        static double LeverHeight(double a,double b,double c,double angle)=>a*Math.Cos(angle)+b*Math.Sin(angle)+c;
        static void LeverRange(double a,double b,double c,double maximum,out double low,out double high)
        {
            low=Math.Min(c+a,LeverHeight(a,b,c,maximum));high=Math.Max(c+a,LeverHeight(a,b,c,maximum));double phase=Math.Atan2(b,a);
            for(int k=-2;k<=2;k++){double t=phase+k*Math.PI;if(t<=0||t>=maximum)continue;double v=LeverHeight(a,b,c,t);low=Math.Min(low,v);high=Math.Max(high,v);}
        }
        static void AppendLeverBands(double a,double b,double c,double maximum,HeadLeverLimit witness,List<LeverBand> bands)
        {
            double radius=Math.Sqrt(a*a+b*b);var cuts=new List<double>{0,maximum};
            if(radius>1e-12)
            {
                double target=-c/radius;if(target>=-1&&target<=1){double alpha=Math.Acos(target),phase=Math.Atan2(b,a);foreach(double sign in new[]{-1d,1d})for(int k=-2;k<=2;k++){double t=phase+sign*alpha+k*2*Math.PI;if(t>0&&t<maximum)cuts.Add(t);}}
            }
            cuts.Sort();for(int i=0;i+1<cuts.Count;i++){double low=cuts[i],high=cuts[i+1];if(high-low<1e-12||LeverHeight(a,b,c,(low+high)*.5)<=0)continue;bands.Add(new LeverBand{low=low,high=high,witness=new HeadLeverLimit{owner=witness.owner,vertex=witness.vertex,triangle=witness.triangle,constraint=witness.constraint,a=witness.a,b=witness.b,c=witness.c,originalPoint=witness.originalPoint,movingLever=witness.movingLever}});}
        }
        static void LeverCoefficients(Vector3 original,Vector3 lever,Vector3 axis,Vector3 direction,Vector3 origin,out double a,out double b,out double c)
        {
            Vector3 perpendicular=lever-axis*Vector3.Dot(axis,lever);a=Vector3.Dot(direction,perpendicular);b=Vector3.Dot(direction,Vector3.Cross(axis,lever));c=Vector3.Dot(direction,original-origin)-a;
        }
        static (bool head,int triangle,float distance) TraceLeverHead(LeverPoseData data,Vector3[] vertices,LeverRay ray)
        {
            float closest=150,weight=0;int triangle=-1;
            for(int i=0;i<data.indices.Length;i+=3)
            {
                int ia=data.indices[i],ib=data.indices[i+1],ic=data.indices[i+2];var face=new ActualDeckTriangle{a=vertices[ia],b=vertices[ib],c=vertices[ic]};if(!ActualTriangleRay(ray.origin,ray.direction,face,out float distance)||distance>=closest)continue;
                var edge0=face.b-face.a;var edge1=face.c-face.a;var delta=ray.origin+ray.direction*distance-face.a;float d00=Vector3.Dot(edge0,edge0),d01=Vector3.Dot(edge0,edge1),d11=Vector3.Dot(edge1,edge1),d20=Vector3.Dot(delta,edge0),d21=Vector3.Dot(delta,edge1),denominator=d00*d11-d01*d01;if(Mathf.Abs(denominator)<1e-14f)continue;float u=(d11*d20-d01*d21)/denominator,v=(d00*d21-d01*d20)/denominator;weight=data.headWeights[ia]*(1-u-v)+data.headWeights[ib]*u+data.headWeights[ic]*v;closest=distance;triangle=i/3;
            }
            return(weight>=.5f,triangle,closest);
        }
        static double LeverDot(Vector3 left,Vector3 right)=>(double)left.x*right.x+(double)left.y*right.y+(double)left.z*right.z;
        static void AppendRigidHeadRayBands(LeverPoseData data,int[] ids,int triangle,LeverRay ray,double maximum,float margin,List<LeverBand> bands)
        {
            Vector3 origin=data.original[ids[0]],edge1=data.original[ids[1]]-origin,edge2=data.original[ids[2]]-origin;
            var cross=Vector3.Cross(ray.direction,edge2);double determinant=LeverDot(edge1,cross);if(Math.Abs(determinant)<1e-8)return;var offset=ray.origin-origin;var q=Vector3.Cross(offset,edge1);
            double u0=LeverDot(offset,cross)/determinant,v0=LeverDot(ray.direction,q)/determinant,t0=LeverDot(edge2,q)/determinant;
            Vector3 lever=data.levers[ids[0]],cosine=lever-data.axis*Vector3.Dot(data.axis,lever),sine=Vector3.Cross(data.axis,lever);
            double ua=-LeverDot(cosine,cross)/determinant,ub=-LeverDot(sine,cross)/determinant,uc=u0-ua;
            double va=-LeverDot(ray.direction,Vector3.Cross(cosine,edge1))/determinant,vb=-LeverDot(ray.direction,Vector3.Cross(sine,edge1))/determinant,vc=v0-va;
            double ta=-LeverDot(edge2,Vector3.Cross(cosine,edge1))/determinant,tb=-LeverDot(edge2,Vector3.Cross(sine,edge1))/determinant,tc=t0-ta;
            double twiceArea=Vector3.Cross(edge1,edge2).magnitude;if(twiceArea<1e-12)return;
            // Expand each actual triangle edge outward by the physical planning
            // margin. This is conservative planning only; final ray/SAT gates
            // still test the complete unexpanded original native geometry.
            double eu=.00002+margin*edge2.magnitude/twiceArea,ev=.00002+margin*edge1.magnitude/twiceArea,ew=.00002+margin*(edge2-edge1).magnitude/twiceArea;
            var conditions=new[]{(a:ua,b:ub,c:uc+eu),(a:-ua,b:-ub,c:1+eu-uc),(a:va,b:vb,c:vc+ev),(a:-ua-va,b:-ub-vb,c:1+ew-uc-vc),(a:ta,b:tb,c:tc),(a:-ta,b:-tb,c:150-tc)};
            var cuts=new List<double>{0,maximum};foreach(var h in conditions){double radius=Math.Sqrt(h.a*h.a+h.b*h.b);if(radius<1e-12)continue;double target=-h.c/radius;if(target< -1||target>1)continue;double phase=Math.Atan2(h.b,h.a),alpha=Math.Acos(target);foreach(double sign in new[]{-1d,1d})for(int k=-2;k<=2;k++){double angle=phase+sign*alpha+k*2*Math.PI;if(angle>0&&angle<maximum)cuts.Add(angle);}}
            cuts.Sort();for(int i=0;i+1<cuts.Count;i++){double low=cuts[i],high=cuts[i+1];if(high-low<1e-12||conditions.Any(h=>LeverHeight(h.a,h.b,h.c,(low+high)*.5)<0))continue;bands.Add(new LeverBand{low=low,high=high,witness=new HeadLeverLimit{owner=data.view.owner,vertex=ids[0],triangle=triangle,constraint="ACTUAL_RIGID_HEAD_TRIANGLE_PHYSICALLY_EXPANDED_RAY_HIT_INTERVAL",originalPoint=origin,movingLever=lever}});}
        }
        public static string InspectFireHeadLeverRedistributionWithoutSavingAssets(string evidencePath)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists(evidencePath))throw new InvalidOperationException("Stopped compiled Editor and exact actual V328 cab evidence required.");
            var report=new HeadLeverReport{input=evidencePath,inputSha256=Hash(evidencePath)};var input=JsonUtility.FromJson<LeverInput>(File.ReadAllText(evidencePath));var source=AssetDatabase.LoadAssetAtPath<GameObject>(ActorPath("dragon-fire"));var watched=AssetDatabase.GetDependencies(ActorPath("dragon-fire"),true).Where(File.Exists).ToDictionary(path=>path,Hash);var dataRows=new List<LeverPoseData>();var bands=new List<LeverBand>();double maximum=report.maximumRedistribution*Math.PI/180;
            foreach(var view in input.views)
            {
                if(!view.exactOwnedStatePreserved||!view.actualBaseClips.Contains("TRANSIENT_CAB_"))throw new InvalidOperationException("Exact preserved actual qualified normal-family frame required.");var identity=view.actualSkinSources.Single(skin=>skin.slot==view.owner);if(identity.trueFullMatrixRmse>=.0001f)throw new InvalidOperationException("Independent actual source skin reference failed.");
                var skin=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(renderer=>{if(renderer.sharedMesh==null)return false;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.sharedMesh,out string guid,out long id);return guid==identity.sourceGuid&&id.ToString()==identity.sourceLocalId;});var mesh=skin.sharedMesh;var frame=view.pureConnectedNeckCandidates.Single(pose=>pose.recipe=="ZERO_OFFSET_NATIVE_TRUE_BAKE_CONTROL");var matrices=frame.connectedJointMatrices.ToDictionary(joint=>joint.bone,joint=>joint.originalWorld);var neckBone=skin.bones.Single(bone=>bone.name=="RigNeck");var headBone=skin.bones.Single(bone=>bone.name=="RigHead");Vector3 neck=matrices[neckBone.name].GetColumn(3),head=matrices[headBone.name].GetColumn(3),axis=frame.modelRight.normalized;var local=mesh.vertices;var bind=mesh.bindposes;var weights=mesh.boneWeights;var bones=skin.bones;var data=new LeverPoseData{view=view,original=new Vector3[local.Length],levers=new Vector3[local.Length],headWeights=new float[local.Length],indices=mesh.triangles,neck=neck,head=head,axis=axis,rays=view.sameFrameVirtualCabCandidates.Single(rays=>rays.name=="ACTUAL_CAMERA_ROUND_TRIP_CONTROL").centralWindscreenRays};
                for(int i=0;i<local.Length;i++)
                {
                    var w=weights[i];Add(w.boneIndex0,w.weight0);Add(w.boneIndex1,w.weight1);Add(w.boneIndex2,w.weight2);Add(w.boneIndex3,w.weight3);
                    void Add(int index,float amount)
                    {
                        if(amount<=0)return;var bone=bones[index];Vector3 point=matrices[bone.name].MultiplyPoint3x4(bind[index].MultiplyPoint3x4(local[i]));data.original[i]+=point*amount;
                        if(bone==headBone||bone.IsChildOf(headBone))data.levers[i]+=(head-neck)*amount;else if(bone==neckBone||bone.IsChildOf(neckBone))data.levers[i]+=(point-neck)*amount;
                        if(bone.name.IndexOf("head",StringComparison.OrdinalIgnoreCase)>=0||bone.name.IndexOf("neck",StringComparison.OrdinalIgnoreCase)>=0)data.headWeights[i]+=amount;
                    }
                }
                var center=data.rays.Single(ray=>(ray.viewport-new Vector2(.5f,.5f)).sqrMagnitude<.000001f);var baseline=TraceLeverHead(data,data.original,center);var row=new HeadLeverPose{owner=view.owner,sourceVertices=local.Length,sourceTriangles=data.indices.Length/3,neck=neck,head=head,axis=axis,baselineCenterHead=baseline.head,baselineCenterDistanceError=Mathf.Abs(baseline.distance-center.distance),sourceMeshGuid=identity.sourceGuid,sourceMeshLocalId=identity.sourceLocalId};data.row=row;report.poses.Add(row);dataRows.Add(data);
                if(!center.ownDragon||center.headNeckWeight<.5f||!baseline.head||baseline.triangle!=center.triangle||row.baselineCenterDistanceError>.001f)throw new InvalidOperationException("Source weighted reconstruction must reproduce the exact actual owner-head center triangle/distance before planning a different posture.");row.baselineOwnHeadHits=data.rays.Count(ray=>TraceLeverHead(data,data.original,ray).head);
                var constrained=new HashSet<int>();Vector3 right=Vector3.Cross(center.direction,view.cameraUp).normalized;
                for(int i=0;i<data.indices.Length;i+=3)
                {
                    var ids=new[]{data.indices[i],data.indices[i+1],data.indices[i+2]};if(ids.All(id=>data.headWeights[id]<.5f))continue;
                    // A common lever preserves this complete triangle's edges
                    // exactly: its ray-hit interval is analytic, rather than
                    // requiring every nearby skull vertex below the eye plane.
                    if((data.levers[ids[0]]-data.levers[ids[1]]).sqrMagnitude<1e-12f&&(data.levers[ids[0]]-data.levers[ids[2]]).sqrMagnitude<1e-12f)
                    {AppendRigidHeadRayBands(data,ids,i/3,center,maximum,report.clearanceMargin,bands);row.rigidHeadRayTriangles++;continue;}
                    row.mixedNeckTriangles++;double sideLow=double.PositiveInfinity,sideHigh=double.NegativeInfinity,forwardLow=double.PositiveInfinity,forwardHigh=double.NegativeInfinity;
                    foreach(int id in ids)
                    {
                        LeverCoefficients(data.original[id],data.levers[id],axis,right,center.origin,out double a,out double b,out double c);LeverRange(a,b,c,maximum,out double low,out double high);sideLow=Math.Min(sideLow,low);sideHigh=Math.Max(sideHigh,high);
                        LeverCoefficients(data.original[id],data.levers[id],axis,center.direction,center.origin,out a,out b,out c);LeverRange(a,b,c,maximum,out low,out high);forwardLow=Math.Min(forwardLow,low);forwardHigh=Math.Max(forwardHigh,high);
                    }
                    if(sideLow>.05||sideHigh<-.05||forwardHigh<0||forwardLow>150)continue;foreach(int id in ids)constrained.Add(id);
                }
                row.constrainedHeadVertices=constrained.Count;
                foreach(int id in constrained)
                {
                    LeverCoefficients(data.original[id],data.levers[id],axis,view.cameraUp,center.origin,out double a,out double b,out double c);var witness=new HeadLeverLimit{owner=view.owner,vertex=id,a=a,b=b,c=c,originalPoint=data.original[id],movingLever=data.levers[id]};AppendLeverBands(a,b,c+report.clearanceMargin,maximum,witness,bands);
                }
            }
            bands.Sort((a,b)=>a.low.CompareTo(b.low));double extra=0;
            foreach(var band in bands)
            {
                if(band.low>extra||band.high<extra)continue;if(report.limitingConstraints.Count<64){band.witness.lowDegrees=band.low*180/Math.PI;band.witness.highDegrees=band.high*180/Math.PI;report.limitingConstraints.Add(band.witness);}extra=band.high+.000001;if(extra>maximum)break;
            }
            report.analyticExtraDegrees=(float)(extra*180/Math.PI);report.candidateExtraDegrees=Mathf.Ceil(report.analyticExtraDegrees);report.finiteCandidate=report.candidateExtraDegrees<=report.maximumRedistribution;
            if(report.finiteCandidate)
            {
                report.candidateNeckDegrees=15+report.candidateExtraDegrees;report.candidateHeadDegrees=30-report.candidateExtraDegrees;
                foreach(var data in dataRows)
                {
                    var rotation=Quaternion.AngleAxis(report.candidateExtraDegrees,data.axis);var candidate=data.original.Select((point,index)=>point+rotation*data.levers[index]-data.levers[index]).ToArray();data.row.headOriginDisplacement=rotation*(data.head-data.neck)-(data.head-data.neck);data.row.candidateOwnHeadHits=data.rays.Count(ray=>TraceLeverHead(data,candidate,ray).head);data.row.candidateCenterHead=TraceLeverHead(data,candidate,data.rays.Single(ray=>(ray.viewport-new Vector2(.5f,.5f)).sqrMagnitude<.000001f)).head;
                }
            }
            report.sourceExact=Hash(evidencePath)==report.inputSha256&&watched.All(pair=>Hash(pair.Key)==pair.Value);string json=JsonUtility.ToJson(report,true);var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","HeadLever");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"actual-frame-analytic-head-lever.json"),json);}if(!report.sourceExact)throw new InvalidOperationException("Read-only head lever planning changed source evidence or original assets.");return json;
        }
    }
}
#endif
