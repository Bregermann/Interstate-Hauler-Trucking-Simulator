#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using LWS.TruckSports;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        sealed class OriginalFootprint
        {
            public string owner;
            public SportsEventTruck sourceTruck;
            public float support;
            public Vector3[] bottom;
        }
        [Serializable]sealed class FootprintReceipt
        {
            public string scope="EXPLICIT OWNED SADDLE PATCH SELECTION AGAINST UNMOVED ORIGINAL NWH COLLIDER GEOMETRY; actual skinned bind/mapped poses; no added clearance or gameplay acceptance",species,failure;
            public float rearward;
            public int originalCrews,uniqueFootprints,violatingRayProofs;
            public bool fits,finiteContactControlsPassed;public List<FiniteContactControls> finiteContactControls=new List<FiniteContactControls>();
            public List<FootprintPose> poses=new List<FootprintPose>();
        }
        [Serializable]sealed class FootprintPose
        {
            public string pose,owner,effectiveClip,incomingClip,failure;
            public string[] actualConfiguredLeafPaths;public float[] actualConfiguredLeafWeights;
            public float actualConfiguredPhase,actualConfiguredHorizontal,actualConfiguredVertical;
            public int actualTailTrianglesTested,actualTailBoxIntersections;
            public TailBoxProof[] actualTailIntersections;
            public float minimumSkinGap,minimumRawProjectionGap=float.MaxValue,platformGap,incomingWeight;
            public bool centerTorso,platformTorso;
            public Vector3 platform,skinUnderPlatform;
            public FootprintPoint[] points;
        }
        [Serializable]sealed class FootprintPoint
        {
            public Vector3 bottom,skin;
            public bool torso;
            public float gap;
            public SurfaceProof actualPenetratingTriangle;public FiniteContactProof finiteOriginalContact;public bool contributesToFiniteMinimum;
        }
        static OriginalFootprint[] OriginalFootprints(SportsEventTruck[] trucks)
        {
            if(trucks==null||trucks.Length==0)throw new InvalidOperationException("Actual original NWH truck geometry required for saddle calibration.");
            var unique=new List<OriginalFootprint>();
            foreach(var truck in trucks)
            {
                if(truck==null||truck.body==null||truck.shape==null||truck.body.transform!=truck.transform||Vector3.Distance(truck.transform.lossyScale,Vector3.one)>.0001f)
                    throw new InvalidOperationException("Preserve the actual unscaled original NWH root/body/shape before footprint calibration.");
                float lower=truck.shape.center.y-truck.shape.size.y*.5f;
                foreach(var wheel in truck.wheels)if(wheel!=null)lower=Mathf.Min(lower,truck.transform.InverseTransformPoint(wheel.transform.position).y-wheel.wheel.radius-wheel.spring.maxLength);
                var points=new List<Vector3>();var matrix=truck.body.transform.worldToLocalMatrix*truck.shape.transform.localToWorldMatrix;
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)points.Add(matrix.MultiplyPoint3x4(truck.shape.center+Vector3.Scale(truck.shape.size*.5f,new Vector3(x,-1,z))));
                float support=-lower+.05f;
                if(unique.Any(value=>Mathf.Abs(value.support-support)<.00001f&&Enumerable.Range(0,9).All(index=>(value.bottom[index]-points[index]).sqrMagnitude<.00000001f)))continue;
                unique.Add(new OriginalFootprint{owner=truck.name,sourceTruck=truck,support=support,bottom=points.ToArray()});
            }
            return unique.ToArray();
        }
        static bool FitsOriginalFootprints(DragonCharacter actor,DragonSpeciesDefinition species,Calibration calibration,BonePose[] originalPoses,SportsEventTruck[] trucks,out FootprintReceipt receipt)
        {
            var originals=OriginalFootprints(trucks);receipt=new FootprintReceipt{species=species.id,rearward=calibration.modelRearwardOffset,originalCrews=trucks.Length,uniqueFootprints=originals.Length};
            var model=actor.motor.transform;var bone=model.GetComponentsInChildren<Transform>(true).Single(value=>value.name==species.saddleBone);
            var probe=CreateOriginalFootprintProbe(actor,species,calibration);var probeObject=probe.gameObject;
            var physicalGeometryProbes=new Dictionary<OriginalFootprint,SportsEventTruck>();
            var trials=MountedAnimationTrials(actor,species);var checkedFinite=new HashSet<OriginalFootprint>();
            try
            {
                foreach(var original in originals)
                {
                    var clone=UnityEngine.Object.Instantiate(original.sourceTruck.gameObject);clone.hideFlags=HideFlags.HideAndDontSave;clone.SetActive(true);foreach(var body in clone.GetComponentsInChildren<Rigidbody>(true))body.isKinematic=true;physicalGeometryProbes.Add(original,clone.GetComponent<SportsEventTruck>());
                }
                foreach(var trial in trials)
                {
                    foreach(var pose in originalPoses)pose.Restore();
                    SampleMountedTrial(actor,trial);
                    try{probe.CaptureEvaluatedAnimationPose();}
                    catch(InvalidOperationException error)
                    {
                        var failed=new FootprintPose{pose=trial.name,effectiveClip=trial.actualConfiguredFlight?"ACTUAL_CONFIGURED_NATIVE_FLIGHT_STATE_THREE_LEAVES":trial.clip!=null?AssetDatabase.GetAssetPath(trial.clip):"Configured staged pose",incomingClip=trial.incoming!=null?AssetDatabase.GetAssetPath(trial.incoming):null,incomingWeight=trial.incomingWeight,failure=error.Message,minimumSkinGap=float.MinValue,platformGap=float.MaxValue};CaptureConfiguredStateProof(actor,trial,failed);receipt.poses.Add(failed);
                        if(string.IsNullOrEmpty(receipt.failure))receipt.failure=trial.name+" / "+error.Message;
                        continue;
                    }
                    var platform=probe.ReadPose();
                    foreach(var original in originals)
                    {
                        var row=new FootprintPose{pose=trial.name,owner=original.owner,effectiveClip=trial.actualConfiguredFlight?"ACTUAL_CONFIGURED_NATIVE_FLIGHT_STATE_THREE_LEAVES":trial.clip!=null?AssetDatabase.GetAssetPath(trial.clip):species.id=="dragon-fire"?AssetDatabase.GetAssetPath(calibration.clip):"Configured imported default; no evaluated clip claimed",incomingClip=trial.incoming!=null?AssetDatabase.GetAssetPath(trial.incoming):null,incomingWeight=trial.incomingWeight,platform=platform.position,minimumSkinGap=float.MaxValue};CaptureConfiguredStateProof(actor,trial,row);var points=new List<FootprintPoint>();
                        Vector3 root=platform.position+platform.rotation*Vector3.up*original.support;
                        if(!physicalGeometryProbes.TryGetValue(original,out var actualTruck))throw new InvalidOperationException("Actual original finite NWH geometry missing.");
                        actualTruck.transform.SetPositionAndRotation(root,platform.rotation);var shapes=actualTruck.GetComponentsInChildren<Collider>(true).Where(value=>value.enabled&&!value.isTrigger&&value.gameObject.activeInHierarchy).ToArray();
                        if(shapes.Length==0||shapes.Any(value=>value is not BoxCollider))throw new InvalidOperationException("Finite actual original shape proof requires every enabled solid NWH box.");var boxes=shapes.Cast<BoxCollider>().ToArray();
                        if(checkedFinite.Add(original)){Physics.SyncTransforms();foreach(var box in boxes)receipt.finiteContactControls.Add(CheckFiniteOriginalContactControls(box,actualTruck.transform));receipt.finiteContactControlsPassed=receipt.finiteContactControls.All(value=>value.passed);}
                        if(species.id=="dragon-fire"){var tail=new ActualFlightReplay();InspectActualTailAgainstOriginalBoxes(probe.surfaceRenderer,shapes,actualTruck.transform,tail);row.actualTailTrianglesTested=tail.actualTailTrianglesTested;row.actualTailBoxIntersections=tail.actualTailBoxIntersections;row.actualTailIntersections=tail.actualTailIntersectionProofs;}
                        for(int index=0;index<original.bottom.Length;index++)
                        {
                            Vector3 bottom=root+platform.rotation*original.bottom[index];bool found=TryMeasureLiveDorsalSupport(model,bone,bottom,out var skin);
                            float gap=found?Vector3.Dot(bottom-skin,model.up):float.MaxValue;
                            var point=new FootprintPoint{bottom=bottom,skin=skin,torso=found,gap=gap,contributesToFiniteMinimum=found};if(found)row.minimumRawProjectionGap=Mathf.Min(row.minimumRawProjectionGap,gap);
                            if(found&&gap<-.15f)
                            {
                                // Explain only a ray that already violates the
                                // unchanged physical gate. Reuse the same real
                                // native skin/triangle oracle without moving
                                // any pose, selecting a different hit or
                                // substituting a torso-only envelope.
                                Vector3 query=model.InverseTransformPoint(bottom),anchor=model.InverseTransformPoint(bone.position);
                                Vector3 explained=DorsalSurface(model,bone,new Vector2(query.x-anchor.x,query.z-anchor.z),"actual-violating-original-truck-underside",out var proof,true);
                                if(Vector3.Distance(explained,skin)>.0001f||proof==null)throw new InvalidOperationException("Read-only penetrating-ray explanation differs from the actual measured skin hit.");
                                point.actualPenetratingTriangle=proof;receipt.violatingRayProofs++;
                                var actual=proof.vertexInfluences.Select(value=>model.TransformPoint(value.cpuExpectedModelPosition)).ToArray();if(actual.Length!=3)throw new InvalidOperationException("Exact actual source triangle required for finite original-box contact.");
                                point.finiteOriginalContact=MeasureFiniteOriginalContact(actualTruck.transform,boxes,skin,actual[0],actual[1],actual[2]);point.contributesToFiniteMinimum=point.finiteOriginalContact.requiresFiniteClearance;
                            }
                            points.Add(point);
                            if(found&&point.contributesToFiniteMinimum)row.minimumSkinGap=Mathf.Min(row.minimumSkinGap,gap);if(index==4)row.centerTorso=found;
                        }
                        row.points=points.ToArray();row.platformTorso=TryMeasureLiveDorsalSupport(model,bone,platform.position,out var platformSkin);row.skinUnderPlatform=platformSkin;row.platformGap=row.platformTorso?Vector3.Dot(platform.position-platformSkin,model.up):float.MaxValue;receipt.poses.Add(row);
                        if(!row.centerTorso||!row.platformTorso||row.minimumSkinGap<-.15f||row.platformGap<.3f||row.platformGap>.9f||row.actualTailBoxIntersections>0)
                        {
                            if(string.IsNullOrEmpty(receipt.failure))receipt.failure=trial.name+" / "+original.owner+" centerTorso="+row.centerTorso+" platformTorso="+row.platformTorso+" minSkinGap="+row.minimumSkinGap+" directPlatformGap="+row.platformGap+" actualTailSolidBoxIntersections="+row.actualTailBoxIntersections;
                        }
                    }
                }
                receipt.fits=string.IsNullOrEmpty(receipt.failure);return receipt.fits;
            }
            finally{foreach(var actual in physicalGeometryProbes.Values)if(actual!=null)UnityEngine.Object.DestroyImmediate(actual.gameObject);probe.ReleaseGeometrySnapshot();UnityEngine.Object.DestroyImmediate(probeObject);foreach(var pose in originalPoses)pose.Restore();}
        }
        static DragonRiderSaddle CreateOriginalFootprintProbe(DragonCharacter actor,DragonSpeciesDefinition species,Calibration calibration)
        {
            var probeObject=new GameObject("Read-only actual original NWH saddle footprint"){hideFlags=HideFlags.HideAndDontSave};var probe=probeObject.AddComponent<DragonRiderSaddle>();
            probe.actor=actor;probe.sourceBone=actor.motor.GetComponentsInChildren<Transform>(true).Single(value=>value.name==species.saddleBone);probe.dorsalClearance=.45f;probe.maximumBank=25;probe.maximumPitch=20;probe.maximumYaw=15;
            probe.surfaceRenderer=actor.motor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(value=>value.sharedMesh!=null&&AssetDatabase.GetAssetPath(value.sharedMesh)==calibration.anchorMesh);
            probe.surfaceVertexA=calibration.anchorA;probe.surfaceVertexB=calibration.anchorB;probe.surfaceVertexC=calibration.anchorC;probe.surfaceBarycentric=calibration.anchorBarycentric;probe.surfacePatchAnchors=calibration.patchAnchors;return probe;
        }
        static void CaptureConfiguredStateProof(DragonCharacter actor,MountedPoseTrial trial,FootprintPose row)
        {
            if(!trial.actualConfiguredFlight)return;var animator=actor.motor.animator;var leaves=animator.GetCurrentAnimatorClipInfo(0).Where(value=>value.weight>.00001f).ToArray();row.actualConfiguredLeafPaths=leaves.Select(value=>AssetDatabase.GetAssetPath(value.clip)).ToArray();row.actualConfiguredLeafWeights=leaves.Select(value=>value.weight).ToArray();row.actualConfiguredPhase=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;row.actualConfiguredHorizontal=animator.GetFloat("Horizontal");row.actualConfiguredVertical=animator.GetFloat("Vertical");
        }
        static void WriteFootprintReceipt(FootprintReceipt receipt)
        {
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)return;
            string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring");Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"footprint-"+receipt.species+"-"+receipt.rearward.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+".json"),JsonUtility.ToJson(receipt,true));
        }
    }
}
#endif
