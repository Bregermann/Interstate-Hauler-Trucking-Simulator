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
        [Serializable]sealed class InterferencePose
        {
            public string scope="READ_ONLY_NATIVE_SAME_PHASE_FULL_ORIGINAL_NWH_CLONE_AND_ACTUAL_ANIMATED_MOUNT_GEOMETRY_NOT_GAMEPLAY_ACCEPTANCE",pose,sourceTruck;
            public int originalBodies,clonedBodies,originalSolidColliders,clonedSolidColliders,renderers,rawWingRays,rawWingPointsInsideOriginalColliders;
            public float originalMass,clonedMass;
            public bool originalGeometryPreserved,actualColliderQueryControlsPassed;
            public float insideControlDistance,outsideControlDistance;
            public Vector3 truckPosition,knownInsidePoint,knownInsideClosest,knownOutsidePoint,knownOutsideClosest;public Quaternion truckRotation;
            public InterferenceRay[] rays;public string[] images;
            public DeckWheelSeating actualWheelDeckSeating;
        }
        [Serializable]sealed class InterferenceRay
        {
            public int index;public Vector3 physicalUnderside,rawHighestSkin,anatomicalSkin;
            public bool rawFound,anatomicalFound,rawIsMajorityWing,insideAnyOriginalSolidCollider;
            public float rawGap,anatomicalGap;
            public SurfaceProof rawProof,anatomicalProof;
            public ColliderDistance[] actualColliderClosestPoints;
        }
        [Serializable]sealed class ColliderDistance
        {public string path,type;public bool enabled,active,pointInsideOrOnBoundary;public Vector3 closestPoint;public float distance;}

        static InterferencePose[] ReviewActualWingInterference(DragonCharacter source,DragonSpeciesDefinition species,Calibration calibration,SportsEventTruck originalTruck,Transform originalDeck)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit native evidence output required.");
            string output=Path.Combine(args[at+1],"RiderSaddleAuthoring","ActualWingCabInterference");Directory.CreateDirectory(output);
            string[] requested={"TurnRight phase=0","Breath phase=0","Actual graph blend Turn Left -> Dive weight=0.75","Actual graph blend Breath -> Fly Idle weight=0.75"};
            var preview=new PreviewRenderUtility();GameObject actorObject=null,truckObject=null,deck=null;DragonRiderSaddle probe=null;
            try
            {
                actorObject=UnityEngine.Object.Instantiate(source.gameObject);preview.AddSingleGO(actorObject);actorObject.SetActive(true);
                var actor=actorObject.GetComponent<DragonCharacter>();actor.motor.animator.enabled=false;
                var model=actor.motor.transform;var bone=model.GetComponentsInChildren<Transform>(true).Single(value=>value.name==species.saddleBone);
                var poses=model.GetComponentsInChildren<Transform>(true).Select(value=>new BonePose{bone=value,position=value.localPosition,rotation=value.localRotation}).ToArray();
                var trials=MountedAnimationTrials(actor,species);
                truckObject=UnityEngine.Object.Instantiate(originalTruck.gameObject);preview.AddSingleGO(truckObject);truckObject.SetActive(true);
                var truck=truckObject.GetComponent<SportsEventTruck>();var originalBodies=originalTruck.GetComponentsInChildren<Rigidbody>(true);var bodies=truck.GetComponentsInChildren<Rigidbody>(true);
                foreach(var body in bodies){body.isKinematic=true;body.interpolation=RigidbodyInterpolation.None;body.detectCollisions=true;}
                foreach(var body in actor.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
                var originalShapes=originalTruck.GetComponentsInChildren<Collider>(true).Where(value=>value.enabled&&!value.isTrigger).ToArray();
                var shapes=truck.GetComponentsInChildren<Collider>(true).Where(value=>value.enabled&&!value.isTrigger).ToArray();
                if(originalBodies.Length!=bodies.Length||originalShapes.Length!=shapes.Length||originalShapes.Length==0)throw new InvalidOperationException("Full original NWH assembly geometry required for interference diagnosis.");
                if(originalDeck==null)throw new InvalidOperationException("Actual saved deck presentation required for faithful mounted diagnostic images.");
                deck=UnityEngine.Object.Instantiate(originalDeck.gameObject);preview.AddSingleGO(deck);deck.SetActive(true);
                probe=CreateOriginalFootprintProbe(actor,species,calibration);probe.supportPresentation=deck.transform;
                var original=OriginalFootprints(new[]{originalTruck}).Single();var frames=new List<InterferencePose>();
                preview.ambientColor=new Color(.35f,.38f,.43f,1);preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(40,35,0);preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(310,210,0);
                preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.09f,.12f,.16f,1);preview.camera.fieldOfView=38;preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=500;
                for(int index=0;index<requested.Length;index++)
                {
                    var trial=trials.Single(value=>value.name==requested[index]);foreach(var pose in poses)pose.Restore();SampleMountedTrial(actor,trial);probe.CaptureEvaluatedAnimationPose();var platform=probe.ReadPose();
                    Vector3 position=platform.position+platform.rotation*Vector3.up*original.support;
                    truck.transform.SetPositionAndRotation(position,platform.rotation);Physics.SyncTransforms();
                    var frame=new InterferencePose{pose=trial.name,sourceTruck=originalTruck.name,truckPosition=position,truckRotation=platform.rotation,originalBodies=originalBodies.Length,clonedBodies=bodies.Length,originalMass=originalBodies.Sum(value=>value.mass),clonedMass=bodies.Sum(value=>value.mass),originalSolidColliders=originalShapes.Length,clonedSolidColliders=shapes.Length};
                    frame.originalGeometryPreserved=frame.originalBodies==frame.clonedBodies&&frame.originalMass==frame.clonedMass&&frame.originalSolidColliders==frame.clonedSolidColliders&&truck.shape.size==originalTruck.shape.size&&truck.shape.center==originalTruck.shape.center&&truck.transform.lossyScale==originalTruck.transform.lossyScale;
                    if(!frame.originalGeometryPreserved)throw new InvalidOperationException("Actual original NWH shape, scale, body count or masses changed in the read-only render clone.");
                    frame.knownInsidePoint=truck.shape.transform.TransformPoint(truck.shape.center);frame.knownOutsidePoint=frame.knownInsidePoint+Vector3.one.normalized*1000;
                    frame.knownInsideClosest=truck.shape.ClosestPoint(frame.knownInsidePoint);frame.knownOutsideClosest=truck.shape.ClosestPoint(frame.knownOutsidePoint);
                    frame.insideControlDistance=Vector3.Distance(frame.knownInsideClosest,frame.knownInsidePoint);frame.outsideControlDistance=Vector3.Distance(frame.knownOutsideClosest,frame.knownOutsidePoint);
                    frame.actualColliderQueryControlsPassed=truck.shape.enabled&&truck.shape.gameObject.activeInHierarchy&&truck.shape.bounds.extents.magnitude>.5f&&frame.insideControlDistance<.0001f&&frame.outsideControlDistance>900;
                    if(!frame.actualColliderQueryControlsPassed)throw new InvalidOperationException("Preview-scene original NWH collider failed known-inside/outside native ClosestPoint controls; raw wing containment cannot be interpreted.");
                    frame.actualWheelDeckSeating=MeasureActualDeckSeating(truck,deck.transform,platform.rotation*Vector3.up,"READ_ONLY_EDITOR_FULL_ORIGINAL_NWH_CLONE_DISABLED_WHEEL_POSE_NOT_LIVE_SUSPENSION_ACCEPTANCE");
                    var rays=new List<InterferenceRay>();
                    for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                    {
                        Vector3 bottom=truck.shape.transform.TransformPoint(truck.shape.center+Vector3.Scale(truck.shape.size*.5f,new Vector3(x,-1,z))),query=model.InverseTransformPoint(bottom),origin=model.InverseTransformPoint(bone.position);
                        int rayIndex=(x+1)*3+z+1;Vector3 expected=position+platform.rotation*original.bottom[rayIndex];
                        if(Vector3.Distance(bottom,expected)>.0001f)throw new InvalidOperationException("Rendered original NWH underside differs from the actual native footprint witness.");
                        var ray=new InterferenceRay{index=rayIndex,physicalUnderside=bottom};
                        try{ray.rawHighestSkin=DorsalSurface(model,bone,new Vector2(query.x-origin.x,query.z-origin.z),"actual-raw-local-skin-including-wing-panel",out var proof,true,true);ray.rawProof=proof;ray.rawFound=true;ray.rawIsMajorityWing=proof.wingDominated;ray.rawGap=Vector3.Dot(bottom-ray.rawHighestSkin,model.up);}
                        catch(DorsalPatchUnavailable){ray.rawFound=false;}
                        try{ray.anatomicalSkin=DorsalSurface(model,bone,new Vector2(query.x-origin.x,query.z-origin.z),"actual-anatomical-dorsal-support-same-ray",out var proof,true);ray.anatomicalProof=proof;ray.anatomicalFound=true;ray.anatomicalGap=Vector3.Dot(bottom-ray.anatomicalSkin,model.up);}
                        catch(DorsalPatchUnavailable){ray.anatomicalFound=false;}
                        if(ray.rawFound&&ray.rawIsMajorityWing)
                        {
                            frame.rawWingRays++;
                            ray.actualColliderClosestPoints=shapes.Select(shape=>{Vector3 closest=shape.ClosestPoint(ray.rawHighestSkin);float distance=Vector3.Distance(closest,ray.rawHighestSkin);return new ColliderDistance{path=AnimationUtility.CalculateTransformPath(shape.transform,truck.transform),type=shape.GetType().Name,enabled=shape.enabled,active=shape.gameObject.activeInHierarchy,closestPoint=closest,distance=distance,pointInsideOrOnBoundary=distance<.0001f};}).OrderBy(value=>value.distance).ToArray();
                            ray.insideAnyOriginalSolidCollider=ray.actualColliderClosestPoints.Any(value=>value.enabled&&value.active&&value.pointInsideOrOnBoundary);if(ray.insideAnyOriginalSolidCollider)frame.rawWingPointsInsideOriginalColliders++;
                        }
                        rays.Add(ray);
                    }
                    frame.rays=rays.ToArray();var actorBounds=ActualRenderedGeometryBounds(actorObject);var truckBounds=ActualRenderedGeometryBounds(truckObject);actorBounds.Encapsulate(truckBounds);frame.renderers=truckObject.GetComponentsInChildren<Renderer>(true).Count(value=>value.enabled&&value.gameObject.activeInHierarchy);
                    string wide=Path.Combine(output,"pose-"+index+"-full-mount.png"),close=Path.Combine(output,"pose-"+index+"-cab-side.png");
                    CaptureInterference(preview,actorBounds.center,new Vector3(-1,.55f,1),Mathf.Max(8,actorBounds.extents.magnitude)*2.8f,wide);
                    var raw=rays.FirstOrDefault(value=>value.rawFound&&value.rawIsMajorityWing&&value.rawGap<-.15f);float side=raw==null?-1:Mathf.Sign(raw.rawHighestSkin.x-truckBounds.center.x);
                    CaptureInterference(preview,truckBounds.center,new Vector3(side,-.15f,.35f),Mathf.Max(4,truckBounds.extents.magnitude)*2.8f,close);frame.images=new[]{wide,close};frames.Add(frame);
                }
                return frames.ToArray();
            }
            finally
            {
                if(probe!=null){probe.ReleaseGeometrySnapshot();UnityEngine.Object.DestroyImmediate(probe.gameObject);}
                preview.Cleanup();
            }
        }
        static Bounds ActualRenderedGeometryBounds(GameObject root)
        {
            bool found=false;Bounds bounds=default;
            void Point(Vector3 value){if(found)bounds.Encapsulate(value);else{bounds=new Bounds(value,Vector3.zero);found=true;}}
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(value=>value.enabled&&value.gameObject.activeInHierarchy))
            {
                var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;var local=mesh.bounds;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)Point(renderer.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3(x,y,z))));
            }
            foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(value=>value.enabled&&value.gameObject.activeInHierarchy&&value.sharedMesh!=null))
            {
                var mesh=new Mesh();try{renderer.BakeMesh(mesh,true);foreach(var point in mesh.vertices)Point(renderer.transform.TransformPoint(point));}finally{UnityEngine.Object.DestroyImmediate(mesh);}
            }
            if(!found)throw new InvalidOperationException("Actual full source model render geometry is missing; renderer/effect bounds cannot substitute.");return bounds;
        }
        static void CaptureInterference(PreviewRenderUtility preview,Vector3 target,Vector3 direction,float distance,string path,int width=1280,int height=720)
        {
            preview.camera.transform.SetPositionAndRotation(target+direction.normalized*distance,Quaternion.LookRotation(-direction.normalized,Vector3.up));
            preview.BeginStaticPreview(new Rect(0,0,width,height));preview.Render(true);var texture=preview.EndStaticPreview();
            try{if(texture==null||texture.width!=width||texture.height!=height)throw new InvalidOperationException("Actual native preview render/readback failed.");File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally{if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
#endif
